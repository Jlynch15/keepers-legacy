import CoreData
import Foundation

// MARK: - Notifications
// Used for loose coupling between ViewModels when inventory changes.

extension Notification.Name {
    /// Posted whenever a creature is added, sold, or moved.
    static let creatureInventoryDidChange = Notification.Name("creatureInventoryDidChange")
    /// Posted whenever a customer order is fulfilled or expires.
    static let customerOrdersDidChange    = Notification.Name("customerOrdersDidChange")
    /// Posted when game state changes that may trigger story events (orders, breeding).
    static let storyCheckNeeded           = Notification.Name("storyCheckNeeded")
}

// MARK: - DataManager

final class DataManager: ObservableObject {

    static let shared = DataManager()

    // MARK: Core Data Stack

    let container: NSPersistentCloudKitContainer

    init(inMemory: Bool = false) {
        container = NSPersistentCloudKitContainer(name: "KeeperLegacy")

        if inMemory {
            container.persistentStoreDescriptions.first?.url = URL(fileURLWithPath: "/dev/null")
        }

        if let description = container.persistentStoreDescriptions.first {
            description.setOption(true as NSNumber, forKey: NSPersistentHistoryTrackingKey)
            description.setOption(true as NSNumber, forKey: NSPersistentStoreRemoteChangeNotificationPostOptionKey)
        }

        container.loadPersistentStores { _, error in
            if let error = error as NSError? {
                fatalError("Core Data failed to load: \(error), \(error.userInfo)")
            }
        }

        container.viewContext.automaticallyMergesChangesFromParent = true
        container.viewContext.mergePolicy = NSMergeByPropertyObjectTrumpMergePolicy
    }

    var context: NSManagedObjectContext { container.viewContext }

    func save() {
        guard context.hasChanges else { return }
        do {
            try context.save()
        } catch {
            print("[DataManager] Save failed: \(error)")
        }
    }

    // MARK: - Player State

    func playerState() -> PlayerStateEntity {
        let request = PlayerStateEntity.fetchRequest()
        request.fetchLimit = 1
        if let existing = try? context.fetch(request).first {
            return existing
        }
        let state = PlayerStateEntity(context: context)
        state.coins                  = 500
        state.stardust               = 0
        state.currentLevel           = 1
        state.currentXP              = 0
        state.storyAct               = 1
        state.totalCoinsEarned       = 500
        state.unlockedFeaturesData   = defaultUnlockedFeatures()
        state.discoveredCatalogIDsData = nil
        state.lastOrderGeneratedAt   = nil
        save()
        return state
    }

    private func defaultUnlockedFeatures() -> Data? {
        try? JSONEncoder().encode(Set(["shop", "habitat", "feeding", "playing"]))
    }

    // MARK: - Discovery Tracking

    func loadDiscoveredCatalogIDs() -> Set<String> {
        guard let data = playerState().discoveredCatalogIDsData else { return [] }
        return (try? JSONDecoder().decode(Set<String>.self, from: data)) ?? []
    }

    func saveDiscoveredCatalogIDs(_ ids: Set<String>) {
        playerState().discoveredCatalogIDsData = try? JSONEncoder().encode(ids)
        save()
    }

    func markDiscovered(catalogID: String) {
        var ids = loadDiscoveredCatalogIDs()
        guard !ids.contains(catalogID) else { return }
        ids.insert(catalogID)
        saveDiscoveredCatalogIDs(ids)
    }

    // MARK: - Creatures

    func allOwnedCreatures() -> [CreatureEntity] {
        let request = CreatureEntity.fetchRequest()
        request.sortDescriptors = [NSSortDescriptor(keyPath: \CreatureEntity.dateAcquired, ascending: true)]
        return (try? context.fetch(request)) ?? []
    }

    func creature(withID id: UUID) -> CreatureEntity? {
        let request = CreatureEntity.fetchRequest()
        request.predicate = NSPredicate(format: "id == %@", id as CVarArg)
        request.fetchLimit = 1
        return try? context.fetch(request).first
    }

    /// Returns owned creatures matching a catalog ID and minimum happiness.
    func ownedCreatures(matchingCatalogID catalogID: String, minHappiness: Double) -> [CreatureEntity] {
        let request = CreatureEntity.fetchRequest()
        request.predicate = NSPredicate(
            format: "catalogID == %@ AND happiness >= %f",
            catalogID, minHappiness
        )
        return (try? context.fetch(request)) ?? []
    }

    @discardableResult
    func createCreature(catalogID: String, mutationIndex: Int = 0, parentIDs: [UUID] = []) -> CreatureEntity {
        let entity                   = CreatureEntity(context: context)
        entity.id                    = UUID()
        entity.catalogID             = catalogID
        entity.mutationIndex         = Int16(mutationIndex)
        entity.hunger                = 0.8
        entity.happiness             = 0.7
        entity.cleanliness           = 0.9
        entity.affection             = 0.5
        entity.playfulness           = 0.8
        entity.lifecycle             = LifecycleStage.adult.rawValue
        entity.lifecycleStartDate    = Date()
        entity.discoveredFavoriteToy = false
        entity.parentIDsData         = try? JSONEncoder().encode(parentIDs)
        entity.dateAcquired          = Date()
        markDiscovered(catalogID: catalogID)
        save()
        NotificationCenter.default.post(name: .creatureInventoryDidChange, object: nil)
        return entity
    }

    func deleteCreature(_ entity: CreatureEntity) {
        context.delete(entity)
        save()
        NotificationCenter.default.post(name: .creatureInventoryDidChange, object: nil)
    }

    // MARK: - Habitats

    func allHabitats() -> [HabitatEntity] {
        let request = HabitatEntity.fetchRequest()
        request.sortDescriptors = [NSSortDescriptor(keyPath: \HabitatEntity.unlockedAtLevel, ascending: true)]
        return (try? context.fetch(request)) ?? []
    }

    func setupInitialHabitats() {
        guard allHabitats().isEmpty else { return }
        let starter              = HabitatEntity(context: context)
        starter.id               = UUID()
        starter.type             = HabitatType.water.rawValue
        starter.unlockedAtLevel  = 0
        starter.decorationIDsData = try? JSONEncoder().encode([String]())
        save()
    }

    func addHabitat(type: HabitatType, unlockedAtLevel: Int) {
        let entity              = HabitatEntity(context: context)
        entity.id               = UUID()
        entity.type             = type.rawValue
        entity.unlockedAtLevel  = Int16(unlockedAtLevel)
        entity.decorationIDsData = try? JSONEncoder().encode([String]())
        save()
    }

    // MARK: - Customer Orders

    func activeOrders() -> [CustomerOrderEntity] {
        let request = CustomerOrderEntity.fetchRequest()
        request.predicate = NSPredicate(
            format: "isFulfilled == NO AND expiresAt > %@", Date() as CVarArg
        )
        request.sortDescriptors = [NSSortDescriptor(keyPath: \CustomerOrderEntity.expiresAt, ascending: true)]
        return (try? context.fetch(request)) ?? []
    }

    func allOrders() -> [CustomerOrderEntity] {
        let request = CustomerOrderEntity.fetchRequest()
        request.sortDescriptors = [NSSortDescriptor(keyPath: \CustomerOrderEntity.expiresAt, ascending: true)]
        return (try? context.fetch(request)) ?? []
    }

    @discardableResult
    func createOrder(
        creatureCatalogID: String,
        rarity: Rarity,
        minHappiness: Double,
        coinReward: Int,
        expiresInHours: Double = 8
    ) -> CustomerOrderEntity {
        let entity               = CustomerOrderEntity(context: context)
        entity.id                = UUID()
        entity.requiredCatalogID = creatureCatalogID
        entity.requiredRarity    = rarity.rawValue
        entity.minHappiness      = minHappiness
        entity.coinReward        = Int32(coinReward)
        entity.expiresAt         = Date().addingTimeInterval(expiresInHours * 3600)
        entity.isFulfilled       = false
        save()
        return entity
    }

    /// Generates new orders to fill up to `targetCount` active slots.
    /// Only generates orders for creatures the player has already discovered (more fun, less frustrating).
    /// Falls back to any creature if no discovered ones exist yet.
    func generateOrdersIfNeeded(targetCount: Int = 3, discoveredIDs: Set<String>) {
        let current = activeOrders().count
        guard current < targetCount else { return }

        let needed = targetCount - current

        // Pull from discovered creatures first; fall back to all if needed
        let pool: [CreatureCatalogEntry]
        let discovered = CreatureCatalogEntry.allCreatures.filter { discoveredIDs.contains($0.id) }
        pool = discovered.isEmpty ? Array(CreatureCatalogEntry.allCreatures.prefix(20)) : discovered

        // Avoid duplicate catalog IDs in the same batch
        let existingIDs = Set(activeOrders().compactMap { $0.requiredCatalogID })
        let candidates  = pool.filter { !existingIDs.contains($0.id) }.shuffled()

        for entry in candidates.prefix(needed) {
            let range  = PricingTable.CustomerOrder.rewardRange(rarity: entry.rarity)
            let reward = Int.random(in: range)
            let minHappiness = entry.rarity == .rare ? 0.5 : 0.3
            let expiryHours  = Double.random(in: 6...8)
            createOrder(
                creatureCatalogID: entry.id,
                rarity:            entry.rarity,
                minHappiness:      minHappiness,
                coinReward:        reward,
                expiresInHours:    expiryHours
            )
        }

        playerState().lastOrderGeneratedAt = Date()
        save()
        NotificationCenter.default.post(name: .customerOrdersDidChange, object: nil)
    }

    /// Remove expired and fulfilled orders older than 24 hours to keep the DB clean.
    func pruneStaleOrders() {
        let cutoff  = Date().addingTimeInterval(-86400)
        let request = CustomerOrderEntity.fetchRequest()
        request.predicate = NSPredicate(
            format: "(isFulfilled == YES OR expiresAt < %@) AND expiresAt < %@",
            Date() as CVarArg, cutoff as CVarArg
        )
        let stale = (try? context.fetch(request)) ?? []
        stale.forEach { context.delete($0) }
        if !stale.isEmpty { save() }
    }

    // MARK: - Breeding Records

    @discardableResult
    func recordBreeding(parentAID: UUID, parentBID: UUID, offspringID: UUID) -> BreedingRecordEntity {
        let entity         = BreedingRecordEntity(context: context)
        entity.id          = UUID()
        entity.parentAID   = parentAID
        entity.parentBID   = parentBID
        entity.offspringID = offspringID
        entity.date        = Date()
        save()
        return entity
    }

    func allBreedingRecords() -> [BreedingRecordEntity] {
        let request = BreedingRecordEntity.fetchRequest()
        request.sortDescriptors = [NSSortDescriptor(keyPath: \BreedingRecordEntity.date, ascending: false)]
        return (try? context.fetch(request)) ?? []
    }

    func allFulfilledOrdersCount() -> Int {
        let request = CustomerOrderEntity.fetchRequest()
        request.predicate = NSPredicate(format: "isFulfilled == YES")
        return (try? context.count(for: request)) ?? 0
    }

    // MARK: - Story State

    func loadStoryState() -> PlayerStoryState {
        let state = playerState()
        var story = PlayerStoryState()
        story.currentAct = Int(state.storyAct)

        if let data = state.completedEventIDsData,
           let ids = try? JSONDecoder().decode(Set<String>.self, from: data) {
            story.completedEventIDs = ids
        }
        if let data = state.npcRelationshipsData,
           let rels = try? JSONDecoder().decode([String: Int].self, from: data) {
            story.npcRelationshipLevels = rels
        }
        return story
    }

    func saveStoryState(_ story: PlayerStoryState) {
        let state = playerState()
        state.storyAct                = Int16(story.currentAct)
        state.completedEventIDsData   = try? JSONEncoder().encode(story.completedEventIDs)
        state.npcRelationshipsData    = try? JSONEncoder().encode(story.npcRelationshipLevels)
        save()
    }
}

// MARK: - Preview DataManager

extension DataManager {
    static var preview: DataManager = {
        let manager = DataManager(inMemory: true)
        let _       = manager.playerState()
        manager.setupInitialHabitats()
        manager.createCreature(catalogID: "aquaburst",  mutationIndex: 0)
        manager.createCreature(catalogID: "wildbloom",  mutationIndex: 1)
        manager.createCreature(catalogID: "sparkburst", mutationIndex: 0)
        // Seed a couple of sample orders
        manager.createOrder(creatureCatalogID: "aquaburst",  rarity: .common,   minHappiness: 0.3, coinReward: 120)
        manager.createOrder(creatureCatalogID: "cinderborne", rarity: .common,  minHappiness: 0.3, coinReward: 90)
        manager.createOrder(creatureCatalogID: "pearlescent", rarity: .rare,    minHappiness: 0.5, coinReward: 800)
        return manager
    }()
}

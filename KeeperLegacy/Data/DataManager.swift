import CoreData
import Foundation

// MARK: - DataManager
// Central Core Data stack for Keeper's Legacy.
// All reads/writes go through this singleton.

final class DataManager: ObservableObject {

    static let shared = DataManager()

    // MARK: Core Data Stack

    let container: NSPersistentCloudKitContainer

    init(inMemory: Bool = false) {
        container = NSPersistentCloudKitContainer(name: "KeeperLegacy")

        if inMemory {
            container.persistentStoreDescriptions.first?.url = URL(fileURLWithPath: "/dev/null")
        }

        // Enable CloudKit sync (iCloud backup — optional for user)
        if let description = container.persistentStoreDescriptions.first {
            description.setOption(true as NSNumber, forKey: NSPersistentHistoryTrackingKey)
            description.setOption(true as NSNumber, forKey: NSPersistentStoreRemoteChangeNotificationPostOptionKey)
        }

        container.loadPersistentStores { storeDescription, error in
            if let error = error as NSError? {
                // In production, handle gracefully (corrupt store recovery, etc.)
                fatalError("Core Data failed to load: \(error), \(error.userInfo)")
            }
        }

        container.viewContext.automaticallyMergesChangesFromParent = true
        container.viewContext.mergePolicy = NSMergeByPropertyObjectTrumpMergePolicy
    }

    var context: NSManagedObjectContext {
        container.viewContext
    }

    // MARK: Save

    func save() {
        guard context.hasChanges else { return }
        do {
            try context.save()
        } catch {
            print("[DataManager] Save failed: \(error)")
        }
    }

    // MARK: - Player State

    /// Returns the singleton PlayerStateEntity, creating it if it doesn't exist.
    func playerState() -> PlayerStateEntity {
        let request = PlayerStateEntity.fetchRequest()
        request.fetchLimit = 1
        if let existing = try? context.fetch(request).first {
            return existing
        }
        let state = PlayerStateEntity(context: context)
        state.coins          = 500
        state.stardust       = 0
        state.currentLevel   = 1
        state.currentXP      = 0
        state.storyAct       = 1
        state.unlockedFeaturesData = defaultUnlockedFeatures()
        save()
        return state
    }

    private func defaultUnlockedFeatures() -> Data? {
        let features: Set<String> = ["shop", "habitat", "feeding", "playing"]
        return try? JSONEncoder().encode(features)
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

    /// Create a new owned creature from a catalog entry.
    @discardableResult
    func createCreature(catalogID: String, mutationIndex: Int = 0, parentIDs: [UUID] = []) -> CreatureEntity {
        let entity = CreatureEntity(context: context)
        entity.id                  = UUID()
        entity.catalogID           = catalogID
        entity.mutationIndex       = Int16(mutationIndex)
        entity.hunger              = 0.8
        entity.happiness           = 0.7
        entity.cleanliness         = 0.9
        entity.affection           = 0.5
        entity.playfulness         = 0.8
        entity.lifecycle           = LifecycleStage.adult.rawValue
        entity.lifecycleStartDate  = Date()
        entity.discoveredFavoriteToy = false
        entity.parentIDsData       = try? JSONEncoder().encode(parentIDs)
        entity.dateAcquired        = Date()
        save()
        return entity
    }

    func deleteCreature(_ entity: CreatureEntity) {
        context.delete(entity)
        save()
    }

    // MARK: - Habitats

    func allHabitats() -> [HabitatEntity] {
        let request = HabitatEntity.fetchRequest()
        request.sortDescriptors = [NSSortDescriptor(keyPath: \HabitatEntity.unlockedAtLevel, ascending: true)]
        return (try? context.fetch(request)) ?? []
    }

    /// Creates the initial habitat if none exist.
    func setupInitialHabitats() {
        guard allHabitats().isEmpty else { return }
        let starter = HabitatEntity(context: context)
        starter.id             = UUID()
        starter.type           = HabitatType.water.rawValue
        starter.unlockedAtLevel = 0
        starter.decorationIDsData = try? JSONEncoder().encode([String]())
        save()
    }

    func addHabitat(type: HabitatType, unlockedAtLevel: Int) {
        let entity = HabitatEntity(context: context)
        entity.id              = UUID()
        entity.type            = type.rawValue
        entity.unlockedAtLevel = Int16(unlockedAtLevel)
        entity.decorationIDsData = try? JSONEncoder().encode([String]())
        save()
    }

    // MARK: - Customer Orders

    func activeOrders() -> [CustomerOrderEntity] {
        let request = CustomerOrderEntity.fetchRequest()
        request.predicate = NSPredicate(format: "isFulfilled == NO AND expiresAt > %@", Date() as CVarArg)
        return (try? context.fetch(request)) ?? []
    }

    @discardableResult
    func createOrder(
        creatureCatalogID: String,
        rarity: String,
        coinReward: Int,
        expiresInHours: Double = 8
    ) -> CustomerOrderEntity {
        let entity = CustomerOrderEntity(context: context)
        entity.id                    = UUID()
        entity.requiredCatalogID     = creatureCatalogID
        entity.requiredRarity        = rarity
        entity.coinReward            = Int32(coinReward)
        entity.expiresAt             = Date().addingTimeInterval(expiresInHours * 3600)
        entity.isFulfilled           = false
        save()
        return entity
    }

    // MARK: - Breeding Records

    @discardableResult
    func recordBreeding(parentAID: UUID, parentBID: UUID, offspringID: UUID) -> BreedingRecordEntity {
        let entity = BreedingRecordEntity(context: context)
        entity.id          = UUID()
        entity.parentAID   = parentAID
        entity.parentBID   = parentBID
        entity.offspringID = offspringID
        entity.date        = Date()
        save()
        return entity
    }
}

// MARK: - Preview DataManager (in-memory, with sample data)

extension DataManager {
    static var preview: DataManager = {
        let manager = DataManager(inMemory: true)
        let _ = manager.playerState()
        manager.setupInitialHabitats()
        // Add a sample creature
        manager.createCreature(catalogID: "aquaburst", mutationIndex: 0)
        return manager
    }()
}

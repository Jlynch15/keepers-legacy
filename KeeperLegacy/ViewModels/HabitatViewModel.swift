import Foundation
import Combine

// MARK: - HabitatViewModel

@MainActor
final class HabitatViewModel: ObservableObject {
    @Published var habitats: [HabitatEntity] = []

    private let dataManager = DataManager.shared
    private var cancellables = Set<AnyCancellable>()

    init() {
        // Stay in sync when other ViewModels change inventory
        NotificationCenter.default
            .publisher(for: .creatureInventoryDidChange)
            .receive(on: DispatchQueue.main)
            .sink { [weak self] _ in self?.refresh() }
            .store(in: &cancellables)
    }

    // MARK: Load / Refresh

    func load() async {
        dataManager.setupInitialHabitats()
        refresh()
    }

    func refresh() {
        habitats = dataManager.allHabitats()
    }

    // MARK: Creature Lookup

    func creature(withID id: UUID) -> CreatureEntity? {
        dataManager.creature(withID: id)
    }

    // MARK: Habitat Expansion

    func canAddHabitat(atLevel level: Int) -> Bool {
        let current = habitats.filter { $0.type != HabitatType.magical.rawValue }.count
        return current < HabitatUnlockSchedule.habitatsUnlocked(atLevel: level)
    }

    func addNextHabitat(progressVM: ProgressionViewModel) {
        let cost = HabitatExpansionCost.cost(forSlot: habitats.count + 1)
        guard progressVM.spendCoins(cost) else { return }
        dataManager.addHabitat(type: .water, unlockedAtLevel: progressVM.level)
        refresh()
    }

    // MARK: Care Actions

    func feed(creature: CreatureEntity, food: PricingTable.Food, progressVM: ProgressionViewModel) {
        guard progressVM.spendCoins(food.coinCost) else { return }
        creature.hunger    = min(1.0, creature.hunger    + food.hungerRestored)
        creature.happiness = min(1.0, creature.happiness + food.hungerRestored * 0.1)
        dataManager.save()
        progressVM.addXP(XPSource.feed.xpReward)
    }

    func play(
        creature: CreatureEntity,
        toy: String,
        entry: CreatureCatalogEntry?,
        progressVM: ProgressionViewModel
    ) {
        let isFavorite       = toy == entry?.favoriteToy
        let bonus: Double    = isFavorite ? 0.10 : 0.0
        creature.playfulness = min(1.0, creature.playfulness + 0.15 + bonus)
        creature.happiness   = min(1.0, creature.happiness   + 0.15 + bonus)
        creature.affection   = min(1.0, creature.affection   + 0.05)
        if isFavorite && !creature.discoveredFavoriteToy {
            creature.discoveredFavoriteToy = true
        }
        dataManager.save()
        progressVM.addXP(isFavorite ? 15 : XPSource.play.xpReward)
    }

    func clean(creature: CreatureEntity, progressVM: ProgressionViewModel) {
        creature.cleanliness = 1.0
        creature.happiness   = min(1.0, creature.happiness + 0.05)
        dataManager.save()
        progressVM.addXP(XPSource.clean.xpReward)
    }

    /// Sell a creature for coins. Posts .creatureInventoryDidChange automatically via DataManager.
    func sellCreature(
        _ creature: CreatureEntity,
        habitat: HabitatEntity,
        progressVM: ProgressionViewModel
    ) {
        guard let catalogID = creature.catalogID,
              let entry = CreatureCatalogEntry.find(byID: catalogID) else { return }

        let saleValue = PricingTable.sellValue(
            rarity: entry.rarity,
            happinessMultiplier: 1.0 + creature.happiness
        )

        progressVM.earnCoins(saleValue)
        progressVM.addXP(XPSource.sell.xpReward)

        habitat.occupantID = nil
        dataManager.deleteCreature(creature)  // Posts .creatureInventoryDidChange
        refresh()
    }

    // MARK: Stat Decay (called on app foreground)

    func applyDecayForAllCreatures(hoursPassed: Double) {
        for habitat in habitats {
            guard let occupantID = habitat.occupantID,
                  let creature   = dataManager.creature(withID: occupantID) else { continue }
            let rate             = 0.02 * hoursPassed
            creature.hunger      = max(0, creature.hunger      - rate * 1.5)
            creature.happiness   = max(0, creature.happiness   - rate * 1.0)
            creature.cleanliness = max(0, creature.cleanliness - rate * 0.8)
            creature.affection   = max(0, creature.affection   - rate * 0.5)
            creature.playfulness = max(0, creature.playfulness - rate * 1.0)
        }
        dataManager.save()
    }
}

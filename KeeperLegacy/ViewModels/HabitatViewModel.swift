import Foundation
import Combine

// MARK: - HabitatViewModel
// Manages the list of habitats, creature placement, and all care actions
// (feed, play, clean, sell).

@MainActor
final class HabitatViewModel: ObservableObject {
    @Published var habitats: [HabitatEntity] = []

    private let dataManager = DataManager.shared

    // MARK: Load

    func load() async {
        dataManager.setupInitialHabitats()
        habitats = dataManager.allHabitats()
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
        let allowed  = HabitatUnlockSchedule.habitatsUnlocked(atLevel: level)
        return current < allowed
    }

    func addNextHabitat(progressVM: ProgressionViewModel) {
        let currentCount = habitats.count
        let cost = HabitatExpansionCost.cost(forSlot: currentCount + 1)
        guard progressVM.canAffordCoins(cost) else { return }
        guard progressVM.spendCoins(cost) else { return }

        // Default to Water for the new habitat; player can change later
        dataManager.addHabitat(type: .water, unlockedAtLevel: progressVM.level)
        refresh()
    }

    // MARK: Care Actions

    /// Feed a creature with a food item. Deducts coins, updates stats, awards XP.
    func feed(
        creature: CreatureEntity,
        food: PricingTable.Food,
        progressVM: ProgressionViewModel
    ) {
        guard progressVM.spendCoins(food.coinCost) else { return }
        creature.hunger     = min(1.0, creature.hunger + food.hungerRestored)
        creature.happiness  = min(1.0, creature.happiness + food.hungerRestored * 0.1)
        dataManager.save()
        progressVM.addXP(XPSource.feed.xpReward)
    }

    /// Play with a creature using a named toy.
    func play(
        creature: CreatureEntity,
        toy: String,
        entry: CreatureCatalogEntry?,
        progressVM: ProgressionViewModel
    ) {
        let isFavorite = toy == entry?.favoriteToy
        let bonus: Double = isFavorite ? 0.10 : 0.0
        creature.playfulness = min(1.0, creature.playfulness + 0.15 + bonus)
        creature.happiness   = min(1.0, creature.happiness   + 0.15 + bonus)
        creature.affection   = min(1.0, creature.affection   + 0.05)

        if isFavorite && !creature.discoveredFavoriteToy {
            creature.discoveredFavoriteToy = true
        }

        dataManager.save()
        progressVM.addXP(isFavorite ? 15 : XPSource.play.xpReward)
    }

    /// Clean a creature.
    func clean(creature: CreatureEntity, progressVM: ProgressionViewModel) {
        creature.cleanliness = 1.0
        creature.happiness   = min(1.0, creature.happiness + 0.05)
        dataManager.save()
        progressVM.addXP(XPSource.clean.xpReward)
    }

    /// Sell a creature: award coins based on happiness, remove from habitat.
    func sellCreature(
        _ creature: CreatureEntity,
        habitat: HabitatEntity,
        progressVM: ProgressionViewModel
    ) {
        guard let catalogID = creature.catalogID,
              let entry = CreatureCatalogEntry.find(byID: catalogID) else { return }

        let happinessMultiplier = 1.0 + creature.happiness  // 1.0x – 2.0x
        let saleValue = PricingTable.sellValue(
            rarity: entry.rarity,
            happinessMultiplier: happinessMultiplier
        )

        progressVM.earnCoins(saleValue)
        progressVM.addXP(XPSource.sell.xpReward)

        // Remove from habitat
        habitat.occupantID = nil
        dataManager.deleteCreature(creature)
        refresh()
    }

    // MARK: Stat Decay (called on app foreground)

    func applyDecayForAllCreatures(hoursPassed: Double) {
        for habitat in habitats {
            guard let occupantID = habitat.occupantID,
                  let creature = dataManager.creature(withID: occupantID) else { continue }

            let decayRate = 0.02 * hoursPassed
            creature.hunger      = max(0, creature.hunger      - decayRate * 1.5)
            creature.happiness   = max(0, creature.happiness   - decayRate * 1.0)
            creature.cleanliness = max(0, creature.cleanliness - decayRate * 0.8)
            creature.affection   = max(0, creature.affection   - decayRate * 0.5)
            creature.playfulness = max(0, creature.playfulness - decayRate * 1.0)
        }
        dataManager.save()
    }
}

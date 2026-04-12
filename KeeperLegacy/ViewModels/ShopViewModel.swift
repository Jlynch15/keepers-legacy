import Foundation
import Combine

// MARK: - ShopViewModel
// Handles creature purchase logic and tracks which catalog entries the player has
// ever owned (for Monsterpedia discovery state).

@MainActor
final class ShopViewModel: ObservableObject {
    /// Catalog IDs of creatures the player currently owns at least one of
    @Published var ownedCatalogIDs: Set<String> = []

    /// Catalog IDs the player has EVER owned — unlocks Pedia entry
    @Published var discoveredCatalogIDs: Set<String> = []

    private let dataManager = DataManager.shared

    // MARK: Load

    func load() async {
        let owned = dataManager.allOwnedCreatures()
        ownedCatalogIDs     = Set(owned.compactMap { $0.catalogID })
        discoveredCatalogIDs = ownedCatalogIDs  // Expand with persistent tracking later
    }

    // MARK: Queries

    func isOwned(catalogID: String) -> Bool {
        ownedCatalogIDs.contains(catalogID)
    }

    func hasDiscovered(catalogID: String) -> Bool {
        discoveredCatalogIDs.contains(catalogID)
    }

    func discoveredCount(ofType type: HabitatType) -> Int {
        CreatureCatalogEntry.creatures(ofType: type)
            .filter { discoveredCatalogIDs.contains($0.id) }
            .count
    }

    // MARK: Purchase

    /// Deducts coins, creates a CreatureEntity, and places it in the first available matching habitat.
    /// Returns true on success.
    @discardableResult
    func purchase(
        creature: CreatureCatalogEntry,
        mutationIndex: Int,
        cost: Int,
        progressVM: ProgressionViewModel
    ) -> Bool {
        guard progressVM.spendCoins(cost) else { return false }

        let entity = dataManager.createCreature(
            catalogID:     creature.id,
            mutationIndex: mutationIndex
        )

        // Auto-place in first empty matching habitat, or any empty habitat
        let habitats = dataManager.allHabitats()
        let matchingEmpty = habitats.first { h in
            h.occupantID == nil && h.type == creature.habitatType.rawValue
        }
        let anyEmpty = habitats.first { $0.occupantID == nil }

        if let target = matchingEmpty ?? anyEmpty {
            target.occupantID = entity.id
            dataManager.save()
        }

        ownedCatalogIDs.insert(creature.id)
        discoveredCatalogIDs.insert(creature.id)

        progressVM.addXP(XPSource.sell.xpReward)
        return true
    }
}

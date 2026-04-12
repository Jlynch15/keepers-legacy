import Foundation
import Combine

// MARK: - ShopViewModel

@MainActor
final class ShopViewModel: ObservableObject {

    /// Catalog IDs the player currently owns at least one of
    @Published var ownedCatalogIDs: Set<String> = []

    /// Catalog IDs ever owned — persisted, unlocks Monsterpedia entries permanently
    @Published var discoveredCatalogIDs: Set<String> = []

    private let dataManager = DataManager.shared
    private var cancellables = Set<AnyCancellable>()

    init() {
        // Refresh whenever another ViewModel adds or removes a creature
        NotificationCenter.default
            .publisher(for: .creatureInventoryDidChange)
            .receive(on: DispatchQueue.main)
            .sink { [weak self] _ in
                Task { await self?.load() }
            }
            .store(in: &cancellables)
    }

    // MARK: Load

    func load() async {
        let owned = dataManager.allOwnedCreatures()
        ownedCatalogIDs     = Set(owned.compactMap { $0.catalogID })
        discoveredCatalogIDs = dataManager.loadDiscoveredCatalogIDs()
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

    /// Deducts coins, creates a CreatureEntity, places it in the first available habitat.
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

        // Auto-place: prefer a matching habitat type, fall back to any empty slot
        let habitats    = dataManager.allHabitats()
        let bestHabitat = habitats.first { $0.occupantID == nil && $0.type == creature.habitatType.rawValue }
                       ?? habitats.first { $0.occupantID == nil }

        if let target = bestHabitat {
            target.occupantID = entity.id
            dataManager.save()
        }

        // Discovery is handled inside DataManager.createCreature — load to sync
        Task { await load() }
        progressVM.addXP(XPSource.sell.xpReward)
        return true
    }
}

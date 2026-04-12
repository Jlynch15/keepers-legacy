import Foundation
import Combine

// MARK: - CustomerOrderViewModel
// Manages spawning, displaying, and fulfilling customer orders.
// Orders are driven by economy_system_v1.json:
//   - 2-4 active orders at a time
//   - Refresh every 6-8 hours
//   - Coin reward based on rarity
//   - Fulfilling awards coins + XP + relationship boost

@MainActor
final class CustomerOrderViewModel: ObservableObject {

    @Published var activeOrders: [CustomerOrderEntity] = []
    @Published var lastFulfillmentResult: FulfillmentResult? = nil

    enum FulfillmentResult: Identifiable {
        var id: UUID { UUID() }
        case success(creatureName: String, coinsEarned: Int)
        case failure(reason: String)
    }

    private let dataManager    = DataManager.shared
    private var cancellables   = Set<AnyCancellable>()
    private var refreshTimer:  Timer?

    init() {
        NotificationCenter.default
            .publisher(for: .customerOrdersDidChange)
            .receive(on: DispatchQueue.main)
            .sink { [weak self] _ in self?.refresh() }
            .store(in: &cancellables)
    }

    // MARK: Load

    func load(discoveredIDs: Set<String>) async {
        dataManager.pruneStaleOrders()
        dataManager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discoveredIDs)
        refresh()
        startRefreshTimer(discoveredIDs: discoveredIDs)
    }

    func refresh() {
        activeOrders = dataManager.activeOrders()
    }

    // MARK: Fulfillment

    /// Check whether the player has a creature that satisfies this order.
    func canFulfill(_ order: CustomerOrderEntity) -> Bool {
        guard let catalogID = order.requiredCatalogID else { return false }
        let matches = dataManager.ownedCreatures(
            matchingCatalogID: catalogID,
            minHappiness: order.minHappiness
        )
        return !matches.isEmpty
    }

    /// Returns which specific creature would be used to fulfill an order (first eligible).
    func fulfillingCreature(for order: CustomerOrderEntity) -> CreatureEntity? {
        guard let catalogID = order.requiredCatalogID else { return nil }
        return dataManager.ownedCreatures(
            matchingCatalogID: catalogID,
            minHappiness: order.minHappiness
        ).first
    }

    /// Fulfill an order: removes the creature, awards coins + XP, marks order fulfilled.
    func fulfill(order: CustomerOrderEntity, progressVM: ProgressionViewModel) {
        guard let creature = fulfillingCreature(for: order) else {
            lastFulfillmentResult = .failure(reason: "No eligible creature found.")
            return
        }
        guard let catalogID = creature.catalogID,
              let entry = CreatureCatalogEntry.find(byID: catalogID) else {
            lastFulfillmentResult = .failure(reason: "Creature data missing.")
            return
        }

        let coinsEarned = Int(order.coinReward)

        // Award coins and XP
        progressVM.earnCoins(coinsEarned)
        progressVM.addXP(XPSource.fulfillOrder.xpReward)

        // Remove creature from its habitat
        let habitats = dataManager.allHabitats()
        if let habitat = habitats.first(where: { $0.occupantID == creature.id }) {
            habitat.occupantID = nil
        }

        // Mark order fulfilled and delete creature
        order.isFulfilled = true
        dataManager.save()
        dataManager.deleteCreature(creature)   // Posts .creatureInventoryDidChange

        lastFulfillmentResult = .success(creatureName: entry.name, coinsEarned: coinsEarned)

        // Generate a replacement order after a short delay
        DispatchQueue.main.asyncAfter(deadline: .now() + 2) { [weak self] in
            guard let self else { return }
            let discovered = self.dataManager.loadDiscoveredCatalogIDs()
            self.dataManager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discovered)
            self.refresh()
        }
    }

    // MARK: Time Remaining

    /// Human-readable countdown for an order, e.g. "5h 23m"
    func timeRemaining(for order: CustomerOrderEntity) -> String {
        guard let expiry = order.expiresAt else { return "Expired" }
        let remaining = expiry.timeIntervalSince(Date())
        guard remaining > 0 else { return "Expired" }
        let hours   = Int(remaining) / 3600
        let minutes = (Int(remaining) % 3600) / 60
        if hours > 0 { return "\(hours)h \(minutes)m" }
        return "\(minutes)m"
    }

    func urgencyColor(for order: CustomerOrderEntity) -> String {
        guard let expiry = order.expiresAt else { return "#FF6B6B" }
        let remaining = expiry.timeIntervalSince(Date())
        if remaining < 3600 { return "#FF6B6B" }   // < 1 hr: red
        if remaining < 7200 { return "#FFB347" }   // < 2 hr: orange
        return "#A8D5A8"                             // plenty of time: green
    }

    // MARK: Auto-Refresh Timer
    // Checks every 15 minutes whether new orders should be generated.

    private func startRefreshTimer(discoveredIDs: Set<String>) {
        refreshTimer?.invalidate()
        refreshTimer = Timer.scheduledTimer(withTimeInterval: 900, repeats: true) { [weak self] _ in
            Task { @MainActor [weak self] in
                self?.dataManager.pruneStaleOrders()
                self?.dataManager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discoveredIDs)
                self?.refresh()
            }
        }
    }

    deinit {
        refreshTimer?.invalidate()
    }
}

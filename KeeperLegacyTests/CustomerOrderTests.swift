import XCTest
@testable import KeeperLegacy

// MARK: - Customer Order Tests
// Tests for order reward ranges, generation logic, and fulfillment eligibility.

final class CustomerOrderTests: XCTestCase {

    // MARK: Reward Ranges

    func testCommonRewardRangeIsPositive() {
        let range = PricingTable.CustomerOrder.rewardRange(rarity: .common)
        XCTAssertGreaterThan(range.lowerBound, 0)
        XCTAssertGreaterThan(range.upperBound, range.lowerBound)
    }

    func testRarityRewardOrderingMakesEconomicSense() {
        let common   = PricingTable.CustomerOrder.rewardRange(rarity: .common)
        let uncommon = PricingTable.CustomerOrder.rewardRange(rarity: .uncommon)
        let rare     = PricingTable.CustomerOrder.rewardRange(rarity: .rare)

        // Higher rarity → higher minimum and maximum reward
        XCTAssertLessThan(common.lowerBound,   uncommon.lowerBound)
        XCTAssertLessThan(uncommon.lowerBound, rare.lowerBound)
        XCTAssertLessThan(common.upperBound,   uncommon.upperBound)
        XCTAssertLessThan(uncommon.upperBound, rare.upperBound)
    }

    // MARK: Order Construction

    func testOrderInitiallyUnfulfilled() {
        let order = PricingTable.CustomerOrder(
            creatureCatalogID: "aquaburst",
            rarity:            .common,
            minHappiness:      0.3,
            coinReward:        100
        )
        XCTAssertFalse(order.isFulfilled)
    }

    func testOrderExpiresInFuture() {
        let order = PricingTable.CustomerOrder(
            creatureCatalogID: "aquaburst",
            rarity:            .common,
            minHappiness:      0.3,
            coinReward:        100,
            expiresInHours:    8
        )
        XCTAssertGreaterThan(order.expiresAt, Date())
    }

    func testOrderIsNotExpiredOnCreation() {
        let order = PricingTable.CustomerOrder(
            creatureCatalogID: "aquaburst",
            rarity: .common,
            minHappiness: 0.3,
            coinReward: 100
        )
        XCTAssertFalse(order.isExpired)
    }

    func testExpiredOrderDetection() {
        let order = PricingTable.CustomerOrder(
            creatureCatalogID: "aquaburst",
            rarity:            .common,
            minHappiness:      0.3,
            coinReward:        100,
            expiresInHours:   -1    // Expired 1 hour ago
        )
        XCTAssertTrue(order.isExpired)
    }

    // MARK: In-Memory DataManager Integration

    private var manager: DataManager!

    override func setUp() {
        super.setUp()
        manager = DataManager(inMemory: true)
        let _ = manager.playerState()
    }

    override func tearDown() {
        manager = nil
        super.tearDown()
    }

    func testCreateOrderPersists() {
        manager.createOrder(
            creatureCatalogID: "aquaburst",
            rarity:            .common,
            minHappiness:      0.3,
            coinReward:        150
        )
        let orders = manager.activeOrders()
        XCTAssertEqual(orders.count, 1)
        XCTAssertEqual(orders.first?.requiredCatalogID, "aquaburst")
        XCTAssertEqual(orders.first?.coinReward, 150)
    }

    func testActiveOrdersExcludesFulfilledOnes() {
        let order = manager.createOrder(
            creatureCatalogID: "aquaburst",
            rarity:            .common,
            minHappiness:      0.3,
            coinReward:        150
        )
        order.isFulfilled = true
        manager.save()
        XCTAssertEqual(manager.activeOrders().count, 0)
    }

    func testGenerateOrdersFillsUpToTarget() {
        let discovered: Set<String> = ["aquaburst", "wildbloom", "sparkburst", "cinderborne", "frostveil"]
        manager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discovered)
        let orders = manager.activeOrders()
        XCTAssertEqual(orders.count, 3)
    }

    func testGenerateOrdersDoesNotExceedTarget() {
        let discovered: Set<String> = ["aquaburst", "wildbloom", "sparkburst"]
        manager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discovered)
        manager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discovered)  // Called twice
        XCTAssertEqual(manager.activeOrders().count, 3)
    }

    func testGenerateOrdersNoDuplicateCatalogIDs() {
        let discovered: Set<String> = ["aquaburst", "wildbloom", "sparkburst"]
        manager.generateOrdersIfNeeded(targetCount: 3, discoveredIDs: discovered)
        let ids = manager.activeOrders().compactMap { $0.requiredCatalogID }
        XCTAssertEqual(ids.count, Set(ids).count, "Duplicate catalog IDs in orders")
    }

    func testOwnedCreaturesMatchingCatalogIDAndHappiness() {
        // Create two aquabursts — one happy, one not
        let happy   = manager.createCreature(catalogID: "aquaburst")
        happy.happiness = 0.8

        let unhappy = manager.createCreature(catalogID: "aquaburst")
        unhappy.happiness = 0.1
        manager.save()

        let matches = manager.ownedCreatures(matchingCatalogID: "aquaburst", minHappiness: 0.5)
        XCTAssertEqual(matches.count, 1)
        XCTAssertEqual(matches.first?.happiness ?? 0, 0.8, accuracy: 0.01)
    }

    // MARK: Discovery Persistence

    func testDiscoveryPersists() {
        manager.markDiscovered(catalogID: "aquaburst")
        manager.markDiscovered(catalogID: "wildbloom")
        let loaded = manager.loadDiscoveredCatalogIDs()
        XCTAssertTrue(loaded.contains("aquaburst"))
        XCTAssertTrue(loaded.contains("wildbloom"))
    }

    func testMarkDiscoveredIdempotent() {
        manager.markDiscovered(catalogID: "aquaburst")
        manager.markDiscovered(catalogID: "aquaburst")
        let loaded = manager.loadDiscoveredCatalogIDs()
        XCTAssertEqual(loaded.filter { $0 == "aquaburst" }.count, 1)
    }

    func testCreateCreatureAutoMarksDiscovered() {
        manager.createCreature(catalogID: "shimmerstream")
        let discovered = manager.loadDiscoveredCatalogIDs()
        XCTAssertTrue(discovered.contains("shimmerstream"))
    }
}

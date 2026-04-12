import XCTest
@testable import KeeperLegacy

// MARK: - Economy Tests
// Tests for pricing, sell values, and coin transactions.
// These run on every GitHub Actions build to catch economy balance regressions.

final class EconomyTests: XCTestCase {

    // MARK: Pricing Table

    func testCommonCreatureBasePrice() {
        XCTAssertEqual(Rarity.common.basePrice, 150)
    }

    func testRarityPriceOrdering() {
        XCTAssertLessThan(Rarity.common.basePrice, Rarity.uncommon.basePrice)
        XCTAssertLessThan(Rarity.uncommon.basePrice, Rarity.rare.basePrice)
    }

    func testRaritySellValueOrdering() {
        XCTAssertLessThan(Rarity.common.baseSellValue, Rarity.uncommon.baseSellValue)
        XCTAssertLessThan(Rarity.uncommon.baseSellValue, Rarity.rare.baseSellValue)
    }

    func testSellValueWithNeutralHappiness() {
        // At 0.0 happiness multiplier (unhappy) → minimum 1.0x
        let value = PricingTable.sellValue(rarity: .common, happinessMultiplier: 1.0)
        XCTAssertEqual(value, Rarity.common.baseSellValue)
    }

    func testSellValueWithMaxHappiness() {
        // At 2.0 multiplier (max happy) → 2x base
        let value = PricingTable.sellValue(rarity: .common, happinessMultiplier: 2.0)
        XCTAssertEqual(value, Rarity.common.baseSellValue * 2)
    }

    func testSellValueClampsAboveMax() {
        // Multiplier > 2.0 should be clamped to 2.0
        let capped   = PricingTable.sellValue(rarity: .common, happinessMultiplier: 5.0)
        let atMax    = PricingTable.sellValue(rarity: .common, happinessMultiplier: 2.0)
        XCTAssertEqual(capped, atMax)
    }

    // MARK: Player Economy

    func testEarnCoins() {
        var economy = PlayerEconomy(startingCoins: 100)
        economy.earnCoins(50)
        XCTAssertEqual(economy.coins, 150)
        XCTAssertEqual(economy.totalCoinsEarned, 150)
    }

    func testSpendCoinsSuccess() {
        var economy = PlayerEconomy(startingCoins: 200)
        let result = economy.spendCoins(100)
        XCTAssertTrue(result)
        XCTAssertEqual(economy.coins, 100)
        XCTAssertEqual(economy.totalCoinsSpent, 100)
    }

    func testSpendCoinsInsufficientFunds() {
        var economy = PlayerEconomy(startingCoins: 50)
        let result = economy.spendCoins(100)
        XCTAssertFalse(result)
        XCTAssertEqual(economy.coins, 50)  // Unchanged
    }

    func testCannotGoNegative() {
        var economy = PlayerEconomy(startingCoins: 10)
        economy.spendCoins(10)
        let result = economy.spendCoins(1)
        XCTAssertFalse(result)
        XCTAssertEqual(economy.coins, 0)
    }

    // MARK: Breeding Costs

    func testBreedingCostOrdering() {
        let common   = PricingTable.Breeding.cost(rarity: .common)
        let uncommon = PricingTable.Breeding.cost(rarity: .uncommon)
        let rare     = PricingTable.Breeding.cost(rarity: .rare)
        XCTAssertLessThan(common, uncommon)
        XCTAssertLessThan(uncommon, rare)
    }

    // MARK: Food Catalog

    func testFoodCatalogNotEmpty() {
        XCTAssertFalse(PricingTable.Food.catalog.isEmpty)
    }

    func testFoodHungerRestoreRange() {
        for food in PricingTable.Food.catalog {
            XCTAssertGreaterThan(food.hungerRestored, 0.0, "\(food.name) restores nothing")
            XCTAssertLessThanOrEqual(food.hungerRestored, 1.0, "\(food.name) restores over 100%")
        }
    }

    func testFoodCostPositive() {
        for food in PricingTable.Food.catalog {
            XCTAssertGreaterThan(food.coinCost, 0, "\(food.name) has zero cost")
        }
    }
}

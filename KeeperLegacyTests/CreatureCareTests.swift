import XCTest
@testable import KeeperLegacy

// MARK: - Creature Care Tests
// Tests for feeding, playing, cleaning, and stat decay.

final class CreatureCareTests: XCTestCase {

    func makeCreature(catalogID: String = "aquaburst") -> CreatureInstance {
        CreatureInstance(catalogID: catalogID)
    }

    // MARK: Feeding

    func testFeedIncreasesHunger() {
        var creature = makeCreature()
        creature.hunger = 0.2
        creature.feed(foodValue: 0.4)
        XCTAssertEqual(creature.hunger, 0.6, accuracy: 0.001)
    }

    func testFeedClampsAtMax() {
        var creature = makeCreature()
        creature.hunger = 0.9
        creature.feed(foodValue: 0.5)
        XCTAssertEqual(creature.hunger, 1.0)
    }

    func testFeedAlsoBoostsHappiness() {
        var creature = makeCreature()
        let beforeHappiness = creature.happiness
        creature.feed(foodValue: 0.5)
        XCTAssertGreaterThan(creature.happiness, beforeHappiness)
    }

    // MARK: Playing

    func testPlayIncreasesPlayfulness() {
        var creature = makeCreature()
        creature.playfulness = 0.3
        creature.play(toyName: "Ball", isFavoriteToy: false)
        XCTAssertGreaterThan(creature.playfulness, 0.3)
    }

    func testFavoriteToyGivesMoreHappiness() {
        var creature1 = makeCreature()
        var creature2 = makeCreature()
        creature1.happiness = 0.5
        creature2.happiness = 0.5

        creature1.play(toyName: "Random Toy", isFavoriteToy: false)
        creature2.play(toyName: "Favorite Toy", isFavoriteToy: true)

        XCTAssertGreaterThan(creature2.happiness, creature1.happiness)
    }

    func testFavoriteToyDiscovery() {
        var creature = makeCreature()
        XCTAssertFalse(creature.discoveredFavoriteToy)
        creature.play(toyName: "Bubble Wand", isFavoriteToy: true)
        XCTAssertTrue(creature.discoveredFavoriteToy)
    }

    func testFavoriteToyOnlyDiscoveredOnce() {
        var creature = makeCreature()
        let (_, discovered1) = creature.play(toyName: "Bubble Wand", isFavoriteToy: true)
        let (_, discovered2) = creature.play(toyName: "Bubble Wand", isFavoriteToy: true)
        XCTAssertTrue(discovered1)
        XCTAssertFalse(discovered2)   // Second play — already discovered
    }

    // MARK: Cleaning

    func testCleanSetsCleanlinessToFull() {
        var creature = makeCreature()
        creature.cleanliness = 0.1
        creature.clean()
        XCTAssertEqual(creature.cleanliness, 1.0)
    }

    // MARK: Overall Happiness

    func testOverallHappinessIsAverageOfStats() {
        var creature = makeCreature()
        creature.hunger      = 1.0
        creature.happiness   = 1.0
        creature.cleanliness = 1.0
        creature.affection   = 1.0
        creature.playfulness = 1.0
        XCTAssertEqual(creature.overallHappiness, 1.0, accuracy: 0.001)
    }

    func testSellMultiplierAtNeutral() {
        var creature = makeCreature()
        creature.hunger      = 0.5
        creature.happiness   = 0.5
        creature.cleanliness = 0.5
        creature.affection   = 0.5
        creature.playfulness = 0.5
        XCTAssertEqual(creature.sellMultiplier, 1.5, accuracy: 0.001)  // 1.0 + 0.5
    }

    // MARK: Stat Decay

    func testDecayReducesStats() {
        var creature = makeCreature()
        let beforeHunger = creature.hunger
        creature.applyDecay(hoursPassed: 10)
        XCTAssertLessThan(creature.hunger, beforeHunger)
    }

    func testDecayNeverGoesBelowZero() {
        var creature = makeCreature()
        creature.applyDecay(hoursPassed: 10000)
        XCTAssertGreaterThanOrEqual(creature.hunger,      0.0)
        XCTAssertGreaterThanOrEqual(creature.happiness,   0.0)
        XCTAssertGreaterThanOrEqual(creature.cleanliness, 0.0)
        XCTAssertGreaterThanOrEqual(creature.affection,   0.0)
        XCTAssertGreaterThanOrEqual(creature.playfulness, 0.0)
    }

    func testHungerDecaysFasterThanAffection() {
        var creature = makeCreature()
        creature.hunger    = 1.0
        creature.affection = 1.0
        creature.applyDecay(hoursPassed: 5)
        // Hunger decay rate is 1.5x, affection is 0.5x
        let hungerLost    = 1.0 - creature.hunger
        let affectionLost = 1.0 - creature.affection
        XCTAssertGreaterThan(hungerLost, affectionLost)
    }
}

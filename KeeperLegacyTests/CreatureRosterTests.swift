import XCTest
@testable import KeeperLegacy

// MARK: - Creature Roster Tests
// Validates that all 58 creatures are present, correctly typed, and well-formed.

final class CreatureRosterTests: XCTestCase {

    // MARK: Count

    func testTotalCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.allCreatures.count, 58)
    }

    func testWaterCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.waterCreatures.count, 15)
    }

    func testDirtCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.dirtCreatures.count, 15)
    }

    func testGrassCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.grassCreatures.count, 15)
    }

    func testFireCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.fireCreatures.count, 10)
    }

    func testIceCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.iceCreatures.count, 10)
    }

    func testElectricCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.electricCreatures.count, 10)
    }

    func testMagicalCreatureCount() {
        XCTAssertEqual(CreatureCatalogEntry.magicalCreatures.count, 5)
    }

    // MARK: Data Integrity

    func testAllCreaturesHaveUniqueIDs() {
        let ids = CreatureCatalogEntry.allCreatures.map { $0.id }
        let uniqueIDs = Set(ids)
        XCTAssertEqual(ids.count, uniqueIDs.count, "Duplicate creature IDs found")
    }

    func testAllCreaturesHaveNonEmptyNames() {
        for creature in CreatureCatalogEntry.allCreatures {
            XCTAssertFalse(creature.name.isEmpty, "Creature '\(creature.id)' has empty name")
        }
    }

    func testAllCreaturesHaveDescriptions() {
        for creature in CreatureCatalogEntry.allCreatures {
            XCTAssertFalse(creature.description.isEmpty, "'\(creature.name)' has no description")
        }
    }

    func testAllCreaturesHaveFavoriteToy() {
        for creature in CreatureCatalogEntry.allCreatures {
            XCTAssertFalse(creature.favoriteToy.isEmpty, "'\(creature.name)' has no favorite toy")
        }
    }

    func testAllCreaturesHave4Mutations() {
        for creature in CreatureCatalogEntry.allCreatures {
            XCTAssertEqual(creature.mutations.count, 4, "'\(creature.name)' doesn't have 4 mutations")
        }
    }

    func testMutationIndicesAre0To3() {
        for creature in CreatureCatalogEntry.allCreatures {
            let indices = creature.mutations.map { $0.index }
            XCTAssertEqual(Set(indices), Set([0, 1, 2, 3]),
                           "'\(creature.name)' has wrong mutation indices: \(indices)")
        }
    }

    // MARK: Magical Creatures

    func testAllMagicalCreaturesAreRare() {
        for creature in CreatureCatalogEntry.magicalCreatures {
            XCTAssertEqual(creature.rarity, .rare, "'\(creature.name)' magical creature is not rare")
        }
    }

    // MARK: Lookup

    func testFindByIDReturnsCorrectCreature() {
        let result = CreatureCatalogEntry.find(byID: "aquaburst")
        XCTAssertNotNil(result)
        XCTAssertEqual(result?.name, "Aquaburst")
        XCTAssertEqual(result?.habitatType, .water)
    }

    func testFindByIDReturnsNilForUnknown() {
        XCTAssertNil(CreatureCatalogEntry.find(byID: "nonexistent_creature"))
    }

    func testCreaturesFilterByType() {
        let waterOnes = CreatureCatalogEntry.creatures(ofType: .water)
        XCTAssertTrue(waterOnes.allSatisfy { $0.habitatType == .water })
    }
}

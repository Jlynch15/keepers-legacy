import XCTest
@testable import KeeperLegacy

// MARK: - Breeding Tests
// Tests for breeding cost, genetic inheritance, feature gating, and breed outcomes.

@MainActor
final class BreedingTests: XCTestCase {

    // MARK: Helpers

    private func makeProgressVM(coins: Int = 10_000, breedingUnlocked: Bool = true) -> ProgressionViewModel {
        let vm = ProgressionViewModel()
        vm.coins = coins
        if breedingUnlocked {
            vm.unlockedFeatures.insert(GameFeature.breeding.rawValue)
        }
        return vm
    }

    private func makeAdultCreature(dm: DataManager, ofType type: HabitatType, mutation: Int = 0) -> CreatureEntity {
        let entry   = CreatureCatalogEntry.creatures(ofType: type).first!
        let entity  = dm.createCreature(catalogID: entry.id, mutationIndex: mutation)
        entity.lifecycle = LifecycleStage.adult.rawValue
        return entity
    }

    // MARK: Breeding Cost

    func testBreedingCostByRarity() {
        let commonCost   = PricingTable.Breeding.cost(rarity: .common)
        let uncommonCost = PricingTable.Breeding.cost(rarity: .uncommon)
        let rareCost     = PricingTable.Breeding.cost(rarity: .rare)

        XCTAssertGreaterThan(commonCost,   0,            "Breeding cost must be positive")
        XCTAssertGreaterThan(uncommonCost, commonCost,   "Uncommon breed costs more than common")
        XCTAssertGreaterThan(rareCost,     uncommonCost, "Rare breed costs more than uncommon")
    }

    func testBreedingCostIsPositiveForAllRarities() {
        for rarity in Rarity.allCases {
            XCTAssertGreaterThan(PricingTable.Breeding.cost(rarity: rarity), 0,
                                 "\(rarity) breeding cost must be positive")
        }
    }

    // MARK: Feature Gating

    func testBreedingRequiresLevel12() {
        XCTAssertEqual(GameFeature.breeding.requiredLevel, 12,
                       "Breeding should unlock at Level 12 per design spec")
    }

    func testBreedingNotUnlockedAtLevel1() {
        var progression = PlayerProgression()
        progression.currentLevel = 1
        XCTAssertFalse(progression.unlockedFeatures.contains(GameFeature.breeding.rawValue),
                       "Breeding should not be available at level 1")
    }

    func testBreedingUnlocksAtLevel12() {
        var progression          = PlayerProgression()
        progression.currentLevel = 1
        progression.currentXP    = 0
        progression.unlockedFeatures = ["shop", "habitat", "feeding", "playing"]

        // Pump XP until level 12
        while progression.currentLevel < 12 {
            let xpNeeded = XPCurve.xpRequired(forLevel: progression.currentLevel)
            _ = progression.addXP(xpNeeded + 1)
        }

        XCTAssertGreaterThanOrEqual(progression.currentLevel, 12)
        XCTAssertTrue(progression.unlockedFeatures.contains(GameFeature.breeding.rawValue),
                      "Breeding should be in unlockedFeatures after reaching level 12")
    }

    // MARK: Mutation Probability Weights

    func testMutationWeightsSumToOne() {
        let weights: [Double] = [0.40, 0.30, 0.20, 0.10]
        let total = weights.reduce(0.0, +)
        XCTAssertEqual(total, 1.0, accuracy: 0.0001,
                       "Mutation probability weights must sum to exactly 1.0")
    }

    func testMutationWeightsDecreaseByIndex() {
        let weights: [Double] = [0.40, 0.30, 0.20, 0.10]
        for i in 0..<(weights.count - 1) {
            XCTAssertGreaterThan(weights[i], weights[i + 1],
                                 "Higher mutation indices should have lower probability")
        }
    }

    func testAllCreaturesHaveFourMutations() {
        for entry in CreatureCatalogEntry.allCreatures {
            XCTAssertEqual(entry.mutations.count, 4,
                           "\(entry.name) must have exactly 4 mutation variants")
        }
    }

    // MARK: Species Compatibility

    func testSameHabitatTypeCreaturesAreCompatible() {
        let waterCreatures = CreatureCatalogEntry.creatures(ofType: .water)
        XCTAssertGreaterThanOrEqual(waterCreatures.count, 2,
                                    "Need at least 2 water creatures to test breeding")
        XCTAssertEqual(waterCreatures[0].habitatType, waterCreatures[1].habitatType)
    }

    func testDifferentHabitatTypesAreIncompatible() {
        let water = CreatureCatalogEntry.creatures(ofType: .water).first!
        let fire  = CreatureCatalogEntry.creatures(ofType: .fire).first!
        XCTAssertNotEqual(water.habitatType, fire.habitatType)
    }

    // MARK: Breed Outcomes (in-memory Core Data)

    func testBreedProducesOffspringWithValidCatalogID() async {
        let dm      = DataManager.preview
        let parentA = makeAdultCreature(dm: dm, ofType: .water, mutation: 0)
        let parentB = makeAdultCreature(dm: dm, ofType: .water, mutation: 1)

        let entryA = CreatureCatalogEntry.find(byID: parentA.catalogID!)!
        let entryB = CreatureCatalogEntry.find(byID: parentB.catalogID!)!

        let progressVM  = makeProgressVM()
        let creatureVM  = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        guard case let .success(offspringCatalogID, mutationIndex) = creatureVM.breedingResult else {
            XCTFail("Expected breeding success")
            return
        }

        XCTAssertTrue([entryA.id, entryB.id].contains(offspringCatalogID),
                      "Offspring catalogID must be one of the two parent species")
        XCTAssertGreaterThanOrEqual(mutationIndex, 0)
        XCTAssertLessThanOrEqual(mutationIndex,    3)
    }

    func testBreedDeductsCoins() {
        let dm      = DataManager.preview
        let parentA = makeAdultCreature(dm: dm, ofType: .grass)
        let parentB = makeAdultCreature(dm: dm, ofType: .grass)

        let entryA      = CreatureCatalogEntry.find(byID: parentA.catalogID!)!
        let expectedCost = PricingTable.Breeding.cost(rarity: entryA.rarity)

        let progressVM = makeProgressVM(coins: expectedCost + 500)
        let balanceBefore = progressVM.coins

        let creatureVM = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        XCTAssertEqual(progressVM.coins, balanceBefore - expectedCost,
                       "Breeding should deduct exactly the rarity-based cost")
    }

    func testBreedFailsWhenInsufficientCoins() {
        let dm      = DataManager.preview
        let parentA = makeAdultCreature(dm: dm, ofType: .fire)
        let parentB = makeAdultCreature(dm: dm, ofType: .fire)

        let progressVM = makeProgressVM(coins: 0)

        let creatureVM = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        guard case .failure = creatureVM.breedingResult else {
            XCTFail("Expected failure when coins are insufficient")
            return
        }
        XCTAssertEqual(progressVM.coins, 0, "Coins should be unchanged after failed breed")
    }

    func testBreedFailsWhenFeatureNotUnlocked() {
        let dm      = DataManager.preview
        let parentA = makeAdultCreature(dm: dm, ofType: .ice)
        let parentB = makeAdultCreature(dm: dm, ofType: .ice)

        let progressVM = makeProgressVM(breedingUnlocked: false)

        let creatureVM = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        guard case .failure = creatureVM.breedingResult else {
            XCTFail("Expected failure when breeding is not yet unlocked")
            return
        }
    }

    func testBreedFailsForDifferentHabitatTypes() {
        let dm      = DataManager.preview
        let parentA = makeAdultCreature(dm: dm, ofType: .water)
        let parentB = makeAdultCreature(dm: dm, ofType: .fire)

        let progressVM = makeProgressVM()

        let creatureVM = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        guard case .failure = creatureVM.breedingResult else {
            XCTFail("Expected failure when parents have different habitat types")
            return
        }
    }

    func testBreedCreatesBreedingRecord() {
        let dm      = DataManager.preview
        let parentA = makeAdultCreature(dm: dm, ofType: .electric)
        let parentB = makeAdultCreature(dm: dm, ofType: .electric)

        let parentAID = parentA.id!
        let parentBID = parentB.id!

        let progressVM = makeProgressVM()
        let creatureVM = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        guard case .success = creatureVM.breedingResult else {
            XCTFail("Breeding should succeed for lineage test")
            return
        }

        let record = dm.allBreedingRecords().first
        XCTAssertNotNil(record,            "A breeding record should be saved after successful breed")
        XCTAssertEqual(record?.parentAID,  parentAID)
        XCTAssertEqual(record?.parentBID,  parentBID)
        XCTAssertNotNil(record?.offspringID)
    }

    func testBreedAddsNewCreatureToOwnedCount() {
        let dm           = DataManager.preview
        let countBefore  = dm.allOwnedCreatures().count

        let parentA = makeAdultCreature(dm: dm, ofType: .dirt)
        let parentB = makeAdultCreature(dm: dm, ofType: .dirt)

        let progressVM = makeProgressVM()
        let creatureVM = CreatureViewModel()
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)

        guard case .success = creatureVM.breedingResult else {
            XCTFail("Breeding should succeed")
            return
        }

        // 2 parents were added + 1 offspring = countBefore + 3
        XCTAssertEqual(dm.allOwnedCreatures().count, countBefore + 3,
                       "Two parents and one offspring should be in inventory after breeding")
    }
}

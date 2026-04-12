import XCTest
@testable import KeeperLegacy

// MARK: - Progression Tests
// Tests for XP curve, leveling, feature unlocks, and milestone rewards.

final class ProgressionTests: XCTestCase {

    // MARK: XP Curve

    func testXPRequiredIsPositive() {
        for level in 1...49 {
            XCTAssertGreaterThan(
                XPCurve.xpRequired(forLevel: level), 0,
                "Level \(level) requires 0 XP — invalid"
            )
        }
    }

    func testEarlyLevelsHaveLowerXPThanLateOnes() {
        let early = XPCurve.xpRequired(forLevel: 3)
        let late  = XPCurve.xpRequired(forLevel: 30)
        XCTAssertLessThan(early, late)
    }

    // MARK: Leveling Up

    func testAddXPTriggersLevelUp() {
        var progression = PlayerProgression()
        XCTAssertEqual(progression.currentLevel, 1)
        let xpNeeded = XPCurve.xpRequired(forLevel: 1)
        let levelsGained = progression.addXP(xpNeeded)
        XCTAssertEqual(levelsGained, [2])
        XCTAssertEqual(progression.currentLevel, 2)
    }

    func testXPDoesNotExceedMaxLevel() {
        var progression = PlayerProgression()
        progression.currentLevel = 50
        progression.currentXP    = 0
        progression.addXP(999999)
        XCTAssertEqual(progression.currentLevel, 50)
        XCTAssertEqual(progression.currentXP, 0)  // Resets at max
    }

    func testXPRemainsAfterLevelUp() {
        var progression = PlayerProgression()
        let xpNeeded = XPCurve.xpRequired(forLevel: 1)
        progression.addXP(xpNeeded + 25)
        XCTAssertEqual(progression.currentLevel, 2)
        XCTAssertEqual(progression.currentXP, 25)  // 25 carries over
    }

    // MARK: Feature Unlocks

    func testBasicFeaturesUnlockedAtStart() {
        let progression = PlayerProgression()
        XCTAssertTrue(progression.isFeatureUnlocked(.shop))
        XCTAssertTrue(progression.isFeatureUnlocked(.habitat))
        XCTAssertTrue(progression.isFeatureUnlocked(.feeding))
        XCTAssertTrue(progression.isFeatureUnlocked(.playing))
    }

    func testBreedingLockedAtStart() {
        let progression = PlayerProgression()
        XCTAssertFalse(progression.isFeatureUnlocked(.breeding))
    }

    func testBreedingUnlocksAtLevel12WithAct1() {
        var progression = PlayerProgression()
        progression.currentLevel = 12
        progression.storyAct     = 1      // Act I complete
        progression.checkAndUnlockFeatures()
        XCTAssertTrue(progression.isFeatureUnlocked(.breeding))
    }

    func testBreedingRemainsLockedAtLevel12WithoutStory() {
        var progression = PlayerProgression()
        progression.currentLevel = 12
        progression.storyAct     = 0      // Act I NOT complete
        progression.checkAndUnlockFeatures()
        XCTAssertFalse(progression.isFeatureUnlocked(.breeding))
    }

    func testMagicalHabitatRequiresAct2() {
        var progression = PlayerProgression()
        progression.currentLevel = 50
        progression.storyAct     = 1
        progression.checkAndUnlockFeatures()
        XCTAssertFalse(progression.isFeatureUnlocked(.magicalHabitat))

        progression.advanceStoryAct(to: 2)
        XCTAssertTrue(progression.isFeatureUnlocked(.magicalHabitat))
    }

    // MARK: Milestones

    func testMilestoneExistsAtLevel5() {
        XCTAssertNotNil(MilestoneReward.milestones[5])
    }

    func testMilestoneLevel10HasStoryEvent() {
        let milestone = MilestoneReward.milestones[10]
        XCTAssertNotNil(milestone?.storyEvent)
    }

    func testMilestoneLevel50HasLargeBonus() {
        let milestone = MilestoneReward.milestones[50]
        XCTAssertNotNil(milestone)
        XCTAssertGreaterThan(milestone!.coinBonus, 1000)
    }
}

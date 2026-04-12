import XCTest
@testable import KeeperLegacy

// MARK: - Story Tests
// Tests for story event triggers, NPC relationship tracking, act advancement,
// and feature unlocking through story events.

@MainActor
final class StoryTests: XCTestCase {

    // MARK: Story Event Structure

    func testAllEventsHaveUniqueIDs() {
        let ids = StoryEvent.allEvents.map { $0.id }
        let unique = Set(ids)
        XCTAssertEqual(ids.count, unique.count, "All story event IDs must be unique")
    }

    func testAllEventsHaveNonEmptyContent() {
        for event in StoryEvent.allEvents {
            XCTAssertFalse(event.title.isEmpty, "\(event.id) must have a title")
            XCTAssertFalse(event.body.isEmpty,  "\(event.id) must have body text")
            XCTAssertFalse(event.npcID.isEmpty, "\(event.id) must have a npcID")
        }
    }

    func testAllEventNPCIDsReferenceRealNPCs() {
        let validNPCIDs = Set(NPC.mainCast.map { $0.id })
        for event in StoryEvent.allEvents {
            XCTAssertTrue(validNPCIDs.contains(event.npcID),
                          "Event '\(event.id)' references unknown NPC '\(event.npcID)'")
        }
    }

    func testEventsAreInChronologicalActOrder() {
        // No Act II event should appear before an Act I event in the array
        var highestActSeen = 1
        for event in StoryEvent.allEvents {
            XCTAssertGreaterThanOrEqual(event.act.rawValue, highestActSeen,
                "Events should be roughly sorted by act (found Act \(event.act.rawValue) after Act \(highestActSeen))")
            highestActSeen = max(highestActSeen, event.act.rawValue)
        }
    }

    func testActAdvancingEventsUnlockFeatures() {
        // Every event that advances to the next act should also unlock a feature
        // (based on current design — this documents the design contract)
        let capstoneEvents = StoryEvent.allEvents.filter { $0.advancesToNextAct }
        XCTAssertFalse(capstoneEvents.isEmpty, "There should be at least one capstone event")
        for event in capstoneEvents {
            XCTAssertNotNil(event.unlocksFeature,
                            "Capstone event '\(event.id)' should unlock a feature")
        }
    }

    // MARK: PlayerStoryState

    func testInitialStateHasNoCompletedEvents() {
        let state = PlayerStoryState()
        XCTAssertTrue(state.completedEventIDs.isEmpty)
        XCTAssertEqual(state.currentAct, 1)
    }

    func testCompleteEventAddsToCompletedSet() {
        var state = PlayerStoryState()
        state.completeEvent("act1_first_egg")
        XCTAssertTrue(state.hasCompletedEvent("act1_first_egg"))
        XCTAssertFalse(state.hasCompletedEvent("act1_shop_secret"))
    }

    func testCompleteEventIsIdempotent() {
        var state = PlayerStoryState()
        state.completeEvent("act1_first_egg")
        state.completeEvent("act1_first_egg")
        XCTAssertEqual(state.completedEventIDs.count, 1)
    }

    func testRelationshipIncreaseClampsAt100() {
        var state = PlayerStoryState()
        state.increaseRelationship(npcID: "elder_mira", by: 95)
        state.increaseRelationship(npcID: "elder_mira", by: 95)
        let level = state.npcRelationshipLevels["elder_mira"] ?? 0
        XCTAssertEqual(level, 100, "Relationship should clamp at 100")
    }

    func testInitialRelationshipLevelsMatchNPCDefaults() {
        let state = PlayerStoryState()
        // Elder Mira starts at 50 per NPC.mainCast definition
        XCTAssertEqual(state.npcRelationshipLevels["elder_mira"], 50)
        // Rival Cass starts at 0
        XCTAssertEqual(state.npcRelationshipLevels["rival_cass"], 0)
    }

    // MARK: StoryViewModel — Trigger Logic

    func testFirstEggTriggersAtLevel1() {
        let vm = StoryViewModel()
        vm.checkTriggers(level: 1)
        XCTAssertNotNil(vm.pendingStoryEvent)
        XCTAssertEqual(vm.pendingStoryEvent?.id, "act1_first_egg")
    }

    func testNoEventTriggersBeforeLevel1() {
        let vm = StoryViewModel()
        vm.checkTriggers(level: 0)
        XCTAssertNil(vm.pendingStoryEvent, "No events should trigger at level 0")
    }

    func testOnlyOneEventPendsAtATime() {
        let vm = StoryViewModel()
        vm.checkTriggers(level: 15)  // Multiple level-based events are eligible
        XCTAssertNotNil(vm.pendingStoryEvent)
        // Only the first eligible unfired event should queue
        XCTAssertEqual(vm.pendingStoryEvent?.id, "act1_first_egg",
                       "Should queue events in allEvents order")
    }

    func testShopSecretTriggersAtLevel10() {
        let vm = StoryViewModel()
        // Pre-complete the first event so act1_shop_secret is next
        vm.storyState.completeEvent("act1_first_egg")
        vm.checkTriggers(level: 10)
        XCTAssertEqual(vm.pendingStoryEvent?.id, "act1_shop_secret")
    }

    func testMagicDiscoveredRequiresPrerequisite() {
        let vm = StoryViewModel()
        vm.storyState.completeEvent("act1_first_egg")
        // act1_shop_secret NOT completed
        vm.checkTriggers(level: 15)
        // act1_shop_secret should fire (level 10 reached), NOT act1_magic_discovered
        XCTAssertEqual(vm.pendingStoryEvent?.id, "act1_shop_secret")
    }

    func testMagicDiscoveredTriggersAfterPrerequisiteCompletes() {
        let vm = StoryViewModel()
        vm.storyState.completeEvent("act1_first_egg")
        vm.storyState.completeEvent("act1_shop_secret")
        vm.checkTriggers(level: 10)
        XCTAssertEqual(vm.pendingStoryEvent?.id, "act1_magic_discovered")
    }

    // MARK: StoryViewModel — Event Completion

    func testCompleteEventUnlocksFeature() {
        let vm         = StoryViewModel()
        let progressVM = ProgressionViewModel()
        progressVM.coins = 10_000

        // Set up: complete prerequisites
        vm.storyState.completeEvent("act1_first_egg")
        vm.storyState.completeEvent("act1_shop_secret")
        vm.pendingStoryEvent = StoryEvent.allEvents.first { $0.id == "act1_magic_discovered" }

        vm.completeCurrentEvent(progressVM: progressVM)

        XCTAssertTrue(progressVM.unlockedFeatures.contains(GameFeature.breeding.rawValue),
                      "Completing act1_magic_discovered should unlock breeding")
    }

    func testCompleteCapstoneEventAdvancesStoryAct() {
        let vm         = StoryViewModel()
        let progressVM = ProgressionViewModel()

        vm.storyState.completeEvent("act1_first_egg")
        vm.storyState.completeEvent("act1_shop_secret")
        vm.pendingStoryEvent = StoryEvent.allEvents.first { $0.id == "act1_magic_discovered" }

        XCTAssertEqual(vm.storyState.currentAct, 1)
        vm.completeCurrentEvent(progressVM: progressVM)
        XCTAssertEqual(vm.storyState.currentAct, 2, "Completing capstone event should advance to Act II")
        XCTAssertEqual(progressVM.storyAct, 2)
    }

    func testCompleteEventBoostsNPCRelationship() {
        let vm         = StoryViewModel()
        let progressVM = ProgressionViewModel()
        let initialLevel = vm.storyState.npcRelationshipLevels["elder_mira"] ?? 0

        vm.pendingStoryEvent = StoryEvent.allEvents.first { $0.id == "act1_first_egg" }
        vm.completeCurrentEvent(progressVM: progressVM)

        let newLevel = vm.storyState.npcRelationshipLevels["elder_mira"] ?? 0
        XCTAssertEqual(newLevel, initialLevel + 10,
                       "Completing an event should boost the delivering NPC's relationship by 10")
    }

    func testCompleteEventClearsPending() {
        let vm         = StoryViewModel()
        let progressVM = ProgressionViewModel()
        vm.pendingStoryEvent = StoryEvent.allEvents[0]
        vm.completeCurrentEvent(progressVM: progressVM)
        XCTAssertNil(vm.pendingStoryEvent)
    }

    func testCompletingEventImmediatelyChecksNext() {
        let vm         = StoryViewModel()
        let progressVM = ProgressionViewModel()
        progressVM.level = 10  // Make act1_shop_secret eligible

        // Complete first event — should immediately queue act1_shop_secret
        vm.pendingStoryEvent = StoryEvent.allEvents.first { $0.id == "act1_first_egg" }
        vm.completeCurrentEvent(progressVM: progressVM)

        XCTAssertEqual(vm.pendingStoryEvent?.id, "act1_shop_secret",
                       "After completing act1_first_egg at level 10, act1_shop_secret should auto-queue")
    }

    // MARK: Story Persistence (DataManager)

    func testSaveAndLoadStoryStateRoundTrips() {
        let dm = DataManager(inMemory: true)
        _ = dm.playerState()

        var state = PlayerStoryState()
        state.completeEvent("act1_first_egg")
        state.completeEvent("act1_shop_secret")
        state.currentAct = 2
        state.increaseRelationship(npcID: "elder_mira", by: 20)

        dm.saveStoryState(state)
        let loaded = dm.loadStoryState()

        XCTAssertTrue(loaded.hasCompletedEvent("act1_first_egg"))
        XCTAssertTrue(loaded.hasCompletedEvent("act1_shop_secret"))
        XCTAssertEqual(loaded.currentAct, 2)
        XCTAssertEqual(loaded.npcRelationshipLevels["elder_mira"], (50 + 20))
    }

    func testFreshLoadReturnsDefaultState() {
        let dm    = DataManager(inMemory: true)
        _ = dm.playerState()
        let state = dm.loadStoryState()

        XCTAssertTrue(state.completedEventIDs.isEmpty)
        XCTAssertEqual(state.currentAct, 1)
    }
}

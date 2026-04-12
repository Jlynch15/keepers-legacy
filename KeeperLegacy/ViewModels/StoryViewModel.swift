import Foundation
import Combine

// MARK: - StoryViewModel
// Manages story progression: event triggers, NPC relationships, act advancement.
// Story events fire one at a time; ContentView shows StoryEventView when pendingStoryEvent is set.

@MainActor
final class StoryViewModel: ObservableObject {

    @Published var pendingStoryEvent: StoryEvent? = nil
    @Published var storyState: PlayerStoryState = PlayerStoryState()

    private let dataManager = DataManager.shared
    private var cancellables = Set<AnyCancellable>()

    init() {
        // Subscribe to non-level story triggers (order fulfillment, breeding)
        NotificationCenter.default
            .publisher(for: .storyCheckNeeded)
            .receive(on: DispatchQueue.main)
            .sink { [weak self] notification in
                let orders = notification.userInfo?["ordersCount"] as? Int ?? 0
                let breeds = notification.userInfo?["breedCount"]  as? Int ?? 0
                self?.checkTriggers(fulfilledOrdersCount: orders, breedCount: breeds)
            }
            .store(in: &cancellables)
    }

    // MARK: Load

    func load() {
        storyState = dataManager.loadStoryState()
    }

    // MARK: Trigger Checking

    /// Evaluate all untriggered events and queue the first eligible one.
    /// Call this after level-up overlay dismisses, on game load, and when an event completes.
    func checkTriggers(level: Int? = nil, fulfilledOrdersCount: Int = 0, breedCount: Int = 0) {
        guard pendingStoryEvent == nil else { return }

        let resolvedLevel = level ?? 0

        for event in StoryEvent.allEvents {
            guard !storyState.hasCompletedEvent(event.id) else { continue }

            let triggered: Bool
            switch event.triggerCondition {
            case .reachLevel(let required):
                triggered = resolvedLevel >= required
            case .completeEvent(let prerequisiteID):
                triggered = storyState.hasCompletedEvent(prerequisiteID)
            case .fulfillOrders(let required):
                triggered = fulfilledOrdersCount >= required
            case .breedCreatures(let required):
                triggered = breedCount >= required
            case .manual:
                triggered = false
            }

            if triggered {
                pendingStoryEvent = event
                return  // Show one at a time
            }
        }
    }

    // MARK: Event Completion

    /// Called when the player taps "Continue" on a StoryEventView.
    func completeCurrentEvent(progressVM: ProgressionViewModel) {
        guard let event = pendingStoryEvent else { return }

        storyState.completeEvent(event.id)

        // Unlock any feature this event grants
        if let feature = event.unlocksFeature {
            progressVM.unlockedFeatures.insert(feature.rawValue)
        }

        // Advance story act if this is a capstone event
        if event.advancesToNextAct {
            let nextAct = min(storyState.currentAct + 1, 3)
            storyState.currentAct = nextAct
            progressVM.advanceStoryAct(to: nextAct)
        }

        // Boost relationship with the NPC who delivered this event
        storyState.increaseRelationship(npcID: event.npcID, by: 10)

        dataManager.saveStoryState(storyState)
        pendingStoryEvent = nil

        // Completing an event may immediately unlock the next one
        checkTriggers(level: progressVM.level)
    }

    // MARK: Computed Properties

    var currentAct: StoryAct {
        StoryAct(rawValue: storyState.currentAct) ?? .act1
    }

    var completedEventsCount: Int {
        storyState.completedEventIDs.count
    }

    /// All NPCs paired with their current relationship level (0-100).
    var npcsWithRelationships: [(npc: NPC, level: Int)] {
        NPC.mainCast.map { npc in
            (npc, storyState.npcRelationshipLevels[npc.id] ?? 0)
        }
    }

    /// NPC object for a given ID, falling back to Elder Mira.
    func npc(for id: String) -> NPC {
        NPC.mainCast.first { $0.id == id } ?? NPC.mainCast[0]
    }
}

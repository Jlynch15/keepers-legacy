import Foundation

// MARK: - Story Acts

enum StoryAct: Int, Codable, CaseIterable {
    case act1 = 1   // Discovery
    case act2 = 2   // Restoration
    case act3 = 3   // Legacy

    var title: String {
        switch self {
        case .act1: return "Act I: Discovery"
        case .act2: return "Act II: Restoration"
        case .act3: return "Act III: Legacy"
        }
    }

    var description: String {
        switch self {
        case .act1: return "You've inherited a mysterious shop from your absent uncle. A first egg appears..."
        case .act2: return "The shop holds ancient magical secrets. You uncover a civilization's legacy."
        case .act3: return "The fate of magic itself is in your hands."
        }
    }
}

// MARK: - Story Events

/// All story events that can be triggered during gameplay.
/// Events are keyed by a stable string ID used in progression data.
struct StoryEvent: Identifiable {
    let id: String
    let act: StoryAct
    let title: String
    let body: String
    let triggerCondition: TriggerCondition
    let unlocksFeature: GameFeature?

    enum TriggerCondition {
        case reachLevel(Int)
        case completeEvent(String)      // Another event's ID must be done first
        case fulfillOrders(Int)         // Fulfill N customer orders
        case breedCreatures(Int)        // Successfully breed N times
        case manual                     // Triggered by NPC dialogue
    }

    static let allEvents: [StoryEvent] = [
        StoryEvent(
            id: "act1_first_egg",
            act: .act1,
            title: "A Mysterious Egg",
            body: "While tidying up the shop, you discover a glowing egg tucked behind the counter. Your uncle's note says: 'When the egg stirs, so does the magic.'",
            triggerCondition: .reachLevel(1),
            unlocksFeature: nil
        ),
        StoryEvent(
            id: "act1_shop_secret",
            act: .act1,
            title: "The Shop's Secret",
            body: "Your creatures begin acting strangely around the old display case. Something is hidden inside...",
            triggerCondition: .reachLevel(10),
            unlocksFeature: nil
        ),
        StoryEvent(
            id: "act1_magic_discovered",
            act: .act1,
            title: "Magic Revealed",
            body: "The truth is undeniable: this isn't just a pet shop. The creatures carry ancient magic, and so do you. Breeding is now possible.",
            triggerCondition: .completeEvent("act1_shop_secret"),
            unlocksFeature: .breeding
        ),
        StoryEvent(
            id: "act2_ancient_origins",
            act: .act2,
            title: "Ancient Origins",
            body: "The shop was founded by an ancient magical civilization. Their creatures were not just pets — they were living conduits of elemental power.",
            triggerCondition: .reachLevel(20),
            unlocksFeature: nil
        ),
        StoryEvent(
            id: "act2_revelation",
            act: .act2,
            title: "The Restoration",
            body: "The magical habitat stirs. A new world opens behind the ordinary ones — a place where magical creatures can finally come home.",
            triggerCondition: .reachLevel(25),
            unlocksFeature: .magicalHabitat
        ),
        StoryEvent(
            id: "act3_legacy",
            act: .act3,
            title: "Keeper's Legacy",
            body: "Magic spreads beyond the shop walls. You must decide: guard it, share it, or let it flow freely into the world. The legacy is yours to define.",
            triggerCondition: .reachLevel(50),
            unlocksFeature: nil
        ),
    ]
}

// MARK: - NPC System

struct NPC: Identifiable {
    let id: String
    let name: String
    let archetype: Archetype
    let portraitAsset: String   // Asset name for portrait image
    var relationshipLevel: Int  // 0-100

    enum Archetype {
        case mentor         // Guides the player through early game
        case businessPartner // Brings customer orders, economy tips
        case collector       // Seeks rare/specific creatures
        case scholar         // Provides lore and story context
        case rival           // Friendly competition, unlocks after Act I
    }

    static let mainCast: [NPC] = [
        NPC(id: "elder_mira",   name: "Elder Mira",  archetype: .mentor,          portraitAsset: "npc_mira",   relationshipLevel: 50),
        NPC(id: "trader_fenn",  name: "Trader Fenn", archetype: .businessPartner, portraitAsset: "npc_fenn",   relationshipLevel: 30),
        NPC(id: "collector_ivy",name: "Ivy",         archetype: .collector,       portraitAsset: "npc_ivy",    relationshipLevel: 20),
        NPC(id: "scholar_rex",  name: "Scholar Rex", archetype: .scholar,         portraitAsset: "npc_rex",    relationshipLevel: 10),
        NPC(id: "rival_cass",   name: "Cass",        archetype: .rival,           portraitAsset: "npc_cass",   relationshipLevel: 0),
    ]
}

// MARK: - Player Story State

struct PlayerStoryState: Codable {
    var currentAct: Int                         // 1, 2, or 3
    var completedEventIDs: Set<String>
    var npcRelationshipLevels: [String: Int]    // NPC id → 0-100

    init() {
        self.currentAct              = 1
        self.completedEventIDs       = []
        self.npcRelationshipLevels   = Dictionary(
            uniqueKeysWithValues: NPC.mainCast.map { ($0.id, $0.relationshipLevel) }
        )
    }

    func hasCompletedEvent(_ eventID: String) -> Bool {
        completedEventIDs.contains(eventID)
    }

    mutating func completeEvent(_ eventID: String) {
        completedEventIDs.insert(eventID)
    }

    mutating func increaseRelationship(npcID: String, by amount: Int) {
        let current = npcRelationshipLevels[npcID] ?? 0
        npcRelationshipLevels[npcID] = min(100, current + amount)
    }
}

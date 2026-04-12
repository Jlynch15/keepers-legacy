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
    let npcID: String               // Which NPC delivers this event
    let title: String
    let body: String
    let triggerCondition: TriggerCondition
    let unlocksFeature: GameFeature?
    let advancesToNextAct: Bool     // If true, completing this event advances the story act

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
            npcID: "elder_mira",
            title: "A Mysterious Egg",
            body: "While tidying up the shop, you discover a glowing egg tucked behind the counter. Elder Mira examines it with knowing eyes.\n\n\"Your uncle always said this day would come. Tend to it carefully — magic recognizes patience.\"",
            triggerCondition: .reachLevel(1),
            unlocksFeature: nil,
            advancesToNextAct: false
        ),
        StoryEvent(
            id: "act1_shop_secret",
            act: .act1,
            npcID: "elder_mira",
            title: "The Shop's Secret",
            body: "Your creatures begin acting strangely around the old display case. Elder Mira arrives unexpectedly, her expression grave.\n\n\"The case is a resonance vault. Only a true Keeper can open it. I think that's you.\"",
            triggerCondition: .reachLevel(10),
            unlocksFeature: nil,
            advancesToNextAct: false
        ),
        StoryEvent(
            id: "act1_magic_discovered",
            act: .act1,
            npcID: "elder_mira",
            title: "Magic Revealed",
            body: "The vault opens. Inside: a crystallized creature egg — the first of its kind in generations. Elder Mira's voice drops to a whisper.\n\n\"This isn't just a pet shop. These creatures carry ancient magic. And now, so do you. The lineage can continue.\"",
            triggerCondition: .completeEvent("act1_shop_secret"),
            unlocksFeature: .breeding,
            advancesToNextAct: true
        ),
        StoryEvent(
            id: "act2_ancient_origins",
            act: .act2,
            npcID: "scholar_rex",
            title: "Ancient Origins",
            body: "Scholar Rex arrives breathless, clutching weathered manuscripts.\n\n\"I've translated the vault inscriptions. This shop was a sanctuary — built by the Aelurin civilization to preserve elemental creatures when the old magic began to fade. Your uncle was the last guardian.\"",
            triggerCondition: .reachLevel(20),
            unlocksFeature: nil,
            advancesToNextAct: false
        ),
        StoryEvent(
            id: "act2_revelation",
            act: .act2,
            npcID: "elder_mira",
            title: "The Restoration",
            body: "The shop trembles. A hidden door reveals itself behind the water habitat — a shimmering threshold.\n\n\"The magical habitat,\" Elder Mira breathes. \"The Aelurin sealed it to protect the rarest creatures. Your bond with the others has restored it. The door is open again.\"",
            triggerCondition: .reachLevel(25),
            unlocksFeature: .magicalHabitat,
            advancesToNextAct: true
        ),
        StoryEvent(
            id: "act3_legacy",
            act: .act3,
            npcID: "elder_mira",
            title: "Keeper's Legacy",
            body: "Magic spreads beyond the shop walls. Creatures from every element gather at your door. Elder Mira stands beside you one last time.\n\n\"The Aelurin spent generations searching for a Keeper worthy of this. You must now decide: guard the magic, share it with the world, or let it flow freely. The legacy is yours to define.\"",
            triggerCondition: .reachLevel(50),
            unlocksFeature: nil,
            advancesToNextAct: false
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

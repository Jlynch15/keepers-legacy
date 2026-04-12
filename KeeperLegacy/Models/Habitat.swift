import Foundation

// MARK: - Habitat

struct Habitat: Identifiable, Codable {
    let id: UUID
    let type: HabitatType

    /// The UUID of the CreatureInstance currently housed here, or nil if empty
    var occupantID: UUID?

    /// Cosmetic decoration item IDs the player has placed
    var decorationIDs: [String]

    /// Player-level at which this habitat slot was unlocked
    let unlockedAtLevel: Int

    // MARK: Computed

    var isEmpty: Bool { occupantID == nil }

    init(type: HabitatType, unlockedAtLevel: Int) {
        self.id              = UUID()
        self.type            = type
        self.occupantID      = nil
        self.decorationIDs   = []
        self.unlockedAtLevel = unlockedAtLevel
    }

    mutating func placeCreature(_ creatureID: UUID) {
        occupantID = creatureID
    }

    mutating func removeCreature() {
        occupantID = nil
    }

    mutating func addDecoration(_ decorationID: String) {
        guard !decorationIDs.contains(decorationID) else { return }
        decorationIDs.append(decorationID)
    }

    mutating func removeDecoration(_ decorationID: String) {
        decorationIDs.removeAll { $0 == decorationID }
    }
}

// MARK: - Habitat Unlock Schedule

/// Maps player level → habitat slots unlocked cumulatively.
/// Sourced from progression_system_v1.json
struct HabitatUnlockSchedule {
    /// Returns the total number of standard habitats unlocked at the given level.
    static func habitatsUnlocked(atLevel level: Int) -> Int {
        switch level {
        case ..<2:  return 1
        case 2..<5: return 2
        case 5..<8: return 3
        case 8..<12: return 4
        case 12..<18: return 5
        case 18..<25: return 6
        case 25..<35: return 7
        case 35...: return 8
        default:    return 1
        }
    }

    /// Magical habitat unlocks via story Act II, not level.
    static let magicalHabitatRequiresAct: Int = 2
}

// MARK: - Habitat Expansion Cost

struct HabitatExpansionCost {
    /// Coin cost to expand from current count to next slot.
    static func cost(forSlot slot: Int) -> Int {
        switch slot {
        case 1: return 0      // First habitat is free
        case 2: return 500
        case 3: return 1000
        case 4: return 2000
        case 5: return 3000
        case 6: return 4000
        case 7: return 5000
        case 8: return 6000
        default: return 6000
        }
    }
}

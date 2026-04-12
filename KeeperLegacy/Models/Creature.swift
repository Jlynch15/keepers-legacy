import Foundation

// MARK: - Enums

enum HabitatType: String, Codable, CaseIterable {
    case water    = "Water"
    case dirt     = "Dirt"
    case grass    = "Grass"
    case fire     = "Fire"
    case ice      = "Ice"
    case electric = "Electric"
    case magical  = "Magical"

    var displayColor: String {
        switch self {
        case .water:    return "#A8D8EA"
        case .dirt:     return "#C9A876"
        case .grass:    return "#A8D5A8"
        case .fire:     return "#FF8C42"
        case .ice:      return "#B8D4E8"
        case .electric: return "#F0E68C"
        case .magical:  return "#C99BFF"
        }
    }

    /// Returns true if this habitat is gated behind story Act II
    var requiresStoryAct: Int? {
        switch self {
        case .magical: return 2
        default:       return nil
        }
    }
}

enum Rarity: String, Codable, CaseIterable {
    case common    = "Common"
    case uncommon  = "Uncommon"
    case rare      = "Rare"

    /// Base shop purchase price in Coins
    var basePrice: Int {
        switch self {
        case .common:   return 150
        case .uncommon: return 400
        case .rare:     return 1200
        }
    }

    /// Base sell value in Coins (before happiness multiplier)
    var baseSellValue: Int {
        switch self {
        case .common:   return 75
        case .uncommon: return 200
        case .rare:     return 700
        }
    }
}

enum LifecycleStage: String, Codable {
    case egg        = "Egg"
    case baby       = "Baby"
    case adolescent = "Adolescent"
    case adult      = "Adult"

    /// Hours required to advance to next stage
    var durationHours: Double {
        switch self {
        case .egg:        return 12
        case .baby:       return 48   // 2 days
        case .adolescent: return 96   // 4 days
        case .adult:      return .infinity
        }
    }
}

// MARK: - Creature Catalog Entry (static reference data)

/// A CreatureCatalogEntry describes a species — it is NOT a player-owned instance.
/// Use CreatureInstance for creatures the player actually owns.
struct CreatureCatalogEntry: Identifiable, Codable {
    let id: String              // e.g. "aquaburst" — stable across sessions
    let name: String
    let description: String
    let habitatType: HabitatType
    let rarity: Rarity
    let favoriteToy: String
    let mutations: [MutationVariant]

    struct MutationVariant: Codable {
        let index: Int          // 0-3
        let colorHint: String   // e.g. "Deep Blue", "Frost White"
    }
}

// MARK: - Creature Instance (player-owned)

/// A CreatureInstance is a specific creature the player owns, with live care stats.
struct CreatureInstance: Identifiable, Codable {
    let id: UUID
    let catalogID: String       // References CreatureCatalogEntry.id
    let mutationIndex: Int      // 0-3

    // Care stats — all 0.0 (empty) to 1.0 (full)
    var hunger: Double          // 1.0 = fully fed, decays over time
    var happiness: Double       // 1.0 = very happy
    var cleanliness: Double     // 1.0 = clean
    var affection: Double       // 1.0 = very bonded
    var playfulness: Double     // 1.0 = wants to play

    // Lifecycle
    var lifecycle: LifecycleStage
    var lifecycleStartDate: Date

    // Discovery
    var discoveredFavoriteToy: Bool

    // Lineage (UUID of parents, empty for shop-purchased)
    var parentIDs: [UUID]

    // Metadata
    let dateAcquired: Date
    var nickname: String?       // Player-assigned optional nickname

    // MARK: Computed

    /// Weighted average of all care stats — used for sell price multiplier
    var overallHappiness: Double {
        (hunger + happiness + cleanliness + affection + playfulness) / 5.0
    }

    /// Sell price multiplier: 1.0x at neutral, 2.0x at max happiness
    var sellMultiplier: Double {
        1.0 + overallHappiness
    }

    init(
        catalogID: String,
        mutationIndex: Int = 0,
        parentIDs: [UUID] = []
    ) {
        self.id               = UUID()
        self.catalogID        = catalogID
        self.mutationIndex    = mutationIndex
        self.hunger           = 0.8
        self.happiness        = 0.7
        self.cleanliness      = 0.9
        self.affection        = 0.5
        self.playfulness      = 0.8
        self.lifecycle        = .adult
        self.lifecycleStartDate = Date()
        self.discoveredFavoriteToy = false
        self.parentIDs        = parentIDs
        self.dateAcquired     = Date()
        self.nickname         = nil
    }

    // MARK: Care Actions

    /// Feed the creature with a food item. Returns XP gained.
    mutating func feed(foodValue: Double) -> Int {
        hunger = min(1.0, hunger + foodValue)
        happiness = min(1.0, happiness + foodValue * 0.1)
        return 5
    }

    /// Play with a toy. Pass isFavoriteToy = true for bonus.
    /// Returns XP gained and whether the favorite toy was discovered this interaction.
    mutating func play(toyName: String, isFavoriteToy: Bool) -> (xp: Int, discoveredFavorite: Bool) {
        let baseHappiness = 0.15
        let bonusHappiness = isFavoriteToy ? 0.10 : 0.0
        playfulness = min(1.0, playfulness + baseHappiness + bonusHappiness)
        happiness   = min(1.0, happiness   + baseHappiness + bonusHappiness)
        affection   = min(1.0, affection   + 0.05)

        var discovered = false
        if isFavoriteToy && !discoveredFavoriteToy {
            discoveredFavoriteToy = true
            discovered = true
        }

        let xp = isFavoriteToy ? 15 : 8
        return (xp, discovered)
    }

    /// Clean the creature.
    mutating func clean() -> Int {
        cleanliness = 1.0
        happiness   = min(1.0, happiness + 0.05)
        return 5
    }

    /// Apply time-based stat decay. Call once per game tick (e.g. every 30 min real-time).
    mutating func applyDecay(hoursPassed: Double) {
        let decayRate = 0.02 * hoursPassed
        hunger      = max(0.0, hunger      - decayRate * 1.5)
        happiness   = max(0.0, happiness   - decayRate * 1.0)
        cleanliness = max(0.0, cleanliness - decayRate * 0.8)
        affection   = max(0.0, affection   - decayRate * 0.5)
        playfulness = max(0.0, playfulness - decayRate * 1.0)
    }
}

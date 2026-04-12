import Foundation

// MARK: - Feature Flags

/// All features that can be locked/unlocked as the player progresses.
enum GameFeature: String, Codable, CaseIterable {
    case shop               = "shop"
    case habitat            = "habitat"
    case feeding            = "feeding"
    case playing            = "playing"
    case selling            = "selling"
    case customerOrders     = "customerOrders"
    case breeding           = "breeding"
    case monsterpedia       = "monsterpedia"
    case habitatExpansion   = "habitatExpansion"
    case mutations          = "mutations"
    case magicalHabitat     = "magicalHabitat"   // Story Act II gate
    case cosmetics          = "cosmetics"

    /// Minimum level required to unlock (0 = available from start)
    var requiredLevel: Int {
        switch self {
        case .shop:             return 0
        case .habitat:          return 0
        case .feeding:          return 0
        case .playing:          return 0
        case .selling:          return 1
        case .customerOrders:   return 3
        case .breeding:         return 12
        case .monsterpedia:     return 2
        case .habitatExpansion: return 2
        case .mutations:        return 15
        case .magicalHabitat:   return 0    // Level not the gate — story Act II is
        case .cosmetics:        return 5
        }
    }

    /// Story act required in addition to level (nil = no story gate)
    var requiredStoryAct: Int? {
        switch self {
        case .breeding:         return 1    // Act I must be complete
        case .magicalHabitat:   return 2
        default:                return nil
        }
    }
}

// MARK: - Milestone Rewards

struct MilestoneReward: Identifiable {
    let id: Int             // The level at which this milestone triggers
    let coinBonus: Int
    let stardustBonus: Int
    let unlocksFeature: GameFeature?
    let storyEvent: String?     // Key into Story events, if any
    let description: String

    static let milestones: [Int: MilestoneReward] = [
        5:  MilestoneReward(id: 5,  coinBonus: 200,  stardustBonus: 0,   unlocksFeature: .cosmetics,         storyEvent: nil,                   description: "Cosmetics unlocked!"),
        10: MilestoneReward(id: 10, coinBonus: 500,  stardustBonus: 25,  unlocksFeature: nil,                storyEvent: "act1_shop_secret",    description: "The shop's secret begins to reveal itself..."),
        12: MilestoneReward(id: 12, coinBonus: 300,  stardustBonus: 0,   unlocksFeature: .breeding,          storyEvent: nil,                   description: "Breeding unlocked! Requires Act I completion."),
        15: MilestoneReward(id: 15, coinBonus: 400,  stardustBonus: 0,   unlocksFeature: .mutations,         storyEvent: nil,                   description: "Mutation variants discovered!"),
        25: MilestoneReward(id: 25, coinBonus: 1000, stardustBonus: 50,  unlocksFeature: nil,                storyEvent: "act2_revelation",     description: "A revelation about the ancient civilization..."),
        50: MilestoneReward(id: 50, coinBonus: 5000, stardustBonus: 200, unlocksFeature: nil,                storyEvent: "act3_legacy",         description: "You have mastered the Keeper's Legacy!"),
    ]
}

// MARK: - Player Progression State

struct PlayerProgression: Codable {
    var currentLevel: Int
    var currentXP: Int
    var unlockedFeatures: Set<String>   // Stores GameFeature.rawValue
    var claimedMilestones: Set<Int>     // Level numbers whose rewards have been claimed
    var storyAct: Int                   // 1, 2, or 3

    init() {
        self.currentLevel       = 1
        self.currentXP          = 0
        self.unlockedFeatures   = Set(GameFeature.allCases
            .filter { $0.requiredLevel == 0 && $0.requiredStoryAct == nil }
            .map { $0.rawValue })
        self.claimedMilestones  = []
        self.storyAct           = 1
    }

    // MARK: XP / Leveling

    /// XP needed to reach the next level from the current one.
    /// Accelerated early curve, then linear from level 10+.
    var xpToNextLevel: Int {
        XPCurve.xpRequired(forLevel: currentLevel)
    }

    /// Add XP and handle level-ups. Returns an array of levels gained (usually 0 or 1).
    mutating func addXP(_ amount: Int) -> [Int] {
        var levelsGained: [Int] = []
        currentXP += amount
        while currentXP >= xpToNextLevel && currentLevel < 50 {
            currentXP -= xpToNextLevel
            currentLevel += 1
            levelsGained.append(currentLevel)
            checkAndUnlockFeatures()
        }
        if currentLevel == 50 { currentXP = 0 }
        return levelsGained
    }

    // MARK: Feature Unlocking

    func isFeatureUnlocked(_ feature: GameFeature) -> Bool {
        unlockedFeatures.contains(feature.rawValue)
    }

    mutating func checkAndUnlockFeatures() {
        for feature in GameFeature.allCases {
            guard !isFeatureUnlocked(feature) else { continue }
            let levelOK = currentLevel >= feature.requiredLevel
            let storyOK: Bool
            if let requiredAct = feature.requiredStoryAct {
                storyOK = storyAct >= requiredAct
            } else {
                storyOK = true
            }
            if levelOK && storyOK {
                unlockedFeatures.insert(feature.rawValue)
            }
        }
    }

    /// Called when a story act completes — re-evaluates story-gated features.
    mutating func advanceStoryAct(to act: Int) {
        storyAct = act
        checkAndUnlockFeatures()
    }
}

// MARK: - XP Curve

struct XPCurve {
    /// XP required to level up FROM the given level to level+1.
    static func xpRequired(forLevel level: Int) -> Int {
        switch level {
        case 1:       return 100
        case 2:       return 150
        case 3:       return 200
        case 4:       return 250
        case 5:       return 350
        case 6...9:   return 100 * level + 150
        default:      return 200 * level   // Linear from level 10+
        }
    }
}

// MARK: - XP Sources

enum XPSource {
    case feed, play, clean, sell, fulfillOrder, levelMilestone, breedSuccess

    var xpReward: Int {
        switch self {
        case .feed:          return 5
        case .play:          return 8
        case .clean:         return 5
        case .sell:          return 20
        case .fulfillOrder:  return 35
        case .levelMilestone: return 0
        case .breedSuccess:  return 50
        }
    }
}

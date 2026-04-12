import Foundation
import Combine

// MARK: - Level-Up Event

struct LevelUpEvent: Identifiable {
    let id       = UUID()
    let newLevel: Int
    let unlockedFeature: GameFeature?   // nil if no new feature this level
    let milestoneReward: MilestoneReward?
}

// MARK: - ProgressionViewModel

@MainActor
final class ProgressionViewModel: ObservableObject {
    @Published var coins:    Int = 500
    @Published var stardust: Int = 0
    @Published var level:    Int = 1
    @Published var xp:       Int = 0
    @Published var storyAct: Int = 1
    @Published var unlockedFeatures: Set<String> = ["shop", "habitat", "feeding", "playing"]

    /// Set when a level-up occurs — consumed by ContentView to show the overlay.
    @Published var pendingLevelUp: LevelUpEvent? = nil

    private let dataManager = DataManager.shared

    // MARK: Load

    func load() async {
        let state        = dataManager.playerState()
        coins            = Int(state.coins)
        stardust         = Int(state.stardust)
        level            = Int(state.currentLevel)
        xp               = Int(state.currentXP)
        storyAct         = Int(state.storyAct)
        if let data      = state.unlockedFeaturesData,
           let features  = try? JSONDecoder().decode(Set<String>.self, from: data) {
            unlockedFeatures = features
        }
    }

    // MARK: Economy

    func canAffordCoins(_ amount: Int) -> Bool { coins >= amount }

    @discardableResult
    func spendCoins(_ amount: Int) -> Bool {
        guard canAffordCoins(amount) else { return false }
        coins -= amount
        persist()
        return true
    }

    func earnCoins(_ amount: Int) {
        coins += amount
        persist()
    }

    // MARK: XP / Leveling

    func addXP(_ amount: Int) {
        var progression  = localProgression
        let levelsGained = progression.addXP(amount)
        xp               = progression.currentXP
        level            = progression.currentLevel
        unlockedFeatures = progression.unlockedFeatures

        // Fire level-up events one at a time (usually just one)
        for newLevel in levelsGained {
            let milestone = MilestoneReward.milestones[newLevel]
            if let m = milestone {
                coins    += m.coinBonus
                stardust += m.stardustBonus
            }

            // Determine if a new feature just unlocked at exactly this level
            let newFeature = GameFeature.allCases.first { feature in
                feature.requiredLevel == newLevel && feature.requiredStoryAct == nil
            }

            pendingLevelUp = LevelUpEvent(
                newLevel:        newLevel,
                unlockedFeature: newFeature,
                milestoneReward: milestone
            )
        }
        persist()
    }

    // MARK: Feature Gating

    func isFeatureUnlocked(_ feature: GameFeature) -> Bool {
        unlockedFeatures.contains(feature.rawValue)
    }

    // MARK: Story

    func advanceStoryAct(to act: Int) {
        storyAct = act
        var progression = localProgression
        progression.advanceStoryAct(to: act)
        unlockedFeatures = progression.unlockedFeatures
        persist()
    }

    // MARK: XP helpers

    var xpToNextLevel: Int   { XPCurve.xpRequired(forLevel: level) }
    var xpFraction:    Double { xpToNextLevel > 0 ? Double(xp) / Double(xpToNextLevel) : 0 }

    // MARK: Persistence

    private var localProgression: PlayerProgression {
        var p            = PlayerProgression()
        p.currentLevel   = level
        p.currentXP      = xp
        p.unlockedFeatures = unlockedFeatures
        p.storyAct       = storyAct
        return p
    }

    private func persist() {
        let state              = dataManager.playerState()
        state.coins            = Int32(coins)
        state.stardust         = Int32(stardust)
        state.currentLevel     = Int16(level)
        state.currentXP        = Int32(xp)
        state.storyAct         = Int16(storyAct)
        state.unlockedFeaturesData = try? JSONEncoder().encode(unlockedFeatures)
        dataManager.save()
    }
}

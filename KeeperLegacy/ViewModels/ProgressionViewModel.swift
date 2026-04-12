import Foundation
import Combine

// MARK: - ProgressionViewModel
// Owns all player state: coins, stardust, level, XP, story act.
// Single source of truth for the currency header and feature gate checks.

@MainActor
final class ProgressionViewModel: ObservableObject {
    @Published var coins:    Int = 500
    @Published var stardust: Int = 0
    @Published var level:    Int = 1
    @Published var xp:       Int = 0
    @Published var storyAct: Int = 1
    @Published var unlockedFeatures: Set<String> = ["shop", "habitat", "feeding", "playing"]

    private let dataManager = DataManager.shared

    // MARK: Load

    func load() async {
        let state = dataManager.playerState()
        coins            = Int(state.coins)
        stardust         = Int(state.stardust)
        level            = Int(state.currentLevel)
        xp               = Int(state.currentXP)
        storyAct         = Int(state.storyAct)
        if let data = state.unlockedFeaturesData,
           let features = try? JSONDecoder().decode(Set<String>.self, from: data) {
            unlockedFeatures = features
        }
    }

    // MARK: Economy

    func canAffordCoins(_ amount: Int) -> Bool { coins >= amount }

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
        var progression = localProgression
        let levelsGained = progression.addXP(amount)
        xp    = progression.currentXP
        level = progression.currentLevel
        unlockedFeatures = progression.unlockedFeatures

        for newLevel in levelsGained {
            handleLevelUp(newLevel)
        }
        persist()
    }

    private func handleLevelUp(_ newLevel: Int) {
        if let milestone = MilestoneReward.milestones[newLevel] {
            earnCoins(milestone.coinBonus)
            stardust += milestone.stardustBonus
        }
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

    var xpToNextLevel: Int { XPCurve.xpRequired(forLevel: level) }
    var xpFraction:    Double { xpToNextLevel > 0 ? Double(xp) / Double(xpToNextLevel) : 0 }

    // MARK: Persistence

    private var localProgression: PlayerProgression {
        var p = PlayerProgression()
        p.currentLevel     = level
        p.currentXP        = xp
        p.unlockedFeatures = unlockedFeatures
        p.storyAct         = storyAct
        return p
    }

    private func persist() {
        let state         = dataManager.playerState()
        state.coins       = Int32(coins)
        state.stardust    = Int32(stardust)
        state.currentLevel = Int16(level)
        state.currentXP   = Int32(xp)
        state.storyAct    = Int16(storyAct)
        state.unlockedFeaturesData = try? JSONEncoder().encode(unlockedFeatures)
        dataManager.save()
    }
}

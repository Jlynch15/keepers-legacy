// Managers/ProgressionManager.cs
// Autoload singleton — owns PlayerEconomy and PlayerProgression.
// Emits signals whenever coins, XP, level, or feature state changes
// so UI nodes can react without polling.

using Godot;
using System.Collections.Generic;
using KeeperLegacy.Models;

public partial class ProgressionManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    [Signal] public delegate void CoinsChangedEventHandler(int newAmount);
    [Signal] public delegate void StardustChangedEventHandler(int newAmount);
    [Signal] public delegate void LeveledUpEventHandler(int newLevel);
    [Signal] public delegate void XPChangedEventHandler(int currentXP, int requiredXP);
    [Signal] public delegate void FeatureUnlockedEventHandler(string featureRaw);
    [Signal] public delegate void MilestoneReachedEventHandler(int level);

    // ── State ─────────────────────────────────────────────────────────────────

    public PlayerEconomy   Economy    { get; private set; } = new();
    public PlayerProgression Progression { get; private set; } = new();

    // ── Convenience read-outs for UI ──────────────────────────────────────────

    public int Coins       => Economy.Coins;
    public int Stardust    => Economy.Stardust;
    public int CurrentLevel => Progression.CurrentLevel;
    public int CurrentXP   => Progression.CurrentXP;
    public int XPToNextLevel => Progression.XpToNextLevel;

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(PlayerEconomy economy, PlayerProgression progression)
    {
        Economy     = economy;
        Progression = progression;
    }

    // ── XP / Leveling ─────────────────────────────────────────────────────────

    public void AddXPFromSource(XPSource source) =>
        AddXPAmount(source.XpReward());

    public void AddXPAmount(int amount)
    {
        if (amount <= 0) return;

        var levelsGained = Progression.AddXP(amount);

        EmitSignal(SignalName.XPChanged, Progression.CurrentXP, Progression.XpToNextLevel);

        foreach (int level in levelsGained)
        {
            EmitSignal(SignalName.LeveledUp, level);
            CheckMilestone(level);
        }

        // Emit any feature unlocks that happened during level-ups
        // (CheckAndUnlockFeatures is called inside AddXP already — emit for all now unlocked)
        // We rely on the scene subscribing to FeatureUnlocked to react.
    }

    private void CheckMilestone(int level)
    {
        if (!MilestoneReward.Milestones.TryGetValue(level, out var milestone)) return;
        if (Progression.ClaimedMilestones.Contains(level)) return;

        Progression.ClaimedMilestones.Add(level);

        if (milestone.CoinBonus > 0)    EarnCoins(milestone.CoinBonus);
        if (milestone.StardustBonus > 0) EarnStardust(milestone.StardustBonus);
        if (milestone.UnlocksFeature.HasValue)
            EmitSignal(SignalName.FeatureUnlocked, milestone.UnlocksFeature.Value.RawValue());

        EmitSignal(SignalName.MilestoneReached, level);
    }

    // ── Economy ───────────────────────────────────────────────────────────────

    public void EarnCoins(int amount)
    {
        if (amount <= 0) return;
        Economy.EarnCoins(amount);
        EmitSignal(SignalName.CoinsChanged, Economy.Coins);
    }

    /// Returns true if coins were spent successfully.
    public bool SpendCoins(int amount)
    {
        if (!Economy.SpendCoins(amount)) return false;
        EmitSignal(SignalName.CoinsChanged, Economy.Coins);
        return true;
    }

    public void EarnStardust(int amount)
    {
        if (amount <= 0) return;
        Economy.EarnStardust(amount);
        EmitSignal(SignalName.StardustChanged, Economy.Stardust);
    }

    public bool SpendStardust(int amount)
    {
        if (!Economy.SpendStardust(amount)) return false;
        EmitSignal(SignalName.StardustChanged, Economy.Stardust);
        return true;
    }

    // ── Feature Queries ───────────────────────────────────────────────────────

    public bool IsFeatureUnlocked(GameFeature feature) =>
        Progression.IsFeatureUnlocked(feature);
}

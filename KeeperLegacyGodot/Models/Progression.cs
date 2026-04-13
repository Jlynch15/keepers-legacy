// Models/Progression.cs
// Port of Progression.swift
// Pure C# — no Godot dependency.

using System;
using System.Collections.Generic;
using System.Linq;

namespace KeeperLegacy.Models
{
    // ── Feature Flags ─────────────────────────────────────────────────────────

    public enum GameFeature
    {
        Shop,
        Habitat,
        Feeding,
        Playing,
        Selling,
        CustomerOrders,
        Breeding,
        Monsterpedia,
        HabitatExpansion,
        Mutations,
        MagicalHabitat,
        Cosmetics
    }

    public static class GameFeatureExtensions
    {
        public static string RawValue(this GameFeature feature) => feature switch
        {
            GameFeature.Shop             => "shop",
            GameFeature.Habitat          => "habitat",
            GameFeature.Feeding          => "feeding",
            GameFeature.Playing          => "playing",
            GameFeature.Selling          => "selling",
            GameFeature.CustomerOrders   => "customerOrders",
            GameFeature.Breeding         => "breeding",
            GameFeature.Monsterpedia     => "monsterpedia",
            GameFeature.HabitatExpansion => "habitatExpansion",
            GameFeature.Mutations        => "mutations",
            GameFeature.MagicalHabitat   => "magicalHabitat",
            GameFeature.Cosmetics        => "cosmetics",
            _                            => feature.ToString().ToLower()
        };

        /// Minimum level required to unlock (0 = available from start).
        public static int RequiredLevel(this GameFeature feature) => feature switch
        {
            GameFeature.Shop             => 0,
            GameFeature.Habitat          => 0,
            GameFeature.Feeding          => 0,
            GameFeature.Playing          => 0,
            GameFeature.Selling          => 1,
            GameFeature.CustomerOrders   => 3,
            GameFeature.Breeding         => 12,
            GameFeature.Monsterpedia     => 2,
            GameFeature.HabitatExpansion => 2,
            GameFeature.Mutations        => 15,
            GameFeature.MagicalHabitat   => 0,   // Story-gated, not level-gated
            GameFeature.Cosmetics        => 5,
            _                            => 0
        };

        /// Story act required in addition to level (null = no story gate).
        public static int? RequiredStoryAct(this GameFeature feature) => feature switch
        {
            GameFeature.Breeding       => 1,
            GameFeature.MagicalHabitat => 2,
            _                          => null
        };

        public static GameFeature? FromRawValue(string raw) => raw switch
        {
            "shop"             => GameFeature.Shop,
            "habitat"          => GameFeature.Habitat,
            "feeding"          => GameFeature.Feeding,
            "playing"          => GameFeature.Playing,
            "selling"          => GameFeature.Selling,
            "customerOrders"   => GameFeature.CustomerOrders,
            "breeding"         => GameFeature.Breeding,
            "monsterpedia"     => GameFeature.Monsterpedia,
            "habitatExpansion" => GameFeature.HabitatExpansion,
            "mutations"        => GameFeature.Mutations,
            "magicalHabitat"   => GameFeature.MagicalHabitat,
            "cosmetics"        => GameFeature.Cosmetics,
            _                  => null
        };
    }

    // ── Milestone Rewards ─────────────────────────────────────────────────────

    public class MilestoneReward
    {
        public int Level { get; }
        public int CoinBonus { get; }
        public int StardustBonus { get; }
        public GameFeature? UnlocksFeature { get; }
        public string? StoryEvent { get; }
        public string Description { get; }

        public MilestoneReward(int level, int coinBonus, int stardustBonus,
            GameFeature? unlocksFeature, string? storyEvent, string description)
        {
            Level          = level;
            CoinBonus      = coinBonus;
            StardustBonus  = stardustBonus;
            UnlocksFeature = unlocksFeature;
            StoryEvent     = storyEvent;
            Description    = description;
        }

        public static readonly Dictionary<int, MilestoneReward> Milestones = new()
        {
            [5]  = new(5,  200,  0,   GameFeature.Cosmetics, null,                 "Cosmetics unlocked!"),
            [10] = new(10, 500,  25,  null,                  "act1_shop_secret",   "The shop's secret begins to reveal itself..."),
            [12] = new(12, 300,  0,   GameFeature.Breeding,  null,                 "Breeding unlocked! Requires Act I completion."),
            [15] = new(15, 400,  0,   GameFeature.Mutations, null,                 "Mutation variants discovered!"),
            [25] = new(25, 1000, 50,  null,                  "act2_revelation",    "A revelation about the ancient civilization..."),
            [50] = new(50, 5000, 200, null,                  "act3_legacy",        "You have mastered the Keeper's Legacy!"),
        };
    }

    // ── Player Progression State ──────────────────────────────────────────────

    public class PlayerProgression
    {
        public int CurrentLevel { get; set; }
        public int CurrentXP { get; set; }
        public HashSet<string> UnlockedFeatures { get; set; }   // GameFeature.RawValue()
        public HashSet<int> ClaimedMilestones { get; set; }     // Level numbers claimed
        public int StoryAct { get; set; }                       // 1, 2, or 3

        public PlayerProgression()
        {
            CurrentLevel     = 1;
            CurrentXP        = 0;
            StoryAct         = 1;
            ClaimedMilestones = new HashSet<int>();

            // Unlock all features that require level 0 and no story gate
            UnlockedFeatures = new HashSet<string>(
                Enum.GetValues<GameFeature>()
                    .Where(f => f.RequiredLevel() == 0 && f.RequiredStoryAct() == null)
                    .Select(f => f.RawValue())
            );
        }

        // For deserialization
        public PlayerProgression(int currentLevel, int currentXP,
            HashSet<string> unlockedFeatures, HashSet<int> claimedMilestones, int storyAct)
        {
            CurrentLevel      = currentLevel;
            CurrentXP         = currentXP;
            UnlockedFeatures  = unlockedFeatures;
            ClaimedMilestones = claimedMilestones;
            StoryAct          = storyAct;
        }

        // ── XP / Leveling ─────────────────────────────────────────────────────

        /// XP needed to advance from current level to the next.
        public int XpToNextLevel => XPCurve.XpRequired(CurrentLevel);

        /// Add XP and handle level-ups. Returns list of levels gained (usually 0 or 1).
        public List<int> AddXP(int amount)
        {
            var levelsGained = new List<int>();
            CurrentXP += amount;

            while (CurrentXP >= XpToNextLevel && CurrentLevel < 50)
            {
                CurrentXP    -= XpToNextLevel;
                CurrentLevel += 1;
                levelsGained.Add(CurrentLevel);
                CheckAndUnlockFeatures();
            }

            if (CurrentLevel == 50) CurrentXP = 0;
            return levelsGained;
        }

        // ── Feature Unlocking ─────────────────────────────────────────────────

        public bool IsFeatureUnlocked(GameFeature feature) =>
            UnlockedFeatures.Contains(feature.RawValue());

        public void CheckAndUnlockFeatures()
        {
            foreach (var feature in Enum.GetValues<GameFeature>())
            {
                if (IsFeatureUnlocked(feature)) continue;

                bool levelOk = CurrentLevel >= feature.RequiredLevel();
                bool storyOk = feature.RequiredStoryAct() is int reqAct
                    ? StoryAct >= reqAct
                    : true;

                if (levelOk && storyOk)
                    UnlockedFeatures.Add(feature.RawValue());
            }
        }

        /// Called when a story act completes — re-evaluates story-gated features.
        public void AdvanceStoryAct(int act)
        {
            StoryAct = act;
            CheckAndUnlockFeatures();
        }
    }

    // ── XP Curve ──────────────────────────────────────────────────────────────

    public static class XPCurve
    {
        /// XP required to level up FROM the given level to level+1.
        public static int XpRequired(int level) => level switch
        {
            1     => 100,
            2     => 150,
            3     => 200,
            4     => 250,
            5     => 350,
            >= 6 and <= 9 => 100 * level + 150,
            _     => 200 * level   // Linear from level 10+
        };
    }

    // ── XP Sources ────────────────────────────────────────────────────────────

    public enum XPSource
    {
        Feed,
        Play,
        Clean,
        Sell,
        FulfillOrder,
        LevelMilestone,
        BreedSuccess
    }

    public static class XPSourceExtensions
    {
        public static int XpReward(this XPSource source) => source switch
        {
            XPSource.Feed           => 5,
            XPSource.Play           => 8,
            XPSource.Clean          => 5,
            XPSource.Sell           => 20,
            XPSource.FulfillOrder   => 35,
            XPSource.LevelMilestone => 0,
            XPSource.BreedSuccess   => 50,
            _                       => 0
        };
    }
}

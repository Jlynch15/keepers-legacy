// Models/Creature.cs
// Port of Creature.swift
// Pure C# — no Godot dependency. Fully unit-testable standalone.

using System;
using System.Collections.Generic;

namespace KeeperLegacy.Models
{
    // ── Enums ──────────────────────────────────────────────────────────────────

    public enum HabitatType
    {
        Water,
        Dirt,
        Grass,
        Fire,
        Ice,
        Electric,
        Magical
    }

    public static class HabitatTypeExtensions
    {
        public static string RawValue(this HabitatType type) => type.ToString();

        public static string DisplayColor(this HabitatType type) => type switch
        {
            HabitatType.Water    => "#A8D8EA",
            HabitatType.Dirt     => "#C9A876",
            HabitatType.Grass    => "#A8D5A8",
            HabitatType.Fire     => "#FF8C42",
            HabitatType.Ice      => "#B8D4E8",
            HabitatType.Electric => "#F0E68C",
            HabitatType.Magical  => "#C99BFF",
            _                    => "#CCCCCC"
        };

        /// Returns the story act required to unlock this habitat type, or null if none.
        public static int? RequiresStoryAct(this HabitatType type) => type switch
        {
            HabitatType.Magical => 2,
            _                   => null
        };

        public static HabitatType? FromRawValue(string raw) => raw switch
        {
            "Water"    => HabitatType.Water,
            "Dirt"     => HabitatType.Dirt,
            "Grass"    => HabitatType.Grass,
            "Fire"     => HabitatType.Fire,
            "Ice"      => HabitatType.Ice,
            "Electric" => HabitatType.Electric,
            "Magical"  => HabitatType.Magical,
            _          => null
        };
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare
    }

    public static class RarityExtensions
    {
        public static string RawValue(this Rarity rarity) => rarity.ToString();

        /// Base shop purchase price in Coins.
        public static int BasePrice(this Rarity rarity) => rarity switch
        {
            Rarity.Common   => 150,
            Rarity.Uncommon => 400,
            Rarity.Rare     => 1200,
            _               => 150
        };

        /// Base sell value in Coins (before happiness multiplier).
        public static int BaseSellValue(this Rarity rarity) => rarity switch
        {
            Rarity.Common   => 75,
            Rarity.Uncommon => 200,
            Rarity.Rare     => 700,
            _               => 75
        };

        public static Rarity? FromRawValue(string raw) => raw switch
        {
            "Common"   => Rarity.Common,
            "Uncommon" => Rarity.Uncommon,
            "Rare"     => Rarity.Rare,
            _          => null
        };
    }

    public enum LifecycleStage
    {
        Egg,
        Baby,
        Adolescent,
        Adult
    }

    public static class LifecycleStageExtensions
    {
        public static string RawValue(this LifecycleStage stage) => stage.ToString();

        /// Hours required to advance to the next stage.
        public static double DurationHours(this LifecycleStage stage) => stage switch
        {
            LifecycleStage.Egg        => 12.0,
            LifecycleStage.Baby       => 48.0,
            LifecycleStage.Adolescent => 96.0,
            LifecycleStage.Adult      => double.PositiveInfinity,
            _                         => double.PositiveInfinity
        };
    }

    // ── Creature Catalog Entry (static reference data) ─────────────────────────

    /// Describes a species. NOT a player-owned instance.
    /// Use CreatureInstance for creatures the player actually owns.
    public class CreatureCatalogEntry
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public HabitatType HabitatType { get; }
        public Rarity Rarity { get; }
        public string FavoriteToy { get; }
        public List<MutationVariant> Mutations { get; }

        public CreatureCatalogEntry(
            string id,
            string name,
            string description,
            HabitatType habitatType,
            Rarity rarity,
            string favoriteToy,
            List<MutationVariant> mutations)
        {
            Id          = id;
            Name        = name;
            Description = description;
            HabitatType = habitatType;
            Rarity      = rarity;
            FavoriteToy = favoriteToy;
            Mutations   = mutations;
        }

        public class MutationVariant
        {
            public int Index { get; }
            public string ColorHint { get; }

            public MutationVariant(int index, string colorHint)
            {
                Index     = index;
                ColorHint = colorHint;
            }
        }
    }

    // ── Creature Instance (player-owned) ──────────────────────────────────────

    /// A specific creature the player owns, with live care stats.
    public class CreatureInstance
    {
        public Guid Id { get; }
        public string CatalogId { get; }      // References CreatureCatalogEntry.Id
        public int MutationIndex { get; }     // 0–3

        // Care stats — 0.0 (empty) to 1.0 (full)
        public double Hunger { get; set; }       // 1.0 = fully fed
        public double Happiness { get; set; }    // 1.0 = very happy
        public double Cleanliness { get; set; }  // 1.0 = clean
        public double Affection { get; set; }    // 1.0 = very bonded
        public double Playfulness { get; set; }  // 1.0 = wants to play

        // Lifecycle
        public LifecycleStage Lifecycle { get; set; }
        public DateTime LifecycleStartDate { get; set; }

        // Discovery
        public bool DiscoveredFavoriteToy { get; set; }

        // Lineage
        public List<Guid> ParentIds { get; }

        // Metadata
        public DateTime DateAcquired { get; }
        public string? Nickname { get; set; }

        // ── Computed ──────────────────────────────────────────────────────────

        /// Weighted average of all care stats — used for sell price multiplier.
        public double OverallHappiness =>
            (Hunger + Happiness + Cleanliness + Affection + Playfulness) / 5.0;

        /// Sell price multiplier: 1.0x at neutral, 2.0x at max happiness.
        public double SellMultiplier => 1.0 + OverallHappiness;

        // ── Constructor ───────────────────────────────────────────────────────

        public CreatureInstance(
            string catalogId,
            int mutationIndex = 0,
            List<Guid>? parentIds = null)
        {
            Id                  = Guid.NewGuid();
            CatalogId           = catalogId;
            MutationIndex       = mutationIndex;
            Hunger              = 0.8;
            Happiness           = 0.7;
            Cleanliness         = 0.9;
            Affection           = 0.5;
            Playfulness         = 0.8;
            Lifecycle           = LifecycleStage.Adult;
            LifecycleStartDate  = DateTime.UtcNow;
            DiscoveredFavoriteToy = false;
            ParentIds           = parentIds ?? new List<Guid>();
            DateAcquired        = DateTime.UtcNow;
            Nickname            = null;
        }

        // For deserialization
        public CreatureInstance(
            Guid id, string catalogId, int mutationIndex,
            double hunger, double happiness, double cleanliness,
            double affection, double playfulness,
            LifecycleStage lifecycle, DateTime lifecycleStartDate,
            bool discoveredFavoriteToy, List<Guid> parentIds,
            DateTime dateAcquired, string? nickname)
        {
            Id                    = id;
            CatalogId             = catalogId;
            MutationIndex         = mutationIndex;
            Hunger                = hunger;
            Happiness             = happiness;
            Cleanliness           = cleanliness;
            Affection             = affection;
            Playfulness           = playfulness;
            Lifecycle             = lifecycle;
            LifecycleStartDate    = lifecycleStartDate;
            DiscoveredFavoriteToy = discoveredFavoriteToy;
            ParentIds             = parentIds;
            DateAcquired          = dateAcquired;
            Nickname              = nickname;
        }

        // ── Care Actions ──────────────────────────────────────────────────────

        /// Feed the creature. Returns XP gained.
        public int Feed(double foodValue)
        {
            Hunger    = Math.Min(1.0, Hunger    + foodValue);
            Happiness = Math.Min(1.0, Happiness + foodValue * 0.1);
            return 5;
        }

        /// Play with a toy. Returns XP gained and whether the favorite was discovered.
        public (int xp, bool discoveredFavorite) Play(string toyName, bool isFavoriteToy)
        {
            double baseHappiness  = 0.15;
            double bonusHappiness = isFavoriteToy ? 0.10 : 0.0;

            Playfulness = Math.Min(1.0, Playfulness + baseHappiness + bonusHappiness);
            Happiness   = Math.Min(1.0, Happiness   + baseHappiness + bonusHappiness);
            Affection   = Math.Min(1.0, Affection   + 0.05);

            bool discovered = false;
            if (isFavoriteToy && !DiscoveredFavoriteToy)
            {
                DiscoveredFavoriteToy = true;
                discovered = true;
            }

            int xp = isFavoriteToy ? 15 : 8;
            return (xp, discovered);
        }

        /// Clean the creature. Returns XP gained.
        public int Clean()
        {
            Cleanliness = 1.0;
            Happiness   = Math.Min(1.0, Happiness + 0.05);
            return 5;
        }

        /// Apply time-based stat decay. Call once per game tick (e.g. every 30 min).
        public void ApplyDecay(double hoursPassed)
        {
            double decayRate = 0.02 * hoursPassed;
            Hunger      = Math.Max(0.0, Hunger      - decayRate * 1.5);
            Happiness   = Math.Max(0.0, Happiness   - decayRate * 1.0);
            Cleanliness = Math.Max(0.0, Cleanliness - decayRate * 0.8);
            Affection   = Math.Max(0.0, Affection   - decayRate * 0.5);
            Playfulness = Math.Max(0.0, Playfulness - decayRate * 1.0);
        }
    }
}

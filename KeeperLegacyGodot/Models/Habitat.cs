// Models/Habitat.cs
// Port of Habitat.swift
// Pure C# — no Godot dependency.

using System;
using System.Collections.Generic;

namespace KeeperLegacy.Models
{
    public class Habitat
    {
        public Guid Id { get; }
        public HabitatType Type { get; }

        /// The Guid of the CreatureInstance currently housed here, or null if empty.
        public Guid? OccupantId { get; set; }

        /// Cosmetic decoration item IDs the player has placed.
        public List<string> DecorationIds { get; }

        /// Player level at which this habitat slot was unlocked.
        public int UnlockedAtLevel { get; }

        // ── Computed ──────────────────────────────────────────────────────────

        public bool IsEmpty => OccupantId == null;

        // ── Constructor ───────────────────────────────────────────────────────

        public Habitat(HabitatType type, int unlockedAtLevel)
        {
            Id              = Guid.NewGuid();
            Type            = type;
            OccupantId      = null;
            DecorationIds   = new List<string>();
            UnlockedAtLevel = unlockedAtLevel;
        }

        // For deserialization
        public Habitat(Guid id, HabitatType type, Guid? occupantId,
                       List<string> decorationIds, int unlockedAtLevel)
        {
            Id              = id;
            Type            = type;
            OccupantId      = occupantId;
            DecorationIds   = decorationIds;
            UnlockedAtLevel = unlockedAtLevel;
        }

        // ── Mutations ─────────────────────────────────────────────────────────

        public void PlaceCreature(Guid creatureId) => OccupantId = creatureId;

        public void RemoveCreature() => OccupantId = null;

        public void AddDecoration(string decorationId)
        {
            if (!DecorationIds.Contains(decorationId))
                DecorationIds.Add(decorationId);
        }

        public void RemoveDecoration(string decorationId) =>
            DecorationIds.Remove(decorationId);
    }

    // ── Habitat Unlock Schedule ───────────────────────────────────────────────

    /// Maps player level → total habitat slots unlocked cumulatively.
    public static class HabitatUnlockSchedule
    {
        /// Returns the total number of standard habitats unlocked at the given level.
        public static int HabitatsUnlocked(int level) => level switch
        {
            < 2  => 1,
            < 5  => 2,
            < 8  => 3,
            < 12 => 4,
            < 18 => 5,
            < 25 => 6,
            < 35 => 7,
            _    => 8
        };

        /// Magical habitat unlocks via story Act II, not level.
        public const int MagicalHabitatRequiresAct = 2;
    }

    // ── Habitat Expansion Cost ────────────────────────────────────────────────

    public static class HabitatExpansionCost
    {
        /// Coin cost to unlock the given slot number (1-indexed).
        public static int Cost(int slot) => slot switch
        {
            1 => 0,       // First habitat is free
            2 => 500,
            3 => 1000,
            4 => 2000,
            5 => 3000,
            6 => 4000,
            7 => 5000,
            _ => 6000
        };
    }
}

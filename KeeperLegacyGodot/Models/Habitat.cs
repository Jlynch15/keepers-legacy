// Models/Habitat.cs
// Habitat instance: holds up to HabitatCapacity.CreaturesPerHabitat creatures.

using System;
using System.Collections.Generic;

namespace KeeperLegacy.Models
{
    public class Habitat
    {
        public Guid Id { get; }
        public HabitatType Type { get; }

        /// Creatures currently housed (max HabitatCapacity.CreaturesPerHabitat).
        /// Mutate only via TryPlaceCreature / RemoveCreature so capacity is enforced.
        public List<Guid> OccupantIds { get; }

        /// Cosmetic decoration item IDs the player has placed.
        public List<string> DecorationIds { get; }

        /// Player level at which this habitat slot was unlocked.
        public int UnlockedAtLevel { get; }

        // -- Computed ----------------------------------------------------------

        public bool IsEmpty => OccupantIds.Count == 0;
        public bool IsFull  => OccupantIds.Count >= HabitatCapacity.CreaturesPerHabitat;
        public int  AvailableSlots => HabitatCapacity.CreaturesPerHabitat - OccupantIds.Count;

        // -- Constructors ------------------------------------------------------

        public Habitat(HabitatType type, int unlockedAtLevel)
        {
            Id              = Guid.NewGuid();
            Type            = type;
            OccupantIds     = new List<Guid>();
            DecorationIds   = new List<string>();
            UnlockedAtLevel = unlockedAtLevel;
        }

        /// Deserialization constructor.
        public Habitat(Guid id, HabitatType type, List<Guid> occupantIds,
                       List<string> decorationIds, int unlockedAtLevel)
        {
            Id              = id;
            Type            = type;
            OccupantIds     = occupantIds ?? new List<Guid>();
            DecorationIds   = decorationIds ?? new List<string>();
            UnlockedAtLevel = unlockedAtLevel;
        }

        // -- Mutations ---------------------------------------------------------

        /// Adds creatureId to OccupantIds. Returns false if full or already present.
        public bool TryPlaceCreature(Guid creatureId)
        {
            if (IsFull) return false;
            if (OccupantIds.Contains(creatureId)) return false;
            OccupantIds.Add(creatureId);
            return true;
        }

        /// Removes creatureId. Returns false if not present.
        public bool RemoveCreature(Guid creatureId)
        {
            return OccupantIds.Remove(creatureId);
        }

        public void AddDecoration(string decorationId)
        {
            if (!DecorationIds.Contains(decorationId))
                DecorationIds.Add(decorationId);
        }

        public void RemoveDecoration(string decorationId) =>
            DecorationIds.Remove(decorationId);
    }

    // Note: HabitatUnlockSchedule has been removed (was level->total-habitats; now
    // per-biome via HabitatCapacity). HabitatExpansionCost stays in this file.

    public static class HabitatExpansionCost
    {
        /// Coin cost to unlock the given slot number (1-indexed).
        public static int Cost(int slot) => slot switch
        {
            1 => 0,
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

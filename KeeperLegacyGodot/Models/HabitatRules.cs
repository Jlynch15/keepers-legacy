// Models/HabitatRules.cs
// Pure-C# habitat-management rules. No Godot dependency, so this layer is
// directly unit-testable. HabitatManager (Node autoload) wraps these helpers
// and adds signal emission + autoload lookups.

using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;

namespace KeeperLegacy.Models
{
    public static class HabitatRules
    {
        /// Place a creature into a specific habitat slot, evicting from a
        /// previous habitat if any. Returns false on missing habitat, full
        /// habitat, or biome-type mismatch. Does NOT emit signals -- caller
        /// (the manager) is responsible for that.
        public static bool TryPlaceCreatureInSlot(
            List<Habitat> habitats,
            List<CreatureInstance> creatures,
            Guid habitatId,
            Guid creatureId)
        {
            var habitat = habitats.FirstOrDefault(h => h.Id == habitatId);
            if (habitat == null) return false;

            var creature = creatures.FirstOrDefault(c => c.Id == creatureId);
            if (creature != null)
            {
                var entry = CreatureRosterData.Find(creature.CatalogId);
                if (entry != null && entry.HabitatType != habitat.Type) return false;
            }

            // Try the destination FIRST. If it rejects (full, duplicate, etc.),
            // we must NOT have already evicted from the previous habitat — doing so
            // would leave the creature in the ledger with no home, violating the
            // "every creature lives in a habitat at all times" invariant.
            var previous = habitats.FirstOrDefault(h => h != habitat && h.OccupantIds.Contains(creatureId));
            if (previous == habitat) return true; // already there — no-op success

            // If the creature is currently in `previous`, the destination's duplicate
            // check won't matter (different habitat), but if `habitat` is full we must
            // bail before touching `previous`.
            if (habitat.IsFull) return false;

            // Atomic-ish: place first, then evict. If TryPlaceCreature returns false
            // for any reason we haven't accounted for, the previous habitat is
            // unchanged.
            if (!habitat.TryPlaceCreature(creatureId)) return false;
            previous?.RemoveCreature(creatureId);
            return true;
        }

        /// Filter habitats by biome.
        public static IReadOnlyList<Habitat> HabitatsOfType(
            List<Habitat> habitats, HabitatType biome)
                => habitats.Where(h => h.Type == biome).ToList();

        /// Returns the unlock state for the Nth habitat of a biome (1-indexed).
        /// magicalUnlocked / expansionUnlocked come from the progression layer
        /// (or default false when no progression context is available).
        public static UnlockReason GetUnlockReason(
            List<Habitat> habitats,
            HabitatType biome,
            int oneIndexedSlot,
            bool magicalUnlocked,
            bool expansionUnlocked)
        {
            int max = HabitatCapacity.MaxHabitatsForBiome(biome);
            if (oneIndexedSlot < 1 || oneIndexedSlot > max)
                return new UnlockReason(UnlockReasonKind.OutOfRange);

            int owned = HabitatsOfType(habitats, biome).Count;
            if (oneIndexedSlot <= owned)
                return new UnlockReason(UnlockReasonKind.Owned);

            // First habitat for mid-tier / Magical biomes is story-gated.
            if (oneIndexedSlot == 1)
            {
                if (biome == HabitatType.Magical && !magicalUnlocked)
                    return new UnlockReason(UnlockReasonKind.StoryGated, StoryAct: 2);

                if ((biome == HabitatType.Fire || biome == HabitatType.Ice ||
                     biome == HabitatType.Electric) && !expansionUnlocked)
                    return new UnlockReason(UnlockReasonKind.StoryGated, StoryAct: 1);
                // Water/Grass/Dirt slot 1 is always Owned (game-start) -- handled above.
            }

            // Slot 2+ is purchasable with coins.
            return new UnlockReason(
                UnlockReasonKind.Purchasable,
                Coins: HabitatCapacity.CoinsForHabitat(biome, oneIndexedSlot));
        }
    }

    // -- Unlock-reason value type (pure-C#, lives with the rules) --------------

    public enum UnlockReasonKind { Owned, Purchasable, StoryGated, OutOfRange }
    public record UnlockReason(UnlockReasonKind Kind, int? Coins = null, int? StoryAct = null);
}

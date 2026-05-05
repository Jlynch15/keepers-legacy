// Models/HabitatCapacity.cs
// Per-biome habitat capacity rules. Single source of truth.

namespace KeeperLegacy.Models
{
    public static class HabitatCapacity
    {
        /// How many creatures fit in one habitat. Bumping this is a one-line change.
        public const int CreaturesPerHabitat = 4;

        /// Max habitats a player can own per biome. Bumping a tier here is the
        /// expansion path when more catalog creatures are added later.
        public static int MaxHabitatsForBiome(HabitatType type) => type switch
        {
            HabitatType.Water  or HabitatType.Grass    or HabitatType.Dirt     => 4,
            HabitatType.Fire   or HabitatType.Ice      or HabitatType.Electric => 3,
            HabitatType.Magical                                                => 2,
            _                                                                  => 0
        };

        /// Coin cost to unlock the Nth habitat of a biome (1-indexed within biome).
        /// First slot is always 0 -- Earth-tier player owns slot 1 at game start;
        /// mid-tier and Magical have slot 1 story-gated (no coin path) so the
        /// returned 0 is also correct (the lock state, not the cost, is what
        /// gates them -- see HabitatManager.GetUnlockReason).
        public static int CoinsForHabitat(HabitatType biome, int oneIndexedSlot)
            => HabitatExpansionCost.Cost(oneIndexedSlot);
    }
}

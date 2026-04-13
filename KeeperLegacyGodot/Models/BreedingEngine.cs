// Models/BreedingEngine.cs
// Pure C# breeding logic — no Godot dependency.
// Used by BreedingManager (Godot autoload) and unit tests.

using System;
using System.Collections.Generic;
using KeeperLegacy.Data;

namespace KeeperLegacy.Models
{
    public class BreedingEngine
    {
        // Weighted probability for offspring mutation index selection.
        // Index 0 (base) is most likely; index 3 (rarest variant) is least likely.
        public static readonly double[] MutationWeights = { 0.40, 0.30, 0.20, 0.10 };

        // ── Result ─────────────────────────────────────────────────────────────

        public abstract class BreedResult { }

        public class Success : BreedResult
        {
            public string OffspringCatalogId { get; }
            public int MutationIndex { get; }
            public BreedingRecord Record { get; }
            public Success(string offspringCatalogId, int mutationIndex, BreedingRecord record)
            {
                OffspringCatalogId = offspringCatalogId;
                MutationIndex      = mutationIndex;
                Record             = record;
            }
        }

        public class Failure : BreedResult
        {
            public string Reason { get; }
            public Failure(string reason) => Reason = reason;
        }

        // ── Breed ─────────────────────────────────────────────────────────────

        /// Attempt to breed two creature instances.
        /// Returns Success with offspring data, or Failure with reason.
        public BreedResult Breed(
            CreatureInstance parentA,
            CreatureInstance parentB,
            CreatureCatalogEntry entryA,
            CreatureCatalogEntry entryB,
            PlayerEconomy economy,
            PlayerProgression progression,
            Random? rng = null)
        {
            // 1. Feature gate
            if (!progression.IsFeatureUnlocked(GameFeature.Breeding))
                return new Failure("Breeding is not yet unlocked.");

            // 2. Must both be adults
            if (parentA.Lifecycle != LifecycleStage.Adult)
                return new Failure($"Parent A ({entryA.Name}) is not an adult.");
            if (parentB.Lifecycle != LifecycleStage.Adult)
                return new Failure($"Parent B ({entryB.Name}) is not an adult.");

            // 3. Must share habitat type
            if (entryA.HabitatType != entryB.HabitatType)
                return new Failure(
                    $"Habitat type mismatch: {entryA.HabitatType} vs {entryB.HabitatType}.");

            // 4. Breeding cost (based on the lower rarity of the two parents)
            var lowerRarity = entryA.Rarity < entryB.Rarity ? entryA.Rarity : entryB.Rarity;
            int cost = PricingTable.Breeding.Cost(lowerRarity);
            if (!economy.SpendCoins(cost))
                return new Failure($"Insufficient coins. Need {cost}.");

            // 5. Determine offspring
            var r = rng ?? new Random();
            string offspringCatalogId = r.NextDouble() < 0.5
                ? entryA.Id
                : entryB.Id;

            int mutationIndex = SelectMutation(r);

            var record = new BreedingRecord(parentA.Id, parentB.Id, Guid.NewGuid());
            return new Success(offspringCatalogId, mutationIndex, record);
        }

        /// Weighted random selection of mutation index.
        public int SelectMutation(Random? rng = null)
        {
            var r = rng ?? new Random();
            double roll = r.NextDouble();
            double cumulative = 0.0;
            for (int i = 0; i < MutationWeights.Length; i++)
            {
                cumulative += MutationWeights[i];
                if (roll < cumulative) return i;
            }
            return MutationWeights.Length - 1;
        }

        /// Record of a completed breed (persisted via SaveManager).
        public class BreedingRecord
        {
            public Guid ParentAId { get; }
            public Guid ParentBId { get; }
            public Guid OffspringId { get; }
            public DateTime Date { get; }

            public BreedingRecord(Guid parentAId, Guid parentBId, Guid offspringId)
            {
                ParentAId   = parentAId;
                ParentBId   = parentBId;
                OffspringId = offspringId;
                Date        = DateTime.UtcNow;
            }
        }
    }
}

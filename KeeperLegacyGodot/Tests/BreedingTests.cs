// Tests/BreedingTests.cs
// Port of BreedingTests.swift
// Tests breeding costs, mutation weights, compatibility rules, and BreedingEngine outcomes.

using System;
using System.Linq;
using NUnit.Framework;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class BreedingTests
    {
        // ── Breeding Costs ────────────────────────────────────────────────────

        [Test]
        public void BreedingCostIsPositiveForAllRarities()
        {
            Assert.That(PricingTable.Breeding.Cost(Rarity.Common),   Is.GreaterThan(0));
            Assert.That(PricingTable.Breeding.Cost(Rarity.Uncommon), Is.GreaterThan(0));
            Assert.That(PricingTable.Breeding.Cost(Rarity.Rare),     Is.GreaterThan(0));
        }

        [Test]
        public void BreedingCostOrderingByRarity()
        {
            int common   = PricingTable.Breeding.Cost(Rarity.Common);
            int uncommon = PricingTable.Breeding.Cost(Rarity.Uncommon);
            int rare     = PricingTable.Breeding.Cost(Rarity.Rare);
            Assert.That(common,   Is.LessThan(uncommon));
            Assert.That(uncommon, Is.LessThan(rare));
        }

        [Test]
        public void BreedingCostUsesLowerRarityParent()
        {
            // Cost is determined by the lower-rarity parent
            int commonCost = PricingTable.Breeding.Cost(Rarity.Common);
            var entryA = CreatureRosterData.Find("aquaburst")!;   // Common
            var entryB = CreatureRosterData.Find("seraphine")!;   // Uncommon water — Common cost applies
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("seraphine");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 500);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            engine.Breed(parentA, parentB, entryA, entryB, economy, progression, new Random(42));

            // 500 − commonCost coins should remain (lower rarity cost applied)
            Assert.That(economy.Coins, Is.EqualTo(500 - commonCost));
        }

        // ── Feature Gating ────────────────────────────────────────────────────

        [Test]
        public void BreedingFailsWhenFeatureNotUnlocked()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();  // Level 1 — breeding locked

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entry, entry, economy, progression);

            Assert.That(result, Is.InstanceOf<BreedingEngine.Failure>());
        }

        [Test]
        public void BreedingSucceedsWhenFeatureUnlocked()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entry, entry, economy, progression, new Random(1));

            Assert.That(result, Is.InstanceOf<BreedingEngine.Success>());
        }

        // ── Mutation Weights ─────────────────────────────────────────────────

        [Test]
        public void MutationWeightsSumToOne()
        {
            double sum = BreedingEngine.MutationWeights.Sum();
            Assert.That(sum, Is.EqualTo(1.0).Within(0.001));
        }

        [Test]
        public void MutationWeightsDecreaseByIndex()
        {
            var w = BreedingEngine.MutationWeights;
            Assert.That(w[0], Is.GreaterThan(w[1]));
            Assert.That(w[1], Is.GreaterThan(w[2]));
            Assert.That(w[2], Is.GreaterThan(w[3]));
        }

        [Test]
        public void MutationWeightsHaveFourEntries()
            => Assert.That(BreedingEngine.MutationWeights.Length, Is.EqualTo(4));

        [Test]
        public void SelectMutationReturnsValidIndex()
        {
            var engine = new BreedingEngine();
            for (int i = 0; i < 50; i++)
            {
                int idx = engine.SelectMutation(new Random(i));
                Assert.That(idx, Is.InRange(0, 3));
            }
        }

        // ── Habitat Compatibility ─────────────────────────────────────────────

        [Test]
        public void SameHabitatTypeIsCompatible()
        {
            var entryA  = CreatureRosterData.Find("aquaburst")!;   // Water
            var entryB  = CreatureRosterData.Find("tidecaller")!;  // Water
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("tidecaller");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entryA, entryB, economy, progression, new Random(0));

            Assert.That(result, Is.InstanceOf<BreedingEngine.Success>());
        }

        [Test]
        public void DifferentHabitatTypesFail()
        {
            var entryA  = CreatureRosterData.Find("aquaburst")!;   // Water
            var entryB  = CreatureRosterData.Find("emberpaw")!;    // Fire
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("emberpaw");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entryA, entryB, economy, progression);

            Assert.That(result, Is.InstanceOf<BreedingEngine.Failure>());
        }

        // ── Lifecycle Requirement ─────────────────────────────────────────────

        [Test]
        public void NonAdultParentFails()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Baby;    // Explicitly not adult
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entry, entry, economy, progression);

            Assert.That(result, Is.InstanceOf<BreedingEngine.Failure>());
        }

        // ── Insufficient Coins ────────────────────────────────────────────────

        [Test]
        public void InsufficientCoinsFails()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1);   // Way too few
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entry, entry, economy, progression);

            Assert.That(result, Is.InstanceOf<BreedingEngine.Failure>());
            Assert.That(economy.Coins, Is.EqualTo(1));   // Coins not deducted on failure
        }

        // ── Breed Success Outcome ─────────────────────────────────────────────

        [Test]
        public void SuccessOffspringCatalogIdIsParent()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine  = new BreedingEngine();
            var result  = engine.Breed(parentA, parentB, entry, entry, economy, progression, new Random(5)) as BreedingEngine.Success;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.OffspringCatalogId, Is.EqualTo("aquaburst"));
        }

        [Test]
        public void SuccessDeductsCoins()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            int startCoins  = 1000;
            var economy     = new PlayerEconomy(startingCoins: startCoins);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            engine.Breed(parentA, parentB, entry, entry, economy, progression, new Random(5));

            Assert.That(economy.Coins, Is.LessThan(startCoins));
        }

        // ── Breeding Record ───────────────────────────────────────────────────

        [Test]
        public void SuccessResultContainsParentIds()
        {
            var entry   = CreatureRosterData.Find("aquaburst")!;
            var parentA = new CreatureInstance("aquaburst");
            var parentB = new CreatureInstance("aquaburst");
            parentA.Lifecycle = LifecycleStage.Adult;
            parentB.Lifecycle = LifecycleStage.Adult;

            var economy     = new PlayerEconomy(startingCoins: 1000);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;
            progression.CheckAndUnlockFeatures();

            var engine = new BreedingEngine();
            var result = engine.Breed(parentA, parentB, entry, entry, economy, progression, new Random(5)) as BreedingEngine.Success;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Record.ParentAId, Is.EqualTo(parentA.Id));
            Assert.That(result.Record.ParentBId,  Is.EqualTo(parentB.Id));
        }
    }
}

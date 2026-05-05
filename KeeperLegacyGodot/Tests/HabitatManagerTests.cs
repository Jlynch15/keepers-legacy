// Tests/HabitatManagerTests.cs
// Tests cover the pure-C# HabitatRules helper that backs HabitatManager.
// HabitatManager itself extends Godot.Node and cannot be instantiated outside
// the engine, so the rule logic is split into HabitatRules for unit testing.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class HabitatManagerTests
    {
        private List<Habitat>          _habitats  = null!;
        private List<CreatureInstance> _creatures = null!;

        [SetUp]
        public void Setup()
        {
            _habitats  = new List<Habitat>();
            _creatures = new List<CreatureInstance>();
        }

        [Test]
        public void TryPlaceCreatureInSlot_AddsToHabitat()
        {
            var habitat = new Habitat(HabitatType.Water, 1);
            _habitats.Add(habitat);

            var creatureId = Guid.NewGuid();
            bool result = HabitatRules.TryPlaceCreatureInSlot(
                _habitats, _creatures, habitat.Id, creatureId);

            Assert.That(result, Is.True);
            Assert.That(habitat.OccupantIds, Contains.Item(creatureId));
        }

        [Test]
        public void TryPlaceCreatureInSlot_RejectsWhenFull()
        {
            var habitat = new Habitat(HabitatType.Water, 1);
            for (int i = 0; i < HabitatCapacity.CreaturesPerHabitat; i++)
                habitat.TryPlaceCreature(Guid.NewGuid());
            _habitats.Add(habitat);

            bool result = HabitatRules.TryPlaceCreatureInSlot(
                _habitats, _creatures, habitat.Id, Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public void GetUnlockReason_OwnedSlot()
        {
            _habitats.Add(new Habitat(HabitatType.Water, 1));
            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Water, 1,
                magicalUnlocked: false, expansionUnlocked: false);
            Assert.That(reason.Kind, Is.EqualTo(UnlockReasonKind.Owned));
        }

        [Test]
        public void GetUnlockReason_PurchasableSlot()
        {
            _habitats.Add(new Habitat(HabitatType.Water, 1));
            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Water, 2,
                magicalUnlocked: false, expansionUnlocked: false);
            Assert.That(reason.Kind,  Is.EqualTo(UnlockReasonKind.Purchasable));
            Assert.That(reason.Coins, Is.EqualTo(HabitatExpansionCost.Cost(2)));
        }

        [Test]
        public void GetUnlockReason_OutOfRange()
        {
            // Magical max is 2 -- slot 3 is out of range
            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Magical, 3,
                magicalUnlocked: true, expansionUnlocked: true);
            Assert.That(reason.Kind, Is.EqualTo(UnlockReasonKind.OutOfRange));
        }

        [Test]
        public void GetUnlockReason_EarthTierSlot1_AlwaysOwned()
        {
            // Earth-tier biomes (Water/Grass/Dirt) start with slot 1 unlocked at game start.
            // After CreateNewGame seeds the first habitat, slot 1 is Owned regardless of feature flags.
            _habitats.Add(new Habitat(HabitatType.Water, 1));

            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Water, 1,
                magicalUnlocked: false, expansionUnlocked: false);

            Assert.That(reason.Kind, Is.EqualTo(UnlockReasonKind.Owned));
        }

        [Test]
        public void GetUnlockReason_MidTierSlot1_StoryGatedWhenLocked()
        {
            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Fire, 1,
                magicalUnlocked: false, expansionUnlocked: false);

            Assert.That(reason.Kind,     Is.EqualTo(UnlockReasonKind.StoryGated));
            Assert.That(reason.StoryAct, Is.EqualTo(1));
        }

        [Test]
        public void GetUnlockReason_MagicalSlot1_StoryGatedWhenLocked()
        {
            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Magical, 1,
                magicalUnlocked: false, expansionUnlocked: false);

            Assert.That(reason.Kind,     Is.EqualTo(UnlockReasonKind.StoryGated));
            Assert.That(reason.StoryAct, Is.EqualTo(2));
        }

        [Test]
        public void GetUnlockReason_MidTierSlot1_PurchasableOnceUnlocked()
        {
            // Once HabitatExpansion is unlocked but the player owns no Fire habitats yet,
            // slot 1 should be Purchasable (player can buy their first Fire habitat).
            var reason = HabitatRules.GetUnlockReason(
                _habitats, HabitatType.Fire, 1,
                magicalUnlocked: false, expansionUnlocked: true);

            Assert.That(reason.Kind, Is.EqualTo(UnlockReasonKind.Purchasable));
        }

        [Test]
        public void HabitatsOfType_FiltersByBiome()
        {
            _habitats.Add(new Habitat(HabitatType.Water, 1));
            _habitats.Add(new Habitat(HabitatType.Grass, 1));
            _habitats.Add(new Habitat(HabitatType.Water, 1));

            var waters = HabitatRules.HabitatsOfType(_habitats, HabitatType.Water);

            Assert.That(waters, Has.Count.EqualTo(2));
        }
    }
}

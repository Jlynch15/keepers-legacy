// Tests/HabitatModelTests.cs
using NUnit.Framework;
using System;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class HabitatModelTests
    {
        [Test]
        public void TryPlaceCreature_AddsToEmptyHabitat()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            var creatureId = Guid.NewGuid();

            bool result = h.TryPlaceCreature(creatureId);

            Assert.That(result,                  Is.True);
            Assert.That(h.OccupantIds,           Has.Count.EqualTo(1));
            Assert.That(h.OccupantIds[0],        Is.EqualTo(creatureId));
            Assert.That(h.IsEmpty,               Is.False);
        }

        [Test]
        public void TryPlaceCreature_RejectsWhenFull()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            for (int i = 0; i < HabitatCapacity.CreaturesPerHabitat; i++)
                h.TryPlaceCreature(Guid.NewGuid());

            bool result = h.TryPlaceCreature(Guid.NewGuid());

            Assert.That(result,                  Is.False);
            Assert.That(h.OccupantIds,           Has.Count.EqualTo(HabitatCapacity.CreaturesPerHabitat));
            Assert.That(h.IsFull,                Is.True);
        }

        [Test]
        public void TryPlaceCreature_RejectsDuplicate()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            var id = Guid.NewGuid();
            h.TryPlaceCreature(id);

            bool secondResult = h.TryPlaceCreature(id);

            Assert.That(secondResult,            Is.False);
            Assert.That(h.OccupantIds,           Has.Count.EqualTo(1));
        }

        [Test]
        public void RemoveCreature_RemovesPresent()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            var id = Guid.NewGuid();
            h.TryPlaceCreature(id);

            bool result = h.RemoveCreature(id);

            Assert.That(result,                  Is.True);
            Assert.That(h.OccupantIds,           Is.Empty);
            Assert.That(h.IsEmpty,               Is.True);
        }

        [Test]
        public void RemoveCreature_ReturnsFalseWhenAbsent()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);

            bool result = h.RemoveCreature(Guid.NewGuid());

            Assert.That(result,                  Is.False);
        }

        [Test]
        public void AvailableSlots_ReflectsOccupancy()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            Assert.That(h.AvailableSlots, Is.EqualTo(HabitatCapacity.CreaturesPerHabitat));

            h.TryPlaceCreature(Guid.NewGuid());
            Assert.That(h.AvailableSlots, Is.EqualTo(3));

            h.TryPlaceCreature(Guid.NewGuid());
            h.TryPlaceCreature(Guid.NewGuid());
            h.TryPlaceCreature(Guid.NewGuid());
            Assert.That(h.AvailableSlots, Is.EqualTo(0));
        }

        [Test]
        public void DeserializeFromSingleOccupantSave_BackfillsList()
        {
            // Simulates loading a save written with the legacy single-occupant shape.
            var legacyId = Guid.NewGuid();
            var habitat  = new Habitat(
                id: Guid.NewGuid(),
                type: HabitatType.Water,
                occupantIds: new System.Collections.Generic.List<Guid> { legacyId },
                decorationIds: new System.Collections.Generic.List<string>(),
                unlockedAtLevel: 1);

            Assert.That(habitat.OccupantIds, Has.Count.EqualTo(1));
            Assert.That(habitat.OccupantIds[0], Is.EqualTo(legacyId));
        }

        // -- HabitatCapacity ---------------------------------------------------

        [Test]
        public void MaxHabitatsForBiome_EarthTier()
        {
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Water), Is.EqualTo(4));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Grass), Is.EqualTo(4));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Dirt),  Is.EqualTo(4));
        }

        [Test]
        public void MaxHabitatsForBiome_MidTier()
        {
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Fire),     Is.EqualTo(3));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Ice),      Is.EqualTo(3));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Electric), Is.EqualTo(3));
        }

        [Test]
        public void MaxHabitatsForBiome_Magical()
            => Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Magical), Is.EqualTo(2));

        [Test]
        public void CreaturesPerHabitat_Is4()
            => Assert.That(HabitatCapacity.CreaturesPerHabitat, Is.EqualTo(4));

        [Test]
        public void CoinsForHabitat_FirstSlotFree()
        {
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 1), Is.EqualTo(0));
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Magical, 1), Is.EqualTo(0));
        }

        [Test]
        public void CoinsForHabitat_LadderMatchesExisting()
        {
            // Should match the existing HabitatExpansionCost ladder so cost UI is consistent.
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 2), Is.EqualTo(HabitatExpansionCost.Cost(2)));
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 3), Is.EqualTo(HabitatExpansionCost.Cost(3)));
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 4), Is.EqualTo(HabitatExpansionCost.Cost(4)));
        }
    }
}

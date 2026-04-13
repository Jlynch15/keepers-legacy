// Tests/CreatureRosterTests.cs
// Port of CreatureRosterTests.swift
// Validates that all 58 creatures are present, correctly typed, and well-formed.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class CreatureRosterTests
    {
        // ── Counts ────────────────────────────────────────────────────────────

        [Test] public void TotalCreatureCount()
            => Assert.That(CreatureRosterData.AllCreatures.Count, Is.EqualTo(80));

        [Test] public void WaterCreatureCount()
            => Assert.That(CreatureRosterData.WaterCreatures.Count, Is.EqualTo(15));

        [Test] public void DirtCreatureCount()
            => Assert.That(CreatureRosterData.DirtCreatures.Count, Is.EqualTo(15));

        [Test] public void GrassCreatureCount()
            => Assert.That(CreatureRosterData.GrassCreatures.Count, Is.EqualTo(15));

        [Test] public void FireCreatureCount()
            => Assert.That(CreatureRosterData.FireCreatures.Count, Is.EqualTo(10));

        [Test] public void IceCreatureCount()
            => Assert.That(CreatureRosterData.IceCreatures.Count, Is.EqualTo(10));

        [Test] public void ElectricCreatureCount()
            => Assert.That(CreatureRosterData.ElectricCreatures.Count, Is.EqualTo(10));

        [Test] public void MagicalCreatureCount()
            => Assert.That(CreatureRosterData.MagicalCreatures.Count, Is.EqualTo(5));

        // ── Data Integrity ────────────────────────────────────────────────────

        [Test]
        public void AllCreaturesHaveUniqueIDs()
        {
            var ids    = CreatureRosterData.AllCreatures.Select(c => c.Id).ToList();
            var unique = new HashSet<string>(ids);
            Assert.That(ids.Count, Is.EqualTo(unique.Count), "Duplicate creature IDs found");
        }

        [Test]
        public void AllCreaturesHaveNonEmptyNames()
        {
            foreach (var c in CreatureRosterData.AllCreatures)
                Assert.That(c.Name, Is.Not.Empty, $"Creature '{c.Id}' has empty name");
        }

        [Test]
        public void AllCreaturesHaveDescriptions()
        {
            foreach (var c in CreatureRosterData.AllCreatures)
                Assert.That(c.Description, Is.Not.Empty, $"'{c.Name}' has no description");
        }

        [Test]
        public void AllCreaturesHaveFavoriteToy()
        {
            foreach (var c in CreatureRosterData.AllCreatures)
                Assert.That(c.FavoriteToy, Is.Not.Empty, $"'{c.Name}' has no favorite toy");
        }

        [Test]
        public void AllCreaturesHave4Mutations()
        {
            foreach (var c in CreatureRosterData.AllCreatures)
                Assert.That(c.Mutations.Count, Is.EqualTo(4),
                    $"'{c.Name}' doesn't have 4 mutations");
        }

        [Test]
        public void MutationIndicesAre0To3()
        {
            foreach (var c in CreatureRosterData.AllCreatures)
            {
                var indices = new HashSet<int>(c.Mutations.Select(m => m.Index));
                Assert.That(indices, Is.EqualTo(new HashSet<int> { 0, 1, 2, 3 }),
                    $"'{c.Name}' has wrong mutation indices");
            }
        }

        // ── Magical Creatures ─────────────────────────────────────────────────

        [Test]
        public void AllMagicalCreaturesAreRare()
        {
            foreach (var c in CreatureRosterData.MagicalCreatures)
                Assert.That(c.Rarity, Is.EqualTo(Rarity.Rare),
                    $"'{c.Name}' magical creature is not rare");
        }

        // ── Lookup ────────────────────────────────────────────────────────────

        [Test]
        public void FindByIDReturnsCorrectCreature()
        {
            var result = CreatureRosterData.Find("aquaburst");
            Assert.That(result,             Is.Not.Null);
            Assert.That(result!.Name,       Is.EqualTo("Aquaburst"));
            Assert.That(result.HabitatType, Is.EqualTo(HabitatType.Water));
        }

        [Test]
        public void FindByIDReturnsNullForUnknown()
            => Assert.That(CreatureRosterData.Find("nonexistent_creature"), Is.Null);

        [Test]
        public void CreaturesFilterByType()
        {
            var water = CreatureRosterData.OfType(HabitatType.Water);
            Assert.That(water.All(c => c.HabitatType == HabitatType.Water), Is.True);
        }
    }
}

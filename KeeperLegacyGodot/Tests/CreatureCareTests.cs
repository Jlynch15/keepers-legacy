// Tests/CreatureCareTests.cs
// Port of CreatureCareTests.swift
// Tests feeding, playing, cleaning, and stat decay on CreatureInstance.

using NUnit.Framework;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class CreatureCareTests
    {
        private CreatureInstance MakeCreature(string catalogId = "aquaburst")
            => new CreatureInstance(catalogId);

        // ── Feeding ───────────────────────────────────────────────────────────

        [Test]
        public void FeedIncreasesHunger()
        {
            var c = MakeCreature();
            c.Hunger = 0.2;
            c.Feed(0.4);
            Assert.That(c.Hunger, Is.EqualTo(0.6).Within(0.001));
        }

        [Test]
        public void FeedClampsAtMax()
        {
            var c = MakeCreature();
            c.Hunger = 0.9;
            c.Feed(0.5);
            Assert.That(c.Hunger, Is.EqualTo(1.0));
        }

        [Test]
        public void FeedAlsoBoostsHappiness()
        {
            var c = MakeCreature();
            double before = c.Happiness;
            c.Feed(0.5);
            Assert.That(c.Happiness, Is.GreaterThan(before));
        }

        // ── Playing ───────────────────────────────────────────────────────────

        [Test]
        public void PlayIncreasesPlayfulness()
        {
            var c = MakeCreature();
            c.Playfulness = 0.3;
            c.Play("Ball", isFavoriteToy: false);
            Assert.That(c.Playfulness, Is.GreaterThan(0.3));
        }

        [Test]
        public void FavoriteToyGivesMoreHappiness()
        {
            var c1 = MakeCreature();
            var c2 = MakeCreature();
            c1.Happiness = 0.5;
            c2.Happiness = 0.5;

            c1.Play("Random Toy",   isFavoriteToy: false);
            c2.Play("Favorite Toy", isFavoriteToy: true);

            Assert.That(c2.Happiness, Is.GreaterThan(c1.Happiness));
        }

        [Test]
        public void FavoriteToyDiscovery()
        {
            var c = MakeCreature();
            Assert.That(c.DiscoveredFavoriteToy, Is.False);
            c.Play("Bubble Wand", isFavoriteToy: true);
            Assert.That(c.DiscoveredFavoriteToy, Is.True);
        }

        [Test]
        public void FavoriteToyOnlyDiscoveredOnce()
        {
            var c = MakeCreature();
            var (_, discovered1) = c.Play("Bubble Wand", isFavoriteToy: true);
            var (_, discovered2) = c.Play("Bubble Wand", isFavoriteToy: true);
            Assert.That(discovered1, Is.True);
            Assert.That(discovered2, Is.False);
        }

        // ── Cleaning ──────────────────────────────────────────────────────────

        [Test]
        public void CleanSetsCleanlinessToFull()
        {
            var c = MakeCreature();
            c.Cleanliness = 0.1;
            c.Clean();
            Assert.That(c.Cleanliness, Is.EqualTo(1.0));
        }

        // ── Overall Happiness ─────────────────────────────────────────────────

        [Test]
        public void OverallHappinessIsAverageOfStats()
        {
            var c = MakeCreature();
            c.Hunger = c.Happiness = c.Cleanliness = c.Affection = c.Playfulness = 1.0;
            Assert.That(c.OverallHappiness, Is.EqualTo(1.0).Within(0.001));
        }

        [Test]
        public void SellMultiplierAtNeutral()
        {
            var c = MakeCreature();
            c.Hunger = c.Happiness = c.Cleanliness = c.Affection = c.Playfulness = 0.5;
            Assert.That(c.SellMultiplier, Is.EqualTo(1.5).Within(0.001));
        }

        // ── Stat Decay ────────────────────────────────────────────────────────

        [Test]
        public void DecayReducesStats()
        {
            var c = MakeCreature();
            double before = c.Hunger;
            c.ApplyDecay(hoursPassed: 10);
            Assert.That(c.Hunger, Is.LessThan(before));
        }

        [Test]
        public void DecayNeverGoesBelowZero()
        {
            var c = MakeCreature();
            c.ApplyDecay(hoursPassed: 10000);
            Assert.That(c.Hunger,      Is.GreaterThanOrEqualTo(0.0));
            Assert.That(c.Happiness,   Is.GreaterThanOrEqualTo(0.0));
            Assert.That(c.Cleanliness, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(c.Affection,   Is.GreaterThanOrEqualTo(0.0));
            Assert.That(c.Playfulness, Is.GreaterThanOrEqualTo(0.0));
        }

        [Test]
        public void HungerDecaysFasterThanAffection()
        {
            var c = MakeCreature();
            c.Hunger    = 1.0;
            c.Affection = 1.0;
            c.ApplyDecay(hoursPassed: 5);
            double hungerLost    = 1.0 - c.Hunger;
            double affectionLost = 1.0 - c.Affection;
            Assert.That(hungerLost, Is.GreaterThan(affectionLost));
        }
    }
}

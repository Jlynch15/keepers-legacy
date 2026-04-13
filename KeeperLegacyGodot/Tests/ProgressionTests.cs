// Tests/ProgressionTests.cs
// Port of ProgressionTests.swift

using NUnit.Framework;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class ProgressionTests
    {
        // ── XP Curve ──────────────────────────────────────────────────────────

        [Test]
        public void XPRequiredIsPositive()
        {
            for (int level = 1; level <= 49; level++)
                Assert.That(XPCurve.XpRequired(level), Is.GreaterThan(0),
                    $"Level {level} requires 0 XP — invalid");
        }

        [Test]
        public void EarlyLevelsHaveLowerXPThanLateOnes()
        {
            int early = XPCurve.XpRequired(3);
            int late  = XPCurve.XpRequired(30);
            Assert.That(early, Is.LessThan(late));
        }

        // ── Leveling Up ───────────────────────────────────────────────────────

        [Test]
        public void AddXPTriggersLevelUp()
        {
            var p = new PlayerProgression();
            Assert.That(p.CurrentLevel, Is.EqualTo(1));
            int xpNeeded = XPCurve.XpRequired(1);
            var levels = p.AddXP(xpNeeded);
            Assert.That(levels, Is.EqualTo(new[] { 2 }));
            Assert.That(p.CurrentLevel, Is.EqualTo(2));
        }

        [Test]
        public void XPDoesNotExceedMaxLevel()
        {
            var p = new PlayerProgression();
            p.CurrentLevel = 50;
            p.CurrentXP    = 0;
            p.AddXP(999999);
            Assert.That(p.CurrentLevel, Is.EqualTo(50));
            Assert.That(p.CurrentXP,    Is.EqualTo(0));
        }

        [Test]
        public void XPRemainsAfterLevelUp()
        {
            var p = new PlayerProgression();
            int xpNeeded = XPCurve.XpRequired(1);
            p.AddXP(xpNeeded + 25);
            Assert.That(p.CurrentLevel, Is.EqualTo(2));
            Assert.That(p.CurrentXP,    Is.EqualTo(25));
        }

        // ── Feature Unlocks ───────────────────────────────────────────────────

        [Test]
        public void BasicFeaturesUnlockedAtStart()
        {
            var p = new PlayerProgression();
            Assert.That(p.IsFeatureUnlocked(GameFeature.Shop),    Is.True);
            Assert.That(p.IsFeatureUnlocked(GameFeature.Habitat),  Is.True);
            Assert.That(p.IsFeatureUnlocked(GameFeature.Feeding),  Is.True);
            Assert.That(p.IsFeatureUnlocked(GameFeature.Playing),  Is.True);
        }

        [Test]
        public void BreedingLockedAtStart()
        {
            var p = new PlayerProgression();
            Assert.That(p.IsFeatureUnlocked(GameFeature.Breeding), Is.False);
        }

        [Test]
        public void BreedingUnlocksAtLevel12WithAct1()
        {
            var p = new PlayerProgression();
            p.CurrentLevel = 12;
            p.StoryAct     = 1;
            p.CheckAndUnlockFeatures();
            Assert.That(p.IsFeatureUnlocked(GameFeature.Breeding), Is.True);
        }

        [Test]
        public void BreedingRemainsLockedAtLevel12WithoutStory()
        {
            var p = new PlayerProgression();
            p.CurrentLevel = 12;
            p.StoryAct     = 0;   // Act I not complete
            p.CheckAndUnlockFeatures();
            Assert.That(p.IsFeatureUnlocked(GameFeature.Breeding), Is.False);
        }

        [Test]
        public void MagicalHabitatRequiresAct2()
        {
            var p = new PlayerProgression();
            p.CurrentLevel = 50;
            p.StoryAct     = 1;
            p.CheckAndUnlockFeatures();
            Assert.That(p.IsFeatureUnlocked(GameFeature.MagicalHabitat), Is.False);

            p.AdvanceStoryAct(2);
            Assert.That(p.IsFeatureUnlocked(GameFeature.MagicalHabitat), Is.True);
        }

        // ── Milestones ────────────────────────────────────────────────────────

        [Test] public void MilestoneExistsAtLevel5()
            => Assert.That(MilestoneReward.Milestones.ContainsKey(5), Is.True);

        [Test]
        public void MilestoneLevel10HasStoryEvent()
        {
            Assert.That(MilestoneReward.Milestones.TryGetValue(10, out var m), Is.True);
            Assert.That(m!.StoryEvent, Is.Not.Null);
        }

        [Test]
        public void MilestoneLevel50HasLargeBonus()
        {
            Assert.That(MilestoneReward.Milestones.TryGetValue(50, out var m), Is.True);
            Assert.That(m!.CoinBonus, Is.GreaterThan(1000));
        }
    }
}

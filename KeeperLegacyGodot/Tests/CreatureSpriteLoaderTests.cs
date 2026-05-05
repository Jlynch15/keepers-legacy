// Tests/CreatureSpriteLoaderTests.cs
using NUnit.Framework;
using KeeperLegacy.Data;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class CreatureSpriteLoaderTests
    {
        [Test]
        public void ResolveIdlePath_BuildsCanonicalPathWithBiomeFolder()
        {
            var path = CreatureSpriteLoader.ResolveIdlePath("coralsprite", 0);
            Assert.That(path, Is.EqualTo("res://Sprites/Creatures/water/coralsprite/coralsprite_v1.png"));
        }

        [Test]
        public void ResolveIdlePath_DirtCreatureUsesDirtFolder()
        {
            var path = CreatureSpriteLoader.ResolveIdlePath("bedrock", 0);
            Assert.That(path, Is.EqualTo("res://Sprites/Creatures/dirt/bedrock/bedrock_v1.png"));
        }

        [Test]
        public void ResolveIdlePath_OneIndexedSuffix()
        {
            // mutationIndex 0..3 -> v1..v4 in the filename
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 0), Does.EndWith("bedrock_v1.png"));
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 1), Does.EndWith("bedrock_v2.png"));
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 2), Does.EndWith("bedrock_v3.png"));
            Assert.That(CreatureSpriteLoader.ResolveIdlePath("bedrock", 3), Does.EndWith("bedrock_v4.png"));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnEmptyId()
        {
            Assert.Throws<System.ArgumentException>(
                () => CreatureSpriteLoader.ResolveIdlePath("", 0));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnUnknownCreature()
        {
            Assert.Throws<System.ArgumentException>(
                () => CreatureSpriteLoader.ResolveIdlePath("not_a_real_creature", 0));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnNegativeIndex()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => CreatureSpriteLoader.ResolveIdlePath("coralsprite", -1));
        }

        [Test]
        public void ResolveIdlePath_ThrowsOnIndexAboveThree()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => CreatureSpriteLoader.ResolveIdlePath("coralsprite", 4));
        }

        [Test]
        public void FallbackPath_IsCanonical()
        {
            Assert.That(CreatureSpriteLoader.FallbackPath,
                Is.EqualTo("res://Sprites/Creatures/_fallback.png"));
        }

        [Test]
        public void ResolveIdlePath_CoversEveryCreatureMutation()
        {
            foreach (var c in CreatureRosterData.AllCreatures)
                for (int i = 0; i < c.Mutations.Count; i++)
                    Assert.That(
                        CreatureSpriteLoader.ResolveIdlePath(c.Id, i),
                        Does.StartWith("res://Sprites/Creatures/"),
                        $"Failed for {c.Id} v{i + 1}");
        }
    }
}

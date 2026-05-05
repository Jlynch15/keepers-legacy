// Tests/BiomeThemeTests.cs
using NUnit.Framework;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class BiomeThemeTests
    {
        [Test]
        public void For_AllBiomesRegistered()
        {
            foreach (HabitatType biome in System.Enum.GetValues(typeof(HabitatType)))
            {
                var theme = BiomeThemes.For(biome);
                Assert.That(theme,         Is.Not.Null,             $"Theme missing for {biome}");
                Assert.That(theme!.Biome,  Is.EqualTo(biome));
            }
        }

        [Test]
        public void For_WaterHasDecorationsAndWanderZone()
        {
            var theme = BiomeThemes.For(HabitatType.Water);
            Assert.That(theme,                       Is.Not.Null);
            Assert.That(theme!.Decorations.Length,   Is.GreaterThan(0));
            Assert.That(theme.WanderZone.Size.X,     Is.GreaterThan(0));
            Assert.That(theme.WanderZone.Size.Y,     Is.GreaterThan(0));
        }

        [Test]
        public void For_WaterIncludesParticles()
            => Assert.That(BiomeThemes.For(HabitatType.Water)!.Particles, Is.Not.Null);
    }
}

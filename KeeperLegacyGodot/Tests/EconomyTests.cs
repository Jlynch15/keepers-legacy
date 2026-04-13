// Tests/EconomyTests.cs
// Port of EconomyTests.swift

using NUnit.Framework;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class EconomyTests
    {
        // ── Pricing Table ─────────────────────────────────────────────────────

        [Test] public void CommonCreatureBasePrice()
            => Assert.That(Rarity.Common.BasePrice(), Is.EqualTo(150));

        [Test]
        public void RarityPriceOrdering()
        {
            Assert.That(Rarity.Common.BasePrice(),   Is.LessThan(Rarity.Uncommon.BasePrice()));
            Assert.That(Rarity.Uncommon.BasePrice(), Is.LessThan(Rarity.Rare.BasePrice()));
        }

        [Test]
        public void RaritySellValueOrdering()
        {
            Assert.That(Rarity.Common.BaseSellValue(),   Is.LessThan(Rarity.Uncommon.BaseSellValue()));
            Assert.That(Rarity.Uncommon.BaseSellValue(), Is.LessThan(Rarity.Rare.BaseSellValue()));
        }

        [Test]
        public void SellValueWithNeutralHappiness()
        {
            int value = PricingTable.SellValue(Rarity.Common, happinessMultiplier: 1.0);
            Assert.That(value, Is.EqualTo(Rarity.Common.BaseSellValue()));
        }

        [Test]
        public void SellValueWithMaxHappiness()
        {
            int value = PricingTable.SellValue(Rarity.Common, happinessMultiplier: 2.0);
            Assert.That(value, Is.EqualTo(Rarity.Common.BaseSellValue() * 2));
        }

        [Test]
        public void SellValueClampsAboveMax()
        {
            int capped = PricingTable.SellValue(Rarity.Common, happinessMultiplier: 5.0);
            int atMax  = PricingTable.SellValue(Rarity.Common, happinessMultiplier: 2.0);
            Assert.That(capped, Is.EqualTo(atMax));
        }

        // ── Player Economy ────────────────────────────────────────────────────

        [Test]
        public void EarnCoins()
        {
            var e = new PlayerEconomy(startingCoins: 100);
            e.EarnCoins(50);
            Assert.That(e.Coins,           Is.EqualTo(150));
            Assert.That(e.TotalCoinsEarned, Is.EqualTo(150));
        }

        [Test]
        public void SpendCoinsSuccess()
        {
            var e = new PlayerEconomy(startingCoins: 200);
            bool result = e.SpendCoins(100);
            Assert.That(result,           Is.True);
            Assert.That(e.Coins,          Is.EqualTo(100));
            Assert.That(e.TotalCoinsSpent, Is.EqualTo(100));
        }

        [Test]
        public void SpendCoinsInsufficientFunds()
        {
            var e = new PlayerEconomy(startingCoins: 50);
            bool result = e.SpendCoins(100);
            Assert.That(result,  Is.False);
            Assert.That(e.Coins, Is.EqualTo(50));
        }

        [Test]
        public void CannotGoNegative()
        {
            var e = new PlayerEconomy(startingCoins: 10);
            e.SpendCoins(10);
            bool result = e.SpendCoins(1);
            Assert.That(result,  Is.False);
            Assert.That(e.Coins, Is.EqualTo(0));
        }

        // ── Breeding Costs ────────────────────────────────────────────────────

        [Test]
        public void BreedingCostOrdering()
        {
            int common   = PricingTable.Breeding.Cost(Rarity.Common);
            int uncommon = PricingTable.Breeding.Cost(Rarity.Uncommon);
            int rare     = PricingTable.Breeding.Cost(Rarity.Rare);
            Assert.That(common,   Is.LessThan(uncommon));
            Assert.That(uncommon, Is.LessThan(rare));
        }

        // ── Food Catalog ──────────────────────────────────────────────────────

        [Test] public void FoodCatalogNotEmpty()
            => Assert.That(PricingTable.Food.Catalog, Is.Not.Empty);

        [Test]
        public void FoodHungerRestoreRange()
        {
            foreach (var food in PricingTable.Food.Catalog)
            {
                Assert.That(food.HungerRestored, Is.GreaterThan(0.0),    $"{food.Name} restores nothing");
                Assert.That(food.HungerRestored, Is.LessThanOrEqualTo(1.0), $"{food.Name} restores over 100%");
            }
        }

        [Test]
        public void FoodCostPositive()
        {
            foreach (var food in PricingTable.Food.Catalog)
                Assert.That(food.CoinCost, Is.GreaterThan(0), $"{food.Name} has zero cost");
        }
    }
}

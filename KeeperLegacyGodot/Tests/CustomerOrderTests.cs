// Tests/CustomerOrderTests.cs
// Port of CustomerOrderTests.swift
// Tests reward ranges, order construction, and expiry logic.

using System;
using NUnit.Framework;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class CustomerOrderTests
    {
        // ── Reward Ranges ─────────────────────────────────────────────────────

        [Test]
        public void CommonRewardRangeIsPositive()
        {
            var (min, max) = PricingTable.CustomerOrder.RewardRange(Rarity.Common);
            Assert.That(min, Is.GreaterThan(0));
            Assert.That(max, Is.GreaterThan(min));
        }

        [Test]
        public void RarityRewardOrderingMakesEconomicSense()
        {
            var (cMin, cMax) = PricingTable.CustomerOrder.RewardRange(Rarity.Common);
            var (uMin, uMax) = PricingTable.CustomerOrder.RewardRange(Rarity.Uncommon);
            var (rMin, rMax) = PricingTable.CustomerOrder.RewardRange(Rarity.Rare);

            Assert.That(cMin, Is.LessThan(uMin));
            Assert.That(uMin, Is.LessThan(rMin));
            Assert.That(cMax, Is.LessThan(uMax));
            Assert.That(uMax, Is.LessThan(rMax));
        }

        // ── Order Construction ────────────────────────────────────────────────

        [Test]
        public void OrderInitiallyUnfulfilled()
        {
            var order = new PricingTable.CustomerOrder("aquaburst", Rarity.Common, 0.3, 100);
            Assert.That(order.IsFulfilled, Is.False);
        }

        [Test]
        public void OrderExpiresInFuture()
        {
            var order = new PricingTable.CustomerOrder("aquaburst", Rarity.Common, 0.3, 100,
                expiresInHours: 8.0);
            Assert.That(order.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
        }

        [Test]
        public void OrderIsNotExpiredOnCreation()
        {
            var order = new PricingTable.CustomerOrder("aquaburst", Rarity.Common, 0.3, 100);
            Assert.That(order.IsExpired, Is.False);
        }

        [Test]
        public void ExpiredOrderDetection()
        {
            // expiresInHours = -1 → already expired 1 hour ago
            var order = new PricingTable.CustomerOrder("aquaburst", Rarity.Common, 0.3, 100,
                expiresInHours: -1.0);
            Assert.That(order.IsExpired, Is.True);
        }

        // ── SaveManager Round-Trip (replaces Core Data integration tests) ─────

        [Test]
        public void SaveAndLoadOrderRoundTrip()
        {
            var order = new PricingTable.CustomerOrder("aquaburst", Rarity.Common, 0.3, 150);

            // Simulate flatten → save data → inflate
            var save = new Data.CustomerOrderSave
            {
                Id                        = order.Id.ToString(),
                RequiredCreatureCatalogId = order.RequiredCreatureCatalogId,
                RequiredRarity            = order.RequiredRarity.RawValue(),
                MinHappiness              = order.MinHappiness,
                CoinReward                = order.CoinReward,
                ExpiresAt                 = order.ExpiresAt,
                IsFulfilled               = order.IsFulfilled,
            };

            // Re-inflate
            Guid.TryParse(save.Id, out var guid);
            var rarity = RarityExtensions.FromRawValue(save.RequiredRarity) ?? Rarity.Common;
            var loaded = new PricingTable.CustomerOrder(
                guid, save.RequiredCreatureCatalogId, rarity,
                save.MinHappiness, save.CoinReward, save.ExpiresAt, save.IsFulfilled);

            Assert.That(loaded.Id,                        Is.EqualTo(order.Id));
            Assert.That(loaded.RequiredCreatureCatalogId, Is.EqualTo("aquaburst"));
            Assert.That(loaded.CoinReward,                Is.EqualTo(150));
            Assert.That(loaded.IsFulfilled,               Is.False);
        }

        [Test]
        public void FulfilledOrderStaysFulfilled()
        {
            var order = new PricingTable.CustomerOrder("aquaburst", Rarity.Common, 0.3, 100);
            order.IsFulfilled = true;
            Assert.That(order.IsFulfilled, Is.True);
        }
    }
}

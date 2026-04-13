// Models/Economy.cs
// Port of Economy.swift
// Pure C# — no Godot dependency.

using System;
using System.Collections.Generic;

namespace KeeperLegacy.Models
{
    // ── Player Economy State ──────────────────────────────────────────────────

    public class PlayerEconomy
    {
        public int Coins { get; set; }
        public int Stardust { get; set; }

        // Lifetime stats
        public int TotalCoinsEarned { get; set; }
        public int TotalCoinsSpent { get; set; }
        public int TotalStardustSpent { get; set; }

        public PlayerEconomy(int startingCoins = 500, int startingStardust = 0)
        {
            Coins              = startingCoins;
            Stardust           = startingStardust;
            TotalCoinsEarned   = startingCoins;
            TotalCoinsSpent    = 0;
            TotalStardustSpent = 0;
        }

        // ── Transactions ──────────────────────────────────────────────────────

        public bool CanAffordCoins(int amount)    => Coins    >= amount;
        public bool CanAffordStardust(int amount) => Stardust >= amount;

        /// Spend coins. Returns false if insufficient funds.
        public bool SpendCoins(int amount)
        {
            if (!CanAffordCoins(amount)) return false;
            Coins          -= amount;
            TotalCoinsSpent += amount;
            return true;
        }

        /// Spend stardust. Returns false if insufficient funds.
        public bool SpendStardust(int amount)
        {
            if (!CanAffordStardust(amount)) return false;
            Stardust           -= amount;
            TotalStardustSpent += amount;
            return true;
        }

        public void EarnCoins(int amount)
        {
            Coins            += amount;
            TotalCoinsEarned += amount;
        }

        public void EarnStardust(int amount) => Stardust += amount;
    }

    // ── Pricing Tables ────────────────────────────────────────────────────────

    public static class PricingTable
    {
        // MARK: Food

        public class Food
        {
            public string Id { get; }
            public string Name { get; }
            public int CoinCost { get; }
            public double HungerRestored { get; }   // 0.0–1.0

            public Food(string id, string name, int coinCost, double hungerRestored)
            {
                Id             = id;
                Name           = name;
                CoinCost       = coinCost;
                HungerRestored = hungerRestored;
            }

            public static readonly List<Food> Catalog = new()
            {
                new Food("basic_kibble",  "Basic Kibble",  10, 0.25),
                new Food("hearty_stew",   "Hearty Stew",   20, 0.50),
                new Food("magical_feast", "Magical Feast", 30, 1.00),
            };
        }

        // MARK: Breeding

        public static class Breeding
        {
            /// Coin cost to breed two creatures of matching rarity.
            public static int Cost(Rarity rarity) => rarity switch
            {
                Rarity.Common   => 300,
                Rarity.Uncommon => 800,
                Rarity.Rare     => 2500,
                _               => 300
            };
        }

        // MARK: Customer Orders

        public class CustomerOrder
        {
            public Guid Id { get; }
            public string RequiredCreatureCatalogId { get; }
            public Rarity RequiredRarity { get; }
            public double MinHappiness { get; }     // 0.0–1.0 threshold
            public int CoinReward { get; }
            public DateTime ExpiresAt { get; }
            public bool IsFulfilled { get; set; }

            public CustomerOrder(
                string creatureCatalogId,
                Rarity rarity,
                double minHappiness,
                int coinReward,
                double expiresInHours = 8.0)
            {
                Id                       = Guid.NewGuid();
                RequiredCreatureCatalogId = creatureCatalogId;
                RequiredRarity           = rarity;
                MinHappiness             = minHappiness;
                CoinReward               = coinReward;
                ExpiresAt                = DateTime.UtcNow.AddHours(expiresInHours);
                IsFulfilled              = false;
            }

            // For deserialization
            public CustomerOrder(Guid id, string creatureCatalogId, Rarity rarity,
                double minHappiness, int coinReward, DateTime expiresAt, bool isFulfilled)
            {
                Id                        = id;
                RequiredCreatureCatalogId = creatureCatalogId;
                RequiredRarity            = rarity;
                MinHappiness              = minHappiness;
                CoinReward                = coinReward;
                ExpiresAt                 = expiresAt;
                IsFulfilled               = isFulfilled;
            }

            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

            /// Base reward range by rarity. Actual reward is set at order creation.
            public static (int min, int max) RewardRange(Rarity rarity) => rarity switch
            {
                Rarity.Common   => (50,  200),
                Rarity.Uncommon => (200, 500),
                Rarity.Rare     => (500, 1500),
                _               => (50,  200)
            };
        }

        // MARK: Sell Values

        /// Calculate coins earned for selling a creature.
        public static int SellValue(Rarity rarity, double happinessMultiplier)
        {
            int baseValue     = rarity.BaseSellValue();
            double multiplier = Math.Max(1.0, Math.Min(2.0, happinessMultiplier));
            return (int)(baseValue * multiplier);
        }
    }

    // ── IAP / Stardust Packages ───────────────────────────────────────────────

    public class StardustPackage
    {
        public string Id { get; }
        public int StardustAmount { get; }
        public double UsdPrice { get; }
        public string DisplayPrice { get; }

        public StardustPackage(string id, int stardustAmount, double usdPrice, string displayPrice)
        {
            Id             = id;
            StardustAmount = stardustAmount;
            UsdPrice       = usdPrice;
            DisplayPrice   = displayPrice;
        }

        public static readonly List<StardustPackage> Packages = new()
        {
            new StardustPackage("stardust_500",  500,  4.99,  "$4.99"),
            new StardustPackage("stardust_1000", 1000, 9.99,  "$9.99"),
            new StardustPackage("stardust_2500", 2500, 19.99, "$19.99"),
        };
    }
}

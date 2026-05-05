// Managers/OrderManager.cs
// Autoload singleton — manages customer orders (generation, fulfillment, expiry).
// Orders are time-limited. This manager tracks them but does not handle the
// coin reward; it emits OrderFulfilled(orderId, coinReward) for GameManager to relay.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class OrderManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    /// Active order list changed (added, fulfilled, or expired).
    [Signal] public delegate void OrdersChangedEventHandler();

    /// An order was successfully fulfilled. Carries the order ID and coin reward.
    [Signal] public delegate void OrderFulfilledEventHandler(string orderId, int coinReward);

    /// An order expired before it was fulfilled.
    [Signal] public delegate void OrderExpiredEventHandler(string orderId);

    // ── State ─────────────────────────────────────────────────────────────────

    public List<PricingTable.CustomerOrder> ActiveOrders { get; private set; } = new();
    public int TotalFulfilled { get; private set; } = 0;

    private static readonly Random _rng = new();

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(List<PricingTable.CustomerOrder> activeOrders)
    {
        ActiveOrders = activeOrders;

        // Prune any that were already expired in the save
        PruneExpired();

        // Ensure at least one order is available on load
        if (ActiveOrders.Count == 0)
            GenerateOrders(3);
    }

    // ── Order Generation ──────────────────────────────────────────────────────

    /// Generate up to `count` new orders from the current creature catalog.
    public void GenerateOrders(int count = 3)
    {
        var catalog = CreatureRosterData.AllCreatures
            .Where(c => c.HabitatType != HabitatType.Magical)   // Magical locked until Act 2
            .ToList();

        if (catalog.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var entry  = catalog[_rng.Next(catalog.Count)];
            var (min, max) = PricingTable.CustomerOrder.RewardRange(entry.Rarity);
            int reward = _rng.Next(min, max + 1);
            double minHappy = 0.3 + (_rng.NextDouble() * 0.4);   // 0.3–0.7

            ActiveOrders.Add(new PricingTable.CustomerOrder(
                entry.Id, entry.Rarity, Math.Round(minHappy, 2), reward,
                expiresInHours: 8.0 + _rng.NextDouble() * 16.0   // 8–24 hours
            ));
        }

        EmitSignal(SignalName.OrdersChanged);
    }

    // ── Fulfillment ───────────────────────────────────────────────────────────

    /// Attempt to fulfill an order with the given creature.
    /// Returns true if the order was successfully fulfilled.
    public bool TryFulfill(Guid orderId, CreatureInstance creature)
    {
        var order = ActiveOrders.FirstOrDefault(o => o.Id == orderId);
        if (order == null || order.IsFulfilled || order.IsExpired) return false;

        // Check creature matches requirements
        var entry = CreatureRosterData.Find(creature.CatalogId);
        if (entry == null) return false;
        if (entry.Id != order.RequiredCreatureCatalogId) return false;
        if (entry.Rarity != order.RequiredRarity) return false;
        if (creature.OverallHappiness < order.MinHappiness) return false;

        order.IsFulfilled = true;
        TotalFulfilled++;

        EmitSignal(SignalName.OrderFulfilled, order.Id.ToString(), order.CoinReward);
        EmitSignal(SignalName.OrdersChanged);

        // Auto-generate a replacement
        GenerateOrders(1);

        return true;
    }

    // ── Expiry ────────────────────────────────────────────────────────────────

    /// Remove expired and fulfilled orders. Returns count removed.
    public int PruneExpired()
    {
        var toRemove = ActiveOrders
            .Where(o => o.IsFulfilled || o.IsExpired)
            .ToList();

        foreach (var o in toRemove)
        {
            if (o.IsExpired && !o.IsFulfilled)
                EmitSignal(SignalName.OrderExpired, o.Id.ToString());
        }

        ActiveOrders.RemoveAll(o => o.IsFulfilled || o.IsExpired);

        if (toRemove.Count > 0) EmitSignal(SignalName.OrdersChanged);
        return toRemove.Count;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public List<PricingTable.CustomerOrder> GetActive() =>
        ActiveOrders.Where(o => !o.IsFulfilled && !o.IsExpired).ToList();

    public PricingTable.CustomerOrder? GetOrder(Guid orderId) =>
        ActiveOrders.FirstOrDefault(o => o.Id == orderId);

    /// Returns true if the given creature is reserved by an active customer
    /// order. Used by HabitatManager.ReleaseCreature to prevent releasing a
    /// promised creature. Implementation will be fleshed out when order
    /// reservation lands; for now returns false (no reservations).
    public bool IsCreatureReserved(System.Guid creatureId) => false;
}

// Managers/ShopManager.cs
// Autoload singleton — manages catalog discovery and creature purchases.
// ShopManager does NOT touch coins; it delegates the spend to ProgressionManager
// via a callback so it remains decoupled from economy logic.

using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class ShopManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    /// A new species has been seen for the first time (Monsterpedia entry created).
    [Signal] public delegate void CatalogDiscoveredEventHandler(string catalogId);

    /// A creature was successfully purchased. Carries the new instance ID.
    [Signal] public delegate void CreaturePurchasedEventHandler(string creatureInstanceId);

    // ── State ─────────────────────────────────────────────────────────────────

    public HashSet<string> DiscoveredCatalogIds { get; private set; } = new();

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(HashSet<string> discoveredCatalogIds)
    {
        DiscoveredCatalogIds = discoveredCatalogIds;
    }

    // ── Purchase ──────────────────────────────────────────────────────────────

    /// Attempt to purchase a creature from the shop.
    /// spendCoins: injected callback — returns true if the spend succeeded.
    /// Returns the new CreatureInstance on success, or null on failure.
    public CreatureInstance? PurchaseCreature(
        string catalogId,
        int mutationIndex,
        Func<int, bool> spendCoins)
    {
        var entry = CreatureRosterData.Find(catalogId);
        if (entry == null) return null;

        int price = entry.Rarity.BasePrice();
        if (!spendCoins(price)) return null;

        var creature = new CreatureInstance(catalogId, mutationIndex);

        Discover(catalogId);
        EmitSignal(SignalName.CreaturePurchased, creature.Id.ToString());
        return creature;
    }

    // ── Discovery ─────────────────────────────────────────────────────────────

    public bool IsDiscovered(string catalogId) =>
        DiscoveredCatalogIds.Contains(catalogId);

    /// Mark a species as discovered (called on purchase, encounter, or breeding).
    public void Discover(string catalogId)
    {
        if (DiscoveredCatalogIds.Add(catalogId))   // Returns true if newly added
            EmitSignal(SignalName.CatalogDiscovered, catalogId);
    }

    // ── Catalog Queries ───────────────────────────────────────────────────────

    /// All catalog entries the player has unlocked access to at their current level.
    public List<CreatureCatalogEntry> AvailableCreatures(
        bool magicalUnlocked = false)
    {
        var all = CreatureRosterData.AllCreatures;
        if (magicalUnlocked) return new List<CreatureCatalogEntry>(all);

        return all.FindAll(c => c.HabitatType != HabitatType.Magical);
    }
}

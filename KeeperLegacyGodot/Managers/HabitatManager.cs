// Managers/HabitatManager.cs
// Autoload singleton — owns all habitats and owned creature instances.
// Handles care actions (feed, play, clean), stat decay, placement, and selling.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class HabitatManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    /// Emitted whenever the creature list changes (purchase, sell, breed result placed).
    [Signal] public delegate void CreaturesChangedEventHandler();

    /// Emitted whenever habitat occupancy or decorations change.
    [Signal] public delegate void HabitatsChangedEventHandler();

    /// Emitted after a care action completes. xpSourceRaw is XPSource enum name.
    [Signal] public delegate void CareActionPerformedEventHandler(string creatureId, string xpSourceRaw);

    /// Emitted when a creature is sold. Carries the coin reward for the sale.
    [Signal] public delegate void CreatureSoldEventHandler(int coinReward);

    // ── State ─────────────────────────────────────────────────────────────────

    public List<Habitat>          Habitats  { get; private set; } = new();
    public List<CreatureInstance> Creatures { get; private set; } = new();

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(List<Habitat> habitats, List<CreatureInstance> creatures)
    {
        Habitats  = habitats;
        Creatures = creatures;
    }

    // ── Care Actions ──────────────────────────────────────────────────────────

    public bool Feed(Guid creatureId, string foodId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return false;

        var food = PricingTable.Food.Catalog.FirstOrDefault(f => f.Id == foodId);
        if (food == null) return false;

        creature.Feed(food.HungerRestored);
        EmitSignal(SignalName.CareActionPerformed, creatureId.ToString(), XPSource.Feed.ToString());
        return true;
    }

    public bool Play(Guid creatureId, string toyName, bool isFavoriteToy)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return false;

        creature.Play(toyName, isFavoriteToy);
        EmitSignal(SignalName.CareActionPerformed, creatureId.ToString(), XPSource.Play.ToString());
        return true;
    }

    public bool Clean(Guid creatureId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return false;

        creature.Clean();
        EmitSignal(SignalName.CareActionPerformed, creatureId.ToString(), XPSource.Clean.ToString());
        return true;
    }

    // ── Selling ───────────────────────────────────────────────────────────────

    /// Remove a creature and return its sell value. Returns 0 if not found.
    public int SellCreature(Guid creatureId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return 0;

        // Determine sell value
        var entry    = CreatureRosterData.Find(creature.CatalogId);
        var rarity   = entry?.Rarity ?? Rarity.Common;
        int sellValue = PricingTable.SellValue(rarity, creature.SellMultiplier);

        // Remove from any habitat
        var habitat = FindHabitatFor(creatureId);
        habitat?.RemoveCreature();

        Creatures.Remove(creature);
        EmitSignal(SignalName.CreatureSold, sellValue);
        EmitSignal(SignalName.CreaturesChanged);
        return sellValue;
    }

    // ── Placement ─────────────────────────────────────────────────────────────

    /// Place a creature into a habitat. Removes it from its previous habitat first.
    public bool PlaceInHabitat(Guid creatureId, Guid habitatId)
    {
        var creature = GetCreature(creatureId);
        var habitat  = GetHabitat(habitatId);
        if (creature == null || habitat == null) return false;

        // Validate habitat type matches
        var entry = CreatureRosterData.Find(creature.CatalogId);
        if (entry != null && entry.HabitatType != habitat.Type) return false;

        // Evict from current habitat if any
        FindHabitatFor(creatureId)?.RemoveCreature();

        habitat.PlaceCreature(creatureId);
        EmitSignal(SignalName.HabitatsChanged);
        return true;
    }

    public bool RemoveFromHabitat(Guid creatureId)
    {
        var habitat = FindHabitatFor(creatureId);
        if (habitat == null) return false;
        habitat.RemoveCreature();
        EmitSignal(SignalName.HabitatsChanged);
        return true;
    }

    // ── Adding Creatures (from shop/breeding) ─────────────────────────────────

    public void AddCreature(CreatureInstance creature)
    {
        Creatures.Add(creature);
        EmitSignal(SignalName.CreaturesChanged);
    }

    // ── Stat Decay ────────────────────────────────────────────────────────────

    /// Called by DecayTimer every 30 in-game minutes.
    public void ApplyDecay(double hoursPassed)
    {
        foreach (var c in Creatures)
            c.ApplyDecay(hoursPassed);

        // Only emit if there are creatures to avoid unnecessary UI redraws
        if (Creatures.Count > 0)
            EmitSignal(SignalName.CreaturesChanged);
    }

    // ── Lifecycle Advancement ─────────────────────────────────────────────────

    /// Advance any creatures whose lifecycle timer has elapsed.
    public void TickLifecycles()
    {
        bool anyChanged = false;
        foreach (var c in Creatures)
        {
            if (c.Lifecycle == LifecycleStage.Adult) continue;
            double hoursElapsed = (DateTime.UtcNow - c.LifecycleStartDate).TotalHours;
            if (hoursElapsed >= c.Lifecycle.DurationHours())
            {
                c.Lifecycle          = c.Lifecycle + 1;   // Advance one stage
                c.LifecycleStartDate = DateTime.UtcNow;
                anyChanged = true;
            }
        }
        if (anyChanged) EmitSignal(SignalName.CreaturesChanged);
    }

    // ── Habitat Expansion ─────────────────────────────────────────────────────

    /// Unlock the next habitat slot. Returns the new Habitat or null if not enough coins.
    public Habitat? ExpandHabitats(HabitatType type, int playerLevel,
                                    Func<int, bool> spendCoins)
    {
        int slot    = Habitats.Count + 1;
        int cost    = HabitatExpansionCost.Cost(slot);
        if (!spendCoins(cost)) return null;

        var newHabitat = new Habitat(type, playerLevel);
        Habitats.Add(newHabitat);
        EmitSignal(SignalName.HabitatsChanged);
        return newHabitat;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public CreatureInstance? GetCreature(Guid id) =>
        Creatures.FirstOrDefault(c => c.Id == id);

    public Habitat? GetHabitat(Guid id) =>
        Habitats.FirstOrDefault(h => h.Id == id);

    public Habitat? FindHabitatFor(Guid creatureId) =>
        Habitats.FirstOrDefault(h => h.OccupantId == creatureId);

    public List<CreatureInstance> CreaturesInHabitat(Guid habitatId)
    {
        var habitat = GetHabitat(habitatId);
        if (habitat?.OccupantId == null) return new List<CreatureInstance>();
        var c = GetCreature(habitat.OccupantId.Value);
        return c != null ? new List<CreatureInstance> { c } : new List<CreatureInstance>();
    }
}

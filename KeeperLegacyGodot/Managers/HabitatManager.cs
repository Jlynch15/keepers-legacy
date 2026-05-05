// Managers/HabitatManager.cs
// Autoload singleton -- owns all habitats and player creature instances.
// Pure-C# rule logic lives in HabitatRules; this class wraps it with signal
// emission and autoload lookups (ProgressionManager, OrderManager).

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class HabitatManager : Node
{
    // -- Signals ---------------------------------------------------------------

    [Signal] public delegate void CreaturesChangedEventHandler();
    [Signal] public delegate void HabitatsChangedEventHandler();
    [Signal] public delegate void CareActionPerformedEventHandler(string creatureId, string xpSourceRaw);
    [Signal] public delegate void CreatureSoldEventHandler(int coinReward);

    // -- State -----------------------------------------------------------------

    public List<Habitat>          Habitats  { get; private set; } = new();
    public List<CreatureInstance> Creatures { get; private set; } = new();

    public void Initialize(List<Habitat> habitats, List<CreatureInstance> creatures)
    {
        Habitats  = habitats;
        Creatures = creatures;
    }

    // -- Care Actions ----------------------------------------------------------
    // (Unchanged -- Feed/Play/Clean still operate on a single creature by id.)

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

    // -- Selling ---------------------------------------------------------------

    public int SellCreature(Guid creatureId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return 0;

        var entry     = CreatureRosterData.Find(creature.CatalogId);
        var rarity    = entry?.Rarity ?? Rarity.Common;
        int sellValue = PricingTable.SellValue(rarity, creature.SellMultiplier);

        FindHabitatFor(creatureId)?.RemoveCreature(creatureId);
        Creatures.Remove(creature);

        EmitSignal(SignalName.CreatureSold, sellValue);
        EmitSignal(SignalName.CreaturesChanged);
        return sellValue;
    }

    // -- Placement (multi-occupant) --------------------------------------------

    /// Place a creature into a specific habitat slot. Returns false if the
    /// habitat is full, the creature is already housed there, or the biome
    /// types don't match.
    public bool TryPlaceCreatureInSlot(Guid habitatId, Guid creatureId)
    {
        if (!HabitatRules.TryPlaceCreatureInSlot(Habitats, Creatures, habitatId, creatureId))
            return false;
        EmitSignal(SignalName.HabitatsChanged);
        return true;
    }

    /// Release a creature back to the wild -- removes from its habitat AND
    /// the creature ledger. Returns false when not found OR when an active
    /// customer order has reserved this creature (caller should toast).
    public bool ReleaseCreature(Guid habitatId, Guid creatureId)
    {
        var habitat  = GetHabitat(habitatId);
        var creature = GetCreature(creatureId);
        if (habitat == null || creature == null) return false;

        var orderManager = GetNodeOrNull<OrderManager>("/root/OrderManager");
        if (orderManager?.IsCreatureReserved(creatureId) == true) return false;

        habitat.RemoveCreature(creatureId);
        Creatures.Remove(creature);

        EmitSignal(SignalName.HabitatsChanged);
        EmitSignal(SignalName.CreaturesChanged);
        return true;
    }

    // -- Habitat creation ------------------------------------------------------

    /// Try to add a new habitat for the given biome. Charges coins via
    /// ProgressionManager. Outputs the coins charged on success.
    public bool TryAddHabitat(HabitatType biome, out int coinsCharged)
    {
        coinsCharged = 0;
        int owned = HabitatsOfType(biome).Count;
        int slot  = owned + 1;
        if (slot > HabitatCapacity.MaxHabitatsForBiome(biome)) return false;

        int cost = HabitatCapacity.CoinsForHabitat(biome, slot);
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (cost > 0 && (pm == null || !pm.SpendCoins(cost))) return false;

        Habitats.Add(new Habitat(biome, pm?.CurrentLevel ?? 1));
        coinsCharged = cost;
        EmitSignal(SignalName.HabitatsChanged);
        return true;
    }

    // -- Adding Creatures (from shop/breeding) ---------------------------------

    public void AddCreature(CreatureInstance creature)
    {
        Creatures.Add(creature);
        EmitSignal(SignalName.CreaturesChanged);
    }

    // -- Stat Decay & Lifecycle (unchanged) ------------------------------------

    public void ApplyDecay(double hoursPassed)
    {
        foreach (var c in Creatures)
            c.ApplyDecay(hoursPassed);
        if (Creatures.Count > 0)
            EmitSignal(SignalName.CreaturesChanged);
    }

    public void TickLifecycles()
    {
        bool anyChanged = false;
        foreach (var c in Creatures)
        {
            if (c.Lifecycle == LifecycleStage.Adult) continue;
            double hoursElapsed = (DateTime.UtcNow - c.LifecycleStartDate).TotalHours;
            if (hoursElapsed >= c.Lifecycle.DurationHours())
            {
                c.Lifecycle          = c.Lifecycle + 1;
                c.LifecycleStartDate = DateTime.UtcNow;
                anyChanged = true;
            }
        }
        if (anyChanged) EmitSignal(SignalName.CreaturesChanged);
    }

    // -- Queries ---------------------------------------------------------------

    public CreatureInstance? GetCreature(Guid id) =>
        Creatures.FirstOrDefault(c => c.Id == id);

    public Habitat? GetHabitat(Guid id) =>
        Habitats.FirstOrDefault(h => h.Id == id);

    /// Find which habitat (if any) houses the given creature.
    public Habitat? FindHabitatFor(Guid creatureId) =>
        Habitats.FirstOrDefault(h => h.OccupantIds.Contains(creatureId));

    public IReadOnlyList<Habitat> HabitatsOfType(HabitatType biome) =>
        HabitatRules.HabitatsOfType(Habitats, biome);

    public List<CreatureInstance> CreaturesInHabitat(Guid habitatId)
    {
        var habitat = GetHabitat(habitatId);
        if (habitat == null) return new List<CreatureInstance>();
        return habitat.OccupantIds
                      .Select(GetCreature)
                      .Where(c => c != null)!
                      .ToList()!;
    }

    // -- Unlock state ----------------------------------------------------------

    /// Returns the unlock state for the Nth habitat of a biome (1-indexed).
    public UnlockReason GetUnlockReason(HabitatType biome, int oneIndexedSlot)
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        bool magicalUnlocked   = pm?.IsFeatureUnlocked(GameFeature.MagicalHabitat) ?? false;
        bool expansionUnlocked = pm?.IsFeatureUnlocked(GameFeature.HabitatExpansion) ?? false;
        return HabitatRules.GetUnlockReason(
            Habitats, biome, oneIndexedSlot, magicalUnlocked, expansionUnlocked);
    }
}

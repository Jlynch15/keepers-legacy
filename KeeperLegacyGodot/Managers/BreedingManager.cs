// Managers/BreedingManager.cs
// Autoload singleton — delegates to BreedingEngine for all breeding logic.
// On success, it creates the offspring CreatureInstance and passes it
// to HabitatManager; the coin deduction happens inside BreedingEngine
// via the passed PlayerEconomy reference.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class BreedingManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    /// Breeding succeeded. Carries offspring catalog ID, mutation, and instance ID.
    [Signal] public delegate void BreedSucceededEventHandler(
        string offspringCatalogId, int mutationIndex, string offspringInstanceId);

    /// Breeding failed. Carries human-readable reason.
    [Signal] public delegate void BreedFailedEventHandler(string reason);

    // ── State ─────────────────────────────────────────────────────────────────

    public List<BreedingRecordSave> BreedingRecords { get; private set; } = new();
    public int TotalBreedCount => BreedingRecords.Count;

    private readonly BreedingEngine _engine = new();

    // ── Initialization ────────────────────────────────────────────────────────

    public void Initialize(List<BreedingRecordSave> records)
    {
        BreedingRecords = records;
    }

    // ── Breed ─────────────────────────────────────────────────────────────────

    /// Attempt to breed two creatures. Returns true on success.
    /// The offspring CreatureInstance is automatically added to HabitatManager.
    public bool TryBreed(Guid parentAId, Guid parentBId)
    {
        var habitatMgr = GetNode<HabitatManager>("/root/HabitatManager");
        var progMgr    = GetNode<ProgressionManager>("/root/ProgressionManager");

        var parentA = habitatMgr.GetCreature(parentAId);
        var parentB = habitatMgr.GetCreature(parentBId);

        if (parentA == null || parentB == null)
        {
            EmitSignal(SignalName.BreedFailed, "One or both parents not found.");
            return false;
        }

        var entryA = CreatureRosterData.Find(parentA.CatalogId);
        var entryB = CreatureRosterData.Find(parentB.CatalogId);

        if (entryA == null || entryB == null)
        {
            EmitSignal(SignalName.BreedFailed, "Catalog entry not found.");
            return false;
        }

        var result = _engine.Breed(
            parentA, parentB, entryA, entryB,
            progMgr.Economy, progMgr.Progression
        );

        switch (result)
        {
            case BreedingEngine.Success success:
            {
                // Coin change already applied by BreedingEngine — notify UI
                // (ProgressionManager doesn't know, so we emit CoinsChanged manually)
                EmitSignal(SignalName.BreedSucceeded,
                    success.OffspringCatalogId,
                    success.MutationIndex,
                    success.Record.OffspringId.ToString());

                // Create offspring and add to roster
                var offspring = new CreatureInstance(
                    success.Record.OffspringId,
                    success.OffspringCatalogId,
                    success.MutationIndex,
                    hunger: 0.8, happiness: 0.7, cleanliness: 0.9,
                    affection: 0.5, playfulness: 0.8,
                    lifecycle: LifecycleStage.Egg,
                    lifecycleStartDate: DateTime.UtcNow,
                    discoveredFavoriteToy: false,
                    parentIds: new List<Guid> { parentAId, parentBId },
                    dateAcquired: DateTime.UtcNow,
                    nickname: null
                );
                habitatMgr.AddCreature(offspring);
                GetNode<ShopManager>("/root/ShopManager").Discover(success.OffspringCatalogId);

                // Record for save
                BreedingRecords.Add(new BreedingRecordSave
                {
                    ParentAId   = parentAId.ToString(),
                    ParentBId   = parentBId.ToString(),
                    OffspringId = success.Record.OffspringId.ToString(),
                    Date        = DateTime.UtcNow,
                });

                // Sync coin display — BreedingEngine spent coins directly on Economy
                // without going through ProgressionManager, so we fire the signal manually
                GetNode<ProgressionManager>("/root/ProgressionManager")
                    .EmitSignal(ProgressionManager.SignalName.CoinsChanged,
                        progMgr.Coins);

                return true;
            }

            case BreedingEngine.Failure failure:
                EmitSignal(SignalName.BreedFailed, failure.Reason);
                return false;

            default:
                return false;
        }
    }
}

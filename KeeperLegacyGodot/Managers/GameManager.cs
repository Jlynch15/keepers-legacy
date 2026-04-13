// Managers/GameManager.cs
// Root coordinator autoload. First to initialize — boots all other managers
// by calling their Initialize() methods with inflated domain objects from SaveManager,
// then wires cross-manager signals so managers never reference each other directly.

using Godot;
using System.Collections.Generic;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class GameManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    [Signal] public delegate void GameLoadedEventHandler();
    [Signal] public delegate void GameSavedEventHandler();

    // ── State ─────────────────────────────────────────────────────────────────

    private SaveManager _saveManager = new();
    private SaveData    _lastSaveData = new();   // Keeps BreedingRecords intact on flatten

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        var saveData = _saveManager.Load();
        _lastSaveData = saveData;

        var (economy, progression, story, creatures, habitats, orders, discovered) =
            _saveManager.Inflate(saveData);

        // Initialize all managers in dependency order
        GetProgression().Initialize(economy, progression);
        GetHabitat().Initialize(habitats, creatures);
        GetShop().Initialize(discovered);
        GetOrders().Initialize(orders);
        GetBreeding().Initialize(saveData.BreedingRecords);
        GetStory().Initialize(story, progression);

        // Start the decay tick
        var decayTimer = new DecayTimer();
        AddChild(decayTimer);

        WireSignals();

        // Kick off first story check
        GetStory().CheckTriggers(progression.CurrentLevel);

        EmitSignal(SignalName.GameLoaded);
    }

    // ── Cross-Manager Signal Wiring ───────────────────────────────────────────

    private void WireSignals()
    {
        var prog     = GetProgression();
        var habitat  = GetHabitat();
        var breeding = GetBreeding();
        var orders   = GetOrders();
        var story    = GetStory();

        // Care actions → XP, save
        habitat.CareActionPerformed += (creatureId, xpSourceRaw) =>
        {
            if (System.Enum.TryParse<XPSource>(xpSourceRaw, out var src))
                prog.AddXPFromSource(src);
            CallDeferred(MethodName.SaveGame);
        };

        // Sell → coins, XP, save
        habitat.CreatureSold += (coinReward) =>
        {
            prog.EarnCoins(coinReward);
            prog.AddXPFromSource(XPSource.Sell);
            CallDeferred(MethodName.SaveGame);
        };

        // Level up → story check, feature unlock notifications
        prog.LeveledUp += (newLevel) =>
        {
            story.CheckTriggers(newLevel);
            GetOrders().PruneExpired();
        };

        // Feature unlocked → habitat may unlock new slot
        prog.FeatureUnlocked += (_) => CallDeferred(MethodName.SaveGame);

        // Breed success → new creature to habitat, XP, story
        breeding.BreedSucceeded += (catalogId, mutationIndex, instanceId) =>
        {
            prog.AddXPFromSource(XPSource.BreedSuccess);
            story.CheckTriggers(prog.CurrentLevel,
                breedCount: breeding.TotalBreedCount);
            CallDeferred(MethodName.SaveGame);
        };

        // Order fulfilled → XP, coin reward, story check
        orders.OrderFulfilled += (orderId, coinReward) =>
        {
            prog.EarnCoins(coinReward);
            prog.AddXPFromSource(XPSource.FulfillOrder);
            story.CheckTriggers(prog.CurrentLevel,
                fulfilledOrders: orders.TotalFulfilled);
            CallDeferred(MethodName.SaveGame);
        };

        // Story event pending → pause and surface to UI (UI subscribes directly)
        // Story event completed → update progression act
        story.StoryActAdvanced += (newAct) =>
        {
            prog.Progression.AdvanceStoryAct(newAct);
            CallDeferred(MethodName.SaveGame);
        };
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public void SaveGame()
    {
        var prog    = GetProgression();
        var habitat = GetHabitat();
        var shop    = GetShop();
        var orders  = GetOrders();
        var breed   = GetBreeding();
        var story   = GetStory();

        var saveData = _saveManager.Flatten(
            prog.Economy,
            prog.Progression,
            story.StoryState,
            habitat.Creatures,
            habitat.Habitats,
            orders.ActiveOrders,
            shop.DiscoveredCatalogIds,
            breed.BreedingRecords,
            _lastSaveData
        );

        _saveManager.Save(saveData);
        _lastSaveData = saveData;

        EmitSignal(SignalName.GameSaved);
    }

    public void NewGame()
    {
        _lastSaveData = _saveManager.DeleteAndReset();
        GetTree().ReloadCurrentScene();
    }

    // ── Manager Accessors ─────────────────────────────────────────────────────
    // Typed helpers avoid string duplication throughout the codebase.

    public ProgressionManager GetProgression() =>
        GetNode<ProgressionManager>("/root/ProgressionManager");

    public HabitatManager GetHabitat() =>
        GetNode<HabitatManager>("/root/HabitatManager");

    public ShopManager GetShop() =>
        GetNode<ShopManager>("/root/ShopManager");

    public OrderManager GetOrders() =>
        GetNode<OrderManager>("/root/OrderManager");

    public BreedingManager GetBreeding() =>
        GetNode<BreedingManager>("/root/BreedingManager");

    public StoryManager GetStory() =>
        GetNode<StoryManager>("/root/StoryManager");
}

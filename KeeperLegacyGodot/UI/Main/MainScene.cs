// UI/Main/MainScene.cs
// Central orchestrator scene — holds ContentContainer, Sidebar, transition overlays,
// and the ImmersiveLayer. Manages all screen swapping, crossfade transitions,
// fade-to-black for immersive screens, and signal wiring to autoload managers.

using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeeperLegacy.Models;
using KeeperLegacy.UI.Story;
using KeeperLegacy.UI.Overlays;

public partial class MainScene : Control
{
    // ── Screen path dictionaries ───────────────────────────────────────────────

    private static readonly Dictionary<string, string> ScreenPaths = new()
    {
        ["Home"]     = "res://UI/Habitat/HabitatFloorScreen.tscn",
        ["Shop"]     = "res://UI/Shop/ShopScreen.tscn",
        ["Orders"]   = "res://UI/Orders/OrdersScreen.tscn",
        ["Breed"]    = "res://UI/Breeding/BreedingScreen.tscn",
        ["Pedia"]    = "res://UI/Pedia/PediaScreen.tscn",
        ["Settings"] = "res://UI/Settings/SettingsScreen.tscn",
    };

    private static readonly Dictionary<string, string> SubScreenPaths = new()
    {
        ["HabitatCategory"] = "res://UI/Habitat/HabitatCategoryScreen.tscn",
        ["HabitatDetail"]   = "res://UI/Habitat/HabitatDetailScreen.tscn",
    };

    // ── Constants ─────────────────────────────────────────────────────────────

    private const float CrossfadeDuration  = 0.3f;
    private const float FadeToBlackDuration = 0.25f; // each direction; 0.5s total round-trip

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Control      _contentContainer;
    private Sidebar      _sidebar;          // NOTE: Sidebar is not namespaced
    private ColorRect    _crossfadeOverlay;
    private ColorRect    _blackOverlay;
    private CanvasLayer  _immersiveLayer;
    private Control      _immersiveContent;
    private Control      _currentScreen;
    private string       _currentScreenName = "";
    private readonly Stack<string> _navStack = new();
    private bool         _transitioning;

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        // 1. Self fills the full viewport.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // 2. ContentContainer — fills viewport but leaves 70px on the right for the sidebar.
        _contentContainer = new Control();
        _contentContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentContainer.OffsetRight = -70;
        AddChild(_contentContainer);

        // 3. Sidebar — instantiated from packed scene, anchored to right edge.
        var sidebarScene = GD.Load<PackedScene>("res://UI/Components/Sidebar.tscn");
        _sidebar = sidebarScene.Instantiate<Sidebar>();
        _sidebar.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        _sidebar.OffsetLeft = -70;
        AddChild(_sidebar);
        _sidebar.NavigationRequested += OnNavigationRequested;

        // 4. CrossfadeOverlay — same footprint as ContentContainer, starts transparent.
        _crossfadeOverlay = new ColorRect();
        _crossfadeOverlay.Color = new Color("#2A1E10");
        _crossfadeOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _crossfadeOverlay.OffsetRight = -70;
        _crossfadeOverlay.Modulate = new Color(1, 1, 1, 0);
        _crossfadeOverlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_crossfadeOverlay);

        // 5. BlackOverlay — covers the ENTIRE viewport, starts transparent.
        _blackOverlay = new ColorRect();
        _blackOverlay.Color = Colors.Black;
        _blackOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _blackOverlay.Modulate = new Color(1, 1, 1, 0);
        _blackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_blackOverlay);

        // 6. ImmersiveLayer — CanvasLayer above everything.
        _immersiveLayer = new CanvasLayer();
        _immersiveLayer.Layer = 10;
        AddChild(_immersiveLayer);

        _immersiveContent = new Control();
        _immersiveContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _immersiveContent.Visible = false;
        _immersiveLayer.AddChild(_immersiveContent);

        // 7. Wire manager signals.
        WireManagerSignals();

        // 8. Load initial screen without animation.
        LoadScreen("Home", animate: false);
    }

    // ── Manager signal wiring ─────────────────────────────────────────────────

    private void WireManagerSignals()
    {
        var progressionManager = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (progressionManager != null)
        {
            progressionManager.LeveledUp       += OnLeveledUp;
            progressionManager.FeatureUnlocked += OnFeatureUnlocked;
        }

        var storyManager = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (storyManager != null)
        {
            storyManager.StoryEventPending += OnStoryEventPending;
        }

        // GameManager.GameLoaded is used by sub-systems; MainScene doesn't need it directly.
    }

    // ── Navigation — public API ───────────────────────────────────────────────

    /// <summary>
    /// Navigate to a top-level screen. Clears the sub-navigation stack.
    /// </summary>
    public void NavigateTo(string screenName)
    {
        OnNavigationRequested(screenName);
    }

    /// <summary>
    /// Push current screen onto the stack and navigate to a sub-screen.
    /// </summary>
    public void NavigateToSubScreen(string subScreenName)
    {
        if (_transitioning) return;
        _navStack.Push(_currentScreenName);
        _ = LoadScreenAsync(subScreenName, animate: true);
    }

    /// <summary>
    /// Pop the stack and navigate back to the previous screen.
    /// </summary>
    public void NavigateBack()
    {
        if (_transitioning || _navStack.Count == 0) return;
        var previous = _navStack.Pop();
        _ = LoadScreenAsync(previous, animate: true);
    }

    // ── Navigation — private implementation ──────────────────────────────────

    private void OnNavigationRequested(string screenName)
    {
        if (_transitioning || screenName == _currentScreenName) return;
        _navStack.Clear();
        _ = LoadScreenAsync(screenName, animate: true);
    }

    /// <summary>
    /// Synchronous wrapper — loads a screen immediately with no crossfade.
    /// Used only for the initial "Home" load in _Ready.
    /// </summary>
    private void LoadScreen(string screenName, bool animate)
    {
        if (animate)
        {
            // Kick off async without awaiting — callers that need sync use animate=false only.
            _ = LoadScreenAsync(screenName, animate: true);
            return;
        }

        RemoveCurrentScreen();
        InstantiateScreen(screenName);
    }

    private async Task LoadScreenAsync(string screenName, bool animate)
    {
        if (!ScreenPaths.ContainsKey(screenName) && !SubScreenPaths.ContainsKey(screenName))
        {
            GD.PushWarning($"MainScene: unknown screen name '{screenName}'");
            return;
        }

        _transitioning = true;

        if (animate && _currentScreen != null)
        {
            await CrossfadeOut();
            RemoveCurrentScreen();
            InstantiateScreen(screenName);
            await CrossfadeIn();
        }
        else
        {
            RemoveCurrentScreen();
            InstantiateScreen(screenName);
        }

        _transitioning = false;
    }

    private void InstantiateScreen(string screenName)
    {
        string path = ScreenPaths.TryGetValue(screenName, out var p)
            ? p
            : SubScreenPaths[screenName];

        var packed = GD.Load<PackedScene>(path);
        if (packed == null)
        {
            GD.PushError($"MainScene: could not load scene at '{path}'");
            return;
        }

        var screen = packed.Instantiate<Control>();
        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentContainer.AddChild(screen);

        _currentScreen     = screen;
        _currentScreenName = screenName;

        // Keep sidebar highlight in sync (map sub-screen names back to a top-level key).
        string sidebarKey = screenName switch
        {
            "HabitatCategory" or "HabitatDetail" => "Home",
            _                                     => screenName,
        };
        _sidebar.SetActiveButton(sidebarKey);
    }

    private void RemoveCurrentScreen()
    {
        if (_currentScreen == null) return;
        _currentScreen.GetParent()?.RemoveChild(_currentScreen);
        _currentScreen.QueueFree();
        _currentScreen = null;
    }

    // ── Crossfade transitions ─────────────────────────────────────────────────

    private async Task CrossfadeOut()
    {
        _crossfadeOverlay.MouseFilter = MouseFilterEnum.Stop;
        var tween = CreateTween();
        tween.TweenProperty(_crossfadeOverlay, "modulate:a", 1.0f, CrossfadeDuration / 2.0f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task CrossfadeIn()
    {
        var tween = CreateTween();
        tween.TweenProperty(_crossfadeOverlay, "modulate:a", 0.0f, CrossfadeDuration / 2.0f);
        await ToSignal(tween, Tween.SignalName.Finished);
        _crossfadeOverlay.MouseFilter = MouseFilterEnum.Ignore;
    }

    // ── Immersive screen transitions ──────────────────────────────────────────

    private async Task ShowImmersiveScreen(Control screen)
    {
        _transitioning = true;

        // Fade to black.
        _blackOverlay.MouseFilter = MouseFilterEnum.Stop;
        var tweenIn = CreateTween();
        tweenIn.TweenProperty(_blackOverlay, "modulate:a", 1.0f, FadeToBlackDuration);
        await ToSignal(tweenIn, Tween.SignalName.Finished);

        // Swap in the immersive content behind the black.
        _sidebar.Hide();
        ClearImmersiveChildren();
        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _immersiveContent.AddChild(screen);
        _immersiveContent.Visible = true;

        // Fade from black.
        var tweenOut = CreateTween();
        tweenOut.TweenProperty(_blackOverlay, "modulate:a", 0.0f, FadeToBlackDuration);
        await ToSignal(tweenOut, Tween.SignalName.Finished);

        _blackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        _transitioning = false;
    }

    private async Task DismissImmersiveScreen()
    {
        _transitioning = true;

        // Fade to black.
        _blackOverlay.MouseFilter = MouseFilterEnum.Stop;
        var tweenIn = CreateTween();
        tweenIn.TweenProperty(_blackOverlay, "modulate:a", 1.0f, FadeToBlackDuration);
        await ToSignal(tweenIn, Tween.SignalName.Finished);

        // Swap back to the regular view.
        ClearImmersiveChildren();
        _immersiveContent.Visible = false;
        _sidebar.Show();

        // Fade from black.
        var tweenOut = CreateTween();
        tweenOut.TweenProperty(_blackOverlay, "modulate:a", 0.0f, FadeToBlackDuration);
        await ToSignal(tweenOut, Tween.SignalName.Finished);

        _blackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        _transitioning = false;
    }

    private void ClearImmersiveChildren()
    {
        foreach (Node child in _immersiveContent.GetChildren())
        {
            _immersiveContent.RemoveChild(child);
            child.QueueFree();
        }
    }

    // ── Manager signal handlers ───────────────────────────────────────────────

    private void OnStoryEventPending(string eventId)
    {
        var packed = GD.Load<PackedScene>("res://UI/Story/StoryEventScreen.tscn");
        if (packed == null)
        {
            GD.PushError("MainScene: could not load StoryEventScreen.tscn");
            return;
        }

        var screen = packed.Instantiate<StoryEventScreen>();
        screen.Dismissed += () =>
        {
            var storyManager = GetNodeOrNull<StoryManager>("/root/StoryManager");
            storyManager?.CompleteCurrentEvent();
            _ = DismissImmersiveScreen();
        };

        _ = ShowImmersiveScreen(screen);
    }

    private void OnLeveledUp(int newLevel)
    {
        var packed = GD.Load<PackedScene>("res://UI/Overlays/LevelUpScreen.tscn");
        if (packed == null)
        {
            GD.PushError("MainScene: could not load LevelUpScreen.tscn");
            return;
        }

        var screen = packed.Instantiate<LevelUpScreen>();
        screen.SetLevel(newLevel);
        screen.Dismissed += () => _ = DismissImmersiveScreen();

        _ = ShowImmersiveScreen(screen);
    }

    private void OnFeatureUnlocked(string featureRaw)
    {
        _sidebar.RefreshLockStates();
    }

    private void DebugUnlockAllFeatures()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm?.Progression == null) return;

        foreach (var feature in System.Enum.GetValues<KeeperLegacy.Models.GameFeature>())
        {
            if (!pm.IsFeatureUnlocked(feature))
            {
                pm.Progression.UnlockedFeatures.Add(feature.RawValue());
                pm.EmitSignal(ProgressionManager.SignalName.FeatureUnlocked, feature.RawValue());
            }
        }
        _sidebar.RefreshLockStates();
        GD.Print("DEBUG: All features unlocked");
    }

    private void DebugTogglePedestalDrag()
    {
        KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled =
            !KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled;

        bool enabled = KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled;
        GD.Print($"DEBUG: Pedestal drag mode {(enabled ? "ON — drag pedestals, press F4 again to print positions and exit" : "OFF")}");

        if (!enabled)
        {
            // Print final positions when turning off drag mode
            DebugPrintPedestalPositions();
        }
    }

    private static readonly float[] PedestalSizes = { 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 1.15f, 1.3f, 1.5f, 1.75f, 2.0f, 2.5f, 3.0f };
    private int _pedestalSizeIndex = 6; // Start at 1.0

    private void DebugCyclePedestalSize()
    {
        _pedestalSizeIndex = (_pedestalSizeIndex + 1) % PedestalSizes.Length;
        KeeperLegacy.UI.Habitat.PedestalNode.DebugScale = PedestalSizes[_pedestalSizeIndex];
        float scale = KeeperLegacy.UI.Habitat.PedestalNode.DebugScale;
        GD.Print($"DEBUG: Pedestal scale = {scale:F2} (base 55px -> {55f * scale:F0}px wide)");
        ApplyDebugVisualsToAll();
    }

    private void DebugNudgeArtOffset(Vector2 delta)
    {
        KeeperLegacy.UI.Habitat.PedestalNode.DebugArtOffset += delta;
        var offset = KeeperLegacy.UI.Habitat.PedestalNode.DebugArtOffset;
        GD.Print($"DEBUG: Art offset = ({offset.X:F0}, {offset.Y:F0})");
        ApplyDebugVisualsToAll();
    }

    private void ApplyDebugVisualsToAll()
    {
        if (_currentScreen == null) return;
        foreach (var child in _currentScreen.GetChildren())
        {
            if (child is KeeperLegacy.UI.Habitat.PedestalNode ped)
                ped.ApplyDebugVisuals();
        }
    }

    private void DebugPrintPedestalPositions()
    {
        if (_currentScreen == null)
        {
            GD.Print("DEBUG: No current screen found");
            return;
        }

        float scale = KeeperLegacy.UI.Habitat.PedestalNode.DebugScale;
        var artOffset = KeeperLegacy.UI.Habitat.PedestalNode.DebugArtOffset;
        GD.Print($"── Pedestal scale: {scale:F2} (base 55px -> {55f * scale:F0}px wide) ──");
        GD.Print($"── Art offset: ({artOffset.X:F0}, {artOffset.Y:F0}) ──");
        GD.Print("── Pedestal positions (paste into PedestalDefs) ──");

        int found = 0;
        foreach (var child in _currentScreen.GetChildren())
        {
            if (child is KeeperLegacy.UI.Habitat.PedestalNode pedestal)
            {
                found++;
                var center = pedestal.GetCenter();
                float scaleX = 1364f / (_currentScreen.Size.X > 0 ? _currentScreen.Size.X : 1364f);
                float scaleY = 768f / (_currentScreen.Size.Y > 0 ? _currentScreen.Size.Y : 768f);
                var artPos = new Vector2(center.X * scaleX, center.Y * scaleY);
                GD.Print($"  (HabitatType.{pedestal.GetHabitatType(),-10} new Vector2({artPos.X,7:F0}, {artPos.Y,5:F0})),");
            }
        }

        if (found == 0)
            GD.Print("  (no pedestals found in current screen children)");

        GD.Print("── end ──");
    }

    // ── Debug helpers ─────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            bool handled = true;
            switch (keyEvent.Keycode)
            {
                case Key.F1:
                    OnStoryEventPending("test_event");
                    break;
                case Key.F2:
                    OnLeveledUp(5);
                    break;
                case Key.F3:
                    DebugUnlockAllFeatures();
                    break;
                case Key.F4:
                case Key.P:
                    DebugTogglePedestalDrag();
                    break;
                case Key.O:
                    if (KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled)
                        DebugCyclePedestalSize();
                    else handled = false;
                    break;
                case Key.Up:
                    if (KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled)
                        DebugNudgeArtOffset(new Vector2(0, -3));
                    else handled = false;
                    break;
                case Key.Down:
                    if (KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled)
                        DebugNudgeArtOffset(new Vector2(0, 3));
                    else handled = false;
                    break;
                case Key.Left:
                    if (KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled)
                        DebugNudgeArtOffset(new Vector2(-3, 0));
                    else handled = false;
                    break;
                case Key.Right:
                    if (KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled)
                        DebugNudgeArtOffset(new Vector2(3, 0));
                    else handled = false;
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled)
                GetViewport().SetInputAsHandled();
        }
    }
}

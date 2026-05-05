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

    // Debug overlay — appears top-left when any debug-tuning mode is active.
    // Lives above content, below immersive overlays.
    private PanelContainer _debugOverlay;
    private Label          _debugOverlayLabel;

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

        // 7. Debug overlay (always present, hidden until a debug mode activates).
        BuildDebugOverlay();

        // 8. Wire manager signals.
        WireManagerSignals();

        // 9. Load initial screen without animation.
        LoadScreen("Home", animate: false);
    }

    private void BuildDebugOverlay()
    {
        _debugOverlay = new PanelContainer();
        _debugOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        _debugOverlay.OffsetLeft   = 12;
        _debugOverlay.OffsetTop    = 100; // below the store sign
        _debugOverlay.Visible      = false;
        _debugOverlay.MouseFilter  = MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat();
        style.BgColor     = new Color(0f, 0f, 0f, 0.78f);
        style.BorderColor = new Color("#FF4040");
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(4);
        style.ContentMarginLeft   = 10;
        style.ContentMarginRight  = 10;
        style.ContentMarginTop    = 6;
        style.ContentMarginBottom = 6;
        _debugOverlay.AddThemeStyleboxOverride("panel", style);

        _debugOverlayLabel = new Label();
        _debugOverlayLabel.AddThemeFontSizeOverride("font_size", 11);
        _debugOverlayLabel.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
        _debugOverlayLabel.MouseFilter = MouseFilterEnum.Ignore;
        _debugOverlay.AddChild(_debugOverlayLabel);

        AddChild(_debugOverlay);
    }

    private void UpdateDebugOverlay()
    {
        if (_debugOverlay == null) return;

        bool dragOn = KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled;
        if (!dragOn)
        {
            _debugOverlay.Visible = false;
            return;
        }

        float scale     = KeeperLegacy.UI.Habitat.PedestalNode.DebugScale;
        var   artOffset = KeeperLegacy.UI.Habitat.PedestalNode.DebugArtOffset;

        _debugOverlayLabel.Text =
            "DEBUG — Pedestal Drag\n" +
            $"  Scale:      {scale:F2}x  ({220f * scale:F0}px wide)\n" +
            $"  Art offset: ({artOffset.X:F0}, {artOffset.Y:F0})\n" +
            "  -----------------------------------\n" +
            "  Drag        : reposition pedestals\n" +
            "  O           : cycle scale\n" +
            "  Arrow keys  : nudge art offset\n" +
            "  Ctrl+S      : print bake values\n" +
            "  F4 / P      : print + exit debug";
        _debugOverlay.Visible = true;
    }

    /// <summary>
    /// Public hook so screen-owned debug systems can drive the shared overlay
    /// without duplicating the panel construction. Pass null/empty to hide.
    /// </summary>
    public void SetDebugOverlayText(string? text)
    {
        if (_debugOverlay == null || _debugOverlayLabel == null) return;
        if (string.IsNullOrEmpty(text))
        {
            _debugOverlay.Visible = false;
            return;
        }
        _debugOverlayLabel.Text = text;
        _debugOverlay.Visible = true;
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
        GD.Print(enabled
            ? "DEBUG: Drag mode ON. Drag pedestals, press O to cycle scale, arrows to nudge offset, Ctrl+S to print bake values, F4/P to print + exit."
            : "DEBUG: Drag mode OFF.");

        if (!enabled)
        {
            // Print final positions when turning off drag mode
            DebugPrintPedestalPositions();
        }

        UpdateDebugOverlay();
    }

    private static readonly float[] PedestalSizes = { 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 1.15f, 1.3f, 1.5f, 1.75f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f };
    private int _pedestalSizeIndex = 6; // Start at 1.0

    private void DebugCyclePedestalSize()
    {
        _pedestalSizeIndex = (_pedestalSizeIndex + 1) % PedestalSizes.Length;
        KeeperLegacy.UI.Habitat.PedestalNode.DebugScale = PedestalSizes[_pedestalSizeIndex];
        float scale = KeeperLegacy.UI.Habitat.PedestalNode.DebugScale;
        // ArtWidth is the baked (scale-1.0) display width — multiplier rides on top.
        const float bakedArtWidth = 220f;
        GD.Print($"DEBUG: Pedestal scale = {scale:F2}x  -> {bakedArtWidth * scale:F0}px wide");
        ApplyDebugVisualsToAll();
        UpdateDebugOverlay();
    }

    private void DebugNudgeArtOffset(Vector2 delta)
    {
        KeeperLegacy.UI.Habitat.PedestalNode.DebugArtOffset += delta;
        var offset = KeeperLegacy.UI.Habitat.PedestalNode.DebugArtOffset;
        GD.Print($"DEBUG: Art offset = ({offset.X:F0}, {offset.Y:F0})");
        ApplyDebugVisualsToAll();
        UpdateDebugOverlay();
    }

    private void ApplyDebugVisualsToAll()
    {
        if (_currentScreen == null) return;
        foreach (var child in _currentScreen.GetChildren())
        {
            if (child is KeeperLegacy.UI.Habitat.PedestalNode ped)
                ped.ApplyDebugLayout();
        }
    }

    private void DebugPrintPedestalPositions()
    {
        // Delegate to the screen — only HabitatFloorScreen knows its own art-space
        // mapping (sidebar offset, letterbox math). Calling its own print routine
        // is the only way to produce paste-ready bake values that match the live
        // visual exactly.
        if (_currentScreen is KeeperLegacy.UI.Habitat.HabitatFloorScreen floor)
        {
            floor.PrintBakeValues();
        }
        else
        {
            GD.Print("DEBUG: bake-print only supported on HabitatFloorScreen for now.");
        }
    }

    // ── Debug helpers ─────────────────────────────────────────────────────────

    // _Input rather than _UnhandledInput: letter keys (P, S) get consumed by
    // focused Controls before _UnhandledInput fires, so the previous version of
    // this handler caught F-keys but missed P / Ctrl+S. _Input fires for every
    // input regardless of focus.
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // F1/F2/F3 are screen-agnostic — story event, level up, feature unlock —
            // and stay handled at MainScene level always.
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
                default:
                    handled = false;
                    break;
            }

            // Pedestal-specific debug keys (F4/P/O/arrows/Ctrl+S) only fire when the
            // floor screen is the active sub-screen. Other screens (e.g. Habitat
            // Category) own their own debug input via their own _Input override.
            // Without this guard, pressing F4 on the category screen would toggle
            // PedestalNode.DebugDragEnabled in the background and corrupt the shared
            // debug overlay state.
            if (!handled && _currentScreen is KeeperLegacy.UI.Habitat.HabitatFloorScreen)
            {
                handled = true;
                switch (keyEvent.Keycode)
                {
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
                    case Key.S:
                        // Ctrl+S in drag mode: print bake values WITHOUT exiting drag mode
                        if (KeeperLegacy.UI.Habitat.PedestalNode.DebugDragEnabled && keyEvent.CtrlPressed)
                            DebugPrintPedestalPositions();
                        else handled = false;
                        break;
                    default:
                        handled = false;
                        break;
                }
            }

            if (handled)
                GetViewport().SetInputAsHandled();
        }
    }
}

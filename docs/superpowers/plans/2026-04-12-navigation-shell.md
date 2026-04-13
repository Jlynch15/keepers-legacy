# Navigation Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Godot navigation shell — persistent sidebar, screen container with crossfade transitions, and immersive overlay layer — so every nav button works with placeholder screens.

**Architecture:** Single `MainScene.tscn` holds four layers: Sidebar (70px right), ContentContainer (swappable screens), TransitionOverlay (crossfade + fade-to-black), ImmersiveLayer (full-screen takeover). Each game screen is an independent `.tscn` file loaded into the container. Existing autoload managers (GameManager, ProgressionManager, StoryManager) provide signals for feature locks and immersive triggers.

**Tech Stack:** Godot 4.2+ (.NET), C# / .NET 6.0, existing manager singletons

**Spec:** `docs/superpowers/specs/2026-04-12-navigation-shell-design.md`

**Existing code reference:**
- Managers in `Managers/` — all 7 autoloads registered in `project.godot`
- `ProgressionManager.IsFeatureUnlocked(GameFeature)` — checks if a feature is unlocked
- `ProgressionManager.LeveledUp` signal — emits `int newLevel`
- `StoryManager.StoryEventPending` signal — emits `string eventId`
- `StoryManager.CompleteCurrentEvent()` — marks story event done
- `GameManager.GameLoaded` signal — emits when save data is inflated
- `GameFeature` enum in `Models/Progression.cs` — includes `Breeding`, `Monsterpedia`, `CustomerOrders`, etc.

---

## File Structure

```
KeeperLegacyGodot/
├── UI/
│   ├── Main/
│   │   ├── MainScene.tscn          (root scene: HBoxContainer with content + sidebar)
│   │   └── MainScene.cs            (screen management, transitions, immersive flow)
│   ├── Components/
│   │   ├── Sidebar.tscn            (VBoxContainer with nav buttons)
│   │   ├── Sidebar.cs              (button clicks, active state, lock icons)
│   │   ├── OverlayBar.tscn         (HBoxContainer: back button + title + coins)
│   │   └── OverlayBar.cs           (back navigation, title/coin display)
│   ├── Screens/
│   │   └── PlaceholderScreen.cs    (base class for all placeholder screens)
│   ├── Habitat/
│   │   ├── HabitatFloorScreen.tscn (placeholder with label)
│   │   ├── HabitatCategoryScreen.tscn
│   │   └── HabitatDetailScreen.tscn
│   ├── Shop/
│   │   └── ShopScreen.tscn
│   ├── Orders/
│   │   └── OrdersScreen.tscn
│   ├── Breeding/
│   │   └── BreedingScreen.tscn
│   ├── Pedia/
│   │   └── PediaScreen.tscn
│   ├── Settings/
│   │   ├── SettingsScreen.tscn
│   │   └── SettingsScreen.cs       (resolution picker logic)
│   ├── Story/
│   │   ├── StoryEventScreen.tscn
│   │   └── StoryEventScreen.cs     (dismiss + completion signal)
│   └── Overlays/
│       ├── LevelUpScreen.tscn
│       └── LevelUpScreen.cs        (dismiss + completion signal)
└── Resources/
    └── Fonts/                      (Cinzel, CrimsonText, IMFellEnglish .ttf files)
```

---

## Task 0: Project Configuration Fix

**Files:**
- Modify: `KeeperLegacyGodot/project.godot`

The spec requires stretch aspect `keep` (maintains aspect ratio with black bars) but the project currently has `expand`. Fix this so UI scales correctly.

- [ ] **Step 1: Update project.godot display settings**

In `project.godot`, change line 34:

```ini
window/stretch/aspect="keep"
```

Also add fullscreen toggle support (needed for resolution picker):

```ini
window/size/borderless=false
```

- [ ] **Step 2: Verify Godot opens the project**

Open Godot 4.2+ (.NET edition). Import the project at `KeeperLegacyGodot/`. Confirm it opens without errors. (MainScene.tscn doesn't exist yet — that's expected.)

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/project.godot
git commit -m "fix: set stretch aspect to keep for consistent UI scaling"
```

---

## Task 1: Download Fonts

**Files:**
- Create: `KeeperLegacyGodot/Resources/Fonts/Cinzel-Bold.ttf`
- Create: `KeeperLegacyGodot/Resources/Fonts/CrimsonText-Regular.ttf`
- Create: `KeeperLegacyGodot/Resources/Fonts/IMFellEnglish-Regular.ttf`

The mockups use three Google Fonts. Download them so Godot scenes can reference them.

- [ ] **Step 1: Download fonts from Google Fonts**

```bash
cd "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/Resources/Fonts"
curl -L -o Cinzel-Bold.ttf "https://github.com/google/fonts/raw/main/ofl/cinzel/Cinzel%5Bwght%5D.ttf"
curl -L -o CrimsonText-Regular.ttf "https://github.com/google/fonts/raw/main/ofl/crimsontext/CrimsonText-Regular.ttf"
curl -L -o IMFellEnglish-Regular.ttf "https://github.com/google/fonts/raw/main/ofl/imfellenglish/IMFellEnglish-Regular.ttf"
```

If the exact URLs fail, download manually from https://fonts.google.com/ — search for Cinzel (Bold), Crimson Text (Regular), and IM Fell English (Regular). Place .ttf files in `Resources/Fonts/`.

- [ ] **Step 2: Verify files exist**

```bash
ls -la "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/Resources/Fonts/"
```

Expected: Three .ttf files, each several KB.

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/Resources/Fonts/
git commit -m "feat: add Cinzel, CrimsonText, and IMFellEnglish fonts"
```

---

## Task 2: Sidebar Component

**Files:**
- Create: `KeeperLegacyGodot/UI/Components/Sidebar.tscn`
- Create: `KeeperLegacyGodot/UI/Components/Sidebar.cs`

Build the 70px right sidebar with nav buttons. Each button is an icon + label stacked vertically. Active button gets gold highlight. Locked buttons show a lock icon and are non-clickable.

- [ ] **Step 1: Create Sidebar.cs**

```csharp
using Godot;
using System;

namespace KeeperLegacy.UI.Components;

public partial class Sidebar : PanelContainer
{
    [Signal] public delegate void NavigationRequestedEventHandler(string screenName);

    private string _activeScreen = "Home";

    // Color constants from mockups
    private static readonly Color BgColor = new("#1A1208");
    private static readonly Color GoldColor = new("#E8B830");
    private static readonly Color MutedColor = new("#9A8070");
    private static readonly Color ActiveBgColor = new("E8B83026"); // 15% opacity gold

    // Nav button definitions: (name, icon emoji, GameFeature or null if always unlocked)
    private static readonly (string Name, string Icon, string Feature)[] NavButtons = new[]
    {
        ("Home",     "⌂", ""),
        ("Shop",     "🛒", "Shop"),
        ("Orders",   "📋", "CustomerOrders"),
        ("Breed",    "🥚", "Breeding"),
        ("Pedia",    "📖", "Monsterpedia"),
    };

    private const string SettingsName = "Settings";
    private const string SettingsIcon = "⚙";

    private VBoxContainer _buttonContainer;
    private Button _settingsButton;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(70, 0);

        // Style the panel background
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = BgColor;
        styleBox.BorderWidthLeft = 1;
        styleBox.BorderColor = new Color("#3A2818");
        AddThemeStyleboxOverride("panel", styleBox);

        // Main vertical layout
        var vbox = new VBoxContainer();
        vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(vbox);

        // Top button group
        _buttonContainer = new VBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(_buttonContainer);

        foreach (var (name, icon, feature) in NavButtons)
        {
            var btn = CreateNavButton(name, icon, feature);
            _buttonContainer.AddChild(btn);
        }

        // Spacer pushes settings to bottom
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(spacer);

        // Settings button at bottom
        _settingsButton = CreateNavButton(SettingsName, SettingsIcon, "");
        vbox.AddChild(_settingsButton);

        UpdateActiveState();
    }

    private Button CreateNavButton(string name, string icon, string feature)
    {
        var btn = new Button();
        btn.Name = name + "Button";
        btn.CustomMinimumSize = new Vector2(70, 56);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Flat style — no default button chrome
        btn.Flat = true;
        btn.FocusMode = FocusModeEnum.None;

        // Check lock state
        bool locked = IsFeatureLocked(feature);

        // Button text: icon + newline + label (or lock icon if locked)
        if (locked)
        {
            btn.Text = "🔒\n" + name;
            btn.Disabled = true;
        }
        else
        {
            btn.Text = icon + "\n" + name;
        }

        btn.SetMeta("screen_name", name);
        btn.SetMeta("feature", feature);

        // Style overrides
        btn.AddThemeFontSizeOverride("font_size", 10);
        btn.AddThemeColorOverride("font_color", MutedColor);
        btn.AddThemeColorOverride("font_hover_color", GoldColor);
        btn.AddThemeColorOverride("font_disabled_color", new Color("#5A4A3A"));

        btn.Pressed += () => OnButtonPressed(name);

        return btn;
    }

    private void OnButtonPressed(string screenName)
    {
        if (screenName == _activeScreen) return;
        _activeScreen = screenName;
        UpdateActiveState();
        EmitSignal(SignalName.NavigationRequested, screenName);
    }

    private void UpdateActiveState()
    {
        // Update top nav buttons
        foreach (var child in _buttonContainer.GetChildren())
        {
            if (child is Button btn)
            {
                string name = btn.GetMeta("screen_name").AsString();
                bool active = name == _activeScreen;
                btn.AddThemeColorOverride("font_color", active ? GoldColor : MutedColor);

                // Active background highlight
                if (active)
                {
                    var activeStyle = new StyleBoxFlat();
                    activeStyle.BgColor = ActiveBgColor;
                    activeStyle.CornerRadiusTopLeft = 6;
                    activeStyle.CornerRadiusTopRight = 6;
                    activeStyle.CornerRadiusBottomLeft = 6;
                    activeStyle.CornerRadiusBottomRight = 6;
                    btn.AddThemeStyleboxOverride("normal", activeStyle);
                }
                else
                {
                    btn.RemoveThemeStyleboxOverride("normal");
                }
            }
        }

        // Update settings button
        bool settingsActive = _activeScreen == SettingsName;
        _settingsButton.AddThemeColorOverride("font_color", settingsActive ? GoldColor : MutedColor);
        if (settingsActive)
        {
            var activeStyle = new StyleBoxFlat();
            activeStyle.BgColor = ActiveBgColor;
            activeStyle.CornerRadiusTopLeft = 6;
            activeStyle.CornerRadiusTopRight = 6;
            activeStyle.CornerRadiusBottomLeft = 6;
            activeStyle.CornerRadiusBottomRight = 6;
            _settingsButton.AddThemeStyleboxOverride("normal", activeStyle);
        }
        else
        {
            _settingsButton.RemoveThemeStyleboxOverride("normal");
        }
    }

    private bool IsFeatureLocked(string featureRaw)
    {
        if (string.IsNullOrEmpty(featureRaw)) return false;

        var progressionMgr = GetNodeOrNull<Node>("/root/ProgressionManager");
        if (progressionMgr == null) return false;

        // Parse feature string to enum
        if (Enum.TryParse<GameFeature>(featureRaw, out var feature))
        {
            return !((ProgressionManager)progressionMgr).IsFeatureUnlocked(feature);
        }
        return false;
    }

    /// <summary>
    /// Called by MainScene to set active button without triggering navigation.
    /// Used when sub-navigation (habitat detail) keeps Home highlighted.
    /// </summary>
    public void SetActiveButton(string screenName)
    {
        _activeScreen = screenName;
        UpdateActiveState();
    }

    /// <summary>
    /// Re-checks lock state on all buttons. Call after FeatureUnlocked signal.
    /// </summary>
    public void RefreshLockStates()
    {
        foreach (var child in _buttonContainer.GetChildren())
        {
            if (child is Button btn)
            {
                string feature = btn.GetMeta("feature").AsString();
                string name = btn.GetMeta("screen_name").AsString();
                bool locked = IsFeatureLocked(feature);

                if (locked)
                {
                    btn.Text = "🔒\n" + name;
                    btn.Disabled = true;
                }
                else
                {
                    // Restore original icon
                    string icon = "";
                    foreach (var (n, ic, _) in NavButtons)
                    {
                        if (n == name) { icon = ic; break; }
                    }
                    btn.Text = icon + "\n" + name;
                    btn.Disabled = false;
                }
            }
        }
    }
}
```

- [ ] **Step 2: Create Sidebar.tscn**

Create the scene file. In Godot, create a new scene with root node `PanelContainer`, attach `Sidebar.cs` script, save as `UI/Components/Sidebar.tscn`.

Alternatively, create the `.tscn` file directly:

```
[gd_scene load_steps=2 format=3 uid="uid://sidebar001"]

[ext_resource type="Script" path="res://UI/Components/Sidebar.cs" id="1"]

[node name="Sidebar" type="PanelContainer"]
custom_minimum_size = Vector2(70, 0)
anchors_preset = 3
anchor_left = 1.0
anchor_top = 0.0
anchor_right = 1.0
anchor_bottom = 1.0
offset_left = -70.0
grow_horizontal = 0
size_flags_vertical = 3
script = ExtResource("1")
```

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/UI/Components/Sidebar.cs KeeperLegacyGodot/UI/Components/Sidebar.tscn
git commit -m "feat: add Sidebar component with nav buttons and lock states"
```

---

## Task 3: Placeholder Screen Base + All Placeholder Screens

**Files:**
- Create: `KeeperLegacyGodot/UI/Screens/PlaceholderScreen.cs`
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatFloorScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatDetailScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Shop/ShopScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Orders/OrdersScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Breeding/BreedingScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Pedia/PediaScreen.tscn`

Each placeholder screen is a colored panel with the screen name displayed. This lets us test navigation before building real content.

- [ ] **Step 1: Create PlaceholderScreen.cs**

```csharp
using Godot;

namespace KeeperLegacy.UI.Screens;

/// <summary>
/// Base class for placeholder screens. Displays screen name on a colored background.
/// Replace with real screen implementations later.
/// </summary>
public partial class PlaceholderScreen : Control
{
    [Export] public string ScreenTitle { get; set; } = "Screen";
    [Export] public Color BackgroundColor { get; set; } = new("#2A1E10");

    public override void _Ready()
    {
        // Fill parent container
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Background
        var bg = new ColorRect();
        bg.Color = BackgroundColor;
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Title label centered
        var label = new Label();
        label.Text = ScreenTitle;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        label.AddThemeFontSizeOverride("font_size", 32);
        label.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
        AddChild(label);

        // Subtitle with navigation hint
        var subtitle = new Label();
        subtitle.Text = "(Placeholder — real content coming soon)";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.VerticalAlignment = VerticalAlignment.Center;
        subtitle.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        subtitle.OffsetTop = 50;
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.AddThemeColorOverride("font_color", new Color("#9A8070"));
        AddChild(subtitle);
    }
}
```

- [ ] **Step 2: Create all placeholder .tscn files**

Each screen is a minimal scene with PlaceholderScreen as the script and exported properties set.

**HabitatFloorScreen.tscn** (`UI/Habitat/`):
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Screens/PlaceholderScreen.cs" id="1"]

[node name="HabitatFloorScreen" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
ScreenTitle = "Habitat Floor"
BackgroundColor = Color(0.165, 0.118, 0.063, 1)
```

**HabitatCategoryScreen.tscn** (`UI/Habitat/`):
Same structure, `ScreenTitle = "Habitat Category"`, `BackgroundColor = Color(0.165, 0.118, 0.063, 1)`

**HabitatDetailScreen.tscn** (`UI/Habitat/`):
Same structure, `ScreenTitle = "Habitat Detail"`, `BackgroundColor = Color(0.165, 0.118, 0.063, 1)`

**ShopScreen.tscn** (`UI/Shop/`):
Same structure, `ScreenTitle = "Shop"`, `BackgroundColor = Color(0.165, 0.118, 0.063, 1)`

**OrdersScreen.tscn** (`UI/Orders/`):
Same structure, `ScreenTitle = "Orders"`, `BackgroundColor = Color(0.165, 0.118, 0.063, 1)`

**BreedingScreen.tscn** (`UI/Breeding/`):
Same structure, `ScreenTitle = "Breeding"`, `BackgroundColor = Color(0.165, 0.118, 0.063, 1)`

**PediaScreen.tscn** (`UI/Pedia/`):
Same structure, `ScreenTitle = "Pedia"`, `BackgroundColor = Color(0.165, 0.118, 0.063, 1)`

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/UI/Screens/ KeeperLegacyGodot/UI/Habitat/ KeeperLegacyGodot/UI/Shop/ KeeperLegacyGodot/UI/Orders/ KeeperLegacyGodot/UI/Breeding/ KeeperLegacyGodot/UI/Pedia/
git commit -m "feat: add PlaceholderScreen base and all 7 placeholder screen scenes"
```

---

## Task 4: Settings Screen with Resolution Picker

**Files:**
- Create: `KeeperLegacyGodot/UI/Settings/SettingsScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Settings/SettingsScreen.cs`

A real (non-placeholder) settings screen with a resolution dropdown. This is the only screen with actual functionality in the shell phase.

- [ ] **Step 1: Create SettingsScreen.cs**

```csharp
using Godot;

namespace KeeperLegacy.UI.Settings;

public partial class SettingsScreen : Control
{
    private static readonly (string Label, Vector2I Size)[] Resolutions = new[]
    {
        ("1280 x 720 (720p)", new Vector2I(1280, 720)),
        ("1920 x 1080 (1080p)", new Vector2I(1920, 1080)),
        ("2560 x 1440 (1440p)", new Vector2I(2560, 1440)),
    };

    private OptionButton _resolutionDropdown;

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Background
        var bg = new ColorRect();
        bg.Color = new Color("#2A1E10");
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Centered container
        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        vbox.OffsetLeft = -200;
        vbox.OffsetRight = 200;
        vbox.OffsetTop = -150;
        vbox.OffsetBottom = 150;
        vbox.AddThemeConstantOverride("separation", 20);
        AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "Settings";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
        vbox.AddChild(title);

        // Resolution section
        var resLabel = new Label();
        resLabel.Text = "Resolution";
        resLabel.AddThemeFontSizeOverride("font_size", 16);
        resLabel.AddThemeColorOverride("font_color", new Color("#B8A080"));
        vbox.AddChild(resLabel);

        _resolutionDropdown = new OptionButton();
        _resolutionDropdown.CustomMinimumSize = new Vector2(300, 40);
        for (int i = 0; i < Resolutions.Length; i++)
        {
            _resolutionDropdown.AddItem(Resolutions[i].Label, i);
        }

        // Select current resolution
        var currentSize = DisplayServer.WindowGetSize();
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Resolutions[i].Size == currentSize)
            {
                _resolutionDropdown.Selected = i;
                break;
            }
        }

        _resolutionDropdown.ItemSelected += OnResolutionSelected;
        vbox.AddChild(_resolutionDropdown);

        // Fullscreen toggle
        var fullscreenCheck = new CheckButton();
        fullscreenCheck.Text = "Fullscreen";
        fullscreenCheck.AddThemeColorOverride("font_color", new Color("#B8A080"));
        fullscreenCheck.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
        fullscreenCheck.Toggled += OnFullscreenToggled;
        vbox.AddChild(fullscreenCheck);
    }

    private void OnResolutionSelected(long index)
    {
        var size = Resolutions[(int)index].Size;
        DisplayServer.WindowSetSize(size);
        // Center window on screen
        var screenSize = DisplayServer.ScreenGetSize();
        var pos = (screenSize - size) / 2;
        DisplayServer.WindowSetPosition(pos);
    }

    private void OnFullscreenToggled(bool toggled)
    {
        DisplayServer.WindowSetMode(
            toggled ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
        );
    }
}
```

- [ ] **Step 2: Create SettingsScreen.tscn**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Settings/SettingsScreen.cs" id="1"]

[node name="SettingsScreen" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/UI/Settings/
git commit -m "feat: add SettingsScreen with resolution picker and fullscreen toggle"
```

---

## Task 5: Immersive Screens (Story Event + Level Up)

**Files:**
- Create: `KeeperLegacyGodot/UI/Story/StoryEventScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Story/StoryEventScreen.cs`
- Create: `KeeperLegacyGodot/UI/Overlays/LevelUpScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Overlays/LevelUpScreen.cs`

Immersive screens take over the full viewport (no sidebar). They display placeholder content and emit a `Dismissed` signal when the player clicks to continue.

- [ ] **Step 1: Create StoryEventScreen.cs**

```csharp
using Godot;

namespace KeeperLegacy.UI.Story;

public partial class StoryEventScreen : Control
{
    [Signal] public delegate void DismissedEventHandler();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Dark mystical background
        var bg = new ColorRect();
        bg.Color = new Color("#0A0510");
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Centered card
        var card = new PanelContainer();
        card.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        card.OffsetLeft = -250;
        card.OffsetRight = 250;
        card.OffsetTop = -150;
        card.OffsetBottom = 150;
        var cardStyle = new StyleBoxFlat();
        cardStyle.BgColor = new Color("#1A1230");
        cardStyle.BorderWidthTop = 1;
        cardStyle.BorderWidthBottom = 1;
        cardStyle.BorderWidthLeft = 1;
        cardStyle.BorderWidthRight = 1;
        cardStyle.BorderColor = new Color("#3A2050");
        cardStyle.CornerRadiusTopLeft = 12;
        cardStyle.CornerRadiusTopRight = 12;
        cardStyle.CornerRadiusBottomLeft = 12;
        cardStyle.CornerRadiusBottomRight = 12;
        card.AddThemeStyleboxOverride("panel", cardStyle);
        AddChild(card);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        card.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "Story Event";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
        vbox.AddChild(title);

        // Placeholder text
        var body = new Label();
        body.Text = "A mysterious story unfolds...\n(Placeholder — real dialogue coming soon)";
        body.HorizontalAlignment = HorizontalAlignment.Center;
        body.AddThemeFontSizeOverride("font_size", 16);
        body.AddThemeColorOverride("font_color", new Color("#B8A080"));
        vbox.AddChild(body);

        // Continue button
        var continueBtn = new Button();
        continueBtn.Text = "Continue";
        continueBtn.CustomMinimumSize = new Vector2(150, 40);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.Pressed += () => EmitSignal(SignalName.Dismissed);
        vbox.AddChild(continueBtn);

        // Skip button top-right
        var skipBtn = new Button();
        skipBtn.Text = "Skip";
        skipBtn.Flat = true;
        skipBtn.Position = new Vector2(GetViewportRect().Size.X - 80, 20);
        skipBtn.AddThemeColorOverride("font_color", new Color("#5A4A3A"));
        skipBtn.AddThemeColorOverride("font_hover_color", new Color("#9A8070"));
        skipBtn.Pressed += () => EmitSignal(SignalName.Dismissed);
        AddChild(skipBtn);
    }
}
```

- [ ] **Step 2: Create StoryEventScreen.tscn**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Story/StoryEventScreen.cs" id="1"]

[node name="StoryEventScreen" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

- [ ] **Step 3: Create LevelUpScreen.cs**

```csharp
using Godot;

namespace KeeperLegacy.UI.Overlays;

public partial class LevelUpScreen : Control
{
    [Signal] public delegate void DismissedEventHandler();

    private int _newLevel = 1;

    public void SetLevel(int level)
    {
        _newLevel = level;
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Gold-tinted background
        var bg = new ColorRect();
        bg.Color = new Color("#1A1208");
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Centered card
        var card = new PanelContainer();
        card.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        card.OffsetLeft = -200;
        card.OffsetRight = 200;
        card.OffsetTop = -120;
        card.OffsetBottom = 120;
        var cardStyle = new StyleBoxFlat();
        cardStyle.BgColor = new Color("#2A1E10");
        cardStyle.BorderWidthTop = 2;
        cardStyle.BorderWidthBottom = 2;
        cardStyle.BorderWidthLeft = 2;
        cardStyle.BorderWidthRight = 2;
        cardStyle.BorderColor = new Color("#E8B830");
        cardStyle.CornerRadiusTopLeft = 12;
        cardStyle.CornerRadiusTopRight = 12;
        cardStyle.CornerRadiusBottomLeft = 12;
        cardStyle.CornerRadiusBottomRight = 12;
        card.AddThemeStyleboxOverride("panel", cardStyle);
        AddChild(card);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        card.AddChild(vbox);

        // Level up header
        var header = new Label();
        header.Text = "Level Up!";
        header.HorizontalAlignment = HorizontalAlignment.Center;
        header.AddThemeFontSizeOverride("font_size", 28);
        header.AddThemeColorOverride("font_color", new Color("#E8B830"));
        vbox.AddChild(header);

        // Level number
        var levelLabel = new Label();
        levelLabel.Text = $"Level {_newLevel}";
        levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        levelLabel.AddThemeFontSizeOverride("font_size", 48);
        levelLabel.AddThemeColorOverride("font_color", new Color("#F0CC50"));
        vbox.AddChild(levelLabel);

        // Placeholder rewards
        var rewards = new Label();
        rewards.Text = "(Rewards will be shown here)";
        rewards.HorizontalAlignment = HorizontalAlignment.Center;
        rewards.AddThemeFontSizeOverride("font_size", 14);
        rewards.AddThemeColorOverride("font_color", new Color("#9A8070"));
        vbox.AddChild(rewards);

        // Continue button
        var continueBtn = new Button();
        continueBtn.Text = "Continue";
        continueBtn.CustomMinimumSize = new Vector2(150, 40);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.Pressed += () => EmitSignal(SignalName.Dismissed);
        vbox.AddChild(continueBtn);
    }
}
```

- [ ] **Step 4: Create LevelUpScreen.tscn**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Overlays/LevelUpScreen.cs" id="1"]

[node name="LevelUpScreen" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

- [ ] **Step 5: Commit**

```bash
git add KeeperLegacyGodot/UI/Story/ KeeperLegacyGodot/UI/Overlays/
git commit -m "feat: add StoryEventScreen and LevelUpScreen immersive placeholders"
```

---

## Task 6: MainScene — Screen Management and Transitions

**Files:**
- Create: `KeeperLegacyGodot/UI/Main/MainScene.tscn`
- Create: `KeeperLegacyGodot/UI/Main/MainScene.cs`

The central orchestrator. Holds the sidebar, content container, transition overlay, and immersive layer. Manages all screen swapping, crossfade transitions, fade-to-black for immersive screens, and signal wiring to managers.

- [ ] **Step 1: Create MainScene.cs**

```csharp
using Godot;
using System.Collections.Generic;

namespace KeeperLegacy.UI.Main;

public partial class MainScene : Control
{
    // Screen scene paths
    private static readonly Dictionary<string, string> ScreenPaths = new()
    {
        ["Home"]     = "res://UI/Habitat/HabitatFloorScreen.tscn",
        ["Shop"]     = "res://UI/Shop/ShopScreen.tscn",
        ["Orders"]   = "res://UI/Orders/OrdersScreen.tscn",
        ["Breed"]    = "res://UI/Breeding/BreedingScreen.tscn",
        ["Pedia"]    = "res://UI/Pedia/PediaScreen.tscn",
        ["Settings"] = "res://UI/Settings/SettingsScreen.tscn",
    };

    // Sub-navigation paths (for habitat drill-down)
    private static readonly Dictionary<string, string> SubScreenPaths = new()
    {
        ["HabitatCategory"] = "res://UI/Habitat/HabitatCategoryScreen.tscn",
        ["HabitatDetail"]   = "res://UI/Habitat/HabitatDetailScreen.tscn",
    };

    private const float CrossfadeDuration = 0.3f;
    private const float FadeToBlackDuration = 0.25f; // Each direction, total 0.5s

    // Scene tree nodes
    private Control _contentContainer;
    private Components.Sidebar _sidebar;
    private ColorRect _crossfadeOverlay;
    private ColorRect _blackOverlay;
    private CanvasLayer _immersiveLayer;
    private Control _immersiveContent;

    // State
    private Control _currentScreen;
    private string _currentScreenName = "";
    private readonly Stack<string> _navStack = new();
    private bool _transitioning;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // --- Content Container (fills viewport minus sidebar) ---
        _contentContainer = new Control();
        _contentContainer.Name = "ContentContainer";
        _contentContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentContainer.OffsetRight = -70; // Leave room for sidebar
        AddChild(_contentContainer);

        // --- Sidebar ---
        var sidebarScene = GD.Load<PackedScene>("res://UI/Components/Sidebar.tscn");
        _sidebar = sidebarScene.Instantiate<Components.Sidebar>();
        _sidebar.Name = "Sidebar";
        _sidebar.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        _sidebar.OffsetLeft = -70;
        AddChild(_sidebar);
        _sidebar.NavigationRequested += OnNavigationRequested;

        // --- Crossfade Overlay (covers content area only) ---
        _crossfadeOverlay = new ColorRect();
        _crossfadeOverlay.Name = "CrossfadeOverlay";
        _crossfadeOverlay.Color = new Color("#2A1E10");
        _crossfadeOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _crossfadeOverlay.OffsetRight = -70;
        _crossfadeOverlay.Modulate = new Color(1, 1, 1, 0); // Start transparent
        _crossfadeOverlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_crossfadeOverlay);

        // --- Black Overlay (covers entire viewport for immersive transitions) ---
        _blackOverlay = new ColorRect();
        _blackOverlay.Name = "BlackOverlay";
        _blackOverlay.Color = Colors.Black;
        _blackOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _blackOverlay.Modulate = new Color(1, 1, 1, 0);
        _blackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_blackOverlay);

        // --- Immersive Layer (above everything) ---
        _immersiveLayer = new CanvasLayer();
        _immersiveLayer.Name = "ImmersiveLayer";
        _immersiveLayer.Layer = 10;
        AddChild(_immersiveLayer);

        _immersiveContent = new Control();
        _immersiveContent.Name = "ImmersiveContent";
        _immersiveContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _immersiveContent.Visible = false;
        _immersiveLayer.AddChild(_immersiveContent);

        // --- Wire manager signals ---
        WireManagerSignals();

        // --- Load initial screen ---
        LoadScreen("Home", false);
    }

    private void WireManagerSignals()
    {
        // Story event triggers
        var storyMgr = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (storyMgr != null)
        {
            storyMgr.StoryEventPending += OnStoryEventPending;
        }

        // Level-up triggers
        var progressionMgr = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (progressionMgr != null)
        {
            progressionMgr.LeveledUp += OnLeveledUp;
            progressionMgr.FeatureUnlocked += OnFeatureUnlocked;
        }
    }

    // ========== MAIN NAVIGATION ==========

    private void OnNavigationRequested(string screenName)
    {
        if (_transitioning || screenName == _currentScreenName) return;
        _navStack.Clear(); // Top-level nav resets the sub-nav stack
        LoadScreen(screenName, true);
    }

    private async void LoadScreen(string screenName, bool animate)
    {
        if (!ScreenPaths.ContainsKey(screenName) && !SubScreenPaths.ContainsKey(screenName))
        {
            GD.PrintErr($"MainScene: Unknown screen '{screenName}'");
            return;
        }

        string path = ScreenPaths.ContainsKey(screenName)
            ? ScreenPaths[screenName]
            : SubScreenPaths[screenName];

        if (animate && _currentScreen != null)
        {
            _transitioning = true;
            await CrossfadeOut();
            RemoveCurrentScreen();
            InstantiateScreen(path, screenName);
            await CrossfadeIn();
            _transitioning = false;
        }
        else
        {
            RemoveCurrentScreen();
            InstantiateScreen(path, screenName);
        }
    }

    private void InstantiateScreen(string scenePath, string screenName)
    {
        var scene = GD.Load<PackedScene>(scenePath);
        _currentScreen = scene.Instantiate<Control>();
        _currentScreen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentContainer.AddChild(_currentScreen);
        _currentScreenName = screenName;
    }

    private void RemoveCurrentScreen()
    {
        if (_currentScreen == null) return;
        _contentContainer.RemoveChild(_currentScreen);
        _currentScreen.QueueFree();
        _currentScreen = null;
    }

    // ========== SUB-NAVIGATION (Habitat drill-down) ==========

    /// <summary>
    /// Navigate deeper into a sub-screen (e.g. Habitat Floor → Category).
    /// Pushes current screen onto stack so Back works.
    /// </summary>
    public void NavigateToSubScreen(string subScreenName)
    {
        if (_transitioning) return;
        _navStack.Push(_currentScreenName);
        LoadScreen(subScreenName, true);
    }

    /// <summary>
    /// Go back one level in the sub-navigation stack.
    /// </summary>
    public void NavigateBack()
    {
        if (_transitioning || _navStack.Count == 0) return;
        string previous = _navStack.Pop();
        LoadScreen(previous, true);
    }

    // ========== CROSSFADE TRANSITIONS ==========

    private async System.Threading.Tasks.Task CrossfadeOut()
    {
        _crossfadeOverlay.MouseFilter = MouseFilterEnum.Stop; // Block input during transition
        var tween = CreateTween();
        tween.TweenProperty(_crossfadeOverlay, "modulate:a", 1.0f, CrossfadeDuration / 2);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async System.Threading.Tasks.Task CrossfadeIn()
    {
        var tween = CreateTween();
        tween.TweenProperty(_crossfadeOverlay, "modulate:a", 0.0f, CrossfadeDuration / 2);
        await ToSignal(tween, Tween.SignalName.Finished);
        _crossfadeOverlay.MouseFilter = MouseFilterEnum.Ignore;
    }

    // ========== IMMERSIVE TRANSITIONS (fade through black) ==========

    private async void ShowImmersiveScreen(Control screen)
    {
        if (_transitioning) return;
        _transitioning = true;

        // Fade to black (covers everything including sidebar)
        _blackOverlay.MouseFilter = MouseFilterEnum.Stop;
        var fadeOut = CreateTween();
        fadeOut.TweenProperty(_blackOverlay, "modulate:a", 1.0f, FadeToBlackDuration);
        await ToSignal(fadeOut, Tween.SignalName.Finished);

        // Hide sidebar, show immersive content
        _sidebar.Visible = false;
        _immersiveContent.Visible = true;

        // Clear any previous immersive content
        foreach (var child in _immersiveContent.GetChildren())
        {
            child.QueueFree();
        }

        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _immersiveContent.AddChild(screen);

        // Fade from black
        var fadeIn = CreateTween();
        fadeIn.TweenProperty(_blackOverlay, "modulate:a", 0.0f, FadeToBlackDuration);
        await ToSignal(fadeIn, Tween.SignalName.Finished);

        _blackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        _transitioning = false;
    }

    private async void DismissImmersiveScreen()
    {
        if (_transitioning) return;
        _transitioning = true;

        // Fade to black
        _blackOverlay.MouseFilter = MouseFilterEnum.Stop;
        var fadeOut = CreateTween();
        fadeOut.TweenProperty(_blackOverlay, "modulate:a", 1.0f, FadeToBlackDuration);
        await ToSignal(fadeOut, Tween.SignalName.Finished);

        // Remove immersive content, restore sidebar
        foreach (var child in _immersiveContent.GetChildren())
        {
            child.QueueFree();
        }
        _immersiveContent.Visible = false;
        _sidebar.Visible = true;

        // Fade from black
        var fadeIn = CreateTween();
        fadeIn.TweenProperty(_blackOverlay, "modulate:a", 0.0f, FadeToBlackDuration);
        await ToSignal(fadeIn, Tween.SignalName.Finished);

        _blackOverlay.MouseFilter = MouseFilterEnum.Ignore;
        _transitioning = false;
    }

    // ========== MANAGER SIGNAL HANDLERS ==========

    private void OnStoryEventPending(string eventId)
    {
        var scene = GD.Load<PackedScene>("res://UI/Story/StoryEventScreen.tscn");
        var screen = scene.Instantiate<Story.StoryEventScreen>();
        screen.Dismissed += () =>
        {
            // Tell StoryManager the event was completed
            var storyMgr = GetNodeOrNull<StoryManager>("/root/StoryManager");
            storyMgr?.CompleteCurrentEvent();
            DismissImmersiveScreen();
        };
        ShowImmersiveScreen(screen);
    }

    private void OnLeveledUp(int newLevel)
    {
        var scene = GD.Load<PackedScene>("res://UI/Overlays/LevelUpScreen.tscn");
        var screen = scene.Instantiate<Overlays.LevelUpScreen>();
        screen.SetLevel(newLevel);
        screen.Dismissed += () => DismissImmersiveScreen();
        ShowImmersiveScreen(screen);
    }

    private void OnFeatureUnlocked(string featureRaw)
    {
        // Refresh sidebar lock icons when a feature unlocks
        _sidebar.RefreshLockStates();
    }

    // ========== DEBUG HELPERS (remove later) ==========

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed)
        {
            // F1 = trigger test story event
            if (key.Keycode == Key.F1)
            {
                OnStoryEventPending("test_event");
            }
            // F2 = trigger test level up
            if (key.Keycode == Key.F2)
            {
                OnLeveledUp(5);
            }
        }
    }
}
```

- [ ] **Step 2: Create MainScene.tscn**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Main/MainScene.cs" id="1"]

[node name="MainScene" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

- [ ] **Step 3: Run the project**

Open Godot 4.2+ (.NET), open the project, press F5.

**Expected behavior:**
- Habitat Floor placeholder appears with sidebar on the right
- Click sidebar buttons → crossfade transitions between placeholder screens
- Breed button shows lock icon and is disabled
- Press F1 → fade to black → story event placeholder → click Continue → fade back
- Press F2 → fade to black → level up placeholder → click Continue → fade back
- Click Settings → resolution dropdown and fullscreen toggle work

- [ ] **Step 4: Commit**

```bash
git add KeeperLegacyGodot/UI/Main/
git commit -m "feat: add MainScene with navigation shell, transitions, and immersive overlay"
```

---

## Task 7: OverlayBar Component

**Files:**
- Create: `KeeperLegacyGodot/UI/Components/OverlayBar.tscn`
- Create: `KeeperLegacyGodot/UI/Components/OverlayBar.cs`

The translucent top bar used on sub-screens (shop, orders, habitat category/detail). Shows back button, screen title, and coin count.

- [ ] **Step 1: Create OverlayBar.cs**

```csharp
using Godot;

namespace KeeperLegacy.UI.Components;

public partial class OverlayBar : PanelContainer
{
    [Signal] public delegate void BackPressedEventHandler();

    [Export] public string Title { get; set; } = "";
    [Export] public bool ShowBackButton { get; set; } = true;
    [Export] public bool ShowCoins { get; set; } = true;

    private Label _coinLabel;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 44);

        // Translucent background
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.047f, 0.035f, 0.027f, 0.8f); // rgba(12,9,7,0.80)
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        AddChild(hbox);

        // Back button
        if (ShowBackButton)
        {
            var backBtn = new Button();
            backBtn.Text = "◀ Back";
            backBtn.Flat = true;
            backBtn.AddThemeColorOverride("font_color", new Color("#B8A080"));
            backBtn.AddThemeColorOverride("font_hover_color", new Color("#E8B830"));
            backBtn.AddThemeFontSizeOverride("font_size", 14);
            backBtn.Pressed += () => EmitSignal(SignalName.BackPressed);
            hbox.AddChild(backBtn);

            // Separator
            var sep = new VSeparator();
            sep.AddThemeConstantOverride("separation", 4);
            hbox.AddChild(sep);
        }

        // Title
        var titleLabel = new Label();
        titleLabel.Text = Title;
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleLabel.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
        hbox.AddChild(titleLabel);

        // Coin display
        if (ShowCoins)
        {
            _coinLabel = new Label();
            _coinLabel.HorizontalAlignment = HorizontalAlignment.Right;
            _coinLabel.AddThemeFontSizeOverride("font_size", 14);
            _coinLabel.AddThemeColorOverride("font_color", new Color("#E8B830"));
            hbox.AddChild(_coinLabel);
            UpdateCoinDisplay();

            // Subscribe to coin changes
            var progressionMgr = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            if (progressionMgr != null)
            {
                progressionMgr.CoinsChanged += (newAmount) => UpdateCoinDisplay();
            }
        }
    }

    private void UpdateCoinDisplay()
    {
        var progressionMgr = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (progressionMgr != null && _coinLabel != null)
        {
            _coinLabel.Text = $"✦ {progressionMgr.Coins}";
        }
        else if (_coinLabel != null)
        {
            _coinLabel.Text = "✦ ---";
        }
    }
}
```

- [ ] **Step 2: Create OverlayBar.tscn**

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Components/OverlayBar.cs" id="1"]

[node name="OverlayBar" type="PanelContainer"]
custom_minimum_size = Vector2(0, 44)
anchors_preset = 10
anchor_right = 1.0
grow_vertical = 1
script = ExtResource("1")
```

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/UI/Components/OverlayBar.cs KeeperLegacyGodot/UI/Components/OverlayBar.tscn
git commit -m "feat: add OverlayBar component with back button, title, and coin display"
```

---

## Task 8: Integration Test — Full Navigation Flow

No new files. This task is a manual verification walkthrough to confirm everything works together.

- [ ] **Step 1: Launch and verify home screen**

Open Godot, press F5. Confirm:
- MainScene loads with sidebar on right (70px, dark background #1A1208)
- Habitat Floor placeholder fills the left content area
- Home button is highlighted gold
- Breed button shows lock icon

- [ ] **Step 2: Test all sidebar navigation**

Click each sidebar button in order: Shop → Orders → Pedia → Settings → Home.

Confirm for each:
- Content crossfades (brief opacity transition, ~0.3s)
- Correct placeholder screen name appears
- Active button highlights gold, previous deactivates
- Sidebar stays fixed during transition

- [ ] **Step 3: Test locked button**

Click the Breed button. Confirm:
- Nothing happens (button is disabled)
- No transition occurs

- [ ] **Step 4: Test immersive story event**

Press F1. Confirm:
- Entire screen fades to black (~0.25s)
- Sidebar disappears
- Story event placeholder appears full-screen (dark purple card)
- Click "Continue" → fade to black → sidebar returns → previous screen restored

- [ ] **Step 5: Test immersive level up**

Press F2. Confirm:
- Same fade-to-black flow
- Gold-themed level up card appears
- Shows "Level 5"
- Click "Continue" → returns to previous screen

- [ ] **Step 6: Test resolution picker**

Navigate to Settings. Change resolution to 1920x1080. Confirm:
- Window resizes
- UI scales correctly (no stretching, black bars if aspect doesn't match)
- Toggle fullscreen on/off

- [ ] **Step 7: Commit verified state**

If any fixes were needed during testing, commit them:

```bash
git add -A KeeperLegacyGodot/UI/
git commit -m "fix: polish navigation shell after integration testing"
```

---

## Summary

| Task | Description | Files Created |
|------|-------------|--------------|
| 0 | Project config fix (stretch aspect) | 0 (modify 1) |
| 1 | Download fonts | 3 |
| 2 | Sidebar component | 2 |
| 3 | Placeholder screens (base + 7 screens) | 8 |
| 4 | Settings screen with resolution picker | 2 |
| 5 | Immersive screens (story + level up) | 4 |
| 6 | MainScene orchestrator | 2 |
| 7 | OverlayBar component | 2 |
| 8 | Integration test | 0 |

**Total: 23 new files, 1 modified file, 8 commits**

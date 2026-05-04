# Habitat Floor Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the placeholder HabitatFloorScreen with a real scene: pre-rendered room background, 7 interactive pedestal habitats, creature display, and live HUD wired to managers.

**Architecture:** A static PNG background image rendered as a TextureRect, with Godot Control nodes layered on top for clickable pedestals, creature blobs, and HUD elements. Pedestals read data from HabitatManager. HUD reads from ProgressionManager and StoryManager. Clicking a pedestal navigates to HabitatCategoryScreen.

**Tech Stack:** Godot 4.6 (.NET), C# / .NET 8.0, existing autoload manager singletons

**Spec:** `docs/superpowers/specs/2026-04-13-habitat-floor-design.md`

**Existing code reference:**
- `HabitatManager` autoload: `Habitats` (List\<Habitat\>), `Creatures` (List\<CreatureInstance\>), `CreaturesInHabitat(Guid)`, signals `HabitatsChanged`, `CreaturesChanged`
- `ProgressionManager` autoload: `CurrentLevel`, `CurrentXP`, `XPToNextLevel`, `Coins`, signals `LeveledUp`, `XPChanged`, `CoinsChanged`
- `StoryManager` autoload: `HasPendingEvent()`, `GetPendingEvent()`, signal `StoryEventPending`, `StoryEventCompleted`
- `CreatureRosterData.Find(catalogId)` → `CreatureCatalogEntry` with `.Name`, `.HabitatType`
- `Habitat` class: `.Id`, `.Type` (HabitatType enum), `.OccupantId` (Guid?), `.IsEmpty`
- `CreatureInstance`: `.Id`, `.CatalogId`, no `.Name` — look up via `CreatureRosterData.Find(ci.CatalogId).Name`
- `HabitatType` enum: Water, Dirt, Grass, Fire, Ice, Electric, Magical
- `MainScene.NavigateToSubScreen(string)` — pushes stack, crossfades to sub-screen
- `GameFeature.MagicalHabitat` — checked via `ProgressionManager.IsFeatureUnlocked()`

---

## File Structure

```
KeeperLegacyGodot/
├── UI/
│   └── Habitat/
│       ├── HabitatFloorScreen.tscn    (REPLACE existing placeholder)
│       ├── HabitatFloorScreen.cs      (CREATE — room logic, pedestal creation, HUD wiring)
│       └── PedestalNode.cs            (CREATE — single pedestal: hitbox, label, creature blobs)
├── Sprites/
│   └── Backgrounds/
│       └── habitat_floor_bg.png       (ADD — Jesse's hand-drawn room art, 1364x768)
```

---

## Task 0: Add Background Art

**Files:**
- Add: `KeeperLegacyGodot/Sprites/Backgrounds/habitat_floor_bg.png`

Jesse provides the background image. This task just places it in the project.

- [ ] **Step 1: Copy the background image into the project**

Copy Jesse's habitat floor background art to `KeeperLegacyGodot/Sprites/Backgrounds/habitat_floor_bg.png`. The image should be 1364x768 pixels, PNG format.

```bash
mkdir -p "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/Sprites/Backgrounds"
# Copy the image file — Jesse will provide the path
# cp "/path/to/jesse/habitat_floor_bg.png" "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/Sprites/Backgrounds/habitat_floor_bg.png"
```

- [ ] **Step 2: Verify the file exists and check dimensions**

```bash
file "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/Sprites/Backgrounds/habitat_floor_bg.png"
```

Expected: PNG image data, 1364 x 768

- [ ] **Step 3: Commit**

```bash
git add KeeperLegacyGodot/Sprites/Backgrounds/habitat_floor_bg.png
git commit -m "feat: add habitat floor background art (1364x768)"
```

---

## Task 1: PedestalNode Component

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/PedestalNode.cs`

A reusable component representing one pedestal on the habitat floor. Handles its own label, creature blobs, lock state, and click interaction.

- [ ] **Step 1: Create PedestalNode.cs**

```csharp
using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Models;
using KeeperLegacy.Data;

namespace KeeperLegacy.UI.Habitat;

/// <summary>
/// A single pedestal hotspot on the habitat floor.
/// Displays habitat type label, creature count, creature blobs, and handles click.
/// </summary>
public partial class PedestalNode : Control
{
    [Signal] public delegate void PedestalClickedEventHandler(int habitatType);

    // Habitat type colors for creature blobs (from approved mockup palette)
    private static readonly Dictionary<HabitatType, Color> HabitatColors = new()
    {
        [HabitatType.Water]    = new Color("#4AA8E0"),
        [HabitatType.Grass]    = new Color("#4AB84A"),
        [HabitatType.Dirt]     = new Color("#C08840"),
        [HabitatType.Fire]     = new Color("#E06030"),
        [HabitatType.Ice]      = new Color("#60D0E0"),
        [HabitatType.Electric] = new Color("#E8D020"),
        [HabitatType.Magical]  = new Color("#9860E0"),
    };

    private HabitatType _habitatType;
    private bool _locked;
    private List<CreatureBlobData> _creatures = new();
    private float _bobTime;

    // Child nodes built in Setup
    private Label _nameLabel = null!;
    private Label _countLabel = null!;
    private Button _hitbox = null!;

    private record CreatureBlobData(string Initial, Color BlobColor, float PhaseOffset);

    /// <summary>
    /// Call once after instantiation to configure the pedestal.
    /// </summary>
    public void Setup(HabitatType type, Vector2 center, bool locked,
                      List<(string creatureName, HabitatType creatureType)> occupants)
    {
        _habitatType = type;
        _locked = locked;

        // Position centered on the given point. Hitbox is 160x80 (matching pedestal diamond).
        Size = new Vector2(160, 80);
        Position = center - Size / 2;

        BuildHitbox();
        BuildLabel();
        UpdateCreatures(occupants);
        ApplyLockState();
    }

    private void BuildHitbox()
    {
        _hitbox = new Button();
        _hitbox.Flat = true;
        _hitbox.FocusMode = FocusModeEnum.None;
        _hitbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _hitbox.MouseDefaultCursorShape = CursorShape.PointingHand;
        _hitbox.Pressed += OnPressed;

        // Transparent — click area only, no visual
        var emptyStyle = new StyleBoxEmpty();
        _hitbox.AddThemeStyleboxOverride("normal", emptyStyle);
        _hitbox.AddThemeStyleboxOverride("hover", emptyStyle);
        _hitbox.AddThemeStyleboxOverride("pressed", emptyStyle);
        _hitbox.AddThemeStyleboxOverride("focus", emptyStyle);

        AddChild(_hitbox);
    }

    private void BuildLabel()
    {
        // Label container positioned above the pedestal
        var labelContainer = new VBoxContainer();
        labelContainer.Position = new Vector2(20, -55);
        labelContainer.Size = new Vector2(120, 50);
        labelContainer.AddThemeConstantOverride("separation", 1);
        labelContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(labelContainer);

        // Habitat type name
        _nameLabel = new Label();
        _nameLabel.Text = _habitatType.ToString();
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        _nameLabel.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
        _nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        labelContainer.AddChild(_nameLabel);

        // Creature count or lock badge
        _countLabel = new Label();
        _countLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _countLabel.AddThemeFontSizeOverride("font_size", 9);
        _countLabel.AddThemeColorOverride("font_color", new Color("#B8A080"));
        _countLabel.MouseFilter = MouseFilterEnum.Ignore;
        labelContainer.AddChild(_countLabel);
    }

    /// <summary>
    /// Update the creature blobs displayed on this pedestal.
    /// </summary>
    public void UpdateCreatures(List<(string creatureName, HabitatType creatureType)> occupants)
    {
        _creatures.Clear();
        var rng = new Random(_habitatType.GetHashCode()); // Deterministic per type
        foreach (var (name, type) in occupants)
        {
            string initial = name.Length > 0 ? name[..1].ToUpper() : "?";
            var color = HabitatColors.GetValueOrDefault(type, new Color("#888888"));
            float phase = (float)(rng.NextDouble() * Math.PI * 2);
            _creatures.Add(new CreatureBlobData(initial, color, phase));
        }

        // Update count label
        if (_locked)
        {
            _countLabel.Text = "🔒 Locked";
        }
        else
        {
            // Get max capacity for display. Show current/max.
            _countLabel.Text = $"{occupants.Count} creature{(occupants.Count != 1 ? "s" : "")}";
        }

        QueueRedraw();
    }

    private void ApplyLockState()
    {
        if (_locked)
        {
            _hitbox.Disabled = true;
            _hitbox.MouseDefaultCursorShape = CursorShape.Arrow;
            Modulate = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            _countLabel.Text = "🔒 Locked";
        }
    }

    private void OnPressed()
    {
        if (_locked) return;
        EmitSignal(SignalName.PedestalClicked, (int)_habitatType);
    }

    // ── Drawing creature blobs ────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_creatures.Count > 0)
        {
            _bobTime += (float)delta;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_creatures.Count == 0) return;

        float blobRadius = 16f;
        float centerX = Size.X / 2;
        float centerY = Size.Y / 2 - 5; // Slightly above pedestal center

        for (int i = 0; i < Math.Min(_creatures.Count, 2); i++)
        {
            var blob = _creatures[i];

            // Offset blobs left/right if there are 2
            float offsetX = _creatures.Count == 1 ? 0 : (i == 0 ? -18 : 18);

            // Idle bob animation
            float bobY = (float)Math.Sin(_bobTime * 2.5f + blob.PhaseOffset) * 4f;

            var pos = new Vector2(centerX + offsetX, centerY + bobY);

            // Shadow
            DrawCircle(pos + new Vector2(1, 3), blobRadius, new Color(0, 0, 0, 0.3f));

            // Blob circle
            DrawCircle(pos, blobRadius, blob.BlobColor);

            // Initial letter
            var font = ThemeDB.FallbackFont;
            int fontSize = 14;
            var textSize = font.GetStringSize(blob.Initial, HorizontalAlignment.Center, -1, fontSize);
            var textPos = pos - new Vector2(textSize.X / 2, -textSize.Y / 4);
            DrawString(font, textPos, blob.Initial, HorizontalAlignment.Left, -1, fontSize, Colors.White);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add KeeperLegacyGodot/UI/Habitat/PedestalNode.cs
git commit -m "feat: add PedestalNode component for habitat floor pedestals"
```

---

## Task 2: HabitatFloorScreen — Background + Pedestals

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatFloorScreen.cs`
- Replace: `KeeperLegacyGodot/UI/Habitat/HabitatFloorScreen.tscn`

Build the main screen with background image, 7 pedestals, and pedestal click → navigation.

- [ ] **Step 1: Create HabitatFloorScreen.cs**

```csharp
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Models;
using KeeperLegacy.Data;
using KeeperLegacy.UI.Habitat;

public partial class HabitatFloorScreen : Control
{
    // Pedestal center positions on the 1364x768 background.
    // These are approximate — adjust to match Jesse's art.
    private static readonly (HabitatType Type, Vector2 Position)[] PedestalLayout = new[]
    {
        (HabitatType.Water,    new Vector2(320, 370)),
        (HabitatType.Grass,    new Vector2(670, 240)),
        (HabitatType.Dirt,     new Vector2(1000, 350)),
        (HabitatType.Fire,     new Vector2(430, 500)),
        (HabitatType.Ice,      new Vector2(700, 420)),
        (HabitatType.Electric, new Vector2(1000, 520)),
        (HabitatType.Magical,  new Vector2(670, 650)),
    };

    /// <summary>
    /// Set by this screen before navigating to HabitatCategory.
    /// The category screen reads this to know which type was selected.
    /// </summary>
    public static HabitatType? SelectedHabitatType { get; set; }

    private readonly List<PedestalNode> _pedestals = new();

    // HUD nodes
    private Label _levelLabel = null!;
    private ColorRect _xpFill = null!;
    private Label _coinLabel = null!;
    private Control _storyBadge = null!;
    private Label _storyBadgeLabel = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        BuildBackground();
        BuildPedestals();
        BuildHUD();
        WireManagerSignals();
        RefreshAllData();
    }

    // ── Background ────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        var bgTexture = GD.Load<Texture2D>("res://Sprites/Backgrounds/habitat_floor_bg.png");
        if (bgTexture == null)
        {
            // Fallback if image not yet added — show dark background
            var fallback = new ColorRect();
            fallback.Color = new Color("#2A1E10");
            fallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(fallback);

            var label = new Label();
            label.Text = "Background art not found — add habitat_floor_bg.png";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            label.AddThemeColorOverride("font_color", new Color("#9A8070"));
            AddChild(label);
            return;
        }

        var bg = new TextureRect();
        bg.Texture = bgTexture;
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        AddChild(bg);
    }

    // ── Pedestals ─────────────────────────────────────────────────────────

    private void BuildPedestals()
    {
        var habitatMgr = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
        var progressionMgr = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

        foreach (var (type, pos) in PedestalLayout)
        {
            var pedestal = new PedestalNode();
            AddChild(pedestal);

            bool locked = IsPedestalLocked(type, habitatMgr, progressionMgr);
            var occupants = GetOccupantsForType(type, habitatMgr);

            // Scale positions from 1364x768 art to actual content area size
            var scaledPos = ScalePosition(pos);

            pedestal.Setup(type, scaledPos, locked, occupants);
            pedestal.PedestalClicked += OnPedestalClicked;
            _pedestals.Add(pedestal);
        }
    }

    private bool IsPedestalLocked(HabitatType type, HabitatManager? hm, ProgressionManager? pm)
    {
        // Magical requires story act II
        if (type == HabitatType.Magical)
        {
            return pm == null || !pm.IsFeatureUnlocked(GameFeature.MagicalHabitat);
        }

        // Other types: check if player has any habitat of this type
        if (hm == null) return true;
        return !hm.Habitats.Any(h => h.Type == type);
    }

    private List<(string name, HabitatType type)> GetOccupantsForType(
        HabitatType type, HabitatManager? hm)
    {
        if (hm == null) return new();

        var result = new List<(string, HabitatType)>();

        foreach (var habitat in hm.Habitats.Where(h => h.Type == type))
        {
            if (habitat.OccupantId is Guid creatureId)
            {
                var creature = hm.GetCreature(creatureId);
                if (creature != null)
                {
                    var catalog = CreatureRosterData.Find(creature.CatalogId);
                    string name = catalog?.Name ?? "?";
                    result.Add((name, type));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Scale a position from the 1364x768 art coordinate space
    /// to the current content area size.
    /// </summary>
    private Vector2 ScalePosition(Vector2 artPos)
    {
        float scaleX = Size.X / 1364f;
        float scaleY = Size.Y / 768f;
        return new Vector2(artPos.X * scaleX, artPos.Y * scaleY);
    }

    private void OnPedestalClicked(int habitatTypeInt)
    {
        var type = (HabitatType)habitatTypeInt;
        SelectedHabitatType = type;

        // Navigate to habitat category sub-screen
        var mainScene = GetNodeOrNull<MainScene>("/root/MainScene");
        if (mainScene == null)
        {
            // Fallback: walk up the tree to find MainScene
            var parent = GetParent();
            while (parent != null)
            {
                if (parent is MainScene ms)
                {
                    ms.NavigateToSubScreen("HabitatCategory");
                    return;
                }
                parent = parent.GetParent();
            }
            GD.PushWarning("HabitatFloorScreen: Could not find MainScene for navigation");
            return;
        }
        mainScene.NavigateToSubScreen("HabitatCategory");
    }

    // ── HUD ───────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        BuildStoreSign();
        BuildInfoStrip();
        BuildStoryBadge();
    }

    private void BuildStoreSign()
    {
        // Container at top-left
        var sign = new PanelContainer();
        sign.Position = new Vector2(16, 10);
        sign.MouseFilter = MouseFilterEnum.Ignore;

        var signStyle = new StyleBoxFlat();
        signStyle.BgColor = new Color(0.118f, 0.086f, 0.031f, 0.9f);
        signStyle.BorderWidthTop = 1;
        signStyle.BorderWidthBottom = 1;
        signStyle.BorderWidthLeft = 1;
        signStyle.BorderWidthRight = 1;
        signStyle.BorderColor = new Color(0.91f, 0.72f, 0.19f, 0.45f);
        signStyle.CornerRadiusTopLeft = 4;
        signStyle.CornerRadiusTopRight = 4;
        signStyle.CornerRadiusBottomLeft = 4;
        signStyle.CornerRadiusBottomRight = 4;
        signStyle.ContentMarginLeft = 22;
        signStyle.ContentMarginRight = 22;
        signStyle.ContentMarginTop = 6;
        signStyle.ContentMarginBottom = 6;
        sign.AddThemeStyleboxOverride("panel", signStyle);

        var signVbox = new VBoxContainer();
        signVbox.AddThemeConstantOverride("separation", 1);
        signVbox.Alignment = BoxContainer.AlignmentMode.Center;
        sign.AddChild(signVbox);

        var titleLabel = new Label();
        titleLabel.Text = "✦  KEEPER'S LEGACY  ✦";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 15);
        titleLabel.AddThemeColorOverride("font_color", new Color("#E8B830"));
        titleLabel.MouseFilter = MouseFilterEnum.Ignore;
        signVbox.AddChild(titleLabel);

        var subLabel = new Label();
        subLabel.Text = "✦ Creature Emporium ✦";
        subLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subLabel.AddThemeFontSizeOverride("font_size", 11);
        subLabel.AddThemeColorOverride("font_color", new Color(0.91f, 0.72f, 0.19f, 0.65f));
        subLabel.MouseFilter = MouseFilterEnum.Ignore;
        signVbox.AddChild(subLabel);

        AddChild(sign);
    }

    private void BuildInfoStrip()
    {
        // Container at top-right
        var strip = new PanelContainer();
        strip.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        strip.Position = new Vector2(-230, 10);
        strip.Size = new Vector2(214, 28);
        strip.MouseFilter = MouseFilterEnum.Ignore;

        var stripStyle = new StyleBoxFlat();
        stripStyle.BgColor = new Color(0.118f, 0.086f, 0.039f, 0.80f);
        stripStyle.CornerRadiusTopLeft = 16;
        stripStyle.CornerRadiusTopRight = 16;
        stripStyle.CornerRadiusBottomLeft = 16;
        stripStyle.CornerRadiusBottomRight = 16;
        stripStyle.BorderWidthTop = 1;
        stripStyle.BorderWidthBottom = 1;
        stripStyle.BorderWidthLeft = 1;
        stripStyle.BorderWidthRight = 1;
        stripStyle.BorderColor = new Color(0.91f, 0.72f, 0.19f, 0.15f);
        stripStyle.ContentMarginLeft = 12;
        stripStyle.ContentMarginRight = 12;
        stripStyle.ContentMarginTop = 4;
        stripStyle.ContentMarginBottom = 4;
        strip.AddThemeStyleboxOverride("panel", stripStyle);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        strip.AddChild(hbox);

        // Level badge
        var levelBadge = new PanelContainer();
        var levelStyle = new StyleBoxFlat();
        levelStyle.BgColor = new Color("#E8B830");
        levelStyle.CornerRadiusTopLeft = 10;
        levelStyle.CornerRadiusTopRight = 10;
        levelStyle.CornerRadiusBottomLeft = 10;
        levelStyle.CornerRadiusBottomRight = 10;
        levelStyle.ContentMarginLeft = 10;
        levelStyle.ContentMarginRight = 10;
        levelStyle.ContentMarginTop = 1;
        levelStyle.ContentMarginBottom = 1;
        levelBadge.AddThemeStyleboxOverride("panel", levelStyle);
        levelBadge.MouseFilter = MouseFilterEnum.Ignore;

        _levelLabel = new Label();
        _levelLabel.Text = "Lv. 1";
        _levelLabel.AddThemeFontSizeOverride("font_size", 11);
        _levelLabel.AddThemeColorOverride("font_color", new Color("#1A1208"));
        _levelLabel.MouseFilter = MouseFilterEnum.Ignore;
        levelBadge.AddChild(_levelLabel);
        hbox.AddChild(levelBadge);

        // XP bar
        var xpTrack = new ColorRect();
        xpTrack.CustomMinimumSize = new Vector2(90, 6);
        xpTrack.Color = new Color(0, 0, 0, 0.4f);
        xpTrack.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(xpTrack);

        _xpFill = new ColorRect();
        _xpFill.Color = new Color("#E8B830");
        _xpFill.Size = new Vector2(0, 6);
        _xpFill.Position = Vector2.Zero;
        _xpFill.MouseFilter = MouseFilterEnum.Ignore;
        xpTrack.AddChild(_xpFill);

        // Coin count
        _coinLabel = new Label();
        _coinLabel.Text = "✦ 0";
        _coinLabel.AddThemeFontSizeOverride("font_size", 12);
        _coinLabel.AddThemeColorOverride("font_color", new Color("#F0CC50"));
        _coinLabel.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(_coinLabel);

        AddChild(strip);
    }

    private void BuildStoryBadge()
    {
        _storyBadge = new PanelContainer();
        _storyBadge.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        _storyBadge.Position = new Vector2(-180, 46);
        _storyBadge.Visible = false;
        _storyBadge.MouseFilter = MouseFilterEnum.Stop;

        var badgeStyle = new StyleBoxFlat();
        badgeStyle.BgColor = new Color(0.596f, 0.376f, 0.878f, 0.85f);
        badgeStyle.BorderWidthTop = 1;
        badgeStyle.BorderWidthBottom = 1;
        badgeStyle.BorderWidthLeft = 1;
        badgeStyle.BorderWidthRight = 1;
        badgeStyle.BorderColor = new Color(0.596f, 0.376f, 0.878f, 0.5f);
        badgeStyle.CornerRadiusTopLeft = 20;
        badgeStyle.CornerRadiusTopRight = 20;
        badgeStyle.CornerRadiusBottomLeft = 20;
        badgeStyle.CornerRadiusBottomRight = 20;
        badgeStyle.ContentMarginLeft = 12;
        badgeStyle.ContentMarginRight = 12;
        badgeStyle.ContentMarginTop = 5;
        badgeStyle.ContentMarginBottom = 5;
        ((PanelContainer)_storyBadge).AddThemeStyleboxOverride("panel", badgeStyle);

        _storyBadgeLabel = new Label();
        _storyBadgeLabel.Text = "✦ Story awaits...";
        _storyBadgeLabel.AddThemeFontSizeOverride("font_size", 10);
        _storyBadgeLabel.AddThemeColorOverride("font_color", Colors.White);
        _storyBadgeLabel.MouseFilter = MouseFilterEnum.Ignore;
        ((PanelContainer)_storyBadge).AddChild(_storyBadgeLabel);

        // Make the badge clickable
        var clickBtn = new Button();
        clickBtn.Flat = true;
        clickBtn.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        clickBtn.FocusMode = FocusModeEnum.None;
        var emptyStyle = new StyleBoxEmpty();
        clickBtn.AddThemeStyleboxOverride("normal", emptyStyle);
        clickBtn.AddThemeStyleboxOverride("hover", emptyStyle);
        clickBtn.AddThemeStyleboxOverride("pressed", emptyStyle);
        clickBtn.MouseDefaultCursorShape = CursorShape.PointingHand;
        clickBtn.Pressed += OnStoryBadgeClicked;
        ((PanelContainer)_storyBadge).AddChild(clickBtn);

        AddChild(_storyBadge);
    }

    private void OnStoryBadgeClicked()
    {
        var storyMgr = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (storyMgr != null && storyMgr.HasPendingEvent())
        {
            storyMgr.EmitSignal(StoryManager.SignalName.StoryEventPending,
                storyMgr.GetPendingEvent()!.Id);
        }
    }

    // ── Manager signal wiring ─────────────────────────────────────────────

    private void WireManagerSignals()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm != null)
        {
            pm.LeveledUp += (_) => RefreshLevelDisplay();
            pm.XPChanged += (_, _) => RefreshXPDisplay();
            pm.CoinsChanged += (_) => RefreshCoinDisplay();
        }

        var sm = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (sm != null)
        {
            sm.StoryEventPending += (_) => RefreshStoryBadge();
            sm.StoryEventCompleted += (_) => RefreshStoryBadge();
        }

        var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
        if (hm != null)
        {
            hm.CreaturesChanged += RefreshPedestals;
            hm.HabitatsChanged += RefreshPedestals;
        }
    }

    // ── Data refresh ──────────────────────────────────────────────────────

    private void RefreshAllData()
    {
        RefreshLevelDisplay();
        RefreshXPDisplay();
        RefreshCoinDisplay();
        RefreshStoryBadge();
    }

    private void RefreshLevelDisplay()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm != null)
            _levelLabel.Text = $"Lv. {pm.CurrentLevel}";
    }

    private void RefreshXPDisplay()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm == null) return;

        float ratio = pm.XPToNextLevel > 0
            ? (float)pm.CurrentXP / pm.XPToNextLevel
            : 0f;
        _xpFill.Size = new Vector2(90f * Math.Clamp(ratio, 0f, 1f), 6f);
    }

    private void RefreshCoinDisplay()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm != null)
            _coinLabel.Text = $"✦ {pm.Coins:N0}";
    }

    private void RefreshStoryBadge()
    {
        var sm = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (sm == null || !sm.HasPendingEvent())
        {
            _storyBadge.Visible = false;
            return;
        }

        var evt = sm.GetPendingEvent();
        if (evt != null)
        {
            // Look up NPC display name from the event's NpcId
            var npc = NPC.AllNPCs.FirstOrDefault(n => n.Id == evt.NpcId);
            string npcName = npc?.Name ?? "Someone";
            _storyBadgeLabel.Text = $"✦ {npcName} awaits...";
            _storyBadge.Visible = true;
        }
    }

    private void RefreshPedestals()
    {
        var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

        for (int i = 0; i < _pedestals.Count && i < PedestalLayout.Length; i++)
        {
            var type = PedestalLayout[i].Type;
            var occupants = GetOccupantsForType(type, hm);
            _pedestals[i].UpdateCreatures(occupants);
        }
    }
}
```

- [ ] **Step 2: Replace HabitatFloorScreen.tscn**

Delete the existing placeholder .tscn and create a new one:

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://UI/Habitat/HabitatFloorScreen.cs" id="1"]

[node name="HabitatFloorScreen" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

- [ ] **Step 3: Verify build compiles**

```bash
cd "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot" && dotnet build 2>&1 | grep -E "error|Build"
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add KeeperLegacyGodot/UI/Habitat/HabitatFloorScreen.cs KeeperLegacyGodot/UI/Habitat/HabitatFloorScreen.tscn
git commit -m "feat: replace habitat floor placeholder with real screen (background + pedestals + HUD)"
```

---

## Task 3: Integration Test — Habitat Floor

No new files. Manual verification in Godot.

- [ ] **Step 1: Run the project**

Open Godot, press F5.

**If background art is present:**
- Warm cabin room background fills the content area
- 7 pedestal hotspots visible (labels floating above their positions)
- Water pedestal shows creature blob(s) if save data has creatures
- Locked pedestals are dimmed with "🔒 Locked" text

**If background art is NOT yet present:**
- Dark #2A1E10 background with "Background art not found" message
- Pedestals and HUD still appear on top (functional without the art)

- [ ] **Step 2: Test HUD**

- Store sign "Keeper's Legacy" appears top-left with gold styling
- Info strip top-right shows "Lv. 1", XP bar, coin count
- Story badge hidden (no pending events in new game)

- [ ] **Step 3: Test pedestal click**

- Click an unlocked pedestal (Water in new game)
- Crossfade to Habitat Category placeholder screen
- Sidebar stays on "Home" highlight

- [ ] **Step 4: Test debug keys**

- Press F3 → all pedestals unlock
- Press F1 → story event appears, then dismiss → story badge may appear on floor

- [ ] **Step 5: Commit any fixes**

```bash
git add -A KeeperLegacyGodot/UI/Habitat/
git commit -m "fix: habitat floor polish after integration testing"
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 0 | Add background art | 1 (image) |
| 1 | PedestalNode component | 1 (.cs) |
| 2 | HabitatFloorScreen (background + pedestals + HUD) | 2 (.cs + .tscn replace) |
| 3 | Integration test | 0 |

**Total: 4 files created/modified, 4 commits**

**Note:** Task 0 depends on Jesse providing the background image. Tasks 1-2 can proceed without it — the screen has a fallback dark background with a "not found" message. Once the image is added, everything will appear correctly.

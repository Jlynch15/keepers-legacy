// UI/Habitat/HabitatFloorScreen.cs
// Main hub screen — pre-rendered room background with 7 clickable pedestal hotspots,
// HUD with level/XP/coins, and a story badge when a story event is pending.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.UI.Habitat;

public partial class HabitatFloorScreen : Control
{
    // ── Art-space reference dimensions ─────────────────────────────────────────

    private const float ArtW = 1364f;
    private const float ArtH = 768f;

    // Pedestal positions in art-space (center of each hotspot on 1364x768 bg)
    // Mapped from Jesse's annotated screenshot 2026-04-13
    private static readonly (HabitatType type, Vector2 pos)[] PedestalDefs =
    {
        (HabitatType.Water,    new Vector2( 340,  410)),  // left mid — blue annotation
        (HabitatType.Grass,    new Vector2( 590,  270)),  // upper center — green annotation
        (HabitatType.Dirt,     new Vector2( 870,  310)),  // upper right — brown annotation
        (HabitatType.Fire,     new Vector2( 280,  560)),  // lower left — red annotation
        (HabitatType.Ice,      new Vector2( 530,  620)),  // bottom center-left — white annotation
        (HabitatType.Electric, new Vector2( 960,  490)),  // right — yellow annotation
        (HabitatType.Magical,  new Vector2( 620,  450)),  // center of floor — purple circle
    };

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color ColourBgFallback   = new Color(0.165f, 0.118f, 0.063f, 1f);
    private static readonly Color ColourSignBg       = new Color(30f/255, 22f/255, 8f/255, 0.9f);
    private static readonly Color ColourSignBorder   = new Color("#E8B830");
    private static readonly Color ColourSignTitle    = new Color("#E8B830");
    private static readonly Color ColourSignSubtitle = new Color("#C09040");
    private static readonly Color ColourHudBg        = new Color(20f/255, 14f/255, 6f/255, 0.88f);
    private static readonly Color ColourHudBorder    = new Color("#E8B830");
    private static readonly Color ColourGold         = new Color("#E8B830");
    private static readonly Color ColourXpTrack      = new Color(60f/255, 44f/255, 20f/255, 1f);
    private static readonly Color ColourXpFill       = new Color("#E8B830");
    private static readonly Color ColourStoryBg      = new Color(50f/255, 20f/255, 80f/255, 0.92f);
    private static readonly Color ColourStoryBorder  = new Color("#9860E0");
    private static readonly Color ColourStoryText    = new Color("#D8B0FF");
    private static readonly Color ColourMuted        = new Color("#B8A080");

    // ── Static selection state (read by HabitatCategoryScreen) ───────────────

    public static HabitatType? SelectedHabitatType { get; set; }

    // ── Child references ──────────────────────────────────────────────────────

    private readonly Dictionary<HabitatType, PedestalNode> _pedestals = new();
    private Label     _levelLabel;
    private ColorRect _xpFill;
    private Label     _coinsLabel;
    private Control   _storyBadge;
    private Label     _storyLabel;

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        BuildBackground();
        BuildPedestals();
        BuildStoreSign();
        BuildInfoStrip();
        BuildStoryBadge();
        WireSignals();
        RefreshHud();
    }

    // ── Background ────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        // Dark fallback always present beneath the image
        var fallback = new ColorRect();
        fallback.Color = ColourBgFallback;
        fallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        fallback.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(fallback);

        // Attempt to load the real background image
        var tex = GD.Load<Texture2D>("res://Sprites/Backgrounds/habitat_floor_bg.png");
        if (tex != null)
        {
            var bg = new TextureRect();
            bg.Texture      = tex;
            bg.StretchMode  = TextureRect.StretchModeEnum.KeepAspectCentered;
            bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            bg.MouseFilter  = MouseFilterEnum.Ignore;
            AddChild(bg);
        }
    }

    // ── Pedestals ─────────────────────────────────────────────────────────────

    private void BuildPedestals()
    {
        var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

        foreach (var (type, artPos) in PedestalDefs)
        {
            bool locked = IsLocked(type, hm, pm);
            var occupants = BuildOccupantList(type, hm);

            // Scale art-space position to actual content area
            var scaledPos = ScalePosition(artPos);

            var pedestal = new PedestalNode();
            AddChild(pedestal);
            pedestal.Setup(type, scaledPos, locked, occupants);
            pedestal.PedestalClicked += OnPedestalClicked;

            _pedestals[type] = pedestal;
        }
    }

    private static bool IsLocked(HabitatType type,
        HabitatManager? hm, ProgressionManager? pm)
    {
        // Magical: locked until Story Act II unlocks MagicalHabitat feature
        if (type == HabitatType.Magical)
        {
            return pm == null || !pm.IsFeatureUnlocked(GameFeature.MagicalHabitat);
        }

        // Water, Grass, Dirt: always unlocked (base habitat types)
        if (type is HabitatType.Water or HabitatType.Grass or HabitatType.Dirt)
        {
            return false;
        }

        // Fire, Ice, Electric: unlocked if player has a habitat of that type
        // OR HabitatExpansion feature is unlocked (level 2+, also unlocked by F3)
        bool hasHabitat = hm?.Habitats.Any(h => h.Type == type) ?? false;
        if (hasHabitat) return false;

        bool expansionUnlocked = pm?.IsFeatureUnlocked(GameFeature.HabitatExpansion) ?? false;
        return !expansionUnlocked;
    }

    private static List<(string name, HabitatType type)> BuildOccupantList(
        HabitatType type, HabitatManager? hm)
    {
        if (hm == null) return new();

        var result = new List<(string, HabitatType)>();
        foreach (var habitat in hm.Habitats.Where(h => h.Type == type))
        {
            if (habitat.OccupantId is { } occupantId)
            {
                var creature = hm.GetCreature(occupantId);
                if (creature != null)
                {
                    string cname = CreatureRosterData.Find(creature.CatalogId)?.Name
                                   ?? "?";
                    result.Add((cname, type));
                }
            }
        }
        return result;
    }

    // ── Store Sign (top-left) ─────────────────────────────────────────────────

    private void BuildStoreSign()
    {
        // Hanging sign: chains from top of screen → wooden sign board
        var signContainer = new Control();
        signContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        signContainer.OffsetLeft = 30;
        signContainer.OffsetTop = 0;
        signContainer.OffsetRight = 220;
        signContainer.OffsetBottom = 90;
        signContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(signContainer);

        // Left chain
        var chainLeft = new ColorRect();
        chainLeft.Position = new Vector2(40, 0);
        chainLeft.Size = new Vector2(2, 30);
        chainLeft.Color = new Color(0.91f, 0.72f, 0.19f, 0.35f);
        chainLeft.MouseFilter = MouseFilterEnum.Ignore;
        signContainer.AddChild(chainLeft);

        // Right chain
        var chainRight = new ColorRect();
        chainRight.Position = new Vector2(148, 0);
        chainRight.Size = new Vector2(2, 30);
        chainRight.Color = new Color(0.91f, 0.72f, 0.19f, 0.35f);
        chainRight.MouseFilter = MouseFilterEnum.Ignore;
        signContainer.AddChild(chainRight);

        // Sign board
        var signBoard = new PanelContainer();
        signBoard.Position = new Vector2(0, 28);
        signBoard.Size = new Vector2(190, 58);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.14f, 0.10f, 0.04f, 0.92f); // Dark wood
        style.BorderColor = ColourSignBorder;
        style.BorderWidthTop = 2;
        style.BorderWidthBottom = 2;
        style.BorderWidthLeft = 2;
        style.BorderWidthRight = 2;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
        // Subtle shadow effect via border
        style.ShadowColor = new Color(0, 0, 0, 0.4f);
        style.ShadowSize = 4;
        style.ShadowOffset = new Vector2(0, 3);
        signBoard.AddThemeStyleboxOverride("panel", style);
        signBoard.MouseFilter = MouseFilterEnum.Ignore;
        signContainer.AddChild(signBoard);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 1);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        signBoard.AddChild(vbox);

        // Decorative top accent
        var accentLine = new Label();
        accentLine.Text = "✦ ─── ✦";
        accentLine.HorizontalAlignment = HorizontalAlignment.Center;
        accentLine.AddThemeFontSizeOverride("font_size", 8);
        accentLine.AddThemeColorOverride("font_color", new Color(0.91f, 0.72f, 0.19f, 0.4f));
        accentLine.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(accentLine);

        var title = new Label();
        title.Text = "KEEPER'S LEGACY";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", ColourSignTitle);
        title.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(title);

        var subtitle = new Label();
        subtitle.Text = "✦ Creature Emporium ✦";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeFontSizeOverride("font_size", 10);
        subtitle.AddThemeColorOverride("font_color", ColourSignSubtitle);
        subtitle.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(subtitle);
    }

    // ── Info Strip (top-right) ────────────────────────────────────────────────

    private void BuildInfoStrip()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        panel.OffsetLeft   = -240;
        panel.OffsetTop    = 12;
        panel.OffsetRight  = -12;
        panel.OffsetBottom = 50;

        var style = new StyleBoxFlat();
        style.BgColor     = ColourHudBg;
        style.BorderColor = ColourHudBorder;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(20); // pill shape
        style.ContentMarginLeft   = 12;
        style.ContentMarginRight  = 12;
        style.ContentMarginTop    = 6;
        style.ContentMarginBottom = 6;
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(hbox);

        // Level badge
        _levelLabel = new Label();
        _levelLabel.AddThemeFontSizeOverride("font_size", 13);
        _levelLabel.AddThemeColorOverride("font_color", ColourGold);
        _levelLabel.VerticalAlignment = VerticalAlignment.Center;
        hbox.AddChild(_levelLabel);

        // XP track
        var xpTrack = new ColorRect();
        xpTrack.Color = ColourXpTrack;
        xpTrack.CustomMinimumSize = new Vector2(90, 8);
        xpTrack.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        hbox.AddChild(xpTrack);

        // XP fill — child of track, sized by fraction in RefreshHud
        _xpFill = new ColorRect();
        _xpFill.Color                = ColourXpFill;
        _xpFill.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
        _xpFill.OffsetRight          = 0; // Set in RefreshHud
        _xpFill.MouseFilter          = MouseFilterEnum.Ignore;
        xpTrack.AddChild(_xpFill);

        // Coin count
        _coinsLabel = new Label();
        _coinsLabel.AddThemeFontSizeOverride("font_size", 13);
        _coinsLabel.AddThemeColorOverride("font_color", ColourGold);
        _coinsLabel.VerticalAlignment = VerticalAlignment.Center;
        hbox.AddChild(_coinsLabel);
    }

    // ── Story Badge (below info strip) ────────────────────────────────────────

    private void BuildStoryBadge()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        panel.OffsetLeft   = -240;
        panel.OffsetTop    = 58;
        panel.OffsetRight  = -12;
        panel.OffsetBottom = 90;
        panel.Visible      = false;

        var style = new StyleBoxFlat();
        style.BgColor     = ColourStoryBg;
        style.BorderColor = ColourStoryBorder;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(16);
        style.ContentMarginLeft   = 10;
        style.ContentMarginRight  = 10;
        style.ContentMarginTop    = 4;
        style.ContentMarginBottom = 4;
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        // Invisible button over the entire badge
        var btn = new Button();
        btn.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        btn.Flat = true;
        btn.FocusMode = FocusModeEnum.None;
        btn.AddThemeStyleboxOverride("normal",   new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("hover",    new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("pressed",  new StyleBoxEmpty());
        btn.AddThemeStyleboxOverride("focus",    new StyleBoxEmpty());
        btn.Pressed += OnStoryBadgePressed;
        panel.AddChild(btn);

        _storyLabel = new Label();
        _storyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _storyLabel.VerticalAlignment   = VerticalAlignment.Center;
        _storyLabel.AddThemeFontSizeOverride("font_size", 11);
        _storyLabel.AddThemeColorOverride("font_color", ColourStoryText);
        _storyLabel.MouseFilter = MouseFilterEnum.Ignore;
        _storyLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panel.AddChild(_storyLabel);

        _storyBadge = panel;
    }

    // ── Signal wiring ─────────────────────────────────────────────────────────

    private void WireSignals()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm != null)
        {
            pm.LeveledUp       += (_) => RefreshHud();
            pm.XPChanged       += (_, _) => RefreshHud();
            pm.CoinsChanged    += (_) => RefreshHud();
            pm.FeatureUnlocked += (_) => RefreshPedestals();
        }

        var sm = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (sm != null)
        {
            sm.StoryEventPending   += (_) => RefreshStoryBadge();
            sm.StoryEventCompleted += (_) => RefreshStoryBadge();
        }

        var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
        if (hm != null)
        {
            hm.CreaturesChanged += RefreshPedestals;
            hm.HabitatsChanged  += RefreshPedestals;
        }
    }

    // ── HUD refresh ───────────────────────────────────────────────────────────

    private void RefreshHud()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm == null) return;

        _levelLabel.Text  = $"Lv. {pm.CurrentLevel}";
        _coinsLabel.Text  = $"\u2746 {pm.Coins}";

        float fraction = pm.XPToNextLevel > 0
            ? Mathf.Clamp((float)pm.CurrentXP / pm.XPToNextLevel, 0f, 1f)
            : 1f;

        // Size the fill bar. The parent track is 90px wide; we use OffsetRight to clip.
        // Since _xpFill is anchored LeftWide (left=0, right=0 offsets relative to parent),
        // setting OffsetRight to a negative value shrinks it from the right.
        float trackWidth = 90f;
        _xpFill.OffsetRight = -(trackWidth * (1f - fraction));

        RefreshStoryBadge();
    }

    private void RefreshStoryBadge()
    {
        var sm = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (sm == null || !sm.HasPendingEvent())
        {
            _storyBadge.Visible = false;
            return;
        }

        var evt  = sm.GetPendingEvent();
        string npcName = evt != null
            ? NPC.MainCast.FirstOrDefault(n => n.Id == evt.NpcId)?.Name ?? "Someone"
            : "Someone";

        _storyLabel.Text    = $"\u2746 {npcName} awaits...";
        _storyBadge.Visible = true;
    }

    private void RefreshPedestals()
    {
        var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

        foreach (var (type, pedestal) in _pedestals)
        {
            bool locked    = IsLocked(type, hm, pm);
            var occupants  = BuildOccupantList(type, hm);
            pedestal.Setup(type, pedestal.Position + pedestal.Size / 2f, locked, occupants);
        }
    }

    // ── Navigation helpers ────────────────────────────────────────────────────

    private void OnPedestalClicked(int habitatTypeInt)
    {
        SelectedHabitatType = (HabitatType)habitatTypeInt;

        var node = (Node)this;
        while (node != null)
        {
            if (node is MainScene ms)
            {
                ms.NavigateToSubScreen("HabitatCategory");
                return;
            }
            node = node.GetParent();
        }

        GD.PushWarning("HabitatFloorScreen: could not find MainScene in tree.");
    }

    private void OnStoryBadgePressed()
    {
        var sm = GetNodeOrNull<StoryManager>("/root/StoryManager");
        if (sm == null || !sm.HasPendingEvent()) return;

        // Trigger the story event through MainScene (same mechanism as F1 debug)
        var node = (Node)this;
        while (node != null)
        {
            node = node.GetParent();
        }
        // Fall back: emit via StoryManager directly so MainScene picks it up
        var evt = sm.GetPendingEvent();
        if (evt != null)
            sm.EmitSignal(StoryManager.SignalName.StoryEventPending, evt.Id);
    }

    // ── Coordinate scaling ────────────────────────────────────────────────────

    /// Scale an art-space position into actual content area coordinates.
    private Vector2 ScalePosition(Vector2 artPos)
    {
        // Content area excludes the 70px sidebar on the right (see MainScene)
        Vector2 viewport = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1364, 768);
        float contentW   = viewport.X - 70f;
        float contentH   = viewport.Y;

        float scaleX = contentW / ArtW;
        float scaleY = contentH / ArtH;
        float scale  = Mathf.Min(scaleX, scaleY);

        float offsetX = (contentW - ArtW * scale) / 2f;
        float offsetY = (contentH - ArtH * scale) / 2f;

        return new Vector2(
            offsetX + artPos.X * scale,
            offsetY + artPos.Y * scale
        );
    }
}

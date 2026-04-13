// UI/Components/Sidebar.cs
// Right-edge navigation sidebar, 70px wide.
// Builds its button layout programmatically in _Ready().
// Connects to ProgressionManager to reflect feature lock state.

using Godot;
using System.Collections.Generic;
using KeeperLegacy.Models;

public partial class Sidebar : PanelContainer
{
    // ── Signals ───────────────────────────────────────────────────────────────

    [Signal] public delegate void NavigationRequestedEventHandler(string screenName);

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color ColourBackground   = new Color("#1A1208");
    private static readonly Color ColourBorder       = new Color("#3A2818");
    private static readonly Color ColourActiveText   = new Color("#E8B830");
    private static readonly Color ColourActiveHighlight = new Color(0.91f, 0.72f, 0.19f, 0.15f); // gold 15 % opacity
    private static readonly Color ColourInactiveText = new Color("#9A8070");
    private static readonly Color ColourLockedText   = new Color("#5A4A3A");

    // ── Button definitions ────────────────────────────────────────────────────

    private record NavEntry(
        string ScreenName,
        string Icon,
        string Label,
        GameFeature? Feature   // null = always unlocked
    );

    private static readonly NavEntry[] MainEntries = new[]
    {
        new NavEntry("Home",     "⌂",  "Home",   null),
        new NavEntry("Shop",     "🛒", "Shop",   GameFeature.Shop),
        new NavEntry("Orders",   "📋", "Orders", GameFeature.CustomerOrders),
        new NavEntry("Breed",    "🥚", "Breed",  GameFeature.Breeding),
        new NavEntry("Pedia",    "📖", "Pedia",  GameFeature.Monsterpedia),
    };

    private static readonly NavEntry SettingsEntry =
        new NavEntry("Settings", "⚙",  "Settings", null);

    // ── Runtime state ─────────────────────────────────────────────────────────

    private string _activeScreen = "Home";

    // Map screenName → its Button node so we can update styles quickly.
    private readonly Dictionary<string, Button> _buttons = new();

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        BuildBackground();
        BuildLayout();
        ConnectProgressionSignal();
        SetActiveButton(_activeScreen);
    }

    // ── Background / border ───────────────────────────────────────────────────

    private void BuildBackground()
    {
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = ColourBackground;
        // Left border only — the right edge is the screen edge.
        styleBox.BorderWidthLeft = 1;
        styleBox.BorderColor = ColourBorder;
        // Remove default padding so we control it ourselves.
        styleBox.ContentMarginLeft   = 0;
        styleBox.ContentMarginRight  = 0;
        styleBox.ContentMarginTop    = 0;
        styleBox.ContentMarginBottom = 0;

        AddThemeStyleboxOverride("panel", styleBox);
    }

    // ── Layout builder ────────────────────────────────────────────────────────

    private void BuildLayout()
    {
        // Root VBoxContainer fills the panel.
        var root = new VBoxContainer();
        root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        // Top section: main nav buttons.
        var topSection = new VBoxContainer();
        topSection.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        topSection.AddThemeConstantOverride("separation", 0);
        root.AddChild(topSection);

        foreach (var entry in MainEntries)
        {
            var btn = CreateNavButton(entry);
            topSection.AddChild(btn);
            _buttons[entry.ScreenName] = btn;
        }

        // Spacer pushes Settings to the bottom.
        var spacer = new Control();
        spacer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        root.AddChild(spacer);

        // Thin separator above Settings.
        var sep = new HSeparator();
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = ColourBorder;
        sep.AddThemeStyleboxOverride("separator", sepStyle);
        root.AddChild(sep);

        // Bottom: Settings button.
        var settingsBtn = CreateNavButton(SettingsEntry);
        root.AddChild(settingsBtn);
        _buttons[SettingsEntry.ScreenName] = settingsBtn;
    }

    // ── Button factory ────────────────────────────────────────────────────────

    private Button CreateNavButton(NavEntry entry)
    {
        // Each button is a Godot Button with its own VBox (icon label + text label).
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(70, 64);
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btn.Flat = true;           // No default border/background — we draw our own.
        btn.FocusMode = Control.FocusModeEnum.None; // Keyboard focus handled elsewhere.
        btn.ClipText = false;
        btn.Name = "Btn_" + entry.ScreenName;

        // Remove Godot's built-in text; we use a custom inner VBox instead.
        btn.Text = "";

        // Inner layout: icon on top, label below.
        // Use FullRect preset so the VBox fills the entire Button area.
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 2);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.MouseFilter = Control.MouseFilterEnum.Ignore; // pass-through to Button
        btn.AddChild(vbox);

        // Icon label.
        var iconLabel = new Label();
        iconLabel.Text = entry.Icon;
        iconLabel.HorizontalAlignment = HorizontalAlignment.Center;
        iconLabel.VerticalAlignment   = VerticalAlignment.Center;
        iconLabel.AddThemeFontSizeOverride("font_size", 22);
        iconLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        iconLabel.Name = "IconLabel";
        vbox.AddChild(iconLabel);

        // Text label.
        var textLabel = new Label();
        textLabel.Text = entry.Label;
        textLabel.HorizontalAlignment = HorizontalAlignment.Center;
        textLabel.VerticalAlignment   = VerticalAlignment.Center;
        textLabel.AddThemeFontSizeOverride("font_size", 9);
        textLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        textLabel.Name = "TextLabel";
        vbox.AddChild(textLabel);

        // Wire up the pressed signal.
        btn.Pressed += () => OnButtonPressed(entry);

        return btn;
    }

    // ── Signal wiring ─────────────────────────────────────────────────────────

    private void ConnectProgressionSignal()
    {
        var pm = GetProgressionManager();
        if (pm == null) return;

        pm.FeatureUnlocked += OnFeatureUnlocked;
    }

    private void OnFeatureUnlocked(string _featureRaw)
    {
        RefreshLockStates();
    }

    private void OnButtonPressed(NavEntry entry)
    {
        if (IsEntryLocked(entry)) return;
        EmitSignal(SignalName.NavigationRequested, entry.ScreenName);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Highlights the button for <paramref name="screenName"/> as active
    /// without emitting a navigation signal. Used by sub-screens that want
    /// to update the sidebar selection programmatically.
    /// </summary>
    public void SetActiveButton(string screenName)
    {
        _activeScreen = screenName;
        ApplyAllButtonStyles();
    }

    /// <summary>
    /// Re-evaluates lock state for all buttons. Call when a feature unlocks.
    /// </summary>
    public void RefreshLockStates()
    {
        ApplyAllButtonStyles();
    }

    // ── Style application ─────────────────────────────────────────────────────

    private void ApplyAllButtonStyles()
    {
        foreach (var entry in MainEntries)
            ApplyButtonStyle(entry);
        ApplyButtonStyle(SettingsEntry);
    }

    private void ApplyButtonStyle(NavEntry entry)
    {
        if (!_buttons.TryGetValue(entry.ScreenName, out var btn)) return;

        bool locked   = IsEntryLocked(entry);
        bool active   = !locked && entry.ScreenName == _activeScreen;

        // Determine colours.
        Color textColour = locked  ? ColourLockedText
                         : active  ? ColourActiveText
                                   : ColourInactiveText;

        // Background stylebox.
        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = active ? ColourActiveHighlight : new Color(0, 0, 0, 0);
        normalStyle.ContentMarginLeft   = 0;
        normalStyle.ContentMarginRight  = 0;
        normalStyle.ContentMarginTop    = 4;
        normalStyle.ContentMarginBottom = 4;

        // Hover style (same highlight, slightly brighter for non-locked).
        var hoverStyle = normalStyle.Duplicate() as StyleBoxFlat;
        if (!locked && hoverStyle != null)
            hoverStyle.BgColor = active
                ? ColourActiveHighlight
                : new Color(0.91f, 0.72f, 0.19f, 0.07f); // faint gold on hover

        btn.AddThemeStyleboxOverride("normal",   normalStyle);
        btn.AddThemeStyleboxOverride("hover",    hoverStyle ?? normalStyle);
        btn.AddThemeStyleboxOverride("pressed",  normalStyle);
        btn.AddThemeStyleboxOverride("disabled", normalStyle);
        btn.AddThemeStyleboxOverride("focus",    new StyleBoxEmpty());

        // Disable interaction for locked buttons.
        btn.Disabled = locked;

        // Find the inner labels and update colours + icon.
        var vbox     = btn.GetChildOrNull<VBoxContainer>(0);
        if (vbox == null) return;

        var iconLabel = vbox.GetNodeOrNull<Label>("IconLabel");
        var textLabel = vbox.GetNodeOrNull<Label>("TextLabel");

        if (iconLabel != null)
        {
            iconLabel.Text = locked ? "🔒" : entry.Icon;
            iconLabel.AddThemeColorOverride("font_color", textColour);
        }

        if (textLabel != null)
        {
            textLabel.AddThemeColorOverride("font_color", textColour);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsEntryLocked(NavEntry entry)
    {
        if (entry.Feature == null) return false; // always unlocked

        var pm = GetProgressionManager();
        if (pm == null) return false;            // autoload not ready — treat as unlocked

        return !pm.IsFeatureUnlocked(entry.Feature.Value);
    }

    private ProgressionManager? GetProgressionManager()
    {
        return GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
    }
}

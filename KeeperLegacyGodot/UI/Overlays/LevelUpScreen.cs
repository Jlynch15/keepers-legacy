// UI/Overlays/LevelUpScreen.cs
// Full-viewport level-up celebration screen shown in the ImmersiveLayer (CanvasLayer).
// Call SetLevel() before adding to the scene tree, then listen for Dismissed.

using Godot;

namespace KeeperLegacy.UI.Overlays;

public partial class LevelUpScreen : Control
{
    // ── Signals ───────────────────────────────────────────────────────────────

    [Signal] public delegate void DismissedEventHandler();

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color ColourBackground   = new Color("#1A1208");
    private static readonly Color ColourCardBg       = new Color("#2A1E10");
    private static readonly Color ColourCardBorder   = new Color("#E8B830");
    private static readonly Color ColourHeader       = new Color("#E8B830");
    private static readonly Color ColourLevelNumber  = new Color("#F0CC50");
    private static readonly Color ColourRewards      = new Color("#9A8070");

    // ── State ─────────────────────────────────────────────────────────────────

    private int _newLevel = 1;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the level number displayed in the card.
    /// Must be called BEFORE the node is added to the scene tree (before _Ready runs).
    /// </summary>
    public void SetLevel(int level)
    {
        _newLevel = level;
    }

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        // 1. Fill the full viewport.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // 2. Gold-tinted background.
        var background = new ColorRect();
        background.Color = ColourBackground;
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        // 3. Centered card (PanelContainer).
        var card = new PanelContainer();
        card.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        card.OffsetLeft   = -200;
        card.OffsetRight  =  200;
        card.OffsetTop    = -120;
        card.OffsetBottom =  120;

        var cardStyle = new StyleBoxFlat();
        cardStyle.BgColor         = ColourCardBg;
        cardStyle.BorderColor     = ColourCardBorder;
        cardStyle.SetBorderWidthAll(2);
        cardStyle.SetCornerRadiusAll(12);
        cardStyle.ContentMarginLeft   = 24;
        cardStyle.ContentMarginRight  = 24;
        cardStyle.ContentMarginTop    = 24;
        cardStyle.ContentMarginBottom = 24;
        card.AddThemeStyleboxOverride("panel", cardStyle);

        AddChild(card);

        // 4. VBoxContainer inside the card.
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical   = SizeFlags.ExpandFill;
        card.AddChild(vbox);

        // Header label.
        var headerLabel = new Label();
        headerLabel.Text = "Level Up!";
        headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        headerLabel.AddThemeFontSizeOverride("font_size", 28);
        headerLabel.AddThemeColorOverride("font_color", ColourHeader);
        headerLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddChild(headerLabel);

        // Level number label.
        var levelLabel = new Label();
        levelLabel.Text = $"Level {_newLevel}";
        levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        levelLabel.AddThemeFontSizeOverride("font_size", 48);
        levelLabel.AddThemeColorOverride("font_color", ColourLevelNumber);
        levelLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddChild(levelLabel);

        // Rewards placeholder label.
        var rewardsLabel = new Label();
        rewardsLabel.Text = "(Rewards will be shown here)";
        rewardsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        rewardsLabel.AddThemeFontSizeOverride("font_size", 14);
        rewardsLabel.AddThemeColorOverride("font_color", ColourRewards);
        rewardsLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddChild(rewardsLabel);

        // Continue button (inside card, centered horizontally).
        var continueBtn = new Button();
        continueBtn.Text = "Continue";
        continueBtn.CustomMinimumSize = new Vector2(150, 40);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.Pressed += OnDismissed;
        vbox.AddChild(continueBtn);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnDismissed()
    {
        EmitSignal(SignalName.Dismissed);
    }
}

// UI/Story/StoryEventScreen.cs
// Full-viewport story event screen shown in the ImmersiveLayer (CanvasLayer).
// Emits Dismissed when the player clicks Continue or Skip.

using Godot;

namespace KeeperLegacy.UI.Story;

public partial class StoryEventScreen : Control
{
    // ── Signals ───────────────────────────────────────────────────────────────

    [Signal] public delegate void DismissedEventHandler();

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color ColourBackground    = new Color("#0A0510");
    private static readonly Color ColourCardBg        = new Color("#1A1230");
    private static readonly Color ColourCardBorder    = new Color("#3A2050");
    private static readonly Color ColourTitle         = new Color("#F0E8D8");
    private static readonly Color ColourBody          = new Color("#B8A080");
    private static readonly Color ColourSkip          = new Color("#5A4A3A");
    private static readonly Color ColourSkipHover     = new Color("#9A8070");

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        // 1. Fill the full viewport.
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // 2. Dark mystical background.
        var background = new ColorRect();
        background.Color = ColourBackground;
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        // 3. Centered card (PanelContainer).
        var card = new PanelContainer();
        card.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        card.OffsetLeft   = -250;
        card.OffsetRight  =  250;
        card.OffsetTop    = -150;
        card.OffsetBottom =  150;

        var cardStyle = new StyleBoxFlat();
        cardStyle.BgColor         = ColourCardBg;
        cardStyle.BorderColor     = ColourCardBorder;
        cardStyle.SetBorderWidthAll(1);
        cardStyle.SetCornerRadiusAll(12);
        // Give some internal breathing room.
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

        // Title label.
        var titleLabel = new Label();
        titleLabel.Text = "Story Event";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", ColourTitle);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddChild(titleLabel);

        // Body label.
        var bodyLabel = new Label();
        bodyLabel.Text = "A mysterious story unfolds...\n(Placeholder \u2014 real dialogue coming soon)";
        bodyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        bodyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        bodyLabel.AddThemeFontSizeOverride("font_size", 16);
        bodyLabel.AddThemeColorOverride("font_color", ColourBody);
        bodyLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddChild(bodyLabel);

        // Continue button (inside card, centered horizontally).
        var continueBtn = new Button();
        continueBtn.Text = "Continue";
        continueBtn.CustomMinimumSize = new Vector2(150, 40);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.Pressed += OnDismissed;
        vbox.AddChild(continueBtn);

        // 5. Skip button — top-right corner of the screen (not inside the card).
        var skipBtn = new Button();
        skipBtn.Text = "Skip";
        skipBtn.Flat = true;
        skipBtn.FocusMode = FocusModeEnum.None;
        skipBtn.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        skipBtn.OffsetLeft   = -80;
        skipBtn.OffsetTop    =  12;
        skipBtn.OffsetRight  = -12;
        skipBtn.OffsetBottom =  44;

        skipBtn.AddThemeColorOverride("font_color",       ColourSkip);
        skipBtn.AddThemeColorOverride("font_hover_color", ColourSkipHover);
        skipBtn.AddThemeStyleboxOverride("normal",  new StyleBoxEmpty());
        skipBtn.AddThemeStyleboxOverride("hover",   new StyleBoxEmpty());
        skipBtn.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        skipBtn.AddThemeStyleboxOverride("focus",   new StyleBoxEmpty());

        skipBtn.Pressed += OnDismissed;
        AddChild(skipBtn);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnDismissed()
    {
        EmitSignal(SignalName.Dismissed);
    }
}

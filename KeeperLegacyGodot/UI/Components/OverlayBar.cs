using Godot;
using System;

namespace KeeperLegacy.UI.Components;

public partial class OverlayBar : PanelContainer
{
    [Signal]
    public delegate void BackPressedEventHandler();

    [Export]
    public string Title { get; set; } = "";

    [Export]
    public bool ShowBackButton { get; set; } = true;

    [Export]
    public bool ShowCoins { get; set; } = true;

    private Label _coinLabel;

    public override void _Ready()
    {
        // Set up the panel background style
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.047f, 0.035f, 0.027f, 0.8f);
        styleBox.ContentMarginLeft = 12;
        styleBox.ContentMarginRight = 12;
        styleBox.ContentMarginTop = 8;
        styleBox.ContentMarginBottom = 8;

        var theme = new Theme();
        theme.SetStylebox("panel", "PanelContainer", styleBox);
        Theme = theme;

        // Create the main horizontal box container
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        AddChild(hbox);

        // Add back button if enabled
        if (ShowBackButton)
        {
            var backButton = new Button();
            backButton.Text = "◀ Back";
            backButton.Flat = true;
            backButton.AddThemeColorOverride("font_color", Color.Color8(184, 160, 128));
            backButton.AddThemeColorOverride("font_hover_color", Color.Color8(232, 184, 48));
            backButton.AddThemeFontSizeOverride("font_size", 14);
            backButton.Pressed += OnBackPressed;
            hbox.AddChild(backButton);

            // Add separator after back button
            var separator = new VSeparator();
            hbox.AddChild(separator);
        }

        // Add title label
        var titleLabel = new Label();
        titleLabel.Text = Title;
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleLabel.AddThemeColorOverride("font_color", Color.Color8(240, 232, 216));
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        hbox.AddChild(titleLabel);

        // Add coin display if enabled
        if (ShowCoins)
        {
            _coinLabel = new Label();
            _coinLabel.HorizontalAlignment = HorizontalAlignment.Right;
            _coinLabel.AddThemeColorOverride("font_color", Color.Color8(232, 184, 48));
            _coinLabel.AddThemeFontSizeOverride("font_size", 14);
            hbox.AddChild(_coinLabel);

            UpdateCoinDisplay();

            // Subscribe to coin changes
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            if (pm != null)
            {
                pm.CoinsChanged += (_) => UpdateCoinDisplay();
            }
        }
    }

    private void OnBackPressed()
    {
        EmitSignal(SignalName.BackPressed);
    }

    private void UpdateCoinDisplay()
    {
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (pm != null && _coinLabel != null)
            _coinLabel.Text = $"✦ {pm.Coins}";
        else if (_coinLabel != null)
            _coinLabel.Text = "✦ ---";
    }
}

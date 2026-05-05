// UI/Habitat/HabitatOverlayBar.cs
// Top translucent bar — back, biome info, capacity pill, coins.

using Godot;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatOverlayBar : PanelContainer
    {
        [Signal] public delegate void BackPressedEventHandler();

        private BiomeTheme? _theme;
        private Label _nameLabel;
        private Label _subtitleLabel;
        private Label _capacityPill;
        private Label _coinsLabel;
        private Label _iconLabel;

        public override void _Ready()
        {
            var style = new StyleBoxFlat();
            style.BgColor           = HabitatPalette.OverlayBarBg;
            style.BorderWidthBottom = 1;
            style.BorderColor       = HabitatPalette.OverlayBarBorderTint;
            style.ContentMarginLeft   = 16;
            style.ContentMarginRight  = 16;
            style.ContentMarginTop    = 4;
            style.ContentMarginBottom = 4;
            AddThemeStyleboxOverride("panel", style);
            CustomMinimumSize = new Vector2(0, 44);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 12);
            hbox.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            AddChild(hbox);

            // Back button
            var back = new Button();
            back.Text = "◀ The Shop";
            back.Flat = true;
            back.FocusMode = FocusModeEnum.None;
            back.AddThemeFontSizeOverride("font_size", 11);
            back.AddThemeColorOverride("font_color",       HabitatPalette.BackButtonText);
            back.AddThemeColorOverride("font_hover_color", HabitatPalette.BackButtonHover);
            back.Pressed += () => EmitSignal(SignalName.BackPressed);
            hbox.AddChild(back);

            // Separator
            var sep = new ColorRect();
            sep.Color = HabitatPalette.SeparatorLine;
            sep.CustomMinimumSize = new Vector2(1, 24);
            hbox.AddChild(sep);

            // Biome icon + info
            _iconLabel = new Label();
            _iconLabel.AddThemeFontSizeOverride("font_size", 18);
            hbox.AddChild(_iconLabel);

            var infoBox = new VBoxContainer();
            infoBox.AddThemeConstantOverride("separation", 0);
            infoBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hbox.AddChild(infoBox);

            _nameLabel = new Label();
            _nameLabel.AddThemeFontSizeOverride("font_size", 14);
            infoBox.AddChild(_nameLabel);

            _subtitleLabel = new Label();
            _subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
            _subtitleLabel.AddThemeColorOverride("font_color", HabitatPalette.LabelMuted);
            infoBox.AddChild(_subtitleLabel);

            // Capacity pill
            _capacityPill = new Label();
            _capacityPill.AddThemeFontSizeOverride("font_size", 10);
            hbox.AddChild(_capacityPill);

            // Coins
            _coinsLabel = new Label();
            _coinsLabel.AddThemeFontSizeOverride("font_size", 11);
            _coinsLabel.AddThemeColorOverride("font_color", HabitatPalette.CoinsText);
            hbox.AddChild(_coinsLabel);
        }

        public void SetTheme(BiomeTheme theme)
        {
            _theme = theme;
            _iconLabel.Text     = theme.IconEmoji;
            _nameLabel.Text     = theme.DisplayName;
            _subtitleLabel.Text = theme.FlavorSubtitle;
            _nameLabel.AddThemeColorOverride("font_color", theme.AccentColor);
            _capacityPill.AddThemeColorOverride("font_color", theme.AccentColor);
        }

        public void SetCapacityText(int totalCreatures, int maxIfAllUnlocked)
            => _capacityPill.Text = $"{totalCreatures} / {maxIfAllUnlocked}";

        public void SetCoinsText(int coins)
            => _coinsLabel.Text = $"✦ {coins:N0}";
    }
}

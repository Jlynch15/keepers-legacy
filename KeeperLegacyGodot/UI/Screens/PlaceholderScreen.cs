using Godot;
using System;

namespace KeeperLegacy.UI.Screens;

/// <summary>
/// A reusable placeholder screen base class that displays a screen name and subtitle
/// on a warm cabin-colored background. Used for screens that will be implemented later.
/// </summary>
public partial class PlaceholderScreen : Control
{
	[Export]
	public string ScreenTitle { get; set; } = "Screen";

	[Export]
	public Color BackgroundColor { get; set; } = new Color(0.165f, 0.118f, 0.063f, 1f);

	public override void _Ready()
	{
		// Fill parent container
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Add background ColorRect
		var background = new ColorRect();
		background.Color = BackgroundColor;
		background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(background);

		// Add main title Label
		var titleLabel = new Label();
		titleLabel.Text = ScreenTitle;
		titleLabel.AddThemeStyleOverride("normal", new StyleBox());
		var titleFont = GD.Load<FontFile>("res://Assets/Fonts/inter_semi_bold.ttf");
		titleLabel.AddThemeFontOverride("font", titleFont);
		titleLabel.AddThemeFontSizeOverride("font_size", 32);
		titleLabel.AddThemeColorOverride("font_color", new Color(0.941f, 0.910f, 0.847f, 1f)); // #F0E8D8

		// Center title label
		titleLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		titleLabel.CustomMinimumSize = new Vector2(400, 100);
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.VerticalAlignment = VerticalAlignment.Center;

		AddChild(titleLabel);

		// Add subtitle Label
		var subtitleLabel = new Label();
		subtitleLabel.Text = "(Placeholder — real content coming soon)";
		var subtitleFont = GD.Load<FontFile>("res://Assets/Fonts/inter_regular.ttf");
		subtitleLabel.AddThemeFontOverride("font", subtitleFont);
		subtitleLabel.AddThemeFontSizeOverride("font_size", 14);
		subtitleLabel.AddThemeColorOverride("font_color", new Color(0.604f, 0.502f, 0.439f, 1f)); // #9A8070

		// Position subtitle below title (50px offset from center)
		subtitleLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomCenter);
		subtitleLabel.OffsetTop = -50;
		subtitleLabel.CustomMinimumSize = new Vector2(400, 50);
		subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		subtitleLabel.VerticalAlignment = VerticalAlignment.Center;

		AddChild(subtitleLabel);
	}
}

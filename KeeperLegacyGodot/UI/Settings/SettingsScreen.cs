using Godot;
using System;

namespace KeeperLegacy.UI.Settings;

/// <summary>
/// Settings screen with resolution picker and fullscreen toggle.
/// Provides real functionality for adjusting display settings in Steam builds.
/// </summary>
public partial class SettingsScreen : Control
{
	private static readonly (string Label, Vector2I Size)[] Resolutions = new[]
	{
		("1280 x 720 (720p)", new Vector2I(1280, 720)),
		("1920 x 1080 (1080p)", new Vector2I(1920, 1080)),
		("2560 x 1440 (1440p)", new Vector2I(2560, 1440)),
	};

	private OptionButton _resolutionDropdown;
	private CheckButton _fullscreenToggle;

	public override void _Ready()
	{
		// Fill parent container
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Add background ColorRect
		var background = new ColorRect();
		background.Color = new Color(0.165f, 0.118f, 0.063f, 1f); // #2A1E10
		background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(background);

		// Create centered VBoxContainer for settings
		var container = new VBoxContainer();
		container.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		container.CustomMinimumSize = new Vector2(400, 300);
		container.Separation = 20;
		container.HorizontalAlignment = HAlignment.Center;
		AddChild(container);

		// Add "Settings" title label
		var titleLabel = new Label();
		titleLabel.Text = "Settings";
		var titleFont = GD.Load<FontFile>("res://Assets/Fonts/inter_semi_bold.ttf");
		titleLabel.AddThemeFontOverride("font", titleFont);
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		titleLabel.AddThemeColorOverride("font_color", new Color(0.941f, 0.910f, 0.847f, 1f)); // #F0E8D8
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeStyleOverride("normal", new StyleBox());
		container.AddChild(titleLabel);

		// Add "Resolution" section label
		var resolutionLabel = new Label();
		resolutionLabel.Text = "Resolution";
		var sectionFont = GD.Load<FontFile>("res://Assets/Fonts/inter_regular.ttf");
		resolutionLabel.AddThemeFontOverride("font", sectionFont);
		resolutionLabel.AddThemeFontSizeOverride("font_size", 16);
		resolutionLabel.AddThemeColorOverride("font_color", new Color(0.722f, 0.627f, 0.502f, 1f)); // #B8A080
		resolutionLabel.AddThemeStyleOverride("normal", new StyleBox());
		container.AddChild(resolutionLabel);

		// Add resolution dropdown
		_resolutionDropdown = new OptionButton();
		_resolutionDropdown.CustomMinimumSize = new Vector2(300, 40);
		for (int i = 0; i < Resolutions.Length; i++)
		{
			_resolutionDropdown.AddItem(Resolutions[i].Label, i);
		}

		// Pre-select current window size
		var currentSize = DisplayServer.WindowGetSize();
		int currentIndex = 0;
		for (int i = 0; i < Resolutions.Length; i++)
		{
			if (Resolutions[i].Size == currentSize)
			{
				currentIndex = i;
				break;
			}
		}
		_resolutionDropdown.Selected = currentIndex;

		_resolutionDropdown.ItemSelected += OnResolutionSelected;
		container.AddChild(_resolutionDropdown);

		// Add fullscreen toggle
		_fullscreenToggle = new CheckButton();
		_fullscreenToggle.Text = "Fullscreen";
		_fullscreenToggle.AddThemeColorOverride("font_color", new Color(0.722f, 0.627f, 0.502f, 1f)); // #B8A080
		_fullscreenToggle.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		container.AddChild(_fullscreenToggle);
	}

	private void OnResolutionSelected(long index)
	{
		var size = Resolutions[(int)index].Size;
		DisplayServer.WindowSetSize(size);
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

using Godot;

namespace KeeperLegacy.UI.Settings;

/// <summary>
/// Settings screen with resolution picker and fullscreen toggle.
/// Resolution changes only work in exported builds or when running
/// the project standalone (not inside the Godot editor's embedded window).
/// </summary>
public partial class SettingsScreen : Control
{
	private static readonly (string Label, Vector2I Size)[] Resolutions = new[]
	{
		("1280 x 720 (720p)", new Vector2I(1280, 720)),
		("1920 x 1080 (1080p)", new Vector2I(1920, 1080)),
		("2560 x 1440 (1440p)", new Vector2I(2560, 1440)),
	};

	private OptionButton _resolutionDropdown = null!;
	private CheckButton _fullscreenToggle = null!;
	private Label _statusLabel = null!;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		// Background
		var background = new ColorRect();
		background.Color = new Color("#2A1E10");
		background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(background);

		// Centered container
		var container = new VBoxContainer();
		container.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		container.CustomMinimumSize = new Vector2(400, 300);
		container.AddThemeConstantOverride("separation", 20);
		AddChild(container);

		// Title
		var titleLabel = new Label();
		titleLabel.Text = "Settings";
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		titleLabel.AddThemeColorOverride("font_color", new Color("#F0E8D8"));
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		container.AddChild(titleLabel);

		// Resolution section
		var resolutionLabel = new Label();
		resolutionLabel.Text = "Resolution";
		resolutionLabel.AddThemeFontSizeOverride("font_size", 16);
		resolutionLabel.AddThemeColorOverride("font_color", new Color("#B8A080"));
		container.AddChild(resolutionLabel);

		// Dropdown
		_resolutionDropdown = new OptionButton();
		_resolutionDropdown.CustomMinimumSize = new Vector2(300, 40);
		for (int i = 0; i < Resolutions.Length; i++)
		{
			_resolutionDropdown.AddItem(Resolutions[i].Label, i);
		}

		// Pre-select current resolution
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

		// Fullscreen toggle
		_fullscreenToggle = new CheckButton();
		_fullscreenToggle.Text = "Fullscreen";
		_fullscreenToggle.AddThemeColorOverride("font_color", new Color("#B8A080"));
		_fullscreenToggle.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		container.AddChild(_fullscreenToggle);

		// Status label for feedback
		_statusLabel = new Label();
		_statusLabel.AddThemeFontSizeOverride("font_size", 12);
		_statusLabel.AddThemeColorOverride("font_color", new Color("#9A8070"));
		_statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		container.AddChild(_statusLabel);

		if (OS.HasFeature("editor"))
		{
			_statusLabel.Text = "(Resolution changes work in exported builds, not the editor)";
		}
	}

	private void OnResolutionSelected(long index)
	{
		if (OS.HasFeature("editor"))
		{
			_statusLabel.Text = $"Resolution set to {Resolutions[(int)index].Label} (applies in exported build)";
			return;
		}

		var size = Resolutions[(int)index].Size;
		DisplayServer.WindowSetSize(size);
		var screenSize = DisplayServer.ScreenGetSize();
		var pos = (screenSize - size) / 2;
		DisplayServer.WindowSetPosition(pos);
		_statusLabel.Text = $"Resolution changed to {Resolutions[(int)index].Label}";
	}

	private void OnFullscreenToggled(bool toggled)
	{
		if (OS.HasFeature("editor"))
		{
			_statusLabel.Text = $"Fullscreen {(toggled ? "on" : "off")} (applies in exported build)";
			return;
		}

		DisplayServer.WindowSetMode(
			toggled ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed
		);
	}
}

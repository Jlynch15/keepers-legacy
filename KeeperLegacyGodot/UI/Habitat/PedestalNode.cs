// UI/Habitat/PedestalNode.cs
// Reusable component representing one pedestal hotspot on the Habitat Floor.
// Draws creature blobs with idle bob animation and emits PedestalClicked on tap.

using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Models;

namespace KeeperLegacy.UI.Habitat;

public partial class PedestalNode : Control
{
    // ── Signal ────────────────────────────────────────────────────────────────

    [Signal] public delegate void PedestalClickedEventHandler(int habitatType);

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color ColourWater    = new Color("#4AA8E0");
    private static readonly Color ColourGrass    = new Color("#4AB84A");
    private static readonly Color ColourDirt     = new Color("#C08840");
    private static readonly Color ColourFire     = new Color("#E06030");
    private static readonly Color ColourIce      = new Color("#60D0E0");
    private static readonly Color ColourElectric = new Color("#E8D020");
    private static readonly Color ColourMagical  = new Color("#9860E0");

    private static readonly Color ColourLabelName  = new Color("#F0E8D8");
    private static readonly Color ColourLabelCount = new Color("#B8A080");
    private static readonly Color ColourLocked     = new Color("#606060");

    // ── Debug drag mode ─────────────────────────────────────────────────────

    public static bool DebugDragEnabled { get; set; }
    public static float DebugScale { get; set; } = 1.0f;
    // Offset of the art image relative to the node position (tunable)
    public static Vector2 DebugArtOffset { get; set; } = Vector2.Zero;
    private bool _dragging;
    private Vector2 _dragOffset;

    // ── State ─────────────────────────────────────────────────────────────────

    private HabitatType _habitatType;
    private bool _locked;
    private List<(string name, HabitatType type)> _occupants = new();

    // Pedestal art base sizing (536x466 source, scaled down to fit room)
    // Actual display size = base * DebugScale
    private const float BasePedestalWidth  = 55f;
    private const float BasePedestalHeight = 48f; // 466/536 * 55
    private float PedestalWidth  => BasePedestalWidth * DebugScale;
    private float PedestalHeight => BasePedestalHeight * DebugScale;

    private static readonly Dictionary<HabitatType, string> PedestalTexturePaths = new()
    {
        [HabitatType.Water]    = "res://Sprites/Pedstals/pedestal_water.png",
        [HabitatType.Grass]    = "res://Sprites/Pedstals/pedestal_grass.png",
        [HabitatType.Dirt]     = "res://Sprites/Pedstals/pedestal_dirt.png",
        [HabitatType.Fire]     = "res://Sprites/Pedstals/pedestal_fire.png",
        [HabitatType.Ice]      = "res://Sprites/Pedstals/pedestal_ice.png",
        [HabitatType.Electric] = "res://Sprites/Pedstals/pedestal_electric.png",
        [HabitatType.Magical]  = "res://Sprites/Pedstals/pedestal_magical.png",
    };

    // Animation
    private float _time;
    private readonly List<float> _phaseOffsets = new();
    private const float BobAmplitude = 4f;
    private const float BobPeriod    = 2.5f;

    // Child references
    private Button    _hitButton;
    private Label     _nameLabel;
    private Label     _countLabel;
    private TextureRect _texRect;

    // Blob drawing area
    private Control   _blobArea;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Setup(HabitatType type, Vector2 center, bool locked,
                      List<(string creatureName, HabitatType creatureType)> occupants)
    {
        _habitatType = type;
        _locked      = locked;
        _occupants   = occupants ?? new List<(string, HabitatType)>();

        // Rebuild phase offsets for each occupant slot
        _phaseOffsets.Clear();
        var rng = new Random((int)(type.GetHashCode()));
        foreach (var _ in _occupants)
            _phaseOffsets.Add((float)(rng.NextDouble() * Math.PI * 2.0));

        // Position the hotspot — sized to fit pedestal art + label below
        // Art is 1104x960, scaled to PedestalWidth wide
        Size             = new Vector2(PedestalWidth, PedestalHeight + 30); // +30 for label
        // Anchor so the center of the pedestal top face aligns with the coordinate
        Position         = center - new Vector2(PedestalWidth / 2f, 0);
        MouseFilter      = MouseFilterEnum.Stop; // Ensure we receive _GuiInput

        BuildChildren();
        RefreshDisplay();
    }

    public void UpdateCreatures(List<(string creatureName, HabitatType creatureType)> occupants)
    {
        _occupants = occupants ?? new List<(string, HabitatType)>();

        _phaseOffsets.Clear();
        var rng = new Random((int)(_habitatType.GetHashCode() + _occupants.Count));
        foreach (var _ in _occupants)
            _phaseOffsets.Add((float)(rng.NextDouble() * Math.PI * 2.0));

        RefreshDisplay();
        _blobArea?.QueueRedraw();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        // Nothing — Setup() must be called after adding to the tree.
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _blobArea?.QueueRedraw();

        // Toggle button passthrough when drag mode changes
        if (_hitButton != null)
        {
            _hitButton.MouseFilter = DebugDragEnabled
                ? MouseFilterEnum.Ignore
                : MouseFilterEnum.Stop;
        }
    }

    // ── Debug drag handling ──────────────────────────────────────────────────

    public override void _GuiInput(InputEvent @event)
    {
        if (!DebugDragEnabled) return;

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    _dragging = true;
                    _dragOffset = mb.Position;
                    AcceptEvent();
                }
                else
                {
                    _dragging = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            Position += mm.Relative;
            AcceptEvent();
        }
    }

    /// <summary>
    /// Rebuild all pedestal children to reflect new scale.
    /// Called on any pedestal but rebuilds siblings too via tree walk.
    /// </summary>

    /// <summary>
    /// Returns the center position of this pedestal (for coordinate export).
    /// </summary>
    public Vector2 GetCenter() => Position + Size / 2f;

    public HabitatType GetHabitatType() => _habitatType;
    public float GetPedestalWidth() => PedestalWidth;
    public float GetPedestalHeight() => PedestalHeight;

    /// <summary>
    /// Update art position and size without rebuilding. Used by debug tools.
    /// </summary>
    public void ApplyDebugVisuals()
    {
        if (_texRect != null)
        {
            _texRect.Position = DebugArtOffset;
            _texRect.Size = new Vector2(PedestalWidth, PedestalHeight);
        }
    }

    // ── Child construction ────────────────────────────────────────────────────

    internal void BuildChildren()
    {
        // Remove any previous children
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        // Pedestal art texture — scale 1104x960 source down to PedestalWidth
        if (PedestalTexturePaths.TryGetValue(_habitatType, out var texPath))
        {
            var tex = GD.Load<Texture2D>(texPath);
            if (tex != null)
            {
                _texRect = new TextureRect();
                _texRect.Texture = tex;
                _texRect.Position = DebugArtOffset;
                _texRect.Size = new Vector2(PedestalWidth, PedestalHeight);
                _texRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                _texRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                _texRect.MouseFilter = MouseFilterEnum.Ignore;
                AddChild(_texRect);
            }
        }

        // Invisible full-size button as the click target (over the pedestal art)
        _hitButton = new Button();
        _hitButton.Position = Vector2.Zero;
        _hitButton.Size = new Vector2(PedestalWidth, PedestalHeight);
        _hitButton.Flat = true;
        _hitButton.FocusMode = FocusModeEnum.None;
        _hitButton.AddThemeStyleboxOverride("normal",   new StyleBoxEmpty());
        _hitButton.AddThemeStyleboxOverride("hover",    new StyleBoxEmpty());
        _hitButton.AddThemeStyleboxOverride("pressed",  new StyleBoxEmpty());
        _hitButton.AddThemeStyleboxOverride("focus",    new StyleBoxEmpty());
        _hitButton.Pressed += OnButtonPressed;
        AddChild(_hitButton);

        // Blob drawing area — sits on top of the pedestal art (upper third)
        _blobArea = new Control();
        _blobArea.Position              = new Vector2(0, PedestalHeight * 0.1f);
        _blobArea.Size                  = new Vector2(PedestalWidth, PedestalHeight * 0.45f);
        _blobArea.MouseFilter           = MouseFilterEnum.Ignore;
        _blobArea.Draw                  += OnBlobAreaDraw;
        AddChild(_blobArea);

        // Name label — below the pedestal art
        _nameLabel = new Label();
        _nameLabel.Position              = new Vector2(0, PedestalHeight + 2);
        _nameLabel.Size                  = new Vector2(PedestalWidth, 16);
        _nameLabel.HorizontalAlignment   = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        _nameLabel.AddThemeColorOverride("font_color", ColourLabelName);
        _nameLabel.MouseFilter           = MouseFilterEnum.Ignore;
        AddChild(_nameLabel);

        // Count / locked label — one row below name
        _countLabel = new Label();
        _countLabel.Position              = new Vector2(0, PedestalHeight + 16);
        _countLabel.Size                  = new Vector2(PedestalWidth, 14);
        _countLabel.HorizontalAlignment   = HorizontalAlignment.Center;
        _countLabel.AddThemeFontSizeOverride("font_size", 9);
        _countLabel.AddThemeColorOverride("font_color", ColourLabelCount);
        _countLabel.MouseFilter           = MouseFilterEnum.Ignore;
        AddChild(_countLabel);
    }

    internal void RefreshDisplay()
    {
        if (_locked)
        {
            Modulate          = new Color(1, 1, 1, 0.6f);
            _hitButton.Disabled = true;
            _nameLabel.Text   = "\ud83d\udd12 Locked";
            _nameLabel.AddThemeColorOverride("font_color", ColourLocked);
            _countLabel.Text  = _habitatType.ToString();
        }
        else
        {
            Modulate          = Colors.White;
            _hitButton.Disabled = false;
            _nameLabel.Text   = _habitatType.ToString();
            _nameLabel.AddThemeColorOverride("font_color", ColourLabelName);
            _countLabel.Text  = _occupants.Count == 0
                ? "Empty"
                : _occupants.Count == 1 ? "1 creature" : $"{_occupants.Count} creatures";
        }
    }

    // ── Custom draw — creature blobs ──────────────────────────────────────────

    private void OnBlobAreaDraw()
    {
        if (_locked || _occupants.Count == 0) return;

        const float BlobDiameter = 32f;
        const float BlobRadius   = BlobDiameter / 2f;
        int count = _occupants.Count;

        // Distribute blobs horizontally across the 160px area
        float spacing = Mathf.Min(BlobDiameter + 8f, 140f / Mathf.Max(count, 1));
        float totalWidth = spacing * (count - 1) + BlobDiameter;
        float startX = (160f - totalWidth) / 2f + BlobRadius;

        for (int i = 0; i < count; i++)
        {
            float phase     = i < _phaseOffsets.Count ? _phaseOffsets[i] : 0f;
            float bobOffset = BobAmplitude * Mathf.Sin(_time * (Mathf.Tau / BobPeriod) + phase);

            float cx = startX + i * spacing;
            float cy = BlobRadius + 4f + bobOffset;

            var pos    = new Vector2(cx, cy);
            Color fill = GetTypeColor(_occupants[i].type);

            // Draw filled circle (approximate with DrawCircle)
            _blobArea.DrawCircle(pos, BlobRadius, fill);

            // Draw initial letter centered in circle
            string letter = string.IsNullOrEmpty(_occupants[i].name)
                ? "?"
                : _occupants[i].name.Substring(0, 1).ToUpper();

            // We can't easily query font metrics, so offset manually
            _blobArea.DrawString(
                ThemeDB.FallbackFont,
                pos + new Vector2(-5f, 5f),
                letter,
                HorizontalAlignment.Left,
                -1,
                14,
                Colors.White
            );
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Color GetTypeColor(HabitatType type) => type switch
    {
        HabitatType.Water    => ColourWater,
        HabitatType.Grass    => ColourGrass,
        HabitatType.Dirt     => ColourDirt,
        HabitatType.Fire     => ColourFire,
        HabitatType.Ice      => ColourIce,
        HabitatType.Electric => ColourElectric,
        HabitatType.Magical  => ColourMagical,
        _                    => Colors.Gray
    };

    private void OnButtonPressed()
    {
        EmitSignal(SignalName.PedestalClicked, (int)_habitatType);
    }
}

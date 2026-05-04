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

    // ── Debug tuning state (global, applied live to all pedestals) ──────────
    // Default 1.0 / Zero means "use baked constants as-is". When tuning, these
    // multiply onto the baked constants. When happy, Ctrl+S prints paste-ready
    // values; the baked constants are updated to absorb the multiplier and
    // DebugScale resets to 1.0. This guarantees what-you-see is what-gets-baked.

    public static bool DebugDragEnabled { get; set; }
    public static float DebugScale { get; set; } = 1.0f;
    public static Vector2 DebugArtOffset { get; set; } = Vector2.Zero;
    private bool _dragging;
    private Vector2 _dragOffset;

    // Last applied debug values — used to detect changes in _Process and re-apply layout
    private float _appliedScale = float.NaN;
    private Vector2 _appliedOffset = new Vector2(float.NaN, float.NaN);

    // The horizontal-center, top-edge anchor point of this pedestal in viewport coords.
    // Drag updates this; scale changes recompute Position from it.
    private Vector2 _viewportAnchor;

    // ── State ─────────────────────────────────────────────────────────────────

    private HabitatType _habitatType;
    private bool _locked;
    private List<(string name, HabitatType type)> _occupants = new();

    // Pedestal art display size — confirmed by Jesse at 4.0x scale
    private const float ArtWidth  = 220f;
    private const float ArtHeight = 192f;
    // Baked art offset: the art image is shifted left so the pedestal top-center
    // aligns with the coordinate point
    private const float ArtOffsetX = -78f;
    private const float ArtOffsetY = 0f;

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
    private Button         _hitButton;
    private Label          _nameLabel;
    private Label          _countLabel;
    private TextureRect    _texRect;
    private PanelContainer _labelGroup;     // Pill background wrapping both labels

    // Blob drawing area
    private Control   _blobArea;

    // Label pill styling
    private static readonly Color ColourLabelPillBg = new Color(0.10f, 0.08f, 0.06f, 0.72f);

    // ── Public API ────────────────────────────────────────────────────────────

    public void Setup(HabitatType type, Vector2 viewportCenter, bool locked,
                      List<(string creatureName, HabitatType creatureType)> occupants)
    {
        _habitatType = type;
        _locked      = locked;
        _occupants   = occupants ?? new List<(string, HabitatType)>();
        _viewportAnchor = viewportCenter;

        // Rebuild phase offsets for each occupant slot
        _phaseOffsets.Clear();
        var rng = new Random((int)(type.GetHashCode()));
        foreach (var _ in _occupants)
            _phaseOffsets.Add((float)(rng.NextDouble() * Math.PI * 2.0));

        MouseFilter = MouseFilterEnum.Stop;

        BuildChildren();
        RefreshDisplay();
        ApplyDebugLayout();
    }

    public void UpdateCreatures(List<(string creatureName, HabitatType creatureType)> occupants)
    {
        _occupants = occupants ?? new List<(string, HabitatType)>();

        _phaseOffsets.Clear();
        var rng = new Random((int)(_habitatType.GetHashCode() + _occupants.Count));
        foreach (var _ in _occupants)
            _phaseOffsets.Add((float)(rng.NextDouble() * Math.PI * 2.0));

        RefreshDisplay();
        ApplyDebugLayout();   // text width changed — re-center the pill
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

        // React to global debug-tuning changes (DebugScale or DebugArtOffset edited
        // by HabitatFloorScreen's input handler). Cheap equality check; only
        // re-applies when something actually changed.
        if (DebugScale != _appliedScale || DebugArtOffset != _appliedOffset)
        {
            ApplyDebugLayout();
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
            // Move the node and its viewport anchor in lock-step so subsequent
            // scale changes don't snap us back to the original position.
            Position += mm.Relative;
            _viewportAnchor += mm.Relative;
            AcceptEvent();
        }
    }

    /// <summary>
    /// Returns the geometric center of this pedestal node in viewport coords.
    /// Note: this is NOT the same as the anchor — the node extends below the
    /// art for the labels. Use GetViewportAnchor() to recover the original
    /// "top-center of art" position used during Setup.
    /// </summary>
    public Vector2 GetCenter() => Position + Size / 2f;

    /// <summary>
    /// The anchor point the pedestal was placed against — top-center of the art
    /// in viewport coords. Updated by drag. Read by HabitatFloorScreen when
    /// printing bake-ready values.
    /// </summary>
    public Vector2 GetViewportAnchor() => _viewportAnchor;

    public HabitatType GetHabitatType() => _habitatType;
    public float GetPedestalWidth() => ArtWidth;
    public float GetPedestalHeight() => ArtHeight;

    // Read-only accessors so the bake-print routine in HabitatFloorScreen
    // doesn't have to duplicate (and risk de-syncing) these constants.
    public static float GetBakedArtWidth()   => ArtWidth;
    public static float GetBakedArtHeight()  => ArtHeight;
    public static float GetBakedArtOffsetX() => ArtOffsetX;
    public static float GetBakedArtOffsetY() => ArtOffsetY;

    /// <summary>
    /// Compute and apply node Size, Position, and all child layouts based on
    /// the current debug values (DebugScale, DebugArtOffset) and the stored
    /// anchor. This is the single source of truth for pedestal layout — calling
    /// it with DebugScale=1.0 and DebugArtOffset=Zero produces the visual that
    /// matches the baked constants exactly.
    /// </summary>
    public void ApplyDebugLayout()
    {
        float s = DebugScale;
        float w = ArtWidth  * s;
        float h = ArtHeight * s;

        // Node footprint = art footprint. Labels float above the node's rect;
        // Godot doesn't clip Control children so they render fine outside.
        Size = new Vector2(w, h);

        // Position the node so the art's horizontal center sits on _viewportAnchor.X
        // and the art's top sits on _viewportAnchor.Y. ArtOffsetX scales with the art.
        Position = _viewportAnchor - new Vector2(w / 2f + ArtOffsetX * s, 0f);

        // Where the art's top-left lands inside the node, in node-local coords.
        // Hit button, blobs, and labels all anchor to the art (not the node origin)
        // so they track wherever the art is — including when DebugArtOffset nudges it.
        Vector2 artOrigin = new Vector2(ArtOffsetX * s, ArtOffsetY * s) + DebugArtOffset;

        if (_texRect != null)
        {
            _texRect.Position = artOrigin;
            _texRect.Size     = new Vector2(w, h);
        }
        if (_hitButton != null)
        {
            _hitButton.Position = artOrigin;
            _hitButton.Size     = new Vector2(w, h);
        }
        if (_blobArea != null)
        {
            _blobArea.Position = artOrigin + new Vector2(0, h * 0.1f);
            _blobArea.Size     = new Vector2(w, h * 0.45f);
        }
        // Label pill — sized to fit its text content, centered horizontally on the
        // art, floating above the pedestal's top vertex. We force a layout pass so
        // GetMinimumSize() reflects the current text widths (which can change when
        // occupants update — "Empty" vs "3 creatures").
        if (_labelGroup != null)
        {
            _labelGroup.Size = Vector2.Zero;            // let it shrink to min
            _labelGroup.UpdateMinimumSize();
            Vector2 pillSize = _labelGroup.GetCombinedMinimumSize();
            _labelGroup.Size = pillSize;

            const float gapAboveArt = 6f;
            _labelGroup.Position = artOrigin + new Vector2(
                (w - pillSize.X) / 2f,
                -pillSize.Y - gapAboveArt * s
            );
        }

        _appliedScale  = s;
        _appliedOffset = DebugArtOffset;
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

        // Pedestal art texture — scale 1104x960 source down to ArtWidth
        if (PedestalTexturePaths.TryGetValue(_habitatType, out var texPath))
        {
            var tex = GD.Load<Texture2D>(texPath);
            if (tex != null)
            {
                _texRect = new TextureRect();
                _texRect.Texture = tex;
                _texRect.Position = new Vector2(ArtOffsetX, ArtOffsetY) + DebugArtOffset;
                _texRect.Size = new Vector2(ArtWidth, ArtHeight);
                _texRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                _texRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                _texRect.MouseFilter = MouseFilterEnum.Ignore;
                AddChild(_texRect);
            }
        }

        // Invisible full-size button as the click target (over the pedestal art)
        _hitButton = new Button();
        _hitButton.Position = Vector2.Zero;
        _hitButton.Size = new Vector2(ArtWidth, ArtHeight);
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
        _blobArea.Position              = new Vector2(0, ArtHeight * 0.1f);
        _blobArea.Size                  = new Vector2(ArtWidth, ArtHeight * 0.45f);
        _blobArea.MouseFilter           = MouseFilterEnum.Ignore;
        _blobArea.Draw                  += OnBlobAreaDraw;
        AddChild(_blobArea);

        // Label group — dark translucent pill behind name + status text, floating
        // above the pedestal. PanelContainer auto-sizes to its child VBoxContainer,
        // which auto-sizes to its labels' text — so the pill always wraps tightly.
        _labelGroup = new PanelContainer();
        _labelGroup.MouseFilter = MouseFilterEnum.Ignore;

        var pillStyle = new StyleBoxFlat();
        pillStyle.BgColor             = ColourLabelPillBg;
        pillStyle.SetCornerRadiusAll(20);   // exceeds half-height -> fully pill
        pillStyle.ContentMarginLeft   = 10;
        pillStyle.ContentMarginRight  = 10;
        pillStyle.ContentMarginTop    = 3;
        pillStyle.ContentMarginBottom = 4;
        _labelGroup.AddThemeStyleboxOverride("panel", pillStyle);

        var labelBox = new VBoxContainer();
        labelBox.MouseFilter = MouseFilterEnum.Ignore;
        labelBox.AddThemeConstantOverride("separation", 0);
        _labelGroup.AddChild(labelBox);

        _nameLabel = new Label();
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        _nameLabel.AddThemeColorOverride("font_color", ColourLabelName);
        _nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        labelBox.AddChild(_nameLabel);

        _countLabel = new Label();
        _countLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _countLabel.AddThemeFontSizeOverride("font_size", 9);
        _countLabel.AddThemeColorOverride("font_color", ColourLabelCount);
        _countLabel.MouseFilter = MouseFilterEnum.Ignore;
        labelBox.AddChild(_countLabel);

        AddChild(_labelGroup);
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

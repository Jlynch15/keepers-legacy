// UI/Habitat/HabitatEnvironmentView.cs
// Biome-themed environment scene driven by a BiomeTheme record. Layered
// rendering: background -> light shafts -> particles -> decorations -> surface
// -> wander zone (debug) -> creatures -> floor.

using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Data;
using KeeperLegacy.Models;
using HabitatModel = KeeperLegacy.Models.Habitat;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatEnvironmentView : Control
    {
        // Reference dimensions (art-space) — same as HabitatFloorScreen.
        public const float ArtW = 1364f;
        public const float ArtH = 768f;

        [Signal] public delegate void CreatureClickedEventHandler(string creatureId);

        // ── State ─────────────────────────────────────────────────────────────

        private BiomeTheme? _theme;
        private HabitatModel? _habitat;
        private float _particleAccumulator;

        // Layer roots
        private ColorRect _bgGradient;
        private Control   _lightLayer;
        private Control   _particleLayer;
        private Control   _decorationLayer;
        private ColorRect _surfaceLine;
        private Control   _wanderZoneOverlay;     // debug only, populated in Task 14
        private Control   _creatureLayer;
        private ColorRect _floorOverlay;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetTheme(BiomeTheme theme)
        {
            _theme = theme;
            BuildLayers();
        }

        public void SetHabitat(HabitatModel habitat)
        {
            _habitat = habitat;
            BuildCreatures();
        }

        private void BuildCreatures()
        {
            foreach (Node child in _creatureLayer.GetChildren()) child.QueueFree();
            if (_habitat == null || _theme == null) return;

            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            foreach (Guid creatureId in _habitat.OccupantIds)
            {
                Texture2D? tex = TryLoadCreatureTexture(creatureId, hm);
                var node = new WanderingCreature(creatureId, _theme.AccentColor, _theme.WanderZone, tex);
                node.Tapped += () => EmitSignal(SignalName.CreatureClicked, creatureId.ToString());
                _creatureLayer.AddChild(node);
            }
        }

        private static Texture2D? TryLoadCreatureTexture(Guid creatureId, HabitatManager? hm)
        {
            var creature = hm?.GetCreature(creatureId);
            if (creature == null) return null;
            string svgPath = $"res://Sprites/Creatures/{creature.CatalogId}.svg";
            string pngPath = $"res://Sprites/Creatures/{creature.CatalogId}.png";
            if (ResourceLoader.Exists(svgPath)) return GD.Load<Texture2D>(svgPath);
            if (ResourceLoader.Exists(pngPath)) return GD.Load<Texture2D>(pngPath);
            return null;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void _Ready()
        {
            ClipContents = true;
            MouseFilter  = MouseFilterEnum.Pass;

            // Create empty layer roots up front so children render in z-order.
            _bgGradient = new ColorRect { Color = new Color(0, 0, 0) };
            _bgGradient.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _bgGradient.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_bgGradient);

            _lightLayer = NewLayer();
            _particleLayer = NewLayer();
            _decorationLayer = NewLayer();
            _surfaceLine = new ColorRect { Color = new Color(0, 0, 0, 0) };
            _surfaceLine.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
            _surfaceLine.OffsetBottom = 4;
            _surfaceLine.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_surfaceLine);

            _wanderZoneOverlay = NewLayer();
            _wanderZoneOverlay.Visible = false;

            _creatureLayer = NewLayer();
            _floorOverlay = new ColorRect();
            _floorOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            _floorOverlay.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_floorOverlay);
        }

        private Control NewLayer()
        {
            var c = new Control();
            c.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            c.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(c);
            return c;
        }

        public override void _Process(double delta)
        {
            if (_theme?.Particles == null) return;

            _particleAccumulator += (float)delta;
            float interval = 1f / Mathf.Max(_theme.Particles.EmitRatePerSec, 0.1f);
            while (_particleAccumulator >= interval)
            {
                _particleAccumulator -= interval;
                SpawnParticle(_theme.Particles);
            }
        }

        // ── Layer construction ────────────────────────────────────────────────

        private void BuildLayers()
        {
            if (_theme == null) return;

            ApplyBackgroundGradient(_theme.BackgroundTopColor, _theme.BackgroundBottomColor);
            BuildLightShafts();
            BuildDecorations();
            BuildSurface();
            BuildFloor();
            // Particles are spawned over time in _Process.
        }

        private void ApplyBackgroundGradient(Color top, Color bottom)
        {
            // For first pass: solid mid-color average. A custom Draw override or
            // shader can replace this later if a real gradient is required.
            _bgGradient.Color = top.Lerp(bottom, 0.5f);
        }

        private void BuildLightShafts()
        {
            foreach (Node child in _lightLayer.GetChildren()) child.QueueFree();
            if (_theme == null || _theme.AmbientLights.Length == 0) return;

            foreach (var shaft in _theme.AmbientLights)
            {
                var rect = new ColorRect();
                rect.Color = _theme.AccentColor with { A = shaft.Opacity * 0.30f };
                rect.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
                rect.OffsetLeft   = shaft.LeftPct * Size.X;
                rect.OffsetRight  = rect.OffsetLeft + shaft.WidthPx;
                rect.OffsetTop    = 0;
                rect.OffsetBottom = 0;
                rect.SetAnchor(Side.Bottom, 1.0f);
                rect.MouseFilter  = MouseFilterEnum.Ignore;
                rect.RotationDegrees = shaft.SkewDeg;
                _lightLayer.AddChild(rect);

                // Pulse animation
                var tween = CreateTween();
                tween.SetLoops();
                tween.TweenProperty(rect, "modulate:a", shaft.Opacity * 0.40f, shaft.PulseDurSec / 2f);
                tween.TweenProperty(rect, "modulate:a", shaft.Opacity * 0.20f, shaft.PulseDurSec / 2f);
            }
        }

        private void BuildDecorations()
        {
            foreach (Node child in _decorationLayer.GetChildren()) child.QueueFree();
            if (_theme == null) return;

            foreach (var dec in _theme.Decorations)
            {
                var node = new DecorationNode(dec);
                _decorationLayer.AddChild(node);
            }
        }

        private void BuildSurface()
        {
            if (_theme?.Surface == null)
            {
                _surfaceLine.Color = new Color(0, 0, 0, 0);
                return;
            }
            _surfaceLine.Color = _theme.Surface.MidColor;

            var tween = CreateTween();
            tween.SetLoops();
            tween.TweenProperty(_surfaceLine, "modulate:a", 1.0f, _theme.Surface.ShimmerDurSec / 2f);
            tween.TweenProperty(_surfaceLine, "modulate:a", 0.6f, _theme.Surface.ShimmerDurSec / 2f);
        }

        private void BuildFloor()
        {
            if (_theme?.Floor == null)
            {
                _floorOverlay.Color = new Color(0, 0, 0, 0);
                _floorOverlay.OffsetTop = 0;
                return;
            }
            _floorOverlay.Color = _theme.Floor.TintBottom;
            _floorOverlay.OffsetTop = -_theme.Floor.HeightPx;
        }

        private void SpawnParticle(ParticleConfig cfg)
        {
            var rng = new RandomNumberGenerator();
            rng.Randomize();

            var label = new Label();
            label.Text = cfg.PlaceholderEmoji;
            label.AddThemeFontSizeOverride("font_size", (int)rng.RandfRange(cfg.MinSize, cfg.MaxSize));
            label.MouseFilter = MouseFilterEnum.Ignore;
            label.Modulate = _theme!.AccentColor with { A = 0.6f };

            // Spawn at a random horizontal position; vertical from the rise direction.
            float startX = rng.RandfRange(40, Size.X - 40);
            float startY = cfg.RiseDirection.Y < 0 ? Size.Y - 60 : 0;
            label.Position = new Vector2(startX, startY);
            _particleLayer.AddChild(label);

            float life = rng.RandfRange(cfg.MinLifetimeSec, cfg.MaxLifetimeSec);
            float dx   = cfg.RiseDirection.X * 80f;
            float dy   = cfg.RiseDirection.Y * 220f;

            var tween = label.CreateTween().SetParallel();
            tween.TweenProperty(label, "position", label.Position + new Vector2(dx, dy), life);
            tween.TweenProperty(label, "modulate:a", 0f, life);
            tween.Chain().TweenCallback(Callable.From(() => label.QueueFree()));
        }

        // ── Decoration node (animated) ────────────────────────────────────────

        private partial class DecorationNode : Label
        {
            public readonly Decoration Spec;
            public Vector2 BasePosition;
            private float _time;

            public DecorationNode(Decoration spec)
            {
                Spec = spec;
                BasePosition = spec.PositionArtSpace;
                Text = spec.PlaceholderEmoji;
                AddThemeFontSizeOverride("font_size", (int)spec.SizePx);
                MouseFilter = MouseFilterEnum.Ignore;
            }

            public override void _Process(double delta)
            {
                _time += (float)delta;
                Position = BasePosition;

                switch (Spec.Animation)
                {
                    case DecorationAnimation.Sway:
                        RotationDegrees = Mathf.Sin(_time * 2.2f) * 5.0f;
                        break;
                    case DecorationAnimation.Float:
                        Position += new Vector2(0, Mathf.Sin(_time * 1.8f) * 4.0f);
                        break;
                    case DecorationAnimation.Drift:
                        Position += new Vector2(Mathf.Sin(_time * 0.4f) * 30.0f, 0);
                        break;
                }
            }
        }

        private partial class WanderingCreature : Control
        {
            [Signal] public delegate void TappedEventHandler();

            public Guid CreatureId { get; }
            private readonly Color _fallbackColor;
            private readonly Rect2 _wanderZone;
            private readonly Texture2D? _bodyTexture;
            private Vector2 _target;
            private float _retargetIn;
            private float _bobTime;
            private ColorRect _shadow;
            private Control _body;

            private const float MoveSpeed     = 30f;   // px/sec
            private const float MinRetarget   = 3f;
            private const float MaxRetarget   = 6f;
            private const float ClickHaloMs   = 300f;
            private const float BodySizePx    = 52f;
            private const float ShadowWidthPx = 40f;
            private const float ShadowHeightPx= 12f;

            public WanderingCreature(Guid id, Color fallbackColor, Rect2 wanderZone, Texture2D? tex)
            {
                CreatureId     = id;
                _fallbackColor = fallbackColor;
                _wanderZone    = wanderZone;
                _bodyTexture   = tex;
            }

            public override void _Ready()
            {
                MouseFilter = MouseFilterEnum.Stop;
                CustomMinimumSize = new Vector2(BodySizePx, BodySizePx);

                // Drop shadow — flat ellipse via ColorRect
                _shadow = new ColorRect();
                _shadow.Color = new Color(0, 0, 0, 0.45f);
                _shadow.Size = new Vector2(ShadowWidthPx, ShadowHeightPx);
                _shadow.Position = new Vector2((BodySizePx - ShadowWidthPx) / 2f, BodySizePx);
                _shadow.MouseFilter = MouseFilterEnum.Ignore;
                AddChild(_shadow);

                // Body — TextureRect when available, else colored circle PanelContainer
                if (_bodyTexture != null)
                {
                    var tr = new TextureRect();
                    tr.Texture     = _bodyTexture;
                    tr.ExpandMode  = TextureRect.ExpandModeEnum.IgnoreSize;
                    tr.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                    tr.Size        = new Vector2(BodySizePx, BodySizePx);
                    tr.MouseFilter = MouseFilterEnum.Ignore;
                    AddChild(tr);
                    _body = tr;
                }
                else
                {
                    var style = new StyleBoxFlat();
                    style.BgColor = _fallbackColor;
                    style.SetCornerRadiusAll(26);
                    var panel = new PanelContainer();
                    panel.AddThemeStyleboxOverride("panel", style);
                    panel.Size = new Vector2(BodySizePx, BodySizePx);
                    panel.MouseFilter = MouseFilterEnum.Ignore;
                    AddChild(panel);
                    _body = panel;
                }

                _retargetIn = 0;
                Position    = RandomPointInZone();
                _target     = RandomPointInZone();
            }

            public override void _Process(double delta)
            {
                _bobTime += (float)delta;

                // Move toward target
                Vector2 toTarget = _target - Position;
                float dist = toTarget.Length();
                if (dist > 1f)
                {
                    Position += toTarget.Normalized() * MoveSpeed * (float)delta;
                }

                _retargetIn -= (float)delta;
                if (_retargetIn <= 0 || dist < 4f)
                {
                    _target = RandomPointInZone();
                    var rng = new RandomNumberGenerator(); rng.Randomize();
                    _retargetIn = rng.RandfRange(MinRetarget, MaxRetarget);
                }

                // Squash-stretch on body
                if (_body != null)
                {
                    float bob = Mathf.Sin(_bobTime * 3.5f) * 0.08f;
                    _body.Scale = new Vector2(1.0f + bob, 1.0f - bob);
                }

                // Y-sort via z_index
                ZIndex = (int)Position.Y;
            }

            public override void _GuiInput(InputEvent @event)
            {
                if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                {
                    EmitClick();
                    AcceptEvent();
                }
            }

            private void EmitClick()
            {
                EmitSignal(SignalName.Tapped);
                // Brief z-index boost so the highlight is on top
                int prior = ZIndex;
                ZIndex = 10000;
                GetTree().CreateTimer(ClickHaloMs / 1000f).Timeout += () => ZIndex = prior;
            }

            private Vector2 RandomPointInZone()
            {
                var rng = new RandomNumberGenerator(); rng.Randomize();
                return new Vector2(
                    rng.RandfRange(_wanderZone.Position.X, _wanderZone.End.X),
                    rng.RandfRange(_wanderZone.Position.Y, _wanderZone.End.Y));
            }
        }
    }
}

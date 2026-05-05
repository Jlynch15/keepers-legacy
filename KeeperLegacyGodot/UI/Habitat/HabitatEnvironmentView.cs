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
            // Creature layer rebuilt in Task 11 — for this task creatures are absent.
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
    }
}

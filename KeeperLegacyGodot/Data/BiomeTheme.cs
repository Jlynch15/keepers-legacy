// Data/BiomeTheme.cs
// Per-biome environment theme. Static lookup keyed by HabitatType.

using System.Collections.Generic;
using Godot;
using KeeperLegacy.Models;

namespace KeeperLegacy.Data
{
    public enum DecorationAnimation
    {
        None,    // Static — coral, rocks, mushrooms
        Sway,    // Rotation oscillation around bottom-center — seaweed, grass blades
        Float,   // Vertical bob — runes, magical motes
        Drift    // Slow horizontal drift — background fish, butterflies
    }

    public record Decoration(
        string PlaceholderEmoji,
        Vector2 PositionArtSpace,
        float SizePx,
        DecorationAnimation Animation = DecorationAnimation.None);

    public record ParticleConfig(
        string PlaceholderEmoji,
        float  EmitRatePerSec,
        Vector2 RiseDirection,
        float  MinLifetimeSec,
        float  MaxLifetimeSec,
        float  MinSize,
        float  MaxSize);

    public record LightShaft(
        float LeftPct,
        float WidthPx,
        float SkewDeg,
        float Opacity,
        float PulseDurSec);

    public record FloorOverlay(Color TintTop, Color TintBottom, float HeightPx);

    public record SurfaceLine(Color StartColor, Color MidColor, Color EndColor, float ShimmerDurSec);

    public record BiomeTheme(
        HabitatType    Biome,
        string         IconEmoji,            // 💧 🌿 🪨 🔥 ❄️ ⚡ ✨
        string         DisplayName,          // "Water Habitats"
        string         FlavorSubtitle,       // "Aquatic · Oceanic · Deep Sea"
        Color          AccentColor,          // Used for active tab bottom border, capacity pill, etc.
        Color          BackgroundTopColor,
        Color          BackgroundBottomColor,
        Decoration[]   Decorations,
        ParticleConfig?Particles,
        LightShaft[]   AmbientLights,
        FloorOverlay?  Floor,
        SurfaceLine?   Surface,
        Rect2          WanderZone);

    public static class BiomeThemes
    {
        // Coords are in art-space (1364x768 reference), same as PedestalDefs.

        private static readonly Dictionary<HabitatType, BiomeTheme> _themes = new()
        {
            [HabitatType.Water] = new BiomeTheme(
                Biome:                 HabitatType.Water,
                IconEmoji:             "💧",
                DisplayName:           "Water Habitats",
                FlavorSubtitle:        "Aquatic · Oceanic · Deep Sea",
                AccentColor:           new Color("#4AA8E0"),
                BackgroundTopColor:    new Color("#061828"),
                BackgroundBottomColor: new Color("#1E2A1E"),
                Decorations: new[]
                {
                    new Decoration("🪸", new Vector2(  56, 410), 26f),
                    new Decoration("🪸", new Vector2( 168, 408), 20f),
                    new Decoration("🪸", new Vector2( 920, 412), 24f),
                    new Decoration("🪸", new Vector2( 800, 408), 18f),
                    new Decoration("🪸", new Vector2( 612, 412), 16f),
                    new Decoration("🪨", new Vector2(  28, 418), 20f),
                    new Decoration("🪨", new Vector2( 970, 416), 22f),
                    new Decoration("🪨", new Vector2( 518, 418), 16f),
                    new Decoration("🌿", new Vector2(  82, 380), 22f, DecorationAnimation.Sway),
                    new Decoration("🌿", new Vector2( 192, 380), 22f, DecorationAnimation.Sway),
                    new Decoration("🌿", new Vector2( 708, 380), 22f, DecorationAnimation.Sway),
                    new Decoration("🌿", new Vector2( 980, 380), 22f, DecorationAnimation.Sway),
                },
                Particles: new ParticleConfig(
                    PlaceholderEmoji: "・",
                    EmitRatePerSec:   2.0f,
                    RiseDirection:    new Vector2(0, -1),
                    MinLifetimeSec:   4.0f,
                    MaxLifetimeSec:   6.0f,
                    MinSize:          3.0f,
                    MaxSize:          8.0f),
                AmbientLights: new[]
                {
                    new LightShaft(LeftPct: 0.10f, WidthPx: 50, SkewDeg: -10, Opacity: 0.50f, PulseDurSec: 7.0f),
                    new LightShaft(LeftPct: 0.30f, WidthPx: 70, SkewDeg:  -6, Opacity: 0.70f, PulseDurSec: 5.5f),
                    new LightShaft(LeftPct: 0.55f, WidthPx: 45, SkewDeg: -12, Opacity: 0.40f, PulseDurSec: 8.0f),
                    new LightShaft(LeftPct: 0.78f, WidthPx: 55, SkewDeg:  -8, Opacity: 0.55f, PulseDurSec: 6.5f),
                },
                Floor: new FloorOverlay(
                    TintTop:    new Color(0.75f, 0.53f, 0.25f, 0.25f),
                    TintBottom: new Color(0.55f, 0.37f, 0.14f, 0.55f),
                    HeightPx:   55f),
                Surface: new SurfaceLine(
                    StartColor:    new Color("#4AA8E0") with { A = 0.6f },
                    MidColor:      new Color("#78C8FF") with { A = 0.8f },
                    EndColor:      new Color("#4AA8E0") with { A = 0.6f },
                    ShimmerDurSec: 3.0f),
                WanderZone: new Rect2(80, 100, 660, 280)
            ),

            [HabitatType.Grass] = new BiomeTheme(
                Biome:                 HabitatType.Grass,
                IconEmoji:             "🌿",
                DisplayName:           "Grass Habitats",
                FlavorSubtitle:        "Meadow · Forest · Garden",
                AccentColor:           new Color("#4AB84A"),
                BackgroundTopColor:    new Color("#1A2810"),
                BackgroundBottomColor: new Color("#2A3818"),
                Decorations: new[]
                {
                    new Decoration("🍄", new Vector2( 100, 400), 22f),
                    new Decoration("🍄", new Vector2( 250, 408), 18f),
                    new Decoration("🍄", new Vector2( 720, 410), 22f),
                    new Decoration("🌱", new Vector2( 180, 380), 18f, DecorationAnimation.Sway),
                    new Decoration("🌱", new Vector2( 400, 388), 18f, DecorationAnimation.Sway),
                    new Decoration("🌱", new Vector2( 840, 384), 18f, DecorationAnimation.Sway),
                    new Decoration("🦋", new Vector2( 320, 200), 16f, DecorationAnimation.Drift),
                    new Decoration("🦋", new Vector2( 680, 240), 16f, DecorationAnimation.Drift),
                },
                Particles: new ParticleConfig("✦", 0.8f, new Vector2(0, -0.3f), 5f, 8f, 2f, 4f),
                AmbientLights: System.Array.Empty<LightShaft>(),
                Floor: new FloorOverlay(new Color(0.30f, 0.45f, 0.20f, 0.25f), new Color(0.20f, 0.32f, 0.12f, 0.55f), 55f),
                Surface: null,
                WanderZone: new Rect2(80, 100, 660, 280)
            ),

            [HabitatType.Dirt] = new BiomeTheme(
                Biome:                 HabitatType.Dirt,
                IconEmoji:             "🪨",
                DisplayName:           "Dirt Habitats",
                FlavorSubtitle:        "Burrow · Cave · Earthen",
                AccentColor:           new Color("#C08840"),
                BackgroundTopColor:    new Color("#1A0E06"),
                BackgroundBottomColor: new Color("#2A1A0C"),
                Decorations: new[]
                {
                    new Decoration("🪨", new Vector2(  60, 408), 26f),
                    new Decoration("🪨", new Vector2( 220, 412), 22f),
                    new Decoration("🪨", new Vector2( 980, 410), 28f),
                    new Decoration("💎", new Vector2( 380, 395), 18f),
                    new Decoration("💎", new Vector2( 720, 398), 16f),
                    new Decoration("🌱", new Vector2( 140, 380), 16f, DecorationAnimation.Sway),
                },
                Particles: null,
                AmbientLights: System.Array.Empty<LightShaft>(),
                Floor: new FloorOverlay(new Color(0.55f, 0.35f, 0.18f, 0.30f), new Color(0.40f, 0.25f, 0.10f, 0.60f), 55f),
                Surface: null,
                WanderZone: new Rect2(80, 100, 660, 280)
            ),

            [HabitatType.Fire] = new BiomeTheme(
                Biome:                 HabitatType.Fire,
                IconEmoji:             "🔥",
                DisplayName:           "Fire Habitats",
                FlavorSubtitle:        "Lava · Forge · Ember",
                AccentColor:           new Color("#E06030"),
                BackgroundTopColor:    new Color("#28080A"),
                BackgroundBottomColor: new Color("#3A1810"),
                Decorations: new[]
                {
                    new Decoration("🔥", new Vector2(  80, 400), 24f, DecorationAnimation.Sway),
                    new Decoration("🔥", new Vector2( 920, 405), 26f, DecorationAnimation.Sway),
                    new Decoration("🪵", new Vector2( 200, 410), 20f),
                    new Decoration("🪵", new Vector2( 720, 408), 22f),
                    new Decoration("🪨", new Vector2( 480, 412), 20f),
                },
                Particles: new ParticleConfig("✦", 1.5f, new Vector2(0, -1), 3f, 5f, 2f, 6f),
                AmbientLights: new[]
                {
                    new LightShaft(LeftPct: 0.20f, WidthPx: 80, SkewDeg: 5,  Opacity: 0.30f, PulseDurSec: 4f),
                    new LightShaft(LeftPct: 0.70f, WidthPx: 80, SkewDeg: -5, Opacity: 0.30f, PulseDurSec: 4.5f),
                },
                Floor: new FloorOverlay(new Color(0.85f, 0.30f, 0.10f, 0.35f), new Color(0.45f, 0.10f, 0.05f, 0.65f), 55f),
                Surface: null,
                WanderZone: new Rect2(80, 100, 660, 280)
            ),

            [HabitatType.Ice] = new BiomeTheme(
                Biome:                 HabitatType.Ice,
                IconEmoji:             "❄️",
                DisplayName:           "Ice Habitats",
                FlavorSubtitle:        "Frost · Glacier · Tundra",
                AccentColor:           new Color("#60D0E0"),
                BackgroundTopColor:    new Color("#0A2030"),
                BackgroundBottomColor: new Color("#1E3848"),
                Decorations: new[]
                {
                    new Decoration("🧊", new Vector2( 100, 408), 28f),
                    new Decoration("🧊", new Vector2( 880, 410), 26f),
                    new Decoration("🧊", new Vector2( 480, 412), 22f),
                    new Decoration("❄️", new Vector2( 200, 380), 18f),
                    new Decoration("❄️", new Vector2( 700, 384), 18f),
                },
                Particles: new ParticleConfig("❄", 1.0f, new Vector2(0, 1), 6f, 9f, 3f, 5f),
                AmbientLights: System.Array.Empty<LightShaft>(),
                Floor: new FloorOverlay(new Color(0.70f, 0.85f, 0.95f, 0.30f), new Color(0.45f, 0.65f, 0.80f, 0.55f), 55f),
                Surface: null,
                WanderZone: new Rect2(80, 100, 660, 280)
            ),

            [HabitatType.Electric] = new BiomeTheme(
                Biome:                 HabitatType.Electric,
                IconEmoji:             "⚡",
                DisplayName:           "Electric Habitats",
                FlavorSubtitle:        "Storm · Conduit · Static",
                AccentColor:           new Color("#E8D020"),
                BackgroundTopColor:    new Color("#1A1A28"),
                BackgroundBottomColor: new Color("#28283A"),
                Decorations: new[]
                {
                    new Decoration("⚡", new Vector2( 200, 200), 22f, DecorationAnimation.Float),
                    new Decoration("⚡", new Vector2( 600, 220), 22f, DecorationAnimation.Float),
                    new Decoration("🔌", new Vector2( 100, 410), 22f),
                    new Decoration("🔌", new Vector2( 920, 412), 22f),
                    new Decoration("🪨", new Vector2( 480, 414), 18f),
                },
                Particles: new ParticleConfig("✦", 1.2f, new Vector2(0.3f, -0.3f), 2f, 4f, 2f, 5f),
                AmbientLights: new[]
                {
                    new LightShaft(LeftPct: 0.40f, WidthPx: 30, SkewDeg: 15, Opacity: 0.50f, PulseDurSec: 1.5f),
                },
                Floor: new FloorOverlay(new Color(0.90f, 0.85f, 0.30f, 0.20f), new Color(0.50f, 0.45f, 0.15f, 0.50f), 55f),
                Surface: null,
                WanderZone: new Rect2(80, 100, 660, 280)
            ),

            [HabitatType.Magical] = new BiomeTheme(
                Biome:                 HabitatType.Magical,
                IconEmoji:             "✨",
                DisplayName:           "Magical Habitats",
                FlavorSubtitle:        "Etheric · Astral · Mystic",
                AccentColor:           new Color("#9860E0"),
                BackgroundTopColor:    new Color("#180828"),
                BackgroundBottomColor: new Color("#2A1A40"),
                Decorations: new[]
                {
                    new Decoration("🔮", new Vector2( 480, 380), 28f),
                    new Decoration("✨", new Vector2( 200, 250), 18f, DecorationAnimation.Float),
                    new Decoration("✨", new Vector2( 760, 260), 18f, DecorationAnimation.Float),
                    new Decoration("✨", new Vector2( 400, 200), 14f, DecorationAnimation.Float),
                    new Decoration("✨", new Vector2( 600, 210), 14f, DecorationAnimation.Float),
                },
                Particles: new ParticleConfig("✦", 1.0f, new Vector2(0, -0.5f), 5f, 8f, 2f, 5f),
                AmbientLights: new[]
                {
                    new LightShaft(LeftPct: 0.15f, WidthPx: 60, SkewDeg: -10, Opacity: 0.45f, PulseDurSec: 8f),
                    new LightShaft(LeftPct: 0.50f, WidthPx: 80, SkewDeg:   0, Opacity: 0.55f, PulseDurSec: 7f),
                    new LightShaft(LeftPct: 0.85f, WidthPx: 60, SkewDeg:  10, Opacity: 0.45f, PulseDurSec: 9f),
                },
                Floor: new FloorOverlay(new Color(0.55f, 0.30f, 0.85f, 0.25f), new Color(0.30f, 0.15f, 0.55f, 0.55f), 55f),
                Surface: null,
                WanderZone: new Rect2(80, 100, 660, 280)
            ),
        };

        public static BiomeTheme? For(HabitatType biome)
            => _themes.TryGetValue(biome, out var t) ? t : null;
    }
}

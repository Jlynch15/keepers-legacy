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

            // Stub themes — minimal so all 7 biomes work even before art-direction.
            // Full configurations land in Task 5.
            [HabitatType.Grass]    = StubTheme(HabitatType.Grass,    "🌿", "Grass Habitats",    "Meadow · Forest · Garden",     new Color("#4AB84A")),
            [HabitatType.Dirt]     = StubTheme(HabitatType.Dirt,     "🪨", "Dirt Habitats",     "Burrow · Cave · Earthen",       new Color("#C08840")),
            [HabitatType.Fire]     = StubTheme(HabitatType.Fire,     "🔥", "Fire Habitats",     "Lava · Forge · Ember",          new Color("#E06030")),
            [HabitatType.Ice]      = StubTheme(HabitatType.Ice,      "❄️", "Ice Habitats",      "Frost · Glacier · Tundra",      new Color("#60D0E0")),
            [HabitatType.Electric] = StubTheme(HabitatType.Electric, "⚡", "Electric Habitats", "Storm · Conduit · Static",      new Color("#E8D020")),
            [HabitatType.Magical]  = StubTheme(HabitatType.Magical,  "✨", "Magical Habitats",  "Etheric · Astral · Mystic",     new Color("#9860E0")),
        };

        private static BiomeTheme StubTheme(HabitatType biome, string icon, string name, string flavor, Color accent)
            => new BiomeTheme(
                Biome:                 biome,
                IconEmoji:             icon,
                DisplayName:           name,
                FlavorSubtitle:        flavor,
                AccentColor:           accent,
                BackgroundTopColor:    accent.Darkened(0.85f),
                BackgroundBottomColor: accent.Darkened(0.95f),
                Decorations:           System.Array.Empty<Decoration>(),
                Particles:             null,
                AmbientLights:         System.Array.Empty<LightShaft>(),
                Floor:                 null,
                Surface:               null,
                WanderZone:            new Rect2(80, 100, 660, 280));

        public static BiomeTheme? For(HabitatType biome)
            => _themes.TryGetValue(biome, out var t) ? t : null;
    }
}

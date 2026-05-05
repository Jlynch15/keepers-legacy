// Data/CreatureSpriteLoader.cs
// Resolves a creature ID + mutation index to its idle sprite path
// (organized by biome subfolder), loads the Texture2D from Godot,
// and falls back to a gray "?" if missing.

using System;
using Godot;

namespace KeeperLegacy.Data
{
    public static class CreatureSpriteLoader
    {
        public const string FallbackPath = "res://Sprites/Creatures/_fallback.png";

        // Pure path resolution. Throws on invalid input or unknown creature.
        // mutationIndex is 0-based; the resulting filename uses 1-based v1..v4.
        // Biome folder is derived from the creature's HabitatType.
        public static string ResolveIdlePath(string creatureId, int mutationIndex)
        {
            if (string.IsNullOrWhiteSpace(creatureId))
                throw new ArgumentException("creatureId must not be empty.", nameof(creatureId));
            if (mutationIndex < 0 || mutationIndex > 3)
                throw new ArgumentOutOfRangeException(
                    nameof(mutationIndex), mutationIndex, "Must be 0..3.");

            var entry = CreatureRosterData.Find(creatureId)
                ?? throw new ArgumentException(
                    $"Unknown creatureId: '{creatureId}'", nameof(creatureId));
            var biome = entry.HabitatType.ToString().ToLowerInvariant();

            return $"res://Sprites/Creatures/{biome}/{creatureId}/{creatureId}_v{mutationIndex + 1}.png";
        }

        // Loads the idle texture. Returns the fallback texture if the file doesn't exist.
        public static Texture2D LoadIdle(string creatureId, int mutationIndex)
        {
            var path = ResolveIdlePath(creatureId, mutationIndex);
            if (ResourceLoader.Exists(path))
            {
                var tex = GD.Load<Texture2D>(path);
                if (tex != null) return tex;
            }
            return GD.Load<Texture2D>(FallbackPath);
        }
    }
}

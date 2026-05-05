# Character Sprite Pipeline — Placeholder Phase

**Date:** 2026-05-04
**Status:** Approved (brainstorm)
**Scope:** Mass-produce placeholder sprites for the full 80-creature roster so the game is visually functional end-to-end. Establishes a clean upgrade path to hand-illustrated PNGs later.

## Context

The project has 80 creatures × 4 mutation variants = **320 sprites needed** (verified by counting entries in `CreatureRosterData.cs`; the older "58" number from prior design docs is stale). Two prior pipelines exist on disk:

1. **`SpriteSheets/`** — 22 hand-coded SVG sprite sheets (4×4 grid, 4 states × 4 mutations). Style is too primitive (mostly bare circles/ellipses, low silhouette identity) and lacks variation between creatures. Will not be used.
2. **`CharacterModels/`** — 3 AI-illustrated PNGs (Coralsprite, Deepecho, Tidecaller) at production-target quality. Idle pose only, ~600×400 each. Too expensive/slow to scale to 232 manually.

Mid-term goal is illustrated PNGs at the quality of the 3 references. Short-term need is something better than the existing SVG sheets, fast enough to produce all 232 in a tractable timeframe, with a frictionless replacement path when illustrated art is ready.

## Decision: Option B — Idle-only sprites, Godot animates emotional states

One image per (creature × mutation). Godot fakes the Interact / Happy / Sad states procedurally:

- **Interact** — quick scale-up bounce on touch.
- **Happy** — continuous bob + sparkle particles.
- **Sad** — downward slump + slight desaturation tween.

Trade-off: facial expression doesn't change between states. Acceptable for placeholder phase; matches the format of the 3 reference PNGs anyway. The procedural-animation logic itself is **not** in this spec — that's a follow-up for the scene-development session.

## File structure & naming

**Source of truth:** `KeeperLegacyGodot/Data/CreatureRosterData.cs`. 58 lowercase IDs, 4 mutations each (0-indexed `MutationVariant.Index`).

**Layout:**

```
KeeperLegacyGodot/Sprites/Creatures/
├── aquaburst/
│   ├── aquaburst.svg          ← editable source (single creature, color-swap layer for mutations)
│   ├── aquaburst_v1.png       ← rendered, mutation index 0 ("Sapphire Blue")
│   ├── aquaburst_v2.png       ← rendered, mutation index 1 ("Teal Green")
│   ├── aquaburst_v3.png       ← rendered, mutation index 2 ("Pearl White")
│   └── aquaburst_v4.png       ← rendered, mutation index 3 ("Coral Pink")
├── shimmerstream/ …
└── cosmicwarden/ …
```

**Conventions:**

- Folder name = creature ID (lowercase, no separators), exactly matches `CreatureCatalogEntry.Id`.
- PNG filenames use **1-indexed** `v1..v4` (matches the human-facing V1-V4 in the spec). Code converts: `mutation_index = N - 1`.
- PNGs are 512×512, transparent background, creature occupies ~80% of canvas.
- Godot loads PNGs at runtime. SVG source files exist for editing but are not loaded by the game.

**Migration of existing illustrated PNGs:**

The 3 hand-illustrated PNGs in `CharacterModels/` are the canonical idle for those creatures immediately. They're copied (not moved) into the new structure with the canonical filename:

- `CharacterModels/Coralsprite/VividOrange.png` → `Sprites/Creatures/coralsprite/coralsprite_v1.png`
- `…/PinkCoral.png` → `coralsprite_v2.png`
- `…/PaleYellow.png` → `coralsprite_v3.png`
- `…/Purple.png` → `coralsprite_v4.png`
- (Same pattern for Deepecho and Tidecaller against their mutation order in `CreatureRosterData.cs`.)

`CharacterModels/` is preserved as the "production-quality reference" archive.

**Important:** the illustrated PNGs have a non-transparent background (gray gradient or dark backdrop). Before copying into the new structure, the background is removed so the creature has a transparent silhouette consistent with the placeholder sprites. If automated background removal is unreliable on these specific images, the copy step is deferred and the placeholder SVG is used for those creatures until the illustrated PNGs are properly cut out.

## Visual quality bar

Hard requirements for SVG placeholders, calibrated specifically against the existing 22 sheets that were rejected:

1. **Distinctive silhouette per creature.** A bedrock and a snowpuff must be unmistakable in pure-black silhouette form. No "interchangeable round body with element-themed nub on top."
2. **3-tone shading per body part** — base color, shadow, highlight. Not flat fills.
3. **Signature-feature emphasis.** The defining trait from the description in `CreatureRosterData.cs` is the dominant visual element: kelpling's cape dominates, sandwhistle's pores are visible, geoheart's chest geode is a window, etc.
4. **Consistent canvas anchor across all 58.** Creature occupies ~80% of the 512×512 canvas, vertically centered with a slight downward bias (room for happy-bounce upward without clipping).
5. **Lighthearted/cute tone.** Soft rounded shapes, large expressive eyes, friendly mouth — matching the 3 reference PNGs.

Mutations within a single creature share identical shape/pose; only fill colors differ. Eye color stays consistent across mutations (dark pupils with white highlight).

## Pilot-first production pacing

**No mass production until the visual baseline is locked.**

### Phase 1: Pilot (3 creatures, 12 PNGs)

I produce three creatures at full quality, all 4 mutations each:

- **Coralsprite** (water, common) — round/organic with branching coral horns. Direct visual comparison against the existing AI-illustrated reference.
- **Bedrock** (dirt, uncommon) — squat/blocky/stone-layered. Tests chunky silhouettes far from the coralsprite shape.
- **Deepecho** (water, uncommon) — dark body with bioluminescent markings. Tests glow/luminance effects in pure SVG.

These 12 PNGs are the locked baseline. User reviews, gives feedback, and iterates as many rounds as needed until the style is approved. **Mass production does not begin until explicit sign-off.**

### Phase 2: Habitat-by-habitat sweep

Once locked, the remaining 77 creatures are produced in habitat batches with a review pause between each. Habitat counts (from `CreatureRosterData.cs`):

1. Finish water (12 remaining — 15 total minus Coralsprite & Deepecho pilots minus Tidecaller which is covered by illustrated migration) → review
2. Dirt (14 remaining — 15 total minus Bedrock pilot) → review
3. Grass (15) → review
4. Fire (10) → review
5. Ice (10) → review
6. Electric (10) → review
7. Magical (5) → review

Sum: 12 + 14 + 15 + 10 + 10 + 10 + 5 = 76 sweep SVGs + 3 pilot SVGs + 1 illustrated-only (Tidecaller) = 80 total creatures covered.

Pilot SVGs for Coralsprite and Deepecho are authored for **style validation only**. They render to `Sprites/Creatures/<id>/_svg_pilot/` rather than overwriting the canonical illustrated PNGs. Bedrock has no illustrated migration, so its SVG renders directly to canonical paths.

Each review is a quick "does this still match the locked style" check, not a re-debate of the baseline. If significant drift appears, work pauses to re-anchor before continuing.

## Godot integration

**Constraint:** another session is actively working on scene/UI development. To avoid conflicts, this work touches **zero existing files** in `KeeperLegacyGodot/`. It only adds new files.

**New file:** `KeeperLegacyGodot/Data/CreatureSpriteLoader.cs`

Static helper exposing:

```csharp
public static class CreatureSpriteLoader {
    /// <summary>
    /// Loads the idle sprite for the given creature ID and mutation index (0-3).
    /// Returns a fallback gray "?" texture if the file is missing.
    /// </summary>
    public static Texture2D LoadIdle(string creatureId, int mutationIndex);
}
```

Path resolution: `res://Sprites/Creatures/{creatureId}/{creatureId}_v{mutationIndex + 1}.png`.

The fallback texture is a pre-rendered 512×512 PNG (also new, in `Sprites/Creatures/_fallback.png`) so `LoadIdle` never returns null. Calling code in the scene-development session wires this loader into pedestals, habitat detail, shop, etc. when ready.

## Build pipeline (SVG → PNG)

A single Python script in `tools/render_creature_sprites.py` (new folder, won't conflict with anything):

- Walks `KeeperLegacyGodot/Sprites/Creatures/*/`.
- For each `<id>.svg` and each mutation index 0-3, renders a 512×512 PNG via Inkscape CLI.
- Mutation colors are baked into the SVG via a CSS class swap or a templated re-fill — exact mechanism decided during pilot.

Re-runs are idempotent. Developer runs it whenever an SVG source changes; PNGs are checked into git (Godot needs them as static assets). The existing `SpriteSheets/prepare_sprites.py` is the reference for the Inkscape CLI calls but is not reused (it targets the SwiftUI Assets.xcassets path, which is stale).

## Future upgrade path (deferred — not part of this spec's implementation)

When approaching production, each creature gets a hand-illustrated PNG matching the 3 reference images. The illustrated PNG drops into the same path (`<id>_v<N>.png`) and overwrites the placeholder. Code path doesn't change. The img2img-from-SVG approach (use the placeholder SVG as a ControlNet/img2img reference for AI illustration) gets prototyped on a few creatures before committing as the upgrade method — it may or may not preserve enough silhouette fidelity to be useful.

## Out of scope

- Procedural animation logic for Interact / Happy / Sad states (follow-up for scene-dev session).
- Edits to any existing `.cs`, `.tscn`, `.json`, or `.md` file — only new files are added.
- Changes to `CreatureRosterData.cs`, mutation color hints, or any roster data.
- AI image-generation tooling, API setup, or img2img pipelines.
- The existing 22 SVG sheets in `SpriteSheets/` — left in place as historical artifact, not deleted, not used.
- Audio, particles, post-FX, or any non-sprite asset.

## Success criteria

- All 80 creature folders exist under `Sprites/Creatures/` with 4 PNGs each (320 total).
- Three reference PNGs from `CharacterModels/` are migrated into canonical paths with backgrounds removed.
- `CreatureSpriteLoader.cs` exists and resolves any `(creatureId, mutationIndex)` pair to a Texture2D — real sprite if present, fallback if missing.
- `tools/render_creature_sprites.py` regenerates all PNGs from SVG sources idempotently.
- Visual baseline (3 pilots) was reviewed and explicitly approved before mass production began.
- No existing file in `KeeperLegacyGodot/` was modified.

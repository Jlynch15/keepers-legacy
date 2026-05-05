# Habitat Category Screen Design — Keeper's Legacy (Godot/C#)

## Context

The Habitat Floor screen is shipped. Clicking a pedestal currently routes to a placeholder `HabitatCategoryScreen.tscn` via `MainScene.NavigateToSubScreen("HabitatCategory")`. This spec replaces that placeholder with the real screen: a biome-themed environment view on the left, a 4-slot creature roster on the right, with tabs for switching between owned/purchasable habitats of the same biome.

The HTML mockup at `KeeperLegacyGodot/Mockups/habitat-category.html` is the visual reference for the Water biome. All other biomes (Grass, Dirt, Fire, Ice, Electric, Magical) are themed via the same component using per-biome data configs, with placeholder emoji decorations until biome art lands.

**Project rule applied:** Visual / positional / sizing elements ship with in-game tuning controls from day one. See "Debug-Tuning Surface" section.

## Scope

**In scope:**
- Habitat Category screen as a full sub-screen (replaces placeholder)
- Habitat data model expansion: 1 → 4 creatures per habitat
- Per-biome habitat capacity rules (4 / 3 / 2 max habitats by biome tier)
- Habitat purchase flow (coin cost ladder, confirm dialog)
- Biome theming system (`BiomeTheme` records + lookup) for all 7 biomes with placeholder decorations
- Creature wander / Y-sort rendering / drop shadows in env view
- Roster grid with empty slot "Add Creature" choice menu (Buy from Shop / Breed New)
- Long-press release flow
- Debug-tuning surface for decorations and wander zone

**Out of scope (deferred):**
- Habitat Detail screen (separate spec)
- Creature stat system (hunger / happy / clean / affection — Detail spec)
- Status dots in roster slots (depends on stat system)
- Real biome decoration art (placeholder emoji until per-biome art lands)
- Animations for habitat-purchase / creature-add transitions (polish phase)
- Tutorial overlays (separate onboarding pass)
- Palette rework (project-wide; brown/gold replacement is a future pass — see `project_palette_rework_pending` memory)

## Architecture Overview

### File structure

```
KeeperLegacyGodot/
├── UI/
│   └── Habitat/
│       ├── HabitatCategoryScreen.cs       (REPLACE placeholder; orchestrator)
│       ├── HabitatCategoryScreen.tscn     (REPLACE; empty Control root, code-built)
│       ├── HabitatOverlayBar.cs           (CREATE; top translucent bar)
│       ├── HabitatTabBar.cs               (CREATE; per-habitat-instance tabs)
│       ├── HabitatEnvironmentView.cs      (CREATE; biome-themed env scene)
│       ├── HabitatRosterPanel.cs          (CREATE; 4-slot grid)
│       ├── HabitatPalette.cs              (CREATE; centralized colors)
│       └── ChoiceMenu.cs                  (CREATE; reusable lightweight floating menu)
├── Models/
│   ├── Habitat.cs                          (MODIFY: OccupantId → OccupantIds list)
│   └── HabitatCapacity.cs                  (CREATE; per-biome limits + cost lookup)
├── Data/
│   └── BiomeTheme.cs                       (CREATE; theme record + lookup table)
└── Managers/
    └── HabitatManager.cs                   (MODIFY; add habitat / place / release / unlock-reason)
```

### Component composition

```
HabitatCategoryScreen (Control, FullRect)
├── HabitatOverlayBar          (top, 44px, translucent above content)
├── HabitatTabBar              (just below overlay bar)
├── (left content area)
│   └── HabitatEnvironmentView (left ~64% of content area)
└── HabitatRosterPanel         (right ~36% of content area)
```

The right-edge sidebar (Home / Shop / Orders / Breed / Pedia / Settings) lives on `MainScene` and is automatically present — this screen does not render it.

### No sub-`.tscn` files

All UI components (`HabitatOverlayBar`, `HabitatTabBar`, `HabitatEnvironmentView`, `HabitatRosterPanel`, `ChoiceMenu`) are code-built `Control` nodes. Consistent with `PedestalNode` and `HabitatFloorScreen` patterns. Avoids scene-tree edit churn during iteration. Components can be promoted to `.tscn` later if scene-editor tuning becomes useful.

## Data Model

### `Habitat` model change

Current single-occupant model is replaced by a list of up to 4 occupants.

```csharp
public class Habitat
{
    public Guid Id { get; }
    public HabitatType Type { get; }

    // CHANGED from Guid? OccupantId. Mutate via Try* methods only.
    public List<Guid> OccupantIds { get; }

    public List<string> DecorationIds { get; }
    public int UnlockedAtLevel { get; }

    // ── Computed ──
    public bool IsEmpty => OccupantIds.Count == 0;
    public bool IsFull  => OccupantIds.Count >= HabitatCapacity.CreaturesPerHabitat;
    public int  AvailableSlots => HabitatCapacity.CreaturesPerHabitat - OccupantIds.Count;

    // ── Mutations ──
    public bool TryPlaceCreature(Guid creatureId);  // false if full or already present
    public bool RemoveCreature(Guid creatureId);    // false if not present
}
```

**Save migration:** Existing saves serialized with `OccupantId: <guid>` deserialize into `OccupantIds: [<guid>]`. Backward-compatible. The legacy `Guid? OccupantId` field is removed from the deserialization surface — readers handle both shapes.

**Stale code removed:** `HabitatUnlockSchedule` (the level → total-habitats map) is now meaningless under the per-biome model. Delete the class. (Verified via grep: no callers outside its own definition.)

### `HabitatCapacity` (new)

Single source of truth for per-biome limits.

```csharp
public static class HabitatCapacity
{
    public const int CreaturesPerHabitat = 4;

    public static int MaxHabitatsForBiome(HabitatType type) => type switch
    {
        HabitatType.Water  or HabitatType.Grass    or HabitatType.Dirt     => 4,
        HabitatType.Fire   or HabitatType.Ice      or HabitatType.Electric => 3,
        HabitatType.Magical                                                => 2,
        _                                                                  => 0
    };

    /// <summary>
    /// Cost to unlock the Nth habitat of a biome (1-indexed within biome).
    /// First Earth-tier habitat is free (player owns it at game start).
    /// First mid/Magical habitat is story-gated (no coin cost; see GetUnlockReason).
    /// Reuses the existing HabitatExpansionCost ladder for slot index.
    /// </summary>
    public static int CoinsForHabitat(HabitatType biome, int oneIndexedSlot)
        => HabitatExpansionCost.Cost(oneIndexedSlot);
}
```

### Per-biome creature counts (informational)

Verified against `Data/CreatureRosterData.cs`:

| Biome  | Creature catalog | Habitats max | Total slots |
|--------|-----------------:|-------------:|------------:|
| Water    | 15 | 4 | 16 |
| Grass    | 15 | 4 | 16 |
| Dirt     | 15 | 4 | 16 |
| Fire     | 10 | 3 | 12 |
| Ice      | 10 | 3 | 12 |
| Electric | 10 | 3 | 12 |
| Magical  |  5 | 2 |  8 |
| **Total**| **80** | **23** | **92** |

Total slots exceed catalog by 12 — a buffer for breeding duplicates. Future-expansion path: bump `CreaturesPerHabitat` from 4 → 5/6/etc., or bump `MaxHabitatsForBiome` per biome. Both are one-line changes.

### `BiomeTheme` (new data record)

Per-biome environment configuration consumed by `HabitatEnvironmentView`.

```csharp
public record BiomeTheme(
    HabitatType  Biome,
    Color        BackgroundTopColor,
    Color        BackgroundBottomColor,
    Decoration[] Decorations,         // Hand-placed (B-tight; see "Biome Theme System")
    ParticleConfig? Particles,        // Bubbles for Water, embers for Fire, etc.
    LightShaft[] AmbientLights,
    FloorOverlay? Floor,              // Sandy floor, magma floor, etc.
    SurfaceLine? Surface,             // Water surface line; null for non-aquatic
    Rect2        WanderZone           // Creatures stay inside this rect (art-space coords)
);

public record Decoration(
    string PlaceholderEmoji,
    Vector2 PositionArtSpace,
    float SizePx,
    DecorationAnimation Animation = DecorationAnimation.None
);

public enum DecorationAnimation
{
    None,    // Static — coral, rocks, mushrooms
    Sway,    // Rotation oscillation around bottom-center — seaweed, grass blades
    Float,   // Vertical bob — runes, magical motes
    Drift    // Slow horizontal drift — background fish, butterflies
}

public record ParticleConfig(
    string PlaceholderEmoji,
    float  EmitRatePerSec,
    Vector2 RiseDirection,            // (0, -1) for bubbles, (0, 1) for falling embers, etc.
    float  MinLifetimeSec,
    float  MaxLifetimeSec,
    float  MinSize,
    float  MaxSize
);

public record LightShaft(float LeftPct, float WidthPx, float SkewDeg, float Opacity, float PulseDurSec);

public record FloorOverlay(Color TintTop, Color TintBottom, float HeightPx);

public record SurfaceLine(Color StartColor, Color MidColor, Color EndColor, float ShimmerDurSec);

public static class BiomeThemes
{
    public static BiomeTheme For(HabitatType biome);
}
```

The lookup table inside `BiomeThemes` is a `Dictionary<HabitatType, BiomeTheme>` populated with all 7 biomes. Initial values use placeholder emoji (🪸, 🪨, 🌿 for Water; 🍄, 🌱, 🦋 for Grass; 🪨, 💎 for Dirt; 🔥, 🪵 for Fire; ❄️, 🧊 for Ice; ⚡, 🔌 for Electric; ✨, 🔮 for Magical).

### `HabitatManager` interface additions

```csharp
public partial class HabitatManager : Node
{
    public IReadOnlyList<Habitat> HabitatsOfType(HabitatType biome);

    /// <summary>Tries to add a new habitat for the biome. Charges coins if applicable.</summary>
    public bool TryAddHabitat(HabitatType biome, out int coinsCharged);

    public bool TryPlaceCreatureInSlot(Guid habitatId, Guid creatureId);

    /// <summary>Releases a creature back to the wild. Removes from habitat AND from creature ledger.</summary>
    public bool ReleaseCreature(Guid habitatId, Guid creatureId);

    public UnlockReason GetUnlockReason(HabitatType biome, int oneIndexedSlot);
}

public enum UnlockReasonKind { Owned, Purchasable, StoryGated, OutOfRange }

public record UnlockReason(UnlockReasonKind Kind, int? Coins = null, int? StoryAct = null);
```

The screen never queries unlock rules directly. It calls `GetUnlockReason` which encapsulates the entire decision tree. Rules can be rebalanced separately without touching the screen.

## Visual Layout

Reference: `KeeperLegacyGodot/Mockups/habitat-category.html` (Water biome).

### Top translucent overlay bar (44px, full-width)

Anchored above content, semi-transparent dark background. Contains:
- **Back button** "◀ The Shop" → `MainScene.NavigateBack()`
- **Vertical separator**
- **Biome icon** (emoji placeholder) + **Habitat name** (e.g., "Water Habitats") + **Subtitle** (biome flavor text from theme: "Aquatic · Oceanic · Deep Sea")
- **Capacity pill** showing total creatures across owned habitats / max-if-all-unlocked (e.g., "5 / 16")
- **Coin display** ("✦ 1,250")

### Tab bar (just below overlay bar)

One tab per `MaxHabitatsForBiome(biome)` slot. Each tab queries `GetUnlockReason(biome, slot)` and renders one of three states:

- **Owned** — tab name "Habitat N" + creature count pill ("3/4"). Active tab highlighted with biome accent color and bottom border; inactive owned tabs dim.
- **Purchasable** — 🔒 icon + cost ("✦ 500"). Tap → confirm dialog → buy. Color treatment: muted, not greyed out (player should see this is reachable).
- **Story-gated** — 🔒 icon + Act number ("Act II"). Tap → toast "Continue your story to unlock." Greyed out fully.

### Environment view (left content area, ~64%)

Z-order layers (back to front):
1. Background gradient (`ColorRect` or two-stop gradient)
2. Ambient light shafts (animated `ColorRect`s with skew + opacity tween)
3. Particle effects (bubbles / embers / snow / sparks / runes)
4. Decorations (placeholder emoji `Label`s positioned per theme)
5. Surface line (water only, `ColorRect` with shimmer animation)
6. Wander zone visual (debug mode only)
7. Creatures (Y-sorted; each with drop shadow Sprite below)
8. Floor overlay (sandy / lava / icy / etc., bottom 55px area with gradient)

Bottom-right small label: "Habitat N · X / 4" (matches mockup).

### Roster panel (right content area, ~36%)

- **Header:** title "Habitat N — Roster" + flavor description from theme + capacity badge "3/4"
- **Body:** 2x2 `GridContainer` with 4 cells
  - Filled slot: creature blob + name + stage badge (no status dots — deferred)
  - Empty slot: dashed border + "+" icon + "Empty Slot" label + "Add Creature" button
- **No footer** (per Q8a — tabs handle both navigation and purchase)

## Components

### `HabitatCategoryScreen.cs`

**Role:** Orchestrator. Builds and wires the four UI children. Owns `_biome` and `_activeHabitat` state. Subscribes to manager signals. Routes user events (tab switch, purchase, creature click, add creature, release) to manager calls or sub-screen navigation.

**Key methods:**
- `_Ready()` — read `_biome` from `HabitatFloorScreen.SelectedHabitatType`; build children; subscribe signals
- `_ExitTree()` — unsubscribe signals
- `OnActiveHabitatChanged(habitatId)` — set `_activeHabitat`, push to env view + roster
- `OnBuyHabitatRequested(slot)` — show confirm dialog; on confirm call `HabitatManager.TryAddHabitat`
- `OnCreatureClicked(creatureId)` — set `HabitatDetailScreen.SelectedCreatureId`; navigate
- `OnAddCreatureRequested()` — show `ChoiceMenu` ("Buy from Shop" / "Breed new")
- `OnReleaseCreatureRequested(creatureId)` — show release confirm; on confirm call `HabitatManager.ReleaseCreature`
- `PrintBakeValues()` — emits paste-ready `BiomeTheme` for the active biome (debug)

### `HabitatOverlayBar.cs`

**Role:** Top bar render. Reactive to `ProgressionManager.CoinsChanged` for coin display.
**Public API:** `SetBiome(HabitatType)`, `SetCapacityText(int owned, int max)`.
**Signals emitted:** `BackPressed`.

### `HabitatTabBar.cs`

**Role:** Tab list render based on `MaxHabitatsForBiome`. Reactive to `HabitatsChanged`, `CoinsChanged`, `FeatureUnlocked`.
**Public API:** `SetBiome(HabitatType)`, `SetActiveHabitat(Guid)`.
**Signals emitted:** `ActiveHabitatChanged(Guid)`, `BuyHabitatRequested(int slot)`.

### `HabitatEnvironmentView.cs`

**Role:** Biome-themed scene composition. Builds layers from `BiomeTheme`. Spawns `WanderingCreature` child nodes for each occupant.
**Public API:** `SetTheme(BiomeTheme)`, `SetHabitat(Habitat)`, `EnterDebugMode(bool)`.
**Signals emitted:** `CreatureClicked(Guid)`.

**Wandering creatures (private inner class `WanderingCreature : Control`):**
- Each creature node owns a `_target` (Vector2) within the wander zone, retargeted every 3-6s
- `_Process` interpolates position toward `_target` with easing
- Bob animation via `AnimationPlayer` or scale tween (squash-stretch on Y axis)
- Z-index updated each frame based on Y position (top of screen = lower z; bottom = higher z) — Y-sorted rendering
- Drop shadow Sprite as a child Control (radial gradient ColorRect, ellipse shape, ~70% alpha black) positioned just under the creature
- Click emits `CreatureClicked` (suppressed when env view is in debug mode)
- Tap visual: ring highlight + temporary z-index boost for 0.3s so tap target is unobstructed

### `HabitatRosterPanel.cs`

**Role:** 4-slot grid render. Reactive to changes in active habitat's occupants.
**Public API:** `SetHabitat(Habitat)`.
**Signals emitted:** `CreatureClicked(Guid)`, `AddCreatureRequested()`, `ReleaseCreatureRequested(Guid)` (long-press fired).

**Slot rendering:**
- Filled: blob + name + stage. Click → `CreatureClicked`. 600ms long-press → `ReleaseCreatureRequested` (with visual fill animation during press; release before fill completes cancels).
- Empty: + icon + "Empty Slot" + "Add Creature" button. Tap → `AddCreatureRequested`.

### `ChoiceMenu.cs`

**Role:** Lightweight floating panel anchored to a screen position. Shows 2-N options as buttons. Tap-outside dismisses.
**Public API:** `Show(Vector2 anchor, IList<ChoiceOption> options)` where `ChoiceOption = (label, enabled, onTap, disabledReason)`.
**Reusable** — used here for "Add Creature" choices and could be used elsewhere.

### `HabitatPalette.cs`

**Role:** Centralizes colors for this screen. Single file to edit when palette rework happens.

```csharp
public static class HabitatPalette
{
    public static readonly Color OverlayBarBg     = new Color(0.047f, 0.035f, 0.027f, 0.80f);
    public static readonly Color BackButtonText   = new Color(0.91f, 0.72f, 0.19f, 0.75f);
    public static readonly Color SeparatorLine    = new Color(0.227f, 0.157f, 0.094f, 0.80f);
    public static readonly Color RosterPanelBg    = new Color(0.102f, 0.071f, 0.031f, 1.00f);
    public static readonly Color RosterCapacityBorder = new Color(1.00f, 1.00f, 1.00f, 0.30f);
    public static readonly Color SlotBgIdle       = new Color(1.00f, 1.00f, 1.00f, 0.05f); // tinted by biome at runtime
    public static readonly Color SlotBgSelected   = new Color(1.00f, 1.00f, 1.00f, 0.15f); // tinted by biome at runtime
    public static readonly Color SlotEmptyBorder  = new Color(1.00f, 1.00f, 1.00f, 0.15f); // tinted by biome at runtime
    public static readonly Color LabelName        = new Color(0.94f, 0.91f, 0.85f, 1.00f);
    public static readonly Color LabelMuted       = new Color(0.60f, 0.50f, 0.44f, 1.00f);
}
```

Biome accent colors come from `BiomeTheme.BackgroundTopColor` / a dedicated `AccentColor` field on the theme. Per-biome theming applies tint at runtime.

## Biome Theme System

**Approach: B-tight initially, evolution path to B-curated.**

Each biome's `Decorations[]` is a hand-placed array of one layout. Position values come from drag-mode tuning sessions (same workflow as pedestal positions).

**Future path to B-curated:** Change `BiomeTheme.Decorations` from `Decoration[]` to `DecorationLayout[]` where `DecorationLayout = Decoration[]`, and have `HabitatEnvironmentView` pick a random index on load. Adding more layouts is purely additive — no code change to the env view itself, just larger configs.

**Placeholder strategy:** All decorations use emoji `Label`s (`🪸`, `🌿`, `🪨`, etc.). When real biome decoration art lands, swap the `Decoration` record from `(emoji, position, size)` to `(texturePath, position, size)` and the env view's render code switches from `Label` to `TextureRect`. One-shot change per biome at art-drop time.

## Data Flow & State

### Static cross-screen state

- `HabitatFloorScreen.SelectedHabitatType` — set when a pedestal is clicked, read on category screen `_Ready` (already exists)
- `HabitatDetailScreen.SelectedCreatureId` — new; set when a creature is clicked on category screen, read on detail screen

### Screen-local state

- `_biome : HabitatType` — set once on load
- `_activeHabitat : Habitat` — current tab's habitat; defaults to first owned, preserved across back-navigation from Detail
- `_pendingPurchaseSlot : int?` and `_pendingReleaseCreature : Guid?` — set while a confirm dialog is open

### Manager signal subscriptions (single point — the screen)

| Signal | Refresh action |
|---|---|
| `HabitatManager.HabitatsChanged` | Re-render `TabBar`. If `_activeHabitat` was deleted (defensive), reset to first owned |
| `HabitatManager.CreaturesChanged` | Re-render `EnvView` + `RosterPanel` |
| `ProgressionManager.CoinsChanged` | Re-render `TabBar` (purchasable affordability) + `OverlayBar` (coin text) |
| `ProgressionManager.FeatureUnlocked` | Re-render `TabBar` (story-gated → purchasable transitions) |

Children do not subscribe directly. They are pure-render given the data the screen pushes via `Set*` methods. Keeps them testable and easy to reason about in isolation.

### User action flows

1. **Tab switch (owned)** — `TabBar` emits `ActiveHabitatChanged` → screen updates `_activeHabitat` → calls `EnvView.SetHabitat` and `RosterPanel.SetHabitat`.
2. **Habitat purchase** — `TabBar` emits `BuyHabitatRequested(slot)` → screen shows confirm dialog → on confirm: `HabitatManager.TryAddHabitat(_biome)` → manager emits `HabitatsChanged` + `CoinsChanged` → tabs re-render → screen auto-switches `_activeHabitat` to the newly-bought one.
3. **Creature click** — either component emits `CreatureClicked(id)` → screen sets `HabitatDetailScreen.SelectedCreatureId = id` → `MainScene.NavigateToSubScreen("HabitatDetail")`.
4. **Add Creature** — `RosterPanel` emits `AddCreatureRequested` → screen shows `ChoiceMenu` anchored to slot → "Buy from Shop" / "Breed new" routes to respective screens with biome static filter.
5. **Release** — `RosterPanel` emits `ReleaseCreatureRequested(id)` (after 600ms long-press) → screen shows release confirm → on confirm: `HabitatManager.ReleaseCreature` → `CreaturesChanged` → re-render.

## Debug-Tuning Surface

Per the project rule "visual features ship with in-game tuning controls from day one."

### Debug input ownership

`HabitatCategoryScreen` overrides `_Input` for screen-specific debug keys. `MainScene` keeps only global keys (F1 story, F2 level, F3 features) and continues to own the on-screen DEBUG overlay panel as shared display infrastructure.

This is a small architectural shift from the existing pedestal debug (which lives in `MainScene._Input`). The pedestal debug logic SHOULD eventually migrate to `HabitatFloorScreen._Input` for consistency — flag as opportunistic follow-up, not part of this spec.

### Drag-tunable elements

**1. Decoration positions** — when debug mode active, every `Decoration` in the active biome's theme renders inside a wrapper `Control` that handles mouse drag. Drag updates `Vector2 PositionArtSpace` (un-scaled from viewport via the same inverse transform used in `HabitatFloorScreen.UnScalePosition`).

**2. Wander zone** — drawn as a translucent colored rectangle with **four corner handles + four edge midpoint handles** (8 drag points total). Corners resize from that corner; edge midpoints move only that edge.

**3. Decoration scale** — last-clicked decoration is "selected" (visual indicator). `O` cycles its scale through `[0.5x, 0.7x, 1.0x, 1.5x, 2.0x, 3.0x]`. `Tab` advances selection to next decoration.

### Key map

| Key | Action |
|---|---|
| **F4** or **P** | Toggle debug mode on/off |
| **Drag** | Reposition decoration / wander handle |
| **O** | Cycle scale of selected decoration |
| **Tab** | Switch focus between decorations |
| **Ctrl+S** | Print paste-ready `BiomeTheme` for active biome (no exit) |
| **F4 / P** when on | Print + exit |

### On-screen overlay (when debug mode active)

```
DEBUG — Biome Theme Editor (Water)
  Mode:        Decoration drag
  Selected:    🪸 #2 (scale 1.0x)
  Decorations: 12
  Wander zone: (80, 100) → (740, 380)
  -----------------------------------
  Drag        : reposition
  O           : cycle scale (selected)
  Tab         : next decoration
  Ctrl+S      : print bake values
  F4 / P      : print + exit
```

### Print output format (via `HabitatCategoryScreen.PrintBakeValues`)

```
=== BIOME THEME — WATER ============================
// Paste into KeeperLegacyGodot/Data/BiomeTheme.cs:
[HabitatType.Water] = new BiomeTheme(
    Biome: HabitatType.Water,
    BackgroundTopColor:    Color("#061828"),
    BackgroundBottomColor: Color("#1E2A1E"),
    Decorations: new[]
    {
        new Decoration("🪸", new Vector2(  56, 410), 26f),
        new Decoration("🪸", new Vector2( 168, 408), 20f),
        // ...
    },
    WanderZone: new Rect2(80, 100, 660, 280),
    Particles: ...,
    AmbientLights: ...,
    Floor: ...,
    Surface: ...
);
=====================================================
```

I bake by pasting verbatim — same workflow as the pedestal positions.

### Not runtime-debug-tuned

- **Panel split ratio** (env view vs roster panel): code constant. Tune once.
- **Particle emit rates / animation timings**: text constants in `BiomeTheme` records. Hot-reload to test.
- **Roster slot grid spacing**: code constants.
- **Tab bar font sizes / heights**: code constants.

Rule of thumb: drag-tune is for things you'll re-tune more than once and want to *see* while adjusting.

## Mobile-First Constraints

Per project memory: this game ships to iOS / iPadOS. All interactions designed for single-touch, no mouse hover, no keyboard required.

- **Minimum touch target 44x44px** (Apple HIG / Material guideline) for every tap target — tabs, buttons, creature blobs, slot cells
- **No hover-only states.** Information is either always visible (cost on locked tab) or revealed by tap (toast on story-gated tab)
- **Long-press for destructive only.** Release is the only non-tap interaction, gated by 600ms hold + visual fill animation
- **Tap-outside-to-dismiss** on all dialogs and the choice menu
- **No right-click semantics anywhere**

## Edge Cases

### Empty / locked states

- **Zero unlocked habitats for biome** (F/I/E/M before story unlock): pedestal click is already blocked on the floor screen by `IsLocked`. No special category-screen UI needed; the screen is unreachable in this state.
- **Active habitat is empty** (0 of 4 creatures): env view renders only background + decorations + ambient effects. Roster shows 4 empty slots. Normal state, no warning copy.

### Purchase flow

- **Insufficient coins for purchasable tab.** Tab still tappable. Confirm dialog opens with cost shown; "Buy" button disabled with red muted text "You have ✦ 320 / ✦ 500." Player can dismiss.
- **Funds drop between dialog open and confirm.** Re-check on tap. If insufficient, dismiss + toast "Not enough coins anymore."
- **Newly-purchased habitat becomes active automatically** for UX continuity.

### Creature lifecycle

- **Release a creature reserved by an active customer order.** `ReleaseCreature` checks `OrderManager.IsCreatureReserved(id)`; if reserved, returns false and screen toasts "This creature is promised to a customer. Cancel the order first to release them." Order cancellation lives on Orders screen — out of scope here, hook is in place.
- **Creature deleted while Detail screen open** (back-nav): on `_EnterTree` from sub-screen, screen re-pulls habitat data; missing IDs render as empty slots. No crash.
- **Long-press on roster slot:** 600ms threshold + visual fill animation. Lift-off before fill completes cancels.

### Debug mode

- **Tap on creature while debug mode on:** does NOT navigate to Detail. Selects creature for visualizing its wander target / target-line debug overlay.
- **Decoration / wander zone dragged outside env view bounds:** no clamping. Print whatever positions exist. Predictable over magical.

### Defensive coding

- **`BiomeThemes.For(type)` returns null** (missing config): fallback to neutral grey-brown background, no decorations, default wander zone. `GD.PushWarning` to surface in dev.
- **Stale `_activeHabitat` after save reload:** reset to first owned on `HabitatsChanged`.
- **Manager signals fire while screen tearing down:** `_ExitTree` unsubscribes before disposal sequence.

## Testing

### Unit tests (extending `Tests/HabitatModelTests.cs`)

- `Habitat.TryPlaceCreature` — boundary cases (0/3/4 occupants), duplicate ID rejection
- `Habitat.RemoveCreature` — present / not-present cases
- `Habitat.IsFull` / `IsEmpty` / `AvailableSlots` — at 0, 1, 4
- Save/load migration: deserializing `OccupantId: <guid>` → `OccupantIds: [<guid>]`
- `HabitatCapacity.MaxHabitatsForBiome` — all 7 enum values
- `HabitatCapacity.CoinsForHabitat` — matches existing `HabitatExpansionCost` ladder

### Manager tests (extending `Tests/HabitatManagerTests.cs`)

- `TryAddHabitat` succeeds when funds + capacity allow; fails on either; charges coins via `ProgressionManager`
- `TryAddHabitat` emits `HabitatsChanged` + `CoinsChanged`
- `TryPlaceCreatureInSlot` rejects on full / already-housed
- `ReleaseCreature` returns false when creature is order-reserved (mock `OrderManager`)
- `GetUnlockReason` returns correct state for owned / purchasable / story-gated / out-of-range slots

### No automated UI tests

Godot scene-tree testing infrastructure is not currently established in the project; adding it is out of scope here. UI components are kept as thin pure-render given input data, so most testable logic lives in the manager and model layers.

### Manual integration smoke test

Sign-off checklist (will be itemized in the implementation plan):

1. Boot → click Water pedestal → category screen loads
2. Tab between owned habitats → env view + roster update
3. Click locked-purchasable tab → confirm dialog → buy → new tab appears + becomes active
4. Click creature in env view → routes to Detail placeholder
5. Click creature in roster slot → routes to Detail placeholder
6. Empty slot → "Add Creature" → choice menu → "Buy from Shop" navigates to Shop with biome filter
7. Long-press filled slot → release confirm → release → roster + env view update
8. F4/P → debug overlay appears, decorations become draggable, wander zone handles visible
9. Drag a decoration, press Ctrl+S → paste-ready output in Output panel
10. F4/P off → values revert to baked; layout matches pre-debug visuals

## Resolved Decisions Log

For future readers — captured during the brainstorming session 2026-05-04.

1. **Multi-creature habitats.** Each `Habitat` holds up to 4 creatures via `OccupantIds: List<Guid>`. The pre-existing single-occupant model is replaced.
2. **Per-biome capacity.** Water/Grass/Dirt = 4 habitats max; Fire/Ice/Electric = 3; Magical = 2. Sized to fit one of each catalog creature with small spare.
3. **Unlock model.** W/G/D first habitat unlocked at game start. F/I/E/Magical first habitat gated by story milestone. Additional habitats purchased with coins via existing `HabitatExpansionCost` ladder.
4. **Tab display.** All max habitats rendered as tabs (owned / purchasable / story-gated states). Locked tabs show their unlock condition.
5. **Spec scope.** Habitat Category screen only. Detail screen and creature stat system are separate specs.
6. **Environment scope.** Approach B (generic env view + per-biome data config) with B-tight decoration placement (one hand-placed layout per biome). Evolution path to B-curated (multiple layouts) is purely additive.
7. **Creature interactions.** Click → navigates to Detail screen (per Q4a-A). "Add Creature" empty-slot → choice menu (Buy from Shop / Breed new), no creature inventory (per Q4b-D + project memory constraint).
8. **Footer.** Removed entirely; tabs handle navigation and purchase.
9. **Release mechanic.** Release to wild — free, no reward, creature removed from ledger. Order fulfillment (different flow on Orders screen) is what pays out coins.
10. **Status dots.** Deferred to Detail spec. Roster slots show clean creature info without the four hunger/happy/clean/affection dots from the mockup.
11. **Y-sort + drop shadows** for creature overlap rendering. No avoidance steering in v1.
12. **Mobile-first.** All controls single-tap; long-press only for release; 44x44px minimum touch target; no hover-only states.
13. **Debug input ownership.** Per-screen `_Input` rather than `MainScene._Input` for screen-specific keys. Pedestal debug migration to `HabitatFloorScreen._Input` is opportunistic follow-up, not blocking this spec.

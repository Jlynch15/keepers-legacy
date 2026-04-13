# Habitat Floor Screen Design — Keeper's Legacy (Godot/C#)

## Context

The navigation shell is complete — sidebar, crossfade transitions, and immersive overlays all work. The habitat floor is the main hub screen (Home), currently a placeholder. This spec replaces it with the real scene: a pre-rendered isometric room with interactive pedestal habitats, creature display, and live HUD.

**Scope:** Core gameplay elements only. Decorations (lanterns, vines, wall plants, floor plants, welcome mat, entrance door, counter) are deferred to a future polish pass.

## Rendering Approach

**Pre-rendered background + interactive overlays.** The isometric room (walls + floor + pedestal shapes) is a static PNG exported from the HTML mockup. Clickable areas, creatures, labels, and HUD are Godot nodes layered on top.

This approach was chosen because:
- Pixel-perfect match to the approved mockup
- Simplest code — room is one TextureRect
- Easy to swap with real art later
- No complex polygon geometry in code

## Room Background

- **Source:** Hand-drawn art provided by Jesse (replaces HTML mockup export)
- **File:** `Sprites/Backgrounds/habitat_floor_bg.png` at 1364x768
- **Rendering:** `TextureRect` node, fills content area (viewport minus 70px sidebar)
- **Stretch mode:** `KeepAspectCentered` so it scales without distortion
- **Note:** Pedestal positions will be placed visually on top of the art — coordinates in this spec are approximate starting points and will be adjusted to match the final background

## Pedestal Habitats

Seven pedestal hotspots positioned to align with the pedestals in the background image.

### Positions (pixel coordinates on the 1210x720 background)

| Habitat | X | Y | Row |
|---------|-----|-----|-----|
| Water | 280 | 340 | Back |
| Grass | 590 | 220 | Back |
| Dirt | 880 | 320 | Back |
| Fire | 380 | 460 | Mid |
| Ice | 620 | 385 | Mid |
| Electric | 880 | 475 | Mid |
| Magical | 590 | 600 | Front |

### Interaction

- Each pedestal is a clickable `Control` node (e.g. `TextureButton` or `Button` with custom draw) positioned at the coordinates above
- Hitbox: roughly 160x80px diamond area centered on position (matching the pedestal top face)
- **Click unlocked pedestal →** Navigate to `HabitatCategoryScreen` via `MainScene.NavigateToSubScreen("HabitatCategory")`, passing the habitat type
- **Locked pedestals:** non-interactive, semi-transparent dark overlay to indicate locked state
- **Hover (unlocked):** subtle brightness increase or glow outline

### Lock State

Read from game state:
- Water, Grass, Dirt: always available (base habitat types)
- Fire, Ice, Electric: unlocked via level progression (HabitatManager unlock schedule)
- Magical: requires Story Act II (`ProgressionManager.IsFeatureUnlocked(GameFeature.MagicalHabitat)`)

For the initial new-game state, only Water is populated (starting habitat). Others may be unlocked but empty.

### Labels

Floating above each pedestal:
- Habitat type name (Cinzel font, 11px, white with drop shadow)
- Creature count: "{current}/{max}" (Cinzel, 9px, muted #B8A080)
- Locked pedestals show a lock badge instead of count: "🔒 Locked"
- Label position: ~30px above the pedestal center

## Creature Display on Pedestals

Placeholder creatures shown as colored circles with initials until real sprite art arrives.

### Appearance
- **Shape:** Circle, 32px diameter
- **Color:** Matched to habitat type:
  - Water: #4AA8E0
  - Grass: #4AB84A
  - Dirt: #C08840
  - Fire: #E06030
  - Ice: #60D0E0
  - Electric: #E8D020
  - Magical: #9860E0
- **Text:** First letter of creature name, centered, white, bold, 14px
- **Shadow:** Subtle drop shadow for depth

### Animation
- Gentle idle bob: translateY ±4px, 2.5s sinusoidal loop
- Each creature has a random phase offset so they don't bob in sync

### Data Source
- Read creature occupants from `HabitatManager.Creatures` and `HabitatManager.Habitats`
- Show up to 2 creature blobs per pedestal (space constraint on the diamond)
- Position creatures on the top face of the pedestal, slightly offset from center

## HUD Elements

### Store Sign (top-left)
- **Position:** (16, 10) from content area top-left
- **Content:** "Keeper's Legacy" title + "Creature Emporium" subtitle
- **Style:** Dark semi-transparent background (rgba 30,22,8,0.9), gold border (1.5px, rgba 232,184,48,0.45), rounded 4px
- **Fonts:** Title: Cinzel 15px bold, gold #E8B830. Subtitle: IM Fell English italic 11px, muted gold.
- **Behavior:** Static display only, not interactive

### Info Strip (top-right)
- **Position:** Right-aligned, 16px from top and right edge of content area
- **Background:** Semi-transparent dark pill (rgba 30,22,10,0.80), blur backdrop, rounded 16px
- **Content (left to right):**
  1. **Level badge:** Gold pill (#E8B830 bg, dark text), "Lv. {level}" — Cinzel 11px bold
  2. **XP bar:** 90px wide, 6px tall, dark track with gold fill proportional to `currentXP / xpToNextLevel`
  3. **Coin count:** "✦ {coins}" — Cinzel 12px, gold-light #F0CC50

### Live Data Wiring
- Subscribe to `ProgressionManager.XPChanged` → update XP bar fill and level badge
- Subscribe to `ProgressionManager.CoinsChanged` → update coin count
- Subscribe to `ProgressionManager.LeveledUp` → update level badge
- Initialize all values from ProgressionManager properties on `_Ready()`

### Story Badge (below info strip)
- **Position:** Right-aligned, below info strip (~46px from top)
- **Visible only** when `StoryManager.HasPendingEvent()` returns true
- **Content:** "✦ {NPC name} awaits..." — Cinzel 10px bold, white
- **Style:** Purple background (rgba 152,96,224,0.85), purple border, rounded 20px, pulsing glow animation (box-shadow 2s ease-in-out infinite)
- **Click:** Triggers immersive story screen (calls `MainScene.OnStoryEventPending()` or emits a signal that MainScene handles)
- Subscribe to `StoryManager.StoryEventPending` and `StoryManager.StoryEventCompleted` to show/hide

## Key Files

```
KeeperLegacyGodot/
├── UI/
│   └── Habitat/
│       ├── HabitatFloorScreen.tscn    (replace existing placeholder)
│       ├── HabitatFloorScreen.cs      (new — room logic, pedestal setup, HUD wiring)
│       └── PedestalNode.cs            (new — reusable pedestal component: hitbox + label + creatures)
├── Sprites/
│   └── Backgrounds/
│       └── habitat_floor_bg.png       (new — exported room background)
```

## Navigation Integration

- Clicking a pedestal calls `MainScene.NavigateToSubScreen("HabitatCategory")` 
- The habitat type needs to be passed to the category screen — use a static property or signal: `HabitatFloorScreen.SelectedHabitatType`
- MainScene already supports sub-navigation with back stack
- Sidebar stays on "Home" highlight during Floor → Category → Detail navigation

## Color Reference

```
Habitat type colors (used for creature blobs and pedestal accents):
  Water: #4AA8E0    Grass: #4AB84A    Dirt: #C08840
  Fire:  #E06030    Ice:   #60D0E0    Electric: #E8D020
  Magical: #9860E0

HUD colors:
  Gold: #E8B830     Gold light: #F0CC50
  Text: #F0E8D8     Muted: #B8A080
  Dark bg: rgba(30,22,10,0.80)
  Story purple: rgba(152,96,224,0.85)
```

## Verification

1. **Launch:** Press F5 in Godot. Habitat floor shows the warm cabin room background with 7 pedestals visible.
2. **Pedestals:** Unlocked pedestals show habitat name and creature count. Locked ones show lock badge and are dimmed.
3. **Creatures:** Colored circles with initials appear on pedestals that have creatures. Idle bob animation running.
4. **HUD - Level/XP:** Info strip shows current level, XP bar, and coin count. Press F3 to unlock features and verify values update.
5. **HUD - Story:** If a story event is pending, purple badge appears. Clicking it triggers the immersive story screen.
6. **Navigation:** Click an unlocked pedestal → crossfade to Habitat Category placeholder. Back button returns to floor.
7. **Resize:** Window at different resolutions — room scales correctly, HUD stays anchored to corners.

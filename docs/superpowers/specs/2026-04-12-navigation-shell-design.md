# Navigation Shell Design — Keeper's Legacy (Godot/C#)

## Context

Keeper's Legacy is transitioning from a dual-codebase approach (SwiftUI + Godot) to **Godot/C# as the single codebase** targeting Steam (Windows/Mac/Linux) and iOS/iPadOS. The backend is complete — 8 manager singletons, 7 domain models, save system, and 110 passing tests. Seven HTML mockups define the UI. No Godot scenes exist yet.

This spec defines the **navigation shell**: the persistent sidebar, screen container, transition system, and immersive overlay layer. It is the skeleton that all future screen implementations plug into. The goal is a clickable app where every nav button works, screens crossfade, and immersive events (story/level-up) take over the full screen — all with placeholder content.

## Platform & Resolution

- **Engine:** Godot 4.2+ with C#/.NET 6.0
- **Primary development:** Windows 11
- **Ship targets:** Steam (Windows/Mac/Linux), iOS, iPadOS
- **Orientation:** Landscape on all platforms
- **Base design resolution:** 1280x720
- **Steam resolutions (player-selectable in Settings):** 1280x720 (720p), 1920x1080 (1080p), 2560x1440 (1440p)
- **iOS resolutions (auto-detected, not player-selectable):** 2532x1170 (iPhone 14/15), 2796x1290 (iPhone Pro Max), 2732x2048 (iPad Pro 12.9"), 2360x1640 (iPad Air)
- **Scaling:** Game renders at base 1280x720 and scales up. Godot stretch mode `canvas_items`, aspect `keep`.

## Scene Architecture

Single persistent `MainScene.tscn` with four layers:

```
MainScene.tscn
├── Sidebar              (always visible, 70px, right edge)
├── ContentContainer     (holds one screen scene at a time)
├── TransitionOverlay    (crossfade + fade-to-black ColorRect layers)
└── ImmersiveLayer       (full-screen takeover for story/level-up)
```

### Sidebar (always visible)

- **Width:** 70px, anchored to right edge
- **Background:** #1A1208 (dark panel color from mockups)
- **Buttons (top to bottom):** Home, Shop, Orders, Breed, Pedia — then Settings separated at bottom
- **Each button:** Icon + label text, vertical stack
- **Active state:** Gold accent (#E8B830) highlight, gold text
- **Inactive state:** Muted icon/text (#9A8070)
- **Locked features:** Lock icon overlay, dimmed, non-clickable (e.g. Breed before level 12 + Act I)
- **Sidebar hides during immersive screens** (story events, level-up)

### ContentContainer (screen swapping)

- **Fills viewport minus sidebar** (0,0 to viewport_width - 70, viewport_height)
- **Holds one child scene at a time**
- **On navigation:** Old screen fades out → is freed → new screen instantiated → fades in
- **Each screen is an independent .tscn file** loaded via `PackedScene.Instantiate()`

### TransitionOverlay

- **Crossfade (main screens):** ~0.3s. A `ColorRect` covers ContentContainer, fades from transparent to opaque while old screen is replaced, then fades back to transparent. Sidebar stays visible throughout.
- **Fade-to-black (immersive entry):** ~0.5s total. `ColorRect` covers entire viewport (including sidebar). Fades to black → sidebar hides → immersive content appears → fades from black. Reverse on exit.

### ImmersiveLayer

- **Sits above all other layers** (highest z-index / CanvasLayer)
- **Used by:** StoryEventScreen, LevelUpScreen
- **Entry:** Triggered by manager signals (StoryManager.StoryEventPending, ProgressionManager.LeveledUp)
- **Exit:** Screen emits completion signal → fade to black → remove immersive → show sidebar → fade from black → resume previous screen

## Screen Scenes

Each screen is a standalone `.tscn` file with a root Control node:

| Scene File | Type | Sidebar Visible | Notes |
|---|---|---|---|
| `HabitatFloorScreen.tscn` | Main | Yes | Default/home screen. Isometric room. |
| `HabitatCategoryScreen.tscn` | Sub | Yes | Drills down from floor. Has back button. |
| `HabitatDetailScreen.tscn` | Sub | Yes | Drills down from category. Has back button. |
| `ShopScreen.tscn` | Main | Yes | Full-width with overlay bar. |
| `OrdersScreen.tscn` | Main | Yes | Full-width with overlay bar. |
| `BreedingScreen.tscn` | Main | Yes | Locked until level 12 + Act I. |
| `PediaScreen.tscn` | Main | Yes | Creature encyclopedia. |
| `SettingsScreen.tscn` | Main | Yes | Resolution picker, save/load, about. |
| `StoryEventScreen.tscn` | Immersive | No | Full-screen story dialogue. |
| `LevelUpScreen.tscn` | Immersive | No | Full-screen celebration. |

### Sub-navigation

Habitat Floor → Category → Detail forms a navigation stack within the ContentContainer. Back button in the overlay bar pops back. Same crossfade transition. The sidebar active button stays on "Home" throughout this stack.

## Key Files to Create

```
UI/
├── Main/
│   ├── MainScene.tscn         (root scene — sidebar + container + overlays)
│   └── MainScene.cs           (screen management, transitions, signal wiring)
├── Components/
│   ├── Sidebar.tscn           (reusable sidebar component)
│   ├── Sidebar.cs             (nav button logic, active state, lock state)
│   ├── OverlayBar.tscn        (reusable top bar with back/title/coins)
│   └── OverlayBar.cs          (back button, title text, coin display)
├── Transitions/
│   └── TransitionManager.cs   (crossfade + fade-to-black logic)
├── Habitat/
│   ├── HabitatFloorScreen.tscn
│   ├── HabitatCategoryScreen.tscn
│   └── HabitatDetailScreen.tscn
├── Shop/
│   └── ShopScreen.tscn
├── Orders/
│   └── OrdersScreen.tscn
├── Breeding/
│   └── BreedingScreen.tscn
├── Pedia/
│   └── PediaScreen.tscn
├── Settings/
│   └── SettingsScreen.tscn
├── Story/
│   └── StoryEventScreen.tscn
└── Overlays/
    └── LevelUpScreen.tscn
```

## Manager Integration

The navigation shell connects to existing autoload managers:

- **ProgressionManager** → Sidebar reads `IsFeatureUnlocked()` to show/hide lock icons. `LeveledUp` signal triggers immersive level-up screen.
- **StoryManager** → `StoryEventPending` signal triggers immersive story screen.
- **GameManager** → `GameLoaded` signal triggers initial screen display.
- **All managers** → Individual screens will subscribe to relevant signals when implemented (future work, not part of this shell spec).

## Color Reference (from approved mockups)

```
Sidebar/overlays:  bg #1A1208, border #3A2818, text #F0E8D8, muted #9A8070
Gold accents:      #E8B830 (primary), #F0CC50 (light)
Cabin palette:     bg #2A1E10, wall #3E2A14, floor #4A3418
Transition black:  #000000
```

## Fonts

- **Headers:** Cinzel
- **Body text:** Crimson Text
- **Dialogue/italic:** IM Fell English
- Fonts bundled as .ttf in `Resources/Fonts/`

## What This Phase Delivers

- Open the Godot project → hit Play → see the habitat floor placeholder with working sidebar
- Click any sidebar button → crossfade to that screen's placeholder
- Locked buttons (Breed) show lock icon, do nothing on click
- Trigger a test story event → fade to black → full-screen story placeholder → fade back
- Trigger a test level-up → same immersive flow
- Settings screen includes resolution picker
- All transitions feel polished (crossfade 0.3s, fade-to-black 0.5s)

## Verification

1. **Launch:** Open project in Godot 4.2+ (.NET), press F5. MainScene loads with sidebar + habitat floor placeholder.
2. **Navigation:** Click each sidebar button. Confirm crossfade transition, correct active highlight, and correct placeholder screen appears.
3. **Locked features:** Breed button shows lock icon. Clicking does nothing. (Can test unlock by modifying ProgressionManager state.)
4. **Immersive entry:** Add a test trigger (button or signal call) that fires `StoryManager.StoryEventPending`. Confirm: fade to black → sidebar hides → story placeholder appears full-screen.
5. **Immersive exit:** Dismiss story screen. Confirm: fade to black → sidebar returns → previous screen restored with crossfade.
6. **Resolution:** Open Settings, change resolution. Confirm window resizes correctly.
7. **Sub-navigation:** From habitat floor, navigate to category → detail. Back button returns correctly through the stack.

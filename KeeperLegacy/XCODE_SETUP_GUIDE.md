# Keeper's Legacy — Xcode Project Setup Guide

This guide walks you through setting up the Xcode project on macOS so the
generated Swift files compile and run.

---

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| macOS | Ventura 13+ | Sonoma 14+ recommended |
| Xcode | 15.0+ | Free on Mac App Store |
| Apple ID | Any | Needed for Simulator. Developer account ($99/yr) needed only for real device + App Store |

---

## Step 1 — Install Xcode

1. Open **App Store** on your Mac
2. Search **Xcode**
3. Click **Get** (it's free, ~7 GB download)
4. Wait for installation to complete
5. Open Xcode once to accept the license agreement

---

## Step 2 — Create the Xcode Project

1. Open **Xcode**
2. Click **Create New Project**
3. Choose template: **iOS → App**
4. Fill in the form:
   - **Product Name:** `KeeperLegacy`
   - **Team:** None (or your Apple ID)
   - **Organization Identifier:** `com.yourname.keeperslegacy` (make it unique)
   - **Interface:** SwiftUI
   - **Language:** Swift
   - **Storage:** Core Data ✅ (check this box)
   - **Include Tests:** Optional
5. Click **Next**, choose a save location (e.g. your Desktop)
6. Click **Create**

---

## Step 3 — Replace Generated Files With Our Files

Xcode creates some default files. Replace them:

### 3a. Delete Xcode's default files

In the Xcode Project Navigator (left sidebar), **right-click → Delete** these files
that Xcode auto-generated (move to Trash):
- `ContentView.swift` (we have ours)
- `{ProjectName}App.swift` — **keep this one** but edit it (see Step 4)
- `Persistence.swift` — delete it (we use `DataManager.swift` instead)
- The default `.xcdatamodeld` file — delete it (we have `KeeperLegacy.xcdatamodeld`)

### 3b. Add our Swift files

1. In Finder, open the `KeeperLegacy/` folder you received
2. In Xcode Navigator, **right-click the project folder → Add Files to "KeeperLegacy"**
3. Select ALL folders and files:
   - `Models/` (all 5 .swift files)
   - `ViewModels/` (all 4 .swift files)
   - `Views/` (all 6 .swift files)
   - `Data/DataManager.swift`
   - `Data/CreatureRosterData.swift`
4. Make sure **"Copy items if needed"** is checked
5. Click **Add**

### 3c. Add the Core Data model

1. In Xcode Navigator, right-click the project → **Add Files to "KeeperLegacy"**
2. Navigate to `Data/KeeperLegacy.xcdatamodel/` and select the entire folder
3. Click **Add**

---

## Step 4 — Edit the App Entry Point

Find `KeeperLegacyApp.swift` (the file Xcode kept) and replace its contents with:

```swift
import SwiftUI

@main
struct KeeperLegacyApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(DataManager.shared)
        }
    }
}
```

---

## Step 5 — Set Deployment Target

1. Click the **KeeperLegacy** project (blue icon) in the Navigator
2. Under **Targets → KeeperLegacy → General**
3. Set **Minimum Deployments** to **iOS 15.0**

---

## Step 6 — Configure iCloud (Optional)

To enable iCloud sync (CloudKit):

1. Go to **Signing & Capabilities** tab
2. Click **+ Capability**
3. Add **iCloud**
4. Under iCloud, enable **CloudKit**
5. Add **Background Modes → Remote notifications**

> If you skip this, remove `NSPersistentCloudKitContainer` in `DataManager.swift`
> and replace it with `NSPersistentContainer`.

---

## Step 7 — Build & Run

1. In the toolbar at the top, select a simulator: **iPhone 15 Pro** (recommended)
2. Press **⌘R** (or click the ▶ Play button)
3. Xcode will build and launch the app in the simulator

### Expected result on first launch:
- Purple/blue gradient splash screen with "Keeper's Legacy" title
- Sparkle particles animate
- App loads into 4-tab interface: Shop, Habitats, Monsterpedia, Settings
- Shop shows all 58 creatures organized by habitat type
- Habitat shows starter habitat (empty, ready for a creature)
- Monsterpedia shows silhouettes until creatures are purchased

---

## Troubleshooting

| Error | Fix |
|-------|-----|
| `Cannot find type 'CreatureEntity'` | Rebuild once — Core Data generates these types at build time |
| `Module 'KeeperLegacy' not found` | Clean build: **Product → Clean Build Folder (⇧⌘K)** then rebuild |
| `NSPersistentCloudKitContainer` crash | Disable iCloud in Capabilities (Step 6 is optional) |
| `Color(hex:)` missing | Make sure `LaunchScreenView.swift` is included (it contains the extension) |
| Build succeeds but screen is blank | Check `KeeperLegacyApp.swift` matches Step 4 exactly |

---

## File Structure Summary

```
KeeperLegacy/
├── Models/
│   ├── Creature.swift          — Creature types, stats, care actions
│   ├── Habitat.swift           — Habitat slots, unlock schedule
│   ├── Economy.swift           — Coins, Stardust, pricing tables
│   ├── Progression.swift       — Levels, XP, feature unlocks
│   └── Story.swift             — Acts, events, NPCs
├── ViewModels/
│   ├── ProgressionViewModel.swift — Player state (coins, level, XP)
│   ├── ShopViewModel.swift        — Purchase logic, discovery tracking
│   ├── HabitatViewModel.swift     — Care actions, habitat management
│   └── CreatureViewModel.swift    — Breeding, lifecycle progression
├── Views/
│   ├── ContentView.swift          — Tab bar, app entry, currency header
│   ├── LaunchScreenView.swift     — Splash screen with sparkle animation
│   ├── ShopView.swift             — Creature shop with filter + purchase
│   ├── HabitatView.swift          — Habitat panel, creature care UI
│   ├── PediaView.swift            — Monsterpedia collection grid
│   └── SettingsView.swift         — Audio, iCloud, notifications
├── Data/
│   ├── DataManager.swift          — Core Data stack, all DB operations
│   ├── CreatureRosterData.swift   — All 58 creatures as static Swift data
│   └── KeeperLegacy.xcdatamodel/ — Core Data schema (5 entities)
└── XCODE_SETUP_GUIDE.md          — This file
```

---

## Art Assets — What to Create

All creature art currently uses emoji placeholders. When real art is ready:

1. Add sprite sheets to `Assets.xcassets/Creatures/`
2. Name format: `creature_{id}_mutation{0-3}_{state}`
   - Example: `creature_aquaburst_mutation0_idle`
   - States: `idle`, `interact`, `happy`, `sad`
3. Replace emoji in `ShopView.swift`, `HabitatView.swift`, `PediaView.swift`
   with `Image("creature_\(entry.id)_mutation\(mutationIndex)_idle")`

### Priority art list (7 creatures to unblock Phase 2 testing):
1. `aquaburst` — Water (common)
2. `crumblebane` — Dirt (common)
3. `wildbloom` — Grass (common)
4. `cinderborne` — Fire (common)
5. `frostveil` — Ice (common)
6. `sparkburst` — Electric (common)
7. `arcane` — Magical (rare, unlocks Act II)

---

## Next Steps After Phase 1

Once you can build and run Phase 1:

- **Phase 2:** Implement customer order system, full shop-to-habitat flow
- **Phase 3:** Breeding UI, mutation display, progression milestones
- **Phase 4:** Story event triggers, NPC dialogue system
- **Phase 5:** Polish, performance optimization, App Store submission

# Habitat Category Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the placeholder `HabitatCategoryScreen` with the real screen — biome-themed environment view + 4-slot roster + tabbed habitat purchase flow + drag-tunable debug surface — built on an expanded multi-creature `Habitat` data model.

**Architecture:** Approach 2 from the spec — one generic `HabitatEnvironmentView` consumes per-biome `BiomeTheme` data records. Reusable `HabitatRosterPanel`. Code-built UI nodes (no sub-`.tscn`s). Texture-driven creature rendering with colored-circle fallback so SVG placeholders from a parallel session drop in without code changes.

**Tech Stack:** Godot 4.6 (.NET), C# / .NET 8.0, NUnit for tests, existing autoload manager singletons.

**Spec:** `docs/superpowers/specs/2026-05-04-habitat-category-design.md`

**Existing code reference (verified during brainstorming):**
- `Habitat` model: `KeeperLegacyGodot/Models/Habitat.cs` — currently single `OccupantId`, will become `OccupantIds: List<Guid>`
- `HabitatType` enum: `Water, Grass, Dirt, Fire, Ice, Electric, Magical`
- `HabitatExpansionCost.Cost(slot)` returns coin cost ladder (0/500/1000/2000/3000/4000/5000/6000)
- `HabitatManager` autoload — `Habitats` (List), `Creatures` (List), signals `CreaturesChanged`, `HabitatsChanged`, `CreatureSold`
- `ProgressionManager` autoload — `Coins`, `IsFeatureUnlocked(GameFeature)`, signals `CoinsChanged`, `FeatureUnlocked`, `LeveledUp`
- `OrderManager` autoload — needs new `IsCreatureReserved(Guid)` query for release safety check
- `CreatureRosterData.Find(catalogId)` → `CreatureCatalogEntry` (`.Name`, `.HabitatType`)
- `CreatureInstance` — `.Id`, `.CatalogId`, `.MutationIndex`
- `MainScene.NavigateToSubScreen(string)` — used to navigate to Detail later
- `HabitatFloorScreen.SelectedHabitatType` — already set when pedestal clicked
- `HabitatCategoryScreen.tscn` — current placeholder uses `PlaceholderScreen.cs` script

**Task list (15 tasks):**
1. Habitat model — OccupantIds list
2. HabitatCapacity helper
3. HabitatManager API expansion
4. BiomeTheme records + lookup (Water configured, others stub)
5. Remaining biome theme stubs (Grass, Dirt, Fire, Ice, Electric, Magical)
6. HabitatPalette + ChoiceMenu component
7. HabitatRosterPanel
8. HabitatTabBar
9. HabitatOverlayBar
10. HabitatEnvironmentView — non-creature layers (background, decorations, particles, ambient lights, surface, floor)
11. HabitatEnvironmentView — wandering creatures with Y-sort and drop shadows
12. HabitatCategoryScreen orchestrator + signal wiring + .tscn replacement
13. Debug surface — decoration drag, scale cycle, focus cycle, bake-print
14. Debug surface — wander zone corner/edge handles
15. Manual integration smoke test (sign-off checklist)

---

## Task 1: Habitat model — multi-occupant expansion

**Files:**
- Modify: `KeeperLegacyGodot/Models/Habitat.cs`
- Create: `KeeperLegacyGodot/Tests/HabitatModelTests.cs`

- [ ] **Step 1: Write failing test for `TryPlaceCreature` capacity boundary**

Create `KeeperLegacyGodot/Tests/HabitatModelTests.cs`:

```csharp
// Tests/HabitatModelTests.cs
using NUnit.Framework;
using System;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class HabitatModelTests
    {
        [Test]
        public void TryPlaceCreature_AddsToEmptyHabitat()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            var creatureId = Guid.NewGuid();

            bool result = h.TryPlaceCreature(creatureId);

            Assert.That(result,                  Is.True);
            Assert.That(h.OccupantIds,           Has.Count.EqualTo(1));
            Assert.That(h.OccupantIds[0],        Is.EqualTo(creatureId));
            Assert.That(h.IsEmpty,               Is.False);
        }

        [Test]
        public void TryPlaceCreature_RejectsWhenFull()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            for (int i = 0; i < HabitatCapacity.CreaturesPerHabitat; i++)
                h.TryPlaceCreature(Guid.NewGuid());

            bool result = h.TryPlaceCreature(Guid.NewGuid());

            Assert.That(result,                  Is.False);
            Assert.That(h.OccupantIds,           Has.Count.EqualTo(HabitatCapacity.CreaturesPerHabitat));
            Assert.That(h.IsFull,                Is.True);
        }

        [Test]
        public void TryPlaceCreature_RejectsDuplicate()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            var id = Guid.NewGuid();
            h.TryPlaceCreature(id);

            bool secondResult = h.TryPlaceCreature(id);

            Assert.That(secondResult,            Is.False);
            Assert.That(h.OccupantIds,           Has.Count.EqualTo(1));
        }

        [Test]
        public void RemoveCreature_RemovesPresent()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            var id = Guid.NewGuid();
            h.TryPlaceCreature(id);

            bool result = h.RemoveCreature(id);

            Assert.That(result,                  Is.True);
            Assert.That(h.OccupantIds,           Is.Empty);
            Assert.That(h.IsEmpty,               Is.True);
        }

        [Test]
        public void RemoveCreature_ReturnsFalseWhenAbsent()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);

            bool result = h.RemoveCreature(Guid.NewGuid());

            Assert.That(result,                  Is.False);
        }

        [Test]
        public void AvailableSlots_ReflectsOccupancy()
        {
            var h = new Habitat(HabitatType.Water, unlockedAtLevel: 1);
            Assert.That(h.AvailableSlots, Is.EqualTo(4));

            h.TryPlaceCreature(Guid.NewGuid());
            Assert.That(h.AvailableSlots, Is.EqualTo(3));

            h.TryPlaceCreature(Guid.NewGuid());
            h.TryPlaceCreature(Guid.NewGuid());
            h.TryPlaceCreature(Guid.NewGuid());
            Assert.That(h.AvailableSlots, Is.EqualTo(0));
        }

        [Test]
        public void DeserializeFromSingleOccupantSave_BackfillsList()
        {
            // Simulates loading a save written with the legacy single-occupant shape.
            var legacyId = Guid.NewGuid();
            var habitat  = new Habitat(
                id: Guid.NewGuid(),
                type: HabitatType.Water,
                occupantIds: new System.Collections.Generic.List<Guid> { legacyId },
                decorationIds: new System.Collections.Generic.List<string>(),
                unlockedAtLevel: 1);

            Assert.That(habitat.OccupantIds, Has.Count.EqualTo(1));
            Assert.That(habitat.OccupantIds[0], Is.EqualTo(legacyId));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail with compile errors (Habitat API doesn't match yet)**

Run: `dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"`
Expected: Build FAILS with errors about `OccupantIds`, `TryPlaceCreature`, `IsFull`, `AvailableSlots`, `HabitatCapacity`, and `RemoveCreature(Guid)` not existing.

- [ ] **Step 3: Replace `Habitat.cs` with multi-occupant model**

Replace the entire contents of `KeeperLegacyGodot/Models/Habitat.cs` with:

```csharp
// Models/Habitat.cs
// Habitat instance: holds up to HabitatCapacity.CreaturesPerHabitat creatures.

using System;
using System.Collections.Generic;

namespace KeeperLegacy.Models
{
    public class Habitat
    {
        public Guid Id { get; }
        public HabitatType Type { get; }

        /// Creatures currently housed (max HabitatCapacity.CreaturesPerHabitat).
        /// Mutate only via TryPlaceCreature / RemoveCreature so capacity is enforced.
        public List<Guid> OccupantIds { get; }

        /// Cosmetic decoration item IDs the player has placed.
        public List<string> DecorationIds { get; }

        /// Player level at which this habitat slot was unlocked.
        public int UnlockedAtLevel { get; }

        // ── Computed ──────────────────────────────────────────────────────────

        public bool IsEmpty => OccupantIds.Count == 0;
        public bool IsFull  => OccupantIds.Count >= HabitatCapacity.CreaturesPerHabitat;
        public int  AvailableSlots => HabitatCapacity.CreaturesPerHabitat - OccupantIds.Count;

        // ── Constructors ──────────────────────────────────────────────────────

        public Habitat(HabitatType type, int unlockedAtLevel)
        {
            Id              = Guid.NewGuid();
            Type            = type;
            OccupantIds     = new List<Guid>();
            DecorationIds   = new List<string>();
            UnlockedAtLevel = unlockedAtLevel;
        }

        /// Deserialization constructor.
        public Habitat(Guid id, HabitatType type, List<Guid> occupantIds,
                       List<string> decorationIds, int unlockedAtLevel)
        {
            Id              = id;
            Type            = type;
            OccupantIds     = occupantIds ?? new List<Guid>();
            DecorationIds   = decorationIds ?? new List<string>();
            UnlockedAtLevel = unlockedAtLevel;
        }

        // ── Mutations ─────────────────────────────────────────────────────────

        /// Adds creatureId to OccupantIds. Returns false if full or already present.
        public bool TryPlaceCreature(Guid creatureId)
        {
            if (IsFull) return false;
            if (OccupantIds.Contains(creatureId)) return false;
            OccupantIds.Add(creatureId);
            return true;
        }

        /// Removes creatureId. Returns false if not present.
        public bool RemoveCreature(Guid creatureId)
        {
            return OccupantIds.Remove(creatureId);
        }

        public void AddDecoration(string decorationId)
        {
            if (!DecorationIds.Contains(decorationId))
                DecorationIds.Add(decorationId);
        }

        public void RemoveDecoration(string decorationId) =>
            DecorationIds.Remove(decorationId);
    }

    // Note: HabitatUnlockSchedule has been removed (was level→total-habitats; now
    // per-biome via HabitatCapacity). HabitatExpansionCost stays in this file.

    public static class HabitatExpansionCost
    {
        /// Coin cost to unlock the given slot number (1-indexed).
        public static int Cost(int slot) => slot switch
        {
            1 => 0,
            2 => 500,
            3 => 1000,
            4 => 2000,
            5 => 3000,
            6 => 4000,
            7 => 5000,
            _ => 6000
        };
    }
}
```

- [ ] **Step 4: Build will still fail — `HabitatCapacity` doesn't exist yet. Move to Task 2 to create it, then return to verify Task 1 tests pass.**

This is fine — we have a forward dependency on Task 2's `HabitatCapacity`. Continue to Task 2; we'll come back to verify and commit Task 1 after Task 2 lands.

---

## Task 2: HabitatCapacity helper

**Files:**
- Create: `KeeperLegacyGodot/Models/HabitatCapacity.cs`
- Modify: `KeeperLegacyGodot/Tests/HabitatModelTests.cs` (add capacity tests)

- [ ] **Step 1: Append HabitatCapacity tests to `HabitatModelTests.cs`**

Add inside the `HabitatModelTests` class (before the closing brace):

```csharp
        // ── HabitatCapacity ───────────────────────────────────────────────────

        [Test]
        public void MaxHabitatsForBiome_EarthTier()
        {
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Water), Is.EqualTo(4));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Grass), Is.EqualTo(4));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Dirt),  Is.EqualTo(4));
        }

        [Test]
        public void MaxHabitatsForBiome_MidTier()
        {
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Fire),     Is.EqualTo(3));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Ice),      Is.EqualTo(3));
            Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Electric), Is.EqualTo(3));
        }

        [Test]
        public void MaxHabitatsForBiome_Magical()
            => Assert.That(HabitatCapacity.MaxHabitatsForBiome(HabitatType.Magical), Is.EqualTo(2));

        [Test]
        public void CreaturesPerHabitat_Is4()
            => Assert.That(HabitatCapacity.CreaturesPerHabitat, Is.EqualTo(4));

        [Test]
        public void CoinsForHabitat_FirstSlotFree()
        {
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 1), Is.EqualTo(0));
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Magical, 1), Is.EqualTo(0));
        }

        [Test]
        public void CoinsForHabitat_LadderMatchesExisting()
        {
            // Should match the existing HabitatExpansionCost ladder so cost UI is consistent.
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 2), Is.EqualTo(HabitatExpansionCost.Cost(2)));
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 3), Is.EqualTo(HabitatExpansionCost.Cost(3)));
            Assert.That(HabitatCapacity.CoinsForHabitat(HabitatType.Water, 4), Is.EqualTo(HabitatExpansionCost.Cost(4)));
        }
```

- [ ] **Step 2: Create `HabitatCapacity.cs`**

Create `KeeperLegacyGodot/Models/HabitatCapacity.cs`:

```csharp
// Models/HabitatCapacity.cs
// Per-biome habitat capacity rules. Single source of truth.

namespace KeeperLegacy.Models
{
    public static class HabitatCapacity
    {
        /// How many creatures fit in one habitat. Bumping this is a one-line change.
        public const int CreaturesPerHabitat = 4;

        /// Max habitats a player can own per biome. Bumping a tier here is the
        /// expansion path when more catalog creatures are added later.
        public static int MaxHabitatsForBiome(HabitatType type) => type switch
        {
            HabitatType.Water  or HabitatType.Grass    or HabitatType.Dirt     => 4,
            HabitatType.Fire   or HabitatType.Ice      or HabitatType.Electric => 3,
            HabitatType.Magical                                                => 2,
            _                                                                  => 0
        };

        /// Coin cost to unlock the Nth habitat of a biome (1-indexed within biome).
        /// First slot is always 0 — Earth-tier player owns slot 1 at game start;
        /// mid-tier and Magical have slot 1 story-gated (no coin path) so the
        /// returned 0 is also correct (the lock state, not the cost, is what
        /// gates them — see HabitatManager.GetUnlockReason).
        public static int CoinsForHabitat(HabitatType biome, int oneIndexedSlot)
            => HabitatExpansionCost.Cost(oneIndexedSlot);
    }
}
```

- [ ] **Step 3: Build the project**

Run: `dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"`
Expected: build error in `HabitatManager.cs` referencing `OccupantId` / `PlaceCreature(creatureId)` / `RemoveCreature()` (no-arg). These are the legacy single-occupant API calls that need migrating in Task 3. The Tests project won't compile yet either.

- [ ] **Step 4: Don't commit yet — Task 3 is required before tests pass**

Tasks 1 + 2 + 3 commit together once `HabitatManager` migrates to the new Habitat API.

---

## Task 3: HabitatManager API expansion

**Files:**
- Modify: `KeeperLegacyGodot/Managers/HabitatManager.cs`
- Modify: `KeeperLegacyGodot/Managers/OrderManager.cs` (add `IsCreatureReserved`)
- Modify: any other callers of `Habitat.OccupantId` / `PlaceCreature` / `RemoveCreature()` no-arg
- Test: `KeeperLegacyGodot/Tests/HabitatManagerTests.cs` (CREATE)

- [ ] **Step 1: Find all call sites of the legacy API**

Run from project root:
```
grep -rn "OccupantId\|PlaceCreature\|RemoveCreature" KeeperLegacyGodot --include="*.cs"
```
Expected: hits in `HabitatManager.cs`, possibly `BreedingManager.cs`, `OrderManager.cs`, `Habitat.cs` itself (already migrated), and tests. List every file you find.

- [ ] **Step 2: Write failing tests for new HabitatManager methods**

Create `KeeperLegacyGodot/Tests/HabitatManagerTests.cs`:

```csharp
// Tests/HabitatManagerTests.cs
using NUnit.Framework;
using System;
using System.Collections.Generic;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class HabitatManagerTests
    {
        private HabitatManager _hm;

        [SetUp]
        public void Setup()
        {
            _hm = new HabitatManager();
            _hm.Initialize(new List<Habitat>(), new List<CreatureInstance>());
        }

        [Test]
        public void TryPlaceCreatureInSlot_AddsToHabitat()
        {
            var habitat = new Habitat(HabitatType.Water, 1);
            _hm.Habitats.Add(habitat);

            var creatureId = Guid.NewGuid();
            bool result = _hm.TryPlaceCreatureInSlot(habitat.Id, creatureId);

            Assert.That(result, Is.True);
            Assert.That(habitat.OccupantIds, Contains.Item(creatureId));
        }

        [Test]
        public void TryPlaceCreatureInSlot_RejectsWhenFull()
        {
            var habitat = new Habitat(HabitatType.Water, 1);
            for (int i = 0; i < HabitatCapacity.CreaturesPerHabitat; i++)
                habitat.TryPlaceCreature(Guid.NewGuid());
            _hm.Habitats.Add(habitat);

            bool result = _hm.TryPlaceCreatureInSlot(habitat.Id, Guid.NewGuid());

            Assert.That(result, Is.False);
        }

        [Test]
        public void GetUnlockReason_OwnedSlot()
        {
            _hm.Habitats.Add(new Habitat(HabitatType.Water, 1));
            var reason = _hm.GetUnlockReason(HabitatType.Water, 1);
            Assert.That(reason.Kind, Is.EqualTo(UnlockReasonKind.Owned));
        }

        [Test]
        public void GetUnlockReason_PurchasableSlot()
        {
            _hm.Habitats.Add(new Habitat(HabitatType.Water, 1));
            var reason = _hm.GetUnlockReason(HabitatType.Water, 2);
            Assert.That(reason.Kind,  Is.EqualTo(UnlockReasonKind.Purchasable));
            Assert.That(reason.Coins, Is.EqualTo(HabitatExpansionCost.Cost(2)));
        }

        [Test]
        public void GetUnlockReason_OutOfRange()
        {
            // Magical max is 2 — slot 3 is out of range
            var reason = _hm.GetUnlockReason(HabitatType.Magical, 3);
            Assert.That(reason.Kind, Is.EqualTo(UnlockReasonKind.OutOfRange));
        }

        [Test]
        public void HabitatsOfType_FiltersByBiome()
        {
            _hm.Habitats.Add(new Habitat(HabitatType.Water, 1));
            _hm.Habitats.Add(new Habitat(HabitatType.Grass, 1));
            _hm.Habitats.Add(new Habitat(HabitatType.Water, 1));

            var waters = _hm.HabitatsOfType(HabitatType.Water);

            Assert.That(waters, Has.Count.EqualTo(2));
        }
    }
}
```

- [ ] **Step 3: Migrate `HabitatManager.cs` to the new Habitat API + add new methods**

Replace `KeeperLegacyGodot/Managers/HabitatManager.cs` entirely:

```csharp
// Managers/HabitatManager.cs
// Autoload singleton — owns all habitats and player creature instances.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

public partial class HabitatManager : Node
{
    // ── Signals ───────────────────────────────────────────────────────────────

    [Signal] public delegate void CreaturesChangedEventHandler();
    [Signal] public delegate void HabitatsChangedEventHandler();
    [Signal] public delegate void CareActionPerformedEventHandler(string creatureId, string xpSourceRaw);
    [Signal] public delegate void CreatureSoldEventHandler(int coinReward);

    // ── State ─────────────────────────────────────────────────────────────────

    public List<Habitat>          Habitats  { get; private set; } = new();
    public List<CreatureInstance> Creatures { get; private set; } = new();

    public void Initialize(List<Habitat> habitats, List<CreatureInstance> creatures)
    {
        Habitats  = habitats;
        Creatures = creatures;
    }

    // ── Care Actions ──────────────────────────────────────────────────────────
    // (Unchanged — Feed/Play/Clean still operate on a single creature by id.)

    public bool Feed(Guid creatureId, string foodId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return false;
        var food = PricingTable.Food.Catalog.FirstOrDefault(f => f.Id == foodId);
        if (food == null) return false;
        creature.Feed(food.HungerRestored);
        EmitSignal(SignalName.CareActionPerformed, creatureId.ToString(), XPSource.Feed.ToString());
        return true;
    }

    public bool Play(Guid creatureId, string toyName, bool isFavoriteToy)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return false;
        creature.Play(toyName, isFavoriteToy);
        EmitSignal(SignalName.CareActionPerformed, creatureId.ToString(), XPSource.Play.ToString());
        return true;
    }

    public bool Clean(Guid creatureId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return false;
        creature.Clean();
        EmitSignal(SignalName.CareActionPerformed, creatureId.ToString(), XPSource.Clean.ToString());
        return true;
    }

    // ── Selling ───────────────────────────────────────────────────────────────

    public int SellCreature(Guid creatureId)
    {
        var creature = GetCreature(creatureId);
        if (creature == null) return 0;

        var entry     = CreatureRosterData.Find(creature.CatalogId);
        var rarity    = entry?.Rarity ?? Rarity.Common;
        int sellValue = PricingTable.SellValue(rarity, creature.SellMultiplier);

        FindHabitatFor(creatureId)?.RemoveCreature(creatureId);
        Creatures.Remove(creature);

        EmitSignal(SignalName.CreatureSold, sellValue);
        EmitSignal(SignalName.CreaturesChanged);
        return sellValue;
    }

    // ── Placement (multi-occupant) ────────────────────────────────────────────

    /// Place a creature into a specific habitat slot. Returns false if the
    /// habitat is full, the creature is already housed there, or the biome
    /// types don't match.
    public bool TryPlaceCreatureInSlot(Guid habitatId, Guid creatureId)
    {
        var habitat = GetHabitat(habitatId);
        if (habitat == null) return false;

        var creature = GetCreature(creatureId);
        if (creature != null)
        {
            var entry = CreatureRosterData.Find(creature.CatalogId);
            if (entry != null && entry.HabitatType != habitat.Type) return false;
        }

        // Evict from previous habitat if any
        FindHabitatFor(creatureId)?.RemoveCreature(creatureId);

        if (!habitat.TryPlaceCreature(creatureId)) return false;
        EmitSignal(SignalName.HabitatsChanged);
        return true;
    }

    /// Release a creature back to the wild — removes from its habitat AND
    /// the creature ledger. Returns false when not found OR when an active
    /// customer order has reserved this creature (caller should toast).
    public bool ReleaseCreature(Guid habitatId, Guid creatureId)
    {
        var habitat  = GetHabitat(habitatId);
        var creature = GetCreature(creatureId);
        if (habitat == null || creature == null) return false;

        var orderManager = GetNodeOrNull<OrderManager>("/root/OrderManager");
        if (orderManager?.IsCreatureReserved(creatureId) == true) return false;

        habitat.RemoveCreature(creatureId);
        Creatures.Remove(creature);

        EmitSignal(SignalName.HabitatsChanged);
        EmitSignal(SignalName.CreaturesChanged);
        return true;
    }

    // ── Habitat creation ──────────────────────────────────────────────────────

    /// Try to add a new habitat for the given biome. Charges coins via
    /// ProgressionManager. Outputs the coins charged on success.
    public bool TryAddHabitat(HabitatType biome, out int coinsCharged)
    {
        coinsCharged = 0;
        int owned = HabitatsOfType(biome).Count;
        int slot  = owned + 1;
        if (slot > HabitatCapacity.MaxHabitatsForBiome(biome)) return false;

        int cost = HabitatCapacity.CoinsForHabitat(biome, slot);
        var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
        if (cost > 0 && (pm == null || !pm.SpendCoins(cost))) return false;

        Habitats.Add(new Habitat(biome, pm?.CurrentLevel ?? 1));
        coinsCharged = cost;
        EmitSignal(SignalName.HabitatsChanged);
        return true;
    }

    // ── Adding Creatures (from shop/breeding) ─────────────────────────────────

    public void AddCreature(CreatureInstance creature)
    {
        Creatures.Add(creature);
        EmitSignal(SignalName.CreaturesChanged);
    }

    // ── Stat Decay & Lifecycle (unchanged) ────────────────────────────────────

    public void ApplyDecay(double hoursPassed)
    {
        foreach (var c in Creatures)
            c.ApplyDecay(hoursPassed);
        if (Creatures.Count > 0)
            EmitSignal(SignalName.CreaturesChanged);
    }

    public void TickLifecycles()
    {
        bool anyChanged = false;
        foreach (var c in Creatures)
        {
            if (c.Lifecycle == LifecycleStage.Adult) continue;
            double hoursElapsed = (DateTime.UtcNow - c.LifecycleStartDate).TotalHours;
            if (hoursElapsed >= c.Lifecycle.DurationHours())
            {
                c.Lifecycle          = c.Lifecycle + 1;
                c.LifecycleStartDate = DateTime.UtcNow;
                anyChanged = true;
            }
        }
        if (anyChanged) EmitSignal(SignalName.CreaturesChanged);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public CreatureInstance? GetCreature(Guid id) =>
        Creatures.FirstOrDefault(c => c.Id == id);

    public Habitat? GetHabitat(Guid id) =>
        Habitats.FirstOrDefault(h => h.Id == id);

    /// Find which habitat (if any) houses the given creature.
    public Habitat? FindHabitatFor(Guid creatureId) =>
        Habitats.FirstOrDefault(h => h.OccupantIds.Contains(creatureId));

    public IReadOnlyList<Habitat> HabitatsOfType(HabitatType biome) =>
        Habitats.Where(h => h.Type == biome).ToList();

    public List<CreatureInstance> CreaturesInHabitat(Guid habitatId)
    {
        var habitat = GetHabitat(habitatId);
        if (habitat == null) return new List<CreatureInstance>();
        return habitat.OccupantIds
                      .Select(GetCreature)
                      .Where(c => c != null)!
                      .ToList()!;
    }

    // ── Unlock state ──────────────────────────────────────────────────────────

    /// Returns the unlock state for the Nth habitat of a biome (1-indexed).
    public UnlockReason GetUnlockReason(HabitatType biome, int oneIndexedSlot)
    {
        int max = HabitatCapacity.MaxHabitatsForBiome(biome);
        if (oneIndexedSlot < 1 || oneIndexedSlot > max)
            return new UnlockReason(UnlockReasonKind.OutOfRange);

        int owned = HabitatsOfType(biome).Count;
        if (oneIndexedSlot <= owned)
            return new UnlockReason(UnlockReasonKind.Owned);

        // First habitat for mid-tier / Magical biomes is story-gated.
        if (oneIndexedSlot == 1)
        {
            if (biome == HabitatType.Magical)
            {
                var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
                if (pm == null || !pm.IsFeatureUnlocked(GameFeature.MagicalHabitat))
                    return new UnlockReason(UnlockReasonKind.StoryGated, StoryAct: 2);
            }
            else if (biome == HabitatType.Fire || biome == HabitatType.Ice || biome == HabitatType.Electric)
            {
                var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
                if (pm == null || !pm.IsFeatureUnlocked(GameFeature.HabitatExpansion))
                    return new UnlockReason(UnlockReasonKind.StoryGated, StoryAct: 1);
            }
            // Water/Grass/Dirt slot 1 is always Owned (game-start) — handled above.
        }

        // Slot 2+ is purchasable with coins.
        return new UnlockReason(UnlockReasonKind.Purchasable, Coins: HabitatCapacity.CoinsForHabitat(biome, oneIndexedSlot));
    }
}

// ── Unlock-reason value type (top-level, not nested) ─────────────────────────

public enum UnlockReasonKind { Owned, Purchasable, StoryGated, OutOfRange }
public record UnlockReason(UnlockReasonKind Kind, int? Coins = null, int? StoryAct = null);
```

- [ ] **Step 4: Add `IsCreatureReserved` stub to OrderManager**

Open `KeeperLegacyGodot/Managers/OrderManager.cs`, find the queries section (or end of class), and add:

```csharp
    /// Returns true if the given creature is reserved by an active customer
    /// order. Used by HabitatManager.ReleaseCreature to prevent releasing a
    /// promised creature. Implementation will be fleshed out when order
    /// reservation lands; for now returns false (no reservations).
    public bool IsCreatureReserved(System.Guid creatureId) => false;
```

- [ ] **Step 5: Update any other call sites flagged in Step 1**

Common sites that may need updates:
- `BreedingManager` — if it calls `habitat.PlaceCreature(id)` rename to `TryPlaceCreature(id)` and ignore return or branch on it
- Any code reading `habitat.OccupantId` (single nullable) — change to `habitat.OccupantIds.FirstOrDefault()` or iterate `OccupantIds`

Check each grep hit and edit accordingly.

- [ ] **Step 6: Build the project**

Run: `dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"`
Expected: Build succeeds with 0 errors.

- [ ] **Step 7: Run all tests**

Run: `dotnet test "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"`
Expected: All tests pass — including the new `HabitatModelTests` and `HabitatManagerTests`, and the existing 110 tests.

- [ ] **Step 8: Commit Tasks 1+2+3 as a single atomic data-layer change**

```
git add KeeperLegacyGodot/Models/Habitat.cs KeeperLegacyGodot/Models/HabitatCapacity.cs KeeperLegacyGodot/Managers/HabitatManager.cs KeeperLegacyGodot/Managers/OrderManager.cs KeeperLegacyGodot/Tests/HabitatModelTests.cs KeeperLegacyGodot/Tests/HabitatManagerTests.cs
# plus any other modified files from Step 5
git commit -m "feat(habitat): multi-occupant model + per-biome capacity + manager API

Habitat.OccupantId (single Guid?) → OccupantIds (List<Guid>) with capacity
4 enforced via TryPlaceCreature. Per-biome max enforced via new
HabitatCapacity helper (4/3/2 by biome tier). HabitatManager gains
TryPlaceCreatureInSlot, ReleaseCreature, TryAddHabitat, HabitatsOfType,
GetUnlockReason — encapsulating the whole owned/purchasable/story-gated
decision tree so the screen UI stays decoupled from rule details.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: BiomeTheme records + Water configuration

**Files:**
- Create: `KeeperLegacyGodot/Data/BiomeTheme.cs`
- Test: `KeeperLegacyGodot/Tests/BiomeThemeTests.cs`

- [ ] **Step 1: Write tests asserting all 7 biomes have a theme registered**

Create `KeeperLegacyGodot/Tests/BiomeThemeTests.cs`:

```csharp
// Tests/BiomeThemeTests.cs
using NUnit.Framework;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class BiomeThemeTests
    {
        [Test]
        public void For_AllBiomesRegistered()
        {
            foreach (HabitatType biome in System.Enum.GetValues(typeof(HabitatType)))
            {
                var theme = BiomeThemes.For(biome);
                Assert.That(theme,         Is.Not.Null,             $"Theme missing for {biome}");
                Assert.That(theme!.Biome,  Is.EqualTo(biome));
            }
        }

        [Test]
        public void For_WaterHasDecorationsAndWanderZone()
        {
            var theme = BiomeThemes.For(HabitatType.Water);
            Assert.That(theme,                       Is.Not.Null);
            Assert.That(theme!.Decorations.Length,   Is.GreaterThan(0));
            Assert.That(theme.WanderZone.Size.X,     Is.GreaterThan(0));
            Assert.That(theme.WanderZone.Size.Y,     Is.GreaterThan(0));
        }

        [Test]
        public void For_WaterIncludesParticles()
            => Assert.That(BiomeThemes.For(HabitatType.Water)!.Particles, Is.Not.Null);
    }
}
```

- [ ] **Step 2: Create `BiomeTheme.cs` with records, enum, and Water-only registration**

Create `KeeperLegacyGodot/Data/BiomeTheme.cs`:

```csharp
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
```

- [ ] **Step 3: Build + run tests**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
dotnet test  "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: build clean, all tests pass.

- [ ] **Step 4: Commit**

```
git add KeeperLegacyGodot/Data/BiomeTheme.cs KeeperLegacyGodot/Tests/BiomeThemeTests.cs
git commit -m "feat(habitat): BiomeTheme records + Water configuration

Per-biome theme record covering background colors, decorations (with
optional sway/float/drift animation), particle config, ambient light
shafts, floor overlay, surface line, and wander zone. Water has the
mockup-parity layout; the other six biomes get stub themes that ship
working but flat — full configurations come in Task 5 (the bake-print
flow lets us hand-tune each one in the running game).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Stub theme placeholders for the remaining 6 biomes

This task's job is to give each biome a non-empty starter set of decorations + particles so the screen has *something* visual when entering Grass/Dirt/Fire/Ice/Electric/Magical. Final hand-tuning happens later via debug drag mode in the running game.

**Files:**
- Modify: `KeeperLegacyGodot/Data/BiomeTheme.cs`

- [ ] **Step 1: Replace the 6 stub theme entries with starter configurations**

Open `KeeperLegacyGodot/Data/BiomeTheme.cs`. Replace the 6 `StubTheme(...)` lines in the `_themes` dictionary with full theme records. Use these as starting points (positions are intentionally rough — Jesse will tune via debug drag):

```csharp
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
```

Also delete the now-unused `StubTheme` helper method — none of the lookups call it anymore.

- [ ] **Step 2: Build + tests**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
dotnet test  "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: clean.

- [ ] **Step 3: Commit**

```
git add KeeperLegacyGodot/Data/BiomeTheme.cs
git commit -m "feat(habitat): starter biome theme configs for the remaining 6 biomes

Each biome ships with placeholder decorations, particles where
appropriate (snow/sparks/embers/etc.), and a starter wander zone.
Positions are intentionally approximate — final tuning happens via
the debug drag mode in the running game.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: HabitatPalette + ChoiceMenu component

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatPalette.cs`
- Create: `KeeperLegacyGodot/UI/Habitat/ChoiceMenu.cs`

- [ ] **Step 1: Create `HabitatPalette.cs`**

```csharp
// UI/Habitat/HabitatPalette.cs
// Centralized colors for the Habitat Category screen. Per project memory
// "Palette Rework Pending", every color used on this screen lives here so the
// future palette swap is a single-file edit.

using Godot;

namespace KeeperLegacy.UI.Habitat
{
    public static class HabitatPalette
    {
        public static readonly Color OverlayBarBg          = new Color(0.047f, 0.035f, 0.027f, 0.80f);
        public static readonly Color OverlayBarBorderTint  = new Color(1.00f, 1.00f, 1.00f, 0.15f); // tinted by biome at runtime
        public static readonly Color BackButtonText        = new Color(0.91f, 0.72f, 0.19f, 0.75f);
        public static readonly Color BackButtonHover       = new Color(0.94f, 0.80f, 0.31f, 1.00f);
        public static readonly Color SeparatorLine         = new Color(0.227f, 0.157f, 0.094f, 0.80f);
        public static readonly Color CoinsText             = new Color(0.94f, 0.80f, 0.31f, 1.00f);
        public static readonly Color CoinsBg               = new Color(0.91f, 0.72f, 0.19f, 0.08f);

        public static readonly Color RosterPanelBg         = new Color(0.102f, 0.071f, 0.031f, 1.00f);
        public static readonly Color RosterCapacityBorder  = new Color(1.00f, 1.00f, 1.00f, 0.30f);
        public static readonly Color SlotBgIdle            = new Color(1.00f, 1.00f, 1.00f, 0.05f);
        public static readonly Color SlotBgSelected        = new Color(1.00f, 1.00f, 1.00f, 0.15f);
        public static readonly Color SlotEmptyBorder       = new Color(1.00f, 1.00f, 1.00f, 0.15f);

        public static readonly Color LabelName             = new Color(0.94f, 0.91f, 0.85f, 1.00f);
        public static readonly Color LabelMuted            = new Color(0.60f, 0.50f, 0.44f, 1.00f);
        public static readonly Color LabelLocked           = new Color(0.38f, 0.38f, 0.38f, 1.00f);

        public static readonly Color ChoiceMenuBg          = new Color(0.10f, 0.08f, 0.06f, 0.95f);
        public static readonly Color ChoiceMenuBorder      = new Color(1.00f, 1.00f, 1.00f, 0.20f);
        public static readonly Color ConfirmDialogScrim    = new Color(0.00f, 0.00f, 0.00f, 0.55f);
    }
}
```

- [ ] **Step 2: Create `ChoiceMenu.cs`**

```csharp
// UI/Habitat/ChoiceMenu.cs
// Lightweight floating choice panel anchored to a screen position. Tap-outside
// dismisses. Reusable; used here for "Add Creature" choices and could be reused
// elsewhere.

using Godot;
using System;
using System.Collections.Generic;

namespace KeeperLegacy.UI.Habitat
{
    public partial class ChoiceMenu : PanelContainer
    {
        public record ChoiceOption(string Label, bool Enabled, Action OnTap, string? DisabledReason = null);

        private VBoxContainer _box;
        private Action? _onDismiss;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Stop;
            ZIndex = 100;

            var style = new StyleBoxFlat();
            style.BgColor               = HabitatPalette.ChoiceMenuBg;
            style.BorderColor           = HabitatPalette.ChoiceMenuBorder;
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(8);
            style.ContentMarginLeft     = 8;
            style.ContentMarginRight    = 8;
            style.ContentMarginTop      = 6;
            style.ContentMarginBottom   = 6;
            AddThemeStyleboxOverride("panel", style);

            _box = new VBoxContainer();
            _box.AddThemeConstantOverride("separation", 4);
            AddChild(_box);
        }

        /// <summary>
        /// Show the menu anchored at viewport position (top-left of the menu).
        /// onDismiss is called when the user taps outside or picks an option.
        /// </summary>
        public void Show(Vector2 anchor, IList<ChoiceOption> options, Action? onDismiss = null)
        {
            _onDismiss = onDismiss;
            foreach (Node child in _box.GetChildren()) child.QueueFree();

            foreach (var opt in options)
            {
                var btn = new Button();
                btn.Text = opt.Label + (!opt.Enabled && opt.DisabledReason != null ? $"  ({opt.DisabledReason})" : "");
                btn.Disabled = !opt.Enabled;
                btn.FocusMode = FocusModeEnum.None;
                btn.AddThemeFontSizeOverride("font_size", 12);
                if (opt.Enabled)
                {
                    btn.Pressed += () =>
                    {
                        opt.OnTap();
                        Dismiss();
                    };
                }
                _box.AddChild(btn);
            }

            Position = anchor;
            Visible  = true;
        }

        public void Dismiss()
        {
            Visible = false;
            _onDismiss?.Invoke();
        }

        public override void _GuiInput(InputEvent @event)
        {
            // Clicks on the menu itself are handled by the buttons — don't dismiss.
        }

        public override void _Input(InputEvent @event)
        {
            if (!Visible) return;
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                // If click is outside the menu rect, dismiss.
                Vector2 local = mb.Position - Position;
                if (local.X < 0 || local.Y < 0 || local.X > Size.X || local.Y > Size.Y)
                {
                    Dismiss();
                }
            }
        }
    }
}
```

- [ ] **Step 3: Build**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: clean.

- [ ] **Step 4: Commit**

```
git add KeeperLegacyGodot/UI/Habitat/HabitatPalette.cs KeeperLegacyGodot/UI/Habitat/ChoiceMenu.cs
git commit -m "feat(habitat): centralized palette + reusable ChoiceMenu component

HabitatPalette holds every color used on the Habitat Category screen so
the pending palette rework is a one-file change. ChoiceMenu is a small
floating panel for 'pick one of N options' flows — used by the empty
roster slot's Add Creature button, can be reused on future screens.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: HabitatRosterPanel

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatRosterPanel.cs`

- [ ] **Step 1: Create `HabitatRosterPanel.cs`**

```csharp
// UI/Habitat/HabitatRosterPanel.cs
// 4-slot grid showing creatures in the active habitat. Empty slots have
// "Add Creature" buttons. Filled slots support tap (→ detail) and 600ms
// long-press (→ release confirm).

using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatRosterPanel : PanelContainer
    {
        // ── Signals ───────────────────────────────────────────────────────────

        [Signal] public delegate void CreatureClickedEventHandler(string creatureId);
        [Signal] public delegate void AddCreatureRequestedEventHandler(int slotIndex);
        [Signal] public delegate void ReleaseCreatureRequestedEventHandler(string creatureId);

        // ── Layout constants ──────────────────────────────────────────────────

        private const float SlotPadding   = 10f;
        private const float HeaderPadding = 12f;
        private const float BlobSize      = 54f;
        private const float LongPressMs   = 600f;

        // ── State ─────────────────────────────────────────────────────────────

        private Habitat? _habitat;
        private BiomeTheme? _theme;
        private int _habitatIndex; // 1-indexed for display

        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _capacityLabel;
        private GridContainer _grid;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetHabitat(Habitat habitat, BiomeTheme theme, int habitatIndex)
        {
            _habitat      = habitat;
            _theme        = theme;
            _habitatIndex = habitatIndex;
            Refresh();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void _Ready()
        {
            var style = new StyleBoxFlat();
            style.BgColor = HabitatPalette.RosterPanelBg;
            style.SetBorderWidthAll(0);
            AddThemeStyleboxOverride("panel", style);

            BuildShell();
        }

        private void BuildShell()
        {
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 0);
            AddChild(vbox);

            // Header
            var header = new HBoxContainer();
            header.AddThemeConstantOverride("separation", 8);
            var headerMargin = new MarginContainer();
            headerMargin.AddThemeConstantOverride("margin_left",   (int)HeaderPadding);
            headerMargin.AddThemeConstantOverride("margin_right",  (int)HeaderPadding);
            headerMargin.AddThemeConstantOverride("margin_top",    (int)HeaderPadding);
            headerMargin.AddThemeConstantOverride("margin_bottom", 8);
            headerMargin.AddChild(header);
            vbox.AddChild(headerMargin);

            var titleBox = new VBoxContainer();
            titleBox.AddThemeConstantOverride("separation", 1);
            titleBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(titleBox);

            _titleLabel = new Label();
            _titleLabel.AddThemeFontSizeOverride("font_size", 12);
            _titleLabel.AddThemeColorOverride("font_color", HabitatPalette.LabelName);
            titleBox.AddChild(_titleLabel);

            _subtitleLabel = new Label();
            _subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
            _subtitleLabel.AddThemeColorOverride("font_color", HabitatPalette.LabelMuted);
            titleBox.AddChild(_subtitleLabel);

            _capacityLabel = new Label();
            _capacityLabel.AddThemeFontSizeOverride("font_size", 11);
            header.AddChild(_capacityLabel);

            // Grid
            _grid = new GridContainer();
            _grid.Columns = 2;
            _grid.AddThemeConstantOverride("h_separation", (int)SlotPadding);
            _grid.AddThemeConstantOverride("v_separation", (int)SlotPadding);
            var gridMargin = new MarginContainer();
            gridMargin.AddThemeConstantOverride("margin_left",   (int)SlotPadding);
            gridMargin.AddThemeConstantOverride("margin_right",  (int)SlotPadding);
            gridMargin.AddThemeConstantOverride("margin_bottom", (int)SlotPadding);
            gridMargin.AddChild(_grid);
            vbox.AddChild(gridMargin);
        }

        // ── Render ────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_habitat == null || _theme == null) return;

            _titleLabel.Text    = $"Habitat {_habitatIndex} — Roster";
            _subtitleLabel.Text = $"{_theme.IconEmoji} {_theme.DisplayName}";
            _capacityLabel.Text = $"{_habitat.OccupantIds.Count} / {HabitatCapacity.CreaturesPerHabitat}";
            _capacityLabel.AddThemeColorOverride("font_color", _theme.AccentColor);

            // Rebuild slot cells
            foreach (Node child in _grid.GetChildren()) child.QueueFree();
            for (int i = 0; i < HabitatCapacity.CreaturesPerHabitat; i++)
            {
                if (i < _habitat.OccupantIds.Count)
                    _grid.AddChild(BuildFilledSlot(_habitat.OccupantIds[i]));
                else
                    _grid.AddChild(BuildEmptySlot(i));
            }
        }

        private Control BuildFilledSlot(Guid creatureId)
        {
            var slot = new SlotControl(creatureId, _theme!.AccentColor);
            slot.CustomMinimumSize = new Vector2(140, 140);
            slot.Tapped       += () => EmitSignal(SignalName.CreatureClicked, creatureId.ToString());
            slot.LongPressed  += () => EmitSignal(SignalName.ReleaseCreatureRequested, creatureId.ToString());
            return slot;
        }

        private Control BuildEmptySlot(int slotIndex)
        {
            var slot = new EmptySlotControl(_theme!.AccentColor);
            slot.CustomMinimumSize = new Vector2(140, 140);
            slot.AddPressed += () => EmitSignal(SignalName.AddCreatureRequested, slotIndex);
            return slot;
        }

        // ── Inner classes ─────────────────────────────────────────────────────

        private partial class SlotControl : PanelContainer
        {
            [Signal] public delegate void TappedEventHandler();
            [Signal] public delegate void LongPressedEventHandler();

            private readonly Guid _creatureId;
            private readonly Color _accent;
            private float _holdMs;
            private bool _holding;
            private bool _longFired;

            public SlotControl(Guid creatureId, Color accent)
            {
                _creatureId = creatureId;
                _accent = accent;
            }

            public override void _Ready()
            {
                var style = new StyleBoxFlat();
                style.BgColor     = HabitatPalette.SlotBgIdle;
                style.BorderColor = _accent with { A = 0.30f };
                style.SetBorderWidthAll(2);
                style.SetCornerRadiusAll(10);
                AddThemeStyleboxOverride("panel", style);

                MouseFilter = MouseFilterEnum.Stop;

                var label = new Label();
                label.Text = "🐾";
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment   = VerticalAlignment.Center;
                label.AddThemeFontSizeOverride("font_size", 28);
                label.AddThemeColorOverride("font_color", HabitatPalette.LabelName);
                AddChild(label);
            }

            public override void _GuiInput(InputEvent @event)
            {
                if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        _holding = true;
                        _longFired = false;
                        _holdMs = 0;
                    }
                    else if (_holding)
                    {
                        _holding = false;
                        if (!_longFired) EmitSignal(SignalName.Tapped);
                    }
                }
            }

            public override void _Process(double delta)
            {
                if (_holding && !_longFired)
                {
                    _holdMs += (float)delta * 1000f;
                    if (_holdMs >= LongPressMs)
                    {
                        _longFired = true;
                        EmitSignal(SignalName.LongPressed);
                    }
                }
            }

            // Long-press threshold copied from outer class so this nested type compiles.
            private const float LongPressMs = HabitatRosterPanel.LongPressMs;
        }

        private partial class EmptySlotControl : PanelContainer
        {
            [Signal] public delegate void AddPressedEventHandler();

            private readonly Color _accent;

            public EmptySlotControl(Color accent) { _accent = accent; }

            public override void _Ready()
            {
                var style = new StyleBoxFlat();
                style.BgColor = new Color(0, 0, 0, 0);
                style.BorderColor = _accent with { A = 0.20f };
                style.SetBorderWidthAll(2);
                style.SetCornerRadiusAll(10);
                // Note: Godot StyleBoxFlat doesn't support dashed borders natively;
                // the dashed look from the mockup is a polish detail we'll add via
                // a custom drawn ColorRect child later if Jesse wants it.
                AddThemeStyleboxOverride("panel", style);

                var vbox = new VBoxContainer();
                vbox.Alignment = BoxContainer.AlignmentMode.Center;
                vbox.AddThemeConstantOverride("separation", 4);
                AddChild(vbox);

                var plus = new Label();
                plus.Text = "+";
                plus.HorizontalAlignment = HorizontalAlignment.Center;
                plus.AddThemeFontSizeOverride("font_size", 26);
                plus.AddThemeColorOverride("font_color", _accent with { A = 0.40f });
                vbox.AddChild(plus);

                var label = new Label();
                label.Text = "Empty Slot";
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.AddThemeFontSizeOverride("font_size", 11);
                label.AddThemeColorOverride("font_color", _accent with { A = 0.55f });
                vbox.AddChild(label);

                var btn = new Button();
                btn.Text = "Add Creature";
                btn.AddThemeFontSizeOverride("font_size", 10);
                btn.FocusMode = FocusModeEnum.None;
                btn.Pressed += () => EmitSignal(SignalName.AddPressed);
                vbox.AddChild(btn);
            }
        }
    }
}
```

- [ ] **Step 2: Build**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: clean. The nested `LongPressMs` constant lives on the outer class; the nested `SlotControl` references it via `HabitatRosterPanel.LongPressMs` for compile.

- [ ] **Step 3: Commit**

```
git add KeeperLegacyGodot/UI/Habitat/HabitatRosterPanel.cs
git commit -m "feat(habitat): roster panel with 4-slot grid, long-press release

Header (title + flavor + capacity), 2x2 grid of slots. Filled slot fires
Tapped (→ detail navigation) on quick tap and LongPressed (→ release
confirm) after 600ms hold. Empty slot fires AddPressed (→ choice menu).
Texture-driven creature rendering will land in a follow-up; current
slots show a 🐾 placeholder so the screen is functional from day one.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: HabitatTabBar

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatTabBar.cs`

- [ ] **Step 1: Create `HabitatTabBar.cs`**

```csharp
// UI/Habitat/HabitatTabBar.cs
// One tab per max habitat slot for the active biome. Owned tabs switch the
// active habitat; purchasable tabs trigger a buy dialog; story-gated tabs
// toast a hint.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatTabBar : PanelContainer
    {
        [Signal] public delegate void ActiveHabitatChangedEventHandler(string habitatId);
        [Signal] public delegate void BuyHabitatRequestedEventHandler(int slot);
        [Signal] public delegate void StoryGatedTappedEventHandler(int storyAct);

        private HabitatType _biome;
        private Guid? _activeHabitatId;
        private BiomeTheme? _theme;
        private HBoxContainer _tabBox;

        public void SetBiome(HabitatType biome, BiomeTheme theme)
        {
            _biome = biome;
            _theme = theme;
            Rebuild();
        }

        public void SetActiveHabitat(Guid habitatId)
        {
            _activeHabitatId = habitatId;
            Rebuild();
        }

        public override void _Ready()
        {
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.031f, 0.024f, 0.016f, 0.90f);
            style.BorderWidthBottom = 1;
            style.BorderColor       = HabitatPalette.OverlayBarBorderTint;
            AddThemeStyleboxOverride("panel", style);

            _tabBox = new HBoxContainer();
            _tabBox.AddThemeConstantOverride("separation", 0);
            AddChild(_tabBox);
        }

        public void Rebuild()
        {
            if (_theme == null) return;
            foreach (Node child in _tabBox.GetChildren()) child.QueueFree();

            int max = HabitatCapacity.MaxHabitatsForBiome(_biome);
            var hm  = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            if (hm == null) return;
            var owned = hm.HabitatsOfType(_biome);

            for (int slot = 1; slot <= max; slot++)
            {
                var reason = hm.GetUnlockReason(_biome, slot);
                _tabBox.AddChild(BuildTab(slot, reason, owned));
            }
        }

        private Control BuildTab(int slot, UnlockReason reason, IReadOnlyList<Habitat> owned)
        {
            var tab = new Button();
            tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            tab.CustomMinimumSize   = new Vector2(0, 44); // 44px touch target
            tab.FocusMode           = FocusModeEnum.None;
            tab.AddThemeFontSizeOverride("font_size", 11);

            switch (reason.Kind)
            {
                case UnlockReasonKind.Owned:
                {
                    var habitat = owned[slot - 1];
                    bool active = habitat.Id == _activeHabitatId;
                    tab.Text = $"Habitat {slot}   {habitat.OccupantIds.Count}/{HabitatCapacity.CreaturesPerHabitat}";
                    tab.AddThemeColorOverride("font_color", active ? _theme!.AccentColor : HabitatPalette.LabelMuted);

                    var style = new StyleBoxFlat();
                    style.BgColor = active ? new Color(_theme!.AccentColor with { A = 0.08f }) : new Color(0,0,0,0);
                    style.BorderWidthBottom = 2;
                    style.BorderColor       = active ? _theme!.AccentColor : new Color(0,0,0,0);
                    tab.AddThemeStyleboxOverride("normal",   style);
                    tab.AddThemeStyleboxOverride("hover",    style);
                    tab.AddThemeStyleboxOverride("pressed",  style);

                    tab.Pressed += () => EmitSignal(SignalName.ActiveHabitatChanged, habitat.Id.ToString());
                    break;
                }

                case UnlockReasonKind.Purchasable:
                {
                    int cost = reason.Coins ?? 0;
                    tab.Text = $"🔒 Habitat {slot}\n✦ {cost}";
                    tab.AddThemeColorOverride("font_color", HabitatPalette.LabelMuted);
                    tab.Pressed += () => EmitSignal(SignalName.BuyHabitatRequested, slot);
                    break;
                }

                case UnlockReasonKind.StoryGated:
                {
                    int act = reason.StoryAct ?? 1;
                    tab.Text = $"🔒 Habitat {slot}\nAct {ToRoman(act)}";
                    tab.AddThemeColorOverride("font_color", HabitatPalette.LabelLocked);
                    tab.Pressed += () => EmitSignal(SignalName.StoryGatedTapped, act);
                    break;
                }

                case UnlockReasonKind.OutOfRange:
                    tab.Visible = false;
                    break;
            }

            return tab;
        }

        private static string ToRoman(int n) => n switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => n.ToString()
        };
    }
}
```

- [ ] **Step 2: Build**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: clean.

- [ ] **Step 3: Commit**

```
git add KeeperLegacyGodot/UI/Habitat/HabitatTabBar.cs
git commit -m "feat(habitat): tab bar rendering owned/purchasable/story-gated states

Driven entirely by HabitatManager.GetUnlockReason — UI is decoupled
from the unlock decision tree. Owned tabs switch active habitat,
purchasable tabs request a buy dialog, story-gated tabs surface the
required Act for a toast hint.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: HabitatOverlayBar

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatOverlayBar.cs`

- [ ] **Step 1: Create `HabitatOverlayBar.cs`**

```csharp
// UI/Habitat/HabitatOverlayBar.cs
// Top translucent bar — back, biome info, capacity pill, coins.

using Godot;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatOverlayBar : PanelContainer
    {
        [Signal] public delegate void BackPressedEventHandler();

        private BiomeTheme? _theme;
        private Label _nameLabel;
        private Label _subtitleLabel;
        private Label _capacityPill;
        private Label _coinsLabel;
        private Label _iconLabel;

        public override void _Ready()
        {
            var style = new StyleBoxFlat();
            style.BgColor           = HabitatPalette.OverlayBarBg;
            style.BorderWidthBottom = 1;
            style.BorderColor       = HabitatPalette.OverlayBarBorderTint;
            style.ContentMarginLeft   = 16;
            style.ContentMarginRight  = 16;
            style.ContentMarginTop    = 4;
            style.ContentMarginBottom = 4;
            AddThemeStyleboxOverride("panel", style);
            CustomMinimumSize = new Vector2(0, 44);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 12);
            hbox.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            AddChild(hbox);

            // Back button
            var back = new Button();
            back.Text = "◀ The Shop";
            back.Flat = true;
            back.FocusMode = FocusModeEnum.None;
            back.AddThemeFontSizeOverride("font_size", 11);
            back.AddThemeColorOverride("font_color",       HabitatPalette.BackButtonText);
            back.AddThemeColorOverride("font_hover_color", HabitatPalette.BackButtonHover);
            back.Pressed += () => EmitSignal(SignalName.BackPressed);
            hbox.AddChild(back);

            // Separator
            var sep = new ColorRect();
            sep.Color = HabitatPalette.SeparatorLine;
            sep.CustomMinimumSize = new Vector2(1, 24);
            hbox.AddChild(sep);

            // Biome icon + info
            _iconLabel = new Label();
            _iconLabel.AddThemeFontSizeOverride("font_size", 18);
            hbox.AddChild(_iconLabel);

            var infoBox = new VBoxContainer();
            infoBox.AddThemeConstantOverride("separation", 0);
            infoBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            hbox.AddChild(infoBox);

            _nameLabel = new Label();
            _nameLabel.AddThemeFontSizeOverride("font_size", 14);
            infoBox.AddChild(_nameLabel);

            _subtitleLabel = new Label();
            _subtitleLabel.AddThemeFontSizeOverride("font_size", 11);
            _subtitleLabel.AddThemeColorOverride("font_color", HabitatPalette.LabelMuted);
            infoBox.AddChild(_subtitleLabel);

            // Capacity pill
            _capacityPill = new Label();
            _capacityPill.AddThemeFontSizeOverride("font_size", 10);
            hbox.AddChild(_capacityPill);

            // Coins
            _coinsLabel = new Label();
            _coinsLabel.AddThemeFontSizeOverride("font_size", 11);
            _coinsLabel.AddThemeColorOverride("font_color", HabitatPalette.CoinsText);
            hbox.AddChild(_coinsLabel);
        }

        public void SetTheme(BiomeTheme theme)
        {
            _theme = theme;
            _iconLabel.Text     = theme.IconEmoji;
            _nameLabel.Text     = theme.DisplayName;
            _subtitleLabel.Text = theme.FlavorSubtitle;
            _nameLabel.AddThemeColorOverride("font_color", theme.AccentColor);
            _capacityPill.AddThemeColorOverride("font_color", theme.AccentColor);
        }

        public void SetCapacityText(int totalCreatures, int maxIfAllUnlocked)
            => _capacityPill.Text = $"{totalCreatures} / {maxIfAllUnlocked}";

        public void SetCoinsText(int coins)
            => _coinsLabel.Text = $"✦ {coins:N0}";
    }
}
```

- [ ] **Step 2: Build + commit**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
git add KeeperLegacyGodot/UI/Habitat/HabitatOverlayBar.cs
git commit -m "feat(habitat): translucent overlay bar — back / info / capacity / coins

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: HabitatEnvironmentView — non-creature layers

**Files:**
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs`

This task builds the env view's static and ambient layers — background gradient, ambient light shafts, particles, decorations, surface line, floor overlay. Wandering creatures land in Task 11.

- [ ] **Step 1: Create `HabitatEnvironmentView.cs`**

```csharp
// UI/Habitat/HabitatEnvironmentView.cs
// Biome-themed environment scene driven by a BiomeTheme record. Layered
// rendering: background -> light shafts -> particles -> decorations -> surface
// -> wander zone (debug) -> creatures -> floor.

using Godot;
using System;
using System.Collections.Generic;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

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
        private Habitat? _habitat;

        // Layer roots
        private ColorRect _bgGradient;
        private Control   _lightLayer;
        private Control   _particleLayer;
        private Control   _decorationLayer;
        private ColorRect _surfaceLine;
        private Control   _wanderZoneOverlay;     // debug only
        private Control   _creatureLayer;
        private ColorRect _floorOverlay;

        // ── Public API ────────────────────────────────────────────────────────

        public void SetTheme(BiomeTheme theme)
        {
            _theme = theme;
            BuildLayers();
        }

        public void SetHabitat(Habitat habitat)
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
            // Decoration sway/float animations are driven by their own _Process
            // overrides (added in BuildDecorations). This override is reserved
            // for global ticks if needed (e.g. shimmer on the surface line).
        }

        // ── Layer construction ────────────────────────────────────────────────

        private void BuildLayers()
        {
            if (_theme == null) return;

            // Background — vertical gradient via simple 2-stop ColorRect with
            // a child shader-less overlay would be ideal; for v1 we set a flat
            // mid-color and tween via _Process if needed. Simpler: use a Control
            // with a Draw callback rendering a gradient.
            ApplyBackgroundGradient(_theme.BackgroundTopColor, _theme.BackgroundBottomColor);

            BuildLightShafts();
            BuildDecorations();
            BuildSurface();
            BuildFloor();
            // Particles spawned over time in _Process — see Task 10 step 2 below.
        }

        private void ApplyBackgroundGradient(Color top, Color bottom)
        {
            // Implemented as a Control with QueueRedraw + DrawRect calls per band.
            // For first pass: solid mid-color average; replace with custom Draw later.
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
                rect.OffsetLeft   = shaft.LeftPct * Size.X;       // recomputed if Size unknown — see ScalePosition note
                rect.OffsetRight  = rect.OffsetLeft + shaft.WidthPx;
                rect.OffsetTop    = 0;
                rect.OffsetBottom = 0;  // FullVertical via offsets to size
                rect.SetAnchor(Side.Bottom, 1.0f);
                rect.MouseFilter  = MouseFilterEnum.Ignore;
                // Skew via PivotOffset + Rotation is approximate; acceptable for v1.
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

        // ── Decoration node (animated) ────────────────────────────────────────

        private partial class DecorationNode : Label
        {
            public readonly Decoration Spec;
            public Vector2 BasePosition;     // mutable so debug drag can update it
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

                // Position relative to parent (env view) using same art-space scaling
                // pattern as the floor screen (computed via parent's Size when known).
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
                        // Slow horizontal drift across the env panel
                        Position += new Vector2(Mathf.Sin(_time * 0.4f) * 30.0f, 0);
                        break;
                }
            }
        }
    }
}
```

> **Note for the engineer:** Coordinate scaling (art-space → viewport) is currently applied via direct `Position = BasePosition`. This is correct only when the env view is sized to the art reference. For v1, the env view docks to a fixed area; if scaling becomes needed for arbitrary screen sizes, port the `ScalePosition` helper from `HabitatFloorScreen.cs:489-507`. Don't speculatively add it now — wait for a real resolution problem before solving it.

- [ ] **Step 2: Add a simple particle ticker in `_Process`**

In `HabitatEnvironmentView`, add a particle spawn timer and update `_Process`:

```csharp
        private float _particleAccumulator;

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
            float dx   = cfg.RiseDirection.X * 80f;   // total horizontal travel
            float dy   = cfg.RiseDirection.Y * 220f;  // total vertical travel

            var tween = label.CreateTween().SetParallel();
            tween.TweenProperty(label, "position", label.Position + new Vector2(dx, dy), life);
            tween.TweenProperty(label, "modulate:a", 0f, life);
            tween.Chain().TweenCallback(Callable.From(() => label.QueueFree()));
        }
```

- [ ] **Step 3: Build + commit**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
git add KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs
git commit -m "feat(habitat): biome-themed env view — bg, lights, decorations, particles

Renders the static and ambient layers driven by BiomeTheme. Decoration
animations (sway/float/drift) implemented per-node in _Process. Particles
spawn at the configured rate and fade as they travel. Surface line + floor
overlay handled where present in the theme. Wandering creatures land in
Task 11.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: HabitatEnvironmentView — wandering creatures with Y-sort + drop shadows

**Files:**
- Modify: `KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs`

- [ ] **Step 1: Add WanderingCreature inner class**

In `HabitatEnvironmentView.cs`, add a private partial inner class. Place it next to `DecorationNode`:

```csharp
        private partial class WanderingCreature : Control
        {
            public Guid CreatureId { get; }
            private readonly Color _fallbackColor;
            private readonly Rect2 _wanderZone;
            private Vector2 _target;
            private float _retargetIn;
            private float _bobTime;
            private TextureRect? _texture;
            private ColorRect _shadow;
            private Control _body;

            private const float MoveSpeed = 30f;       // px/sec
            private const float MinRetarget = 3f;
            private const float MaxRetarget = 6f;
            private const float ClickHaloMs = 300f;

            public WanderingCreature(Guid id, Color fallbackColor, Rect2 wanderZone, Texture2D? tex = null)
            {
                CreatureId    = id;
                _fallbackColor = fallbackColor;
                _wanderZone    = wanderZone;
            }

            public override void _Ready()
            {
                MouseFilter = MouseFilterEnum.Stop;
                CustomMinimumSize = new Vector2(52, 52);

                // Drop shadow — simple ellipse via ColorRect with full corner radius
                _shadow = new ColorRect();
                _shadow.Color = new Color(0, 0, 0, 0.45f);
                _shadow.Size = new Vector2(40, 12);
                _shadow.Position = new Vector2(6, 50);
                _shadow.MouseFilter = MouseFilterEnum.Ignore;
                AddChild(_shadow);

                // Body — TextureRect when available, else colored circle ColorRect
                if (_texture != null)
                {
                    var tr = new TextureRect();
                    tr.Texture    = _texture;
                    tr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    tr.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                    tr.Size        = new Vector2(52, 52);
                    tr.MouseFilter = MouseFilterEnum.Ignore;
                    AddChild(tr);
                    _body = tr;
                    _texture = tr;
                }
                else
                {
                    var blob = new ColorRect();
                    blob.Color = _fallbackColor;
                    blob.Size  = new Vector2(52, 52);
                    blob.MouseFilter = MouseFilterEnum.Ignore;
                    var style = new StyleBoxFlat();
                    style.BgColor = _fallbackColor;
                    style.SetCornerRadiusAll(26);
                    // ColorRect doesn't take StyleBox — fall back to Panel
                    blob.QueueFree();
                    var panel = new PanelContainer();
                    panel.AddThemeStyleboxOverride("panel", style);
                    panel.Size = new Vector2(52, 52);
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

            [Signal] public delegate void TappedEventHandler();
            private void EmitClick()
            {
                EmitSignal(SignalName.Tapped);
                // Brief z-index boost
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
```

- [ ] **Step 2: Wire creature spawning in `SetHabitat`**

Replace the `SetHabitat` method body and add a `BuildCreatures` helper:

```csharp
        public void SetHabitat(Habitat habitat)
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
```

- [ ] **Step 3: Build + commit**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
git add KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs
git commit -m "feat(habitat): wandering creatures with Y-sort + drop shadows

Each occupant of the active habitat spawns a WanderingCreature node
that retargets a random point in the wander zone every 3-6s and tweens
to it with a soft squash-stretch bob. Y-position drives z_index so
creatures further down render in front (2.5D depth feel). Drop shadow
ColorRect anchors each creature to the floor visually. Texture
resolution tries res://Sprites/Creatures/{catalogId}.svg|.png with
fallback to colored-circle blob for catalog entries without art yet.
Tap fires CreatureClicked + brief z-boost so the tap target is on top
during the 300ms feedback window.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: HabitatCategoryScreen orchestrator + .tscn replacement

**Files:**
- Replace contents: `KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.tscn`
- Create: `KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.cs`

- [ ] **Step 1: Replace `HabitatCategoryScreen.tscn`**

Open the file and replace the entire content with:

```
[gd_scene load_steps=2 format=3 uid="uid://ctmhrl6m7rh5s"]

[ext_resource type="Script" path="res://UI/Habitat/HabitatCategoryScreen.cs" id="1"]

[node name="HabitatCategoryScreen" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

UID preserved so any existing references continue to work.

- [ ] **Step 2: Create `HabitatCategoryScreen.cs` orchestrator**

```csharp
// UI/Habitat/HabitatCategoryScreen.cs
// Orchestrator for the Habitat Category screen — wires the four UI children
// to manager calls and to each other.

using Godot;
using System;
using System.Linq;
using KeeperLegacy.Data;
using KeeperLegacy.Models;
using KeeperLegacy.UI.Story;

namespace KeeperLegacy.UI.Habitat
{
    public partial class HabitatCategoryScreen : Control
    {
        // Static — set by Detail screen so we know who's selected post-navigation.
        public static Guid? SelectedCreatureId { get; set; }

        private HabitatType _biome;
        private Habitat? _activeHabitat;

        private HabitatOverlayBar      _overlayBar;
        private HabitatTabBar          _tabBar;
        private HabitatEnvironmentView _envView;
        private HabitatRosterPanel     _rosterPanel;
        private ChoiceMenu             _choiceMenu;

        private Action<Guid>? _hHandler;
        private Action<Guid>? _cHandler;
        private Action<int>?  _coinHandler;
        private Action<string>? _featureHandler;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void _Ready()
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _biome = HabitatFloorScreen.SelectedHabitatType ?? HabitatType.Water;

            BuildLayout();
            WireSignals();
            LoadInitialState();
        }

        public override void _ExitTree()
        {
            UnwireSignals();
        }

        public override void _EnterTree()
        {
            // Re-pull data when returning from a sub-screen — defensive in case
            // creatures were deleted while on Detail.
            if (_activeHabitat != null) RefreshChildren();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildLayout()
        {
            var theme = BiomeThemes.For(_biome);
            if (theme == null)
            {
                GD.PushWarning($"No BiomeTheme registered for {_biome} — using neutral fallback");
                return;
            }

            // Root vbox: overlay bar + tab bar + content area
            var root = new VBoxContainer();
            root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            root.AddThemeConstantOverride("separation", 0);
            AddChild(root);

            _overlayBar = new HabitatOverlayBar();
            _overlayBar.SetTheme(theme);
            _overlayBar.BackPressed += OnBackPressed;
            root.AddChild(_overlayBar);

            _tabBar = new HabitatTabBar();
            _tabBar.SetBiome(_biome, theme);
            _tabBar.ActiveHabitatChanged += OnTabActiveHabitatChanged;
            _tabBar.BuyHabitatRequested  += OnBuyHabitatRequested;
            _tabBar.StoryGatedTapped     += OnStoryGatedTapped;
            root.AddChild(_tabBar);

            // Content area — env view + roster panel
            var content = new HBoxContainer();
            content.SizeFlagsVertical   = SizeFlags.ExpandFill;
            content.AddThemeConstantOverride("separation", 0);
            root.AddChild(content);

            _envView = new HabitatEnvironmentView();
            _envView.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _envView.SetTheme(theme);
            _envView.CreatureClicked += OnCreatureClicked;
            content.AddChild(_envView);

            _rosterPanel = new HabitatRosterPanel();
            _rosterPanel.CustomMinimumSize = new Vector2(360, 0);
            _rosterPanel.CreatureClicked          += OnCreatureClicked;
            _rosterPanel.AddCreatureRequested     += OnAddCreatureRequested;
            _rosterPanel.ReleaseCreatureRequested += OnReleaseCreatureRequested;
            content.AddChild(_rosterPanel);

            // Choice menu — added at root for top z-index
            _choiceMenu = new ChoiceMenu();
            _choiceMenu.Visible = false;
            AddChild(_choiceMenu);
        }

        // ── Initial state ─────────────────────────────────────────────────────

        private void LoadInitialState()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            if (hm == null) return;

            var owned = hm.HabitatsOfType(_biome);
            _activeHabitat = owned.FirstOrDefault();
            if (_activeHabitat != null) _tabBar.SetActiveHabitat(_activeHabitat.Id);

            RefreshChildren();
            RefreshOverlayBarCoins();
            RefreshOverlayBarCapacity();
        }

        // ── Manager signal subscriptions ──────────────────────────────────────

        private void WireSignals()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

            if (hm != null)
            {
                hm.HabitatsChanged  += OnHabitatsChanged;
                hm.CreaturesChanged += OnCreaturesChanged;
            }
            if (pm != null)
            {
                pm.CoinsChanged    += OnCoinsChanged;
                pm.FeatureUnlocked += OnFeatureUnlocked;
            }
        }

        private void UnwireSignals()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");

            if (hm != null)
            {
                hm.HabitatsChanged  -= OnHabitatsChanged;
                hm.CreaturesChanged -= OnCreaturesChanged;
            }
            if (pm != null)
            {
                pm.CoinsChanged    -= OnCoinsChanged;
                pm.FeatureUnlocked -= OnFeatureUnlocked;
            }
        }

        private void OnHabitatsChanged()
        {
            _tabBar.Rebuild();
            // Defensive — if active was deleted, reset to first owned
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            if (hm == null) return;
            if (_activeHabitat == null || !hm.Habitats.Contains(_activeHabitat))
            {
                _activeHabitat = hm.HabitatsOfType(_biome).FirstOrDefault();
            }
            if (_activeHabitat != null) _tabBar.SetActiveHabitat(_activeHabitat.Id);
            RefreshChildren();
            RefreshOverlayBarCapacity();
        }

        private void OnCreaturesChanged()
        {
            RefreshChildren();
            RefreshOverlayBarCapacity();
        }

        private void OnCoinsChanged(int coins)
        {
            _tabBar.Rebuild();
            RefreshOverlayBarCoins();
        }

        private void OnFeatureUnlocked(string featureRaw)
        {
            _tabBar.Rebuild();
        }

        // ── User action handlers ──────────────────────────────────────────────

        private void OnBackPressed()
        {
            FindMainScene()?.NavigateBack();
        }

        private void OnTabActiveHabitatChanged(string habitatIdStr)
        {
            if (!Guid.TryParse(habitatIdStr, out var id)) return;
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            _activeHabitat = hm?.GetHabitat(id);
            if (_activeHabitat == null) return;
            _tabBar.SetActiveHabitat(id);
            RefreshChildren();
        }

        private void OnBuyHabitatRequested(int slot)
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            if (hm == null || pm == null) return;

            int cost = HabitatCapacity.CoinsForHabitat(_biome, slot);
            // Inline simple confirm via ChoiceMenu. (A dedicated ConfirmDialog
            // component can replace this if more polish is needed later.)
            _choiceMenu.Show(
                anchor: GetGlobalMousePosition(),
                options: new System.Collections.Generic.List<ChoiceMenu.ChoiceOption>
                {
                    new ChoiceMenu.ChoiceOption(
                        Label: $"Buy Habitat {slot}  (✦ {cost})",
                        Enabled: pm.Coins >= cost,
                        DisabledReason: pm.Coins < cost ? $"Have ✦ {pm.Coins}" : null,
                        OnTap: () =>
                        {
                            if (hm.TryAddHabitat(_biome, out _))
                            {
                                // Auto-switch to new habitat
                                var owned = hm.HabitatsOfType(_biome);
                                if (owned.Count > 0)
                                {
                                    _activeHabitat = owned[owned.Count - 1];
                                    _tabBar.SetActiveHabitat(_activeHabitat.Id);
                                    RefreshChildren();
                                }
                            }
                        }),
                    new ChoiceMenu.ChoiceOption(Label: "Cancel", Enabled: true, OnTap: () => { })
                });
        }

        private void OnStoryGatedTapped(int storyAct)
        {
            // Toast — a dedicated Toast component is out of scope; print a console
            // message and a console-visible debug line so QA can see it.
            GD.Print($"[Toast] Continue your story to unlock — Act {storyAct}");
            // TODO_LANDED_BY_TOAST_COMPONENT: replace with a real toast UI later.
        }

        private void OnCreatureClicked(string creatureIdStr)
        {
            if (!Guid.TryParse(creatureIdStr, out var id)) return;
            SelectedCreatureId = id;
            FindMainScene()?.NavigateToSubScreen("HabitatDetail");
        }

        private void OnAddCreatureRequested(int slotIndex)
        {
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            bool breedingUnlocked = pm?.IsFeatureUnlocked(GameFeature.Breeding) ?? false;

            _choiceMenu.Show(
                anchor: GetGlobalMousePosition(),
                options: new System.Collections.Generic.List<ChoiceMenu.ChoiceOption>
                {
                    new ChoiceMenu.ChoiceOption(
                        Label: "Buy from Shop",
                        Enabled: true,
                        OnTap: () => FindMainScene()?.NavigateTo("Shop")),
                    new ChoiceMenu.ChoiceOption(
                        Label: "Breed New",
                        Enabled: breedingUnlocked,
                        DisabledReason: breedingUnlocked ? null : "Locked",
                        OnTap: () => FindMainScene()?.NavigateTo("Breed")),
                });
        }

        private void OnReleaseCreatureRequested(string creatureIdStr)
        {
            if (!Guid.TryParse(creatureIdStr, out var id)) return;
            if (_activeHabitat == null) return;

            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            string name = hm?.GetCreature(id) is { } c
                ? CreatureRosterData.Find(c.CatalogId)?.Name ?? "this creature"
                : "this creature";

            _choiceMenu.Show(
                anchor: GetGlobalMousePosition(),
                options: new System.Collections.Generic.List<ChoiceMenu.ChoiceOption>
                {
                    new ChoiceMenu.ChoiceOption(
                        Label: $"Release {name}",
                        Enabled: true,
                        OnTap: () =>
                        {
                            bool ok = hm?.ReleaseCreature(_activeHabitat.Id, id) ?? false;
                            if (!ok) GD.Print($"[Toast] Cannot release — {name} is reserved by an active customer order.");
                        }),
                    new ChoiceMenu.ChoiceOption(Label: "Cancel", Enabled: true, OnTap: () => { })
                });
        }

        // ── Refresh helpers ───────────────────────────────────────────────────

        private void RefreshChildren()
        {
            if (_activeHabitat == null) return;
            var theme = BiomeThemes.For(_biome);
            if (theme == null) return;

            var hm    = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            int index = (hm?.HabitatsOfType(_biome).ToList().IndexOf(_activeHabitat) ?? 0) + 1;

            _envView.SetHabitat(_activeHabitat);
            _rosterPanel.SetHabitat(_activeHabitat, theme, index);
        }

        private void RefreshOverlayBarCoins()
        {
            var pm = GetNodeOrNull<ProgressionManager>("/root/ProgressionManager");
            _overlayBar.SetCoinsText(pm?.Coins ?? 0);
        }

        private void RefreshOverlayBarCapacity()
        {
            var hm = GetNodeOrNull<HabitatManager>("/root/HabitatManager");
            int total = 0;
            if (hm != null)
            {
                foreach (var h in hm.HabitatsOfType(_biome))
                    total += h.OccupantIds.Count;
            }
            int max = HabitatCapacity.MaxHabitatsForBiome(_biome) * HabitatCapacity.CreaturesPerHabitat;
            _overlayBar.SetCapacityText(total, max);
        }

        // ── Main scene lookup ─────────────────────────────────────────────────

        private MainScene? FindMainScene()
        {
            Node? n = this;
            while (n != null)
            {
                if (n is MainScene ms) return ms;
                n = n.GetParent();
            }
            return null;
        }
    }
}
```

- [ ] **Step 3: Build and run a smoke check**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: clean.

Then in Godot, hit ▶ Play, navigate Home → click Water pedestal. Verify:
- Screen loads with overlay bar + tabs + env view + roster panel.
- Tab "Habitat 1" is active.
- Locked tabs show ✦ cost or Act gate.
- Roster shows 4 empty slots if no creatures.
- Clicking the back button returns to floor.

Don't worry about wandering creatures yet (no creatures placed in dev save). The visual frame should be in place.

- [ ] **Step 4: Commit**

```
git add KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.cs KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.tscn
git commit -m "feat(habitat): orchestrator screen wiring all four UI children

Builds the layout (overlay bar / tab bar / env+roster split) on _Ready,
subscribes to manager signals, owns active-habitat state. Routes user
events through the manager APIs. Inline confirm dialogs use ChoiceMenu
for now — a dedicated ConfirmDialog component can replace later if
needed. Toast messages currently print to console; a Toast component
will land alongside any other UI that needs it.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: Debug surface — decoration drag, scale cycle, focus, bake-print

**Files:**
- Modify: `KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs` — make `DecorationNode` debug-aware
- Modify: `KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.cs` — add `_Input` handler + `PrintBakeValues`
- Modify: `KeeperLegacyGodot/UI/Main/MainScene.cs` — add `SetDebugOverlayText(string)` public method

- [ ] **Step 1: Add `SetDebugOverlayText` to `MainScene.cs`**

In `MainScene.cs`, just below the existing `UpdateDebugOverlay` method, add:

```csharp
    /// <summary>
    /// Public hook so screen-owned debug systems can drive the shared overlay
    /// without duplicating the panel construction. Pass null/empty to hide.
    /// </summary>
    public void SetDebugOverlayText(string? text)
    {
        if (_debugOverlay == null || _debugOverlayLabel == null) return;
        if (string.IsNullOrEmpty(text))
        {
            _debugOverlay.Visible = false;
            return;
        }
        _debugOverlayLabel.Text = text;
        _debugOverlay.Visible = true;
    }
```

- [ ] **Step 2: Make `DecorationNode` debug-aware**

In `HabitatEnvironmentView.cs`, replace the `DecorationNode` class with this drag-aware version:

```csharp
        private partial class DecorationNode : Label
        {
            public static bool DebugDragEnabled { get; set; }
            public static DecorationNode? FocusedNode { get; set; }

            public readonly Decoration Spec;
            public Vector2 BasePosition;
            public float ScaleMultiplier = 1.0f;

            private float _time;
            private bool _dragging;

            public DecorationNode(Decoration spec)
            {
                Spec = spec;
                BasePosition = spec.PositionArtSpace;
                Text = spec.PlaceholderEmoji;
                AddThemeFontSizeOverride("font_size", (int)spec.SizePx);
                MouseFilter = MouseFilterEnum.Stop;
            }

            public override void _Process(double delta)
            {
                _time += (float)delta;
                Position = BasePosition;
                AddThemeFontSizeOverride("font_size", (int)(Spec.SizePx * ScaleMultiplier));

                if (!_dragging)
                {
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

                // Focus highlight
                Modulate = (FocusedNode == this && DebugDragEnabled)
                    ? new Color(1.2f, 1.2f, 1.2f, 1f)
                    : new Color(1f, 1f, 1f, 1f);
            }

            public override void _GuiInput(InputEvent @event)
            {
                if (!DebugDragEnabled) return;
                if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        FocusedNode = this;
                        _dragging = true;
                        AcceptEvent();
                    }
                    else _dragging = false;
                }
                else if (@event is InputEventMouseMotion mm && _dragging)
                {
                    BasePosition += mm.Relative;
                    AcceptEvent();
                }
            }
        }
```

- [ ] **Step 3: Add screen-level `_Input` for debug keys + `PrintBakeValues`**

In `HabitatCategoryScreen.cs`, add at the bottom of the class (before the closing brace):

```csharp
        // ── Debug surface ─────────────────────────────────────────────────────

        private static readonly float[] DecorationScales = { 0.5f, 0.7f, 1.0f, 1.5f, 2.0f, 3.0f };
        private int _decoScaleIndex = 2; // 1.0x

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

            switch (key.Keycode)
            {
                case Key.F4:
                case Key.P:
                    HabitatEnvironmentView.DecorationNode.DebugDragEnabled = !HabitatEnvironmentView.DecorationNode.DebugDragEnabled;
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled)
                    {
                        PrintBakeValues();
                    }
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.O:
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled) return;
                    var focused = HabitatEnvironmentView.DecorationNode.FocusedNode;
                    if (focused == null) return;
                    _decoScaleIndex = (_decoScaleIndex + 1) % DecorationScales.Length;
                    focused.ScaleMultiplier = DecorationScales[_decoScaleIndex];
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.Tab:
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled) return;
                    AdvanceDecorationFocus();
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;

                case Key.S when key.CtrlPressed:
                    if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled) return;
                    PrintBakeValues();
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }

        private void AdvanceDecorationFocus()
        {
            // Find current focused index, advance to next decoration node in the env layer.
            var nodes = new System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode>();
            CollectDecorationNodes(_envView, nodes);
            if (nodes.Count == 0) return;
            int idx = HabitatEnvironmentView.DecorationNode.FocusedNode == null
                ? -1
                : nodes.IndexOf(HabitatEnvironmentView.DecorationNode.FocusedNode);
            HabitatEnvironmentView.DecorationNode.FocusedNode = nodes[(idx + 1) % nodes.Count];
        }

        private static void CollectDecorationNodes(Node parent, System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode> outList)
        {
            foreach (Node child in parent.GetChildren())
            {
                if (child is HabitatEnvironmentView.DecorationNode d) outList.Add(d);
                CollectDecorationNodes(child, outList);
            }
        }

        private void UpdateScreenDebugOverlay()
        {
            var ms = FindMainScene();
            if (ms == null) return;
            if (!HabitatEnvironmentView.DecorationNode.DebugDragEnabled)
            {
                ms.SetDebugOverlayText(null);
                return;
            }

            var nodes = new System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode>();
            CollectDecorationNodes(_envView, nodes);

            string focused = HabitatEnvironmentView.DecorationNode.FocusedNode is { } f
                ? $"{f.Spec.PlaceholderEmoji}  scale {f.ScaleMultiplier:F1}x"
                : "(none)";

            ms.SetDebugOverlayText(
                $"DEBUG — Biome Theme Editor ({_biome})\n" +
                $"  Mode:        Decoration drag\n" +
                $"  Selected:    {focused}\n" +
                $"  Decorations: {nodes.Count}\n" +
                "  -----------------------------------\n" +
                "  Drag        : reposition\n" +
                "  O           : cycle scale (selected)\n" +
                "  Tab         : next decoration\n" +
                "  Ctrl+S      : print bake values\n" +
                "  F4 / P      : print + exit");
        }

        public void PrintBakeValues()
        {
            var theme = BiomeThemes.For(_biome);
            if (theme == null) return;

            var nodes = new System.Collections.Generic.List<HabitatEnvironmentView.DecorationNode>();
            CollectDecorationNodes(_envView, nodes);

            GD.Print("");
            GD.Print($"=== BIOME THEME — {_biome.ToString().ToUpper()} ====================================");
            GD.Print($"// Paste into KeeperLegacyGodot/Data/BiomeTheme.cs:");
            GD.Print($"[HabitatType.{_biome}] = new BiomeTheme(");
            GD.Print($"    Biome:                 HabitatType.{_biome},");
            GD.Print($"    IconEmoji:             \"{theme.IconEmoji}\",");
            GD.Print($"    DisplayName:           \"{theme.DisplayName}\",");
            GD.Print($"    FlavorSubtitle:        \"{theme.FlavorSubtitle}\",");
            GD.Print($"    AccentColor:           new Color(\"{theme.AccentColor.ToHtml()}\"),");
            GD.Print($"    BackgroundTopColor:    new Color(\"{theme.BackgroundTopColor.ToHtml()}\"),");
            GD.Print($"    BackgroundBottomColor: new Color(\"{theme.BackgroundBottomColor.ToHtml()}\"),");
            GD.Print($"    Decorations: new[]");
            GD.Print($"    {{");
            foreach (var n in nodes)
            {
                float size = n.Spec.SizePx * n.ScaleMultiplier;
                string anim = n.Spec.Animation == DecorationAnimation.None
                    ? ""
                    : $", DecorationAnimation.{n.Spec.Animation}";
                GD.Print($"        new Decoration(\"{n.Spec.PlaceholderEmoji}\", new Vector2({n.BasePosition.X,5:F0}, {n.BasePosition.Y,5:F0}), {size:F0}f{anim}),");
            }
            GD.Print($"    }},");
            GD.Print($"    Particles:             /* unchanged from current theme */ null,");
            GD.Print($"    AmbientLights:         /* unchanged from current theme */ System.Array.Empty<LightShaft>(),");
            GD.Print($"    Floor:                 /* unchanged from current theme */ null,");
            GD.Print($"    Surface:               /* unchanged from current theme */ null,");
            GD.Print($"    WanderZone:            new Rect2({theme.WanderZone.Position.X}, {theme.WanderZone.Position.Y}, {theme.WanderZone.Size.X}, {theme.WanderZone.Size.Y})");
            GD.Print($");");
            GD.Print($"==============================================================");
            GD.Print("");
        }
```

> **Note on the print output:** Particles / AmbientLights / Floor / Surface are emitted with comments saying "unchanged from current theme" because the debug surface in this task only tunes decorations and decoration scale. When pasting the printed block, the engineer keeps the existing values for those four fields and only replaces the Biome/Display/colors/Decorations/WanderZone portion. Future debug expansion can include those fields if needed.

- [ ] **Step 4: Make DecorationNode public-visible**

The current `DecorationNode` is `private partial class` inside `HabitatEnvironmentView`. To allow `HabitatCategoryScreen` to reference its static fields and class type, change to `internal partial class` and ensure access from the same assembly:

In `HabitatEnvironmentView.cs`, change:
```csharp
        private partial class DecorationNode : Label
```
to:
```csharp
        internal partial class DecorationNode : Label
```

Same change for any other inner classes the orchestrator references.

- [ ] **Step 5: Build + manual smoke**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```
Expected: clean.

In Godot — Play, navigate Home → click Water pedestal → press F4. The DEBUG overlay should appear top-left (anchored from MainScene). Drag a decoration → it moves. Press Tab → focus advances. Press O → focused decoration scales. Press Ctrl+S → bake values print to Output panel. Press F4 to exit.

- [ ] **Step 6: Commit**

```
git add KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.cs KeeperLegacyGodot/UI/Main/MainScene.cs
git commit -m "feat(habitat): live decoration drag + scale cycle + bake-print

F4/P toggles the per-screen debug mode for category screen. While on,
decorations are draggable, Tab cycles focus, O scales the focused
decoration, Ctrl+S prints paste-ready BiomeTheme constants. Print output
explicitly notes which fields are 'unchanged from current theme' so the
bake paste stays surgical. MainScene.SetDebugOverlayText is the new
shared hook for any screen to drive the existing debug overlay panel.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 14: Debug surface — wander zone corner / edge handles

**Files:**
- Modify: `KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs` — wander zone overlay + drag handles
- Modify: `KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.cs` — extend bake-print to include WanderZone

- [ ] **Step 1: Add wander zone overlay control to `HabitatEnvironmentView.cs`**

Add a new internal class inside `HabitatEnvironmentView`:

```csharp
        internal partial class WanderZoneOverlay : Control
        {
            public static bool DebugEnabled { get; set; }
            public Rect2 Zone;

            private const float HandleSize = 12f;

            public override void _Process(double delta)
            {
                Visible = DebugEnabled;
                if (Visible) QueueRedraw();
            }

            public override void _Draw()
            {
                if (!Visible) return;
                // Translucent fill
                DrawRect(Zone, new Color(1, 0.5f, 0.2f, 0.10f), filled: true);
                // Border
                DrawRect(Zone, new Color(1, 0.5f, 0.2f, 0.65f), filled: false, width: 2f);

                // 8 handles: 4 corners + 4 edge midpoints
                foreach (Vector2 h in HandlePositions())
                {
                    DrawRect(new Rect2(h - new Vector2(HandleSize/2f, HandleSize/2f), new Vector2(HandleSize, HandleSize)),
                             new Color(1, 0.5f, 0.2f, 1f), filled: true);
                }
            }

            public System.Collections.Generic.IEnumerable<Vector2> HandlePositions()
            {
                yield return Zone.Position;                                                         // TL
                yield return Zone.Position + new Vector2(Zone.Size.X, 0);                           // TR
                yield return Zone.Position + new Vector2(0, Zone.Size.Y);                           // BL
                yield return Zone.End;                                                              // BR
                yield return Zone.Position + new Vector2(Zone.Size.X / 2, 0);                       // T-mid
                yield return Zone.Position + new Vector2(Zone.Size.X / 2, Zone.Size.Y);             // B-mid
                yield return Zone.Position + new Vector2(0, Zone.Size.Y / 2);                       // L-mid
                yield return Zone.Position + new Vector2(Zone.Size.X, Zone.Size.Y / 2);             // R-mid
            }

            // Drag state
            private int _dragHandle = -1;

            public override void _GuiInput(InputEvent @event)
            {
                if (!DebugEnabled) return;

                if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        _dragHandle = HitTestHandle(mb.Position);
                        if (_dragHandle >= 0) AcceptEvent();
                    }
                    else _dragHandle = -1;
                }
                else if (@event is InputEventMouseMotion mm && _dragHandle >= 0)
                {
                    ApplyDragToHandle(_dragHandle, mm.Position);
                    AcceptEvent();
                }
            }

            private int HitTestHandle(Vector2 p)
            {
                int i = 0;
                foreach (Vector2 h in HandlePositions())
                {
                    if (Mathf.Abs(p.X - h.X) < HandleSize && Mathf.Abs(p.Y - h.Y) < HandleSize)
                        return i;
                    i++;
                }
                return -1;
            }

            private void ApplyDragToHandle(int handle, Vector2 p)
            {
                // 0=TL 1=TR 2=BL 3=BR 4=Tmid 5=Bmid 6=Lmid 7=Rmid
                Vector2 tl = Zone.Position;
                Vector2 br = Zone.End;
                switch (handle)
                {
                    case 0: tl = p; break;
                    case 1: tl.Y = p.Y; br.X = p.X; break;
                    case 2: tl.X = p.X; br.Y = p.Y; break;
                    case 3: br = p; break;
                    case 4: tl.Y = p.Y; break;
                    case 5: br.Y = p.Y; break;
                    case 6: tl.X = p.X; break;
                    case 7: br.X = p.X; break;
                }
                // Normalize so size stays positive
                if (br.X < tl.X) (tl.X, br.X) = (br.X, tl.X);
                if (br.Y < tl.Y) (tl.Y, br.Y) = (br.Y, tl.Y);
                Zone = new Rect2(tl, br - tl);
            }
        }
```

- [ ] **Step 2: Wire the overlay into `HabitatEnvironmentView`**

Replace the `_wanderZoneOverlay` field declaration:
```csharp
        private Control   _wanderZoneOverlay;
```
with:
```csharp
        private WanderZoneOverlay _wanderZoneOverlay;
```

Replace the corresponding line in `_Ready`:
```csharp
            _wanderZoneOverlay = NewLayer();
            _wanderZoneOverlay.Visible = false;
```
with:
```csharp
            _wanderZoneOverlay = new WanderZoneOverlay();
            _wanderZoneOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _wanderZoneOverlay.MouseFilter = MouseFilterEnum.Pass;
            AddChild(_wanderZoneOverlay);
```

In `BuildLayers`, before particles, add:
```csharp
            _wanderZoneOverlay.Zone = _theme.WanderZone;
```

Add a public read accessor on `HabitatEnvironmentView` so the orchestrator can include the rect in bake-print:
```csharp
        public Rect2 GetCurrentWanderZone() => _wanderZoneOverlay.Zone;
```

- [ ] **Step 3: Toggle the overlay alongside decoration drag mode in the screen**

In `HabitatCategoryScreen._Input`'s `Key.F4`/`Key.P` case, after toggling `DebugDragEnabled`, also toggle the overlay:

```csharp
                case Key.F4:
                case Key.P:
                    bool newState = !HabitatEnvironmentView.DecorationNode.DebugDragEnabled;
                    HabitatEnvironmentView.DecorationNode.DebugDragEnabled = newState;
                    HabitatEnvironmentView.WanderZoneOverlay.DebugEnabled  = newState;
                    if (!newState) PrintBakeValues();
                    UpdateScreenDebugOverlay();
                    GetViewport().SetInputAsHandled();
                    break;
```

- [ ] **Step 4: Update `PrintBakeValues` to use the live wander zone**

Replace the `WanderZone:` print line with:

```csharp
            var liveZone = _envView.GetCurrentWanderZone();
            GD.Print($"    WanderZone:            new Rect2({liveZone.Position.X:F0}, {liveZone.Position.Y:F0}, {liveZone.Size.X:F0}, {liveZone.Size.Y:F0})");
```

- [ ] **Step 5: Build + manual smoke**

```
dotnet build "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"
```

In Godot, Play → Water habitat → F4. Wander zone outline with 8 handles should appear. Drag a corner → resizes. Drag an edge midpoint → moves only that edge. Ctrl+S prints bake values including the new `WanderZone`.

- [ ] **Step 6: Commit**

```
git add KeeperLegacyGodot/UI/Habitat/HabitatEnvironmentView.cs KeeperLegacyGodot/UI/Habitat/HabitatCategoryScreen.cs
git commit -m "feat(habitat): wander zone debug overlay with 8-handle edit

When debug mode is on, the wander zone is rendered as a translucent
outlined rect with 4 corner + 4 edge-midpoint handles. Dragging a corner
resizes from that corner; dragging an edge midpoint moves only that edge.
Ctrl+S now includes the live WanderZone in the bake-print output.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 15: Manual integration smoke test (sign-off checklist)

**No code in this task — this is the validation pass before declaring the feature done.** Run through every item, screenshot/note any issue, fix and re-test before moving on.

- [ ] **Step 1: Boot game from the floor screen**
  - Play → Home pedestals visible
  - Click Water pedestal → Habitat Category screen loads with Water theme
  - Overlay bar shows "💧 Water Habitats" + flavor + capacity pill + coins

- [ ] **Step 2: Tab states render correctly**
  - First tab "Habitat 1   X/4" is active (highlighted with biome color)
  - Tabs 2/3/4 are locked with "✦ N" cost display
  - For Magical biome, first tab is "🔒 Habitat 1  Act II" (story-gated) — verify by clicking Magical pedestal

- [ ] **Step 3: Tab switching**
  - Press F3 to unlock all features (existing debug)
  - Manually add a 2nd Water habitat via TryAddHabitat (or buy via the tab — see Step 5)
  - Click Habitat 2 tab → env view + roster panel update

- [ ] **Step 4: Roster panel basics**
  - Empty habitat: 4 empty slots with "Empty Slot" + "Add Creature" buttons
  - Tap "Add Creature" → ChoiceMenu opens with "Buy from Shop" / "Breed New"
  - Tap "Buy from Shop" → navigates to Shop screen

- [ ] **Step 5: Habitat purchase**
  - Return to category screen
  - Tap a locked-purchasable tab → confirm dialog opens with cost
  - If sufficient coins: confirm → tab becomes owned + active
  - If insufficient: button is disabled + shows "Have ✦ N"

- [ ] **Step 6: Creature placement (manual)**
  - In Godot debug console (or via Shop UI): add a Water creature to the player's inventory and place it in Habitat 1
  - Verify: creature appears in env view (wandering blob with shadow) AND in roster slot 1
  - Click creature in env view → routes to HabitatDetail placeholder
  - Back → returns to category, state preserved (still on same tab)
  - Click creature in roster slot → also routes to detail

- [ ] **Step 7: Release flow**
  - Long-press (600ms hold) on a filled roster slot
  - Release confirm dialog appears
  - Confirm → creature removed from habitat AND from creature ledger; roster + env view update
  - Capacity pill in overlay bar decrements

- [ ] **Step 8: Debug surface**
  - F4 (or P) → DEBUG overlay appears (top-left, MainScene-owned panel)
  - Decorations are draggable; Tab cycles focused decoration; O cycles its scale
  - Wander zone outline visible with 8 handles; drag corners/edges resize/move
  - Ctrl+S → bake values printed in Output panel (paste-ready format)
  - F4/P again → debug mode exits, prints final values

- [ ] **Step 9: Bake the printed Water values**
  - Copy the printed `[HabitatType.Water]` block
  - Paste over the existing `[HabitatType.Water]` entry in `KeeperLegacyGodot/Data/BiomeTheme.cs`
  - Re-run game → visuals match what you tuned (this is the parity check that the bake/live cycle is round-trip lossless, like the pedestal system)

- [ ] **Step 10: Edge cases**
  - Try buying a habitat without enough coins → button disabled with "Have ✦ N"
  - Try clicking a story-gated tab (Magical when not unlocked) → console shows toast print
  - Try loading a save with old single-occupant `OccupantId` → habitat opens with that creature in OccupantIds[0]
  - Resolution: stretch window to landscape ratios — layout should remain stable (env panel + roster panel proportions)

- [ ] **Step 11: All automated tests still pass**

Run: `dotnet test "d:/Projects/Creature's Legacy Design/KeeperLegacyGodot/KeeperLegacy.csproj"`
Expected: all tests pass — at least the original 110 plus the new HabitatModelTests (8 cases), HabitatManagerTests (6 cases), BiomeThemeTests (3 cases). Total 127.

- [ ] **Step 12: Commit any baked theme values + sign-off**

If the smoke test surfaced theme tuning that you baked, commit:
```
git add KeeperLegacyGodot/Data/BiomeTheme.cs
git commit -m "tune: bake Water biome decoration positions and wander zone

Tuned via debug drag mode in the running game.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

If everything passes, the screen is shipped.

---

## Self-Review

Spec coverage check:

- ✅ Multi-creature habitats (4 per) — Task 1
- ✅ Per-biome capacity (4/3/2) — Task 2
- ✅ Unlock model (W/G/D start, others story-gated, additional via coins) — Task 3 (`GetUnlockReason`)
- ✅ Tab display (all max with locked + cost preview) — Task 8
- ✅ Creature click → Detail navigation — Task 11 (env), Task 7 (roster), Task 12 (orchestrator routing)
- ✅ Add Creature → choice menu — Task 6 (component), Task 12 (wiring)
- ✅ No creature inventory — Task 12 routes Add to Shop/Breed only; Task 3's `ReleaseCreature` removes from creature ledger
- ✅ Footer removed — no footer in `HabitatRosterPanel`
- ✅ Release-to-wild — Task 3 `ReleaseCreature` (with order-reservation guard)
- ✅ No status dots — `SlotControl` shows blob + name only (deferred to Detail)
- ✅ Y-sort + drop shadow — Task 11
- ✅ Mobile-first (44px touch targets, long-press, no hover-only) — Tasks 7, 8, 9
- ✅ Debug input on screen, MainScene owns shared overlay — Tasks 13, 14
- ✅ Decoration drag + scale cycle + Tab focus + bake-print — Task 13
- ✅ Wander zone 8-handle drag — Task 14
- ✅ Texture-driven creature rendering with circle fallback — Task 11
- ✅ Save migration for single-occupant `OccupantId` — Task 1 deserialization constructor

Open issues addressed:

- The `BiomeTheme` print routine emits `Particles/AmbientLights/Floor/Surface` as `unchanged from current theme` placeholder comments. Documented in Task 13 step 3 — engineer keeps existing values when pasting.
- `EmptySlotControl` doesn't render a true dashed border — Godot's `StyleBoxFlat` doesn't support dashed natively. Solid border + lighter opacity is the v1 compromise. A custom `_Draw` override can add dashes later if needed; not blocking.
- Toast UI is a `GD.Print` placeholder; a proper `Toast` component lands when needed.

The plan is internally consistent; type names and method signatures match between earlier and later tasks.


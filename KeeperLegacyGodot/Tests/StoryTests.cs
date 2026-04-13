// Tests/StoryTests.cs
// Port of StoryTests.swift
// Tests story event structure, PlayerStoryState, StoryEngine trigger logic,
// event completion side-effects, and save/load round-trip.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KeeperLegacy.Data;
using KeeperLegacy.Models;

namespace KeeperLegacy.Tests
{
    [TestFixture]
    public class StoryTests
    {
        // ── Event Structure ───────────────────────────────────────────────────

        [Test]
        public void AllEventsHaveUniqueIds()
        {
            var ids    = StoryEvent.AllEvents.Select(e => e.Id).ToList();
            var unique = new HashSet<string>(ids);
            Assert.That(ids.Count, Is.EqualTo(unique.Count), "Duplicate story event IDs found");
        }

        [Test]
        public void AllEventsHaveNonEmptyTitle()
        {
            foreach (var e in StoryEvent.AllEvents)
                Assert.That(e.Title, Is.Not.Empty, $"Event '{e.Id}' has empty title");
        }

        [Test]
        public void AllEventsHaveNonEmptyBody()
        {
            foreach (var e in StoryEvent.AllEvents)
                Assert.That(e.Body, Is.Not.Empty, $"Event '{e.Id}' has empty body");
        }

        [Test]
        public void AllEventsReferenceValidNpcs()
        {
            var npcIds = new HashSet<string>(NPC.MainCast.Select(n => n.Id));
            foreach (var e in StoryEvent.AllEvents)
                Assert.That(npcIds, Does.Contain(e.NpcId),
                    $"Event '{e.Id}' references unknown NPC '{e.NpcId}'");
        }

        [Test]
        public void EventsAreInChronologicalActOrder()
        {
            // Acts should only ever stay the same or increase as we iterate AllEvents
            int maxActSeen = 0;
            foreach (var e in StoryEvent.AllEvents)
            {
                int act = (int)e.Act;
                Assert.That(act, Is.GreaterThanOrEqualTo(maxActSeen),
                    $"Event '{e.Id}' (Act {act}) appears after Act {maxActSeen} — wrong order");
                maxActSeen = act;
            }
        }

        [Test]
        public void CapstoneEventsUnlockFeaturesOrAdvanceAct()
        {
            // Every event marked AdvancesToNextAct must also unlock a feature
            // (Capstones always do both — the act gate is tied to the feature unlock)
            foreach (var e in StoryEvent.AllEvents.Where(ev => ev.AdvancesToNextAct))
                Assert.That(e.UnlocksFeature, Is.Not.Null,
                    $"Capstone event '{e.Id}' advances act but unlocks no feature");
        }

        // ── PlayerStoryState ──────────────────────────────────────────────────

        [Test]
        public void InitialStoryStateIsAct1()
        {
            var state = new PlayerStoryState();
            Assert.That(state.CurrentAct, Is.EqualTo(1));
        }

        [Test]
        public void NoEventsCompletedInitially()
        {
            var state = new PlayerStoryState();
            Assert.That(state.CompletedEventIds, Is.Empty);
        }

        [Test]
        public void CompleteEventAddsToCompletedSet()
        {
            var state = new PlayerStoryState();
            state.CompleteEvent("act1_first_egg");
            Assert.That(state.HasCompletedEvent("act1_first_egg"), Is.True);
        }

        [Test]
        public void CompleteEventIsIdempotent()
        {
            var state = new PlayerStoryState();
            state.CompleteEvent("act1_first_egg");
            state.CompleteEvent("act1_first_egg");
            int count = state.CompletedEventIds.Count(id => id == "act1_first_egg");
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void HasCompletedEventReturnsFalseForUnknown()
        {
            var state = new PlayerStoryState();
            Assert.That(state.HasCompletedEvent("nonexistent_event"), Is.False);
        }

        [Test]
        public void IncreaseRelationshipAddsAmount()
        {
            var state = new PlayerStoryState();
            state.IncreaseRelationship("elder_mira", 10);
            int level = state.NpcRelationshipLevels["elder_mira"];
            Assert.That(level, Is.GreaterThan(0));
        }

        [Test]
        public void RelationshipClampedAt100()
        {
            var state = new PlayerStoryState();
            state.IncreaseRelationship("elder_mira", 999);
            Assert.That(state.NpcRelationshipLevels["elder_mira"], Is.LessThanOrEqualTo(100));
        }

        [Test]
        public void InitialNpcRelationshipLevelsMatchMainCast()
        {
            var state  = new PlayerStoryState();
            var npcIds = new HashSet<string>(NPC.MainCast.Select(n => n.Id));
            foreach (var id in npcIds)
                Assert.That(state.NpcRelationshipLevels, Does.ContainKey(id),
                    $"Initial state missing relationship for NPC '{id}'");
        }

        // ── StoryEngine Triggers ──────────────────────────────────────────────

        [Test]
        public void NoEventPendingBeforeTriggerCheck()
        {
            var state  = new PlayerStoryState();
            var engine = new StoryEngine(state);
            Assert.That(engine.PendingEvent, Is.Null);
        }

        [Test]
        public void FirstEggEventTriggersAtLevel1()
        {
            var state  = new PlayerStoryState();
            var engine = new StoryEngine(state);
            engine.CheckTriggers(level: 1);
            Assert.That(engine.PendingEvent, Is.Not.Null);
            Assert.That(engine.PendingEvent!.Id, Is.EqualTo("act1_first_egg"));
        }

        [Test]
        public void NoEventsAtLevel0()
        {
            var state  = new PlayerStoryState();
            var engine = new StoryEngine(state);
            engine.CheckTriggers(level: 0);
            Assert.That(engine.PendingEvent, Is.Null);
        }

        [Test]
        public void OnlyOneEventPendsAtATime()
        {
            var state = new PlayerStoryState();
            // Pre-complete act1_first_egg so a second event could be eligible
            state.CompleteEvent("act1_first_egg");
            var engine = new StoryEngine(state);
            engine.CheckTriggers(level: 10);
            // Even if multiple events are eligible, only one should be pending
            Assert.That(engine.PendingEvent, Is.Not.Null);
            // The count of "pending" is implicitly 1 because PendingEvent is a single reference
        }

        [Test]
        public void ShopSecretEventTriggersAtLevel10()
        {
            var state = new PlayerStoryState();
            state.CompleteEvent("act1_first_egg");   // Prerequisite
            var engine = new StoryEngine(state);
            engine.CheckTriggers(level: 10);
            Assert.That(engine.PendingEvent, Is.Not.Null);
            Assert.That(engine.PendingEvent!.Id, Is.EqualTo("act1_shop_secret"));
        }

        [Test]
        public void ShopSecretDoesNotTriggerWithoutPrerequisite()
        {
            var state  = new PlayerStoryState();
            // act1_first_egg NOT completed
            var engine = new StoryEngine(state);
            engine.CheckTriggers(level: 10);
            // Should get act1_first_egg (still pending), not act1_shop_secret
            Assert.That(engine.PendingEvent?.Id, Is.Not.EqualTo("act1_shop_secret"));
        }

        [Test]
        public void AlreadyCompletedEventDoesNotPendAgain()
        {
            var state = new PlayerStoryState();
            state.CompleteEvent("act1_first_egg");
            var engine = new StoryEngine(state);
            engine.CheckTriggers(level: 1);
            // act1_first_egg is done — should not re-trigger
            Assert.That(engine.PendingEvent?.Id, Is.Not.EqualTo("act1_first_egg"));
        }

        // ── Event Completion Side-Effects ─────────────────────────────────────

        [Test]
        public void CompletingEventUnlocksAssociatedFeature()
        {
            var state       = new PlayerStoryState();
            var engine      = new StoryEngine(state);
            var progression = new PlayerProgression();
            progression.CurrentLevel = 12;
            progression.StoryAct     = 1;

            // Manually set the capstone event as pending
            var capstone = StoryEvent.AllEvents.First(e => e.UnlocksFeature == GameFeature.Breeding);
            engine.PendingEvent = capstone;
            engine.CompleteCurrentEvent(progression, currentLevel: 12);

            Assert.That(progression.IsFeatureUnlocked(GameFeature.Breeding), Is.True);
        }

        [Test]
        public void CompletingCapstoneAdvancesStoryAct()
        {
            var state       = new PlayerStoryState();
            var engine      = new StoryEngine(state);
            var progression = new PlayerProgression();

            var capstone = StoryEvent.AllEvents.First(e => e.AdvancesToNextAct && e.Act == StoryAct.Act1);
            engine.PendingEvent = capstone;
            engine.CompleteCurrentEvent(progression, currentLevel: 12);

            Assert.That(state.CurrentAct, Is.EqualTo(2));
        }

        [Test]
        public void CompletingEventBoostsNpcRelationship()
        {
            var state  = new PlayerStoryState();
            var engine = new StoryEngine(state);
            var prog   = new PlayerProgression();

            var firstEvent     = StoryEvent.AllEvents.First();
            int levelBefore    = state.NpcRelationshipLevels.GetValueOrDefault(firstEvent.NpcId, 0);
            engine.PendingEvent = firstEvent;
            engine.CompleteCurrentEvent(prog, currentLevel: 1);

            int levelAfter = state.NpcRelationshipLevels.GetValueOrDefault(firstEvent.NpcId, 0);
            Assert.That(levelAfter, Is.GreaterThan(levelBefore));
        }

        [Test]
        public void CompletingEventClearsPending()
        {
            var state  = new PlayerStoryState();
            var engine = new StoryEngine(state);
            var prog   = new PlayerProgression();

            engine.PendingEvent = StoryEvent.AllEvents.First();
            engine.CompleteCurrentEvent(prog, currentLevel: 1);

            // PendingEvent is either null or the NEXT chained event — not the same one
            Assert.That(engine.PendingEvent?.Id,
                Is.Not.EqualTo(StoryEvent.AllEvents.First().Id));
        }

        [Test]
        public void CompletingEventMarksIdAsCompleted()
        {
            var state       = new PlayerStoryState();
            var engine      = new StoryEngine(state);
            var prog        = new PlayerProgression();
            var firstEvent  = StoryEvent.AllEvents.First();

            engine.PendingEvent = firstEvent;
            engine.CompleteCurrentEvent(prog, currentLevel: 1);

            Assert.That(state.HasCompletedEvent(firstEvent.Id), Is.True);
        }

        // ── Save / Load Round-Trip ────────────────────────────────────────────

        [Test]
        public void SaveAndLoadStoryStateRoundTrip()
        {
            var state = new PlayerStoryState();
            state.CurrentAct = 2;
            state.CompleteEvent("act1_first_egg");
            state.CompleteEvent("act1_shop_secret");
            state.IncreaseRelationship("elder_mira", 15);

            // Flatten
            var save = new Data.StorySave
            {
                CurrentAct         = state.CurrentAct,
                CompletedEventIds  = new List<string>(state.CompletedEventIds),
                NpcRelationshipLevels   = new Dictionary<string, int>(state.NpcRelationshipLevels),
            };

            // Re-inflate
            var loaded = new PlayerStoryState
            {
                CurrentAct            = save.CurrentAct,
                CompletedEventIds     = new HashSet<string>(save.CompletedEventIds),
                NpcRelationshipLevels = new Dictionary<string, int>(save.NpcRelationshipLevels),
            };

            Assert.That(loaded.CurrentAct,                            Is.EqualTo(2));
            Assert.That(loaded.HasCompletedEvent("act1_first_egg"),   Is.True);
            Assert.That(loaded.HasCompletedEvent("act1_shop_secret"), Is.True);
            Assert.That(loaded.NpcRelationshipLevels["elder_mira"],   Is.GreaterThan(0));
        }

        [Test]
        public void SaveAndLoadPreservesAllCompletedEvents()
        {
            var state = new PlayerStoryState();
            foreach (var e in StoryEvent.AllEvents.Take(3))
                state.CompleteEvent(e.Id);

            var save = new Data.StorySave
            {
                CurrentAct        = state.CurrentAct,
                CompletedEventIds = new List<string>(state.CompletedEventIds),
                NpcRelationshipLevels  = new Dictionary<string, int>(state.NpcRelationshipLevels),
            };

            var loaded = new PlayerStoryState
            {
                CurrentAct            = save.CurrentAct,
                CompletedEventIds     = new HashSet<string>(save.CompletedEventIds),
                NpcRelationshipLevels = new Dictionary<string, int>(save.NpcRelationshipLevels),
            };

            Assert.That(loaded.CompletedEventIds.Count, Is.EqualTo(3));
        }
    }
}

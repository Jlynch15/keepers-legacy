// Models/StoryEngine.cs
// Pure C# story trigger and completion logic — no Godot dependency.
// Used by StoryManager (Godot autoload) and unit tests.

using System;
using System.Collections.Generic;

namespace KeeperLegacy.Models
{
    public class StoryEngine
    {
        public PlayerStoryState State { get; private set; }
        public StoryEvent? PendingEvent { get; set; }

        public StoryEngine(PlayerStoryState? initialState = null)
        {
            State = initialState ?? new PlayerStoryState();
        }

        // ── Trigger Evaluation ────────────────────────────────────────────────

        /// Evaluate all story events and queue the first eligible unfired one.
        /// Only one event may be pending at a time.
        public void CheckTriggers(
            int level           = 0,
            int fulfilledOrders = 0,
            int breedCount      = 0)
        {
            if (PendingEvent != null) return;

            foreach (var evt in StoryEvent.AllEvents)
            {
                if (State.HasCompletedEvent(evt.Id)) continue;
                if (IsEligible(evt, level, fulfilledOrders, breedCount))
                {
                    PendingEvent = evt;
                    return;
                }
            }
        }

        private bool IsEligible(StoryEvent evt, int level, int orders, int breeds)
        {
            return evt.Trigger switch
            {
                StoryEvent.ReachLevel   t => level        >= t.Level,
                StoryEvent.CompleteEvent t => State.HasCompletedEvent(t.EventId),
                StoryEvent.FulfillOrders t => orders      >= t.Count,
                StoryEvent.BreedCreatures t => breeds     >= t.Count,
                StoryEvent.Manual         _ => false,
                _                         => false
            };
        }

        // ── Event Completion ──────────────────────────────────────────────────

        /// Complete the currently pending event.
        /// - Marks it done
        /// - Unlocks any gated feature
        /// - Advances act if it's a capstone event
        /// - Boosts the delivering NPC's relationship by 10
        /// - Immediately checks for the next eligible event
        public void CompleteCurrentEvent(PlayerProgression progression, int currentLevel = 0)
        {
            if (PendingEvent is not { } evt) return;

            State.CompleteEvent(evt.Id);

            // Unlock feature
            if (evt.UnlocksFeature.HasValue)
                progression.UnlockedFeatures.Add(evt.UnlocksFeature.Value.RawValue());

            // Advance act
            if (evt.AdvancesToNextAct)
            {
                int nextAct = State.CurrentAct + 1;
                State.CurrentAct = nextAct;
                progression.AdvanceStoryAct(nextAct);
            }

            // NPC relationship boost
            State.IncreaseRelationship(evt.NpcId, 10);

            // Clear pending and re-evaluate
            PendingEvent = null;
            CheckTriggers(level: currentLevel);
        }
    }
}

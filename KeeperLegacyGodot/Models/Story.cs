// Models/Story.cs
// Port of Story.swift
// Pure C# — no Godot dependency.

using System;
using System.Collections.Generic;
using System.Linq;

namespace KeeperLegacy.Models
{
    // ── Story Acts ────────────────────────────────────────────────────────────

    public enum StoryAct
    {
        Act1 = 1,
        Act2 = 2,
        Act3 = 3
    }

    public static class StoryActExtensions
    {
        public static string Title(this StoryAct act) => act switch
        {
            StoryAct.Act1 => "Act I: Discovery",
            StoryAct.Act2 => "Act II: Restoration",
            StoryAct.Act3 => "Act III: Legacy",
            _             => "Unknown"
        };

        public static string Description(this StoryAct act) => act switch
        {
            StoryAct.Act1 => "You've inherited a mysterious shop from your absent uncle. A first egg appears...",
            StoryAct.Act2 => "The shop holds ancient magical secrets. You uncover a civilization's legacy.",
            StoryAct.Act3 => "The fate of magic itself is in your hands.",
            _             => ""
        };
    }

    // ── Story Events ──────────────────────────────────────────────────────────

    public class StoryEvent
    {
        public string Id { get; }
        public StoryAct Act { get; }
        public string NpcId { get; }
        public string Title { get; }
        public string Body { get; }
        public TriggerCondition Trigger { get; }
        public GameFeature? UnlocksFeature { get; }
        public bool AdvancesToNextAct { get; }

        public StoryEvent(string id, StoryAct act, string npcId, string title,
            string body, TriggerCondition trigger,
            GameFeature? unlocksFeature, bool advancesToNextAct)
        {
            Id                = id;
            Act               = act;
            NpcId             = npcId;
            Title             = title;
            Body              = body;
            Trigger           = trigger;
            UnlocksFeature    = unlocksFeature;
            AdvancesToNextAct = advancesToNextAct;
        }

        // ── Trigger Conditions ─────────────────────────────────────────────────

        public abstract class TriggerCondition { }

        public class ReachLevel : TriggerCondition
        {
            public int Level { get; }
            public ReachLevel(int level) => Level = level;
        }

        public class CompleteEvent : TriggerCondition
        {
            public string EventId { get; }
            public CompleteEvent(string eventId) => EventId = eventId;
        }

        public class FulfillOrders : TriggerCondition
        {
            public int Count { get; }
            public FulfillOrders(int count) => Count = count;
        }

        public class BreedCreatures : TriggerCondition
        {
            public int Count { get; }
            public BreedCreatures(int count) => Count = count;
        }

        public class Manual : TriggerCondition { }

        // ── All Events ────────────────────────────────────────────────────────

        public static readonly List<StoryEvent> AllEvents = new()
        {
            new StoryEvent(
                id:                "act1_first_egg",
                act:               StoryAct.Act1,
                npcId:             "elder_mira",
                title:             "A Mysterious Egg",
                body:              "While tidying up the shop, you discover a glowing egg tucked behind the counter. Elder Mira examines it with knowing eyes.\n\n\"Your uncle always said this day would come. Tend to it carefully — magic recognizes patience.\"",
                trigger:           new ReachLevel(1),
                unlocksFeature:    null,
                advancesToNextAct: false
            ),
            new StoryEvent(
                id:                "act1_shop_secret",
                act:               StoryAct.Act1,
                npcId:             "elder_mira",
                title:             "The Shop's Secret",
                body:              "Your creatures begin acting strangely around the old display case. Elder Mira arrives unexpectedly, her expression grave.\n\n\"The case is a resonance vault. Only a true Keeper can open it. I think that's you.\"",
                trigger:           new ReachLevel(10),
                unlocksFeature:    null,
                advancesToNextAct: false
            ),
            new StoryEvent(
                id:                "act1_magic_discovered",
                act:               StoryAct.Act1,
                npcId:             "elder_mira",
                title:             "Magic Revealed",
                body:              "The vault opens. Inside: a crystallized creature egg — the first of its kind in generations. Elder Mira's voice drops to a whisper.\n\n\"This isn't just a pet shop. These creatures carry ancient magic. And now, so do you. The lineage can continue.\"",
                trigger:           new CompleteEvent("act1_shop_secret"),
                unlocksFeature:    GameFeature.Breeding,
                advancesToNextAct: true
            ),
            new StoryEvent(
                id:                "act2_ancient_origins",
                act:               StoryAct.Act2,
                npcId:             "scholar_rex",
                title:             "Ancient Origins",
                body:              "Scholar Rex arrives breathless, clutching weathered manuscripts.\n\n\"I've translated the vault inscriptions. This shop was a sanctuary — built by the Aelurin civilization to preserve elemental creatures when the old magic began to fade. Your uncle was the last guardian.\"",
                trigger:           new ReachLevel(20),
                unlocksFeature:    null,
                advancesToNextAct: false
            ),
            new StoryEvent(
                id:                "act2_revelation",
                act:               StoryAct.Act2,
                npcId:             "elder_mira",
                title:             "The Restoration",
                body:              "The shop trembles. A hidden door reveals itself behind the water habitat — a shimmering threshold.\n\n\"The magical habitat,\" Elder Mira breathes. \"The Aelurin sealed it to protect the rarest creatures. Your bond with the others has restored it. The door is open again.\"",
                trigger:           new ReachLevel(25),
                unlocksFeature:    GameFeature.MagicalHabitat,
                advancesToNextAct: true
            ),
            new StoryEvent(
                id:                "act3_legacy",
                act:               StoryAct.Act3,
                npcId:             "elder_mira",
                title:             "Keeper's Legacy",
                body:              "Magic spreads beyond the shop walls. Creatures from every element gather at your door. Elder Mira stands beside you one last time.\n\n\"The Aelurin spent generations searching for a Keeper worthy of this. You must now decide: guard the magic, share it with the world, or let it flow freely. The legacy is yours to define.\"",
                trigger:           new ReachLevel(50),
                unlocksFeature:    null,
                advancesToNextAct: false
            ),
        };
    }

    // ── NPC System ────────────────────────────────────────────────────────────

    public class NPC
    {
        public string Id { get; }
        public string Name { get; }
        public Archetype NPCArchetype { get; }
        public string PortraitAsset { get; }
        public int RelationshipLevel { get; set; }   // 0–100

        public enum Archetype
        {
            Mentor,
            BusinessPartner,
            Collector,
            Scholar,
            Rival
        }

        public NPC(string id, string name, Archetype archetype,
                   string portraitAsset, int relationshipLevel)
        {
            Id                = id;
            Name              = name;
            NPCArchetype      = archetype;
            PortraitAsset     = portraitAsset;
            RelationshipLevel = relationshipLevel;
        }

        public static readonly List<NPC> MainCast = new()
        {
            new NPC("elder_mira",    "Elder Mira",  Archetype.Mentor,          "npc_mira", 50),
            new NPC("trader_fenn",   "Trader Fenn", Archetype.BusinessPartner, "npc_fenn", 30),
            new NPC("collector_ivy", "Ivy",         Archetype.Collector,       "npc_ivy",  20),
            new NPC("scholar_rex",   "Scholar Rex", Archetype.Scholar,         "npc_rex",  10),
            new NPC("rival_cass",    "Cass",        Archetype.Rival,           "npc_cass",  0),
        };
    }

    // ── Player Story State ────────────────────────────────────────────────────

    public class PlayerStoryState
    {
        public int CurrentAct { get; set; }                             // 1, 2, or 3
        public HashSet<string> CompletedEventIds { get; set; }
        public Dictionary<string, int> NpcRelationshipLevels { get; set; }  // npc id → 0–100

        public PlayerStoryState()
        {
            CurrentAct         = 1;
            CompletedEventIds  = new HashSet<string>();
            NpcRelationshipLevels = new Dictionary<string, int>(
                NPC.MainCast.Select(n => new KeyValuePair<string, int>(n.Id, n.RelationshipLevel))
            );
        }

        // For deserialization
        public PlayerStoryState(int currentAct, HashSet<string> completedEventIds,
            Dictionary<string, int> npcRelationshipLevels)
        {
            CurrentAct            = currentAct;
            CompletedEventIds     = completedEventIds;
            NpcRelationshipLevels = npcRelationshipLevels;
        }

        public bool HasCompletedEvent(string eventId) =>
            CompletedEventIds.Contains(eventId);

        public void CompleteEvent(string eventId) =>
            CompletedEventIds.Add(eventId);

        public void IncreaseRelationship(string npcId, int amount)
        {
            int current = NpcRelationshipLevels.GetValueOrDefault(npcId, 0);
            NpcRelationshipLevels[npcId] = Math.Min(100, current + amount);
        }
    }
}

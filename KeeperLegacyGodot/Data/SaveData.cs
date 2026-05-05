// Data/SaveData.cs
// Root serializable save state — replaces Core Data + CloudKit.
// All field names use stable string IDs (never integer indices) for
// forward-compatibility across game versions.
//
// File location (PC):  %APPDATA%\Godot\app_userdata\KeeperLegacy\save.json
// File location (iOS): NSDocumentDirectory\save.json  (redirected via #if IOS)
//
// Steam Cloud Save:    Steam automatically syncs OS.GetUserDataDir() when
//                      configured in the Steamworks partner dashboard.
//
// Pure C# — no Godot dependency. Serialized with System.Text.Json.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KeeperLegacy.Data
{
    // ── Root Save Container ───────────────────────────────────────────────────

    public class SaveData
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("lastSaved")]
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("economy")]
        public EconomySave Economy { get; set; } = new();

        [JsonPropertyName("progression")]
        public ProgressionSave Progression { get; set; } = new();

        [JsonPropertyName("story")]
        public StorySave Story { get; set; } = new();

        [JsonPropertyName("ownedCreatures")]
        public List<CreatureSave> OwnedCreatures { get; set; } = new();

        [JsonPropertyName("habitats")]
        public List<HabitatSave> Habitats { get; set; } = new();

        [JsonPropertyName("activeOrders")]
        public List<CustomerOrderSave> ActiveOrders { get; set; } = new();

        [JsonPropertyName("discoveredCatalogIds")]
        public HashSet<string> DiscoveredCatalogIds { get; set; } = new();

        [JsonPropertyName("breedingRecords")]
        public List<BreedingRecordSave> BreedingRecords { get; set; } = new();
    }

    // ── Economy ───────────────────────────────────────────────────────────────

    public class EconomySave
    {
        [JsonPropertyName("coins")]           public int Coins { get; set; } = 500;
        [JsonPropertyName("stardust")]        public int Stardust { get; set; } = 0;
        [JsonPropertyName("totalEarned")]     public int TotalCoinsEarned { get; set; } = 500;
        [JsonPropertyName("totalSpent")]      public int TotalCoinsSpent { get; set; } = 0;
        [JsonPropertyName("totalStardust")]   public int TotalStardustSpent { get; set; } = 0;
    }

    // ── Progression ───────────────────────────────────────────────────────────

    public class ProgressionSave
    {
        [JsonPropertyName("level")]               public int CurrentLevel { get; set; } = 1;
        [JsonPropertyName("xp")]                  public int CurrentXP { get; set; } = 0;
        [JsonPropertyName("storyAct")]            public int StoryAct { get; set; } = 1;
        [JsonPropertyName("unlockedFeatures")]    public List<string> UnlockedFeatures { get; set; } = new();
        [JsonPropertyName("claimedMilestones")]   public List<int> ClaimedMilestones { get; set; } = new();
    }

    // ── Story ─────────────────────────────────────────────────────────────────

    public class StorySave
    {
        [JsonPropertyName("currentAct")]          public int CurrentAct { get; set; } = 1;
        [JsonPropertyName("completedEvents")]     public List<string> CompletedEventIds { get; set; } = new();
        [JsonPropertyName("npcRelationships")]    public Dictionary<string, int> NpcRelationshipLevels { get; set; } = new();
    }

    // ── Creature Instance ─────────────────────────────────────────────────────

    public class CreatureSave
    {
        [JsonPropertyName("id")]              public string Id { get; set; } = "";
        [JsonPropertyName("catalogId")]       public string CatalogId { get; set; } = "";
        [JsonPropertyName("mutation")]        public int MutationIndex { get; set; } = 0;
        [JsonPropertyName("hunger")]          public double Hunger { get; set; } = 0.8;
        [JsonPropertyName("happiness")]       public double Happiness { get; set; } = 0.7;
        [JsonPropertyName("cleanliness")]     public double Cleanliness { get; set; } = 0.9;
        [JsonPropertyName("affection")]       public double Affection { get; set; } = 0.5;
        [JsonPropertyName("playfulness")]     public double Playfulness { get; set; } = 0.8;
        [JsonPropertyName("lifecycle")]       public string Lifecycle { get; set; } = "Adult";
        [JsonPropertyName("lifecycleStart")]  public DateTime LifecycleStartDate { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("foundFavToy")]     public bool DiscoveredFavoriteToy { get; set; } = false;
        [JsonPropertyName("parentIds")]       public List<string> ParentIds { get; set; } = new();
        [JsonPropertyName("dateAcquired")]    public DateTime DateAcquired { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("nickname")]        public string? Nickname { get; set; } = null;
    }

    // ── Habitat ───────────────────────────────────────────────────────────────

    public class HabitatSave
    {
        [JsonPropertyName("id")]              public string Id { get; set; } = "";
        [JsonPropertyName("type")]            public string Type { get; set; } = "Water";

        /// Legacy single-occupant field. Older saves wrote this; newer saves
        /// write OccupantIds instead. SaveManager backfills OccupantIds from
        /// this when present.
        [JsonPropertyName("occupantId")]      public string? OccupantId { get; set; } = null;

        /// Multi-occupant list (current format).
        [JsonPropertyName("occupantIds")]     public List<string> OccupantIds { get; set; } = new();

        [JsonPropertyName("decorations")]     public List<string> DecorationIds { get; set; } = new();
        [JsonPropertyName("unlockedAtLevel")] public int UnlockedAtLevel { get; set; } = 1;
    }

    // ── Customer Order ────────────────────────────────────────────────────────

    public class CustomerOrderSave
    {
        [JsonPropertyName("id")]          public string Id { get; set; } = "";
        [JsonPropertyName("catalogId")]   public string RequiredCreatureCatalogId { get; set; } = "";
        [JsonPropertyName("rarity")]      public string RequiredRarity { get; set; } = "Common";
        [JsonPropertyName("minHappy")]    public double MinHappiness { get; set; } = 0.5;
        [JsonPropertyName("reward")]      public int CoinReward { get; set; } = 100;
        [JsonPropertyName("expiresAt")]   public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("fulfilled")]   public bool IsFulfilled { get; set; } = false;
    }

    // ── Breeding Record ───────────────────────────────────────────────────────

    public class BreedingRecordSave
    {
        [JsonPropertyName("parentAId")]   public string ParentAId { get; set; } = "";
        [JsonPropertyName("parentBId")]   public string ParentBId { get; set; } = "";
        [JsonPropertyName("offspringId")] public string OffspringId { get; set; } = "";
        [JsonPropertyName("date")]        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}

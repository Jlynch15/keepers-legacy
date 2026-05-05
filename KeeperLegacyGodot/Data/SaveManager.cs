// Data/SaveManager.cs
// Reads and writes SaveData to a JSON file in the user's data directory.
//
// PC path:  %APPDATA%\Godot\app_userdata\KeeperLegacy\save.json
// Keeps a rotating backup (save.backup.json) to guard against corruption.
//
// This file has a thin Godot dependency (OS.GetUserDataDir()) which is
// wrapped in a seam (ISavePathProvider) so pure-C# tests can inject a
// temp directory path instead.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeeperLegacy.Models;

namespace KeeperLegacy.Data
{
    // ── Path Provider Seam (for testability) ─────────────────────────────────

    public interface ISavePathProvider
    {
        string UserDataDir();
    }

    public class DefaultSavePathProvider : ISavePathProvider
    {
        // In Godot this would call OS.GetUserDataDir().
        // Kept as a virtual method so the Godot node subclass can override it.
        public virtual string UserDataDir() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Godot", "app_userdata", "KeeperLegacy"
            );
    }

    // ── Save Manager ──────────────────────────────────────────────────────────

    public class SaveManager
    {
        private readonly ISavePathProvider _pathProvider;
        private readonly JsonSerializerOptions _jsonOptions;

        private string SavePath     => Path.Combine(_pathProvider.UserDataDir(), "save.json");
        private string BackupPath   => Path.Combine(_pathProvider.UserDataDir(), "save.backup.json");

        public SaveManager(ISavePathProvider? pathProvider = null)
        {
            _pathProvider = pathProvider ?? new DefaultSavePathProvider();
            _jsonOptions  = new JsonSerializerOptions
            {
                WriteIndented          = true,
                PropertyNameCaseInsensitive = true,
            };
        }

        // ── Load ──────────────────────────────────────────────────────────────

        /// Load and return the current save, or a fresh save if none exists.
        public SaveData Load()
        {
            if (!File.Exists(SavePath))
                return CreateNewGame();

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);
                if (data == null) throw new InvalidDataException("Deserialized null save data.");
                return data;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveManager] Failed to load save.json: {ex.Message}");
                return TryLoadBackup() ?? CreateNewGame();
            }
        }

        private SaveData? TryLoadBackup()
        {
            if (!File.Exists(BackupPath)) return null;
            try
            {
                string json = File.ReadAllText(BackupPath);
                var data = JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);
                Console.WriteLine("[SaveManager] Loaded from backup.");
                return data;
            }
            catch
            {
                return null;
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────

        /// Persist the current save state to disk.
        public void Save(SaveData data)
        {
            try
            {
                string dir = _pathProvider.UserDataDir();
                Directory.CreateDirectory(dir);

                data.LastSaved = DateTime.UtcNow;
                string json = JsonSerializer.Serialize(data, _jsonOptions);

                // Rotate existing save to backup before overwriting
                if (File.Exists(SavePath))
                    File.Copy(SavePath, BackupPath, overwrite: true);

                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveManager] Failed to write save: {ex.Message}");
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────

        /// Wipe all save data and return a fresh state (used by Settings → Clear Data).
        public SaveData DeleteAndReset()
        {
            try
            {
                if (File.Exists(SavePath))   File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SaveManager] Failed to delete saves: {ex.Message}");
            }
            return CreateNewGame();
        }

        // ── Conversion: SaveData ↔ Domain Models ──────────────────────────────

        /// Inflate a SaveData into live domain model objects.
        public (PlayerEconomy economy,
                PlayerProgression progression,
                PlayerStoryState story,
                List<CreatureInstance> creatures,
                List<Habitat> habitats,
                List<PricingTable.CustomerOrder> orders,
                HashSet<string> discoveredIds)
            Inflate(SaveData save)
        {
            // Economy
            var economy = new PlayerEconomy(save.Economy.Coins, save.Economy.Stardust)
            {
                TotalCoinsEarned   = save.Economy.TotalCoinsEarned,
                TotalCoinsSpent    = save.Economy.TotalCoinsSpent,
                TotalStardustSpent = save.Economy.TotalStardustSpent,
            };

            // Progression
            var prog = new PlayerProgression(
                currentLevel:      save.Progression.CurrentLevel,
                currentXP:         save.Progression.CurrentXP,
                unlockedFeatures:  new HashSet<string>(save.Progression.UnlockedFeatures),
                claimedMilestones: new HashSet<int>(save.Progression.ClaimedMilestones),
                storyAct:          save.Progression.StoryAct
            );

            // Story
            var story = new PlayerStoryState(
                currentAct:            save.Story.CurrentAct,
                completedEventIds:     new HashSet<string>(save.Story.CompletedEventIds),
                npcRelationshipLevels: new Dictionary<string, int>(save.Story.NpcRelationshipLevels)
            );

            // Creatures
            var creatures = new List<CreatureInstance>();
            foreach (var c in save.OwnedCreatures)
            {
                if (!Guid.TryParse(c.Id, out var guid)) continue;
                var lifecycle = Enum.TryParse<LifecycleStage>(c.Lifecycle, out var lc)
                    ? lc : LifecycleStage.Adult;
                var parentIds = new List<Guid>();
                foreach (var pid in c.ParentIds)
                    if (Guid.TryParse(pid, out var pg)) parentIds.Add(pg);

                creatures.Add(new CreatureInstance(
                    guid, c.CatalogId, c.MutationIndex,
                    c.Hunger, c.Happiness, c.Cleanliness,
                    c.Affection, c.Playfulness,
                    lifecycle, c.LifecycleStartDate,
                    c.DiscoveredFavoriteToy, parentIds,
                    c.DateAcquired, c.Nickname
                ));
            }

            // Habitats
            var habitats = new List<Habitat>();
            foreach (var h in save.Habitats)
            {
                if (!Guid.TryParse(h.Id, out var guid)) continue;
                var type = HabitatTypeExtensions.FromRawValue(h.Type) ?? HabitatType.Water;

                // Hydrate occupant list. Prefer the new OccupantIds; fall back
                // to the legacy single OccupantId field for older saves.
                var occupantIds = new List<Guid>();
                foreach (var oidStr in h.OccupantIds)
                    if (Guid.TryParse(oidStr, out var og)) occupantIds.Add(og);
                if (occupantIds.Count == 0 && h.OccupantId != null
                    && Guid.TryParse(h.OccupantId, out var legacyOg))
                {
                    occupantIds.Add(legacyOg);
                }

                habitats.Add(new Habitat(guid, type, occupantIds,
                    new List<string>(h.DecorationIds), h.UnlockedAtLevel));
            }

            // Orders
            var orders = new List<PricingTable.CustomerOrder>();
            foreach (var o in save.ActiveOrders)
            {
                if (!Guid.TryParse(o.Id, out var guid)) continue;
                var rarity = RarityExtensions.FromRawValue(o.RequiredRarity) ?? Rarity.Common;
                orders.Add(new PricingTable.CustomerOrder(
                    guid, o.RequiredCreatureCatalogId, rarity,
                    o.MinHappiness, o.CoinReward, o.ExpiresAt, o.IsFulfilled
                ));
            }

            return (economy, prog, story, creatures, habitats, orders,
                    new HashSet<string>(save.DiscoveredCatalogIds));
        }

        /// Flatten live domain objects into a SaveData for serialization.
        public SaveData Flatten(
            PlayerEconomy economy,
            PlayerProgression progression,
            PlayerStoryState story,
            IEnumerable<CreatureInstance> creatures,
            IEnumerable<Habitat> habitats,
            IEnumerable<PricingTable.CustomerOrder> orders,
            HashSet<string> discoveredIds,
            IEnumerable<BreedingRecordSave> breedingRecords,
            SaveData? existing = null)
        {
            var save = new SaveData
            {
                Version          = existing?.Version ?? 1,
                BreedingRecords  = existing != null
                    ? new List<BreedingRecordSave>(existing.BreedingRecords)
                    : new List<BreedingRecordSave>(),
            };

            save.Economy = new EconomySave
            {
                Coins              = economy.Coins,
                Stardust           = economy.Stardust,
                TotalCoinsEarned   = economy.TotalCoinsEarned,
                TotalCoinsSpent    = economy.TotalCoinsSpent,
                TotalStardustSpent = economy.TotalStardustSpent,
            };

            save.Progression = new ProgressionSave
            {
                CurrentLevel      = progression.CurrentLevel,
                CurrentXP         = progression.CurrentXP,
                StoryAct          = progression.StoryAct,
                UnlockedFeatures  = new List<string>(progression.UnlockedFeatures),
                ClaimedMilestones = new List<int>(progression.ClaimedMilestones),
            };

            save.Story = new StorySave
            {
                CurrentAct            = story.CurrentAct,
                CompletedEventIds     = new List<string>(story.CompletedEventIds),
                NpcRelationshipLevels = new Dictionary<string, int>(story.NpcRelationshipLevels),
            };

            foreach (var c in creatures)
            {
                var cs = new CreatureSave
                {
                    Id                    = c.Id.ToString(),
                    CatalogId             = c.CatalogId,
                    MutationIndex         = c.MutationIndex,
                    Hunger                = c.Hunger,
                    Happiness             = c.Happiness,
                    Cleanliness           = c.Cleanliness,
                    Affection             = c.Affection,
                    Playfulness           = c.Playfulness,
                    Lifecycle             = c.Lifecycle.ToString(),
                    LifecycleStartDate    = c.LifecycleStartDate,
                    DiscoveredFavoriteToy = c.DiscoveredFavoriteToy,
                    DateAcquired          = c.DateAcquired,
                    Nickname              = c.Nickname,
                };
                foreach (var pid in c.ParentIds) cs.ParentIds.Add(pid.ToString());
                save.OwnedCreatures.Add(cs);
            }

            foreach (var h in habitats)
            {
                save.Habitats.Add(new HabitatSave
                {
                    Id             = h.Id.ToString(),
                    Type           = h.Type.RawValue(),
                    OccupantId     = null,  // Legacy field -- no longer written.
                    OccupantIds    = h.OccupantIds.ConvertAll(g => g.ToString()),
                    DecorationIds  = new List<string>(h.DecorationIds),
                    UnlockedAtLevel = h.UnlockedAtLevel,
                });
            }

            foreach (var o in orders)
            {
                save.ActiveOrders.Add(new CustomerOrderSave
                {
                    Id                       = o.Id.ToString(),
                    RequiredCreatureCatalogId = o.RequiredCreatureCatalogId,
                    RequiredRarity           = o.RequiredRarity.RawValue(),
                    MinHappiness             = o.MinHappiness,
                    CoinReward               = o.CoinReward,
                    ExpiresAt                = o.ExpiresAt,
                    IsFulfilled              = o.IsFulfilled,
                });
            }

            save.DiscoveredCatalogIds = new HashSet<string>(discoveredIds);
            if (breedingRecords != null)
                save.BreedingRecords = new List<BreedingRecordSave>(breedingRecords);

            return save;
        }

        // ── Fresh Game ────────────────────────────────────────────────────────

        private SaveData CreateNewGame()
        {
            var save = new SaveData();

            // Starting economy
            save.Economy.Coins          = 500;
            save.Economy.TotalCoinsEarned = 500;

            // Starting progression — unlock level-0 features
            var startProg = new PlayerProgression();
            save.Progression.UnlockedFeatures = new List<string>(startProg.UnlockedFeatures);

            // Starting story — NPC default relationship levels
            var startStory = new PlayerStoryState();
            foreach (var kv in startStory.NpcRelationshipLevels)
                save.Story.NpcRelationshipLevels[kv.Key] = kv.Value;

            // Starting habitat (Water, unlocked at level 1)
            save.Habitats.Add(new HabitatSave
            {
                Id              = Guid.NewGuid().ToString(),
                Type            = "Water",
                OccupantIds     = new List<string>(),
                OccupantId      = null,
                DecorationIds   = new List<string>(),
                UnlockedAtLevel = 1,
            });

            return save;
        }
    }
}

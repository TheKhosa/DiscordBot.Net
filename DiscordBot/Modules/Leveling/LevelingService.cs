using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Leveling
{
    /// <summary>
    /// Service for managing user levels and XP
    /// </summary>
    public class LevelingService
    {
        private readonly ConcurrentDictionary<string, UserLevel> _userLevels = new();
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        // XP settings
        public int MinXpPerMessage { get; set; } = 15;
        public int MaxXpPerMessage { get; set; } = 25;
        public int XpCooldownSeconds { get; set; } = 60;
        public int XpPerVoiceMinute { get; set; } = 10;

        public LevelingService(string? dataDirectory = null)
        {
            _dataDirectory = dataDirectory ?? Path.Combine(AppContext.BaseDirectory, "LevelingData");
            Directory.CreateDirectory(_dataDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get user level data
        /// </summary>
        public UserLevel GetUserLevel(ulong guildId, ulong userId)
        {
            var key = $"{guildId}_{userId}";
            return _userLevels.GetOrAdd(key, _ => LoadUserLevel(guildId, userId) ?? new UserLevel
            {
                UserId = userId,
                GuildId = guildId,
                Experience = 0,
                Level = 0,
                LastMessageTime = DateTime.MinValue,
                MessageCount = 0,
                VoiceMinutes = 0
            });
        }

        /// <summary>
        /// Add XP to a user (returns true if leveled up)
        /// </summary>
        public async Task<bool> AddExperienceAsync(ulong guildId, ulong userId, int amount)
        {
            var user = GetUserLevel(guildId, userId);
            user.Experience += amount;

            // Check for level up
            var newLevel = CalculateLevel(user.Experience);
            bool leveledUp = newLevel > user.Level;
            user.Level = newLevel;

            await SaveUserLevelAsync(user);
            return leveledUp;
        }

        /// <summary>
        /// Award XP for sending a message (with cooldown)
        /// </summary>
        public async Task<(bool awarded, bool leveledUp, int xpGained)> AwardMessageXpAsync(ulong guildId, ulong userId)
        {
            var user = GetUserLevel(guildId, userId);

            // Check cooldown
            if ((DateTime.UtcNow - user.LastMessageTime).TotalSeconds < XpCooldownSeconds)
            {
                return (false, false, 0);
            }

            // Generate random XP
            var random = new Random();
            var xp = random.Next(MinXpPerMessage, MaxXpPerMessage + 1);

            user.LastMessageTime = DateTime.UtcNow;
            user.MessageCount++;

            var leveledUp = await AddExperienceAsync(guildId, userId, xp);
            return (true, leveledUp, xp);
        }

        /// <summary>
        /// Award XP for voice chat time
        /// </summary>
        public async Task<bool> AwardVoiceXpAsync(ulong guildId, ulong userId, int minutes)
        {
            var user = GetUserLevel(guildId, userId);
            user.VoiceMinutes += minutes;

            var xp = minutes * XpPerVoiceMinute;
            return await AddExperienceAsync(guildId, userId, xp);
        }

        /// <summary>
        /// Get leaderboard for a guild
        /// </summary>
        public List<UserLevel> GetLeaderboard(ulong guildId, int limit = 10)
        {
            return _userLevels.Values
                .Where(u => u.GuildId == guildId)
                .OrderByDescending(u => u.Experience)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Get user rank in guild
        /// </summary>
        public int GetUserRank(ulong guildId, ulong userId)
        {
            var allUsers = _userLevels.Values
                .Where(u => u.GuildId == guildId)
                .OrderByDescending(u => u.Experience)
                .ToList();

            return allUsers.FindIndex(u => u.UserId == userId) + 1;
        }

        /// <summary>
        /// Calculate level from XP
        /// </summary>
        private int CalculateLevel(long xp)
        {
            int level = 0;
            while (UserLevel.XpForLevel(level + 1) <= xp)
            {
                level++;
            }
            return level;
        }

        /// <summary>
        /// Load user level from file
        /// </summary>
        private UserLevel? LoadUserLevel(ulong guildId, ulong userId)
        {
            try
            {
                var filePath = GetFilePath(guildId, userId);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<UserLevel>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user level {guildId}/{userId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save user level to file
        /// </summary>
        private async Task SaveUserLevelAsync(UserLevel userLevel)
        {
            try
            {
                var guildDir = Path.Combine(_dataDirectory, userLevel.GuildId.ToString());
                Directory.CreateDirectory(guildDir);

                var filePath = GetFilePath(userLevel.GuildId, userLevel.UserId);
                var json = JsonSerializer.Serialize(userLevel, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user level {userLevel.GuildId}/{userLevel.UserId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get file path for user level data
        /// </summary>
        private string GetFilePath(ulong guildId, ulong userId)
        {
            return Path.Combine(_dataDirectory, guildId.ToString(), $"{userId}.json");
        }

        /// <summary>
        /// Load all user levels for a guild
        /// </summary>
        public async Task LoadGuildDataAsync(ulong guildId)
        {
            try
            {
                var guildDir = Path.Combine(_dataDirectory, guildId.ToString());
                if (!Directory.Exists(guildDir))
                    return;

                var files = Directory.GetFiles(guildDir, "*.json");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (ulong.TryParse(fileName, out var userId))
                    {
                        var userLevel = LoadUserLevel(guildId, userId);
                        if (userLevel != null)
                        {
                            var key = $"{guildId}_{userId}";
                            _userLevels[key] = userLevel;
                        }
                    }
                }

                Console.WriteLine($"[Leveling] Loaded {files.Length} user(s) for guild {guildId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading guild leveling data: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}

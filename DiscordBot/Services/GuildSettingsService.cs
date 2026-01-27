using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    /// <summary>
    /// Manages per-guild settings with automatic persistence
    /// </summary>
    public class GuildSettingsService
    {
        private readonly ConcurrentDictionary<ulong, GuildSettings> _settings = new();
        private readonly string _settingsDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public GuildSettingsService(string? settingsDirectory = null)
        {
            _settingsDirectory = settingsDirectory ?? Path.Combine(AppContext.BaseDirectory, "GuildSettings");
            Directory.CreateDirectory(_settingsDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get settings for a guild (creates default if not exists)
        /// </summary>
        public GuildSettings GetSettings(ulong guildId)
        {
            return _settings.GetOrAdd(guildId, id =>
            {
                // Try to load from file
                var settings = LoadFromFile(id);
                if (settings == null)
                {
                    // Create new default settings
                    settings = new GuildSettings { GuildId = id };
                    _ = SaveToFileAsync(settings); // Save defaults
                }
                return settings;
            });
        }

        /// <summary>
        /// Update settings for a guild
        /// </summary>
        public async Task UpdateSettingsAsync(GuildSettings settings)
        {
            _settings[settings.GuildId] = settings;
            await SaveToFileAsync(settings);
        }

        /// <summary>
        /// Reset settings to default for a guild
        /// </summary>
        public async Task ResetSettingsAsync(ulong guildId)
        {
            var settings = new GuildSettings { GuildId = guildId };
            _settings[guildId] = settings;
            await SaveToFileAsync(settings);
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        private GuildSettings? LoadFromFile(ulong guildId)
        {
            try
            {
                var filePath = GetFilePath(guildId);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<GuildSettings>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings for guild {guildId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        private async Task SaveToFileAsync(GuildSettings settings)
        {
            try
            {
                var filePath = GetFilePath(settings.GuildId);
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings for guild {settings.GuildId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get file path for guild settings
        /// </summary>
        private string GetFilePath(ulong guildId)
        {
            return Path.Combine(_settingsDirectory, $"{guildId}.json");
        }

        /// <summary>
        /// Load all guild settings on startup
        /// </summary>
        public async Task LoadAllSettingsAsync()
        {
            try
            {
                var files = Directory.GetFiles(_settingsDirectory, "*.json");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (ulong.TryParse(fileName, out var guildId))
                    {
                        var settings = LoadFromFile(guildId);
                        if (settings != null)
                        {
                            _settings[guildId] = settings;
                        }
                    }
                }

                Console.WriteLine($"[GuildSettings] Loaded settings for {_settings.Count} guild(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading guild settings: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }
}

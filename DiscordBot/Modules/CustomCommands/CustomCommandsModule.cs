using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Modules.CustomCommands
{
    /// <summary>
    /// Module for custom commands and scheduled messages
    /// </summary>
    public class CustomCommandsModule : ModuleBase
    {
        public override string ModuleId => "customcommands";
        public override string Name => "Custom Commands & Scheduled Messages";
        public override string Description => "Create custom commands and schedule timed messages";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly ConcurrentDictionary<string, CustomCommand> _commands = new();
        private readonly ConcurrentDictionary<ulong, ScheduledMessage> _scheduledMessages = new();
        private readonly string _dataDirectory;
        private Timer? _scheduledMessageTimer;

        public CustomCommandsModule()
        {
            _dataDirectory = Path.Combine(AppContext.BaseDirectory, "CustomCommandsData");
            Directory.CreateDirectory(_dataDirectory);
        }

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadAllCommands();
            LoadScheduledMessages();

            // Start timer to check scheduled messages every minute
            _scheduledMessageTimer = new Timer(CheckScheduledMessages, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            return base.InitializeAsync(client, services);
        }

        public override Task ShutdownAsync()
        {
            _scheduledMessageTimer?.Dispose();
            return base.ShutdownAsync();
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            // Check for custom commands first
            if (content.StartsWith("!"))
            {
                var commandName = content.Split(' ')[0].Substring(1);
                var key = $"{guildId}_{commandName}";

                if (_commands.TryGetValue(key, out var customCommand))
                {
                    await ExecuteCustomCommand(message, customCommand);
                    return true;
                }
            }

            // Handle management commands
            if (content.StartsWith("!addcommand") || content.StartsWith("!addcmd"))
            {
                await HandleAddCommandCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!removecommand") || content.StartsWith("!delcmd"))
            {
                await HandleRemoveCommandCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!listcommands") || content.StartsWith("!commands"))
            {
                await HandleListCommandsCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!schedule"))
            {
                await HandleScheduleCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!timer"))
            {
                await HandleTimerCommand(message);
                return true;
            }

            if (content.StartsWith("!listscheduled"))
            {
                await HandleListScheduledCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!cancelscheduled"))
            {
                await HandleCancelScheduledCommand(message);
                return true;
            }

            return false;
        }

        private async Task ExecuteCustomCommand(SocketUserMessage message, CustomCommand command)
        {
            command.UseCount++;
            await SaveCommandAsync(command);

            if (command.IsEmbed)
            {
                var embed = new EmbedBuilder()
                    .WithDescription(command.Response)
                    .WithColor(Color.Blue)
                    .WithFooter($"Custom command • Used {command.UseCount} times")
                    .Build();

                await message.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                await message.Channel.SendMessageAsync(command.Response);
            }
        }

        private async Task HandleAddCommandCommand(SocketUserMessage message, ulong guildId)
        {
            var author = message.Author as SocketGuildUser;
            if (author == null || !author.GuildPermissions.ManageGuild)
            {
                await message.Channel.SendMessageAsync("❌ You need Manage Server permission to add commands.");
                return;
            }

            var parts = message.Content.Split(' ', 3, StringSplitOptions.TrimEntries);
            
            if (parts.Length < 3)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!addcommand <name> <response>`\n" +
                    "Example: `!addcommand rules Please read #rules before posting!`");
                return;
            }

            var commandName = parts[1].ToLower();
            var response = parts[2];

            var command = new CustomCommand
            {
                GuildId = guildId,
                Name = commandName,
                Response = response,
                CreatorId = message.Author.Id,
                CreatedAt = DateTime.UtcNow,
                IsEmbed = false,
                UseCount = 0
            };

            var key = $"{guildId}_{commandName}";
            _commands[key] = command;
            await SaveCommandAsync(command);

            await message.Channel.SendMessageAsync($"✅ Custom command `!{commandName}` created!");
        }

        private async Task HandleRemoveCommandCommand(SocketUserMessage message, ulong guildId)
        {
            var author = message.Author as SocketGuildUser;
            if (author == null || !author.GuildPermissions.ManageGuild)
            {
                await message.Channel.SendMessageAsync("❌ You need Manage Server permission to remove commands.");
                return;
            }

            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!removecommand <name>`");
                return;
            }

            var commandName = parts[1].ToLower();
            var key = $"{guildId}_{commandName}";

            if (_commands.TryRemove(key, out var command))
            {
                var filePath = Path.Combine(_dataDirectory, $"{guildId}_{commandName}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await message.Channel.SendMessageAsync($"✅ Custom command `!{commandName}` removed!");
            }
            else
            {
                await message.Channel.SendMessageAsync($"❌ Command `!{commandName}` not found!");
            }
        }

        private async Task HandleListCommandsCommand(SocketUserMessage message, ulong guildId)
        {
            var guildCommands = _commands.Values.Where(c => c.GuildId == guildId).OrderBy(c => c.Name).ToList();

            if (guildCommands.Count == 0)
            {
                await message.Channel.SendMessageAsync("No custom commands in this server yet!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("📝 Custom Commands")
                .WithColor(Color.Green)
                .WithDescription($"Total: {guildCommands.Count} commands");

            foreach (var command in guildCommands.Take(25)) // Discord embed limit
            {
                embed.AddField(
                    $"!{command.Name}",
                    $"Used {command.UseCount} times",
                    inline: true
                );
            }

            if (guildCommands.Count > 25)
            {
                embed.WithFooter($"Showing 25 of {guildCommands.Count} commands");
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleScheduleCommand(SocketUserMessage message, ulong guildId)
        {
            var author = message.Author as SocketGuildUser;
            if (author == null || !author.GuildPermissions.ManageGuild)
            {
                await message.Channel.SendMessageAsync("❌ You need Manage Server permission to schedule messages.");
                return;
            }

            var parts = message.Content.Split('|', StringSplitOptions.TrimEntries);
            
            if (parts.Length < 3)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!schedule <time> | <interval> | <message>`\n" +
                    "Time: `yyyy-MM-dd HH:mm` or `+30m` (30 minutes from now)\n" +
                    "Interval: `once`, `daily`, `weekly`, or `30m` (every 30 minutes)\n" +
                    "Example: `!schedule +30m | once | Server restart in 30 minutes!`\n" +
                    "Example: `!schedule 2026-01-28 14:00 | daily | Daily reminder!`");
                return;
            }

            var timeStr = parts[0].Replace("!schedule", "").Trim();
            var intervalStr = parts[1].Trim().ToLower();
            var messageText = parts[2].Trim();

            DateTime scheduledTime;
            
            // Parse time
            if (timeStr.StartsWith("+"))
            {
                var duration = ParseDuration(timeStr.Substring(1));
                if (duration == TimeSpan.Zero)
                {
                    await message.Channel.SendMessageAsync("❌ Invalid time format!");
                    return;
                }
                scheduledTime = DateTime.UtcNow + duration;
            }
            else if (!DateTime.TryParse(timeStr, out scheduledTime))
            {
                await message.Channel.SendMessageAsync("❌ Invalid time format!");
                return;
            }

            // Parse interval
            TimeSpan? repeatInterval = intervalStr switch
            {
                "once" => null,
                "daily" => TimeSpan.FromDays(1),
                "weekly" => TimeSpan.FromDays(7),
                _ => ParseDuration(intervalStr)
            };

            if (intervalStr != "once" && repeatInterval == TimeSpan.Zero)
            {
                await message.Channel.SendMessageAsync("❌ Invalid interval!");
                return;
            }

            var scheduledMessage = new ScheduledMessage
            {
                Id = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GuildId = guildId,
                ChannelId = message.Channel.Id,
                Message = messageText,
                ScheduledTime = scheduledTime,
                RepeatInterval = repeatInterval,
                CreatorId = message.Author.Id
            };

            _scheduledMessages[scheduledMessage.Id] = scheduledMessage;
            await SaveScheduledMessageAsync(scheduledMessage);

            var intervalText = repeatInterval.HasValue ? $"repeating every {FormatDuration(repeatInterval.Value)}" : "once";
            await message.Channel.SendMessageAsync(
                $"✅ Message scheduled for {scheduledTime:yyyy-MM-dd HH:mm} UTC ({intervalText})\n" +
                $"Schedule ID: `{scheduledMessage.Id}`");
        }

        private async Task HandleTimerCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.TrimEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!timer <duration>`\n" +
                    "Examples: `!timer 30s`, `!timer 5m`, `!timer 1h`");
                return;
            }

            var duration = ParseDuration(parts[1]);
            if (duration == TimeSpan.Zero || duration > TimeSpan.FromHours(24))
            {
                await message.Channel.SendMessageAsync("❌ Invalid duration! (Max 24 hours)");
                return;
            }

            await message.Channel.SendMessageAsync($"⏰ Timer set for {FormatDuration(duration)}. I'll ping you when it's done!");

            await Task.Delay(duration);

            await message.Channel.SendMessageAsync($"⏰ {message.Author.Mention} Your timer for {FormatDuration(duration)} is up!");
        }

        private async Task HandleListScheduledCommand(SocketUserMessage message, ulong guildId)
        {
            var guildScheduled = _scheduledMessages.Values.Where(s => s.GuildId == guildId).OrderBy(s => s.ScheduledTime).ToList();

            if (guildScheduled.Count == 0)
            {
                await message.Channel.SendMessageAsync("No scheduled messages in this server!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("📅 Scheduled Messages")
                .WithColor(Color.Blue);

            foreach (var scheduled in guildScheduled.Take(10))
            {
                var intervalText = scheduled.RepeatInterval.HasValue ? $" (repeats {FormatDuration(scheduled.RepeatInterval.Value)})" : "";
                embed.AddField(
                    $"ID: {scheduled.Id}",
                    $"Time: {scheduled.ScheduledTime:yyyy-MM-dd HH:mm} UTC{intervalText}\n" +
                    $"Message: {(scheduled.Message.Length > 50 ? scheduled.Message.Substring(0, 50) + "..." : scheduled.Message)}",
                    inline: false
                );
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleCancelScheduledCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || !ulong.TryParse(parts[1], out ulong id))
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!cancelscheduled <id>`");
                return;
            }

            if (_scheduledMessages.TryRemove(id, out var scheduled))
            {
                var filePath = Path.Combine(_dataDirectory, "scheduled", $"{id}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await message.Channel.SendMessageAsync($"✅ Scheduled message {id} cancelled!");
            }
            else
            {
                await message.Channel.SendMessageAsync($"❌ Scheduled message {id} not found!");
            }
        }

        private async void CheckScheduledMessages(object? state)
        {
            var now = DateTime.UtcNow;

            foreach (var scheduled in _scheduledMessages.Values.ToList())
            {
                if (scheduled.ScheduledTime <= now)
                {
                    try
                    {
                        var guild = Client?.GetGuild(scheduled.GuildId);
                        var channel = guild?.GetTextChannel(scheduled.ChannelId);

                        if (channel != null)
                        {
                            await channel.SendMessageAsync(scheduled.Message);

                            // Handle repeating messages
                            if (scheduled.RepeatInterval.HasValue)
                            {
                                scheduled.ScheduledTime = now + scheduled.RepeatInterval.Value;
                                await SaveScheduledMessageAsync(scheduled);
                            }
                            else
                            {
                                // Remove one-time messages
                                _scheduledMessages.TryRemove(scheduled.Id, out _);
                                var filePath = Path.Combine(_dataDirectory, "scheduled", $"{scheduled.Id}.json");
                                if (File.Exists(filePath))
                                {
                                    File.Delete(filePath);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending scheduled message: {ex.Message}");
                    }
                }
            }
        }

        private TimeSpan ParseDuration(string duration)
        {
            duration = duration.ToLower().Trim();
            
            if (duration.EndsWith("s"))
            {
                if (int.TryParse(duration.TrimEnd('s'), out int seconds))
                    return TimeSpan.FromSeconds(seconds);
            }
            else if (duration.EndsWith("m"))
            {
                if (int.TryParse(duration.TrimEnd('m'), out int minutes))
                    return TimeSpan.FromMinutes(minutes);
            }
            else if (duration.EndsWith("h"))
            {
                if (int.TryParse(duration.TrimEnd('h'), out int hours))
                    return TimeSpan.FromHours(hours);
            }
            else if (duration.EndsWith("d"))
            {
                if (int.TryParse(duration.TrimEnd('d'), out int days))
                    return TimeSpan.FromDays(days);
            }

            return TimeSpan.Zero;
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h";
            if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}m";
            return $"{(int)duration.TotalSeconds}s";
        }

        private void LoadAllCommands()
        {
            try
            {
                var files = Directory.GetFiles(_dataDirectory, "*.json");
                foreach (var file in files)
                {
                    if (file.Contains("scheduled")) continue;

                    var json = File.ReadAllText(file);
                    var command = JsonSerializer.Deserialize<CustomCommand>(json);
                    if (command != null)
                    {
                        var key = $"{command.GuildId}_{command.Name}";
                        _commands[key] = command;
                    }
                }

                Console.WriteLine($"[CustomCommands] Loaded {_commands.Count} command(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading custom commands: {ex.Message}");
            }
        }

        private void LoadScheduledMessages()
        {
            try
            {
                var scheduledDir = Path.Combine(_dataDirectory, "scheduled");
                Directory.CreateDirectory(scheduledDir);

                var files = Directory.GetFiles(scheduledDir, "*.json");
                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);
                    var scheduled = JsonSerializer.Deserialize<ScheduledMessage>(json);
                    if (scheduled != null)
                    {
                        _scheduledMessages[scheduled.Id] = scheduled;
                    }
                }

                Console.WriteLine($"[CustomCommands] Loaded {_scheduledMessages.Count} scheduled message(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading scheduled messages: {ex.Message}");
            }
        }

        private async Task SaveCommandAsync(CustomCommand command)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{command.GuildId}_{command.Name}.json");
                var json = JsonSerializer.Serialize(command, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving custom command: {ex.Message}");
            }
        }

        private async Task SaveScheduledMessageAsync(ScheduledMessage scheduled)
        {
            try
            {
                var scheduledDir = Path.Combine(_dataDirectory, "scheduled");
                Directory.CreateDirectory(scheduledDir);
                
                var filePath = Path.Combine(scheduledDir, $"{scheduled.Id}.json");
                var json = JsonSerializer.Serialize(scheduled, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving scheduled message: {ex.Message}");
            }
        }

        private class CustomCommand
        {
            public ulong GuildId { get; set; }
            public string Name { get; set; } = "";
            public string Response { get; set; } = "";
            public ulong CreatorId { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsEmbed { get; set; }
            public int UseCount { get; set; }
        }

        private class ScheduledMessage
        {
            public ulong Id { get; set; }
            public ulong GuildId { get; set; }
            public ulong ChannelId { get; set; }
            public string Message { get; set; } = "";
            public DateTime ScheduledTime { get; set; }
            public TimeSpan? RepeatInterval { get; set; }
            public ulong CreatorId { get; set; }
        }
    }
}

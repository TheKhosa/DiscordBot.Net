using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Moderation
{
    /// <summary>
    /// Moderation module for server management
    /// </summary>
    public class ModerationModule : ModuleBase
    {
        public override string ModuleId => "moderation";
        public override string Name => "Moderation";
        public override string Description => "Server moderation tools - kicks, bans, mutes, warnings";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly string _dataFolder = "ModerationData";
        private Dictionary<ulong, ModerationData> _guildData = new();

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadData();
            
            // Set up timer to check for expired mutes
            var timer = new System.Timers.Timer(60000); // Check every minute
            timer.Elapsed += async (sender, e) => await CheckExpiredMutes();
            timer.Start();
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            // Check if user has moderation permissions
            var user = message.Author as SocketGuildUser;
            if (user == null) return false;

            if (content.StartsWith("!warn"))
            {
                if (!user.GuildPermissions.KickMembers)
                {
                    await message.Channel.SendMessageAsync("❌ You need Kick Members permission to use this command!");
                    return true;
                }
                await HandleWarnCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!warnings"))
            {
                await HandleWarningsCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!clearwarns"))
            {
                if (!user.GuildPermissions.KickMembers)
                {
                    await message.Channel.SendMessageAsync("❌ You need Kick Members permission to use this command!");
                    return true;
                }
                await HandleClearWarningsCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!kick"))
            {
                if (!user.GuildPermissions.KickMembers)
                {
                    await message.Channel.SendMessageAsync("❌ You need Kick Members permission to use this command!");
                    return true;
                }
                await HandleKickCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!ban"))
            {
                if (!user.GuildPermissions.BanMembers)
                {
                    await message.Channel.SendMessageAsync("❌ You need Ban Members permission to use this command!");
                    return true;
                }
                await HandleBanCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!unban"))
            {
                if (!user.GuildPermissions.BanMembers)
                {
                    await message.Channel.SendMessageAsync("❌ You need Ban Members permission to use this command!");
                    return true;
                }
                await HandleUnbanCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!mute"))
            {
                if (!user.GuildPermissions.ManageRoles)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Roles permission to use this command!");
                    return true;
                }
                await HandleMuteCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!unmute"))
            {
                if (!user.GuildPermissions.ManageRoles)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Roles permission to use this command!");
                    return true;
                }
                await HandleUnmuteCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!purge") || content.StartsWith("!clear"))
            {
                if (!user.GuildPermissions.ManageMessages)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Messages permission to use this command!");
                    return true;
                }
                await HandlePurgeCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!modlogs"))
            {
                if (!user.GuildPermissions.ViewAuditLog)
                {
                    await message.Channel.SendMessageAsync("❌ You need View Audit Log permission to use this command!");
                    return true;
                }
                await HandleModLogsCommand(message, guildId);
                return true;
            }

            return false;
        }

        private async Task HandleWarnCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!warn @user [reason]`");
                return;
            }

            var target = message.MentionedUsers.First();
            var reason = parts.Length > 2 ? parts[2] : "No reason provided";

            var data = GetGuildData(guild.Id);
            
            var warning = new Warning
            {
                UserId = target.Id,
                ModeratorId = message.Author.Id,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            if (!data.Warnings.ContainsKey(target.Id))
                data.Warnings[target.Id] = new List<Warning>();

            data.Warnings[target.Id].Add(warning);

            LogAction(guild.Id, ModerationActionType.Warn, target, message.Author, reason);
            SaveData();

            var warnCount = data.Warnings[target.Id].Count;

            var embed = new EmbedBuilder()
                .WithTitle("⚠️ User Warned")
                .WithColor(Color.Orange)
                .AddField("User", target.Mention, inline: true)
                .AddField("Moderator", message.Author.Mention, inline: true)
                .AddField("Reason", reason, inline: false)
                .AddField("Total Warnings", warnCount.ToString(), inline: true)
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);

            // Try to DM the user
            try
            {
                await target.SendMessageAsync($"⚠️ You have been warned in **{guild.Name}**\n**Reason:** {reason}\n**Total Warnings:** {warnCount}");
            }
            catch { }
        }

        private async Task HandleWarningsCommand(SocketUserMessage message, ulong guildId)
        {
            var target = message.MentionedUsers.Count > 0 ? message.MentionedUsers.First() : message.Author;

            var data = GetGuildData(guildId);

            if (!data.Warnings.ContainsKey(target.Id) || data.Warnings[target.Id].Count == 0)
            {
                await message.Channel.SendMessageAsync($"{target.Mention} has no warnings.");
                return;
            }

            var warnings = data.Warnings[target.Id];

            var embed = new EmbedBuilder()
                .WithTitle($"⚠️ Warnings for {target.Username}")
                .WithColor(Color.Orange)
                .WithThumbnailUrl(target.GetAvatarUrl() ?? target.GetDefaultAvatarUrl());

            for (int i = 0; i < Math.Min(warnings.Count, 10); i++)
            {
                var warning = warnings[i];
                var mod = await Client!.GetUserAsync(warning.ModeratorId);
                var modName = mod?.Username ?? "Unknown";

                embed.AddField(
                    $"Warning #{i + 1} - {warning.Timestamp:yyyy-MM-dd HH:mm}",
                    $"**Moderator:** {modName}\n**Reason:** {warning.Reason}",
                    inline: false
                );
            }

            if (warnings.Count > 10)
            {
                embed.WithFooter($"Showing 10 of {warnings.Count} warnings");
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleClearWarningsCommand(SocketUserMessage message, ulong guildId)
        {
            if (message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!clearwarns @user`");
                return;
            }

            var target = message.MentionedUsers.First();
            var data = GetGuildData(guildId);

            if (data.Warnings.ContainsKey(target.Id))
            {
                var count = data.Warnings[target.Id].Count;
                data.Warnings.Remove(target.Id);
                SaveData();

                await message.Channel.SendMessageAsync($"✅ Cleared {count} warning(s) for {target.Mention}");
            }
            else
            {
                await message.Channel.SendMessageAsync($"{target.Mention} has no warnings to clear.");
            }
        }

        private async Task HandleKickCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!kick @user [reason]`");
                return;
            }

            var target = guild.GetUser(message.MentionedUsers.First().Id);
            if (target == null)
            {
                await message.Channel.SendMessageAsync("❌ User not found in this server!");
                return;
            }

            var reason = parts.Length > 2 ? parts[2] : "No reason provided";

            try
            {
                await target.SendMessageAsync($"⚠️ You have been kicked from **{guild.Name}**\n**Reason:** {reason}");
            }
            catch { }

            await target.KickAsync(reason);

            LogAction(guild.Id, ModerationActionType.Kick, target, message.Author, reason);
            SaveData();

            var embed = new EmbedBuilder()
                .WithTitle("👢 User Kicked")
                .WithColor(Color.Orange)
                .AddField("User", $"{target.Username}#{target.Discriminator}", inline: true)
                .AddField("Moderator", message.Author.Mention, inline: true)
                .AddField("Reason", reason, inline: false)
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleBanCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!ban @user [reason]`");
                return;
            }

            var target = guild.GetUser(message.MentionedUsers.First().Id);
            if (target == null)
            {
                await message.Channel.SendMessageAsync("❌ User not found in this server!");
                return;
            }

            var reason = parts.Length > 2 ? parts[2] : "No reason provided";

            try
            {
                await target.SendMessageAsync($"🔨 You have been banned from **{guild.Name}**\n**Reason:** {reason}");
            }
            catch { }

            await guild.AddBanAsync(target, 0, reason);

            LogAction(guild.Id, ModerationActionType.Ban, target, message.Author, reason);
            SaveData();

            var embed = new EmbedBuilder()
                .WithTitle("🔨 User Banned")
                .WithColor(Color.Red)
                .AddField("User", $"{target.Username}#{target.Discriminator}", inline: true)
                .AddField("Moderator", message.Author.Mention, inline: true)
                .AddField("Reason", reason, inline: false)
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleUnbanCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!unban <user-id> [reason]`");
                return;
            }

            if (!ulong.TryParse(parts[1], out ulong userId))
            {
                await message.Channel.SendMessageAsync("❌ Invalid user ID!");
                return;
            }

            var reason = parts.Length > 2 ? parts[2] : "No reason provided";

            try
            {
                await guild.RemoveBanAsync(userId);

                var user = await Client!.GetUserAsync(userId);
                var username = user?.Username ?? $"User {userId}";

                LogAction(guild.Id, ModerationActionType.Unban, userId, username, message.Author.Id, message.Author.Username, reason);
                SaveData();

                var embed = new EmbedBuilder()
                    .WithTitle("✅ User Unbanned")
                    .WithColor(Color.Green)
                    .AddField("User", username, inline: true)
                    .AddField("Moderator", message.Author.Mention, inline: true)
                    .AddField("Reason", reason, inline: false)
                    .WithCurrentTimestamp()
                    .Build();

                await message.Channel.SendMessageAsync(embed: embed);
            }
            catch
            {
                await message.Channel.SendMessageAsync("❌ Failed to unban user. Make sure they are banned and the ID is correct.");
            }
        }

        private async Task HandleMuteCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!mute @user [duration] [reason]`\nExample: `!mute @user 30m Spamming`");
                return;
            }

            var target = guild.GetUser(message.MentionedUsers.First().Id);
            if (target == null)
            {
                await message.Channel.SendMessageAsync("❌ User not found in this server!");
                return;
            }

            // Parse duration
            TimeSpan? duration = null;
            string reason = "No reason provided";
            
            if (parts.Length >= 3)
            {
                duration = ParseDuration(parts[2]);
                if (parts.Length >= 4)
                    reason = parts[3];
            }

            // Get or create Muted role
            var mutedRole = guild.Roles.FirstOrDefault(r => r.Name == "Muted");
            if (mutedRole == null)
            {
                var restRole = await guild.CreateRoleAsync("Muted", isMentionable: false);
                mutedRole = guild.GetRole(restRole.Id);
                
                // Configure role permissions for all channels
                foreach (var channel in guild.TextChannels)
                {
                    await channel.AddPermissionOverwriteAsync(mutedRole!, new OverwritePermissions(
                        sendMessages: PermValue.Deny,
                        addReactions: PermValue.Deny
                    ));
                }
            }

            await target.AddRoleAsync(mutedRole);

            var data = GetGuildData(guild.Id);
            var mute = new Mute
            {
                UserId = target.Id,
                ModeratorId = message.Author.Id,
                Reason = reason,
                StartTime = DateTime.UtcNow,
                EndTime = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null,
                MutedRoleId = mutedRole.Id
            };

            if (!data.ActiveMutes.ContainsKey(guild.Id))
                data.ActiveMutes[guild.Id] = new List<Mute>();

            data.ActiveMutes[guild.Id].Add(mute);

            var durationStr = duration.HasValue ? FormatDuration(duration.Value) : "Permanent";
            LogAction(guild.Id, ModerationActionType.Mute, target, message.Author, reason, durationStr);
            SaveData();

            var embed = new EmbedBuilder()
                .WithTitle("🔇 User Muted")
                .WithColor(Color.DarkGrey)
                .AddField("User", target.Mention, inline: true)
                .AddField("Moderator", message.Author.Mention, inline: true)
                .AddField("Duration", durationStr, inline: true)
                .AddField("Reason", reason, inline: false)
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);

            try
            {
                await target.SendMessageAsync($"🔇 You have been muted in **{guild.Name}**\n**Duration:** {durationStr}\n**Reason:** {reason}");
            }
            catch { }
        }

        private async Task HandleUnmuteCommand(SocketUserMessage message, SocketGuild guild)
        {
            if (message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!unmute @user`");
                return;
            }

            var target = guild.GetUser(message.MentionedUsers.First().Id);
            if (target == null)
            {
                await message.Channel.SendMessageAsync("❌ User not found in this server!");
                return;
            }

            var mutedRole = guild.Roles.FirstOrDefault(r => r.Name == "Muted");
            if (mutedRole == null || !target.Roles.Contains(mutedRole))
            {
                await message.Channel.SendMessageAsync("❌ User is not muted!");
                return;
            }

            await target.RemoveRoleAsync(mutedRole);

            var data = GetGuildData(guild.Id);
            if (data.ActiveMutes.ContainsKey(guild.Id))
            {
                data.ActiveMutes[guild.Id].RemoveAll(m => m.UserId == target.Id);
            }

            LogAction(guild.Id, ModerationActionType.Unmute, target, message.Author, "Manual unmute");
            SaveData();

            await message.Channel.SendMessageAsync($"✅ {target.Mention} has been unmuted.");
        }

        private async Task HandlePurgeCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2 || !int.TryParse(parts[1], out int count) || count <= 0 || count > 100)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!purge <amount>` (1-100)");
                return;
            }

            var channel = message.Channel as SocketTextChannel;
            if (channel == null) return;

            var messages = await channel.GetMessagesAsync(count + 1).FlattenAsync();
            var deletableMessages = messages.Where(m => (DateTimeOffset.UtcNow - m.Timestamp).TotalDays < 14);

            await channel.DeleteMessagesAsync(deletableMessages);

            LogAction(guild.Id, ModerationActionType.Purge, 0, $"Channel: {channel.Name}", message.Author.Id, message.Author.Username, $"Deleted {count} messages");
            SaveData();

            var confirmMsg = await message.Channel.SendMessageAsync($"✅ Deleted {count} messages.");
            await Task.Delay(3000);
            await confirmMsg.DeleteAsync();
        }

        private async Task HandleModLogsCommand(SocketUserMessage message, ulong guildId)
        {
            var data = GetGuildData(guildId);

            if (data.ActionHistory.Count == 0)
            {
                await message.Channel.SendMessageAsync("No moderation actions recorded.");
                return;
            }

            var recentActions = data.ActionHistory.OrderByDescending(a => a.Timestamp).Take(10).ToList();

            var embed = new EmbedBuilder()
                .WithTitle("📋 Recent Moderation Actions")
                .WithColor(Color.Blue);

            foreach (var action in recentActions)
            {
                var icon = action.Type switch
                {
                    ModerationActionType.Warn => "⚠️",
                    ModerationActionType.Kick => "👢",
                    ModerationActionType.Ban => "🔨",
                    ModerationActionType.Unban => "✅",
                    ModerationActionType.Mute => "🔇",
                    ModerationActionType.Unmute => "🔊",
                    ModerationActionType.Purge => "🗑️",
                    _ => "•"
                };

                var durationText = !string.IsNullOrEmpty(action.Duration) ? $" ({action.Duration})" : "";

                embed.AddField(
                    $"{icon} {action.Type} - {action.Timestamp:yyyy-MM-dd HH:mm}",
                    $"**User:** {action.Username}\n**Moderator:** {action.ModeratorName}\n**Reason:** {action.Reason}{durationText}",
                    inline: false
                );
            }

            if (data.ActionHistory.Count > 10)
            {
                embed.WithFooter($"Showing 10 of {data.ActionHistory.Count} actions");
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task CheckExpiredMutes()
        {
            if (Client == null) return;

            foreach (var guildData in _guildData)
            {
                var guildId = guildData.Key;
                var data = guildData.Value;

                if (!data.ActiveMutes.ContainsKey(guildId)) continue;

                var expiredMutes = data.ActiveMutes[guildId]
                    .Where(m => m.EndTime.HasValue && DateTime.UtcNow >= m.EndTime.Value)
                    .ToList();

                foreach (var mute in expiredMutes)
                {
                    var guild = Client.GetGuild(guildId);
                    if (guild == null) continue;

                    var user = guild.GetUser(mute.UserId);
                    if (user == null) continue;

                    var mutedRole = guild.GetRole(mute.MutedRoleId ?? 0);
                    if (mutedRole != null && user.Roles.Contains(mutedRole))
                    {
                        await user.RemoveRoleAsync(mutedRole);
                        data.ActiveMutes[guildId].Remove(mute);
                        
                        LogAction(guildId, ModerationActionType.Unmute, user.Id, user.Username, 0, "System", "Mute expired");
                    }
                }
            }

            SaveData();
        }

        private ModerationData GetGuildData(ulong guildId)
        {
            if (!_guildData.ContainsKey(guildId))
                _guildData[guildId] = new ModerationData();

            return _guildData[guildId];
        }

        private void LogAction(ulong guildId, ModerationActionType type, IUser target, IUser moderator, string reason, string? duration = null)
        {
            LogAction(guildId, type, target.Id, target.Username, moderator.Id, moderator.Username, reason, duration);
        }

        private void LogAction(ulong guildId, ModerationActionType type, ulong userId, string username, ulong modId, string modName, string reason, string? duration = null)
        {
            var data = GetGuildData(guildId);

            data.ActionHistory.Add(new ModerationAction
            {
                Type = type,
                UserId = userId,
                Username = username,
                ModeratorId = modId,
                ModeratorName = modName,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                Duration = duration
            });

            // Keep only last 1000 actions
            if (data.ActionHistory.Count > 1000)
            {
                data.ActionHistory = data.ActionHistory.OrderByDescending(a => a.Timestamp).Take(1000).ToList();
            }
        }

        private TimeSpan? ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return null;

            var number = new string(duration.TakeWhile(char.IsDigit).ToArray());
            var unit = new string(duration.SkipWhile(char.IsDigit).ToArray()).ToLower();

            if (!int.TryParse(number, out int value)) return null;

            return unit switch
            {
                "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(value),
                "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(value),
                "h" or "hr" or "hour" or "hours" => TimeSpan.FromHours(value),
                "d" or "day" or "days" => TimeSpan.FromDays(value),
                _ => null
            };
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration.Hours}h";
            else if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            else if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes}m";
            else
                return $"{(int)duration.TotalSeconds}s";
        }

        private void LoadData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var file in Directory.GetFiles(_dataFolder, "*.json"))
            {
                var guildIdStr = Path.GetFileNameWithoutExtension(file);
                if (ulong.TryParse(guildIdStr, out ulong guildId))
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<ModerationData>(json);
                    if (data != null)
                        _guildData[guildId] = data;
                }
            }

            Console.WriteLine($"[Moderation] Loaded data for {_guildData.Count} guild(s)");
        }

        private void SaveData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var kvp in _guildData)
            {
                var filePath = Path.Combine(_dataFolder, $"{kvp.Key}.json");
                var json = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }
    }
}

using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Logging
{
    /// <summary>
    /// Comprehensive server logging module
    /// </summary>
    public class LoggingModule : ModuleBase
    {
        public override string ModuleId => "logging";
        public override string Name => "Server Logging";
        public override string Description => "Comprehensive audit logs for all server events";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly string _dataFolder = "LoggingData";
        private Dictionary<ulong, LoggingConfig> _guildConfigs = new();

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadData();
            
            // Subscribe to all logging events
            client.MessageDeleted += LogMessageDeleted;
            client.MessageUpdated += LogMessageUpdated;
            client.UserJoined += LogUserJoined;
            client.UserLeft += LogUserLeft;
            client.GuildMemberUpdated += LogGuildMemberUpdated;
            client.UserBanned += LogUserBanned;
            client.UserUnbanned += LogUserUnbanned;
            client.ChannelCreated += LogChannelCreated;
            client.ChannelDestroyed += LogChannelDestroyed;
            client.ChannelUpdated += LogChannelUpdated;
            client.RoleCreated += LogRoleCreated;
            client.RoleDeleted += LogRoleDeleted;
            client.RoleUpdated += LogRoleUpdated;
            client.UserVoiceStateUpdated += LogVoiceStateUpdated;
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            var user = message.Author as SocketGuildUser;
            if (user == null) return false;

            if (content.StartsWith("!setlog"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission!");
                    return true;
                }
                await HandleSetLogCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!logconfig"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission!");
                    return true;
                }
                await HandleLogConfigCommand(message, guildId);
                return true;
            }

            return false;
        }

        private async Task HandleSetLogCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!setlog <type> [#channel]`\n\n" +
                    "**Types:**\n" +
                    "`messages` - Message edits and deletes\n" +
                    "`members` - Member joins, leaves, updates\n" +
                    "`server` - Channel/role changes\n" +
                    "`voice` - Voice channel activity\n\n" +
                    "**Example:** `!setlog messages #mod-log`\n" +
                    "**Disable:** `!setlog messages off`");
                return;
            }

            var type = parts[1].ToLower();
            var config = GetConfig(guildId);

            if (parts.Length == 2 || parts[2].ToLower() == "off")
            {
                // Disable logging
                switch (type)
                {
                    case "messages":
                        config.MessageLogChannel = null;
                        await message.Channel.SendMessageAsync("✅ Message logging disabled.");
                        break;
                    case "members":
                        config.MemberLogChannel = null;
                        await message.Channel.SendMessageAsync("✅ Member logging disabled.");
                        break;
                    case "server":
                        config.ServerLogChannel = null;
                        await message.Channel.SendMessageAsync("✅ Server logging disabled.");
                        break;
                    case "voice":
                        config.VoiceLogChannel = null;
                        await message.Channel.SendMessageAsync("✅ Voice logging disabled.");
                        break;
                    default:
                        await message.Channel.SendMessageAsync("❌ Invalid type! Use: messages, members, server, or voice");
                        return;
                }
                SaveData();
                return;
            }

            // Set log channel
            if (message.MentionedChannels.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Please mention a channel!");
                return;
            }

            var channel = message.MentionedChannels.First();

            switch (type)
            {
                case "messages":
                    config.MessageLogChannel = channel.Id;
                    await message.Channel.SendMessageAsync($"✅ Message logs will be sent to <#{channel.Id}>");
                    break;
                case "members":
                    config.MemberLogChannel = channel.Id;
                    await message.Channel.SendMessageAsync($"✅ Member logs will be sent to <#{channel.Id}>");
                    break;
                case "server":
                    config.ServerLogChannel = channel.Id;
                    await message.Channel.SendMessageAsync($"✅ Server logs will be sent to <#{channel.Id}>");
                    break;
                case "voice":
                    config.VoiceLogChannel = channel.Id;
                    await message.Channel.SendMessageAsync($"✅ Voice logs will be sent to <#{channel.Id}>");
                    break;
                default:
                    await message.Channel.SendMessageAsync("❌ Invalid type!");
                    return;
            }

            SaveData();
        }

        private async Task HandleLogConfigCommand(SocketUserMessage message, ulong guildId)
        {
            var config = GetConfig(guildId);

            var embed = new EmbedBuilder()
                .WithTitle("📋 Logging Configuration")
                .WithColor(Color.Blue);

            var messageChannel = config.MessageLogChannel.HasValue ? $"<#{config.MessageLogChannel.Value}>" : "❌ Not set";
            var memberChannel = config.MemberLogChannel.HasValue ? $"<#{config.MemberLogChannel.Value}>" : "❌ Not set";
            var serverChannel = config.ServerLogChannel.HasValue ? $"<#{config.ServerLogChannel.Value}>" : "❌ Not set";
            var voiceChannel = config.VoiceLogChannel.HasValue ? $"<#{config.VoiceLogChannel.Value}>" : "❌ Not set";

            embed.AddField("📝 Message Logs", messageChannel, inline: true);
            embed.AddField("👥 Member Logs", memberChannel, inline: true);
            embed.AddField("🛠️ Server Logs", serverChannel, inline: true);
            embed.AddField("🎙️ Voice Logs", voiceChannel, inline: true);

            embed.AddField("Logged Events",
                $"✅ Message edits & deletes\n" +
                $"✅ Member joins & leaves\n" +
                $"✅ Member role changes\n" +
                $"✅ Channel changes\n" +
                $"✅ Role changes\n" +
                $"✅ Voice activity\n" +
                $"✅ Bans & unbans",
                inline: false);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        #region Event Handlers

        private async Task LogMessageDeleted(Cacheable<IMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel)
        {
            var channel = await cachedChannel.GetOrDownloadAsync();
            if (channel is not SocketGuildChannel guildChannel) return;

            var config = GetConfig(guildChannel.Guild.Id);
            if (!config.MessageLogChannel.HasValue || !config.LogMessageDeletes) return;

            var logChannel = guildChannel.Guild.GetTextChannel(config.MessageLogChannel.Value);
            if (logChannel == null) return;

            var message = await cachedMessage.GetOrDownloadAsync();
            if (message == null || message.Author.IsBot) return;

            var embed = new EmbedBuilder()
                .WithTitle("🗑️ Message Deleted")
                .WithColor(Color.Red)
                .AddField("Author", $"{message.Author.Mention} ({message.Author.Username}#{message.Author.Discriminator})", inline: true)
                .AddField("Channel", $"<#{channel.Id}>", inline: true)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (!string.IsNullOrEmpty(message.Content))
            {
                var content = message.Content.Length > 1024 ? message.Content.Substring(0, 1021) + "..." : message.Content;
                embed.AddField("Content", content, inline: false);
            }

            if (message.Attachments.Any())
            {
                embed.AddField("Attachments", $"{message.Attachments.Count} file(s)", inline: false);
            }

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        private async Task LogMessageUpdated(Cacheable<IMessage, ulong> cachedBefore, SocketMessage after, ISocketMessageChannel channel)
        {
            if (channel is not SocketGuildChannel guildChannel) return;
            if (after.Author.IsBot) return;

            var config = GetConfig(guildChannel.Guild.Id);
            if (!config.MessageLogChannel.HasValue || !config.LogMessageEdits) return;

            var before = await cachedBefore.GetOrDownloadAsync();
            if (before == null || before.Content == after.Content) return;

            var logChannel = guildChannel.Guild.GetTextChannel(config.MessageLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("📝 Message Edited")
                .WithColor(Color.Orange)
                .AddField("Author", $"{after.Author.Mention} ({after.Author.Username}#{after.Author.Discriminator})", inline: true)
                .AddField("Channel", $"<#{channel.Id}>", inline: true)
                .WithTimestamp(DateTimeOffset.UtcNow);

            if (!string.IsNullOrEmpty(before.Content))
            {
                var beforeContent = before.Content.Length > 512 ? before.Content.Substring(0, 509) + "..." : before.Content;
                embed.AddField("Before", beforeContent, inline: false);
            }

            if (!string.IsNullOrEmpty(after.Content))
            {
                var afterContent = after.Content.Length > 512 ? after.Content.Substring(0, 509) + "..." : after.Content;
                embed.AddField("After", afterContent, inline: false);
            }

            embed.AddField("Jump to Message", $"[Click here]({after.GetJumpUrl()})", inline: false);

            await logChannel.SendMessageAsync(embed: embed.Build());
        }

        private async Task LogUserJoined(SocketGuildUser user)
        {
            var config = GetConfig(user.Guild.Id);
            if (!config.MemberLogChannel.HasValue || !config.LogMemberJoins) return;

            var logChannel = user.Guild.GetTextChannel(config.MemberLogChannel.Value);
            if (logChannel == null) return;

            var accountAge = DateTime.UtcNow - user.CreatedAt.UtcDateTime;

            var embed = new EmbedBuilder()
                .WithTitle("📥 Member Joined")
                .WithColor(Color.Green)
                .AddField("User", $"{user.Mention} ({user.Username}#{user.Discriminator})", inline: true)
                .AddField("ID", user.Id.ToString(), inline: true)
                .AddField("Account Created", $"{user.CreatedAt:yyyy-MM-dd HH:mm} UTC ({accountAge.Days} days ago)", inline: false)
                .AddField("Member Count", user.Guild.MemberCount.ToString(), inline: true)
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogUserLeft(SocketGuild guild, SocketUser user)
        {
            var config = GetConfig(guild.Id);
            if (!config.MemberLogChannel.HasValue || !config.LogMemberLeaves) return;

            var logChannel = guild.GetTextChannel(config.MemberLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("📤 Member Left")
                .WithColor(Color.Red)
                .AddField("User", $"{user.Username}#{user.Discriminator}", inline: true)
                .AddField("ID", user.Id.ToString(), inline: true)
                .AddField("Member Count", guild.MemberCount.ToString(), inline: true)
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> cachedBefore, SocketGuildUser after)
        {
            var config = GetConfig(after.Guild.Id);
            if (!config.MemberLogChannel.HasValue || !config.LogMemberUpdates) return;

            var before = await cachedBefore.GetOrDownloadAsync();
            if (before == null) return;

            var logChannel = after.Guild.GetTextChannel(config.MemberLogChannel.Value);
            if (logChannel == null) return;

            var changes = new List<string>();

            // Check nickname change
            if (before.Nickname != after.Nickname)
            {
                changes.Add($"**Nickname:** {before.Nickname ?? "None"} → {after.Nickname ?? "None"}");
            }

            // Check role changes
            var addedRoles = after.Roles.Except(before.Roles).ToList();
            var removedRoles = before.Roles.Except(after.Roles).ToList();

            if (addedRoles.Any())
            {
                changes.Add($"**Roles Added:** {string.Join(", ", addedRoles.Select(r => r.Mention))}");
            }

            if (removedRoles.Any())
            {
                changes.Add($"**Roles Removed:** {string.Join(", ", removedRoles.Select(r => r.Mention))}");
            }

            if (!changes.Any()) return;

            var embed = new EmbedBuilder()
                .WithTitle("👤 Member Updated")
                .WithColor(Color.Blue)
                .AddField("User", $"{after.Mention} ({after.Username}#{after.Discriminator})", inline: false)
                .AddField("Changes", string.Join("\n", changes), inline: false)
                .WithThumbnailUrl(after.GetAvatarUrl() ?? after.GetDefaultAvatarUrl())
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogUserBanned(SocketUser user, SocketGuild guild)
        {
            var config = GetConfig(guild.Id);
            if (!config.MemberLogChannel.HasValue || !config.LogBans) return;

            var logChannel = guild.GetTextChannel(config.MemberLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("🔨 User Banned")
                .WithColor(Color.DarkRed)
                .AddField("User", $"{user.Username}#{user.Discriminator}", inline: true)
                .AddField("ID", user.Id.ToString(), inline: true)
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogUserUnbanned(SocketUser user, SocketGuild guild)
        {
            var config = GetConfig(guild.Id);
            if (!config.MemberLogChannel.HasValue || !config.LogBans) return;

            var logChannel = guild.GetTextChannel(config.MemberLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("✅ User Unbanned")
                .WithColor(Color.Green)
                .AddField("User", $"{user.Username}#{user.Discriminator}", inline: true)
                .AddField("ID", user.Id.ToString(), inline: true)
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogChannelCreated(SocketChannel channel)
        {
            if (channel is not SocketGuildChannel guildChannel) return;

            var config = GetConfig(guildChannel.Guild.Id);
            if (!config.ServerLogChannel.HasValue || !config.LogChannelChanges) return;

            var logChannel = guildChannel.Guild.GetTextChannel(config.ServerLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("➕ Channel Created")
                .WithColor(Color.Green)
                .AddField("Channel", $"<#{guildChannel.Id}> ({guildChannel.Name})", inline: true)
                .AddField("Type", guildChannel.GetType().Name, inline: true)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogChannelDestroyed(SocketChannel channel)
        {
            if (channel is not SocketGuildChannel guildChannel) return;

            var config = GetConfig(guildChannel.Guild.Id);
            if (!config.ServerLogChannel.HasValue || !config.LogChannelChanges) return;

            var logChannel = guildChannel.Guild.GetTextChannel(config.ServerLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("➖ Channel Deleted")
                .WithColor(Color.Red)
                .AddField("Channel", guildChannel.Name, inline: true)
                .AddField("Type", guildChannel.GetType().Name, inline: true)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogChannelUpdated(SocketChannel before, SocketChannel after)
        {
            if (before is not SocketGuildChannel beforeGuild || after is not SocketGuildChannel afterGuild) return;

            var config = GetConfig(afterGuild.Guild.Id);
            if (!config.ServerLogChannel.HasValue || !config.LogChannelChanges) return;

            var logChannel = afterGuild.Guild.GetTextChannel(config.ServerLogChannel.Value);
            if (logChannel == null) return;

            var changes = new List<string>();

            if (beforeGuild.Name != afterGuild.Name)
            {
                changes.Add($"**Name:** {beforeGuild.Name} → {afterGuild.Name}");
            }

            if (!changes.Any()) return;

            var embed = new EmbedBuilder()
                .WithTitle("🔧 Channel Updated")
                .WithColor(Color.Orange)
                .AddField("Channel", $"<#{afterGuild.Id}>", inline: true)
                .AddField("Changes", string.Join("\n", changes), inline: false)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogRoleCreated(SocketRole role)
        {
            var config = GetConfig(role.Guild.Id);
            if (!config.ServerLogChannel.HasValue || !config.LogRoleChanges) return;

            var logChannel = role.Guild.GetTextChannel(config.ServerLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("➕ Role Created")
                .WithColor(Color.Green)
                .AddField("Role", role.Mention, inline: true)
                .AddField("Color", role.Color.ToString(), inline: true)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogRoleDeleted(SocketRole role)
        {
            var config = GetConfig(role.Guild.Id);
            if (!config.ServerLogChannel.HasValue || !config.LogRoleChanges) return;

            var logChannel = role.Guild.GetTextChannel(config.ServerLogChannel.Value);
            if (logChannel == null) return;

            var embed = new EmbedBuilder()
                .WithTitle("➖ Role Deleted")
                .WithColor(Color.Red)
                .AddField("Role", role.Name, inline: true)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogRoleUpdated(SocketRole before, SocketRole after)
        {
            var config = GetConfig(after.Guild.Id);
            if (!config.ServerLogChannel.HasValue || !config.LogRoleChanges) return;

            var logChannel = after.Guild.GetTextChannel(config.ServerLogChannel.Value);
            if (logChannel == null) return;

            var changes = new List<string>();

            if (before.Name != after.Name)
            {
                changes.Add($"**Name:** {before.Name} → {after.Name}");
            }

            if (before.Color != after.Color)
            {
                changes.Add($"**Color:** {before.Color} → {after.Color}");
            }

            if (!changes.Any()) return;

            var embed = new EmbedBuilder()
                .WithTitle("🔧 Role Updated")
                .WithColor(Color.Orange)
                .AddField("Role", after.Mention, inline: true)
                .AddField("Changes", string.Join("\n", changes), inline: false)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        private async Task LogVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (user is not SocketGuildUser guildUser) return;

            var config = GetConfig(guildUser.Guild.Id);
            if (!config.VoiceLogChannel.HasValue || !config.LogVoiceActivity) return;

            var logChannel = guildUser.Guild.GetTextChannel(config.VoiceLogChannel.Value);
            if (logChannel == null) return;

            string action = "";
            Color color = Color.Blue;

            if (before.VoiceChannel == null && after.VoiceChannel != null)
            {
                action = $"Joined voice channel {after.VoiceChannel.Mention}";
                color = Color.Green;
            }
            else if (before.VoiceChannel != null && after.VoiceChannel == null)
            {
                action = $"Left voice channel {before.VoiceChannel.Mention}";
                color = Color.Red;
            }
            else if (before.VoiceChannel != after.VoiceChannel)
            {
                action = $"Moved from {before.VoiceChannel?.Mention} to {after.VoiceChannel?.Mention}";
                color = Color.Orange;
            }
            else if (before.IsMuted != after.IsMuted)
            {
                action = after.IsMuted ? "Muted" : "Unmuted";
            }
            else if (before.IsDeafened != after.IsDeafened)
            {
                action = after.IsDeafened ? "Deafened" : "Undeafened";
            }
            else
            {
                return; // No relevant changes
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎙️ Voice Activity")
                .WithColor(color)
                .AddField("User", $"{user.Mention} ({user.Username}#{user.Discriminator})", inline: true)
                .AddField("Action", action, inline: false)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await logChannel.SendMessageAsync(embed: embed);
        }

        #endregion

        private LoggingConfig GetConfig(ulong guildId)
        {
            if (!_guildConfigs.ContainsKey(guildId))
                _guildConfigs[guildId] = new LoggingConfig();

            return _guildConfigs[guildId];
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
                    var config = JsonConvert.DeserializeObject<LoggingConfig>(json);
                    if (config != null)
                        _guildConfigs[guildId] = config;
                }
            }

            Console.WriteLine($"[Logging] Loaded configs for {_guildConfigs.Count} guild(s)");
        }

        private void SaveData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var kvp in _guildConfigs)
            {
                var filePath = Path.Combine(_dataFolder, $"{kvp.Key}.json");
                var json = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }
    }
}

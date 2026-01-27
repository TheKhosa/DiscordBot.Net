using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Stats
{
    /// <summary>
    /// Module for server statistics and member counters
    /// </summary>
    public class StatsModule : ModuleBase
    {
        public override string ModuleId => "stats";
        public override string Name => "Statistics";
        public override string Description => "Server statistics and member count channels";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly ConcurrentDictionary<ulong, GuildStats> _guildStats = new();
        private readonly string _dataDirectory;

        public StatsModule()
        {
            _dataDirectory = Path.Combine(AppContext.BaseDirectory, "StatsData");
            Directory.CreateDirectory(_dataDirectory);
        }

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            client.UserJoined += OnUserJoined;
            client.UserLeft += OnUserLeft;
            client.MessageReceived += OnMessageReceived;

            return base.InitializeAsync(client, services);
        }

        public override Task ShutdownAsync()
        {
            if (Client != null)
            {
                Client.UserJoined -= OnUserJoined;
                Client.UserLeft -= OnUserLeft;
                Client.MessageReceived -= OnMessageReceived;
            }

            return base.ShutdownAsync();
        }

        private Task OnUserJoined(SocketGuildUser user)
        {
            var stats = GetGuildStats(user.Guild.Id);
            stats.TotalJoins++;
            _ = SaveStatsAsync(stats);
            _ = UpdateStatChannels(user.Guild);
            return Task.CompletedTask;
        }

        private Task OnUserLeft(SocketGuild guild, SocketUser user)
        {
            var stats = GetGuildStats(guild.Id);
            stats.TotalLeaves++;
            _ = SaveStatsAsync(stats);
            _ = UpdateStatChannels(guild);
            return Task.CompletedTask;
        }

        private Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return Task.CompletedTask;
            if (message.Channel is not SocketGuildChannel guildChannel) return Task.CompletedTask;

            var stats = GetGuildStats(guildChannel.Guild.Id);
            stats.TotalMessages++;
            
            // Save every 100 messages to reduce I/O
            if (stats.TotalMessages % 100 == 0)
            {
                _ = SaveStatsAsync(stats);
            }

            return Task.CompletedTask;
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            if (content.StartsWith("!stats"))
            {
                await HandleStatsCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!setupstats"))
            {
                await HandleSetupStatsCommand(message, guildChannel.Guild);
                return true;
            }

            return false;
        }

        private async Task HandleStatsCommand(SocketUserMessage message, SocketGuild guild)
        {
            var stats = GetGuildStats(guild.Id);

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Server Statistics: {guild.Name}")
                .WithThumbnailUrl(guild.IconUrl)
                .WithColor(Color.Blue)
                .AddField("👥 Total Members", guild.MemberCount.ToString("N0"), inline: true)
                .AddField("👤 Humans", guild.Users.Count(u => !u.IsBot).ToString("N0"), inline: true)
                .AddField("🤖 Bots", guild.Users.Count(u => u.IsBot).ToString("N0"), inline: true)
                .AddField("💬 Total Messages", stats.TotalMessages.ToString("N0"), inline: true)
                .AddField("📥 Total Joins", stats.TotalJoins.ToString("N0"), inline: true)
                .AddField("📤 Total Leaves", stats.TotalLeaves.ToString("N0"), inline: true)
                .AddField("📺 Voice Channels", guild.VoiceChannels.Count.ToString(), inline: true)
                .AddField("💬 Text Channels", guild.TextChannels.Count.ToString(), inline: true)
                .AddField("📁 Categories", guild.CategoryChannels.Count.ToString(), inline: true)
                .AddField("🎭 Roles", guild.Roles.Count.ToString(), inline: true)
                .AddField("😀 Emojis", guild.Emotes.Count.ToString(), inline: true)
                .AddField("🚀 Boost Level", $"Tier {guild.PremiumTier} ({guild.PremiumSubscriptionCount} boosts)", inline: true)
                .WithFooter($"Server created on {guild.CreatedAt:yyyy-MM-dd}")
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleSetupStatsCommand(SocketUserMessage message, SocketGuild guild)
        {
            var author = message.Author as SocketGuildUser;
            if (author == null || !author.GuildPermissions.Administrator)
            {
                await message.Channel.SendMessageAsync("❌ You need Administrator permission to use this command.");
                return;
            }

            try
            {
                // Create stat channels
                var category = await guild.CreateCategoryChannelAsync("📊 Server Stats");

                var memberChannel = await guild.CreateVoiceChannelAsync($"👥 Members: {guild.MemberCount}", props =>
                {
                    props.CategoryId = category.Id;
                });

                var humanChannel = await guild.CreateVoiceChannelAsync($"👤 Humans: {guild.Users.Count(u => !u.IsBot)}", props =>
                {
                    props.CategoryId = category.Id;
                });

                var botChannel = await guild.CreateVoiceChannelAsync($"🤖 Bots: {guild.Users.Count(u => u.IsBot)}", props =>
                {
                    props.CategoryId = category.Id;
                });

                var boostChannel = await guild.CreateVoiceChannelAsync($"🚀 Boosts: {guild.PremiumSubscriptionCount}", props =>
                {
                    props.CategoryId = category.Id;
                });

                // Lock permissions so users can't join
                await memberChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, 
                    new OverwritePermissions(connect: PermValue.Deny));
                await humanChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, 
                    new OverwritePermissions(connect: PermValue.Deny));
                await botChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, 
                    new OverwritePermissions(connect: PermValue.Deny));
                await boostChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, 
                    new OverwritePermissions(connect: PermValue.Deny));

                // Save channel IDs
                var stats = GetGuildStats(guild.Id);
                stats.MemberCountChannelId = memberChannel.Id;
                stats.HumanCountChannelId = humanChannel.Id;
                stats.BotCountChannelId = botChannel.Id;
                stats.BoostCountChannelId = boostChannel.Id;
                await SaveStatsAsync(stats);

                await message.Channel.SendMessageAsync("✅ Statistics channels created!");
            }
            catch (Exception ex)
            {
                await message.Channel.SendMessageAsync($"❌ Error creating stat channels: {ex.Message}");
            }
        }

        private async Task UpdateStatChannels(SocketGuild guild)
        {
            var stats = GetGuildStats(guild.Id);

            try
            {
                if (stats.MemberCountChannelId.HasValue)
                {
                    var channel = guild.GetVoiceChannel(stats.MemberCountChannelId.Value);
                    await channel?.ModifyAsync(c => c.Name = $"👥 Members: {guild.MemberCount}");
                }

                if (stats.HumanCountChannelId.HasValue)
                {
                    var channel = guild.GetVoiceChannel(stats.HumanCountChannelId.Value);
                    await channel?.ModifyAsync(c => c.Name = $"👤 Humans: {guild.Users.Count(u => !u.IsBot)}");
                }

                if (stats.BotCountChannelId.HasValue)
                {
                    var channel = guild.GetVoiceChannel(stats.BotCountChannelId.Value);
                    await channel?.ModifyAsync(c => c.Name = $"🤖 Bots: {guild.Users.Count(u => u.IsBot)}");
                }

                if (stats.BoostCountChannelId.HasValue)
                {
                    var channel = guild.GetVoiceChannel(stats.BoostCountChannelId.Value);
                    await channel?.ModifyAsync(c => c.Name = $"🚀 Boosts: {guild.PremiumSubscriptionCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating stat channels: {ex.Message}");
            }
        }

        private GuildStats GetGuildStats(ulong guildId)
        {
            return _guildStats.GetOrAdd(guildId, _ => LoadStats(guildId) ?? new GuildStats { GuildId = guildId });
        }

        private GuildStats? LoadStats(ulong guildId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{guildId}.json");
                if (!File.Exists(filePath)) return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<GuildStats>(json);
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveStatsAsync(GuildStats stats)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{stats.GuildId}.json");
                var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving stats: {ex.Message}");
            }
        }

        private class GuildStats
        {
            public ulong GuildId { get; set; }
            public long TotalMessages { get; set; }
            public long TotalJoins { get; set; }
            public long TotalLeaves { get; set; }
            public ulong? MemberCountChannelId { get; set; }
            public ulong? HumanCountChannelId { get; set; }
            public ulong? BotCountChannelId { get; set; }
            public ulong? BoostCountChannelId { get; set; }
        }
    }
}

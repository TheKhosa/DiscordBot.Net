using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Giveaways
{
    /// <summary>
    /// Giveaways module for hosting contests with automatic winner selection
    /// </summary>
    public class GiveawaysModule : ModuleBase
    {
        public override string ModuleId => "giveaways";
        public override string Name => "Giveaways";
        public override string Description => "Host and manage giveaways with automatic winner selection";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly string _dataFolder = "GiveawaysData";
        private Dictionary<ulong, GiveawayData> _guildData = new();
        private const string GiveawayEmoji = "🎉";

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadData();
            
            // Subscribe to reaction events
            client.ReactionAdded += HandleReactionAdded;
            client.ReactionRemoved += HandleReactionRemoved;
            
            // Start timer to check for ended giveaways
            var timer = new System.Timers.Timer(30000); // Check every 30 seconds
            timer.Elapsed += async (sender, e) => await CheckEndedGiveaways();
            timer.Start();
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            var user = message.Author as SocketGuildUser;
            if (user == null) return false;

            if (content.StartsWith("!giveaway") || content.StartsWith("!gstart"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission to create giveaways!");
                    return true;
                }
                await HandleGiveawayCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!gend"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission to end giveaways!");
                    return true;
                }
                await HandleEndGiveawayCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!greroll"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission to reroll giveaways!");
                    return true;
                }
                await HandleRerollCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!glist"))
            {
                await HandleListGiveawaysCommand(message, guildId);
                return true;
            }

            return false;
        }

        private async Task HandleGiveawayCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 4)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!giveaway <duration> <winners> <prize>`\n\n" +
                    "**Examples:**\n" +
                    "`!giveaway 1h 1 Nitro Classic`\n" +
                    "`!giveaway 3d 3 Discord Nitro`\n" +
                    "`!giveaway 30m 2 $50 Steam Gift Card`\n\n" +
                    "**Duration formats:**\n" +
                    "`30s` = 30 seconds\n" +
                    "`5m` = 5 minutes\n" +
                    "`2h` = 2 hours\n" +
                    "`7d` = 7 days");
                return;
            }

            var duration = ParseDuration(parts[1]);
            if (duration == null)
            {
                await message.Channel.SendMessageAsync("❌ Invalid duration format! Use: 30s, 5m, 2h, 7d");
                return;
            }

            if (!int.TryParse(parts[2], out int winners) || winners < 1 || winners > 20)
            {
                await message.Channel.SendMessageAsync("❌ Winners must be between 1 and 20!");
                return;
            }

            var prize = parts[3];
            if (prize.Length > 200)
            {
                await message.Channel.SendMessageAsync("❌ Prize description is too long! Maximum 200 characters.");
                return;
            }

            var endTime = DateTime.UtcNow.Add(duration.Value);

            var giveaway = new Giveaway
            {
                GuildId = guild.Id,
                ChannelId = message.Channel.Id,
                HostId = message.Author.Id,
                HostName = message.Author.Username,
                Prize = prize,
                Winners = winners,
                StartTime = DateTime.UtcNow,
                EndTime = endTime
            };

            var embed = CreateGiveawayEmbed(giveaway, true);
            var giveawayMessage = await message.Channel.SendMessageAsync(embed: embed);
            
            await giveawayMessage.AddReactionAsync(new Emoji(GiveawayEmoji));

            giveaway.MessageId = giveawayMessage.Id;

            var data = GetGuildData(guild.Id);
            data.ActiveGiveaways[giveaway.Id] = giveaway;
            SaveData();

            await message.DeleteAsync();
            
            Console.WriteLine($"[Giveaways] Created giveaway '{prize}' in {guild.Name}, ends at {endTime:yyyy-MM-dd HH:mm}");
        }

        private async Task HandleEndGiveawayCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!gend <giveaway-id>`\n\n" +
                    "Use `!glist` to see active giveaway IDs.");
                return;
            }

            var giveawayId = parts[1];
            var data = GetGuildData(guildId);

            if (!data.ActiveGiveaways.ContainsKey(giveawayId))
            {
                await message.Channel.SendMessageAsync("❌ Giveaway not found! Use `!glist` to see active giveaways.");
                return;
            }

            var giveaway = data.ActiveGiveaways[giveawayId];
            await EndGiveaway(giveaway);
            
            await message.Channel.SendMessageAsync($"✅ Ended giveaway: {giveaway.Prize}");
        }

        private async Task HandleRerollCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!greroll <giveaway-id>`");
                return;
            }

            var giveawayId = parts[1];
            var data = GetGuildData(guildId);

            var giveaway = data.CompletedGiveaways.FirstOrDefault(g => g.Id == giveawayId);
            if (giveaway == null)
            {
                await message.Channel.SendMessageAsync("❌ Completed giveaway not found!");
                return;
            }

            if (giveaway.Entries.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ No entries to reroll!");
                return;
            }

            // Pick new winners
            var newWinners = PickWinners(giveaway.Entries, Math.Min(giveaway.Winners, giveaway.Entries.Count));
            
            var channel = await Client!.GetChannelAsync(giveaway.ChannelId) as ITextChannel;
            if (channel == null) return;

            var winnerMentions = new List<string>();
            foreach (var winnerId in newWinners)
            {
                winnerMentions.Add($"<@{winnerId}>");
            }

            await channel.SendMessageAsync(
                $"🎉 **GIVEAWAY REROLLED!**\n\n" +
                $"**Prize:** {giveaway.Prize}\n" +
                $"**New Winner(s):** {string.Join(", ", winnerMentions)}\n\n" +
                $"Congratulations!");

            Console.WriteLine($"[Giveaways] Rerolled giveaway '{giveaway.Prize}'");
        }

        private async Task HandleListGiveawaysCommand(SocketUserMessage message, ulong guildId)
        {
            var data = GetGuildData(guildId);

            if (data.ActiveGiveaways.Count == 0)
            {
                await message.Channel.SendMessageAsync("No active giveaways in this server.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎉 Active Giveaways")
                .WithColor(Color.Gold);

            foreach (var giveaway in data.ActiveGiveaways.Values.OrderBy(g => g.EndTime))
            {
                var timeLeft = giveaway.EndTime - DateTime.UtcNow;
                var timeStr = FormatTimeSpan(timeLeft);

                embed.AddField(
                    $"{giveaway.Prize}",
                    $"**ID:** `{giveaway.Id.Substring(0, 8)}`\n" +
                    $"**Winners:** {giveaway.Winners}\n" +
                    $"**Entries:** {giveaway.Entries.Count}\n" +
                    $"**Ends in:** {timeStr}\n" +
                    $"**Channel:** <#{giveaway.ChannelId}>",
                    inline: false
                );
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
            if (reaction.Emote.Name != GiveawayEmoji) return;

            var messageId = reaction.MessageId;
            var userId = reaction.UserId;

            // Find giveaway
            Giveaway? giveaway = null;
            foreach (var guildData in _guildData.Values)
            {
                giveaway = guildData.ActiveGiveaways.Values.FirstOrDefault(g => g.MessageId == messageId);
                if (giveaway != null) break;
            }

            if (giveaway == null) return;

            // Add entry
            if (!giveaway.Entries.Contains(userId))
            {
                giveaway.Entries.Add(userId);
                SaveData();
                Console.WriteLine($"[Giveaways] User {userId} entered giveaway '{giveaway.Prize}'");
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;
            if (reaction.Emote.Name != GiveawayEmoji) return;

            var messageId = reaction.MessageId;
            var userId = reaction.UserId;

            // Find giveaway
            Giveaway? giveaway = null;
            foreach (var guildData in _guildData.Values)
            {
                giveaway = guildData.ActiveGiveaways.Values.FirstOrDefault(g => g.MessageId == messageId);
                if (giveaway != null) break;
            }

            if (giveaway == null) return;

            // Remove entry
            if (giveaway.Entries.Contains(userId))
            {
                giveaway.Entries.Remove(userId);
                SaveData();
                Console.WriteLine($"[Giveaways] User {userId} left giveaway '{giveaway.Prize}'");
            }
        }

        private async Task CheckEndedGiveaways()
        {
            if (Client == null) return;

            var now = DateTime.UtcNow;
            var endedGiveaways = new List<Giveaway>();

            foreach (var guildData in _guildData.Values)
            {
                foreach (var giveaway in guildData.ActiveGiveaways.Values)
                {
                    if (now >= giveaway.EndTime)
                    {
                        endedGiveaways.Add(giveaway);
                    }
                }
            }

            foreach (var giveaway in endedGiveaways)
            {
                await EndGiveaway(giveaway);
            }
        }

        private async Task EndGiveaway(Giveaway giveaway)
        {
            if (Client == null) return;

            giveaway.IsCompleted = true;

            var channel = await Client.GetChannelAsync(giveaway.ChannelId) as ITextChannel;
            if (channel == null) return;

            try
            {
                var message = await channel.GetMessageAsync(giveaway.MessageId);
                if (message != null)
                {
                    var embed = CreateGiveawayEmbed(giveaway, false);
                    await (message as IUserMessage)!.ModifyAsync(m => m.Embed = embed);
                }
            }
            catch { }

            if (giveaway.Entries.Count == 0)
            {
                await channel.SendMessageAsync(
                    $"🎉 **GIVEAWAY ENDED**\n\n" +
                    $"**Prize:** {giveaway.Prize}\n" +
                    $"**Winner:** No one entered 😢");
            }
            else
            {
                var winnersCount = Math.Min(giveaway.Winners, giveaway.Entries.Count);
                var winners = PickWinners(giveaway.Entries, winnersCount);

                giveaway.WinnersList = new List<GiveawayWinner>();
                var winnerMentions = new List<string>();

                foreach (var winnerId in winners)
                {
                    var user = await Client.GetUserAsync(winnerId);
                    winnerMentions.Add($"<@{winnerId}>");
                    giveaway.WinnersList.Add(new GiveawayWinner
                    {
                        UserId = winnerId,
                        Username = user?.Username ?? "Unknown",
                        WonAt = DateTime.UtcNow
                    });
                }

                await channel.SendMessageAsync(
                    $"🎉 **GIVEAWAY ENDED!**\n\n" +
                    $"**Prize:** {giveaway.Prize}\n" +
                    $"**Winner(s):** {string.Join(", ", winnerMentions)}\n\n" +
                    $"Congratulations! 🎊");
            }

            // Move to completed
            var data = GetGuildData(giveaway.GuildId);
            data.ActiveGiveaways.Remove(giveaway.Id);
            data.CompletedGiveaways.Add(giveaway);
            
            // Keep only last 50 completed giveaways
            if (data.CompletedGiveaways.Count > 50)
            {
                data.CompletedGiveaways = data.CompletedGiveaways
                    .OrderByDescending(g => g.EndTime)
                    .Take(50)
                    .ToList();
            }

            SaveData();
            Console.WriteLine($"[Giveaways] Ended giveaway '{giveaway.Prize}' with {giveaway.WinnersList.Count} winner(s)");
        }

        private List<ulong> PickWinners(List<ulong> entries, int count)
        {
            var random = new Random();
            var shuffled = entries.OrderBy(x => random.Next()).ToList();
            return shuffled.Take(count).ToList();
        }

        private Embed CreateGiveawayEmbed(Giveaway giveaway, bool isActive)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 GIVEAWAY 🎉")
                .WithColor(isActive ? Color.Gold : Color.DarkGrey)
                .AddField("Prize", giveaway.Prize, inline: false)
                .AddField("Hosted by", $"<@{giveaway.HostId}>", inline: true)
                .AddField("Winners", giveaway.Winners.ToString(), inline: true)
                .AddField("Entries", giveaway.Entries.Count.ToString(), inline: true);

            if (isActive)
            {
                var timeLeft = giveaway.EndTime - DateTime.UtcNow;
                embed.AddField("Ends in", FormatTimeSpan(timeLeft), inline: false);
                embed.WithDescription($"React with {GiveawayEmoji} to enter!");
                embed.WithFooter($"Ends at • Giveaway ID: {giveaway.Id.Substring(0, 8)}");
                embed.WithTimestamp(giveaway.EndTime);
            }
            else
            {
                embed.WithDescription("**ENDED**");
                if (giveaway.WinnersList.Count > 0)
                {
                    var winners = string.Join("\n", giveaway.WinnersList.Select(w => $"<@{w.UserId}>"));
                    embed.AddField("Winner(s)", winners, inline: false);
                }
                embed.WithFooter($"Ended at • {giveaway.Entries.Count} total entries");
            }

            return embed.Build();
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

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            else
                return $"{(int)timeSpan.TotalSeconds}s";
        }

        private GiveawayData GetGuildData(ulong guildId)
        {
            if (!_guildData.ContainsKey(guildId))
                _guildData[guildId] = new GiveawayData();

            return _guildData[guildId];
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
                    var data = JsonConvert.DeserializeObject<GiveawayData>(json);
                    if (data != null)
                        _guildData[guildId] = data;
                }
            }

            Console.WriteLine($"[Giveaways] Loaded data for {_guildData.Count} guild(s)");
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

using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Leveling
{
    /// <summary>
    /// Module for user leveling and XP system
    /// </summary>
    public class LevelingModule : ModuleBase
    {
        public override string ModuleId => "leveling";
        public override string Name => "Leveling System";
        public override string Description => "Track user activity with XP, levels, and leaderboards";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private LevelingService? _levelingService;

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _levelingService = new LevelingService();

            // Listen to message events for XP
            client.MessageReceived += OnMessageReceived;

            return base.InitializeAsync(client, services);
        }

        public override Task ShutdownAsync()
        {
            if (Client != null)
            {
                Client.MessageReceived -= OnMessageReceived;
            }

            return base.ShutdownAsync();
        }

        private async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            if (message.Channel is not SocketGuildChannel guildChannel) return;

            var guildId = guildChannel.Guild.Id;
            var userId = message.Author.Id;

            var (awarded, leveledUp, xpGained) = await _levelingService!.AwardMessageXpAsync(guildId, userId);

            if (leveledUp)
            {
                var userLevel = _levelingService.GetUserLevel(guildId, userId);
                var embed = new EmbedBuilder()
                    .WithTitle("🎉 Level Up!")
                    .WithDescription($"{message.Author.Mention} reached **Level {userLevel.Level}**!")
                    .WithColor(Color.Gold)
                    .WithThumbnailUrl(message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl())
                    .WithCurrentTimestamp()
                    .Build();

                await message.Channel.SendMessageAsync(embed: embed);
            }
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();

            if (content.StartsWith("!rank") || content.StartsWith("!level"))
            {
                await HandleRankCommand(message, guildChannel.Guild.Id);
                return true;
            }

            if (content.StartsWith("!leaderboard") || content.StartsWith("!lb"))
            {
                await HandleLeaderboardCommand(message, guildChannel.Guild.Id);
                return true;
            }

            if (content.StartsWith("!setxp") && message.Author is SocketGuildUser guildUser)
            {
                await HandleSetXpCommand(message, guildChannel.Guild.Id, guildUser);
                return true;
            }

            return false;
        }

        private async Task HandleRankCommand(SocketUserMessage message, ulong guildId)
        {
            // Check if targeting another user
            ulong targetUserId = message.Author.Id;
            string targetUsername = message.Author.Username;
            string? avatarUrl = message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl();

            if (message.MentionedUsers.Count > 0)
            {
                var mentioned = message.MentionedUsers.First();
                targetUserId = mentioned.Id;
                targetUsername = mentioned.Username;
                avatarUrl = mentioned.GetAvatarUrl() ?? mentioned.GetDefaultAvatarUrl();
            }

            var userLevel = _levelingService!.GetUserLevel(guildId, targetUserId);
            var rank = _levelingService.GetUserRank(guildId, targetUserId);
            var progress = userLevel.GetProgress();
            var xpForNext = userLevel.XpForNextLevel();
            var currentLevelXp = UserLevel.XpForLevel(userLevel.Level);
            var xpIntoLevel = userLevel.Experience - currentLevelXp;

            // Create progress bar
            var progressBar = CreateProgressBar(progress, 20);

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Rank Card - {targetUsername}")
                .WithColor(Color.Blue)
                .WithThumbnailUrl(avatarUrl)
                .AddField("Level", userLevel.Level.ToString(), inline: true)
                .AddField("Rank", rank == 0 ? "Unranked" : $"#{rank}", inline: true)
                .AddField("Total XP", userLevel.Experience.ToString("N0"), inline: true)
                .AddField("Progress", $"{progressBar} {progress:P1}")
                .AddField("XP Progress", $"{xpIntoLevel:N0} / {xpForNext:N0} XP", inline: true)
                .AddField("Messages", userLevel.MessageCount.ToString("N0"), inline: true)
                .AddField("Voice Time", $"{userLevel.VoiceMinutes} min", inline: true)
                .WithFooter($"Keep chatting to gain more XP!")
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleLeaderboardCommand(SocketUserMessage message, ulong guildId)
        {
            var leaderboard = _levelingService!.GetLeaderboard(guildId, 10);

            if (leaderboard.Count == 0)
            {
                await message.Channel.SendMessageAsync("No users on the leaderboard yet!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🏆 Leaderboard - Top 10")
                .WithColor(Color.Gold)
                .WithDescription("Top users by experience");

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var user = leaderboard[i];
                var discordUser = await Client!.GetUserAsync(user.UserId);
                var username = discordUser?.Username ?? $"User {user.UserId}";

                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"#{i + 1}"
                };

                embed.AddField(
                    $"{medal} {username}",
                    $"Level {user.Level} • {user.Experience:N0} XP • {user.MessageCount:N0} messages",
                    inline: false
                );
            }

            embed.WithFooter("Keep chatting to climb the ranks!");
            embed.WithCurrentTimestamp();

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleSetXpCommand(SocketUserMessage message, ulong guildId, SocketGuildUser author)
        {
            // Check if user has admin permissions
            if (!author.GuildPermissions.Administrator)
            {
                await message.Channel.SendMessageAsync("❌ You need Administrator permission to use this command.");
                return;
            }

            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!setxp @user <amount>`");
                return;
            }

            if (message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Please mention a user.");
                return;
            }

            if (!int.TryParse(parts[2], out int amount) || amount < 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid XP amount.");
                return;
            }

            var targetUser = message.MentionedUsers.First();
            var userLevel = _levelingService!.GetUserLevel(guildId, targetUser.Id);
            userLevel.Experience = amount;
            userLevel.Level = _levelingService.GetUserLevel(guildId, targetUser.Id).Level;

            await message.Channel.SendMessageAsync($"✅ Set {targetUser.Mention}'s XP to {amount:N0}");
        }

        private string CreateProgressBar(double progress, int length)
        {
            var filled = (int)(progress * length);
            var empty = length - filled;

            return $"[{new string('█', filled)}{new string('░', empty)}]";
        }
    }
}

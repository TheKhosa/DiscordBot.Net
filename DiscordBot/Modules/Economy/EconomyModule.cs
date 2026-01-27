using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Economy
{
    /// <summary>
    /// Module for virtual currency economy
    /// </summary>
    public class EconomyModule : ModuleBase
    {
        public override string ModuleId => "economy";
        public override string Name => "Economy System";
        public override string Description => "Virtual currency with banking, trading, and rewards";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private EconomyService? _economyService;

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _economyService = new EconomyService();
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            if (content.StartsWith("!balance") || content.StartsWith("!bal"))
            {
                await HandleBalanceCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!daily"))
            {
                await HandleDailyCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!weekly"))
            {
                await HandleWeeklyCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!work"))
            {
                await HandleWorkCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!pay") || content.StartsWith("!give"))
            {
                await HandlePayCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!deposit") || content.StartsWith("!dep"))
            {
                await HandleDepositCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!withdraw") || content.StartsWith("!with"))
            {
                await HandleWithdrawCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!richest") || content.StartsWith("!baltop"))
            {
                await HandleRichestCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!inventory") || content.StartsWith("!inv"))
            {
                await HandleInventoryCommand(message, guildId);
                return true;
            }

            return false;
        }

        private async Task HandleBalanceCommand(SocketUserMessage message, ulong guildId)
        {
            ulong targetUserId = message.Author.Id;
            string targetUsername = message.Author.Username;

            if (message.MentionedUsers.Count > 0)
            {
                var mentioned = message.MentionedUsers.First();
                targetUserId = mentioned.Id;
                targetUsername = mentioned.Username;
            }

            var wallet = _economyService!.GetWallet(guildId, targetUserId);

            var embed = new EmbedBuilder()
                .WithTitle($"{_economyService.CurrencySymbol} {targetUsername}'s Wallet")
                .WithColor(Color.Green)
                .AddField("💵 Cash", $"{wallet.Balance:N0} {_economyService.CurrencyName}", inline: true)
                .AddField("🏦 Bank", $"{wallet.BankBalance:N0} {_economyService.CurrencyName}", inline: true)
                .AddField("💰 Total", $"{wallet.TotalMoney:N0} {_economyService.CurrencyName}", inline: true)
                .AddField("📊 Statistics", 
                    $"Total Earned: {wallet.TotalEarned:N0}\n" +
                    $"Total Spent: {wallet.TotalSpent:N0}\n" +
                    $"Daily Streak: {wallet.DailyStreak} days")
                .WithThumbnailUrl(message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl())
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleDailyCommand(SocketUserMessage message, ulong guildId)
        {
            var (success, amount, streak) = await _economyService!.ClaimDailyAsync(guildId, message.Author.Id);

            if (!success)
            {
                var wallet = _economyService.GetWallet(guildId, message.Author.Id);
                var timeLeft = TimeSpan.FromHours(24) - (DateTime.UtcNow - wallet.LastDaily);
                await message.Channel.SendMessageAsync(
                    $"❌ You've already claimed your daily reward! Come back in {timeLeft.Hours}h {timeLeft.Minutes}m");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎁 Daily Reward Claimed!")
                .WithColor(Color.Gold)
                .WithDescription($"You received **{amount:N0}** {_economyService.CurrencyName}!")
                .AddField("🔥 Streak", $"{streak} day{(streak != 1 ? "s" : "")}", inline: true)
                .AddField("💰 Bonus", $"+{Math.Min(streak * 50, 500)} {_economyService.CurrencyName}", inline: true)
                .WithFooter("Claim every day to increase your streak bonus!")
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleWeeklyCommand(SocketUserMessage message, ulong guildId)
        {
            var (success, amount) = await _economyService!.ClaimWeeklyAsync(guildId, message.Author.Id);

            if (!success)
            {
                var wallet = _economyService.GetWallet(guildId, message.Author.Id);
                var timeLeft = TimeSpan.FromDays(7) - (DateTime.UtcNow - wallet.LastWeekly);
                await message.Channel.SendMessageAsync(
                    $"❌ You've already claimed your weekly reward! Come back in {timeLeft.Days}d {timeLeft.Hours}h");
                return;
            }

            await message.Channel.SendMessageAsync(
                $"🎉 You claimed your weekly reward and received **{amount:N0}** {_economyService.CurrencyName}!");
        }

        private async Task HandleWorkCommand(SocketUserMessage message, ulong guildId)
        {
            var (success, amount) = await _economyService!.WorkAsync(guildId, message.Author.Id);

            if (!success)
            {
                var wallet = _economyService.GetWallet(guildId, message.Author.Id);
                var timeLeft = TimeSpan.FromMinutes(_economyService.WorkCooldownMinutes) - (DateTime.UtcNow - wallet.LastWork);
                await message.Channel.SendMessageAsync(
                    $"❌ You're too tired to work! Rest for {timeLeft.Minutes}m {timeLeft.Seconds}s");
                return;
            }

            var jobs = new[]
            {
                "delivered pizza", "walked dogs", "washed cars", "mowed lawns",
                "fixed computers", "wrote code", "designed websites", "made coffee",
                "taught classes", "cleaned houses", "painted walls", "repaired bikes"
            };

            var random = new Random();
            var job = jobs[random.Next(jobs.Length)];

            await message.Channel.SendMessageAsync(
                $"💼 You {job} and earned **{amount:N0}** {_economyService.CurrencyName}!");
        }

        private async Task HandlePayCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 3 || message.MentionedUsers.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!pay @user <amount>`");
                return;
            }

            if (!long.TryParse(parts[2], out long amount) || amount <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid amount!");
                return;
            }

            var recipient = message.MentionedUsers.First();
            if (recipient.Id == message.Author.Id)
            {
                await message.Channel.SendMessageAsync("❌ You can't pay yourself!");
                return;
            }

            if (recipient.IsBot)
            {
                await message.Channel.SendMessageAsync("❌ You can't pay bots!");
                return;
            }

            var success = await _economyService!.TransferAsync(guildId, message.Author.Id, recipient.Id, amount);

            if (!success)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough money!");
                return;
            }

            await message.Channel.SendMessageAsync(
                $"✅ {message.Author.Mention} paid {recipient.Mention} **{amount:N0}** {_economyService.CurrencyName}!");
        }

        private async Task HandleDepositCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!deposit <amount>` or `!deposit all`");
                return;
            }

            long amount;
            if (parts[1].ToLower() == "all")
            {
                var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
                amount = wallet.Balance;
            }
            else if (!long.TryParse(parts[1], out amount) || amount <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid amount!");
                return;
            }

            var success = await _economyService!.DepositAsync(guildId, message.Author.Id, amount);

            if (!success)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough cash!");
                return;
            }

            await message.Channel.SendMessageAsync(
                $"🏦 Deposited **{amount:N0}** {_economyService.CurrencyName} to your bank!");
        }

        private async Task HandleWithdrawCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!withdraw <amount>` or `!withdraw all`");
                return;
            }

            long amount;
            if (parts[1].ToLower() == "all")
            {
                var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
                amount = wallet.BankBalance;
            }
            else if (!long.TryParse(parts[1], out amount) || amount <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid amount!");
                return;
            }

            var success = await _economyService!.WithdrawAsync(guildId, message.Author.Id, amount);

            if (!success)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough in your bank!");
                return;
            }

            await message.Channel.SendMessageAsync(
                $"💵 Withdrew **{amount:N0}** {_economyService.CurrencyName} from your bank!");
        }

        private async Task HandleRichestCommand(SocketUserMessage message, ulong guildId)
        {
            var leaderboard = _economyService!.GetLeaderboard(guildId, 10);

            if (leaderboard.Count == 0)
            {
                await message.Channel.SendMessageAsync("No one has any money yet!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"💰 Richest Users")
                .WithColor(Color.Gold);

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var wallet = leaderboard[i];
                var user = await Client!.GetUserAsync(wallet.UserId);
                var username = user?.Username ?? $"User {wallet.UserId}";

                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"#{i + 1}"
                };

                embed.AddField(
                    $"{medal} {username}",
                    $"**{wallet.TotalMoney:N0}** {_economyService.CurrencyName} (💵 {wallet.Balance:N0} | 🏦 {wallet.BankBalance:N0})",
                    inline: false
                );
            }

            embed.WithCurrentTimestamp();
            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleInventoryCommand(SocketUserMessage message, ulong guildId)
        {
            ulong targetUserId = message.Author.Id;
            string targetUsername = message.Author.Username;

            if (message.MentionedUsers.Count > 0)
            {
                var mentioned = message.MentionedUsers.First();
                targetUserId = mentioned.Id;
                targetUsername = mentioned.Username;
            }

            var wallet = _economyService!.GetWallet(guildId, targetUserId);

            if (wallet.Inventory.Count == 0)
            {
                await message.Channel.SendMessageAsync($"{targetUsername}'s inventory is empty!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"🎒 {targetUsername}'s Inventory")
                .WithColor(Color.Purple);

            foreach (var item in wallet.Inventory.OrderByDescending(x => x.Value))
            {
                embed.AddField(item.Key, $"x{item.Value}", inline: true);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}

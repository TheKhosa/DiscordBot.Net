using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Economy
{
    /// <summary>
    /// Service for managing virtual currency
    /// </summary>
    public class EconomyService
    {
        private readonly ConcurrentDictionary<string, UserWallet> _wallets = new();
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        // Economy settings
        public string CurrencySymbol { get; set; } = "💰";
        public string CurrencyName { get; set; } = "coins";
        public int DailyAmount { get; set; } = 500;
        public int WeeklyAmount { get; set; } = 3500;
        public int MinWorkAmount { get; set; } = 100;
        public int MaxWorkAmount { get; set; } = 500;
        public int WorkCooldownMinutes { get; set; } = 60;

        public EconomyService(string? dataDirectory = null)
        {
            _dataDirectory = dataDirectory ?? Path.Combine(AppContext.BaseDirectory, "EconomyData");
            Directory.CreateDirectory(_dataDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Get user wallet
        /// </summary>
        public UserWallet GetWallet(ulong guildId, ulong userId)
        {
            var key = $"{guildId}_{userId}";
            return _wallets.GetOrAdd(key, _ => LoadWallet(guildId, userId) ?? new UserWallet
            {
                UserId = userId,
                GuildId = guildId,
                Balance = 0,
                BankBalance = 0,
                LastDaily = DateTime.MinValue,
                LastWeekly = DateTime.MinValue,
                LastWork = DateTime.MinValue,
                DailyStreak = 0
            });
        }

        /// <summary>
        /// Add currency to user
        /// </summary>
        public async Task AddBalanceAsync(ulong guildId, ulong userId, long amount)
        {
            var wallet = GetWallet(guildId, userId);
            wallet.Balance += amount;
            wallet.TotalEarned += amount;
            await SaveWalletAsync(wallet);
        }

        /// <summary>
        /// Remove currency from user
        /// </summary>
        public async Task<bool> RemoveBalanceAsync(ulong guildId, ulong userId, long amount)
        {
            var wallet = GetWallet(guildId, userId);
            if (wallet.Balance < amount)
                return false;

            wallet.Balance -= amount;
            wallet.TotalSpent += amount;
            await SaveWalletAsync(wallet);
            return true;
        }

        /// <summary>
        /// Transfer currency between users
        /// </summary>
        public async Task<bool> TransferAsync(ulong guildId, ulong fromUserId, ulong toUserId, long amount)
        {
            if (amount <= 0) return false;

            var fromWallet = GetWallet(guildId, fromUserId);
            if (fromWallet.Balance < amount)
                return false;

            var toWallet = GetWallet(guildId, toUserId);

            fromWallet.Balance -= amount;
            toWallet.Balance += amount;

            await SaveWalletAsync(fromWallet);
            await SaveWalletAsync(toWallet);

            return true;
        }

        /// <summary>
        /// Deposit money to bank
        /// </summary>
        public async Task<bool> DepositAsync(ulong guildId, ulong userId, long amount)
        {
            var wallet = GetWallet(guildId, userId);
            if (wallet.Balance < amount)
                return false;

            wallet.Balance -= amount;
            wallet.BankBalance += amount;
            await SaveWalletAsync(wallet);
            return true;
        }

        /// <summary>
        /// Withdraw money from bank
        /// </summary>
        public async Task<bool> WithdrawAsync(ulong guildId, ulong userId, long amount)
        {
            var wallet = GetWallet(guildId, userId);
            if (wallet.BankBalance < amount)
                return false;

            wallet.BankBalance -= amount;
            wallet.Balance += amount;
            await SaveWalletAsync(wallet);
            return true;
        }

        /// <summary>
        /// Claim daily reward
        /// </summary>
        public async Task<(bool success, long amount, int streak)> ClaimDailyAsync(ulong guildId, ulong userId)
        {
            var wallet = GetWallet(guildId, userId);
            var now = DateTime.UtcNow;

            // Check if already claimed today
            if ((now - wallet.LastDaily).TotalHours < 24)
            {
                return (false, 0, wallet.DailyStreak);
            }

            // Check streak
            if ((now - wallet.LastDaily).TotalHours <= 48)
            {
                wallet.DailyStreak++;
            }
            else
            {
                wallet.DailyStreak = 1;
            }

            // Calculate bonus
            var bonus = Math.Min(wallet.DailyStreak * 50, 500); // Max 500 bonus
            var total = DailyAmount + bonus;

            wallet.Balance += total;
            wallet.TotalEarned += total;
            wallet.LastDaily = now;

            await SaveWalletAsync(wallet);
            return (true, total, wallet.DailyStreak);
        }

        /// <summary>
        /// Claim weekly reward
        /// </summary>
        public async Task<(bool success, long amount)> ClaimWeeklyAsync(ulong guildId, ulong userId)
        {
            var wallet = GetWallet(guildId, userId);
            var now = DateTime.UtcNow;

            // Check if already claimed this week
            if ((now - wallet.LastWeekly).TotalDays < 7)
            {
                return (false, 0);
            }

            wallet.Balance += WeeklyAmount;
            wallet.TotalEarned += WeeklyAmount;
            wallet.LastWeekly = now;

            await SaveWalletAsync(wallet);
            return (true, WeeklyAmount);
        }

        /// <summary>
        /// Work for money
        /// </summary>
        public async Task<(bool success, long amount)> WorkAsync(ulong guildId, ulong userId)
        {
            var wallet = GetWallet(guildId, userId);
            var now = DateTime.UtcNow;

            // Check cooldown
            if ((now - wallet.LastWork).TotalMinutes < WorkCooldownMinutes)
            {
                return (false, 0);
            }

            var random = new Random();
            var amount = random.Next(MinWorkAmount, MaxWorkAmount + 1);

            wallet.Balance += amount;
            wallet.TotalEarned += amount;
            wallet.LastWork = now;

            await SaveWalletAsync(wallet);
            return (true, amount);
        }

        /// <summary>
        /// Get richest users in guild
        /// </summary>
        public List<UserWallet> GetLeaderboard(ulong guildId, int limit = 10)
        {
            return _wallets.Values
                .Where(w => w.GuildId == guildId)
                .OrderByDescending(w => w.TotalMoney)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        public async Task AddItemAsync(ulong guildId, ulong userId, string itemName, int quantity = 1)
        {
            var wallet = GetWallet(guildId, userId);
            
            if (wallet.Inventory.ContainsKey(itemName))
            {
                wallet.Inventory[itemName] += quantity;
            }
            else
            {
                wallet.Inventory[itemName] = quantity;
            }

            await SaveWalletAsync(wallet);
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public async Task<bool> RemoveItemAsync(ulong guildId, ulong userId, string itemName, int quantity = 1)
        {
            var wallet = GetWallet(guildId, userId);

            if (!wallet.Inventory.ContainsKey(itemName) || wallet.Inventory[itemName] < quantity)
                return false;

            wallet.Inventory[itemName] -= quantity;
            if (wallet.Inventory[itemName] <= 0)
            {
                wallet.Inventory.Remove(itemName);
            }

            await SaveWalletAsync(wallet);
            return true;
        }

        private UserWallet? LoadWallet(ulong guildId, ulong userId)
        {
            try
            {
                var filePath = GetFilePath(guildId, userId);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<UserWallet>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading wallet {guildId}/{userId}: {ex.Message}");
                return null;
            }
        }

        private async Task SaveWalletAsync(UserWallet wallet)
        {
            try
            {
                var guildDir = Path.Combine(_dataDirectory, wallet.GuildId.ToString());
                Directory.CreateDirectory(guildDir);

                var filePath = GetFilePath(wallet.GuildId, wallet.UserId);
                var json = JsonSerializer.Serialize(wallet, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving wallet {wallet.GuildId}/{wallet.UserId}: {ex.Message}");
            }
        }

        private string GetFilePath(ulong guildId, ulong userId)
        {
            return Path.Combine(_dataDirectory, guildId.ToString(), $"{userId}.json");
        }
    }
}

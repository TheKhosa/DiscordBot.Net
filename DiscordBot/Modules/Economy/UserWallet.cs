using System;
using System.Collections.Generic;

namespace DiscordBot.Modules.Economy
{
    /// <summary>
    /// User's economy data
    /// </summary>
    public class UserWallet
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public long Balance { get; set; }
        public long BankBalance { get; set; }
        public Dictionary<string, int> Inventory { get; set; } = new();
        public DateTime LastDaily { get; set; }
        public DateTime LastWeekly { get; set; }
        public DateTime LastWork { get; set; }
        public int DailyStreak { get; set; }
        public long TotalEarned { get; set; }
        public long TotalSpent { get; set; }

        public long TotalMoney => Balance + BankBalance;
    }
}

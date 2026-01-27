using System;

namespace DiscordBot.Modules.Leveling
{
    /// <summary>
    /// User leveling data
    /// </summary>
    public class UserLevel
    {
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public long Experience { get; set; }
        public int Level { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int MessageCount { get; set; }
        public int VoiceMinutes { get; set; }

        /// <summary>
        /// Calculate XP needed for next level
        /// </summary>
        public long XpForNextLevel()
        {
            // Formula: 5 * (level ^ 2) + 50 * level + 100
            return 5 * (Level * Level) + 50 * Level + 100;
        }

        /// <summary>
        /// Calculate total XP needed to reach a specific level
        /// </summary>
        public static long XpForLevel(int level)
        {
            long total = 0;
            for (int i = 0; i < level; i++)
            {
                total += 5 * (i * i) + 50 * i + 100;
            }
            return total;
        }

        /// <summary>
        /// Get current level progress (0.0 to 1.0)
        /// </summary>
        public double GetProgress()
        {
            var currentLevelXp = XpForLevel(Level);
            var nextLevelXp = XpForLevel(Level + 1);
            var xpIntoLevel = Experience - currentLevelXp;
            var xpNeeded = nextLevelXp - currentLevelXp;
            return (double)xpIntoLevel / xpNeeded;
        }
    }
}

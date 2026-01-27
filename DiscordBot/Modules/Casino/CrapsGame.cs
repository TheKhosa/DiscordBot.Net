using System;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Craps dice game
    /// </summary>
    public class CrapsGame
    {
        public ulong PlayerId { get; set; }
        public ulong GuildId { get; set; }
        public long Bet { get; set; }
        public int? Point { get; set; }
        public CrapsState State { get; set; }
        public DateTime StartTime { get; set; }

        // Current roll
        public int Die1 { get; set; }
        public int Die2 { get; set; }
        public int Total => Die1 + Die2;

        /// <summary>
        /// Roll two dice
        /// </summary>
        public void RollDice()
        {
            var random = new Random();
            Die1 = random.Next(1, 7);
            Die2 = random.Next(1, 7);
        }

        /// <summary>
        /// Process come-out roll
        /// </summary>
        public CrapsResult ProcessComeOutRoll()
        {
            RollDice();

            return Total switch
            {
                7 or 11 => CrapsResult.Win,     // Natural win
                2 or 3 or 12 => CrapsResult.Lose, // Craps
                _ => CrapsResult.Point            // Establish point
            };
        }

        /// <summary>
        /// Process point roll
        /// </summary>
        public CrapsResult ProcessPointRoll()
        {
            RollDice();

            if (Total == Point)
                return CrapsResult.Win;     // Made the point
            else if (Total == 7)
                return CrapsResult.Lose;    // Seven out
            else
                return CrapsResult.Continue; // Keep rolling
        }

        /// <summary>
        /// Calculate payout
        /// </summary>
        public long CalculatePayout(CrapsResult result)
        {
            return result switch
            {
                CrapsResult.Win => Bet * 2,      // 1:1 payout (2x total)
                CrapsResult.Lose => 0,           // Lose bet
                _ => 0
            };
        }

        /// <summary>
        /// Get dice emoji representation
        /// </summary>
        public string GetDiceEmoji()
        {
            var die1Emoji = Die1 switch
            {
                1 => "⚀",
                2 => "⚁",
                3 => "⚂",
                4 => "⚃",
                5 => "⚄",
                6 => "⚅",
                _ => "?"
            };

            var die2Emoji = Die2 switch
            {
                1 => "⚀",
                2 => "⚁",
                3 => "⚂",
                4 => "⚃",
                5 => "⚄",
                6 => "⚅",
                _ => "?"
            };

            return $"{die1Emoji} {die2Emoji}";
        }
    }

    public enum CrapsState
    {
        ComeOut,    // First roll
        Point,      // Point established, trying to hit it
        Finished
    }

    public enum CrapsResult
    {
        Win,
        Lose,
        Point,      // Point established
        Continue    // Keep rolling for point
    }
}

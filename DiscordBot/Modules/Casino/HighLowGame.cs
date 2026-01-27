using System;
using System.Collections.Generic;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// High-Low card guessing game with streak multipliers
    /// </summary>
    public class HighLowGame
    {
        public string DeckId { get; set; } = "";
        public ulong PlayerId { get; set; }
        public ulong GuildId { get; set; }
        public long BaseBet { get; set; }
        public long CurrentBet { get; set; }
        public Card? CurrentCard { get; set; }
        public Card? PreviousCard { get; set; }
        public int Streak { get; set; } = 0;
        public DateTime StartTime { get; set; }
        public HighLowState State { get; set; }

        /// <summary>
        /// Calculate payout based on streak
        /// </summary>
        public long CalculatePayout()
        {
            if (Streak == 0) return 0;
            
            // Exponential multiplier for streaks: 1.8x, 2.5x, 3.5x, 5x, 7x, etc.
            double multiplier = Streak switch
            {
                1 => 1.8,
                2 => 2.5,
                3 => 3.5,
                4 => 5.0,
                5 => 7.0,
                6 => 10.0,
                7 => 15.0,
                8 => 25.0,
                >= 9 => 50.0,
                _ => 1.0
            };

            return (long)(BaseBet * multiplier);
        }

        /// <summary>
        /// Compare two cards for High-Low logic
        /// </summary>
        public static bool IsHigher(Card card1, Card card2)
        {
            return GetCardRank(card1) > GetCardRank(card2);
        }

        public static bool IsLower(Card card1, Card card2)
        {
            return GetCardRank(card1) < GetCardRank(card2);
        }

        /// <summary>
        /// Get numeric rank of card (2-14, Ace high)
        /// </summary>
        private static int GetCardRank(Card card)
        {
            return card.Value switch
            {
                "2" => 2,
                "3" => 3,
                "4" => 4,
                "5" => 5,
                "6" => 6,
                "7" => 7,
                "8" => 8,
                "9" => 9,
                "10" => 10,
                "JACK" => 11,
                "QUEEN" => 12,
                "KING" => 13,
                "ACE" => 14,
                _ => 0
            };
        }

        /// <summary>
        /// Get display string for multiplier
        /// </summary>
        public string GetMultiplierDisplay()
        {
            if (Streak == 0) return "1.8x";
            
            return Streak switch
            {
                1 => "2.5x",
                2 => "3.5x",
                3 => "5x",
                4 => "7x",
                5 => "10x",
                6 => "15x",
                7 => "25x",
                >= 8 => "50x",
                _ => "1.8x"
            };
        }
    }

    public enum HighLowState
    {
        WaitingForGuess,
        Finished
    }
}

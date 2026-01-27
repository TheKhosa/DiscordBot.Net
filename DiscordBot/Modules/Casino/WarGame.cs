using System;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Card War game - Player vs Dealer
    /// </summary>
    public class WarGame
    {
        public string DeckId { get; set; } = "";
        public ulong PlayerId { get; set; }
        public ulong GuildId { get; set; }
        public long Bet { get; set; }
        public Card? PlayerCard { get; set; }
        public Card? DealerCard { get; set; }
        public DateTime StartTime { get; set; }
        public WarState State { get; set; }

        /// <summary>
        /// Determine game result
        /// </summary>
        public WarResult DetermineResult()
        {
            if (PlayerCard == null || DealerCard == null)
                return WarResult.Invalid;

            int playerRank = GetCardRank(PlayerCard);
            int dealerRank = GetCardRank(DealerCard);

            if (playerRank > dealerRank)
                return WarResult.PlayerWin;
            else if (playerRank < dealerRank)
                return WarResult.DealerWin;
            else
                return WarResult.War;
        }

        /// <summary>
        /// Calculate payout
        /// </summary>
        public long CalculatePayout()
        {
            var result = DetermineResult();
            
            return result switch
            {
                WarResult.PlayerWin => Bet * 2,        // 1:1 payout
                WarResult.War => Bet,                  // Push (return bet)
                WarResult.DealerWin => 0,              // Lose bet
                _ => 0
            };
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
    }

    public enum WarState
    {
        Playing,
        Finished
    }

    public enum WarResult
    {
        Invalid,
        PlayerWin,
        DealerWin,
        War  // Tie
    }
}

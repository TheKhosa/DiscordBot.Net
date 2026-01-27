using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Blackjack game logic (Player vs Dealer)
    /// </summary>
    public class BlackjackGame
    {
        public string DeckId { get; set; } = "";
        public ulong PlayerId { get; set; }
        public ulong GuildId { get; set; }
        public long Bet { get; set; }
        public List<Card> PlayerHand { get; set; } = new();
        public List<Card> DealerHand { get; set; } = new();
        public BlackjackState State { get; set; } = BlackjackState.NotStarted;
        public DateTime StartTime { get; set; }
        public bool PlayerStanding { get; set; }

        /// <summary>
        /// Calculate hand value (handles Ace as 1 or 11)
        /// </summary>
        public static int CalculateHandValue(List<Card> hand)
        {
            int value = 0;
            int aces = 0;

            foreach (var card in hand)
            {
                if (card.Value == "ACE")
                {
                    aces++;
                    value += 11;
                }
                else
                {
                    value += DeckOfCardsAPI.GetCardValue(card, false);
                }
            }

            // Adjust for aces if busted
            while (value > 21 && aces > 0)
            {
                value -= 10;
                aces--;
            }

            return value;
        }

        /// <summary>
        /// Check if hand is blackjack (21 with 2 cards)
        /// </summary>
        public static bool IsBlackjack(List<Card> hand)
        {
            return hand.Count == 2 && CalculateHandValue(hand) == 21;
        }

        /// <summary>
        /// Check if hand is busted
        /// </summary>
        public static bool IsBusted(List<Card> hand)
        {
            return CalculateHandValue(hand) > 21;
        }

        /// <summary>
        /// Get hand display string
        /// </summary>
        public static string GetHandDisplay(List<Card> hand, bool hideFirstCard = false)
        {
            if (hideFirstCard && hand.Count > 0)
            {
                return $"[🎴 Hidden] {string.Join(" ", hand.Skip(1).Select(c => c.ToString()))}";
            }

            return string.Join(" ", hand.Select(c => c.ToString()));
        }

        /// <summary>
        /// Determine game result
        /// </summary>
        public BlackjackResult DetermineResult()
        {
            var playerValue = CalculateHandValue(PlayerHand);
            var dealerValue = CalculateHandValue(DealerHand);

            var playerBlackjack = IsBlackjack(PlayerHand);
            var dealerBlackjack = IsBlackjack(DealerHand);

            // Both blackjack = push
            if (playerBlackjack && dealerBlackjack)
                return BlackjackResult.Push;

            // Player blackjack = win 1.5x
            if (playerBlackjack)
                return BlackjackResult.BlackjackWin;

            // Dealer blackjack = lose
            if (dealerBlackjack)
                return BlackjackResult.DealerBlackjack;

            // Player busted = lose
            if (IsBusted(PlayerHand))
                return BlackjackResult.PlayerBusted;

            // Dealer busted = win
            if (IsBusted(DealerHand))
                return BlackjackResult.DealerBusted;

            // Compare values
            if (playerValue > dealerValue)
                return BlackjackResult.PlayerWin;
            
            if (playerValue < dealerValue)
                return BlackjackResult.DealerWin;

            return BlackjackResult.Push;
        }

        /// <summary>
        /// Calculate payout based on result
        /// </summary>
        public long CalculatePayout()
        {
            var result = DetermineResult();

            return result switch
            {
                BlackjackResult.BlackjackWin => (long)(Bet * 2.5), // 1.5x payout + original bet
                BlackjackResult.PlayerWin or BlackjackResult.DealerBusted => Bet * 2, // 1:1 payout + original bet
                BlackjackResult.Push => Bet, // Return bet
                _ => 0 // Lost
            };
        }
    }

    public enum BlackjackState
    {
        NotStarted,
        PlayerTurn,
        DealerTurn,
        Finished
    }

    public enum BlackjackResult
    {
        PlayerWin,
        DealerWin,
        Push,
        PlayerBusted,
        DealerBusted,
        BlackjackWin,
        DealerBlackjack
    }
}

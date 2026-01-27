using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Texas Hold'em Poker game (Player vs Player)
    /// </summary>
    public class PokerGame
    {
        public string GameId { get; set; } = "";
        public string DeckId { get; set; } = "";
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public long BuyIn { get; set; }
        public List<PokerPlayer> Players { get; set; } = new();
        public List<Card> CommunityCards { get; set; } = new();
        public PokerState State { get; set; } = PokerState.Waiting;
        public DateTime StartTime { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public long Pot { get; set; }
        public long CurrentBet { get; set; }
        public int DealerButtonIndex { get; set; }

        public PokerPlayer? CurrentPlayer => Players.Count > 0 && CurrentPlayerIndex < Players.Count 
            ? Players[CurrentPlayerIndex] 
            : null;

        /// <summary>
        /// Add player to game
        /// </summary>
        public bool AddPlayer(ulong userId, string username, long chips)
        {
            if (Players.Count >= 8) return false;
            if (Players.Any(p => p.UserId == userId)) return false;
            if (State != PokerState.Waiting) return false;

            Players.Add(new PokerPlayer
            {
                UserId = userId,
                Username = username,
                Chips = chips,
                Hand = new List<Card>()
            });

            return true;
        }

        /// <summary>
        /// Remove player from game
        /// </summary>
        public bool RemovePlayer(ulong userId)
        {
            if (State != PokerState.Waiting) return false;
            return Players.RemoveAll(p => p.UserId == userId) > 0;
        }

        /// <summary>
        /// Move to next player
        /// </summary>
        public void NextPlayer()
        {
            do
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            } while (Players[CurrentPlayerIndex].Folded && !AllButOneFolded());
        }

        /// <summary>
        /// Check if all but one player folded
        /// </summary>
        public bool AllButOneFolded()
        {
            return Players.Count(p => !p.Folded) <= 1;
        }

        /// <summary>
        /// Get winner (when all but one folded)
        /// </summary>
        public PokerPlayer? GetWinnerByFold()
        {
            return Players.FirstOrDefault(p => !p.Folded);
        }

        /// <summary>
        /// Check if betting round is complete
        /// </summary>
        public bool IsBettingRoundComplete()
        {
            var activePlayers = Players.Where(p => !p.Folded).ToList();
            if (activePlayers.Count <= 1) return true;

            // All active players have bet the same amount or are all-in
            return activePlayers.All(p => p.CurrentBet == CurrentBet || p.Chips == 0);
        }

        /// <summary>
        /// Simple poker hand evaluation (High Card to Straight Flush)
        /// </summary>
        public static (PokerHandRank rank, int highCard) EvaluateHand(List<Card> playerCards, List<Card> communityCards)
        {
            var allCards = playerCards.Concat(communityCards).ToList();
            
            // Group by value
            var valueGroups = allCards.GroupBy(c => c.Value).OrderByDescending(g => g.Count()).ToList();
            var suitGroups = allCards.GroupBy(c => c.Suit).OrderByDescending(g => g.Count()).ToList();

            // Check for flush
            bool hasFlush = suitGroups.Any(g => g.Count() >= 5);

            // Check for straight
            var values = allCards.Select(c => GetPokerCardValue(c.Value)).OrderByDescending(v => v).Distinct().ToList();
            bool hasStraight = CheckStraight(values);

            // Check combinations
            var topGroup = valueGroups.FirstOrDefault();
            var secondGroup = valueGroups.Skip(1).FirstOrDefault();

            if (hasFlush && hasStraight)
                return (PokerHandRank.StraightFlush, values.First());
            
            if (topGroup?.Count() == 4)
                return (PokerHandRank.FourOfAKind, GetPokerCardValue(topGroup.Key));
            
            if (topGroup?.Count() == 3 && secondGroup?.Count() >= 2)
                return (PokerHandRank.FullHouse, GetPokerCardValue(topGroup.Key));
            
            if (hasFlush)
                return (PokerHandRank.Flush, values.First());
            
            if (hasStraight)
                return (PokerHandRank.Straight, values.First());
            
            if (topGroup?.Count() == 3)
                return (PokerHandRank.ThreeOfAKind, GetPokerCardValue(topGroup.Key));
            
            if (topGroup?.Count() == 2 && secondGroup?.Count() == 2)
                return (PokerHandRank.TwoPair, GetPokerCardValue(topGroup.Key));
            
            if (topGroup?.Count() == 2)
                return (PokerHandRank.Pair, GetPokerCardValue(topGroup.Key));

            return (PokerHandRank.HighCard, values.First());
        }

        private static bool CheckStraight(List<int> values)
        {
            for (int i = 0; i <= values.Count - 5; i++)
            {
                if (values[i] - values[i + 4] == 4)
                    return true;
            }

            // Check for Ace-low straight (A-2-3-4-5)
            if (values.Contains(14) && values.Contains(2) && values.Contains(3) && values.Contains(4) && values.Contains(5))
                return true;

            return false;
        }

        private static int GetPokerCardValue(string value)
        {
            return value switch
            {
                "ACE" => 14,
                "KING" => 13,
                "QUEEN" => 12,
                "JACK" => 11,
                _ => int.TryParse(value, out int v) ? v : 0
            };
        }
    }

    public class PokerPlayer
    {
        public ulong UserId { get; set; }
        public string Username { get; set; } = "";
        public long Chips { get; set; }
        public List<Card> Hand { get; set; } = new();
        public long CurrentBet { get; set; }
        public bool Folded { get; set; }
        public bool IsAllIn { get; set; }
    }

    public enum PokerState
    {
        Waiting,
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown,
        Finished
    }

    public enum PokerHandRank
    {
        HighCard = 0,
        Pair = 1,
        TwoPair = 2,
        ThreeOfAKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourOfAKind = 7,
        StraightFlush = 8
    }
}

using System;
using System.Collections.Generic;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Roulette game with number, color, and special bets
    /// </summary>
    public class RouletteGame
    {
        public ulong PlayerId { get; set; }
        public ulong GuildId { get; set; }
        public List<RouletteBet> Bets { get; set; } = new();
        public int WinningNumber { get; set; }
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Spin the roulette wheel
        /// </summary>
        public static int Spin()
        {
            return new Random().Next(0, 37); // 0-36
        }

        /// <summary>
        /// Get color of a number
        /// </summary>
        public static RouletteColor GetNumberColor(int number)
        {
            if (number == 0) return RouletteColor.Green;

            var redNumbers = new[] { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
            return Array.Exists(redNumbers, n => n == number) ? RouletteColor.Red : RouletteColor.Black;
        }

        /// <summary>
        /// Calculate total payout for all winning bets
        /// </summary>
        public long CalculateTotalPayout()
        {
            long totalPayout = 0;

            foreach (var bet in Bets)
            {
                if (IsBetWinner(bet, WinningNumber))
                {
                    totalPayout += CalculateBetPayout(bet);
                }
            }

            return totalPayout;
        }

        /// <summary>
        /// Check if a bet wins
        /// </summary>
        private bool IsBetWinner(RouletteBet bet, int winningNumber)
        {
            return bet.BetType switch
            {
                RouletteBetType.Number => bet.Number == winningNumber,
                RouletteBetType.Red => GetNumberColor(winningNumber) == RouletteColor.Red,
                RouletteBetType.Black => GetNumberColor(winningNumber) == RouletteColor.Black,
                RouletteBetType.Odd => winningNumber > 0 && winningNumber % 2 == 1,
                RouletteBetType.Even => winningNumber > 0 && winningNumber % 2 == 0,
                RouletteBetType.Low => winningNumber >= 1 && winningNumber <= 18,
                RouletteBetType.High => winningNumber >= 19 && winningNumber <= 36,
                RouletteBetType.Dozen1 => winningNumber >= 1 && winningNumber <= 12,
                RouletteBetType.Dozen2 => winningNumber >= 13 && winningNumber <= 24,
                RouletteBetType.Dozen3 => winningNumber >= 25 && winningNumber <= 36,
                _ => false
            };
        }

        /// <summary>
        /// Calculate payout for a winning bet
        /// </summary>
        private long CalculateBetPayout(RouletteBet bet)
        {
            var multiplier = bet.BetType switch
            {
                RouletteBetType.Number => 36,      // 35:1 payout (36x total)
                RouletteBetType.Dozen1 => 3,       // 2:1 payout (3x total)
                RouletteBetType.Dozen2 => 3,       // 2:1 payout
                RouletteBetType.Dozen3 => 3,       // 2:1 payout
                _ => 2                             // Even money (2x total)
            };

            return bet.Amount * multiplier;
        }

        /// <summary>
        /// Get display string for bet type
        /// </summary>
        public static string GetBetTypeDisplay(RouletteBetType type)
        {
            return type switch
            {
                RouletteBetType.Number => "Number",
                RouletteBetType.Red => "Red",
                RouletteBetType.Black => "Black",
                RouletteBetType.Odd => "Odd",
                RouletteBetType.Even => "Even",
                RouletteBetType.Low => "Low (1-18)",
                RouletteBetType.High => "High (19-36)",
                RouletteBetType.Dozen1 => "1st Dozen (1-12)",
                RouletteBetType.Dozen2 => "2nd Dozen (13-24)",
                RouletteBetType.Dozen3 => "3rd Dozen (25-36)",
                _ => "Unknown"
            };
        }
    }

    public class RouletteBet
    {
        public RouletteBetType BetType { get; set; }
        public int Number { get; set; }
        public long Amount { get; set; }
    }

    public enum RouletteBetType
    {
        Number,     // 35:1
        Red,        // 1:1
        Black,      // 1:1
        Odd,        // 1:1
        Even,       // 1:1
        Low,        // 1:1 (1-18)
        High,       // 1:1 (19-36)
        Dozen1,     // 2:1 (1-12)
        Dozen2,     // 2:1 (13-24)
        Dozen3      // 2:1 (25-36)
    }

    public enum RouletteColor
    {
        Red,
        Black,
        Green
    }
}

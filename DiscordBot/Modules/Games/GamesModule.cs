using Discord;
using Discord.WebSocket;
using DiscordBot.Modules.Economy;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Games
{
    /// <summary>
    /// Module for games and gambling
    /// </summary>
    public class GamesModule : ModuleBase
    {
        public override string ModuleId => "games";
        public override string Name => "Games & Gambling";
        public override string Description => "Fun games including trivia, gambling, and mini-games";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private EconomyService? _economyService;
        private Random _random = new Random();

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

            if (content.StartsWith("!coinflip") || content.StartsWith("!cf"))
            {
                await HandleCoinflipCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!dice") || content.StartsWith("!roll"))
            {
                await HandleDiceCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!slots"))
            {
                await HandleSlotsCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!rps"))
            {
                await HandleRockPaperScissorsCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!8ball"))
            {
                await Handle8BallCommand(message);
                return true;
            }

            return false;
        }

        private async Task HandleCoinflipCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 3)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!coinflip <heads/tails> <bet>`");
                return;
            }

            var choice = parts[1].ToLower();
            if (choice != "heads" && choice != "tails" && choice != "h" && choice != "t")
            {
                await message.Channel.SendMessageAsync("❌ Choose heads or tails!");
                return;
            }

            if (!long.TryParse(parts[2], out long bet) || bet <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid bet amount!");
                return;
            }

            var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
            if (wallet.Balance < bet)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough money!");
                return;
            }

            // Flip coin
            var result = _random.Next(2) == 0 ? "heads" : "tails";
            var userChoice = choice.StartsWith("h") ? "heads" : "tails";
            var won = result == userChoice;

            if (won)
            {
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet);
                await message.Channel.SendMessageAsync(
                    $"🪙 The coin landed on **{result}**!\n" +
                    $"✅ You won **{bet:N0}** coins!");
            }
            else
            {
                await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);
                await message.Channel.SendMessageAsync(
                    $"🪙 The coin landed on **{result}**!\n" +
                    $"❌ You lost **{bet:N0}** coins!");
            }
        }

        private async Task HandleDiceCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                // Just roll a die
                var roll = _random.Next(1, 7);
                await message.Channel.SendMessageAsync($"🎲 You rolled a **{roll}**!");
                return;
            }

            if (!long.TryParse(parts[1], out long bet) || bet <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid bet amount!");
                return;
            }

            var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
            if (wallet.Balance < bet)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough money!");
                return;
            }

            var userRoll = _random.Next(1, 7);
            var botRoll = _random.Next(1, 7);

            string result;
            if (userRoll > botRoll)
            {
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet);
                result = $"✅ You won **{bet:N0}** coins!";
            }
            else if (userRoll < botRoll)
            {
                await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);
                result = $"❌ You lost **{bet:N0}** coins!";
            }
            else
            {
                result = "🤝 It's a tie! No one wins!";
            }

            await message.Channel.SendMessageAsync(
                $"🎲 **Dice Battle!**\n" +
                $"You rolled: **{userRoll}**\n" +
                $"Bot rolled: **{botRoll}**\n" +
                $"{result}");
        }

        private async Task HandleSlotsCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!slots <bet>`");
                return;
            }

            if (!long.TryParse(parts[1], out long bet) || bet <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid bet amount!");
                return;
            }

            var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
            if (wallet.Balance < bet)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough money!");
                return;
            }

            var symbols = new[] { "🍒", "🍋", "🍊", "🍇", "7️⃣", "💎" };
            var slot1 = symbols[_random.Next(symbols.Length)];
            var slot2 = symbols[_random.Next(symbols.Length)];
            var slot3 = symbols[_random.Next(symbols.Length)];

            long winnings = 0;
            string resultText;

            if (slot1 == slot2 && slot2 == slot3)
            {
                // Three of a kind
                winnings = slot1 == "💎" ? bet * 10 : slot1 == "7️⃣" ? bet * 5 : bet * 3;
                resultText = $"🎰 **JACKPOT!** 🎰\nYou won **{winnings:N0}** coins!";
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, winnings);
            }
            else if (slot1 == slot2 || slot2 == slot3 || slot1 == slot3)
            {
                // Two of a kind
                winnings = bet;
                resultText = $"✅ Two matching! You won **{winnings:N0}** coins!";
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, winnings);
            }
            else
            {
                // No match
                await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);
                resultText = $"❌ No match! You lost **{bet:N0}** coins!";
            }

            await message.Channel.SendMessageAsync(
                $"🎰 **Slots** 🎰\n" +
                $"[ {slot1} | {slot2} | {slot3} ]\n" +
                $"{resultText}");
        }

        private async Task HandleRockPaperScissorsCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!rps <rock/paper/scissors> [bet]`");
                return;
            }

            var choice = parts[1].ToLower();
            if (choice != "rock" && choice != "paper" && choice != "scissors" &&
                choice != "r" && choice != "p" && choice != "s")
            {
                await message.Channel.SendMessageAsync("❌ Choose rock, paper, or scissors!");
                return;
            }

            long bet = 0;
            if (parts.Length >= 3)
            {
                if (!long.TryParse(parts[2], out bet) || bet <= 0)
                {
                    await message.Channel.SendMessageAsync("❌ Invalid bet amount!");
                    return;
                }

                var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
                if (wallet.Balance < bet)
                {
                    await message.Channel.SendMessageAsync("❌ You don't have enough money!");
                    return;
                }
            }

            var choices = new[] { "rock", "paper", "scissors" };
            var userChoice = choice[0] switch
            {
                'r' => "rock",
                'p' => "paper",
                's' => "scissors",
                _ => choice
            };
            var botChoice = choices[_random.Next(3)];

            var emojis = new System.Collections.Generic.Dictionary<string, string>
            {
                { "rock", "🪨" },
                { "paper", "📄" },
                { "scissors", "✂️" }
            };

            string result;
            if (userChoice == botChoice)
            {
                result = "🤝 It's a tie!";
            }
            else if ((userChoice == "rock" && botChoice == "scissors") ||
                     (userChoice == "paper" && botChoice == "rock") ||
                     (userChoice == "scissors" && botChoice == "paper"))
            {
                result = bet > 0 ? $"✅ You win **{bet:N0}** coins!" : "✅ You win!";
                if (bet > 0)
                {
                    await _economyService!.AddBalanceAsync(guildId, message.Author.Id, bet);
                }
            }
            else
            {
                result = bet > 0 ? $"❌ You lose **{bet:N0}** coins!" : "❌ You lose!";
                if (bet > 0)
                {
                    await _economyService!.RemoveBalanceAsync(guildId, message.Author.Id, bet);
                }
            }

            await message.Channel.SendMessageAsync(
                $"**Rock Paper Scissors!**\n" +
                $"You chose: {emojis[userChoice]} **{userChoice}**\n" +
                $"Bot chose: {emojis[botChoice]} **{botChoice}**\n" +
                $"{result}");
        }

        private async Task Handle8BallCommand(SocketUserMessage message)
        {
            var responses = new[]
            {
                "It is certain.", "It is decidedly so.", "Without a doubt.",
                "Yes definitely.", "You may rely on it.", "As I see it, yes.",
                "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.",
                "Reply hazy, try again.", "Ask again later.", "Better not tell you now.",
                "Cannot predict now.", "Concentrate and ask again.",
                "Don't count on it.", "My reply is no.", "My sources say no.",
                "Outlook not so good.", "Very doubtful."
            };

            var response = responses[_random.Next(responses.Length)];
            
            await message.Channel.SendMessageAsync($"🎱 **Magic 8-Ball says:** {response}");
        }
    }
}

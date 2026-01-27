using Discord;
using Discord.WebSocket;
using DiscordBot.Modules.Economy;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Casino
{
    /// <summary>
    /// Casino module with card games using DeckOfCardsAPI
    /// </summary>
    public class CasinoModule : ModuleBase
    {
        public override string ModuleId => "casino";
        public override string Name => "Casino & Card Games";
        public override string Description => "Blackjack, Poker, and other card games with temporary tables";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private DeckOfCardsAPI? _deckAPI;
        private EconomyService? _economyService;
        private readonly ConcurrentDictionary<ulong, BlackjackGame> _blackjackGames = new();
        private readonly ConcurrentDictionary<string, PokerGame> _pokerGames = new();
        private readonly ConcurrentDictionary<ulong, HighLowGame> _highLowGames = new();
        private readonly ConcurrentDictionary<ulong, RouletteGame> _rouletteGames = new();
        private readonly ConcurrentDictionary<ulong, CrapsGame> _crapsGames = new();
        private readonly ConcurrentDictionary<ulong, ulong> _tempChannels = new(); // ChannelId -> GuildId

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _deckAPI = new DeckOfCardsAPI(new HttpClient());
            _economyService = new EconomyService();
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            // Blackjack commands
            if (content.StartsWith("!blackjack") || content.StartsWith("!bj"))
            {
                await HandleBlackjackCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!hit"))
            {
                await HandleHitCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!stand"))
            {
                await HandleStandCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!doubledown") || content.StartsWith("!dd"))
            {
                await HandleDoubleDownCommand(message, guildId);
                return true;
            }

            // Poker commands
            if (content.StartsWith("!poker"))
            {
                await HandlePokerCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!joinpoker"))
            {
                await HandleJoinPokerCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!startpoker"))
            {
                await HandleStartPokerCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!fold"))
            {
                await HandleFoldCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!call"))
            {
                await HandleCallCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!raise"))
            {
                await HandleRaiseCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!check"))
            {
                await HandleCheckCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!leavetable"))
            {
                await HandleLeaveTableCommand(message);
                return true;
            }

            // High-Low commands
            if (content.StartsWith("!highlow") || content.StartsWith("!hl"))
            {
                await HandleHighLowCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!higher") || content.StartsWith("!hi"))
            {
                await HandleHigherCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!lower") || content.StartsWith("!lo"))
            {
                await HandleLowerCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!cashout"))
            {
                await HandleCashoutCommand(message, guildId);
                return true;
            }

            // War command
            if (content.StartsWith("!war"))
            {
                await HandleWarCommand(message, guildId);
                return true;
            }

            // Roulette commands
            if (content.StartsWith("!roulette") || content.StartsWith("!roul"))
            {
                await HandleRouletteCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!spin"))
            {
                await HandleSpinCommand(message, guildId);
                return true;
            }

            // Craps commands
            if (content.StartsWith("!craps"))
            {
                await HandleCrapsCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!roll"))
            {
                await HandleRollCommand(message, guildId);
                return true;
            }

            return false;
        }

        #region Blackjack Commands

        private async Task HandleBlackjackCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!blackjack <bet>`");
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
                await message.Channel.SendMessageAsync("❌ You don't have enough coins!");
                return;
            }

            if (_blackjackGames.TryGetValue(message.Author.Id, out var existingGame))
            {
                await message.Channel.SendMessageAsync($"❌ You already have an active blackjack game with deck {existingGame.DeckId}! Use `!hit` or `!stand`");
                return;
            }

            // Remove bet from wallet
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);

            // Create NEW shuffled deck for this game
            var deckId = await _deckAPI!.CreateNewDeckAsync();
            if (deckId == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to create deck. Try again!");
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet); // Refund
                return;
            }

            Console.WriteLine($"[Blackjack] Created new deck {deckId} for player {message.Author.Username}");

            // Deal initial cards from the fresh deck
            var cards = await _deckAPI.DrawCardsAsync(deckId, 4);
            if (cards == null || cards.Count != 4)
            {
                await message.Channel.SendMessageAsync("❌ Failed to deal cards. Try again!");
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet); // Refund
                return;
            }

            var game = new BlackjackGame
            {
                DeckId = deckId,
                PlayerId = message.Author.Id,
                GuildId = guildId,
                Bet = bet,
                PlayerHand = new() { cards[0], cards[1] },
                DealerHand = new() { cards[2], cards[3] },
                State = BlackjackState.PlayerTurn,
                StartTime = DateTime.UtcNow
            };

            _blackjackGames[message.Author.Id] = game;

            // Check for instant blackjack
            if (BlackjackGame.IsBlackjack(game.PlayerHand))
            {
                await FinishBlackjackGame(message, game);
                return;
            }

            await DisplayBlackjackGame(message.Channel, game, false);
        }

        private async Task HandleHitCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_blackjackGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ You don't have an active blackjack game!");
                return;
            }

            if (game.State != BlackjackState.PlayerTurn)
            {
                await message.Channel.SendMessageAsync("❌ It's not your turn!");
                return;
            }

            // Draw a card
            var cards = await _deckAPI!.DrawCardsAsync(game.DeckId, 1);
            if (cards == null || cards.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Failed to draw card!");
                return;
            }

            game.PlayerHand.Add(cards[0]);

            // Check if busted
            if (BlackjackGame.IsBusted(game.PlayerHand))
            {
                await FinishBlackjackGame(message, game);
                return;
            }

            // Check if 21
            if (BlackjackGame.CalculateHandValue(game.PlayerHand) == 21)
            {
                game.PlayerStanding = true;
                await FinishBlackjackGame(message, game);
                return;
            }

            await DisplayBlackjackGame(message.Channel, game, false);
        }

        private async Task HandleStandCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_blackjackGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ You don't have an active blackjack game!");
                return;
            }

            if (game.State != BlackjackState.PlayerTurn)
            {
                await message.Channel.SendMessageAsync("❌ It's not your turn!");
                return;
            }

            game.PlayerStanding = true;
            await FinishBlackjackGame(message, game);
        }

        private async Task HandleDoubleDownCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_blackjackGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ You don't have an active blackjack game!");
                return;
            }

            if (game.PlayerHand.Count != 2)
            {
                await message.Channel.SendMessageAsync("❌ You can only double down on your first two cards!");
                return;
            }

            var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
            if (wallet.Balance < game.Bet)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough coins to double down!");
                return;
            }

            // Double the bet
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, game.Bet);
            game.Bet *= 2;

            // Draw one card
            var cards = await _deckAPI!.DrawCardsAsync(game.DeckId, 1);
            if (cards == null || cards.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Failed to draw card!");
                return;
            }

            game.PlayerHand.Add(cards[0]);
            game.PlayerStanding = true;

            await FinishBlackjackGame(message, game);
        }

        private async Task FinishBlackjackGame(SocketUserMessage message, BlackjackGame game)
        {
            game.State = BlackjackState.DealerTurn;

            // Dealer plays (hits until 17 or higher)
            while (BlackjackGame.CalculateHandValue(game.DealerHand) < 17 && !BlackjackGame.IsBusted(game.PlayerHand))
            {
                var cards = await _deckAPI!.DrawCardsAsync(game.DeckId, 1);
                if (cards != null && cards.Count > 0)
                {
                    game.DealerHand.Add(cards[0]);
                }
            }

            game.State = BlackjackState.Finished;

            // Calculate result
            var payout = game.CalculatePayout();
            if (payout > 0)
            {
                await _economyService!.AddBalanceAsync(game.GuildId, game.PlayerId, payout);
            }

            await DisplayBlackjackGame(message.Channel, game, true);

            // Remove game and discard old deck
            _blackjackGames.TryRemove(message.Author.Id, out _);
            Console.WriteLine($"[Blackjack] Game finished for {message.Author.Username}, deck {game.DeckId} discarded");
        }

        private async Task DisplayBlackjackGame(ISocketMessageChannel channel, BlackjackGame game, bool showDealerCards)
        {
            var playerValue = BlackjackGame.CalculateHandValue(game.PlayerHand);
            var dealerValue = BlackjackGame.CalculateHandValue(game.DealerHand);

            var embed = new EmbedBuilder()
                .WithTitle("🎴 Blackjack")
                .WithColor(Color.Green);

            embed.AddField("Your Hand", 
                $"{BlackjackGame.GetHandDisplay(game.PlayerHand)}\n**Value:** {playerValue}", 
                inline: false);

            embed.AddField("Dealer's Hand", 
                $"{BlackjackGame.GetHandDisplay(game.DealerHand, !showDealerCards)}\n" +
                $"**Value:** {(showDealerCards ? dealerValue.ToString() : "?")}", 
                inline: false);

            embed.AddField("Bet", $"{game.Bet:N0} coins", inline: true);

            if (game.State == BlackjackState.Finished)
            {
                var result = game.DetermineResult();
                var payout = game.CalculatePayout();
                var profit = payout - game.Bet;

                var resultText = result switch
                {
                    BlackjackResult.BlackjackWin => $"🎉 BLACKJACK! You won {payout:N0} coins! (+{profit:N0})",
                    BlackjackResult.PlayerWin => $"✅ You win! You won {payout:N0} coins! (+{profit:N0})",
                    BlackjackResult.DealerBusted => $"✅ Dealer busted! You won {payout:N0} coins! (+{profit:N0})",
                    BlackjackResult.Push => $"🤝 Push! Your bet of {payout:N0} coins returned.",
                    BlackjackResult.DealerWin => $"❌ Dealer wins! You lost {game.Bet:N0} coins.",
                    BlackjackResult.PlayerBusted => $"💥 Busted! You lost {game.Bet:N0} coins.",
                    BlackjackResult.DealerBlackjack => $"❌ Dealer Blackjack! You lost {game.Bet:N0} coins.",
                    _ => "Game ended."
                };

                embed.WithDescription(resultText);
                embed.WithColor(profit > 0 ? Color.Gold : profit < 0 ? Color.Red : Color.Blue);
            }
            else
            {
                embed.WithDescription("Use `!hit` to draw a card, `!stand` to hold, or `!dd` to double down!");
            }

            await channel.SendMessageAsync(embed: embed.Build());
        }

        #endregion

        #region Poker Commands

        private async Task HandlePokerCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!poker <buy-in>`\n" +
                    "Creates a poker table with specified buy-in.\n" +
                    "Example: `!poker 1000`");
                return;
            }

            if (!long.TryParse(parts[1], out long buyIn) || buyIn <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid buy-in amount!");
                return;
            }

            var wallet = _economyService!.GetWallet(guild.Id, message.Author.Id);
            if (wallet.Balance < buyIn)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough coins!");
                return;
            }

            // Create temporary poker table channel
            var category = await guild.CreateCategoryChannelAsync($"🎰 Poker Table");
            var textChannel = await guild.CreateTextChannelAsync($"poker-table-{message.Author.Username}", props =>
            {
                props.CategoryId = category.Id;
            });

            _tempChannels[textChannel.Id] = guild.Id;

            // Create NEW shuffled deck for this poker game
            var deckId = await _deckAPI!.CreateNewDeckAsync();
            if (deckId == null)
            {
                await textChannel.DeleteAsync();
                await category.DeleteAsync();
                await message.Channel.SendMessageAsync("❌ Failed to create deck!");
                return;
            }

            var gameId = Guid.NewGuid().ToString().Substring(0, 8);
            Console.WriteLine($"[Poker] Created new deck {deckId} for game {gameId} by {message.Author.Username}");
            var pokerGame = new PokerGame
            {
                GameId = gameId,
                DeckId = deckId,
                GuildId = guild.Id,
                ChannelId = textChannel.Id,
                BuyIn = buyIn,
                State = PokerState.Waiting,
                StartTime = DateTime.UtcNow
            };

            // Creator joins automatically
            await _economyService.RemoveBalanceAsync(guild.Id, message.Author.Id, buyIn);
            pokerGame.AddPlayer(message.Author.Id, message.Author.Username, buyIn);

            _pokerGames[gameId] = pokerGame;

            var embed = new EmbedBuilder()
                .WithTitle($"🎰 Poker Table Created")
                .WithDescription(
                    $"Buy-in: **{buyIn:N0} coins**\n" +
                    $"Game ID: `{gameId}`\n" +
                    $"Channel: {textChannel.Mention}\n\n" +
                    $"Players can join with `!joinpoker {gameId}`\n" +
                    $"Start the game with `!startpoker` (need 2+ players)")
                .WithColor(Color.Gold)
                .AddField("Players", $"1. {message.Author.Username} ({buyIn:N0} chips)")
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
            await textChannel.SendMessageAsync($"Welcome to the poker table! Game ID: `{gameId}`");
        }

        private async Task HandleJoinPokerCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!joinpoker <game-id>`");
                return;
            }

            var gameId = parts[1];
            if (!_pokerGames.TryGetValue(gameId, out var game))
            {
                await message.Channel.SendMessageAsync("❌ Game not found!");
                return;
            }

            if (game.State != PokerState.Waiting)
            {
                await message.Channel.SendMessageAsync("❌ Game already started!");
                return;
            }

            var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
            if (wallet.Balance < game.BuyIn)
            {
                await message.Channel.SendMessageAsync($"❌ You need {game.BuyIn:N0} coins to join!");
                return;
            }

            // Remove buy-in and add player
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, game.BuyIn);
            
            if (!game.AddPlayer(message.Author.Id, message.Author.Username, game.BuyIn))
            {
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, game.BuyIn); // Refund
                await message.Channel.SendMessageAsync("❌ Failed to join game (table full or already joined)!");
                return;
            }

            var channel = Client!.GetChannel(game.ChannelId) as ITextChannel;
            var playerList = string.Join("\n", game.Players.Select((p, i) => $"{i + 1}. {p.Username} ({p.Chips:N0} chips)"));

            await channel!.SendMessageAsync($"✅ {message.Author.Mention} joined the table!\n\n**Players:**\n{playerList}");
        }

        private async Task HandleStartPokerCommand(SocketUserMessage message, ulong guildId)
        {
            var game = _pokerGames.Values.FirstOrDefault(g => g.ChannelId == message.Channel.Id);
            if (game == null)
            {
                await message.Channel.SendMessageAsync("❌ No poker game in this channel!");
                return;
            }

            if (game.Players.Count < 2)
            {
                await message.Channel.SendMessageAsync("❌ Need at least 2 players to start!");
                return;
            }

            if (game.State != PokerState.Waiting)
            {
                await message.Channel.SendMessageAsync("❌ Game already started!");
                return;
            }

            // Deal hole cards
            foreach (var player in game.Players)
            {
                var cards = await _deckAPI!.DrawCardsAsync(game.DeckId, 2);
                if (cards != null)
                {
                    player.Hand = cards;
                    
                    // DM player their cards
                    var user = await Client!.GetUserAsync(player.UserId);
                    try
                    {
                        await user.SendMessageAsync(
                            $"🎴 Your hole cards for Poker (Game {game.GameId}):\n" +
                            $"{string.Join(" ", cards.Select(c => c.ToString()))}");
                    }
                    catch
                    {
                        // User has DMs disabled
                    }
                }
            }

            game.State = PokerState.PreFlop;
            game.CurrentPlayerIndex = 0;

            await message.Channel.SendMessageAsync(
                $"🎰 **Poker game started!**\n\n" +
                $"Hole cards dealt. Check your DMs!\n" +
                $"Current player: **{game.CurrentPlayer!.Username}**\n\n" +
                $"Use `!call`, `!raise <amount>`, `!fold`, or `!check`");
        }

        private async Task HandleFoldCommand(SocketUserMessage message, ulong guildId)
        {
            var game = _pokerGames.Values.FirstOrDefault(g => g.ChannelId == message.Channel.Id);
            if (game == null || game.CurrentPlayer?.UserId != message.Author.Id) return;

            game.CurrentPlayer.Folded = true;
            await message.Channel.SendMessageAsync($"❌ **{message.Author.Username}** folded!");

            if (game.AllButOneFolded())
            {
                await EndPokerGame(game, message.Channel);
                return;
            }

            game.NextPlayer();
            await message.Channel.SendMessageAsync($"Current player: **{game.CurrentPlayer!.Username}**");
        }

        private async Task HandleCallCommand(SocketUserMessage message, ulong guildId)
        {
            var game = _pokerGames.Values.FirstOrDefault(g => g.ChannelId == message.Channel.Id);
            if (game == null || game.CurrentPlayer?.UserId != message.Author.Id) return;

            var toCall = game.CurrentBet - game.CurrentPlayer.CurrentBet;
            if (toCall > game.CurrentPlayer.Chips)
            {
                await message.Channel.SendMessageAsync("❌ Not enough chips! Use `!fold` instead.");
                return;
            }

            game.CurrentPlayer.Chips -= toCall;
            game.CurrentPlayer.CurrentBet += toCall;
            game.Pot += toCall;

            await message.Channel.SendMessageAsync($"✅ **{message.Author.Username}** called {toCall:N0} chips!");

            game.NextPlayer();
            await message.Channel.SendMessageAsync($"Current player: **{game.CurrentPlayer!.Username}**");
        }

        private async Task HandleRaiseCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !long.TryParse(parts[1], out long amount))
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!raise <amount>`");
                return;
            }

            var game = _pokerGames.Values.FirstOrDefault(g => g.ChannelId == message.Channel.Id);
            if (game == null || game.CurrentPlayer?.UserId != message.Author.Id) return;

            var totalBet = game.CurrentBet - game.CurrentPlayer.CurrentBet + amount;
            if (totalBet > game.CurrentPlayer.Chips)
            {
                await message.Channel.SendMessageAsync("❌ Not enough chips!");
                return;
            }

            game.CurrentPlayer.Chips -= totalBet;
            game.CurrentPlayer.CurrentBet += totalBet;
            game.CurrentBet = game.CurrentPlayer.CurrentBet;
            game.Pot += totalBet;

            await message.Channel.SendMessageAsync($"💰 **{message.Author.Username}** raised to {game.CurrentBet:N0} chips!");

            game.NextPlayer();
            await message.Channel.SendMessageAsync($"Current player: **{game.CurrentPlayer!.Username}**");
        }

        private async Task HandleCheckCommand(SocketUserMessage message, ulong guildId)
        {
            var game = _pokerGames.Values.FirstOrDefault(g => g.ChannelId == message.Channel.Id);
            if (game == null || game.CurrentPlayer?.UserId != message.Author.Id) return;

            if (game.CurrentPlayer.CurrentBet < game.CurrentBet)
            {
                await message.Channel.SendMessageAsync("❌ You must call or fold!");
                return;
            }

            await message.Channel.SendMessageAsync($"✅ **{message.Author.Username}** checked.");

            game.NextPlayer();
            await message.Channel.SendMessageAsync($"Current player: **{game.CurrentPlayer!.Username}**");
        }

        private async Task HandleLeaveTableCommand(SocketUserMessage message)
        {
            if (_tempChannels.TryRemove(message.Channel.Id, out var guildId))
            {
                var textChannel = message.Channel as SocketTextChannel;
                var category = textChannel?.Category;
                
                await message.Channel.SendMessageAsync("Closing table...");
                await Task.Delay(2000);
                
                if (textChannel != null)
                {
                    await textChannel.DeleteAsync();
                }
                if (category != null)
                {
                    await category.DeleteAsync();
                }
            }
        }

        private async Task EndPokerGame(PokerGame game, ISocketMessageChannel channel)
        {
            var winner = game.GetWinnerByFold();
            if (winner != null)
            {
                await _economyService!.AddBalanceAsync(game.GuildId, winner.UserId, game.Pot);
                await channel.SendMessageAsync(
                    $"🏆 **{winner.Username}** wins the pot of {game.Pot:N0} coins!\n\n" +
                    $"Use `!leavetable` to close this table.");
            }
        }

        #endregion

        #region High-Low Commands

        private async Task HandleHighLowCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!highlow <bet>` or `!hl <bet>`");
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
                await message.Channel.SendMessageAsync("❌ You don't have enough coins!");
                return;
            }

            if (_highLowGames.ContainsKey(message.Author.Id))
            {
                await message.Channel.SendMessageAsync("❌ You already have an active High-Low game! Use `!higher` or `!lower`");
                return;
            }

            // Remove bet from wallet
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);

            // Create NEW shuffled deck
            var deckId = await _deckAPI!.CreateNewDeckAsync();
            if (deckId == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to create deck. Try again!");
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet); // Refund
                return;
            }

            Console.WriteLine($"[HighLow] Created new deck {deckId} for player {message.Author.Username}");

            // Draw first card
            var cards = await _deckAPI.DrawCardsAsync(deckId, 1);
            if (cards == null || cards.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Failed to draw card. Try again!");
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet); // Refund
                return;
            }

            var game = new HighLowGame
            {
                DeckId = deckId,
                PlayerId = message.Author.Id,
                GuildId = guildId,
                BaseBet = bet,
                CurrentBet = bet,
                CurrentCard = cards[0],
                State = HighLowState.WaitingForGuess,
                StartTime = DateTime.UtcNow
            };

            _highLowGames[message.Author.Id] = game;

            await DisplayHighLowGame(message.Channel, game);
        }

        private async Task HandleHigherCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_highLowGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ You don't have an active High-Low game! Start one with `!highlow <bet>`");
                return;
            }

            await ProcessHighLowGuess(message, game, true);
        }

        private async Task HandleLowerCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_highLowGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ You don't have an active High-Low game! Start one with `!highlow <bet>`");
                return;
            }

            await ProcessHighLowGuess(message, game, false);
        }

        private async Task ProcessHighLowGuess(SocketUserMessage message, HighLowGame game, bool guessHigher)
        {
            // Draw next card
            var cards = await _deckAPI!.DrawCardsAsync(game.DeckId, 1);
            if (cards == null || cards.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Failed to draw card!");
                return;
            }

            var newCard = cards[0];
            game.PreviousCard = game.CurrentCard;
            game.CurrentCard = newCard;

            bool correct = guessHigher 
                ? HighLowGame.IsHigher(newCard, game.PreviousCard!) 
                : HighLowGame.IsLower(newCard, game.PreviousCard!);

            if (correct)
            {
                game.Streak++;
                
                var embed = new EmbedBuilder()
                    .WithTitle("📈 High-Low")
                    .WithColor(Color.Green)
                    .WithDescription($"✅ **Correct!** The card is {newCard}")
                    .AddField("Previous Card", game.PreviousCard!.ToString(), inline: true)
                    .AddField("New Card", newCard.ToString(), inline: true)
                    .AddField("Streak", $"🔥 {game.Streak} win{(game.Streak != 1 ? "s" : "")}", inline: true)
                    .AddField("Current Multiplier", game.GetMultiplierDisplay(), inline: true)
                    .AddField("Potential Payout", $"{game.CalculatePayout():N0} coins", inline: true)
                    .WithFooter("Use !higher or !lower to continue, or !cashout to collect your winnings!")
                    .Build();

                await message.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                // Lost - game over
                game.State = HighLowState.Finished;
                _highLowGames.TryRemove(message.Author.Id, out _);

                var embed = new EmbedBuilder()
                    .WithTitle("📉 High-Low")
                    .WithColor(Color.Red)
                    .WithDescription($"❌ **Wrong!** You guessed {(guessHigher ? "HIGHER" : "LOWER")}")
                    .AddField("Previous Card", game.PreviousCard!.ToString(), inline: true)
                    .AddField("New Card", newCard.ToString(), inline: true)
                    .AddField("Final Streak", $"{game.Streak} win{(game.Streak != 1 ? "s" : "")}", inline: true)
                    .AddField("Lost", $"{game.BaseBet:N0} coins", inline: true)
                    .WithFooter("Better luck next time!")
                    .Build();

                await message.Channel.SendMessageAsync(embed: embed);
                Console.WriteLine($"[HighLow] Game finished for {message.Author.Username}, deck {game.DeckId} discarded");
            }
        }

        private async Task HandleCashoutCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_highLowGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ You don't have an active High-Low game!");
                return;
            }

            if (game.Streak == 0)
            {
                await message.Channel.SendMessageAsync("❌ You need at least one win to cash out!");
                return;
            }

            var payout = game.CalculatePayout();
            await _economyService!.AddBalanceAsync(guildId, message.Author.Id, payout);

            game.State = HighLowState.Finished;
            _highLowGames.TryRemove(message.Author.Id, out _);

            var embed = new EmbedBuilder()
                .WithTitle("💰 Cashed Out!")
                .WithColor(Color.Gold)
                .WithDescription($"You collected your winnings!")
                .AddField("Streak", $"🔥 {game.Streak} win{(game.Streak != 1 ? "s" : "")}", inline: true)
                .AddField("Multiplier", game.GetMultiplierDisplay(), inline: true)
                .AddField("Payout", $"**{payout:N0}** coins", inline: true)
                .AddField("Profit", $"+{payout - game.BaseBet:N0} coins", inline: true)
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
            Console.WriteLine($"[HighLow] Player {message.Author.Username} cashed out {payout:N0} coins (streak: {game.Streak})");
        }

        private async Task DisplayHighLowGame(ISocketMessageChannel channel, HighLowGame game)
        {
            var embed = new EmbedBuilder()
                .WithTitle("📊 High-Low")
                .WithColor(Color.Blue)
                .WithDescription($"Current Card: **{game.CurrentCard}**")
                .AddField("Base Bet", $"{game.BaseBet:N0} coins", inline: true)
                .AddField("Streak", $"🔥 {game.Streak} win{(game.Streak != 1 ? "s" : "")}", inline: true)
                .AddField("Next Multiplier", game.GetMultiplierDisplay(), inline: true)
                .WithFooter("Will the next card be higher or lower? Use !higher or !lower")
                .Build();

            await channel.SendMessageAsync(embed: embed);
        }

        #endregion

        #region War Commands

        private async Task HandleWarCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!war <bet>`");
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
                await message.Channel.SendMessageAsync("❌ You don't have enough coins!");
                return;
            }

            // Remove bet from wallet
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);

            // Create NEW shuffled deck
            var deckId = await _deckAPI!.CreateNewDeckAsync();
            if (deckId == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to create deck. Try again!");
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet); // Refund
                return;
            }

            Console.WriteLine($"[War] Created new deck {deckId} for player {message.Author.Username}");

            // Draw 2 cards (player and dealer)
            var cards = await _deckAPI.DrawCardsAsync(deckId, 2);
            if (cards == null || cards.Count != 2)
            {
                await message.Channel.SendMessageAsync("❌ Failed to deal cards. Try again!");
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, bet); // Refund
                return;
            }

            var game = new WarGame
            {
                DeckId = deckId,
                PlayerId = message.Author.Id,
                GuildId = guildId,
                Bet = bet,
                PlayerCard = cards[0],
                DealerCard = cards[1],
                State = WarState.Finished,
                StartTime = DateTime.UtcNow
            };

            // Determine result
            var result = game.DetermineResult();
            var payout = game.CalculatePayout();

            if (payout > 0)
            {
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, payout);
            }

            // Display result
            var embed = new EmbedBuilder()
                .WithTitle("⚔️ WAR")
                .AddField("Your Card", cards[0].ToString(), inline: true)
                .AddField("Dealer's Card", cards[1].ToString(), inline: true)
                .AddField("Bet", $"{bet:N0} coins", inline: true);

            var profit = payout - bet;
            switch (result)
            {
                case WarResult.PlayerWin:
                    embed.WithColor(Color.Green);
                    embed.WithDescription($"✅ **YOU WIN!**\n\nYou won **{payout:N0}** coins! (+{profit:N0})");
                    break;
                case WarResult.DealerWin:
                    embed.WithColor(Color.Red);
                    embed.WithDescription($"❌ **DEALER WINS**\n\nYou lost {bet:N0} coins.");
                    break;
                case WarResult.War:
                    embed.WithColor(Color.Blue);
                    embed.WithDescription($"🤝 **TIE!**\n\nYour bet of {payout:N0} coins returned.");
                    break;
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
            Console.WriteLine($"[War] Game finished for {message.Author.Username}, result: {result}, deck {deckId} discarded");
        }

        #endregion

        #region Roulette Commands

        private async Task HandleRouletteCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 3)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!roulette <bet-type> <amount>`\n\n" +
                    "**Bet Types:**\n" +
                    "`red`, `black` - Even money (2x)\n" +
                    "`odd`, `even` - Even money (2x)\n" +
                    "`low`, `high` - Even money (2x)\n" +
                    "`1-12`, `13-24`, `25-36` - Dozens (3x)\n" +
                    "`0-36` - Straight number (36x)\n\n" +
                    "**Examples:**\n" +
                    "`!roulette red 100`\n" +
                    "`!roulette 17 50`\n" +
                    "`!roulette 1-12 200`");
                return;
            }

            var betTypeStr = parts[1].ToLower();
            if (!long.TryParse(parts[2], out long amount) || amount <= 0)
            {
                await message.Channel.SendMessageAsync("❌ Invalid bet amount!");
                return;
            }

            var wallet = _economyService!.GetWallet(guildId, message.Author.Id);
            if (wallet.Balance < amount)
            {
                await message.Channel.SendMessageAsync("❌ You don't have enough coins!");
                return;
            }

            // Parse bet type
            RouletteBet? bet = ParseRouletteBet(betTypeStr, amount);
            if (bet == null)
            {
                await message.Channel.SendMessageAsync("❌ Invalid bet type! Use `!roulette` to see valid bets.");
                return;
            }

            // Remove bet from wallet
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, amount);

            // Create game and add bet
            var game = new RouletteGame
            {
                PlayerId = message.Author.Id,
                GuildId = guildId,
                StartTime = DateTime.UtcNow
            };
            game.Bets.Add(bet);

            _rouletteGames[message.Author.Id] = game;

            await message.Channel.SendMessageAsync(
                $"🎰 Bet placed: **{RouletteGame.GetBetTypeDisplay(bet.BetType)}** for **{amount:N0}** coins\n" +
                $"Use `!spin` to spin the wheel!");
        }

        private async Task HandleSpinCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_rouletteGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ No active roulette game! Start one with `!roulette <bet-type> <amount>`");
                return;
            }

            // Spin the wheel
            game.WinningNumber = RouletteGame.Spin();
            var color = RouletteGame.GetNumberColor(game.WinningNumber);

            // Calculate results
            var totalBet = game.Bets.Sum(b => b.Amount);
            var totalPayout = game.CalculateTotalPayout();
            var profit = totalPayout - totalBet;

            // Pay out winnings
            if (totalPayout > 0)
            {
                await _economyService!.AddBalanceAsync(guildId, message.Author.Id, totalPayout);
            }

            // Display result
            var colorEmoji = color switch
            {
                RouletteColor.Red => "🔴",
                RouletteColor.Black => "⚫",
                RouletteColor.Green => "🟢",
                _ => "⚪"
            };

            var embed = new EmbedBuilder()
                .WithTitle("🎰 Roulette")
                .WithDescription($"The ball lands on...\n\n# {colorEmoji} **{game.WinningNumber}** {colorEmoji}")
                .AddField("Total Bet", $"{totalBet:N0} coins", inline: true);

            if (totalPayout > 0)
            {
                embed.WithColor(Color.Gold);
                embed.AddField("Payout", $"**{totalPayout:N0}** coins", inline: true);
                embed.AddField("Profit", $"+{profit:N0} coins", inline: true);
                
                var winningBets = game.Bets.Where(b => 
                    (b.BetType == RouletteBetType.Number && b.Number == game.WinningNumber) ||
                    (b.BetType == RouletteBetType.Red && color == RouletteColor.Red) ||
                    (b.BetType == RouletteBetType.Black && color == RouletteColor.Black) ||
                    (b.BetType == RouletteBetType.Odd && game.WinningNumber > 0 && game.WinningNumber % 2 == 1) ||
                    (b.BetType == RouletteBetType.Even && game.WinningNumber > 0 && game.WinningNumber % 2 == 0) ||
                    (b.BetType == RouletteBetType.Low && game.WinningNumber >= 1 && game.WinningNumber <= 18) ||
                    (b.BetType == RouletteBetType.High && game.WinningNumber >= 19 && game.WinningNumber <= 36) ||
                    (b.BetType == RouletteBetType.Dozen1 && game.WinningNumber >= 1 && game.WinningNumber <= 12) ||
                    (b.BetType == RouletteBetType.Dozen2 && game.WinningNumber >= 13 && game.WinningNumber <= 24) ||
                    (b.BetType == RouletteBetType.Dozen3 && game.WinningNumber >= 25 && game.WinningNumber <= 36)
                ).ToList();

                if (winningBets.Any())
                {
                    var winningBetsList = string.Join("\n", winningBets.Select(b => 
                        $"✅ {RouletteGame.GetBetTypeDisplay(b.BetType)}"));
                    embed.AddField("Winning Bets", winningBetsList, inline: false);
                }
            }
            else
            {
                embed.WithColor(Color.Red);
                embed.AddField("Result", $"Lost {totalBet:N0} coins", inline: true);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());

            // Remove game
            _rouletteGames.TryRemove(message.Author.Id, out _);
            Console.WriteLine($"[Roulette] Game finished for {message.Author.Username}, landed on {game.WinningNumber}");
        }

        private RouletteBet? ParseRouletteBet(string betType, long amount)
        {
            return betType switch
            {
                "red" => new RouletteBet { BetType = RouletteBetType.Red, Amount = amount },
                "black" => new RouletteBet { BetType = RouletteBetType.Black, Amount = amount },
                "odd" => new RouletteBet { BetType = RouletteBetType.Odd, Amount = amount },
                "even" => new RouletteBet { BetType = RouletteBetType.Even, Amount = amount },
                "low" => new RouletteBet { BetType = RouletteBetType.Low, Amount = amount },
                "high" => new RouletteBet { BetType = RouletteBetType.High, Amount = amount },
                "1-12" => new RouletteBet { BetType = RouletteBetType.Dozen1, Amount = amount },
                "13-24" => new RouletteBet { BetType = RouletteBetType.Dozen2, Amount = amount },
                "25-36" => new RouletteBet { BetType = RouletteBetType.Dozen3, Amount = amount },
                _ => int.TryParse(betType, out int num) && num >= 0 && num <= 36
                    ? new RouletteBet { BetType = RouletteBetType.Number, Number = num, Amount = amount }
                    : null
            };
        }

        #endregion

        #region Craps Commands

        private async Task HandleCrapsCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!craps <bet>`");
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
                await message.Channel.SendMessageAsync("❌ You don't have enough coins!");
                return;
            }

            if (_crapsGames.ContainsKey(message.Author.Id))
            {
                await message.Channel.SendMessageAsync("❌ You already have an active craps game! Use `!roll` to continue.");
                return;
            }

            // Remove bet from wallet
            await _economyService.RemoveBalanceAsync(guildId, message.Author.Id, bet);

            // Create game
            var game = new CrapsGame
            {
                PlayerId = message.Author.Id,
                GuildId = guildId,
                Bet = bet,
                State = CrapsState.ComeOut,
                StartTime = DateTime.UtcNow
            };

            // Come-out roll
            var result = game.ProcessComeOutRoll();

            _crapsGames[message.Author.Id] = game;

            var embed = new EmbedBuilder()
                .WithTitle("🎲 CRAPS - Come Out Roll")
                .AddField("Dice", game.GetDiceEmoji(), inline: true)
                .AddField("Total", game.Total.ToString(), inline: true)
                .AddField("Bet", $"{bet:N0} coins", inline: true);

            if (result == CrapsResult.Win)
            {
                // Natural win
                var payout = game.CalculatePayout(CrapsResult.Win);
                await _economyService.AddBalanceAsync(guildId, message.Author.Id, payout);
                
                embed.WithColor(Color.Green);
                embed.WithDescription($"🎉 **NATURAL!** You rolled {game.Total}!\n\nYou won **{payout:N0}** coins! (+{payout - bet:N0})");
                
                _crapsGames.TryRemove(message.Author.Id, out _);
            }
            else if (result == CrapsResult.Lose)
            {
                // Craps
                embed.WithColor(Color.Red);
                embed.WithDescription($"❌ **CRAPS!** You rolled {game.Total}!\n\nYou lost {bet:N0} coins.");
                
                _crapsGames.TryRemove(message.Author.Id, out _);
            }
            else
            {
                // Point established
                game.Point = game.Total;
                game.State = CrapsState.Point;
                
                embed.WithColor(Color.Blue);
                embed.WithDescription(
                    $"📍 **POINT ESTABLISHED: {game.Point}**\n\n" +
                    $"Roll the point ({game.Point}) to win!\n" +
                    $"Roll a 7 and you lose.\n\n" +
                    $"Use `!roll` to continue!");
                embed.AddField("Point", game.Point.ToString(), inline: true);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
            Console.WriteLine($"[Craps] Game started for {message.Author.Username}, come-out roll: {game.Total}");
        }

        private async Task HandleRollCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_crapsGames.TryGetValue(message.Author.Id, out var game))
            {
                await message.Channel.SendMessageAsync("❌ No active craps game! Start one with `!craps <bet>`");
                return;
            }

            if (game.State != CrapsState.Point)
            {
                await message.Channel.SendMessageAsync("❌ No active point to roll for!");
                return;
            }

            // Roll for point
            var result = game.ProcessPointRoll();

            var embed = new EmbedBuilder()
                .WithTitle("🎲 CRAPS - Point Roll")
                .AddField("Dice", game.GetDiceEmoji(), inline: true)
                .AddField("Total", game.Total.ToString(), inline: true)
                .AddField("Point", game.Point.ToString(), inline: true);

            if (result == CrapsResult.Win)
            {
                // Made the point
                var payout = game.CalculatePayout(CrapsResult.Win);
                await _economyService!.AddBalanceAsync(guildId, message.Author.Id, payout);
                
                embed.WithColor(Color.Green);
                embed.WithDescription($"🎉 **POINT MADE!** You rolled {game.Total}!\n\nYou won **{payout:N0}** coins! (+{payout - game.Bet:N0})");
                
                _crapsGames.TryRemove(message.Author.Id, out _);
            }
            else if (result == CrapsResult.Lose)
            {
                // Seven out
                embed.WithColor(Color.Red);
                embed.WithDescription($"❌ **SEVEN OUT!** You rolled 7 before the point.\n\nYou lost {game.Bet:N0} coins.");
                
                _crapsGames.TryRemove(message.Author.Id, out _);
            }
            else
            {
                // Keep rolling
                embed.WithColor(Color.Blue);
                embed.WithDescription($"Keep rolling! You need {game.Point} to win (rolled {game.Total}).\n\nUse `!roll` to continue!");
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
            
            if (result != CrapsResult.Continue)
            {
                Console.WriteLine($"[Craps] Game finished for {message.Author.Username}, result: {result}");
            }
        }

        #endregion
    }
}

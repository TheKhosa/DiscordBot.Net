# Casino Module - v1.2.0

## Overview
The Casino module adds real card games to the Discord bot using the DeckOfCardsAPI (https://deckofcardsapi.com/). Players can play Blackjack, High-Low, War, and Poker with real card shuffling and dealing, fully integrated with the Economy module.

## Features

### Roulette (Wheel of Fortune)
Classic European roulette with multiple betting options and authentic payouts.

**Commands:**
- `!roulette <bet-type> <amount>` or `!roul <bet-type> <amount>` - Place bet
- `!spin` - Spin the wheel

**Bet Types & Payouts:**
- **Even Money (2x):** `red`, `black`, `odd`, `even`, `low` (1-18), `high` (19-36)
- **Dozens (3x):** `1-12`, `13-24`, `25-36`
- **Straight Number (36x):** `0` through `36`

**Example:**
```
!roulette red 100
!spin

!roulette 17 50
!spin

!roulette 1-12 200
!spin
```

### Craps (Dice Game)
Traditional casino craps with come-out rolls and point system.

**Commands:**
- `!craps <bet>` - Start game (automatic come-out roll)
- `!roll` - Continue rolling for point

**Rules:**
- **Come-out roll:**
  - 7 or 11 = Natural win (instant)
  - 2, 3, or 12 = Craps (instant loss)
  - 4, 5, 6, 8, 9, 10 = Point established
- **Point rolls:**
  - Roll the point number = Win
  - Roll 7 = Seven out (lose)
  - Any other number = Keep rolling

**Example:**
```
!craps 500
!roll
!roll
```

### High-Low (Card Guessing with Streaks)
Fast-paced card guessing game where players predict if the next card is higher or lower. Build winning streaks for massive multipliers!

**Commands:**
- `!highlow <bet>` or `!hl <bet>` - Start a new High-Low game
- `!higher` or `!hi` - Guess the next card is higher
- `!lower` or `!lo` - Guess the next card is lower
- `!cashout` - Collect your winnings and end the game

**Multipliers:**
- 1 win: 1.8x
- 2 wins: 2.5x
- 3 wins: 3.5x
- 4 wins: 5x
- 5 wins: 7x
- 6 wins: 10x
- 7 wins: 15x
- 8 wins: 25x
- 9+ wins: 50x

**Example:**
```
!highlow 100
!higher
!higher
!lower
!cashout
```

### War (Quick Battle Game)
Classic card war game - draw against the dealer, highest card wins instantly!

**Commands:**
- `!war <bet>` - Play War (instant result)

**Rules:**
- Each player draws one card
- Highest card wins (Ace is high)
- Ties return your bet
- Wins pay 1:1 (2x your bet)

**Example:**
```
!war 500
```

### Blackjack (Player vs Dealer)
Classic Blackjack with proper rules and economy integration.

**Commands:**
- `!blackjack <bet>` or `!bj <bet>` - Start a new Blackjack game
- `!hit` - Draw another card
- `!stand` - Hold your current hand
- `!doubledown` or `!dd` - Double your bet and draw one final card

**Rules:**
- Dealer hits until 17 or higher
- Blackjack (natural 21) pays 1.5x the bet
- Win pays 2x the bet
- Push (tie) returns the bet
- Bust or dealer win loses the bet

**Example:**
```
!blackjack 500
!hit
!stand
```

### Poker (Player vs Player - Texas Hold'em)
Multi-player Texas Hold'em poker with temporary table channels.

**Commands:**
- `!poker <buy-in>` - Create a new poker table
- `!joinpoker <game-id>` - Join an existing table
- `!startpoker` - Start the game (requires 2+ players)
- `!fold` - Fold your hand
- `!call` - Match the current bet
- `!raise <amount>` - Raise the bet
- `!check` - Pass without betting (only if current bet = 0)
- `!leavetable` - Close the poker table channel

**Game Flow:**
1. Creator uses `!poker 1000` to create table
2. Others join with `!joinpoker <game-id>`
3. Creator starts with `!startpoker`
4. Players receive hole cards via DM
5. Betting rounds (pre-flop, flop, turn, river)
6. Winner takes the pot

**Example:**
```
!poker 1000
!joinpoker a1b2c3d4
!startpoker
!call
!raise 200
```

## Technical Details

### Files
- `DeckOfCardsAPI.cs` - API wrapper for card deck operations
- `RouletteGame.cs` - Roulette wheel logic with 10 bet types
- `CrapsGame.cs` - Craps dice game with come-out and point rolls
- `BlackjackGame.cs` - Blackjack game logic and state
- `HighLowGame.cs` - High-Low game logic and streak multipliers
- `WarGame.cs` - War game logic and instant results
- `PokerGame.cs` - Poker game logic and state
- `CasinoModule.cs` - Main module with all command handlers (1,300+ lines)

### Dependencies
- **Economy Module** - For betting and payouts
- **DeckOfCardsAPI** - Real card deck shuffling/dealing
- **Discord.Net** - Temporary channel creation for poker

### State Management
- Blackjack games stored per user ID
- Poker games stored per game ID
- Temporary channels tracked for cleanup

### Integration
The Casino module integrates with:
- **EconomyService** - Deducts bets and awards winnings
- **DeckOfCardsAPI** - Creates/manages card decks
- **Discord Channels** - Creates temporary poker table channels

## Testing

### Test Roulette
1. Ensure you have coins: `!balance`
2. Place bet: `!roulette red 100`
3. Spin wheel: `!spin`
4. Check winnings: `!balance`

### Test Craps
1. Ensure you have coins: `!balance`
2. Start game: `!craps 200`
3. If point established: `!roll`
4. Check winnings: `!balance`

### Test High-Low
1. Ensure you have coins: `!balance`
2. Start game: `!highlow 100`
3. Guess: `!higher` or `!lower`
4. Build streak and cash out: `!cashout`
5. Check winnings: `!balance`

### Test War
1. Ensure you have coins: `!balance`
2. Play: `!war 100` (instant result)
3. Check winnings: `!balance`

### Test Blackjack
1. Ensure you have coins: `!balance`
2. Start game: `!blackjack 100`
3. Play: `!hit` or `!stand`
4. Check winnings: `!balance`

### Test Poker
1. Create table: `!poker 500`
2. Join with another user: `!joinpoker <game-id>`
3. Start: `!startpoker`
4. Play rounds: `!call`, `!raise 100`, `!fold`
5. Close table: `!leavetable`

## Known Limitations
- Poker hand evaluation is basic (High Card to Straight Flush)
- No split or insurance in Blackjack
- Poker tables require manual cleanup with `!leavetable`
- Maximum 6 players per poker table

## Current Games
✅ **Roulette** - European roulette with 10 bet types (0-36, colors, odds/evens, dozens)
✅ **Craps** - Classic dice game with come-out rolls and point system
✅ **High-Low** - Card guessing with streak multipliers (up to 50x!)
✅ **War** - Instant card battle game
✅ **Blackjack** - Classic 21 with proper dealer rules
✅ **Poker** - Texas Hold'em with temporary tables

## Future Enhancements
- 🃏 Baccarat - Player vs Banker, closest to 9 wins
- 🎴 Three Card Poker - Bonus payouts for pairs+
- 🎰 Video Poker - Jacks or Better variant
- 🏆 Tournament mode - Compete with leaderboards
- 🧹 Auto-cleanup of abandoned poker tables
- 💰 Advanced poker features (side pots, all-in)
- 📊 Casino statistics and player history
- 🎁 Daily casino bonuses and VIP tiers

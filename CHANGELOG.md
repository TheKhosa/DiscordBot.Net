# Changelog

## [v1.2.0] - Casino Module Update

### Added - Casino Games (6 Total!)

- **Roulette** - Classic roulette wheel
  - Commands: `!roulette <type> <amount>`, `!roul`, `!spin`
  - Bet types: red/black, odd/even, low/high, dozens, numbers
  - Payouts: 2x (even money), 3x (dozens), 36x (straight number)
  - Full European roulette with 0-36

- **Craps** - Dice rolling casino game
  - Commands: `!craps <bet>`, `!roll`
  - Come-out roll and point system
  - Natural wins (7/11), craps (2/3/12), point rolls
  - Authentic casino craps rules

- **High-Low (Hi-Lo)** - Card guessing game with streak multipliers
  - Commands: `!highlow`, `!hl`, `!higher`, `!hi`, `!lower`, `!lo`, `!cashout`
  - Streak system with exponential multipliers (1.8x to 50x)
  - Risk/reward gameplay - cash out early or push for huge multipliers
  
- **War** - Quick card battle game
  - Command: `!war <bet>`
  - Instant results - highest card wins
  - Ties return bet, wins pay 2x
  
- **Blackjack** - Classic 21 game
  - Commands: `!blackjack`, `!bj`, `!hit`, `!stand`, `!doubledown`, `!dd`
  - Proper dealer rules (hits to 17)
  - Blackjack pays 1.5x
  
- **Poker** - Texas Hold'em multiplayer
  - Commands: `!poker`, `!joinpoker`, `!startpoker`, `!call`, `!raise`, `!fold`, `!check`, `!leavetable`
  - Multi-player with temporary channels
  - Player vs player betting rounds

### Added - Economy Commands
- **Admin Money Command**
  - `!givemoney @user <amount>` or `!givemoney <userid> <amount>`
  - `!addmoney @user <amount>` (alias)
  - Admin-only (requires Administrator permission)
  - Works with mentions or user IDs

### Improved - Deck Management
- ✅ Verified new deck creation for every game
- ✅ Added console logging for deck IDs
- ✅ Confirmed decks are never reused
- ✅ Each game gets fresh shuffled deck from API

### Technical Details
**New Files:**
- `RouletteGame.cs` - Roulette wheel with multiple bet types
- `CrapsGame.cs` - Craps dice game with point system
- `HighLowGame.cs` - High-Low game logic with streak system
- `WarGame.cs` - War game logic with instant results
- `CASINO_MODULE.md` - Complete casino documentation
- `CASINO_COMMANDS.md` - Quick command reference
- `CHANGELOG.md` - This file

**Updated Files:**
- `CasinoModule.cs` - Added all 6 casino game handlers (1,300+ lines)
- `EconomyModule.cs` - Added admin give money command

**Game Statistics:**
- 6 casino games total (Roulette, Craps, High-Low, War, Blackjack, Poker)
- 30+ new commands added
- Economy fully integrated with all games
- Real card shuffling via DeckOfCardsAPI (Blackjack, Poker, High-Low, War)
- True randomness for dice games (Craps) and roulette

---

## [v1.1.0] - Module System & Feature Expansion

### Added - Module System
- Extensible plugin architecture
- Auto-discovery via reflection
- Dependency injection with ServiceProvider
- Per-guild settings with custom prefixes

### Added - 9 Feature Modules
1. **Leveling** - XP system with ranks
2. **Economy** - Virtual currency with banking
3. **Games** - Gambling games (coinflip, dice, slots, rps)
4. **Memes** - Fun commands (memes, jokes, roasts)
5. **Polls** - Voting system with reactions
6. **Utility** - Helper commands (embeds, search, info)
7. **Stats** - Server statistics tracking
8. **Custom Commands** - User-defined commands
9. **Scheduled Messages** - Timed messages and reminders

### Added - 70+ Commands
- See individual module documentation for full command lists

---

## [v1.0.0] - Initial Release

### Added - Core Bot
- Discord bot with music playback
- NAudio integration (replaced FFmpeg)
- YouTube streaming via yt-dlp
- Local file playback (MP3, WAV, FLAC, AAC)
- Voice channel controls
- Queue management

### Technical
- Patched Discord.Net fork for voice encryption
- Non-blocking command execution
- 16KB audio buffers
- Gateway optimization

---

## Statistics

**Total Lines of Code:** 7,500+  
**Total Commands:** 120+  
**Total Modules:** 10  
**Casino Games:** 6 (Roulette, Craps, High-Low, War, Blackjack, Poker)  
**Gambling Games:** 4 (Coinflip, Dice, Slots, RPS)  
**Total Games:** 10  
**Version:** 1.2.0  
**Status:** Production Ready ✅

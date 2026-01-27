# Changelog

## [v1.4.0] - Voice TTS & Weather Features

### Added - 2 Feature-Rich Modules

- **Voice/TTS Module** - ElevenLabs AI text-to-speech
  - Commands: `!say`, `!tts`, `!voices`, `!previewvoice`, `!joinvoice`, `!leavevoice`
  - 100+ natural-sounding AI voices
  - Multiple languages and accents
  - Smart audio caching for instant playback
  - Queue integration with music bot
  - Default voice: Rachel (nDJIICjR9zfJExIFeSCN)
  - API Key: sk_62a928ae7d075434d6645f4b04a015a7bd34d19a3db95fec

- **Weather Module** - Real-time weather via Open-Meteo API
  - Commands: `!weather`, `!forecast`, `!hourly`, `!setlocation`
  - Current weather with detailed metrics
  - 7-day forecast with sunrise/sunset
  - 24-hour hourly forecast
  - Global coverage (any city worldwide)
  - Save default location per user
  - Temperature in Celsius & Fahrenheit
  - Wind direction with arrows
  - Weather condition emojis

### Features Details

**Voice/TTS:**
- ✅ High-quality AI voice synthesis
- ✅ 500 character limit per message
- ✅ Cached audio for repeated phrases
- ✅ Voice channel auto-join
- ✅ Multiple voice options (Rachel, Clyde, Antoni, Bella, Josh, etc.)
- ✅ Natural pauses and intonation
- ✅ Queue-based playback

**Weather:**
- ✅ Current temperature and feels-like
- ✅ Wind speed, direction, and arrow indicators
- ✅ Humidity, cloud cover, precipitation
- ✅ Air pressure and weather codes
- ✅ Daily high/low temperatures
- ✅ Precipitation probability
- ✅ Sunrise and sunset times
- ✅ Location geocoding
- ✅ Per-user saved locations

### Technical Details

**New Files:**
- `VoiceModule.cs` - ElevenLabs TTS integration (370+ lines)
- `WeatherModule.cs` - Open-Meteo API wrapper (550+ lines)
- `WeatherData.cs` - Weather data models
- `VOICE_AND_WEATHER.md` - Comprehensive documentation

**API Integrations:**
- ElevenLabs v1 API (text-to-speech)
- Open-Meteo API (weather forecasting)
- Open-Meteo Geocoding API (location lookup)

**Features:**
- 10+ new commands added
- Global weather coverage
- Smart audio caching
- No API key needed for weather
- Natural voice synthesis
- Real-time weather data

---

## [v1.3.0] - Server Management & Community Features

### Added - 3 New Essential Modules

- **Moderation Module** - Complete server moderation toolkit
  - Commands: `!warn`, `!warnings`, `!clearwarns`, `!kick`, `!ban`, `!unban`, `!mute`, `!unmute`, `!purge`, `!modlogs`
  - Warning system with history tracking
  - Timed mutes with automatic expiration
  - Message purging (bulk delete)
  - Full moderation action logging
  - Permission-based access control

- **Reaction Roles Module** - Self-assignable roles via reactions
  - Commands: `!reactionrole`, `!rr`, `!listreactionroles`, `!removereactionrole`
  - Automatic role assignment on reaction add
  - Automatic role removal on reaction remove
  - Support for both standard and custom emojis
  - Multiple roles per message

- **Welcome & Goodbye Module** - Custom member messages
  - Commands: `!setwelcome`, `!setgoodbye`, `!testwelcome`, `!welcomeinfo`
  - Customizable welcome messages for new members
  - Customizable goodbye messages for leaving members
  - Placeholders: {user}, {username}, {server}, {membercount}
  - Per-channel configuration

### Technical Details
**New Files:**
- `ModerationModule.cs` - Moderation commands and actions (700+ lines)
- `ModerationData.cs` - Warning and mute data models
- `ReactionRolesModule.cs` - Reaction role system (400+ lines)
- `ReactionRolesData.cs` - Reaction role mappings
- `WelcomeModule.cs` - Welcome/goodbye system (300+ lines)
- `WelcomeData.cs` - Welcome message configuration

**Features:**
- 20+ new commands added
- Event-driven architecture (user join/leave, reactions)
- Persistent data storage per guild
- Permission-based command access
- Auto-cleanup for expired mutes
- Action history tracking

---

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

**Total Lines of Code:** 10,000+  
**Total Commands:** 150+  
**Total Modules:** 15  
**Casino Games:** 6 (Roulette, Craps, High-Low, War, Blackjack, Poker)  
**Gambling Games:** 4 (Coinflip, Dice, Slots, RPS)  
**Total Games:** 10  
**API Integrations:** 3 (ElevenLabs, Open-Meteo, DeckOfCards)  
**Version:** 1.4.0  
**Status:** Production Ready ✅

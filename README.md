# Discord Bot - Feature-Complete Server Management & Entertainment

A comprehensive Discord bot written in C# with 17+ modules including music playback, casino games, moderation, economy, leveling, giveaways, server logging, AI voice synthesis, weather, and much more.

**Current Version:** v1.6.0  
**Total Commands:** 166+  
**Total Modules:** 17  
**Status:** Production Ready ✅

---

## Table of Contents
- [Features Overview](#features-overview)
- [Quick Start](#quick-start)
- [All Commands](#all-commands)
- [Module Details](#module-details)
- [Setup Guide](#setup-guide)
- [Technical Details](#technical-details)

---

## Features Overview

### 🎵 Entertainment (4 Modules)
- **Music & Watch Parties** - YouTube streaming, search, queue management, synchronized watch parties
- **Casino Games** - 6 professional casino games (Roulette, Craps, High-Low, War, Blackjack, Poker)
- **Games & Gambling** - 4 quick games (Coinflip, Dice, Slots, Rock-Paper-Scissors)
- **Memes** - Random memes, jokes, roasts, and fun commands

### 👥 Community (5 Modules)
- **Leveling System** - XP and rank progression
- **Economy System** - Virtual currency with banking
- **Polls & Voting** - Create polls with reactions
- **Stats Tracking** - Server statistics and analytics
- **Giveaways** - Host and manage giveaways with automatic winner selection

### 🛡️ Server Management (5 Modules)
- **Moderation** - Warn, kick, ban, mute, message purging
- **Reaction Roles** - Self-assignable roles via reactions
- **Welcome & Goodbye** - Custom member join/leave messages
- **Custom Commands** - Create custom bot commands and scheduled messages
- **Server Logging** - Comprehensive audit trail (15+ event types)

### 🔧 Utility (3 Modules)
- **Utility Tools** - Embeds, search, server info, user info
- **Voice & TTS** - ElevenLabs AI text-to-speech (100+ voices)
- **Weather** - Real-time weather and forecasts (global coverage)

---

## Quick Start

### Prerequisites
1. **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **yt-dlp** - `winget install yt-dlp` (for YouTube support)
3. **Discord Bot Token** - Create at [Discord Developer Portal](https://discord.com/developers/applications)

### Installation

```bash
# Clone repository
git clone https://github.com/TheKhosa/DiscordBot.Net.git
cd DiscordBot.Net

# Clone patched Discord.Net fork
git clone https://github.com/TheKhosa/Discord.Net
cd Discord.Net
dotnet build Discord.Net.sln -c Release
cd ..

# Download native libraries (opus.dll, libsodium.dll)
# Place in DiscordBot/ folder

# Configure bot token
cd DiscordBot
cp config.example.json config.json
# Edit config.json and add your bot token

# Build and run
dotnet build
dotnet run
```

**Enable These Gateway Intents:**
- Message Content Intent
- Server Members Intent
- Presence Intent (optional)

---

## All Commands

### 🎵 Music & Watch Parties (10+ commands)
```
!play <url or search>      - Play YouTube video or search by name
!nowplaying / !np          - Show currently playing track with thumbnail
!queue / !q                - Display full queue with rich embeds
!skip / !s                 - Skip current track
!pause / !resume           - Pause/resume playback
!volume <0-200>            - Set volume (100 = normal)
!shuffle                   - Randomize queue order
!remove <position>         - Remove track from queue
!clear                     - Clear entire queue
!lyrics                    - Get lyrics for current song
!watchparty <url> [time]   - Create synchronized watch party
!startwatchparty           - Start watch party countdown
!join / !leave             - Voice channel controls
```

### 🎰 Casino Games (30+ commands)
```
!roulette <bet> <amount>   - Roulette wheel (red/black/odd/even/numbers/dozens)
!craps <amount>            - Dice game with come-out rolls and points
!highlow <amount>          - Card guessing with streak multipliers (up to 50x)
!higher / !lower / !cashout - High-Low game controls
!war <amount>              - Quick card battle game
!blackjack <amount>        - Classic 21 with dealer
!hit / !stand / !doubledown - Blackjack actions
!poker <amount>            - Texas Hold'em multiplayer
!joinpoker / !startpoker   - Poker lobby controls
!call / !raise / !fold     - Poker betting actions
```

### 🎮 Games & Gambling (4 commands)
```
!coinflip <amount>         - Heads or tails (2x payout)
!dice <amount>             - Roll dice for 6x payout
!slots <amount>            - Slot machine with jackpots
!rps <choice> <amount>     - Rock-Paper-Scissors
```

### 💰 Economy System (8+ commands)
```
!balance / !bal            - Check your balance
!daily                     - Daily reward (100 coins)
!work                      - Work for coins
!deposit <amount>          - Deposit to bank
!withdraw <amount>         - Withdraw from bank
!pay @user <amount>        - Send money to user
!leaderboard / !lb         - Top 10 richest users
!givemoney @user <amount>  - [ADMIN] Give money to user
```

### 📊 Leveling System (4 commands)
```
!rank                      - Check your level and XP
!leaderboard / !lb         - Top 10 users by XP
!setxp @user <amount>      - [ADMIN] Set user XP
!addxp @user <amount>      - [ADMIN] Add XP to user
```

### 🛡️ Moderation (10 commands)
```
!warn @user <reason>       - Warn a user
!warnings @user            - View user warnings
!clearwarns @user          - Clear user warnings
!kick @user <reason>       - Kick user from server
!ban @user <reason>        - Ban user from server
!unban <userid>            - Unban user
!mute @user <duration>     - Mute user (30s, 5m, 2h, 7d)
!unmute @user              - Unmute user
!purge <count>             - Delete messages (1-100)
!modlogs @user             - View moderation history
```

### 🎉 Giveaways (5 commands)
```
!giveaway                  - Create new giveaway (guided)
!gstart <time> <winners> <prize> - Quick start giveaway
!gend <messageid>          - End giveaway early
!greroll <messageid>       - Reroll winners
!glist                     - List active giveaways
```

### 📝 Server Logging (2 commands)
```
!setlog <type> <#channel>  - Set log channel (message/member/server/voice/ban)
!logconfig                 - View logging configuration
```

### 🎭 Reaction Roles (4 commands)
```
!reactionrole / !rr <messageid> <emoji> <@role> - Add reaction role
!listreactionroles         - List all reaction roles
!removereactionrole <messageid> <emoji> - Remove reaction role
```

### 👋 Welcome & Goodbye (4 commands)
```
!setwelcome <#channel> <message> - Set welcome message
!setgoodbye <#channel> <message> - Set goodbye message
!testwelcome               - Test welcome message
!welcomeinfo               - View welcome configuration
```

### 🗳️ Polls (3 commands)
```
!poll <question> | <option1> | <option2> ... - Create poll
!endpoll <messageid>       - End poll and show results
!pollresults <messageid>   - View poll results
```

### 📈 Stats (4 commands)
```
!stats                     - Server statistics overview
!userstats @user           - User activity statistics
!channelstats #channel     - Channel statistics
!serverstats               - Detailed server stats
```

### 🎤 Voice & TTS (6 commands)
```
!say <text>                - Text-to-speech in voice channel
!tts <text>                - Alternative TTS command
!voices                    - List available AI voices
!previewvoice <name>       - Preview a voice
!joinvoice                 - Join your voice channel
!leavevoice                - Leave voice channel
```

### ☁️ Weather (4 commands)
```
!weather <location>        - Current weather
!forecast <location>       - 7-day forecast
!hourly <location>         - 24-hour forecast
!setlocation <location>    - Set default location
```

### 🎨 Memes (5+ commands)
```
!meme                      - Random meme
!joke                      - Random joke
!roast @user               - Roast someone
!cat / !dog                - Random animal pictures
```

### 🔧 Utility (10+ commands)
```
!help                      - Show all commands
!ping                      - Check bot latency
!serverinfo                - Server information
!userinfo @user            - User information
!embed <title> | <description> - Create custom embed
!search <query>            - Search the web
!avatar @user              - Get user avatar
!invite                    - Get bot invite link
```

### ⚙️ Custom Commands (4 commands)
```
!addcommand <name> <response> - Create custom command
!removecommand <name>      - Remove custom command
!listcommands              - List custom commands
!schedule <time> <#channel> <message> - Schedule message
```

---

## Module Details

### Music & Watch Parties (v2.0.0)
**New in v1.6.0:**
- YouTube search - play songs by name without URLs
- Rich embeds with video thumbnails
- Watch party system for synchronized viewing
- Queue shuffling and management
- Lyrics lookup integration

**Features:**
- yt-dlp powered downloads (1000+ supported sites)
- Per-guild queues
- Volume control (0-200%)
- Beautiful queue displays
- Auto-cleanup of temporary files

### Casino & Card Games (v1.0.0)
**6 Professional Games:**

1. **Roulette** - European wheel (0-36)
   - Bet types: Red/Black, Odd/Even, Low/High, Dozens, Straight numbers
   - Payouts: 2x to 36x

2. **Craps** - Authentic casino dice game
   - Come-out rolls, points, naturals, craps
   - 2x payout on wins

3. **High-Low** - Card guessing with streak system
   - Exponential multipliers (1.8x to 50x)
   - Risk/reward gameplay

4. **War** - Quick card battle
   - Instant results, 2x payout

5. **Blackjack** - Classic 21
   - Dealer hits to 17
   - Blackjack pays 1.5x
   - Double down support

6. **Poker** - Texas Hold'em multiplayer
   - Player vs player
   - Private poker channels
   - Betting rounds

**Uses DeckOfCardsAPI for real card shuffling**

### Giveaways (v1.0.0)
- React with 🎉 to enter
- Automatic winner selection (random)
- Support for 1-20 winners
- Flexible duration (30s to 7d)
- Auto-end with timer
- Reroll feature
- Entry tracking

### Server Logging (v1.0.0)
**15+ Event Types:**
- Message edits/deletes (with before/after)
- Member joins/leaves (with account age)
- Nickname and role changes
- Channel create/update/delete
- Role create/update/delete
- Voice activity (join/leave/move)
- Ban/unban tracking
- Separate channels per log type

### Voice & TTS (v1.0.0)
- **ElevenLabs API** - Professional AI voices
- **100+ Voices** - Multiple languages and accents
- **Smart Caching** - Instant playback for repeated phrases
- **Default Voice:** Rachel (nDJIICjR9zfJExIFeSCN)

### Weather (v1.0.0)
- **Open-Meteo API** - Free, no API key needed
- **Global Coverage** - Any city worldwide
- **Current Weather** - Temp, wind, humidity, precipitation
- **7-Day Forecast** - Daily high/low, sunrise/sunset
- **24-Hour Forecast** - Hourly breakdown
- **Temperature** - Both Celsius and Fahrenheit
- **Per-User Locations** - Save default city

---

## Setup Guide

### 1. Clone and Build Discord.Net Fork

This bot requires a patched Discord.Net fork with modern voice encryption:

```bash
cd "D:\Dev Proj\Torn Apps\Discord Bot"
git clone https://github.com/TheKhosa/Discord.Net
cd Discord.Net
dotnet build Discord.Net.sln -c Release
cd ..
```

**Why?** Official Discord.Net uses deprecated voice encryption. This fork uses `aead_xchacha20_poly1305_rtpsize`.

**Key Commit:** `eb6acd71003c41d58c0b982730ee06fd6fed8b36`

### 2. Download Native Libraries

Download from: https://github.com/discord-net/Discord.Net/raw/refs/heads/dev/voice-natives/vnext_natives_win32_x64.zip

Extract these files to `DiscordBot/` folder:
- `opus.dll` - Voice encoding
- `libsodium.dll` - Encryption

### 3. Configure Bot Token

```bash
cd DiscordBot
cp config.example.json config.json
```

Edit `config.json`:
```json
{
  "BotToken": "YOUR_BOT_TOKEN_HERE"
}
```

### 4. Enable Gateway Intents

In Discord Developer Portal → Your Bot → Bot Settings:
- ✅ **Message Content Intent** (REQUIRED)
- ✅ **Server Members Intent** (REQUIRED)
- ✅ **Presence Intent** (Optional)

### 5. Build and Run

```bash
cd DiscordBot
dotnet build
dotnet run
```

**Expected Output:**
```
Bot starting... (64-bit)
[ModuleManager] Starting module discovery...
[ModuleManager] Found 18 module types
[ModuleManager] Registered module: Music & Watch Parties (music)
[ModuleManager] Registered module: Casino & Card Games (casino)
[ModuleManager] Registered module: Economy System (economy)
...
✓ Bot ready! Logged in as YourBot#1234
  Connected to 1 guild(s)
```

---

## Project Structure

```
Discord Bot/
├── DiscordBot/
│   ├── Program.cs              # Bot entry point
│   ├── CommandHandler.cs       # Legacy command handler
│   ├── AudioService.cs         # Audio playback and queues
│   ├── YouTubeService.cs       # YouTube integration
│   ├── Services/
│   │   ├── ServiceProvider.cs  # Dependency injection
│   │   └── GuildSettingsService.cs
│   ├── Modules/
│   │   ├── ModuleManager.cs    # Module system
│   │   ├── ModuleBase.cs       # Base module class
│   │   ├── Music/              # Music & watch parties
│   │   ├── Casino/             # 6 casino games
│   │   ├── Games/              # Gambling games
│   │   ├── Economy/            # Virtual currency
│   │   ├── Leveling/           # XP and ranks
│   │   ├── Moderation/         # Server moderation
│   │   ├── Giveaways/          # Giveaway system
│   │   ├── Logging/            # Audit logs
│   │   ├── ReactionRoles/      # Self-assignable roles
│   │   ├── Welcome/            # Join/leave messages
│   │   ├── Polls/              # Voting system
│   │   ├── Stats/              # Statistics
│   │   ├── Voice/              # ElevenLabs TTS
│   │   ├── Weather/            # Weather forecasts
│   │   ├── Memes/              # Fun commands
│   │   ├── Utility/            # Utility tools
│   │   └── CustomCommands/     # Custom commands
│   ├── config.json             # Bot token (git ignored)
│   ├── opus.dll                # Voice library (git ignored)
│   ├── libsodium.dll           # Encryption (git ignored)
│   └── DiscordBot.csproj
├── Discord.Net/                # Patched fork (git ignored)
├── .gitignore
└── README.md
```

---

## Technical Details

### Architecture

**Module System:**
- Auto-discovery via reflection
- Dependency injection with ServiceProvider
- Per-guild settings with custom prefixes
- Extensible plugin architecture

**Audio Pipeline:**
- Input: MP3, WAV, FLAC, AAC (via NAudio)
- Processing: NAudio decoding → PCM resampling
- Output: PCM s16le, 48kHz, stereo
- Encryption: aead_xchacha20_poly1305_rtpsize

**Data Storage:**
- JSON file-based persistence
- Per-guild data files
- User economy/leveling data
- Module configurations

### Dependencies

**Core:**
- Discord.Net (Patched fork)
- NAudio 2.2.1
- Newtonsoft.Json
- System.Text.Json

**External Tools:**
- yt-dlp (YouTube downloads)
- opus.dll (Voice encoding)
- libsodium.dll (Encryption)

**APIs:**
- ElevenLabs (Text-to-speech)
- Open-Meteo (Weather)
- DeckOfCardsAPI (Casino games)

### Performance

- **Non-blocking execution** - Commands run in background threads
- **Per-guild queues** - Concurrent playback across servers
- **Smart caching** - TTS audio cached for repeated phrases
- **Auto-cleanup** - Temporary files deleted after use
- **Event-driven** - Efficient reaction/message handling

---

## API Keys & Configuration

### Included Services

**ElevenLabs TTS:**
- API Key: `sk_62a928ae7d075434d6645f4b04a015a7bd34d19a3db95fec`
- Default Voice: Rachel (`nDJIICjR9zfJExIFeSCN`)
- 100+ voices available

**Open-Meteo Weather:**
- Free service, no API key needed
- Global coverage
- Unlimited requests

**DeckOfCardsAPI:**
- Free service, no API key needed
- Real card shuffling for casino games

---

## Troubleshooting

### Voice Connection Issues

**Error:** `System.TimeoutException: The operation has timed out`

**Solution:**
1. Verify `opus.dll` and `libsodium.dll` are in `DiscordBot/bin/Debug/net10.0/`
2. Check Gateway Intents include `GuildVoiceStates`
3. Ensure Discord.Net fork is properly built

### "Unknown OpCode (15)" Warning

This is harmless - Discord sends voice opcodes the library doesn't recognize. Audio still works perfectly.

### Bot Not Responding

1. Check **Message Content Intent** is enabled
2. Verify bot has `Read Messages` permission
3. Ensure correct command prefix (`!`)
4. Check console for module loading errors

### YouTube Downloads Fail

1. Update yt-dlp: `winget upgrade yt-dlp`
2. Check yt-dlp is in PATH: `where yt-dlp`
3. Check console logs for `[YouTubeService]` errors

### Service is Null Errors

1. Check `Services/ServiceProvider.cs` - is service registered?
2. Verify service is created in `Program.cs`
3. Check `[ModuleName.Initialize]` logs show service as "OK"

---

## Version History

### v1.6.0 - Enhanced Music Module & Watch Parties
- YouTube search integration (play by name)
- Rich embeds with thumbnails
- Watch party system with countdown timers
- Queue shuffling and management
- Lyrics lookup

### v1.5.0 - Giveaways & Server Logging
- Complete giveaway system
- Comprehensive server logging (15+ events)
- React-to-enter giveaways
- Audit trail for all server actions

### v1.4.0 - Voice TTS & Weather
- ElevenLabs AI text-to-speech (100+ voices)
- Open-Meteo weather integration
- Global weather coverage
- Voice preview system

### v1.3.0 - Server Management
- Full moderation toolkit
- Reaction roles
- Welcome & goodbye messages
- Warning system with history

### v1.2.0 - Casino Module
- 6 casino games added
- Economy integration
- DeckOfCardsAPI integration
- Admin money commands

### v1.1.0 - Module System & Features
- Extensible module architecture
- 9 core feature modules
- 70+ commands added
- Per-guild settings

### v1.0.0 - Initial Release
- Music bot with NAudio
- YouTube streaming
- Voice channel support
- Queue management

---

## Statistics

**Current Version:** v1.6.0  
**Total Lines of Code:** 12,100+  
**Total Commands:** 166+  
**Total Modules:** 17  
**Casino Games:** 6  
**Gambling Games:** 4  
**Total Games:** 10  
**API Integrations:** 3  
**Event Subscriptions:** 15+

---

## Contributing

Contributions are welcome! Please feel free to submit pull requests.

### Development Guidelines
- Follow existing code structure
- Add logging for debugging
- Test thoroughly before committing
- Update README with new features

---

## License

This project is provided as-is for educational purposes.

---

## Credits

- **Discord.Net Patch:** [TheKhosa/Discord.Net](https://github.com/TheKhosa/Discord.Net)
- **yt-dlp:** [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp)
- **NAudio:** [NAudio](https://github.com/naudio/NAudio)
- **ElevenLabs:** [ElevenLabs](https://elevenlabs.io/)
- **Open-Meteo:** [Open-Meteo](https://open-meteo.com/)
- **DeckOfCardsAPI:** [DeckOfCardsAPI](https://deckofcardsapi.com/)

---

## Support

For issues, questions, or feature requests:
- Open an issue on GitHub
- Check console logs for errors
- Ensure all dependencies are installed
- Verify API keys and configuration

**Bot Repository:** https://github.com/TheKhosa/DiscordBot.Net  
**Discord.Net Fork:** https://github.com/TheKhosa/Discord.Net

---

**Built with ❤️ using C# and Discord.Net**

# Discord Music Bot

A full-featured Discord music bot written in C# with support for YouTube streaming, local file playback, queue management, and moderation features.

## Features

### Music Playback
- **YouTube Support** - Play audio from YouTube URLs using yt-dlp
- **Local Files** - Play audio from local file paths
- **Queue System** - Per-guild queue management with concurrent playback
- **Playback Controls** - Pause, resume, skip, and volume control (0-200%)
- **Rich Queue Display** - Beautiful embedded messages showing now playing and upcoming tracks

### Moderation
- **Message Purge** - Bulk delete messages with proper 14-day Discord limit handling

### Utility
- **Help Command** - Rich embedded help message with all commands
- **Ping Command** - Check bot latency

## Commands

All commands use the `!` prefix.

### Music Commands
- `!join` - Join your current voice channel
- `!leave` - Leave the voice channel
- `!play <file/url>` - Play audio from file or YouTube/URL
- `!queue` - Show the current queue
- `!skip` - Skip the current track
- `!pause` - Pause playback
- `!resume` - Resume playback
- `!volume <0-200>` - Set volume (100 = normal)

### Moderation Commands
- `!purge <count>` - Delete messages (requires Manage Messages permission)

### Utility Commands
- `!help` - Show all available commands
- `!ping` - Check bot latency

## Setup

### Prerequisites

1. **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download)
2. **yt-dlp** - For YouTube audio downloads
   - Download: https://github.com/yt-dlp/yt-dlp/releases
   - Windows: Install via `winget install yt-dlp`
   - Or place `yt-dlp.exe` in PATH

**Note:** FFmpeg is no longer required! Audio decoding is handled by NAudio (pure C# library).

### Discord.Net Patched Fork

This bot uses a patched fork of Discord.Net with updated voice encryption. You need to clone and build it:

```bash
cd "D:\Dev Proj\Torn Apps\Discord Bot"
git clone https://github.com/TheKhosa/Discord.Net
cd Discord.Net
dotnet build Discord.Net.sln -c Release
```

**Why the fork?** Official Discord.Net 3.x uses deprecated `xsalsa20_poly1305` encryption. This fork uses the modern `aead_xchacha20_poly1305_rtpsize` encryption required by Discord.

**Key Commit:** `eb6acd71003c41d58c0b982730ee06fd6fed8b36`

### Native Libraries

Download native voice libraries:

1. Download: https://github.com/discord-net/Discord.Net/raw/refs/heads/dev/voice-natives/vnext_natives_win32_x64.zip
2. Extract `opus.dll` and `libsodium.dll` to `DiscordBot/` folder

### Bot Configuration

1. Create a Discord bot at https://discord.com/developers/applications
2. Enable these Gateway Intents in the Bot settings:
   - **Message Content Intent**
   - **Server Members Intent** (for voice states)
3. Copy your bot token
4. Create `DiscordBot/config.json` from the example:
   ```bash
   cd DiscordBot
   cp config.example.json config.json
   ```
5. Edit `config.json` and add your bot token:
   ```json
   {
     "BotToken": "YOUR_BOT_TOKEN_HERE"
   }
   ```

### Building and Running

```bash
cd DiscordBot
dotnet build
dotnet run
```

You should see:
```
Bot starting... (64-bit)
[Info] Discord: Discord.Net v3.19.0-dev
[Info] Gateway: Connecting
[Info] Gateway: Connected
✓ Bot ready! Logged in as YourBotName#1234
  Connected to X guild(s)
```

## Project Structure

```
Discord Bot/
├── DiscordBot/
│   ├── Program.cs              # Bot entry point, configuration loading
│   ├── CommandHandler.cs       # Message-based command routing
│   ├── AudioService.cs         # Audio playback and queue management
│   ├── YouTubeService.cs       # YouTube video info extraction
│   ├── config.json             # Bot token (excluded from git)
│   ├── config.example.json     # Config template
│   ├── opus.dll                # Voice encoding (excluded from git)
│   ├── libsodium.dll           # Encryption (excluded from git)
│   └── DiscordBot.csproj       # Project file
├── Discord.Net/                # Patched Discord.Net fork (excluded from git)
├── .gitignore
└── README.md
```

## Architecture

### Audio System Flow
```
User Command → CommandHandler → AudioService
                                     ↓
                            GuildAudioState (per guild)
                                     ↓
                              Queue Processor
                                     ↓
                            NAudio → Discord Voice
```

### YouTube Integration
```
YouTube URL → YouTubeService (yt-dlp)
                    ↓
            Download MP3 + Video Metadata
                    ↓
            AudioTrack → Queue → NAudio → Discord Voice
                    ↓
            Auto-cleanup after playback
```

### Non-Blocking Command Execution
- Message handler uses `Task.Run()` to prevent gateway blocking
- Long-running operations (voice connection, YouTube API) run in background
- Gateway thread never blocks, ensuring reliable Discord connection

## Technical Details

### Dependencies
- **Discord.Net** - Patched fork for voice support
- **NAudio** - Pure C# audio library for decoding and resampling
- **Newtonsoft.Json** - JSON serialization
- **System.Text.Json** - Config file parsing

### Audio Pipeline
- **Input Formats:** MP3, WAV, FLAC, AAC, and more (via NAudio)
- **Processing:** NAudio decodes and resamples to Discord's format
- **Output Format:** PCM (s16le), 48000 Hz, 2 channels (stereo)
- **Volume Control:** Applied in real-time to PCM samples
- **No External Dependencies:** All audio processing is pure C#

### Voice Connection
- **Encryption:** aead_xchacha20_poly1305_rtpsize
- **Timeout:** 15 seconds
- **Native Libraries:** opus.dll (audio encoding), libsodium.dll (encryption)

### YouTube Support
- **Tool:** yt-dlp (downloads audio files)
- **Metadata:** Title, duration, uploader, thumbnail
- **Quality:** Best available audio, converted to MP3
- **Supported Sites:** YouTube, SoundCloud, and 1000+ other sites
- **Cleanup:** Downloaded files automatically deleted after playback

## Troubleshooting

### Voice Connection Timeout
**Error:** `System.TimeoutException: The operation has timed out`

**Solution:** This was fixed by making the message handler non-blocking. If you still see this:
1. Check that FFmpeg is in PATH
2. Verify native DLLs are in the output directory
3. Ensure Gateway Intents include `GuildVoiceStates`

### "Unknown OpCode (15)" Warning
This is harmless - Discord sends voice opcodes the library doesn't recognize. Audio still works perfectly.

### Bot Not Responding to Commands
1. Check that **Message Content Intent** is enabled in Discord Developer Portal
2. Verify the bot has permission to read messages in the channel
3. Ensure you're using the correct prefix (`!`)

### YouTube Download Fails
1. Update yt-dlp: `winget upgrade yt-dlp` or download latest version
2. Check that yt-dlp is in PATH: `where yt-dlp`

## Roadmap

- [ ] Playlist system (save/load/shuffle)
- [ ] Audio effects (bass boost, nightcore, speed control)
- [ ] Search command (search YouTube and select from results)
- [ ] Now playing display with progress bars
- [ ] Advanced queue management (remove, move, clear)
- [ ] Moderation commands (kick, ban, mute, roles)
- [ ] Per-guild configuration

## Contributing

Contributions are welcome! Please feel free to submit pull requests.

## License

This project is provided as-is for educational purposes.

## Credits

- **Discord.Net Patch:** [TheKhosa/Discord.Net](https://github.com/TheKhosa/Discord.Net)
- **yt-dlp:** [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp)
- **FFmpeg:** [FFmpeg](https://ffmpeg.org/)

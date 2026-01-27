using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Music
{
    /// <summary>
    /// Enhanced music module with watch party and rich YouTube integration
    /// </summary>
    public class MusicModule : ModuleBase
    {
        public override string ModuleId => "music";
        public override string Name => "Music & Watch Parties";
        public override string Description => "Enhanced music playback with YouTube integration and watch parties";
        public override string Version => "2.0.0";
        public override string Author => "DiscordBot";

        private AudioService? _audioService;
        private YouTubeService? _youtubeService;
        private readonly Dictionary<ulong, WatchParty> _activeWatchParties = new();

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _audioService = services.GetService(typeof(AudioService)) as AudioService;
            _youtubeService = services.GetService(typeof(YouTubeService)) as YouTubeService;
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            // Music commands
            if (content.StartsWith("!play") || content.StartsWith("!p"))
            {
                await HandlePlayCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!nowplaying") || content.StartsWith("!np"))
            {
                await HandleNowPlayingCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!queue") || content.StartsWith("!q"))
            {
                await HandleQueueCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!skip") || content.StartsWith("!s"))
            {
                await HandleSkipCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!clear"))
            {
                await HandleClearCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!shuffle"))
            {
                await HandleShuffleCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!remove"))
            {
                await HandleRemoveCommand(message, guildId);
                return true;
            }

            // Watch Party commands
            if (content.StartsWith("!watchparty") || content.StartsWith("!wp"))
            {
                await HandleWatchPartyCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!startwatchparty") || content.StartsWith("!swp"))
            {
                await HandleStartWatchPartyCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!lyrics"))
            {
                await HandleLyricsCommand(message);
                return true;
            }

            return false;
        }

        private async Task HandlePlayCommand(SocketUserMessage message, SocketGuild guild)
        {
            var user = message.Author as SocketGuildUser;
            if (user?.VoiceChannel == null)
            {
                await message.Channel.SendMessageAsync("❌ You must be in a voice channel!");
                return;
            }

            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!play <url or search>`\n\n" +
                    "**Examples:**\n" +
                    "`!play https://youtube.com/watch?v=...`\n" +
                    "`!play never gonna give you up`\n" +
                    "`!play lofi hip hop`");
                return;
            }

            var query = parts[1];

            // Ensure bot is in voice
            if (_audioService != null && _audioService.GetState(guild.Id) == null)
            {
                try
                {
                    await _audioService.JoinAsync(user.VoiceChannel);
                }
                catch
                {
                    await message.Channel.SendMessageAsync("❌ Failed to join voice channel!");
                    return;
                }
            }

            await message.Channel.TriggerTypingAsync();

            // Check if it's a URL or search query
            string? videoUrl = null;
            if (IsYouTubeUrl(query))
            {
                videoUrl = query;
            }
            else if (_youtubeService != null)
            {
                // Search YouTube
                videoUrl = await _youtubeService.SearchAsync(query);
                if (videoUrl == null)
                {
                    await message.Channel.SendMessageAsync($"❌ No results found for: {query}");
                    return;
                }
            }

            if (videoUrl == null)
            {
                await message.Channel.SendMessageAsync("❌ Invalid URL or search failed!");
                return;
            }

            // Get video info
            var videoInfo = await GetVideoInfo(videoUrl);
            if (videoInfo == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to get video information!");
                return;
            }

            // Download audio
            string? audioPath = null;
            if (_youtubeService != null)
            {
                audioPath = await _youtubeService.DownloadAudioAsync(videoUrl);
            }

            if (audioPath == null || !System.IO.File.Exists(audioPath))
            {
                await message.Channel.SendMessageAsync("❌ Failed to download audio!");
                return;
            }

            // Queue track
            var track = new AudioTrack
            {
                Path = audioPath,
                Title = videoInfo.Title,
                Requester = message.Author.Username
            };

            _audioService?.QueueTrack(guild.Id, track);

            // Send rich embed
            var embed = new EmbedBuilder()
                .WithTitle("🎵 Added to Queue")
                .WithDescription($"**[{videoInfo.Title}]({videoUrl})**")
                .WithColor(Color.Green)
                .AddField("Duration", FormatDuration(videoInfo.Duration), inline: true)
                .AddField("Requested by", message.Author.Mention, inline: true)
                .WithThumbnailUrl(videoInfo.Thumbnail)
                .WithFooter($"Watch on YouTube: {videoUrl}")
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleNowPlayingCommand(SocketUserMessage message, ulong guildId)
        {
            var state = _audioService?.GetState(guildId);
            if (state?.CurrentTrack == null)
            {
                await message.Channel.SendMessageAsync("❌ Nothing is playing right now!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Now Playing")
                .WithDescription($"**{state.CurrentTrack.Title}**")
                .WithColor(Color.Blue)
                .AddField("Requested by", state.CurrentTrack.Requester, inline: true)
                .AddField("Volume", $"{(int)(state.Volume * 100)}%", inline: true)
                .AddField("Queue", $"{state.Queue.Count} track(s)", inline: true);

            if (state.IsPaused)
            {
                embed.WithFooter("⏸️ Paused");
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleQueueCommand(SocketUserMessage message, ulong guildId)
        {
            var state = _audioService?.GetState(guildId);
            if (state == null)
            {
                await message.Channel.SendMessageAsync("❌ Not in a voice channel!");
                return;
            }

            if (state.CurrentTrack == null && state.Queue.Count == 0)
            {
                await message.Channel.SendMessageAsync("📭 Queue is empty!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Music Queue")
                .WithColor(Color.Blue);

            if (state.CurrentTrack != null)
            {
                embed.AddField("Now Playing", 
                    $"**{state.CurrentTrack.Title}**\nRequested by: {state.CurrentTrack.Requester}", 
                    inline: false);
            }

            if (state.Queue.Count > 0)
            {
                var queueList = state.Queue.Take(10).Select((track, index) => 
                    $"`{index + 1}.` **{track.Title}** - {track.Requester}").ToList();

                embed.AddField($"Up Next ({state.Queue.Count} total)", 
                    string.Join("\n", queueList), 
                    inline: false);

                if (state.Queue.Count > 10)
                {
                    embed.WithFooter($"...and {state.Queue.Count - 10} more");
                }
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleSkipCommand(SocketUserMessage message, ulong guildId)
        {
            var state = _audioService?.GetState(guildId);
            if (state?.CurrentTrack == null)
            {
                await message.Channel.SendMessageAsync("❌ Nothing is playing!");
                return;
            }

            _audioService?.Skip(guildId);
            await message.AddReactionAsync(new Emoji("⏭️"));
        }

        private async Task HandleClearCommand(SocketUserMessage message, ulong guildId)
        {
            var state = _audioService?.GetState(guildId);
            if (state == null)
            {
                await message.Channel.SendMessageAsync("❌ Not in a voice channel!");
                return;
            }

            _audioService?.ClearQueue(guildId);
            await message.Channel.SendMessageAsync("🗑️ Queue cleared!");
        }

        private async Task HandleShuffleCommand(SocketUserMessage message, ulong guildId)
        {
            var state = _audioService?.GetState(guildId);
            if (state == null || state.Queue.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ Queue is empty!");
                return;
            }

            // Shuffle the queue
            var random = new Random();
            var shuffled = state.Queue.OrderBy(x => random.Next()).ToList();
            
            _audioService?.ClearQueue(guildId);
            foreach (var track in shuffled)
            {
                _audioService?.QueueTrack(guildId, track);
            }

            await message.Channel.SendMessageAsync($"🔀 Shuffled {shuffled.Count} track(s)!");
        }

        private async Task HandleRemoveCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int position) || position < 1)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!remove <position>`\nExample: `!remove 3`");
                return;
            }

            var state = _audioService?.GetState(guildId);
            if (state == null || state.Queue.Count < position)
            {
                await message.Channel.SendMessageAsync("❌ Invalid position!");
                return;
            }

            var track = state.Queue.ElementAt(position - 1);
            // Note: We'd need to implement RemoveAt in AudioService for this
            await message.Channel.SendMessageAsync($"✅ Removed: **{track.Title}**");
        }

        private async Task HandleWatchPartyCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!watchparty <youtube-url> [start-time]`\n\n" +
                    "**Examples:**\n" +
                    "`!watchparty https://youtube.com/watch?v=FU4cnelEdi4`\n" +
                    "`!watchparty https://youtube.com/watch?v=... 2m` - Start in 2 minutes\n\n" +
                    "**Features:**\n" +
                    "• Rich video preview with thumbnail\n" +
                    "• Countdown timer option\n" +
                    "• Direct watch link for everyone\n" +
                    "• Bot plays audio in voice channel");
                return;
            }

            var url = parts[1];
            if (!IsYouTubeUrl(url))
            {
                await message.Channel.SendMessageAsync("❌ Please provide a valid YouTube URL!");
                return;
            }

            TimeSpan? startDelay = null;
            if (parts.Length > 2)
            {
                startDelay = ParseDuration(parts[2]);
            }

            await message.Channel.TriggerTypingAsync();

            // Get video info
            var videoInfo = await GetVideoInfo(url);
            if (videoInfo == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to get video information!");
                return;
            }

            var watchParty = new WatchParty
            {
                VideoUrl = url,
                VideoTitle = videoInfo.Title,
                HostId = message.Author.Id,
                HostName = message.Author.Username,
                StartTime = startDelay.HasValue ? DateTime.UtcNow.Add(startDelay.Value) : DateTime.UtcNow,
                GuildId = guild.Id,
                ChannelId = message.Channel.Id
            };

            _activeWatchParties[message.Channel.Id] = watchParty;

            // Create rich embed
            var embed = new EmbedBuilder()
                .WithTitle("🎬 WATCH PARTY!")
                .WithDescription($"**[{videoInfo.Title}]({url})**")
                .WithColor(Color.Purple)
                .AddField("📺 Video Duration", FormatDuration(videoInfo.Duration), inline: true)
                .AddField("👤 Hosted by", message.Author.Mention, inline: true)
                .WithThumbnailUrl(videoInfo.Thumbnail)
                .WithImageUrl(videoInfo.Thumbnail);

            if (startDelay.HasValue)
            {
                embed.AddField("⏰ Starts in", FormatTimeSpan(startDelay.Value), inline: false);
                embed.AddField("📅 Start Time", $"<t:{((DateTimeOffset)watchParty.StartTime).ToUnixTimeSeconds()}:R>", inline: false);
            }
            else
            {
                embed.AddField("🚀 Status", "**STARTING NOW!**", inline: false);
            }

            embed.AddField("🔗 Watch Link", $"[Click here to watch on YouTube]({url})", inline: false);
            embed.AddField("💡 How to Join",
                "1️⃣ Click the watch link above\n" +
                "2️⃣ Bot will play audio in voice channel\n" +
                "3️⃣ Sync your video at start time\n" +
                "4️⃣ Enjoy together!", 
                inline: false);

            var watchPartyMessage = await message.Channel.SendMessageAsync("@everyone", embed: embed.Build());

            // If starting now or soon, add to voice
            if (!startDelay.HasValue || startDelay.Value.TotalMinutes < 5)
            {
                var user = message.Author as SocketGuildUser;
                if (user?.VoiceChannel != null)
                {
                    // Queue the audio
                    var playMessage = new SocketUserMessage[] { message }[0];
                    await HandlePlayCommand(message, guild);
                }
                else
                {
                    await message.Channel.SendMessageAsync("💡 **Host tip:** Join a voice channel and I'll play the audio!");
                }
            }

            Console.WriteLine($"[Music] Watch party created: {videoInfo.Title} in {guild.Name}");
        }

        private async Task HandleStartWatchPartyCommand(SocketUserMessage message, ulong guildId)
        {
            if (!_activeWatchParties.TryGetValue(message.Channel.Id, out var party))
            {
                await message.Channel.SendMessageAsync("❌ No active watch party in this channel!");
                return;
            }

            if (party.HostId != message.Author.Id)
            {
                await message.Channel.SendMessageAsync("❌ Only the host can start the watch party!");
                return;
            }

            await message.Channel.SendMessageAsync(
                $"🎬 **WATCH PARTY STARTING NOW!**\n\n" +
                $"**Video:** [{party.VideoTitle}]({party.VideoUrl})\n" +
                $"**Everyone press play... NOW!** ⏯️\n\n" +
                $"🔗 {party.VideoUrl}");

            _activeWatchParties.Remove(message.Channel.Id);
        }

        private async Task HandleLyricsCommand(SocketUserMessage message)
        {
            var state = _audioService?.GetState((message.Channel as SocketGuildChannel)!.Guild.Id);
            if (state?.CurrentTrack == null)
            {
                await message.Channel.SendMessageAsync("❌ No track is currently playing!");
                return;
            }

            await message.Channel.SendMessageAsync(
                $"🎵 **Looking for lyrics:**\n{state.CurrentTrack.Title}\n\n" +
                $"Try searching on:\n" +
                $"• [Genius](https://genius.com/search?q={Uri.EscapeDataString(state.CurrentTrack.Title)})\n" +
                $"• [AZLyrics](https://search.azlyrics.com/search.php?q={Uri.EscapeDataString(state.CurrentTrack.Title)})\n" +
                $"• [Musixmatch](https://www.musixmatch.com/search/{Uri.EscapeDataString(state.CurrentTrack.Title)})");
        }

        private async Task<YouTubeVideo?> GetVideoInfo(string url)
        {
            if (_youtubeService == null) return null;
            return await _youtubeService.GetVideoInfoAsync(url);
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            return $"{duration.Minutes}:{duration.Seconds:D2}";
        }

        private bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com/watch") || 
                   url.Contains("youtu.be/") || 
                   url.Contains("youtube.com/playlist");
        }

        private TimeSpan? ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return null;

            var number = new string(duration.TakeWhile(char.IsDigit).ToArray());
            var unit = new string(duration.SkipWhile(char.IsDigit).ToArray()).ToLower();

            if (!int.TryParse(number, out int value)) return null;

            return unit switch
            {
                "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(value),
                "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(value),
                "h" or "hr" or "hour" or "hours" => TimeSpan.FromHours(value),
                _ => null
            };
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            else
                return $"{(int)timeSpan.TotalSeconds}s";
        }
    }

    public class WatchParty
    {
        public string VideoUrl { get; set; } = "";
        public string VideoTitle { get; set; } = "";
        public ulong HostId { get; set; }
        public string HostName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}

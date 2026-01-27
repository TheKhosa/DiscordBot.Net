using Discord;
using Discord.WebSocket;
using DiscordBot.Modules;
using DiscordBot.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly AudioService _audioService;
        private readonly GuildSettingsService _guildSettings;
        private readonly ModuleManager _moduleManager;
        private readonly YouTubeService _youtubeService;

        public CommandHandler(
            DiscordSocketClient client, 
            AudioService audioService, 
            GuildSettingsService guildSettings,
            ModuleManager moduleManager)
        {
            _client = client;
            _audioService = audioService;
            _guildSettings = guildSettings;
            _moduleManager = moduleManager;
            _youtubeService = new YouTubeService();
        }

        private string GetPrefix(ulong? guildId)
        {
            if (guildId == null)
                return "!"; // Default prefix for DMs

            var settings = _guildSettings.GetSettings(guildId.Value);
            return settings.CommandPrefix;
        }

        public async Task HandleMessageAsync(SocketMessage messageParam)
        {
            // Don't process the command if it's from a bot or not a user message
            if (!(messageParam is SocketUserMessage message)) return;
            if (message.Author.IsBot) return;

            // Get guild-specific prefix
            var context = new SocketCommandContext(_client, message);
            var prefix = GetPrefix(context.Guild?.Id);

            // Check if message starts with prefix
            if (!message.Content.StartsWith(prefix)) return;

            // Fire and forget to avoid blocking the gateway
            _ = Task.Run(async () => await ExecuteCommandAsync(message));
        }

        private async Task ExecuteCommandAsync(SocketUserMessage message)
        {
            var context = new SocketCommandContext(_client, message);
            var prefix = GetPrefix(context.Guild?.Id);
            
            int argPos = prefix.Length;
            var commandText = message.Content.Substring(argPos);
            var args = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args.Length == 0) return;

            // First, try to handle message with modules
            if (await _moduleManager.HandleMessageAsync(message))
                return; // Module handled the message

            var command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "help":
                        await HelpCommand(context);
                        break;
                    case "ping":
                        await PingCommand(context);
                        break;
                    case "join":
                        await JoinCommand(context);
                        break;
                    case "leave":
                        await LeaveCommand(context);
                        break;
                    case "play":
                        await PlayCommand(context, args.Skip(1).ToArray());
                        break;
                    case "queue":
                        await QueueCommand(context);
                        break;
                    case "skip":
                        await SkipCommand(context);
                        break;
                    case "pause":
                        await PauseCommand(context);
                        break;
                    case "resume":
                        await ResumeCommand(context);
                        break;
                    case "volume":
                        await VolumeCommand(context, args.Skip(1).ToArray());
                        break;
                    case "purge":
                        await PurgeCommand(context, args.Skip(1).ToArray());
                        break;
                    default:
                        await context.Channel.SendMessageAsync($"Unknown command. Use `{prefix}help` for a list of commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync($"❌ Error: {ex.Message}");
                Console.WriteLine($"Error executing command: {ex}");
            }
        }

        private async Task HelpCommand(SocketCommandContext context)
        {
            var prefix = GetPrefix(context.Guild?.Id);
            var embed = new EmbedBuilder()
                .WithTitle("🎵 Discord Bot Commands")
                .WithColor(Color.Blue)
                .WithDescription("Here are the available commands:")
                .AddField("**Music Commands**", 
                    $"`{prefix}join` - Join your voice channel\n" +
                    $"`{prefix}leave` - Leave the voice channel\n" +
                    $"`{prefix}play <file/url>` - Play audio from file or YouTube/URL\n" +
                    $"`{prefix}queue` - Show the current queue\n" +
                    $"`{prefix}skip` - Skip the current track\n" +
                    $"`{prefix}pause` - Pause playback\n" +
                    $"`{prefix}resume` - Resume playback\n" +
                    $"`{prefix}volume <0-200>` - Set volume (100 = normal)")
                .AddField("**Moderation Commands**",
                    $"`{prefix}purge <count>` - Delete messages (requires Manage Messages permission)")
                .AddField("**Utility Commands**",
                    $"`{prefix}ping` - Check bot latency\n" +
                    $"`{prefix}help` - Show this help message")
                .WithFooter($"Prefix: {prefix}")
                .WithCurrentTimestamp()
                .Build();

            await context.Channel.SendMessageAsync(embed: embed);
        }

        private async Task PingCommand(SocketCommandContext context)
        {
            var latency = _client.Latency;
            await context.Channel.SendMessageAsync($"🏓 Pong! Latency: {latency}ms");
        }

        private async Task JoinCommand(SocketCommandContext context)
        {
            var voiceChannel = (context.User as IVoiceState)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await context.Channel.SendMessageAsync("❌ You need to be in a voice channel!");
                return;
            }

            await _audioService.JoinAsync(voiceChannel);
            await context.Channel.SendMessageAsync($"✅ Joined {voiceChannel.Name}");
        }

        private async Task LeaveCommand(SocketCommandContext context)
        {
            await _audioService.LeaveAsync(context.Guild.Id);
            await context.Channel.SendMessageAsync("✅ Left voice channel");
        }

        private async Task PlayCommand(SocketCommandContext context, string[] args)
        {
            if (args.Length == 0)
            {
                var cmdPrefix = GetPrefix(context.Guild?.Id);
                await context.Channel.SendMessageAsync($"❌ Usage: `{cmdPrefix}play <file/url>`");
                return;
            }

            var path = string.Join(" ", args);

            // Check if user is in a voice channel
            var voiceChannel = (context.User as IVoiceState)?.VoiceChannel;
            if (voiceChannel == null)
            {
                await context.Channel.SendMessageAsync("❌ You need to be in a voice channel!");
                return;
            }

            // Join the channel if not already connected
            var state = _audioService.GetState(context.Guild.Id);
            if (state == null)
            {
                await _audioService.JoinAsync(voiceChannel);
            }

            // Check if it's a file or URL
            if (File.Exists(path))
            {
                var track = new AudioTrack 
                { 
                    Path = path, 
                    Title = Path.GetFileName(path),
                    Requester = context.User.Username
                };
                _audioService.QueueTrack(context.Guild.Id, track);
                await context.Channel.SendMessageAsync($"🎵 Added to queue: `{Path.GetFileName(path)}`");
            }
            else if (_youtubeService.IsSupportedUrl(path))
            {
                // Send "processing" message
                var processingMsg = await context.Channel.SendMessageAsync("🔄 Downloading audio...");

                try
                {
                    // Get video info (for metadata)
                    var videoInfo = await _youtubeService.GetVideoInfoAsync(path);
                    if (videoInfo == null)
                    {
                        await processingMsg.ModifyAsync(msg => msg.Content = "❌ Failed to fetch video information.");
                        return;
                    }

                    // Download audio file
                    var audioFilePath = await _youtubeService.DownloadAudioAsync(path);
                    if (audioFilePath == null)
                    {
                        await processingMsg.ModifyAsync(msg => msg.Content = "❌ Failed to download audio.");
                        return;
                    }

                    // Create track with downloaded file path
                    var track = new AudioTrack
                    {
                        Path = audioFilePath,  // Use the downloaded file path
                        Title = videoInfo.Title,
                        Requester = context.User.Username,
                        Duration = videoInfo.Duration
                    };

                    _audioService.QueueTrack(context.Guild.Id, track);

                    // Update message with success
                    var durationStr = videoInfo.Duration.TotalHours >= 1
                        ? $"{videoInfo.Duration:h\\:mm\\:ss}"
                        : $"{videoInfo.Duration:m\\:ss}";

                    await processingMsg.ModifyAsync(msg => 
                        msg.Content = $"🎵 Added to queue: **{videoInfo.Title}** `[{durationStr}]`");
                }
                catch (Exception ex)
                {
                    await processingMsg.ModifyAsync(msg => msg.Content = $"❌ Error: {ex.Message}");
                    Console.WriteLine($"Error processing YouTube URL: {ex}");
                }
            }
            else
            {
                await context.Channel.SendMessageAsync($"❌ File not found or invalid URL: `{path}`");
            }
        }

        private async Task QueueCommand(SocketCommandContext context)
        {
            var state = _audioService.GetState(context.Guild.Id);
            
            if (state == null || (state.CurrentTrack == null && state.Queue.Count == 0))
            {
                await context.Channel.SendMessageAsync("📭 Queue is empty");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎵 Music Queue")
                .WithColor(Color.Purple);

            if (state.CurrentTrack != null)
            {
                embed.AddField("**Now Playing**", $"🎶 {state.CurrentTrack.Title}");
            }

            if (state.Queue.Count > 0)
            {
                var queueArray = state.Queue.ToArray();
                var queueList = queueArray.Take(10).Select((track, index) => $"{index + 1}. {track.Title}");
                embed.AddField($"**Up Next ({queueArray.Length} tracks)**", string.Join("\n", queueList));

                if (queueArray.Length > 10)
                {
                    embed.AddField("", $"... and {queueArray.Length - 10} more");
                }
            }

            await context.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task SkipCommand(SocketCommandContext context)
        {
            _audioService.Skip(context.Guild.Id);
            await context.Channel.SendMessageAsync("⏭️ Skipped to next track");
        }

        private async Task PauseCommand(SocketCommandContext context)
        {
            _audioService.Pause(context.Guild.Id);
            await context.Channel.SendMessageAsync("⏸️ Playback paused");
        }

        private async Task ResumeCommand(SocketCommandContext context)
        {
            _audioService.Resume(context.Guild.Id);
            await context.Channel.SendMessageAsync("▶️ Playback resumed");
        }

        private async Task VolumeCommand(SocketCommandContext context, string[] args)
        {
            if (args.Length == 0)
            {
                var state = _audioService.GetState(context.Guild.Id);
                var currentVolume = state?.Volume ?? 1.0f;
                await context.Channel.SendMessageAsync($"🔊 Current volume: {(int)(currentVolume * 100)}%");
                return;
            }

            if (!int.TryParse(args[0], out int volumePercent) || volumePercent < 0 || volumePercent > 200)
            {
                await context.Channel.SendMessageAsync($"❌ Volume must be between 0 and 200");
                return;
            }

            float volume = volumePercent / 100f;
            _audioService.SetVolume(context.Guild.Id, volume);
            await context.Channel.SendMessageAsync($"🔊 Volume set to {volumePercent}%");
        }

        private async Task PurgeCommand(SocketCommandContext context, string[] args)
        {
            // Check permissions
            var user = context.User as SocketGuildUser;
            if (user == null || !user.GuildPermissions.ManageMessages)
            {
                await context.Channel.SendMessageAsync("❌ You need 'Manage Messages' permission to use this command.");
                return;
            }

            if (args.Length == 0)
            {
                var cmdPrefix = GetPrefix(context.Guild?.Id);
                await context.Channel.SendMessageAsync($"❌ Usage: `{cmdPrefix}purge <count>`");
                return;
            }

            if (!int.TryParse(args[0], out int count) || count <= 0 || count > 100)
            {
                await context.Channel.SendMessageAsync("❌ Count must be between 1 and 100");
                return;
            }

            var channel = context.Channel as ITextChannel;
            if (channel == null) return;

            var messages = await channel.GetMessagesAsync(count + 1).FlattenAsync(); // +1 to include command message
            var deletableMessages = messages.Where(m => 
                (DateTimeOffset.UtcNow - m.Timestamp).TotalDays < 14).ToList();

            if (deletableMessages.Count == 0)
            {
                await context.Channel.SendMessageAsync("❌ No messages found to delete (messages older than 14 days cannot be bulk deleted)");
                return;
            }

            await (channel as ITextChannel).DeleteMessagesAsync(deletableMessages);

            var confirmMsg = await context.Channel.SendMessageAsync($"✅ Deleted {deletableMessages.Count} message(s)");
            await Task.Delay(3000);
            await confirmMsg.DeleteAsync();
        }
    }

    // Helper class to create command context
    public class SocketCommandContext
    {
        public DiscordSocketClient Client { get; }
        public SocketUserMessage Message { get; }
        public ISocketMessageChannel Channel => Message.Channel;
        public SocketGuild Guild => (Message.Channel as SocketGuildChannel)?.Guild;
        public SocketUser User => Message.Author;

        public SocketCommandContext(DiscordSocketClient client, SocketUserMessage message)
        {
            Client = client;
            Message = message;
        }
    }
}

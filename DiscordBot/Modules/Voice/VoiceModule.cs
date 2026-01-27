using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Voice
{
    /// <summary>
    /// Voice/TTS module with ElevenLabs integration
    /// </summary>
    public class VoiceModule : ModuleBase
    {
        public override string ModuleId => "voice";
        public override string Name => "Voice & TTS";
        public override string Description => "Text-to-speech with ElevenLabs AI voices";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private const string ElevenLabsApiKey = "sk_62a928ae7d075434d6645f4b04a015a7bd34d19a3db95fec";
        private const string ElevenLabsBaseUrl = "https://api.elevenlabs.io/v1";
        private readonly HttpClient _httpClient = new();
        private readonly string _cacheFolder = "VoiceTTS";
        
        private AudioService? _audioService;
        private Dictionary<string, ElevenLabsVoice> _availableVoices = new();

        public override async Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _audioService = services.GetService(typeof(AudioService)) as AudioService;
            
            if (!Directory.Exists(_cacheFolder))
                Directory.CreateDirectory(_cacheFolder);

            _httpClient.DefaultRequestHeaders.Add("xi-api-key", ElevenLabsApiKey);

            // Load available voices
            await LoadAvailableVoices();
            
            return;
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            if (content.StartsWith("!say") || content.StartsWith("!tts"))
            {
                await HandleSayCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!voices"))
            {
                await HandleVoicesCommand(message);
                return true;
            }

            if (content.StartsWith("!joinvoice") || content.StartsWith("!joinvc"))
            {
                await HandleJoinVoiceCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!leavevoice") || content.StartsWith("!leavevc"))
            {
                await HandleLeaveVoiceCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!previewvoice") || content.StartsWith("!pv"))
            {
                await HandlePreviewVoiceCommand(message);
                return true;
            }

            return false;
        }

        private async Task HandleSayCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!say <text>` or `!say <voice> <text>`\n\n" +
                    "**Examples:**\n" +
                    "`!say Hello everyone!` - Use default voice\n" +
                    "`!say Rachel Hello everyone!` - Use specific voice\n" +
                    "`!voices` - List available voices");
                return;
            }

            // Check if user is in a voice channel
            var user = message.Author as SocketGuildUser;
            if (user?.VoiceChannel == null)
            {
                await message.Channel.SendMessageAsync("❌ You must be in a voice channel first! Use `!joinvoice` or join a channel.");
                return;
            }

            // Parse voice and text
            string voiceId = "nDJIICjR9zfJExIFeSCN"; // Default voice ID (Rachel)
            string text;

            if (parts.Length == 2)
            {
                text = parts[1];
            }
            else
            {
                var possibleVoice = parts[1];
                var voice = _availableVoices.Values.FirstOrDefault(v => 
                    v.Name.Equals(possibleVoice, StringComparison.OrdinalIgnoreCase));
                
                if (voice != null)
                {
                    voiceId = voice.VoiceId;
                    text = parts[2];
                }
                else
                {
                    text = string.Join(" ", parts.Skip(1));
                }
            }

            // Limit text length
            if (text.Length > 500)
            {
                await message.Channel.SendMessageAsync("❌ Text is too long! Maximum 500 characters.");
                return;
            }

            await message.Channel.TriggerTypingAsync();

            // Ensure bot is in voice
            var audioState = _audioService?.GetState(guild.Id);
            if (_audioService != null && audioState == null)
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

            // Generate TTS audio
            var audioFile = await GenerateSpeech(text, voiceId);
            if (audioFile == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to generate speech! Check the voice name and try again.");
                return;
            }

            // Queue audio
            if (_audioService != null)
            {
                var track = new AudioTrack
                {
                    Title = $"TTS: {text.Substring(0, Math.Min(30, text.Length))}...",
                    Path = audioFile,
                    Requester = message.Author.Username
                };

                _audioService.QueueTrack(guild.Id, track);
                await message.AddReactionAsync(new Emoji("🔊"));
            }
        }

        private async Task HandleVoicesCommand(SocketUserMessage message)
        {
            if (_availableVoices.Count == 0)
            {
                await message.Channel.SendMessageAsync("⏳ Loading voices...");
                await LoadAvailableVoices();
            }

            if (_availableVoices.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ No voices available!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎙️ Available ElevenLabs Voices")
                .WithColor(Color.Purple)
                .WithDescription("Use `!say <voice> <text>` to speak with a specific voice")
                .WithFooter($"Total voices: {_availableVoices.Count}");

            var voiceGroups = _availableVoices.Values
                .GroupBy(v => v.Category ?? "Other")
                .OrderBy(g => g.Key);

            foreach (var group in voiceGroups)
            {
                var voiceList = string.Join("\n", group.Take(10).Select(v => 
                    $"**{v.Name}** - {v.Description ?? "No description"}"));

                if (!string.IsNullOrEmpty(voiceList))
                {
                    embed.AddField(group.Key, voiceList, inline: false);
                }
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleJoinVoiceCommand(SocketUserMessage message, SocketGuild guild)
        {
            var user = message.Author as SocketGuildUser;
            if (user?.VoiceChannel == null)
            {
                await message.Channel.SendMessageAsync("❌ You must be in a voice channel first!");
                return;
            }

            if (_audioService == null)
            {
                await message.Channel.SendMessageAsync("❌ Audio service not available!");
                return;
            }

            try
            {
                await _audioService.JoinAsync(user.VoiceChannel);
                await message.Channel.SendMessageAsync($"✅ Joined {user.VoiceChannel.Mention}!");
            }
            catch
            {
                await message.Channel.SendMessageAsync("❌ Failed to join voice channel!");
            }
        }

        private async Task HandleLeaveVoiceCommand(SocketUserMessage message, SocketGuild guild)
        {
            if (_audioService == null)
            {
                await message.Channel.SendMessageAsync("❌ Audio service not available!");
                return;
            }

            await _audioService.LeaveAsync(guild.Id);
            await message.Channel.SendMessageAsync("✅ Left voice channel!");
        }

        private async Task HandlePreviewVoiceCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!previewvoice <voice-name>`");
                return;
            }

            var voiceName = parts[1];
            var voice = _availableVoices.Values.FirstOrDefault(v => 
                v.Name.Equals(voiceName, StringComparison.OrdinalIgnoreCase));

            if (voice == null)
            {
                await message.Channel.SendMessageAsync($"❌ Voice '{voiceName}' not found! Use `!voices` to see available voices.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"🎙️ Voice: {voice.Name}")
                .WithColor(Color.Purple)
                .AddField("Voice ID", voice.VoiceId, inline: true)
                .AddField("Category", voice.Category ?? "N/A", inline: true);

            if (!string.IsNullOrEmpty(voice.Description))
            {
                embed.AddField("Description", voice.Description, inline: false);
            }

            if (voice.Labels != null && voice.Labels.Any())
            {
                embed.AddField("Labels", string.Join(", ", voice.Labels), inline: false);
            }

            if (!string.IsNullOrEmpty(voice.PreviewUrl))
            {
                embed.AddField("Preview", $"[Listen to sample]({voice.PreviewUrl})", inline: false);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task<string?> GenerateSpeech(string text, string voiceId)
        {
            try
            {
                // Create cache filename
                var textHash = text.GetHashCode().ToString("X");
                var voiceName = _availableVoices.Values.FirstOrDefault(v => v.VoiceId == voiceId)?.Name ?? "default";
                var cacheFile = Path.Combine(_cacheFolder, $"{voiceName}_{textHash}.mp3");

                // Check cache
                if (File.Exists(cacheFile))
                {
                    Console.WriteLine($"[Voice] Using cached audio: {cacheFile}");
                    return cacheFile;
                }

                // Generate new audio
                var url = $"{ElevenLabsBaseUrl}/text-to-speech/{voiceId}";
                
                var requestBody = new
                {
                    text = text,
                    model_id = "eleven_monolingual_v1",
                    voice_settings = new
                    {
                        stability = 0.5,
                        similarity_boost = 0.75,
                        style = 0.0,
                        use_speaker_boost = true
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Voice] API error: {response.StatusCode} - {error}");
                    return null;
                }

                var audioData = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(cacheFile, audioData);

                Console.WriteLine($"[Voice] Generated and cached audio: {cacheFile} ({audioData.Length} bytes)");
                return cacheFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Voice] Error generating speech: {ex.Message}");
                return null;
            }
        }

        private async Task LoadAvailableVoices()
        {
            try
            {
                var url = $"{ElevenLabsBaseUrl}/voices";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var voices = json["voices"];
                if (voices != null)
                {
                    foreach (var voiceJson in voices)
                    {
                        var voice = new ElevenLabsVoice
                        {
                            VoiceId = voiceJson["voice_id"]?.ToString() ?? "",
                            Name = voiceJson["name"]?.ToString() ?? "",
                            Category = voiceJson["category"]?.ToString(),
                            Description = voiceJson["description"]?.ToString(),
                            PreviewUrl = voiceJson["preview_url"]?.ToString(),
                            Labels = voiceJson["labels"]?
                                .ToObject<Dictionary<string, string>>()
                                ?.Select(kvp => $"{kvp.Key}: {kvp.Value}")
                                .ToList()
                        };

                        _availableVoices[voice.Name.ToLower()] = voice;
                    }

                    Console.WriteLine($"[Voice] Loaded {_availableVoices.Count} voices from ElevenLabs");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Voice] Error loading voices: {ex.Message}");
            }
        }
    }

    public class ElevenLabsVoice
    {
        public string VoiceId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? PreviewUrl { get; set; }
        public List<string>? Labels { get; set; }
    }
}

using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System.Collections.Concurrent;
using System.Diagnostics;

public class AudioService
{
    private readonly ConcurrentDictionary<ulong, GuildAudioState> _audioStates = new();

    public async Task JoinAsync(IVoiceChannel channel)
    {
        var guildId = channel.GuildId;
        
        if (_audioStates.ContainsKey(guildId))
        {
            throw new InvalidOperationException("Already connected to a voice channel in this guild");
        }

        var audioClient = await channel.ConnectAsync();
        var state = new GuildAudioState(audioClient, channel);
        _audioStates[guildId] = state;
        
        // Start queue processor
        _ = Task.Run(() => ProcessQueueAsync(guildId));
    }

    public async Task LeaveAsync(ulong guildId)
    {
        if (_audioStates.TryRemove(guildId, out var state))
        {
            state.CancellationTokenSource.Cancel();
            await state.AudioClient.StopAsync();
        }
    }

    public void QueueTrack(ulong guildId, AudioTrack track)
    {
        if (_audioStates.TryGetValue(guildId, out var state))
        {
            state.Queue.Enqueue(track);
        }
    }

    public void Skip(ulong guildId)
    {
        if (_audioStates.TryGetValue(guildId, out var state))
        {
            state.SkipCurrent = true;
        }
    }

    public void Pause(ulong guildId)
    {
        if (_audioStates.TryGetValue(guildId, out var state))
        {
            state.IsPaused = true;
        }
    }

    public void Resume(ulong guildId)
    {
        if (_audioStates.TryGetValue(guildId, out var state))
        {
            state.IsPaused = false;
        }
    }

    public void ClearQueue(ulong guildId)
    {
        if (_audioStates.TryGetValue(guildId, out var state))
        {
            while (state.Queue.TryDequeue(out _)) { }
        }
    }

    public void SetVolume(ulong guildId, float volume)
    {
        if (_audioStates.TryGetValue(guildId, out var state))
        {
            state.Volume = Math.Clamp(volume, 0.0f, 2.0f);
        }
    }

    public GuildAudioState? GetState(ulong guildId)
    {
        _audioStates.TryGetValue(guildId, out var state);
        return state;
    }

    private async Task ProcessQueueAsync(ulong guildId)
    {
        while (_audioStates.TryGetValue(guildId, out var state))
        {
            if (state.CancellationTokenSource.Token.IsCancellationRequested)
                break;

            if (state.IsPaused)
            {
                await Task.Delay(100);
                continue;
            }

            if (state.Queue.TryDequeue(out var track))
            {
                state.CurrentTrack = track;
                state.SkipCurrent = false;
                
                try
                {
                    await PlayTrackAsync(state, track);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing track: {ex.Message}");
                }
                finally
                {
                    // Clean up downloaded file if it's a temp file
                    try
                    {
                        if (track.Path.Contains(Path.GetTempPath()) && File.Exists(track.Path))
                        {
                            File.Delete(track.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting temp file: {ex.Message}");
                    }
                }
                
                state.CurrentTrack = null;
            }
            else
            {
                await Task.Delay(500);
            }
        }
    }

    private async Task PlayTrackAsync(GuildAudioState state, AudioTrack track)
    {
        if (!File.Exists(track.Path))
        {
            Console.WriteLine($"Audio file not found: {track.Path}");
            return;
        }

        using var discord = state.AudioClient.CreatePCMStream(AudioApplication.Music);

        try
        {
            // Open audio file with NAudio
            using var audioFile = new AudioFileReader(track.Path);
            
            // Create resampler to convert to Discord's format (48kHz, 2 channels, 16-bit PCM)
            var outFormat = new WaveFormat(48000, 16, 2);
            using var resampler = new MediaFoundationResampler(audioFile, outFormat);
            
            // Buffer for reading from NAudio
            var buffer = new byte[16384];
            int bytesRead;

            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (state.SkipCurrent || state.CancellationTokenSource.Token.IsCancellationRequested)
                    break;

                if (!state.IsPaused)
                {
                    // Apply volume to PCM data (16-bit signed samples)
                    if (state.Volume != 1.0f)
                    {
                        for (int i = 0; i < bytesRead - 1; i += 2)
                        {
                            // Read 16-bit sample (little-endian)
                            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                            
                            // Apply volume and clamp to prevent distortion
                            sample = (short)Math.Clamp(sample * state.Volume, short.MinValue, short.MaxValue);
                            
                            // Write back
                            buffer[i] = (byte)(sample & 0xFF);
                            buffer[i + 1] = (byte)(sample >> 8);
                        }
                    }

                    await discord.WriteAsync(buffer, 0, bytesRead);
                }
                else
                {
                    // Small delay when paused to reduce CPU usage
                    await Task.Delay(20);
                }
            }

            await discord.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during playback: {ex.Message}");
        }
    }
}

public class GuildAudioState
{
    public IAudioClient AudioClient { get; }
    public IVoiceChannel VoiceChannel { get; }
    public ConcurrentQueue<AudioTrack> Queue { get; } = new();
    public AudioTrack? CurrentTrack { get; set; }
    public bool IsPaused { get; set; }
    public bool SkipCurrent { get; set; }
    public float Volume { get; set; } = 1.0f;
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public GuildAudioState(IAudioClient audioClient, IVoiceChannel voiceChannel)
    {
        AudioClient = audioClient;
        VoiceChannel = voiceChannel;
    }
}

public class AudioTrack
{
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
    public string Requester { get; set; } = "";
    public TimeSpan Duration { get; set; }
}

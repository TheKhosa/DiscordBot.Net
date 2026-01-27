using Discord;
using Discord.Audio;
using Discord.WebSocket;
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
        // Determine if path is a URL or local file
        string inputArg = track.Path.StartsWith("http://") || track.Path.StartsWith("https://")
            ? track.Path  // URL doesn't need quotes
            : $"\"{track.Path}\"";  // Local file needs quotes

        using var ffmpeg = Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i {inputArg} -ac 2 -f s16le -ar 48000 -af \"volume={state.Volume}\" pipe:1",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (ffmpeg == null) return;

        using var output = ffmpeg.StandardOutput.BaseStream;
        using var discord = state.AudioClient.CreatePCMStream(AudioApplication.Music);

        var buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await output.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            if (state.SkipCurrent || state.CancellationTokenSource.Token.IsCancellationRequested)
                break;

            while (state.IsPaused)
            {
                await Task.Delay(100);
                if (state.SkipCurrent) break;
            }

            await discord.WriteAsync(buffer, 0, bytesRead);
        }

        await discord.FlushAsync();
        
        if (!ffmpeg.HasExited)
            ffmpeg.Kill();
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

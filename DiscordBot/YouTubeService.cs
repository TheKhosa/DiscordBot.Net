using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class YouTubeService
    {
        public async Task<YouTubeVideo?> GetVideoInfoAsync(string url)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $"--dump-json --no-playlist \"{url}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"yt-dlp error: {error}");
                    return null;
                }

                var json = JsonDocument.Parse(output);
                var root = json.RootElement;

                return new YouTubeVideo
                {
                    Url = url,
                    Title = root.GetProperty("title").GetString() ?? "Unknown Title",
                    Duration = TimeSpan.FromSeconds(root.GetProperty("duration").GetDouble()),
                    Uploader = root.GetProperty("uploader").GetString() ?? "Unknown",
                    Thumbnail = root.GetProperty("thumbnail").GetString() ?? ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting video info: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetAudioStreamUrlAsync(string url)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $"-f bestaudio --get-url --no-playlist \"{url}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"yt-dlp error: {error}");
                    return null;
                }

                return output.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting audio stream: {ex.Message}");
                return null;
            }
        }

        public bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") || 
                   url.Contains("youtu.be") || 
                   url.Contains("music.youtube.com");
        }

        public bool IsSupportedUrl(string url)
        {
            // yt-dlp supports many sites, but we'll validate it's a proper URL
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class YouTubeVideo
    {
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public string Uploader { get; set; } = "";
        public string Thumbnail { get; set; } = "";
    }
}

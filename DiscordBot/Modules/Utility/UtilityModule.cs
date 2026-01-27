using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace DiscordBot.Modules.Utility
{
    /// <summary>
    /// Module for utility commands like embeds and search
    /// </summary>
    public class UtilityModule : ModuleBase
    {
        public override string ModuleId => "utility";
        public override string Name => "Utility Commands";
        public override string Description => "Embed builder, search, and other utilities";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private HttpClient? _httpClient;

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DiscordBot/1.0");
            return base.InitializeAsync(client, services);
        }

        public override Task ShutdownAsync()
        {
            _httpClient?.Dispose();
            return base.ShutdownAsync();
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            var content = message.Content.ToLower();

            if (content.StartsWith("!embed"))
            {
                await HandleEmbedCommand(message);
                return true;
            }

            if (content.StartsWith("!search") || content.StartsWith("!google"))
            {
                await HandleSearchCommand(message);
                return true;
            }

            if (content.StartsWith("!wiki") || content.StartsWith("!wikipedia"))
            {
                await HandleWikipediaCommand(message);
                return true;
            }

            if (content.StartsWith("!weather"))
            {
                await HandleWeatherCommand(message);
                return true;
            }

            if (content.StartsWith("!serverinfo"))
            {
                await HandleServerInfoCommand(message);
                return true;
            }

            if (content.StartsWith("!userinfo"))
            {
                await HandleUserInfoCommand(message);
                return true;
            }

            if (content.StartsWith("!avatar"))
            {
                await HandleAvatarCommand(message);
                return true;
            }

            return false;
        }

        private async Task HandleEmbedCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split('|', StringSplitOptions.TrimEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!embed Title | Description | Color (optional)`\n" +
                    "Example: `!embed My Title | This is the description | Blue`\n" +
                    "Available colors: Red, Blue, Green, Gold, Purple, Orange");
                return;
            }

            var title = parts[0].Substring(6).Trim(); // Remove "!embed"
            var description = parts.Length > 1 ? parts[1] : "";
            var colorName = parts.Length > 2 ? parts[2].ToLower() : "blue";

            var color = colorName switch
            {
                "red" => Color.Red,
                "blue" => Color.Blue,
                "green" => Color.Green,
                "gold" => Color.Gold,
                "purple" => Color.Purple,
                "orange" => Color.Orange,
                _ => Color.Blue
            };

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(color)
                .WithFooter($"Created by {message.Author.Username}")
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
            await message.DeleteAsync();
        }

        private async Task HandleSearchCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.TrimEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!search <query>`");
                return;
            }

            var query = parts[1];
            var encodedQuery = HttpUtility.UrlEncode(query);
            var searchUrl = $"https://www.google.com/search?q={encodedQuery}";

            var embed = new EmbedBuilder()
                .WithTitle($"🔍 Search Results for: {query}")
                .WithDescription($"[Click here to view results on Google]({searchUrl})")
                .WithColor(Color.Blue)
                .WithFooter("Powered by Google Search")
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleWikipediaCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.TrimEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!wiki <search term>`");
                return;
            }

            var query = parts[1];
            var encodedQuery = HttpUtility.UrlEncode(query);
            var apiUrl = $"https://en.wikipedia.org/api/rest_v1/page/summary/{encodedQuery}";

            try
            {
                var response = await _httpClient!.GetStringAsync(apiUrl);
                var json = JsonDocument.Parse(response);
                var root = json.RootElement;

                var title = root.GetProperty("title").GetString();
                var extract = root.GetProperty("extract").GetString();
                var pageUrl = root.GetProperty("content_urls").GetProperty("desktop").GetProperty("page").GetString();
                
                string? thumbnail = null;
                if (root.TryGetProperty("thumbnail", out var thumbProp))
                {
                    thumbnail = thumbProp.GetProperty("source").GetString();
                }

                var embed = new EmbedBuilder()
                    .WithTitle($"📖 {title}")
                    .WithDescription(extract?.Length > 500 ? extract.Substring(0, 500) + "..." : extract)
                    .WithUrl(pageUrl)
                    .WithColor(Color.Blue)
                    .WithFooter("Source: Wikipedia")
                    .WithCurrentTimestamp();

                if (thumbnail != null)
                {
                    embed.WithThumbnailUrl(thumbnail);
                }

                await message.Channel.SendMessageAsync(embed: embed.Build());
            }
            catch
            {
                await message.Channel.SendMessageAsync("❌ Article not found or Wikipedia API error!");
            }
        }

        private async Task HandleWeatherCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.TrimEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!weather <city>`");
                return;
            }

            await message.Channel.SendMessageAsync(
                "🌤️ Weather feature requires an API key from OpenWeatherMap.\n" +
                "For now, try searching: `!search weather " + parts[1] + "`");
        }

        private async Task HandleServerInfoCommand(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return;

            var guild = guildChannel.Guild;
            var owner = await Client!.Rest.GetUserAsync(guild.OwnerId);

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Server Info: {guild.Name}")
                .WithThumbnailUrl(guild.IconUrl)
                .WithColor(Color.Blue)
                .AddField("Owner", owner?.Username ?? "Unknown", inline: true)
                .AddField("Created", guild.CreatedAt.ToString("yyyy-MM-dd"), inline: true)
                .AddField("Region", "Auto", inline: true)
                .AddField("Members", guild.MemberCount.ToString("N0"), inline: true)
                .AddField("Channels", guild.Channels.Count.ToString(), inline: true)
                .AddField("Roles", guild.Roles.Count.ToString(), inline: true)
                .AddField("Boost Level", $"Tier {guild.PremiumTier}", inline: true)
                .AddField("Boosts", guild.PremiumSubscriptionCount.ToString(), inline: true)
                .WithFooter($"Server ID: {guild.Id}")
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleUserInfoCommand(SocketUserMessage message)
        {
            var user = message.MentionedUsers.Count > 0 
                ? message.MentionedUsers.First() 
                : message.Author;

            var embed = new EmbedBuilder()
                .WithTitle($"👤 User Info: {user.Username}")
                .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                .WithColor(Color.Blue)
                .AddField("Username", user.Username, inline: true)
                .AddField("Discriminator", user.Discriminator, inline: true)
                .AddField("ID", user.Id.ToString(), inline: true)
                .AddField("Created", user.CreatedAt.ToString("yyyy-MM-dd"), inline: true)
                .AddField("Bot", user.IsBot ? "Yes" : "No", inline: true);

            if (user is SocketGuildUser guildUser)
            {
                embed.AddField("Joined Server", guildUser.JoinedAt?.ToString("yyyy-MM-dd") ?? "Unknown", inline: true);
                embed.AddField("Roles", guildUser.Roles.Count.ToString(), inline: true);
                embed.AddField("Nickname", guildUser.Nickname ?? "None", inline: true);
            }

            embed.WithCurrentTimestamp();

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleAvatarCommand(SocketUserMessage message)
        {
            var user = message.MentionedUsers.Count > 0 
                ? message.MentionedUsers.First() 
                : message.Author;

            var avatarUrl = user.GetAvatarUrl(size: 2048) ?? user.GetDefaultAvatarUrl();

            var embed = new EmbedBuilder()
                .WithTitle($"{user.Username}'s Avatar")
                .WithImageUrl(avatarUrl)
                .WithColor(Color.Blue)
                .WithUrl(avatarUrl)
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }
    }
}

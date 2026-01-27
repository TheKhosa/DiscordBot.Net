using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Welcome
{
    /// <summary>
    /// Welcome and goodbye messages module
    /// </summary>
    public class WelcomeModule : ModuleBase
    {
        public override string ModuleId => "welcome";
        public override string Name => "Welcome & Goodbye";
        public override string Description => "Custom welcome and goodbye messages for new members";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly string _dataFolder = "WelcomeData";
        private Dictionary<ulong, WelcomeData> _guildData = new();

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadData();
            
            // Subscribe to user join/leave events
            client.UserJoined += HandleUserJoined;
            client.UserLeft += HandleUserLeft;
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            var user = message.Author as SocketGuildUser;
            if (user == null) return false;

            if (content.StartsWith("!setwelcome"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission to use this command!");
                    return true;
                }
                await HandleSetWelcomeCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!setgoodbye"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission to use this command!");
                    return true;
                }
                await HandleSetGoodbyeCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!testwelcome"))
            {
                if (!user.GuildPermissions.ManageGuild)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Server permission to use this command!");
                    return true;
                }
                await HandleTestWelcomeCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!welcomeinfo"))
            {
                await HandleWelcomeInfoCommand(message, guildChannel.Guild);
                return true;
            }

            return false;
        }

        private async Task HandleSetWelcomeCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!setwelcome <message>`\n\n" +
                    "**Placeholders:**\n" +
                    "`{user}` - Mentions the new user\n" +
                    "`{username}` - User's username\n" +
                    "`{server}` - Server name\n" +
                    "`{membercount}` - Total member count\n\n" +
                    "**Example:** `!setwelcome Welcome {user} to {server}! You are member #{membercount}`\n\n" +
                    "**Disable:** `!setwelcome off`");
                return;
            }

            var data = GetGuildData(guildId);

            if (parts[1].ToLower() == "off")
            {
                data.WelcomeEnabled = false;
                SaveData();
                await message.Channel.SendMessageAsync("✅ Welcome messages disabled.");
                return;
            }

            data.WelcomeEnabled = true;
            data.WelcomeChannelId = message.Channel.Id;
            data.WelcomeMessage = parts[1];
            SaveData();

            await message.Channel.SendMessageAsync($"✅ Welcome message set! New members will be greeted in this channel.\n\n**Preview:** {FormatMessage(data.WelcomeMessage, message.Author, message.Channel as SocketGuildChannel)}");
        }

        private async Task HandleSetGoodbyeCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!setgoodbye <message>`\n\n" +
                    "**Placeholders:**\n" +
                    "`{user}` - User's name (not mention, they're gone)\n" +
                    "`{username}` - User's username\n" +
                    "`{server}` - Server name\n" +
                    "`{membercount}` - Total member count\n\n" +
                    "**Example:** `!setgoodbye Goodbye {username}, thanks for being part of {server}!`\n\n" +
                    "**Disable:** `!setgoodbye off`");
                return;
            }

            var data = GetGuildData(guildId);

            if (parts[1].ToLower() == "off")
            {
                data.GoodbyeEnabled = false;
                SaveData();
                await message.Channel.SendMessageAsync("✅ Goodbye messages disabled.");
                return;
            }

            data.GoodbyeEnabled = true;
            data.GoodbyeChannelId = message.Channel.Id;
            data.GoodbyeMessage = parts[1];
            SaveData();

            await message.Channel.SendMessageAsync($"✅ Goodbye message set! Members leaving will get a farewell in this channel.\n\n**Preview:** {FormatMessage(data.GoodbyeMessage, message.Author, message.Channel as SocketGuildChannel, false)}");
        }

        private async Task HandleTestWelcomeCommand(SocketUserMessage message, SocketGuild guild)
        {
            var data = GetGuildData(guild.Id);

            if (!data.WelcomeEnabled)
            {
                await message.Channel.SendMessageAsync("❌ Welcome messages are not enabled! Use `!setwelcome` to enable them.");
                return;
            }

            var formatted = FormatMessage(data.WelcomeMessage, message.Author, guild);
            await message.Channel.SendMessageAsync($"**Test Welcome Message:**\n\n{formatted}");
        }

        private async Task HandleWelcomeInfoCommand(SocketUserMessage message, SocketGuild guild)
        {
            var data = GetGuildData(guild.Id);

            var embed = new EmbedBuilder()
                .WithTitle("👋 Welcome & Goodbye Configuration")
                .WithColor(Color.Blue);

            // Welcome info
            if (data.WelcomeEnabled && data.WelcomeChannelId.HasValue)
            {
                var channel = guild.GetTextChannel(data.WelcomeChannelId.Value);
                embed.AddField("Welcome Messages", 
                    $"**Status:** ✅ Enabled\n" +
                    $"**Channel:** {(channel != null ? channel.Mention : "Unknown")}\n" +
                    $"**Message:** {data.WelcomeMessage}", 
                    inline: false);
            }
            else
            {
                embed.AddField("Welcome Messages", "❌ Disabled", inline: false);
            }

            // Goodbye info
            if (data.GoodbyeEnabled && data.GoodbyeChannelId.HasValue)
            {
                var channel = guild.GetTextChannel(data.GoodbyeChannelId.Value);
                embed.AddField("Goodbye Messages", 
                    $"**Status:** ✅ Enabled\n" +
                    $"**Channel:** {(channel != null ? channel.Mention : "Unknown")}\n" +
                    $"**Message:** {data.GoodbyeMessage}", 
                    inline: false);
            }
            else
            {
                embed.AddField("Goodbye Messages", "❌ Disabled", inline: false);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleUserJoined(SocketGuildUser user)
        {
            var data = GetGuildData(user.Guild.Id);

            if (!data.WelcomeEnabled || !data.WelcomeChannelId.HasValue) return;

            var channel = user.Guild.GetTextChannel(data.WelcomeChannelId.Value);
            if (channel == null) return;

            var formatted = FormatMessage(data.WelcomeMessage, user, user.Guild);

            try
            {
                await channel.SendMessageAsync(formatted);
                Console.WriteLine($"[Welcome] Sent welcome message for {user.Username} in {user.Guild.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Welcome] Error sending welcome message: {ex.Message}");
            }
        }

        private async Task HandleUserLeft(SocketGuild guild, SocketUser user)
        {
            var data = GetGuildData(guild.Id);

            if (!data.GoodbyeEnabled || !data.GoodbyeChannelId.HasValue) return;

            var channel = guild.GetTextChannel(data.GoodbyeChannelId.Value);
            if (channel == null) return;

            var formatted = FormatMessage(data.GoodbyeMessage, user, guild, false);

            try
            {
                await channel.SendMessageAsync(formatted);
                Console.WriteLine($"[Welcome] Sent goodbye message for {user.Username} in {guild.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Welcome] Error sending goodbye message: {ex.Message}");
            }
        }

        private string FormatMessage(string template, IUser user, SocketGuild guild, bool mention = true)
        {
            return template
                .Replace("{user}", mention ? user.Mention : user.Username)
                .Replace("{username}", user.Username)
                .Replace("{server}", guild.Name)
                .Replace("{membercount}", guild.MemberCount.ToString());
        }

        private string FormatMessage(string template, IUser user, SocketGuildChannel? channel, bool mention = true)
        {
            if (channel == null) return template;

            return template
                .Replace("{user}", mention ? user.Mention : user.Username)
                .Replace("{username}", user.Username)
                .Replace("{server}", channel.Guild.Name)
                .Replace("{membercount}", channel.Guild.MemberCount.ToString());
        }

        private WelcomeData GetGuildData(ulong guildId)
        {
            if (!_guildData.ContainsKey(guildId))
                _guildData[guildId] = new WelcomeData();

            return _guildData[guildId];
        }

        private void LoadData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var file in Directory.GetFiles(_dataFolder, "*.json"))
            {
                var guildIdStr = Path.GetFileNameWithoutExtension(file);
                if (ulong.TryParse(guildIdStr, out ulong guildId))
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<WelcomeData>(json);
                    if (data != null)
                        _guildData[guildId] = data;
                }
            }

            Console.WriteLine($"[Welcome] Loaded data for {_guildData.Count} guild(s)");
        }

        private void SaveData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var kvp in _guildData)
            {
                var filePath = Path.Combine(_dataFolder, $"{kvp.Key}.json");
                var json = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }
    }
}

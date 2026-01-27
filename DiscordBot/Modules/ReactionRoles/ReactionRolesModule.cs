using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.ReactionRoles
{
    /// <summary>
    /// Reaction roles module for self-assignable roles
    /// </summary>
    public class ReactionRolesModule : ModuleBase
    {
        public override string ModuleId => "reactionroles";
        public override string Name => "Reaction Roles";
        public override string Description => "Self-assignable roles via reactions";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly string _dataFolder = "ReactionRolesData";
        private Dictionary<ulong, ReactionRolesData> _guildData = new();

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadData();
            
            // Subscribe to reaction events
            client.ReactionAdded += HandleReactionAdded;
            client.ReactionRemoved += HandleReactionRemoved;
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            var user = message.Author as SocketGuildUser;
            if (user == null) return false;

            if (content.StartsWith("!reactionrole") || content.StartsWith("!rr"))
            {
                if (!user.GuildPermissions.ManageRoles)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Roles permission to use this command!");
                    return true;
                }
                await HandleReactionRoleCommand(message, guildChannel.Guild);
                return true;
            }

            if (content.StartsWith("!listreactionroles") || content.StartsWith("!listrr"))
            {
                await HandleListReactionRolesCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!removereactionrole") || content.StartsWith("!removerr"))
            {
                if (!user.GuildPermissions.ManageRoles)
                {
                    await message.Channel.SendMessageAsync("❌ You need Manage Roles permission to use this command!");
                    return true;
                }
                await HandleRemoveReactionRoleCommand(message, guildId);
                return true;
            }

            return false;
        }

        private async Task HandleReactionRoleCommand(SocketUserMessage message, SocketGuild guild)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 4)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!reactionrole <message-id> <emoji> <@role>`\n\n" +
                    "**Example:** `!reactionrole 123456789 👍 @Member`\n\n" +
                    "**How to get message ID:**\n" +
                    "1. Enable Developer Mode (User Settings → Advanced → Developer Mode)\n" +
                    "2. Right-click message → Copy ID");
                return;
            }

            if (!ulong.TryParse(parts[1], out ulong messageId))
            {
                await message.Channel.SendMessageAsync("❌ Invalid message ID!");
                return;
            }

            var emoji = parts[2];
            
            if (message.MentionedRoles.Count == 0)
            {
                await message.Channel.SendMessageAsync("❌ You must mention a role!");
                return;
            }

            var role = message.MentionedRoles.First();

            // Find the message
            var channel = message.Channel as ITextChannel;
            if (channel == null) return;

            IMessage? targetMessage = null;
            try
            {
                targetMessage = await channel.GetMessageAsync(messageId);
            }
            catch
            {
                await message.Channel.SendMessageAsync("❌ Message not found! Make sure the ID is correct and the message is in this channel.");
                return;
            }

            if (targetMessage == null)
            {
                await message.Channel.SendMessageAsync("❌ Message not found!");
                return;
            }

            // Add reaction to message
            try
            {
                if (Emote.TryParse(emoji, out var customEmote))
                {
                    await targetMessage.AddReactionAsync(customEmote);
                }
                else
                {
                    await targetMessage.AddReactionAsync(new Emoji(emoji));
                }
            }
            catch
            {
                await message.Channel.SendMessageAsync("❌ Invalid emoji! Try a standard emoji or a custom one from this server.");
                return;
            }

            // Store in data
            var data = GetGuildData(guild.Id);
            
            if (!data.Messages.ContainsKey(messageId))
            {
                data.Messages[messageId] = new ReactionRoleMessage
                {
                    MessageId = messageId,
                    ChannelId = channel.Id,
                    GuildId = guild.Id
                };
            }

            data.Messages[messageId].EmojiRoleMap[emoji] = role.Id;
            SaveData();

            await message.Channel.SendMessageAsync($"✅ Reaction role created! React with {emoji} on the target message to get {role.Mention}");
        }

        private async Task HandleListReactionRolesCommand(SocketUserMessage message, ulong guildId)
        {
            var data = GetGuildData(guildId);

            if (data.Messages.Count == 0)
            {
                await message.Channel.SendMessageAsync("No reaction roles configured in this server.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎭 Reaction Roles")
                .WithColor(Color.Purple);

            foreach (var msg in data.Messages.Values.Take(10))
            {
                var channel = await Client!.GetChannelAsync(msg.ChannelId) as ITextChannel;
                var channelMention = channel != null ? $"<#{channel.Id}>" : "Unknown Channel";

                var roles = new List<string>();
                foreach (var mapping in msg.EmojiRoleMap)
                {
                    var roleMention = $"<@&{mapping.Value}>";
                    roles.Add($"{mapping.Key} → {roleMention}");
                }

                embed.AddField(
                    $"Message ID: {msg.MessageId}",
                    $"**Channel:** {channelMention}\n" +
                    $"**Mappings:**\n{string.Join("\n", roles)}",
                    inline: false
                );
            }

            if (data.Messages.Count > 10)
            {
                embed.WithFooter($"Showing 10 of {data.Messages.Count} messages");
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleRemoveReactionRoleCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!removereactionrole <message-id>`");
                return;
            }

            if (!ulong.TryParse(parts[1], out ulong messageId))
            {
                await message.Channel.SendMessageAsync("❌ Invalid message ID!");
                return;
            }

            var data = GetGuildData(guildId);

            if (!data.Messages.ContainsKey(messageId))
            {
                await message.Channel.SendMessageAsync("❌ No reaction roles found for that message!");
                return;
            }

            // Remove all reactions from the message
            var msg = data.Messages[messageId];
            var channel = await Client!.GetChannelAsync(msg.ChannelId) as ITextChannel;
            if (channel != null)
            {
                try
                {
                    var targetMessage = await channel.GetMessageAsync(messageId);
                    if (targetMessage != null)
                    {
                        await targetMessage.RemoveAllReactionsAsync();
                    }
                }
                catch { }
            }

            data.Messages.Remove(messageId);
            SaveData();

            await message.Channel.SendMessageAsync($"✅ Removed reaction roles from message {messageId}");
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            // Ignore bot reactions
            if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

            var messageId = reaction.MessageId;
            var userId = reaction.UserId;

            // Check if this message has reaction roles
            var allData = _guildData.Values;
            ReactionRoleMessage? roleMessage = null;

            foreach (var guildData in allData)
            {
                if (guildData.Messages.ContainsKey(messageId))
                {
                    roleMessage = guildData.Messages[messageId];
                    break;
                }
            }

            if (roleMessage == null) return;

            // Check if this emoji has a role
            var emojiStr = reaction.Emote.ToString();
            if (!roleMessage.EmojiRoleMap.ContainsKey(emojiStr)) return;

            var roleId = roleMessage.EmojiRoleMap[emojiStr];

            // Add role to user
            var guild = Client!.GetGuild(roleMessage.GuildId);
            if (guild == null) return;

            var user = guild.GetUser(userId);
            if (user == null) return;

            var role = guild.GetRole(roleId);
            if (role == null) return;

            try
            {
                await user.AddRoleAsync(role);
                Console.WriteLine($"[ReactionRoles] Added role {role.Name} to {user.Username} in {guild.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReactionRoles] Error adding role: {ex.Message}");
            }
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            // Ignore bot reactions
            if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

            var messageId = reaction.MessageId;
            var userId = reaction.UserId;

            // Check if this message has reaction roles
            var allData = _guildData.Values;
            ReactionRoleMessage? roleMessage = null;

            foreach (var guildData in allData)
            {
                if (guildData.Messages.ContainsKey(messageId))
                {
                    roleMessage = guildData.Messages[messageId];
                    break;
                }
            }

            if (roleMessage == null) return;

            // Check if this emoji has a role
            var emojiStr = reaction.Emote.ToString();
            if (!roleMessage.EmojiRoleMap.ContainsKey(emojiStr)) return;

            var roleId = roleMessage.EmojiRoleMap[emojiStr];

            // Remove role from user
            var guild = Client!.GetGuild(roleMessage.GuildId);
            if (guild == null) return;

            var user = guild.GetUser(userId);
            if (user == null) return;

            var role = guild.GetRole(roleId);
            if (role == null) return;

            try
            {
                await user.RemoveRoleAsync(role);
                Console.WriteLine($"[ReactionRoles] Removed role {role.Name} from {user.Username} in {guild.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReactionRoles] Error removing role: {ex.Message}");
            }
        }

        private ReactionRolesData GetGuildData(ulong guildId)
        {
            if (!_guildData.ContainsKey(guildId))
                _guildData[guildId] = new ReactionRolesData();

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
                    var data = JsonConvert.DeserializeObject<ReactionRolesData>(json);
                    if (data != null)
                        _guildData[guildId] = data;
                }
            }

            Console.WriteLine($"[ReactionRoles] Loaded data for {_guildData.Count} guild(s)");
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

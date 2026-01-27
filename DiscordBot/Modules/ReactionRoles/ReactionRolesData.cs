using System.Collections.Generic;

namespace DiscordBot.Modules.ReactionRoles
{
    /// <summary>
    /// Data model for reaction roles
    /// </summary>
    public class ReactionRolesData
    {
        public Dictionary<ulong, ReactionRoleMessage> Messages { get; set; } = new(); // MessageId -> ReactionRoleMessage
    }

    public class ReactionRoleMessage
    {
        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public Dictionary<string, ulong> EmojiRoleMap { get; set; } = new(); // Emoji -> RoleId
    }
}

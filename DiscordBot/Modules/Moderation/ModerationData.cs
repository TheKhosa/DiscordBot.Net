using System;
using System.Collections.Generic;

namespace DiscordBot.Modules.Moderation
{
    /// <summary>
    /// Data model for moderation actions and warnings
    /// </summary>
    public class ModerationData
    {
        public Dictionary<ulong, List<Warning>> Warnings { get; set; } = new();
        public Dictionary<ulong, List<Mute>> ActiveMutes { get; set; } = new();
        public List<ModerationAction> ActionHistory { get; set; } = new();
    }

    public class Warning
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ulong UserId { get; set; }
        public ulong ModeratorId { get; set; }
        public string Reason { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class Mute
    {
        public ulong UserId { get; set; }
        public ulong ModeratorId { get; set; }
        public string Reason { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public ulong? MutedRoleId { get; set; }
    }

    public class ModerationAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ModerationActionType Type { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; } = "";
        public ulong ModeratorId { get; set; }
        public string ModeratorName { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string? Duration { get; set; }
    }

    public enum ModerationActionType
    {
        Warn,
        Kick,
        Ban,
        Unban,
        Mute,
        Unmute,
        Purge
    }
}

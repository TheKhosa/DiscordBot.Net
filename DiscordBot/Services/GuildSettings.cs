using System;

namespace DiscordBot.Services
{
    /// <summary>
    /// Per-guild configuration settings
    /// </summary>
    public class GuildSettings
    {
        public ulong GuildId { get; set; }
        
        // Command settings
        public string CommandPrefix { get; set; } = "!";
        
        // Music settings
        public float DefaultVolume { get; set; } = 1.0f;
        public int MaxQueueSize { get; set; } = 100;
        public bool AutoLeaveWhenEmpty { get; set; } = true;
        public TimeSpan AutoLeaveTimeout { get; set; } = TimeSpan.FromMinutes(5);
        
        // DJ Role settings
        public ulong? DjRoleId { get; set; }
        public bool RequireDjForSkip { get; set; } = false;
        public bool RequireDjForVolume { get; set; } = false;
        public bool RequireDjForPlayback { get; set; } = false;
        
        // Moderation settings
        public ulong? ModLogChannelId { get; set; }
        public bool LogModActions { get; set; } = true;
        
        // Module settings
        public bool EnabledModules { get; set; } = true;
    }
}

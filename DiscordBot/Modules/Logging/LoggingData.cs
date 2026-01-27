namespace DiscordBot.Modules.Logging
{
    public class LoggingConfig
    {
        public ulong? MessageLogChannel { get; set; }
        public ulong? MemberLogChannel { get; set; }
        public ulong? ServerLogChannel { get; set; }
        public ulong? VoiceLogChannel { get; set; }
        
        public bool LogMessageEdits { get; set; } = true;
        public bool LogMessageDeletes { get; set; } = true;
        public bool LogMemberJoins { get; set; } = true;
        public bool LogMemberLeaves { get; set; } = true;
        public bool LogMemberUpdates { get; set; } = true;
        public bool LogRoleChanges { get; set; } = true;
        public bool LogChannelChanges { get; set; } = true;
        public bool LogVoiceActivity { get; set; } = true;
        public bool LogBans { get; set; } = true;
    }
}

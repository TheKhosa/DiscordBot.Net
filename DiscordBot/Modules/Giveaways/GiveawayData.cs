using System;
using System.Collections.Generic;

namespace DiscordBot.Modules.Giveaways
{
    public class GiveawayData
    {
        public Dictionary<string, Giveaway> ActiveGiveaways { get; set; } = new(); // GiveawayId -> Giveaway
        public List<Giveaway> CompletedGiveaways { get; set; } = new();
    }

    public class Giveaway
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong HostId { get; set; }
        public string HostName { get; set; } = "";
        public string Prize { get; set; } = "";
        public int Winners { get; set; } = 1;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<ulong> Entries { get; set; } = new();
        public List<GiveawayWinner> WinnersList { get; set; } = new();
        public bool IsCompleted { get; set; } = false;
        public string? Requirements { get; set; }
    }

    public class GiveawayWinner
    {
        public ulong UserId { get; set; }
        public string Username { get; set; } = "";
        public DateTime WonAt { get; set; }
    }
}

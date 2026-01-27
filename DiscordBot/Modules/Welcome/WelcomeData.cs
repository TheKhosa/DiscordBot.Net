namespace DiscordBot.Modules.Welcome
{
    public class WelcomeData
    {
        public bool WelcomeEnabled { get; set; } = false;
        public ulong? WelcomeChannelId { get; set; }
        public string WelcomeMessage { get; set; } = "Welcome {user} to {server}!";
        
        public bool GoodbyeEnabled { get; set; } = false;
        public ulong? GoodbyeChannelId { get; set; }
        public string GoodbyeMessage { get; set; } = "Goodbye {user}, thanks for being part of {server}!";
    }
}

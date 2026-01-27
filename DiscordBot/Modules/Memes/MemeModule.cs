using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Memes
{
    /// <summary>
    /// Module for memes and fun responses
    /// </summary>
    public class MemeModule : ModuleBase
    {
        public override string ModuleId => "memes";
        public override string Name => "Meme Generator";
        public override string Description => "Generate memes and fun responses";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private Random _random = new Random();

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            var content = message.Content.ToLower();

            if (content.StartsWith("!meme"))
            {
                await HandleMemeCommand(message);
                return true;
            }

            if (content.StartsWith("!joke"))
            {
                await HandleJokeCommand(message);
                return true;
            }

            if (content.StartsWith("!roast"))
            {
                await HandleRoastCommand(message);
                return true;
            }

            if (content.StartsWith("!compliment"))
            {
                await HandleComplimentCommand(message);
                return true;
            }

            if (content.StartsWith("!ascii"))
            {
                await HandleAsciiCommand(message);
                return true;
            }

            return false;
        }

        private async Task HandleMemeCommand(SocketUserMessage message)
        {
            var memes = new[]
            {
                "https://i.imgflip.com/30b1gx.jpg", // Mocking SpongeBob
                "https://i.imgflip.com/1bij.jpg",    // Success Kid
                "https://i.imgflip.com/1g8my4.jpg",  // Distracted Boyfriend
                "https://i.imgflip.com/4t0m5.jpg",   // Woman Yelling at Cat
                "https://i.imgflip.com/26am.jpg",    // One Does Not Simply
                "https://i.imgflip.com/261o.jpg",    // Drake
                "https://i.imgflip.com/2fm6x.jpg",   // Disaster Girl
                "https://i.imgflip.com/9vct.jpg"     // Philosoraptor
            };

            var meme = memes[_random.Next(memes.Length)];
            
            var embed = new EmbedBuilder()
                .WithTitle("😂 Random Meme")
                .WithImageUrl(meme)
                .WithColor(Color.Purple)
                .WithFooter("Use !joke for a random joke")
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleJokeCommand(SocketUserMessage message)
        {
            var jokes = new[]
            {
                "Why don't scientists trust atoms? Because they make up everything!",
                "Why did the scarecrow win an award? He was outstanding in his field!",
                "I told my wife she was drawing her eyebrows too high. She looked surprised.",
                "Why don't eggs tell jokes? They'd crack each other up!",
                "What do you call a bear with no teeth? A gummy bear!",
                "Why couldn't the bicycle stand up? It was two tired!",
                "What do you call a fake noodle? An impasta!",
                "How do you organize a space party? You planet!",
                "Why did the math book look so sad? Because it had too many problems!",
                "What's orange and sounds like a parrot? A carrot!"
            };

            var joke = jokes[_random.Next(jokes.Length)];
            await message.Channel.SendMessageAsync($"😄 **Joke:** {joke}");
        }

        private async Task HandleRoastCommand(SocketUserMessage message)
        {
            var roasts = new[]
            {
                "If I wanted to kill myself, I'd climb your ego and jump to your IQ.",
                "You're not stupid; you just have bad luck thinking.",
                "I'd agree with you, but then we'd both be wrong.",
                "You're the reason the gene pool needs a lifeguard.",
                "If ignorance is bliss, you must be the happiest person alive.",
                "I'm jealous of people who don't know you.",
                "You bring everyone so much joy... when you leave the room.",
                "I'd explain it to you, but I don't have any crayons.",
                "You're proof that evolution can go in reverse.",
                "I'm not insulting you, I'm describing you."
            };

            var roast = roasts[_random.Next(roasts.Length)];
            
            var target = message.MentionedUsers.Count > 0 
                ? message.MentionedUsers.First().Mention 
                : message.Author.Mention;

            await message.Channel.SendMessageAsync($"🔥 {target}: {roast}");
        }

        private async Task HandleComplimentCommand(SocketUserMessage message)
        {
            var compliments = new[]
            {
                "You're an awesome friend!",
                "You light up the room!",
                "You're a gift to those around you!",
                "You're a smart cookie!",
                "You are awesome!",
                "You have impeccable manners!",
                "You're strong!",
                "Your voice is magnificent!",
                "You're really something special!",
                "You're a great listener!"
            };

            var compliment = compliments[_random.Next(compliments.Length)];
            
            var target = message.MentionedUsers.Count > 0 
                ? message.MentionedUsers.First().Mention 
                : message.Author.Mention;

            await message.Channel.SendMessageAsync($"💝 {target}: {compliment}");
        }

        private async Task HandleAsciiCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!ascii <text>`");
                return;
            }

            var text = string.Join(" ", parts.Skip(1)).ToUpper();
            
            if (text.Length > 10)
            {
                await message.Channel.SendMessageAsync("❌ Text too long! Maximum 10 characters.");
                return;
            }

            // Simple ASCII art generator (just makes it big and bold)
            var ascii = $"```\n{text}\n```";
            
            await message.Channel.SendMessageAsync(ascii);
        }
    }
}

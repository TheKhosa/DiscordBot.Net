using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Polls
{
    /// <summary>
    /// Module for creating and managing polls
    /// </summary>
    public class PollsModule : ModuleBase
    {
        public override string ModuleId => "polls";
        public override string Name => "Polls & Voting";
        public override string Description => "Create polls with reactions and track votes";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly ConcurrentDictionary<ulong, Poll> _activePolls = new();
        private readonly string[] _emojiNumbers = { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            client.ReactionAdded += OnReactionAdded;
            return base.InitializeAsync(client, services);
        }

        public override Task ShutdownAsync()
        {
            if (Client != null)
            {
                Client.ReactionAdded -= OnReactionAdded;
            }
            return base.ShutdownAsync();
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, Cacheable<IMessageChannel, ulong> cachedChannel, SocketReaction reaction)
        {
            if (reaction.User.Value?.IsBot == true) return;
            
            // Check if this is a poll message
            if (_activePolls.TryGetValue(cachedMessage.Id, out var poll))
            {
                var emoji = reaction.Emote.Name;
                var optionIndex = Array.IndexOf(_emojiNumbers, emoji);
                
                if (optionIndex >= 0 && optionIndex < poll.Options.Count)
                {
                    poll.Votes[optionIndex]++;
                }
            }
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            var content = message.Content.ToLower();

            if (content.StartsWith("!poll"))
            {
                await HandlePollCommand(message);
                return true;
            }

            if (content.StartsWith("!quickpoll") || content.StartsWith("!qpoll"))
            {
                await HandleQuickPollCommand(message);
                return true;
            }

            if (content.StartsWith("!endpoll"))
            {
                await HandleEndPollCommand(message);
                return true;
            }

            return false;
        }

        private async Task HandlePollCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split('|', StringSplitOptions.TrimEntries);
            
            if (parts.Length < 3)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!poll Question | Option 1 | Option 2 | Option 3 ...`\n" +
                    "Example: `!poll What's your favorite color? | Red | Blue | Green`");
                return;
            }

            var question = parts[0].Substring(5).Trim(); // Remove "!poll"
            var options = parts.Skip(1).Take(10).ToList(); // Max 10 options

            if (options.Count < 2)
            {
                await message.Channel.SendMessageAsync("❌ You need at least 2 options!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Poll: {question}")
                .WithDescription("React with the corresponding number to vote!")
                .WithColor(Color.Blue)
                .WithFooter($"Poll by {message.Author.Username}")
                .WithCurrentTimestamp();

            for (int i = 0; i < options.Count; i++)
            {
                embed.AddField($"{_emojiNumbers[i]} Option {i + 1}", options[i], inline: false);
            }

            var pollMessage = await message.Channel.SendMessageAsync(embed: embed.Build());

            // Add reactions
            for (int i = 0; i < options.Count; i++)
            {
                await pollMessage.AddReactionAsync(new Emoji(_emojiNumbers[i]));
            }

            // Store poll
            _activePolls[pollMessage.Id] = new Poll
            {
                MessageId = pollMessage.Id,
                Question = question,
                Options = options,
                CreatorId = message.Author.Id,
                CreatedAt = DateTime.UtcNow,
                Votes = new int[options.Count]
            };

            // Delete command message
            await message.DeleteAsync();
        }

        private async Task HandleQuickPollCommand(SocketUserMessage message)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.TrimEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync(
                    "❌ Usage: `!quickpoll <question>`\n" +
                    "Creates a simple Yes/No poll");
                return;
            }

            var question = parts[1];

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Quick Poll: {question}")
                .WithDescription("React with ✅ for Yes or ❌ for No")
                .WithColor(Color.Green)
                .WithFooter($"Poll by {message.Author.Username}")
                .WithCurrentTimestamp()
                .Build();

            var pollMessage = await message.Channel.SendMessageAsync(embed: embed);

            await pollMessage.AddReactionAsync(new Emoji("✅"));
            await pollMessage.AddReactionAsync(new Emoji("❌"));

            await message.DeleteAsync();
        }

        private async Task HandleEndPollCommand(SocketUserMessage message)
        {
            if (message.Channel is not SocketTextChannel textChannel) return;

            var parts = message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!endpoll <message_id>`");
                return;
            }

            if (!ulong.TryParse(parts[1], out ulong messageId))
            {
                await message.Channel.SendMessageAsync("❌ Invalid message ID!");
                return;
            }

            if (!_activePolls.TryRemove(messageId, out var poll))
            {
                await message.Channel.SendMessageAsync("❌ Poll not found!");
                return;
            }

            // Fetch the message to get actual vote counts
            var pollMessage = await textChannel.GetMessageAsync(messageId) as IUserMessage;
            if (pollMessage == null)
            {
                await message.Channel.SendMessageAsync("❌ Poll message not found!");
                return;
            }

            // Count votes from reactions
            var results = new int[poll.Options.Count];
            var reactions = pollMessage.Reactions;

            for (int i = 0; i < poll.Options.Count; i++)
            {
                var emoji = _emojiNumbers[i];
                if (reactions.TryGetValue(new Emoji(emoji), out var reactionMetadata))
                {
                    results[i] = reactionMetadata.ReactionCount - 1; // Subtract bot's reaction
                }
            }

            var totalVotes = results.Sum();
            var maxVotes = results.Max();
            var winnerIndex = Array.IndexOf(results, maxVotes);

            var embed = new EmbedBuilder()
                .WithTitle($"📊 Poll Results: {poll.Question}")
                .WithColor(Color.Gold)
                .WithDescription($"Total Votes: **{totalVotes}**")
                .WithFooter("Poll ended")
                .WithCurrentTimestamp();

            for (int i = 0; i < poll.Options.Count; i++)
            {
                var percentage = totalVotes > 0 ? (results[i] * 100.0 / totalVotes) : 0;
                var progressBar = CreateProgressBar(percentage / 100.0, 10);
                var isWinner = i == winnerIndex && results[i] > 0;

                embed.AddField(
                    $"{(isWinner ? "🏆 " : "")}{_emojiNumbers[i]} {poll.Options[i]}",
                    $"{progressBar} {results[i]} votes ({percentage:F1}%)",
                    inline: false
                );
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private string CreateProgressBar(double progress, int length)
        {
            var filled = (int)(progress * length);
            var empty = length - filled;
            return $"[{new string('█', filled)}{new string('░', empty)}]";
        }

        private class Poll
        {
            public ulong MessageId { get; set; }
            public string Question { get; set; } = "";
            public List<string> Options { get; set; } = new();
            public ulong CreatorId { get; set; }
            public DateTime CreatedAt { get; set; }
            public int[] Votes { get; set; } = Array.Empty<int>();
        }
    }
}

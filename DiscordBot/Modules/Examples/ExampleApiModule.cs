using Discord.WebSocket;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Examples
{
    /// <summary>
    /// Example module demonstrating 3rd party API integration
    /// This serves as a template for creating custom modules
    /// </summary>
    public class ExampleApiModule : ModuleBase
    {
        public override string ModuleId => "example_api";
        public override string Name => "Example API Module";
        public override string Description => "Demonstrates how to integrate 3rd party APIs";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private HttpClient? _httpClient;

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DiscordBot/1.0");
            
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            // Example: Respond to !example command
            if (message.Content.StartsWith("!example"))
            {
                var channel = message.Channel;
                await channel.SendMessageAsync("This is an example module! You can create modules like this to integrate with any API.");
                return true; // Message was handled
            }

            return false; // Message not handled by this module
        }

        public override Task ShutdownAsync()
        {
            _httpClient?.Dispose();
            return base.ShutdownAsync();
        }

        // Example: Method to call a 3rd party API
        private async Task<string?> FetchDataFromApiAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient!.GetStringAsync(endpoint);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] API error: {ex.Message}");
                return null;
            }
        }
    }
}

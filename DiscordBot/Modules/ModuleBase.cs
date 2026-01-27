using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    /// <summary>
    /// Base class for modules with common functionality
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        public abstract string ModuleId { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Version { get; }
        public abstract string Author { get; }
        public bool IsEnabled { get; set; } = true;

        protected DiscordSocketClient? Client { get; private set; }
        protected IServiceProvider? Services { get; private set; }

        public virtual Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            Client = client;
            Services = services;
            Console.WriteLine($"[Module] Initialized: {Name} v{Version}");
            return Task.CompletedTask;
        }

        public virtual Task ShutdownAsync()
        {
            Console.WriteLine($"[Module] Shutdown: {Name}");
            return Task.CompletedTask;
        }

        public virtual Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            // Base implementation: module doesn't handle this message
            return Task.FromResult(false);
        }
    }
}

using Discord.WebSocket;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    /// <summary>
    /// Base interface for all bot modules.
    /// Modules can extend bot functionality with custom commands, services, and event handlers.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Unique identifier for the module
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// Display name of the module
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Module description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Module version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Module author
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Whether the module is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Initialize the module
        /// </summary>
        Task InitializeAsync(DiscordSocketClient client, IServiceProvider services);

        /// <summary>
        /// Shutdown the module and cleanup resources
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Handle incoming messages (for custom commands)
        /// </summary>
        Task<bool> HandleMessageAsync(SocketUserMessage message);
    }
}

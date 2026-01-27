using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    /// <summary>
    /// Manages loading, initializing, and executing modules
    /// </summary>
    public class ModuleManager
    {
        private readonly List<IModule> _modules = new();
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

        public ModuleManager(DiscordSocketClient client, IServiceProvider services)
        {
            _client = client;
            _services = services;
        }

        /// <summary>
        /// Register a module instance
        /// </summary>
        public async Task RegisterModuleAsync(IModule module)
        {
            if (_modules.Any(m => m.ModuleId == module.ModuleId))
            {
                throw new InvalidOperationException($"Module '{module.ModuleId}' is already registered");
            }

            _modules.Add(module);
            await module.InitializeAsync(_client, _services);
            Console.WriteLine($"[ModuleManager] Registered module: {module.Name} ({module.ModuleId})");
        }

        /// <summary>
        /// Unregister a module
        /// </summary>
        public async Task UnregisterModuleAsync(string moduleId)
        {
            var module = _modules.FirstOrDefault(m => m.ModuleId == moduleId);
            if (module == null)
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            await module.ShutdownAsync();
            _modules.Remove(module);
            Console.WriteLine($"[ModuleManager] Unregistered module: {module.Name} ({moduleId})");
        }

        /// <summary>
        /// Auto-discover and register modules from the current assembly
        /// </summary>
        public async Task DiscoverAndRegisterModulesAsync()
        {
            Console.WriteLine("[ModuleManager] Starting module discovery...");
            var moduleTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IModule).IsAssignableFrom(t));

            Console.WriteLine($"[ModuleManager] Found {moduleTypes.Count()} module types");

            foreach (var type in moduleTypes)
            {
                try
                {
                    Console.WriteLine($"[ModuleManager] Loading module: {type.Name} from {type.Namespace}");
                    var module = Activator.CreateInstance(type) as IModule;
                    if (module != null)
                    {
                        await RegisterModuleAsync(module);
                    }
                    else
                    {
                        Console.WriteLine($"[ModuleManager] Failed to create instance of {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ModuleManager] Failed to load module {type.Name}: {ex.Message}");
                    Console.WriteLine($"[ModuleManager] Stack trace: {ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Handle incoming message through all modules
        /// </summary>
        public async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            foreach (var module in _modules.Where(m => m.IsEnabled))
            {
                try
                {
                    if (await module.HandleMessageAsync(message))
                    {
                        // Module handled the message, stop processing
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ModuleManager] Error in module {module.Name}: {ex.Message}");
                }
            }

            return false; // No module handled the message
        }

        /// <summary>
        /// Shutdown all modules
        /// </summary>
        public async Task ShutdownAllAsync()
        {
            foreach (var module in _modules)
            {
                try
                {
                    await module.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ModuleManager] Error shutting down module {module.Name}: {ex.Message}");
                }
            }

            _modules.Clear();
        }

        /// <summary>
        /// Get module by ID
        /// </summary>
        public IModule? GetModule(string moduleId)
        {
            return _modules.FirstOrDefault(m => m.ModuleId == moduleId);
        }

        /// <summary>
        /// Enable/disable a module
        /// </summary>
        public void SetModuleEnabled(string moduleId, bool enabled)
        {
            var module = GetModule(moduleId);
            if (module != null)
            {
                module.IsEnabled = enabled;
                Console.WriteLine($"[ModuleManager] Module {module.Name} {(enabled ? "enabled" : "disabled")}");
            }
        }
    }
}

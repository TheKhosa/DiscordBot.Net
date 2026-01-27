using System;

namespace DiscordBot.Services
{
    /// <summary>
    /// Simple service provider for dependency injection in modules
    /// </summary>
    public class ServiceProvider : IServiceProvider
    {
        private readonly AudioService _audioService;
        private readonly GuildSettingsService _guildSettings;

        public ServiceProvider(AudioService audioService, GuildSettingsService guildSettings)
        {
            _audioService = audioService;
            _guildSettings = guildSettings;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(AudioService))
                return _audioService;
            
            if (serviceType == typeof(GuildSettingsService))
                return _guildSettings;

            return null;
        }

        public T? GetService<T>() where T : class
        {
            return GetService(typeof(T)) as T;
        }
    }
}

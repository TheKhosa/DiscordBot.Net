using Discord;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.Modules;
using DiscordBot.Services;
using System.Text.Json;

class Program
{
    private DiscordSocketClient? _client;
    private AudioService? _audioService;
    private GuildSettingsService? _guildSettings;
    private ModuleManager? _moduleManager;
    private CommandHandler? _commandHandler;

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        Console.WriteLine($"Bot starting... ({(Environment.Is64BitProcess ? "64-bit" : "32-bit")})");

        // Load bot token from config file
        string token;
        try
        {
            // Look for config.json in the application's directory
            var appDir = AppContext.BaseDirectory;
            var configPath = Path.Combine(appDir, "config.json");
            
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Error: config.json not found at: {configPath}");
                Console.WriteLine("Please create a config.json file with your bot token.");
                Console.WriteLine("See config.example.json for the format.");
                return;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var configData = JsonDocument.Parse(configJson);
            token = configData.RootElement.GetProperty("BotToken").GetString() 
                ?? throw new Exception("BotToken not found in config.json");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error: config.json not found!");
            Console.WriteLine("Please create a config.json file with your bot token.");
            Console.WriteLine("See config.example.json for the format.");
            return;
        }

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates,
            LogLevel = LogSeverity.Info
        };

        _client = new DiscordSocketClient(config);
        _audioService = new AudioService();
        _guildSettings = new GuildSettingsService();
        
        // Load guild settings
        await _guildSettings.LoadAllSettingsAsync();

        // Initialize module system
        var services = new ServiceProvider(_audioService, _guildSettings);
        _moduleManager = new ModuleManager(_client, services);
        
        // Auto-discover and register modules
        await _moduleManager.DiscoverAndRegisterModulesAsync();

        _commandHandler = new CommandHandler(_client, _audioService, _guildSettings, _moduleManager);

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += _commandHandler.HandleMessageAsync;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Keep bot running
        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage msg)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{msg.Severity}] {msg.Source}: {msg.Message}");
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"✓ Bot ready! Logged in as {_client!.CurrentUser.Username}#{_client.CurrentUser.Discriminator}");
        Console.WriteLine($"  Connected to {_client.Guilds.Count} guild(s)");
        return Task.CompletedTask;
    }
}

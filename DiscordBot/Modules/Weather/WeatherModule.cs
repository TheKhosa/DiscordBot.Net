using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Weather
{
    /// <summary>
    /// Weather module using Open-Meteo API
    /// </summary>
    public class WeatherModule : ModuleBase
    {
        public override string ModuleId => "weather";
        public override string Name => "Weather";
        public override string Description => "Real-time weather forecasts and conditions";
        public override string Version => "1.0.0";
        public override string Author => "DiscordBot";

        private readonly string _dataFolder = "WeatherData";
        private Dictionary<ulong, WeatherData> _guildData = new();
        private readonly HttpClient _httpClient = new();
        private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
        private const string WeatherUrl = "https://api.open-meteo.com/v1/forecast";

        public override Task InitializeAsync(DiscordSocketClient client, IServiceProvider services)
        {
            LoadData();
            return base.InitializeAsync(client, services);
        }

        public override async Task<bool> HandleMessageAsync(SocketUserMessage message)
        {
            if (message.Channel is not SocketGuildChannel guildChannel) return false;

            var content = message.Content.ToLower();
            var guildId = guildChannel.Guild.Id;

            if (content.StartsWith("!weather") || content.StartsWith("!w"))
            {
                await HandleWeatherCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!forecast") || content.StartsWith("!fc"))
            {
                await HandleForecastCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!setlocation") || content.StartsWith("!setloc"))
            {
                await HandleSetLocationCommand(message, guildId);
                return true;
            }

            if (content.StartsWith("!hourly"))
            {
                await HandleHourlyCommand(message, guildId);
                return true;
            }

            return false;
        }

        private async Task HandleWeatherCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            string location;
            if (parts.Length < 2)
            {
                // Use saved location
                var data = GetGuildData(guildId);
                if (!data.SavedLocations.ContainsKey(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync(
                        "❌ No location provided! Use:\n" +
                        "`!weather <location>` - Check weather for a location\n" +
                        "`!setlocation <location>` - Save your default location");
                    return;
                }
                location = data.SavedLocations[message.Author.Id];
            }
            else
            {
                location = parts[1];
            }

            await message.Channel.TriggerTypingAsync();

            // Geocode location
            var geoResult = await GeocodeLocation(location);
            if (geoResult == null)
            {
                await message.Channel.SendMessageAsync($"❌ Location '{location}' not found! Try including country (e.g., 'London, UK')");
                return;
            }

            // Get weather data
            var weather = await GetCurrentWeather(geoResult.Latitude, geoResult.Longitude);
            if (weather?.Current == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to fetch weather data!");
                return;
            }

            // Build embed
            var embed = new EmbedBuilder()
                .WithTitle($"{WeatherCodes.GetEmoji(weather.Current.WeatherCode)} Weather in {geoResult.Name}")
                .WithColor(GetWeatherColor(weather.Current.Temperature))
                .WithDescription($"**{WeatherCodes.GetDescription(weather.Current.WeatherCode)}**")
                .WithFooter($"Powered by Open-Meteo • {DateTime.Now:yyyy-MM-dd HH:mm}")
                .WithThumbnailUrl(GetWeatherImageUrl(weather.Current.WeatherCode));

            // Temperature
            embed.AddField("🌡️ Temperature", 
                $"**{weather.Current.Temperature:F1}°C** ({CelsiusToFahrenheit(weather.Current.Temperature):F1}°F)\n" +
                $"Feels like: {weather.Current.ApparentTemperature:F1}°C ({CelsiusToFahrenheit(weather.Current.ApparentTemperature):F1}°F)", 
                inline: true);

            // Wind
            var windDir = WindDirection.GetDirection(weather.Current.WindDirection);
            var windArrow = WindDirection.GetArrow(weather.Current.WindDirection);
            embed.AddField("💨 Wind", 
                $"{weather.Current.WindSpeed:F1} km/h {windArrow}\n" +
                $"Direction: {windDir} ({weather.Current.WindDirection:F0}°)", 
                inline: true);

            // Additional info
            embed.AddField("💧 Humidity", $"{weather.Current.RelativeHumidity}%", inline: true);
            embed.AddField("☁️ Cloud Cover", $"{weather.Current.CloudCover:F0}%", inline: true);
            embed.AddField("🌧️ Precipitation", $"{weather.Current.Precipitation:F1} mm", inline: true);
            embed.AddField("📊 Pressure", $"{weather.Current.Pressure:F0} hPa", inline: true);

            // Location details
            var locationStr = geoResult.Name;
            if (!string.IsNullOrEmpty(geoResult.Admin1))
                locationStr += $", {geoResult.Admin1}";
            if (!string.IsNullOrEmpty(geoResult.Country))
                locationStr += $", {geoResult.Country}";

            embed.AddField("📍 Location", locationStr, inline: false);

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleForecastCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            string location;
            if (parts.Length < 2)
            {
                var data = GetGuildData(guildId);
                if (!data.SavedLocations.ContainsKey(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync("❌ No location provided! Use `!forecast <location>` or save a location with `!setlocation`");
                    return;
                }
                location = data.SavedLocations[message.Author.Id];
            }
            else
            {
                location = parts[1];
            }

            await message.Channel.TriggerTypingAsync();

            var geoResult = await GeocodeLocation(location);
            if (geoResult == null)
            {
                await message.Channel.SendMessageAsync($"❌ Location '{location}' not found!");
                return;
            }

            var weather = await GetDailyForecast(geoResult.Latitude, geoResult.Longitude);
            if (weather?.Daily == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to fetch forecast data!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"📅 7-Day Forecast for {geoResult.Name}")
                .WithColor(Color.Blue)
                .WithFooter($"Powered by Open-Meteo • {DateTime.Now:yyyy-MM-dd HH:mm}");

            // Add daily forecasts (show 7 days)
            for (int i = 0; i < Math.Min(7, weather.Daily.Time.Count); i++)
            {
                var date = DateTime.Parse(weather.Daily.Time[i]);
                var dayName = i == 0 ? "Today" : i == 1 ? "Tomorrow" : date.ToString("dddd");
                
                var emoji = WeatherCodes.GetEmoji(weather.Daily.WeatherCode[i]);
                var condition = WeatherCodes.GetDescription(weather.Daily.WeatherCode[i]);
                var tempMax = weather.Daily.TemperatureMax[i];
                var tempMin = weather.Daily.TemperatureMin[i];
                var precip = weather.Daily.PrecipitationSum[i];
                var precipProb = weather.Daily.PrecipitationProbability[i];
                var windMax = weather.Daily.WindSpeedMax[i];

                var sunrise = DateTime.Parse(weather.Daily.Sunrise[i]).ToString("HH:mm");
                var sunset = DateTime.Parse(weather.Daily.Sunset[i]).ToString("HH:mm");

                var fieldValue = $"{emoji} **{condition}**\n" +
                    $"🌡️ {tempMax:F0}°C / {tempMin:F0}°C ({CelsiusToFahrenheit(tempMax):F0}°F / {CelsiusToFahrenheit(tempMin):F0}°F)\n" +
                    $"💧 Rain: {precipProb:F0}% ({precip:F1}mm) | 💨 Wind: {windMax:F0} km/h\n" +
                    $"🌅 {sunrise} → 🌇 {sunset}";

                embed.AddField($"{dayName} - {date:MMM dd}", fieldValue, inline: false);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleHourlyCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            string location;
            if (parts.Length < 2)
            {
                var data = GetGuildData(guildId);
                if (!data.SavedLocations.ContainsKey(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync("❌ No location provided! Use `!hourly <location>` or save a location with `!setlocation`");
                    return;
                }
                location = data.SavedLocations[message.Author.Id];
            }
            else
            {
                location = parts[1];
            }

            await message.Channel.TriggerTypingAsync();

            var geoResult = await GeocodeLocation(location);
            if (geoResult == null)
            {
                await message.Channel.SendMessageAsync($"❌ Location '{location}' not found!");
                return;
            }

            var weather = await GetHourlyForecast(geoResult.Latitude, geoResult.Longitude);
            if (weather?.Hourly == null)
            {
                await message.Channel.SendMessageAsync("❌ Failed to fetch hourly data!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"⏰ 24-Hour Forecast for {geoResult.Name}")
                .WithColor(Color.Teal)
                .WithFooter($"Powered by Open-Meteo • {DateTime.Now:yyyy-MM-dd HH:mm}");

            var now = DateTime.UtcNow;
            var hourlyData = new List<string>();

            // Show next 24 hours (every 3 hours)
            for (int i = 0; i < Math.Min(24, weather.Hourly.Time.Count); i += 3)
            {
                var time = DateTime.Parse(weather.Hourly.Time[i]);
                if (time < now) continue;

                var emoji = WeatherCodes.GetEmoji(weather.Hourly.WeatherCode[i]);
                var temp = weather.Hourly.Temperature[i];
                var precip = weather.Hourly.Precipitation[i];

                hourlyData.Add($"{emoji} **{time:HH:mm}** - {temp:F1}°C ({CelsiusToFahrenheit(temp):F0}°F) | 🌧️ {precip:F1}mm");

                if (hourlyData.Count >= 8) break; // Show 8 time slots
            }

            embed.WithDescription(string.Join("\n", hourlyData));

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task HandleSetLocationCommand(SocketUserMessage message, ulong guildId)
        {
            var parts = message.Content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("❌ Usage: `!setlocation <location>`\nExample: `!setlocation London, UK`");
                return;
            }

            var location = parts[1];

            // Verify location exists
            var geoResult = await GeocodeLocation(location);
            if (geoResult == null)
            {
                await message.Channel.SendMessageAsync($"❌ Location '{location}' not found! Try including country (e.g., 'Paris, France')");
                return;
            }

            var data = GetGuildData(guildId);
            data.SavedLocations[message.Author.Id] = location;
            SaveData();

            var locationStr = geoResult.Name;
            if (!string.IsNullOrEmpty(geoResult.Admin1))
                locationStr += $", {geoResult.Admin1}";
            if (!string.IsNullOrEmpty(geoResult.Country))
                locationStr += $", {geoResult.Country}";

            await message.Channel.SendMessageAsync($"✅ Default location set to: **{locationStr}**\n\nYou can now use `!weather` without specifying a location!");
        }

        private async Task<GeocodingResult?> GeocodeLocation(string location)
        {
            try
            {
                var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var results = json["results"];
                if (results == null || !results.Any()) return null;

                var first = results.First();
                return new GeocodingResult
                {
                    Name = first["name"]?.ToString() ?? "",
                    Latitude = first["latitude"]?.Value<double>() ?? 0,
                    Longitude = first["longitude"]?.Value<double>() ?? 0,
                    Country = first["country"]?.ToString(),
                    Admin1 = first["admin1"]?.ToString(),
                    Population = first["population"]?.Value<int>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] Geocoding error: {ex.Message}");
                return null;
            }
        }

        private async Task<WeatherResponse?> GetCurrentWeather(double lat, double lon)
        {
            try
            {
                var url = $"{WeatherUrl}?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,cloud_cover,pressure_msl,wind_speed_10m,wind_direction_10m&timezone=auto";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var current = json["current"];
                if (current == null) return null;

                return new WeatherResponse
                {
                    Latitude = lat,
                    Longitude = lon,
                    Current = new CurrentWeather
                    {
                        Temperature = current["temperature_2m"]?.Value<double>() ?? 0,
                        ApparentTemperature = current["apparent_temperature"]?.Value<double>() ?? 0,
                        RelativeHumidity = current["relative_humidity_2m"]?.Value<int>() ?? 0,
                        Precipitation = current["precipitation"]?.Value<double>() ?? 0,
                        WeatherCode = current["weather_code"]?.Value<int>() ?? 0,
                        CloudCover = current["cloud_cover"]?.Value<double>() ?? 0,
                        Pressure = current["pressure_msl"]?.Value<double>() ?? 0,
                        WindSpeed = current["wind_speed_10m"]?.Value<double>() ?? 0,
                        WindDirection = current["wind_direction_10m"]?.Value<double>() ?? 0,
                        Time = current["time"]?.ToString() ?? ""
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] API error: {ex.Message}");
                return null;
            }
        }

        private async Task<WeatherResponse?> GetDailyForecast(double lat, double lon)
        {
            try
            {
                var url = $"{WeatherUrl}?latitude={lat}&longitude={lon}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,wind_speed_10m_max,sunrise,sunset&timezone=auto&forecast_days=7";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var daily = json["daily"];
                if (daily == null) return null;

                return new WeatherResponse
                {
                    Latitude = lat,
                    Longitude = lon,
                    Daily = new DailyWeather
                    {
                        Time = daily["time"]?.ToObject<List<string>>() ?? new(),
                        WeatherCode = daily["weather_code"]?.ToObject<List<int>>() ?? new(),
                        TemperatureMax = daily["temperature_2m_max"]?.ToObject<List<double>>() ?? new(),
                        TemperatureMin = daily["temperature_2m_min"]?.ToObject<List<double>>() ?? new(),
                        PrecipitationSum = daily["precipitation_sum"]?.ToObject<List<double>>() ?? new(),
                        PrecipitationProbability = daily["precipitation_probability_max"]?.ToObject<List<double>>() ?? new(),
                        WindSpeedMax = daily["wind_speed_10m_max"]?.ToObject<List<double>>() ?? new(),
                        Sunrise = daily["sunrise"]?.ToObject<List<string>>() ?? new(),
                        Sunset = daily["sunset"]?.ToObject<List<string>>() ?? new()
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] API error: {ex.Message}");
                return null;
            }
        }

        private async Task<WeatherResponse?> GetHourlyForecast(double lat, double lon)
        {
            try
            {
                var url = $"{WeatherUrl}?latitude={lat}&longitude={lon}&hourly=temperature_2m,weather_code,precipitation&timezone=auto&forecast_days=2";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var hourly = json["hourly"];
                if (hourly == null) return null;

                return new WeatherResponse
                {
                    Latitude = lat,
                    Longitude = lon,
                    Hourly = new HourlyWeather
                    {
                        Time = hourly["time"]?.ToObject<List<string>>() ?? new(),
                        Temperature = hourly["temperature_2m"]?.ToObject<List<double>>() ?? new(),
                        WeatherCode = hourly["weather_code"]?.ToObject<List<int>>() ?? new(),
                        Precipitation = hourly["precipitation"]?.ToObject<List<double>>() ?? new()
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Weather] API error: {ex.Message}");
                return null;
            }
        }

        private double CelsiusToFahrenheit(double celsius)
        {
            return (celsius * 9 / 5) + 32;
        }

        private Color GetWeatherColor(double temp)
        {
            if (temp >= 30) return new Color(255, 69, 0); // Hot (red-orange)
            if (temp >= 20) return new Color(255, 165, 0); // Warm (orange)
            if (temp >= 10) return new Color(255, 215, 0); // Mild (gold)
            if (temp >= 0) return new Color(135, 206, 250); // Cool (light blue)
            return new Color(0, 191, 255); // Cold (deep blue)
        }

        private string GetWeatherImageUrl(int code)
        {
            // Return placeholder weather icon URLs (you can customize these)
            return code switch
            {
                0 => "https://cdn-icons-png.flaticon.com/512/869/869869.png", // Clear
                1 or 2 or 3 => "https://cdn-icons-png.flaticon.com/512/414/414825.png", // Cloudy
                45 or 48 => "https://cdn-icons-png.flaticon.com/512/1146/1146869.png", // Fog
                _ when code >= 61 && code <= 67 => "https://cdn-icons-png.flaticon.com/512/3351/3351979.png", // Rain
                _ when code >= 71 && code <= 77 => "https://cdn-icons-png.flaticon.com/512/642/642102.png", // Snow
                _ when code >= 95 => "https://cdn-icons-png.flaticon.com/512/1146/1146858.png", // Thunder
                _ => "https://cdn-icons-png.flaticon.com/512/1163/1163661.png" // Default
            };
        }

        private WeatherData GetGuildData(ulong guildId)
        {
            if (!_guildData.ContainsKey(guildId))
                _guildData[guildId] = new WeatherData();

            return _guildData[guildId];
        }

        private void LoadData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var file in Directory.GetFiles(_dataFolder, "*.json"))
            {
                var guildIdStr = Path.GetFileNameWithoutExtension(file);
                if (ulong.TryParse(guildIdStr, out ulong guildId))
                {
                    var json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<WeatherData>(json);
                    if (data != null)
                        _guildData[guildId] = data;
                }
            }

            Console.WriteLine($"[Weather] Loaded data for {_guildData.Count} guild(s)");
        }

        private void SaveData()
        {
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);

            foreach (var kvp in _guildData)
            {
                var filePath = Path.Combine(_dataFolder, $"{kvp.Key}.json");
                var json = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }
    }
}

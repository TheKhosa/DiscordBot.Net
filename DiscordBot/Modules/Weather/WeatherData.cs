using System;
using System.Collections.Generic;

namespace DiscordBot.Modules.Weather
{
    /// <summary>
    /// Weather data models for Open-Meteo API
    /// </summary>
    public class WeatherData
    {
        public Dictionary<ulong, string> SavedLocations { get; set; } = new(); // UserId -> Location
    }

    public class GeocodingResult
    {
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Country { get; set; }
        public string? Admin1 { get; set; } // State/Province
        public int? Population { get; set; }
    }

    public class WeatherResponse
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public CurrentWeather? Current { get; set; }
        public DailyWeather? Daily { get; set; }
        public HourlyWeather? Hourly { get; set; }
    }

    public class CurrentWeather
    {
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public int WeatherCode { get; set; }
        public double ApparentTemperature { get; set; }
        public int RelativeHumidity { get; set; }
        public double Precipitation { get; set; }
        public double CloudCover { get; set; }
        public double Pressure { get; set; }
        public string Time { get; set; } = "";
    }

    public class DailyWeather
    {
        public List<string> Time { get; set; } = new();
        public List<int> WeatherCode { get; set; } = new();
        public List<double> TemperatureMax { get; set; } = new();
        public List<double> TemperatureMin { get; set; } = new();
        public List<double> PrecipitationSum { get; set; } = new();
        public List<double> WindSpeedMax { get; set; } = new();
        public List<double> PrecipitationProbability { get; set; } = new();
        public List<string> Sunrise { get; set; } = new();
        public List<string> Sunset { get; set; } = new();
    }

    public class HourlyWeather
    {
        public List<string> Time { get; set; } = new();
        public List<double> Temperature { get; set; } = new();
        public List<int> WeatherCode { get; set; } = new();
        public List<double> Precipitation { get; set; } = new();
    }

    /// <summary>
    /// Weather condition codes from Open-Meteo
    /// </summary>
    public static class WeatherCodes
    {
        public static string GetDescription(int code)
        {
            return code switch
            {
                0 => "Clear sky",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                45 => "Foggy",
                48 => "Rime fog",
                51 => "Light drizzle",
                53 => "Moderate drizzle",
                55 => "Dense drizzle",
                56 => "Freezing drizzle (light)",
                57 => "Freezing drizzle (dense)",
                61 => "Slight rain",
                63 => "Moderate rain",
                65 => "Heavy rain",
                66 => "Freezing rain (light)",
                67 => "Freezing rain (heavy)",
                71 => "Slight snow",
                73 => "Moderate snow",
                75 => "Heavy snow",
                77 => "Snow grains",
                80 => "Slight rain showers",
                81 => "Moderate rain showers",
                82 => "Violent rain showers",
                85 => "Slight snow showers",
                86 => "Heavy snow showers",
                95 => "Thunderstorm",
                96 => "Thunderstorm with slight hail",
                99 => "Thunderstorm with heavy hail",
                _ => "Unknown"
            };
        }

        public static string GetEmoji(int code)
        {
            return code switch
            {
                0 => "☀️",
                1 => "🌤️",
                2 => "⛅",
                3 => "☁️",
                45 or 48 => "🌫️",
                51 or 53 or 55 or 56 or 57 => "🌧️",
                61 or 63 or 65 or 66 or 67 => "🌧️",
                71 or 73 or 75 or 77 => "❄️",
                80 or 81 or 82 => "🌦️",
                85 or 86 => "🌨️",
                95 or 96 or 99 => "⛈️",
                _ => "🌡️"
            };
        }
    }

    public static class WindDirection
    {
        public static string GetDirection(double degrees)
        {
            var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            var index = (int)Math.Round(degrees / 22.5) % 16;
            return directions[index];
        }

        public static string GetArrow(double degrees)
        {
            var arrows = new[] { "↓", "↙", "←", "↖", "↑", "↗", "→", "↘" };
            var index = (int)Math.Round(degrees / 45) % 8;
            return arrows[index];
        }
    }
}

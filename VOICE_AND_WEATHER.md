# Voice & Weather Modules

## 🎙️ Voice Module (ElevenLabs TTS)

AI-powered text-to-speech using ElevenLabs with natural-sounding voices.

### Commands

- `!say <text>` or `!tts <text>` - Speak text with default voice
- `!say <voice> <text>` - Speak with specific voice
- `!voices` - List all available voices
- `!previewvoice <voice>` or `!pv <voice>` - Preview voice details
- `!joinvoice` or `!joinvc` - Join your voice channel
- `!leavevoice` or `!leavevc` - Leave voice channel

### Features

✅ **High-Quality AI Voices**
- 100+ natural-sounding voices
- Multiple languages and accents
- Emotional range and tone control

✅ **Smart Caching**
- Caches generated audio
- Saves API calls
- Faster playback for repeated phrases

✅ **Easy to Use**
- Works with existing music bot
- Queue-based playback
- Automatic voice channel joining

### Setup

**Requirements:**
1. Bot must have permission to connect to voice channels
2. ElevenLabs API key configured (already set)
3. User must be in a voice channel

### Examples

**Basic Usage:**
```
!joinvc
!say Hello everyone! Welcome to the server!
!say This is a test of the text to speech system.
```

**Using Specific Voices:**
```
!voices
!say Rachel Hello! I'm Rachel, nice to meet you.
!say Clyde Hey there! I'm Clyde with a deeper voice.
!previewvoice Rachel
```

**Advanced:**
```
!say Antoni The weather today is absolutely beautiful!
!say Bella This is a longer sentence to demonstrate the natural flow of speech.
```

### Voice Selection

Use `!voices` to see all available voices grouped by category:
- **Premade** - High-quality professional voices
- **Cloned** - Custom cloned voices
- **Professional** - Premium voices
- **Generated** - AI-generated voices

**Popular Voices:**
- **Rachel** - Clear, friendly female (default)
- **Clyde** - Deep, authoritative male
- **Antoni** - Warm, engaging male
- **Bella** - Soft, pleasant female
- **Josh** - Professional male narrator

### Tips

💡 **Best Practices:**
- Keep text under 500 characters
- Use punctuation for natural pauses
- Join voice channel before using `!say`
- Test different voices for your needs

💡 **Performance:**
- First generation takes a few seconds
- Cached audio plays instantly
- Clear cache periodically if needed

---

## 🌤️ Weather Module (Open-Meteo)

Real-time weather forecasts and conditions using free Open-Meteo API.

### Commands

- `!weather [location]` or `!w [location]` - Current weather
- `!forecast [location]` or `!fc [location]` - 7-day forecast
- `!hourly [location]` - 24-hour forecast
- `!setlocation <location>` or `!setloc <location>` - Save default location

### Features

✅ **Comprehensive Weather Data**
- Current temperature and conditions
- Feels-like temperature
- Wind speed and direction
- Humidity and cloud cover
- Precipitation
- Air pressure

✅ **7-Day Forecast**
- Daily high/low temperatures
- Weather conditions
- Precipitation probability
- Wind speed
- Sunrise/sunset times

✅ **Hourly Forecast**
- Next 24 hours
- 3-hour intervals
- Temperature and precipitation
- Weather icons

✅ **Global Coverage**
- Works worldwide
- Multiple cities
- Accurate geocoding

### Setup

**Save Your Location:**
```
!setlocation London, UK
!setlocation New York, USA
!setlocation Tokyo, Japan
```

After saving, you can use commands without specifying location:
```
!weather
!forecast
!hourly
```

### Examples

**Current Weather:**
```
!weather London
!w Paris, France
!weather New York, USA
```

**7-Day Forecast:**
```
!forecast Tokyo
!fc Berlin, Germany
!forecast Sydney, Australia
```

**24-Hour Forecast:**
```
!hourly London
!hourly Miami, USA
```

**With Saved Location:**
```
!setlocation Los Angeles, USA
!weather
!forecast
!hourly
```

### Weather Display

**Current Weather Shows:**
- 🌡️ Temperature (Celsius & Fahrenheit)
- 💨 Wind (speed, direction, arrow)
- 💧 Humidity percentage
- ☁️ Cloud cover
- 🌧️ Precipitation
- 📊 Air pressure
- 📍 Location details

**7-Day Forecast Shows:**
- Day name and date
- Weather emoji and condition
- High/low temperatures (C & F)
- Rain probability and amount
- Maximum wind speed
- Sunrise and sunset times

**Hourly Forecast Shows:**
- Time slots (every 3 hours)
- Weather emoji
- Temperature
- Precipitation amount

### Location Tips

💡 **For Best Results:**
- Include country: `London, UK` not just `London`
- Use full state name: `Portland, Oregon` or `Portland, Maine`
- Major cities work without country
- Use English names

💡 **Supported Formats:**
```
✅ London, UK
✅ Paris, France
✅ New York, USA
✅ Tokyo
✅ Los Angeles, California
✅ Sydney, Australia
❌ LON (too short)
❌ NYC (abbreviation)
```

---

## 🔧 Technical Details

### Voice Module

**API:** ElevenLabs v1
**API Key:** Configured (sk_62a928...3db95fec)
**Default Voice:** Rachel (nDJIICjR9zfJExIFeSCN)
**Audio Format:** MP3
**Cache Location:** `VoiceTTS/`
**Max Text Length:** 500 characters

**Voice Settings:**
- Stability: 0.5
- Similarity Boost: 0.75
- Style: 0.0
- Speaker Boost: Enabled
- Model: eleven_monolingual_v1

### Weather Module

**API:** Open-Meteo (free, no key needed)
**Geocoding:** Open-Meteo Geocoding API
**Coverage:** Global
**Update Frequency:** Hourly
**Forecast Range:** 7 days
**Temperature Units:** Celsius (with Fahrenheit conversion)

**Data Sources:**
- Current: Real-time weather stations
- Forecast: NOAA GFS model
- Geocoding: OpenStreetMap data

---

## 🚀 Quick Start

### Voice TTS
```bash
# 1. Join voice channel in Discord
# 2. Use bot commands:
!joinvc
!say Hello world!
!voices
!say Rachel Welcome everyone!
```

### Weather
```bash
# 1. Set your location (optional):
!setlocation New York, USA

# 2. Check weather:
!weather
!forecast
!hourly
```

---

## 🛠️ Troubleshooting

### Voice Issues

**"Failed to join voice channel"**
- Ensure bot has Connect permission
- Check if bot role is high enough
- Try manually inviting bot to channel

**"Failed to generate speech"**
- Check ElevenLabs API quota
- Verify voice name is correct
- Use `!voices` to see available voices

**Audio not playing**
- Ensure you're in the voice channel
- Check bot's speaker permissions
- Try `!joinvc` again

### Weather Issues

**"Location not found"**
- Include country name
- Use full city name
- Try major cities nearby
- Example: `!weather London, UK`

**Old/incorrect data**
- Data updates hourly
- Check timestamp in embed footer
- Try again in a few minutes

---

## 💡 Pro Tips

### Voice Module
- Use shorter sentences for better quality
- Test different voices for different contexts
- Cached audio plays instantly - reuse phrases!
- Rachel is great for general announcements
- Clyde works well for dramatic effects

### Weather Module
- Save your location once, use forever
- Check `!hourly` before going out
- Use `!forecast` for planning trips
- Temperatures shown in both C and F
- Sunrise/sunset times in local timezone

---

## 🎯 Use Cases

### Voice TTS
- Welcome messages for new members
- Event announcements
- Reading chat messages
- Voice channel greetings
- Fun interactions
- Accessibility features

### Weather
- Daily weather checks
- Event planning (outdoor events)
- Travel preparation
- Server weather updates
- Location-based roleplay
- Community discussions

---

## 📊 Limitations

### Voice Module
- Max 500 characters per request
- Requires voice channel connection
- API rate limits apply
- Cache grows over time (manual cleanup needed)

### Weather Module
- Updates every hour
- 7-day forecast limit
- Requires internet connection
- Location must be valid city/town

# Claude Best Practices - Discord Bot Debugging & Development

## DO NOT REPEAT THE YOUTUBESERVICE DISASTER

This document exists because I wasted 30+ minutes on a 5-minute fix. Follow these rules religiously.

---

## Golden Rule: **VERIFY BEFORE BUILD**

Never add features to broken infrastructure. Fix the foundation first, then build.

---

## Emergency Debugging Protocol (When User Reports Error)

### Step 1: ADD LOGGING IMMEDIATELY (2 minutes max)
When user says "X is broken":

1. **Add ONE log line** to see what's null/failing
   ```csharp
   Console.WriteLine($"[DEBUG] _service is null: {_service == null}");
   ```
2. Build, run, test
3. **READ THE LOGS** before doing anything else

**DO NOT:**
- ❌ Rewrite entire modules
- ❌ Add new features
- ❌ "Fix" things you haven't verified are broken
- ❌ Guess at solutions

---

## Systematic Debugging Checklist

When something fails, check IN THIS ORDER:

### 1. **Dependency Injection** (CHECK FIRST!)
File: `Services/ServiceProvider.cs` (36 lines)

**Questions:**
- [ ] Is the service registered in ServiceProvider constructor?
- [ ] Is it in the GetService() method?
- [ ] Is it created in Program.cs?
- [ ] Is it passed to ServiceProvider?

**If service is NULL, it's 99% a DI issue. Check ServiceProvider FIRST.**

### 2. **Service Initialization**
- [ ] Is the service initialized in module's InitializeAsync?
- [ ] Are there null checks?
- [ ] Add logging: `Console.WriteLine($"Service: {_service != null ? "OK" : "NULL"}")`

### 3. **External Dependencies**
- [ ] Is yt-dlp/tool installed?
- [ ] Is API key valid?
- [ ] Is the external service working? (test with bash command)

### 4. **Logic/Code Issues**
Only check this AFTER verifying 1-3 above.

---

## File Reading Priority (When Debugging)

Read files in this order to diagnose issues fast:

### Tier 1: Small Infrastructure Files (Read FIRST)
1. `ServiceProvider.cs` (36 lines) ← **CHECK THIS FOR DI ISSUES**
2. `Program.cs` (100 lines) - Service creation
3. `ModuleManager.cs` - Module registration

### Tier 2: Service Files
4. The failing service (YouTubeService, AudioService, etc.)
5. Related data models

### Tier 3: Module Files
6. The module that's failing (MusicModule, etc.)

**DO NOT** read a 600-line module before checking a 36-line ServiceProvider.

---

## Before Adding Any Feature

### Checklist:
- [ ] Does the existing code work?
- [ ] Have I tested the basic functionality?
- [ ] Are all dependencies registered?
- [ ] Do the logs show services are initialized?

**IF ANY ANSWER IS NO, FIX IT FIRST.**

---

## Logging Standards

### Always Log These Critical Points:

**1. Service Initialization:**
```csharp
Console.WriteLine($"[ModuleName.Initialize] ServiceName: {_service != null ? "OK" : "NULL"}");
```

**2. Key Operations:**
```csharp
Console.WriteLine($"[ServiceName] Starting operation: {parameter}");
Console.WriteLine($"[ServiceName] Result: {result != null ? "SUCCESS" : "NULL"}");
```

**3. External Calls:**
```csharp
Console.WriteLine($"[ServiceName] Calling external API/tool...");
Console.WriteLine($"[ServiceName] Exit code: {exitCode}");
Console.WriteLine($"[ServiceName] Error: {error}");
```

**Keep logs in production - they're invaluable for debugging.**

---

## Common Failure Patterns & Solutions

### Pattern 1: "Service returned null"
**Check:** Is service registered in ServiceProvider?
**File:** `Services/ServiceProvider.cs`
**Fix:** Add to constructor, GetService(), and Program.cs

### Pattern 2: "Method not found"
**Check:** Does the method exist in the service?
**Action:** Read the service file, don't assume methods exist

### Pattern 3: "External tool failed"
**Check:** Run the tool manually via bash first
**Verify:** Tool is installed, updated, and working
**Then:** Check how the code calls it

---

## What NOT to Do (Mistakes from YouTubeService Incident)

### ❌ DON'T:
1. **Build features on broken code** - Fix first, enhance later
2. **Skip ServiceProvider checks** - It's 36 lines, just read it
3. **Make assumptions** - "AudioService works so DI must be fine" ← WRONG
4. **Fix cosmetic issues** - Don't refactor when the core is broken
5. **Test manually without logs** - Add logs, THEN test
6. **Rewrite entire modules** - Add ONE log line first
7. **Commit broken code** - Test before committing

### ✅ DO:
1. **Add logging immediately** - One line can save 30 minutes
2. **Check DI registration first** - Most common issue
3. **Read small files first** - ServiceProvider before modules
4. **Verify with logs** - Don't guess, confirm
5. **Fix root cause** - Not symptoms
6. **Test after EVERY change** - Small iterations
7. **Ask user to test** - They can run it faster than you can automate

---

## Dependency Injection Quick Reference

### To Add a New Service:

**1. Create the service:**
```csharp
// In appropriate file (e.g., YouTubeService.cs)
public class YouTubeService { ... }
```

**2. Register in ServiceProvider.cs:**
```csharp
private readonly YourService _yourService;

public ServiceProvider(AudioService audio, GuildSettings settings, YourService yourService)
{
    _audioService = audio;
    _guildSettings = settings;
    _yourService = yourService;  // ADD THIS
}

public object? GetService(Type serviceType)
{
    if (serviceType == typeof(AudioService)) return _audioService;
    if (serviceType == typeof(GuildSettingsService)) return _guildSettings;
    if (serviceType == typeof(YourService)) return _yourService;  // ADD THIS
    return null;
}
```

**3. Create and pass in Program.cs:**
```csharp
_audioService = new AudioService();
_guildSettings = new GuildSettingsService();
var yourService = new YourService();  // ADD THIS

var services = new ServiceProvider(_audioService, _guildSettings, yourService);  // ADD PARAM
```

**4. Verify with logging:**
```csharp
// In your module's InitializeAsync:
_yourService = services.GetService(typeof(YourService)) as YourService;
Console.WriteLine($"[YourModule] YourService: {_yourService != null ? "OK" : "NULL"}");
```

---

## Time Budgets (Max Time Before Escalating)

- **Adding logs:** 2 minutes
- **Checking ServiceProvider:** 3 minutes  
- **Testing external tool (bash):** 5 minutes
- **Reading service file:** 5 minutes
- **Simple DI fix:** 5 minutes

**If you exceed these times, you're doing it wrong. Stop and reassess.**

---

## The "5-Minute Rule"

If you can't identify the root cause in 5 minutes:

1. **STOP** - You're going down the wrong path
2. **Add comprehensive logging** - Every step of execution
3. **Ask user to test and copy logs** - Get real data
4. **Read the logs systematically** - Trace execution
5. **Find where it breaks** - Don't guess

**Most bugs are simple. If it seems complicated, you're looking in the wrong place.**

---

## Testing New Features

### Before writing code:
1. Verify dependencies are registered
2. Test external tools work
3. Check existing similar features

### While writing code:
1. Add logging for key operations
2. Test incrementally (don't write 600 lines then test)
3. Verify each service is accessible

### After writing code:
1. Build (check for errors)
2. Run (check logs show services initialized)
3. Test basic functionality
4. Then test advanced features

---

## Project-Specific Quick Reference

### Services That Must Be Registered:
- [x] AudioService
- [x] GuildSettingsService
- [x] YouTubeService

**When adding a new service, CHECK THIS LIST and update ServiceProvider.**

### Key Files:
- **ServiceProvider.cs** - DI registration (CHECK FIRST FOR NULL SERVICES)
- **Program.cs** - Service creation and initialization
- **ModuleManager.cs** - Module discovery and registration

### Common Commands:
```bash
# Build
dotnet build

# Run from project directory
dotnet run

# Test yt-dlp manually
yt-dlp --dump-json --no-playlist "URL"

# Check for running bot
tasklist | findstr dotnet
```

---

## Commit Message Standards

### Good Commit Message:
```
Fix: Register YouTubeService in dependency injection

Issue: MusicModule._youtubeService was null
Root cause: Service not registered in ServiceProvider
Solution: Added to ServiceProvider constructor and Program.cs
Verified: Service now initializes correctly
```

### Bad Commit Message:
```
fix stuff
```

---

## Remember:

> **"The fastest way to solve a problem is to properly identify it first."**

> **"When you hear hoofbeats, think horses not zebras."** (Common issues are common)

> **"ServiceProvider.cs is 36 lines. Just read it."**

---

## Post-Mortem Template (For Major Incidents)

When something takes >15 minutes to fix, document it:

**Incident:** [What broke]  
**Time to Fix:** [Actual time]  
**Should Have Taken:** [Estimated correct time]  
**Root Cause:** [The actual issue]  
**What I Did Wrong:** [Mistakes made]  
**What I Should Have Done:** [Correct approach]  
**Prevention:** [How to avoid this in future]

---

## Emergency Contacts

**When completely stuck:**
1. Ask user to copy full console logs
2. Ask user to test external tool manually (e.g., yt-dlp)
3. Read ServiceProvider.cs
4. Add logging to every step
5. Binary search the problem (comment out half the code)

---

## Final Words

**This file exists because I wasted your time.**

Every minute spent debugging is a minute not building features.

**Follow. These. Practices.**

If you find yourself violating these rules, STOP and come back to this document.

---

*Last Updated: 2026-01-27 - After the Great YouTubeService Incident*

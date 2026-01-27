# Server Management Modules

## 🛡️ Moderation Module

Complete moderation toolkit for server management.

### Commands

**Warnings:**
- `!warn @user [reason]` - Warn a user
- `!warnings [@user]` - View warnings (your own or mentioned user's)
- `!clearwarns @user` - Clear all warnings for a user

**Actions:**
- `!kick @user [reason]` - Kick a user from the server
- `!ban @user [reason]` - Ban a user from the server
- `!unban <user-id> [reason]` - Unban a user by ID
- `!mute @user [duration] [reason]` - Mute a user (e.g., `30m`, `2h`, `1d`)
- `!unmute @user` - Unmute a user

**Utilities:**
- `!purge <amount>` - Delete messages (1-100, max 14 days old)
- `!modlogs` - View recent moderation actions

### Permissions Required
- **Kick/Ban:** Requires Kick Members or Ban Members permission
- **Mute:** Requires Manage Roles permission
- **Purge:** Requires Manage Messages permission
- **Mod Logs:** Requires View Audit Log permission

### Features
- ✅ Warning system with full history
- ✅ Timed mutes (auto-expire)
- ✅ DM notifications to users
- ✅ Full action logging
- ✅ Automatic "Muted" role creation

### Examples
```
!warn @user Spamming in chat
!mute @user 30m Excessive caps
!kick @user Breaking server rules
!ban @user Harassment
!purge 50
!modlogs
```

---

## 🎭 Reaction Roles Module

Self-assignable roles via message reactions.

### Commands

- `!reactionrole <message-id> <emoji> <@role>` (or `!rr`)
- `!listreactionroles` (or `!listrr`)
- `!removereactionrole <message-id>` (or `!removerr`)

### How It Works

1. Create a message with role information
2. Use `!reactionrole` to link an emoji to a role
3. Users react to get/remove roles automatically

### Setup Guide

**Step 1:** Enable Developer Mode
- User Settings → Advanced → Developer Mode

**Step 2:** Create a roles message
```
React to get your roles!
🎮 - Gamer
🎨 - Artist
🎵 - Music Lover
```

**Step 3:** Get message ID
- Right-click message → Copy ID

**Step 4:** Add reaction roles
```
!reactionrole 123456789 🎮 @Gamer
!reactionrole 123456789 🎨 @Artist
!reactionrole 123456789 🎵 @Music
```

**Step 5:** Done!
- Bot adds reactions automatically
- Users react to get roles
- Remove reaction to remove role

### Features
- ✅ Automatic role add/remove
- ✅ Standard & custom emoji support
- ✅ Multiple roles per message
- ✅ Real-time role updates

### Examples
```
!rr 123456789 ✅ @Verified
!rr 123456789 🔔 @Notifications
!listrr
!removerr 123456789
```

---

## 👋 Welcome & Goodbye Module

Custom messages for members joining and leaving.

### Commands

- `!setwelcome <message>` - Set welcome message
- `!setgoodbye <message>` - Set goodbye message
- `!testwelcome` - Test welcome message
- `!welcomeinfo` - View current configuration

### Placeholders

Use these in your messages:
- `{user}` - Mentions the user (@User)
- `{username}` - User's username (User)
- `{server}` - Server name
- `{membercount}` - Total members

### Setup Guide

**Welcome Message:**
```
!setwelcome Welcome {user} to {server}! 🎉 You are member #{membercount}. Check out <#rules> and <#announcements>!
```

**Goodbye Message:**
```
!setgoodbye Goodbye {username}, thanks for being part of {server}! We'll miss you. 👋
```

**Test:**
```
!testwelcome
```

**Disable:**
```
!setwelcome off
!setgoodbye off
```

### Features
- ✅ Custom welcome messages
- ✅ Custom goodbye messages
- ✅ Per-channel configuration
- ✅ Automatic member count
- ✅ Channel mentions support

### Examples
```
!setwelcome Hey {user}! Welcome to **{server}**! 🎊
!setgoodbye See you later {username}! 😢
!welcomeinfo
!testwelcome
```

---

## 🔒 Permission Requirements Summary

| Module | Command | Permission Needed |
|--------|---------|-------------------|
| **Moderation** | warn, kick | Kick Members |
| **Moderation** | ban, unban | Ban Members |
| **Moderation** | mute, unmute | Manage Roles |
| **Moderation** | purge | Manage Messages |
| **Moderation** | modlogs | View Audit Log |
| **Reaction Roles** | All commands | Manage Roles |
| **Welcome** | All commands | Manage Server |

---

## 📊 Data Storage

All modules store data in JSON files per guild:

- **ModerationData/** - Warnings, mutes, action history
- **ReactionRolesData/** - Emoji→Role mappings
- **WelcomeData/** - Welcome/goodbye configuration

**Note:** These folders are automatically excluded from git (.gitignore).

---

## 🚀 Quick Start

### 1. Enable Moderation
```
!warn @user Testing the warning system
!warnings
```

### 2. Set Up Reaction Roles
```
# Create a message, then:
!rr <message-id> 🎮 @Gamer
```

### 3. Configure Welcome Messages
```
!setwelcome Welcome {user} to {server}! 🎉
!setgoodbye Goodbye {username}! 👋
```

---

## 💡 Pro Tips

**Moderation:**
- Use timed mutes for temporary punishments (`!mute @user 1h`)
- Check warnings before taking action (`!warnings @user`)
- Review mod logs regularly (`!modlogs`)

**Reaction Roles:**
- Create a clean roles message in a dedicated channel
- Use clear emoji choices (not too similar)
- Test with a personal account first

**Welcome Messages:**
- Keep messages short and friendly
- Include important channel mentions
- Use emojis to make it welcoming
- Test before enabling (`!testwelcome`)

---

## 🛠️ Troubleshooting

**Mute role not working?**
- Bot needs "Manage Roles" permission
- Bot's role must be above "Muted" role

**Reaction roles not responding?**
- Ensure bot has "Manage Roles" permission
- Check emoji is valid (works in Discord)
- Verify message ID is correct

**Welcome messages not sending?**
- Check bot has "Send Messages" permission in target channel
- Verify channel still exists
- Test with `!testwelcome`

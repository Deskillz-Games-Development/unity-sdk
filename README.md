# Deskillz Unity SDK

<p align="center">
  <img src="https://deskillz.games/logo.png" alt="Deskillz.Games" width="200"/>
</p>

<p align="center">
  <strong>Integrate competitive tournaments into your Unity games</strong>
</p>

<p align="center">
  <a href="https://github.com/deskillz-games/unity-sdk/releases"><img src="https://img.shields.io/badge/version-2.3.0-blue.svg" alt="Version"></a>
  <a href="https://unity.com"><img src="https://img.shields.io/badge/unity-2020.3+-black.svg" alt="Unity"></a>
  <a href="https://github.com/deskillz-games/unity-sdk/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License"></a>
</p>

<p align="center">
  <a href="#installation">Installation</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#features">Features</a> •
  <a href="#auto-updater">Auto-Updater</a> •
  <a href="#private-rooms">Private Rooms</a> •
  <a href="#navigation-deep-links">Navigation Links</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#support">Support</a>
</p>

---

## Overview

The Deskillz Unity SDK enables game developers to integrate their Unity games with the Deskillz.Games competitive gaming platform. Players can compete in skill-based tournaments, create private rooms to play with friends, and win cryptocurrency prizes (BTC, ETH, SOL, XRP, BNB, USDT, USDC).

### How It Works (Global Lobby Architecture)

```
┌─────────────────────────────────────────────────────────────────┐
│                    PLAYER JOURNEY                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. Player opens Deskillz.Games website/app                     │
│              ↓                                                  │
│  2. Player browses Global Lobby                                 │
│     • Select game                                               │
│     • Choose tournament/match type                              │
│     • Join matchmaking queue OR private room                    │
│              ↓                                                  │
│  3. Match found → Deep link sent to your game                   │
│     deskillz://launch?matchId=abc123&token=xyz...               │
│              ↓                                                  │
│  4. Your game app opens via deep link                           │
│              ↓                                                  │
│  5. SDK receives match data → Start gameplay                    │
│              ↓                                                  │
│  6. Player plays → Score submitted → Results shown              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Key Point:** Matchmaking happens in the Deskillz platform, NOT in your game. Your SDK just needs to:
1. Handle deep links (navigation + match launch)
2. Start the match
3. Submit scores securely

## Requirements

- **Unity:** 2020.3 LTS or newer
- **Platforms:** iOS 12+, Android 5.0+ (API 21)
- **.NET:** Standard 2.1
- **Build Tools:** Xcode 14+ (iOS), Android SDK (Android)

## Installation

### Option 1: Unity Package Manager (Recommended)

1. Open **Window → Package Manager**
2. Click **+ → Add package from git URL**
3. Enter: `https://github.com/deskillz/unity-sdk.git`

### Option 2: Download .unitypackage

1. Download latest release from [deskillz.games/developer](https://deskillz.games/developer)
2. Import via **Assets → Import Package → Custom Package**

### Option 3: Clone Repository

```bash
cd YourProject/Packages
git clone https://github.com/deskillz/unity-sdk.git com.deskillz.sdk
```

### Setup

1. Create config: **Assets → Create → Deskillz → Config**
2. Enter your API Key and Game ID (get from [deskillz.games/developer](https://deskillz.games/developer))
3. Place in `Resources` folder

## Quick Start

### 1. Initialize the SDK

```csharp
using Deskillz;
using Deskillz.Lobby;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Initialize SDK (auto-loads config from Resources)
        Deskillz.Initialize();
        
        // Register for deep link events
        DeepLinkHandler.Initialize();
        
        // Navigation events (NEW in v2.0)
        DeepLinkHandler.OnNavigationReceived += HandleNavigation;
        
        // Match launch events
        DeepLinkHandler.OnMatchLaunchReceived += HandleMatchLaunch;
        DeepLinkHandler.OnMatchReady += OnMatchReady;
        DeepLinkHandler.OnValidationFailed += OnValidationFailed;
        
        // Check for updates (NEW in v2.3)
        DeskillzUpdater.Instance.CheckForUpdates();
        
        // Process any pending deep links (cold start)
        if (DeepLinkHandler.HasPendingDeepLink())
        {
            DeepLinkHandler.ProcessPendingDeepLinks();
        }
    }
    
    void OnDestroy()
    {
        DeepLinkHandler.OnNavigationReceived -= HandleNavigation;
        DeepLinkHandler.OnMatchLaunchReceived -= HandleMatchLaunch;
        DeepLinkHandler.OnMatchReady -= OnMatchReady;
        DeepLinkHandler.OnValidationFailed -= OnValidationFailed;
    }
}
```

### 2. Handle Navigation Deep Links (NEW in v2.0)

```csharp
using System.Collections.Generic;

void HandleNavigation(NavigationAction action, Dictionary<string, string> parameters)
{
    switch (action)
    {
        case NavigationAction.Tournaments:
            UIManager.Instance.ShowTournaments();
            break;
            
        case NavigationAction.Wallet:
            UIManager.Instance.ShowWallet();
            break;
            
        case NavigationAction.Profile:
            UIManager.Instance.ShowProfile();
            break;
            
        case NavigationAction.Game:
            string gameId = parameters.GetValueOrDefault("id", "");
            if (!string.IsNullOrEmpty(gameId))
            {
                UIManager.Instance.ShowGameDetails(gameId);
            }
            break;
            
        case NavigationAction.Settings:
            UIManager.Instance.ShowSettings();
            break;
    }
}
```

### 3. Handle Match Launch Deep Links

```csharp
// Simple handler - just matchId and token
void HandleMatchLaunch(string matchId, string authToken)
{
    // Store auth token for API calls
    DeskillzApi.SetAuthToken(authToken);
    
    // Load match scene
    SceneManager.LoadScene("MatchScene");
    
    Debug.Log($"Launching match: {matchId}");
}

// Full match data handler
void OnMatchReady(MatchLaunchData data)
{
    Debug.Log($"Match ready: {data.MatchId}");
    Debug.Log($"Opponent: {data.OpponentName} (Rating: {data.OpponentRating})");
    Debug.Log($"Entry Fee: {data.EntryFee} {data.Currency}");
    Debug.Log($"Duration: {data.Duration}s");
    
    // Store match data
    CurrentMatchData = data;
    
    // Load game scene
    SceneManager.LoadScene("GameScene");
}

void OnValidationFailed(string reason, MatchLaunchData data)
{
    Debug.LogError($"Match validation failed: {reason}");
    // Show error and return to main menu
    ShowErrorDialog(reason);
}
```

### 4. Submit Score

```csharp
// When player finishes the game
public void OnGameComplete(int finalScore, float playDuration)
{
    // Score is automatically encrypted with HMAC-SHA256
    Deskillz.SubmitScore(finalScore, playDuration, OnScoreSubmitted);
}

void OnScoreSubmitted(bool success, string message)
{
    if (success)
    {
        Debug.Log("Score submitted successfully!");
        Deskillz.EndMatch();
    }
    else
    {
        Debug.LogError($"Score submission failed: {message}");
    }
}
```

### 5. Minimal Integration (2 Lines!)

```csharp
using Deskillz;

// When player finishes the game
Deskillz.SubmitScore(playerScore);
Deskillz.EndMatch();
```

That's it for basic integration! The SDK handles everything else automatically.

---

## Auto-Updater (NEW in v2.3.0)

The SDK includes automatic update checking to ensure players always have the latest version of your game. This integrates with the Deskillz APK Hosting system.

### Basic Usage

```csharp
using Deskillz;

void Start()
{
    // Auto-checks on startup by default
    // Or check manually:
    DeskillzUpdater.Instance.CheckForUpdates();
}
```

### Custom UI with Events

```csharp
using Deskillz;

public class UpdateManager : MonoBehaviour
{
    void Start()
    {
        // Configure version info
        DeskillzUpdater updater = DeskillzUpdater.Instance;
        updater.CurrentVersion = "1.0.0";
        updater.CurrentVersionCode = 1;
        
        // Subscribe to events
        DeskillzUpdater.OnUpdateCheckStarted += () => ShowLoadingSpinner();
        DeskillzUpdater.OnUpdateAvailable += HandleOptionalUpdate;
        DeskillzUpdater.OnForcedUpdateRequired += HandleForcedUpdate;
        DeskillzUpdater.OnNoUpdateNeeded += HandleNoUpdate;
        DeskillzUpdater.OnUpdateCheckFailed += HandleCheckFailed;
        DeskillzUpdater.OnUpdateAccepted += HandleUpdateAccepted;
        DeskillzUpdater.OnUpdateSkipped += HandleUpdateSkipped;
        
        updater.CheckForUpdates();
    }
    
    void HandleOptionalUpdate(UpdateInfo info)
    {
        Debug.Log($"Optional update available: {info.LatestVersion}");
        Debug.Log($"Download size: {info.FileSizeFormatted}");
        Debug.Log($"Release notes: {info.ReleaseNotes}");
        
        // Show your custom dialog with Update/Skip buttons
        ShowUpdateDialog(info, canSkip: true);
    }
    
    void HandleForcedUpdate(UpdateInfo info)
    {
        Debug.Log($"REQUIRED update: {info.LatestVersion}");
        
        // Show dialog with only Update button (no skip)
        ShowForcedUpdateDialog(info);
        
        // Pause the game - user must update
        Time.timeScale = 0;
    }
    
    void HandleNoUpdate()
    {
        Debug.Log("App is up to date!");
        HideLoadingSpinner();
        ShowMainMenu();
    }
    
    void HandleCheckFailed(string error)
    {
        Debug.LogWarning($"Update check failed: {error}");
        // Continue anyway - don't block users on network errors
        HideLoadingSpinner();
        ShowMainMenu();
    }
    
    void HandleUpdateAccepted(UpdateInfo info)
    {
        Debug.Log($"User accepted update to {info.LatestVersion}");
        // Analytics tracking, etc.
    }
    
    void HandleUpdateSkipped(UpdateInfo info)
    {
        Debug.Log($"User skipped update {info.LatestVersion}");
        ShowMainMenu();
    }
    
    // Called from Update button in your UI
    public void OnUpdateButtonClicked()
    {
        DeskillzUpdater.Instance.StartUpdate(); // Opens download URL in browser
    }
    
    // Called from Skip button (optional updates only)
    public void OnSkipButtonClicked()
    {
        DeskillzUpdater.Instance.SkipUpdate(); // Remembers skipped version
    }
}
```

### Built-in Update UI

The SDK includes a pre-built update dialog you can use:

```csharp
using Deskillz;
using Deskillz.UI;

void Start()
{
    // Use the built-in UI (loads from Resources)
    DeskillzUpdaterUI.Instance.ShowOnUpdateAvailable = true;
    DeskillzUpdaterUI.Instance.ShowOnForcedUpdate = true;
    
    // Customize appearance
    DeskillzUpdaterUI.Instance.SetTheme(UpdateUITheme.Dark);
    
    // Check for updates - UI shows automatically
    DeskillzUpdater.Instance.CheckForUpdates();
}
```

### Configuration Options

```csharp
DeskillzUpdater updater = DeskillzUpdater.Instance;

// Version info (REQUIRED - must match your APK)
updater.CurrentVersion = "1.0.0";           // versionName in build.gradle
updater.CurrentVersionCode = 1;             // versionCode in build.gradle

// Behavior settings
updater.AutoCheckOnStart = true;            // Check automatically on app start
updater.AutoCheckDelay = 2.0f;              // Delay before auto-check (seconds)
updater.ShowOptionalUpdatePrompt = true;    // Show dialog for optional updates
updater.AllowSkipOptionalUpdate = true;     // Allow users to skip optional updates
updater.RememberSkippedVersion = true;      // Don't prompt again for skipped versions
updater.SkipVersionExpireDays = 7;          // Re-prompt after N days even if skipped

// Network settings
updater.TimeoutSeconds = 30;                // API request timeout
updater.RetryOnFailure = true;              // Retry on network errors
updater.MaxRetries = 3;                     // Maximum retry attempts
```

### UpdateInfo Properties

| Property | Type | Description |
|----------|------|-------------|
| `LatestVersion` | string | Version string (e.g., "1.2.0") |
| `VersionCode` | int | Integer version code (e.g., 10200) |
| `UpdateAvailable` | bool | Whether an update is available |
| `IsForced` | bool | Whether update is required (can't skip) |
| `DownloadUrl` | string | Direct APK download URL |
| `FileSize` | long | File size in bytes |
| `FileSizeFormatted` | string | Human-readable size (e.g., "52.4 MB") |
| `ReleaseNotes` | string | Changelog/release notes text |
| `MinOsVersion` | string | Minimum Android API level required |
| `ReleasedAt` | DateTime | When this version was released |

### Update Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `OnUpdateCheckStarted` | None | Update check has begun |
| `OnUpdateCheckCompleted` | `result`, `UpdateInfo` | Check completed (success or fail) |
| `OnUpdateAvailable` | `UpdateInfo` | Optional update is available |
| `OnForcedUpdateRequired` | `UpdateInfo` | Required update - user must update |
| `OnNoUpdateNeeded` | None | App is already up to date |
| `OnUpdateCheckFailed` | `string error` | Network or parsing error |
| `OnUpdateAccepted` | `UpdateInfo` | User clicked "Update" button |
| `OnUpdateSkipped` | `UpdateInfo` | User clicked "Skip" button |

### Version Code Best Practices

```csharp
// Recommended version code format: MAJOR * 10000 + MINOR * 100 + PATCH
// Examples:
// 1.0.0  → 10000
// 1.2.0  → 10200
// 1.2.3  → 10203
// 2.0.0  → 20000

// In Unity Player Settings → Android → Other Settings:
// Bundle Version Code: 10000 (for v1.0.0)
```

---

## Private Rooms (NEW in v2.2.0)

Players can create private rooms to play with friends! The SDK includes a full Room API and pre-built UI components.

### Room API

```csharp
using Deskillz.Rooms;

// Create a room
DeskillzRooms.CreateRoom(new CreateRoomConfig
{
    Name = "My Room",
    EntryFee = 5.00m,
    EntryCurrency = "USDT",
    MaxPlayers = 4,
    MinPlayers = 2,
    Mode = RoomMode.Sync,
    Visibility = RoomVisibility.PublicListed
},
onSuccess: (room) => Debug.Log($"Room created: {room.RoomCode}"),
onError: (error) => Debug.LogError(error.Message));

// Quick create with defaults
DeskillzRooms.QuickCreateRoom("Quick Match", 1.00m, OnSuccess, OnError);

// Join by code
DeskillzRooms.JoinRoom("DSKZ-AB3C", OnSuccess, OnError);

// Browse public rooms
DeskillzRooms.GetPublicRooms(
    onSuccess: (rooms) => { /* Display room list */ },
    onError: (error) => { /* Handle error */ });

// Get room by code (preview before joining)
DeskillzRooms.GetRoomByCode("DSKZ-AB3C", OnSuccess, OnError);

// Ready up
DeskillzRooms.SetReady(true);

// Send chat message
DeskillzRooms.SendChat("Hello everyone!");

// Leave room
DeskillzRooms.LeaveRoom(OnSuccess, OnError);

// Host: Cancel room
DeskillzRooms.CancelRoom(OnSuccess, OnError);

// Host: Kick player
DeskillzRooms.KickPlayer(playerId, OnSuccess, OnError);

// Host: Start match
DeskillzRooms.StartMatch(OnSuccess, OnError);
```

### Room Events

```csharp
// Subscribe to room events
DeskillzRooms.OnRoomJoined += (room) => Debug.Log($"Joined: {room.Name}");
DeskillzRooms.OnRoomUpdated += (room) => UpdateRoomDisplay(room);
DeskillzRooms.OnPlayerJoined += (player) => Debug.Log($"{player.Username} joined");
DeskillzRooms.OnPlayerLeft += (playerId) => Debug.Log($"Player left");
DeskillzRooms.OnPlayerReadyChanged += (playerId, isReady) => UpdatePlayerCard(playerId, isReady);
DeskillzRooms.OnCountdownStarted += (seconds) => ShowCountdown(seconds);
DeskillzRooms.OnCountdownTick += (seconds) => UpdateCountdown(seconds);
DeskillzRooms.OnMatchLaunching += (data) => StartMatch(data);
DeskillzRooms.OnChatReceived += (senderId, username, message) => ShowChatMessage(username, message);
DeskillzRooms.OnKicked += (reason) => ShowKickedMessage(reason);
DeskillzRooms.OnRoomCancelled += (reason) => ReturnToLobby();
DeskillzRooms.OnRoomLeft += () => ReturnToLobby();
```

### Pre-Built Room UI

The SDK includes ready-to-use UI components for private rooms:

```csharp
using Deskillz.UI.Rooms;

// Show room browser (list of public rooms)
PrivateRoomUI.Instance.ShowRoomList();

// Show create room form
PrivateRoomUI.Instance.ShowCreateRoom();

// Show join by code dialog
PrivateRoomUI.Instance.ShowJoinRoom();

// Show with pre-filled code (e.g., from deep link or share)
PrivateRoomUI.Instance.ShowJoinRoom("DSKZ-AB3C");

// Show room lobby (waiting room)
PrivateRoomUI.Instance.ShowRoomLobby();

// Quick actions (create/join and auto-show lobby)
PrivateRoomUI.Instance.QuickCreateRoom("My Room", 5.00m);
PrivateRoomUI.Instance.QuickJoinRoom("DSKZ-AB3C");

// Hide all room UI
PrivateRoomUI.Instance.HideAll();

// Close and remove from viewport
PrivateRoomUI.Instance.Close();
```

### Room UI Components

| Component | File | Description |
|-----------|------|-------------|
| **PrivateRoomUI** | `PrivateRoomUI.cs` | Main UI manager singleton, orchestrates all panels |
| **RoomListUI** | `RoomListUI.cs` | Browse public rooms with search, filter, and sort |
| **CreateRoomUI** | `CreateRoomUI.cs` | Room creation form with validation |
| **JoinRoomUI** | `JoinRoomUI.cs` | Enter room code dialog with preview |
| **RoomLobbyUI** | `RoomLobbyUI.cs` | Waiting room with player list, ready status, chat |
| **RoomPlayerCard** | `RoomPlayerCard.cs` | Individual player card + UIComponents helper |

All UI components are located in: `Runtime/UI/Rooms/`

### Room UI Events

```csharp
// Subscribe to UI navigation events
PrivateRoomUI.Instance.OnPanelShown += (room) => Debug.Log("Panel shown");
PrivateRoomUI.Instance.OnRoomCreatedFromUI += (room) => Debug.Log($"Created: {room.RoomCode}");
PrivateRoomUI.Instance.OnRoomJoinedFromUI += (room) => Debug.Log($"Joined: {room.Name}");
PrivateRoomUI.Instance.OnAllHidden += () => Debug.Log("UI hidden");
```

---

## Navigation Deep Links (NEW in v2.0)

The Deskillz platform can send navigation deep links to your game for seamless user experience.

### Supported Navigation Actions

| URL | Action | Description |
|-----|--------|-------------|
| `deskillz://tournaments` | Tournaments | Show tournament list |
| `deskillz://wallet` | Wallet | Show wallet screen |
| `deskillz://profile` | Profile | Show user profile |
| `deskillz://game?id=xxx` | Game | Show specific game details |
| `deskillz://settings` | Settings | Show settings screen |
| `deskillz://launch?matchId=xxx` | Match | Start match |

### NavigationAction Enum

```csharp
public enum NavigationAction
{
    None,
    Tournaments,
    Wallet,
    Profile,
    Game,
    Settings
}
```

### Testing Navigation Links

```csharp
// Test navigation deep links
DeepLinkHandler.SimulateDeepLink("deskillz://tournaments");
DeepLinkHandler.SimulateDeepLink("deskillz://wallet");
DeepLinkHandler.SimulateDeepLink("deskillz://game?id=battle-blocks");

// Test match launch
DeepLinkHandler.SimulateDeepLink("deskillz://launch?matchId=test-123&token=test-token");
```

---

## Match Launch Deep Link Format

Your game will receive match launch deep links in this format:

```
deskillz://launch?
  matchId=<match_id>&
  token=<auth_token>&
  gameId=<game_id>&
  mode=<SYNC|ASYNC>&
  opponentId=<opponent_id>&
  entryFee=<amount>&
  currency=<BTC|ETH|USDT|etc>&
  duration=<seconds>&
  seed=<random_seed>
```

The SDK parses this automatically - you just handle the `OnMatchReady` or `OnMatchLaunchReceived` events.

## MatchLaunchData Fields

| Field | Type | Description |
|-------|------|-------------|
| `MatchId` | string | Unique match identifier |
| `TournamentId` | string | Tournament this match belongs to |
| `Token` | string | Authentication token for API calls |
| `Duration` | int | Match duration in seconds |
| `RandomSeed` | int | Seed for deterministic gameplay |
| `EntryFee` | float | Entry fee amount |
| `Currency` | string | Currency (BTC, ETH, USDT, etc.) |
| `MatchType` | enum | Synchronous or Asynchronous |
| `OpponentId` | string | Opponent player ID |
| `OpponentName` | string | Opponent display name |
| `OpponentRating` | int | Opponent skill rating |

## Architecture: Global Lobby vs Old SDK-Based

| Aspect | Old (SDK-Based) | New (Global Lobby) ✅ |
|--------|-----------------|----------------------|
| Matchmaking Location | Inside your game | Deskillz website/app |
| Player Pool | Fragmented per-game | Unified across all games |
| User Experience | Inconsistent | Consistent platform UI |
| SDK Complexity | High (matchmaking logic) | Low (deep links only) |
| Developer Burden | Heavy | Minimal |
| Private Rooms | Complex to implement | Built into platform + SDK |
| NPC Opponents | SDK handles | Platform handles |
| Navigation | N/A | Full deep link support |

## Features

| Feature | Description |
|---------|-------------|
| 🔄 **Auto-Updater** | Automatic game updates with forced/optional prompts (NEW in v2.3) |
| 🔗 **Navigation Deep Links** | Navigate to any screen from Deskillz app (NEW in v2.0) |
| 🎮 **Match Launch Deep Links** | Receive match data from Global Lobby |
| 🚪 **Private Rooms** | Create/join rooms with friends (NEW in v2.2) |
| 🎨 **Pre-built Room UI** | Ready-to-use room management interface (NEW in v2.2) |
| 🏆 **Asynchronous Tournaments** | Players compete separately, scores compared |
| ⚡ **Real-time Multiplayer** | 2-10 players competing simultaneously |
| 🎯 **Custom Stages** | Player-created private rooms |
| 💰 **Cryptocurrency Prizes** | BTC, ETH, SOL, XRP, BNB, USDT, USDC |
| 🎨 **Built-in UI** | Pre-made UI components with themes |
| 🛡️ **Anti-Cheat** | Server-side validation and protection |
| 📶 **Offline Support** | Automatic score caching and retry |
| 🔐 **Score Encryption** | HMAC-SHA256 signed submission |

## SDK Structure

```
deskillz-unity-sdk/
├── Runtime/
│   ├── Core/
│   │   ├── Deskillz.cs
│   │   ├── DeskillzConfig.cs
│   │   ├── DeskillzManager.cs
│   │   ├── DeskillzEvents.cs
│   │   ├── DeskillzModels.cs
│   │   ├── DeskillzNetwork.cs
│   │   ├── DeskillzUpdater.cs          # NEW in v2.3
│   │   └── DeskillzUpdaterUI.cs        # NEW in v2.3
│   ├── Match/
│   │   ├── MatchController.cs
│   │   ├── MatchTimer.cs
│   │   └── MatchStateMachine.cs
│   ├── Security/
│   │   ├── ScoreManager.cs
│   │   ├── ScoreEncryption.cs
│   │   └── ScoreValidator.cs
│   ├── Rooms/                          # NEW in v2.2
│   │   ├── DeskillzRooms.cs            # Main room API
│   │   ├── RoomModels.cs               # Room data models
│   │   ├── RoomApiClient.cs            # HTTP REST client
│   │   └── RoomWebSocket.cs            # Real-time WebSocket
│   ├── UI/
│   │   ├── DeskillzUIManager.cs
│   │   ├── UIPanel.cs
│   │   └── Rooms/                      # NEW in v2.2
│   │       ├── PrivateRoomUI.cs        # Main UI manager
│   │       ├── RoomListUI.cs           # Browse public rooms
│   │       ├── CreateRoomUI.cs         # Create room form
│   │       ├── JoinRoomUI.cs           # Join by code dialog
│   │       ├── RoomLobbyUI.cs          # Waiting room
│   │       └── RoomPlayerCard.cs       # Player card component
│   ├── Lobby/
│   │   ├── DeepLinkHandler.cs
│   │   ├── DeskillzBridge.cs
│   │   └── DeskillzLobbyClient.cs
│   ├── Multiplayer/
│   │   └── SyncManager.cs
│   └── NPC/
│       └── NPCManager.cs
├── Editor/
│   └── DeskillzEditor.cs
├── package.json
└── README.md
```

## iOS Setup

Add URL scheme to `Info.plist`:

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.yourstudio.yourgame</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>deskillz</string>
            <string>yourgame</string>
        </array>
    </dict>
</array>
```

In Unity, add to **Player Settings → iOS → Other Settings → Supported URL Schemes**.

## Android Setup

Add to `AndroidManifest.xml` (Unity auto-generates, or use custom template):

```xml
<activity android:name="com.unity3d.player.UnityPlayerActivity"
          android:launchMode="singleTask">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="deskillz" />
        <data android:scheme="yourgame" />
    </intent-filter>
</activity>
```

**Important:** Use `android:launchMode="singleTask"` to ensure deep links are handled by the existing app instance.

## Test Mode

Test your integration without real currency:

```csharp
// Starts automatically in Unity Editor
// Or enable manually in DeskillzConfig

// Start a test match
Deskillz.StartTestMatch(MatchMode.Asynchronous);

// Simulate opponent score
Deskillz.SimulateOpponentScore(1000);

// Test navigation deep links (NEW)
DeepLinkHandler.SimulateDeepLink("deskillz://tournaments");
DeepLinkHandler.SimulateDeepLink("deskillz://wallet");

// Test match launch
DeepLinkHandler.SimulateDeepLink("deskillz://launch?matchId=test&token=test");

// Test room UI (NEW in v2.2)
PrivateRoomUI.Instance.ShowRoomList();
PrivateRoomUI.Instance.ShowCreateRoom();

// Test auto-updater (NEW in v2.3)
DeskillzUpdater.Instance.CheckForUpdates();
```

## Documentation

- [Quick Start Guide](https://docs.deskillz.games/unity/quickstart)
- [API Reference](https://docs.deskillz.games/unity/api)
- [Multiplayer Guide](https://docs.deskillz.games/unity/multiplayer)
- [Deep Link Integration](https://docs.deskillz.games/unity/deep-links)
- [Private Rooms Guide](https://docs.deskillz.games/unity/private-rooms)
- [Auto-Updater Guide](https://docs.deskillz.games/unity/updater)
- [Custom UI Guide](https://docs.deskillz.games/unity/custom-ui)
- [Troubleshooting](https://docs.deskillz.games/unity/troubleshooting)

## Sample Project

Check out our sample game implementation:
[Deskillz Unity Sample](https://github.com/deskillz/unity-sample)

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for version history.

### v2.3.0 (January 2025)
- **NEW:** Auto-Updater (`DeskillzUpdater`)
- **NEW:** Built-in update UI (`DeskillzUpdaterUI`)
- **NEW:** Forced vs optional update support
- **NEW:** Remember skipped versions
- **NEW:** Version comparison utilities
- **NEW:** Update events and callbacks
- APK hosting integration with Cloudflare R2

### v2.2.0 (December 2024)
- **NEW:** Private Rooms API (`DeskillzRooms`)
- **NEW:** Pre-built Room UI (6 components)
- **NEW:** Real-time WebSocket for rooms
- **NEW:** Room events (join, leave, ready, chat, countdown)
- Room list with search, filter, and sort
- Room lobby with player cards and ready status
- Host controls (start, cancel, kick)

### v2.1.0 (December 2024)
- Deep link improvements
- Bug fixes and stability

### v2.0.0 (December 2024)
- **NEW:** Navigation deep links (`OnNavigationReceived`)
- **NEW:** Simplified match launch (`OnMatchLaunchReceived`)
- **NEW:** `SimulateDeepLink()` for testing
- **NEW:** `NavigationAction` enum
- Improved deep link parsing
- Better error handling

## Troubleshooting

### Deep links not working
1. Verify URL schemes are configured correctly
2. Check app is properly signed
3. Test with: `adb shell am start -a android.intent.action.VIEW -d "deskillz://tournaments"`
4. Enable logging to see incoming deep links

### Navigation events not firing
1. Ensure `DeepLinkHandler.Initialize()` is called first
2. Verify event subscriptions before processing
3. Check `HasPendingDeepLink()` and call `ProcessPendingDeepLinks()`
4. Test with `SimulateDeepLink()` first

### Room UI not showing
1. Ensure `DeskillzRooms.Initialize()` is called
2. Check that UI prefabs are properly loaded
3. Verify WebSocket connection is established
4. Test with `PrivateRoomUI.Instance.ShowRoomList()`

### Auto-updater not checking
1. Verify `CurrentVersion` and `CurrentVersionCode` are set correctly
2. Check network connectivity
3. Ensure Game ID is configured in DeskillzConfig
4. Enable logging to see API responses
5. Test manually: `DeskillzUpdater.Instance.CheckForUpdates()`

### SDK Not Initializing
```csharp
// Check initialization status
if (!Deskillz.IsInitialized)
{
    // Verify credentials in DeskillzConfig
    // Check network connectivity
    // Enable logging for details
}
```

### iOS build errors
- Ensure Xcode 14+ is installed
- Check iOS deployment target is 12.0+
- Verify signing certificates

### Android build errors
- Check Min SDK is 21+
- Verify Gradle version compatibility
- Check for duplicate AndroidManifest entries

## Support

- **Email:** sdk@deskillz.games
- **Discord:** [discord.gg/deskillz](https://discord.gg/deskillz)
- **Documentation:** [docs.deskillz.games](https://docs.deskillz.games)
- **Developer Portal:** [deskillz.games/developer](https://deskillz.games/developer)

## License

Copyright © 2025 Deskillz.Games. All rights reserved.

---

<p align="center">
  Made with ❤️ by <a href="https://deskillz.games">Deskillz.Games</a>
</p>
# Deskillz Unity SDK

<p align="center">
  <img src="https://deskillz.games/logo.png" alt="Deskillz.Games" width="200"/>
</p>

<p align="center">
  <strong>Integrate competitive tournaments into your Unity games</strong>
</p>

<p align="center">
  <a href="https://github.com/Deskillz-Games-Development/unity-sdk/releases"><img src="https://img.shields.io/badge/version-2.6.0-blue.svg" alt="Version"></a>
  <a href="https://unity.com"><img src="https://img.shields.io/badge/unity-2020.3+-black.svg" alt="Unity"></a>
  <a href="https://github.com/Deskillz-Games-Development/unity-sdk/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License"></a>
</p>

<p align="center">
  <a href="#getting-your-credentials">Get Credentials</a> |
  <a href="#installation">Installation</a> |
  <a href="#quick-start">Quick Start</a> |
  <a href="#features">Features</a> |
  <a href="#auto-updater">Auto-Updater</a> |
  <a href="#private-rooms">Private Rooms</a> |
  <a href="#host-system">Host System</a> |
  <a href="#social-games">Social Games</a> |
  <a href="#spectator-mode">Spectator Mode</a> |
  <a href="#navigation-deep-links">Navigation Links</a> |
  <a href="#documentation">Documentation</a> |
  <a href="#support">Support</a>
</p>

---

## Overview

The Deskillz Unity SDK enables game developers to integrate their Unity games with the Deskillz.Games competitive gaming platform. Players can compete in skill-based tournaments, create private rooms to play with friends, host social games with rake systems, and win cryptocurrency prizes (BTC, ETH, SOL, XRP, BNB, USDT, USDC).

### How It Works (Global Lobby Architecture)

```
+------------------------------------------------------------------+
|                    PLAYER JOURNEY                                |
+------------------------------------------------------------------+
|                                                                  |
|  1. Player opens Deskillz.Games website/app                      |
|              |                                                   |
|  2. Player browses Global Lobby                                  |
|     - Select game                                                |
|     - Choose tournament/match type                               |
|     - Join matchmaking queue OR private room                     |
|              |                                                   |
|  3. Match found -> Deep link sent to your game                   |
|     deskillz://launch?matchId=abc123&token=xyz...                |
|              |                                                   |
|  4. Your game app opens via deep link                            |
|              |                                                   |
|  5. SDK receives match data -> Start gameplay                    |
|              |                                                   |
|  6. Player plays -> Score submitted -> Results shown             |
|                                                                  |
+------------------------------------------------------------------+
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

---

## Getting Your Credentials

**IMPORTANT: Start here before installation!**

The SDK requires a Game ID and API Key to initialize. With our **Credentials-First Flow**, you can get these instantly.

### Step 1: Access Developer Portal

1. Go to [deskillz.games/developer](https://deskillz.games/developer)
2. Connect your wallet or create an account
3. Click **"Register New Game"**

### Step 2: Generate Credentials Instantly

1. Enter your **Game Name** (e.g., "Block Puzzle Master")
2. Select your **Target Platform** (Android / iOS / Both)
3. Click **"Generate Game ID & API Key"**

### Step 3: You Receive Immediately

| Credential | Example | Purpose |
|------------|---------|---------|
| **Game ID** | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` | Unique identifier |
| **API Key** | `dsk_live_abc123def456ghi789...` | Public key for SDK |
| **API Secret** | `dss_xyz789abc456def123...` | Private key for HMAC signing |
| **Deep Link Scheme** | `deskillz-blockpuzzlemaster` | Custom URL scheme |

### CRITICAL: Save Your API Secret!

> **WARNING:** Your API Secret is displayed **only once**. Copy it immediately and store it securely:
> - Save it in a secure password manager
> - Never commit it to source control
> - You cannot retrieve it later - you would need to regenerate

### Step 4: Create DeskillzConfig Asset

1. In Unity: **Assets > Create > Deskillz > Config**
2. Or create a ScriptableObject:

```csharp
// Assets/Resources/DeskillzConfig.asset
[CreateAssetMenu(fileName = "DeskillzConfig", menuName = "Deskillz/Config")]
public class DeskillzConfig : ScriptableObject
{
    [Header("Credentials (from Developer Portal)")]
    public string GameId = "YOUR_GAME_ID";
    public string ApiKey = "YOUR_API_KEY";
    
    [Header("Security (keep secure!)")]
    public string ApiSecret = "YOUR_API_SECRET"; // For HMAC signing
    
    [Header("Settings")]
    public string DeepLinkScheme = "deskillz-yourgame";
    public DeskillzEnvironment Environment = DeskillzEnvironment.Sandbox;
}
```

**Add to .gitignore:**
```
# Deskillz credentials - do not commit!
Assets/Resources/DeskillzConfig.asset
```

### Step 5: Complete Registration (When Ready)

After verifying your SDK integration works, return to Developer Portal to:
1. Complete the full game submission form
2. Upload screenshots, icon, and video
3. Upload your APK/IPA build
4. Submit for review

---

## Installation

### Option 1: Unity Package Manager (Recommended)

1. Open **Window -> Package Manager**
2. Click **+ -> Add package from git URL**
3. Enter: `https://github.com/Deskillz-Games-Development/unity-sdk.git`

### Option 2: Download .unitypackage

1. Download latest release from [GitHub Releases](https://github.com/Deskillz-Games-Development/unity-sdk/releases)
2. Import via **Assets -> Import Package -> Custom Package**

### Option 3: Clone Repository

```bash
cd YourProject/Packages
git clone https://github.com/Deskillz-Games-Development/unity-sdk.git com.deskillz.sdk
```

### Setup

1. Create config: **Assets -> Create -> Deskillz -> Config**
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

## Features

| Feature | Description |
|---------|-------------|
| [TROPHY] **Tournaments** | Async and real-time competitive matches |
| [COIN] **Crypto Prizes** | BTC, ETH, SOL, XRP, BNB, USDT, USDC |
| [USERS] **Private Rooms** | Play with friends using room codes |
| [HOST] **Host System** | 6-tier host program with revenue sharing (NEW v2.6) |
| [CARDS] **Social Games** | Rake-based games with buy-ins (NEW v2.6) |
| [EYE] **Spectator Mode** | Watch live games in progress (NEW v2.6) |
| [LIGHTNING] **Real-time Sync** | Sub-100ms latency multiplayer |
| [SHIELD] **Anti-Cheat** | Score encryption and validation |
| [DOWNLOAD] **Auto-Updater** | Forced and optional app updates |
| [ROBOT] **NPC Opponents** | AI players for off-peak hours |
| [CHART] **Analytics** | Built-in telemetry and insights |
| [OFFLINE] **Offline Support** | Automatic score caching and retry |
| [LOCK] **Score Encryption** | HMAC-SHA256 signed submission |

---

## Host System (NEW in v2.6)

The Host System enables users to become verified hosts who can create and manage private rooms, earning revenue through the rake system.

### Host Tiers

| Tier | Name | Revenue Share | Requirements |
|------|------|---------------|--------------|
| 0 | Starter | 50% | None |
| 1 | Bronze | 55% | 10 rooms, $100 earned, 3.5 rating |
| 2 | Silver | 60% | 50 rooms, $500 earned, 4.0 rating |
| 3 | Gold | 65% | 150 rooms, $2,000 earned, 4.3 rating |
| 4 | Platinum | 70% | 500 rooms, $10,000 earned, 4.5 rating |
| 5 | Diamond | 75% | 1,000 rooms, $50,000 earned, 4.7 rating |

### Host Manager Usage

```csharp
using Deskillz.Host;

// Initialize Host Manager
HostManager.Instance.Initialize(userId);

// Register as a host
var request = new HostRegistrationRequest
{
    DisplayName = "ProHost",
    Email = "host@example.com",
    AcceptedTerms = true
};
HostManager.Instance.RegisterAsHost(request);

// Listen for events
HostManager.Instance.OnHostRegistrationComplete += (success, error) =>
{
    if (success) Debug.Log("Registered as host!");
};

HostManager.Instance.OnTierUpgraded += (oldTier, newTier) =>
{
    Debug.Log($"Congratulations! Upgraded from {oldTier} to {newTier}!");
};

// Create a room as host
var roomRequest = new CreateRoomRequest
{
    RoomName = "Pro Poker Night",
    GameId = "poker-texas-holdem",
    MaxPlayers = 8,
    MinBuyIn = 10f,
    MaxBuyIn = 200f,
    PointValue = 0.01f,
    RakePercent = 5f,
    RakeCap = 3f
};
HostManager.Instance.CreateRoom(roomRequest);

// Check earnings
HostManager.Instance.FetchEarnings(EarningsPeriod.Month);
HostManager.Instance.OnEarningsUpdated += (earnings) =>
{
    Debug.Log($"Total: ${earnings.TotalEarnings}");
    Debug.Log($"Rake: ${earnings.RakeEarnings}");
    Debug.Log($"Available: ${earnings.AvailableBalance}");
};
```

---

## Social Games (NEW in v2.6)

Social games enable real-money gameplay with rake collection, buy-ins, rebuys, and cash-outs.

### Social Game Manager Usage

```csharp
using Deskillz.Social;

// Configure session
var config = new SocialSessionConfig
{
    RoomId = "room-123",
    GameId = "poker-texas-holdem",
    HostId = "host-456",
    PointValue = 0.01f,
    RakePercent = 5f,
    RakeCap = 3f,
    MinBuyIn = 10f,
    MaxBuyIn = 200f,
    AllowRebuy = true,
    RebuyPeriodRounds = 5
};

// Start session
SocialGameManager.Instance.StartSession(config);

// Add player with buy-in
SocialGameManager.Instance.AddPlayer(playerId, buyInAmount);

// Handle rebuys
SocialGameManager.Instance.OnRebuyRequested += (playerId) =>
{
    // Show rebuy modal
    RebuyModal.Show(playerId, config.MinBuyIn, config.MaxBuyIn);
};

// Process round results
SocialGameManager.Instance.StartRound();
// ... gameplay ...
SocialGameManager.Instance.EndRound(winnerId, potAmount);

// Cash out
SocialGameManager.Instance.ProcessCashOut(playerId);
```

### Rake Calculator

```csharp
using Deskillz.Social;

// Get rake breakdown
var breakdown = RakeCalculator.Instance.GetRakeBreakdown(potAmount);
Debug.Log($"Pot: ${breakdown.PotAmount}");
Debug.Log($"Rake: ${breakdown.TotalRake}");
Debug.Log($"Winner Payout: ${breakdown.WinnerPayout}");
Debug.Log($"Host Share: ${breakdown.HostShare}");
Debug.Log($"Platform Share: ${breakdown.PlatformShare}");

// Estimate earnings
float hostEarnings = RakeCalculator.Instance.EstimateHostEarnings(
    expectedPotSize: 100f,
    expectedHands: 50,
    hostTier: 3
);
```

### Buy-In Manager

```csharp
using Deskillz.Social;

// Validate buy-in
var validation = BuyInManager.Instance.GetValidationResult(
    playerId, 
    amount, 
    isRebuy: false
);

if (validation.IsValid)
{
    Debug.Log($"Chips: {validation.ChipsReceived}");
    BuyInManager.Instance.ProcessBuyIn(playerId, amount);
}
else
{
    Debug.LogError(validation.ErrorMessage);
}

// Get preset amounts
float[] presets = BuyInManager.Instance.GetPresetBuyInAmounts();
// Returns: [MinBuyIn, 100x, 200x, MaxBuyIn]
```

---

## Spectator Mode (NEW in v2.6)

Allow users to watch live games in progress without participating.

### Spectator Manager Usage

```csharp
using Deskillz.Spectator;

// Fetch available rooms to spectate
SpectatorManager.Instance.FetchSpectatorRooms(new SpectatorRoomFilter
{
    GameId = "poker-texas-holdem",
    MinPlayers = 2,
    ActiveOnly = true
});

SpectatorManager.Instance.OnRoomsFetched += (rooms) =>
{
    foreach (var room in rooms)
    {
        Debug.Log($"{room.RoomName}: {room.CurrentPlayers}/{room.MaxPlayers} - Pot: ${room.CurrentPot}");
    }
};

// Join as spectator
SpectatorManager.Instance.JoinAsSpectator(roomId);

// View controls
SpectatorManager.Instance.SetViewMode(SpectatorViewMode.FollowPlayer);
SpectatorManager.Instance.FollowPlayer(playerId);
SpectatorManager.Instance.CycleToNextPlayer();

// Playback controls
SpectatorManager.Instance.SetPlaybackSpeed(2.0f);
SpectatorManager.Instance.PausePlayback();
SpectatorManager.Instance.ResumePlayback();

// Listen for game events
SpectatorManager.Instance.OnRoundStarted += (roundNumber) => { };
SpectatorManager.Instance.OnPlayerAction += (playerId, action, value) => { };
SpectatorManager.Instance.OnPotUpdated += (potAmount) => { };
SpectatorManager.Instance.OnRoundEnded += (roundNumber, winnerId) => { };
```

---

## Private Rooms

Create custom rooms for friends or public tournaments.

### Room Creation

```csharp
using Deskillz.Rooms;

// Create a private room
var roomConfig = new RoomConfig
{
    Name = "Friday Night Tournament",
    GameId = "your-game-id",
    MaxPlayers = 8,
    EntryFee = 5.0f,
    Currency = "USDT",
    IsPrivate = true
};

DeskillzRooms.CreateRoom(roomConfig, (room) =>
{
    Debug.Log($"Room created! Code: {room.RoomCode}");
    // Share room.RoomCode with friends
});

// Join via code
DeskillzRooms.JoinRoomByCode("ABC123", OnRoomJoined);

// Browse public rooms
DeskillzRooms.GetPublicRooms(gameId, OnRoomsLoaded);
```

### Room Events

```csharp
// Subscribe to room events
DeskillzRooms.OnPlayerJoined += (player) => 
{
    Debug.Log($"{player.Name} joined the room");
};

DeskillzRooms.OnPlayerLeft += (playerId) => { };
DeskillzRooms.OnPlayerReady += (playerId) => { };
DeskillzRooms.OnCountdownStarted += (seconds) => { };
DeskillzRooms.OnMatchStarting += (matchData) => { };
DeskillzRooms.OnChatMessage += (playerId, message) => { };

// Host controls
DeskillzRooms.StartMatch();  // Host only
DeskillzRooms.CancelRoom();  // Host only
DeskillzRooms.KickPlayer(playerId);  // Host only
```

### Pre-built Room UI

```csharp
using Deskillz.UI.Rooms;

// Show room browser
PrivateRoomUI.Instance.ShowRoomList();

// Show create room form
PrivateRoomUI.Instance.ShowCreateRoom();

// Show join by code dialog
PrivateRoomUI.Instance.ShowJoinRoom();

// Customize theme
PrivateRoomUI.Instance.SetTheme(new RoomUITheme
{
    PrimaryColor = Color.blue,
    BackgroundColor = new Color(0.1f, 0.1f, 0.15f),
    FontSize = 16
});
```

---

## Auto-Updater

Automatically check for and prompt app updates.

### Basic Setup

```csharp
using Deskillz;

void Start()
{
    // Set current version
    DeskillzUpdater.Instance.CurrentVersion = Application.version;
    DeskillzUpdater.Instance.CurrentVersionCode = GetVersionCode();
    
    // Subscribe to events
    DeskillzUpdater.OnUpdateAvailable += HandleOptionalUpdate;
    DeskillzUpdater.OnForcedUpdateRequired += HandleForcedUpdate;
    DeskillzUpdater.OnNoUpdateNeeded += () => Debug.Log("Up to date!");
    
    // Check for updates
    DeskillzUpdater.Instance.CheckForUpdates();
}

void HandleOptionalUpdate(UpdateInfo info)
{
    Debug.Log($"Update available: {info.LatestVersion}");
    Debug.Log($"Size: {info.FileSizeFormatted}");
    // Show optional update dialog
}

void HandleForcedUpdate(UpdateInfo info)
{
    // Block app until user updates
    DeskillzUpdaterUI.Instance.ShowForcedUpdateDialog(info);
}
```

### Built-in Update UI

```csharp
// Automatic UI handling
DeskillzUpdaterUI.Instance.ShowOnUpdateAvailable = true;
DeskillzUpdaterUI.Instance.BlockOnForcedUpdate = true;
DeskillzUpdater.Instance.CheckForUpdates();
```

---

## Navigation Deep Links

Handle platform navigation requests:

| URL | Action | Description |
|-----|--------|-------------|
| `deskillz://tournaments` | Tournaments | Show tournament list |
| `deskillz://wallet` | Wallet | Show wallet/balance |
| `deskillz://profile` | Profile | Show user profile |
| `deskillz://settings` | Settings | Show settings |
| `deskillz://game?id=X` | Game | Show specific game |
| `deskillz://leaderboard?id=X` | Leaderboard | Show leaderboard |

---

## SDK Structure

```
deskillz-unity-sdk/
+-- Runtime/
|   +-- Core/
|   |   +-- Deskillz.cs
|   |   +-- DeskillzConfig.cs
|   |   +-- DeskillzManager.cs
|   |   +-- DeskillzEvents.cs
|   |   +-- DeskillzModels.cs
|   |   +-- DeskillzNetwork.cs
|   |   +-- DeskillzUpdater.cs
|   |   +-- DeskillzUpdaterUI.cs
|   +-- Match/
|   |   +-- MatchController.cs
|   |   +-- MatchTimer.cs
|   |   +-- MatchStateMachine.cs
|   +-- Security/
|   |   +-- ScoreManager.cs
|   |   +-- ScoreEncryption.cs
|   |   +-- ScoreValidator.cs
|   +-- Rooms/
|   |   +-- DeskillzRooms.cs
|   |   +-- RoomModels.cs
|   |   +-- RoomApiClient.cs
|   |   +-- RoomWebSocket.cs
|   +-- Host/                              # NEW in v2.6
|   |   +-- HostManager.cs                 # Host registration, tiers, rooms
|   |   +-- HostModels.cs                  # Host data structures
|   |   +-- HostApiClient.cs               # Host API integration
|   |   +-- HostEvents.cs                  # Host event delegates
|   +-- Social/                            # NEW in v2.6
|   |   +-- SocialGameManager.cs           # Session management
|   |   +-- RakeCalculator.cs              # Rake calculation
|   |   +-- BuyInManager.cs                # Buy-in/rebuy/cashout
|   |   +-- SocialModels.cs                # Social game data structures
|   |   +-- SocialEvents.cs                # Social game events
|   +-- Spectator/                         # NEW in v2.6
|   |   +-- SpectatorManager.cs            # Spectator mode management
|   |   +-- SpectatorModels.cs             # Spectator data structures
|   |   +-- SpectatorEvents.cs             # Spectator events
|   +-- UI/
|   |   +-- DeskillzUIManager.cs
|   |   +-- UIPanel.cs
|   |   +-- Rooms/
|   |   |   +-- PrivateRoomUI.cs
|   |   |   +-- RoomListUI.cs
|   |   |   +-- CreateRoomUI.cs
|   |   |   +-- JoinRoomUI.cs
|   |   |   +-- RoomLobbyUI.cs
|   |   |   +-- RoomPlayerCard.cs
|   |   +-- Host/                          # NEW in v2.6
|   |   |   +-- HostDashboardUI.cs         # Main host dashboard
|   |   |   +-- HostProfileCard.cs         # Host profile display
|   |   |   +-- HostTierProgress.cs        # Tier progression UI
|   |   |   +-- HostBadgeGrid.cs           # Achievement badges
|   |   |   +-- HostEarningsChart.cs       # Earnings visualization
|   |   +-- Social/                        # NEW in v2.6
|   |   |   +-- BuyInModal.cs              # Buy-in dialog
|   |   |   +-- RebuyModal.cs              # Rebuy dialog with timer
|   |   |   +-- CashOutModal.cs            # Cash out confirmation
|   |   |   +-- SocialGameSettings.cs      # Game configuration UI
|   |   |   +-- TurnTimer.cs               # Turn countdown display
|   |   |   +-- PauseRequestUI.cs          # Pause voting system
|   |   +-- Spectator/                     # NEW in v2.6
|   |   |   +-- SpectatorView.cs           # Main spectator interface
|   |   |   +-- SpectatorScorePanel.cs     # Live score display
|   |   |   +-- RoomSwitcher.cs            # Room navigation
|   +-- Lobby/
|   |   +-- DeepLinkHandler.cs
|   |   +-- DeskillzBridge.cs
|   |   +-- DeskillzLobbyClient.cs
|   +-- Multiplayer/
|   |   +-- SyncManager.cs
|   +-- NPC/
|       +-- NPCManager.cs
+-- Editor/
|   +-- DeskillzEditor.cs
+-- package.json
+-- README.md
```

---

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

In Unity, add to **Player Settings -> iOS -> Other Settings -> Supported URL Schemes**.

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

// Test navigation deep links
DeepLinkHandler.SimulateDeepLink("deskillz://tournaments");
DeepLinkHandler.SimulateDeepLink("deskillz://wallet");

// Test match launch
DeepLinkHandler.SimulateDeepLink("deskillz://launch?matchId=test&token=test");

// Test room UI
PrivateRoomUI.Instance.ShowRoomList();
PrivateRoomUI.Instance.ShowCreateRoom();

// Test auto-updater
DeskillzUpdater.Instance.CheckForUpdates();

// Test host system (NEW in v2.6)
HostManager.Instance.Initialize("test-host-id");
HostDashboardUI.Instance.Show();

// Test social games (NEW in v2.6)
SocialGameManager.Instance.StartTestSession();
BuyInModal.Instance.Show(10f, 200f);

// Test spectator mode (NEW in v2.6)
SpectatorManager.Instance.FetchSpectatorRooms(new SpectatorRoomFilter());
SpectatorView.Instance.Show();
```

## Documentation

- [Quick Start Guide](https://docs.deskillz.games/unity/quickstart)
- [API Reference](https://docs.deskillz.games/unity/api)
- [Multiplayer Guide](https://docs.deskillz.games/unity/multiplayer)
- [Deep Link Integration](https://docs.deskillz.games/unity/deep-links)
- [Private Rooms Guide](https://docs.deskillz.games/unity/private-rooms)
- [Host System Guide](https://docs.deskillz.games/unity/host-system)
- [Social Games Guide](https://docs.deskillz.games/unity/social-games)
- [Spectator Mode Guide](https://docs.deskillz.games/unity/spectator)
- [Auto-Updater Guide](https://docs.deskillz.games/unity/updater)
- [Custom UI Guide](https://docs.deskillz.games/unity/custom-ui)
- [Troubleshooting](https://docs.deskillz.games/unity/troubleshooting)

## Sample Project

Check out our sample game implementation:
[Deskillz Unity Sample](https://github.com/Deskillz-Games-Development/unity-sample)

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for version history.

### v2.6.0 (January 2025)
- **NEW:** Host System with 6-tier progression
- **NEW:** HostManager for host registration and management
- **NEW:** Host Dashboard UI components (5 files)
- **NEW:** Social Game Manager for rake-based games
- **NEW:** RakeCalculator with tiered rake structure
- **NEW:** BuyInManager for buy-in/rebuy/cashout flows
- **NEW:** Social Game UI components (6 files)
- **NEW:** SpectatorManager for live game viewing
- **NEW:** Spectator UI components (3 files)
- **NEW:** 26 total new files for Private Room Enhancement
- Revenue sharing system (50%-75% based on tier)
- Real-time WebSocket updates for spectators
- Pause/resume functionality for social games

### v2.5.1 (January 2025)
- Fixed duplicate class definitions
- Fixed duplicate struct definitions
- README URL corrections

### v2.5.0 (January 2025)
- Enhanced README documentation
- SDK testing procedures
- APK hosting improvements

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

### Host system not initializing
1. Ensure user is authenticated first
2. Call `HostManager.Instance.Initialize(userId)`
3. Check for registration errors in callbacks
4. Verify API connectivity

### Social game session errors
1. Validate session config before starting
2. Check buy-in amounts are within range
3. Ensure all players have sufficient balance
4. Monitor WebSocket connection status

### Spectator mode not connecting
1. Verify room exists and is active
2. Check spectating is enabled for the room
3. Ensure WebSocket connection is established
4. Monitor for connection timeout errors

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
- **GitHub Issues:** [github.com/Deskillz-Games-Development/unity-sdk/issues](https://github.com/Deskillz-Games-Development/unity-sdk/issues)

## License

Copyright (c) 2025 Deskillz.Games. All rights reserved.

MIT License - see [LICENSE](https://github.com/Deskillz-Games-Development/unity-sdk/blob/main/LICENSE) for details.

---

<p align="center">
  Made with love by <a href="https://deskillz.games">Deskillz.Games</a>
</p>
# Deskillz Unity SDK

<p align="center">
  <img src="https://deskillz.games/logo.png" alt="Deskillz.Games" width="200"/>
</p>

<p align="center">
  <strong>Integrate competitive tournaments into your Unity games</strong>
</p>

<p align="center">
  <a href="#installation">Installation</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#features">Features</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#support">Support</a>
</p>

---

## Overview

The Deskillz Unity SDK enables game developers to integrate their Unity games with the Deskillz.Games competitive gaming platform. Players can compete in skill-based tournaments and win cryptocurrency prizes.

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
│     deskillz://match?id=abc123&token=xyz...                     │
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
1. Handle deep links
2. Start the match
3. Submit scores securely

## Requirements

- **Unity:** 2021.3 LTS or newer
- **Platforms:** iOS 12+, Android API 21+
- **Dependencies:** Newtonsoft.Json

## Installation

### Option 1: Unity Package Manager (Recommended)

1. Open **Window → Package Manager**
2. Click **+ → Add package from git URL**
3. Enter: `https://github.com/Deskillz-Games-Development/unity-sdk.git`

### Option 2: Manual Installation

1. Download the [latest release](https://github.com/Deskillz-Games-Development/unity-sdk/releases/latest)
2. Extract to your project's `Assets/Plugins/Deskillz/` folder

## Quick Start

### 1. Initialize the SDK

```csharp
using Deskillz;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Initialize SDK
        DeskillzManager.Initialize(new DeskillzConfig
        {
            GameId = "your-game-id",
            ApiKey = "your-api-key",
            Environment = DeskillzEnvironment.Sandbox
        });
        
        // Register for deep link events (THIS IS THE KEY INTEGRATION)
        DeskillzEvents.OnDeepLinkReceived += HandleDeepLink;
        DeskillzEvents.OnMatchReady += StartMatch;
    }
}
```

### 2. Handle Deep Links (Primary Integration Point)

```csharp
// Called when player launches your game from the Deskillz Global Lobby
private void HandleDeepLink(DeepLinkData data)
{
    Debug.Log($"Match received: {data.MatchId}");
    
    // SDK automatically parses the deep link and prepares match data
    // The OnMatchReady event will fire when everything is ready
}

private void StartMatch(MatchData match)
{
    // Match data received from the Deskillz platform
    Debug.Log($"Starting match: {match.Id}");
    Debug.Log($"Opponent: {match.Opponent.Username}");
    Debug.Log($"Entry Fee: {match.EntryFee}");
    Debug.Log($"Game Mode: {match.GameMode}"); // SYNC or ASYNC
    
    // Start your gameplay
    GameController.Instance.StartMatch(match);
}
```

### 3. Submit Score

```csharp
// When game ends - score is automatically signed with HMAC-SHA256
public void OnGameComplete(int finalScore)
{
    DeskillzManager.Instance.SubmitScore(new ScoreSubmission
    {
        Score = finalScore,
        MatchId = currentMatch.Id,
        Metadata = new Dictionary<string, object>
        {
            { "level", currentLevel },
            { "timeElapsed", gameTime },
            { "accuracy", hitAccuracy }
        }
    });
}
```

### 4. Handle Results

```csharp
DeskillzEvents.OnMatchComplete += (result) =>
{
    if (result.IsWinner)
    {
        ShowVictoryScreen(result.PrizeAmount, result.CryptoType);
    }
    else
    {
        ShowResultsScreen(result.FinalRanking, result.WinnerScore);
    }
    
    // Return player to Deskillz app
    DeskillzManager.Instance.ReturnToLobby();
};
```

## Architecture: Global Lobby vs Old SDK-Based

| Aspect | Old (SDK-Based) | New (Global Lobby) ✅ |
|--------|-----------------|----------------------|
| Matchmaking Location | Inside your game | Deskillz website/app |
| Player Pool | Fragmented per-game | Unified across all games |
| User Experience | Inconsistent | Consistent platform UI |
| SDK Complexity | High (matchmaking logic) | Low (deep links only) |
| Developer Burden | Heavy | Minimal |
| Private Rooms | Complex to implement | Built into platform |
| NPC Opponents | SDK handles | Platform handles |

## Features

| Feature | Description |
|---------|-------------|
| **Deep Link Handling** | Receive match data from Global Lobby |
| **Score Encryption** | HMAC-SHA256 signed score submission |
| **Anti-Cheat** | Built-in validation and monitoring |
| **Match HUD** | Optional in-game UI components |
| **Results Display** | Show match outcomes |
| **Analytics** | Track player engagement metrics |
| **Sync/Async Support** | Both game modes supported |

## SDK Structure

```
Runtime/
├── Core/           # Initialization, deep link handling
├── Match/          # Match lifecycle, state machine
├── Security/       # Score encryption, validation
├── UI/             # Optional HUD, results screens
└── Analytics/      # Telemetry, event tracking

Editor/
└── Build/          # iOS/Android build automation
```

## Deep Link Format

Your game will receive deep links in this format:

```
deskillz://match?
  id=<match_id>&
  token=<auth_token>&
  game=<game_id>&
  mode=<SYNC|ASYNC>&
  opponent=<opponent_id>&
  entry_fee=<amount>&
  currency=<BTC|ETH|USDT|etc>
```

The SDK parses this automatically - you just handle the `OnMatchReady` event.

## iOS Setup

Add URL scheme to `Info.plist`:

```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>deskillz</string>
      <string>deskillz-yourgameid</string>
    </array>
  </dict>
</array>
```

## Android Setup

Add intent filter to `AndroidManifest.xml`:

```xml
<intent-filter>
  <action android:name="android.intent.action.VIEW" />
  <category android:name="android.intent.category.DEFAULT" />
  <category android:name="android.intent.category.BROWSABLE" />
  <data android:scheme="deskillz" />
  <data android:scheme="deskillz-yourgameid" />
</intent-filter>
```

## Documentation

- [Quick Start Guide](./Documentation~/QUICKSTART.md)
- [API Reference](./Documentation~/API_REFERENCE.md)
- [Integration Guide](./Documentation~/INTEGRATION_GUIDE.md)
- [Online Docs](https://docs.deskillz.games/unity)

## Sample Project

Check out our sample game implementation:
[Deskillz Unity Sample](https://github.com/Deskillz-Games-Development/unity-sample)

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for version history.

## Support

- **Documentation:** [docs.deskillz.games](https://docs.deskillz.games)
- **Developer Portal:** [deskillz.games/developer](https://deskillz.games/developer)
- **Email:** developers@deskillz.games
- **Discord:** [Join our community](https://discord.gg/deskillz)

## License

This SDK is licensed under the MIT License. See [LICENSE](./LICENSE) for details.

---

<p align="center">
  Made with ❤️ by <a href="https://deskillz.games">Deskillz.Games</a>
</p>
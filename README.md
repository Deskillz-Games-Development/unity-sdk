# Deskillz Unity SDK

**Version:** 3.5.2 | **Unity:** 2020.3+ | **License:** MIT

---

## Overview

The Deskillz Unity SDK integrates your Unity game with the Deskillz competitive gaming platform. Players compete in skill-based tournaments, create private rooms, host social games with rake systems, and win cryptocurrency prizes (BNB, USDT, USDC on BSC and TRON networks).

**Architecture:** Self-sufficient -- everything happens inside your game app. Players authenticate, browse tournaments, join matches, and collect prizes without leaving your game.

```
YOUR GAME APP
  1. Player opens your game
  2. Login (email/social/wallet)
  3. Browse tournaments / Quick Play / Private Rooms
  4. Join match -> Play -> Submit score
  5. See results -> Collect winnings
```

---

## Requirements

- Unity 2020.3 or later
- .NET Standard 2.1 or .NET Framework 4.x
- iOS 13+ / Android 7.0+ (API 24+)
- Deskillz Developer account ([deskillz.games/developer](https://deskillz.games/developer))

---

## Installation

### Unity Package Manager (Recommended)

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.deskillz.sdk": "https://github.com/Deskillz-Games/unity-sdk.git"
  }
}
```

### Manual

1. Download the latest release from [GitHub Releases](https://github.com/Deskillz-Games/unity-sdk/releases)
2. Import the `.unitypackage` into your project

### Setup

1. Go to **Assets > Create > Deskillz > Config** to create a `DeskillzConfig` asset
2. Enter your **Game ID** and **API Key** from the Developer Portal
3. Set your **Environment** (Development / Sandbox / Production)
4. Add `DeskillzManager` to your first scene (or let it auto-create)

---

## SDK Structure

```
Runtime/
  Core/
    DeskillzManager.cs          - Singleton SDK manager (entry point)
    DeskillzAuth.cs             - Authentication (email, social, wallet)
    DeskillzConfig.cs           - Configuration ScriptableObject
    DeskillzEnums.cs            - All enumerations
    DeskillzModels.cs           - Data models (auth, match, wallet, room, etc.)
    DeskillzLobbyModels.cs      - Lobby + tournament models
    DeskillzNetwork.cs          - HTTP + WebSocket networking
    DeskillzSessionManager.cs   - SSO tokens, session resume, guest mode [NEW]
    GameCapabilitiesManager.cs  - Game capabilities config [NEW]
    RealtimeEventRouter.cs      - Socket event dispatcher [NEW]
    DeskillzEvents.cs           - Event system
    DeskillzCache.cs            - Local caching
    DeskillzLogger.cs           - Debug logging
    AuthSceneController.cs      - Scene flow for auth
    DeskillzUpdater.cs          - Auto-updater
  Tournaments/                  [NEW]
    TournamentManager.cs        - Tournament registration, check-in, bracket
    TournamentApiClient.cs      - Tournament HTTP client
  QuickPlay/                    [NEW]
    QuickPlayManager.cs         - Instant matchmaking + social Quick Play
    QuickPlayApiClient.cs       - Quick Play HTTP client
  Disputes/                     [NEW]
    DisputeManager.cs           - File disputes, add evidence
    DisputeApiClient.cs         - Dispute HTTP client
  Wallet/                       [NEW]
    WalletManager.cs            - Multi-currency wallet, deposit, withdraw
  Rooms/
    DeskillzRooms.cs            - Room create, join, leave, ready, start
    RoomExtensions.cs           - Buy-in, cash-out, invites, settlement [NEW]
    RoomModels.cs               - Room data models
    RoomWebSocket.cs            - Room real-time events
  Host/
    HostManager.cs              - Host profile, earnings, badges, tiers
    HostDashboardExtensions.cs  - Composite dashboard, withdrawals [NEW]
    HostApiClient.cs            - Host HTTP client
  Match/
    MatchController.cs          - Match lifecycle
    ScoreManager.cs             - Score submission
  Score/
    ScoreEncryption.cs          - HMAC-SHA256 score signing
    ScoreValidator.cs           - Anti-cheat validation
  Spectator/
    HostSpectatorManager.cs     - Spectator mode
  Multiplayer/
    SyncManager.cs              - Real-time state sync
  UI/                           - Pre-built UI components
```

---

## Quick Start

### 1. Initialize

```csharp
using Deskillz;

public class GameBootstrap : MonoBehaviour
{
    void Start()
    {
        // SDK auto-initializes if AutoInitialize is checked in DeskillzConfig.
        // Otherwise:
        DeskillzManager.Instance.Initialize();

        // Listen for ready
        DeskillzEvents.OnReady += () => Debug.Log("SDK Ready!");
        DeskillzEvents.OnMatchReady += OnMatchReady;
    }

    void OnMatchReady(MatchData match)
    {
        Debug.Log($"Match ready: {match.MatchId}, Mode: {match.Mode}");
        // Load your game scene
        SceneManager.LoadScene("GameScene");
    }
}
```

### 2. Authentication

```csharp
using Deskillz;

// Email login
DeskillzManager.Instance.Login("player@email.com", "password123");

// Social login
DeskillzManager.Instance.SocialLogin("google", idToken);

// Wallet login (crypto-native users)
DeskillzManager.Instance.LinkWallet(walletAddress, signature, message, nonce);

// Listen for auth events
DeskillzAuth.OnLoginSuccess += user => Debug.Log($"Logged in: {user.Username}");
DeskillzAuth.OnLogout += () => Debug.Log("Logged out");
```

### 3. Submit Score

```csharp
using Deskillz;

// When gameplay ends:
ScoreManager.SubmitScore(playerScore,
    result => Debug.Log($"Score submitted! Outcome: {result.Outcome}"),
    error => Debug.LogError($"Score failed: {error.Message}")
);
```

---

## Tournaments

Full tournament lifecycle: browse, register, check-in, play, view bracket.

```csharp
using Deskillz.Tournaments;

// Browse tournaments
TournamentManager.GetTournaments(
    tournaments => {
        foreach (var t in tournaments)
            Debug.Log($"{t.Name} - {t.EntryFee} {t.Currency} - {t.CurrentPlayers}/{t.MaxPlayers}");
    },
    error => Debug.LogError(error.Message)
);

// Register for a tournament
TournamentManager.Register("tournament-id",
    reg => Debug.Log($"Registered! Status: {reg.Status}"),
    error => Debug.LogError(error.Message)
);

// Check in (during check-in window, T-30 to T-10 min before start)
TournamentManager.CheckIn("tournament-id",
    reg => Debug.Log("Checked in!"),
    error => Debug.LogError(error.Message)
);

// Leave / unregister (refunds entry fee)
TournamentManager.Leave("tournament-id",
    () => Debug.Log("Left tournament"),
    error => Debug.LogError(error.Message)
);

// Get enrollment status (drives UI button state)
TournamentManager.GetEnrollmentStatus("tournament-id",
    state => Debug.Log($"Status: {state.Status}, CanCheckIn: {state.CanCheckIn}"),
    error => Debug.LogError(error.Message)
);

// Get bracket schedule
TournamentManager.GetSchedule("tournament-id",
    schedule => Debug.Log($"Round {schedule.CurrentRound}/{schedule.TotalRounds}"),
    error => Debug.LogError(error.Message)
);

// Get my table assignment
TournamentManager.GetMyTableAssignment("tournament-id",
    seat => Debug.Log($"Table {seat.TableNumber}, Seat {seat.SeatNumber}"),
    error => Debug.LogError(error.Message)
);

// Events
TournamentEvents.OnRegistered += reg => Debug.Log("Registered!");
TournamentEvents.OnCheckedIn += reg => Debug.Log("Checked in!");
TournamentEvents.OnLeft += id => Debug.Log($"Left {id}");
TournamentEvents.OnMatchLaunch += data => Debug.Log($"Match launching: {data.MatchId}");
```

---

## Quick Play

Instant matchmaking for esport games + social cash game rooms.

### Esport Quick Play

```csharp
using Deskillz.QuickPlay;

// Join matchmaking queue
QuickPlayManager.JoinQueue(
    new QuickPlayJoinParams {
        GameId = "your-game-id",
        EntryFee = 1.00m,
        Currency = "USDT_BSC"
    },
    result => Debug.Log($"In queue, position: {result.Position}"),
    error => Debug.LogError(error.Message)
);

// Listen for match found
QuickPlayEvents.OnMatchFound += data => {
    // Launch the match
    QuickPlayManager.LaunchMatch(data.MatchSessionId,
        launchData => Debug.Log($"Match launched: {launchData.MatchId}"),
        error => Debug.LogError(error.Message)
    );
};

// Submit score when gameplay ends
QuickPlayManager.SubmitScore("match-id", 1500,
    result => Debug.Log($"Score submitted, rank: {result.Rank}"),
    error => Debug.LogError(error.Message)
);

// Get final results
QuickPlayManager.GetMatchResults("match-id",
    results => {
        foreach (var p in results.Players)
            Debug.Log($"{p.Username}: {p.Score} - Prize: {p.PrizeWon}");
    },
    error => Debug.LogError(error.Message)
);

// Leave queue
QuickPlayManager.LeaveQueue(() => Debug.Log("Left queue"), error => {});

// Events
QuickPlayEvents.OnSearching += result => Debug.Log("Searching...");
QuickPlayEvents.OnMatchLaunched += data => Debug.Log("Match started!");
QuickPlayEvents.OnMatchCompleted += result => Debug.Log("Match done!");
QuickPlayEvents.OnQueueTimeout += () => Debug.Log("No match found");
```

### Social Quick Play (Cash Games)

```csharp
using Deskillz.QuickPlay;

// Create a social room
QuickPlayManager.CreateSocialRoom(
    new CreateSocialQuickPlayParams {
        PointValueUsd = 0.10m,
        Currency = "USDT_BSC",
        SeatsPerTable = 4
    },
    result => Debug.Log($"Room created: {result.RoomCode}"),
    error => Debug.LogError(error.Message)
);

// Submit round results
QuickPlayManager.SubmitSocialRound("room-id",
    new SocialRoundPayload {
        RoundNumber = 1,
        PlayerResults = new List<SocialRoundPlayerResult> {
            new() { PlayerId = "p1", Score = 100, PointsWon = 30 },
            new() { PlayerId = "p2", Score = 80, PointsWon = -10 },
        }
    },
    () => Debug.Log("Round submitted"),
    error => Debug.LogError(error.Message)
);

// Rebuy / Cash out / End
QuickPlayManager.SocialRebuy("room-id", 50m, balance => Debug.Log($"Balance: {balance}"), err => {});
QuickPlayManager.SocialCashOut("room-id", amount => Debug.Log($"Cashed out: {amount}"), err => {});
QuickPlayManager.EndSocialGame("room-id", () => Debug.Log("Game ended"), err => {});
```

---

## Private Rooms

Create rooms, invite friends, manage players.

### Basic Room Operations

```csharp
using Deskillz.Rooms;

// Create a room
DeskillzRooms.CreateRoom(new CreateRoomConfig {
    Name = "My Room",
    MaxPlayers = 4,
    EntryFee = 5m,
    EntryCurrency = "USDT_BSC"
},
    room => Debug.Log($"Room created: {room.RoomCode}"),
    error => Debug.LogError(error.Message)
);

// Join by code
DeskillzRooms.JoinRoom("DSKZ-AB3C",
    room => Debug.Log($"Joined: {room.Name}"),
    error => Debug.LogError(error.Message)
);

// Ready up / Start
DeskillzRooms.SetReady(true);
DeskillzRooms.StartMatch();
```

### Extended Room Operations (v3.5.2)

```csharp
using Deskillz.Rooms;

// Create with host role
RoomExtensions.CreateEsportRoom(new CreateEsportRoomOpts {
    Name = "Tournament Room",
    EntryFee = 10m,
    Currency = "USDT_BSC",
    HostRole = HostRole.SPECTATOR,  // Host watches, doesn't play
    MaxPlayers = 8,
    MatchMode = EsportMatchMode.BEST_OF_3
}, onSuccess, onError);

RoomExtensions.CreateSocialRoom(new CreateSocialRoomOpts {
    Name = "Mahjong Night",
    SocialGameType = "MAHJONG",
    TableStakes = 1m,
    Currency = "USDT_BSC",
    HostRole = HostRole.PLAYER,
    WinCondition = SocialWinCondition.FIXED_ROUNDS,
    WinConditionTarget = 8
}, onSuccess, onError);

// Financial operations
RoomExtensions.BuyIn("room-id", 100m, "USDT_BSC", balance => {}, err => {});
RoomExtensions.CashOut("room-id", amount => {}, err => {});
RoomExtensions.Rebuy("room-id", 50m, "USDT_BSC", balance => {}, err => {});
RoomExtensions.SubmitRound("room-id", 1, playerResults, () => {}, err => {});
RoomExtensions.TriggerSettlement("room-id", () => {}, err => {});

// Invites
RoomExtensions.InvitePlayer("room-id", "friendUsername", "Come play!", () => {}, err => {});
RoomExtensions.GetMyInvites(invites => {
    foreach (var inv in invites)
        Debug.Log($"Invite from {inv.SenderUsername} to {inv.RoomName}");
}, err => {});
RoomExtensions.RespondToInvite("invite-id", true, () => {}, err => {});

// Events
RoomExtensionEvents.OnInviteReceived += invite => Debug.Log($"Invite from {invite.SenderUsername}!");
RoomExtensionEvents.OnBuyInComplete += balance => Debug.Log($"Chips: {balance}");
```

---

## Wallet

Multi-currency wallet with deposit, withdrawal, and transaction history.

```csharp
using Deskillz.Wallet;

// Get all balances
WalletManager.GetBalance(
    balances => {
        foreach (var b in balances)
            Debug.Log($"{b.Symbol}: {b.Amount} (${b.UsdValue})");
    },
    error => Debug.LogError(error.Message)
);

// Get specific currency balance
WalletManager.GetBalanceForCurrency("USDT_BSC", balance => {}, err => {});

// Deposit
WalletManager.Deposit("USDT_BSC", 100m, result => {
    Debug.Log($"Deposit address: {result.depositAddress}");
}, err => {});

// Withdraw
WalletManager.Withdraw("USDT_BSC", 50m, "0xYourWallet...", result => {
    Debug.Log($"Withdrawal submitted: {result.TransactionId}");
}, err => {});

// Transaction history
WalletManager.GetTransactions(txns => {
    foreach (var tx in txns)
        Debug.Log($"{tx.Type}: {tx.Amount} {tx.Currency} - {tx.Status}");
}, err => {});

// Match history
WalletManager.GetMatchHistory(matches => {
    foreach (var m in matches)
        Debug.Log($"{m.GameName}: Score {m.Score}, Prize {m.PrizeWon}");
}, err => {});

// Leaderboard
WalletManager.GetGameLeaderboard("game-id", entries => {
    foreach (var e in entries)
        Debug.Log($"#{e.Rank} {e.DisplayName}: {e.Score}");
}, err => {});
```

### Currency Formatting

```csharp
using Deskillz;

// Display helpers
string label = CurrencyLabels.GetLabel(Currency.USDT_BSC);     // "USDT (BSC)"
string symbol = CurrencyLabels.GetSymbol(Currency.USDT_TRON);  // "USDT"
string network = CurrencyLabels.GetNetwork(Currency.USDC_BSC);  // "BSC"
string formatted = CurrencyLabels.Format(5.50m, Currency.BNB);  // "5.50 BNB (BSC)"
```

**Supported currencies:** BNB, USDT (BSC), USDT (TRON), USDC (BSC), USDC (TRON)

---

## Host System

Players become hosts, create rooms, and earn revenue from platform fees and rake.

```csharp
using Deskillz.Host;

// Get full dashboard (single API call)
HostDashboardExtensions.GetDashboard(
    dashboard => {
        Debug.Log($"Level: {dashboard.Profile.Level}");
        Debug.Log($"Earnings: {dashboard.Earnings.TotalEarnings}");
        Debug.Log($"Active rooms: {dashboard.ActiveRooms.Count}");
    },
    error => Debug.LogError(error.Message)
);

// Get profile
HostManager.GetProfile(profile => {
    Debug.Log($"Tier: {profile.EsportsTier}, Share: {profile.GetEsportsRevenueShare()}%");
}, err => {});

// Withdraw earnings
HostDashboardExtensions.RequestWithdrawal(100m, "USDT_BSC", "0xWallet...",
    result => Debug.Log($"Withdrawal: {result.TransactionId}"),
    error => Debug.LogError(error.Message)
);

// Age verification (required for hosting social games with rake)
HostManager.VerifyAge(true, profile => Debug.Log("Age verified!"), err => {});
HostDashboardExtensions.CheckAgeVerified(
    status => Debug.Log($"Verified: {status.IsVerified}"),
    err => {}
);
```

### Host Tiers and Revenue Share

| Tier | Players Required | Revenue Share |
|------|-----------------|---------------|
| Bronze | 0 | 15% |
| Silver | 50 | 18% |
| Gold | 250 | 20% |
| Platinum | 1,000 | 23% |
| Diamond | 5,000 | 25% |
| Elite | 10,000 | 28% |

---

## Disputes

File disputes against matches with evidence.

```csharp
using Deskillz.Disputes;

// File a dispute
DisputeManager.FileDispute(
    new FileDisputeParams {
        DisputeType = "TOURNAMENT",
        MatchId = "match-id",
        Reason = "Cheating",
        Description = "Opponent used auto-clicker software",
        Evidence = new List<string> { "screenshot_url_1", "screenshot_url_2" }
    },
    dispute => Debug.Log($"Dispute filed: {dispute.Id}"),
    error => Debug.LogError(error.Message)
);

// View my disputes
DisputeManager.GetMyDisputes(disputes => {
    foreach (var d in disputes)
        Debug.Log($"{d.Id}: {d.Status} - {d.Reason}");
}, err => {});

// Auto-suggested last match (stored locally for 7 days)
var lastMatch = DisputeManager.GetLastMatch();
if (lastMatch != null)
    Debug.Log($"Last match: {lastMatch.MatchId} vs {lastMatch.OpponentName}");
```

---

## Game Capabilities

Query what your game supports (configured in Developer Portal).

```csharp
using Deskillz;

// Fetch capabilities (falls back to defaults before API responds)
GameCapabilitiesManager.GetCapabilities(caps => {
    Debug.Log($"1v1: {caps.Supports1v1}, FFA: {caps.SupportsFFA}");
    Debug.Log($"Max tournament: {caps.MaxTournamentSize}");
    Debug.Log($"Match duration: {caps.MinMatchDurationSeconds}-{caps.MaxMatchDurationSeconds}s");
});

// Access cached capabilities anytime
var current = GameCapabilitiesManager.Current;
if (current.SupportsSync)
    Debug.Log("Real-time multiplayer supported");
```

---

## Session Management

SSO token handoff, active session resume, and guest mode.

```csharp
using Deskillz;

// SSO token is consumed automatically on launch.
// Subscribe to the event if you need to react:
DeskillzSessionManager.OnSSOAuthenticated += user =>
    Debug.Log($"SSO login: {user.Username}");

// Check for active session (room reconnect after app restart)
DeskillzSessionManager.CheckForActiveSession(session => {
    if (session.HasActiveSession)
        Debug.Log($"Resuming {session.Type} room: {session.RoomCode}");
});

// Guest mode (browse-only, no paid features)
DeskillzSessionManager.EnableGuestMode();
bool canJoin = DeskillzSessionManager.CanPerformAction("joinTournament"); // false
```

---

## Events Reference

All events are static and can be subscribed to at any time.

### Core Events (DeskillzEvents)
- `OnReady` -- SDK initialized
- `OnMatchReady(MatchData)` -- match received via deep link
- `OnMatchStart(MatchData)` -- match gameplay begins
- `OnMatchComplete(MatchResult)` -- match finished
- `OnPlayerAuthenticated(PlayerData)` -- user logged in
- `OnPlayerUpdated(PlayerData)` -- profile changed
- `OnError(DeskillzError)` -- SDK error

### Tournament Events (TournamentEvents)
- `OnRegistered(TournamentRegistration)`, `OnCheckedIn`, `OnLeft(string)`, `OnTournamentStarted(string)`, `OnMatchLaunch(MatchLaunchPayload)`

### Quick Play Events (QuickPlayEvents)
- `OnSearching`, `OnMatchFound`, `OnMatchLaunched`, `OnScoreSubmitted`, `OnMatchCompleted`, `OnQueueTimeout`, `OnLeft`

### Room Events (DeskillzRooms)
- `OnRoomJoined`, `OnRoomUpdated`, `OnPlayerJoined`, `OnPlayerLeft`, `OnPlayerReadyChanged`, `OnAllPlayersReady`, `OnCountdownStarted`, `OnMatchLaunching`, `OnRoomCancelled`

### Room Extension Events (RoomExtensionEvents)
- `OnInviteReceived`, `OnBuyInComplete`, `OnCashOutComplete`, `OnSettlementTriggered`

### Session Events (DeskillzSessionManager)
- `OnSSOAuthenticated`, `OnSessionResumed`, `OnGuestModeActivated`

---

## Configuration

Create a `DeskillzConfig` asset via **Assets > Create > Deskillz > Config**.

| Field | Description | Default |
|-------|-------------|---------|
| API Key | Your API key from Developer Portal | (required) |
| Game ID | Your Game ID from Developer Portal | (required) |
| Environment | Production / Sandbox / Development | Sandbox |
| Score Type | HigherIsBetter / LowerIsBetter | HigherIsBetter |
| Test Mode | Enable test mode (no real money) | false |
| Auto Initialize | Initialize SDK on Awake | true |
| Log Level | None / Error / Warning / Info / Debug | Info |
| Require Auth | Require login before gameplay | true |
| Allow Guest Mode | Allow browsing without login | false |
| Auth Scene | Scene name for login/signup UI | "Auth" |
| Lobby Scene | Scene name for tournament lobby | "Lobby" |
| Game Scene | Scene name for gameplay | "Game" |

---

## Test Mode

Enable in DeskillzConfig or via code:

```csharp
// Start a test match (no entry fee, no real money)
DeskillzManager.Instance.StartTestMatch(MatchMode.Asynchronous, timeLimitSeconds: 120);
```

Test mode uses local test data and does not connect to production servers.

---

## Platform Setup

### Android

Add to `AndroidManifest.xml` for deep link support:

```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW"/>
    <category android:name="android.intent.category.DEFAULT"/>
    <category android:name="android.intent.category.BROWSABLE"/>
    <data android:scheme="deskillz" android:host="launch"/>
</intent-filter>
```

### iOS

Add to `Info.plist`:

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>deskillz</string>
        </array>
    </dict>
</array>
```

---

## Troubleshooting

**SDK not initializing:** Check that `DeskillzConfig` asset exists and API Key / Game ID are set. Check Console for error logs.

**Deep links not working:** Verify `AndroidManifest.xml` / `Info.plist` has the `deskillz://` URL scheme registered.

**Auth not working:** Ensure `RequireAuth` is enabled in config. Check that the backend API URL is reachable from the device.

**Room events not firing:** Confirm WebSocket URL is correct for your environment. Check `DeskillzLogger` output at Debug level.

**Score submission failing:** Verify `ScoreType` in config matches your game (HigherIsBetter vs LowerIsBetter). Check that match is in `InProgress` status before submitting.

**Wallet balance showing 0:** User must have a connected wallet with funds deposited. Guest mode users have no wallet access.

---

## Support

- Developer Portal: [deskillz.games/developer](https://deskillz.games/developer)
- Documentation: [docs.deskillz.games/unity](https://docs.deskillz.games/unity)
- Email: sdk@deskillz.games
- Discord: [discord.gg/deskillz](https://discord.gg/deskillz)

---

## License

MIT License. See [LICENSE](LICENSE) for details.
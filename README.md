# Deskillz Unity SDK

**Version:** 3.7.7 | **Unity:** 6000.0+ (Unity 6) | **License:** MIT

Turn a Unity game into a Deskillz skill-gaming title: paid tournaments, quick play,
private rooms, host-earns social tables (Big 2, Thirteen, Mahjong) and crypto prizes
(BNB, USDT, USDC on BNB Chain and Tron). Everything runs inside your game app.

## Install

1. Unity Hub > Unity 6 (6000.x). Android Build Support for phones; the Web module for
   browser builds.
2. Package Manager > **+** > *Install package from disk* > this package's `package.json`.
   Unity resolves `com.unity.ugui 2.0.0` and `com.unity.nuget.newtonsoft-json 3.2.1`.
3. Menu **Deskillz > Create Config Asset**, then fill in **API Key** and **Game ID** from
   [deskillz.games/developer](https://deskillz.games/developer). Leave *Environment* on
   Production; *API Host Override* is only for self-hosted backends.

## Ten-line integration

```csharp
using Deskillz;

void Start()
{
    Deskillz.Deskillz.Initialize();
    DeskillzEvents.OnMatchReady += m => Deskillz.Deskillz.StartMatch();
}

void GameOver(int score)
{
    Deskillz.Deskillz.SubmitScore(score);   // signed, server-verified
    Deskillz.Deskillz.EndMatch();
}
```

Package Manager > Deskillz SDK > *Samples* > **QuickStart** imports the same thing as a
ready MonoBehaviour. Tick **Test Mode** on the config asset to run without the platform.

## How a match starts

Players never type anything. The Deskillz site (or your in-app lobby) issues a launch
link -- `deskillz-<gameId>://match?id=<matchId>&token=<launchToken>&tournament=...` on
mobile, `?matchId=&token=` on the web -- and the SDK:

1. exchanges the single-use launch token (`POST /api/v1/auth/launch/exchange`) for an
   access token and a per-launch score secret,
2. loads the player (`GET /api/v1/users/me`),
3. raises `DeskillzEvents.OnMatchReady(MatchData)`,
4. for synchronous games joins the Socket.IO match room so `Multiplayer.SetLocalState`
   / `SendMessage` reach the other seats.

`match:launch` and `quick-play:match-launched` socket events do the same automatically
when the player is already in the app.

## Scores

`Deskillz.SubmitScore(int)` posts `{ matchId, score, timestamp, signature }` to
`POST /api/v1/tournaments/:id/score`. The signature is HMAC-SHA256 over
`gameId:matchId:score:timestamp:userId` with the launch's score secret; the server
verifies it. Checkpoints (`ScoreManager`) stay local; only the final score is sent.

## In-app lobby (self-sufficient apps)

| Need | Call |
|---|---|
| Sign in | `DeskillzAuth.Login(email, password)` / `SignUp(...)` |
| Tournaments | `TournamentManager.GetActiveTournaments`, `Register`, `CheckIn`, `GetMyLaunch` |
| Quick play | `QuickPlayManager.Join(gameId)` -> `quick-play:found` -> launch |
| Private rooms | `RoomApiClient.CreateRoom`, `JoinRoom(code)`, `SetReady`, `StartMatch` |
| Social tables | `RoomApiClient.CreateSocialRoom(new CreateSocialRoomConfig { GameType = "BIG2", ... })` |
| Wallet | `WalletManager.GetBalance`, `GetDepositAddress("bsc")`, `OpenAddFunds()`, `OpenWithdraw()` (money moves on deskillz.games) |
| Disputes | `DisputeManager.File(...)` |

All REST calls use `/api/v1/...` on the platform origin
`https://api.deskillz.games`.

## Realtime

Socket.IO v4 on the same origin, two namespaces: `/` (match relay) and `/lobby`
(rooms, quick play). Relayable game events: `game:state`, `game:hand`, `game:action`,
`game:round-end`, `game:game-end`, `game:request-sync`, `game:chat`,
`game:round-ready`, `game:round-ready-state`, `game:ping`, `game:pong`.
Raw access: `DeskillzEvents.OnSocketMessage(eventName, json)`; send with
`DeskillzManager.Instance.Network.Relay("game:action", payload)`.
A private hand goes to its player only: `Deskillz.Multiplayer.RelayTo(playerId,
"game:hand", hand)` (`match:relay_to`). At a table with a host, `game:state`,
`game:round-end`, `game:game-end` and `game:round-ready-state` come from the
table host only. The server adds `senderUserId` to every relayed payload; trust
it, not a sender field. Board seeds come from `Deskillz.GetMatchSeed`.

Native platforms use `System.Net.WebSockets`; Web builds use the browser socket through
`Runtime/Plugins/WebGL/DeskillzWebSocket.jslib`. A Unity **Web** build can be uploaded
to Deskillz Cloud Build and wrapped as PWA / Android / Windows like any web game.

## Building

* **Android**: File > Build Profiles > Android. Add the deep-link intent filter for
  `deskillz-<gameId>://` (Player Settings > Publishing, or the manifest template).
* **iOS**: build the Xcode project on a Mac; add the URL scheme `deskillz-<gameId>`.
* **Web**: File > Build Profiles > Web; upload the build folder to Cloud Build.

## Package layout

```
Runtime/Core        Deskillz facade, manager, config, events, auth, JSON, launch, realtime
Runtime/Tournaments QuickPlay Rooms Wallet Disputes Host Multiplayer Match Score
Runtime/Legacy~     older built-in UI panels (not compiled; rework scheduled)
Editor              Deskillz menu
Samples~/QuickStart DeskillzQuickStart.cs
```

## Changelog

See CHANGELOG.md.

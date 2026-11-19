# Changelog

## 3.7.7 -- 2026-11-18

Same platform contract as Web SDK 3.7.7 (guide v6.2). The version jumps from
3.7.1 to match the web SDK.

### Removed
- `WalletManager.Deposit` (`POST /wallet/deposit` no longer exists) and
  `WalletManager.Withdraw` (a game session cannot withdraw). Money moves only on
  deskillz.games: call `WalletManager.OpenAddFunds()` and
  `WalletManager.OpenWithdraw()` from your wallet screen.
- Host earnings withdraw (`HostManager.RequestWithdrawal`,
  `HostApiClient.RequestWithdrawal`, `HostDashboardExtensions.WithdrawAllEarnings`
  and `RequestWithdrawal`, the `HostEvents.OnWithdrawal*` events): `POST
  /host/withdraw` no longer exists; earnings are part of the Deskillz balance.
- `Currency.USDC_TRON`. Entries are in `USDT_BSC`, `USDC_BSC` and `USDT_TRON`;
  `BNB` is a legacy balance only.
- `DeskillzSessionManager.ConsumeSSOToken` and `OnSSOAuthenticated`. The
  `?token=` of a launch link is a single-use launch token; `DeskillzManager`
  already exchanges it (`POST /auth/launch/exchange`).

### Added
- `WalletManager.GetDepositAddress("bsc" | "tron")`: the player's own deposit
  address and the tokens credited there (`GET /wallet/deposit-address/:chain`).
- `Deskillz.GetMatchSeed(onSeed, onError)`: the board seed of a live match
  (`GET /matches/:matchId/seed`, seated players only).
- `Deskillz.Multiplayer.RelayTo(playerId, gameEvent, payload)` and
  `DeskillzNetwork.RelayTo`: one event to one seat (`match:relay_to`), for
  private hands (`game:hand`).
- `DeskillzNetwork.SendProgress(score)` (`match:progress`); another seat's
  progress raises `OnPlayerScoreUpdated` and `OnOpponentScoreUpdated`.

### Changed
- `Deskillz.Multiplayer.SendToPlayer` goes to that player only
  (`match:relay_to`); it used to reach every seat with a `targetId`.
- A received message's `SenderId` is the server's `senderUserId`, never a
  field the sender wrote.
- `Deskillz.UpdateScore` shows the live score with `match:progress` instead
  of `game:state` (a table with a host accepts `game:state` from the host only).
- `SDK_VERSION` is 3.7.7 (sent as `X-SDK-Version`).

## 3.7.1 -- 2026-09-17

### Changed
- Default API origin is `https://api.deskillz.games` (REST under `/api/v1`,
  Socket.IO on the same host). `DeskillzConfig.ApiHostOverride` still overrides it.
- `SDK_VERSION` is 3.7.1 (sent as `X-SDK-Version`).

### Fixed
- `SyncManager` no longer falls back to a host that does not exist when no config
  asset is loaded.

## 3.7.0 -- 2026-09-15

First release that compiles and talks to the live platform.

### Platform contract
- API origin is the platform host (`newdeskillzgames-production.up.railway.app`); all
  routes under `/api/v1`. The sandbox host was removed; `DeskillzConfig.ApiHostOverride`
  covers self-hosted backends.
- Launch links carry a single-use token: `POST /auth/launch/exchange` -> access token +
  per-launch score secret (`LaunchSession`, `DeepLinkParams`).
- Scores are HMAC-SHA256 signed and posted to `POST /tournaments/:id/score`; the dead
  `/sdk/*` routes and the AES score encryption are gone.
- `GET /tournaments/:id/my-launch` (live + lobby payloads), `POST /private-rooms/social`.
- Corrected paths: wallet balances, leaderboard, private room list/request, host
  dashboard/tier/badges/notifications, version-check (`X-Package-Name`).
- Tournament and room models carry `gameplayMode`, `gameRuleVariant`, `mahjongVariant`,
  `turnTimerSeconds`, `esportMatchMode`, `hostRole` and the rest of the current fields.

### Realtime
- In-package Socket.IO v4 client (`Deskillz.Realtime.SocketIOClient`): namespaces `/`
  and `/lobby`, acks, ping/pong, backoff reconnect with fresh-token auth.
- Native transport on `ClientWebSocket`; Web transport via `DeskillzWebSocket.jslib`.
- Match relay: `match:join_room`, `match:relay` with the platform's relayable event set.
- Event router listens to the backend's real event names and auto-runs launches.

### Package
- Unity 6 (6000.0) minimum; dependencies `com.unity.ugui 2.0.0`,
  `com.unity.nuget.newtonsoft-json 3.2.1`; Newtonsoft replaces `JsonUtility` throughout.
- Built-in UI panels, Stage and Spectator moved to `Runtime/Legacy~` (not compiled).
- `Deskillz > Create Config Asset` menu; QuickStart sample.

### Fixed
- 3.5.2 did not compile (undefined types, unclosed methods, duplicate definitions,
  placeholder WebSocket clients, response envelope that the backend never sent).

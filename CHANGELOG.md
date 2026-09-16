# Changelog

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

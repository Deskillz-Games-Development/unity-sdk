# Legacy modules (not compiled)

Unity ignores folders whose name ends with `~`. These modules were written against
older SDK models and do not compile against 3.7.0:

- `UI/`        built-in panels (rooms, host dashboard, results, HUD, social modals)
- `Stage/`     custom stages (replaced by Private Rooms on the platform)
- `Spectator/` host spectator view

They are kept for the Patch 2 UI rework. Nothing in the play-loop core depends on
them: Deskillz.Initialize / launch links / tournaments / quick play / private rooms /
wallet / disputes / realtime all live in Runtime/Core, Realtime, Launch, Score,
Tournaments, QuickPlay, Rooms, Wallet, Disputes, Multiplayer, Match, Host.

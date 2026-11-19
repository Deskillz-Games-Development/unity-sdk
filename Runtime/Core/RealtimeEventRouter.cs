// =============================================================================
// Deskillz SDK for Unity - Realtime Event Router
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 10)
// =============================================================================
//
// Central router that listens to Socket.IO events from DeskillzNetwork
// and dispatches them to the appropriate managers:
//   - TournamentManager (tournament:starting/left, match:launch)
//   - QuickPlayManager (quick-play:found/timeout/match-completed)
//   - RoomExtensions (room:invite)
//
// Registered by DeskillzManager during initialization.
// Games should NOT call this directly -- subscribe to events on the
// individual managers instead.
//
// Path: Runtime/Core/RealtimeEventRouter.cs
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Routes realtime socket events to the appropriate managers.
    /// </summary>
    public static class RealtimeEventRouter
    {
        private static bool _isRegistered;

        // =====================================================================
        // REGISTRATION
        // =====================================================================

        /// <summary>
        /// Register socket event listeners. Called once during SDK init.
        /// </summary>
        internal static void Register()
        {
            if (_isRegistered) return;

            // Subscribe to DeskillzEvents for socket messages
            // The existing DeskillzNetwork/RoomWebSocket fires these
            DeskillzEvents.OnSocketMessage += HandleSocketMessage;

            _isRegistered = true;
            DeskillzLogger.Debug("[RealtimeRouter] Registered socket event handlers");
        }

        /// <summary>
        /// Unregister on shutdown.
        /// </summary>
        internal static void Unregister()
        {
            DeskillzEvents.OnSocketMessage -= HandleSocketMessage;
            _isRegistered = false;
        }

        // =====================================================================
        // SOCKET MESSAGE DISPATCHER
        // =====================================================================

        /// <summary>
        /// Central handler for all socket messages.
        /// Routes to the correct manager based on event type.
        /// </summary>
        private static void HandleSocketMessage(string eventType, string jsonData)
        {
            // [P2-3.7.0] event names are the backend's (events.gateway, lobby.gateway,
            // quick-play-*.service, tournament-*.service). Payloads bind case-insensitively.
            try
            {
                switch (eventType)
                {
                    // ---------------- tournaments ----------------
                    case "tournament:registered":
                    case "tournament:checkin-open":
                    case "tournament:checked-in":
                        DeskillzLogger.Info($"[RealtimeRouter] {eventType}");
                        break;
                    case "tournament:starting":
                        Tournaments.TournamentManager.HandleTournamentStarted(DeskillzJson.DeserializeRaw<TournamentIdPayload>(jsonData)?.tournamentId);
                        break;
                    case "tournament:left":
                    case "tournament:dq-noshow":
                        Tournaments.TournamentManager.HandleTournamentLeft(DeskillzJson.DeserializeRaw<TournamentIdPayload>(jsonData)?.tournamentId);
                        break;

                    // ---------------- launches ----------------
                    case "match:launch":
                    case "quick-play:match-launched":
                        var launch = DeskillzJson.DeserializeRaw<Tournaments.MatchLaunchPayload>(jsonData);
                        if (launch != null)
                        {
                            Tournaments.TournamentManager.HandleMatchLaunch(launch);
                            if (!string.IsNullOrEmpty(launch.DeepLink)) DeskillzManager.Instance?.LaunchFromLink(launch.DeepLink);
                        }
                        break;

                    // ---------------- quick play ----------------
                    case "quick-play:searching":
                    case "quick-play:npc-filling":
                    case "quick-play:lobby-update":
                    case "quick-play:queue-roster":
                    case "quick-play:score-submitted":
                        DeskillzLogger.Verbose($"[RealtimeRouter] {eventType}");
                        break;
                    case "quick-play:found":
                    case "quick-play:starting":
                        QuickPlay.QuickPlayManager.HandleMatchFound(DeskillzJson.DeserializeRaw<QuickPlay.QuickPlayFoundData>(jsonData));
                        break;
                    case "quick-play:timeout":
                        QuickPlay.QuickPlayManager.HandleQueueTimeout();
                        break;
                    case "quick-play:match-completed":
                        QuickPlay.QuickPlayManager.HandleMatchCompleted(DeskillzJson.DeserializeRaw<QuickPlayMatchResult>(jsonData));
                        break;
                    case "quick-play:social-room-created":
                    case "quick-play:social-round-complete":
                    case "quick-play:social-rebuy-required":
                    case "quick-play:social-session-ended":
                        DeskillzLogger.Info($"[RealtimeRouter] {eventType}");
                        break;

                    // ---------------- match room (events.gateway "/") ----------------
                    case "match:found":
                    case "match:accepted":
                    case "match:declined":
                    case "match:ready":
                    case "match:ready_update":
                    case "match:all_ready":
                    case "match:player_joined":
                    case "match:player_left":
                    case "match:state_change":
                    case "match:cancelled":
                        DeskillzLogger.Verbose($"[RealtimeRouter] {eventType}");
                        break;

                    case "match:progress": // [N790-3.7.7] another seat's display-only live score
                        var progress = DeskillzJson.DeserializeRaw<ProgressPayload>(jsonData);
                        if (progress != null && !string.IsNullOrEmpty(progress.odid) && progress.score.HasValue)
                        {
                            var live = (int)Math.Round(progress.score.Value);
                            DeskillzEvents.RaisePlayerScoreUpdated(progress.odid, live);
                            DeskillzEvents.RaiseOpponentScoreUpdated(progress.odid, live);
                        }
                        break;

                    // ---------------- private rooms (lobby.gateway "/lobby") ----------------
                    case "room:invite":
                    case "PRIVATE_ROOM_INVITE":
                        Rooms.RoomExtensions.HandleInviteReceived(DeskillzJson.DeserializeRaw<RoomInvite>(jsonData));
                        break;
                    case "room:state":
                    case "room:table-assigned":
                    case "room:start_success":
                    case "private-room:player-ready":
                    case "private-room:all-ready":
                    case "private-room:player-kicked":
                    case "private-room:player-disconnected":
                    case "private-room:kicked":
                    case "private-room:cancelled":
                    case "private-room:chat":
                        // handled by RoomWebSocket on the /lobby namespace
                        break;

                    default:
                        DeskillzLogger.Verbose($"[RealtimeRouter] Unhandled event: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[RealtimeRouter] Error handling {eventType}: {ex.Message}");
            }
        }

        // =====================================================================
        // INTERNAL PAYLOAD MODELS
        // =====================================================================

        [Serializable]
        private class TournamentIdPayload
        {
            public string tournamentId;
        }

        /// <summary>match:progress { odid, username, score, board, at } [N790-3.7.7]</summary>
        [Serializable]
        private class ProgressPayload
        {
            public string odid;
            public double? score;
        }
    }
}
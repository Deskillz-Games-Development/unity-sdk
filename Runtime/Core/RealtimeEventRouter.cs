// =============================================================================
// Deskillz SDK for Unity - Realtime Event Router
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 10)
// =============================================================================
//
// Central router that listens to Socket.IO events from DeskillzNetwork
// and dispatches them to the appropriate managers:
//   - TournamentManager (tournament:started, tournament:left, match:launch)
//   - QuickPlayManager (queue:matched, queue:timeout, match:completed)
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
            try
            {
                switch (eventType)
                {
                    // ==========================================================
                    // TOURNAMENT EVENTS (Phase 3 / 10.3)
                    // ==========================================================

                    case "tournament:started":
                        var tsData = JsonUtility.FromJson<TournamentIdPayload>(jsonData);
                        Tournaments.TournamentManager.HandleTournamentStarted(tsData?.tournamentId);
                        break;

                    case "tournament:left":
                        var tlData = JsonUtility.FromJson<TournamentIdPayload>(jsonData);
                        Tournaments.TournamentManager.HandleTournamentLeft(tlData?.tournamentId);
                        break;

                    case "match:launch":
                        var mlData = JsonUtility.FromJson<Tournaments.MatchLaunchPayload>(jsonData);
                        Tournaments.TournamentManager.HandleMatchLaunch(mlData);
                        break;

                    // ==========================================================
                    // QUICK PLAY EVENTS (Phase 6 / 10.4)
                    // ==========================================================

                    case "queue:matched":
                    case "quick-play:matched":
                        var qmData = JsonUtility.FromJson<QuickPlay.QuickPlayFoundData>(jsonData);
                        QuickPlay.QuickPlayManager.HandleMatchFound(qmData);
                        break;

                    case "queue:timeout":
                    case "quick-play:timeout":
                        QuickPlay.QuickPlayManager.HandleQueueTimeout();
                        break;

                    case "quickplay:match:completed":
                    case "quick-play:match:completed":
                        var qcData = JsonUtility.FromJson<QuickPlayMatchResult>(jsonData);
                        QuickPlay.QuickPlayManager.HandleMatchCompleted(qcData);
                        break;

                    // ==========================================================
                    // ROOM INVITE EVENTS (Phase 5 / 10.5)
                    // ==========================================================

                    case "room:invite":
                    case "PRIVATE_ROOM_INVITE":
                        var invite = JsonUtility.FromJson<RoomInvite>(jsonData);
                        Rooms.RoomExtensions.HandleInviteReceived(invite);
                        break;

                    // ==========================================================
                    // ROOM EVENTS (Phase 10 / 10.2) -- forwarded to DeskillzRooms
                    // These are already handled by RoomWebSocket, but we log them
                    // ==========================================================

                    case "room:player:joined":
                    case "room:player:left":
                    case "room:player:ready":
                    case "room:countdown":
                    case "room:launching":
                    case "room:cancelled":
                    case "room:chat":
                        // Already handled by RoomWebSocket -- no action needed
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
    }
}
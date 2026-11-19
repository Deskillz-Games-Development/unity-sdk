// =============================================================================
// Deskillz SDK for Unity - Session Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 1)
// =============================================================================
//
// Handles active session resume and guest mode. [N790-3.7.7] The ?token= of a
// launch link is a single-use launch token, exchanged by DeskillzManager
// (LaunchSession.Exchange); the old SSO path that used it as a sign-in token
// is gone.
// Mirrors DeskillzBridge.ts lines 951-1184.
//
// Called automatically by DeskillzManager during initialization.
// Games can also call these methods directly if needed.
//
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz
{
    /// <summary>
    /// Manages session resume and guest mode.
    /// </summary>
    public static class DeskillzSessionManager
    {
        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Whether the current session is a guest session</summary>
        public static bool IsGuest { get; private set; }

        /// <summary>Active session payload from last check</summary>
        public static ActiveSessionPayload ActiveSession { get; private set; }

        /// <summary>Whether an active session exists</summary>
        public static bool HasActiveSession => ActiveSession?.HasActiveSession ?? false;

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Fired when an active session is found (room reconnect)</summary>
        public static event Action<ActiveSessionPayload> OnSessionResumed;

        /// <summary>Fired when guest mode is activated</summary>
        public static event Action OnGuestModeActivated;

        // =====================================================================
        // ACTIVE SESSION RESUME (1.2, 1.3)
        // =====================================================================

        /// <summary>
        /// Check for an in-progress room/tournament/quickplay session.
        /// If found, emits OnSessionResumed event.
        /// Called automatically after authentication.
        /// </summary>
        public static void CheckForActiveSession(
            Action<ActiveSessionPayload> onResult = null)
        {
            if (IsGuest || DeskillzManager.Instance?.CurrentPlayer == null)
            {
                var empty = new ActiveSessionPayload { HasActiveSession = false };
                ActiveSession = empty;
                onResult?.Invoke(empty);
                return;
            }

            DeskillzManager.Instance.StartCoroutine(CheckForActiveSessionAsync(onResult));
        }

        private static IEnumerator CheckForActiveSessionAsync(Action<ActiveSessionPayload> onResult)
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? DeskillzConfig.PLATFORM_API_HOST;
            var url = $"{baseUrl}/api/v1/private-rooms/my-active";

            using var request = UnityWebRequest.Get(url);
            request.timeout = 10;

            var token = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseText = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(responseText) && responseText != "null")
                    {
                        var session = DeskillzJson.DeserializeRaw<ActiveSessionPayload>(responseText);
                        if (session != null && !string.IsNullOrEmpty(session.RoomId))
                        {
                            session.HasActiveSession = true;
                            ActiveSession = session;
                            DeskillzLogger.Info($"[SessionManager] Active session found: {session.Type} room={session.RoomCode}");
                            OnSessionResumed?.Invoke(session);
                            onResult?.Invoke(session);
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Warning($"[SessionManager] Active session parse error: {ex.Message}");
                }
            }

            var empty = new ActiveSessionPayload { HasActiveSession = false };
            ActiveSession = empty;
            onResult?.Invoke(empty);
        }

        /// <summary>
        /// Get the current active session (cached from last check).
        /// Returns null if no active session.
        /// </summary>
        public static ActiveSessionPayload GetActiveSession()
        {
            return HasActiveSession ? ActiveSession : null;
        }

        // =====================================================================
        // GUEST MODE (1.7)
        // =====================================================================

        /// <summary>
        /// Enable guest mode. Guest users can browse but cannot enter
        /// tournaments, Quick Play, or private rooms with entry fees.
        /// All API methods that require authentication return empty/default values.
        /// </summary>
        public static void EnableGuestMode()
        {
            IsGuest = true;
            DeskillzLogger.Info("[SessionManager] Guest mode enabled");
            OnGuestModeActivated?.Invoke();
        }

        /// <summary>
        /// Disable guest mode (after user logs in).
        /// </summary>
        public static void DisableGuestMode()
        {
            IsGuest = false;
            DeskillzLogger.Info("[SessionManager] Guest mode disabled");
        }

        /// <summary>
        /// Check if an action is allowed in current mode.
        /// Returns false for guest users attempting premium actions.
        /// </summary>
        public static bool CanPerformAction(string actionName)
        {
            if (!IsGuest) return true;

            DeskillzLogger.Warning($"[SessionManager] Action '{actionName}' not available in guest mode");
            return false;
        }

        // =====================================================================
        // RESET
        // =====================================================================

        /// <summary>
        /// Reset session state on logout.
        /// </summary>
        internal static void Reset()
        {
            IsGuest = false;
            ActiveSession = null;
        }
    }
}
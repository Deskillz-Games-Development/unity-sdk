// =============================================================================
// Deskillz SDK for Unity - Tournament Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 3)
// =============================================================================
//
// Public API for tournament operations. Mirrors DeskillzBridge.ts tournament
// methods (lines 1826-1945, 2355).
//
// Usage:
//   TournamentManager.GetTournaments(
//       tournaments => Debug.Log($"Found {tournaments.Count} tournaments"),
//       error => Debug.LogError(error.Message)
//   );
//
//   TournamentManager.Register("tournament-id",
//       reg => Debug.Log($"Registered: {reg.Status}"),
//       error => Debug.LogError(error.Message)
//   );
//
//   TournamentEvents.OnRegistered += reg => Debug.Log("Registered!");
//   TournamentEvents.OnCheckedIn += reg => Debug.Log("Checked in!");
//   TournamentEvents.OnLeft += id => Debug.Log($"Left {id}");
//   TournamentEvents.OnMatchLaunch += data => Debug.Log("Match launching!");
//
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Tournaments
{
    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>
    /// Tournament lifecycle events.
    /// Subscribe in your game to react to tournament state changes.
    /// </summary>
    public static class TournamentEvents
    {
        /// <summary>Fired when the player registers for a tournament</summary>
        public static event Action<TournamentRegistration> OnRegistered;

        /// <summary>Fired when the player checks in to a tournament</summary>
        public static event Action<TournamentRegistration> OnCheckedIn;

        /// <summary>Fired when the player leaves/unregisters from a tournament</summary>
        public static event Action<string> OnLeft;

        /// <summary>Fired when a tournament the player is in starts</summary>
        public static event Action<string> OnTournamentStarted;

        /// <summary>Fired when a match is ready to launch (from socket event)</summary>
        public static event Action<MatchLaunchPayload> OnMatchLaunch;

        /// <summary>Fired when enrollment status changes</summary>
        public static event Action<TournamentEnrollmentState> OnEnrollmentChanged;

        // Internal emit methods
        internal static void EmitRegistered(TournamentRegistration reg) => OnRegistered?.Invoke(reg);
        internal static void EmitCheckedIn(TournamentRegistration reg) => OnCheckedIn?.Invoke(reg);
        internal static void EmitLeft(string tournamentId) => OnLeft?.Invoke(tournamentId);
        internal static void EmitTournamentStarted(string tournamentId) => OnTournamentStarted?.Invoke(tournamentId);
        internal static void EmitMatchLaunch(MatchLaunchPayload data) => OnMatchLaunch?.Invoke(data);
        internal static void EmitEnrollmentChanged(TournamentEnrollmentState state) => OnEnrollmentChanged?.Invoke(state);
    }

    // =========================================================================
    // MODELS (Tournament-specific, not in core DeskillzModels)
    // =========================================================================

    /// <summary>
    /// Match launch payload from socket event
    /// </summary>
    [Serializable]
    public class MatchLaunchPayload
    {
        public string MatchId;
        public string TournamentId;
        public string GameId;
        public string DeepLink;
        public string Token;
        public int RoundNumber;
        public int TableNumber;
    }

    /// <summary>
    /// Filter parameters for tournament listing
    /// </summary>
    [Serializable]
    public class TournamentFilters
    {
        public string GameId;
        public string Status;
        public string Type;
        public string Currency;
        public bool? IsFeatured;
        public int? MinEntryFee;
        public int? MaxEntryFee;
        public int Limit = 20;
        public int Offset = 0;

        /// <summary>Convert to query string parameters</summary>
        internal string ToQueryString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(GameId)) parts.Add($"gameId={GameId}");
            if (!string.IsNullOrEmpty(Status)) parts.Add($"status={Status}");
            if (!string.IsNullOrEmpty(Type)) parts.Add($"type={Type}");
            if (!string.IsNullOrEmpty(Currency)) parts.Add($"currency={Currency}");
            if (IsFeatured.HasValue) parts.Add($"isFeatured={IsFeatured.Value}");
            if (MinEntryFee.HasValue) parts.Add($"minEntryFee={MinEntryFee.Value}");
            if (MaxEntryFee.HasValue) parts.Add($"maxEntryFee={MaxEntryFee.Value}");
            parts.Add($"limit={Limit}");
            parts.Add($"offset={Offset}");
            return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        }
    }

    /// <summary>
    /// Tournament API error
    /// </summary>
    [Serializable]
    public class TournamentError
    {
        public string Code;
        public string Message;

        public TournamentError() { }
        public TournamentError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"TournamentError({Code}: {Message})";

        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string NotFound = "NOT_FOUND";
            public const string AlreadyRegistered = "ALREADY_REGISTERED";
            public const string TournamentFull = "TOURNAMENT_FULL";
            public const string CheckInNotOpen = "CHECKIN_NOT_OPEN";
            public const string NotRegistered = "NOT_REGISTERED";
            public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
            public const string GuestNotAllowed = "GUEST_NOT_ALLOWED";
            public const string NetworkError = "NETWORK_ERROR";
            public const string ServerError = "SERVER_ERROR";
        }
    }

    // =========================================================================
    // MANAGER
    // =========================================================================

    /// <summary>
    /// Main API for tournament operations.
    /// All methods are static and use callback pattern (Action onSuccess, Action onError).
    /// </summary>
    public static class TournamentManager
    {
        // =====================================================================
        // CACHED STATE
        // =====================================================================

        /// <summary>Cached list of tournaments from last fetch</summary>
        public static List<TournamentListing> CachedTournaments { get; private set; } = new List<TournamentListing>();

        /// <summary>Cached list of player's registrations</summary>
        public static List<TournamentRegistration> MyRegistrations { get; private set; } = new List<TournamentRegistration>();

        /// <summary>Whether the manager has been initialized</summary>
        public static bool IsInitialized { get; private set; }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the tournament manager. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Initialize()
        {
            IsInitialized = true;
            DeskillzLogger.Info("[TournamentManager] Initialized");
        }

        /// <summary>
        /// Reset state on logout.
        /// </summary>
        internal static void Reset()
        {
            CachedTournaments.Clear();
            MyRegistrations.Clear();
            IsInitialized = false;
        }

        // =====================================================================
        // GET TOURNAMENTS (3.1)
        // =====================================================================

        /// <summary>
        /// Fetch available tournaments with optional filters.
        /// GET /api/v1/tournaments
        /// </summary>
        public static void GetTournaments(
            Action<List<TournamentListing>> onSuccess,
            Action<TournamentError> onError,
            TournamentFilters filters = null)
        {
            if (!EnsureAuth(onError)) return;

            // Default filter: current game
            if (filters == null) filters = new TournamentFilters();
            if (string.IsNullOrEmpty(filters.GameId))
                filters.GameId = DeskillzManager.Instance?.Config?.GameId;

            TournamentApiClient.GetTournaments(filters,
                tournaments =>
                {
                    CachedTournaments = tournaments ?? new List<TournamentListing>();
                    onSuccess?.Invoke(CachedTournaments);
                },
                onError
            );
        }

        /// <summary>
        /// Fetch active tournaments for a specific game.
        /// GET /api/v1/tournaments/game/:gameId/active
        /// </summary>
        public static void GetActiveTournaments(
            string gameId,
            Action<List<TournamentListing>> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;
            TournamentApiClient.GetActiveTournaments(gameId, onSuccess, onError);
        }

        // =====================================================================
        // JOIN TOURNAMENT (3.2)
        // =====================================================================

        /// <summary>
        /// Join a tournament directly (legacy -- prefer Register flow).
        /// POST /api/v1/tournaments/:id/join
        /// </summary>
        public static void JoinTournament(
            string tournamentId,
            Action onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;
            TournamentApiClient.JoinTournament(tournamentId, onSuccess, onError);
        }

        // =====================================================================
        // REGISTER (3.3)
        // =====================================================================

        /// <summary>
        /// Step 1: Register for a tournament.
        /// Creates a TournamentEntry with status REGISTERED.
        /// Player must check in during the 30-min window before start or be DQ'd.
        /// POST /api/v1/tournaments/:id/register
        /// </summary>
        public static void Register(
            string tournamentId,
            Action<TournamentRegistration> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;

            TournamentApiClient.Register(tournamentId,
                reg =>
                {
                    TournamentEvents.EmitRegistered(reg);
                    onSuccess?.Invoke(reg);
                },
                onError
            );
        }

        // =====================================================================
        // CHECK IN (3.4)
        // =====================================================================

        /// <summary>
        /// Step 2: Check in to a tournament.
        /// Only works during check-in window (T-30 to T-10 min before start).
        /// POST /api/v1/tournaments/:id/checkin
        /// </summary>
        public static void CheckIn(
            string tournamentId,
            Action<TournamentRegistration> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;

            TournamentApiClient.CheckIn(tournamentId,
                reg =>
                {
                    TournamentEvents.EmitCheckedIn(reg);
                    onSuccess?.Invoke(reg);
                },
                onError
            );
        }

        // =====================================================================
        // LEAVE (3.5)
        // =====================================================================

        /// <summary>
        /// Leave / unregister from a tournament.
        /// Only works while status is SCHEDULED or OPEN.
        /// Entry fee is refunded.
        /// DELETE /api/v1/tournaments/:id/leave
        /// </summary>
        public static void Leave(
            string tournamentId,
            Action onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;

            TournamentApiClient.Leave(tournamentId,
                () =>
                {
                    TournamentEvents.EmitLeft(tournamentId);
                    onSuccess?.Invoke();
                },
                onError
            );
        }

        // =====================================================================
        // ENROLLMENT STATUS (3.6)
        // =====================================================================

        /// <summary>
        /// Get current player's enrollment status for a tournament.
        /// Returns button state, DQ countdown, check-in window times.
        /// GET /api/v1/tournaments/:id/my-status
        /// </summary>
        public static void GetEnrollmentStatus(
            string tournamentId,
            Action<TournamentEnrollmentState> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;
            TournamentApiClient.GetEnrollmentStatus(tournamentId, onSuccess, onError);
        }

        // =====================================================================
        // MY REGISTRATIONS (3.7)
        // =====================================================================

        /// <summary>
        /// Get all tournaments the current player is registered for.
        /// Sorted by urgency: CHECKIN_OPEN first, then soonest start.
        /// GET /api/v1/tournaments/my-registrations
        /// </summary>
        public static void GetMyRegistrations(
            Action<List<TournamentRegistration>> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;

            TournamentApiClient.GetMyRegistrations(
                regs =>
                {
                    MyRegistrations = regs ?? new List<TournamentRegistration>();
                    onSuccess?.Invoke(MyRegistrations);
                },
                onError
            );
        }

        // =====================================================================
        // SCHEDULE (3.8)
        // =====================================================================

        /// <summary>
        /// Get tournament bracket schedule with rounds, tables, and seats.
        /// GET /api/v1/tournaments/:id/schedule
        /// </summary>
        public static void GetSchedule(
            string tournamentId,
            Action<TournamentSchedule> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;
            TournamentApiClient.GetSchedule(tournamentId, onSuccess, onError);
        }

        // =====================================================================
        // TABLE ASSIGNMENT (3.9)
        // =====================================================================

        /// <summary>
        /// Get current player's table assignment in the current round.
        /// Returns null if not yet seated.
        /// GET /api/v1/tournaments/:id/my-seat
        /// </summary>
        public static void GetMyTableAssignment(
            string tournamentId,
            Action<TableAssignment> onSuccess,
            Action<TournamentError> onError)
        {
            if (!EnsureAuth(onError)) return;
            TournamentApiClient.GetMyTableAssignment(tournamentId, onSuccess, onError);
        }

        // =====================================================================
        // SOCKET EVENT HANDLERS (3.10)
        // Called by DeskillzNetwork/EventsGateway when socket events arrive
        // =====================================================================

        /// <summary>
        /// Handle tournament:started socket event
        /// </summary>
        internal static void HandleTournamentStarted(string tournamentId)
        {
            DeskillzLogger.Info($"[TournamentManager] Tournament started: {tournamentId}");
            TournamentEvents.EmitTournamentStarted(tournamentId);
        }

        /// <summary>
        /// Handle tournament:left socket event
        /// </summary>
        internal static void HandleTournamentLeft(string tournamentId)
        {
            DeskillzLogger.Info($"[TournamentManager] Tournament left: {tournamentId}");
            TournamentEvents.EmitLeft(tournamentId);
        }

        /// <summary>
        /// Handle match:launch socket event
        /// </summary>
        internal static void HandleMatchLaunch(MatchLaunchPayload data)
        {
            DeskillzLogger.Info($"[TournamentManager] Match launch: {data.MatchId} (Tournament: {data.TournamentId})");
            TournamentEvents.EmitMatchLaunch(data);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Check if user is authenticated. Returns false and invokes error if not.
        /// </summary>
        private static bool EnsureAuth(Action<TournamentError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                onError?.Invoke(new TournamentError(
                    TournamentError.Codes.NotAuthenticated,
                    "Must be authenticated to access tournaments"
                ));
                return false;
            }
            return true;
        }
    }
}
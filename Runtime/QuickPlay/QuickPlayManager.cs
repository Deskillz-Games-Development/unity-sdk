// =============================================================================
// Deskillz SDK for Unity - Quick Play Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 6)
// =============================================================================
//
// Public API for Quick Play matchmaking and social Quick Play rooms.
// Mirrors DeskillzBridge.ts Quick Play methods (lines 2659-2970).
//
// Esport Quick Play flow:
//   1. JoinQueue() -> queued for matchmaking
//   2. OnMatchFound event fires -> call LaunchMatch()
//   3. Play the game -> call SubmitScore()
//   4. GetMatchResults() to see final standings
//
// Social Quick Play flow:
//   1. CreateSocialRoom() -> room created
//   2. Players join via room code
//   3. SubmitSocialRound() after each round
//   4. SocialRebuy() / SocialCashOut() / EndSocialGame()
//
// Events:
//   QuickPlayEvents.OnSearching     - entered queue
//   QuickPlayEvents.OnMatchFound    - match ready, call LaunchMatch
//   QuickPlayEvents.OnMatchLaunched - match launched, start gameplay
//   QuickPlayEvents.OnScoreSubmitted - score recorded
//   QuickPlayEvents.OnMatchCompleted - final results available
//   QuickPlayEvents.OnQueueTimeout  - no match found in time
//   QuickPlayEvents.OnLeft          - left queue
//
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.QuickPlay
{
    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>
    /// Quick Play lifecycle events.
    /// </summary>
    public static class QuickPlayEvents
    {
        /// <summary>Entered matchmaking queue</summary>
        public static event Action<QuickPlayJoinResult> OnSearching;

        /// <summary>Match found, ready to launch</summary>
        public static event Action<QuickPlayFoundData> OnMatchFound;

        /// <summary>Match launched, gameplay starts</summary>
        public static event Action<QuickPlayLaunchData> OnMatchLaunched;

        /// <summary>Score submitted successfully</summary>
        public static event Action<QuickPlayScoreResult> OnScoreSubmitted;

        /// <summary>Match completed, final results</summary>
        public static event Action<QuickPlayMatchResult> OnMatchCompleted;

        /// <summary>Queue timed out, no match found</summary>
        public static event Action OnQueueTimeout;

        /// <summary>Left the queue</summary>
        public static event Action OnLeft;

        // Internal emitters
        internal static void EmitSearching(QuickPlayJoinResult r) => OnSearching?.Invoke(r);
        internal static void EmitMatchFound(QuickPlayFoundData d) => OnMatchFound?.Invoke(d);
        internal static void EmitMatchLaunched(QuickPlayLaunchData d) => OnMatchLaunched?.Invoke(d);
        internal static void EmitScoreSubmitted(QuickPlayScoreResult r) => OnScoreSubmitted?.Invoke(r);
        internal static void EmitMatchCompleted(QuickPlayMatchResult r) => OnMatchCompleted?.Invoke(r);
        internal static void EmitQueueTimeout() => OnQueueTimeout?.Invoke();
        internal static void EmitLeft() => OnLeft?.Invoke();
    }

    // =========================================================================
    // ADDITIONAL MODELS (Quick Play specific, not in core DeskillzModels)
    // =========================================================================

    /// <summary>
    /// Data received when a match is found via socket event
    /// </summary>
    [Serializable]
    public class QuickPlayFoundData
    {
        public string MatchId;
        public string MatchSessionId;
        public string GameId;
        public decimal EntryFee;
        public string Currency;
    }

    /// <summary>
    /// Quick Play queue status
    /// </summary>
    [Serializable]
    public class QuickPlayStatus
    {
        public bool InQueue;
        public List<QuickPlayQueueEntry> Queues;

        public QuickPlayStatus()
        {
            Queues = new List<QuickPlayQueueEntry>();
        }
    }

    /// <summary>
    /// Individual queue entry in status response
    /// </summary>
    [Serializable]
    public class QuickPlayQueueEntry
    {
        public string QueueKey;
        public string GameId;
        public decimal EntryFee;
        public string Currency;
        public int Position;
        public int PlayersInQueue;
        public string JoinedAt;
    }

    /// <summary>
    /// Parameters for creating a social Quick Play room
    /// </summary>
    [Serializable]
    public class CreateSocialQuickPlayParams
    {
        public string GameId;
        public decimal PointValueUsd;
        public string Currency = "USDT_BSC";
        public int SeatsPerTable = 4;
    }

    /// <summary>
    /// Result of creating a social Quick Play room
    /// </summary>
    [Serializable]
    public class CreateSocialQuickPlayResult
    {
        public bool Success;
        public string RoomId;
        public string RoomCode;
    }

    /// <summary>
    /// Payload for submitting a social Quick Play round
    /// </summary>
    [Serializable]
    public class SocialRoundPayload
    {
        public int RoundNumber;
        public List<SocialRoundPlayerResult> PlayerResults;

        public SocialRoundPayload()
        {
            PlayerResults = new List<SocialRoundPlayerResult>();
        }
    }

    /// <summary>
    /// Per-player result in a social round
    /// </summary>
    [Serializable]
    public class SocialRoundPlayerResult
    {
        public string PlayerId;
        public int Score;
        public int PointsWon;
    }

    /// <summary>
    /// Quick Play error
    /// </summary>
    [Serializable]
    public class QuickPlayError
    {
        public string Code;
        public string Message;

        public QuickPlayError() { }
        public QuickPlayError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"QuickPlayError({Code}: {Message})";

        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string AlreadyInQueue = "ALREADY_IN_QUEUE";
            public const string NotInQueue = "NOT_IN_QUEUE";
            public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
            public const string MatchNotFound = "MATCH_NOT_FOUND";
            public const string QueueTimeout = "QUEUE_TIMEOUT";
            public const string GuestNotAllowed = "GUEST_NOT_ALLOWED";
            public const string NetworkError = "NETWORK_ERROR";
            public const string ServerError = "SERVER_ERROR";
        }
    }

    // =========================================================================
    // MANAGER
    // =========================================================================

    /// <summary>
    /// Main API for Quick Play matchmaking and social Quick Play rooms.
    /// </summary>
    public static class QuickPlayManager
    {
        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Current Quick Play match data (null if not in a match)</summary>
        public static QuickPlayLaunchData CurrentMatch { get; private set; }

        /// <summary>Whether the player is currently in queue</summary>
        public static bool IsInQueue { get; private set; }

        /// <summary>Cached Quick Play config for current game</summary>
        public static QuickPlayConfig CachedConfig { get; private set; }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        internal static void Initialize()
        {
            DeskillzLogger.Info("[QuickPlayManager] Initialized");
        }

        internal static void Reset()
        {
            CurrentMatch = null;
            IsInQueue = false;
            CachedConfig = null;
        }

        // =====================================================================
        // JOIN QUEUE (6.1)
        // =====================================================================

        /// <summary>
        /// Join the Quick Play matchmaking queue.
        /// POST /api/v1/lobby/quick-play/join
        /// </summary>
        public static void JoinQueue(
            QuickPlayJoinParams parameters,
            Action<QuickPlayJoinResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            // Default game ID from config
            if (string.IsNullOrEmpty(parameters.GameId))
                parameters.GameId = DeskillzManager.Instance?.Config?.GameId;

            QuickPlayApiClient.JoinQueue(parameters,
                result =>
                {
                    IsInQueue = true;
                    QuickPlayEvents.EmitSearching(result);
                    onSuccess?.Invoke(result);
                },
                onError
            );
        }

        // =====================================================================
        // LEAVE QUEUE (6.2)
        // =====================================================================

        /// <summary>
        /// Leave the Quick Play matchmaking queue.
        /// POST /api/v1/lobby/quick-play/leave
        /// </summary>
        public static void LeaveQueue(
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            QuickPlayApiClient.LeaveQueue(
                () =>
                {
                    IsInQueue = false;
                    QuickPlayEvents.EmitLeft();
                    onSuccess?.Invoke();
                },
                onError
            );
        }

        // =====================================================================
        // GET CONFIG (6.3)
        // =====================================================================

        /// <summary>
        /// Fetch Quick Play configuration for a game.
        /// Returns tiers, player modes, matchmaking settings.
        /// GET /api/v1/quick-play/games/:gameId
        /// </summary>
        public static void GetConfig(
            string gameId,
            Action<QuickPlayConfig> onSuccess,
            Action<QuickPlayError> onError)
        {
            QuickPlayApiClient.GetConfig(gameId,
                config =>
                {
                    CachedConfig = config;
                    onSuccess?.Invoke(config);
                },
                onError
            );
        }

        // =====================================================================
        // GET STATUS (6.4)
        // =====================================================================

        /// <summary>
        /// Get current Quick Play queue status.
        /// GET /api/v1/lobby/quick-play/status
        /// </summary>
        public static void GetStatus(
            Action<QuickPlayStatus> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            QuickPlayApiClient.GetStatus(
                status =>
                {
                    IsInQueue = status?.InQueue ?? false;
                    onSuccess?.Invoke(status);
                },
                onError
            );
        }

        // =====================================================================
        // LAUNCH MATCH (6.5)
        // =====================================================================

        /// <summary>
        /// Launch a Quick Play match after being matched.
        /// POST /api/v1/lobby/quick-play/match/launch
        /// </summary>
        public static void LaunchMatch(
            string matchSessionId,
            Action<QuickPlayLaunchData> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            QuickPlayApiClient.LaunchMatch(matchSessionId,
                data =>
                {
                    CurrentMatch = data;
                    IsInQueue = false;
                    QuickPlayEvents.EmitMatchLaunched(data);
                    onSuccess?.Invoke(data);
                },
                onError
            );
        }

        // =====================================================================
        // SUBMIT SCORE (6.6)
        // =====================================================================

        /// <summary>
        /// Submit score for a Quick Play match.
        /// POST /api/v1/lobby/quick-play/match/:id/score
        /// </summary>
        public static void SubmitScore(
            string matchId,
            int score,
            Action<QuickPlayScoreResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            QuickPlayApiClient.SubmitScore(matchId, score,
                result =>
                {
                    QuickPlayEvents.EmitScoreSubmitted(result);
                    onSuccess?.Invoke(result);
                },
                onError
            );
        }

        // =====================================================================
        // GET MATCH RESULTS (6.7)
        // =====================================================================

        /// <summary>
        /// Get final results of a Quick Play match.
        /// GET /api/v1/lobby/quick-play/match/:id/results
        /// </summary>
        public static void GetMatchResults(
            string matchId,
            Action<QuickPlayMatchResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;
            QuickPlayApiClient.GetMatchResults(matchId, onSuccess, onError);
        }

        // =====================================================================
        // FORCE COMPLETE (6.8)
        // =====================================================================

        /// <summary>
        /// Force complete a Quick Play match (timeout / disconnect scenarios).
        /// POST /api/v1/lobby/quick-play/match/:id/complete
        /// </summary>
        public static void ForceCompleteMatch(
            string matchId,
            Action<QuickPlayMatchResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            QuickPlayApiClient.ForceComplete(matchId,
                result =>
                {
                    CurrentMatch = null;
                    QuickPlayEvents.EmitMatchCompleted(result);
                    onSuccess?.Invoke(result);
                },
                onError
            );
        }

        // =====================================================================
        // SOCIAL QUICK PLAY (6.9 - 6.11)
        // =====================================================================

        /// <summary>
        /// Create a social Quick Play room (cash game).
        /// POST /api/v1/lobby/quick-play/social/create
        /// </summary>
        public static void CreateSocialRoom(
            CreateSocialQuickPlayParams parameters,
            Action<CreateSocialQuickPlayResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;

            if (string.IsNullOrEmpty(parameters.GameId))
                parameters.GameId = DeskillzManager.Instance?.Config?.GameId;

            QuickPlayApiClient.CreateSocialRoom(parameters, onSuccess, onError);
        }

        /// <summary>
        /// Submit a round result for a social Quick Play game.
        /// POST /api/v1/lobby/quick-play/social/:roomId/round
        /// </summary>
        public static void SubmitSocialRound(
            string roomId,
            SocialRoundPayload payload,
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;
            QuickPlayApiClient.SubmitSocialRound(roomId, payload, onSuccess, onError);
        }

        /// <summary>
        /// Rebuy chips in a social Quick Play game.
        /// POST /api/v1/lobby/quick-play/social/:roomId/rebuy
        /// </summary>
        public static void SocialRebuy(
            string roomId,
            decimal amount,
            Action<decimal> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;
            QuickPlayApiClient.SocialRebuy(roomId, amount, onSuccess, onError);
        }

        /// <summary>
        /// Cash out of a social Quick Play game.
        /// POST /api/v1/lobby/quick-play/social/:roomId/cashout
        /// </summary>
        public static void SocialCashOut(
            string roomId,
            Action<decimal> onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;
            QuickPlayApiClient.SocialCashOut(roomId, onSuccess, onError);
        }

        /// <summary>
        /// End a social Quick Play game (host only).
        /// POST /api/v1/lobby/quick-play/social/:roomId/end
        /// </summary>
        public static void EndSocialGame(
            string roomId,
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            if (!EnsureAuth(onError)) return;
            QuickPlayApiClient.EndSocialGame(roomId, onSuccess, onError);
        }

        // =====================================================================
        // SOCKET EVENT HANDLERS (6.12)
        // =====================================================================

        /// <summary>Handle queue:matched socket event</summary>
        internal static void HandleMatchFound(QuickPlayFoundData data)
        {
            DeskillzLogger.Info($"[QuickPlayManager] Match found: {data.MatchId}");
            IsInQueue = false;
            QuickPlayEvents.EmitMatchFound(data);
        }

        /// <summary>Handle queue:timeout socket event</summary>
        internal static void HandleQueueTimeout()
        {
            DeskillzLogger.Info("[QuickPlayManager] Queue timed out");
            IsInQueue = false;
            QuickPlayEvents.EmitQueueTimeout();
        }

        /// <summary>Handle match:completed socket event</summary>
        internal static void HandleMatchCompleted(QuickPlayMatchResult result)
        {
            DeskillzLogger.Info($"[QuickPlayManager] Match completed: {result.MatchId}");
            CurrentMatch = null;
            QuickPlayEvents.EmitMatchCompleted(result);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>Get current match data (null if not in a match)</summary>
        public static QuickPlayLaunchData GetCurrentMatch() => CurrentMatch;

        private static bool EnsureAuth(Action<QuickPlayError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                onError?.Invoke(new QuickPlayError(
                    QuickPlayError.Codes.NotAuthenticated,
                    "Must be authenticated to use Quick Play"
                ));
                return false;
            }
            return true;
        }
    }
}
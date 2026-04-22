// =============================================================================
// Deskillz SDK for Unity - Dispute Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 4)
// =============================================================================
//
// Public API for dispute operations. Mirrors DeskillzBridge.ts dispute
// methods (lines 1952-2060).
//
// Usage:
//   DisputeManager.FileDispute(new FileDisputeParams {
//       DisputeType = "TOURNAMENT",
//       MatchId = "...",
//       Reason = "Cheating",
//       Description = "Opponent used auto-clicker"
//   },
//       dispute => Debug.Log($"Filed: {dispute.Id}"),
//       error => Debug.LogError(error.Message)
//   );
//
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Disputes
{
    // =========================================================================
    // REQUEST MODELS
    // =========================================================================

    /// <summary>
    /// Parameters for filing a dispute
    /// </summary>
    [Serializable]
    public class FileDisputeParams
    {
        /// <summary>Type: TOURNAMENT, QUICK_PLAY, or PRIVATE_ROOM</summary>
        public string DisputeType;
        public string TournamentId;
        public string MatchId;
        public string RoomCode;
        public string Reason;
        public string Description;
        public List<string> Evidence;

        public FileDisputeParams()
        {
            Evidence = new List<string>();
        }
    }

    /// <summary>
    /// Internal JSON request for file dispute API
    /// </summary>
    [Serializable]
    internal class FileDisputeRequest
    {
        public string disputeType;
        public string tournamentId;
        public string matchId;
        public string roomCode;
        public string reason;
        public string description;
        public string[] evidence;
    }

    /// <summary>
    /// Recent match formatted for dispute context selection
    /// </summary>
    [Serializable]
    public class RecentMatchForDispute
    {
        public string MatchId;
        public string TournamentId;
        public string MatchType;
        public string OpponentName;
        public int? MyScore;
        public bool IsWinner;
        public string PlayedAt;
        public string GameName;
    }

    /// <summary>
    /// Last match context (persisted locally for auto-suggest)
    /// </summary>
    [Serializable]
    public class LastMatchContext
    {
        public string MatchId;
        public string TournamentId;
        public string RoomCode;
        public string GameId;
        public string DisputeType;
        public string OpponentName;
        public string CompletedAt;
    }

    /// <summary>
    /// Dispute API error
    /// </summary>
    [Serializable]
    public class DisputeError
    {
        public string Code;
        public string Message;

        public DisputeError() { }
        public DisputeError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"DisputeError({Code}: {Message})";
    }

    // =========================================================================
    // MANAGER
    // =========================================================================

    /// <summary>
    /// Main API for dispute operations.
    /// </summary>
    public static class DisputeManager
    {
        private const string LAST_MATCH_KEY = "deskillz_last_match";
        private const int LAST_MATCH_EXPIRY_DAYS = 7;

        // =====================================================================
        // FILE DISPUTE (4.1)
        // =====================================================================

        /// <summary>
        /// File a dispute against a tournament, QuickPlay, or private room match.
        /// POST /api/v1/disputes
        /// </summary>
        public static void FileDispute(
            FileDisputeParams parameters,
            Action<DisputeRecord> onSuccess,
            Action<DisputeError> onError)
        {
            if (!EnsureAuth(onError)) return;

            var request = new FileDisputeRequest
            {
                disputeType = parameters.DisputeType,
                tournamentId = parameters.TournamentId,
                matchId = parameters.MatchId,
                roomCode = parameters.RoomCode,
                reason = parameters.Reason,
                description = parameters.Description,
                evidence = parameters.Evidence?.ToArray() ?? Array.Empty<string>(),
            };

            DisputeApiClient.FileDispute(request, onSuccess, onError);
        }

        // =====================================================================
        // GET MY DISPUTES (4.2)
        // =====================================================================

        /// <summary>
        /// Get all disputes filed by the current user.
        /// GET /api/v1/disputes/me
        /// </summary>
        public static void GetMyDisputes(
            Action<List<DisputeRecord>> onSuccess,
            Action<DisputeError> onError,
            string status = null)
        {
            if (!EnsureAuth(onError)) return;
            DisputeApiClient.GetMyDisputes(status, onSuccess, onError);
        }

        // =====================================================================
        // GET DISPUTE DETAILS (4.3)
        // =====================================================================

        /// <summary>
        /// Get dispute details by ID (own disputes only).
        /// GET /api/v1/disputes/:id
        /// </summary>
        public static void GetDisputeDetails(
            string disputeId,
            Action<DisputeRecord> onSuccess,
            Action<DisputeError> onError)
        {
            if (!EnsureAuth(onError)) return;
            DisputeApiClient.GetDisputeDetails(disputeId, onSuccess, onError);
        }

        // =====================================================================
        // ADD EVIDENCE (4.4)
        // =====================================================================

        /// <summary>
        /// Add evidence to an existing open dispute.
        /// POST /api/v1/disputes/:id/evidence
        /// </summary>
        public static void AddEvidence(
            string disputeId,
            List<string> evidence,
            Action<int> onSuccess,
            Action<DisputeError> onError)
        {
            if (!EnsureAuth(onError)) return;
            DisputeApiClient.AddEvidence(disputeId, evidence.ToArray(), onSuccess, onError);
        }

        // =====================================================================
        // RECENT MATCHES FOR DISPUTE (4.5)
        // =====================================================================

        /// <summary>
        /// Get recent matches formatted for dispute context selection.
        /// Returns last 10 matches with identifiers needed for DisputeModal.
        /// GET /api/v1/matches/history/me?limit=10
        /// </summary>
        public static void GetRecentMatches(
            Action<List<RecentMatchForDispute>> onSuccess,
            Action<DisputeError> onError)
        {
            if (!EnsureAuth(onError)) return;
            DisputeApiClient.GetRecentMatches(onSuccess, onError);
        }

        // =====================================================================
        // LAST MATCH PERSISTENCE (4.6)
        // =====================================================================

        /// <summary>
        /// Persist last completed match context to PlayerPrefs.
        /// Called automatically after score submission or match completion.
        /// Enables dispute auto-suggest for the most recent match.
        /// </summary>
        public static void PersistLastMatch(LastMatchContext data)
        {
            try
            {
                var json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(LAST_MATCH_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                DeskillzLogger.Warning($"[DisputeManager] Failed to persist last match: {ex.Message}");
            }
        }

        /// <summary>
        /// Get last completed match context from PlayerPrefs.
        /// Returns null if no match stored or if expired (7 days).
        /// </summary>
        public static LastMatchContext GetLastMatch()
        {
            try
            {
                var json = PlayerPrefs.GetString(LAST_MATCH_KEY, null);
                if (string.IsNullOrEmpty(json)) return null;

                var data = JsonUtility.FromJson<LastMatchContext>(json);

                // Expire after 7 days
                if (!string.IsNullOrEmpty(data.CompletedAt))
                {
                    if (DateTime.TryParse(data.CompletedAt, out var completedAt))
                    {
                        if ((DateTime.UtcNow - completedAt).TotalDays > LAST_MATCH_EXPIRY_DAYS)
                        {
                            PlayerPrefs.DeleteKey(LAST_MATCH_KEY);
                            return null;
                        }
                    }
                }

                return data;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clear stored last match context.
        /// </summary>
        public static void ClearLastMatch()
        {
            PlayerPrefs.DeleteKey(LAST_MATCH_KEY);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool EnsureAuth(Action<DisputeError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                onError?.Invoke(new DisputeError("NOT_AUTHENTICATED", "Must be authenticated to manage disputes"));
                return false;
            }
            return true;
        }
    }
}
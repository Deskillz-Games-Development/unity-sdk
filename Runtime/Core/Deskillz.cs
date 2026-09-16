// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using Deskillz.Multiplayer; // [P1-3.7.0 fixB]

namespace Deskillz
{
    /// <summary>
    /// Main entry point for the Deskillz SDK.
    /// Use this static class for all SDK interactions.
    /// </summary>
    public static class Deskillz
    {
        // =============================================================================
        // SDK STATE PROPERTIES
        // =============================================================================

        /// <summary>
        /// Current SDK state.
        /// </summary>
        public static SDKState State => Manager?.State ?? SDKState.Uninitialized;

        /// <summary>
        /// Whether the SDK is ready to use.
        /// </summary>
        public static bool IsReady => Manager?.IsReady ?? false;

        /// <summary>
        /// Whether the SDK is in test mode.
        /// </summary>
        public static bool TestMode => Manager?.IsTestMode ?? false;

        /// <summary>
        /// SDK version string.
        /// </summary>
        public static string Version => DeskillzConfig.SDK_VERSION;

        // =============================================================================
        // PLAYER PROPERTIES
        // =============================================================================

        /// <summary>
        /// Current player data.
        /// </summary>
        public static PlayerData CurrentPlayer => Manager?.CurrentPlayer;

        /// <summary>
        /// Whether a player is authenticated.
        /// </summary>
        public static bool IsPlayerAuthenticated => CurrentPlayer != null;

        // =============================================================================
        // MATCH PROPERTIES
        // =============================================================================

        /// <summary>
        /// Current match data.
        /// </summary>
        public static MatchData CurrentMatch => Manager?.CurrentMatch;

        /// <summary>
        /// Whether there is an active match.
        /// </summary>
        public static bool HasActiveMatch => CurrentMatch != null && 
            CurrentMatch.Status != MatchStatus.None && 
            CurrentMatch.Status != MatchStatus.Completed;

        /// <summary>
        /// Whether currently in a match.
        /// </summary>
        public static bool IsInMatch => CurrentMatch?.Status == MatchStatus.InProgress;

        // =============================================================================
        // INTERNAL ACCESSORS
        // =============================================================================

        private static DeskillzManager Manager => DeskillzManager.Instance;
        private static DeskillzConfig Config => DeskillzConfig.Instance;
        private static DeskillzNetwork Network => Manager?.Network;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize the SDK. Usually auto-called, but can be called manually.
        /// </summary>
        public static void Initialize()
        {
            EnsureManager();
            Manager.Initialize();
        }

        /// <summary>
        /// Initialize with custom configuration.
        /// </summary>
        public static void Initialize(DeskillzConfig config)
        {
            EnsureManager();
            Manager.Initialize(config);
        }

        private static void EnsureManager()
        {
            // Accessing Instance auto-creates if needed
            var _ = Manager;
        }

        // =============================================================================
        // MATCH CONTROL
        // =============================================================================

        /// <summary>
        /// Start the current match. Call when player is ready to play.
        /// </summary>
        public static void StartMatch()
        {
            if (!IsReady)
            {
                DeskillzLogger.Error("SDK not ready. Cannot start match.");
                DeskillzEvents.RaiseError(new DeskillzError(ErrorCode.NotInitialized, "SDK not initialized"));
                return;
            }

            if (CurrentMatch == null)
            {
                DeskillzLogger.Error("No match available. Wait for OnMatchReady event.");
                DeskillzEvents.RaiseError(new DeskillzError(ErrorCode.NoActiveMatch, "No match to start"));
                return;
            }

            Manager.BeginMatch();
        }

        /// <summary>
        /// Submit the player's final score.
        /// </summary>
        /// <param name="score">The final score to submit</param>
        public static void SubmitScore(int score)
        {
            if (!ValidateMatchAction("submit score")) return;

            DeskillzLogger.LogScore("Submitting", score);

            // Update local match data
            CurrentMatch.LocalPlayerScore = score;
            DeskillzEvents.RaiseLocalScoreUpdated(score);

            // Submit to server
            if (TestMode)
            {
                // In test mode, just simulate success
                Manager.Run(SimulateScoreSubmission(score));
            }
            else
            {
                Manager.Run(SubmitScoreToServer(score));
            }
        }

        /// <summary>
        /// Update score in real-time (for sync matches).
        /// </summary>
        /// <param name="score">Current score</param>
        public static void UpdateScore(int score)
        {
            if (!ValidateMatchAction("update score")) return;

            CurrentMatch.LocalPlayerScore = score;
            DeskillzEvents.RaiseLocalScoreUpdated(score);

            // Broadcast to other players in real-time matches
            if (CurrentMatch.IsRealtime && Network?.IsConnected == true)
            {
                // [P1-3.7.0] relay live score to the other seats (game:state via match:relay)
                Network.Relay("game:state", new { playerId = CurrentPlayer.Id, score });
            }
        }

        /// <summary>
        /// End the current match.
        /// </summary>
        public static void EndMatch()
        {
            if (!ValidateMatchAction("end match")) return;

            DeskillzLogger.LogMatch("Ending", CurrentMatch.MatchId);
            DeskillzEvents.RaiseMatchEnding();

            if (TestMode)
            {
                Manager.Run(SimulateMatchEnd());
            }
            else
            {
                Manager.Run(EndMatchOnServer());
            }
        }

        /// <summary>
        /// Forfeit the current match.
        /// </summary>
        public static void ForfeitMatch()
        {
            if (!ValidateMatchAction("forfeit match")) return;

            DeskillzLogger.LogMatch("Forfeiting", CurrentMatch.MatchId);
            DeskillzEvents.RaiseMatchForfeited();

            CurrentMatch.Status = MatchStatus.Forfeited;

            var result = new MatchResult
            {
                MatchId = CurrentMatch.MatchId,
                Outcome = MatchOutcome.Forfeit,
                FinalScore = CurrentMatch.LocalPlayerScore,
                PrizeWon = 0,
                Currency = CurrentMatch.Currency
            };

            Manager.EndCurrentMatch(result);
        }

        /// <summary>
        /// Report another player for cheating or misconduct.
        /// </summary>
        public static void ReportPlayer(string playerId, string reason)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                DeskillzLogger.Warning("Cannot report: invalid player ID");
                return;
            }

            DeskillzLogger.Info($"Reporting player {playerId}: {reason}");

            if (!TestMode)
            {
                // [P1-3.7.0] reports are disputes: POST /api/v1/disputes
                var tournamentId = ScoreSigner.CurrentTournamentId();
                Manager.Run(Network.Post<object>(
                    "/disputes",
                    new
                    {
                        disputeType = string.IsNullOrEmpty(tournamentId) ? "QUICK_PLAY" : "TOURNAMENT",
                        tournamentId,
                        matchId = CurrentMatch?.MatchId,
                        reason,
                        description = $"Player report against {playerId}: {reason}"
                    },
                    _ => DeskillzLogger.Info("Player reported successfully"),
                    error => DeskillzLogger.Error($"Failed to report player: {error.Message}")
                ));
            }
        }

        // =============================================================================
        // TEST MODE HELPERS
        // =============================================================================

        /// <summary>
        /// Start a test match (Test Mode only).
        /// </summary>
        public static void StartTestMatch(MatchMode mode = MatchMode.Asynchronous, int timeLimitSeconds = 0)
        {
            if (!TestMode)
            {
                DeskillzLogger.Error("StartTestMatch only available in Test Mode");
                return;
            }

            Manager.StartTestMatch(mode, timeLimitSeconds);
        }

        /// <summary>
        /// Simulate receiving a score from opponent (Test Mode only).
        /// </summary>
        public static void SimulateOpponentScore(int score)
        {
            if (!TestMode)
            {
                DeskillzLogger.Warning("SimulateOpponentScore only works in Test Mode");
                return;
            }

            if (CurrentMatch == null || CurrentMatch.Players.Count < 2) return;

            var opponent = CurrentMatch.Players.Find(p => !p.IsLocalPlayer);
            if (opponent != null)
            {
                opponent.Score = score;
                DeskillzEvents.RaisePlayerScoreUpdated(opponent.PlayerId, score);
            }
        }

        // =============================================================================
        // MULTIPLAYER (Nested Class)
        // =============================================================================

        /// <summary>
        /// Real-time multiplayer functionality.
        /// </summary>
        public static class Multiplayer
        {
            /// <summary>
            /// Whether connected to multiplayer server.
            /// </summary>
            public static bool IsConnected => Network?.IsConnected ?? false;

            /// <summary>
            /// Current connection state.
            /// </summary>
            public static ConnectionState ConnectionState => Network?.ConnectionState ?? ConnectionState.Disconnected;

            /// <summary>
            /// Send a message to all other players.
            /// </summary>
            public static void SendMessage<T>(T data)
            {
                // [P1-3.7.0 fixB] broadcast to the other seats via match:relay (game:action)
                if (!IsConnected)
                {
                    DeskillzLogger.Warning("Cannot send message: not connected");
                    return;
                }
                Network.Relay("game:action", new { senderId = CurrentPlayer?.Id, data });
            }

            /// <summary>
            /// Send a message to a specific player.
            /// </summary>
            public static void SendToPlayer<T>(string playerId, T data)
            {
                // [P1-3.7.0 fixB] the relay is room-wide; receivers filter on targetId
                if (!IsConnected)
                {
                    DeskillzLogger.Warning("Cannot send message: not connected");
                    return;
                }
                Network.Relay("game:action", new { senderId = CurrentPlayer?.Id, targetId = playerId, data });
            }

            /// <summary>
            /// Get all players in current match.
            /// </summary>
            public static MatchPlayer[] GetPlayers()
            {
                return CurrentMatch?.Players?.ToArray() ?? Array.Empty<MatchPlayer>();
            }

            /// <summary>
            /// Get a specific player's state.
            /// </summary>
            public static PlayerState GetPlayerState(string playerId)
            {
                return DeskillzCache.Get<PlayerState>($"player_state_{playerId}");
            }

            /// <summary>
            /// Update and broadcast local player state.
            /// </summary>
            public static void SetLocalState<T>(T data)
            {
                if (!IsConnected) return;
                var state = new PlayerState
                {
                    PlayerId = CurrentPlayer?.Id,
                    Score = CurrentMatch?.LocalPlayerScore ?? 0
                };
                state.SetCustomData(data);
                DeskillzCache.Set($"player_state_{CurrentPlayer?.Id}", state);
                Network.Relay("game:state", state); // [P1-3.7.0 fixB]
            }
        }

        // =============================================================================
        // CUSTOM STAGE (Nested Class)
        // =============================================================================

        /// <summary>
        /// Custom stage/room functionality.
        /// </summary>
        // [P1-3.7.0 fixC] the obsolete Stage facade left with the Stage module (see Runtime/Legacy~)
        // =============================================================================
        // UI (Nested Class)
        // =============================================================================

        /// <summary>
        /// UI management functionality.
        /// </summary>
        public static class UI
        {
            private static IDeskillzMatchUI _customMatchUI;

            /// <summary>
            /// Set a custom match UI implementation.
            /// </summary>
            public static void SetMatchUI(IDeskillzMatchUI customUI)
            {
                _customMatchUI = customUI;
                DeskillzLogger.Info("Custom match UI registered");
            }

            /// <summary>
            /// Get the current match UI.
            /// </summary>
            public static IDeskillzMatchUI GetMatchUI()
            {
                return _customMatchUI;
            }

            /// <summary>
            /// Clear custom UI and use built-in.
            /// </summary>
            public static void UseBuiltInUI()
            {
                _customMatchUI = null;
                DeskillzLogger.Info("Using built-in UI");
            }
        }

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        private static bool ValidateMatchAction(string action)
        {
            if (!IsReady)
            {
                DeskillzLogger.Error($"Cannot {action}: SDK not ready");
                return false;
            }

            if (CurrentMatch == null)
            {
                DeskillzLogger.Error($"Cannot {action}: no active match");
                return false;
            }

            if (CurrentMatch.Status != MatchStatus.InProgress)
            {
                DeskillzLogger.Warning($"Cannot {action}: match not in progress (status: {CurrentMatch.Status})");
                return false;
            }

            return true;
        }

        // =============================================================================
        // COROUTINES
        // =============================================================================

        private static IEnumerator SimulateScoreSubmission(int score)
        {
            yield return new WaitForSeconds(0.3f);
            DeskillzEvents.RaiseScoreSubmitted(score);
        }

        private static IEnumerator SubmitScoreToServer(int score)
        {
            // [P1-3.7.0] HMAC-signed final score -> POST /api/v1/tournaments/:id/score
            var matchId = CurrentMatch.MatchId;
            yield return ScoreSigner.SubmitTournamentScore(Network, matchId, score,
                () => DeskillzEvents.RaiseScoreSubmitted(score),
                error =>
                {
                    DeskillzLogger.Error($"Score submission failed: {error.Message}");
                    if (error.IsRecoverable)
                    {
                        DeskillzCache.QueuePendingScore(matchId, score, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    }
                    DeskillzEvents.RaiseScoreSubmissionFailed(error);
                });
        }

        private static IEnumerator SimulateMatchEnd()
        {
            yield return new WaitForSeconds(0.5f);

            var opponentScore = UnityEngine.Random.Range(0, CurrentMatch.LocalPlayerScore + 100);
            MatchOutcome outcome;

            if (Config.ScoreType == ScoreType.HigherIsBetter)
            {
                outcome = CurrentMatch.LocalPlayerScore > opponentScore ? MatchOutcome.Win :
                          CurrentMatch.LocalPlayerScore < opponentScore ? MatchOutcome.Loss : MatchOutcome.Tie;
            }
            else
            {
                outcome = CurrentMatch.LocalPlayerScore < opponentScore ? MatchOutcome.Win :
                          CurrentMatch.LocalPlayerScore > opponentScore ? MatchOutcome.Loss : MatchOutcome.Tie;
            }

            var result = new MatchResult
            {
                MatchId = CurrentMatch.MatchId,
                Outcome = outcome,
                FinalScore = CurrentMatch.LocalPlayerScore,
                FinalRank = outcome == MatchOutcome.Win ? 1 : 2,
                PrizeWon = outcome == MatchOutcome.Win ? CurrentMatch.PrizePool * 0.95m : 0,
                Currency = CurrentMatch.Currency,
                DurationSeconds = CurrentMatch.StartTime.HasValue
                    ? (float)(DateTime.UtcNow - CurrentMatch.StartTime.Value).TotalSeconds
                    : 0,
                XpEarned = outcome == MatchOutcome.Win ? 100 : 25
            };

            Manager.EndCurrentMatch(result);
        }

        private static IEnumerator EndMatchOnServer()
        {
            // [P1-3.7.0] There is no "end match" endpoint: the final score submit is the
            // terminal call and the platform settles the table (results arrive via
            // tournament:* / match:* socket events and the site). Close locally.
            var result = new MatchResult
            {
                MatchId = CurrentMatch.MatchId,
                Outcome = MatchOutcome.Pending,
                FinalScore = CurrentMatch.LocalPlayerScore,
                PrizeWon = 0,
                Currency = CurrentMatch.Currency,
                DurationSeconds = CurrentMatch.StartTime.HasValue
                    ? (float)(DateTime.UtcNow - CurrentMatch.StartTime.Value).TotalSeconds
                    : 0
            };
            Manager.EndCurrentMatch(result);
            yield break;
        }
    }

    // =============================================================================
    // UI INTERFACE
    // =============================================================================

    /// <summary>
    /// Interface for custom match UI implementations.
    /// </summary>
    public interface IDeskillzMatchUI
    {
        void ShowMatchStart(MatchData match);
        void UpdateScore(int myScore, int opponentScore);
        void ShowCountdown(int seconds);
        void ShowResults(MatchResult result);
        void Hide();
    }
}

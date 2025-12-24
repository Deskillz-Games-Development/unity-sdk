// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;

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
                var message = NetworkMessage.Create(MessageType.StateSync, new { score });
                message.SenderId = CurrentPlayer.Id;
                Network.Send(message);
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
                Manager.Run(Network.Post<object>(
                    "/sdk/report",
                    new { playerId, reason, matchId = CurrentMatch?.MatchId },
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
                if (!IsConnected)
                {
                    DeskillzLogger.Warning("Cannot send message: not connected");
                    return;
                }

                var message = NetworkMessage.Create(MessageType.Custom, data);
                message.SenderId = CurrentPlayer?.Id;
                Network.Send(message);
            }

            /// <summary>
            /// Send a message to a specific player.
            /// </summary>
            public static void SendToPlayer<T>(string playerId, T data)
            {
                if (!IsConnected)
                {
                    DeskillzLogger.Warning("Cannot send message: not connected");
                    return;
                }

                var message = NetworkMessage.Create(MessageType.Custom, data, playerId);
                message.SenderId = CurrentPlayer?.Id;
                Network.Send(message);
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

                var message = NetworkMessage.Create(MessageType.StateSync, state);
                message.SenderId = CurrentPlayer?.Id;
                Network.Send(message);
            }
        }

        // =============================================================================
        // CUSTOM STAGE (Nested Class)
        // =============================================================================

        /// <summary>
        /// Custom stage/room functionality.
        /// </summary>
        public static class Stage
        {
            /// <summary>
            /// Current stage data (if in a stage).
            /// </summary>
            public static StageData CurrentStage => CurrentMatch?.Stage;

            /// <summary>
            /// Whether currently in a custom stage.
            /// </summary>
            public static bool IsInStage => CurrentStage != null;

            /// <summary>
            /// Whether local player is the stage admin.
            /// </summary>
            public static bool IsAdmin => CurrentStage?.IsLocalPlayerAdmin ?? false;

            /// <summary>
            /// Create a new custom stage.
            /// </summary>
            public static void Create(StageConfig config, Action<StageData> onSuccess = null, Action<DeskillzError> onError = null)
            {
                if (!IsReady)
                {
                    onError?.Invoke(new DeskillzError(ErrorCode.NotInitialized, "SDK not ready"));
                    return;
                }

                if (!config.IsValid(out var validationError))
                {
                    onError?.Invoke(new DeskillzError(ErrorCode.Unknown, validationError));
                    return;
                }

                if (TestMode)
                {
                    Manager.Run(SimulateStageCreation(config, onSuccess));
                }
                else
                {
                    Manager.Run(Network.Post<StageData>(
                        "/sdk/stage/create",
                        config,
                        stage =>
                        {
                            HandleStageJoined(stage);
                            onSuccess?.Invoke(stage);
                        },
                        onError
                    ));
                }
            }

            /// <summary>
            /// Join a stage by invite code.
            /// </summary>
            public static void Join(string code, Action<StageData> onSuccess = null, Action<DeskillzError> onError = null)
            {
                if (!IsReady)
                {
                    onError?.Invoke(new DeskillzError(ErrorCode.NotInitialized, "SDK not ready"));
                    return;
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    onError?.Invoke(new DeskillzError(ErrorCode.InvalidStageCode, "Invalid stage code"));
                    return;
                }

                if (TestMode)
                {
                    onError?.Invoke(new DeskillzError(ErrorCode.StageNotFound, "Stages not available in test mode"));
                }
                else
                {
                    Manager.Run(Network.Post<StageData>(
                        "/sdk/stage/join",
                        new { code = code.ToUpper().Trim() },
                        stage =>
                        {
                            HandleStageJoined(stage);
                            onSuccess?.Invoke(stage);
                        },
                        onError
                    ));
                }
            }

            /// <summary>
            /// Leave current stage.
            /// </summary>
            public static void Leave()
            {
                if (!IsInStage) return;

                DeskillzLogger.LogEvent("Leaving stage", CurrentStage.Name);

                if (!TestMode)
                {
                    Manager.Run(Network.Post<object>(
                        "/sdk/stage/leave",
                        new { stageId = CurrentStage.StageId },
                        _ => { },
                        error => DeskillzLogger.Error($"Failed to leave stage: {error.Message}")
                    ));
                }

                Network?.Disconnect();
                DeskillzEvents.RaiseStageLeft();
            }

            /// <summary>
            /// Get players in current stage.
            /// </summary>
            public static StagePlayer[] GetPlayers()
            {
                return CurrentStage?.Players?.ToArray() ?? Array.Empty<StagePlayer>();
            }

            /// <summary>
            /// Invite a player to the stage (by player ID).
            /// </summary>
            public static void Invite(string playerId)
            {
                if (!IsInStage || !IsAdmin) return;

                Manager.Run(Network.Post<object>(
                    "/sdk/stage/invite",
                    new { stageId = CurrentStage.StageId, playerId },
                    _ => DeskillzLogger.Info($"Invited player {playerId}"),
                    error => DeskillzLogger.Error($"Failed to invite: {error.Message}")
                ));
            }

            /// <summary>
            /// Kick a player from the stage (Admin only).
            /// </summary>
            public static void Kick(string playerId)
            {
                if (!IsInStage || !IsAdmin) return;

                Manager.Run(Network.Post<object>(
                    "/sdk/stage/kick",
                    new { stageId = CurrentStage.StageId, playerId },
                    _ => DeskillzLogger.Info($"Kicked player {playerId}"),
                    error => DeskillzLogger.Error($"Failed to kick: {error.Message}")
                ));
            }

            /// <summary>
            /// Start the match (Admin only).
            /// </summary>
            public static void StartMatch()
            {
                if (!IsInStage || !IsAdmin) return;

                Manager.Run(Network.Post<MatchData>(
                    "/sdk/stage/start",
                    new { stageId = CurrentStage.StageId },
                    match =>
                    {
                        Manager.UpdateMatch(match);
                        DeskillzEvents.RaiseStageMatchStarting(CurrentStage);
                    },
                    error => DeskillzLogger.Error($"Failed to start match: {error.Message}")
                ));
            }

            /// <summary>
            /// Update stage configuration (Admin only).
            /// </summary>
            public static void UpdateConfig(StageConfig config)
            {
                if (!IsInStage || !IsAdmin) return;

                if (!config.IsValid(out var error))
                {
                    DeskillzLogger.Error($"Invalid config: {error}");
                    return;
                }

                Manager.Run(Network.Post<StageData>(
                    "/sdk/stage/update",
                    new { stageId = CurrentStage.StageId, config },
                    stage => DeskillzEvents.RaiseStageUpdated(stage),
                    err => DeskillzLogger.Error($"Failed to update stage: {err.Message}")
                ));
            }

            private static void HandleStageJoined(StageData stage)
            {
                // Create match data for the stage
                var match = new MatchData
                {
                    MatchId = $"stage_{stage.StageId}",
                    GameId = Config.GameId,
                    Mode = MatchMode.CustomStage,
                    Status = MatchStatus.Pending,
                    EntryFee = stage.EntryFee,
                    Currency = stage.Currency,
                    TimeLimitSeconds = stage.TimeLimitSeconds,
                    Rounds = stage.Rounds,
                    Stage = stage
                };

                Manager.UpdateMatch(match);
                Network?.Connect(stage.StageId);
                DeskillzEvents.RaiseStageJoined(stage);
            }

            private static IEnumerator SimulateStageCreation(StageConfig config, Action<StageData> onSuccess)
            {
                yield return new WaitForSeconds(0.5f);

                var stage = new StageData
                {
                    StageId = "test_stage_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    InviteCode = GenerateInviteCode(),
                    Name = config.Name,
                    CreatorId = CurrentPlayer.Id,
                    AdminId = CurrentPlayer.Id,
                    Visibility = config.Visibility,
                    MaxPlayers = config.MaxPlayers,
                    CurrentPlayers = 1,
                    EntryFee = config.EntryFee,
                    Currency = config.Currency,
                    Rounds = config.Rounds,
                    TimeLimitSeconds = config.TimeLimitSeconds,
                    CreatedAt = DateTime.UtcNow,
                    IsWaiting = true,
                    IsLocalPlayerAdmin = true
                };

                stage.Players.Add(new StagePlayer
                {
                    PlayerId = CurrentPlayer.Id,
                    Username = CurrentPlayer.Username,
                    IsAdmin = true,
                    IsLocalPlayer = true,
                    IsReady = true,
                    JoinedAt = DateTime.UtcNow
                });

                HandleStageJoined(stage);
                onSuccess?.Invoke(stage);
            }

            private static string GenerateInviteCode()
            {
                const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                var code = new char[8];
                var random = new System.Random();
                for (int i = 0; i < 8; i++)
                {
                    code[i] = chars[random.Next(chars.Length)];
                    if (i == 3) code[i] = '-'; // Format: ABCD-1234
                }
                return new string(code);
            }
        }

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
            yield return Network.Post<object>(
                "/sdk/match/score",
                new
                {
                    matchId = CurrentMatch.MatchId,
                    score,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                },
                _ => DeskillzEvents.RaiseScoreSubmitted(score),
                error =>
                {
                    DeskillzLogger.Error($"Score submission failed: {error.Message}");
                    
                    // Queue for retry if recoverable
                    if (error.IsRecoverable)
                    {
                        DeskillzCache.QueuePendingScore(
                            CurrentMatch.MatchId,
                            score,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        );
                    }
                    
                    DeskillzEvents.RaiseScoreSubmissionFailed(error);
                }
            );
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
            yield return Network.Post<MatchResult>(
                "/sdk/match/end",
                new
                {
                    matchId = CurrentMatch.MatchId,
                    finalScore = CurrentMatch.LocalPlayerScore,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                },
                result => Manager.EndCurrentMatch(result),
                error =>
                {
                    DeskillzLogger.Error($"Failed to end match: {error.Message}");
                    DeskillzEvents.RaiseMatchCancelled(error);
                }
            );
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

// =============================================================================
// Deskillz SDK for Unity - Social Game Manager
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Deskillz.Host;
using Deskillz.Rooms;

namespace Deskillz.Social
{
    /// <summary>
    /// Main API for social game session management in the Deskillz SDK.
    /// Handles rake-based games like Big 2, Mahjong, and 13-Card Poker.
    /// 
    /// Usage:
    /// <code>
    /// // Get current session
    /// SocialGameManager.GetSession(
    ///     session => Debug.Log($"Round: {session.CurrentRound}"),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Subscribe to events
    /// SocialGameManager.OnRoundCompleted += result => 
    ///     Debug.Log($"Winner: {result.WinnerUsername}");
    /// </code>
    /// </summary>
    public static class SocialGameManager
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Fired when a social game session is joined</summary>
        public static event Action<SocialGameSession> OnSessionJoined;

        /// <summary>Fired when session data is updated</summary>
        public static event Action<SocialGameSession> OnSessionUpdated;

        /// <summary>Fired when session ends</summary>
        public static event Action<SessionEndSummary> OnSessionEnded;

        /// <summary>Fired when a round starts</summary>
        public static event Action<int> OnRoundStarted;

        /// <summary>Fired when a round completes</summary>
        public static event Action<RoundResult> OnRoundCompleted;

        /// <summary>Fired when it becomes the local player's turn</summary>
        public static event Action<float> OnTurnStarted;

        /// <summary>Fired when turn timer updates</summary>
        public static event Action<float> OnTurnTimerTick;

        /// <summary>Fired when turn timer expires</summary>
        public static event Action OnTurnTimerExpired;

        /// <summary>Fired when a pause is requested</summary>
        public static event Action<PauseStatusResponse> OnPauseRequested;

        /// <summary>Fired when pause vote updates</summary>
        public static event Action<PauseStatusResponse> OnPauseVoteUpdated;

        /// <summary>Fired when game is paused</summary>
        public static event Action<float> OnGamePaused;

        /// <summary>Fired when game resumes</summary>
        public static event Action OnGameResumed;

        /// <summary>Fired when settlement occurs</summary>
        public static event Action<Settlement> OnSettlementReceived;

        /// <summary>Fired when a player's balance changes</summary>
        public static event Action<string, decimal> OnPlayerBalanceChanged;

        /// <summary>Fired when a player busts</summary>
        public static event Action<SocialPlayer> OnPlayerBusted;

        /// <summary>Fired when a player rebuys</summary>
        public static event Action<SocialPlayer, decimal> OnPlayerRebuy;

        /// <summary>Fired when a player leaves</summary>
        public static event Action<string> OnPlayerLeft;

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const string SOCIAL_ENDPOINT = "/api/v1/social";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Current social game session</summary>
        public static SocialGameSession CurrentSession { get; private set; }

        /// <summary>Whether currently in a social game session</summary>
        public static bool IsInSession => CurrentSession != null;

        /// <summary>Whether it's the local player's turn</summary>
        public static bool IsMyTurn => CurrentSession?.IsMyTurn ?? false;

        /// <summary>Whether the game is currently paused</summary>
        public static bool IsPaused => CurrentSession?.PauseStatus == PauseVoteStatus.Paused;

        /// <summary>Whether the manager is initialized</summary>
        public static bool IsInitialized { get; private set; }

        private static Coroutine _turnTimerCoroutine;
        private static Coroutine _pauseTimerCoroutine;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the social game manager. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;

            IsInitialized = true;
            DeskillzLogger.Debug("[SocialGameManager] Initialized");
        }

        /// <summary>
        /// Shutdown the social game manager.
        /// </summary>
        internal static void Shutdown()
        {
            if (!IsInitialized) return;

            StopTimers();
            CurrentSession = null;
            BuyInManager.Reset();
            ClearAllSubscriptions();
            IsInitialized = false;

            DeskillzLogger.Debug("[SocialGameManager] Shutdown");
        }

        // =====================================================================
        // SESSION MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Get the current social game session.
        /// </summary>
        /// <param name="onSuccess">Called with session on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetSession(
            Action<SocialGameSession> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateInRoom(onError)) return;

            var roomId = DeskillzRooms.CurrentRoom?.Id;

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<SocialGameSession>(
                    $"{SOCIAL_ENDPOINT}/session/{roomId}",
                    session =>
                    {
                        SetSession(session);
                        onSuccess?.Invoke(session);
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Join a social game session in the current room.
        /// Called automatically when joining a room with social game settings.
        /// </summary>
        /// <param name="buyInAmount">Initial buy-in amount</param>
        /// <param name="currency">Currency for buy-in</param>
        /// <param name="onSuccess">Called with session on success</param>
        /// <param name="onError">Called on error</param>
        public static void JoinSession(
            decimal buyInAmount,
            string currency,
            Action<SocialGameSession> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;
            if (!ValidateInRoom(onError)) return;

            var roomId = DeskillzRooms.CurrentRoom?.Id;

            var request = new JoinSessionRequest
            {
                roomId = roomId,
                buyInAmount = buyInAmount,
                currency = currency
            };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<SocialGameSession>(
                    $"{SOCIAL_ENDPOINT}/session/join",
                    JsonUtility.ToJson(request),
                    session =>
                    {
                        SetSession(session);
                        OnSessionJoined?.Invoke(session);
                        onSuccess?.Invoke(session);
                        DeskillzLogger.Debug($"[SocialGameManager] Joined session: {session.Id}");
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Leave the current social game session.
        /// Can only be called between rounds.
        /// </summary>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void LeaveSession(
            Action onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!IsInSession)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                ));
                return;
            }

            // Cash out first
            BuyInManager.CashOut(
                response =>
                {
                    StopTimers();
                    CurrentSession = null;
                    BuyInManager.Reset();
                    onSuccess?.Invoke();
                    DeskillzLogger.Debug("[SocialGameManager] Left session");
                },
                error =>
                {
                    onError?.Invoke(error);
                }
            );
        }

        // =====================================================================
        // ROUND MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Submit round result (game developer calls this when round ends).
        /// The winner is determined by the game logic.
        /// </summary>
        /// <param name="winnerId">Player ID of the winner</param>
        /// <param name="scores">Dictionary of player ID to score</param>
        /// <param name="onSuccess">Called with round result on success</param>
        /// <param name="onError">Called on error</param>
        public static void SubmitRoundResult(
            string winnerId,
            Dictionary<string, int> scores,
            Action<RoundResult> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!IsInSession)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                ));
                return;
            }

            var request = new SubmitRoundRequest
            {
                sessionId = CurrentSession.Id,
                winnerId = winnerId,
                scores = scores
            };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<RoundResult>(
                    $"{SOCIAL_ENDPOINT}/round/submit",
                    JsonUtility.ToJson(request),
                    result =>
                    {
                        // Update session state
                        if (CurrentSession != null)
                        {
                            CurrentSession.CurrentRound++;
                            CurrentSession.TotalRounds++;
                            CurrentSession.TotalPotVolume += result.TotalPot;
                            CurrentSession.TotalRake += result.RakeTaken;
                        }

                        // Update player balances
                        foreach (var score in result.PlayerScores)
                        {
                            UpdatePlayerBalance(score.PlayerId, score.BalanceAfter);
                        }

                        OnRoundCompleted?.Invoke(result);
                        onSuccess?.Invoke(result);
                        DeskillzLogger.Debug($"[SocialGameManager] Round {result.RoundNumber} complete, Winner: {result.WinnerUsername}");
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Signal that a new round is starting.
        /// </summary>
        /// <param name="roundNumber">Round number</param>
        public static void StartRound(int roundNumber)
        {
            if (CurrentSession != null)
            {
                CurrentSession.CurrentRound = roundNumber;
            }

            OnRoundStarted?.Invoke(roundNumber);
            DeskillzLogger.Debug($"[SocialGameManager] Round {roundNumber} started");
        }

        // =====================================================================
        // TURN MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Signal that it's the local player's turn.
        /// Starts the turn timer if enabled.
        /// </summary>
        /// <param name="timeLimit">Time limit in seconds (0 = no limit)</param>
        public static void StartMyTurn(float timeLimit = 0)
        {
            if (CurrentSession != null)
            {
                CurrentSession.IsMyTurn = true;
                CurrentSession.TurnTimeRemaining = timeLimit;
            }

            OnTurnStarted?.Invoke(timeLimit);

            if (timeLimit > 0)
            {
                StartTurnTimer(timeLimit);
            }

            DeskillzLogger.Debug($"[SocialGameManager] My turn started, time: {timeLimit}s");
        }

        /// <summary>
        /// Signal that the local player's turn has ended.
        /// </summary>
        public static void EndMyTurn()
        {
            if (CurrentSession != null)
            {
                CurrentSession.IsMyTurn = false;
                CurrentSession.TurnTimeRemaining = 0;
            }

            StopTurnTimer();
            DeskillzLogger.Debug("[SocialGameManager] My turn ended");
        }

        private static void StartTurnTimer(float duration)
        {
            StopTurnTimer();
            _turnTimerCoroutine = DeskillzManager.Instance.StartCoroutine(
                TurnTimerCoroutine(duration)
            );
        }

        private static void StopTurnTimer()
        {
            if (_turnTimerCoroutine != null)
            {
                DeskillzManager.Instance.StopCoroutine(_turnTimerCoroutine);
                _turnTimerCoroutine = null;
            }
        }

        private static IEnumerator TurnTimerCoroutine(float duration)
        {
            float remaining = duration;
            float lastTick = duration;

            while (remaining > 0)
            {
                yield return null;
                remaining -= Time.deltaTime;

                if (CurrentSession != null)
                {
                    CurrentSession.TurnTimeRemaining = remaining;
                }

                // Tick every second
                if (Mathf.Floor(remaining) < Mathf.Floor(lastTick))
                {
                    OnTurnTimerTick?.Invoke(remaining);
                    lastTick = remaining;
                }
            }

            if (CurrentSession != null)
            {
                CurrentSession.TurnTimeRemaining = 0;
            }

            OnTurnTimerExpired?.Invoke();
            DeskillzLogger.Debug("[SocialGameManager] Turn timer expired");
        }

        // =====================================================================
        // PAUSE MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Request a pause in the game.
        /// Requires all players to vote to approve.
        /// </summary>
        /// <param name="reason">Optional reason for pause</param>
        /// <param name="onSuccess">Called with pause status on success</param>
        /// <param name="onError">Called on error</param>
        public static void RequestPause(
            string reason = null,
            Action<PauseStatusResponse> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!IsInSession)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                ));
                return;
            }

            var request = new PauseRequest
            {
                roomId = CurrentSession.RoomId,
                reason = reason
            };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<PauseStatusResponse>(
                    $"{SOCIAL_ENDPOINT}/pause/request",
                    JsonUtility.ToJson(request),
                    status =>
                    {
                        if (CurrentSession != null)
                        {
                            CurrentSession.PauseStatus = status.Status;
                        }
                        OnPauseRequested?.Invoke(status);
                        onSuccess?.Invoke(status);
                        DeskillzLogger.Debug("[SocialGameManager] Pause requested");
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Vote on a pause request.
        /// </summary>
        /// <param name="approve">Whether to approve the pause</param>
        /// <param name="onSuccess">Called with updated status on success</param>
        /// <param name="onError">Called on error</param>
        public static void VotePause(
            bool approve,
            Action<PauseStatusResponse> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!IsInSession)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                ));
                return;
            }

            var request = new PauseVote
            {
                roomId = CurrentSession.RoomId,
                approve = approve
            };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<PauseStatusResponse>(
                    $"{SOCIAL_ENDPOINT}/pause/vote",
                    JsonUtility.ToJson(request),
                    status =>
                    {
                        if (CurrentSession != null)
                        {
                            CurrentSession.PauseStatus = status.Status;
                        }
                        OnPauseVoteUpdated?.Invoke(status);

                        if (status.Status == PauseVoteStatus.Paused)
                        {
                            StartPauseTimer(status.PauseTimeRemaining);
                            OnGamePaused?.Invoke(status.PauseTimeRemaining);
                        }

                        onSuccess?.Invoke(status);
                        DeskillzLogger.Debug($"[SocialGameManager] Pause vote: {approve}");
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Resume from pause (host only or automatic after timeout).
        /// </summary>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void ResumePause(
            Action onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!IsInSession)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                ));
                return;
            }

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<object>(
                    $"{SOCIAL_ENDPOINT}/pause/resume",
                    JsonUtility.ToJson(new { roomId = CurrentSession.RoomId }),
                    _ =>
                    {
                        if (CurrentSession != null)
                        {
                            CurrentSession.PauseStatus = PauseVoteStatus.None;
                        }
                        StopPauseTimer();
                        OnGameResumed?.Invoke();
                        onSuccess?.Invoke();
                        DeskillzLogger.Debug("[SocialGameManager] Game resumed");
                    },
                    onError
                )
            );
        }

        private static void StartPauseTimer(float duration)
        {
            StopPauseTimer();
            _pauseTimerCoroutine = DeskillzManager.Instance.StartCoroutine(
                PauseTimerCoroutine(duration)
            );
        }

        private static void StopPauseTimer()
        {
            if (_pauseTimerCoroutine != null)
            {
                DeskillzManager.Instance.StopCoroutine(_pauseTimerCoroutine);
                _pauseTimerCoroutine = null;
            }
        }

        private static IEnumerator PauseTimerCoroutine(float duration)
        {
            float remaining = duration;

            while (remaining > 0)
            {
                yield return new WaitForSeconds(1f);
                remaining -= 1f;

                if (CurrentSession != null)
                {
                    CurrentSession.PauseTimeRemaining = remaining;
                }
            }

            // Auto-resume when pause expires
            if (CurrentSession?.PauseStatus == PauseVoteStatus.Paused)
            {
                ResumePause();
            }
        }

        private static void StopTimers()
        {
            StopTurnTimer();
            StopPauseTimer();
        }

        // =====================================================================
        // RAKE & SETTLEMENT
        // =====================================================================

        /// <summary>
        /// Get rake preview for a potential pot amount.
        /// </summary>
        /// <param name="potAmount">Pot amount</param>
        /// <returns>Rake calculation result</returns>
        public static RakeResult PreviewRake(decimal potAmount)
        {
            if (CurrentSession == null)
            {
                return RakeCalculator.CalculateRake(potAmount);
            }

            return RakeCalculator.CalculateRake(
                potAmount,
                CurrentSession.RakePercent,
                CurrentSession.RakeCap
            );
        }

        /// <summary>
        /// Get accumulated rake for current session.
        /// </summary>
        /// <returns>Accumulated rake since last settlement</returns>
        public static decimal GetAccumulatedRake()
        {
            return CurrentSession?.AccumulatedRake ?? 0;
        }

        /// <summary>
        /// Manually trigger settlement (host only).
        /// </summary>
        /// <param name="onSuccess">Called with settlement on success</param>
        /// <param name="onError">Called on error</param>
        public static void TriggerSettlement(
            Action<Settlement> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            EnsureInitialized();

            if (!IsInSession)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                ));
                return;
            }

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<Settlement>(
                    $"{SOCIAL_ENDPOINT}/settlement/trigger",
                    JsonUtility.ToJson(new { sessionId = CurrentSession.Id }),
                    settlement =>
                    {
                        if (CurrentSession != null)
                        {
                            CurrentSession.AccumulatedRake = 0;
                            CurrentSession.LastSettlementAt = DateTime.UtcNow;
                        }
                        OnSettlementReceived?.Invoke(settlement);
                        onSuccess?.Invoke(settlement);
                        DeskillzLogger.Debug($"[SocialGameManager] Settlement triggered: ${settlement.TotalRake}");
                    },
                    onError
                )
            );
        }

        // =====================================================================
        // PLAYER MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Get local player in current session.
        /// </summary>
        /// <returns>Local player or null</returns>
        public static SocialPlayer GetLocalPlayer()
        {
            if (CurrentSession?.Players == null) return null;

            foreach (var player in CurrentSession.Players)
            {
                if (player.IsLocalPlayer)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Get player by ID.
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <returns>Player or null</returns>
        public static SocialPlayer GetPlayer(string playerId)
        {
            if (CurrentSession?.Players == null) return null;

            foreach (var player in CurrentSession.Players)
            {
                if (player.Id == playerId)
                {
                    return player;
                }
            }
            return null;
        }

        private static void UpdatePlayerBalance(string playerId, decimal newBalance)
        {
            var player = GetPlayer(playerId);
            if (player != null)
            {
                decimal oldBalance = player.Balance;
                player.Balance = newBalance;

                OnPlayerBalanceChanged?.Invoke(playerId, newBalance);

                // Check for bust
                if (oldBalance > 0 && newBalance <= 0)
                {
                    player.Status = SocialPlayerStatus.Busted;
                    OnPlayerBusted?.Invoke(player);
                }

                // Update BuyInManager if local player
                if (player.IsLocalPlayer)
                {
                    BuyInManager.UpdateBalance(newBalance);
                }
            }
        }

        // =====================================================================
        // INTERNAL HELPERS
        // =====================================================================

        private static void SetSession(SocialGameSession session)
        {
            CurrentSession = session;

            if (session != null)
            {
                // Link players to session
                foreach (var player in session.Players)
                {
                    player.CurrentSession = session;
                    player.IsLocalPlayer = player.Id == DeskillzManager.Instance?.CurrentPlayer?.Id;
                }

                // Initialize BuyInManager
                var localPlayer = GetLocalPlayer();
                BuyInManager.Initialize(
                    session.RoomId,
                    session.PointValue,
                    localPlayer?.Balance ?? 0
                );
            }

            OnSessionUpdated?.Invoke(session);
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }

        private static bool ValidateAuthentication(Action<SocialGameError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotAuthenticated,
                    "Not authenticated"
                ));
                return false;
            }
            return true;
        }

        private static bool ValidateInRoom(Action<SocialGameError> onError)
        {
            if (!DeskillzRooms.IsInRoom)
            {
                onError?.Invoke(new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a room"
                ));
                return false;
            }
            return true;
        }

        // =====================================================================
        // HTTP HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<SocialGameError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Get(url);
            SetupRequest(request);

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator PostRequest<T>(
            string endpoint,
            string json,
            Action<T> onSuccess,
            Action<SocialGameError> onError)
        {
            var url = GetFullUrl(endpoint);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        private static string GetFullUrl(string endpoint)
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? "https://api.deskillz.games";
            return $"{baseUrl}{endpoint}";
        }

        private static void SetupRequest(UnityWebRequest request)
        {
            request.timeout = REQUEST_TIMEOUT;

            var token = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            var gameId = DeskillzManager.Instance?.Config?.GameId;
            if (!string.IsNullOrEmpty(gameId))
            {
                request.SetRequestHeader("X-Game-Id", gameId);
            }

            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request,
            Action<T> onSuccess,
            Action<SocialGameError> onError)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse<T>>(request.downloadHandler.text);
                    if (response.success)
                    {
                        onSuccess?.Invoke(response.data);
                    }
                    else
                    {
                        onError?.Invoke(new SocialGameError(
                            SocialGameError.Codes.ServerError,
                            response.error ?? "Unknown error"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke(new SocialGameError(
                        SocialGameError.Codes.ServerError,
                        $"Parse error: {ex.Message}"
                    ));
                }
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        private static SocialGameError ParseError(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return new SocialGameError(SocialGameError.Codes.NetworkError, "Network error");
            }
            return new SocialGameError(SocialGameError.Codes.ServerError, $"Error: {request.responseCode}");
        }

        // =====================================================================
        // INTERNAL TYPES
        // =====================================================================

        [Serializable]
        private class ApiResponse<T>
        {
            public bool success;
            public T data;
            public string error;
        }

        [Serializable]
        private class JoinSessionRequest
        {
            public string roomId;
            public decimal buyInAmount;
            public string currency;
        }

        [Serializable]
        private class SubmitRoundRequest
        {
            public string sessionId;
            public string winnerId;
            public Dictionary<string, int> scores;
        }

        // =====================================================================
        // WEBSOCKET HANDLERS (Internal)
        // =====================================================================

        internal static void HandleSessionUpdate(SocialGameSession session)
        {
            SetSession(session);
        }

        internal static void HandleRoundResult(RoundResult result)
        {
            OnRoundCompleted?.Invoke(result);
        }

        internal static void HandleSettlement(Settlement settlement)
        {
            if (CurrentSession != null)
            {
                CurrentSession.AccumulatedRake = 0;
                CurrentSession.LastSettlementAt = DateTime.UtcNow;
            }
            OnSettlementReceived?.Invoke(settlement);
        }

        internal static void HandlePlayerLeft(string playerId)
        {
            if (CurrentSession?.Players != null)
            {
                for (int i = CurrentSession.Players.Count - 1; i >= 0; i--)
                {
                    if (CurrentSession.Players[i].Id == playerId)
                    {
                        CurrentSession.Players[i].Status = SocialPlayerStatus.Left;
                        break;
                    }
                }
            }
            OnPlayerLeft?.Invoke(playerId);
        }

        internal static void HandleSessionEnd(SessionEndSummary summary)
        {
            StopTimers();
            OnSessionEnded?.Invoke(summary);
            CurrentSession = null;
            BuyInManager.Reset();
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        private static void ClearAllSubscriptions()
        {
            OnSessionJoined = null;
            OnSessionUpdated = null;
            OnSessionEnded = null;
            OnRoundStarted = null;
            OnRoundCompleted = null;
            OnTurnStarted = null;
            OnTurnTimerTick = null;
            OnTurnTimerExpired = null;
            OnPauseRequested = null;
            OnPauseVoteUpdated = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnSettlementReceived = null;
            OnPlayerBalanceChanged = null;
            OnPlayerBusted = null;
            OnPlayerRebuy = null;
            OnPlayerLeft = null;

            BuyInManager.ClearAllSubscriptions();
        }
    }
}
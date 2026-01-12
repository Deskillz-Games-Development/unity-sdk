// =============================================================================
// Deskillz SDK for Unity - Spectator Manager
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Spectator
{
    /// <summary>
    /// Main API for spectator mode in the Deskillz SDK.
    /// Allows users to watch ongoing games in real-time.
    /// 
    /// Usage:
    /// <code>
    /// // Get rooms available to spectate
    /// SpectatorManager.GetSpectatorRooms(
    ///     rooms => foreach(var r in rooms) Debug.Log(r.Name),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Join as spectator
    /// SpectatorManager.JoinAsSpectator("room-123",
    ///     session => Debug.Log($"Watching: {session.RoomName}"),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Subscribe to events
    /// SpectatorManager.OnGameStateUpdated += state => UpdateDisplay(state);
    /// </code>
    /// </summary>
    public static class SpectatorManager
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Fired when connected to a room as spectator</summary>
        public static event Action<SpectatorSession> OnConnected;

        /// <summary>Fired when disconnected from spectating</summary>
        public static event Action<string> OnDisconnected;

        /// <summary>Fired when connection state changes</summary>
        public static event Action<SpectatorState> OnStateChanged;

        /// <summary>Fired when game state is updated</summary>
        public static event Action<GameStateSnapshot> OnGameStateUpdated;

        /// <summary>Fired when a game action occurs</summary>
        public static event Action<GameAction> OnGameAction;

        /// <summary>Fired when scores update</summary>
        public static event Action<ScoreUpdate> OnScoreUpdated;

        /// <summary>Fired when a round ends</summary>
        public static event Action<SpectatorRoundEnd> OnRoundEnded;

        /// <summary>Fired when a new round starts</summary>
        public static event Action<int> OnRoundStarted;

        /// <summary>Fired when player info updates</summary>
        public static event Action<SpectatorPlayerInfo> OnPlayerUpdated;

        /// <summary>Fired when a player joins the game</summary>
        public static event Action<SpectatorPlayerInfo> OnPlayerJoined;

        /// <summary>Fired when a player leaves the game</summary>
        public static event Action<string> OnPlayerLeft;

        /// <summary>Fired when chat message received</summary>
        public static event Action<SpectatorChatMessage> OnChatReceived;

        /// <summary>Fired when spectator count changes</summary>
        public static event Action<int> OnSpectatorCountChanged;

        /// <summary>Fired when game is paused</summary>
        public static event Action<float> OnGamePaused;

        /// <summary>Fired when game resumes</summary>
        public static event Action OnGameResumed;

        /// <summary>Fired when game ends</summary>
        public static event Action OnGameEnded;

        /// <summary>Fired on connection error</summary>
        public static event Action<SpectatorError> OnError;

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const string SPECTATOR_ENDPOINT = "/api/v1/spectator";
        private const int REQUEST_TIMEOUT = 30;
        private const float RECONNECT_DELAY = 3f;
        private const int MAX_RECONNECT_ATTEMPTS = 5;

        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Current spectator session</summary>
        public static SpectatorSession CurrentSession { get; private set; }

        /// <summary>Current connection state</summary>
        public static SpectatorState State { get; private set; } = SpectatorState.Disconnected;

        /// <summary>Whether currently spectating</summary>
        public static bool IsSpectating => State == SpectatorState.Connected && CurrentSession != null;

        /// <summary>Current view mode</summary>
        public static SpectatorViewMode ViewMode { get; private set; } = SpectatorViewMode.Overview;

        /// <summary>Currently followed player ID (if in FollowPlayer mode)</summary>
        public static string FollowedPlayerId { get; private set; }

        /// <summary>Whether the manager is initialized</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>List of rooms being watched (multi-room support)</summary>
        public static List<SpectatorSession> WatchedRooms { get; private set; } = new List<SpectatorSession>();

        /// <summary>Maximum rooms that can be watched simultaneously</summary>
        public const int MAX_WATCHED_ROOMS = 4;

        private static Rooms.RoomWebSocket _webSocket;
        private static int _reconnectAttempts;
        private static Coroutine _reconnectCoroutine;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the spectator manager. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;

            IsInitialized = true;
            DeskillzLogger.Debug("[SpectatorManager] Initialized");
        }

        /// <summary>
        /// Shutdown the spectator manager.
        /// </summary>
        internal static void Shutdown()
        {
            if (!IsInitialized) return;

            LeaveAllRooms();
            ClearAllSubscriptions();
            IsInitialized = false;

            DeskillzLogger.Debug("[SpectatorManager] Shutdown");
        }

        // =====================================================================
        // ROOM DISCOVERY
        // =====================================================================

        /// <summary>
        /// Get rooms available for spectating.
        /// </summary>
        /// <param name="gameId">Optional game ID to filter by</param>
        /// <param name="onSuccess">Called with list of rooms on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetSpectatorRooms(
            string gameId = null,
            Action<List<SpectatorRoom>> onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            EnsureInitialized();

            var endpoint = SPECTATOR_ENDPOINT + "/rooms";
            if (!string.IsNullOrEmpty(gameId))
            {
                endpoint += $"?gameId={gameId}";
            }

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<SpectatorRoomsResponse>(
                    endpoint,
                    response => onSuccess?.Invoke(response?.rooms ?? new List<SpectatorRoom>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get spectator rooms for a specific game.
        /// </summary>
        /// <param name="gameId">Game ID</param>
        /// <param name="onSuccess">Called with rooms on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetRoomsForGame(
            string gameId,
            Action<List<SpectatorRoom>> onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            GetSpectatorRooms(gameId, onSuccess, onError);
        }

        /// <summary>
        /// Get room details for spectating.
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="onSuccess">Called with room details on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetRoomDetails(
            string roomId,
            Action<SpectatorRoom> onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            EnsureInitialized();

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<SpectatorRoom>(
                    $"{SPECTATOR_ENDPOINT}/rooms/{roomId}",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // SPECTATING
        // =====================================================================

        /// <summary>
        /// Join a room as a spectator.
        /// </summary>
        /// <param name="roomId">Room ID to spectate</param>
        /// <param name="onSuccess">Called with session on success</param>
        /// <param name="onError">Called on error</param>
        public static void JoinAsSpectator(
            string roomId,
            Action<SpectatorSession> onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            EnsureInitialized();

            if (State == SpectatorState.Connecting)
            {
                onError?.Invoke(new SpectatorError(
                    SpectatorError.Codes.AlreadySpectating,
                    "Already connecting to a room"
                ));
                return;
            }

            // Check if already watching this room
            foreach (var session in WatchedRooms)
            {
                if (session.RoomId == roomId)
                {
                    // Switch to this room as primary
                    CurrentSession = session;
                    OnConnected?.Invoke(session);
                    onSuccess?.Invoke(session);
                    return;
                }
            }

            // Check max rooms
            if (WatchedRooms.Count >= MAX_WATCHED_ROOMS)
            {
                onError?.Invoke(new SpectatorError(
                    SpectatorError.Codes.RoomFull,
                    $"Maximum {MAX_WATCHED_ROOMS} rooms can be watched simultaneously"
                ));
                return;
            }

            SetState(SpectatorState.Connecting);

            var request = new JoinSpectatorRequest { roomId = roomId };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<SpectatorSession>(
                    $"{SPECTATOR_ENDPOINT}/join",
                    JsonUtility.ToJson(request),
                    session =>
                    {
                        CurrentSession = session;
                        WatchedRooms.Add(session);
                        SetState(SpectatorState.Connected);
                        _reconnectAttempts = 0;

                        ConnectWebSocket(session.RoomId);

                        OnConnected?.Invoke(session);
                        onSuccess?.Invoke(session);

                        DeskillzLogger.Debug($"[SpectatorManager] Joined as spectator: {session.RoomCode}");
                    },
                    error =>
                    {
                        SetState(SpectatorState.Error);
                        OnError?.Invoke(error);
                        onError?.Invoke(error);
                    }
                )
            );
        }

        /// <summary>
        /// Join a room by room code as spectator.
        /// </summary>
        /// <param name="roomCode">Room code (e.g., DSKZ-AB3C)</param>
        /// <param name="onSuccess">Called with session on success</param>
        /// <param name="onError">Called on error</param>
        public static void JoinByCode(
            string roomCode,
            Action<SpectatorSession> onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            EnsureInitialized();

            // First lookup room ID by code
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<SpectatorRoom>(
                    $"{SPECTATOR_ENDPOINT}/rooms/code/{roomCode}",
                    room =>
                    {
                        JoinAsSpectator(room.Id, onSuccess, onError);
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Leave current spectator session.
        /// </summary>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void LeaveSpectator(
            Action onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            if (!IsSpectating)
            {
                onSuccess?.Invoke();
                return;
            }

            var roomId = CurrentSession.RoomId;

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<object>(
                    $"{SPECTATOR_ENDPOINT}/leave",
                    JsonUtility.ToJson(new { roomId }),
                    _ =>
                    {
                        RemoveWatchedRoom(roomId);
                        DisconnectWebSocket();

                        if (WatchedRooms.Count > 0)
                        {
                            CurrentSession = WatchedRooms[0];
                        }
                        else
                        {
                            CurrentSession = null;
                            SetState(SpectatorState.Disconnected);
                        }

                        OnDisconnected?.Invoke(roomId);
                        onSuccess?.Invoke();

                        DeskillzLogger.Debug($"[SpectatorManager] Left spectator: {roomId}");
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Leave all spectated rooms.
        /// </summary>
        public static void LeaveAllRooms()
        {
            StopReconnect();
            DisconnectWebSocket();

            foreach (var session in WatchedRooms)
            {
                OnDisconnected?.Invoke(session.RoomId);
            }

            WatchedRooms.Clear();
            CurrentSession = null;
            SetState(SpectatorState.Disconnected);

            DeskillzLogger.Debug("[SpectatorManager] Left all rooms");
        }

        // =====================================================================
        // MULTI-ROOM SUPPORT
        // =====================================================================

        /// <summary>
        /// Switch to viewing a different watched room.
        /// </summary>
        /// <param name="roomId">Room ID to switch to</param>
        /// <returns>True if switch was successful</returns>
        public static bool SwitchToRoom(string roomId)
        {
            foreach (var session in WatchedRooms)
            {
                if (session.RoomId == roomId)
                {
                    CurrentSession = session;
                    OnConnected?.Invoke(session);
                    DeskillzLogger.Debug($"[SpectatorManager] Switched to room: {roomId}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get list of currently watched rooms.
        /// </summary>
        /// <returns>List of watched room sessions</returns>
        public static List<SpectatorSession> GetWatchedRooms()
        {
            return new List<SpectatorSession>(WatchedRooms);
        }

        /// <summary>
        /// Remove a room from watched list.
        /// </summary>
        /// <param name="roomId">Room ID to remove</param>
        public static void RemoveWatchedRoom(string roomId)
        {
            for (int i = WatchedRooms.Count - 1; i >= 0; i--)
            {
                if (WatchedRooms[i].RoomId == roomId)
                {
                    WatchedRooms.RemoveAt(i);
                    break;
                }
            }
        }

        // =====================================================================
        // VIEW MODE
        // =====================================================================

        /// <summary>
        /// Set the spectator view mode.
        /// </summary>
        /// <param name="mode">View mode</param>
        /// <param name="playerId">Player ID to follow (only for FollowPlayer mode)</param>
        public static void SetViewMode(SpectatorViewMode mode, string playerId = null)
        {
            ViewMode = mode;
            FollowedPlayerId = mode == SpectatorViewMode.FollowPlayer ? playerId : null;

            DeskillzLogger.Debug($"[SpectatorManager] View mode: {mode}" + 
                (playerId != null ? $", following: {playerId}" : ""));
        }

        /// <summary>
        /// Follow a specific player.
        /// </summary>
        /// <param name="playerId">Player ID to follow</param>
        public static void FollowPlayer(string playerId)
        {
            SetViewMode(SpectatorViewMode.FollowPlayer, playerId);
        }

        /// <summary>
        /// Stop following and return to overview.
        /// </summary>
        public static void StopFollowing()
        {
            SetViewMode(SpectatorViewMode.Overview);
        }

        // =====================================================================
        // CHAT
        // =====================================================================

        /// <summary>
        /// Send a chat message in spectator chat.
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void SendChat(
            string message,
            Action onSuccess = null,
            Action<SpectatorError> onError = null)
        {
            if (!IsSpectating)
            {
                onError?.Invoke(new SpectatorError(
                    SpectatorError.Codes.NotSpectating,
                    "Not spectating any room"
                ));
                return;
            }

            if (!CurrentSession.ChatEnabled)
            {
                onError?.Invoke(new SpectatorError(
                    SpectatorError.Codes.SpectatingDisabled,
                    "Chat is disabled for this room"
                ));
                return;
            }

            var request = new SpectatorChatRequest
            {
                roomId = CurrentSession.RoomId,
                message = message
            };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<object>(
                    $"{SPECTATOR_ENDPOINT}/chat",
                    JsonUtility.ToJson(request),
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        // =====================================================================
        // WEBSOCKET CONNECTION
        // =====================================================================

        private static void ConnectWebSocket(string roomId)
        {
            // Use the existing room WebSocket with spectator channel
            _webSocket = new Rooms.RoomWebSocket();
            _webSocket.OnMessage += HandleWebSocketMessage;
            _webSocket.OnDisconnect += HandleWebSocketDisconnect;
            _webSocket.OnError += HandleWebSocketError;

            _webSocket.Connect(roomId, true); // true = spectator mode
        }

        private static void DisconnectWebSocket()
        {
            if (_webSocket != null)
            {
                _webSocket.OnMessage -= HandleWebSocketMessage;
                _webSocket.OnDisconnect -= HandleWebSocketDisconnect;
                _webSocket.OnError -= HandleWebSocketError;
                _webSocket.Disconnect();
                _webSocket = null;
            }
        }

        private static void HandleWebSocketMessage(string messageType, string data)
        {
            try
            {
                switch (messageType)
                {
                    case "game_state":
                        var state = JsonUtility.FromJson<GameStateSnapshot>(data);
                        if (CurrentSession != null)
                        {
                            CurrentSession.CurrentState = state;
                        }
                        OnGameStateUpdated?.Invoke(state);
                        break;

                    case "game_action":
                        var action = JsonUtility.FromJson<GameAction>(data);
                        OnGameAction?.Invoke(action);
                        break;

                    case "score_update":
                        var scoreUpdate = JsonUtility.FromJson<ScoreUpdate>(data);
                        OnScoreUpdated?.Invoke(scoreUpdate);
                        break;

                    case "round_end":
                        var roundEnd = JsonUtility.FromJson<SpectatorRoundEnd>(data);
                        OnRoundEnded?.Invoke(roundEnd);
                        break;

                    case "round_start":
                        var roundData = JsonUtility.FromJson<RoundStartData>(data);
                        OnRoundStarted?.Invoke(roundData.roundNumber);
                        break;

                    case "player_update":
                        var playerInfo = JsonUtility.FromJson<SpectatorPlayerInfo>(data);
                        OnPlayerUpdated?.Invoke(playerInfo);
                        break;

                    case "player_joined":
                        var newPlayer = JsonUtility.FromJson<SpectatorPlayerInfo>(data);
                        CurrentSession?.Players.Add(newPlayer);
                        OnPlayerJoined?.Invoke(newPlayer);
                        break;

                    case "player_left":
                        var leftData = JsonUtility.FromJson<PlayerLeftData>(data);
                        OnPlayerLeft?.Invoke(leftData.playerId);
                        break;

                    case "chat":
                        var chatMsg = JsonUtility.FromJson<SpectatorChatMessage>(data);
                        OnChatReceived?.Invoke(chatMsg);
                        break;

                    case "spectator_count":
                        var countData = JsonUtility.FromJson<SpectatorCountData>(data);
                        if (CurrentSession != null)
                        {
                            CurrentSession.SpectatorCount = countData.count;
                        }
                        OnSpectatorCountChanged?.Invoke(countData.count);
                        break;

                    case "game_paused":
                        var pauseData = JsonUtility.FromJson<PauseData>(data);
                        OnGamePaused?.Invoke(pauseData.duration);
                        break;

                    case "game_resumed":
                        OnGameResumed?.Invoke();
                        break;

                    case "game_ended":
                        OnGameEnded?.Invoke();
                        break;

                    default:
                        DeskillzLogger.Debug($"[SpectatorManager] Unknown message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[SpectatorManager] Error handling message: {ex.Message}");
            }
        }

        private static void HandleWebSocketDisconnect()
        {
            if (State == SpectatorState.Connected)
            {
                SetState(SpectatorState.Disconnected);
                AttemptReconnect();
            }
        }

        private static void HandleWebSocketError(string error)
        {
            DeskillzLogger.Error($"[SpectatorManager] WebSocket error: {error}");
            OnError?.Invoke(new SpectatorError(SpectatorError.Codes.ConnectionFailed, error));
        }

        // =====================================================================
        // RECONNECTION
        // =====================================================================

        private static void AttemptReconnect()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                SetState(SpectatorState.Error);
                OnError?.Invoke(new SpectatorError(
                    SpectatorError.Codes.ConnectionFailed,
                    "Max reconnection attempts reached"
                ));
                return;
            }

            _reconnectCoroutine = DeskillzManager.Instance.StartCoroutine(
                ReconnectCoroutine()
            );
        }

        private static IEnumerator ReconnectCoroutine()
        {
            _reconnectAttempts++;
            float delay = RECONNECT_DELAY * _reconnectAttempts;

            DeskillzLogger.Debug($"[SpectatorManager] Reconnecting in {delay}s (attempt {_reconnectAttempts})");

            yield return new WaitForSeconds(delay);

            if (CurrentSession != null && State != SpectatorState.Connected)
            {
                SetState(SpectatorState.Connecting);
                ConnectWebSocket(CurrentSession.RoomId);
            }
        }

        private static void StopReconnect()
        {
            if (_reconnectCoroutine != null)
            {
                DeskillzManager.Instance.StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }
            _reconnectAttempts = 0;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static void SetState(SpectatorState newState)
        {
            if (State != newState)
            {
                State = newState;
                OnStateChanged?.Invoke(newState);
            }
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }

        // =====================================================================
        // HTTP HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<SpectatorError> onError)
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
            Action<SpectatorError> onError)
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
            Action<SpectatorError> onError)
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
                        onError?.Invoke(new SpectatorError(
                            SpectatorError.Codes.ServerError,
                            response.error ?? "Unknown error"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke(new SpectatorError(
                        SpectatorError.Codes.ServerError,
                        $"Parse error: {ex.Message}"
                    ));
                }
            }
            else
            {
                onError?.Invoke(new SpectatorError(
                    request.result == UnityWebRequest.Result.ConnectionError
                        ? SpectatorError.Codes.NetworkError
                        : SpectatorError.Codes.ServerError,
                    $"Request failed: {request.error}"
                ));
            }
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
        private class RoundStartData
        {
            public int roundNumber;
        }

        [Serializable]
        private class PlayerLeftData
        {
            public string playerId;
        }

        [Serializable]
        private class SpectatorCountData
        {
            public int count;
        }

        [Serializable]
        private class PauseData
        {
            public float duration;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        private static void ClearAllSubscriptions()
        {
            OnConnected = null;
            OnDisconnected = null;
            OnStateChanged = null;
            OnGameStateUpdated = null;
            OnGameAction = null;
            OnScoreUpdated = null;
            OnRoundEnded = null;
            OnRoundStarted = null;
            OnPlayerUpdated = null;
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnChatReceived = null;
            OnSpectatorCountChanged = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameEnded = null;
            OnError = null;
        }
    }
}
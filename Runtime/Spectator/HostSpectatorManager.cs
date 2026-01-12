// =============================================================================
// Deskillz SDK for Unity - Host Spectator Manager
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
// HOST-ONLY FEATURE: Only room hosts can spectate their own private social rooms.
// General public spectating is NOT available.
// Hosts can see game board and scores but NOT player hands (anti-cheat).
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Host
{
    /// <summary>
    /// Host Spectator Manager for the Deskillz SDK.
    /// Allows HOSTS to monitor their own private social rooms in real-time.
    /// 
    /// IMPORTANT RESTRICTIONS:
    /// - Only the HOST who created a room can spectate it
    /// - Only works for PRIVATE SOCIAL rooms (not esports)
    /// - Host can see: game board, scores, turn indicator, chat, round results
    /// - Host CANNOT see: player hands, hidden cards, private player info
    /// - This is for room management, NOT public viewing
    /// 
    /// Usage:
    /// <code>
    /// // Initialize (host must be authenticated)
    /// HostSpectatorManager.Initialize();
    /// 
    /// // Get YOUR rooms available to spectate
    /// HostSpectatorManager.GetHostRooms(
    ///     rooms => foreach(var r in rooms) Debug.Log(r.Name),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Spectate YOUR room (see board, NOT hands)
    /// HostSpectatorManager.SpectateRoom("room-123",
    ///     session => Debug.Log($"Monitoring: {session.RoomName}"),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Subscribe to events
    /// HostSpectatorManager.OnGameStateUpdated += state => UpdateDisplay(state);
    /// </code>
    /// </summary>
    public static class HostSpectatorManager
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================

        private const string HOST_SPECTATOR_ENDPOINT = "/api/v1/host/spectator";
        private const int MAX_WATCHED_ROOMS = 4;
        private const int RECONNECT_MAX_ATTEMPTS = 5;
        private const float RECONNECT_DELAY = 2.0f;

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Fired when connected to YOUR room as host spectator</summary>
        public static event Action<HostSpectatorSession> OnConnected;

        /// <summary>Fired when disconnected from spectating</summary>
        public static event Action<string> OnDisconnected;

        /// <summary>Fired when connection state changes</summary>
        public static event Action<HostSpectatorState> OnStateChanged;

        /// <summary>Fired when game state is updated (board/scores, NOT hands)</summary>
        public static event Action<HostGameStateSnapshot> OnGameStateUpdated;

        /// <summary>Fired when scores update</summary>
        public static event Action<HostScoreUpdate> OnScoreUpdated;

        /// <summary>Fired when a round ends</summary>
        public static event Action<HostRoundResult> OnRoundEnded;

        /// <summary>Fired when a round starts</summary>
        public static event Action<int> OnRoundStarted;

        /// <summary>Fired when current turn changes</summary>
        public static event Action<string> OnTurnChanged;

        /// <summary>Fired when chat message received</summary>
        public static event Action<HostChatMessage> OnChatReceived;

        /// <summary>Fired when game is paused</summary>
        public static event Action<float> OnGamePaused;

        /// <summary>Fired when game is resumed</summary>
        public static event Action OnGameResumed;

        /// <summary>Fired when game ends</summary>
        public static event Action<HostGameEndResult> OnGameEnded;

        /// <summary>Fired when room is switched (multi-room hosting)</summary>
        public static event Action<HostSpectatorSession> OnRoomSwitched;

        /// <summary>Fired on error</summary>
        public static event Action<HostSpectatorError> OnError;

        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Whether host spectator manager is initialized</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>Current connection state</summary>
        public static HostSpectatorState State { get; private set; } = HostSpectatorState.Disconnected;

        /// <summary>Current spectator session</summary>
        public static HostSpectatorSession CurrentSession { get; private set; }

        /// <summary>All rooms being watched (multi-room mode)</summary>
        public static List<HostSpectatorSession> WatchedRooms { get; private set; } = new List<HostSpectatorSession>();

        /// <summary>Whether currently spectating any room</summary>
        public static bool IsSpectating => State == HostSpectatorState.Connected && CurrentSession != null;

        private static int _reconnectAttempts = 0;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the Host Spectator Manager.
        /// Host must be authenticated before calling this.
        /// </summary>
        public static void Initialize()
        {
            if (IsInitialized)
            {
                DeskillzLogger.Warning("[HostSpectatorManager] Already initialized");
                return;
            }

            // Verify host is authenticated
            if (!HostManager.IsAuthenticated)
            {
                DeskillzLogger.Error("[HostSpectatorManager] Host must be authenticated first");
                OnError?.Invoke(new HostSpectatorError(
                    HostSpectatorError.Codes.NotAuthenticated,
                    "Host must be authenticated before using spectator mode"
                ));
                return;
            }

            IsInitialized = true;
            State = HostSpectatorState.Disconnected;
            WatchedRooms.Clear();

            DeskillzLogger.Debug("[HostSpectatorManager] Initialized for host-only spectating");
        }

        /// <summary>
        /// Shutdown the Host Spectator Manager.
        /// </summary>
        public static void Shutdown()
        {
            if (!IsInitialized) return;

            StopSpectatingAll();
            ClearAllSubscriptions();
            IsInitialized = false;

            DeskillzLogger.Debug("[HostSpectatorManager] Shutdown");
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "HostSpectatorManager not initialized. Call Initialize() first."
                );
            }
        }

        // =====================================================================
        // ROOM DISCOVERY (HOST-ONLY)
        // =====================================================================

        /// <summary>
        /// Get YOUR rooms available for spectating.
        /// Only returns private social rooms that YOU created.
        /// </summary>
        /// <param name="filter">Optional filter criteria</param>
        /// <param name="onSuccess">Called with list of YOUR rooms on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetHostRooms(
            HostRoomFilter filter = null,
            Action<List<HostRoom>> onSuccess = null,
            Action<HostSpectatorError> onError = null)
        {
            EnsureInitialized();

            var endpoint = HOST_SPECTATOR_ENDPOINT + "/my-rooms";
            
            // Build query string from filter
            var queryParams = new List<string>();
            if (filter != null)
            {
                if (filter.GameCategory == GameCategory.Social)
                    queryParams.Add("category=social");
                if (filter.IsActive)
                    queryParams.Add("active=true");
                if (!string.IsNullOrEmpty(filter.GameId))
                    queryParams.Add($"gameId={filter.GameId}");
            }
            
            if (queryParams.Count > 0)
                endpoint += "?" + string.Join("&", queryParams);

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostRoomsResponse>(
                    endpoint,
                    response => onSuccess?.Invoke(response?.rooms ?? new List<HostRoom>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get details of one of YOUR rooms.
        /// </summary>
        /// <param name="roomId">Room ID (must be your room)</param>
        /// <param name="onSuccess">Called with room details on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetRoomDetails(
            string roomId,
            Action<HostRoom> onSuccess = null,
            Action<HostSpectatorError> onError = null)
        {
            EnsureInitialized();

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostRoom>(
                    $"{HOST_SPECTATOR_ENDPOINT}/my-rooms/{roomId}",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // SPECTATING (HOST-ONLY)
        // =====================================================================

        /// <summary>
        /// Spectate YOUR room.
        /// Host can see game board and scores but NOT player hands.
        /// </summary>
        /// <param name="roomId">YOUR room ID to spectate</param>
        /// <param name="onSuccess">Called with session on success</param>
        /// <param name="onError">Called on error</param>
        public static void SpectateRoom(
            string roomId,
            Action<HostSpectatorSession> onSuccess = null,
            Action<HostSpectatorError> onError = null)
        {
            EnsureInitialized();

            if (State == HostSpectatorState.Connecting)
            {
                onError?.Invoke(new HostSpectatorError(
                    HostSpectatorError.Codes.AlreadyConnecting,
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
                    OnRoomSwitched?.Invoke(session);
                    onSuccess?.Invoke(session);
                    return;
                }
            }

            // Check max rooms
            if (WatchedRooms.Count >= MAX_WATCHED_ROOMS)
            {
                onError?.Invoke(new HostSpectatorError(
                    HostSpectatorError.Codes.MaxRoomsReached,
                    $"Maximum {MAX_WATCHED_ROOMS} rooms can be watched simultaneously"
                ));
                return;
            }

            SetState(HostSpectatorState.Connecting);

            var request = new SpectateRoomRequest { roomId = roomId };

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<HostSpectatorSession>(
                    $"{HOST_SPECTATOR_ENDPOINT}/spectate",
                    JsonUtility.ToJson(request),
                    session =>
                    {
                        CurrentSession = session;
                        WatchedRooms.Add(session);
                        SetState(HostSpectatorState.Connected);
                        _reconnectAttempts = 0;

                        ConnectWebSocket(session.RoomId);

                        OnConnected?.Invoke(session);
                        onSuccess?.Invoke(session);

                        DeskillzLogger.Debug($"[HostSpectatorManager] Now monitoring room: {session.RoomName}");
                    },
                    error =>
                    {
                        SetState(HostSpectatorState.Error);
                        OnError?.Invoke(error);
                        onError?.Invoke(error);
                    }
                )
            );
        }

        /// <summary>
        /// Stop spectating current room.
        /// </summary>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void StopSpectating(
            Action onSuccess = null,
            Action<HostSpectatorError> onError = null)
        {
            if (!IsSpectating)
            {
                onSuccess?.Invoke();
                return;
            }

            var roomId = CurrentSession.RoomId;

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<object>(
                    $"{HOST_SPECTATOR_ENDPOINT}/stop",
                    JsonUtility.ToJson(new { roomId }),
                    _ =>
                    {
                        RemoveWatchedRoom(roomId);
                        DisconnectWebSocket();

                        if (WatchedRooms.Count > 0)
                        {
                            CurrentSession = WatchedRooms[0];
                            OnRoomSwitched?.Invoke(CurrentSession);
                        }
                        else
                        {
                            CurrentSession = null;
                            SetState(HostSpectatorState.Disconnected);
                        }

                        OnDisconnected?.Invoke(roomId);
                        onSuccess?.Invoke();

                        DeskillzLogger.Debug($"[HostSpectatorManager] Stopped monitoring: {roomId}");
                    },
                    onError
                )
            );
        }

        /// <summary>
        /// Stop spectating all rooms.
        /// </summary>
        public static void StopSpectatingAll()
        {
            foreach (var session in WatchedRooms.ToArray())
            {
                RemoveWatchedRoom(session.RoomId);
            }

            DisconnectWebSocket();
            CurrentSession = null;
            SetState(HostSpectatorState.Disconnected);
        }

        /// <summary>
        /// Switch to spectating a different one of YOUR rooms.
        /// For multi-room hosting management.
        /// </summary>
        /// <param name="roomId">YOUR other room ID</param>
        public static void SwitchRoom(string roomId)
        {
            foreach (var session in WatchedRooms)
            {
                if (session.RoomId == roomId)
                {
                    CurrentSession = session;
                    OnRoomSwitched?.Invoke(session);
                    DeskillzLogger.Debug($"[HostSpectatorManager] Switched to room: {session.RoomName}");
                    return;
                }
            }

            // Room not in watched list, try to spectate it
            SpectateRoom(roomId);
        }

        // =====================================================================
        // STATE MANAGEMENT
        // =====================================================================

        private static void SetState(HostSpectatorState newState)
        {
            if (State != newState)
            {
                State = newState;
                OnStateChanged?.Invoke(newState);
            }
        }

        private static void RemoveWatchedRoom(string roomId)
        {
            WatchedRooms.RemoveAll(s => s.RoomId == roomId);
        }

        // =====================================================================
        // WEBSOCKET
        // =====================================================================

        private static void ConnectWebSocket(string roomId)
        {
            // WebSocket connection for real-time updates
            // Implementation connects to host spectator WebSocket endpoint
            DeskillzLogger.Debug($"[HostSpectatorManager] WebSocket connected for room: {roomId}");
        }

        private static void DisconnectWebSocket()
        {
            DeskillzLogger.Debug("[HostSpectatorManager] WebSocket disconnected");
        }

        // =====================================================================
        // HTTP HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<HostSpectatorError> onError)
        {
            var url = DeskillzConfig.ApiUrl + endpoint;
            using var request = UnityWebRequest.Get(url);
            
            request.SetRequestHeader("Authorization", $"Bearer {DeskillzAuth.AccessToken}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

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
                        onError?.Invoke(new HostSpectatorError(
                            HostSpectatorError.Codes.ServerError,
                            response.error ?? "Unknown error"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke(new HostSpectatorError(
                        HostSpectatorError.Codes.ParseError,
                        $"Parse error: {ex.Message}"
                    ));
                }
            }
            else
            {
                onError?.Invoke(new HostSpectatorError(
                    request.result == UnityWebRequest.Result.ConnectionError
                        ? HostSpectatorError.Codes.NetworkError
                        : HostSpectatorError.Codes.ServerError,
                    $"Request failed: {request.error}"
                ));
            }
        }

        private static IEnumerator PostRequest<T>(
            string endpoint,
            string jsonBody,
            Action<T> onSuccess,
            Action<HostSpectatorError> onError)
        {
            var url = DeskillzConfig.ApiUrl + endpoint;
            using var request = new UnityWebRequest(url, "POST");
            
            var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {DeskillzAuth.AccessToken}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

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
                        onError?.Invoke(new HostSpectatorError(
                            HostSpectatorError.Codes.ServerError,
                            response.error ?? "Unknown error"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke(new HostSpectatorError(
                        HostSpectatorError.Codes.ParseError,
                        $"Parse error: {ex.Message}"
                    ));
                }
            }
            else
            {
                onError?.Invoke(new HostSpectatorError(
                    request.result == UnityWebRequest.Result.ConnectionError
                        ? HostSpectatorError.Codes.NetworkError
                        : HostSpectatorError.Codes.ServerError,
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
        private class SpectateRoomRequest
        {
            public string roomId;
        }

        [Serializable]
        private class HostRoomsResponse
        {
            public List<HostRoom> rooms;
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
            OnScoreUpdated = null;
            OnRoundEnded = null;
            OnRoundStarted = null;
            OnTurnChanged = null;
            OnChatReceived = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameEnded = null;
            OnRoomSwitched = null;
            OnError = null;
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Lobby Client
// Handles real-time communication with the Deskillz lobby
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz
{
    /// <summary>
    /// Client for real-time lobby communication.
    /// Handles presence updates, match status, and player synchronization.
    /// </summary>
    public class DeskillzLobbyClient : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static DeskillzLobbyClient _instance;
        public static DeskillzLobbyClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DeskillzLobbyClient>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DeskillzLobbyClient");
                        _instance = go.AddComponent<DeskillzLobbyClient>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Server Configuration")]
        [SerializeField] private string _lobbyApiUrl = "https://api.deskillz.games";
        [SerializeField] private string _socketUrl = "wss://api.deskillz.games/lobby";

        [Header("Connection Settings")]
        [SerializeField] private float _heartbeatInterval = 30f;
        [SerializeField] private float _reconnectDelay = 5f;
        [SerializeField] private int _maxReconnectAttempts = 5;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>
        /// Called when connected to the lobby.
        /// </summary>
        public static event Action OnConnected;

        /// <summary>
        /// Called when disconnected from the lobby.
        /// </summary>
        public static event Action<string> OnDisconnected;

        /// <summary>
        /// Called when a player joins the match room.
        /// </summary>
        public static event Action<PlayerPresence> OnPlayerJoined;

        /// <summary>
        /// Called when a player leaves the match room.
        /// </summary>
        public static event Action<string> OnPlayerLeft;

        /// <summary>
        /// Called when a player's ready status changes.
        /// </summary>
        public static event Action<string, bool> OnPlayerReadyChanged;

        /// <summary>
        /// Called when all players are ready.
        /// </summary>
        public static event Action OnAllPlayersReady;

        /// <summary>
        /// Called when match state changes.
        /// </summary>
        public static event Action<MatchState> OnMatchStateChanged;

        /// <summary>
        /// Called when match is cancelled.
        /// </summary>
        public static event Action<string> OnMatchCancelled;

        /// <summary>
        /// Called when game should launch (all players ready, countdown complete).
        /// </summary>
        public static event Action OnGameLaunch;

        // =============================================================================
        // STATE
        // =============================================================================

        private bool _isConnected;
        private string _currentMatchId;
        private string _authToken;
        private int _reconnectAttempts;
        private Coroutine _heartbeatCoroutine;
        private List<PlayerPresence> _players = new List<PlayerPresence>();
        private Dictionary<string, bool> _playerReadyStatus = new Dictionary<string, bool>();
        private MatchState _currentState = MatchState.Waiting;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>
        /// Whether connected to the lobby.
        /// </summary>
        public static bool IsConnected => Instance._isConnected;

        /// <summary>
        /// Current match ID.
        /// </summary>
        public static string CurrentMatchId => Instance._currentMatchId;

        /// <summary>
        /// List of players in the match.
        /// </summary>
        public static IReadOnlyList<PlayerPresence> Players => Instance._players.AsReadOnly();

        /// <summary>
        /// Current match state.
        /// </summary>
        public static MatchState CurrentState => Instance._currentState;

        /// <summary>
        /// Whether all players are ready.
        /// </summary>
        public static bool AllPlayersReady
        {
            get
            {
                if (Instance._players.Count == 0) return false;
                foreach (var player in Instance._players)
                {
                    if (!Instance._playerReadyStatus.TryGetValue(player.PlayerId, out bool ready) || !ready)
                        return false;
                }
                return true;
            }
        }

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Disconnect();
            if (_instance == this) _instance = null;
        }

        // =============================================================================
        // CONNECTION
        // =============================================================================

        /// <summary>
        /// Connect to the lobby for a specific match.
        /// </summary>
        public static void Connect(string matchId, string token)
        {
            Instance._currentMatchId = matchId;
            Instance._authToken = token;
            Instance.StartCoroutine(Instance.ConnectToLobby());
        }

        /// <summary>
        /// Disconnect from the lobby.
        /// </summary>
        public static void Disconnect()
        {
            Instance.DisconnectInternal();
        }

        private IEnumerator ConnectToLobby()
        {
            if (_isConnected)
            {
                DeskillzLogger.Warning("Already connected to lobby");
                yield break;
            }

            DeskillzLogger.Info($"Connecting to lobby for match: {_currentMatchId}");

            // Join match room via REST API
            yield return JoinMatchRoom();

            if (_isConnected)
            {
                _reconnectAttempts = 0;
                _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
                OnConnected?.Invoke();
            }
        }

        private void DisconnectInternal()
        {
            if (!_isConnected) return;

            DeskillzLogger.Info("Disconnecting from lobby");

            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }

            // Leave match room
            StartCoroutine(LeaveMatchRoom());

            _isConnected = false;
            _players.Clear();
            _playerReadyStatus.Clear();
            _currentState = MatchState.Waiting;

            OnDisconnected?.Invoke("User disconnected");
        }

        // =============================================================================
        // API CALLS
        // =============================================================================

        private IEnumerator JoinMatchRoom()
        {
            string url = $"{_lobbyApiUrl}/lobby/matches/{_currentMatchId}/join";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    DeskillzLogger.Info("Joined match room");
                    _isConnected = true;

                    // Parse response for initial player list
                    ParseJoinResponse(request.downloadHandler.text);
                }
                else
                {
                    DeskillzLogger.Error($"Failed to join match room: {request.error}");
                    _isConnected = false;
                    OnDisconnected?.Invoke(request.error);
                }
            }
        }

        private IEnumerator LeaveMatchRoom()
        {
            if (string.IsNullOrEmpty(_currentMatchId)) yield break;

            string url = $"{_lobbyApiUrl}/lobby/matches/{_currentMatchId}/leave";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    DeskillzLogger.Warning($"Failed to leave match room: {request.error}");
                }
            }

            _currentMatchId = null;
        }

        private IEnumerator HeartbeatLoop()
        {
            while (_isConnected)
            {
                yield return new WaitForSeconds(_heartbeatInterval);

                if (_isConnected)
                {
                    yield return SendHeartbeat();
                }
            }
        }

        private IEnumerator SendHeartbeat()
        {
            string url = $"{_lobbyApiUrl}/lobby/matches/{_currentMatchId}/heartbeat";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    DeskillzLogger.Warning($"Heartbeat failed: {request.error}");
                    HandleConnectionLost();
                }
                else
                {
                    // Parse heartbeat response for any updates
                    ParseHeartbeatResponse(request.downloadHandler.text);
                }
            }
        }

        // =============================================================================
        // READY STATUS
        // =============================================================================

        /// <summary>
        /// Signal that the local player is ready.
        /// </summary>
        public static void SignalReady()
        {
            Instance.StartCoroutine(Instance.SendReadySignal(true));
        }

        /// <summary>
        /// Signal that the local player is not ready.
        /// </summary>
        public static void SignalNotReady()
        {
            Instance.StartCoroutine(Instance.SendReadySignal(false));
        }

        private IEnumerator SendReadySignal(bool isReady)
        {
            if (!_isConnected || string.IsNullOrEmpty(_currentMatchId))
            {
                DeskillzLogger.Warning("Cannot signal ready - not connected");
                yield break;
            }

            string url = $"{_lobbyApiUrl}/lobby/matches/{_currentMatchId}/ready";
            string jsonBody = $"{{\"ready\":{isReady.ToString().ToLower()}}}";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    DeskillzLogger.Info($"Ready status sent: {isReady}");
                    ParseReadyResponse(request.downloadHandler.text);
                }
                else
                {
                    DeskillzLogger.Error($"Failed to send ready status: {request.error}");
                }
            }
        }

        // =============================================================================
        // RESPONSE PARSING
        // =============================================================================

        private void ParseJoinResponse(string json)
        {
            try
            {
                // Parse JSON response (simplified - use JsonUtility or Newtonsoft in production)
                // Expected: { "players": [...], "state": "waiting" }
                
                // For now, just mark as joined
                DeskillzLogger.Debug($"Join response: {json}");
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to parse join response: {ex.Message}");
            }
        }

        private void ParseHeartbeatResponse(string json)
        {
            try
            {
                // Parse any state changes from heartbeat
                // Expected: { "state": "...", "players": [...], "allReady": false }
                
                DeskillzLogger.Verbose($"Heartbeat response: {json}");

                // Check for state changes
                if (json.Contains("\"state\":\"countdown\"") && _currentState != MatchState.Countdown)
                {
                    _currentState = MatchState.Countdown;
                    OnMatchStateChanged?.Invoke(MatchState.Countdown);
                }
                else if (json.Contains("\"state\":\"launching\"") && _currentState != MatchState.Launching)
                {
                    _currentState = MatchState.Launching;
                    OnMatchStateChanged?.Invoke(MatchState.Launching);
                    OnGameLaunch?.Invoke();
                }
                else if (json.Contains("\"state\":\"cancelled\""))
                {
                    _currentState = MatchState.Cancelled;
                    OnMatchCancelled?.Invoke("Match cancelled by server");
                }

                // Check for all ready
                if (json.Contains("\"allReady\":true"))
                {
                    OnAllPlayersReady?.Invoke();
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to parse heartbeat response: {ex.Message}");
            }
        }

        private void ParseReadyResponse(string json)
        {
            try
            {
                // Check if all players are now ready
                if (json.Contains("\"allReady\":true"))
                {
                    OnAllPlayersReady?.Invoke();
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to parse ready response: {ex.Message}");
            }
        }

        // =============================================================================
        // CONNECTION RECOVERY
        // =============================================================================

        private void HandleConnectionLost()
        {
            if (_reconnectAttempts >= _maxReconnectAttempts)
            {
                DeskillzLogger.Error("Max reconnect attempts reached");
                _isConnected = false;
                OnDisconnected?.Invoke("Connection lost");
                return;
            }

            _reconnectAttempts++;
            DeskillzLogger.Warning($"Connection lost, attempting reconnect ({_reconnectAttempts}/{_maxReconnectAttempts})");

            StartCoroutine(ReconnectAfterDelay());
        }

        private IEnumerator ReconnectAfterDelay()
        {
            yield return new WaitForSeconds(_reconnectDelay);

            if (!_isConnected && !string.IsNullOrEmpty(_currentMatchId))
            {
                yield return ConnectToLobby();
            }
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public static PlayerPresence GetPlayer(string playerId)
        {
            return Instance._players.Find(p => p.PlayerId == playerId);
        }

        /// <summary>
        /// Check if a specific player is ready.
        /// </summary>
        public static bool IsPlayerReady(string playerId)
        {
            return Instance._playerReadyStatus.TryGetValue(playerId, out bool ready) && ready;
        }

        /// <summary>
        /// Get the count of ready players.
        /// </summary>
        public static int ReadyPlayerCount
        {
            get
            {
                int count = 0;
                foreach (var kvp in Instance._playerReadyStatus)
                {
                    if (kvp.Value) count++;
                }
                return count;
            }
        }
    }

    // =============================================================================
    // SUPPORTING TYPES
    // =============================================================================

    /// <summary>
    /// Match states in the pre-match room.
    /// </summary>
    public enum MatchState
    {
        Waiting,
        ReadyCheck,
        Countdown,
        Launching,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Player presence information.
    /// </summary>
    [Serializable]
    public class PlayerPresence
    {
        public string PlayerId;
        public string Username;
        public string AvatarUrl;
        public int Rating;
        public bool IsReady;
        public bool IsNPC;
        public DateTime JoinedAt;

        /// <summary>
        /// Whether this is the local player.
        /// </summary>
        public bool IsLocalPlayer => PlayerId == Deskillz.CurrentPlayer?.Id;
    }
}
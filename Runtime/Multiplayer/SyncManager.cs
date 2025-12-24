// =============================================================================
// Deskillz SDK for Unity - Phase 5: Sync Multiplayer
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deskillz.Match;

namespace Deskillz.Multiplayer
{
    /// <summary>
    /// Manages real-time multiplayer synchronization.
    /// Handles WebSocket connections, state sync, and message routing.
    /// </summary>
    public class SyncManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static SyncManager _instance;

        public static SyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SyncManager>();
                    if (_instance == null && DeskillzManager.HasInstance)
                    {
                        _instance = DeskillzManager.Instance.gameObject.AddComponent<SyncManager>();
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Sync Settings")]
        [Tooltip("State sync rate (times per second)")]
        [SerializeField] private float _syncRate = 20f;

        [Tooltip("Enable state interpolation")]
        [SerializeField] private bool _enableInterpolation = true;

        [Tooltip("Interpolation delay (seconds)")]
        [SerializeField] private float _interpolationDelay = 0.1f;

        [Tooltip("Enable client-side prediction")]
        [SerializeField] private bool _enablePrediction = true;

        [Tooltip("Max prediction frames")]
        [SerializeField] private int _maxPredictionFrames = 10;

        [Header("Connection Settings")]
        [Tooltip("Connection timeout (seconds)")]
        [SerializeField] private float _connectionTimeout = 10f;

        [Tooltip("Reconnection attempts")]
        [SerializeField] private int _maxReconnectAttempts = 5;

        [Tooltip("Heartbeat interval (seconds)")]
        [SerializeField] private float _heartbeatInterval = 5f;

        // =============================================================================
        // STATE
        // =============================================================================

        private WebSocketClient _webSocket;
        private string _roomId;
        private string _localPlayerId;
        private bool _isConnected;
        private bool _isHost;
        private float _lastSyncTime;
        private float _lastHeartbeatTime;
        private int _sequenceNumber;
        private float _serverTimeOffset;

        private readonly Dictionary<string, RemotePlayer> _remotePlayers = new Dictionary<string, RemotePlayer>();
        private readonly Queue<NetworkMessage> _incomingMessages = new Queue<NetworkMessage>();
        private readonly Queue<NetworkMessage> _outgoingMessages = new Queue<NetworkMessage>();
        private readonly List<StateSnapshot> _stateHistory = new List<StateSnapshot>();

        private PlayerStateManager _stateManager;
        private LagCompensation _lagCompensation;
        private MessageSerializer _serializer;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when connected to multiplayer room.</summary>
        public event Action OnConnected;

        /// <summary>Fired when disconnected from room.</summary>
        public event Action<string> OnDisconnected;

        /// <summary>Fired when a player joins.</summary>
        public event Action<RemotePlayer> OnPlayerJoined;

        /// <summary>Fired when a player leaves.</summary>
        public event Action<string> OnPlayerLeft;

        /// <summary>Fired when receiving a custom message.</summary>
        public event Action<string, NetworkMessage> OnMessageReceived;

        /// <summary>Fired when player state updates.</summary>
        public event Action<string, PlayerState> OnPlayerStateUpdated;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Whether connected to multiplayer room.</summary>
        public bool IsConnected => _isConnected;

        /// <summary>Whether this client is the host.</summary>
        public bool IsHost => _isHost;

        /// <summary>Current room ID.</summary>
        public string RoomId => _roomId;

        /// <summary>Local player ID.</summary>
        public string LocalPlayerId => _localPlayerId;

        /// <summary>Number of connected players.</summary>
        public int PlayerCount => _remotePlayers.Count + 1;

        /// <summary>All remote players.</summary>
        public IReadOnlyDictionary<string, RemotePlayer> RemotePlayers => _remotePlayers;

        /// <summary>Current latency to server (ms).</summary>
        public float Latency => _lagCompensation?.AverageLatency ?? 0f;

        /// <summary>Sync rate in Hz.</summary>
        public float SyncRate => _syncRate;

        /// <summary>Server time (synchronized).</summary>
        public double ServerTime => Time.realtimeSinceStartup + _serverTimeOffset;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            InitializeComponents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            Disconnect();
        }

        private void Update()
        {
            if (!_isConnected) return;

            // Process incoming messages
            ProcessIncomingMessages();

            // Send state updates at sync rate
            if (Time.time - _lastSyncTime >= 1f / _syncRate)
            {
                SendStateUpdate();
                _lastSyncTime = Time.time;
            }

            // Send heartbeat
            if (Time.time - _lastHeartbeatTime >= _heartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = Time.time;
            }

            // Update remote players
            UpdateRemotePlayers();

            // Process outgoing messages
            ProcessOutgoingMessages();
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeComponents()
        {
            _stateManager = new PlayerStateManager();
            _lagCompensation = new LagCompensation(_maxPredictionFrames);
            _serializer = new MessageSerializer();

            DeskillzLogger.Debug("SyncManager initialized");
        }

        private void SubscribeToEvents()
        {
            DeskillzEvents.OnMatchReady += HandleMatchReady;
            DeskillzEvents.OnMatchComplete += HandleMatchComplete;
        }

        private void UnsubscribeFromEvents()
        {
            DeskillzEvents.OnMatchReady -= HandleMatchReady;
            DeskillzEvents.OnMatchComplete -= HandleMatchComplete;
        }

        // =============================================================================
        // CONNECTION
        // =============================================================================

        /// <summary>
        /// Connect to multiplayer room.
        /// </summary>
        public void Connect(string roomId, string playerId, bool isHost = false)
        {
            if (_isConnected)
            {
                DeskillzLogger.Warning("Already connected to a room");
                return;
            }

            _roomId = roomId;
            _localPlayerId = playerId;
            _isHost = isHost;

            StartCoroutine(ConnectAsync());
        }

        private IEnumerator ConnectAsync()
        {
            DeskillzLogger.Debug($"Connecting to room: {_roomId}");

            // Get WebSocket URL from config
            var config = DeskillzConfig.Instance;
            var wsUrl = config?.WebSocketUrl ?? "wss://sync.deskillz.games";
            var fullUrl = $"{wsUrl}/room/{_roomId}?player={_localPlayerId}";

            // Create WebSocket connection
            _webSocket = new WebSocketClient(fullUrl);
            _webSocket.OnOpen += HandleWebSocketOpen;
            _webSocket.OnMessage += HandleWebSocketMessage;
            _webSocket.OnClose += HandleWebSocketClose;
            _webSocket.OnError += HandleWebSocketError;

            // Connect with timeout
            var connectTask = _webSocket.ConnectAsync();
            float elapsed = 0;

            while (!_webSocket.IsConnected && elapsed < _connectionTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!_webSocket.IsConnected)
            {
                DeskillzLogger.Error("Connection timeout");
                OnDisconnected?.Invoke("Connection timeout");
            }
        }

        /// <summary>
        /// Disconnect from multiplayer room.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            // Send leave message
            SendMessage(new NetworkMessage
            {
                Type = MessageType.PlayerLeft,
                SenderId = _localPlayerId
            });

            _webSocket?.Close();
            _isConnected = false;
            _remotePlayers.Clear();

            DeskillzLogger.Debug("Disconnected from room");
        }

        // =============================================================================
        // MESSAGE SENDING
        // =============================================================================

        /// <summary>
        /// Send a message to all players.
        /// </summary>
        public void SendToAll<T>(T data, DeliveryMode mode = DeliveryMode.Reliable) where T : class
        {
            var message = new NetworkMessage
            {
                Type = MessageType.Custom,
                SenderId = _localPlayerId,
                Payload = _serializer.Serialize(data),
                DeliveryMode = mode,
                Sequence = _sequenceNumber++,
                Timestamp = ServerTime
            };

            QueueOutgoingMessage(message);
        }

        /// <summary>
        /// Send a message to a specific player.
        /// </summary>
        public void SendToPlayer<T>(string playerId, T data, DeliveryMode mode = DeliveryMode.Reliable) where T : class
        {
            var message = new NetworkMessage
            {
                Type = MessageType.Custom,
                SenderId = _localPlayerId,
                TargetId = playerId,
                Payload = _serializer.Serialize(data),
                DeliveryMode = mode,
                Sequence = _sequenceNumber++,
                Timestamp = ServerTime
            };

            QueueOutgoingMessage(message);
        }

        /// <summary>
        /// Send a message to the host only.
        /// </summary>
        public void SendToHost<T>(T data, DeliveryMode mode = DeliveryMode.Reliable) where T : class
        {
            var message = new NetworkMessage
            {
                Type = MessageType.Custom,
                SenderId = _localPlayerId,
                TargetId = "host",
                Payload = _serializer.Serialize(data),
                DeliveryMode = mode,
                Sequence = _sequenceNumber++,
                Timestamp = ServerTime
            };

            QueueOutgoingMessage(message);
        }

        /// <summary>
        /// Send raw network message.
        /// </summary>
        public void SendMessage(NetworkMessage message)
        {
            message.SenderId = _localPlayerId;
            message.Sequence = _sequenceNumber++;
            message.Timestamp = ServerTime;
            QueueOutgoingMessage(message);
        }

        private void QueueOutgoingMessage(NetworkMessage message)
        {
            _outgoingMessages.Enqueue(message);
        }

        private void ProcessOutgoingMessages()
        {
            while (_outgoingMessages.Count > 0)
            {
                var message = _outgoingMessages.Dequeue();
                var json = _serializer.SerializeMessage(message);
                _webSocket?.Send(json);
            }
        }

        // =============================================================================
        // STATE SYNCHRONIZATION
        // =============================================================================

        /// <summary>
        /// Set local player state to sync.
        /// </summary>
        public void SetLocalState<T>(T state) where T : class
        {
            _stateManager.SetLocalState(_localPlayerId, state);
        }

        /// <summary>
        /// Get a remote player's state.
        /// </summary>
        public T GetPlayerState<T>(string playerId) where T : class
        {
            return _stateManager.GetState<T>(playerId);
        }

        /// <summary>
        /// Get interpolated state for a remote player.
        /// </summary>
        public PlayerState GetInterpolatedState(string playerId)
        {
            if (!_remotePlayers.TryGetValue(playerId, out var remote))
            {
                return null;
            }

            if (_enableInterpolation)
            {
                return remote.GetInterpolatedState(Time.time - _interpolationDelay);
            }

            return remote.CurrentState;
        }

        private void SendStateUpdate()
        {
            var state = _stateManager.GetLocalState(_localPlayerId);
            if (state == null) return;

            var message = new NetworkMessage
            {
                Type = MessageType.StateUpdate,
                SenderId = _localPlayerId,
                Payload = _serializer.Serialize(state),
                Sequence = _sequenceNumber++,
                Timestamp = ServerTime
            };

            // Use unreliable for frequent state updates
            message.DeliveryMode = DeliveryMode.Unreliable;
            QueueOutgoingMessage(message);

            // Store in history for lag compensation
            _stateHistory.Add(new StateSnapshot
            {
                Timestamp = ServerTime,
                State = state
            });

            // Limit history size
            while (_stateHistory.Count > 60)
            {
                _stateHistory.RemoveAt(0);
            }
        }

        private void SendHeartbeat()
        {
            var message = new NetworkMessage
            {
                Type = MessageType.Heartbeat,
                SenderId = _localPlayerId,
                Timestamp = ServerTime
            };

            QueueOutgoingMessage(message);
        }

        // =============================================================================
        // MESSAGE PROCESSING
        // =============================================================================

        private void ProcessIncomingMessages()
        {
            while (_incomingMessages.Count > 0)
            {
                var message = _incomingMessages.Dequeue();
                ProcessMessage(message);
            }
        }

        private void ProcessMessage(NetworkMessage message)
        {
            // Calculate latency
            if (message.Timestamp > 0)
            {
                var latency = (float)(ServerTime - message.Timestamp) * 1000f;
                _lagCompensation.RecordLatency(latency);
            }

            switch (message.Type)
            {
                case MessageType.PlayerJoined:
                    HandlePlayerJoined(message);
                    break;

                case MessageType.PlayerLeft:
                    HandlePlayerLeft(message);
                    break;

                case MessageType.StateUpdate:
                    HandleStateUpdate(message);
                    break;

                case MessageType.Heartbeat:
                    HandleHeartbeatResponse(message);
                    break;

                case MessageType.TimeSync:
                    HandleTimeSync(message);
                    break;

                case MessageType.Custom:
                    OnMessageReceived?.Invoke(message.SenderId, message);
                    break;

                case MessageType.RoomState:
                    HandleRoomState(message);
                    break;
            }
        }

        private void HandlePlayerJoined(NetworkMessage message)
        {
            if (message.SenderId == _localPlayerId) return;

            var player = new RemotePlayer
            {
                PlayerId = message.SenderId,
                JoinTime = Time.time,
                IsHost = message.Payload?.Contains("\"isHost\":true") ?? false
            };

            _remotePlayers[message.SenderId] = player;

            DeskillzLogger.Debug($"Player joined: {message.SenderId}");
            OnPlayerJoined?.Invoke(player);
            DeskillzEvents.RaisePlayerJoined(message.SenderId);
        }

        private void HandlePlayerLeft(NetworkMessage message)
        {
            if (_remotePlayers.ContainsKey(message.SenderId))
            {
                _remotePlayers.Remove(message.SenderId);
                _stateManager.RemovePlayer(message.SenderId);

                DeskillzLogger.Debug($"Player left: {message.SenderId}");
                OnPlayerLeft?.Invoke(message.SenderId);
                DeskillzEvents.RaisePlayerLeft(message.SenderId);
            }
        }

        private void HandleStateUpdate(NetworkMessage message)
        {
            if (message.SenderId == _localPlayerId) return;

            if (_remotePlayers.TryGetValue(message.SenderId, out var remote))
            {
                var state = _serializer.Deserialize<PlayerState>(message.Payload);
                if (state != null)
                {
                    state.Timestamp = message.Timestamp;
                    remote.AddState(state);
                    _stateManager.SetRemoteState(message.SenderId, state);

                    OnPlayerStateUpdated?.Invoke(message.SenderId, state);
                }
            }
        }

        private void HandleHeartbeatResponse(NetworkMessage message)
        {
            // Update server time offset
            if (message.Timestamp > 0)
            {
                var rtt = (float)(Time.realtimeSinceStartup - message.Timestamp);
                _lagCompensation.RecordLatency(rtt * 1000f);
            }
        }

        private void HandleTimeSync(NetworkMessage message)
        {
            if (double.TryParse(message.Payload, out var serverTime))
            {
                _serverTimeOffset = serverTime - Time.realtimeSinceStartup;
                DeskillzLogger.Debug($"Time synced, offset: {_serverTimeOffset:F3}s");
            }
        }

        private void HandleRoomState(NetworkMessage message)
        {
            // Parse room state with all current players
            var roomState = _serializer.Deserialize<RoomState>(message.Payload);
            if (roomState == null) return;

            foreach (var player in roomState.Players)
            {
                if (player.PlayerId != _localPlayerId && !_remotePlayers.ContainsKey(player.PlayerId))
                {
                    var remote = new RemotePlayer
                    {
                        PlayerId = player.PlayerId,
                        JoinTime = Time.time,
                        IsHost = player.IsHost
                    };
                    _remotePlayers[player.PlayerId] = remote;
                    OnPlayerJoined?.Invoke(remote);
                }
            }
        }

        // =============================================================================
        // WEBSOCKET HANDLERS
        // =============================================================================

        private void HandleWebSocketOpen()
        {
            _isConnected = true;
            _lastSyncTime = Time.time;
            _lastHeartbeatTime = Time.time;

            // Send join message
            SendMessage(new NetworkMessage
            {
                Type = MessageType.PlayerJoined,
                Payload = _serializer.Serialize(new { isHost = _isHost })
            });

            DeskillzLogger.Debug("WebSocket connected");
            OnConnected?.Invoke();
            DeskillzEvents.RaiseConnectionStateChanged(ConnectionState.Connected);
        }

        private void HandleWebSocketMessage(string data)
        {
            try
            {
                var message = _serializer.DeserializeMessage(data);
                if (message != null)
                {
                    _incomingMessages.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to parse message: {ex.Message}");
            }
        }

        private void HandleWebSocketClose(string reason)
        {
            _isConnected = false;

            DeskillzLogger.Debug($"WebSocket closed: {reason}");
            OnDisconnected?.Invoke(reason);
            DeskillzEvents.RaiseConnectionStateChanged(ConnectionState.Disconnected);
        }

        private void HandleWebSocketError(string error)
        {
            DeskillzLogger.Error($"WebSocket error: {error}");
        }

        // =============================================================================
        // REMOTE PLAYER UPDATE
        // =============================================================================

        private void UpdateRemotePlayers()
        {
            foreach (var kvp in _remotePlayers)
            {
                kvp.Value.Update(Time.deltaTime);
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleMatchReady(MatchData match)
        {
            if (match.IsRealtime)
            {
                _localPlayerId = Deskillz.CurrentPlayer?.Id;
                Connect(match.MatchId, _localPlayerId, match.LocalPlayerScore == 0);
            }
        }

        private void HandleMatchComplete(MatchResult result)
        {
            Disconnect();
        }

        // =============================================================================
        // LAG COMPENSATION
        // =============================================================================

        /// <summary>
        /// Get historical state for lag compensation.
        /// </summary>
        public PlayerState GetStateAtTime(string playerId, double time)
        {
            if (playerId == _localPlayerId)
            {
                // Return from local history
                for (int i = _stateHistory.Count - 1; i >= 0; i--)
                {
                    if (_stateHistory[i].Timestamp <= time)
                    {
                        return _stateHistory[i].State;
                    }
                }
            }
            else if (_remotePlayers.TryGetValue(playerId, out var remote))
            {
                return remote.GetStateAtTime(time);
            }

            return null;
        }

        /// <summary>
        /// Perform lag-compensated raycast/collision check.
        /// </summary>
        public void PerformLagCompensatedAction(double clientTime, Action action)
        {
            // Rewind all players to the specified time
            var originalStates = new Dictionary<string, PlayerState>();

            foreach (var kvp in _remotePlayers)
            {
                originalStates[kvp.Key] = kvp.Value.CurrentState;
                var historicalState = kvp.Value.GetStateAtTime(clientTime);
                if (historicalState != null)
                {
                    kvp.Value.ApplyState(historicalState);
                }
            }

            // Perform the action
            action?.Invoke();

            // Restore original states
            foreach (var kvp in _remotePlayers)
            {
                if (originalStates.TryGetValue(kvp.Key, out var original))
                {
                    kvp.Value.ApplyState(original);
                }
            }
        }
    }

    // =============================================================================
    // SUPPORTING CLASSES
    // =============================================================================

    /// <summary>
    /// Room state snapshot from server.
    /// </summary>
    [Serializable]
    internal class RoomState
    {
        public string RoomId;
        public List<RoomPlayer> Players = new List<RoomPlayer>();
        public double ServerTime;
    }

    [Serializable]
    internal class RoomPlayer
    {
        public string PlayerId;
        public bool IsHost;
        public double JoinTime;
    }

    /// <summary>
    /// State snapshot for history.
    /// </summary>
    internal class StateSnapshot
    {
        public double Timestamp;
        public PlayerState State;
    }
}

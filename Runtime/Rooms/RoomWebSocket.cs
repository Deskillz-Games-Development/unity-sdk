// =============================================================================
// Deskillz SDK for Unity - Room WebSocket Client
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Rooms
{
    /// <summary>
    /// WebSocket client for real-time private room updates.
    /// Handles connection, reconnection, and event dispatching.
    /// </summary>
    internal class RoomWebSocket
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when room state is received</summary>
        public event Action<PrivateRoom> OnStateUpdate;

        /// <summary>Called when a player joins</summary>
        public event Action<RoomPlayer> OnPlayerJoined;

        /// <summary>Called when a player leaves</summary>
        public event Action<string> OnPlayerLeft;

        /// <summary>Called when a player is kicked</summary>
        public event Action<string> OnPlayerKicked;

        /// <summary>Called when a player's ready status changes</summary>
        public event Action<string, bool, bool> OnPlayerReady; // id, isReady, allReady

        /// <summary>Called when all players are ready</summary>
        public event Action<int> OnAllReady; // playerCount

        /// <summary>Called when countdown starts</summary>
        public event Action<int> OnCountdownStarted;

        /// <summary>Called each countdown tick</summary>
        public event Action<int> OnCountdownTick;

        /// <summary>Called when match is launching</summary>
        public event Action<MatchLaunchData> OnLaunching;

        /// <summary>Called when room is cancelled</summary>
        public event Action<string> OnCancelled;

        /// <summary>Called when current user is kicked</summary>
        public event Action<string> OnKicked;

        /// <summary>Called when chat message is received</summary>
        public event Action<string, string, string> OnChat; // id, username, message

        /// <summary>Called on connection error</summary>
        public event Action<string> OnError;

        /// <summary>Called when connected</summary>
        public event Action OnConnected;

        /// <summary>Called when disconnected</summary>
        public event Action OnDisconnected;

        // =====================================================================
        // STATE
        // =====================================================================

        private WebSocketClient _socket;
        private string _currentRoomId;
        private bool _isConnecting;
        private bool _shouldReconnect;
        private int _reconnectAttempts;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private const float RECONNECT_DELAY = 2f;

        /// <summary>Whether currently connected to a room</summary>
        public bool IsConnected => _socket?.IsConnected ?? false;

        /// <summary>Current room ID being subscribed to</summary>
        public string CurrentRoomId => _currentRoomId;

        // =====================================================================
        // CONNECTION
        // =====================================================================

        /// <summary>
        /// Connect to a room and subscribe to updates
        /// </summary>
        public void Connect(string roomId)
        {
            if (_isConnecting)
            {
                DeskillzLogger.Warning("[RoomWebSocket] Already connecting");
                return;
            }

            _currentRoomId = roomId;
            _shouldReconnect = true;
            _reconnectAttempts = 0;

            ConnectInternal();
        }

        /// <summary>
        /// Disconnect from the current room
        /// </summary>
        public void Disconnect()
        {
            _shouldReconnect = false;
            _currentRoomId = null;

            if (_socket != null)
            {
                // Unsubscribe before disconnecting
                SendUnsubscribe();

                _socket.OnConnected -= HandleConnected;
                _socket.OnDisconnected -= HandleDisconnected;
                _socket.OnMessage -= HandleMessage;
                _socket.OnError -= HandleError;
                _socket.Disconnect();
                _socket = null;
            }

            DeskillzLogger.Debug("[RoomWebSocket] Disconnected");
        }

        private void ConnectInternal()
        {
            if (_socket != null && _socket.IsConnected)
            {
                // Already connected, just subscribe to new room
                SendSubscribe();
                return;
            }

            _isConnecting = true;

            var wsUrl = GetWebSocketUrl();
            var authToken = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;

            _socket = new WebSocketClient(wsUrl);
            _socket.SetAuthToken(authToken);

            _socket.OnConnected += HandleConnected;
            _socket.OnDisconnected += HandleDisconnected;
            _socket.OnMessage += HandleMessage;
            _socket.OnError += HandleError;

            _socket.Connect();

            DeskillzLogger.Debug($"[RoomWebSocket] Connecting to {wsUrl}");
        }

        private string GetWebSocketUrl()
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? "https://api.deskillz.games";
            var wsUrl = baseUrl
                .Replace("https://", "wss://")
                .Replace("http://", "ws://");
            return $"{wsUrl}/lobby";
        }

        // =====================================================================
        // SEND MESSAGES
        // =====================================================================

        /// <summary>
        /// Subscribe to room updates
        /// </summary>
        private void SendSubscribe()
        {
            if (_socket == null || !_socket.IsConnected || string.IsNullOrEmpty(_currentRoomId))
                return;

            var message = new SocketMessage
            {
                @event = "room:subscribe",
                data = new { roomId = _currentRoomId }
            };

            _socket.Send(JsonUtility.ToJson(message));
            DeskillzLogger.Debug($"[RoomWebSocket] Subscribed to room {_currentRoomId}");
        }

        /// <summary>
        /// Unsubscribe from room updates
        /// </summary>
        private void SendUnsubscribe()
        {
            if (_socket == null || !_socket.IsConnected || string.IsNullOrEmpty(_currentRoomId))
                return;

            var message = new SocketMessage
            {
                @event = "room:unsubscribe",
                data = new { roomId = _currentRoomId }
            };

            _socket.Send(JsonUtility.ToJson(message));
            DeskillzLogger.Debug($"[RoomWebSocket] Unsubscribed from room {_currentRoomId}");
        }

        /// <summary>
        /// Send ready status
        /// </summary>
        public void SendReady(string roomId, bool isReady)
        {
            if (_socket == null || !_socket.IsConnected)
            {
                DeskillzLogger.Warning("[RoomWebSocket] Cannot send ready: not connected");
                return;
            }

            var message = new SocketMessage
            {
                @event = "room:ready",
                data = new { roomId, isReady }
            };

            _socket.Send(JsonUtility.ToJson(message));
            DeskillzLogger.Debug($"[RoomWebSocket] Sent ready={isReady} for room {roomId}");
        }

        /// <summary>
        /// Send chat message
        /// </summary>
        public void SendChat(string roomId, string chatMessage)
        {
            if (_socket == null || !_socket.IsConnected)
                return;

            if (string.IsNullOrEmpty(chatMessage) || chatMessage.Length > 500)
                return;

            var message = new SocketMessage
            {
                @event = "room:chat",
                data = new { roomId, message = chatMessage }
            };

            _socket.Send(JsonUtility.ToJson(message));
        }

        /// <summary>
        /// Request to start the match (host only)
        /// </summary>
        public void SendStart(string roomId)
        {
            if (_socket == null || !_socket.IsConnected)
                return;

            var message = new SocketMessage
            {
                @event = "room:start",
                data = new { roomId }
            };

            _socket.Send(JsonUtility.ToJson(message));
            DeskillzLogger.Debug($"[RoomWebSocket] Sent start request for room {roomId}");
        }

        /// <summary>
        /// Request to kick a player (host only)
        /// </summary>
        public void SendKick(string roomId, string targetUserId)
        {
            if (_socket == null || !_socket.IsConnected)
                return;

            var message = new SocketMessage
            {
                @event = "room:kick",
                data = new { roomId, targetUserId }
            };

            _socket.Send(JsonUtility.ToJson(message));
        }

        /// <summary>
        /// Request to cancel the room (host only)
        /// </summary>
        public void SendCancel(string roomId)
        {
            if (_socket == null || !_socket.IsConnected)
                return;

            var message = new SocketMessage
            {
                @event = "room:cancel",
                data = new { roomId }
            };

            _socket.Send(JsonUtility.ToJson(message));
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandleConnected()
        {
            _isConnecting = false;
            _reconnectAttempts = 0;

            DeskillzLogger.Debug("[RoomWebSocket] Connected");

            // Subscribe to room once connected
            SendSubscribe();

            OnConnected?.Invoke();
        }

        private void HandleDisconnected()
        {
            _isConnecting = false;

            DeskillzLogger.Debug("[RoomWebSocket] Disconnected");
            OnDisconnected?.Invoke();

            // Attempt reconnection if needed
            if (_shouldReconnect && _reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                _reconnectAttempts++;
                DeskillzLogger.Debug($"[RoomWebSocket] Reconnecting (attempt {_reconnectAttempts})...");
                DeskillzManager.Instance?.StartCoroutine(ReconnectCoroutine());
            }
        }

        private IEnumerator ReconnectCoroutine()
        {
            yield return new WaitForSeconds(RECONNECT_DELAY * _reconnectAttempts);
            
            if (_shouldReconnect)
            {
                ConnectInternal();
            }
        }

        private void HandleError(string error)
        {
            DeskillzLogger.Error($"[RoomWebSocket] Error: {error}");
            OnError?.Invoke(error);
        }

        private void HandleMessage(string messageJson)
        {
            try
            {
                var envelope = JsonUtility.FromJson<SocketEnvelope>(messageJson);
                
                if (string.IsNullOrEmpty(envelope?.@event))
                {
                    DeskillzLogger.Warning("[RoomWebSocket] Received message without event type");
                    return;
                }

                ProcessEvent(envelope.@event, envelope.data);
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[RoomWebSocket] Error parsing message: {ex.Message}");
            }
        }

        private void ProcessEvent(string eventType, string dataJson)
        {
            DeskillzLogger.Debug($"[RoomWebSocket] Event: {eventType}");

            switch (eventType)
            {
                case "room:state":
                    var room = JsonUtility.FromJson<PrivateRoom>(dataJson);
                    OnStateUpdate?.Invoke(room);
                    break;

                case "private-room:player-joined":
                    var joinedData = JsonUtility.FromJson<PlayerJoinedEvent>(dataJson);
                    var joinedPlayer = new RoomPlayer
                    {
                        Id = joinedData.id,
                        Username = joinedData.username,
                        AvatarUrl = joinedData.avatarUrl,
                        IsReady = false,
                        IsAdmin = false,
                        JoinedAt = DateTime.UtcNow
                    };
                    OnPlayerJoined?.Invoke(joinedPlayer);
                    break;

                case "private-room:player-left":
                    var leftData = JsonUtility.FromJson<PlayerLeftEvent>(dataJson);
                    OnPlayerLeft?.Invoke(leftData.id);
                    break;

                case "private-room:player-kicked":
                    var kickedData = JsonUtility.FromJson<PlayerLeftEvent>(dataJson);
                    OnPlayerKicked?.Invoke(kickedData.id);
                    break;

                case "private-room:player-ready":
                    var readyData = JsonUtility.FromJson<PlayerReadyEvent>(dataJson);
                    OnPlayerReady?.Invoke(readyData.id, readyData.isReady, readyData.allReady);
                    break;

                case "private-room:all-ready":
                    var allReadyData = JsonUtility.FromJson<AllReadyEvent>(dataJson);
                    OnAllReady?.Invoke(allReadyData.playerCount);
                    break;

                case "private-room:countdown-started":
                    var countdownStartData = JsonUtility.FromJson<CountdownEvent>(dataJson);
                    OnCountdownStarted?.Invoke(countdownStartData.seconds);
                    break;

                case "private-room:countdown-tick":
                    var tickData = JsonUtility.FromJson<CountdownEvent>(dataJson);
                    OnCountdownTick?.Invoke(tickData.seconds);
                    break;

                case "private-room:launching":
                    var launchData = JsonUtility.FromJson<LaunchingEvent>(dataJson);
                    var matchLaunchData = new MatchLaunchData
                    {
                        MatchId = launchData.matchId,
                        DeepLink = launchData.deepLink,
                        Token = launchData.token,
                        GameSessionId = launchData.gameSessionId
                    };
                    OnLaunching?.Invoke(matchLaunchData);
                    break;

                case "private-room:cancelled":
                    var cancelledData = JsonUtility.FromJson<CancelledEvent>(dataJson);
                    OnCancelled?.Invoke(cancelledData.reason);
                    break;

                case "private-room:kicked":
                    var kickedMeData = JsonUtility.FromJson<KickedEvent>(dataJson);
                    OnKicked?.Invoke(kickedMeData.reason);
                    break;

                case "private-room:chat":
                    var chatData = JsonUtility.FromJson<ChatEvent>(dataJson);
                    OnChat?.Invoke(chatData.id, chatData.username, chatData.message);
                    break;

                case "error":
                    var errorData = JsonUtility.FromJson<ErrorEvent>(dataJson);
                    OnError?.Invoke(errorData.message);
                    break;

                default:
                    DeskillzLogger.Debug($"[RoomWebSocket] Unknown event: {eventType}");
                    break;
            }
        }

        // =====================================================================
        // INTERNAL MODELS
        // =====================================================================

        [Serializable]
        private class SocketMessage
        {
            public string @event;
            public object data;
        }

        [Serializable]
        private class SocketEnvelope
        {
            public string @event;
            public string data;
        }

        [Serializable]
        private class AllReadyEvent
        {
            public bool canStart;
            public int playerCount;
        }

        [Serializable]
        private class KickedEvent
        {
            public string roomId;
            public string reason;
        }

        [Serializable]
        private class ErrorEvent
        {
            public string message;
            public string code;
        }
    }

    // =========================================================================
    // WEBSOCKET CLIENT (Internal Implementation)
    // =========================================================================

    /// <summary>
    /// Low-level WebSocket client wrapper.
    /// Uses Unity's native WebSocket support or a fallback library.
    /// </summary>
    internal class WebSocketClient
    {
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessage;
        public event Action<string> OnError;

        private readonly string _url;
        private string _authToken;
        private bool _isConnected;

        // Platform-specific implementation would go here
        // For simplicity, this uses a generic interface
        private IWebSocketImpl _impl;

        public bool IsConnected => _isConnected;

        public WebSocketClient(string url)
        {
            _url = url;
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        public void Connect()
        {
            // Create platform-specific implementation
            #if UNITY_WEBGL && !UNITY_EDITOR
            _impl = new WebGLWebSocket(_url);
            #else
            _impl = new NativeWebSocket(_url);
            #endif

            _impl.OnOpen += () =>
            {
                _isConnected = true;
                OnConnected?.Invoke();
            };

            _impl.OnClose += () =>
            {
                _isConnected = false;
                OnDisconnected?.Invoke();
            };

            _impl.OnMessage += (msg) => OnMessage?.Invoke(msg);
            _impl.OnError += (err) => OnError?.Invoke(err);

            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(_authToken))
            {
                headers["Authorization"] = $"Bearer {_authToken}";
            }

            _impl.Connect(headers);
        }

        public void Send(string message)
        {
            if (_isConnected && _impl != null)
            {
                _impl.Send(message);
            }
        }

        public void Disconnect()
        {
            _impl?.Close();
            _isConnected = false;
        }
    }

    /// <summary>
    /// WebSocket implementation interface
    /// </summary>
    internal interface IWebSocketImpl
    {
        event Action OnOpen;
        event Action OnClose;
        event Action<string> OnMessage;
        event Action<string> OnError;

        void Connect(Dictionary<string, string> headers);
        void Send(string message);
        void Close();
    }

    /// <summary>
    /// Native WebSocket implementation for standalone builds
    /// </summary>
    internal class NativeWebSocket : IWebSocketImpl
    {
        public event Action OnOpen;
        public event Action OnClose;
        public event Action<string> OnMessage;
        public event Action<string> OnError;

        private readonly string _url;

        public NativeWebSocket(string url)
        {
            _url = url;
        }

        public void Connect(Dictionary<string, string> headers)
        {
            // Implementation would use System.Net.WebSockets or a third-party library
            // For now, this is a placeholder
            DeskillzLogger.Debug($"[NativeWebSocket] Connecting to {_url}");
            
            // Simulate connection for structure purposes
            // Real implementation would use:
            // - ClientWebSocket for .NET Standard 2.1
            // - websocket-sharp for older Unity versions
            // - Best WebSocket for comprehensive support
        }

        public void Send(string message)
        {
            DeskillzLogger.Debug($"[NativeWebSocket] Sending: {message}");
        }

        public void Close()
        {
            DeskillzLogger.Debug("[NativeWebSocket] Closing");
            OnClose?.Invoke();
        }
    }

    /// <summary>
    /// WebGL WebSocket implementation using JavaScript interop
    /// </summary>
    internal class WebGLWebSocket : IWebSocketImpl
    {
        public event Action OnOpen;
        public event Action OnClose;
        public event Action<string> OnMessage;
        public event Action<string> OnError;

        private readonly string _url;

        public WebGLWebSocket(string url)
        {
            _url = url;
        }

        public void Connect(Dictionary<string, string> headers)
        {
            // Implementation would use JavaScript interop
            // jslib functions to create and manage WebSocket
            DeskillzLogger.Debug($"[WebGLWebSocket] Connecting to {_url}");
        }

        public void Send(string message)
        {
            DeskillzLogger.Debug($"[WebGLWebSocket] Sending: {message}");
        }

        public void Close()
        {
            DeskillzLogger.Debug("[WebGLWebSocket] Closing");
            OnClose?.Invoke();
        }
    }
}
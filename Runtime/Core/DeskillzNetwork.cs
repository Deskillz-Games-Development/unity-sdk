// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Deskillz.Realtime; // [P1-3.7.0]

namespace Deskillz
{
    /// <summary>
    /// Network layer for the Deskillz SDK.
    /// Handles REST API calls and WebSocket connections.
    /// </summary>
    internal class DeskillzNetwork
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        private const int DEFAULT_TIMEOUT = 30;
        private const int MAX_RETRIES = 3;
        private const float RETRY_DELAY = 1f;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private readonly string _baseUrl;
        private readonly string _wsUrl;
        private readonly string _apiKey;
        private string _authToken;
        private readonly Dictionary<string, string> _defaultHeaders;
        
        // WebSocket state
        private SocketIOClient _socket;
        private string _currentMatchId;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private int _reconnectAttempts;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private const float RECONNECT_DELAY = 2f;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public ConnectionState ConnectionState => _connectionState;
        public bool IsConnected => _connectionState == ConnectionState.Connected;

        // =============================================================================
        // CONSTRUCTOR
        // =============================================================================

        public DeskillzNetwork(string baseUrl, string wsUrl, string apiKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _wsUrl = wsUrl.TrimEnd('/');
            _apiKey = apiKey;

            _defaultHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Accept", "application/json" },
                { "X-SDK-Version", DeskillzConfig.SDK_VERSION },
                { "X-SDK-Platform", Application.platform.ToString() }
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _defaultHeaders["X-API-Key"] = _apiKey;
            }
        }

        // =============================================================================
        // AUTHENTICATION
        // =============================================================================

        public void SetAuthToken(string token)
        {
            _authToken = token;
            if (!string.IsNullOrEmpty(token))
            {
                _defaultHeaders["Authorization"] = $"Bearer {token}";
            }
            else
            {
                _defaultHeaders.Remove("Authorization");
            }
        }

        public void ClearAuthToken()
        {
            _authToken = null;
            _defaultHeaders.Remove("Authorization");
        }

        // =============================================================================
        // HTTP METHODS
        // =============================================================================

        /// <summary>
        /// Perform a GET request.
        /// </summary>
        public IEnumerator Get<T>(string endpoint, Action<T> onSuccess, Action<DeskillzError> onError)
        {
            yield return SendRequest<T>("GET", endpoint, null, onSuccess, onError);
        }

        /// <summary>
        /// Perform a POST request.
        /// </summary>
        public IEnumerator Post<T>(string endpoint, object body, Action<T> onSuccess, Action<DeskillzError> onError)
        {
            var json = DeskillzJson.Serialize(body);
            yield return SendRequest<T>("POST", endpoint, json, onSuccess, onError);
        }

        /// <summary>
        /// Perform a PUT request.
        /// </summary>
        public IEnumerator Put<T>(string endpoint, object body, Action<T> onSuccess, Action<DeskillzError> onError)
        {
            var json = DeskillzJson.Serialize(body);
            yield return SendRequest<T>("PUT", endpoint, json, onSuccess, onError);
        }

        /// <summary>
        /// Perform a DELETE request.
        /// </summary>
        public IEnumerator Delete<T>(string endpoint, Action<T> onSuccess, Action<DeskillzError> onError)
        {
            yield return SendRequest<T>("DELETE", endpoint, null, onSuccess, onError);
        }

        /// <summary>
        /// Core request method with retry logic.
        /// </summary>
        private IEnumerator SendRequest<T>(string method, string endpoint, string body, 
            Action<T> onSuccess, Action<DeskillzError> onError, int attempt = 0)
        {
            var url = endpoint.StartsWith("http") ? endpoint : $"{_baseUrl}{endpoint}";
            var startTime = Time.realtimeSinceStartup;

            DeskillzLogger.LogRequest(method, url, body);

            using (var request = CreateRequest(method, url, body))
            {
                request.timeout = DEFAULT_TIMEOUT;

                yield return request.SendWebRequest();

                var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
                var responseBody = request.downloadHandler?.text;

                DeskillzLogger.LogResponse(method, url, (int)request.responseCode, responseBody, duration);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(responseBody))
                        {
                            onSuccess?.Invoke(default);
                        }
                        else
                        {
                            onSuccess?.Invoke(DeskillzJson.Deserialize<T>(responseBody));
                        }
                    }
                    catch (Exception ex)
                    {
                        DeskillzLogger.Error("Failed to parse response", ex);
                        onError?.Invoke(new DeskillzError(ErrorCode.Unknown, "Failed to parse response"));
                    }
                }
                else
                {
                    var error = DeskillzJson.WithServerMessage(HandleRequestError(request), responseBody);

                    // Retry on transient errors
                    if (error.IsRecoverable && attempt < MAX_RETRIES)
                    {
                        DeskillzLogger.Warning($"Request failed, retrying ({attempt + 1}/{MAX_RETRIES})...");
                        yield return new WaitForSeconds(RETRY_DELAY * (attempt + 1));
                        yield return SendRequest<T>(method, endpoint, body, onSuccess, onError, attempt + 1);
                    }
                    else
                    {
                        onError?.Invoke(error);
                    }
                }
            }
        }

        private UnityWebRequest CreateRequest(string method, string url, string body)
        {
            UnityWebRequest request;

            switch (method.ToUpper())
            {
                case "GET":
                    request = UnityWebRequest.Get(url);
                    break;
                case "POST":
                    request = new UnityWebRequest(url, "POST");
                    if (!string.IsNullOrEmpty(body))
                    {
                        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    }
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case "PUT":
                    request = UnityWebRequest.Put(url, body ?? "");
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case "DELETE":
                    request = UnityWebRequest.Delete(url);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
                default:
                    throw new ArgumentException($"Unsupported HTTP method: {method}");
            }

            // Apply headers
            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            return request;
        }

        private DeskillzError HandleRequestError(UnityWebRequest request)
        {
            var statusCode = (int)request.responseCode;
            var isRecoverable = true;

            ErrorCode errorCode;
            string message;

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                errorCode = ErrorCode.NetworkError;
                message = "Network connection failed. Please check your internet connection.";
            }
            else if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                errorCode = ErrorCode.Unknown;
                message = "Failed to process response data.";
                isRecoverable = false;
            }
            else
            {
                switch (statusCode)
                {
                    case 400:
                        errorCode = ErrorCode.Unknown;
                        message = "Invalid request.";
                        isRecoverable = false;
                        break;
                    case 401:
                        errorCode = ErrorCode.NotAuthenticated;
                        message = "Authentication required.";
                        isRecoverable = false;
                        break;
                    case 403:
                        errorCode = ErrorCode.Unauthorized;
                        message = "Access denied.";
                        isRecoverable = false;
                        break;
                    case 404:
                        errorCode = ErrorCode.Unknown;
                        message = "Resource not found.";
                        isRecoverable = false;
                        break;
                    case 408:
                    case 504:
                        errorCode = ErrorCode.Timeout;
                        message = "Request timed out.";
                        break;
                    case 429:
                        errorCode = ErrorCode.ServerError;
                        message = "Too many requests. Please wait.";
                        break;
                    case >= 500:
                        errorCode = ErrorCode.ServerError;
                        message = "Server error. Please try again later.";
                        break;
                    default:
                        errorCode = ErrorCode.Unknown;
                        message = request.error ?? "An unknown error occurred.";
                        break;
                }
            }

            return new DeskillzError(errorCode, message, isRecoverable)
            {
                Details = $"Status: {statusCode}, Error: {request.error}"
            };
        }

        // =============================================================================
        // REALTIME (Socket.IO "/" namespace: match relay)  [P1-3.7.0]
        // =============================================================================

        /// <summary>Connect to the platform socket and, when matchId is given, join that match room.</summary>
        public void Connect(string matchId = null)
        {
            _currentMatchId = matchId ?? _currentMatchId;
            if (_socket != null && (_connectionState == ConnectionState.Connected || _connectionState == ConnectionState.Connecting))
            {
                if (!string.IsNullOrEmpty(matchId) && IsConnected) JoinMatchRoom(matchId);
                return;
            }

            _connectionState = ConnectionState.Connecting;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);

            if (_socket == null)
            {
                _socket = new SocketIOClient(_wsUrl, "/", () => _authToken);
                _socket.OnConnected += OnSocketConnected;
                _socket.OnDisconnected += OnSocketDisconnected;
                _socket.OnError += OnSocketError;
                _socket.OnEvent += OnSocketEvent;
            }
            DeskillzLogger.LogWebSocket("Connecting", _wsUrl + " ns=/");
            _socket.Connect();
        }

        /// <summary>Disconnect from the platform socket.</summary>
        public void Disconnect()
        {
            if (_socket != null)
            {
                _socket.OnConnected -= OnSocketConnected;
                _socket.OnDisconnected -= OnSocketDisconnected;
                _socket.OnError -= OnSocketError;
                _socket.OnEvent -= OnSocketEvent;
                _socket.Dispose();
                _socket = null;
            }
            _connectionState = ConnectionState.Disconnected;
            _reconnectAttempts = 0;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
        }

        /// <summary>Emit any event on the "/" namespace.</summary>
        public void Emit(string eventName, object data = null, Action<string> ack = null)
        {
            if (!IsConnected)
            {
                DeskillzLogger.Warning($"Cannot emit {eventName}: not connected");
                return;
            }
            _socket.Emit(eventName, data, ack);
            DeskillzLogger.LogWebSocket("Sent", eventName);
        }

        /// <summary>match:join_room { matchId } -- required before any relay.</summary>
        public void JoinMatchRoom(string matchId)
        {
            _currentMatchId = matchId;
            Emit("match:join_room", new { matchId }, ack =>
            {
                var err = DeskillzJson.GetString(ack, "error");
                if (!string.IsNullOrEmpty(err))
                {
                    DeskillzLogger.Error($"match:join_room refused: {err}");
                    DeskillzEvents.RaiseError(new DeskillzError(ErrorCode.WebSocketError, "Join match room failed: " + err));
                }
                else DeskillzLogger.LogWebSocket("Joined match room", matchId);
            });
        }

        /// <summary>
        /// Relay a game event to the other seats via match:relay. gameEvent must be one of
        /// game:state, game:hand, game:action, game:round-end, game:game-end, game:request-sync,
        /// game:chat, game:round-ready, game:round-ready-state, game:ping, game:pong.
        /// </summary>
        public void Relay(string gameEvent, object payload)
        {
            if (string.IsNullOrEmpty(_currentMatchId))
            {
                DeskillzLogger.Warning("Cannot relay: no match room joined");
                return;
            }
            Emit("match:relay", new { matchId = _currentMatchId, @event = gameEvent, payload }, ack =>
            {
                var err = DeskillzJson.GetString(ack, "error");
                if (!string.IsNullOrEmpty(err)) DeskillzLogger.Warning($"match:relay {gameEvent} refused: {err}");
            });
        }

        /// <summary>Legacy multiplayer message: relayed as game:state (StateSync) or game:action.</summary>
        public void Send(global::Deskillz.Multiplayer.NetworkMessage message)
        {
            if (message == null) return;
            var ev = IsStateMessage(message.Type) ? "game:state" : "game:action";
            Relay(ev, message);
        }

        private static bool IsStateMessage(global::Deskillz.Multiplayer.MessageType type) // [P1-3.7.0 fixB]
        {
            switch (type)
            {
                case global::Deskillz.Multiplayer.MessageType.StateUpdate:
                case global::Deskillz.Multiplayer.MessageType.FullSync:
                case global::Deskillz.Multiplayer.MessageType.DeltaSync:
                case global::Deskillz.Multiplayer.MessageType.RoomState:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Send a raw JSON string as a game:action relay payload.</summary>
        public void SendRaw(string json) => Relay("game:action", json);

        private void OnSocketConnected()
        {
            DeskillzLogger.LogWebSocket("Connected");
            _connectionState = ConnectionState.Connected;
            _reconnectAttempts = 0;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
            DeskillzEvents.RaiseConnected();
            if (!string.IsNullOrEmpty(_currentMatchId)) JoinMatchRoom(_currentMatchId);
        }

        private void OnSocketEvent(string eventName, string dataJson)
        {
            DeskillzLogger.LogWebSocket("Received", eventName);
            DeskillzEvents.RaiseSocketMessage(eventName, dataJson);
            if (eventName.StartsWith("game:"))
            {
                try
                {
                    var message = DeskillzJson.Deserialize<global::Deskillz.Multiplayer.NetworkMessage>(dataJson);
                    if (message != null) DeskillzEvents.RaiseMessageReceived(message);
                }
                catch (Exception)
                {
                    // Not every relayed payload is a NetworkMessage; subscribers use OnSocketMessage for raw JSON.
                }
            }
        }

        private void OnSocketError(string error)
        {
            DeskillzLogger.Error($"Socket error: {error}");
            DeskillzEvents.RaiseError(new DeskillzError(ErrorCode.WebSocketError, error));
        }

        private void OnSocketDisconnected(string reason)
        {
            DeskillzLogger.LogWebSocket("Closed", reason);
            var wasConnected = _connectionState == ConnectionState.Connected;
            _connectionState = reason == "client" ? ConnectionState.Disconnected : ConnectionState.Reconnecting;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
            if (wasConnected) DeskillzEvents.RaiseDisconnected();
            if (_connectionState == ConnectionState.Reconnecting) DeskillzEvents.RaiseReconnecting(++_reconnectAttempts);
        }

        internal void ExecuteReconnect()
        {
            if (_connectionState != ConnectionState.Reconnecting) return;
            Connect(_currentMatchId);
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        public void Dispose()
        {
            Disconnect();
        }
    }
}

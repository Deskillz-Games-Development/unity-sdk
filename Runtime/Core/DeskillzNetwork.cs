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
        private WebSocketClient _webSocket;
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
            var json = body != null ? JsonUtility.ToJson(body) : null;
            yield return SendRequest<T>("POST", endpoint, json, onSuccess, onError);
        }

        /// <summary>
        /// Perform a PUT request.
        /// </summary>
        public IEnumerator Put<T>(string endpoint, object body, Action<T> onSuccess, Action<DeskillzError> onError)
        {
            var json = body != null ? JsonUtility.ToJson(body) : null;
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
                            var response = JsonUtility.FromJson<ApiResponse<T>>(responseBody);
                            if (response.Success)
                            {
                                onSuccess?.Invoke(response.Data);
                            }
                            else
                            {
                                onError?.Invoke(response.Error ?? new DeskillzError(ErrorCode.Unknown, "Request failed"));
                            }
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
                    var error = HandleRequestError(request);

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
        // WEBSOCKET METHODS
        // =============================================================================

        /// <summary>
        /// Connect to WebSocket server for real-time features.
        /// </summary>
        public void Connect(string matchId = null)
        {
            if (_connectionState == ConnectionState.Connected || 
                _connectionState == ConnectionState.Connecting)
            {
                DeskillzLogger.Warning("Already connected or connecting");
                return;
            }

            _connectionState = ConnectionState.Connecting;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);

            var wsUrl = BuildWebSocketUrl(matchId);
            
            _webSocket = new WebSocketClient();
            _webSocket.OnOpen += OnWebSocketOpen;
            _webSocket.OnMessage += OnWebSocketMessage;
            _webSocket.OnError += OnWebSocketError;
            _webSocket.OnClose += OnWebSocketClose;

            DeskillzLogger.LogWebSocket("Connecting", wsUrl);
            _webSocket.Connect(wsUrl);
        }

        /// <summary>
        /// Disconnect from WebSocket server.
        /// </summary>
        public void Disconnect()
        {
            if (_webSocket != null)
            {
                _webSocket.OnOpen -= OnWebSocketOpen;
                _webSocket.OnMessage -= OnWebSocketMessage;
                _webSocket.OnError -= OnWebSocketError;
                _webSocket.OnClose -= OnWebSocketClose;
                _webSocket.Close();
                _webSocket = null;
            }

            _connectionState = ConnectionState.Disconnected;
            _reconnectAttempts = 0;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
        }

        /// <summary>
        /// Send a message through WebSocket.
        /// </summary>
        public void Send(NetworkMessage message)
        {
            if (!IsConnected)
            {
                DeskillzLogger.Warning("Cannot send message: not connected");
                return;
            }

            var json = JsonUtility.ToJson(message);
            _webSocket.Send(json);
            DeskillzLogger.LogWebSocket("Sent", $"{message.Type}");
        }

        /// <summary>
        /// Send raw JSON through WebSocket.
        /// </summary>
        public void SendRaw(string json)
        {
            if (!IsConnected)
            {
                DeskillzLogger.Warning("Cannot send message: not connected");
                return;
            }

            _webSocket.Send(json);
            DeskillzLogger.LogWebSocket("Sent", json);
        }

        private string BuildWebSocketUrl(string matchId)
        {
            var url = _wsUrl;
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(_authToken))
            {
                queryParams.Add($"token={Uri.EscapeDataString(_authToken)}");
            }

            if (!string.IsNullOrEmpty(matchId))
            {
                queryParams.Add($"match_id={Uri.EscapeDataString(matchId)}");
            }

            if (!string.IsNullOrEmpty(_apiKey))
            {
                queryParams.Add($"api_key={Uri.EscapeDataString(_apiKey)}");
            }

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            return url;
        }

        // =============================================================================
        // WEBSOCKET EVENT HANDLERS
        // =============================================================================

        private void OnWebSocketOpen()
        {
            DeskillzLogger.LogWebSocket("Connected");
            _connectionState = ConnectionState.Connected;
            _reconnectAttempts = 0;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
            DeskillzEvents.RaiseConnected();
        }

        private void OnWebSocketMessage(string data)
        {
            DeskillzLogger.LogWebSocket("Received", data);

            try
            {
                var message = JsonUtility.FromJson<NetworkMessage>(data);
                DeskillzEvents.RaiseMessageReceived(message);
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Failed to parse WebSocket message", ex);
            }
        }

        private void OnWebSocketError(string error)
        {
            DeskillzLogger.Error($"WebSocket error: {error}");
            DeskillzEvents.RaiseError(new DeskillzError(ErrorCode.WebSocketError, error));
        }

        private void OnWebSocketClose(int code, string reason)
        {
            DeskillzLogger.LogWebSocket("Closed", $"Code: {code}, Reason: {reason}");
            
            var wasConnected = _connectionState == ConnectionState.Connected;
            _connectionState = ConnectionState.Disconnected;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
            
            if (wasConnected)
            {
                DeskillzEvents.RaiseDisconnected();
                AttemptReconnect();
            }
        }

        private void AttemptReconnect()
        {
            if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS)
            {
                DeskillzLogger.Error("Max reconnection attempts reached");
                DeskillzEvents.RaiseReconnectionFailed(
                    new DeskillzError(ErrorCode.WebSocketError, "Failed to reconnect after multiple attempts", false));
                return;
            }

            _reconnectAttempts++;
            _connectionState = ConnectionState.Reconnecting;
            DeskillzEvents.RaiseConnectionStateChanged(_connectionState);
            DeskillzEvents.RaiseReconnecting(_reconnectAttempts);

            DeskillzLogger.Info($"Attempting reconnect in {RECONNECT_DELAY}s (attempt {_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})");
            
            // Schedule reconnect through DeskillzManager
            DeskillzManager.Instance?.ScheduleReconnect(RECONNECT_DELAY);
        }

        internal void ExecuteReconnect()
        {
            if (_connectionState != ConnectionState.Reconnecting) return;
            
            var matchId = Deskillz.CurrentMatch?.MatchId;
            Connect(matchId);
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        public void Dispose()
        {
            Disconnect();
        }
    }

    // =============================================================================
    // WEBSOCKET CLIENT (Placeholder - would use native WebSocket or library)
    // =============================================================================

    /// <summary>
    /// WebSocket client wrapper. In production, this would use a proper WebSocket library.
    /// </summary>
    internal class WebSocketClient
    {
        public event Action OnOpen;
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action<int, string> OnClose;

        private bool _isConnected;
        private string _url;

        public void Connect(string url)
        {
            _url = url;
            
            // In a real implementation, this would use:
            // - UnityWebSocket package
            // - NativeWebSocket package  
            // - System.Net.WebSockets (Unity 2021+)
            
            // For now, simulate connection
            DeskillzLogger.Debug($"WebSocket connecting to: {url}");
            
            // Simulate async connection
            DeskillzManager.Instance?.StartCoroutine(SimulateConnect());
        }

        private IEnumerator SimulateConnect()
        {
            yield return new WaitForSeconds(0.1f);
            
            _isConnected = true;
            OnOpen?.Invoke();
        }

        public void Send(string message)
        {
            if (!_isConnected)
            {
                OnError?.Invoke("Not connected");
                return;
            }

            // In real implementation, send through WebSocket
            DeskillzLogger.Verbose($"WebSocket send: {message}");
        }

        public void Close()
        {
            if (_isConnected)
            {
                _isConnected = false;
                OnClose?.Invoke(1000, "Normal closure");
            }
        }

        // Method to simulate receiving a message (for testing)
        internal void SimulateMessage(string message)
        {
            if (_isConnected)
            {
                OnMessage?.Invoke(message);
            }
        }
    }
}

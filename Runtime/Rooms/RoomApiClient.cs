// =============================================================================
// Deskillz SDK for Unity - Room API Client
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Rooms
{
    /// <summary>
    /// Internal HTTP client for Private Room API calls.
    /// Uses DeskillzNetwork for authentication and base URL.
    /// </summary>
    internal static class RoomApiClient
    {
        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const string ROOMS_ENDPOINT = "/api/v1/private-rooms";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // ROOM CREATION
        // =====================================================================

        /// <summary>
        /// Create a new private room
        /// </summary>
        public static void CreateRoom(
            CreateRoomConfig config,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var gameId = DeskillzManager.Instance?.Config?.GameId;
            if (string.IsNullOrEmpty(gameId))
            {
                onError?.Invoke(new RoomError(RoomError.Codes.ServerError, "Game ID not configured"));
                return;
            }

            var request = config.ToRequest(gameId);
            var json = JsonUtility.ToJson(request);

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<PrivateRoom>(
                    ROOMS_ENDPOINT,
                    json,
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // ROOM DISCOVERY
        // =====================================================================

        /// <summary>
        /// Get public rooms for a specific game
        /// </summary>
        public static void GetPublicRooms(
            string gameId,
            Action<List<PrivateRoom>> onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}?gameId={gameId}";

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<RoomListResponse>(
                    endpoint,
                    response => onSuccess?.Invoke(response?.rooms ?? new List<PrivateRoom>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get rooms created by or joined by current user
        /// </summary>
        public static void GetMyRooms(
            Action<List<PrivateRoom>> onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/my-rooms";

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<RoomListResponse>(
                    endpoint,
                    response => onSuccess?.Invoke(response?.rooms ?? new List<PrivateRoom>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get room details by code
        /// </summary>
        public static void GetRoomByCode(
            string roomCode,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/code/{roomCode}";

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<PrivateRoom>(
                    endpoint,
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Get room details by ID
        /// </summary>
        public static void GetRoomById(
            string roomId,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}";

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<PrivateRoom>(
                    endpoint,
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // JOIN / LEAVE
        // =====================================================================

        /// <summary>
        /// Join a room by code
        /// </summary>
        public static void JoinRoom(
            string roomCode,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/join";
            var json = JsonUtility.ToJson(new JoinRoomRequest { roomCode = roomCode });

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<PrivateRoom>(
                    endpoint,
                    json,
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Leave a room
        /// </summary>
        public static void LeaveRoom(
            string roomId,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}/leave";

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<EmptyResponse>(
                    endpoint,
                    null,
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        // =====================================================================
        // READY STATUS
        // =====================================================================

        /// <summary>
        /// Set ready status
        /// </summary>
        public static void SetReady(
            string roomId,
            bool isReady,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}/ready";
            var json = JsonUtility.ToJson(new SetReadyRequest { isReady = isReady });

            DeskillzManager.Instance.StartCoroutine(
                PutRequest<PrivateRoom>(
                    endpoint,
                    json,
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // HOST ACTIONS
        // =====================================================================

        /// <summary>
        /// Start the match (host only)
        /// </summary>
        public static void StartMatch(
            string roomId,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}/start";

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<EmptyResponse>(
                    endpoint,
                    null,
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        /// <summary>
        /// Kick a player from the room (host only)
        /// </summary>
        public static void KickPlayer(
            string roomId,
            string playerId,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}/kick";
            var json = JsonUtility.ToJson(new KickPlayerRequest { playerId = playerId });

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<EmptyResponse>(
                    endpoint,
                    json,
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        /// <summary>
        /// Cancel the room (host only)
        /// </summary>
        public static void CancelRoom(
            string roomId,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}";

            DeskillzManager.Instance.StartCoroutine(
                DeleteRequest<EmptyResponse>(
                    endpoint,
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        // =====================================================================
        // INVITES
        // =====================================================================

        /// <summary>
        /// Send invite to a player
        /// </summary>
        public static void InvitePlayer(
            string roomId,
            string playerId,
            string message,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}/invite";
            var json = JsonUtility.ToJson(new InvitePlayerRequest 
            { 
                playerId = playerId,
                message = message 
            });

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<EmptyResponse>(
                    endpoint,
                    json,
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        /// <summary>
        /// Request to join a private room
        /// </summary>
        public static void RequestJoin(
            string roomId,
            string message,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var endpoint = $"{ROOMS_ENDPOINT}/{roomId}/request-join";
            var json = JsonUtility.ToJson(new JoinRequestRequest { message = message });

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<EmptyResponse>(
                    endpoint,
                    json,
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        // =====================================================================
        // INTERNAL HTTP METHODS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<RoomError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Get(url);
            
            SetHeaders(request);
            request.timeout = REQUEST_TIMEOUT;

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator PostRequest<T>(
            string endpoint,
            string json,
            Action<T> onSuccess,
            Action<RoomError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = new UnityWebRequest(url, "POST");
            
            if (!string.IsNullOrEmpty(json))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.timeout = REQUEST_TIMEOUT;

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator PutRequest<T>(
            string endpoint,
            string json,
            Action<T> onSuccess,
            Action<RoomError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = new UnityWebRequest(url, "PUT");
            
            if (!string.IsNullOrEmpty(json))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.timeout = REQUEST_TIMEOUT;

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator DeleteRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<RoomError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Delete(url);
            
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.timeout = REQUEST_TIMEOUT;

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static string GetFullUrl(string endpoint)
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? "https://api.deskillz.games";
            return baseUrl.TrimEnd('/') + endpoint;
        }

        private static void SetHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
            request.SetRequestHeader("X-SDK-Platform", Application.platform.ToString());

            var authToken = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;
            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            var apiKey = DeskillzManager.Instance?.Config?.ApiKey;
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("X-API-Key", apiKey);
            }
        }

        private static void HandleResponse<T>(
            UnityWebRequest request,
            Action<T> onSuccess,
            Action<RoomError> onError)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseText = request.downloadHandler.text;
                    
                    if (typeof(T) == typeof(EmptyResponse))
                    {
                        onSuccess?.Invoke(default);
                        return;
                    }

                    var response = JsonUtility.FromJson<T>(responseText);
                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Error($"[RoomApiClient] JSON parse error: {ex.Message}");
                    onError?.Invoke(new RoomError(RoomError.Codes.ServerError, "Invalid response format"));
                }
            }
            else
            {
                var error = ParseError(request);
                DeskillzLogger.Error($"[RoomApiClient] Request failed: {error.Code} - {error.Message}");
                onError?.Invoke(error);
            }
        }

        private static RoomError ParseError(UnityWebRequest request)
        {
            try
            {
                var responseText = request.downloadHandler?.text;
                if (!string.IsNullOrEmpty(responseText))
                {
                    var errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);
                    if (!string.IsNullOrEmpty(errorResponse?.message))
                    {
                        return new RoomError(
                            errorResponse.code ?? RoomError.Codes.ServerError,
                            errorResponse.message
                        );
                    }
                }
            }
            catch
            {
                // Ignore parse errors
            }

            // Default error based on HTTP status
            var statusCode = request.responseCode;
            return statusCode switch
            {
                401 => new RoomError(RoomError.Codes.NotAuthenticated, "Authentication required"),
                403 => new RoomError(RoomError.Codes.NotHost, "Permission denied"),
                404 => new RoomError(RoomError.Codes.RoomNotFound, "Room not found"),
                409 => new RoomError(RoomError.Codes.AlreadyInRoom, "Already in room"),
                422 => new RoomError(RoomError.Codes.InvalidCode, "Invalid request"),
                _ => new RoomError(RoomError.Codes.NetworkError, request.error ?? "Network error")
            };
        }

        // =====================================================================
        // INTERNAL REQUEST MODELS
        // =====================================================================

        [Serializable]
        private class JoinRoomRequest
        {
            public string roomCode;
        }

        [Serializable]
        private class SetReadyRequest
        {
            public bool isReady;
        }

        [Serializable]
        private class KickPlayerRequest
        {
            public string playerId;
        }

        [Serializable]
        private class InvitePlayerRequest
        {
            public string playerId;
            public string message;
        }

        [Serializable]
        private class JoinRequestRequest
        {
            public string message;
        }

        [Serializable]
        private class RoomListResponse
        {
            public List<PrivateRoom> rooms;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string code;
            public string message;
        }

        [Serializable]
        private class EmptyResponse { }
    }
}
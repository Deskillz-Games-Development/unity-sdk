// =============================================================================
// Deskillz SDK for Unity - Host API Client
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
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
    /// Internal HTTP client for Host API calls.
    /// Uses DeskillzNetwork for authentication and base URL.
    /// </summary>
    internal static class HostApiClient
    {
        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const string HOST_ENDPOINT = "/api/v1/host";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // PROFILE ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get current user's host profile
        /// </summary>
        public static void GetProfile(
            Action<HostProfile> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostProfile>(
                    $"{HOST_ENDPOINT}/profile",
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Create host profile for current user
        /// </summary>
        public static void CreateProfile(
            Action<HostProfile> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<HostProfile>(
                    $"{HOST_ENDPOINT}/profile",
                    "{}",
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Verify age (18+ confirmation)
        /// </summary>
        public static void VerifyAge(
            bool confirmed,
            Action<HostProfile> onSuccess,
            Action<HostError> onError)
        {
            var json = JsonUtility.ToJson(new AgeVerificationRequest { confirmed = confirmed });

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<HostProfile>(
                    $"{HOST_ENDPOINT}/verify-age",
                    json,
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // EARNINGS ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get host earnings summary
        /// </summary>
        public static void GetEarnings(
            Action<HostEarnings> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostEarnings>(
                    $"{HOST_ENDPOINT}/earnings",
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Get earnings history with pagination
        /// </summary>
        public static void GetEarningsHistory(
            int page,
            int limit,
            Action<List<HostTransaction>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TransactionListResponse>(
                    $"{HOST_ENDPOINT}/earnings/history?page={page}&limit={limit}",
                    response => onSuccess?.Invoke(response?.transactions ?? new List<HostTransaction>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Request withdrawal of available balance
        /// </summary>
        public static void RequestWithdrawal(
            WithdrawRequest request,
            Action<WithdrawalResponse> onSuccess,
            Action<HostError> onError)
        {
            var json = JsonUtility.ToJson(request);

            DeskillzManager.Instance.StartCoroutine(
                PostRequest<WithdrawalResponse>(
                    $"{HOST_ENDPOINT}/withdraw",
                    json,
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // BADGE ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get all badges for current host
        /// </summary>
        public static void GetBadges(
            Action<List<HostBadge>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<BadgeListResponse>(
                    $"{HOST_ENDPOINT}/badges",
                    response => onSuccess?.Invoke(response?.badges ?? new List<HostBadge>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get available badges (not yet earned)
        /// </summary>
        public static void GetAvailableBadges(
            Action<List<HostBadge>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<BadgeListResponse>(
                    $"{HOST_ENDPOINT}/badges/available",
                    response => onSuccess?.Invoke(response?.badges ?? new List<HostBadge>()),
                    onError
                )
            );
        }

        // =====================================================================
        // TIER ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get tier configuration and thresholds
        /// </summary>
        public static void GetTierConfig(
            RoomRevenueType revenueType,
            Action<List<HostTierConfig>> onSuccess,
            Action<HostError> onError)
        {
            var typeParam = revenueType.ToString().ToLower();

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TierConfigResponse>(
                    $"{HOST_ENDPOINT}/tiers?type={typeParam}",
                    response => onSuccess?.Invoke(response?.tiers ?? new List<HostTierConfig>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get tier history
        /// </summary>
        public static void GetTierHistory(
            int limit,
            Action<List<TierHistoryEntry>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TierHistoryResponse>(
                    $"{HOST_ENDPOINT}/tiers/history?limit={limit}",
                    response => onSuccess?.Invoke(response?.history ?? new List<TierHistoryEntry>()),
                    onError
                )
            );
        }

        // =====================================================================
        // ACTIVE ROOMS ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get active rooms hosted by current user
        /// </summary>
        public static void GetActiveRooms(
            Action<List<ActiveRoomSummary>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<ActiveRoomsResponse>(
                    $"{HOST_ENDPOINT}/rooms/active",
                    response => onSuccess?.Invoke(response?.rooms ?? new List<ActiveRoomSummary>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Get room history with pagination
        /// </summary>
        public static void GetRoomHistory(
            int page,
            int limit,
            Action<List<RoomHistoryEntry>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<RoomHistoryResponse>(
                    $"{HOST_ENDPOINT}/rooms/history?page={page}&limit={limit}",
                    response => onSuccess?.Invoke(response?.rooms ?? new List<RoomHistoryEntry>()),
                    onError
                )
            );
        }

        // =====================================================================
        // STATS ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get host statistics
        /// </summary>
        public static void GetStats(
            Action<HostStats> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostStats>(
                    $"{HOST_ENDPOINT}/stats",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // NOTIFICATION ENDPOINTS
        // =====================================================================

        /// <summary>
        /// Get notification settings
        /// </summary>
        public static void GetNotificationSettings(
            Action<HostNotificationSettings> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostNotificationSettings>(
                    $"{HOST_ENDPOINT}/notifications/settings",
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        public static void UpdateNotificationSettings(
            UpdateNotificationSettingsRequest request,
            Action<HostNotificationSettings> onSuccess,
            Action<HostError> onError)
        {
            var json = JsonUtility.ToJson(request);

            DeskillzManager.Instance.StartCoroutine(
                PutRequest<HostNotificationSettings>(
                    $"{HOST_ENDPOINT}/notifications/settings",
                    json,
                    onSuccess,
                    onError
                )
            );
        }

        /// <summary>
        /// Get recent notifications
        /// </summary>
        public static void GetNotifications(
            int limit,
            Action<List<HostNotification>> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<NotificationListResponse>(
                    $"{HOST_ENDPOINT}/notifications?limit={limit}",
                    response => onSuccess?.Invoke(response?.notifications ?? new List<HostNotification>()),
                    onError
                )
            );
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public static void MarkNotificationRead(
            string notificationId,
            Action onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PutRequest<EmptyResponse>(
                    $"{HOST_ENDPOINT}/notifications/{notificationId}/read",
                    "{}",
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        public static void MarkAllNotificationsRead(
            Action onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PutRequest<EmptyResponse>(
                    $"{HOST_ENDPOINT}/notifications/read-all",
                    "{}",
                    _ => onSuccess?.Invoke(),
                    onError
                )
            );
        }

        // =====================================================================
        // HTTP REQUEST HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<HostError> onError)
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
            Action<HostError> onError)
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

        private static IEnumerator PutRequest<T>(
            string endpoint,
            string json,
            Action<T> onSuccess,
            Action<HostError> onError)
        {
            var url = GetFullUrl(endpoint);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            SetupRequest(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator DeleteRequest(
            string endpoint,
            Action onSuccess,
            Action<HostError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Delete(url);

            SetupRequest(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                var error = ParseError(request);
                onError?.Invoke(error);
            }
        }

        private static string GetFullUrl(string endpoint)
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? "https://api.deskillz.games";
            return $"{baseUrl}{endpoint}";
        }

        private static void SetupRequest(UnityWebRequest request)
        {
            request.timeout = REQUEST_TIMEOUT;

            // Add auth token if available
            var token = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            // Add game ID header
            var gameId = DeskillzManager.Instance?.Config?.GameId;
            if (!string.IsNullOrEmpty(gameId))
            {
                request.SetRequestHeader("X-Game-Id", gameId);
            }

            // Add SDK version header
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request,
            Action<T> onSuccess,
            Action<HostError> onError)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseText = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<HostApiResponse<T>>(responseText);

                    if (response.success)
                    {
                        onSuccess?.Invoke(response.data);
                    }
                    else
                    {
                        onError?.Invoke(new HostError(HostError.Codes.ServerError, response.error ?? "Unknown error"));
                    }
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Error($"[HostApiClient] Failed to parse response: {ex.Message}");
                    onError?.Invoke(new HostError(HostError.Codes.ServerError, "Failed to parse response"));
                }
            }
            else
            {
                var error = ParseError(request);
                onError?.Invoke(error);
            }
        }

        private static HostError ParseError(UnityWebRequest request)
        {
            // Try to parse error from response body
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                try
                {
                    var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(errorResponse?.error))
                    {
                        return new HostError(
                            errorResponse.code ?? HostError.Codes.ServerError,
                            errorResponse.error
                        );
                    }
                }
                catch
                {
                    // Ignore parse errors
                }
            }

            // Default error based on HTTP status or network error
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return new HostError(HostError.Codes.NetworkError, "Network connection failed");
            }

            var statusCode = request.responseCode;
            return statusCode switch
            {
                401 => new HostError(HostError.Codes.NotAuthenticated, "Not authenticated"),
                403 => new HostError(HostError.Codes.NotVerified, "Access denied"),
                404 => new HostError(HostError.Codes.ProfileNotFound, "Resource not found"),
                429 => new HostError(HostError.Codes.RateLimited, "Too many requests"),
                _ => new HostError(HostError.Codes.ServerError, $"Server error: {statusCode}")
            };
        }

        // =====================================================================
        // INTERNAL RESPONSE MODELS
        // =====================================================================

        [Serializable]
        private class ErrorResponse
        {
            public string error;
            public string code;
        }

        [Serializable]
        private class EmptyResponse { }

        [Serializable]
        private class TransactionListResponse
        {
            public List<HostTransaction> transactions;
            public int total;
            public int page;
        }

        [Serializable]
        private class BadgeListResponse
        {
            public List<HostBadge> badges;
        }

        [Serializable]
        private class TierConfigResponse
        {
            public List<HostTierConfig> tiers;
        }

        [Serializable]
        private class TierHistoryResponse
        {
            public List<TierHistoryEntry> history;
        }

        [Serializable]
        private class ActiveRoomsResponse
        {
            public List<ActiveRoomSummary> rooms;
        }

        [Serializable]
        private class RoomHistoryResponse
        {
            public List<RoomHistoryEntry> rooms;
            public int total;
            public int page;
        }

        [Serializable]
        private class NotificationListResponse
        {
            public List<HostNotification> notifications;
        }

        [Serializable]
        private class AgeVerificationRequest
        {
            public bool confirmed;
        }
    }

    // =========================================================================
    // ADDITIONAL RESPONSE MODELS
    // =========================================================================

    /// <summary>
    /// Withdrawal response
    /// </summary>
    [Serializable]
    public class WithdrawalResponse
    {
        public string Id;
        public decimal Amount;
        public string Currency;
        public string Status;
        public string TxHash;
        public DateTime CreatedAt;
        public DateTime? CompletedAt;
    }

    /// <summary>
    /// Tier history entry
    /// </summary>
    [Serializable]
    public class TierHistoryEntry
    {
        public HostTier OldTier;
        public HostTier NewTier;
        public RoomRevenueType RevenueType;
        public string Reason;
        public DateTime ChangedAt;
    }

    /// <summary>
    /// Room history entry
    /// </summary>
    [Serializable]
    public class RoomHistoryEntry
    {
        public string Id;
        public string RoomCode;
        public string Name;
        public string GameName;
        public RoomRevenueType RevenueType;
        public int PlayerCount;
        public decimal TotalPot;
        public decimal HostEarnings;
        public int RoundCount;
        public float DurationMinutes;
        public DateTime StartedAt;
        public DateTime EndedAt;
    }
}
// =============================================================================
// Deskillz SDK for Unity - Host Dashboard Extensions
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 8)
// =============================================================================
//
// Extends existing HostManager with v3.5.2 methods:
//   - GetDashboard() composite endpoint (8.1)
//   - (8.2 / 8.3 host withdraw removed in 3.7.7: earnings are withdrawn on deskillz.games)
//   - CheckAgeVerified() status check (8.4)
//
// Path: Runtime/Host/HostDashboardExtensions.cs
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Host
{
    // =========================================================================
    // DASHBOARD MODEL
    // =========================================================================

    /// <summary>
    /// Composite host dashboard response (v3.5.2)
    /// Single API call returns profile + tiers + earnings + badges + rooms
    /// </summary>
    [Serializable]
    public class HostDashboard
    {
        public HostProfile Profile;
        public HostTierInfo EsportsTierInfo;
        public HostTierInfo SocialTierInfo;
        public HostLevelInfo LevelInfo;
        public HostEarnings Earnings;
        public List<HostBadge> Badges;
        public List<ActiveRoomSummary> ActiveRooms;
        public List<SettlementRecord> RecentSettlements;

        public HostDashboard()
        {
            Badges = new List<HostBadge>();
            ActiveRooms = new List<ActiveRoomSummary>();
            RecentSettlements = new List<SettlementRecord>();
        }
    }

    /// <summary>
    /// Host tier detailed info
    /// </summary>
    [Serializable]
    public class HostTierInfo
    {
        public string Tier;
        public float MinThreshold;
        public float MaxThreshold;
        public float HostShare;
        public float PlatformShare;
        public float DeveloperShare;
        public float CurrentValue;
        public float ProgressPercent;
        public string NextTier;
        public float ValueToNextTier;
    }

    /// <summary>
    /// Host level progress info
    /// </summary>
    [Serializable]
    public class HostLevelInfo
    {
        public int Level;
        public string Title;
        public int PlayersHosted;
        public float TotalEarnings;
        public int CurrentPlayers;
        public float CurrentEarnings;
        public float ProgressPercent;
        public int NextLevel;
        public List<string> Benefits;

        public HostLevelInfo()
        {
            Benefits = new List<string>();
        }
    }

    /// <summary>
    /// Settlement record for host earnings
    /// </summary>
    [Serializable]
    public class SettlementRecord
    {
        public string Id;
        public string RoomId;
        public string RoomName;
        public int RoundsSettled;
        public decimal HostShare;
        public string Trigger;
        public DateTime SettledAt;
    }

    /// <summary>
    /// Age verification status
    /// </summary>
    [Serializable]
    public class AgeVerificationStatus
    {
        public bool IsVerified;
        public DateTime? VerifiedAt;
    }

    // =========================================================================
    // EXTENSIONS
    // =========================================================================

    /// <summary>
    /// Extended host operations added in v3.5.2.
    /// Supplements the existing HostManager static class.
    /// </summary>
    public static class HostDashboardExtensions
    {
        private const string HOST_ENDPOINT = "/api/v1/host";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // GET DASHBOARD (8.1)
        // =====================================================================

        /// <summary>
        /// Get full host dashboard in a single API call.
        /// GET /api/v1/host/dashboard
        /// </summary>
        public static void GetDashboard(
            Action<HostDashboard> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<HostDashboard>($"{HOST_ENDPOINT}/dashboard", onSuccess, onError)
            );
        }

        // [N790-3.7.7] WithdrawAllEarnings and RequestWithdrawal removed:
        // POST /host/withdraw no longer exists; earnings are part of the
        // Deskillz balance and are withdrawn on deskillz.games.

        // =====================================================================
        // CHECK AGE VERIFIED (8.4)
        // =====================================================================

        /// <summary>
        /// Check if the current user has completed age verification.
        /// GET /api/v1/host/verify-age/status
        /// </summary>
        public static void CheckAgeVerified(
            Action<AgeVerificationStatus> onSuccess,
            Action<HostError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<AgeVerificationStatus>(
                    $"{HOST_ENDPOINT}/verify-age/status",
                    onSuccess, onError
                )
            );
        }

        // =====================================================================
        // HTTP HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint, Action<T> onSuccess, Action<HostError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Get(url);
            SetupRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator PostRequest<T>(
            string endpoint, string json, Action<T> onSuccess, Action<HostError> onError)
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
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? DeskillzConfig.PLATFORM_API_HOST;
            return $"{baseUrl}{endpoint}";
        }

        private static void SetupRequest(UnityWebRequest request)
        {
            request.timeout = REQUEST_TIMEOUT;
            var token = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request, Action<T> onSuccess, Action<HostError> onError)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var text = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(text)) { onSuccess?.Invoke(default); return; }
                    onSuccess?.Invoke(DeskillzJson.Deserialize<T>(text));
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Error($"[HostDashboard] Parse error: {ex.Message}");
                    onError?.Invoke(new HostError(HostError.Codes.ServerError, "Failed to parse response"));
                }
            }
            else
            {
                onError?.Invoke(new HostError(request.responseCode.ToString(), request.error ?? "Unknown error"));
            }
        }
    }
}
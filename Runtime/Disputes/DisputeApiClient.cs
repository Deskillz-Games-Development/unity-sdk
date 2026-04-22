// =============================================================================
// Deskillz SDK for Unity - Dispute API Client
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 4)
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Disputes
{
    /// <summary>
    /// Internal HTTP client for dispute API endpoints.
    /// </summary>
    internal static class DisputeApiClient
    {
        private const string DISPUTES_ENDPOINT = "/api/v1/disputes";
        private const string MATCHES_ENDPOINT = "/api/v1/matches/history/me";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // FILE DISPUTE
        // =====================================================================

        public static void FileDispute(
            FileDisputeRequest request,
            Action<DisputeRecord> onSuccess,
            Action<DisputeError> onError)
        {
            var json = JsonUtility.ToJson(request);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<DisputeRecord>(DISPUTES_ENDPOINT, json, onSuccess, onError)
            );
        }

        // =====================================================================
        // GET MY DISPUTES
        // =====================================================================

        public static void GetMyDisputes(
            string status,
            Action<List<DisputeRecord>> onSuccess,
            Action<DisputeError> onError)
        {
            var query = !string.IsNullOrEmpty(status) ? $"?status={status}" : "";
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<DisputeListWrapper>(
                    $"{DISPUTES_ENDPOINT}/me{query}",
                    wrapper => onSuccess?.Invoke(wrapper?.disputes ?? new List<DisputeRecord>()),
                    onError
                )
            );
        }

        // =====================================================================
        // GET DISPUTE DETAILS
        // =====================================================================

        public static void GetDisputeDetails(
            string disputeId,
            Action<DisputeRecord> onSuccess,
            Action<DisputeError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<DisputeRecord>(
                    $"{DISPUTES_ENDPOINT}/{disputeId}",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // ADD EVIDENCE
        // =====================================================================

        public static void AddEvidence(
            string disputeId,
            string[] evidence,
            Action<int> onSuccess,
            Action<DisputeError> onError)
        {
            var body = new EvidenceRequest { evidence = evidence };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<EvidenceResponse>(
                    $"{DISPUTES_ENDPOINT}/{disputeId}/evidence",
                    json,
                    response => onSuccess?.Invoke(response?.evidenceCount ?? 0),
                    onError
                )
            );
        }

        // =====================================================================
        // RECENT MATCHES
        // =====================================================================

        public static void GetRecentMatches(
            Action<List<RecentMatchForDispute>> onSuccess,
            Action<DisputeError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<RecentMatchesWrapper>(
                    $"{MATCHES_ENDPOINT}?limit=10",
                    wrapper => onSuccess?.Invoke(wrapper?.matches ?? new List<RecentMatchForDispute>()),
                    onError
                )
            );
        }

        // =====================================================================
        // INTERNAL MODELS
        // =====================================================================

        [Serializable]
        private class DisputeListWrapper
        {
            public List<DisputeRecord> disputes;
        }

        [Serializable]
        private class EvidenceRequest
        {
            public string[] evidence;
        }

        [Serializable]
        private class EvidenceResponse
        {
            public bool success;
            public int evidenceCount;
        }

        [Serializable]
        private class RecentMatchesWrapper
        {
            public List<RecentMatchForDispute> matches;
        }

        // =====================================================================
        // HTTP METHODS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<DisputeError> onError)
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
            Action<DisputeError> onError)
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
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            var gameId = DeskillzManager.Instance?.Config?.GameId;
            if (!string.IsNullOrEmpty(gameId))
                request.SetRequestHeader("X-Game-Id", gameId);
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request,
            Action<T> onSuccess,
            Action<DisputeError> onError)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseText = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(responseText))
                    {
                        onSuccess?.Invoke(default);
                        return;
                    }
                    var data = JsonUtility.FromJson<T>(responseText);
                    onSuccess?.Invoke(data);
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Error($"[DisputeApiClient] Parse error: {ex.Message}");
                    onError?.Invoke(new DisputeError("PARSE_ERROR", "Failed to parse server response"));
                }
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        private static DisputeError ParseError(UnityWebRequest request)
        {
            try
            {
                var text = request.downloadHandler?.text;
                if (!string.IsNullOrEmpty(text))
                {
                    var err = JsonUtility.FromJson<ApiErrorResponse>(text);
                    if (!string.IsNullOrEmpty(err?.message))
                        return new DisputeError(err.statusCode.ToString(), err.message);
                }
            }
            catch { }

            return new DisputeError(
                request.responseCode.ToString(),
                request.error ?? "Unknown error"
            );
        }

        [Serializable]
        private class ApiErrorResponse
        {
            public int statusCode;
            public string message;
        }
    }
}
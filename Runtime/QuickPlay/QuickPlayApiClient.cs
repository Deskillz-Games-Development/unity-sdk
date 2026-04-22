// =============================================================================
// Deskillz SDK for Unity - Quick Play API Client
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 6)
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.QuickPlay
{
    /// <summary>
    /// Internal HTTP client for Quick Play API endpoints.
    /// </summary>
    internal static class QuickPlayApiClient
    {
        private const string QP_ENDPOINT = "/api/v1/lobby/quick-play";
        private const string QP_CONFIG_ENDPOINT = "/api/v1/quick-play/games";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // ESPORT QUICK PLAY
        // =====================================================================

        public static void JoinQueue(
            QuickPlayJoinParams parameters,
            Action<QuickPlayJoinResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            var body = new JoinQueueRequest
            {
                gameId = parameters.GameId,
                entryFee = parameters.EntryFee,
                currency = parameters.Currency,
                matchMode = parameters.MatchMode,
                selectedTarget = parameters.SelectedTarget,
            };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<QuickPlayJoinResult>($"{QP_ENDPOINT}/join", json, onSuccess, onError)
            );
        }

        public static void LeaveQueue(
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid($"{QP_ENDPOINT}/leave", "{}", onSuccess, onError)
            );
        }

        public static void GetConfig(
            string gameId,
            Action<QuickPlayConfig> onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<QuickPlayConfig>($"{QP_CONFIG_ENDPOINT}/{gameId}", onSuccess, onError)
            );
        }

        public static void GetStatus(
            Action<QuickPlayStatus> onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<QuickPlayStatus>($"{QP_ENDPOINT}/status", onSuccess, onError)
            );
        }

        public static void LaunchMatch(
            string matchSessionId,
            Action<QuickPlayLaunchData> onSuccess,
            Action<QuickPlayError> onError)
        {
            var body = new LaunchMatchRequest { matchSessionId = matchSessionId };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<QuickPlayLaunchData>($"{QP_ENDPOINT}/match/launch", json, onSuccess, onError)
            );
        }

        public static void SubmitScore(
            string matchId,
            int score,
            Action<QuickPlayScoreResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            var body = new SubmitScoreRequest { score = score };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<QuickPlayScoreResult>(
                    $"{QP_ENDPOINT}/match/{matchId}/score", json, onSuccess, onError)
            );
        }

        public static void GetMatchResults(
            string matchId,
            Action<QuickPlayMatchResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<QuickPlayMatchResult>(
                    $"{QP_ENDPOINT}/match/{matchId}/results", onSuccess, onError)
            );
        }

        public static void ForceComplete(
            string matchId,
            Action<QuickPlayMatchResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<QuickPlayMatchResult>(
                    $"{QP_ENDPOINT}/match/{matchId}/complete", "{}", onSuccess, onError)
            );
        }

        // =====================================================================
        // SOCIAL QUICK PLAY
        // =====================================================================

        public static void CreateSocialRoom(
            CreateSocialQuickPlayParams parameters,
            Action<CreateSocialQuickPlayResult> onSuccess,
            Action<QuickPlayError> onError)
        {
            var body = new CreateSocialRoomRequest
            {
                gameId = parameters.GameId,
                pointValueUsd = parameters.PointValueUsd,
                currency = parameters.Currency,
                seatsPerTable = parameters.SeatsPerTable,
            };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<CreateSocialQuickPlayResult>(
                    $"{QP_ENDPOINT}/social/create", json, onSuccess, onError)
            );
        }

        public static void SubmitSocialRound(
            string roomId,
            SocialRoundPayload payload,
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            var json = JsonUtility.ToJson(payload);
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid($"{QP_ENDPOINT}/social/{roomId}/round", json, onSuccess, onError)
            );
        }

        public static void SocialRebuy(
            string roomId,
            decimal amount,
            Action<decimal> onSuccess,
            Action<QuickPlayError> onError)
        {
            var body = new RebuyRequest { amount = amount };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<RebuyResponse>(
                    $"{QP_ENDPOINT}/social/{roomId}/rebuy",
                    json,
                    response => onSuccess?.Invoke(response?.pointBalance ?? 0),
                    onError
                )
            );
        }

        public static void SocialCashOut(
            string roomId,
            Action<decimal> onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<CashOutResponse>(
                    $"{QP_ENDPOINT}/social/{roomId}/cashout",
                    "{}",
                    response => onSuccess?.Invoke(response?.amount ?? 0),
                    onError
                )
            );
        }

        public static void EndSocialGame(
            string roomId,
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid($"{QP_ENDPOINT}/social/{roomId}/end", "{}", onSuccess, onError)
            );
        }

        // =====================================================================
        // INTERNAL REQUEST MODELS
        // =====================================================================

        [Serializable]
        private class JoinQueueRequest
        {
            public string gameId;
            public decimal entryFee;
            public string currency;
            public string matchMode;
            public int? selectedTarget;
        }

        [Serializable]
        private class LaunchMatchRequest
        {
            public string matchSessionId;
        }

        [Serializable]
        private class SubmitScoreRequest
        {
            public int score;
        }

        [Serializable]
        private class CreateSocialRoomRequest
        {
            public string gameId;
            public decimal pointValueUsd;
            public string currency;
            public int seatsPerTable;
        }

        [Serializable]
        private class RebuyRequest
        {
            public decimal amount;
        }

        [Serializable]
        private class RebuyResponse
        {
            public bool success;
            public decimal pointBalance;
        }

        [Serializable]
        private class CashOutResponse
        {
            public bool success;
            public decimal amount;
        }

        // =====================================================================
        // HTTP METHODS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<QuickPlayError> onError)
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
            Action<QuickPlayError> onError)
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

        private static IEnumerator PostRequestVoid(
            string endpoint,
            string json,
            Action onSuccess,
            Action<QuickPlayError> onError)
        {
            var url = GetFullUrl(endpoint);
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(request);
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke();
            else
                onError?.Invoke(ParseError(request));
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
            Action<QuickPlayError> onError)
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
                    DeskillzLogger.Error($"[QuickPlayApiClient] Parse error: {ex.Message}");
                    onError?.Invoke(new QuickPlayError(QuickPlayError.Codes.ServerError, "Failed to parse server response"));
                }
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        private static QuickPlayError ParseError(UnityWebRequest request)
        {
            try
            {
                var text = request.downloadHandler?.text;
                if (!string.IsNullOrEmpty(text))
                {
                    var err = JsonUtility.FromJson<ApiErrorResponse>(text);
                    if (!string.IsNullOrEmpty(err?.message))
                        return new QuickPlayError(err.statusCode.ToString(), err.message);
                }
            }
            catch { }

            return new QuickPlayError(
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
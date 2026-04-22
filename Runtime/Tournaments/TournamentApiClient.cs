// =============================================================================
// Deskillz SDK for Unity - Tournament API Client
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 3)
// =============================================================================
//
// Internal HTTP client for tournament API calls.
// Uses DeskillzManager for auth token, base URL, and coroutine runner.
// Follows the same pattern as HostApiClient.
//
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Tournaments
{
    /// <summary>
    /// Internal HTTP client for tournament API endpoints.
    /// </summary>
    internal static class TournamentApiClient
    {
        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const string TOURNAMENTS_ENDPOINT = "/api/v1/tournaments";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // TOURNAMENT LISTING
        // =====================================================================

        /// <summary>
        /// GET /api/v1/tournaments?filters
        /// </summary>
        public static void GetTournaments(
            TournamentFilters filters,
            Action<List<TournamentListing>> onSuccess,
            Action<TournamentError> onError)
        {
            var queryString = filters?.ToQueryString() ?? "";
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TournamentsListApiResponse>(
                    $"{TOURNAMENTS_ENDPOINT}{queryString}",
                    response => onSuccess?.Invoke(response?.tournaments ?? new List<TournamentListing>()),
                    onError
                )
            );
        }

        /// <summary>
        /// GET /api/v1/tournaments/game/:gameId/active
        /// </summary>
        public static void GetActiveTournaments(
            string gameId,
            Action<List<TournamentListing>> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TournamentsListApiResponse>(
                    $"{TOURNAMENTS_ENDPOINT}/game/{gameId}/active",
                    response => onSuccess?.Invoke(response?.tournaments ?? new List<TournamentListing>()),
                    onError
                )
            );
        }

        // =====================================================================
        // JOIN
        // =====================================================================

        /// <summary>
        /// POST /api/v1/tournaments/:id/join
        /// </summary>
        public static void JoinTournament(
            string tournamentId,
            Action onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/join",
                    "{}",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // REGISTER
        // =====================================================================

        /// <summary>
        /// POST /api/v1/tournaments/:id/register
        /// </summary>
        public static void Register(
            string tournamentId,
            Action<TournamentRegistration> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<TournamentRegistration>(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/register",
                    "{}",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // CHECK IN
        // =====================================================================

        /// <summary>
        /// POST /api/v1/tournaments/:id/checkin
        /// </summary>
        public static void CheckIn(
            string tournamentId,
            Action<TournamentRegistration> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<TournamentRegistration>(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/checkin",
                    "{}",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // LEAVE
        // =====================================================================

        /// <summary>
        /// DELETE /api/v1/tournaments/:id/leave
        /// </summary>
        public static void Leave(
            string tournamentId,
            Action onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                DeleteRequest(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/leave",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // ENROLLMENT STATUS
        // =====================================================================

        /// <summary>
        /// GET /api/v1/tournaments/:id/my-status
        /// </summary>
        public static void GetEnrollmentStatus(
            string tournamentId,
            Action<TournamentEnrollmentState> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TournamentEnrollmentState>(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/my-status",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // MY REGISTRATIONS
        // =====================================================================

        /// <summary>
        /// GET /api/v1/tournaments/my-registrations
        /// </summary>
        public static void GetMyRegistrations(
            Action<List<TournamentRegistration>> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<List<TournamentRegistration>>(
                    $"{TOURNAMENTS_ENDPOINT}/my-registrations",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // SCHEDULE
        // =====================================================================

        /// <summary>
        /// GET /api/v1/tournaments/:id/schedule
        /// </summary>
        public static void GetSchedule(
            string tournamentId,
            Action<TournamentSchedule> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TournamentSchedule>(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/schedule",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // TABLE ASSIGNMENT
        // =====================================================================

        /// <summary>
        /// GET /api/v1/tournaments/:id/my-seat
        /// </summary>
        public static void GetMyTableAssignment(
            string tournamentId,
            Action<TableAssignment> onSuccess,
            Action<TournamentError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TableAssignment>(
                    $"{TOURNAMENTS_ENDPOINT}/{tournamentId}/my-seat",
                    onSuccess,
                    onError
                )
            );
        }

        // =====================================================================
        // INTERNAL API RESPONSE WRAPPERS
        // =====================================================================

        [Serializable]
        private class TournamentsListApiResponse
        {
            public List<TournamentListing> tournaments;
        }

        // =====================================================================
        // HTTP METHODS (mirrors HostApiClient pattern)
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint,
            Action<T> onSuccess,
            Action<TournamentError> onError)
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
            Action<TournamentError> onError)
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
            Action<TournamentError> onError)
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
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        private static IEnumerator DeleteRequest(
            string endpoint,
            Action onSuccess,
            Action<TournamentError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Delete(url);
            request.downloadHandler = new DownloadHandlerBuffer();

            SetupRequest(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

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
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            var gameId = DeskillzManager.Instance?.Config?.GameId;
            if (!string.IsNullOrEmpty(gameId))
            {
                request.SetRequestHeader("X-Game-Id", gameId);
            }

            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request,
            Action<T> onSuccess,
            Action<TournamentError> onError)
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
                    DeskillzLogger.Error($"[TournamentApiClient] Failed to parse response: {ex.Message}");
                    onError?.Invoke(new TournamentError(
                        TournamentError.Codes.ServerError,
                        "Failed to parse server response"
                    ));
                }
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        private static TournamentError ParseError(UnityWebRequest request)
        {
            try
            {
                var responseText = request.downloadHandler?.text;
                if (!string.IsNullOrEmpty(responseText))
                {
                    var errorResponse = JsonUtility.FromJson<ErrorApiResponse>(responseText);
                    if (!string.IsNullOrEmpty(errorResponse?.message))
                    {
                        return new TournamentError(
                            errorResponse.statusCode.ToString(),
                            errorResponse.message
                        );
                    }
                }
            }
            catch
            {
                // Fall through to generic error
            }

            // Map HTTP status codes
            var code = request.responseCode;
            var message = code switch
            {
                401 => "Authentication required",
                403 => "Access denied",
                404 => "Tournament not found",
                409 => "Already registered or conflict",
                422 => "Invalid request data",
                429 => "Too many requests, please try again later",
                _ => request.error ?? "Unknown error"
            };

            return new TournamentError(code.ToString(), message);
        }

        [Serializable]
        private class ErrorApiResponse
        {
            public int statusCode;
            public string message;
            public string error;
        }
    }
}
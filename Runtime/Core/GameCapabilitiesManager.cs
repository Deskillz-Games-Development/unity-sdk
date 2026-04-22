// =============================================================================
// Deskillz SDK for Unity - Game Capabilities Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 9)
// =============================================================================
//
// Fetches and caches game capabilities from the API.
// Falls back to GameCapabilities.Default before API responds.
// Mirrors DeskillzBridge.ts line 3089 getGameCapabilities().
//
// Path: Runtime/Core/GameCapabilitiesManager.cs
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz
{
    /// <summary>
    /// Manages game capabilities configuration.
    /// </summary>
    public static class GameCapabilitiesManager
    {
        /// <summary>Current capabilities (starts with defaults, updated from API)</summary>
        public static GameCapabilities Current { get; private set; } = GameCapabilities.Default;

        /// <summary>Whether capabilities have been fetched from the API</summary>
        public static bool IsFetched { get; private set; }

        /// <summary>Fired when capabilities are updated from API</summary>
        public static event Action<GameCapabilities> OnCapabilitiesUpdated;

        // =====================================================================
        // GET CAPABILITIES (9.1)
        // =====================================================================

        /// <summary>
        /// Fetch game capabilities from the API.
        /// Falls back to GameCapabilities.Default on error.
        /// GET /api/v1/games/:gameId/capabilities
        /// </summary>
        public static void GetCapabilities(
            Action<GameCapabilities> onSuccess = null,
            Action<string> onError = null,
            string gameId = null)
        {
            gameId ??= DeskillzManager.Instance?.Config?.GameId;

            if (string.IsNullOrEmpty(gameId))
            {
                DeskillzLogger.Warning("[Capabilities] No game ID available");
                onSuccess?.Invoke(Current);
                return;
            }

            DeskillzManager.Instance.StartCoroutine(
                FetchCapabilities(gameId, onSuccess, onError)
            );
        }

        /// <summary>
        /// Refresh capabilities from server. Called during SDK initialization.
        /// </summary>
        internal static void Refresh()
        {
            GetCapabilities();
        }

        /// <summary>
        /// Reset to defaults on logout.
        /// </summary>
        internal static void Reset()
        {
            Current = GameCapabilities.Default;
            IsFetched = false;
        }

        // =====================================================================
        // INTERNAL
        // =====================================================================

        private static IEnumerator FetchCapabilities(
            string gameId,
            Action<GameCapabilities> onSuccess,
            Action<string> onError)
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? "https://api.deskillz.games";
            var url = $"{baseUrl}/api/v1/games/{gameId}/capabilities";

            using var request = UnityWebRequest.Get(url);
            request.timeout = 10;

            var token = DeskillzManager.Instance?.CurrentPlayer?.AuthToken;
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var text = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        var caps = JsonUtility.FromJson<GameCapabilities>(text);
                        Current = caps;
                        IsFetched = true;
                        DeskillzLogger.Info($"[Capabilities] Fetched for game {gameId}");
                        OnCapabilitiesUpdated?.Invoke(caps);
                        onSuccess?.Invoke(caps);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Warning($"[Capabilities] Parse error: {ex.Message}");
                }
            }
            else
            {
                DeskillzLogger.Warning($"[Capabilities] Fetch failed: {request.error}");
            }

            // Fallback to defaults
            onSuccess?.Invoke(Current);
            onError?.Invoke(request.error ?? "Failed to fetch capabilities");
        }
    }
}
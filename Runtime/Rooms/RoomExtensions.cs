// =============================================================================
// Deskillz SDK for Unity - Room Extensions
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 5)
// =============================================================================
//
// Extends DeskillzRooms with v3.5.2 room methods:
//   - CreateRoom with HostRole (5.1, 5.2)
//   - BuyIn / CashOut / Rebuy (5.3-5.5)
//   - SubmitRound (5.6)
//   - TriggerSettlement (5.7)
//   - InvitePlayer / GetMyInvites / RespondToInvite (5.8-5.10)
//
// Path: Runtime/Rooms/RoomExtensions.cs
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Rooms
{
    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>
    /// Room extension events for invites and financial actions.
    /// </summary>
    public static class RoomExtensionEvents
    {
        /// <summary>Fired when an invite is received</summary>
        public static event Action<RoomInvite> OnInviteReceived;

        /// <summary>Fired when buy-in completes</summary>
        public static event Action<decimal> OnBuyInComplete;

        /// <summary>Fired when cash-out completes</summary>
        public static event Action<decimal> OnCashOutComplete;

        /// <summary>Fired when settlement is triggered</summary>
        public static event Action<string> OnSettlementTriggered;

        internal static void EmitInviteReceived(RoomInvite invite) => OnInviteReceived?.Invoke(invite);
        internal static void EmitBuyInComplete(decimal amount) => OnBuyInComplete?.Invoke(amount);
        internal static void EmitCashOutComplete(decimal amount) => OnCashOutComplete?.Invoke(amount);
        internal static void EmitSettlementTriggered(string roomId) => OnSettlementTriggered?.Invoke(roomId);
    }

    // =========================================================================
    // REQUEST/RESPONSE MODELS
    // =========================================================================

    [Serializable]
    internal class BuyInRequest
    {
        public decimal amount;
        public string currency;
    }

    [Serializable]
    internal class BuyInResponse
    {
        public bool success;
        public decimal chipBalance;
    }

    [Serializable]
    internal class CashOutResponse
    {
        public bool success;
        public decimal amount;
        public string currency;
    }

    [Serializable]
    internal class RebuyRequest
    {
        public decimal amount;
        public string currency;
    }

    [Serializable]
    internal class RebuyResponse
    {
        public bool success;
        public decimal chipBalance;
    }

    [Serializable]
    internal class SubmitRoundRequest
    {
        public int roundNumber;
        public string resultsJson;
    }

    [Serializable]
    internal class InvitePlayerRequest
    {
        public string targetUsernameOrId;
        public string message;
    }

    [Serializable]
    internal class InviteRespondRequest
    {
        public bool accept;
    }

    [Serializable]
    internal class InviteListWrapper
    {
        public List<RoomInvite> invites;
    }

    [Serializable]
    internal class CreateEsportRoomRequest
    {
        public string name;
        public string gameId;
        public decimal entryFee;
        public string currency;
        public int minPlayers;
        public int maxPlayers;
        public string hostRole;
        public int matchDurationSeconds;
        public string matchMode;
        public string visibility;
    }

    [Serializable]
    internal class CreateSocialRoomRequest
    {
        public string name;
        public string gameId;
        public string socialGameType;
        public decimal tableStakes;
        public string currency;
        public int playersPerTable;
        public int maxTables;
        public string hostRole;
        public decimal rakePercent;
        public string visibility;
        public string winCondition;
        public int? winConditionTarget;
    }

    // =========================================================================
    // ROOM EXTENSIONS MANAGER
    // =========================================================================

    /// <summary>
    /// Extended room operations added in v3.5.2.
    /// Supplements the existing DeskillzRooms static class.
    /// </summary>
    public static class RoomExtensions
    {
        private const string ROOMS_ENDPOINT = "/api/v1/private-rooms";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // CREATE WITH HOST ROLE (5.1, 5.2)
        // =====================================================================

        /// <summary>
        /// Create an esport room with host role support.
        /// POST /api/v1/private-rooms/create
        /// </summary>
        public static void CreateEsportRoom(
            CreateEsportRoomOpts opts,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var gameId = DeskillzManager.Instance?.Config?.GameId;
            var request = new CreateEsportRoomRequest
            {
                name = opts.Name,
                gameId = gameId,
                entryFee = opts.EntryFee,
                currency = opts.Currency,
                minPlayers = opts.MinPlayers,
                maxPlayers = opts.MaxPlayers,
                hostRole = opts.HostRole.ToString(),
                matchDurationSeconds = opts.MatchDurationSeconds,
                matchMode = opts.MatchMode.ToString(),
                visibility = opts.Visibility,
            };
            var json = JsonUtility.ToJson(request);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<PrivateRoom>($"{ROOMS_ENDPOINT}/create", json, onSuccess, onError)
            );
        }

        /// <summary>
        /// Create a social game room with host role and win condition.
        /// POST /api/v1/private-rooms/create
        /// </summary>
        public static void CreateSocialRoom(
            CreateSocialRoomOpts opts,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError)
        {
            var gameId = DeskillzManager.Instance?.Config?.GameId;
            var request = new CreateSocialRoomRequest
            {
                name = opts.Name,
                gameId = gameId,
                socialGameType = opts.SocialGameType,
                tableStakes = opts.TableStakes,
                currency = opts.Currency,
                playersPerTable = opts.PlayersPerTable,
                maxTables = opts.MaxTables,
                hostRole = opts.HostRole.ToString(),
                rakePercent = opts.RakePercent,
                visibility = opts.Visibility,
                winCondition = opts.WinCondition.ToString(),
                winConditionTarget = opts.WinConditionTarget,
            };
            var json = JsonUtility.ToJson(request);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<PrivateRoom>($"{ROOMS_ENDPOINT}/create", json, onSuccess, onError)
            );
        }

        // =====================================================================
        // BUY IN (5.3)
        // =====================================================================

        /// <summary>
        /// Buy in to a social room (purchase chips).
        /// POST /api/v1/private-rooms/:roomId/buy-in
        /// </summary>
        public static void BuyIn(
            string roomId,
            decimal amount,
            string currency,
            Action<decimal> onSuccess,
            Action<RoomError> onError)
        {
            var body = new BuyInRequest { amount = amount, currency = currency };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<BuyInResponse>(
                    $"{ROOMS_ENDPOINT}/{roomId}/buy-in", json,
                    response =>
                    {
                        RoomExtensionEvents.EmitBuyInComplete(response.chipBalance);
                        onSuccess?.Invoke(response.chipBalance);
                    },
                    onError
                )
            );
        }

        // =====================================================================
        // CASH OUT (5.4)
        // =====================================================================

        /// <summary>
        /// Cash out of a social room (convert chips back to crypto).
        /// POST /api/v1/private-rooms/:roomId/cash-out
        /// </summary>
        public static void CashOut(
            string roomId,
            Action<decimal> onSuccess,
            Action<RoomError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<CashOutResponse>(
                    $"{ROOMS_ENDPOINT}/{roomId}/cash-out", "{}",
                    response =>
                    {
                        RoomExtensionEvents.EmitCashOutComplete(response.amount);
                        onSuccess?.Invoke(response.amount);
                    },
                    onError
                )
            );
        }

        // =====================================================================
        // REBUY (5.5)
        // =====================================================================

        /// <summary>
        /// Rebuy chips in a social room.
        /// POST /api/v1/private-rooms/:roomId/rebuy
        /// </summary>
        public static void Rebuy(
            string roomId,
            decimal amount,
            string currency,
            Action<decimal> onSuccess,
            Action<RoomError> onError)
        {
            var body = new RebuyRequest { amount = amount, currency = currency };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<RebuyResponse>(
                    $"{ROOMS_ENDPOINT}/{roomId}/rebuy", json,
                    response => onSuccess?.Invoke(response.chipBalance),
                    onError
                )
            );
        }

        // =====================================================================
        // SUBMIT ROUND (5.6)
        // =====================================================================

        /// <summary>
        /// Submit a round result for a social game.
        /// POST /api/v1/private-rooms/:roomId/round
        /// </summary>
        public static void SubmitRound(
            string roomId,
            int roundNumber,
            object playerResults,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var body = new SubmitRoundRequest
            {
                roundNumber = roundNumber,
                resultsJson = JsonUtility.ToJson(playerResults),
            };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid($"{ROOMS_ENDPOINT}/{roomId}/round", json, onSuccess, onError)
            );
        }

        // =====================================================================
        // TRIGGER SETTLEMENT (5.7)
        // =====================================================================

        /// <summary>
        /// Trigger rake settlement for a social room (host only).
        /// POST /api/v1/private-rooms/:roomId/settle
        /// </summary>
        public static void TriggerSettlement(
            string roomId,
            Action onSuccess,
            Action<RoomError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid(
                    $"{ROOMS_ENDPOINT}/{roomId}/settle", "{}",
                    () =>
                    {
                        RoomExtensionEvents.EmitSettlementTriggered(roomId);
                        onSuccess?.Invoke();
                    },
                    onError
                )
            );
        }

        // =====================================================================
        // INVITE PLAYER (5.8)
        // =====================================================================

        /// <summary>
        /// Invite a player to a room by username or user ID.
        /// POST /api/v1/private-rooms/:roomId/invite
        /// </summary>
        public static void InvitePlayer(
            string roomId,
            string targetUsernameOrId,
            string message,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var body = new InvitePlayerRequest
            {
                targetUsernameOrId = targetUsernameOrId,
                message = message,
            };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid($"{ROOMS_ENDPOINT}/{roomId}/invite", json, onSuccess, onError)
            );
        }

        // =====================================================================
        // GET MY INVITES (5.9)
        // =====================================================================

        /// <summary>
        /// Get all pending room invites for the current user.
        /// GET /api/v1/private-rooms/invites/my
        /// </summary>
        public static void GetMyInvites(
            Action<List<RoomInvite>> onSuccess,
            Action<RoomError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<InviteListWrapper>(
                    $"{ROOMS_ENDPOINT}/invites/my",
                    wrapper => onSuccess?.Invoke(wrapper?.invites ?? new List<RoomInvite>()),
                    onError
                )
            );
        }

        // =====================================================================
        // RESPOND TO INVITE (5.10)
        // =====================================================================

        /// <summary>
        /// Accept or decline a room invite.
        /// POST /api/v1/private-rooms/invites/:id/respond
        /// </summary>
        public static void RespondToInvite(
            string inviteId,
            bool accept,
            Action onSuccess,
            Action<RoomError> onError)
        {
            var body = new InviteRespondRequest { accept = accept };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequestVoid(
                    $"{ROOMS_ENDPOINT}/invites/{inviteId}/respond", json,
                    onSuccess, onError
                )
            );
        }

        // =====================================================================
        // SOCKET EVENT HANDLERS (5.10 - room invite notifications)
        // =====================================================================

        /// <summary>Handle room invite received from socket</summary>
        internal static void HandleInviteReceived(RoomInvite invite)
        {
            DeskillzLogger.Info($"[RoomExtensions] Invite received: {invite.RoomName} from {invite.SenderUsername}");
            RoomExtensionEvents.EmitInviteReceived(invite);
        }

        // =====================================================================
        // HTTP HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint, Action<T> onSuccess, Action<RoomError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Get(url);
            SetupRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator PostRequest<T>(
            string endpoint, string json, Action<T> onSuccess, Action<RoomError> onError)
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
            string endpoint, string json, Action onSuccess, Action<RoomError> onError)
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
            UnityWebRequest request, Action<T> onSuccess, Action<RoomError> onError)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var text = request.downloadHandler.text;
                    if (string.IsNullOrEmpty(text)) { onSuccess?.Invoke(default); return; }
                    onSuccess?.Invoke(JsonUtility.FromJson<T>(text));
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Error($"[RoomExtensions] Parse error: {ex.Message}");
                    onError?.Invoke(new RoomError(RoomError.Codes.ServerError, "Failed to parse response"));
                }
            }
            else
            {
                onError?.Invoke(ParseError(request));
            }
        }

        private static RoomError ParseError(UnityWebRequest request)
        {
            return new RoomError(
                request.responseCode.ToString(),
                request.error ?? "Unknown error"
            );
        }
    }
}
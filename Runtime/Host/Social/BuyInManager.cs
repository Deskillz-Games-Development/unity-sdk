// =============================================================================
// Deskillz SDK for Unity - Buy-In Manager
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Social
{
    /// <summary>
    /// Manager for buy-in, rebuy, and cashout operations in social games.
    /// 
    /// Usage:
    /// <code>
    /// // Buy into a room
    /// BuyInManager.BuyIn("room-123", 100m, "USDT",
    ///     response => Debug.Log($"New balance: ${response.NewBalance}"),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Subscribe to events
    /// BuyInManager.OnBuyInComplete += (amount, balance) => UpdateUI(balance);
    /// </code>
    /// </summary>
    public static class BuyInManager
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Fired when buy-in is successful</summary>
        public static event Action<decimal, decimal> OnBuyInComplete;

        /// <summary>Fired when buy-in fails</summary>
        public static event Action<SocialGameError> OnBuyInFailed;

        /// <summary>Fired when rebuy is successful</summary>
        public static event Action<decimal, decimal> OnRebuyComplete;

        /// <summary>Fired when rebuy fails</summary>
        public static event Action<SocialGameError> OnRebuyFailed;

        /// <summary>Fired when cash out is successful</summary>
        public static event Action<decimal, decimal> OnCashOutComplete;

        /// <summary>Fired when cash out fails</summary>
        public static event Action<SocialGameError> OnCashOutFailed;

        /// <summary>Fired when balance drops below warning threshold</summary>
        public static event Action<decimal, decimal> OnLowBalanceWarning;

        /// <summary>Fired when player busts (balance = 0)</summary>
        public static event Action<string> OnPlayerBusted;

        /// <summary>Fired when rebuy is required</summary>
        public static event Action<decimal> OnRebuyRequired;

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const string SOCIAL_ENDPOINT = "/api/v1/social";
        private const int REQUEST_TIMEOUT = 30;

        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Current session balance</summary>
        public static decimal CurrentBalance { get; private set; }

        /// <summary>Total bought in this session</summary>
        public static decimal TotalBuyIn { get; private set; }

        /// <summary>Current room ID</summary>
        public static string CurrentRoomId { get; private set; }

        /// <summary>Current point value</summary>
        public static decimal CurrentPointValue { get; private set; }

        /// <summary>Whether currently in a session</summary>
        public static bool IsInSession => !string.IsNullOrEmpty(CurrentRoomId);

        /// <summary>Whether balance is low</summary>
        public static bool IsLowBalance => CurrentBalance > 0 && CurrentBalance < LowBalanceThreshold;

        /// <summary>Whether player has busted</summary>
        public static bool HasBusted => CurrentBalance <= 0 && IsInSession;

        /// <summary>Low balance threshold</summary>
        public static decimal LowBalanceThreshold => CurrentPointValue * 20;

        /// <summary>Minimum buy-in amount</summary>
        public static decimal MinBuyIn => CurrentPointValue * 50;

        /// <summary>Default buy-in amount</summary>
        public static decimal DefaultBuyIn => CurrentPointValue * 100;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the buy-in manager for a session.
        /// Called automatically when joining a social game room.
        /// </summary>
        internal static void Initialize(string roomId, decimal pointValue, decimal initialBalance = 0)
        {
            CurrentRoomId = roomId;
            CurrentPointValue = pointValue;
            CurrentBalance = initialBalance;
            TotalBuyIn = initialBalance;

            DeskillzLogger.Debug($"[BuyInManager] Initialized for room {roomId}, point value: ${pointValue}");
        }

        /// <summary>
        /// Reset the buy-in manager.
        /// Called when leaving a room.
        /// </summary>
        internal static void Reset()
        {
            CurrentRoomId = null;
            CurrentPointValue = 0;
            CurrentBalance = 0;
            TotalBuyIn = 0;

            DeskillzLogger.Debug("[BuyInManager] Reset");
        }

        // =====================================================================
        // BUY-IN
        // =====================================================================

        /// <summary>
        /// Buy into the current room.
        /// </summary>
        /// <param name="amount">Amount to buy in</param>
        /// <param name="currency">Currency to use (USDT, USDC, BNB, etc.)</param>
        /// <param name="onSuccess">Called on success with response</param>
        /// <param name="onError">Called on error</param>
        public static void BuyIn(
            decimal amount,
            string currency,
            Action<BuyInResponse> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            BuyIn(CurrentRoomId, amount, currency, onSuccess, onError);
        }

        /// <summary>
        /// Buy into a specific room.
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="amount">Amount to buy in</param>
        /// <param name="currency">Currency to use</param>
        /// <param name="onSuccess">Called on success with response</param>
        /// <param name="onError">Called on error</param>
        public static void BuyIn(
            string roomId,
            decimal amount,
            string currency,
            Action<BuyInResponse> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            if (!ValidateAuthentication(onError)) return;

            if (string.IsNullOrEmpty(roomId))
            {
                var error = new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a room"
                );
                onError?.Invoke(error);
                OnBuyInFailed?.Invoke(error);
                return;
            }

            // Validate minimum buy-in
            if (amount < MinBuyIn)
            {
                var error = new SocialGameError(
                    SocialGameError.Codes.BelowMinBuyIn,
                    $"Minimum buy-in is ${MinBuyIn}"
                );
                onError?.Invoke(error);
                OnBuyInFailed?.Invoke(error);
                return;
            }

            var request = new BuyInRequest
            {
                roomId = roomId,
                amount = amount,
                currency = currency
            };

            DeskillzManager.Instance.StartCoroutine(
                ExecuteBuyIn(request, onSuccess, onError)
            );
        }

        private static IEnumerator ExecuteBuyIn(
            BuyInRequest request,
            Action<BuyInResponse> onSuccess,
            Action<SocialGameError> onError)
        {
            var url = GetFullUrl($"{SOCIAL_ENDPOINT}/buy-in");
            var json = JsonUtility.ToJson(request);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse<BuyInResponse>>(
                        webRequest.downloadHandler.text
                    );

                    if (response.success)
                    {
                        // Update local state
                        CurrentBalance = response.data.NewBalance;
                        TotalBuyIn = response.data.TotalBuyIn;

                        OnBuyInComplete?.Invoke(response.data.Amount, response.data.NewBalance);
                        onSuccess?.Invoke(response.data);

                        DeskillzLogger.Debug($"[BuyInManager] Buy-in success: ${response.data.Amount}, Balance: ${response.data.NewBalance}");
                    }
                    else
                    {
                        var error = new SocialGameError(
                            SocialGameError.Codes.ServerError,
                            response.error ?? "Buy-in failed"
                        );
                        OnBuyInFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                catch (Exception ex)
                {
                    var error = new SocialGameError(
                        SocialGameError.Codes.ServerError,
                        $"Failed to parse response: {ex.Message}"
                    );
                    OnBuyInFailed?.Invoke(error);
                    onError?.Invoke(error);
                }
            }
            else
            {
                var error = ParseError(webRequest);
                OnBuyInFailed?.Invoke(error);
                onError?.Invoke(error);
            }
        }

        // =====================================================================
        // REBUY
        // =====================================================================

        /// <summary>
        /// Rebuy into the current session after busting.
        /// </summary>
        /// <param name="amount">Amount to rebuy (minimum is MinBuyIn)</param>
        /// <param name="currency">Currency to use</param>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void Rebuy(
            decimal amount,
            string currency,
            Action<BuyInResponse> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            if (!ValidateAuthentication(onError)) return;

            if (!IsInSession)
            {
                var error = new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                );
                onError?.Invoke(error);
                OnRebuyFailed?.Invoke(error);
                return;
            }

            // Validate minimum
            if (amount < MinBuyIn)
            {
                var error = new SocialGameError(
                    SocialGameError.Codes.BelowMinBuyIn,
                    $"Minimum rebuy is ${MinBuyIn}"
                );
                onError?.Invoke(error);
                OnRebuyFailed?.Invoke(error);
                return;
            }

            var request = new BuyInRequest
            {
                roomId = CurrentRoomId,
                amount = amount,
                currency = currency
            };

            DeskillzManager.Instance.StartCoroutine(
                ExecuteRebuy(request, onSuccess, onError)
            );
        }

        private static IEnumerator ExecuteRebuy(
            BuyInRequest request,
            Action<BuyInResponse> onSuccess,
            Action<SocialGameError> onError)
        {
            var url = GetFullUrl($"{SOCIAL_ENDPOINT}/rebuy");
            var json = JsonUtility.ToJson(request);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse<BuyInResponse>>(
                        webRequest.downloadHandler.text
                    );

                    if (response.success)
                    {
                        CurrentBalance = response.data.NewBalance;
                        TotalBuyIn = response.data.TotalBuyIn;

                        OnRebuyComplete?.Invoke(response.data.Amount, response.data.NewBalance);
                        onSuccess?.Invoke(response.data);

                        DeskillzLogger.Debug($"[BuyInManager] Rebuy success: ${response.data.Amount}, Balance: ${response.data.NewBalance}");
                    }
                    else
                    {
                        var error = new SocialGameError(
                            SocialGameError.Codes.ServerError,
                            response.error ?? "Rebuy failed"
                        );
                        OnRebuyFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                catch (Exception ex)
                {
                    var error = new SocialGameError(
                        SocialGameError.Codes.ServerError,
                        $"Failed to parse response: {ex.Message}"
                    );
                    OnRebuyFailed?.Invoke(error);
                    onError?.Invoke(error);
                }
            }
            else
            {
                var error = ParseError(webRequest);
                OnRebuyFailed?.Invoke(error);
                onError?.Invoke(error);
            }
        }

        // =====================================================================
        // CASH OUT
        // =====================================================================

        /// <summary>
        /// Cash out of the current session.
        /// Can only be called between rounds.
        /// </summary>
        /// <param name="onSuccess">Called on success with cash out response</param>
        /// <param name="onError">Called on error</param>
        public static void CashOut(
            Action<CashOutResponse> onSuccess = null,
            Action<SocialGameError> onError = null)
        {
            if (!ValidateAuthentication(onError)) return;

            if (!IsInSession)
            {
                var error = new SocialGameError(
                    SocialGameError.Codes.NotInSession,
                    "Not in a session"
                );
                onError?.Invoke(error);
                OnCashOutFailed?.Invoke(error);
                return;
            }

            var request = new CashOutRequest
            {
                roomId = CurrentRoomId
            };

            DeskillzManager.Instance.StartCoroutine(
                ExecuteCashOut(request, onSuccess, onError)
            );
        }

        private static IEnumerator ExecuteCashOut(
            CashOutRequest request,
            Action<CashOutResponse> onSuccess,
            Action<SocialGameError> onError)
        {
            var url = GetFullUrl($"{SOCIAL_ENDPOINT}/cash-out");
            var json = JsonUtility.ToJson(request);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var webRequest = new UnityWebRequest(url, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            SetupRequest(webRequest);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse<CashOutResponse>>(
                        webRequest.downloadHandler.text
                    );

                    if (response.success)
                    {
                        decimal cashedOutAmount = CurrentBalance;
                        decimal netProfitLoss = response.data.NetProfitLoss;

                        // Clear local state
                        CurrentBalance = 0;

                        OnCashOutComplete?.Invoke(cashedOutAmount, netProfitLoss);
                        onSuccess?.Invoke(response.data);

                        DeskillzLogger.Debug($"[BuyInManager] Cash out success: ${cashedOutAmount}, Net P/L: ${netProfitLoss}");
                    }
                    else
                    {
                        var error = new SocialGameError(
                            SocialGameError.Codes.ServerError,
                            response.error ?? "Cash out failed"
                        );
                        OnCashOutFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                catch (Exception ex)
                {
                    var error = new SocialGameError(
                        SocialGameError.Codes.ServerError,
                        $"Failed to parse response: {ex.Message}"
                    );
                    OnCashOutFailed?.Invoke(error);
                    onError?.Invoke(error);
                }
            }
            else
            {
                var error = ParseError(webRequest);
                OnCashOutFailed?.Invoke(error);
                onError?.Invoke(error);
            }
        }

        // =====================================================================
        // BALANCE UPDATES (Internal)
        // =====================================================================

        /// <summary>
        /// Update balance from game events (called internally by SocialGameManager).
        /// </summary>
        internal static void UpdateBalance(decimal newBalance)
        {
            decimal previousBalance = CurrentBalance;
            CurrentBalance = newBalance;

            // Check for low balance warning
            if (!IsLowBalance && previousBalance >= LowBalanceThreshold && newBalance < LowBalanceThreshold && newBalance > 0)
            {
                OnLowBalanceWarning?.Invoke(newBalance, LowBalanceThreshold);
                DeskillzLogger.Debug($"[BuyInManager] Low balance warning: ${newBalance}");
            }

            // Check for bust
            if (previousBalance > 0 && newBalance <= 0)
            {
                OnPlayerBusted?.Invoke(CurrentRoomId);
                OnRebuyRequired?.Invoke(MinBuyIn);
                DeskillzLogger.Debug("[BuyInManager] Player busted, rebuy required");
            }
        }

        /// <summary>
        /// Get buy-in options for current session.
        /// </summary>
        public static BuyInOptions GetBuyInOptions()
        {
            return RakeCalculator.CalculateBuyInOptions(CurrentPointValue);
        }

        /// <summary>
        /// Validate a buy-in amount for current session.
        /// </summary>
        public static BuyInValidation ValidateBuyIn(decimal amount)
        {
            return RakeCalculator.ValidateBuyIn(amount, CurrentPointValue);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static bool ValidateAuthentication(Action<SocialGameError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                var error = new SocialGameError(
                    SocialGameError.Codes.NotAuthenticated,
                    "Not authenticated"
                );
                onError?.Invoke(error);
                return false;
            }
            return true;
        }

        private static string GetFullUrl(string endpoint)
        {
            var baseUrl = DeskillzManager.Instance?.Config?.ApiBaseUrl ?? "https://api.deskillz.games";
            return $"{baseUrl}{endpoint}";
        }

        private static void SetupRequest(UnityWebRequest request)
        {
            request.timeout = REQUEST_TIMEOUT;
            request.SetRequestHeader("Content-Type", "application/json");

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

        private static SocialGameError ParseError(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                try
                {
                    var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(errorResponse?.error))
                    {
                        return new SocialGameError(
                            errorResponse.code ?? SocialGameError.Codes.ServerError,
                            errorResponse.error
                        );
                    }
                }
                catch { }
            }

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return new SocialGameError(SocialGameError.Codes.NetworkError, "Network connection failed");
            }

            return new SocialGameError(SocialGameError.Codes.ServerError, $"Server error: {request.responseCode}");
        }

        // =====================================================================
        // INTERNAL TYPES
        // =====================================================================

        [Serializable]
        private class ApiResponse<T>
        {
            public bool success;
            public T data;
            public string error;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string error;
            public string code;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        /// <summary>
        /// Clear all event subscriptions.
        /// </summary>
        internal static void ClearAllSubscriptions()
        {
            OnBuyInComplete = null;
            OnBuyInFailed = null;
            OnRebuyComplete = null;
            OnRebuyFailed = null;
            OnCashOutComplete = null;
            OnCashOutFailed = null;
            OnLowBalanceWarning = null;
            OnPlayerBusted = null;
            OnRebuyRequired = null;
        }
    }
}
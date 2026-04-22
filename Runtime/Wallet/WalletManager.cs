// =============================================================================
// Deskillz SDK for Unity - Wallet & Profile Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity - Phase 7)
// =============================================================================
//
// Multi-currency wallet operations, player stats, match history, leaderboard.
// Mirrors DeskillzBridge.ts lines 1377-1796.
//
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz.Wallet
{
    // =========================================================================
    // REQUEST/RESPONSE MODELS
    // =========================================================================

    [Serializable]
    internal class DepositRequest
    {
        public string currency;
        public decimal amount;
    }

    [Serializable]
    internal class WithdrawRequest
    {
        public string currency;
        public decimal amount;
        public string walletAddress;
    }

    [Serializable]
    internal class DepositResponse
    {
        public bool success;
        public string transactionId;
        public string depositAddress;
        public string currency;
        public decimal amount;
    }

    [Serializable]
    internal class WithdrawResponse
    {
        public bool success;
        public string transactionId;
        public decimal amount;
        public string currency;
        public string estimatedArrival;
    }

    [Serializable]
    internal class BalanceListWrapper
    {
        public List<WalletBalanceEntry> balances;
    }

    [Serializable]
    internal class TransactionListWrapper
    {
        public List<TransactionRecord> transactions;
        public int total;
    }

    [Serializable]
    internal class MatchHistoryWrapper
    {
        public List<MatchRecord> matches;
        public int total;
    }

    [Serializable]
    internal class LeaderboardWrapper
    {
        public List<LeaderboardEntry> entries;
        public LeaderboardEntry currentPlayer;
    }

    /// <summary>
    /// Transaction record for wallet history
    /// </summary>
    [Serializable]
    public class TransactionRecord
    {
        public string Id;
        public string Type;
        public decimal Amount;
        public string Currency;
        public string Status;
        public string Description;
        public string TxHash;
        public DateTime CreatedAt;
    }

    /// <summary>
    /// Wallet API error
    /// </summary>
    [Serializable]
    public class WalletError
    {
        public string Code;
        public string Message;

        public WalletError() { }
        public WalletError(string code, string message) { Code = code; Message = message; }
        public override string ToString() => $"WalletError({Code}: {Message})";
    }

    // =========================================================================
    // MANAGER
    // =========================================================================

    /// <summary>
    /// Wallet, profile stats, match history, and leaderboard operations.
    /// </summary>
    public static class WalletManager
    {
        private const string WALLET_ENDPOINT = "/api/v1/wallet";
        private const string USERS_ENDPOINT = "/api/v1/users";
        private const string MATCHES_ENDPOINT = "/api/v1/matches/history/me";
        private const string LEADERBOARD_ENDPOINT = "/api/v1/leaderboard";
        private const int REQUEST_TIMEOUT = 30;

        /// <summary>Cached wallet balances</summary>
        public static List<WalletBalanceEntry> CachedBalances { get; private set; } = new List<WalletBalanceEntry>();

        // =====================================================================
        // GET WALLET BALANCE (7.1)
        // =====================================================================

        /// <summary>
        /// Get all wallet balances (multi-currency).
        /// GET /api/v1/wallet/balance
        /// </summary>
        public static void GetBalance(
            Action<List<WalletBalanceEntry>> onSuccess,
            Action<WalletError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<BalanceListWrapper>(
                    $"{WALLET_ENDPOINT}/balance",
                    wrapper =>
                    {
                        CachedBalances = wrapper?.balances ?? new List<WalletBalanceEntry>();
                        onSuccess?.Invoke(CachedBalances);
                    },
                    onError
                )
            );
        }

        // =====================================================================
        // GET BALANCE FOR CURRENCY (7.2)
        // =====================================================================

        /// <summary>
        /// Get balance for a specific currency.
        /// GET /api/v1/wallet/balance/:currency
        /// </summary>
        public static void GetBalanceForCurrency(
            string currency,
            Action<WalletBalanceEntry> onSuccess,
            Action<WalletError> onError)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<WalletBalanceEntry>(
                    $"{WALLET_ENDPOINT}/balance/{currency}",
                    onSuccess, onError
                )
            );
        }

        // =====================================================================
        // DEPOSIT (7.3)
        // =====================================================================

        /// <summary>
        /// Initiate a deposit.
        /// POST /api/v1/wallet/deposit
        /// </summary>
        public static void Deposit(
            string currency,
            decimal amount,
            Action<DepositResponse> onSuccess,
            Action<WalletError> onError)
        {
            var body = new DepositRequest { currency = currency, amount = amount };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<DepositResponse>($"{WALLET_ENDPOINT}/deposit", json, onSuccess, onError)
            );
        }

        // =====================================================================
        // WITHDRAW (7.4)
        // =====================================================================

        /// <summary>
        /// Initiate a withdrawal.
        /// POST /api/v1/wallet/withdraw
        /// </summary>
        public static void Withdraw(
            string currency,
            decimal amount,
            string walletAddress,
            Action<WithdrawResponse> onSuccess,
            Action<WalletError> onError)
        {
            var body = new WithdrawRequest
            {
                currency = currency,
                amount = amount,
                walletAddress = walletAddress,
            };
            var json = JsonUtility.ToJson(body);
            DeskillzManager.Instance.StartCoroutine(
                PostRequest<WithdrawResponse>($"{WALLET_ENDPOINT}/withdraw", json, onSuccess, onError)
            );
        }

        // =====================================================================
        // PLAYER STATS (7.5)
        // =====================================================================

        /// <summary>
        /// Get current player's stats.
        /// GET /api/v1/users/:id/stats
        /// </summary>
        public static void GetPlayerStats(
            Action<PlayerStats> onSuccess,
            Action<WalletError> onError)
        {
            var userId = DeskillzManager.Instance?.CurrentPlayer?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                onError?.Invoke(new WalletError("NOT_AUTHENTICATED", "Not authenticated"));
                return;
            }
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<PlayerStats>($"{USERS_ENDPOINT}/{userId}/stats", onSuccess, onError)
            );
        }

        // =====================================================================
        // MATCH HISTORY (7.6)
        // =====================================================================

        /// <summary>
        /// Get match history for current player.
        /// GET /api/v1/matches/history/me
        /// </summary>
        public static void GetMatchHistory(
            Action<List<MatchRecord>> onSuccess,
            Action<WalletError> onError,
            int page = 1,
            int limit = 20)
        {
            var offset = (page - 1) * limit;
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<MatchHistoryWrapper>(
                    $"{MATCHES_ENDPOINT}?limit={limit}&offset={offset}",
                    wrapper => onSuccess?.Invoke(wrapper?.matches ?? new List<MatchRecord>()),
                    onError
                )
            );
        }

        // =====================================================================
        // LEADERBOARD (7.7)
        // =====================================================================

        /// <summary>
        /// Get game leaderboard.
        /// GET /api/v1/leaderboard/:gameId
        /// </summary>
        public static void GetGameLeaderboard(
            string gameId,
            Action<List<LeaderboardEntry>> onSuccess,
            Action<WalletError> onError,
            string period = "all",
            int limit = 50)
        {
            DeskillzManager.Instance.StartCoroutine(
                GetRequest<LeaderboardWrapper>(
                    $"{LEADERBOARD_ENDPOINT}/{gameId}?period={period}&limit={limit}",
                    wrapper => onSuccess?.Invoke(wrapper?.entries ?? new List<LeaderboardEntry>()),
                    onError
                )
            );
        }

        // =====================================================================
        // TRANSACTIONS (7.8)
        // =====================================================================

        /// <summary>
        /// Get wallet transaction history.
        /// GET /api/v1/wallet/transactions
        /// </summary>
        public static void GetTransactions(
            Action<List<TransactionRecord>> onSuccess,
            Action<WalletError> onError,
            int limit = 20,
            int offset = 0,
            string type = null,
            string currency = null)
        {
            var query = $"?limit={limit}&offset={offset}";
            if (!string.IsNullOrEmpty(type)) query += $"&type={type}";
            if (!string.IsNullOrEmpty(currency)) query += $"&currency={currency}";

            DeskillzManager.Instance.StartCoroutine(
                GetRequest<TransactionListWrapper>(
                    $"{WALLET_ENDPOINT}/transactions{query}",
                    wrapper => onSuccess?.Invoke(wrapper?.transactions ?? new List<TransactionRecord>()),
                    onError
                )
            );
        }

        // =====================================================================
        // HTTP HELPERS
        // =====================================================================

        private static IEnumerator GetRequest<T>(
            string endpoint, Action<T> onSuccess, Action<WalletError> onError)
        {
            var url = GetFullUrl(endpoint);
            using var request = UnityWebRequest.Get(url);
            SetupRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private static IEnumerator PostRequest<T>(
            string endpoint, string json, Action<T> onSuccess, Action<WalletError> onError)
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
            request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request, Action<T> onSuccess, Action<WalletError> onError)
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
                    DeskillzLogger.Error($"[WalletManager] Parse error: {ex.Message}");
                    onError?.Invoke(new WalletError("PARSE_ERROR", "Failed to parse response"));
                }
            }
            else
            {
                onError?.Invoke(new WalletError(request.responseCode.ToString(), request.error ?? "Unknown error"));
            }
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Deep Link Handler
// Handles incoming deep links from the Deskillz website/lobby
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Handles deep links from the Deskillz website lobby to the game.
    /// Players join tournaments via the website, then the game is launched via deep link.
    /// </summary>
    public class DeepLinkHandler : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static DeepLinkHandler _instance;
        public static DeepLinkHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DeepLinkHandler>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DeskillzDeepLinkHandler");
                        _instance = go.AddComponent<DeepLinkHandler>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when a match deep link is received from the lobby.
        /// </summary>
        public static event Action<MatchLaunchData> OnMatchLaunchReceived;

        /// <summary>
        /// Fired when the deep link is invalid or missing required data.
        /// </summary>
        public static event Action<string> OnDeepLinkError;

        /// <summary>
        /// Fired when app is opened without a deep link (normal launch).
        /// </summary>
        public static event Action OnNormalLaunch;
       /// <summary>
        /// Fired when a navigation deep link is received (not a match launch).
        /// </summary>
        public static event Action<NavigationAction, string> OnNavigationReceived;

        // =============================================================================
        // NAVIGATION ACTIONS
        // =============================================================================

        /// <summary>
        /// Navigation actions from deep links (when app is opened from website).
        /// </summary>
        public enum NavigationAction
        {
            None,
            Tournaments,    // deskillz://tournaments
            Wallet,         // deskillz://wallet
            Profile,        // deskillz://profile
            Game,           // deskillz://game?id=xxx
            Settings        // deskillz://settings
        }
        // =============================================================================
        // STATE
        // =============================================================================

        private bool _hasProcessedInitialDeepLink;
        private MatchLaunchData _pendingMatch;

        /// <summary>
        /// Whether there's a pending match waiting to be started.
        /// </summary>
        public bool HasPendingMatch => _pendingMatch != null;

        /// <summary>
        /// The pending match data (null if no match pending).
        /// </summary>
        public MatchLaunchData PendingMatch => _pendingMatch;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Check for deep link on initial launch
            ProcessInitialDeepLink();
        }

        private void OnEnable()
        {
            // Subscribe to deep link events (for when app is already running)
            Application.deepLinkActivated += HandleDeepLink;
        }

        private void OnDisable()
        {
            Application.deepLinkActivated -= HandleDeepLink;
        }

        // =============================================================================
        // DEEP LINK PROCESSING
        // =============================================================================

        /// <summary>
        /// Process the deep link that launched the app (if any).
        /// </summary>
        private void ProcessInitialDeepLink()
        {
            if (_hasProcessedInitialDeepLink) return;
            _hasProcessedInitialDeepLink = true;

            string initialDeepLink = Application.absoluteURL;

            if (string.IsNullOrEmpty(initialDeepLink))
            {
                DeskillzLogger.Debug("App launched normally (no deep link)");
                OnNormalLaunch?.Invoke();
                return;
            }

            DeskillzLogger.Info($"App launched via deep link: {initialDeepLink}");
            HandleDeepLink(initialDeepLink);
        }

       /// <summary>
        /// Handle an incoming deep link.
        /// </summary>
        private void HandleDeepLink(string url)
        {
            DeskillzLogger.Info($"Deep link received: {url}");

            try
            {
                // =====================================================
                // STEP 1: Check for navigation deep links FIRST
                // These don't have matchId/token parameters
                // =====================================================
                var navAction = ParseNavigationLink(url);
                if (navAction.Action != NavigationAction.None)
                {
                    DeskillzLogger.Info($"Navigation deep link: {navAction.Action}, Target: {navAction.TargetId}");
                    OnNavigationReceived?.Invoke(navAction.Action, navAction.TargetId);
                    DeskillzEvents.RaiseNavigationReceived(navAction.Action, navAction.TargetId);
                    return;
                }

                // =====================================================
                // STEP 2: Try to parse as match launch deep link
                // =====================================================
                var matchData = ParseDeepLink(url);

                if (matchData == null)
                {
                    DeskillzLogger.Error("Failed to parse deep link");
                    OnDeepLinkError?.Invoke("Invalid deep link format");
                    return;
                }

                // Validate required fields
                if (string.IsNullOrEmpty(matchData.MatchId))
                {
                    DeskillzLogger.Error("Deep link missing matchId");
                    OnDeepLinkError?.Invoke("Missing match ID");
                    return;
                }

                if (string.IsNullOrEmpty(matchData.Token))
                {
                    DeskillzLogger.Error("Deep link missing token");
                    OnDeepLinkError?.Invoke("Missing authentication token");
                    return;
                }

                // Store pending match
                _pendingMatch = matchData;

                DeskillzLogger.LogMatch("Received from lobby", matchData.MatchId, 
                    $"Tournament: {matchData.TournamentId}, Mode: {matchData.Mode}");

                // Notify listeners
                OnMatchLaunchReceived?.Invoke(matchData);

                // Also raise SDK event
                DeskillzEvents.RaiseDeepLinkReceived(url, matchData);

            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Error processing deep link: {ex.Message}");
                OnDeepLinkError?.Invoke(ex.Message);
            }
        }
        /// <summary>
        /// Parse navigation deep links (tournaments, wallet, profile, etc.)
        /// Format: deskillz://tournaments OR deskillz://game?id=xxx
        /// </summary>
        private (NavigationAction Action, string TargetId) ParseNavigationLink(string url)
        {
            if (string.IsNullOrEmpty(url))
                return (NavigationAction.None, null);

            try
            {
                // Extract the host/path part (e.g., "tournaments" from "deskillz://tournaments")
                string lowerUrl = url.ToLower();
                
                // Get the part after ://
                int schemeEnd = url.IndexOf("://");
                if (schemeEnd < 0) 
                    return (NavigationAction.None, null);

                string remainder = url.Substring(schemeEnd + 3);
                
                // Split by ? to separate path from query
                int queryStart = remainder.IndexOf('?');
                string path = queryStart >= 0 ? remainder.Substring(0, queryStart) : remainder;
                string query = queryStart >= 0 ? remainder.Substring(queryStart + 1) : "";

                // Remove trailing slashes
                path = path.TrimEnd('/').ToLower();

                // Parse based on path
                switch (path)
                {
                    case "tournaments":
                    case "tournament":
                        return (NavigationAction.Tournaments, null);

                    case "wallet":
                        return (NavigationAction.Wallet, null);

                    case "profile":
                        return (NavigationAction.Profile, null);

                    case "settings":
                        return (NavigationAction.Settings, null);

                    case "game":
                    case "games":
                        // Extract game ID from query string
                        var queryParams = ParseQueryString(query);
                        string gameId = GetParam(queryParams, "id", "gameId", "game_id");
                        if (!string.IsNullOrEmpty(gameId))
                            return (NavigationAction.Game, gameId);
                        return (NavigationAction.None, null);

                    default:
                        // Not a navigation link (might be a match link)
                        return (NavigationAction.None, null);
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to parse navigation link: {ex.Message}");
                return (NavigationAction.None, null);
            }
        }
        /// <summary>
        /// Parse deep link URL into MatchLaunchData.
        /// Expected format: deskillz-gamename://match?id=xxx&token=xxx&tournament=xxx&mode=sync
        /// </summary>
        private MatchLaunchData ParseDeepLink(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                // Extract query string
                int queryStart = url.IndexOf('?');
                if (queryStart < 0) return null;

                string queryString = url.Substring(queryStart + 1);
                var parameters = ParseQueryString(queryString);

                var data = new MatchLaunchData
                {
                    RawDeepLink = url,
                    MatchId = GetParam(parameters, "id", "matchId", "match_id"),
                    Token = GetParam(parameters, "token", "auth_token", "authToken"),
                    TournamentId = GetParam(parameters, "tournament", "tournamentId", "tournament_id"),
                    GameId = GetParam(parameters, "game", "gameId", "game_id"),
                    ReceivedAt = DateTime.UtcNow
                };

                // Parse mode
                string modeStr = GetParam(parameters, "mode", "matchMode");
                data.Mode = ParseMatchMode(modeStr);

                // Parse optional numeric values
                if (parameters.TryGetValue("entry_fee", out string entryFeeStr) ||
                    parameters.TryGetValue("entryFee", out entryFeeStr))
                {
                    decimal.TryParse(entryFeeStr, out data.EntryFee);
                }

                if (parameters.TryGetValue("prize_pool", out string prizePoolStr) ||
                    parameters.TryGetValue("prizePool", out prizePoolStr))
                {
                    decimal.TryParse(prizePoolStr, out data.PrizePool);
                }

                if (parameters.TryGetValue("time_limit", out string timeLimitStr) ||
                    parameters.TryGetValue("timeLimit", out timeLimitStr))
                {
                    int.TryParse(timeLimitStr, out data.TimeLimitSeconds);
                }

                if (parameters.TryGetValue("max_players", out string maxPlayersStr) ||
                    parameters.TryGetValue("maxPlayers", out maxPlayersStr))
                {
                    int.TryParse(maxPlayersStr, out data.MaxPlayers);
                }

                // Parse currency
                string currencyStr = GetParam(parameters, "currency", "coin");
                data.Currency = ParseCurrency(currencyStr);

                // Store custom parameters
                foreach (var kvp in parameters)
                {
                    if (!IsStandardParam(kvp.Key))
                    {
                        data.CustomParams[kvp.Key] = kvp.Value;
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to parse deep link: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse query string into dictionary.
        /// </summary>
        private Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(query)) return result;

            string[] pairs = query.Split('&');
            foreach (string pair in pairs)
            {
                string[] kvp = pair.Split('=');
                if (kvp.Length == 2)
                {
                    string key = UnityWebRequest.UnEscapeURL(kvp[0]);
                    string value = UnityWebRequest.UnEscapeURL(kvp[1]);
                    result[key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Get parameter value trying multiple possible keys.
        /// </summary>
        private string GetParam(Dictionary<string, string> parameters, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (parameters.TryGetValue(key, out string value))
                {
                    return value;
                }
            }
            return null;
        }

        /// <summary>
        /// Parse match mode from string.
        /// </summary>
        private MatchMode ParseMatchMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return MatchMode.Asynchronous;

            mode = mode.ToLower();
            switch (mode)
            {
                case "sync":
                case "synchronous":
                case "realtime":
                case "real-time":
                    return MatchMode.Synchronous;

                case "stage":
                case "private":
                case "custom":
                    return MatchMode.Stage;

                case "async":
                case "asynchronous":
                default:
                    return MatchMode.Asynchronous;
            }
        }

        /// <summary>
        /// Parse currency from string.
        /// </summary>
        private Currency ParseCurrency(string currency)
        {
            if (string.IsNullOrEmpty(currency)) return Currency.USDT;

            currency = currency.ToUpper();
            switch (currency)
            {
                case "BTC": return Currency.BTC;
                case "ETH": return Currency.ETH;
                case "SOL": return Currency.SOL;
                case "XRP": return Currency.XRP;
                case "BNB": return Currency.BNB;
                case "USDC": return Currency.USDC;
                case "FREE": return Currency.Free;
                default: return Currency.USDT;
            }
        }

        /// <summary>
        /// Check if a parameter key is a standard (non-custom) parameter.
        /// </summary>
        private bool IsStandardParam(string key)
        {
            string lowerKey = key.ToLower();
            return lowerKey == "id" || lowerKey == "matchid" || lowerKey == "match_id" ||
                   lowerKey == "token" || lowerKey == "auth_token" || lowerKey == "authtoken" ||
                   lowerKey == "tournament" || lowerKey == "tournamentid" || lowerKey == "tournament_id" ||
                   lowerKey == "game" || lowerKey == "gameid" || lowerKey == "game_id" ||
                   lowerKey == "mode" || lowerKey == "matchmode" ||
                   lowerKey == "entry_fee" || lowerKey == "entryfee" ||
                   lowerKey == "prize_pool" || lowerKey == "prizepool" ||
                   lowerKey == "time_limit" || lowerKey == "timelimit" ||
                   lowerKey == "max_players" || lowerKey == "maxplayers" ||
                   lowerKey == "currency" || lowerKey == "coin";
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Manually process a deep link (for testing or custom implementations).
        /// </summary>
        public void ProcessDeepLink(string url)
        {
            HandleDeepLink(url);
        }

        /// <summary>
        /// Clear the pending match (after it has been started).
        /// </summary>
        public void ClearPendingMatch()
        {
            _pendingMatch = null;
            DeskillzLogger.Debug("Pending match cleared");
        }

        /// <summary>
        /// Consume and return the pending match (clears it after returning).
        /// </summary>
        public MatchLaunchData ConsumePendingMatch()
        {
            var match = _pendingMatch;
            _pendingMatch = null;
            return match;
        }

        /// <summary>
        /// Generate a test deep link for development.
        /// </summary>
        public static string GenerateTestDeepLink(
            string matchId = null,
            string tournamentId = null,
            MatchMode mode = MatchMode.Asynchronous,
            decimal entryFee = 0,
            Currency currency = Currency.Free)
        {
            matchId = matchId ?? $"test_match_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            tournamentId = tournamentId ?? $"test_tournament_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string token = $"test_token_{Guid.NewGuid().ToString("N")}";

            string scheme = DeskillzConfig.Instance?.DeepLinkScheme ?? "deskillz";
            string modeStr = mode == MatchMode.Synchronous ? "sync" : 
                            mode == MatchMode.Stage ? "stage" : "async";

            return $"{scheme}://match?id={matchId}&token={token}&tournament={tournamentId}" +
                   $"&mode={modeStr}&entry_fee={entryFee}&currency={currency}";
        }
    }

    // =============================================================================
    // MATCH LAUNCH DATA
    // =============================================================================

    /// <summary>
    /// Data received from a deep link when launching into a match from the lobby.
    /// </summary>
    [Serializable]
    public class MatchLaunchData
    {
        /// <summary>
        /// Raw deep link URL that was received.
        /// </summary>
        public string RawDeepLink;

        /// <summary>
        /// Unique match identifier.
        /// </summary>
        public string MatchId;

        /// <summary>
        /// Authentication token for this match session.
        /// </summary>
        public string Token;

        /// <summary>
        /// Tournament this match belongs to.
        /// </summary>
        public string TournamentId;

        /// <summary>
        /// Game identifier.
        /// </summary>
        public string GameId;

        /// <summary>
        /// Match mode (sync, async, stage).
        /// </summary>
        public MatchMode Mode = MatchMode.Asynchronous;

        /// <summary>
        /// Entry fee for the match.
        /// </summary>
        public decimal EntryFee;

        /// <summary>
        /// Prize pool for the match.
        /// </summary>
        public decimal PrizePool;

        /// <summary>
        /// Currency for entry fee and prizes.
        /// </summary>
        public Currency Currency = Currency.USDT;

        /// <summary>
        /// Time limit in seconds (0 = unlimited).
        /// </summary>
        public int TimeLimitSeconds;

        /// <summary>
        /// Maximum players in the match.
        /// </summary>
        public int MaxPlayers = 2;

        /// <summary>
        /// When the deep link was received.
        /// </summary>
        public DateTime ReceivedAt;

        /// <summary>
        /// Custom parameters passed via deep link.
        /// </summary>
        public Dictionary<string, string> CustomParams = new Dictionary<string, string>();

        /// <summary>
        /// Whether the token is still valid (not expired).
        /// Tokens are typically valid for 5 minutes.
        /// </summary>
        public bool IsTokenValid => (DateTime.UtcNow - ReceivedAt).TotalMinutes < 5;

        /// <summary>
        /// Whether this is a real-time (synchronous) match.
        /// </summary>
        public bool IsRealtime => Mode == MatchMode.Synchronous;
    }
}
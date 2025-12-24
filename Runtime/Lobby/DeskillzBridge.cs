// =============================================================================
// Deskillz SDK for Unity - Lobby Bridge
// Simplified bridge between the Deskillz website lobby and your game
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Main bridge class for integrating your game with the Deskillz platform.
    /// 
    /// NEW ARCHITECTURE:
    /// - Players browse games and join tournaments on the Deskillz website/app
    /// - When a match is found, the website launches your game via deep link
    /// - Your game receives the match data and starts gameplay
    /// - When gameplay ends, scores are submitted and the player returns to the website
    /// 
    /// This removes the need for in-game matchmaking UI - all matchmaking happens
    /// on the centralized Deskillz platform.
    /// </summary>
    public class DeskillzBridge : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static DeskillzBridge _instance;
        public static DeskillzBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DeskillzBridge>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DeskillzBridge");
                        _instance = go.AddComponent<DeskillzBridge>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [Tooltip("Your game's unique ID from the Deskillz Developer Portal")]
        [SerializeField] private string _gameId;

        [Tooltip("Enable test mode for development")]
        [SerializeField] private bool _testMode = true;

        [Tooltip("API endpoint (leave empty for production)")]
        [SerializeField] private string _apiEndpoint = "";

        [Header("Game Settings")]
        [Tooltip("Higher score wins (true) or lower score wins (false)")]
        [SerializeField] private bool _higherScoreWins = true;

        [Tooltip("Default match duration in seconds (0 = unlimited)")]
        [SerializeField] private int _defaultMatchDuration = 0;

        // =============================================================================
        // EVENTS - Subscribe to these in your game
        // =============================================================================

        /// <summary>
        /// Called when the SDK is ready to use.
        /// </summary>
        public static event Action OnReady;

        /// <summary>
        /// Called when a match is received from the lobby (deep link).
        /// Your game should load the gameplay scene when this fires.
        /// </summary>
        public static event Action<MatchInfo> OnMatchReceived;

        /// <summary>
        /// Called when gameplay should begin.
        /// Start your game timer and enable player input.
        /// </summary>
        public static event Action OnMatchStart;

        /// <summary>
        /// Called when the match ends (score submitted successfully).
        /// Show results UI or return to main menu.
        /// </summary>
        public static event Action<MatchResult> OnMatchComplete;

        /// <summary>
        /// Called when an error occurs.
        /// </summary>
        public static event Action<string> OnError;

        /// <summary>
        /// Called when the app is launched normally (not from a deep link).
        /// Show your main menu / practice mode.
        /// </summary>
        public static event Action OnNormalLaunch;

        // =============================================================================
        // STATE
        // =============================================================================

        private bool _isInitialized;
        private bool _isInMatch;
        private MatchInfo _currentMatch;
        private int _currentScore;
        private DateTime _matchStartTime;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>
        /// Whether the SDK is initialized and ready.
        /// </summary>
        public static bool IsReady => Instance._isInitialized;

        /// <summary>
        /// Whether currently in an active match.
        /// </summary>
        public static bool IsInMatch => Instance._isInMatch;

        /// <summary>
        /// Current match info (null if not in match).
        /// </summary>
        public static MatchInfo CurrentMatch => Instance._currentMatch;

        /// <summary>
        /// Whether running in test mode.
        /// </summary>
        public static bool IsTestMode => Instance._testMode;

        /// <summary>
        /// Current player's score in the active match.
        /// </summary>
        public static int CurrentScore => Instance._currentScore;

        /// <summary>
        /// Time elapsed since match started.
        /// </summary>
        public static float MatchElapsedTime => Instance._isInMatch 
            ? (float)(DateTime.UtcNow - Instance._matchStartTime).TotalSeconds 
            : 0f;

        /// <summary>
        /// Time remaining in the match (if time-limited).
        /// </summary>
        public static float MatchTimeRemaining
        {
            get
            {
                if (!Instance._isInMatch || Instance._currentMatch == null) return 0f;
                if (Instance._currentMatch.TimeLimitSeconds <= 0) return float.MaxValue;
                return Mathf.Max(0, Instance._currentMatch.TimeLimitSeconds - MatchElapsedTime);
            }
        }

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

            Initialize();
        }

        private void OnEnable()
        {
            // Subscribe to deep link handler
            DeepLinkHandler.OnMatchLaunchReceived += HandleMatchLaunchReceived;
            DeepLinkHandler.OnNormalLaunch += HandleNormalLaunch;
            DeepLinkHandler.OnDeepLinkError += HandleDeepLinkError;
        }

        private void OnDisable()
        {
            DeepLinkHandler.OnMatchLaunchReceived -= HandleMatchLaunchReceived;
            DeepLinkHandler.OnNormalLaunch -= HandleNormalLaunch;
            DeepLinkHandler.OnDeepLinkError -= HandleDeepLinkError;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_isInitialized) return;

            DeskillzLogger.Info($"DeskillzBridge initializing (TestMode: {_testMode})");

            // Initialize the core SDK
            Deskillz.Initialize();

            // Configure API endpoint
            if (!string.IsNullOrEmpty(_apiEndpoint))
            {
                // Custom endpoint for development
            }

            _isInitialized = true;

            DeskillzLogger.Info("DeskillzBridge ready");
            OnReady?.Invoke();

            // Ensure deep link handler is active
            var _ = DeepLinkHandler.Instance;
        }

        // =============================================================================
        // DEEP LINK HANDLERS
        // =============================================================================

        private void HandleMatchLaunchReceived(MatchLaunchData launchData)
        {
            DeskillzLogger.LogMatch("Received from lobby", launchData.MatchId);

            // Convert to MatchInfo
            _currentMatch = new MatchInfo
            {
                MatchId = launchData.MatchId,
                TournamentId = launchData.TournamentId,
                Token = launchData.Token,
                Mode = launchData.Mode,
                EntryFee = launchData.EntryFee,
                PrizePool = launchData.PrizePool,
                Currency = launchData.Currency,
                TimeLimitSeconds = launchData.TimeLimitSeconds,
                MaxPlayers = launchData.MaxPlayers,
                IsRealtime = launchData.IsRealtime,
                CustomParams = launchData.CustomParams
            };

            OnMatchReceived?.Invoke(_currentMatch);
        }

        private void HandleNormalLaunch()
        {
            DeskillzLogger.Info("Normal launch (no match)");
            OnNormalLaunch?.Invoke();
        }

        private void HandleDeepLinkError(string error)
        {
            DeskillzLogger.Error($"Deep link error: {error}");
            OnError?.Invoke(error);
        }

        // =============================================================================
        // PUBLIC API - MATCH FLOW
        // =============================================================================

        /// <summary>
        /// Call this when your game is ready to start the match.
        /// Typically called after loading your gameplay scene.
        /// </summary>
        public static void StartMatch()
        {
            if (!IsReady)
            {
                DeskillzLogger.Error("SDK not ready");
                OnError?.Invoke("SDK not initialized");
                return;
            }

            if (Instance._currentMatch == null)
            {
                DeskillzLogger.Error("No match to start");
                OnError?.Invoke("No match data available");
                return;
            }

            if (Instance._isInMatch)
            {
                DeskillzLogger.Warning("Match already in progress");
                return;
            }

            Instance._isInMatch = true;
            Instance._currentScore = 0;
            Instance._matchStartTime = DateTime.UtcNow;

            DeskillzLogger.LogMatch("Started", Instance._currentMatch.MatchId);

            // Notify core SDK
            Deskillz.StartMatch();

            OnMatchStart?.Invoke();
        }

        /// <summary>
        /// Update the player's score during gameplay.
        /// Call this whenever the score changes.
        /// </summary>
        public static void UpdateScore(int score)
        {
            if (!Instance._isInMatch)
            {
                DeskillzLogger.Warning("Cannot update score - not in match");
                return;
            }

            Instance._currentScore = score;
            
            // Update core SDK
            Deskillz.UpdateScore(score);

            DeskillzLogger.LogScore("Updated", score);
        }

        /// <summary>
        /// Add to the current score.
        /// </summary>
        public static void AddScore(int points)
        {
            UpdateScore(Instance._currentScore + points);
        }

        /// <summary>
        /// Submit final score and end the match.
        /// Call this when gameplay is complete.
        /// </summary>
        public static void SubmitScore(int finalScore)
        {
            if (!Instance._isInMatch)
            {
                DeskillzLogger.Error("Cannot submit score - not in match");
                OnError?.Invoke("Not in an active match");
                return;
            }

            Instance._currentScore = finalScore;

            DeskillzLogger.LogScore("Submitting final", finalScore);

            // Submit to server
            if (IsTestMode)
            {
                Instance.StartCoroutine(Instance.SimulateScoreSubmission(finalScore));
            }
            else
            {
                Deskillz.SubmitScore(finalScore);
                Instance.StartCoroutine(Instance.WaitForMatchResult());
            }
        }

        /// <summary>
        /// Forfeit the current match.
        /// </summary>
        public static void ForfeitMatch()
        {
            if (!Instance._isInMatch)
            {
                DeskillzLogger.Warning("Cannot forfeit - not in match");
                return;
            }

            DeskillzLogger.LogMatch("Forfeiting", Instance._currentMatch?.MatchId);

            Deskillz.ForfeitMatch();

            var result = new MatchResult
            {
                MatchId = Instance._currentMatch?.MatchId,
                FinalScore = Instance._currentScore,
                Outcome = MatchOutcome.Forfeit,
                PrizeWon = 0
            };

            Instance.EndMatch(result);
        }

        /// <summary>
        /// Return to the Deskillz lobby/website.
        /// Call this after showing results.
        /// </summary>
        public static void ReturnToLobby()
        {
            DeskillzLogger.Info("Returning to lobby");

            // Open the Deskillz website/app
            string lobbyUrl = IsTestMode 
                ? "https://staging.deskillz.games" 
                : "https://deskillz.games";

            Application.OpenURL(lobbyUrl);
        }

        // =============================================================================
        // TEST MODE
        // =============================================================================

        /// <summary>
        /// Start a test match for development.
        /// Only works when TestMode is enabled.
        /// </summary>
        public static void StartTestMatch(
            MatchMode mode = MatchMode.Asynchronous,
            int timeLimitSeconds = 0,
            decimal entryFee = 0,
            Currency currency = Currency.Free)
        {
            if (!IsTestMode)
            {
                DeskillzLogger.Error("Test matches only available in test mode");
                return;
            }

            Instance._currentMatch = new MatchInfo
            {
                MatchId = $"test_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                TournamentId = "test_tournament",
                Token = "test_token",
                Mode = mode,
                EntryFee = entryFee,
                PrizePool = entryFee * 1.9m,
                Currency = currency,
                TimeLimitSeconds = timeLimitSeconds,
                MaxPlayers = 2,
                IsRealtime = mode == MatchMode.Synchronous,
                IsTestMatch = true
            };

            DeskillzLogger.LogMatch("Test match created", Instance._currentMatch.MatchId);
            OnMatchReceived?.Invoke(Instance._currentMatch);
        }

        /// <summary>
        /// Simulate receiving a deep link (for testing).
        /// </summary>
        public static void SimulateDeepLink(string deepLinkUrl)
        {
            if (!IsTestMode)
            {
                DeskillzLogger.Error("Deep link simulation only available in test mode");
                return;
            }

            DeepLinkHandler.Instance.ProcessDeepLink(deepLinkUrl);
        }

        // =============================================================================
        // PRIVATE HELPERS
        // =============================================================================

        private void EndMatch(MatchResult result)
        {
            _isInMatch = false;
            
            DeskillzLogger.LogMatch("Complete", _currentMatch?.MatchId, 
                $"Score: {result.FinalScore}, Outcome: {result.Outcome}");

            OnMatchComplete?.Invoke(result);

            // Clear match data after a delay to allow UI to show results
            StartCoroutine(ClearMatchAfterDelay(5f));
        }

        private IEnumerator ClearMatchAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _currentMatch = null;
            DeepLinkHandler.Instance.ClearPendingMatch();
        }

        private IEnumerator SimulateScoreSubmission(int score)
        {
            yield return new WaitForSeconds(0.5f);

            // Simulate opponent score
            int opponentScore = UnityEngine.Random.Range(0, score + 100);

            MatchOutcome outcome;
            if (_higherScoreWins)
            {
                outcome = score > opponentScore ? MatchOutcome.Win :
                         score < opponentScore ? MatchOutcome.Loss : MatchOutcome.Tie;
            }
            else
            {
                outcome = score < opponentScore ? MatchOutcome.Win :
                         score > opponentScore ? MatchOutcome.Loss : MatchOutcome.Tie;
            }

            var result = new MatchResult
            {
                MatchId = _currentMatch?.MatchId,
                FinalScore = score,
                OpponentScore = opponentScore,
                Outcome = outcome,
                PrizeWon = outcome == MatchOutcome.Win ? (_currentMatch?.PrizePool ?? 0) * 0.95m : 0,
                Currency = _currentMatch?.Currency ?? Currency.Free,
                DurationSeconds = (float)(DateTime.UtcNow - _matchStartTime).TotalSeconds,
                XpEarned = outcome == MatchOutcome.Win ? 100 : 25
            };

            EndMatch(result);
        }

        private IEnumerator WaitForMatchResult()
        {
            // Wait for SDK to process
            yield return new WaitForSeconds(2f);

            // Listen for match complete event from core SDK
            // The actual result will come through DeskillzEvents
        }
    }

    // =============================================================================
    // MATCH INFO
    // =============================================================================

    /// <summary>
    /// Information about the current match.
    /// </summary>
    [Serializable]
    public class MatchInfo
    {
        public string MatchId;
        public string TournamentId;
        public string Token;
        public MatchMode Mode;
        public decimal EntryFee;
        public decimal PrizePool;
        public Currency Currency;
        public int TimeLimitSeconds;
        public int MaxPlayers;
        public bool IsRealtime;
        public bool IsTestMatch;
        public System.Collections.Generic.Dictionary<string, string> CustomParams;

        /// <summary>
        /// Get a custom parameter value.
        /// </summary>
        public string GetCustomParam(string key, string defaultValue = null)
        {
            if (CustomParams != null && CustomParams.TryGetValue(key, out string value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a custom parameter as int.
        /// </summary>
        public int GetCustomParamInt(string key, int defaultValue = 0)
        {
            string value = GetCustomParam(key);
            return int.TryParse(value, out int result) ? result : defaultValue;
        }
    }
}
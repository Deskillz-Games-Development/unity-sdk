// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Core SDK manager. Add to your first scene or let it auto-create.
    /// Handles SDK lifecycle, networking, and Unity integration.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DeskillzManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static DeskillzManager _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting;

        /// <summary>
        /// Singleton instance. Auto-creates if not present.
        /// </summary>
        public static DeskillzManager Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    DeskillzLogger.Warning("DeskillzManager already destroyed on application quit");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<DeskillzManager>();

                        if (_instance == null)
                        {
                            var go = new GameObject("DeskillzManager");
                            _instance = go.AddComponent<DeskillzManager>();
                            DontDestroyOnLoad(go);
                            DeskillzLogger.Debug("DeskillzManager auto-created");
                        }
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Check if instance exists without creating one.
        /// </summary>
        public static bool HasInstance => _instance != null;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        
        [Tooltip("SDK configuration asset")]
        [SerializeField] private DeskillzConfig _config;

        [Tooltip("Override config values at runtime")]
        [SerializeField] private bool _useRuntimeConfig;

        // =============================================================================
        // STATE
        // =============================================================================

        private SDKState _state = SDKState.Uninitialized;
        private DeskillzNetwork _network;
        private PlayerData _currentPlayer;
        private MatchData _currentMatch;
        private bool _isInitializing;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>
        /// Current SDK state.
        /// </summary>
        public SDKState State => _state;

        /// <summary>
        /// Whether SDK is ready to use.
        /// </summary>
        public bool IsReady => _state == SDKState.Ready || _state == SDKState.TestMode;

        /// <summary>
        /// Whether SDK is in test mode.
        /// </summary>
        public bool IsTestMode => _state == SDKState.TestMode;

        /// <summary>
        /// Current player data.
        /// </summary>
        public PlayerData CurrentPlayer => _currentPlayer;

        /// <summary>
        /// Current match data.
        /// </summary>
        public MatchData CurrentMatch => _currentMatch;

        /// <summary>
        /// Active configuration.
        /// </summary>
        public DeskillzConfig Config => _config ?? DeskillzConfig.Instance;

        /// <summary>
        /// Network manager for API calls.
        /// </summary>
        internal DeskillzNetwork Network => _network;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                DeskillzLogger.Debug("Duplicate DeskillzManager destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load config
            if (_config == null)
            {
                _config = DeskillzConfig.Instance;
            }

            // Configure logging
            DeskillzLogger.Level = _config.LogLevel;
            DeskillzLogger.Info($"Deskillz SDK v{DeskillzConfig.SDK_VERSION} initializing...");

            // Initialize cache
            DeskillzCache.Initialize();

            // Check for auto-initialize
            if (_config.AutoInitialize)
            {
                StartCoroutine(InitializeAsync());
            }
        }

        private void Start()
        {
            // Check for deep link on start
            CheckDeepLink();
        }

        private void OnEnable()
        {
            // Subscribe to Unity events
            Application.deepLinkActivated += OnDeepLinkActivated;
            Application.focusChanged += OnApplicationFocus;
        }

        private void OnDisable()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
            Application.focusChanged -= OnApplicationFocus;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                DeskillzLogger.Debug("Application paused");
                // Save any pending data
                if (_currentMatch != null && _currentMatch.Status == MatchStatus.InProgress)
                {
                    DeskillzCache.CacheMatch(_currentMatch);
                }
            }
            else
            {
                DeskillzLogger.Debug("Application resumed");
                // Attempt to reconnect if needed
                if (_network != null && !_network.IsConnected && _currentMatch?.IsRealtime == true)
                {
                    _network.Connect(_currentMatch.MatchId);
                }
            }
        }

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
            Shutdown();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Shutdown();
                _instance = null;
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize the SDK.
        /// </summary>
        public void Initialize()
        {
            if (IsReady || _isInitializing)
            {
                DeskillzLogger.Warning("SDK already initialized or initializing");
                return;
            }

            StartCoroutine(InitializeAsync());
        }

        /// <summary>
        /// Initialize with custom config.
        /// </summary>
        public void Initialize(DeskillzConfig config)
        {
            _config = config;
            Initialize();
        }

        private IEnumerator InitializeAsync()
        {
            if (_isInitializing) yield break;
            _isInitializing = true;

            _state = SDKState.Initializing;
            DeskillzEvents.RaiseInitializing();

            // Validate configuration
            if (!_config.Validate(out var configError))
            {
                DeskillzLogger.Error($"Invalid configuration: {configError}");
                _state = SDKState.Failed;
                _isInitializing = false;
                DeskillzEvents.RaiseInitializationFailed(
                    new DeskillzError(ErrorCode.InvalidApiKey, configError, false));
                yield break;
            }

            // Create network manager
            _network = new DeskillzNetwork(
                _config.BaseUrl,
                _config.WebSocketUrl,
                _config.ApiKey
            );

            // Check if running in editor or test mode
            if (Application.isEditor || _config.TestMode)
            {
                yield return InitializeTestMode();
            }
            else
            {
                yield return InitializeProduction();
            }

            _isInitializing = false;
        }

        private IEnumerator InitializeTestMode()
        {
            DeskillzLogger.Info("Initializing in TEST MODE");

            // Create test player
            _currentPlayer = new PlayerData
            {
                Id = "test_player_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Username = "TestPlayer",
                AvatarUrl = "",
                WalletAddress = "0x0000000000000000000000000000000000000000",
                Role = PlayerRole.Player,
                Level = 1,
                TotalGamesPlayed = 0,
                TotalWins = 0,
                IsLocalPlayer = true
            };

            DeskillzCache.CachePlayer(_currentPlayer);

            _state = SDKState.TestMode;
            DeskillzEvents.RaiseTestModeChanged(true);
            DeskillzEvents.RaisePlayerAuthenticated(_currentPlayer);
            DeskillzEvents.RaiseReady();

            DeskillzLogger.Info("SDK ready (Test Mode)");
            yield return null;
        }

        private IEnumerator InitializeProduction()
        {
            DeskillzLogger.Info("Initializing in PRODUCTION mode");

            // Validate API key with server
            var validationComplete = false;
            var validationSuccess = false;
            DeskillzError validationError = null;

            yield return _network.Post<PlayerData>(
                "/sdk/init",
                new { gameId = _config.GameId, platform = Application.platform.ToString() },
                (player) =>
                {
                    _currentPlayer = player;
                    _currentPlayer.IsLocalPlayer = true;
                    validationSuccess = true;
                    validationComplete = true;
                },
                (error) =>
                {
                    validationError = error;
                    validationComplete = true;
                }
            );

            // Wait for validation
            while (!validationComplete)
            {
                yield return null;
            }

            if (!validationSuccess)
            {
                DeskillzLogger.Error($"SDK initialization failed: {validationError?.Message}");
                _state = SDKState.Failed;
                DeskillzEvents.RaiseInitializationFailed(validationError);
                yield break;
            }

            // Cache player data
            DeskillzCache.CachePlayer(_currentPlayer);

            _state = SDKState.Ready;
            DeskillzEvents.RaisePlayerAuthenticated(_currentPlayer);
            DeskillzEvents.RaiseReady();

            DeskillzLogger.Info("SDK ready (Production)");
        }

        // =============================================================================
        // DEEP LINK HANDLING
        // =============================================================================

        private void CheckDeepLink()
        {
            // Check for deep link on cold start
            var absoluteUrl = Application.absoluteURL;
            if (!string.IsNullOrEmpty(absoluteUrl))
            {
                ProcessDeepLink(absoluteUrl);
            }
        }

        private void OnDeepLinkActivated(string url)
        {
            DeskillzLogger.Info($"Deep link received: {url}");
            ProcessDeepLink(url);
        }

        private void ProcessDeepLink(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var deepLinkParams = DeepLinkParams.Parse(url);

            // Validate player token
            if (!string.IsNullOrEmpty(deepLinkParams.PlayerToken))
            {
                _network?.SetAuthToken(deepLinkParams.PlayerToken);
            }

            // Create match data from deep link
            if (!string.IsNullOrEmpty(deepLinkParams.MatchId))
            {
                _currentMatch = new MatchData
                {
                    MatchId = deepLinkParams.MatchId,
                    TournamentId = deepLinkParams.TournamentId,
                    GameId = _config.GameId,
                    Mode = deepLinkParams.Mode,
                    Status = MatchStatus.Pending,
                    EntryFee = deepLinkParams.EntryFee,
                    Currency = deepLinkParams.Currency,
                    TimeLimitSeconds = deepLinkParams.TimeLimitSeconds,
                    Rounds = deepLinkParams.Rounds > 0 ? deepLinkParams.Rounds : 1,
                    CurrentRound = 1,
                    ScoreType = deepLinkParams.ScoreType,
                    CustomParams = deepLinkParams.CustomParams
                };

                DeskillzCache.CacheMatch(_currentMatch);
                DeskillzEvents.RaiseMatchReady(_currentMatch);

                DeskillzLogger.LogMatch("Received", _currentMatch.MatchId, $"Mode: {_currentMatch.Mode}");

                // Connect WebSocket for real-time matches
                if (_currentMatch.IsRealtime)
                {
                    _network?.Connect(_currentMatch.MatchId);
                }
            }
        }

        // =============================================================================
        // MATCH MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Start a test match (Test Mode only).
        /// </summary>
        public void StartTestMatch(MatchMode mode = MatchMode.Asynchronous, int timeLimitSeconds = 0)
        {
            if (!IsTestMode)
            {
                DeskillzLogger.Error("StartTestMatch only available in Test Mode");
                return;
            }

            _currentMatch = new MatchData
            {
                MatchId = "test_match_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                GameId = _config.GameId,
                Mode = mode,
                Status = MatchStatus.Pending,
                EntryFee = 0,
                Currency = Currency.Free,
                TimeLimitSeconds = timeLimitSeconds,
                Rounds = 1,
                CurrentRound = 1,
                ScoreType = _config.ScoreType,
                StartTime = DateTime.UtcNow
            };

            // Add test opponent for sync/stage modes
            if (mode != MatchMode.Asynchronous)
            {
                _currentMatch.Players.Add(new MatchPlayer
                {
                    PlayerId = _currentPlayer.Id,
                    Username = _currentPlayer.Username,
                    IsLocalPlayer = true,
                    IsConnected = true
                });

                _currentMatch.Players.Add(new MatchPlayer
                {
                    PlayerId = "test_opponent_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                    Username = "TestOpponent",
                    IsLocalPlayer = false,
                    IsConnected = true
                });
            }

            DeskillzCache.CacheMatch(_currentMatch);
            DeskillzEvents.RaiseMatchReady(_currentMatch);

            DeskillzLogger.LogMatch("Test Match Started", _currentMatch.MatchId, $"Mode: {mode}");
        }

        /// <summary>
        /// Called when match gameplay should begin.
        /// </summary>
        internal void BeginMatch()
        {
            if (_currentMatch == null)
            {
                DeskillzLogger.Error("No match to begin");
                return;
            }

            _currentMatch.Status = MatchStatus.InProgress;
            _currentMatch.StartTime = DateTime.UtcNow;
            
            DeskillzEvents.RaiseMatchStart(_currentMatch);
        }

        /// <summary>
        /// Called when match ends.
        /// </summary>
        internal void EndCurrentMatch(MatchResult result)
        {
            if (_currentMatch == null) return;

            _currentMatch.Status = MatchStatus.Completed;
            DeskillzEvents.RaiseMatchComplete(result);

            // Disconnect from real-time if connected
            if (_currentMatch.IsRealtime)
            {
                _network?.Disconnect();
            }

            // Clear match data
            DeskillzCache.ClearMatch();
            _currentMatch = null;
        }

        /// <summary>
        /// Update current match data.
        /// </summary>
        internal void UpdateMatch(MatchData match)
        {
            _currentMatch = match;
            DeskillzCache.CacheMatch(match);
        }

        // =============================================================================
        // PLAYER MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Update player data.
        /// </summary>
        internal void UpdatePlayer(PlayerData player)
        {
            _currentPlayer = player;
            DeskillzCache.CachePlayer(player);
            DeskillzEvents.RaisePlayerUpdated(player);
        }

        // =============================================================================
        // APPLICATION EVENTS
        // =============================================================================

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                DeskillzLogger.Verbose("Application focused");
            }
            else
            {
                DeskillzLogger.Verbose("Application unfocused");
            }
        }

        // =============================================================================
        // COROUTINE HELPERS
        // =============================================================================

        /// <summary>
        /// Schedule a reconnection attempt.
        /// </summary>
        internal void ScheduleReconnect(float delay)
        {
            StartCoroutine(ReconnectAfterDelay(delay));
        }

        private IEnumerator ReconnectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _network?.ExecuteReconnect();
        }

        /// <summary>
        /// Run a coroutine on the manager.
        /// </summary>
        internal Coroutine Run(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        // =============================================================================
        // SHUTDOWN
        // =============================================================================

        private void Shutdown()
        {
            DeskillzLogger.Info("SDK shutting down...");

            // Disconnect network
            _network?.Dispose();
            _network = null;

            // Save any pending data
            if (_currentMatch != null)
            {
                DeskillzCache.CacheMatch(_currentMatch);
            }

            // Clear events
            DeskillzEvents.ClearAllSubscriptions();

            // Clear session data
            DeskillzCache.ClearSessionData();

            _state = SDKState.Uninitialized;
            _currentPlayer = null;
            _currentMatch = null;

            DeskillzLogger.Info("SDK shutdown complete");
        }

        /// <summary>
        /// Force shutdown and cleanup.
        /// </summary>
        public void ForceShutdown()
        {
            Shutdown();
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }
}

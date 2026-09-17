// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Deskillz SDK configuration. Create via Assets > Create > Deskillz > Config.
    /// </summary>
    [CreateAssetMenu(fileName = "DeskillzConfig", menuName = "Deskillz/Config", order = 1)]
    public class DeskillzConfig : ScriptableObject
    {
        // =============================================================================
        // VERSION INFO
        // =============================================================================

        /// <summary>
        /// Current SDK version.
        /// </summary>
        public const string SDK_VERSION = "3.7.1"; // [P1-3.7.0] [A5-3.7.1]

        /// <summary>
        /// Minimum supported Unity version.
        /// </summary>
        public const string MIN_UNITY_VERSION = "6000.0";

        // =============================================================================
        // API CONFIGURATION
        // =============================================================================

        [Header("API Configuration")]
        
        [Tooltip("Your Deskillz API key. Get this from deskillz.games/developer")]
        [SerializeField] private string _apiKey = "";

        [Tooltip("Your Game ID as registered on Deskillz")]
        [SerializeField] private string _gameId = "";

        [Tooltip("Environment to connect to")]
        [SerializeField] private Environment _environment = Environment.Production;

        [Tooltip("Optional. Override the API origin (e.g. https://my-backend.example.com). Leave empty to use the Deskillz platform host.")]
        [SerializeField] private string _apiHostOverride = "";

        [Tooltip("Currency code used when a room or match does not specify one (see /api/v1/wallet/currencies)")]
        [SerializeField] private string _defaultCurrency = "USDT";

        /// <summary>
        /// Your Deskillz API key.
        /// </summary>
        public string ApiKey => _apiKey;

        /// <summary>
        /// Your registered Game ID.
        /// </summary>
        public string GameId => _gameId;

        /// <summary>
        /// Current environment.
        /// </summary>
        public Environment Environment => _environment;

        /// <summary>Default currency code for rooms/matches.</summary>
        public string DefaultCurrency => string.IsNullOrWhiteSpace(_defaultCurrency) ? "USDT" : _defaultCurrency.Trim();

        // =============================================================================
        // GAME CONFIGURATION
        // =============================================================================

        [Header("Game Configuration")]
        
        [Tooltip("Score comparison type for this game")]
        [SerializeField] private ScoreType _scoreType = ScoreType.HigherIsBetter;

        [Tooltip("Supported match modes")]
        [SerializeField] private MatchModeFlags _supportedModes = MatchModeFlags.Asynchronous;

        [Tooltip("Maximum players in sync/stage matches (2-10)")]
        [Range(2, 10)]
        [SerializeField] private int _maxPlayersPerMatch = 2;

        /// <summary>
        /// Score comparison type.
        /// </summary>
        public ScoreType ScoreType => _scoreType;

        /// <summary>
        /// Supported match modes.
        /// </summary>
        public MatchModeFlags SupportedModes => _supportedModes;

        /// <summary>
        /// Maximum players per match.
        /// </summary>
        public int MaxPlayersPerMatch => _maxPlayersPerMatch;

        // =============================================================================
        // SDK BEHAVIOR
        // =============================================================================

        [Header("SDK Behavior")]
        
        [Tooltip("Enable test mode for development (no real currency)")]
        [SerializeField] private bool _testMode = true;

        [Tooltip("Automatically initialize SDK on scene load")]
        [SerializeField] private bool _autoInitialize = true;

        [Tooltip("Show debug logging in console")]
        [SerializeField] private LogLevel _logLevel = LogLevel.Info;

        [Tooltip("Enable anti-cheat protection")]
        [SerializeField] private bool _enableAntiCheat = true;

        /// <summary>
        /// Whether test mode is enabled.
        /// </summary>
        public bool TestMode => _testMode;

        /// <summary>
        /// Whether to auto-initialize.
        /// </summary>
        public bool AutoInitialize => _autoInitialize;

        /// <summary>
        /// Log level for debug output.
        /// </summary>
        public LogLevel LogLevel => _logLevel;

        /// <summary>
        /// Whether anti-cheat is enabled.
        /// </summary>
        public bool EnableAntiCheat => _enableAntiCheat;

 // =============================================================================
        // SCENE CONFIGURATION (Self-Sufficient Architecture)
        // =============================================================================

        [Header("Scene Configuration")]
        
        [Tooltip("Authentication scene name")]
        [SerializeField] private string _authSceneName = "DeskillzAuth";

        [Tooltip("Main lobby/menu scene name")]
        [SerializeField] private string _lobbySceneName = "DeskillzLobby";

        [Tooltip("Gameplay scene name")]
        [SerializeField] private string _gameSceneName = "Game";

        [Tooltip("Loading/splash scene name")]
        [SerializeField] private string _loadingSceneName = "Loading";

        /// <summary>
        /// Authentication scene name.
        /// </summary>
        public string AuthSceneName => _authSceneName;

        /// <summary>
        /// Main lobby scene name.
        /// </summary>
        public string LobbySceneName => _lobbySceneName;

        /// <summary>
        /// Gameplay scene name.
        /// </summary>
        public string GameSceneName => _gameSceneName;

        /// <summary>
        /// Loading scene name.
        /// </summary>
        public string LoadingSceneName => _loadingSceneName;

        // =============================================================================
        // AUTH CONFIGURATION (Self-Sufficient Architecture)
        // =============================================================================

        [Header("Authentication")]
        
        [Tooltip("Require authentication before accessing lobby")]
        [SerializeField] private bool _requireAuth = true;

        [Tooltip("Allow guest/anonymous play (practice mode only)")]
        [SerializeField] private bool _allowGuestMode = true;

        [Tooltip("Auto-login if session exists")]
        [SerializeField] private bool _autoLogin = true;

        [Tooltip("Remember me enabled by default")]
        [SerializeField] private bool _rememberMeDefault = true;

        /// <summary>
        /// Whether authentication is required.
        /// </summary>
        public bool RequireAuth => _requireAuth;

        /// <summary>
        /// Whether guest mode is allowed.
        /// </summary>
        public bool AllowGuestMode => _allowGuestMode;

        /// <summary>
        /// Whether to auto-login on startup.
        /// </summary>
        public bool AutoLogin => _autoLogin;

        /// <summary>
        /// Default value for remember me checkbox.
        /// </summary>
        public bool RememberMeDefault => _rememberMeDefault;


        // =============================================================================
        // UI CONFIGURATION
        // =============================================================================

        [Header("UI Configuration")]
        
        [Tooltip("Use built-in Deskillz UI components")]
        [SerializeField] private bool _useBuiltInUI = true;

        [Tooltip("Theme for built-in UI")]
        [SerializeField] private ScriptableObject _theme; // [P1-3.7.0 fixC] typed loosely: the built-in UI lives outside the core assembly

        /// <summary>
        /// Whether to use built-in UI.
        /// </summary>
        public bool UseBuiltInUI => _useBuiltInUI;

        /// <summary>
        /// UI theme configuration.
        /// </summary>
        public ScriptableObject Theme => _theme;

        // =============================================================================
        // NETWORK CONFIGURATION
        // =============================================================================

        [Header("Network Configuration")]
        
        [Tooltip("Request timeout in seconds")]
        [Range(10, 120)]
        [SerializeField] private int _requestTimeout = 30;

        [Tooltip("Enable automatic reconnection")]
        [SerializeField] private bool _autoReconnect = true;

        [Tooltip("Maximum reconnection attempts")]
        [Range(1, 10)]
        [SerializeField] private int _maxReconnectAttempts = 5;

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int RequestTimeout => _requestTimeout;

        /// <summary>
        /// Whether auto-reconnect is enabled.
        /// </summary>
        public bool AutoReconnect => _autoReconnect;

        /// <summary>
        /// Maximum reconnection attempts.
        /// </summary>
        public int MaxReconnectAttempts => _maxReconnectAttempts;

        // =============================================================================
        // URL ENDPOINTS  [P1-3.7.0]
        // One origin serves REST (/api/v1) and Socket.IO (/socket.io). Production
        // and Sandbox both point at the platform API the six hosted games use;
        // Sandbox is kept for inspector compatibility (there is no separate host).
        // =============================================================================

        public const string PLATFORM_API_HOST = "https://api.deskillz.games"; // [A5-3.7.1]
        public const string DEVELOPMENT_API_HOST = "http://localhost:3001";

        /// <summary>API origin without any path (scheme + host).</summary>
        public string ApiHost
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_apiHostOverride)) return _apiHostOverride.Trim().TrimEnd('/');
                return _environment == Environment.Development ? DEVELOPMENT_API_HOST : PLATFORM_API_HOST;
            }
        }

        /// <summary>Base API URL including the /api/v1 prefix. Used by DeskillzNetwork.</summary>
        public string BaseUrl => ApiHost + "/api/v1";

        /// <summary>Root API URL without /api/v1 (ApiClient classes include the prefix in their constants).</summary>
        public string ApiBaseUrl => ApiHost;

        /// <summary>Auth API URL.</summary>
        public string AuthUrl => BaseUrl + "/auth";

        /// <summary>Socket.IO origin (http/https; the client upgrades to ws/wss itself).</summary>
        public string WebSocketUrl => ApiHost;

        // =============================================================================
        // DEEP LINK CONFIGURATION
        // =============================================================================

        /// <summary>
        /// Deep link URL scheme for this game.
        /// </summary>
        public string DeepLinkScheme => $"deskillz-{_gameId}";

        // =============================================================================
        // VALIDATION
        // =============================================================================

        /// <summary>
        /// Validate configuration.
        /// </summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                error = "API Key is required. Get yours at deskillz.games/developer";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_gameId))
            {
                error = "Game ID is required. Register your game at deskillz.games/developer";
                return false;
            }

            if (_apiKey.Length < 20)
            {
                error = "API Key appears to be invalid (too short)";
                return false;
            }

            if (_supportedModes == 0)
            {
                error = "At least one match mode must be selected";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Check if a specific match mode is supported.
        /// </summary>
        public bool SupportsMode(MatchMode mode)
        {
            return mode switch
            {
                MatchMode.Asynchronous => (_supportedModes & MatchModeFlags.Asynchronous) != 0,
                MatchMode.Synchronous => (_supportedModes & MatchModeFlags.Synchronous) != 0,
                MatchMode.CustomStage => (_supportedModes & MatchModeFlags.CustomStage) != 0,
                _ => false
            };
        }

        // =============================================================================
        // SINGLETON ACCESS
        // =============================================================================

        private static DeskillzConfig _instance;

        /// <summary>
        /// Get the active configuration instance.
        /// </summary>
        public static DeskillzConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DeskillzConfig>("DeskillzConfig");
                    
                    if (_instance == null)
                    {
                        DeskillzLogger.Warning("DeskillzConfig not found in Resources. Using default configuration.");
                        _instance = CreateInstance<DeskillzConfig>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Set the active configuration (for runtime override).
        /// </summary>
        public static void SetInstance(DeskillzConfig config)
        {
            _instance = config;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset singleton for editor testing.
        /// </summary>
        internal static void ResetInstance()
        {
            _instance = null;
        }
#endif
    }

    // =============================================================================
    // ENUMS
    // =============================================================================

    /// <summary>
    /// Server environment.
    /// </summary>
    public enum Environment
    {
        /// <summary>Sandbox/test environment (no real currency)</summary>
        Sandbox,
        
        /// <summary>Production environment (real currency)</summary>
        Production,
        
        /// <summary>Local development server</summary>
        Development
    }

    /// <summary>
    /// Supported match mode flags (can combine multiple).
    /// </summary>
    [Flags]
    public enum MatchModeFlags
    {
        None = 0,
        Asynchronous = 1 << 0,
        Synchronous = 1 << 1,
        CustomStage = 1 << 2,
        All = Asynchronous | Synchronous | CustomStage
    }

    // NOTE: DeskillzTheme class is defined in DeskillzTheme.cs
}
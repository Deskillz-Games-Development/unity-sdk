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
        public const string SDK_VERSION = "1.0.0";

        /// <summary>
        /// Minimum supported Unity version.
        /// </summary>
        public const string MIN_UNITY_VERSION = "2020.3";

        // =============================================================================
        // API CONFIGURATION
        // =============================================================================

        [Header("API Configuration")]
        
        [Tooltip("Your Deskillz API key. Get this from deskillz.games/developer")]
        [SerializeField] private string _apiKey = "";

        [Tooltip("Your Game ID as registered on Deskillz")]
        [SerializeField] private string _gameId = "";

        [Tooltip("Environment to connect to")]
        [SerializeField] private Environment _environment = Environment.Sandbox;

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
        // UI CONFIGURATION
        // =============================================================================

        [Header("UI Configuration")]
        
        [Tooltip("Use built-in Deskillz UI components")]
        [SerializeField] private bool _useBuiltInUI = true;

        [Tooltip("Theme for built-in UI")]
        [SerializeField] private DeskillzTheme _theme;

        /// <summary>
        /// Whether to use built-in UI.
        /// </summary>
        public bool UseBuiltInUI => _useBuiltInUI;

        /// <summary>
        /// UI theme configuration.
        /// </summary>
        public DeskillzTheme Theme => _theme;

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
        // URL ENDPOINTS
        // =============================================================================

        /// <summary>
        /// Base API URL for current environment.
        /// </summary>
        public string BaseUrl
        {
            get
            {
                return _environment switch
                {
                    Environment.Production => "https://api.deskillz.games/api/v1",
                    Environment.Sandbox => "https://sandbox-api.deskillz.games/api/v1",
                    Environment.Development => "http://localhost:3001/api/v1",
                    _ => "https://sandbox-api.deskillz.games/api/v1"
                };
            }
        }

        /// <summary>
        /// WebSocket URL for current environment.
        /// </summary>
        public string WebSocketUrl
        {
            get
            {
                return _environment switch
                {
                    Environment.Production => "wss://ws.deskillz.games",
                    Environment.Sandbox => "wss://sandbox-ws.deskillz.games",
                    Environment.Development => "ws://localhost:3001",
                    _ => "wss://sandbox-ws.deskillz.games"
                };
            }
        }

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

    // =============================================================================
    // THEME CONFIGURATION
    // =============================================================================

    /// <summary>
    /// UI theme configuration for built-in components.
    /// </summary>
    [Serializable]
    public class DeskillzTheme
    {
        [Header("Colors")]
        
        [Tooltip("Primary color for buttons and highlights")]
        public Color PrimaryColor = new Color(0.39f, 0.4f, 0.95f); // #6366F1

        [Tooltip("Secondary color for backgrounds")]
        public Color SecondaryColor = new Color(0.12f, 0.11f, 0.29f); // #1E1B4B

        [Tooltip("Accent color for scores and important elements")]
        public Color AccentColor = new Color(0.13f, 0.83f, 0.93f); // #22D3EE

        [Tooltip("Success color")]
        public Color SuccessColor = new Color(0.13f, 0.77f, 0.37f); // #22C55E

        [Tooltip("Warning color")]
        public Color WarningColor = new Color(0.98f, 0.72f, 0.13f); // #FBBF24

        [Tooltip("Error color")]
        public Color ErrorColor = new Color(0.94f, 0.27f, 0.27f); // #EF4444

        [Tooltip("Text color")]
        public Color TextColor = Color.white;

        [Tooltip("Muted text color")]
        public Color MutedTextColor = new Color(0.6f, 0.6f, 0.7f);

        [Header("Typography")]
        
        [Tooltip("Primary font for UI text")]
        public Font Font;

        [Tooltip("Font size for body text")]
        [Range(12, 24)]
        public int FontSize = 16;

        [Header("Sounds")]
        
        [Tooltip("Sound for button clicks")]
        public AudioClip ButtonClickSound;

        [Tooltip("Sound for score updates")]
        public AudioClip ScoreSound;

        [Tooltip("Sound for match start")]
        public AudioClip MatchStartSound;

        [Tooltip("Sound for win")]
        public AudioClip WinSound;

        [Tooltip("Sound for loss")]
        public AudioClip LoseSound;

        [Header("Animation")]
        
        [Tooltip("Animation speed multiplier")]
        [Range(0.5f, 2f)]
        public float AnimationSpeed = 1f;

        /// <summary>
        /// Get default theme.
        /// </summary>
        public static DeskillzTheme Default => new DeskillzTheme();
    }
}

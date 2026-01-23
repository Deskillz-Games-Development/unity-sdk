// =============================================================================
// Deskillz SDK for Unity - Auth Scene Controller
// Manages authentication scene flow and navigation
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deskillz
{
    /// <summary>
    /// Controls the authentication flow and scene navigation.
    /// Determines whether to show login, go to lobby, or launch match.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class AuthSceneController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static AuthSceneController _instance;
        public static AuthSceneController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AuthSceneController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DeskillzAuthSceneController");
                        _instance = go.AddComponent<AuthSceneController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Scene Names")]
        [Tooltip("Scene to show for login/signup")]
        [SerializeField] private string _authSceneName = "DeskillzAuth";

        [Tooltip("Main lobby/menu scene")]
        [SerializeField] private string _lobbySceneName = "DeskillzLobby";

        [Tooltip("Gameplay scene")]
        [SerializeField] private string _gameSceneName = "Game";

        [Tooltip("Loading/splash scene")]
        [SerializeField] private string _loadingSceneName = "Loading";

        [Header("Behavior")]
        [Tooltip("Auto-navigate based on auth state on startup")]
        [SerializeField] private bool _autoNavigateOnStart = true;

        [Tooltip("Show loading screen during transitions")]
        [SerializeField] private bool _useLoadingScreen = true;

        [Tooltip("Minimum loading screen duration (seconds)")]
        [SerializeField] private float _minLoadingDuration = 1.0f;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when navigating to a new scene.
        /// </summary>
        public static event Action<string> OnSceneChanging;

        /// <summary>
        /// Fired when scene change is complete.
        /// </summary>
        public static event Action<string> OnSceneChanged;

        /// <summary>
        /// Fired when auth flow is complete and user is authenticated.
        /// </summary>
        public static event Action OnAuthFlowComplete;

        /// <summary>
        /// Fired when user logs out.
        /// </summary>
        public static event Action OnLogoutComplete;

        // =============================================================================
        // STATE
        // =============================================================================

        private bool _isInitialized;
        private bool _isTransitioning;
        private string _pendingDestination;
        private MatchLaunchData _pendingMatchData;

        /// <summary>
        /// Whether currently transitioning between scenes.
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Current scene name.
        /// </summary>
        public string CurrentScene => SceneManager.GetActiveScene().name;

        /// <summary>
        /// Whether user is authenticated.
        /// </summary>
        public bool IsAuthenticated => DeskillzAuth.IsAuthenticated;

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

            LoadSceneNamesFromConfig();
        }

        private void Start()
        {
            Initialize();

            if (_autoNavigateOnStart)
            {
                DetermineInitialNavigation();
            }
        }

        private void OnEnable()
        {
            // Subscribe to auth events
            DeskillzAuth.OnLoginSuccess += HandleLoginSuccess;
            DeskillzAuth.OnLogout += HandleLogout;
            DeskillzAuth.OnAuthError += HandleAuthError;

            // Subscribe to deep link events
            DeepLinkHandler.OnMatchLaunchReceived += HandleMatchLaunchReceived;
            DeepLinkHandler.OnNormalLaunch += HandleNormalLaunch;
            DeepLinkHandler.OnNavigationReceived += HandleNavigationReceived;

            // Subscribe to scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            DeskillzAuth.OnLoginSuccess -= HandleLoginSuccess;
            DeskillzAuth.OnLogout -= HandleLogout;
            DeskillzAuth.OnAuthError -= HandleAuthError;

            DeepLinkHandler.OnMatchLaunchReceived -= HandleMatchLaunchReceived;
            DeepLinkHandler.OnNormalLaunch -= HandleNormalLaunch;
            DeepLinkHandler.OnNavigationReceived -= HandleNavigationReceived;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_isInitialized) return;

            DeskillzLogger.Info("AuthSceneController initializing...");

            // Initialize auth system
            DeskillzAuth.Initialize();

            _isInitialized = true;
            DeskillzLogger.Info("AuthSceneController ready");
        }

        private void LoadSceneNamesFromConfig()
        {
            var config = DeskillzConfig.Instance;
            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.AuthSceneName))
                    _authSceneName = config.AuthSceneName;
                if (!string.IsNullOrEmpty(config.LobbySceneName))
                    _lobbySceneName = config.LobbySceneName;
                if (!string.IsNullOrEmpty(config.GameSceneName))
                    _gameSceneName = config.GameSceneName;
                if (!string.IsNullOrEmpty(config.LoadingSceneName))
                    _loadingSceneName = config.LoadingSceneName;
            }
        }

        // =============================================================================
        // NAVIGATION LOGIC
        // =============================================================================

        /// <summary>
        /// Determine where to navigate based on current state.
        /// </summary>
        private void DetermineInitialNavigation()
        {
            DeskillzLogger.Debug("Determining initial navigation...");

            // Check for pending match from deep link
            if (DeepLinkHandler.Instance.HasPendingMatch)
            {
                var matchData = DeepLinkHandler.Instance.PendingMatch;
                DeskillzLogger.Info($"Pending match found: {matchData.MatchId}");

                if (IsAuthenticated)
                {
                    // Authenticated with pending match - go to game
                    LaunchMatch(matchData);
                }
                else
                {
                    // Need to authenticate first, then launch match
                    _pendingMatchData = matchData;
                    GoToAuth();
                }
                return;
            }

            // No pending match - check auth state
            if (IsAuthenticated)
            {
                DeskillzLogger.Info("User authenticated - going to lobby");
                GoToLobby();
            }
            else
            {
                DeskillzLogger.Info("User not authenticated - going to auth");
                GoToAuth();
            }
        }

        // =============================================================================
        // PUBLIC NAVIGATION API
        // =============================================================================

        /// <summary>
        /// Navigate to the authentication scene.
        /// </summary>
        public void GoToAuth()
        {
            NavigateToScene(_authSceneName);
        }

        /// <summary>
        /// Navigate to the main lobby scene.
        /// </summary>
        public void GoToLobby()
        {
            if (!IsAuthenticated)
            {
                DeskillzLogger.Warning("Cannot go to lobby - not authenticated");
                GoToAuth();
                return;
            }

            NavigateToScene(_lobbySceneName);
        }

        /// <summary>
        /// Navigate to the game scene.
        /// </summary>
        public void GoToGame()
        {
            NavigateToScene(_gameSceneName);
        }

        /// <summary>
        /// Launch a match with the given data.
        /// </summary>
        public void LaunchMatch(MatchLaunchData matchData)
        {
            if (!IsAuthenticated)
            {
                DeskillzLogger.Warning("Cannot launch match - not authenticated");
                _pendingMatchData = matchData;
                GoToAuth();
                return;
            }

            DeskillzLogger.LogMatch("Launching", matchData.MatchId);

            // Store match data for the game scene
            DeskillzBridge.Instance.SetPendingMatch(matchData);

            // Navigate to game
            NavigateToScene(_gameSceneName);
        }

        /// <summary>
        /// Return to lobby after match completes.
        /// </summary>
        public void ReturnToLobby()
        {
            GoToLobby();
        }

        /// <summary>
        /// Logout and return to auth screen.
        /// </summary>
        public void LogoutAndGoToAuth()
        {
            DeskillzAuth.Logout();
            GoToAuth();
        }

        // =============================================================================
        // SCENE NAVIGATION
        // =============================================================================

        private void NavigateToScene(string sceneName)
        {
            if (_isTransitioning)
            {
                DeskillzLogger.Warning($"Already transitioning, queuing: {sceneName}");
                _pendingDestination = sceneName;
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                DeskillzLogger.Error("Cannot navigate to empty scene name");
                return;
            }

            if (CurrentScene == sceneName)
            {
                DeskillzLogger.Debug($"Already in scene: {sceneName}");
                return;
            }

            DeskillzLogger.Info($"Navigating to: {sceneName}");
            OnSceneChanging?.Invoke(sceneName);

            if (_useLoadingScreen && !string.IsNullOrEmpty(_loadingSceneName))
            {
                StartCoroutine(NavigateWithLoadingScreen(sceneName));
            }
            else
            {
                StartCoroutine(NavigateDirect(sceneName));
            }
        }

        private IEnumerator NavigateDirect(string sceneName)
        {
            _isTransitioning = true;

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = true;

            while (!asyncOp.isDone)
            {
                yield return null;
            }

            _isTransitioning = false;
            OnSceneChanged?.Invoke(sceneName);

            ProcessPendingNavigation();
        }

        private IEnumerator NavigateWithLoadingScreen(string targetScene)
        {
            _isTransitioning = true;
            float startTime = Time.time;

            // Load loading screen
            var loadingOp = SceneManager.LoadSceneAsync(_loadingSceneName);
            while (!loadingOp.isDone)
            {
                yield return null;
            }

            // Start loading target scene
            var targetOp = SceneManager.LoadSceneAsync(targetScene);
            targetOp.allowSceneActivation = false;

            // Wait for load to complete
            while (targetOp.progress < 0.9f)
            {
                yield return null;
            }

            // Ensure minimum loading duration
            float elapsed = Time.time - startTime;
            if (elapsed < _minLoadingDuration)
            {
                yield return new WaitForSeconds(_minLoadingDuration - elapsed);
            }

            // Activate target scene
            targetOp.allowSceneActivation = true;

            while (!targetOp.isDone)
            {
                yield return null;
            }

            _isTransitioning = false;
            OnSceneChanged?.Invoke(targetScene);

            ProcessPendingNavigation();
        }

        private void ProcessPendingNavigation()
        {
            if (!string.IsNullOrEmpty(_pendingDestination))
            {
                string dest = _pendingDestination;
                _pendingDestination = null;
                NavigateToScene(dest);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DeskillzLogger.Debug($"Scene loaded: {scene.name}");

            // Raise scene changed event through DeskillzEvents
            DeskillzEvents.RaiseSceneChanged(scene.name);
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleLoginSuccess(AuthUser user)
        {
            DeskillzLogger.Info($"Login success: {user.Username}");

            OnAuthFlowComplete?.Invoke();
            DeskillzEvents.RaiseAuthFlowComplete(user);

            // Check for pending match
            if (_pendingMatchData != null)
            {
                var matchData = _pendingMatchData;
                _pendingMatchData = null;
                LaunchMatch(matchData);
            }
            else
            {
                GoToLobby();
            }
        }

        private void HandleLogout()
        {
            DeskillzLogger.Info("Logout - returning to auth");

            _pendingMatchData = null;
            OnLogoutComplete?.Invoke();

            GoToAuth();
        }

        private void HandleAuthError(string error)
        {
            DeskillzLogger.Error($"Auth error: {error}");
            // Stay on auth scene, UI will show error
        }

        private void HandleMatchLaunchReceived(MatchLaunchData matchData)
        {
            DeskillzLogger.LogMatch("Deep link match received", matchData.MatchId);

            if (IsAuthenticated)
            {
                LaunchMatch(matchData);
            }
            else
            {
                _pendingMatchData = matchData;
                GoToAuth();
            }
        }

        private void HandleNormalLaunch()
        {
            DeskillzLogger.Debug("Normal launch (no deep link)");
            // Navigation already handled by DetermineInitialNavigation
        }

        private void HandleNavigationReceived(NavigationAction action, string targetId)
        {
            DeskillzLogger.Info($"Navigation received: {action}, Target: {targetId}");

            if (!IsAuthenticated)
            {
                GoToAuth();
                return;
            }

            switch (action)
            {
                case NavigationAction.Tournaments:
                case NavigationAction.Wallet:
                case NavigationAction.Profile:
                case NavigationAction.Settings:
                    // All handled in lobby
                    GoToLobby();
                    break;

                case NavigationAction.Game:
                    // Specific game - lobby will handle
                    GoToLobby();
                    break;

                default:
                    GoToLobby();
                    break;
            }
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Check if a scene exists in build settings.
        /// </summary>
        public bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get scene names from config.
        /// </summary>
        public (string auth, string lobby, string game, string loading) GetSceneNames()
        {
            return (_authSceneName, _lobbySceneName, _gameSceneName, _loadingSceneName);
        }

        /// <summary>
        /// Set scene names at runtime.
        /// </summary>
        public void SetSceneNames(string auth = null, string lobby = null, string game = null, string loading = null)
        {
            if (!string.IsNullOrEmpty(auth)) _authSceneName = auth;
            if (!string.IsNullOrEmpty(lobby)) _lobbySceneName = lobby;
            if (!string.IsNullOrEmpty(game)) _gameSceneName = game;
            if (!string.IsNullOrEmpty(loading)) _loadingSceneName = loading;
        }
    }
}
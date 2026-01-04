// =============================================================================
// Deskillz SDK for Unity - Auto-Updater
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
//
// Handles automatic game updates from the Deskillz platform.
// Checks for new APK versions and prompts users to update.
//
// USAGE:
// 1. Add DeskillzUpdater component to a persistent GameObject
// 2. Configure settings in inspector or via DeskillzConfig
// 3. Call CheckForUpdates() manually or enable auto-check
//
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Deskillz
{
    /// <summary>
    /// Manages automatic game updates from the Deskillz platform.
    /// </summary>
    public class DeskillzUpdater : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static DeskillzUpdater _instance;
        
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static DeskillzUpdater Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("DeskillzUpdater");
                    _instance = go.AddComponent<DeskillzUpdater>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Update Settings")]
        
        [Tooltip("Check for updates automatically on app start")]
        [SerializeField] private bool _autoCheckOnStart = true;

        [Tooltip("Delay before auto-check (seconds)")]
        [SerializeField] private float _autoCheckDelay = 2f;

        [Tooltip("Show UI prompts for optional updates")]
        [SerializeField] private bool _showOptionalUpdatePrompt = true;

        [Tooltip("Allow skipping optional updates")]
        [SerializeField] private bool _allowSkipOptionalUpdate = true;

        [Tooltip("Remember skipped version (don't prompt again)")]
        [SerializeField] private bool _rememberSkippedVersion = true;

        [Header("Current Version")]
        
        [Tooltip("Current app version (e.g., 1.0.0)")]
        [SerializeField] private string _currentVersion = "1.0.0";

        [Tooltip("Current version code (integer, must increase with each release)")]
        [SerializeField] private int _currentVersionCode = 1;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when update check starts.
        /// </summary>
        public static event Action OnUpdateCheckStarted;

        /// <summary>
        /// Fired when update check completes (regardless of result).
        /// </summary>
        public static event Action<UpdateCheckResult> OnUpdateCheckCompleted;

        /// <summary>
        /// Fired when an update is available.
        /// </summary>
        public static event Action<UpdateInfo> OnUpdateAvailable;

        /// <summary>
        /// Fired when a forced update is required.
        /// </summary>
        public static event Action<UpdateInfo> OnForcedUpdateRequired;

        /// <summary>
        /// Fired when no update is needed.
        /// </summary>
        public static event Action OnNoUpdateNeeded;

        /// <summary>
        /// Fired when update check fails.
        /// </summary>
        public static event Action<string> OnUpdateCheckFailed;

        /// <summary>
        /// Fired when user accepts an update.
        /// </summary>
        public static event Action<UpdateInfo> OnUpdateAccepted;

        /// <summary>
        /// Fired when user skips an optional update.
        /// </summary>
        public static event Action<UpdateInfo> OnUpdateSkipped;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>
        /// Current app version string.
        /// </summary>
        public string CurrentVersion => _currentVersion;

        /// <summary>
        /// Current version code.
        /// </summary>
        public int CurrentVersionCode => _currentVersionCode;

        /// <summary>
        /// Whether an update check is in progress.
        /// </summary>
        public bool IsCheckingForUpdates { get; private set; }

        /// <summary>
        /// Latest update info (null if not checked or no update).
        /// </summary>
        public UpdateInfo LatestUpdateInfo { get; private set; }

        /// <summary>
        /// Whether an update is available.
        /// </summary>
        public bool IsUpdateAvailable => LatestUpdateInfo != null && LatestUpdateInfo.UpdateAvailable;

        /// <summary>
        /// Whether a forced update is required.
        /// </summary>
        public bool IsForcedUpdateRequired => LatestUpdateInfo != null && LatestUpdateInfo.IsForced;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private const string SKIPPED_VERSION_KEY = "deskillz_skipped_version";
        private const string LAST_CHECK_KEY = "deskillz_last_update_check";
        private DeskillzUpdaterUI _ui;

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

            // Try to get version from config
            if (DeskillzConfig.Instance != null)
            {
                // Use SDK version as fallback
                if (string.IsNullOrEmpty(_currentVersion))
                {
                    _currentVersion = DeskillzConfig.SDK_VERSION;
                }
            }

            DeskillzLogger.Debug($"DeskillzUpdater initialized. Version: {_currentVersion} ({_currentVersionCode})");
        }

        private void Start()
        {
            if (_autoCheckOnStart)
            {
                StartCoroutine(AutoCheckCoroutine());
            }
        }

        private IEnumerator AutoCheckCoroutine()
        {
            yield return new WaitForSeconds(_autoCheckDelay);
            CheckForUpdates();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Check for available updates.
        /// </summary>
        /// <param name="onComplete">Optional callback with result.</param>
        public void CheckForUpdates(Action<UpdateCheckResult> onComplete = null)
        {
            if (IsCheckingForUpdates)
            {
                DeskillzLogger.Warning("Update check already in progress");
                return;
            }

            StartCoroutine(CheckForUpdatesCoroutine(onComplete));
        }

        /// <summary>
        /// Start the update process (opens download URL or app store).
        /// </summary>
        public void StartUpdate()
        {
            if (LatestUpdateInfo == null || !LatestUpdateInfo.UpdateAvailable)
            {
                DeskillzLogger.Warning("No update available to start");
                return;
            }

            DeskillzLogger.Info($"Starting update to version {LatestUpdateInfo.LatestVersion}");
            OnUpdateAccepted?.Invoke(LatestUpdateInfo);

            // Open download URL
            if (!string.IsNullOrEmpty(LatestUpdateInfo.DownloadUrl))
            {
                Application.OpenURL(LatestUpdateInfo.DownloadUrl);
            }
            else
            {
                // Fallback to game page on website
                var gameId = DeskillzConfig.Instance?.GameId ?? "";
                var fallbackUrl = $"https://deskillz.games/games/{gameId}/download";
                Application.OpenURL(fallbackUrl);
            }
        }

        /// <summary>
        /// Skip the current optional update.
        /// </summary>
        public void SkipUpdate()
        {
            if (LatestUpdateInfo == null || LatestUpdateInfo.IsForced)
            {
                DeskillzLogger.Warning("Cannot skip forced update");
                return;
            }

            DeskillzLogger.Info($"User skipped update to version {LatestUpdateInfo.LatestVersion}");

            if (_rememberSkippedVersion)
            {
                PlayerPrefs.SetInt(SKIPPED_VERSION_KEY, LatestUpdateInfo.VersionCode);
                PlayerPrefs.Save();
            }

            OnUpdateSkipped?.Invoke(LatestUpdateInfo);
            HideUpdateUI();
        }

        /// <summary>
        /// Show the update UI manually.
        /// </summary>
        public void ShowUpdateUI()
        {
            if (LatestUpdateInfo == null || !LatestUpdateInfo.UpdateAvailable)
            {
                DeskillzLogger.Warning("No update available to show");
                return;
            }

            EnsureUIExists();
            _ui.Show(LatestUpdateInfo);
        }

        /// <summary>
        /// Hide the update UI.
        /// </summary>
        public void HideUpdateUI()
        {
            if (_ui != null)
            {
                _ui.Hide();
            }
        }

        /// <summary>
        /// Clear the skipped version (will prompt again).
        /// </summary>
        public void ClearSkippedVersion()
        {
            PlayerPrefs.DeleteKey(SKIPPED_VERSION_KEY);
            PlayerPrefs.Save();
        }

        // =============================================================================
        // UPDATE CHECK LOGIC
        // =============================================================================

        private IEnumerator CheckForUpdatesCoroutine(Action<UpdateCheckResult> onComplete)
        {
            IsCheckingForUpdates = true;
            OnUpdateCheckStarted?.Invoke();

            DeskillzLogger.Info("Checking for updates...");

            var result = new UpdateCheckResult();

            // Build request URL
            var config = DeskillzConfig.Instance;
            var baseUrl = config?.BaseUrl ?? "https://api.deskillz.games/api/v1";
            var gameId = config?.GameId ?? "";
            
            var url = $"{baseUrl}/sdk/version-check?gameId={gameId}&currentVersion={_currentVersion}&versionCode={_currentVersionCode}&platform=ANDROID";

            using (var request = UnityWebRequest.Get(url))
            {
                // Set headers
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-SDK-Version", DeskillzConfig.SDK_VERSION);
                request.SetRequestHeader("X-SDK-Platform", "Unity");

                // Add API key if available
                var apiKey = config?.ApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.SetRequestHeader("X-API-Key", apiKey);
                }

                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<VersionCheckResponse>(request.downloadHandler.text);
                        
                        LatestUpdateInfo = new UpdateInfo
                        {
                            LatestVersion = response.latestVersion,
                            VersionCode = response.versionCode,
                            UpdateAvailable = response.updateAvailable,
                            IsForced = response.isForced,
                            DownloadUrl = response.downloadUrl,
                            FileSize = response.fileSize,
                            ReleaseNotes = response.releaseNotes
                        };

                        result.Success = true;
                        result.UpdateInfo = LatestUpdateInfo;

                        DeskillzLogger.Info($"Update check complete. Available: {LatestUpdateInfo.UpdateAvailable}, Forced: {LatestUpdateInfo.IsForced}");

                        // Handle result
                        if (LatestUpdateInfo.UpdateAvailable)
                        {
                            HandleUpdateAvailable();
                        }
                        else
                        {
                            OnNoUpdateNeeded?.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to parse update response";
                        DeskillzLogger.Error($"Failed to parse update response: {ex.Message}");
                        OnUpdateCheckFailed?.Invoke(result.ErrorMessage);
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Network error: {request.error}";
                    DeskillzLogger.Error($"Update check failed: {request.error}");
                    OnUpdateCheckFailed?.Invoke(result.ErrorMessage);
                }
            }

            IsCheckingForUpdates = false;
            
            // Save last check time
            PlayerPrefs.SetString(LAST_CHECK_KEY, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.Save();

            OnUpdateCheckCompleted?.Invoke(result);
            onComplete?.Invoke(result);
        }

        private void HandleUpdateAvailable()
        {
            // Check if this version was skipped
            if (_rememberSkippedVersion && !LatestUpdateInfo.IsForced)
            {
                var skippedVersion = PlayerPrefs.GetInt(SKIPPED_VERSION_KEY, 0);
                if (skippedVersion >= LatestUpdateInfo.VersionCode)
                {
                    DeskillzLogger.Debug($"Version {LatestUpdateInfo.VersionCode} was previously skipped");
                    OnNoUpdateNeeded?.Invoke();
                    return;
                }
            }

            if (LatestUpdateInfo.IsForced)
            {
                DeskillzLogger.Warning($"Forced update required! Version {LatestUpdateInfo.LatestVersion}");
                OnForcedUpdateRequired?.Invoke(LatestUpdateInfo);
                
                // Always show UI for forced updates
                EnsureUIExists();
                _ui.Show(LatestUpdateInfo);
            }
            else
            {
                OnUpdateAvailable?.Invoke(LatestUpdateInfo);

                if (_showOptionalUpdatePrompt)
                {
                    EnsureUIExists();
                    _ui.Show(LatestUpdateInfo);
                }
            }
        }

        private void EnsureUIExists()
        {
            if (_ui == null)
            {
                var uiGo = new GameObject("DeskillzUpdaterUI");
                uiGo.transform.SetParent(transform);
                _ui = uiGo.AddComponent<DeskillzUpdaterUI>();
                _ui.Initialize(this);
            }
        }

        // =============================================================================
        // STATIC HELPERS
        // =============================================================================

        /// <summary>
        /// Compare two semantic version strings.
        /// Returns: -1 if v1 < v2, 0 if equal, 1 if v1 > v2
        /// </summary>
        public static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) return -1;
            if (string.IsNullOrEmpty(v2)) return 1;

            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            int maxLength = Mathf.Max(parts1.Length, parts2.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int num1 = i < parts1.Length && int.TryParse(parts1[i], out int p1) ? p1 : 0;
                int num2 = i < parts2.Length && int.TryParse(parts2[i], out int p2) ? p2 : 0;

                if (num1 < num2) return -1;
                if (num1 > num2) return 1;
            }

            return 0;
        }

        /// <summary>
        /// Format file size for display.
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }
    }

    // =============================================================================
    // DATA MODELS
    // =============================================================================

    /// <summary>
    /// Information about an available update.
    /// </summary>
    [Serializable]
    public class UpdateInfo
    {
        /// <summary>Latest version string (e.g., "1.2.0").</summary>
        public string LatestVersion;

        /// <summary>Latest version code (integer).</summary>
        public int VersionCode;

        /// <summary>Whether an update is available.</summary>
        public bool UpdateAvailable;

        /// <summary>Whether this is a forced/required update.</summary>
        public bool IsForced;

        /// <summary>Direct download URL for the APK.</summary>
        public string DownloadUrl;

        /// <summary>File size in bytes.</summary>
        public long FileSize;

        /// <summary>Release notes/changelog.</summary>
        public string ReleaseNotes;

        /// <summary>File size formatted for display.</summary>
        public string FileSizeFormatted => DeskillzUpdater.FormatFileSize(FileSize);
    }

    /// <summary>
    /// Result of an update check.
    /// </summary>
    [Serializable]
    public class UpdateCheckResult
    {
        /// <summary>Whether the check was successful.</summary>
        public bool Success;

        /// <summary>Error message if check failed.</summary>
        public string ErrorMessage;

        /// <summary>Update info if successful.</summary>
        public UpdateInfo UpdateInfo;
    }

    /// <summary>
    /// API response for version check.
    /// </summary>
    [Serializable]
    internal class VersionCheckResponse
    {
        public string latestVersion;
        public int versionCode;
        public bool updateAvailable;
        public bool isForced;
        public string downloadUrl;
        public long fileSize;
        public string releaseNotes;
    }
}
// =============================================================================
// Deskillz SDK for Unity (Updated for Lobby Architecture)
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz
{
    // =============================================================================
    // NAVIGATION ACTION ENUM
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

    /// <summary>
    /// Central event system for the Deskillz SDK.
    /// Subscribe to these events to respond to SDK state changes.
    /// </summary>
    public static class DeskillzEvents
    {
        // =============================================================================
        // SDK LIFECYCLE EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when SDK initialization begins.
        /// </summary>
        public static event Action OnInitializing;

        /// <summary>
        /// Fired when SDK is fully initialized and ready.
        /// </summary>
        public static event Action OnReady;

        /// <summary>
        /// Fired when SDK initialization fails.
        /// </summary>
        public static event Action<DeskillzError> OnInitializationFailed;

        /// <summary>
        /// Fired when SDK enters or exits test mode.
        /// </summary>
        public static event Action<bool> OnTestModeChanged;

        // =============================================================================
        // DEEP LINK EVENTS (NEW - Lobby Architecture)
        // =============================================================================

        /// <summary>
        /// Fired when a deep link is received from the Deskillz website/lobby.
        /// Provides the raw URL and parsed match launch data.
        /// </summary>
        public static event Action<string, MatchLaunchData> OnDeepLinkReceived;

        /// <summary>
        /// Fired when app is launched normally (not via deep link).
        /// Use this to show main menu or practice mode.
        /// </summary>
        public static event Action OnNormalLaunch;

        /// <summary>
        /// Fired when deep link parsing fails.
        /// </summary>
        public static event Action<string> OnDeepLinkError;

        /// <summary>
        /// Fired when a navigation deep link is received (not a match launch).
        /// Action types: Tournaments, Wallet, Profile, Game, Settings
        /// TargetId is optional (e.g., game ID for Game action).
        /// </summary>
        public static event Action<NavigationAction, string> OnNavigationReceived;

        // =============================================================================
        // PLAYER EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when player data is received/updated.
        /// </summary>
        public static event Action<PlayerData> OnPlayerUpdated;

        /// <summary>
        /// Fired when player authentication is validated.
        /// </summary>
        public static event Action<PlayerData> OnPlayerAuthenticated;

        /// <summary>
        /// Fired when player session expires or is invalidated.
        /// </summary>
        public static event Action OnPlayerSessionExpired;

        // =============================================================================
        // MATCH EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when match data is received and match is ready to start.
        /// </summary>
        public static event Action<MatchData> OnMatchReady;

        /// <summary>
        /// Fired when match countdown begins.
        /// Provides countdown seconds remaining.
        /// </summary>
        public static event Action<int> OnMatchCountdown;

        /// <summary>
        /// Fired for countdown UI (alias for OnMatchCountdown).
        /// </summary>
        public static event Action<int> OnCountdown;

        /// <summary>
        /// Fired when match gameplay should begin.
        /// </summary>
        public static event Action<MatchData> OnMatchStart;

        /// <summary>
        /// Fired when match is paused.
        /// </summary>
        public static event Action OnMatchPaused;

        /// <summary>
        /// Fired when match resumes from pause.
        /// </summary>
        public static event Action OnMatchResumed;

        /// <summary>
        /// Fired when match ends (before results are processed).
        /// </summary>
        public static event Action OnMatchEnding;

        /// <summary>
        /// Fired when match results are available.
        /// </summary>
        public static event Action<MatchResult> OnMatchComplete;

        /// <summary>
        /// Fired when match is cancelled.
        /// </summary>
        public static event Action<DeskillzError> OnMatchCancelled;

        /// <summary>
        /// Fired when player forfeits the match.
        /// </summary>
        public static event Action OnMatchForfeited;

        // =============================================================================
        // SCORE EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when local player's score is updated.
        /// </summary>
        public static event Action<int> OnLocalScoreUpdated;

        /// <summary>
        /// Fired when any player's score changes (real-time matches).
        /// Provides player ID and new score.
        /// </summary>
        public static event Action<string, int> OnPlayerScoreUpdated;

        /// <summary>
        /// Fired when opponent's score is updated (for HUD display).
        /// </summary>
        public static event Action<string, int> OnOpponentScoreUpdated;

        /// <summary>
        /// Fired when score is successfully submitted to server.
        /// </summary>
        public static event Action<int> OnScoreSubmitted;

        /// <summary>
        /// Fired when score submission fails.
        /// </summary>
        public static event Action<DeskillzError> OnScoreSubmissionFailed;

        // =============================================================================
        // MULTIPLAYER EVENTS (Sync Mode)
        // =============================================================================

        /// <summary>
        /// Fired when a player joins the match (real-time).
        /// </summary>
        public static event Action<MatchPlayer> OnPlayerJoined;

        /// <summary>
        /// Fired when a player leaves the match (real-time).
        /// </summary>
        public static event Action<MatchPlayer> OnPlayerLeft;

        /// <summary>
        /// Fired when a player's state is updated (real-time).
        /// </summary>
        public static event Action<PlayerState> OnPlayerStateUpdated;

        /// <summary>
        /// Fired when a network message is received.
        /// </summary>
        public static event Action<NetworkMessage> OnMessageReceived;

        /// <summary>
        /// Fired when all players are ready and synced.
        /// </summary>
        public static event Action OnAllPlayersReady;

        // =============================================================================
        // LOBBY EVENTS (NEW - For Pre-Match Room)
        // =============================================================================

        /// <summary>
        /// Fired when connected to the lobby server.
        /// </summary>
        public static event Action OnLobbyConnected;

        /// <summary>
        /// Fired when disconnected from the lobby server.
        /// </summary>
        public static event Action<string> OnLobbyDisconnected;

        /// <summary>
        /// Fired when a player joins the pre-match room.
        /// </summary>
        public static event Action<PlayerPresence> OnLobbyPlayerJoined;

        /// <summary>
        /// Fired when a player leaves the pre-match room.
        /// </summary>
        public static event Action<string> OnLobbyPlayerLeft;

        /// <summary>
        /// Fired when a player's ready status changes in the lobby.
        /// </summary>
        public static event Action<string, bool> OnLobbyPlayerReadyChanged;

        /// <summary>
        /// Fired when all players in the lobby are ready.
        /// </summary>
        public static event Action OnLobbyAllReady;

        /// <summary>
        /// Fired when the match is about to launch from the lobby.
        /// </summary>
        public static event Action OnLobbyMatchLaunching;

        /// <summary>
        /// Fired when the lobby/match is cancelled.
        /// </summary>
        public static event Action<string> OnLobbyCancelled;

        // =============================================================================
        // CUSTOM STAGE EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when a custom stage is successfully created.
        /// </summary>
        public static event Action<StageData> OnStageCreated;

        /// <summary>
        /// Fired when successfully joined a custom stage.
        /// </summary>
        public static event Action<StageData> OnStageJoined;

        /// <summary>
        /// Fired when stage configuration is updated.
        /// </summary>
        public static event Action<StageData> OnStageUpdated;

        /// <summary>
        /// Fired when a player joins the stage.
        /// </summary>
        public static event Action<StagePlayer> OnStagePlayerJoined;

        /// <summary>
        /// Fired when a player leaves the stage.
        /// </summary>
        public static event Action<StagePlayer> OnStagePlayerLeft;

        /// <summary>
        /// Fired when a player's ready state changes.
        /// </summary>
        public static event Action<StagePlayer> OnStagePlayerReadyChanged;

        /// <summary>
        /// Fired when stage admin changes.
        /// </summary>
        public static event Action<string> OnStageAdminChanged;

        /// <summary>
        /// Fired when leaving a stage.
        /// </summary>
        public static event Action OnStageLeft;

        /// <summary>
        /// Fired when stage is cancelled/dissolved.
        /// </summary>
        public static event Action<string> OnStageCancelled;

        /// <summary>
        /// Fired when local player is kicked from stage.
        /// </summary>
        public static event Action<string> OnKickedFromStage;

        /// <summary>
        /// Fired when stage match is about to start.
        /// </summary>
        public static event Action<StageData> OnStageMatchStarting;

        // =============================================================================
        // CONNECTION EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when connection state changes.
        /// </summary>
        public static event Action<ConnectionState> OnConnectionStateChanged;

        /// <summary>
        /// Fired when connection is established.
        /// </summary>
        public static event Action OnConnected;

        /// <summary>
        /// Fired when connection is lost.
        /// </summary>
        public static event Action OnDisconnected;

        /// <summary>
        /// Fired during reconnection attempts.
        /// Provides current attempt number.
        /// </summary>
        public static event Action<int> OnReconnecting;

        /// <summary>
        /// Fired when reconnection is successful.
        /// </summary>
        public static event Action OnReconnected;

        /// <summary>
        /// Fired when all reconnection attempts fail.
        /// </summary>
        public static event Action<DeskillzError> OnReconnectionFailed;

        // =============================================================================
        // ERROR EVENTS
        // =============================================================================

        /// <summary>
        /// General error event for non-specific errors.
        /// </summary>
        public static event Action<DeskillzError> OnError;

        /// <summary>
        /// Fired when anti-cheat system detects a violation.
        /// </summary>
        public static event Action<DeskillzError> OnAntiCheatViolation;

        // =============================================================================
        // RAISERS - DEEP LINK (NEW)
        // =============================================================================

        internal static void RaiseDeepLinkReceived(string url, MatchLaunchData data)
        {
            DeskillzLogger.LogEvent("Deep Link Received", url);
            SafeInvoke(OnDeepLinkReceived, url, data);
        }

        internal static void RaiseNormalLaunch()
        {
            DeskillzLogger.LogEvent("Normal Launch");
            SafeInvoke(OnNormalLaunch);
        }

        internal static void RaiseDeepLinkError(string error)
        {
            DeskillzLogger.Error($"Deep Link Error: {error}");
            SafeInvoke(OnDeepLinkError, error);
        }

        internal static void RaiseNavigationReceived(NavigationAction action, string targetId)
        {
            DeskillzLogger.LogEvent("Navigation Received", $"{action}, Target: {targetId ?? "none"}");
            SafeInvoke(OnNavigationReceived, action, targetId);
        }

        // =============================================================================
        // RAISERS - LOBBY (NEW)
        // =============================================================================

        internal static void RaiseLobbyConnected()
        {
            DeskillzLogger.LogEvent("Lobby Connected");
            SafeInvoke(OnLobbyConnected);
        }

        internal static void RaiseLobbyDisconnected(string reason)
        {
            DeskillzLogger.LogEvent("Lobby Disconnected", reason);
            SafeInvoke(OnLobbyDisconnected, reason);
        }

        internal static void RaiseLobbyPlayerJoined(PlayerPresence player)
        {
            DeskillzLogger.LogEvent("Lobby Player Joined", player.Username);
            SafeInvoke(OnLobbyPlayerJoined, player);
        }

        internal static void RaiseLobbyPlayerLeft(string playerId)
        {
            DeskillzLogger.LogEvent("Lobby Player Left", playerId);
            SafeInvoke(OnLobbyPlayerLeft, playerId);
        }

        internal static void RaiseLobbyPlayerReadyChanged(string playerId, bool isReady)
        {
            DeskillzLogger.LogEvent("Lobby Player Ready", $"{playerId}: {isReady}");
            SafeInvoke(OnLobbyPlayerReadyChanged, playerId, isReady);
        }

        internal static void RaiseLobbyAllReady()
        {
            DeskillzLogger.LogEvent("Lobby All Ready");
            SafeInvoke(OnLobbyAllReady);
        }

        internal static void RaiseLobbyMatchLaunching()
        {
            DeskillzLogger.LogEvent("Lobby Match Launching");
            SafeInvoke(OnLobbyMatchLaunching);
        }

        internal static void RaiseLobbyCancelled(string reason)
        {
            DeskillzLogger.LogEvent("Lobby Cancelled", reason);
            SafeInvoke(OnLobbyCancelled, reason);
        }

        // =============================================================================
        // RAISERS - SDK LIFECYCLE
        // =============================================================================

        internal static void RaiseInitializing()
        {
            DeskillzLogger.LogEvent("SDK Initializing");
            SafeInvoke(OnInitializing);
        }

        internal static void RaiseReady()
        {
            DeskillzLogger.LogEvent("SDK Ready");
            SafeInvoke(OnReady);
        }

        internal static void RaiseInitializationFailed(DeskillzError error)
        {
            DeskillzLogger.Error($"SDK Initialization Failed: {error.Message}");
            SafeInvoke(OnInitializationFailed, error);
        }

        internal static void RaiseTestModeChanged(bool testMode)
        {
            DeskillzLogger.LogEvent("Test Mode", testMode ? "Enabled" : "Disabled");
            SafeInvoke(OnTestModeChanged, testMode);
        }

        // =============================================================================
        // RAISERS - PLAYER
        // =============================================================================

        internal static void RaisePlayerUpdated(PlayerData player)
        {
            DeskillzLogger.LogEvent("Player Updated", player.Username);
            SafeInvoke(OnPlayerUpdated, player);
        }

        internal static void RaisePlayerAuthenticated(PlayerData player)
        {
            DeskillzLogger.LogEvent("Player Authenticated", player.Username);
            SafeInvoke(OnPlayerAuthenticated, player);
        }

        internal static void RaisePlayerSessionExpired()
        {
            DeskillzLogger.Warning("Player Session Expired");
            SafeInvoke(OnPlayerSessionExpired);
        }

        // =============================================================================
        // RAISERS - MATCH
        // =============================================================================

        internal static void RaiseMatchReady(MatchData match)
        {
            DeskillzLogger.LogMatch("Ready", match.MatchId);
            SafeInvoke(OnMatchReady, match);
        }

        internal static void RaiseMatchCountdown(int seconds)
        {
            DeskillzLogger.LogEvent("Countdown", $"{seconds}s");
            SafeInvoke(OnMatchCountdown, seconds);
            SafeInvoke(OnCountdown, seconds);
        }

        internal static void RaiseMatchStart(MatchData match)
        {
            DeskillzLogger.LogMatch("Started", match.MatchId);
            SafeInvoke(OnMatchStart, match);
        }

        internal static void RaiseMatchPaused()
        {
            DeskillzLogger.LogEvent("Match Paused");
            SafeInvoke(OnMatchPaused);
        }

        internal static void RaiseMatchResumed()
        {
            DeskillzLogger.LogEvent("Match Resumed");
            SafeInvoke(OnMatchResumed);
        }

        internal static void RaiseMatchEnding()
        {
            DeskillzLogger.LogEvent("Match Ending");
            SafeInvoke(OnMatchEnding);
        }

        internal static void RaiseMatchComplete(MatchResult result)
        {
            DeskillzLogger.LogMatch("Complete", result.MatchId, $"Outcome: {result.Outcome}");
            SafeInvoke(OnMatchComplete, result);
        }

        internal static void RaiseMatchCancelled(DeskillzError error)
        {
            DeskillzLogger.Warning($"Match Cancelled: {error.Message}");
            SafeInvoke(OnMatchCancelled, error);
        }

        internal static void RaiseMatchForfeited()
        {
            DeskillzLogger.LogEvent("Match Forfeited");
            SafeInvoke(OnMatchForfeited);
        }

        // =============================================================================
        // RAISERS - SCORE
        // =============================================================================

        internal static void RaiseLocalScoreUpdated(int score)
        {
            DeskillzLogger.LogScore("Updated", score);
            SafeInvoke(OnLocalScoreUpdated, score);
        }

        internal static void RaisePlayerScoreUpdated(string playerId, int score)
        {
            DeskillzLogger.LogScore($"Player {playerId}", score);
            SafeInvoke(OnPlayerScoreUpdated, playerId, score);
        }

        internal static void RaiseOpponentScoreUpdated(string playerId, int score)
        {
            DeskillzLogger.LogScore($"Opponent {playerId}", score);
            SafeInvoke(OnOpponentScoreUpdated, playerId, score);
        }

        internal static void RaiseScoreSubmitted(int score)
        {
            DeskillzLogger.LogScore("Submitted", score);
            SafeInvoke(OnScoreSubmitted, score);
        }

        internal static void RaiseScoreSubmissionFailed(DeskillzError error)
        {
            DeskillzLogger.Error($"Score Submission Failed: {error.Message}");
            SafeInvoke(OnScoreSubmissionFailed, error);
        }

        // =============================================================================
        // RAISERS - MULTIPLAYER
        // =============================================================================

        internal static void RaisePlayerJoined(MatchPlayer player)
        {
            DeskillzLogger.LogEvent("Player Joined", player.Username);
            SafeInvoke(OnPlayerJoined, player);
        }

        internal static void RaisePlayerLeft(MatchPlayer player)
        {
            DeskillzLogger.LogEvent("Player Left", player.Username);
            SafeInvoke(OnPlayerLeft, player);
        }

        internal static void RaisePlayerStateUpdated(PlayerState state)
        {
            DeskillzLogger.Verbose($"Player State Updated: {state.PlayerId}");
            SafeInvoke(OnPlayerStateUpdated, state);
        }

        internal static void RaiseMessageReceived(NetworkMessage message)
        {
            DeskillzLogger.Verbose($"Message Received: {message.Type} from {message.SenderId}");
            SafeInvoke(OnMessageReceived, message);
        }

        internal static void RaiseAllPlayersReady()
        {
            DeskillzLogger.LogEvent("All Players Ready");
            SafeInvoke(OnAllPlayersReady);
        }

        // =============================================================================
        // RAISERS - STAGE
        // =============================================================================

        internal static void RaiseStageCreated(StageData stage)
        {
            DeskillzLogger.LogEvent("Stage Created", $"{stage.Name} ({stage.InviteCode})");
            SafeInvoke(OnStageCreated, stage);
        }

        internal static void RaiseStageJoined(StageData stage)
        {
            DeskillzLogger.LogEvent("Stage Joined", stage.Name);
            SafeInvoke(OnStageJoined, stage);
        }

        internal static void RaiseStageUpdated(StageData stage)
        {
            DeskillzLogger.LogEvent("Stage Updated", stage.Name);
            SafeInvoke(OnStageUpdated, stage);
        }

        internal static void RaiseStagePlayerJoined(StagePlayer player)
        {
            DeskillzLogger.LogEvent("Stage Player Joined", player.Username);
            SafeInvoke(OnStagePlayerJoined, player);
        }

        internal static void RaiseStagePlayerLeft(StagePlayer player)
        {
            DeskillzLogger.LogEvent("Stage Player Left", player.Username);
            SafeInvoke(OnStagePlayerLeft, player);
        }

        internal static void RaiseStagePlayerReadyChanged(StagePlayer player)
        {
            DeskillzLogger.LogEvent("Player Ready Changed", $"{player.Username}: {player.IsReady}");
            SafeInvoke(OnStagePlayerReadyChanged, player);
        }

        internal static void RaiseStageAdminChanged(string newAdminId)
        {
            DeskillzLogger.LogEvent("Stage Admin Changed", newAdminId);
            SafeInvoke(OnStageAdminChanged, newAdminId);
        }

        internal static void RaiseStageLeft()
        {
            DeskillzLogger.LogEvent("Stage Left");
            SafeInvoke(OnStageLeft);
        }

        internal static void RaiseStageCancelled(string reason)
        {
            DeskillzLogger.LogEvent("Stage Cancelled", reason);
            SafeInvoke(OnStageCancelled, reason);
        }

        internal static void RaiseKickedFromStage(string reason)
        {
            DeskillzLogger.Warning($"Kicked From Stage: {reason}");
            SafeInvoke(OnKickedFromStage, reason);
        }

        internal static void RaiseStageMatchStarting(StageData stage)
        {
            DeskillzLogger.LogEvent("Stage Match Starting", stage.Name);
            SafeInvoke(OnStageMatchStarting, stage);
        }

        // =============================================================================
        // RAISERS - CONNECTION
        // =============================================================================

        internal static void RaiseConnectionStateChanged(ConnectionState state)
        {
            DeskillzLogger.LogEvent("Connection State", state.ToString());
            SafeInvoke(OnConnectionStateChanged, state);
        }

        internal static void RaiseConnected()
        {
            DeskillzLogger.Info("Connected to server");
            SafeInvoke(OnConnected);
        }

        internal static void RaiseDisconnected()
        {
            DeskillzLogger.Warning("Disconnected from server");
            SafeInvoke(OnDisconnected);
        }

        internal static void RaiseReconnecting(int attempt)
        {
            DeskillzLogger.Info($"Reconnecting... (attempt {attempt})");
            SafeInvoke(OnReconnecting, attempt);
        }

        internal static void RaiseReconnected()
        {
            DeskillzLogger.Info("Reconnected successfully");
            SafeInvoke(OnReconnected);
        }

        internal static void RaiseReconnectionFailed(DeskillzError error)
        {
            DeskillzLogger.Error($"Reconnection Failed: {error.Message}");
            SafeInvoke(OnReconnectionFailed, error);
        }

        // =============================================================================
        // RAISERS - ERROR
        // =============================================================================

        internal static void RaiseError(DeskillzError error)
        {
            DeskillzLogger.Error($"Error: {error.Code} - {error.Message}");
            SafeInvoke(OnError, error);
        }

        internal static void RaiseAntiCheatViolation(DeskillzError error)
        {
            DeskillzLogger.Error($"Anti-Cheat Violation: {error.Message}");
            SafeInvoke(OnAntiCheatViolation, error);
        }

        // =============================================================================
        // SAFE INVOKE HELPERS
        // =============================================================================

        private static void SafeInvoke(Action action)
        {
            if (action == null) return;
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Event handler exception", ex);
            }
        }

        private static void SafeInvoke<T>(Action<T> action, T arg)
        {
            if (action == null) return;
            try
            {
                action.Invoke(arg);
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Event handler exception", ex);
            }
        }

        private static void SafeInvoke<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            if (action == null) return;
            try
            {
                action.Invoke(arg1, arg2);
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Event handler exception", ex);
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        /// <summary>
        /// Clear all event subscriptions. Called during SDK shutdown.
        /// </summary>
        internal static void ClearAllSubscriptions()
        {
            // SDK Lifecycle
            OnInitializing = null;
            OnReady = null;
            OnInitializationFailed = null;
            OnTestModeChanged = null;

            // Deep Link (NEW)
            OnDeepLinkReceived = null;
            OnNormalLaunch = null;
            OnDeepLinkError = null;

            // Player
            OnPlayerUpdated = null;
            OnPlayerAuthenticated = null;
            OnPlayerSessionExpired = null;

            // Match
            OnMatchReady = null;
            OnMatchCountdown = null;
            OnCountdown = null;
            OnMatchStart = null;
            OnMatchPaused = null;
            OnMatchResumed = null;
            OnMatchEnding = null;
            OnMatchComplete = null;
            OnMatchCancelled = null;
            OnMatchForfeited = null;

            // Score
            OnLocalScoreUpdated = null;
            OnPlayerScoreUpdated = null;
            OnOpponentScoreUpdated = null;
            OnScoreSubmitted = null;
            OnScoreSubmissionFailed = null;

            // Multiplayer
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnPlayerStateUpdated = null;
            OnMessageReceived = null;
            OnAllPlayersReady = null;

            // Lobby (NEW)
            OnLobbyConnected = null;
            OnLobbyDisconnected = null;
            OnLobbyPlayerJoined = null;
            OnLobbyPlayerLeft = null;
            OnLobbyPlayerReadyChanged = null;
            OnLobbyAllReady = null;
            OnLobbyMatchLaunching = null;
            OnLobbyCancelled = null;

            // Stage
            OnStageCreated = null;
            OnStageJoined = null;
            OnStageUpdated = null;
            OnStagePlayerJoined = null;
            OnStagePlayerLeft = null;
            OnStagePlayerReadyChanged = null;
            OnStageAdminChanged = null;
            OnStageLeft = null;
            OnStageCancelled = null;
            OnKickedFromStage = null;
            OnStageMatchStarting = null;

            // Connection
            OnConnectionStateChanged = null;
            OnConnected = null;
            OnDisconnected = null;
            OnReconnecting = null;
            OnReconnected = null;
            OnReconnectionFailed = null;

            // Error
            OnError = null;
            OnAntiCheatViolation = null;

            DeskillzLogger.Debug("All event subscriptions cleared");
        }
    }
}
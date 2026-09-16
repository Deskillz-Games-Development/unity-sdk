// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System.Collections.Generic;

namespace Deskillz.UI
{
    /// <summary>
    /// Interface for custom UI implementations.
    /// Implement this to create your own UI while still integrating with Deskillz.
    /// </summary>
    public interface IDeskillzUI
    {
        // =============================================================================
        // HUD
        // =============================================================================

        /// <summary>
        /// Show the in-game HUD.
        /// </summary>
        void ShowHUD();

        /// <summary>
        /// Hide the in-game HUD.
        /// </summary>
        void HideHUD();

        /// <summary>
        /// Update the score display.
        /// </summary>
        void UpdateScore(int score);

        /// <summary>
        /// Update the timer display.
        /// </summary>
        void UpdateTimer(float secondsRemaining);

        // =============================================================================
        // LEADERBOARD
        // =============================================================================

        /// <summary>
        /// Show leaderboard with player list.
        /// </summary>
        void ShowLeaderboard(List<MatchPlayer> players);

        /// <summary>
        /// Hide leaderboard.
        /// </summary>
        void HideLeaderboard();

        // =============================================================================
        // RESULTS
        // =============================================================================

        /// <summary>
        /// Show match results screen.
        /// </summary>
        void ShowResults(MatchResult result);

        /// <summary>
        /// Hide results screen.
        /// </summary>
        void HideResults();

        // =============================================================================
        // COUNTDOWN
        // =============================================================================

        /// <summary>
        /// Show countdown number.
        /// </summary>
        void ShowCountdown(int seconds);
    }

    /// <summary>
    /// Extended interface with additional UI features.
    /// </summary>
    public interface IDeskillzUIExtended : IDeskillzUI
    {
        /// <summary>
        /// Show a notification.
        /// </summary>
        void ShowNotification(string message, NotificationType type);

        /// <summary>
        /// Show pause menu.
        /// </summary>
        void ShowPauseMenu();

        /// <summary>
        /// Hide pause menu.
        /// </summary>
        void HidePauseMenu();

        /// <summary>
        /// Update opponent score.
        /// </summary>
        void UpdateOpponentScore(string playerId, int score);

        /// <summary>
        /// Show round indicator.
        /// </summary>
        void ShowRound(int current, int total);

        /// <summary>
        /// Show combo indicator.
        /// </summary>
        void ShowCombo(int comboCount);

        /// <summary>
        /// Show multiplier indicator.
        /// </summary>
        void ShowMultiplier(float multiplier);
    }

    /// <summary>
    /// Base class for custom UI implementations.
    /// Provides default implementations that do nothing.
    /// Override only what you need.
    /// </summary>
    public abstract class DeskillzUIBase : IDeskillzUIExtended
    {
        public virtual void ShowHUD() { }
        public virtual void HideHUD() { }
        public virtual void UpdateScore(int score) { }
        public virtual void UpdateTimer(float secondsRemaining) { }
        public virtual void ShowLeaderboard(List<MatchPlayer> players) { }
        public virtual void HideLeaderboard() { }
        public virtual void ShowResults(MatchResult result) { }
        public virtual void HideResults() { }
        public virtual void ShowCountdown(int seconds) { }
        public virtual void ShowNotification(string message, NotificationType type) { }
        public virtual void ShowPauseMenu() { }
        public virtual void HidePauseMenu() { }
        public virtual void UpdateOpponentScore(string playerId, int score) { }
        public virtual void ShowRound(int current, int total) { }
        public virtual void ShowCombo(int comboCount) { }
        public virtual void ShowMultiplier(float multiplier) { }
    }
}

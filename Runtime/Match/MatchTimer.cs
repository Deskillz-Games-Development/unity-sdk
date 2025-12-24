// =============================================================================
// Deskillz SDK for Unity - Phase 2: Match System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz.Match
{
    /// <summary>
    /// Manages match timing including countdown, time limits, and grace periods.
    /// </summary>
    public class MatchTimer
    {
        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when time warning threshold is reached.</summary>
        public event Action<float> OnTimeWarning;

        /// <summary>Fired when main time expires.</summary>
        public event Action OnTimeExpired;

        /// <summary>Fired when grace period ends.</summary>
        public event Action OnGracePeriodEnd;

        /// <summary>Fired every second while running.</summary>
        public event Action<float> OnTick;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        private float _totalTime;
        private float _gracePeriod;
        private float _warningThreshold = 10f;
        private bool _warningFired;

        // =============================================================================
        // STATE
        // =============================================================================

        private float _elapsedTime;
        private bool _isRunning;
        private bool _isPaused;
        private bool _isExpired;
        private bool _isInGracePeriod;
        private bool _gracePeriodEnded;
        private float _lastTickTime;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Total time limit in seconds (0 = no limit).</summary>
        public float TotalTime => _totalTime;

        /// <summary>Time elapsed since start.</summary>
        public float TimeElapsed => _elapsedTime;

        /// <summary>Time remaining (-1 if no limit).</summary>
        public float TimeRemaining
        {
            get
            {
                if (_totalTime <= 0) return -1f;
                return Mathf.Max(0f, _totalTime - _elapsedTime);
            }
        }

        /// <summary>Time remaining including grace period.</summary>
        public float TimeRemainingWithGrace
        {
            get
            {
                if (_totalTime <= 0) return -1f;
                var remaining = (_totalTime + _gracePeriod) - _elapsedTime;
                return Mathf.Max(0f, remaining);
            }
        }

        /// <summary>Progress from 0 to 1.</summary>
        public float Progress
        {
            get
            {
                if (_totalTime <= 0) return 0f;
                return Mathf.Clamp01(_elapsedTime / _totalTime);
            }
        }

        /// <summary>Whether timer is currently running.</summary>
        public bool IsRunning => _isRunning && !_isPaused;

        /// <summary>Whether timer is paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Whether main time has expired.</summary>
        public bool IsExpired => _isExpired;

        /// <summary>Whether currently in grace period.</summary>
        public bool IsInGracePeriod => _isInGracePeriod;

        /// <summary>Whether grace period has ended.</summary>
        public bool GracePeriodEnded => _gracePeriodEnded;

        /// <summary>Whether this is a timed match (has time limit).</summary>
        public bool HasTimeLimit => _totalTime > 0;

        /// <summary>Formatted time remaining (MM:SS).</summary>
        public string FormattedTimeRemaining
        {
            get
            {
                var time = TimeRemaining;
                if (time < 0) return "--:--";
                
                var minutes = Mathf.FloorToInt(time / 60f);
                var seconds = Mathf.FloorToInt(time % 60f);
                return $"{minutes:00}:{seconds:00}";
            }
        }

        /// <summary>Formatted time remaining with milliseconds (MM:SS.ms).</summary>
        public string FormattedTimeRemainingPrecise
        {
            get
            {
                var time = TimeRemaining;
                if (time < 0) return "--:--.--";
                
                var minutes = Mathf.FloorToInt(time / 60f);
                var seconds = Mathf.FloorToInt(time % 60f);
                var ms = Mathf.FloorToInt((time * 100f) % 100f);
                return $"{minutes:00}:{seconds:00}.{ms:00}";
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        /// <summary>
        /// Configure the timer with time limit and grace period.
        /// </summary>
        public void Configure(float totalSeconds, float gracePeriodSeconds = 0f)
        {
            _totalTime = Mathf.Max(0f, totalSeconds);
            _gracePeriod = Mathf.Max(0f, gracePeriodSeconds);
            Reset();

            DeskillzLogger.Debug($"Timer configured: {totalSeconds}s + {gracePeriodSeconds}s grace");
        }

        /// <summary>
        /// Set the warning threshold (seconds before expiration).
        /// </summary>
        public void SetWarningThreshold(float seconds)
        {
            _warningThreshold = Mathf.Max(0f, seconds);
        }

        // =============================================================================
        // CONTROL
        // =============================================================================

        /// <summary>
        /// Start the timer.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                DeskillzLogger.Warning("Timer already running");
                return;
            }

            _isRunning = true;
            _isPaused = false;
            _lastTickTime = _elapsedTime;

            DeskillzLogger.Debug("Timer started");
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;

            DeskillzLogger.Debug($"Timer stopped at {_elapsedTime:F2}s");
        }

        /// <summary>
        /// Pause the timer.
        /// </summary>
        public void Pause()
        {
            if (!_isRunning || _isPaused) return;

            _isPaused = true;
            DeskillzLogger.Debug("Timer paused");
        }

        /// <summary>
        /// Resume from pause.
        /// </summary>
        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;

            _isPaused = false;
            DeskillzLogger.Debug("Timer resumed");
        }

        /// <summary>
        /// Reset the timer to initial state.
        /// </summary>
        public void Reset()
        {
            _elapsedTime = 0f;
            _isRunning = false;
            _isPaused = false;
            _isExpired = false;
            _isInGracePeriod = false;
            _gracePeriodEnded = false;
            _warningFired = false;
            _lastTickTime = 0f;

            DeskillzLogger.Debug("Timer reset");
        }

        /// <summary>
        /// Add time to the timer (power-up, bonus, etc).
        /// </summary>
        public void AddTime(float seconds)
        {
            if (seconds <= 0) return;

            _totalTime += seconds;
            
            // May need to un-expire if we were in grace period
            if (_isExpired && TimeRemaining > 0)
            {
                _isExpired = false;
                _isInGracePeriod = false;
            }

            DeskillzLogger.Debug($"Added {seconds}s to timer. New total: {_totalTime}s");
        }

        /// <summary>
        /// Remove time from the timer (penalty, etc).
        /// </summary>
        public void RemoveTime(float seconds)
        {
            if (seconds <= 0) return;

            _elapsedTime += seconds;
            
            DeskillzLogger.Debug($"Removed {seconds}s from timer");

            // Check for expiration
            CheckExpiration();
        }

        // =============================================================================
        // UPDATE
        // =============================================================================

        /// <summary>
        /// Update the timer. Call from Update() or with Time.deltaTime.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isRunning || _isPaused || _gracePeriodEnded) return;

            _elapsedTime += deltaTime;

            // Fire tick events every second
            if (_elapsedTime - _lastTickTime >= 1f)
            {
                _lastTickTime = Mathf.Floor(_elapsedTime);
                OnTick?.Invoke(TimeRemaining);
            }

            // Check expiration
            CheckExpiration();
        }

        private void CheckExpiration()
        {
            if (!HasTimeLimit) return;

            var remaining = TimeRemaining;

            // Warning check
            if (!_warningFired && remaining <= _warningThreshold && remaining > 0)
            {
                _warningFired = true;
                OnTimeWarning?.Invoke(remaining);
            }

            // Main expiration
            if (!_isExpired && remaining <= 0)
            {
                _isExpired = true;
                _isInGracePeriod = _gracePeriod > 0;
                OnTimeExpired?.Invoke();

                DeskillzLogger.Debug("Timer expired" + (_isInGracePeriod ? ", entering grace period" : ""));
            }

            // Grace period end
            if (_isExpired && !_gracePeriodEnded && TimeRemainingWithGrace <= 0)
            {
                _gracePeriodEnded = true;
                _isInGracePeriod = false;
                OnGracePeriodEnd?.Invoke();

                DeskillzLogger.Debug("Grace period ended");
            }
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Get time remaining as a TimeSpan.
        /// </summary>
        public TimeSpan GetTimeRemainingSpan()
        {
            var remaining = TimeRemaining;
            if (remaining < 0) return TimeSpan.Zero;
            return TimeSpan.FromSeconds(remaining);
        }

        /// <summary>
        /// Check if there's less than X seconds remaining.
        /// </summary>
        public bool IsUnderTime(float seconds)
        {
            var remaining = TimeRemaining;
            return remaining >= 0 && remaining < seconds;
        }

        /// <summary>
        /// Get percentage of time remaining (0-100).
        /// </summary>
        public float GetPercentageRemaining()
        {
            return (1f - Progress) * 100f;
        }
    }
}

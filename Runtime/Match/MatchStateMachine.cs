// =============================================================================
// Deskillz SDK for Unity - Phase 2: Match System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Deskillz.Match
{
    /// <summary>
    /// State machine managing match state transitions.
    /// Ensures valid state flow and prevents invalid transitions.
    /// </summary>
    public class MatchStateMachine
    {
        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>
        /// Fired when state changes. Provides old and new state.
        /// </summary>
        public event Action<MatchStatus, MatchStatus> OnStateChanged;

        /// <summary>
        /// Fired when an invalid transition is attempted.
        /// </summary>
        public event Action<MatchStatus, MatchStatus> OnInvalidTransition;

        // =============================================================================
        // STATE
        // =============================================================================

        private MatchStatus _currentState = MatchStatus.None;
        private readonly Dictionary<MatchStatus, HashSet<MatchStatus>> _validTransitions;
        private readonly List<StateTransition> _transitionHistory;
        private const int MAX_HISTORY = 50;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current match state.</summary>
        public MatchStatus CurrentState => _currentState;

        /// <summary>Previous state (if any).</summary>
        public MatchStatus PreviousState { get; private set; } = MatchStatus.None;

        /// <summary>Time spent in current state.</summary>
        public float TimeInCurrentState { get; private set; }

        /// <summary>Timestamp when entered current state.</summary>
        public DateTime StateEnteredAt { get; private set; }

        /// <summary>History of state transitions.</summary>
        public IReadOnlyList<StateTransition> TransitionHistory => _transitionHistory.AsReadOnly();

        // =============================================================================
        // CONSTRUCTOR
        // =============================================================================

        public MatchStateMachine()
        {
            _transitionHistory = new List<StateTransition>();
            _validTransitions = BuildTransitionTable();
            StateEnteredAt = DateTime.UtcNow;
        }

        // =============================================================================
        // TRANSITION TABLE
        // =============================================================================

        /// <summary>
        /// Defines valid state transitions.
        /// </summary>
        private Dictionary<MatchStatus, HashSet<MatchStatus>> BuildTransitionTable()
        {
            return new Dictionary<MatchStatus, HashSet<MatchStatus>>
            {
                // From None - can go to Pending (match received)
                {
                    MatchStatus.None,
                    new HashSet<MatchStatus> { MatchStatus.Pending }
                },

                // From Pending - can start countdown, start directly, cancel, or forfeit
                {
                    MatchStatus.Pending,
                    new HashSet<MatchStatus>
                    {
                        MatchStatus.Countdown,
                        MatchStatus.InProgress,
                        MatchStatus.Cancelled,
                        MatchStatus.Forfeited
                    }
                },

                // From Countdown - can start playing or cancel
                {
                    MatchStatus.Countdown,
                    new HashSet<MatchStatus>
                    {
                        MatchStatus.InProgress,
                        MatchStatus.Cancelled
                    }
                },

                // From InProgress - can pause, process end, forfeit, or cancel
                {
                    MatchStatus.InProgress,
                    new HashSet<MatchStatus>
                    {
                        MatchStatus.Paused,
                        MatchStatus.Processing,
                        MatchStatus.Forfeited,
                        MatchStatus.Cancelled
                    }
                },

                // From Paused - can resume, process end, forfeit, or cancel
                {
                    MatchStatus.Paused,
                    new HashSet<MatchStatus>
                    {
                        MatchStatus.InProgress,
                        MatchStatus.Processing,
                        MatchStatus.Forfeited,
                        MatchStatus.Cancelled
                    }
                },

                // From Processing - waiting for server, can complete or cancel
                {
                    MatchStatus.Processing,
                    new HashSet<MatchStatus>
                    {
                        MatchStatus.Completed,
                        MatchStatus.Cancelled
                    }
                },

                // Terminal states - can only go back to None
                {
                    MatchStatus.Completed,
                    new HashSet<MatchStatus> { MatchStatus.None }
                },
                {
                    MatchStatus.Cancelled,
                    new HashSet<MatchStatus> { MatchStatus.None }
                },
                {
                    MatchStatus.Forfeited,
                    new HashSet<MatchStatus> { MatchStatus.None }
                }
            };
        }

        // =============================================================================
        // STATE TRANSITIONS
        // =============================================================================

        /// <summary>
        /// Attempt to transition to a new state.
        /// </summary>
        /// <returns>True if transition was valid and executed.</returns>
        public bool TransitionTo(MatchStatus newState)
        {
            if (newState == _currentState)
            {
                DeskillzLogger.Verbose($"Already in state {newState}");
                return true;
            }

            if (!IsValidTransition(_currentState, newState))
            {
                DeskillzLogger.Warning($"Invalid state transition: {_currentState} -> {newState}");
                OnInvalidTransition?.Invoke(_currentState, newState);
                return false;
            }

            var oldState = _currentState;
            PreviousState = oldState;
            _currentState = newState;
            TimeInCurrentState = 0f;
            StateEnteredAt = DateTime.UtcNow;

            // Record transition
            RecordTransition(oldState, newState);

            DeskillzLogger.Debug($"State transition: {oldState} -> {newState}");
            OnStateChanged?.Invoke(oldState, newState);

            return true;
        }

        /// <summary>
        /// Force a state change without validation.
        /// Use sparingly - primarily for error recovery.
        /// </summary>
        public void ForceState(MatchStatus state)
        {
            var oldState = _currentState;
            PreviousState = oldState;
            _currentState = state;
            TimeInCurrentState = 0f;
            StateEnteredAt = DateTime.UtcNow;

            RecordTransition(oldState, state, forced: true);

            DeskillzLogger.Warning($"Forced state: {oldState} -> {state}");
            OnStateChanged?.Invoke(oldState, state);
        }

        /// <summary>
        /// Check if a transition is valid.
        /// </summary>
        public bool IsValidTransition(MatchStatus from, MatchStatus to)
        {
            if (!_validTransitions.TryGetValue(from, out var validTargets))
            {
                return false;
            }
            return validTargets.Contains(to);
        }

        /// <summary>
        /// Check if can transition to a specific state from current.
        /// </summary>
        public bool CanTransitionTo(MatchStatus target)
        {
            return IsValidTransition(_currentState, target);
        }

        /// <summary>
        /// Get all valid transitions from current state.
        /// </summary>
        public MatchStatus[] GetValidTransitions()
        {
            if (_validTransitions.TryGetValue(_currentState, out var targets))
            {
                var result = new MatchStatus[targets.Count];
                targets.CopyTo(result);
                return result;
            }
            return Array.Empty<MatchStatus>();
        }

        // =============================================================================
        // STATE QUERIES
        // =============================================================================

        /// <summary>
        /// Whether match is in an active state (can be played).
        /// </summary>
        public bool IsActive => _currentState == MatchStatus.InProgress || 
                               _currentState == MatchStatus.Paused;

        /// <summary>
        /// Whether match is in a terminal state.
        /// </summary>
        public bool IsTerminal => _currentState == MatchStatus.Completed ||
                                 _currentState == MatchStatus.Cancelled ||
                                 _currentState == MatchStatus.Forfeited;

        /// <summary>
        /// Whether match has started (past pending/countdown).
        /// </summary>
        public bool HasStarted => _currentState != MatchStatus.None &&
                                 _currentState != MatchStatus.Pending &&
                                 _currentState != MatchStatus.Countdown;

        /// <summary>
        /// Whether match can be paused.
        /// </summary>
        public bool CanPause => _currentState == MatchStatus.InProgress;

        /// <summary>
        /// Whether match can be resumed.
        /// </summary>
        public bool CanResume => _currentState == MatchStatus.Paused;

        /// <summary>
        /// Whether match can be ended.
        /// </summary>
        public bool CanEnd => IsActive;

        /// <summary>
        /// Whether match can be forfeited.
        /// </summary>
        public bool CanForfeit => _currentState == MatchStatus.Pending ||
                                 _currentState == MatchStatus.InProgress ||
                                 _currentState == MatchStatus.Paused;

        // =============================================================================
        // HISTORY
        // =============================================================================

        private void RecordTransition(MatchStatus from, MatchStatus to, bool forced = false)
        {
            var transition = new StateTransition
            {
                FromState = from,
                ToState = to,
                Timestamp = DateTime.UtcNow,
                WasForced = forced
            };

            _transitionHistory.Add(transition);

            // Trim history if too long
            while (_transitionHistory.Count > MAX_HISTORY)
            {
                _transitionHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clear transition history.
        /// </summary>
        public void ClearHistory()
        {
            _transitionHistory.Clear();
        }

        // =============================================================================
        // RESET
        // =============================================================================

        /// <summary>
        /// Reset state machine to initial state.
        /// </summary>
        public void Reset()
        {
            PreviousState = _currentState;
            _currentState = MatchStatus.None;
            TimeInCurrentState = 0f;
            StateEnteredAt = DateTime.UtcNow;
            ClearHistory();
        }
    }

    // =============================================================================
    // STATE TRANSITION RECORD
    // =============================================================================

    /// <summary>
    /// Record of a state transition.
    /// </summary>
    public struct StateTransition
    {
        public MatchStatus FromState;
        public MatchStatus ToState;
        public DateTime Timestamp;
        public bool WasForced;

        public override string ToString()
        {
            var forced = WasForced ? " (forced)" : "";
            return $"{FromState} -> {ToState} at {Timestamp:HH:mm:ss}{forced}";
        }
    }
}

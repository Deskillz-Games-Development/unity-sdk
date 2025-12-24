// =============================================================================
// Deskillz SDK for Unity - Phase 5: Sync Multiplayer
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Multiplayer
{
    /// <summary>
    /// Represents a player's synchronized state.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        // =============================================================================
        // CORE STATE
        // =============================================================================

        /// <summary>Player ID.</summary>
        public string PlayerId { get; set; }

        /// <summary>Server timestamp when state was captured.</summary>
        public double Timestamp { get; set; }

        /// <summary>Sequence number for ordering.</summary>
        public int Sequence { get; set; }

        // =============================================================================
        // TRANSFORM STATE
        // =============================================================================

        /// <summary>Position in world space.</summary>
        public Vector3 Position { get; set; }

        /// <summary>Rotation as Euler angles.</summary>
        public Vector3 Rotation { get; set; }

        /// <summary>Velocity for prediction.</summary>
        public Vector3 Velocity { get; set; }

        /// <summary>Angular velocity for prediction.</summary>
        public Vector3 AngularVelocity { get; set; }

        // =============================================================================
        // GAME STATE
        // =============================================================================

        /// <summary>Current score.</summary>
        public int Score { get; set; }

        /// <summary>Health/lives remaining.</summary>
        public int Health { get; set; }

        /// <summary>Whether player is alive/active.</summary>
        public bool IsAlive { get; set; } = true;

        /// <summary>Current animation state.</summary>
        public string AnimationState { get; set; }

        /// <summary>Current action being performed.</summary>
        public string CurrentAction { get; set; }

        // =============================================================================
        // INPUT STATE
        // =============================================================================

        /// <summary>Movement input.</summary>
        public Vector2 InputMove { get; set; }

        /// <summary>Look/aim direction.</summary>
        public Vector2 InputLook { get; set; }

        /// <summary>Active input flags.</summary>
        public InputFlags Inputs { get; set; }

        // =============================================================================
        // CUSTOM DATA
        // =============================================================================

        /// <summary>Custom state data as JSON.</summary>
        public string CustomData { get; set; }

        /// <summary>Custom state dictionary.</summary>
        private Dictionary<string, object> _customValues;

        /// <summary>
        /// Set a custom state value.
        /// </summary>
        public void SetCustom<T>(string key, T value)
        {
            _customValues ??= new Dictionary<string, object>();
            _customValues[key] = value;
        }

        /// <summary>
        /// Get a custom state value.
        /// </summary>
        public T GetCustom<T>(string key, T defaultValue = default)
        {
            if (_customValues != null && _customValues.TryGetValue(key, out var value))
            {
                if (value is T typed)
                {
                    return typed;
                }
            }
            return defaultValue;
        }

        // =============================================================================
        // INTERPOLATION
        // =============================================================================

        /// <summary>
        /// Interpolate between two states.
        /// </summary>
        public static PlayerState Lerp(PlayerState a, PlayerState b, float t)
        {
            if (a == null) return b;
            if (b == null) return a;

            return new PlayerState
            {
                PlayerId = a.PlayerId,
                Timestamp = Mathf.Lerp((float)a.Timestamp, (float)b.Timestamp, t),
                Sequence = t < 0.5f ? a.Sequence : b.Sequence,
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Rotation = Vector3.Lerp(a.Rotation, b.Rotation, t),
                Velocity = Vector3.Lerp(a.Velocity, b.Velocity, t),
                AngularVelocity = Vector3.Lerp(a.AngularVelocity, b.AngularVelocity, t),
                Score = t < 0.5f ? a.Score : b.Score,
                Health = Mathf.RoundToInt(Mathf.Lerp(a.Health, b.Health, t)),
                IsAlive = t < 0.5f ? a.IsAlive : b.IsAlive,
                AnimationState = t < 0.5f ? a.AnimationState : b.AnimationState,
                CurrentAction = t < 0.5f ? a.CurrentAction : b.CurrentAction,
                InputMove = Vector2.Lerp(a.InputMove, b.InputMove, t),
                InputLook = Vector2.Lerp(a.InputLook, b.InputLook, t),
                Inputs = t < 0.5f ? a.Inputs : b.Inputs
            };
        }

        /// <summary>
        /// Extrapolate state forward in time.
        /// </summary>
        public PlayerState Extrapolate(float deltaTime)
        {
            return new PlayerState
            {
                PlayerId = PlayerId,
                Timestamp = Timestamp + deltaTime,
                Sequence = Sequence,
                Position = Position + Velocity * deltaTime,
                Rotation = Rotation + AngularVelocity * deltaTime,
                Velocity = Velocity,
                AngularVelocity = AngularVelocity,
                Score = Score,
                Health = Health,
                IsAlive = IsAlive,
                AnimationState = AnimationState,
                CurrentAction = CurrentAction,
                InputMove = InputMove,
                InputLook = InputLook,
                Inputs = Inputs,
                CustomData = CustomData
            };
        }

        /// <summary>
        /// Create a copy of this state.
        /// </summary>
        public PlayerState Clone()
        {
            return new PlayerState
            {
                PlayerId = PlayerId,
                Timestamp = Timestamp,
                Sequence = Sequence,
                Position = Position,
                Rotation = Rotation,
                Velocity = Velocity,
                AngularVelocity = AngularVelocity,
                Score = Score,
                Health = Health,
                IsAlive = IsAlive,
                AnimationState = AnimationState,
                CurrentAction = CurrentAction,
                InputMove = InputMove,
                InputLook = InputLook,
                Inputs = Inputs,
                CustomData = CustomData,
                _customValues = _customValues != null ? new Dictionary<string, object>(_customValues) : null
            };
        }
    }

    // =============================================================================
    // INPUT FLAGS
    // =============================================================================

    /// <summary>
    /// Bit flags for input state.
    /// </summary>
    [Flags]
    public enum InputFlags
    {
        None = 0,
        Jump = 1 << 0,
        Fire = 1 << 1,
        AltFire = 1 << 2,
        Reload = 1 << 3,
        Use = 1 << 4,
        Crouch = 1 << 5,
        Sprint = 1 << 6,
        Ability1 = 1 << 7,
        Ability2 = 1 << 8,
        Ability3 = 1 << 9,
        Ability4 = 1 << 10
    }

    // =============================================================================
    // REMOTE PLAYER
    // =============================================================================

    /// <summary>
    /// Represents a remote player with state history for interpolation.
    /// </summary>
    public class RemotePlayer
    {
        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Player ID.</summary>
        public string PlayerId { get; set; }

        /// <summary>Whether this player is the host.</summary>
        public bool IsHost { get; set; }

        /// <summary>Time when player joined.</summary>
        public float JoinTime { get; set; }

        /// <summary>Current state.</summary>
        public PlayerState CurrentState { get; private set; }

        /// <summary>Predicted state (ahead of current).</summary>
        public PlayerState PredictedState { get; private set; }

        /// <summary>Time since last state update.</summary>
        public float TimeSinceLastUpdate { get; private set; }

        /// <summary>Whether player is considered timed out.</summary>
        public bool IsTimedOut => TimeSinceLastUpdate > 5f;

        // =============================================================================
        // STATE HISTORY
        // =============================================================================

        private readonly List<PlayerState> _stateHistory = new List<PlayerState>();
        private const int MAX_HISTORY = 30;
        private const float INTERPOLATION_WINDOW = 0.2f;

        // =============================================================================
        // STATE MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Add a new state to history.
        /// </summary>
        public void AddState(PlayerState state)
        {
            // Ignore out-of-order packets
            if (CurrentState != null && state.Sequence <= CurrentState.Sequence)
            {
                return;
            }

            _stateHistory.Add(state);
            CurrentState = state;
            TimeSinceLastUpdate = 0f;

            // Limit history size
            while (_stateHistory.Count > MAX_HISTORY)
            {
                _stateHistory.RemoveAt(0);
            }

            // Update prediction
            UpdatePrediction();
        }

        /// <summary>
        /// Apply a state directly (for lag compensation).
        /// </summary>
        public void ApplyState(PlayerState state)
        {
            CurrentState = state;
        }

        /// <summary>
        /// Get state at a specific time (for interpolation).
        /// </summary>
        public PlayerState GetStateAtTime(double time)
        {
            if (_stateHistory.Count == 0) return null;
            if (_stateHistory.Count == 1) return _stateHistory[0];

            // Find surrounding states
            PlayerState before = null;
            PlayerState after = null;

            for (int i = 0; i < _stateHistory.Count; i++)
            {
                if (_stateHistory[i].Timestamp <= time)
                {
                    before = _stateHistory[i];
                }
                else if (after == null)
                {
                    after = _stateHistory[i];
                    break;
                }
            }

            // If no before state, return earliest
            if (before == null) return _stateHistory[0];

            // If no after state, extrapolate from before
            if (after == null)
            {
                float deltaTime = (float)(time - before.Timestamp);
                return before.Extrapolate(deltaTime);
            }

            // Interpolate between states
            float t = (float)((time - before.Timestamp) / (after.Timestamp - before.Timestamp));
            return PlayerState.Lerp(before, after, t);
        }

        /// <summary>
        /// Get interpolated state with render delay.
        /// </summary>
        public PlayerState GetInterpolatedState(double renderTime)
        {
            return GetStateAtTime(renderTime);
        }

        // =============================================================================
        // UPDATE
        // =============================================================================

        /// <summary>
        /// Update remote player (called every frame).
        /// </summary>
        public void Update(float deltaTime)
        {
            TimeSinceLastUpdate += deltaTime;

            // Update predicted state
            if (PredictedState != null)
            {
                PredictedState = PredictedState.Extrapolate(deltaTime);
            }
        }

        private void UpdatePrediction()
        {
            if (CurrentState == null) return;

            // Predict forward based on velocity
            PredictedState = CurrentState.Extrapolate(0.1f);
        }

        /// <summary>
        /// Clear state history.
        /// </summary>
        public void ClearHistory()
        {
            _stateHistory.Clear();
            CurrentState = null;
            PredictedState = null;
        }
    }

    // =============================================================================
    // PLAYER STATE MANAGER
    // =============================================================================

    /// <summary>
    /// Manages state for all players in a match.
    /// </summary>
    public class PlayerStateManager
    {
        private readonly Dictionary<string, PlayerState> _localStates = new Dictionary<string, PlayerState>();
        private readonly Dictionary<string, PlayerState> _remoteStates = new Dictionary<string, PlayerState>();

        /// <summary>
        /// Set local player state.
        /// </summary>
        public void SetLocalState<T>(string playerId, T state) where T : class
        {
            if (state is PlayerState playerState)
            {
                playerState.PlayerId = playerId;
                _localStates[playerId] = playerState;
            }
        }

        /// <summary>
        /// Get local player state.
        /// </summary>
        public PlayerState GetLocalState(string playerId)
        {
            _localStates.TryGetValue(playerId, out var state);
            return state;
        }

        /// <summary>
        /// Set remote player state.
        /// </summary>
        public void SetRemoteState(string playerId, PlayerState state)
        {
            state.PlayerId = playerId;
            _remoteStates[playerId] = state;
        }

        /// <summary>
        /// Get any player state.
        /// </summary>
        public T GetState<T>(string playerId) where T : class
        {
            if (_localStates.TryGetValue(playerId, out var local))
            {
                return local as T;
            }

            if (_remoteStates.TryGetValue(playerId, out var remote))
            {
                return remote as T;
            }

            return null;
        }

        /// <summary>
        /// Get all player states.
        /// </summary>
        public IEnumerable<PlayerState> GetAllStates()
        {
            foreach (var state in _localStates.Values)
                yield return state;

            foreach (var state in _remoteStates.Values)
                yield return state;
        }

        /// <summary>
        /// Remove a player's state.
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            _localStates.Remove(playerId);
            _remoteStates.Remove(playerId);
        }

        /// <summary>
        /// Clear all states.
        /// </summary>
        public void Clear()
        {
            _localStates.Clear();
            _remoteStates.Clear();
        }
    }
}

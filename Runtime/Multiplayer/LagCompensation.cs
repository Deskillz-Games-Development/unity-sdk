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
    /// Handles lag compensation, prediction, and reconciliation.
    /// </summary>
    public class LagCompensation
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        private readonly int _maxPredictionFrames;
        private const int LATENCY_SAMPLE_SIZE = 20;
        private const float MAX_EXTRAPOLATION_TIME = 0.25f;
        private const float RECONCILIATION_THRESHOLD = 0.01f;

        // =============================================================================
        // STATE
        // =============================================================================

        private readonly Queue<float> _latencySamples = new Queue<float>();
        private readonly List<InputSnapshot> _pendingInputs = new List<InputSnapshot>();
        private readonly List<PredictedState> _predictions = new List<PredictedState>();

        private float _averageLatency;
        private float _latencyVariance;
        private float _jitter;
        private int _inputSequence;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Average latency in milliseconds.</summary>
        public float AverageLatency => _averageLatency;

        /// <summary>Latency variance (stability indicator).</summary>
        public float LatencyVariance => _latencyVariance;

        /// <summary>Network jitter in milliseconds.</summary>
        public float Jitter => _jitter;

        /// <summary>One-way delay estimate (half RTT).</summary>
        public float OneWayDelay => _averageLatency / 2f;

        /// <summary>Recommended interpolation delay.</summary>
        public float RecommendedInterpolationDelay => (_averageLatency + _jitter * 2) / 1000f;

        /// <summary>Current prediction buffer size.</summary>
        public int PendingInputCount => _pendingInputs.Count;

        // =============================================================================
        // CONSTRUCTOR
        // =============================================================================

        public LagCompensation(int maxPredictionFrames = 10)
        {
            _maxPredictionFrames = maxPredictionFrames;
        }

        // =============================================================================
        // LATENCY TRACKING
        // =============================================================================

        /// <summary>
        /// Record a latency sample.
        /// </summary>
        public void RecordLatency(float latencyMs)
        {
            _latencySamples.Enqueue(latencyMs);

            while (_latencySamples.Count > LATENCY_SAMPLE_SIZE)
            {
                _latencySamples.Dequeue();
            }

            CalculateLatencyStats();
        }

        private void CalculateLatencyStats()
        {
            if (_latencySamples.Count == 0) return;

            // Calculate average
            float sum = 0;
            foreach (var sample in _latencySamples)
            {
                sum += sample;
            }
            _averageLatency = sum / _latencySamples.Count;

            // Calculate variance and jitter
            float varianceSum = 0;
            float lastSample = 0;
            float jitterSum = 0;
            int jitterCount = 0;

            foreach (var sample in _latencySamples)
            {
                float diff = sample - _averageLatency;
                varianceSum += diff * diff;

                if (lastSample > 0)
                {
                    jitterSum += Mathf.Abs(sample - lastSample);
                    jitterCount++;
                }
                lastSample = sample;
            }

            _latencyVariance = varianceSum / _latencySamples.Count;
            _jitter = jitterCount > 0 ? jitterSum / jitterCount : 0;
        }

        /// <summary>
        /// Get network quality rating (0-1).
        /// </summary>
        public float GetNetworkQuality()
        {
            // Based on latency and jitter
            float latencyScore = Mathf.Clamp01(1f - _averageLatency / 200f);
            float jitterScore = Mathf.Clamp01(1f - _jitter / 50f);

            return (latencyScore + jitterScore) / 2f;
        }

        /// <summary>
        /// Get network quality description.
        /// </summary>
        public string GetNetworkQualityText()
        {
            float quality = GetNetworkQuality();
            if (quality > 0.8f) return "Excellent";
            if (quality > 0.6f) return "Good";
            if (quality > 0.4f) return "Fair";
            if (quality > 0.2f) return "Poor";
            return "Bad";
        }

        // =============================================================================
        // CLIENT-SIDE PREDICTION
        // =============================================================================

        /// <summary>
        /// Record an input for prediction.
        /// </summary>
        public int RecordInput(PlayerState state, InputFlags inputs, Vector2 moveInput)
        {
            var snapshot = new InputSnapshot
            {
                Sequence = _inputSequence++,
                Timestamp = Time.time,
                State = state.Clone(),
                Inputs = inputs,
                MoveInput = moveInput
            };

            _pendingInputs.Add(snapshot);

            // Limit pending inputs
            while (_pendingInputs.Count > _maxPredictionFrames * 2)
            {
                _pendingInputs.RemoveAt(0);
            }

            return snapshot.Sequence;
        }

        /// <summary>
        /// Apply predicted movement locally.
        /// </summary>
        public PlayerState PredictState(PlayerState currentState, InputFlags inputs, Vector2 moveInput, float deltaTime)
        {
            var predicted = currentState.Clone();

            // Apply movement prediction
            Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
            float speed = 5f; // Base speed - should be configured

            if ((inputs & InputFlags.Sprint) != 0)
            {
                speed *= 1.5f;
            }

            predicted.Position += movement * speed * deltaTime;
            predicted.Velocity = movement * speed;

            // Apply input state
            predicted.InputMove = moveInput;
            predicted.Inputs = inputs;

            // Store prediction
            _predictions.Add(new PredictedState
            {
                Sequence = _inputSequence,
                State = predicted,
                Timestamp = Time.time
            });

            // Limit predictions
            while (_predictions.Count > _maxPredictionFrames * 2)
            {
                _predictions.RemoveAt(0);
            }

            return predicted;
        }

        /// <summary>
        /// Reconcile with server state.
        /// </summary>
        public PlayerState Reconcile(PlayerState serverState, int lastProcessedInput)
        {
            // Remove acknowledged inputs
            _pendingInputs.RemoveAll(i => i.Sequence <= lastProcessedInput);

            // Check if server state matches our prediction
            var prediction = _predictions.Find(p => p.Sequence == lastProcessedInput);
            if (prediction != null)
            {
                float posError = Vector3.Distance(serverState.Position, prediction.State.Position);

                if (posError > RECONCILIATION_THRESHOLD)
                {
                    // Significant error - need to reconcile
                    DeskillzLogger.Debug($"Reconciling: error = {posError:F3}");

                    // Start from server state and re-apply pending inputs
                    var reconciledState = serverState.Clone();

                    foreach (var input in _pendingInputs)
                    {
                        reconciledState = ApplyInput(reconciledState, input);
                    }

                    return reconciledState;
                }
            }

            // No reconciliation needed, return current prediction
            return null;
        }

        private PlayerState ApplyInput(PlayerState state, InputSnapshot input)
        {
            // Re-apply input to state
            Vector3 movement = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);
            float speed = 5f;

            if ((input.Inputs & InputFlags.Sprint) != 0)
            {
                speed *= 1.5f;
            }

            float deltaTime = 1f / 60f; // Assume fixed timestep
            state.Position += movement * speed * deltaTime;
            state.Velocity = movement * speed;

            return state;
        }

        // =============================================================================
        // SERVER-SIDE LAG COMPENSATION
        // =============================================================================

        /// <summary>
        /// Calculate the time offset for lag compensation.
        /// </summary>
        public float GetLagCompensationTime(float clientTimestamp)
        {
            // Account for latency - look back in time
            return (float)(Time.time - clientTimestamp - OneWayDelay / 1000f);
        }

        /// <summary>
        /// Check if a hit was valid considering lag.
        /// </summary>
        public bool ValidateHit(Vector3 shooterPosition, Vector3 targetPosition, float clientTimestamp, float maxRange)
        {
            float distance = Vector3.Distance(shooterPosition, targetPosition);

            // Account for position uncertainty due to lag
            float uncertainty = (_averageLatency / 1000f) * 5f; // Assume 5 units/sec max speed

            return distance <= maxRange + uncertainty;
        }

        // =============================================================================
        // INTERPOLATION HELPERS
        // =============================================================================

        /// <summary>
        /// Get the optimal render delay for interpolation.
        /// </summary>
        public float GetOptimalRenderDelay()
        {
            // 2x jitter plus one packet interval
            float packetInterval = 1f / 20f; // Assuming 20 Hz sync rate
            return ((_jitter * 2) / 1000f) + packetInterval;
        }

        /// <summary>
        /// Calculate interpolation factor between two timestamps.
        /// </summary>
        public float GetInterpolationFactor(double fromTime, double toTime, double renderTime)
        {
            if (toTime <= fromTime) return 1f;
            return Mathf.Clamp01((float)((renderTime - fromTime) / (toTime - fromTime)));
        }

        /// <summary>
        /// Should extrapolate (no recent data).
        /// </summary>
        public bool ShouldExtrapolate(float timeSinceLastUpdate)
        {
            float threshold = ((_averageLatency * 2) + _jitter) / 1000f;
            return timeSinceLastUpdate > threshold && timeSinceLastUpdate < MAX_EXTRAPOLATION_TIME;
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        /// <summary>
        /// Clear all prediction data.
        /// </summary>
        public void Clear()
        {
            _pendingInputs.Clear();
            _predictions.Clear();
            _latencySamples.Clear();
            _inputSequence = 0;
            _averageLatency = 0;
            _latencyVariance = 0;
            _jitter = 0;
        }
    }

    // =============================================================================
    // INPUT SNAPSHOT
    // =============================================================================

    /// <summary>
    /// Snapshot of input at a specific time.
    /// </summary>
    internal class InputSnapshot
    {
        public int Sequence;
        public float Timestamp;
        public PlayerState State;
        public InputFlags Inputs;
        public Vector2 MoveInput;
    }

    /// <summary>
    /// Predicted state at a specific sequence.
    /// </summary>
    internal class PredictedState
    {
        public int Sequence;
        public PlayerState State;
        public float Timestamp;
    }

    // =============================================================================
    // NETWORK STATS
    // =============================================================================

    /// <summary>
    /// Network statistics for debugging/display.
    /// </summary>
    public class NetworkStats
    {
        public float Latency { get; set; }
        public float Jitter { get; set; }
        public float PacketLoss { get; set; }
        public int BytesSent { get; set; }
        public int BytesReceived { get; set; }
        public int MessagesSent { get; set; }
        public int MessagesReceived { get; set; }
        public float Quality { get; set; }

        /// <summary>
        /// Get formatted stats string.
        /// </summary>
        public override string ToString()
        {
            return $"Ping: {Latency:F0}ms | Jitter: {Jitter:F0}ms | Loss: {PacketLoss:P0} | Quality: {Quality:P0}";
        }
    }

    // =============================================================================
    // SMOOTH SYNC COMPONENT
    // =============================================================================

    /// <summary>
    /// Component for smooth network synchronization of transforms.
    /// Attach to networked GameObjects.
    /// </summary>
    public class NetworkTransform : MonoBehaviour
    {
        [Header("Sync Settings")]
        [SerializeField] private bool _syncPosition = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncScale = false;

        [Header("Interpolation")]
        [SerializeField] private float _interpolationSpeed = 15f;
        [SerializeField] private float _snapThreshold = 3f;

        [Header("Prediction")]
        [SerializeField] private bool _enablePrediction = true;
        [SerializeField] private float _predictionSmoothTime = 0.1f;

        // State
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;
        private Vector3 _velocity;
        private bool _isLocalPlayer;
        private string _playerId;

        /// <summary>
        /// Initialize for a player.
        /// </summary>
        public void Initialize(string playerId, bool isLocal)
        {
            _playerId = playerId;
            _isLocalPlayer = isLocal;
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetScale = transform.localScale;
        }

        /// <summary>
        /// Set network target transform.
        /// </summary>
        public void SetTarget(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            _targetPosition = position;
            _targetRotation = rotation;
            _velocity = velocity;
        }

        /// <summary>
        /// Set target from player state.
        /// </summary>
        public void SetTarget(PlayerState state)
        {
            if (state == null) return;

            _targetPosition = state.Position;
            _targetRotation = Quaternion.Euler(state.Rotation);
            _velocity = state.Velocity;
        }

        private void Update()
        {
            if (_isLocalPlayer) return;

            // Interpolate position
            if (_syncPosition)
            {
                float distance = Vector3.Distance(transform.position, _targetPosition);

                if (distance > _snapThreshold)
                {
                    // Snap if too far
                    transform.position = _targetPosition;
                }
                else
                {
                    // Smooth interpolation with prediction
                    Vector3 predictedTarget = _targetPosition;
                    if (_enablePrediction)
                    {
                        predictedTarget += _velocity * _predictionSmoothTime;
                    }

                    transform.position = Vector3.Lerp(
                        transform.position,
                        predictedTarget,
                        Time.deltaTime * _interpolationSpeed
                    );
                }
            }

            // Interpolate rotation
            if (_syncRotation)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    _targetRotation,
                    Time.deltaTime * _interpolationSpeed
                );
            }

            // Interpolate scale
            if (_syncScale)
            {
                transform.localScale = Vector3.Lerp(
                    transform.localScale,
                    _targetScale,
                    Time.deltaTime * _interpolationSpeed
                );
            }
        }

        /// <summary>
        /// Get current state for sending.
        /// </summary>
        public PlayerState GetState()
        {
            return new PlayerState
            {
                PlayerId = _playerId,
                Position = transform.position,
                Rotation = transform.rotation.eulerAngles,
                Velocity = _velocity
            };
        }
    }
}

// =============================================================================
// Deskillz SDK for Unity - Phase 3: Score System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Deskillz.Match;

namespace Deskillz.Score
{
    /// <summary>
    /// Client-side score validation and anti-cheat detection.
    /// Validates score changes and detects suspicious patterns.
    /// </summary>
    public class ScoreValidator
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        /// <summary>Maximum allowed score per second.</summary>
        public int MaxScorePerSecond { get; set; } = 1000;

        /// <summary>Maximum single score increase.</summary>
        public int MaxSingleIncrease { get; set; } = 10000;

        /// <summary>Minimum time between score changes (seconds).</summary>
        public float MinTimeBetweenChanges { get; set; } = 0.01f;

        /// <summary>Maximum score decreases allowed (for games that don't allow decreases).</summary>
        public int MaxDecreases { get; set; } = -1; // -1 = unlimited

        /// <summary>Whether negative scores are allowed.</summary>
        public bool AllowNegativeScores { get; set; } = false;

        /// <summary>Maximum total score allowed.</summary>
        public int MaxTotalScore { get; set; } = int.MaxValue;

        /// <summary>Violation threshold before flagging as suspicious.</summary>
        public int ViolationThreshold { get; set; } = 3;

        // =============================================================================
        // STATE
        // =============================================================================

        private MatchData _matchData;
        private bool _isTracking;
        private float _trackingStartTime;
        private int _lastScore;
        private float _lastScoreTime;
        private int _violationCount;
        private int _decreaseCount;
        private int _inputCount;
        private int _actionCount;

        private readonly List<ScoreChange> _changeHistory = new List<ScoreChange>();
        private readonly List<Violation> _violations = new List<Violation>();

        // Rate limiting
        private readonly Queue<float> _recentChangeTimes = new Queue<float>();
        private const int RATE_LIMIT_WINDOW = 10; // Consider last N changes
        private const float RATE_LIMIT_PERIOD = 1f; // Over this many seconds

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Whether validation is active.</summary>
        public bool IsTracking => _isTracking;

        /// <summary>Number of violations detected.</summary>
        public int ViolationCount => _violationCount;

        /// <summary>Whether suspicious activity has been detected.</summary>
        public bool IsSuspicious => _violationCount >= ViolationThreshold;

        /// <summary>All detected violations.</summary>
        public IReadOnlyList<Violation> Violations => _violations.AsReadOnly();

        /// <summary>Total input count during match.</summary>
        public int InputCount => _inputCount;

        /// <summary>Total action count during match.</summary>
        public int ActionCount => _actionCount;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize validator with match data.
        /// </summary>
        public void Initialize(MatchData matchData)
        {
            _matchData = matchData;
            Reset();

            // Adjust limits based on match type
            if (matchData != null)
            {
                AdjustLimitsForMatch(matchData);
            }

            DeskillzLogger.Debug("ScoreValidator initialized");
        }

        private void AdjustLimitsForMatch(MatchData match)
        {
            // Could adjust limits based on game type, time limit, etc.
            if (match.TimeLimitSeconds > 0)
            {
                // For timed matches, cap max score based on time
                var maxPossible = MaxScorePerSecond * match.TimeLimitSeconds;
                if (MaxTotalScore > maxPossible * 1.5f) // 50% buffer
                {
                    MaxTotalScore = (int)(maxPossible * 1.5f);
                }
            }
        }

        /// <summary>
        /// Start tracking score changes.
        /// </summary>
        public void StartTracking()
        {
            _isTracking = true;
            _trackingStartTime = Time.realtimeSinceStartup;
            _lastScoreTime = _trackingStartTime;

            DeskillzLogger.Debug("Score tracking started");
        }

        /// <summary>
        /// Stop tracking.
        /// </summary>
        public void StopTracking()
        {
            _isTracking = false;
            DeskillzLogger.Debug($"Score tracking stopped. Violations: {_violationCount}");
        }

        // =============================================================================
        // VALIDATION
        // =============================================================================

        /// <summary>
        /// Validate a score change.
        /// </summary>
        public bool ValidateScoreChange(int currentScore, int newScore, out string error)
        {
            error = null;

            // Basic range checks
            if (newScore < 0 && !AllowNegativeScores)
            {
                error = "Negative scores not allowed";
                RecordViolation(ViolationType.NegativeScore, currentScore, newScore);
                return false;
            }

            if (newScore > MaxTotalScore)
            {
                error = $"Score exceeds maximum ({MaxTotalScore})";
                RecordViolation(ViolationType.ExceedsMaximum, currentScore, newScore);
                return false;
            }

            // Skip further checks if not tracking
            if (!_isTracking)
            {
                return true;
            }

            var now = Time.realtimeSinceStartup;
            var delta = newScore - currentScore;
            var timeSinceLastChange = now - _lastScoreTime;

            // Time-based checks
            if (timeSinceLastChange < MinTimeBetweenChanges)
            {
                error = "Score changes too fast";
                RecordViolation(ViolationType.TooFast, currentScore, newScore);
                return false;
            }

            // Rate check
            if (!CheckScoreRate(delta, timeSinceLastChange, out error))
            {
                RecordViolation(ViolationType.ExceedsRate, currentScore, newScore);
                return false;
            }

            // Single increase check
            if (delta > 0 && delta > MaxSingleIncrease)
            {
                error = $"Single increase too large ({delta} > {MaxSingleIncrease})";
                RecordViolation(ViolationType.LargeIncrease, currentScore, newScore);
                return false;
            }

            // Decrease check
            if (delta < 0)
            {
                _decreaseCount++;
                if (MaxDecreases >= 0 && _decreaseCount > MaxDecreases)
                {
                    error = "Too many score decreases";
                    RecordViolation(ViolationType.TooManyDecreases, currentScore, newScore);
                    return false;
                }
            }

            // Record the change
            RecordChange(currentScore, newScore, now);

            _lastScore = newScore;
            _lastScoreTime = now;

            return true;
        }

        private bool CheckScoreRate(int delta, float timeDelta, out string error)
        {
            error = null;

            if (delta <= 0 || timeDelta <= 0)
            {
                return true;
            }

            var rate = delta / timeDelta;
            if (rate > MaxScorePerSecond)
            {
                error = $"Score rate too high ({rate:F0}/s > {MaxScorePerSecond}/s)";
                return false;
            }

            // Check sustained rate over recent history
            _recentChangeTimes.Enqueue(Time.realtimeSinceStartup);
            while (_recentChangeTimes.Count > RATE_LIMIT_WINDOW)
            {
                _recentChangeTimes.Dequeue();
            }

            if (_recentChangeTimes.Count >= RATE_LIMIT_WINDOW)
            {
                var oldestTime = _recentChangeTimes.Peek();
                var windowDuration = Time.realtimeSinceStartup - oldestTime;
                
                if (windowDuration < RATE_LIMIT_PERIOD)
                {
                    // Too many changes in short period
                    error = "Score changing too frequently";
                    return false;
                }
            }

            return true;
        }

        // =============================================================================
        // INPUT TRACKING
        // =============================================================================

        /// <summary>
        /// Record a player input (for validation correlation).
        /// </summary>
        public void RecordInput()
        {
            if (!_isTracking) return;
            _inputCount++;
        }

        /// <summary>
        /// Record a game action (for validation correlation).
        /// </summary>
        public void RecordAction()
        {
            if (!_isTracking) return;
            _actionCount++;
        }

        /// <summary>
        /// Validate that score correlates with inputs/actions.
        /// </summary>
        public bool ValidateInputCorrelation(int score, out string warning)
        {
            warning = null;

            if (!_isTracking || _inputCount == 0)
            {
                return true;
            }

            // Check if score is suspiciously high relative to inputs
            var scorePerInput = (float)score / _inputCount;
            if (scorePerInput > MaxScorePerSecond)
            {
                warning = $"Score per input unusually high ({scorePerInput:F0})";
                return false;
            }

            return true;
        }

        // =============================================================================
        // VIOLATION TRACKING
        // =============================================================================

        private void RecordViolation(ViolationType type, int fromScore, int toScore)
        {
            var violation = new Violation
            {
                Type = type,
                FromScore = fromScore,
                ToScore = toScore,
                Timestamp = DateTime.UtcNow,
                GameTime = MatchController.Instance?.TimeElapsed ?? 0f
            };

            _violations.Add(violation);
            _violationCount++;

            DeskillzLogger.Warning($"Score violation: {type} ({fromScore} -> {toScore})");

            // Check if we've hit the threshold
            if (_violationCount == ViolationThreshold)
            {
                DeskillzLogger.Error("Suspicious score activity threshold reached");
            }
        }

        private void RecordChange(int fromScore, int toScore, float time)
        {
            var change = new ScoreChange
            {
                FromScore = fromScore,
                ToScore = toScore,
                Delta = toScore - fromScore,
                Time = time,
                GameTime = MatchController.Instance?.TimeElapsed ?? 0f
            };

            _changeHistory.Add(change);

            // Limit history size
            while (_changeHistory.Count > 1000)
            {
                _changeHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Check if there's suspicious activity.
        /// </summary>
        public bool IsSuspiciousActivity()
        {
            if (_violations.Count >= ViolationThreshold)
            {
                return true;
            }

            // Check for patterns
            if (HasSuspiciousPattern())
            {
                return true;
            }

            return false;
        }

        private bool HasSuspiciousPattern()
        {
            if (_changeHistory.Count < 10)
            {
                return false;
            }

            // Check for unnaturally consistent increases
            int consistentCount = 0;
            int lastDelta = 0;

            for (int i = _changeHistory.Count - 10; i < _changeHistory.Count; i++)
            {
                if (i < 0) continue;

                var delta = _changeHistory[i].Delta;
                if (delta == lastDelta && delta > 0)
                {
                    consistentCount++;
                }
                lastDelta = delta;
            }

            // If all recent changes are exactly the same, suspicious
            if (consistentCount >= 8)
            {
                DeskillzLogger.Warning("Suspicious pattern: identical score increases");
                return true;
            }

            return false;
        }

        // =============================================================================
        // VALIDATION DATA GENERATION
        // =============================================================================

        /// <summary>
        /// Generate validation data for server submission.
        /// </summary>
        public ValidationData GenerateValidationData(SecureScorePayload payload)
        {
            var elapsed = _isTracking ? Time.realtimeSinceStartup - _trackingStartTime : 0f;

            return new ValidationData
            {
                ScoreHash = ScoreEncryption.Hash($"{payload.Score}:{payload.Timestamp}:{payload.MatchId}"),
                StateHash = GenerateStateHash(),
                InputCount = _inputCount,
                PlayTime = elapsed,
                ActionCount = _actionCount,
                DeviceFingerprint = GenerateDeviceFingerprint()
            };
        }

        private string GenerateStateHash()
        {
            // Create hash of current validation state
            var state = $"{_violationCount}:{_changeHistory.Count}:{_inputCount}:{_actionCount}";
            return ScoreEncryption.Hash(state);
        }

        private string GenerateDeviceFingerprint()
        {
            // Generate a device identifier (not personally identifiable)
            var components = new[]
            {
                SystemInfo.deviceModel,
                SystemInfo.operatingSystem,
                SystemInfo.graphicsDeviceName,
                Screen.width.ToString(),
                Screen.height.ToString()
            };

            return ScoreEncryption.Hash(string.Join(":", components));
        }

        // =============================================================================
        // ANALYSIS
        // =============================================================================

        /// <summary>
        /// Get a summary of score behavior.
        /// </summary>
        public ScoreBehaviorSummary GetBehaviorSummary()
        {
            var summary = new ScoreBehaviorSummary
            {
                TotalChanges = _changeHistory.Count,
                ViolationCount = _violationCount,
                DecreaseCount = _decreaseCount,
                InputCount = _inputCount,
                ActionCount = _actionCount,
                IsSuspicious = IsSuspiciousActivity()
            };

            if (_changeHistory.Count > 0)
            {
                // Calculate statistics
                int totalIncrease = 0;
                int maxSingleIncrease = 0;
                float totalTime = 0;

                foreach (var change in _changeHistory)
                {
                    if (change.Delta > 0)
                    {
                        totalIncrease += change.Delta;
                        if (change.Delta > maxSingleIncrease)
                        {
                            maxSingleIncrease = change.Delta;
                        }
                    }
                }

                if (_changeHistory.Count > 1)
                {
                    totalTime = _changeHistory[_changeHistory.Count - 1].Time - _changeHistory[0].Time;
                }

                summary.TotalScoreIncrease = totalIncrease;
                summary.MaxSingleIncrease = maxSingleIncrease;
                summary.AverageRate = totalTime > 0 ? totalIncrease / totalTime : 0;
            }

            return summary;
        }

        // =============================================================================
        // RESET
        // =============================================================================

        /// <summary>
        /// Reset validator state.
        /// </summary>
        public void Reset()
        {
            _isTracking = false;
            _lastScore = 0;
            _lastScoreTime = 0;
            _violationCount = 0;
            _decreaseCount = 0;
            _inputCount = 0;
            _actionCount = 0;
            _changeHistory.Clear();
            _violations.Clear();
            _recentChangeTimes.Clear();

            DeskillzLogger.Debug("ScoreValidator reset");
        }
    }

    // =============================================================================
    // DATA STRUCTURES
    // =============================================================================

    /// <summary>
    /// Types of validation violations.
    /// </summary>
    public enum ViolationType
    {
        NegativeScore,
        ExceedsMaximum,
        ExceedsRate,
        TooFast,
        LargeIncrease,
        TooManyDecreases,
        SuspiciousPattern,
        InputMismatch
    }

    /// <summary>
    /// Recorded violation.
    /// </summary>
    public class Violation
    {
        public ViolationType Type { get; set; }
        public int FromScore { get; set; }
        public int ToScore { get; set; }
        public DateTime Timestamp { get; set; }
        public float GameTime { get; set; }

        public override string ToString()
        {
            return $"{Type}: {FromScore} -> {ToScore} at {GameTime:F2}s";
        }
    }

    /// <summary>
    /// Recorded score change.
    /// </summary>
    internal class ScoreChange
    {
        public int FromScore { get; set; }
        public int ToScore { get; set; }
        public int Delta { get; set; }
        public float Time { get; set; }
        public float GameTime { get; set; }
    }

    /// <summary>
    /// Summary of score behavior for analysis.
    /// </summary>
    public class ScoreBehaviorSummary
    {
        public int TotalChanges { get; set; }
        public int ViolationCount { get; set; }
        public int DecreaseCount { get; set; }
        public int InputCount { get; set; }
        public int ActionCount { get; set; }
        public int TotalScoreIncrease { get; set; }
        public int MaxSingleIncrease { get; set; }
        public float AverageRate { get; set; }
        public bool IsSuspicious { get; set; }
    }
}

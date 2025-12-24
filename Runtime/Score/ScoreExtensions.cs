// =============================================================================
// Deskillz SDK for Unity - Phase 3: Score System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz.Score
{
    /// <summary>
    /// Extension methods and utilities for score system.
    /// </summary>
    public static class ScoreExtensions
    {
        // =============================================================================
        // SCORE MANAGER EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Submit score with combo multiplier.
        /// </summary>
        public static void AddComboScore(this ScoreManager manager, int basePoints, int comboCount)
        {
            var multiplier = 1f + (comboCount * 0.1f);
            var points = Mathf.RoundToInt(basePoints * multiplier);
            manager.AddScore(points);
        }

        /// <summary>
        /// Submit time-bonus score.
        /// </summary>
        public static void AddTimeBonusScore(this ScoreManager manager, int basePoints, float timeRemaining, float maxTime)
        {
            if (maxTime <= 0) return;
            var timeBonus = Mathf.Clamp01(timeRemaining / maxTime);
            var points = Mathf.RoundToInt(basePoints * (1f + timeBonus));
            manager.AddScore(points);
        }

        /// <summary>
        /// Add score with streak bonus.
        /// </summary>
        public static void AddStreakScore(this ScoreManager manager, int basePoints, int streakCount)
        {
            var streakMultiplier = Mathf.Min(streakCount, 10); // Cap at 10x
            var points = basePoints * streakMultiplier;
            manager.AddScore(points);
        }
    }

    // =============================================================================
    // PROTECTED SCORE
    // =============================================================================

    /// <summary>
    /// Memory-protected score value.
    /// Obfuscates score in memory to prevent memory editing cheats.
    /// </summary>
    [Serializable]
    public struct ProtectedScore
    {
        private int _obfuscatedValue;
        private int _key;
        private int _checksum;

        /// <summary>
        /// Create a new protected score.
        /// </summary>
        public ProtectedScore(int value)
        {
            _key = ScoreEncryption.GenerateObfuscationKey();
            _obfuscatedValue = ScoreEncryption.ObfuscateScore(value, _key);
            _checksum = CalculateChecksum(value, _key);
        }

        /// <summary>
        /// Get the actual score value.
        /// </summary>
        public int Value
        {
            get
            {
                var value = ScoreEncryption.DeobfuscateScore(_obfuscatedValue, _key);
                
                // Verify checksum
                if (_checksum != CalculateChecksum(value, _key))
                {
                    DeskillzLogger.Error("Score tampering detected!");
                    DeskillzEvents.RaiseAntiCheatViolation(
                        new DeskillzError(ErrorCode.AntiCheatViolation, "Score memory tampering detected"));
                    return 0;
                }
                
                return value;
            }
            set
            {
                _key = ScoreEncryption.GenerateObfuscationKey();
                _obfuscatedValue = ScoreEncryption.ObfuscateScore(value, _key);
                _checksum = CalculateChecksum(value, _key);
            }
        }

        private static int CalculateChecksum(int value, int key)
        {
            return (value * 31) ^ (key * 17) ^ 0x5A5A5A5A;
        }

        // Implicit conversions
        public static implicit operator int(ProtectedScore ps) => ps.Value;
        public static implicit operator ProtectedScore(int value) => new ProtectedScore(value);

        // Operators
        public static ProtectedScore operator +(ProtectedScore a, int b) => new ProtectedScore(a.Value + b);
        public static ProtectedScore operator -(ProtectedScore a, int b) => new ProtectedScore(a.Value - b);
        public static ProtectedScore operator *(ProtectedScore a, int b) => new ProtectedScore(a.Value * b);
        public static ProtectedScore operator /(ProtectedScore a, int b) => new ProtectedScore(a.Value / b);

        public static bool operator ==(ProtectedScore a, ProtectedScore b) => a.Value == b.Value;
        public static bool operator !=(ProtectedScore a, ProtectedScore b) => a.Value != b.Value;
        public static bool operator <(ProtectedScore a, ProtectedScore b) => a.Value < b.Value;
        public static bool operator >(ProtectedScore a, ProtectedScore b) => a.Value > b.Value;
        public static bool operator <=(ProtectedScore a, ProtectedScore b) => a.Value <= b.Value;
        public static bool operator >=(ProtectedScore a, ProtectedScore b) => a.Value >= b.Value;

        public override bool Equals(object obj)
        {
            if (obj is ProtectedScore ps) return Value == ps.Value;
            if (obj is int i) return Value == i;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    // =============================================================================
    // PROTECTED FLOAT (for multipliers, etc.)
    // =============================================================================

    /// <summary>
    /// Memory-protected float value.
    /// </summary>
    [Serializable]
    public struct ProtectedFloat
    {
        private long _obfuscatedBits;
        private int _key;

        public ProtectedFloat(float value)
        {
            _key = ScoreEncryption.GenerateObfuscationKey();
            var bits = BitConverter.DoubleToInt64Bits(value);
            _obfuscatedBits = bits ^ _key ^ ((long)_key << 32);
        }

        public float Value
        {
            get
            {
                var bits = _obfuscatedBits ^ _key ^ ((long)_key << 32);
                return (float)BitConverter.Int64BitsToDouble(bits);
            }
            set
            {
                _key = ScoreEncryption.GenerateObfuscationKey();
                var bits = BitConverter.DoubleToInt64Bits(value);
                _obfuscatedBits = bits ^ _key ^ ((long)_key << 32);
            }
        }

        public static implicit operator float(ProtectedFloat pf) => pf.Value;
        public static implicit operator ProtectedFloat(float value) => new ProtectedFloat(value);

        public override string ToString() => Value.ToString("F2");
    }

    // =============================================================================
    // SCORE DISPLAY HELPERS
    // =============================================================================

    /// <summary>
    /// Utilities for displaying scores.
    /// </summary>
    public static class ScoreDisplay
    {
        /// <summary>
        /// Format score with thousands separator.
        /// </summary>
        public static string Format(int score)
        {
            return score.ToString("N0");
        }

        /// <summary>
        /// Format score with suffix (K, M, B).
        /// </summary>
        public static string FormatCompact(int score)
        {
            if (score >= 1_000_000_000)
                return $"{score / 1_000_000_000f:F1}B";
            if (score >= 1_000_000)
                return $"{score / 1_000_000f:F1}M";
            if (score >= 1_000)
                return $"{score / 1_000f:F1}K";
            return score.ToString();
        }

        /// <summary>
        /// Format score with leading zeros.
        /// </summary>
        public static string FormatPadded(int score, int digits = 8)
        {
            return score.ToString($"D{digits}");
        }

        /// <summary>
        /// Format score change with +/- sign.
        /// </summary>
        public static string FormatDelta(int delta)
        {
            if (delta > 0) return $"+{delta:N0}";
            if (delta < 0) return delta.ToString("N0");
            return "0";
        }

        /// <summary>
        /// Format score with animation-friendly digits.
        /// Returns array of individual digit strings.
        /// </summary>
        public static string[] GetDigits(int score, int minDigits = 1)
        {
            var scoreStr = score.ToString();
            var padded = scoreStr.PadLeft(minDigits, '0');
            var digits = new string[padded.Length];
            
            for (int i = 0; i < padded.Length; i++)
            {
                digits[i] = padded[i].ToString();
            }
            
            return digits;
        }

        /// <summary>
        /// Get ordinal string (1st, 2nd, 3rd, etc.).
        /// </summary>
        public static string GetOrdinal(int rank)
        {
            if (rank <= 0) return rank.ToString();

            var suffix = (rank % 100) switch
            {
                11 or 12 or 13 => "th",
                _ => (rank % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th"
                }
            };

            return $"{rank}{suffix}";
        }
    }

    // =============================================================================
    // SCORE ANIMATION HELPERS
    // =============================================================================

    /// <summary>
    /// Helper for animating score changes.
    /// </summary>
    public class ScoreAnimator
    {
        private float _displayedScore;
        private float _targetScore;
        private float _velocity;

        /// <summary>Current displayed score value.</summary>
        public int DisplayedScore => Mathf.RoundToInt(_displayedScore);

        /// <summary>Target score value.</summary>
        public int TargetScore => Mathf.RoundToInt(_targetScore);

        /// <summary>Whether animation is complete.</summary>
        public bool IsComplete => Mathf.Approximately(_displayedScore, _targetScore);

        /// <summary>
        /// Set a new target score.
        /// </summary>
        public void SetTarget(int score)
        {
            _targetScore = score;
        }

        /// <summary>
        /// Instantly set score (no animation).
        /// </summary>
        public void SetImmediate(int score)
        {
            _displayedScore = score;
            _targetScore = score;
            _velocity = 0;
        }

        /// <summary>
        /// Update animation. Call from Update().
        /// </summary>
        /// <param name="smoothTime">Smoothing time (lower = faster)</param>
        public void Update(float smoothTime = 0.3f)
        {
            _displayedScore = Mathf.SmoothDamp(_displayedScore, _targetScore, ref _velocity, smoothTime);
        }

        /// <summary>
        /// Update with speed-based animation.
        /// </summary>
        /// <param name="speed">Points per second</param>
        public void UpdateLinear(float speed = 1000f)
        {
            var diff = _targetScore - _displayedScore;
            var maxDelta = speed * Time.deltaTime;

            if (Mathf.Abs(diff) <= maxDelta)
            {
                _displayedScore = _targetScore;
            }
            else
            {
                _displayedScore += Mathf.Sign(diff) * maxDelta;
            }
        }

        /// <summary>
        /// Get formatted displayed score.
        /// </summary>
        public string GetFormatted()
        {
            return ScoreDisplay.Format(DisplayedScore);
        }
    }

    // =============================================================================
    // SCORE MULTIPLIER
    // =============================================================================

    /// <summary>
    /// Manages score multipliers with decay.
    /// </summary>
    public class ScoreMultiplier
    {
        private float _baseMultiplier = 1f;
        private float _bonusMultiplier = 0f;
        private float _decayRate = 0.1f;
        private float _maxMultiplier = 10f;

        /// <summary>Current total multiplier.</summary>
        public float Current => Mathf.Min(_baseMultiplier + _bonusMultiplier, _maxMultiplier);

        /// <summary>Base multiplier (doesn't decay).</summary>
        public float Base => _baseMultiplier;

        /// <summary>Bonus multiplier (decays over time).</summary>
        public float Bonus => _bonusMultiplier;

        /// <summary>
        /// Add to bonus multiplier.
        /// </summary>
        public void AddBonus(float amount)
        {
            _bonusMultiplier = Mathf.Min(_bonusMultiplier + amount, _maxMultiplier - _baseMultiplier);
        }

        /// <summary>
        /// Set base multiplier.
        /// </summary>
        public void SetBase(float value)
        {
            _baseMultiplier = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Configure decay settings.
        /// </summary>
        public void Configure(float decayRate, float maxMultiplier)
        {
            _decayRate = decayRate;
            _maxMultiplier = maxMultiplier;
        }

        /// <summary>
        /// Update multiplier decay. Call from Update().
        /// </summary>
        public void Update()
        {
            if (_bonusMultiplier > 0)
            {
                _bonusMultiplier = Mathf.Max(0, _bonusMultiplier - _decayRate * Time.deltaTime);
            }
        }

        /// <summary>
        /// Apply multiplier to a score.
        /// </summary>
        public int Apply(int baseScore)
        {
            return Mathf.RoundToInt(baseScore * Current);
        }

        /// <summary>
        /// Reset to default.
        /// </summary>
        public void Reset()
        {
            _baseMultiplier = 1f;
            _bonusMultiplier = 0f;
        }
    }
}

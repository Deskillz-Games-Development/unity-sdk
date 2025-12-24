// =============================================================================
// Deskillz SDK for Unity - Phase 2: Match System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;

namespace Deskillz.Match
{
    /// <summary>
    /// Manages multi-round matches including score tracking per round.
    /// </summary>
    public class MatchRound
    {
        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when round changes.</summary>
        public event Action<int> OnRoundChanged;

        /// <summary>Fired when a round is completed.</summary>
        public event Action<int, int> OnRoundCompleted; // round, score

        /// <summary>Fired when all rounds are complete.</summary>
        public event Action OnAllRoundsComplete;

        // =============================================================================
        // STATE
        // =============================================================================

        private int _totalRounds = 1;
        private int _currentRound = 1;
        private List<RoundData> _rounds;
        private bool _isComplete;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Total number of rounds.</summary>
        public int TotalRounds => _totalRounds;

        /// <summary>Current round number (1-indexed).</summary>
        public int CurrentRound => _currentRound;

        /// <summary>Whether all rounds are complete.</summary>
        public bool IsComplete => _isComplete;

        /// <summary>Whether this is a multi-round match.</summary>
        public bool IsMultiRound => _totalRounds > 1;

        /// <summary>Number of completed rounds.</summary>
        public int CompletedRounds => _rounds?.FindAll(r => r.IsComplete).Count ?? 0;

        /// <summary>Rounds remaining.</summary>
        public int RoundsRemaining => _totalRounds - CompletedRounds;

        /// <summary>Total score across all completed rounds.</summary>
        public int TotalScore
        {
            get
            {
                if (_rounds == null) return 0;
                int total = 0;
                foreach (var round in _rounds)
                {
                    if (round.IsComplete) total += round.Score;
                }
                return total;
            }
        }

        /// <summary>Average score per round.</summary>
        public float AverageScore
        {
            get
            {
                var completed = CompletedRounds;
                if (completed == 0) return 0f;
                return (float)TotalScore / completed;
            }
        }

        /// <summary>Best score achieved in any round.</summary>
        public int BestRoundScore
        {
            get
            {
                if (_rounds == null) return 0;
                int best = 0;
                foreach (var round in _rounds)
                {
                    if (round.IsComplete && round.Score > best)
                        best = round.Score;
                }
                return best;
            }
        }

        /// <summary>All round data.</summary>
        public IReadOnlyList<RoundData> Rounds => _rounds?.AsReadOnly();

        /// <summary>Current round data.</summary>
        public RoundData CurrentRoundData
        {
            get
            {
                if (_rounds == null || _currentRound < 1 || _currentRound > _rounds.Count)
                    return null;
                return _rounds[_currentRound - 1];
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        /// <summary>
        /// Configure the round manager.
        /// </summary>
        public void Configure(int totalRounds)
        {
            _totalRounds = Math.Max(1, totalRounds);
            Reset();

            DeskillzLogger.Debug($"RoundManager configured: {_totalRounds} rounds");
        }

        // =============================================================================
        // CONTROL
        // =============================================================================

        /// <summary>
        /// Start the first round.
        /// </summary>
        public void StartFirstRound()
        {
            if (_rounds == null || _rounds.Count == 0)
            {
                DeskillzLogger.Error("RoundManager not configured");
                return;
            }

            _currentRound = 1;
            _rounds[0].StartTime = DateTime.UtcNow;
            
            OnRoundChanged?.Invoke(_currentRound);
            DeskillzLogger.Debug("Started round 1");
        }

        /// <summary>
        /// Start the next round.
        /// </summary>
        public void StartNextRound()
        {
            if (_isComplete)
            {
                DeskillzLogger.Warning("All rounds already complete");
                return;
            }

            if (_currentRound >= _totalRounds)
            {
                DeskillzLogger.Warning("No more rounds available");
                return;
            }

            _currentRound++;
            _rounds[_currentRound - 1].StartTime = DateTime.UtcNow;

            OnRoundChanged?.Invoke(_currentRound);
            DeskillzLogger.Debug($"Started round {_currentRound}");
        }

        /// <summary>
        /// Complete the current round with a score.
        /// </summary>
        public void CompleteRound(int score)
        {
            if (_isComplete)
            {
                DeskillzLogger.Warning("All rounds already complete");
                return;
            }

            var roundData = CurrentRoundData;
            if (roundData == null)
            {
                DeskillzLogger.Error("No current round to complete");
                return;
            }

            roundData.Score = score;
            roundData.EndTime = DateTime.UtcNow;
            roundData.IsComplete = true;

            DeskillzLogger.Debug($"Completed round {_currentRound} with score {score}");
            OnRoundCompleted?.Invoke(_currentRound, score);

            // Check if all rounds complete
            if (_currentRound >= _totalRounds)
            {
                _isComplete = true;
                OnAllRoundsComplete?.Invoke();
                DeskillzLogger.Debug("All rounds complete");
            }
        }

        /// <summary>
        /// Get score for a specific round.
        /// </summary>
        public int GetRoundScore(int round)
        {
            if (_rounds == null || round < 1 || round > _rounds.Count)
                return 0;
            return _rounds[round - 1].Score;
        }

        /// <summary>
        /// Get data for a specific round.
        /// </summary>
        public RoundData GetRoundData(int round)
        {
            if (_rounds == null || round < 1 || round > _rounds.Count)
                return null;
            return _rounds[round - 1];
        }

        /// <summary>
        /// Check if a specific round is complete.
        /// </summary>
        public bool IsRoundComplete(int round)
        {
            var data = GetRoundData(round);
            return data?.IsComplete ?? false;
        }

        // =============================================================================
        // SCORING
        // =============================================================================

        /// <summary>
        /// Update score for current round (in progress).
        /// </summary>
        public void UpdateCurrentRoundScore(int score)
        {
            var roundData = CurrentRoundData;
            if (roundData == null || roundData.IsComplete)
            {
                DeskillzLogger.Warning("Cannot update score: no active round");
                return;
            }

            roundData.Score = score;
        }

        /// <summary>
        /// Add to current round score.
        /// </summary>
        public void AddToCurrentRoundScore(int points)
        {
            var roundData = CurrentRoundData;
            if (roundData == null || roundData.IsComplete) return;

            roundData.Score += points;
        }

        // =============================================================================
        // STATISTICS
        // =============================================================================

        /// <summary>
        /// Get all completed round scores.
        /// </summary>
        public int[] GetCompletedScores()
        {
            if (_rounds == null) return Array.Empty<int>();

            var scores = new List<int>();
            foreach (var round in _rounds)
            {
                if (round.IsComplete)
                    scores.Add(round.Score);
            }
            return scores.ToArray();
        }

        /// <summary>
        /// Get score improvement from first to last round.
        /// </summary>
        public int GetScoreImprovement()
        {
            if (CompletedRounds < 2) return 0;
            
            int first = _rounds[0].Score;
            int last = 0;
            
            for (int i = _rounds.Count - 1; i >= 0; i--)
            {
                if (_rounds[i].IsComplete)
                {
                    last = _rounds[i].Score;
                    break;
                }
            }
            
            return last - first;
        }

        /// <summary>
        /// Get total time spent across all rounds.
        /// </summary>
        public TimeSpan GetTotalPlayTime()
        {
            if (_rounds == null) return TimeSpan.Zero;

            var total = TimeSpan.Zero;
            foreach (var round in _rounds)
            {
                total += round.Duration;
            }
            return total;
        }

        // =============================================================================
        // RESET
        // =============================================================================

        /// <summary>
        /// Reset all round data.
        /// </summary>
        public void Reset()
        {
            _currentRound = 1;
            _isComplete = false;

            _rounds = new List<RoundData>(_totalRounds);
            for (int i = 0; i < _totalRounds; i++)
            {
                _rounds.Add(new RoundData
                {
                    RoundNumber = i + 1,
                    Score = 0,
                    IsComplete = false
                });
            }

            DeskillzLogger.Debug("RoundManager reset");
        }
    }

    // =============================================================================
    // ROUND DATA
    // =============================================================================

    /// <summary>
    /// Data for a single round.
    /// </summary>
    public class RoundData
    {
        /// <summary>Round number (1-indexed).</summary>
        public int RoundNumber { get; set; }

        /// <summary>Score achieved in this round.</summary>
        public int Score { get; set; }

        /// <summary>Whether round is complete.</summary>
        public bool IsComplete { get; set; }

        /// <summary>When round started.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>When round ended.</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Round duration.</summary>
        public TimeSpan Duration
        {
            get
            {
                if (StartTime == default) return TimeSpan.Zero;
                var end = IsComplete ? EndTime : DateTime.UtcNow;
                return end - StartTime;
            }
        }

        /// <summary>Duration in seconds.</summary>
        public float DurationSeconds => (float)Duration.TotalSeconds;

        /// <summary>Custom data for this round.</summary>
        public Dictionary<string, object> CustomData { get; set; }

        public RoundData()
        {
            CustomData = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            var status = IsComplete ? $"Score: {Score}" : "In Progress";
            return $"Round {RoundNumber}: {status}";
        }
    }
}

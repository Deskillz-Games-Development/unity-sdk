// =============================================================================
// Deskillz SDK for Unity - Phase 3: Score System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deskillz.Match;

namespace Deskillz.Score
{
    /// <summary>
    /// Manages score tracking, validation, encryption, and submission.
    /// Provides secure score handling with anti-cheat measures.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static ScoreManager _instance;

        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ScoreManager>();
                    if (_instance == null && DeskillzManager.HasInstance)
                    {
                        _instance = DeskillzManager.Instance.gameObject.AddComponent<ScoreManager>();
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Score Settings")]
        [Tooltip("Minimum time between score submissions (seconds)")]
        [SerializeField] private float _minSubmissionInterval = 1f;

        [Tooltip("Maximum score submissions per match")]
        [SerializeField] private int _maxSubmissionsPerMatch = 100;

        [Tooltip("Auto-submit score on significant changes")]
        [SerializeField] private bool _autoSubmitOnChange = true;

        [Tooltip("Score change threshold for auto-submit")]
        [SerializeField] private int _autoSubmitThreshold = 100;

        [Tooltip("Enable score encryption")]
        [SerializeField] private bool _encryptScores = true;

        [Tooltip("Enable client-side validation")]
        [SerializeField] private bool _enableValidation = true;

        // =============================================================================
        // STATE
        // =============================================================================

        private int _currentScore;
        private int _lastSubmittedScore;
        private int _highScore;
        private int _submissionCount;
        private float _lastSubmissionTime;
        private bool _isSubmitting;

        private readonly List<ScoreEntry> _scoreHistory = new List<ScoreEntry>();
        private readonly Queue<ScoreSubmission> _pendingSubmissions = new Queue<ScoreSubmission>();

        private ScoreEncryption _encryption;
        private ScoreValidator _validator;
        private string _currentMatchId;
        private string _sessionToken;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current score.</summary>
        public int CurrentScore => _currentScore;

        /// <summary>Last score successfully submitted to server.</summary>
        public int LastSubmittedScore => _lastSubmittedScore;

        /// <summary>Highest score this match.</summary>
        public int HighScore => _highScore;

        /// <summary>Number of submissions this match.</summary>
        public int SubmissionCount => _submissionCount;

        /// <summary>Whether currently submitting a score.</summary>
        public bool IsSubmitting => _isSubmitting;

        /// <summary>Score history for this match.</summary>
        public IReadOnlyList<ScoreEntry> ScoreHistory => _scoreHistory.AsReadOnly();

        /// <summary>Number of pending submissions in queue.</summary>
        public int PendingSubmissions => _pendingSubmissions.Count;

        /// <summary>Whether encryption is enabled.</summary>
        public bool IsEncryptionEnabled => _encryptScores;

        /// <summary>Whether validation is enabled.</summary>
        public bool IsValidationEnabled => _enableValidation;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            InitializeComponents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Process pending submissions
            ProcessPendingSubmissions();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeComponents()
        {
            _encryption = new ScoreEncryption();
            _validator = new ScoreValidator();

            DeskillzLogger.Debug("ScoreManager initialized");
        }

        private void SubscribeToEvents()
        {
            DeskillzEvents.OnMatchReady += HandleMatchReady;
            DeskillzEvents.OnMatchStart += HandleMatchStart;
            DeskillzEvents.OnMatchComplete += HandleMatchComplete;
            DeskillzEvents.OnConnectionStateChanged += HandleConnectionChanged;
        }

        private void UnsubscribeFromEvents()
        {
            DeskillzEvents.OnMatchReady -= HandleMatchReady;
            DeskillzEvents.OnMatchStart -= HandleMatchStart;
            DeskillzEvents.OnMatchComplete -= HandleMatchComplete;
            DeskillzEvents.OnConnectionStateChanged -= HandleConnectionChanged;
        }

        // =============================================================================
        // SCORE OPERATIONS
        // =============================================================================

        /// <summary>
        /// Set the current score.
        /// </summary>
        public void SetScore(int score)
        {
            if (!ValidateScoreChange(score, out var error))
            {
                DeskillzLogger.Warning($"Score rejected: {error}");
                RaiseValidationFailed(error);
                return;
            }

            var previousScore = _currentScore;
            _currentScore = score;

            if (score > _highScore)
            {
                _highScore = score;
            }

            // Record in history
            RecordScoreEntry(score, ScoreChangeType.Set);

            // Notify listeners
            DeskillzEvents.RaiseLocalScoreUpdated(score);

            DeskillzLogger.LogScore("Set", score, $"Previous: {previousScore}");

            // Auto-submit if enabled and threshold met
            if (_autoSubmitOnChange && Math.Abs(score - _lastSubmittedScore) >= _autoSubmitThreshold)
            {
                SubmitCheckpoint();
            }
        }

        /// <summary>
        /// Add points to current score.
        /// </summary>
        public void AddScore(int points)
        {
            if (points == 0) return;
            SetScore(_currentScore + points);
        }

        /// <summary>
        /// Subtract points from current score.
        /// </summary>
        public void SubtractScore(int points)
        {
            if (points == 0) return;
            SetScore(Math.Max(0, _currentScore - points));
        }

        /// <summary>
        /// Multiply current score.
        /// </summary>
        public void MultiplyScore(float multiplier)
        {
            if (multiplier == 1f) return;
            SetScore(Mathf.RoundToInt(_currentScore * multiplier));
        }

        /// <summary>
        /// Reset score to zero.
        /// </summary>
        public void ResetScore()
        {
            SetScore(0);
            RecordScoreEntry(0, ScoreChangeType.Reset);
        }

        // =============================================================================
        // SCORE SUBMISSION
        // =============================================================================

        /// <summary>
        /// Submit a score checkpoint (during match).
        /// </summary>
        public void SubmitCheckpoint()
        {
            if (!CanSubmit(out var reason))
            {
                DeskillzLogger.Debug($"Cannot submit checkpoint: {reason}");
                return;
            }

            QueueSubmission(_currentScore, isCheckpoint: true);
        }

        /// <summary>
        /// Submit final score (end of match).
        /// </summary>
        public void SubmitFinalScore()
        {
            SubmitFinalScore(_currentScore);
        }

        /// <summary>
        /// Submit a specific final score.
        /// </summary>
        public void SubmitFinalScore(int score)
        {
            if (!ValidateScoreChange(score, out var error))
            {
                DeskillzLogger.Error($"Final score rejected: {error}");
                RaiseValidationFailed(error);
                return;
            }

            _currentScore = score;
            QueueSubmission(score, isCheckpoint: false, priority: true);

            DeskillzLogger.LogScore("Final Submitted", score);
        }

        /// <summary>
        /// Check if score can be submitted now.
        /// </summary>
        public bool CanSubmit(out string reason)
        {
            if (_isSubmitting)
            {
                reason = "Submission in progress";
                return false;
            }

            if (_submissionCount >= _maxSubmissionsPerMatch)
            {
                reason = "Max submissions reached";
                return false;
            }

            if (Time.time - _lastSubmissionTime < _minSubmissionInterval)
            {
                reason = "Too soon since last submission";
                return false;
            }

            if (string.IsNullOrEmpty(_currentMatchId))
            {
                reason = "No active match";
                return false;
            }

            reason = null;
            return true;
        }

        // =============================================================================
        // SUBMISSION QUEUE
        // =============================================================================

        private void QueueSubmission(int score, bool isCheckpoint, bool priority = false)
        {
            var submission = new ScoreSubmission
            {
                Score = score,
                MatchId = _currentMatchId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsCheckpoint = isCheckpoint,
                Sequence = _submissionCount + _pendingSubmissions.Count + 1
            };

            // Build secure payload
            var payload = BuildSecurePayload(submission);
            submission.EncryptedPayload = payload;

            if (priority)
            {
                // For final scores, process immediately
                StartCoroutine(ProcessSubmission(submission));
            }
            else
            {
                _pendingSubmissions.Enqueue(submission);
            }

            DeskillzLogger.Debug($"Queued score submission: {score} (checkpoint: {isCheckpoint})");
        }

        private void ProcessPendingSubmissions()
        {
            if (_isSubmitting || _pendingSubmissions.Count == 0) return;
            if (Time.time - _lastSubmissionTime < _minSubmissionInterval) return;

            var submission = _pendingSubmissions.Dequeue();
            StartCoroutine(ProcessSubmission(submission));
        }

        private IEnumerator ProcessSubmission(ScoreSubmission submission)
        {
            _isSubmitting = true;

            if (Deskillz.TestMode)
            {
                // Simulate submission in test mode
                yield return new WaitForSeconds(0.2f);
                HandleSubmissionSuccess(submission);
            }
            else
            {
                yield return SubmitToServer(submission);
            }

            _isSubmitting = false;
            _lastSubmissionTime = Time.time;
        }

        private IEnumerator SubmitToServer(ScoreSubmission submission)
        {
            var network = DeskillzManager.Instance?.Network;
            if (network == null)
            {
                HandleSubmissionFailed(submission, new DeskillzError(ErrorCode.NotInitialized, "Network not available"));
                yield break;
            }

            var endpoint = submission.IsCheckpoint ? "/sdk/score/checkpoint" : "/sdk/score/final";
            bool completed = false;
            DeskillzError error = null;

            yield return network.Post<ScoreResponse>(
                endpoint,
                submission.EncryptedPayload,
                response =>
                {
                    completed = true;
                    if (response.Accepted)
                    {
                        HandleSubmissionSuccess(submission);
                    }
                    else
                    {
                        error = new DeskillzError(ErrorCode.ScoreValidationFailed, response.RejectionReason);
                    }
                },
                err =>
                {
                    completed = true;
                    error = err;
                }
            );

            if (error != null)
            {
                HandleSubmissionFailed(submission, error);
            }
        }

        private void HandleSubmissionSuccess(ScoreSubmission submission)
        {
            _lastSubmittedScore = submission.Score;
            _submissionCount++;

            RecordScoreEntry(submission.Score, 
                submission.IsCheckpoint ? ScoreChangeType.Checkpoint : ScoreChangeType.Final);

            if (!submission.IsCheckpoint)
            {
                DeskillzEvents.RaiseScoreSubmitted(submission.Score);
            }

            DeskillzLogger.LogScore("Submitted", submission.Score, 
                $"Type: {(submission.IsCheckpoint ? "Checkpoint" : "Final")}");
        }

        private void HandleSubmissionFailed(ScoreSubmission submission, DeskillzError error)
        {
            DeskillzLogger.Error($"Score submission failed: {error.Message}");

            if (error.IsRecoverable && submission.IsCheckpoint)
            {
                // Re-queue checkpoint submissions
                _pendingSubmissions.Enqueue(submission);
            }
            else if (!submission.IsCheckpoint)
            {
                // Cache final scores for retry
                DeskillzCache.QueuePendingScore(
                    submission.MatchId,
                    submission.Score,
                    submission.Timestamp
                );

                DeskillzEvents.RaiseScoreSubmissionFailed(error);
            }
        }

        // =============================================================================
        // SECURE PAYLOAD
        // =============================================================================

        private SecureScorePayload BuildSecurePayload(ScoreSubmission submission)
        {
            var payload = new SecureScorePayload
            {
                MatchId = submission.MatchId,
                PlayerId = Deskillz.CurrentPlayer?.Id,
                Score = submission.Score,
                Timestamp = submission.Timestamp,
                Sequence = submission.Sequence,
                IsCheckpoint = submission.IsCheckpoint,
                GameTime = MatchController.Instance?.TimeElapsed ?? 0f,
                Round = MatchController.Instance?.CurrentRound ?? 1,
                ClientVersion = DeskillzConfig.SDK_VERSION,
                Platform = Application.platform.ToString()
            };

            // Add validation data
            if (_enableValidation)
            {
                payload.ValidationData = _validator.GenerateValidationData(payload);
            }

            // Encrypt if enabled
            if (_encryptScores && !Deskillz.TestMode)
            {
                payload.EncryptedScore = _encryption.EncryptScore(submission.Score, submission.Timestamp);
                payload.Signature = _encryption.SignPayload(payload);
            }

            return payload;
        }

        // =============================================================================
        // VALIDATION
        // =============================================================================

        private bool ValidateScoreChange(int newScore, out string error)
        {
            if (!_enableValidation)
            {
                error = null;
                return true;
            }

            return _validator.ValidateScoreChange(_currentScore, newScore, out error);
        }

        private void RaiseValidationFailed(string reason)
        {
            var error = new DeskillzError(ErrorCode.ScoreValidationFailed, reason);
            DeskillzEvents.RaiseError(error);

            // Potential anti-cheat violation
            if (_validator.IsSuspiciousActivity())
            {
                DeskillzEvents.RaiseAntiCheatViolation(
                    new DeskillzError(ErrorCode.AntiCheatViolation, "Suspicious score activity detected"));
            }
        }

        // =============================================================================
        // SCORE HISTORY
        // =============================================================================

        private void RecordScoreEntry(int score, ScoreChangeType type)
        {
            var entry = new ScoreEntry
            {
                Score = score,
                Timestamp = DateTime.UtcNow,
                GameTime = MatchController.Instance?.TimeElapsed ?? 0f,
                ChangeType = type,
                Delta = score - (_scoreHistory.Count > 0 ? _scoreHistory[_scoreHistory.Count - 1].Score : 0)
            };

            _scoreHistory.Add(entry);

            // Limit history size
            while (_scoreHistory.Count > 1000)
            {
                _scoreHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get score at a specific game time.
        /// </summary>
        public int GetScoreAtTime(float gameTime)
        {
            for (int i = _scoreHistory.Count - 1; i >= 0; i--)
            {
                if (_scoreHistory[i].GameTime <= gameTime)
                {
                    return _scoreHistory[i].Score;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get score delta over a time period.
        /// </summary>
        public int GetScoreDelta(float startTime, float endTime)
        {
            int startScore = GetScoreAtTime(startTime);
            int endScore = GetScoreAtTime(endTime);
            return endScore - startScore;
        }

        /// <summary>
        /// Get average score rate (points per second).
        /// </summary>
        public float GetScoreRate()
        {
            var elapsed = MatchController.Instance?.TimeElapsed ?? 0f;
            if (elapsed <= 0) return 0f;
            return _currentScore / elapsed;
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleMatchReady(MatchData match)
        {
            _currentMatchId = match.MatchId;
            _sessionToken = Guid.NewGuid().ToString("N");
            
            // Initialize encryption with match-specific key
            _encryption.Initialize(_currentMatchId, _sessionToken);
            _validator.Initialize(match);

            DeskillzLogger.Debug($"ScoreManager ready for match: {_currentMatchId}");
        }

        private void HandleMatchStart(MatchData match)
        {
            Reset();
            _validator.StartTracking();
        }

        private void HandleMatchComplete(MatchResult result)
        {
            _validator.StopTracking();
            
            // Submit any remaining pending scores
            FlushPendingSubmissions();
        }

        private void HandleConnectionChanged(ConnectionState state)
        {
            if (state == ConnectionState.Connected && _pendingSubmissions.Count > 0)
            {
                DeskillzLogger.Info($"Connection restored, processing {_pendingSubmissions.Count} pending submissions");
            }
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        private void FlushPendingSubmissions()
        {
            while (_pendingSubmissions.Count > 0)
            {
                var submission = _pendingSubmissions.Dequeue();
                StartCoroutine(ProcessSubmission(submission));
            }
        }

        /// <summary>
        /// Reset score manager state.
        /// </summary>
        public void Reset()
        {
            _currentScore = 0;
            _lastSubmittedScore = 0;
            _highScore = 0;
            _submissionCount = 0;
            _lastSubmissionTime = 0;
            _scoreHistory.Clear();
            _pendingSubmissions.Clear();

            DeskillzLogger.Debug("ScoreManager reset");
        }

        /// <summary>
        /// Get score statistics for current match.
        /// </summary>
        public ScoreStatistics GetStatistics()
        {
            return new ScoreStatistics
            {
                CurrentScore = _currentScore,
                HighScore = _highScore,
                TotalSubmissions = _submissionCount,
                AverageScoreRate = GetScoreRate(),
                ScoreEntries = _scoreHistory.Count,
                PendingSubmissions = _pendingSubmissions.Count
            };
        }
    }

    // =============================================================================
    // DATA STRUCTURES
    // =============================================================================

    /// <summary>
    /// Score entry in history.
    /// </summary>
    public class ScoreEntry
    {
        public int Score { get; set; }
        public DateTime Timestamp { get; set; }
        public float GameTime { get; set; }
        public ScoreChangeType ChangeType { get; set; }
        public int Delta { get; set; }
    }

    /// <summary>
    /// Type of score change.
    /// </summary>
    public enum ScoreChangeType
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Reset,
        Checkpoint,
        Final
    }

    /// <summary>
    /// Pending score submission.
    /// </summary>
    internal class ScoreSubmission
    {
        public int Score { get; set; }
        public string MatchId { get; set; }
        public long Timestamp { get; set; }
        public bool IsCheckpoint { get; set; }
        public int Sequence { get; set; }
        public SecureScorePayload EncryptedPayload { get; set; }
    }

    /// <summary>
    /// Server response to score submission.
    /// </summary>
    [Serializable]
    internal class ScoreResponse
    {
        public bool Accepted { get; set; }
        public string RejectionReason { get; set; }
        public int ServerScore { get; set; }
        public long ServerTimestamp { get; set; }
    }

    /// <summary>
    /// Score statistics snapshot.
    /// </summary>
    public class ScoreStatistics
    {
        public int CurrentScore { get; set; }
        public int HighScore { get; set; }
        public int TotalSubmissions { get; set; }
        public float AverageScoreRate { get; set; }
        public int ScoreEntries { get; set; }
        public int PendingSubmissions { get; set; }
    }
}

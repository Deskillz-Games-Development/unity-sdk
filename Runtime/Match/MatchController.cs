// =============================================================================
// Deskillz SDK for Unity - Phase 2: Match System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;

namespace Deskillz.Match
{
    /// <summary>
    /// Controls the complete match lifecycle from initialization to completion.
    /// Handles state transitions, timing, rounds, and server synchronization.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static MatchController _instance;
        
        public static MatchController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MatchController>();
                    if (_instance == null && DeskillzManager.HasInstance)
                    {
                        _instance = DeskillzManager.Instance.gameObject.AddComponent<MatchController>();
                    }
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Match Settings")]
        [Tooltip("Countdown seconds before match starts")]
        [SerializeField] private int _countdownSeconds = 3;

        [Tooltip("Grace period after time expires (seconds)")]
        [SerializeField] private float _gracePeriodSeconds = 2f;

        [Tooltip("Auto-end match when timer expires")]
        [SerializeField] private bool _autoEndOnTimeExpire = true;

        [Tooltip("Allow pause during match")]
        [SerializeField] private bool _allowPause = true;

        // =============================================================================
        // STATE
        // =============================================================================

        private MatchStateMachine _stateMachine;
        private MatchTimer _timer;
        private MatchRound _roundManager;
        private MatchData _matchData;
        private bool _isInitialized;

        // Score tracking
        private int _currentScore;
        private int _highestScore;
        private int _scoreSubmissions;
        private const int MAX_SCORE_SUBMISSIONS = 100;

        // Pause state
        private float _pauseStartTime;
        private float _totalPausedTime;
        private bool _isPaused;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current match data.</summary>
        public MatchData CurrentMatch => _matchData;

        /// <summary>Current match state.</summary>
        public MatchStatus CurrentState => _stateMachine?.CurrentState ?? MatchStatus.None;

        /// <summary>Whether match is active (in progress or paused).</summary>
        public bool IsMatchActive => CurrentState == MatchStatus.InProgress || CurrentState == MatchStatus.Paused;

        /// <summary>Whether match is in progress (not paused).</summary>
        public bool IsPlaying => CurrentState == MatchStatus.InProgress;

        /// <summary>Whether match is paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Current player score.</summary>
        public int CurrentScore => _currentScore;

        /// <summary>Highest score achieved this match.</summary>
        public int HighestScore => _highestScore;

        /// <summary>Time remaining in seconds (-1 if no time limit).</summary>
        public float TimeRemaining => _timer?.TimeRemaining ?? -1f;

        /// <summary>Time elapsed in seconds.</summary>
        public float TimeElapsed => _timer?.TimeElapsed ?? 0f;

        /// <summary>Current round number.</summary>
        public int CurrentRound => _roundManager?.CurrentRound ?? 1;

        /// <summary>Total rounds in match.</summary>
        public int TotalRounds => _matchData?.Rounds ?? 1;

        /// <summary>Whether this is a timed match.</summary>
        public bool IsTimedMatch => _matchData?.TimeLimitSeconds > 0;

        /// <summary>Whether this is a multi-round match.</summary>
        public bool IsMultiRound => TotalRounds > 1;

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
            if (!IsMatchActive) return;

            // Update timer
            _timer?.Update(Time.deltaTime);

            // Check for time expiration
            if (_autoEndOnTimeExpire && _timer != null && _timer.IsExpired && !_timer.IsInGracePeriod)
            {
                HandleTimeExpired();
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeComponents()
        {
            _stateMachine = new MatchStateMachine();
            _timer = new MatchTimer();
            _roundManager = new MatchRound();

            // Subscribe to state changes
            _stateMachine.OnStateChanged += HandleStateChanged;
            _timer.OnTimeWarning += HandleTimeWarning;
            _timer.OnTimeExpired += HandleTimerExpired;
            _timer.OnGracePeriodEnd += HandleGracePeriodEnd;
            _roundManager.OnRoundChanged += HandleRoundChanged;
            _roundManager.OnAllRoundsComplete += HandleAllRoundsComplete;

            _isInitialized = true;
            DeskillzLogger.Debug("MatchController initialized");
        }

        private void SubscribeToEvents()
        {
            DeskillzEvents.OnMatchReady += HandleMatchReady;
            DeskillzEvents.OnConnectionStateChanged += HandleConnectionChanged;
        }

        private void UnsubscribeFromEvents()
        {
            DeskillzEvents.OnMatchReady -= HandleMatchReady;
            DeskillzEvents.OnConnectionStateChanged -= HandleConnectionChanged;
        }

        // =============================================================================
        // MATCH LIFECYCLE
        // =============================================================================

        /// <summary>
        /// Initialize a match with provided data.
        /// Called automatically when match is received from Deskillz app.
        /// </summary>
        public void InitializeMatch(MatchData matchData)
        {
            if (matchData == null)
            {
                DeskillzLogger.Error("Cannot initialize match: null match data");
                return;
            }

            _matchData = matchData;
            _currentScore = 0;
            _highestScore = 0;
            _scoreSubmissions = 0;
            _totalPausedTime = 0;
            _isPaused = false;

            // Configure timer
            if (matchData.TimeLimitSeconds > 0)
            {
                _timer.Configure(matchData.TimeLimitSeconds, _gracePeriodSeconds);
            }

            // Configure rounds
            _roundManager.Configure(matchData.Rounds);

            // Transition to pending state
            _stateMachine.TransitionTo(MatchStatus.Pending);

            DeskillzLogger.LogMatch("Initialized", matchData.MatchId, 
                $"Mode: {matchData.Mode}, Rounds: {matchData.Rounds}, Time: {matchData.TimeLimitSeconds}s");
        }

        /// <summary>
        /// Start the match with optional countdown.
        /// </summary>
        public void StartMatch(bool withCountdown = true)
        {
            if (CurrentState != MatchStatus.Pending)
            {
                DeskillzLogger.Warning($"Cannot start match: invalid state ({CurrentState})");
                return;
            }

            if (withCountdown && _countdownSeconds > 0)
            {
                StartCoroutine(CountdownSequence());
            }
            else
            {
                BeginGameplay();
            }
        }

        /// <summary>
        /// Pause the current match.
        /// </summary>
        public void PauseMatch()
        {
            if (!_allowPause || CurrentState != MatchStatus.InProgress)
            {
                DeskillzLogger.Warning("Cannot pause match");
                return;
            }

            _isPaused = true;
            _pauseStartTime = Time.realtimeSinceStartup;
            _timer?.Pause();
            _stateMachine.TransitionTo(MatchStatus.Paused);

            DeskillzLogger.LogMatch("Paused", _matchData?.MatchId);
        }

        /// <summary>
        /// Resume from pause.
        /// </summary>
        public void ResumeMatch()
        {
            if (CurrentState != MatchStatus.Paused)
            {
                DeskillzLogger.Warning("Cannot resume: not paused");
                return;
            }

            _totalPausedTime += Time.realtimeSinceStartup - _pauseStartTime;
            _isPaused = false;
            _timer?.Resume();
            _stateMachine.TransitionTo(MatchStatus.InProgress);

            DeskillzLogger.LogMatch("Resumed", _matchData?.MatchId);
        }

        /// <summary>
        /// End the current match and submit final score.
        /// </summary>
        public void EndMatch()
        {
            if (!IsMatchActive && CurrentState != MatchStatus.Pending)
            {
                DeskillzLogger.Warning($"Cannot end match: invalid state ({CurrentState})");
                return;
            }

            _timer?.Stop();
            _stateMachine.TransitionTo(MatchStatus.Processing);

            DeskillzLogger.LogMatch("Ending", _matchData?.MatchId, $"Final Score: {_currentScore}");

            // Submit final score and process results
            StartCoroutine(ProcessMatchEnd());
        }

        /// <summary>
        /// Forfeit the current match.
        /// </summary>
        public void ForfeitMatch()
        {
            if (!IsMatchActive && CurrentState != MatchStatus.Pending)
            {
                DeskillzLogger.Warning("Cannot forfeit: no active match");
                return;
            }

            _timer?.Stop();
            _stateMachine.TransitionTo(MatchStatus.Forfeited);

            DeskillzLogger.LogMatch("Forfeited", _matchData?.MatchId);

            var result = CreateMatchResult(MatchOutcome.Forfeit);
            CompleteMatch(result);
        }

        // =============================================================================
        // SCORE MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Update the current score.
        /// </summary>
        public void UpdateScore(int score)
        {
            if (!IsMatchActive)
            {
                DeskillzLogger.Warning("Cannot update score: no active match");
                return;
            }

            var previousScore = _currentScore;
            _currentScore = score;

            if (score > _highestScore)
            {
                _highestScore = score;
            }

            // Update match data
            if (_matchData != null)
            {
                _matchData.LocalPlayerScore = score;
            }

            DeskillzLogger.LogScore("Updated", score, $"Previous: {previousScore}");
            DeskillzEvents.RaiseLocalScoreUpdated(score);
        }

        /// <summary>
        /// Add to the current score.
        /// </summary>
        public void AddScore(int points)
        {
            UpdateScore(_currentScore + points);
        }

        /// <summary>
        /// Submit score to server (for checkpointing during match).
        /// </summary>
        public void SubmitScoreCheckpoint()
        {
            if (!IsMatchActive) return;

            if (_scoreSubmissions >= MAX_SCORE_SUBMISSIONS)
            {
                DeskillzLogger.Warning("Max score submissions reached");
                return;
            }

            _scoreSubmissions++;
            
            // Queue score submission
            if (DeskillzManager.Instance?.Network != null)
            {
                StartCoroutine(SubmitScoreToServer(_currentScore, isCheckpoint: true));
            }
        }

        // =============================================================================
        // ROUND MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Complete current round and advance to next.
        /// </summary>
        public void CompleteRound(int roundScore)
        {
            if (!IsMultiRound) return;

            _roundManager.CompleteRound(roundScore);

            if (!_roundManager.IsComplete)
            {
                // Reset for next round
                StartCoroutine(TransitionToNextRound());
            }
        }

        /// <summary>
        /// Get score for a specific round.
        /// </summary>
        public int GetRoundScore(int round)
        {
            return _roundManager.GetRoundScore(round);
        }

        /// <summary>
        /// Get total score across all completed rounds.
        /// </summary>
        public int GetTotalRoundScore()
        {
            return _roundManager.TotalScore;
        }

        // =============================================================================
        // COUNTDOWN SEQUENCE
        // =============================================================================

        private IEnumerator CountdownSequence()
        {
            _stateMachine.TransitionTo(MatchStatus.Countdown);

            for (int i = _countdownSeconds; i > 0; i--)
            {
                DeskillzEvents.RaiseMatchCountdown(i);
                DeskillzLogger.LogEvent("Countdown", i.ToString());
                yield return new WaitForSeconds(1f);
            }

            BeginGameplay();
        }

        private void BeginGameplay()
        {
            _stateMachine.TransitionTo(MatchStatus.InProgress);
            _timer?.Start();

            if (_matchData != null)
            {
                _matchData.Status = MatchStatus.InProgress;
                _matchData.StartTime = DateTime.UtcNow;
            }

            DeskillzLogger.LogMatch("Started", _matchData?.MatchId);
            DeskillzEvents.RaiseMatchStart(_matchData);
        }

        // =============================================================================
        // ROUND TRANSITIONS
        // =============================================================================

        private IEnumerator TransitionToNextRound()
        {
            // Brief pause between rounds
            _timer?.Pause();
            
            yield return new WaitForSeconds(1f);

            // Reset timer for new round
            if (IsTimedMatch)
            {
                _timer.Reset();
            }

            _timer?.Resume();
            _roundManager.StartNextRound();

            DeskillzLogger.LogMatch("Round Started", _matchData?.MatchId, $"Round {CurrentRound}/{TotalRounds}");
        }

        // =============================================================================
        // MATCH END PROCESSING
        // =============================================================================

        private IEnumerator ProcessMatchEnd()
        {
            // Submit final score
            yield return SubmitScoreToServer(_currentScore, isCheckpoint: false);

            // Wait for server response (or timeout)
            float timeout = 10f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                // In test mode, skip waiting
                if (Deskillz.TestMode) break;

                // Check for server response
                // In real implementation, wait for server confirmation
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            // Determine outcome
            var outcome = DetermineOutcome();
            var result = CreateMatchResult(outcome);

            CompleteMatch(result);
        }

        private MatchOutcome DetermineOutcome()
        {
            if (_matchData == null) return MatchOutcome.Pending;

            // For async matches, outcome is determined server-side later
            if (_matchData.IsAsync)
            {
                return MatchOutcome.Pending;
            }

            // For real-time matches, compare scores
            var opponent = _matchData.Players.Find(p => !p.IsLocalPlayer);
            if (opponent == null) return MatchOutcome.Win;

            var config = DeskillzConfig.Instance;
            
            if (config.ScoreType == ScoreType.HigherIsBetter)
            {
                if (_currentScore > opponent.Score) return MatchOutcome.Win;
                if (_currentScore < opponent.Score) return MatchOutcome.Loss;
                return MatchOutcome.Tie;
            }
            else // LowerIsBetter
            {
                if (_currentScore < opponent.Score) return MatchOutcome.Win;
                if (_currentScore > opponent.Score) return MatchOutcome.Loss;
                return MatchOutcome.Tie;
            }
        }

        private MatchResult CreateMatchResult(MatchOutcome outcome)
        {
            var result = new MatchResult
            {
                MatchId = _matchData?.MatchId ?? "unknown",
                Outcome = outcome,
                FinalScore = _currentScore,
                FinalRank = outcome == MatchOutcome.Win ? 1 : 2,
                Currency = _matchData?.Currency ?? Currency.Free,
                DurationSeconds = TimeElapsed,
                XpEarned = CalculateXpEarned(outcome)
            };

            // Calculate prize for wins
            if (outcome == MatchOutcome.Win && _matchData != null)
            {
                // 95% of prize pool (5% platform fee)
                result.PrizeWon = _matchData.PrizePool * 0.95m;
            }

            // Copy final standings
            if (_matchData?.Players != null)
            {
                foreach (var player in _matchData.Players)
                {
                    result.FinalStandings.Add(new MatchPlayer
                    {
                        PlayerId = player.PlayerId,
                        Username = player.Username,
                        Score = player.IsLocalPlayer ? _currentScore : player.Score,
                        IsLocalPlayer = player.IsLocalPlayer,
                        Rank = player.IsLocalPlayer ? result.FinalRank : (result.FinalRank == 1 ? 2 : 1)
                    });
                }
            }

            return result;
        }

        private int CalculateXpEarned(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Win => 100,
                MatchOutcome.Tie => 50,
                MatchOutcome.Loss => 25,
                MatchOutcome.Forfeit => 0,
                _ => 10
            };
        }

        private void CompleteMatch(MatchResult result)
        {
            _stateMachine.TransitionTo(MatchStatus.Completed);

            DeskillzLogger.LogMatch("Completed", result.MatchId, 
                $"Outcome: {result.Outcome}, Score: {result.FinalScore}, Prize: {result.PrizeWon}");

            // Notify manager
            DeskillzManager.Instance?.EndCurrentMatch(result);

            // Clear local state
            _matchData = null;
            _currentScore = 0;
            _highestScore = 0;
        }

        // =============================================================================
        // SERVER COMMUNICATION
        // =============================================================================

        private IEnumerator SubmitScoreToServer(int score, bool isCheckpoint)
        {
            if (Deskillz.TestMode)
            {
                DeskillzLogger.LogScore(isCheckpoint ? "Checkpoint" : "Final", score, "(Test Mode)");
                if (!isCheckpoint)
                {
                    DeskillzEvents.RaiseScoreSubmitted(score);
                }
                yield break;
            }

            var network = DeskillzManager.Instance?.Network;
            if (network == null) yield break;

            var endpoint = isCheckpoint ? "/sdk/match/score/checkpoint" : "/sdk/match/score";
            var payload = new
            {
                matchId = _matchData?.MatchId,
                score,
                round = CurrentRound,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                isCheckpoint
            };

            bool completed = false;
            DeskillzError error = null;

            yield return network.Post<object>(
                endpoint,
                payload,
                _ =>
                {
                    completed = true;
                    if (!isCheckpoint)
                    {
                        DeskillzEvents.RaiseScoreSubmitted(score);
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
                DeskillzLogger.Error($"Score submission failed: {error.Message}");
                
                // Queue for retry if checkpoint
                if (isCheckpoint && error.IsRecoverable)
                {
                    DeskillzCache.QueuePendingScore(
                        _matchData?.MatchId ?? "unknown",
                        score,
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    );
                }
                else if (!isCheckpoint)
                {
                    DeskillzEvents.RaiseScoreSubmissionFailed(error);
                }
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void HandleMatchReady(MatchData matchData)
        {
            InitializeMatch(matchData);
        }

        private void HandleStateChanged(MatchStatus oldState, MatchStatus newState)
        {
            DeskillzLogger.Debug($"Match state: {oldState} -> {newState}");

            switch (newState)
            {
                case MatchStatus.Paused:
                    DeskillzEvents.RaiseMatchPaused();
                    break;
                case MatchStatus.InProgress when oldState == MatchStatus.Paused:
                    DeskillzEvents.RaiseMatchResumed();
                    break;
            }
        }

        private void HandleTimeWarning(float secondsRemaining)
        {
            DeskillzLogger.LogEvent("Time Warning", $"{secondsRemaining}s remaining");
            // Could raise an event here for UI to show warning
        }

        private void HandleTimerExpired()
        {
            DeskillzLogger.LogEvent("Timer Expired", "Entering grace period");
        }

        private void HandleGracePeriodEnd()
        {
            HandleTimeExpired();
        }

        private void HandleTimeExpired()
        {
            if (!IsMatchActive) return;

            DeskillzLogger.LogMatch("Time Expired", _matchData?.MatchId);
            EndMatch();
        }

        private void HandleRoundChanged(int round)
        {
            if (_matchData != null)
            {
                _matchData.CurrentRound = round;
            }
            DeskillzLogger.LogEvent("Round Changed", $"Round {round}");
        }

        private void HandleAllRoundsComplete()
        {
            DeskillzLogger.LogMatch("All Rounds Complete", _matchData?.MatchId);
            EndMatch();
        }

        private void HandleConnectionChanged(ConnectionState state)
        {
            if (state == ConnectionState.Disconnected && IsMatchActive && _matchData?.IsRealtime == true)
            {
                // Pause match on disconnect for real-time matches
                PauseMatch();
                DeskillzLogger.Warning("Match paused due to connection loss");
            }
            else if (state == ConnectionState.Connected && IsPaused)
            {
                // Could auto-resume or wait for user input
                DeskillzLogger.Info("Connection restored - match still paused");
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        /// <summary>
        /// Cancel and cleanup current match without submitting.
        /// </summary>
        public void CancelMatch(string reason = "Cancelled")
        {
            if (_matchData == null) return;

            _timer?.Stop();
            _stateMachine.TransitionTo(MatchStatus.Cancelled);

            DeskillzLogger.LogMatch("Cancelled", _matchData.MatchId, reason);
            DeskillzEvents.RaiseMatchCancelled(new DeskillzError(ErrorCode.Unknown, reason));

            _matchData = null;
            _currentScore = 0;
        }

        /// <summary>
        /// Reset controller state.
        /// </summary>
        public void Reset()
        {
            _timer?.Stop();
            _timer?.Reset();
            _roundManager?.Reset();
            _stateMachine?.TransitionTo(MatchStatus.None);
            
            _matchData = null;
            _currentScore = 0;
            _highestScore = 0;
            _scoreSubmissions = 0;
            _totalPausedTime = 0;
            _isPaused = false;

            DeskillzLogger.Debug("MatchController reset");
        }
    }
}

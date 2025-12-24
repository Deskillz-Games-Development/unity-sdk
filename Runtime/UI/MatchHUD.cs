// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Deskillz.Match;
using Deskillz.Score;

namespace Deskillz.UI
{
    /// <summary>
    /// In-game HUD showing score, timer, opponent info, and match status.
    /// </summary>
    public class MatchHUD : UIPanel
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("HUD Settings")]
        [SerializeField] private bool _showOpponentScore = true;
        [SerializeField] private bool _showTimer = true;
        [SerializeField] private bool _showRound = true;
        [SerializeField] private bool _animateScoreChanges = true;
        [SerializeField] private float _scoreAnimationSpeed = 1000f;

        // =============================================================================
        // UI REFERENCES
        // =============================================================================

        private RectTransform _container;

        // Score section
        private RectTransform _scorePanel;
        private Text _scoreLabelText;
        private Text _scoreValueText;
        private Text _highScoreText;

        // Timer section
        private RectTransform _timerPanel;
        private Image _timerIcon;
        private Text _timerText;
        private Image _timerProgressBar;

        // Opponent section
        private RectTransform _opponentPanel;
        private Text _opponentNameText;
        private Text _opponentScoreText;
        private Image _opponentAvatar;

        // Round section
        private RectTransform _roundPanel;
        private Text _roundText;

        // Pause button
        private Button _pauseButton;

        // =============================================================================
        // STATE
        // =============================================================================

        private MatchData _matchData;
        private ScoreAnimator _scoreAnimator;
        private int _currentScore;
        private int _opponentScore;
        private float _totalTime;
        private float _timeRemaining;
        private bool _isTimerWarning;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current displayed score.</summary>
        public int DisplayedScore => _scoreAnimator?.DisplayedScore ?? _currentScore;

        /// <summary>Current opponent score.</summary>
        public int OpponentScore => _opponentScore;

        /// <summary>Time remaining.</summary>
        public float TimeRemaining => _timeRemaining;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        protected override void SetupLayout()
        {
            _scoreAnimator = new ScoreAnimator();

            // Main container
            _container = gameObject.GetComponent<RectTransform>();
            SetAnchorFill(_container);

            // Create sections
            CreateScoreSection();
            CreateTimerSection();
            CreateOpponentSection();
            CreateRoundSection();
            CreatePauseButton();
        }

        private void CreateScoreSection()
        {
            // Score panel - top left
            var panelGO = new GameObject("ScorePanel");
            panelGO.transform.SetParent(_container, false);

            _scorePanel = panelGO.AddComponent<RectTransform>();
            _scorePanel.anchorMin = new Vector2(0, 1);
            _scorePanel.anchorMax = new Vector2(0, 1);
            _scorePanel.pivot = new Vector2(0, 1);
            _scorePanel.anchoredPosition = new Vector2(20, -20);
            _scorePanel.sizeDelta = new Vector2(300, 100);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.5f);

            // Score label
            _scoreLabelText = CreateText("ScoreLabel", _scorePanel, "SCORE");
            _scoreLabelText.rectTransform.anchorMin = new Vector2(0, 0.6f);
            _scoreLabelText.rectTransform.anchorMax = new Vector2(1, 1);
            _scoreLabelText.rectTransform.offsetMin = new Vector2(10, 0);
            _scoreLabelText.rectTransform.offsetMax = new Vector2(-10, -5);
            _scoreLabelText.alignment = TextAnchor.LowerLeft;
            _scoreLabelText.fontSize = 18;

            // Score value
            _scoreValueText = CreateText("ScoreValue", _scorePanel, "0");
            _scoreValueText.rectTransform.anchorMin = new Vector2(0, 0);
            _scoreValueText.rectTransform.anchorMax = new Vector2(1, 0.7f);
            _scoreValueText.rectTransform.offsetMin = new Vector2(10, 5);
            _scoreValueText.rectTransform.offsetMax = new Vector2(-10, 0);
            _scoreValueText.alignment = TextAnchor.UpperLeft;
            _scoreValueText.fontSize = 48;
            _scoreValueText.fontStyle = FontStyle.Bold;
        }

        private void CreateTimerSection()
        {
            // Timer panel - top center
            var panelGO = new GameObject("TimerPanel");
            panelGO.transform.SetParent(_container, false);

            _timerPanel = panelGO.AddComponent<RectTransform>();
            _timerPanel.anchorMin = new Vector2(0.5f, 1);
            _timerPanel.anchorMax = new Vector2(0.5f, 1);
            _timerPanel.pivot = new Vector2(0.5f, 1);
            _timerPanel.anchoredPosition = new Vector2(0, -20);
            _timerPanel.sizeDelta = new Vector2(200, 80);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.5f);

            // Timer text
            _timerText = CreateText("TimerText", _timerPanel, "--:--");
            _timerText.rectTransform.anchorMin = Vector2.zero;
            _timerText.rectTransform.anchorMax = new Vector2(1, 0.8f);
            _timerText.rectTransform.offsetMin = Vector2.zero;
            _timerText.rectTransform.offsetMax = Vector2.zero;
            _timerText.alignment = TextAnchor.MiddleCenter;
            _timerText.fontSize = 42;
            _timerText.fontStyle = FontStyle.Bold;

            // Progress bar background
            var progressBgGO = new GameObject("ProgressBg");
            progressBgGO.transform.SetParent(_timerPanel, false);

            var progressBgRect = progressBgGO.AddComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0.1f, 0);
            progressBgRect.anchorMax = new Vector2(0.9f, 0.15f);
            progressBgRect.offsetMin = Vector2.zero;
            progressBgRect.offsetMax = Vector2.zero;

            var progressBgImage = progressBgGO.AddComponent<Image>();
            progressBgImage.color = new Color(0.2f, 0.2f, 0.2f);

            // Progress bar fill
            var progressFillGO = new GameObject("ProgressFill");
            progressFillGO.transform.SetParent(progressBgRect, false);

            var progressFillRect = progressFillGO.AddComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = Vector2.one;
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;

            _timerProgressBar = progressFillGO.AddComponent<Image>();
            _timerProgressBar.color = _theme?.AccentColor ?? Color.yellow;
            _timerProgressBar.type = Image.Type.Filled;
            _timerProgressBar.fillMethod = Image.FillMethod.Horizontal;
            _timerProgressBar.fillAmount = 1f;

            if (!_showTimer)
            {
                _timerPanel.gameObject.SetActive(false);
            }
        }

        private void CreateOpponentSection()
        {
            // Opponent panel - top right
            var panelGO = new GameObject("OpponentPanel");
            panelGO.transform.SetParent(_container, false);

            _opponentPanel = panelGO.AddComponent<RectTransform>();
            _opponentPanel.anchorMin = new Vector2(1, 1);
            _opponentPanel.anchorMax = new Vector2(1, 1);
            _opponentPanel.pivot = new Vector2(1, 1);
            _opponentPanel.anchoredPosition = new Vector2(-20, -20);
            _opponentPanel.sizeDelta = new Vector2(250, 80);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.5f);

            // Opponent name
            _opponentNameText = CreateText("OpponentName", _opponentPanel, "Opponent");
            _opponentNameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _opponentNameText.rectTransform.anchorMax = new Vector2(1, 1);
            _opponentNameText.rectTransform.offsetMin = new Vector2(10, 0);
            _opponentNameText.rectTransform.offsetMax = new Vector2(-10, -5);
            _opponentNameText.alignment = TextAnchor.LowerRight;
            _opponentNameText.fontSize = 18;

            // Opponent score
            _opponentScoreText = CreateText("OpponentScore", _opponentPanel, "0");
            _opponentScoreText.rectTransform.anchorMin = new Vector2(0, 0);
            _opponentScoreText.rectTransform.anchorMax = new Vector2(1, 0.6f);
            _opponentScoreText.rectTransform.offsetMin = new Vector2(10, 5);
            _opponentScoreText.rectTransform.offsetMax = new Vector2(-10, 0);
            _opponentScoreText.alignment = TextAnchor.UpperRight;
            _opponentScoreText.fontSize = 36;
            _opponentScoreText.fontStyle = FontStyle.Bold;

            if (!_showOpponentScore)
            {
                _opponentPanel.gameObject.SetActive(false);
            }
        }

        private void CreateRoundSection()
        {
            // Round indicator - below timer
            var panelGO = new GameObject("RoundPanel");
            panelGO.transform.SetParent(_container, false);

            _roundPanel = panelGO.AddComponent<RectTransform>();
            _roundPanel.anchorMin = new Vector2(0.5f, 1);
            _roundPanel.anchorMax = new Vector2(0.5f, 1);
            _roundPanel.pivot = new Vector2(0.5f, 1);
            _roundPanel.anchoredPosition = new Vector2(0, -110);
            _roundPanel.sizeDelta = new Vector2(150, 30);

            // Round text
            _roundText = CreateText("RoundText", _roundPanel, "Round 1/1");
            _roundText.rectTransform.anchorMin = Vector2.zero;
            _roundText.rectTransform.anchorMax = Vector2.one;
            _roundText.rectTransform.offsetMin = Vector2.zero;
            _roundText.rectTransform.offsetMax = Vector2.zero;
            _roundText.alignment = TextAnchor.MiddleCenter;
            _roundText.fontSize = 20;

            if (!_showRound)
            {
                _roundPanel.gameObject.SetActive(false);
            }
        }

        private void CreatePauseButton()
        {
            // Pause button - top right corner
            var buttonGO = new GameObject("PauseButton");
            buttonGO.transform.SetParent(_container, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 1);
            buttonRect.anchoredPosition = new Vector2(-20, -110);
            buttonRect.sizeDelta = new Vector2(50, 50);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0, 0, 0, 0.5f);

            _pauseButton = buttonGO.AddComponent<Button>();
            _pauseButton.targetGraphic = buttonImage;
            _pauseButton.onClick.AddListener(OnPauseClicked);

            // Pause icon (using text as fallback)
            var iconText = CreateText("PauseIcon", buttonRect, "⏸");
            iconText.rectTransform.anchorMin = Vector2.zero;
            iconText.rectTransform.anchorMax = Vector2.one;
            iconText.rectTransform.offsetMin = Vector2.zero;
            iconText.rectTransform.offsetMax = Vector2.zero;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.fontSize = 24;
        }

        // =============================================================================
        // UPDATE
        // =============================================================================

        private void Update()
        {
            if (!_isVisible) return;

            // Update score animation
            if (_animateScoreChanges && _scoreAnimator != null)
            {
                _scoreAnimator.Update();
                _scoreValueText.text = ScoreDisplay.Format(_scoreAnimator.DisplayedScore);
            }

            // Update timer
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (!_showTimer || _timerPanel == null) return;

            // Get time from MatchController
            var controller = MatchController.Instance;
            if (controller != null && controller.IsTimedMatch)
            {
                _timeRemaining = controller.TimeRemaining;
                _totalTime = controller.CurrentMatch?.TimeLimitSeconds ?? 0;

                // Update text
                var minutes = Mathf.FloorToInt(_timeRemaining / 60f);
                var seconds = Mathf.FloorToInt(_timeRemaining % 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";

                // Update progress bar
                if (_totalTime > 0)
                {
                    _timerProgressBar.fillAmount = _timeRemaining / _totalTime;
                }

                // Warning color when low
                if (_timeRemaining <= 10f && !_isTimerWarning)
                {
                    _isTimerWarning = true;
                    _timerText.color = _theme?.WarningColor ?? Color.yellow;
                    StartCoroutine(PulseTimerWarning());
                }
            }
        }

        private IEnumerator PulseTimerWarning()
        {
            while (_isTimerWarning && _timeRemaining > 0)
            {
                // Pulse effect
                _timerText.transform.localScale = Vector3.one * 1.1f;
                yield return new WaitForSeconds(0.1f);
                _timerText.transform.localScale = Vector3.one;
                yield return new WaitForSeconds(0.4f);
            }
        }

        // =============================================================================
        // PUBLIC METHODS
        // =============================================================================

        /// <summary>
        /// Set match data for the HUD.
        /// </summary>
        public void SetMatchData(MatchData match)
        {
            _matchData = match;

            // Setup timer
            if (match.TimeLimitSeconds > 0)
            {
                _totalTime = match.TimeLimitSeconds;
                _timeRemaining = _totalTime;
                _timerPanel.gameObject.SetActive(true);
            }
            else
            {
                _timerPanel.gameObject.SetActive(false);
            }

            // Setup opponent
            var opponent = match.Players?.Find(p => !p.IsLocalPlayer);
            if (opponent != null && _showOpponentScore)
            {
                _opponentNameText.text = opponent.Username ?? "Opponent";
                _opponentScoreText.text = "0";
                _opponentPanel.gameObject.SetActive(true);
            }
            else
            {
                _opponentPanel.gameObject.SetActive(false);
            }

            // Setup rounds
            if (match.Rounds > 1 && _showRound)
            {
                _roundText.text = $"Round {match.CurrentRound}/{match.Rounds}";
                _roundPanel.gameObject.SetActive(true);
            }
            else
            {
                _roundPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update the score display.
        /// </summary>
        public void UpdateScore(int score)
        {
            _currentScore = score;

            if (_animateScoreChanges)
            {
                _scoreAnimator.SetTarget(score);
            }
            else
            {
                _scoreValueText.text = ScoreDisplay.Format(score);
            }
        }

        /// <summary>
        /// Update opponent score.
        /// </summary>
        public void UpdateOpponentScore(string playerId, int score)
        {
            _opponentScore = score;
            if (_opponentScoreText != null)
            {
                _opponentScoreText.text = ScoreDisplay.Format(score);
            }
        }

        /// <summary>
        /// Update timer display.
        /// </summary>
        public void UpdateTimer(float secondsRemaining)
        {
            _timeRemaining = secondsRemaining;
        }

        /// <summary>
        /// Update round display.
        /// </summary>
        public void UpdateRound(int current, int total)
        {
            if (_roundText != null)
            {
                _roundText.text = $"Round {current}/{total}";
            }
        }

        // =============================================================================
        // THEME
        // =============================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_scoreValueText != null)
            {
                _scoreValueText.color = theme.TextPrimary;
            }

            if (_scoreLabelText != null)
            {
                _scoreLabelText.color = theme.TextSecondary;
            }

            if (_timerText != null && !_isTimerWarning)
            {
                _timerText.color = theme.TextPrimary;
            }

            if (_timerProgressBar != null)
            {
                _timerProgressBar.color = theme.AccentColor;
            }

            if (_opponentNameText != null)
            {
                _opponentNameText.color = theme.TextSecondary;
            }

            if (_opponentScoreText != null)
            {
                _opponentScoreText.color = theme.TextPrimary;
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnPauseClicked()
        {
            MatchController.Instance?.PauseMatch();
        }
    }
}

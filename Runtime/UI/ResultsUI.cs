// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Deskillz.Score;

namespace Deskillz.UI
{
    /// <summary>
    /// Displays match results with outcome, score, and prizes.
    /// </summary>
    public class ResultsUI : UIPanel
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Results Settings")]
        [SerializeField] private bool _animateResults = true;
        [SerializeField] private float _resultDisplayDelay = 0.5f;
        [SerializeField] private float _scoreCountUpDuration = 2f;

        // =============================================================================
        // UI REFERENCES
        // =============================================================================

        private RectTransform _panel;
        private Text _outcomeText;
        private Image _outcomeIcon;
        private Text _scoreLabel;
        private Text _scoreValue;
        private Text _prizeLabel;
        private Text _prizeValue;
        private Text _rankText;
        private Text _xpText;
        private RectTransform _statsContainer;
        private Button _continueButton;
        private Button _playAgainButton;

        // =============================================================================
        // STATE
        // =============================================================================

        private MatchResult _result;
        private ScoreAnimator _scoreAnimator;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when continue button is clicked.</summary>
        public event Action OnContinue;

        /// <summary>Fired when play again button is clicked.</summary>
        public event Action OnPlayAgain;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        protected override void SetupLayout()
        {
            _scoreAnimator = new ScoreAnimator();

            // Background overlay
            var overlayGO = new GameObject("Overlay");
            overlayGO.transform.SetParent(transform, false);

            var overlayRect = overlayGO.AddComponent<RectTransform>();
            SetAnchorFill(overlayRect);

            var overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = _theme?.OverlayColor ?? new Color(0, 0, 0, 0.85f);

            // Main panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);

            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(600, 500);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = _theme?.PanelColor ?? new Color(0.15f, 0.15f, 0.2f);

            // Outcome icon
            var iconGO = new GameObject("OutcomeIcon");
            iconGO.transform.SetParent(_panel, false);

            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1);
            iconRect.anchorMax = new Vector2(0.5f, 1);
            iconRect.pivot = new Vector2(0.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, -30);
            iconRect.sizeDelta = new Vector2(80, 80);

            _outcomeIcon = iconGO.AddComponent<Image>();
            _outcomeIcon.color = _theme?.SuccessColor ?? Color.green;

            // Outcome text
            _outcomeText = CreateText("OutcomeText", _panel, "VICTORY!");
            _outcomeText.rectTransform.anchorMin = new Vector2(0, 1);
            _outcomeText.rectTransform.anchorMax = new Vector2(1, 1);
            _outcomeText.rectTransform.pivot = new Vector2(0.5f, 1);
            _outcomeText.rectTransform.anchoredPosition = new Vector2(0, -120);
            _outcomeText.rectTransform.sizeDelta = new Vector2(0, 60);
            _outcomeText.alignment = TextAnchor.MiddleCenter;
            _outcomeText.fontSize = _theme?.TitleFontSize ?? 48;
            _outcomeText.fontStyle = FontStyle.Bold;

            // Score section
            CreateScoreSection();

            // Prize section
            CreatePrizeSection();

            // Stats section
            CreateStatsSection();

            // Buttons
            CreateButtons();
        }

        private void CreateScoreSection()
        {
            var scoreGO = new GameObject("ScoreSection");
            scoreGO.transform.SetParent(_panel, false);

            var scoreRect = scoreGO.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.pivot = new Vector2(0.5f, 1);
            scoreRect.anchoredPosition = new Vector2(0, -200);
            scoreRect.sizeDelta = new Vector2(0, 100);

            // Score label
            _scoreLabel = CreateText("ScoreLabel", scoreRect, "FINAL SCORE");
            _scoreLabel.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _scoreLabel.rectTransform.anchorMax = new Vector2(1, 1);
            _scoreLabel.rectTransform.offsetMin = Vector2.zero;
            _scoreLabel.rectTransform.offsetMax = Vector2.zero;
            _scoreLabel.alignment = TextAnchor.MiddleCenter;
            _scoreLabel.fontSize = 20;
            _scoreLabel.color = _theme?.TextSecondary ?? Color.gray;

            // Score value
            _scoreValue = CreateText("ScoreValue", scoreRect, "0");
            _scoreValue.rectTransform.anchorMin = new Vector2(0, 0);
            _scoreValue.rectTransform.anchorMax = new Vector2(1, 0.6f);
            _scoreValue.rectTransform.offsetMin = Vector2.zero;
            _scoreValue.rectTransform.offsetMax = Vector2.zero;
            _scoreValue.alignment = TextAnchor.MiddleCenter;
            _scoreValue.fontSize = _theme?.ScoreFontSize ?? 64;
            _scoreValue.fontStyle = FontStyle.Bold;
        }

        private void CreatePrizeSection()
        {
            var prizeGO = new GameObject("PrizeSection");
            prizeGO.transform.SetParent(_panel, false);

            var prizeRect = prizeGO.AddComponent<RectTransform>();
            prizeRect.anchorMin = new Vector2(0, 1);
            prizeRect.anchorMax = new Vector2(1, 1);
            prizeRect.pivot = new Vector2(0.5f, 1);
            prizeRect.anchoredPosition = new Vector2(0, -310);
            prizeRect.sizeDelta = new Vector2(0, 60);

            // Prize label
            _prizeLabel = CreateText("PrizeLabel", prizeRect, "PRIZE WON");
            _prizeLabel.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _prizeLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
            _prizeLabel.rectTransform.offsetMin = new Vector2(40, 0);
            _prizeLabel.rectTransform.offsetMax = Vector2.zero;
            _prizeLabel.alignment = TextAnchor.MiddleLeft;
            _prizeLabel.fontSize = 18;
            _prizeLabel.color = _theme?.TextSecondary ?? Color.gray;

            // Prize value
            _prizeValue = CreateText("PrizeValue", prizeRect, "$0.00");
            _prizeValue.rectTransform.anchorMin = new Vector2(0, 0);
            _prizeValue.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            _prizeValue.rectTransform.offsetMin = new Vector2(40, 0);
            _prizeValue.rectTransform.offsetMax = Vector2.zero;
            _prizeValue.alignment = TextAnchor.MiddleLeft;
            _prizeValue.fontSize = 28;
            _prizeValue.fontStyle = FontStyle.Bold;
            _prizeValue.color = _theme?.SuccessColor ?? Color.green;

            // XP earned
            _xpText = CreateText("XPText", prizeRect, "+100 XP");
            _xpText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _xpText.rectTransform.anchorMax = new Vector2(1, 1);
            _xpText.rectTransform.offsetMin = Vector2.zero;
            _xpText.rectTransform.offsetMax = new Vector2(-40, 0);
            _xpText.alignment = TextAnchor.MiddleRight;
            _xpText.fontSize = 24;
            _xpText.color = _theme?.AccentColor ?? Color.yellow;
        }

        private void CreateStatsSection()
        {
            var statsGO = new GameObject("StatsSection");
            statsGO.transform.SetParent(_panel, false);

            _statsContainer = statsGO.AddComponent<RectTransform>();
            _statsContainer.anchorMin = new Vector2(0, 1);
            _statsContainer.anchorMax = new Vector2(1, 1);
            _statsContainer.pivot = new Vector2(0.5f, 1);
            _statsContainer.anchoredPosition = new Vector2(0, -380);
            _statsContainer.sizeDelta = new Vector2(0, 40);

            // Rank text
            _rankText = CreateText("RankText", _statsContainer, "Rank: 1st");
            _rankText.rectTransform.anchorMin = new Vector2(0, 0);
            _rankText.rectTransform.anchorMax = new Vector2(1, 1);
            _rankText.rectTransform.offsetMin = Vector2.zero;
            _rankText.rectTransform.offsetMax = Vector2.zero;
            _rankText.alignment = TextAnchor.MiddleCenter;
            _rankText.fontSize = 20;
            _rankText.color = _theme?.TextSecondary ?? Color.gray;
        }

        private void CreateButtons()
        {
            // Button container
            var buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(_panel, false);

            var buttonRect = buttonContainer.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 20);
            buttonRect.sizeDelta = new Vector2(0, 60);

            var layout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Continue button
            _continueButton = CreateButton("Continue", buttonRect.transform, "CONTINUE", OnContinueClicked);
            _continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

            // Play again button  
            _playAgainButton = CreateButton("PlayAgain", buttonRect.transform, "PLAY AGAIN", OnPlayAgainClicked);
            _playAgainButton.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

            // Style play again as secondary
            var playAgainImage = _playAgainButton.GetComponent<Image>();
            if (playAgainImage != null)
            {
                playAgainImage.color = _theme?.SecondaryButton.NormalColor ?? Color.gray;
            }
        }

        // =============================================================================
        // PUBLIC METHODS
        // =============================================================================

        /// <summary>
        /// Set the match result to display.
        /// </summary>
        public void SetResult(MatchResult result)
        {
            _result = result;

            if (_animateResults)
            {
                StartCoroutine(AnimateResultsSequence());
            }
            else
            {
                DisplayResultsImmediate();
            }
        }

        private void DisplayResultsImmediate()
        {
            // Outcome
            _outcomeText.text = GetOutcomeText(_result.Outcome);
            _outcomeText.color = _theme?.GetOutcomeColor(_result.Outcome) ?? Color.white;
            _outcomeIcon.color = _theme?.GetOutcomeColor(_result.Outcome) ?? Color.white;

            // Score
            _scoreValue.text = ScoreDisplay.Format(_result.FinalScore);

            // Prize
            if (_result.PrizeWon > 0)
            {
                _prizeValue.text = $"+{_result.PrizeWon:F2} {_result.Currency}";
                _prizeValue.gameObject.SetActive(true);
                _prizeLabel.gameObject.SetActive(true);
            }
            else
            {
                _prizeValue.gameObject.SetActive(false);
                _prizeLabel.gameObject.SetActive(false);
            }

            // XP
            _xpText.text = $"+{_result.XpEarned} XP";

            // Rank
            _rankText.text = $"Rank: {ScoreDisplay.GetOrdinal(_result.FinalRank)}";
        }

        private IEnumerator AnimateResultsSequence()
        {
            // Initially hide everything
            _outcomeText.color = new Color(_outcomeText.color.r, _outcomeText.color.g, _outcomeText.color.b, 0);
            _scoreValue.text = "0";

            yield return new WaitForSeconds(_resultDisplayDelay);

            // Animate outcome text
            var outcomeColor = _theme?.GetOutcomeColor(_result.Outcome) ?? Color.white;
            _outcomeText.text = GetOutcomeText(_result.Outcome);
            _outcomeIcon.color = outcomeColor;

            float elapsed = 0;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / 0.5f);
                _outcomeText.color = new Color(outcomeColor.r, outcomeColor.g, outcomeColor.b, alpha);
                yield return null;
            }
            _outcomeText.color = outcomeColor;

            yield return new WaitForSeconds(0.3f);

            // Animate score count-up
            _scoreAnimator.SetImmediate(0);
            _scoreAnimator.SetTarget(_result.FinalScore);

            float countUpStart = Time.time;
            while (Time.time - countUpStart < _scoreCountUpDuration)
            {
                _scoreAnimator.UpdateLinear(_result.FinalScore / _scoreCountUpDuration);
                _scoreValue.text = ScoreDisplay.Format(_scoreAnimator.DisplayedScore);
                yield return null;
            }
            _scoreValue.text = ScoreDisplay.Format(_result.FinalScore);

            yield return new WaitForSeconds(0.3f);

            // Show prize
            if (_result.PrizeWon > 0)
            {
                _prizeValue.text = $"+{_result.PrizeWon:F2} {_result.Currency}";
                _prizeValue.gameObject.SetActive(true);
                _prizeLabel.gameObject.SetActive(true);

                // Pop animation
                _prizeValue.transform.localScale = Vector3.one * 1.3f;
                elapsed = 0;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.deltaTime;
                    _prizeValue.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, elapsed / 0.2f);
                    yield return null;
                }
            }

            // Show XP
            _xpText.text = $"+{_result.XpEarned} XP";

            // Show rank
            _rankText.text = $"Rank: {ScoreDisplay.GetOrdinal(_result.FinalRank)}";
        }

        private string GetOutcomeText(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Win => "VICTORY!",
                MatchOutcome.Loss => "DEFEAT",
                MatchOutcome.Tie => "DRAW",
                MatchOutcome.Forfeit => "FORFEITED",
                MatchOutcome.Pending => "AWAITING RESULTS...",
                _ => "MATCH COMPLETE"
            };
        }

        // =============================================================================
        // THEME
        // =============================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_result != null)
            {
                _outcomeText.color = theme.GetOutcomeColor(_result.Outcome);
                _outcomeIcon.color = theme.GetOutcomeColor(_result.Outcome);
            }

            if (_scoreLabel != null) _scoreLabel.color = theme.TextSecondary;
            if (_scoreValue != null) _scoreValue.color = theme.TextPrimary;
            if (_prizeValue != null) _prizeValue.color = theme.SuccessColor;
            if (_xpText != null) _xpText.color = theme.AccentColor;
            if (_rankText != null) _rankText.color = theme.TextSecondary;
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnContinueClicked()
        {
            OnContinue?.Invoke();
            
            // Return to Deskillz app
            DeskillzManager.Instance?.ReturnToApp();
        }

        private void OnPlayAgainClicked()
        {
            OnPlayAgain?.Invoke();
        }
    }
}

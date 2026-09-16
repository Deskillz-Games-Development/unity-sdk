// =============================================================================
// Deskillz SDK for Unity - Pause Request UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Social;

namespace Deskillz.UI.Social
{
    /// <summary>
    /// UI component for pause requests and voting in social games.
    /// Shows pause status, vote progress, and countdown.
    /// </summary>
    public class PauseRequestUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when player votes on a pause request</summary>
        public event Action<bool> OnVoteSubmitted;

        /// <summary>Called when player requests a pause</summary>
        public event Action OnPauseRequested;

        /// <summary>Called when resume is requested</summary>
        public event Action OnResumeRequested;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        // Request button (shown when not paused)
        private Button _requestPauseButton;
        private TextMeshProUGUI _requestButtonText;
        private TextMeshProUGUI _pausesRemainingText;

        // Vote panel (shown when vote in progress)
        private GameObject _votePanelContainer;
        private TextMeshProUGUI _voteTitle;
        private TextMeshProUGUI _requesterText;
        private TextMeshProUGUI _voteProgressText;
        private Slider _voteProgressBar;
        private TextMeshProUGUI _voteTimerText;
        private Button _approveButton;
        private Button _denyButton;
        private GameObject _waitingForVotesText;

        // Paused panel (shown when game is paused)
        private GameObject _pausedPanelContainer;
        private TextMeshProUGUI _pausedTitle;
        private TextMeshProUGUI _pauseReasonText;
        private TextMeshProUGUI _pauseTimerText;
        private Slider _pauseTimerBar;
        private Button _resumeButton;

        // Vote indicators
        private List<VoteIndicatorUI> _voteIndicators = new List<VoteIndicatorUI>();

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isPaused;
        private bool _isVoting;
        private bool _hasVoted;
        private int _pausesRemaining;
        private float _voteTimeRemaining;
        private float _pauseTimeRemaining;
        private float _totalPauseTime;
        private string _requesterId;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(280, 180);

            // Create all panels
            CreateRequestButton();
            CreateVotePanel();
            CreatePausedPanel();

            // Subscribe to events
            SocialGameManager.OnPauseRequested += HandlePauseRequested;
            SocialGameManager.OnPauseVoteUpdated += HandleVoteUpdated;
            SocialGameManager.OnGamePaused += HandleGamePaused;
            SocialGameManager.OnGameResumed += HandleGameResumed;

            // Initial state
            ShowRequestButton();
        }

        private void CreateRequestButton()
        {
            var requestContainer = new GameObject("RequestContainer");
            requestContainer.transform.SetParent(transform, false);

            var requestRect = requestContainer.AddComponent<RectTransform>();
            requestRect.anchorMin = Vector2.zero;
            requestRect.anchorMax = Vector2.one;
            requestRect.offsetMin = Vector2.zero;
            requestRect.offsetMax = Vector2.zero;

            var requestLayout = requestContainer.AddComponent<VerticalLayoutGroup>();
            requestLayout.childAlignment = TextAnchor.MiddleCenter;
            requestLayout.spacing = 8;

            // Request button
            var buttonGO = new GameObject("RequestButton");
            buttonGO.transform.SetParent(requestContainer.transform, false);

            var buttonLayout = buttonGO.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 140;
            buttonLayout.preferredHeight = 40;

            var buttonBg = buttonGO.AddComponent<Image>();
            buttonBg.color = new Color(0.3f, 0.3f, 0.35f);

            _requestPauseButton = buttonGO.AddComponent<Button>();
            _requestPauseButton.onClick.AddListener(OnRequestPauseClicked);

            _requestButtonText = UIComponents.CreateText(buttonGO.transform, "Request Pause", 14);
            _requestButtonText.alignment = TextAlignmentOptions.Center;

            var buttonTextRect = _requestButtonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;

            // Pauses remaining text
            _pausesRemainingText = UIComponents.CreateText(requestContainer.transform, "3 pauses remaining", 11);
            _pausesRemainingText.alignment = TextAlignmentOptions.Center;
            _pausesRemainingText.color = _theme?.TextSecondary ?? Color.gray;
        }

        private void CreateVotePanel()
        {
            _votePanelContainer = new GameObject("VotePanel");
            _votePanelContainer.transform.SetParent(transform, false);

            var voteRect = _votePanelContainer.AddComponent<RectTransform>();
            voteRect.anchorMin = Vector2.zero;
            voteRect.anchorMax = Vector2.one;
            voteRect.offsetMin = Vector2.zero;
            voteRect.offsetMax = Vector2.zero;

            var voteBg = _votePanelContainer.AddComponent<Image>();
            voteBg.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            var voteLayout = _votePanelContainer.AddComponent<VerticalLayoutGroup>();
            voteLayout.padding = new RectOffset(16, 16, 12, 12);
            voteLayout.spacing = 8;
            voteLayout.childAlignment = TextAnchor.MiddleCenter;

            // Title
            _voteTitle = UIComponents.CreateText(_votePanelContainer.transform, "Pause Requested", 16);
            _voteTitle.alignment = TextAlignmentOptions.Center;
            _voteTitle.fontStyle = FontStyles.Bold;

            // Requester
            _requesterText = UIComponents.CreateText(_votePanelContainer.transform, "by Player", 12);
            _requesterText.alignment = TextAlignmentOptions.Center;
            _requesterText.color = _theme?.TextSecondary ?? Color.gray;

            // Vote progress
            var progressRow = new GameObject("ProgressRow");
            progressRow.transform.SetParent(_votePanelContainer.transform, false);

            var progressLayout = progressRow.AddComponent<HorizontalLayoutGroup>();
            progressLayout.spacing = 8;
            progressLayout.childForceExpandWidth = false;
            progressLayout.childAlignment = TextAnchor.MiddleCenter;

            var barGO = new GameObject("ProgressBar");
            barGO.transform.SetParent(progressRow.transform, false);

            var barLayout = barGO.AddComponent<LayoutElement>();
            barLayout.preferredWidth = 120;
            barLayout.preferredHeight = 12;

            _voteProgressBar = UIComponents.CreateProgressBar(barGO.transform);

            _voteProgressText = UIComponents.CreateText(progressRow.transform, "0/0", 12);

            // Timer
            _voteTimerText = UIComponents.CreateText(_votePanelContainer.transform, "30s", 14);
            _voteTimerText.alignment = TextAlignmentOptions.Center;
            _voteTimerText.color = _theme?.WarningColor ?? new Color(1f, 0.7f, 0f);

            // Vote buttons row
            var buttonsRow = new GameObject("ButtonsRow");
            buttonsRow.transform.SetParent(_votePanelContainer.transform, false);

            var buttonsLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 12;
            buttonsLayout.childForceExpandWidth = true;

            // Deny button
            var denyGO = new GameObject("DenyButton");
            denyGO.transform.SetParent(buttonsRow.transform, false);

            var denyLayout = denyGO.AddComponent<LayoutElement>();
            denyLayout.preferredHeight = 36;

            var denyBg = denyGO.AddComponent<Image>();
            denyBg.color = new Color(0.5f, 0.2f, 0.2f);

            _denyButton = denyGO.AddComponent<Button>();
            _denyButton.onClick.AddListener(() => OnVoteClicked(false));

            var denyText = UIComponents.CreateText(denyGO.transform, "Deny", 14);
            denyText.alignment = TextAlignmentOptions.Center;

            // Approve button
            var approveGO = new GameObject("ApproveButton");
            approveGO.transform.SetParent(buttonsRow.transform, false);

            var approveLayout = approveGO.AddComponent<LayoutElement>();
            approveLayout.preferredHeight = 36;

            var approveBg = approveGO.AddComponent<Image>();
            approveBg.color = new Color(0.2f, 0.5f, 0.2f);

            _approveButton = approveGO.AddComponent<Button>();
            _approveButton.onClick.AddListener(() => OnVoteClicked(true));

            var approveText = UIComponents.CreateText(approveGO.transform, "Approve", 14);
            approveText.alignment = TextAlignmentOptions.Center;

            // Waiting text (shown after voting)
            var waitingGO = new GameObject("WaitingText");
            waitingGO.transform.SetParent(_votePanelContainer.transform, false);

            _waitingForVotesText = waitingGO;

            var waitingText = UIComponents.CreateText(waitingGO.transform, "Waiting for other votes...", 12);
            waitingText.alignment = TextAlignmentOptions.Center;
            waitingText.color = _theme?.TextSecondary ?? Color.gray;

            _waitingForVotesText.SetActive(false);
            _votePanelContainer.SetActive(false);
        }

        private void CreatePausedPanel()
        {
            _pausedPanelContainer = new GameObject("PausedPanel");
            _pausedPanelContainer.transform.SetParent(transform, false);

            var pausedRect = _pausedPanelContainer.AddComponent<RectTransform>();
            pausedRect.anchorMin = Vector2.zero;
            pausedRect.anchorMax = Vector2.one;
            pausedRect.offsetMin = Vector2.zero;
            pausedRect.offsetMax = Vector2.zero;

            var pausedBg = _pausedPanelContainer.AddComponent<Image>();
            pausedBg.color = new Color(0.15f, 0.12f, 0.1f);

            var pausedLayout = _pausedPanelContainer.AddComponent<VerticalLayoutGroup>();
            pausedLayout.padding = new RectOffset(16, 16, 16, 16);
            pausedLayout.spacing = 10;
            pausedLayout.childAlignment = TextAnchor.MiddleCenter;

            // Paused icon/title
            _pausedTitle = UIComponents.CreateText(_pausedPanelContainer.transform, "GAME PAUSED", 20);
            _pausedTitle.alignment = TextAlignmentOptions.Center;
            _pausedTitle.fontStyle = FontStyles.Bold;
            _pausedTitle.color = _theme?.WarningColor ?? new Color(1f, 0.7f, 0f);

            // Reason
            _pauseReasonText = UIComponents.CreateText(_pausedPanelContainer.transform, "Requested by Player", 12);
            _pauseReasonText.alignment = TextAlignmentOptions.Center;
            _pauseReasonText.color = _theme?.TextSecondary ?? Color.gray;

            // Timer bar
            var timerContainer = new GameObject("TimerContainer");
            timerContainer.transform.SetParent(_pausedPanelContainer.transform, false);

            var timerContainerLayout = timerContainer.AddComponent<LayoutElement>();
            timerContainerLayout.preferredHeight = 20;

            var timerLayout = timerContainer.AddComponent<VerticalLayoutGroup>();
            timerLayout.spacing = 4;

            var barGO = new GameObject("TimerBar");
            barGO.transform.SetParent(timerContainer.transform, false);

            var barLayout = barGO.AddComponent<LayoutElement>();
            barLayout.preferredHeight = 8;

            _pauseTimerBar = UIComponents.CreateProgressBar(barGO.transform);

            _pauseTimerText = UIComponents.CreateText(timerContainer.transform, "5:00 remaining", 12);
            _pauseTimerText.alignment = TextAlignmentOptions.Center;

            // Resume button (only shown to pause requester or host)
            var resumeGO = new GameObject("ResumeButton");
            resumeGO.transform.SetParent(_pausedPanelContainer.transform, false);

            var resumeLayout = resumeGO.AddComponent<LayoutElement>();
            resumeLayout.preferredWidth = 120;
            resumeLayout.preferredHeight = 36;

            var resumeBg = resumeGO.AddComponent<Image>();
            resumeBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.5f, 0.8f);

            _resumeButton = resumeGO.AddComponent<Button>();
            _resumeButton.onClick.AddListener(OnResumeClicked);

            var resumeText = UIComponents.CreateText(resumeGO.transform, "Resume", 14);
            resumeText.alignment = TextAlignmentOptions.Center;
            resumeText.fontStyle = FontStyles.Bold;

            var resumeTextRect = resumeText.GetComponent<RectTransform>();
            resumeTextRect.anchorMin = Vector2.zero;
            resumeTextRect.anchorMax = Vector2.one;

            _pausedPanelContainer.SetActive(false);
        }

        private void OnDestroy()
        {
            SocialGameManager.OnPauseRequested -= HandlePauseRequested;
            SocialGameManager.OnPauseVoteUpdated -= HandleVoteUpdated;
            SocialGameManager.OnGamePaused -= HandleGamePaused;
            SocialGameManager.OnGameResumed -= HandleGameResumed;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the number of pauses remaining for the local player.
        /// </summary>
        public void SetPausesRemaining(int remaining)
        {
            _pausesRemaining = remaining;
            _pausesRemainingText.text = remaining > 0 
                ? $"{remaining} pause{(remaining == 1 ? "" : "s")} remaining" 
                : "No pauses remaining";
            _requestPauseButton.interactable = remaining > 0;
        }

        /// <summary>
        /// Show a pause vote in progress.
        /// </summary>
        public void ShowVote(string requesterName, int approveCount, int totalPlayers, float timeRemaining)
        {
            _isVoting = true;
            _hasVoted = false;
            _voteTimeRemaining = timeRemaining;

            _requesterText.text = $"by {requesterName}";
            UpdateVoteProgress(approveCount, totalPlayers);

            ShowVotePanel();
        }

        /// <summary>
        /// Show the paused state.
        /// </summary>
        public void ShowPaused(string requesterName, float totalDuration, bool canResume)
        {
            _isPaused = true;
            _totalPauseTime = totalDuration;
            _pauseTimeRemaining = totalDuration;

            _pauseReasonText.text = $"Requested by {requesterName}";
            _resumeButton.gameObject.SetActive(canResume);

            ShowPausedPanel();
        }

        // =====================================================================
        // UPDATE
        // =====================================================================

        private void Update()
        {
            if (_isVoting && _voteTimeRemaining > 0)
            {
                _voteTimeRemaining -= Time.deltaTime;
                UpdateVoteTimer();
            }

            if (_isPaused && _pauseTimeRemaining > 0)
            {
                _pauseTimeRemaining -= Time.deltaTime;
                UpdatePauseTimer();
            }
        }

        private void UpdateVoteTimer()
        {
            int seconds = Mathf.CeilToInt(_voteTimeRemaining);
            _voteTimerText.text = $"{seconds}s";

            if (_voteTimeRemaining < 10)
            {
                _voteTimerText.color = _theme?.ErrorColor ?? Color.red;
            }
        }

        private void UpdatePauseTimer()
        {
            int totalSeconds = Mathf.CeilToInt(_pauseTimeRemaining);
            int mins = totalSeconds / 60;
            int secs = totalSeconds % 60;
            _pauseTimerText.text = $"{mins}:{secs:D2} remaining";

            float progress = _totalPauseTime > 0 ? _pauseTimeRemaining / _totalPauseTime : 0;
            _pauseTimerBar.value = progress;
        }

        private void UpdateVoteProgress(int approveCount, int totalPlayers)
        {
            _voteProgressText.text = $"{approveCount}/{totalPlayers}";
            _voteProgressBar.value = totalPlayers > 0 ? (float)approveCount / totalPlayers : 0;
        }

        // =====================================================================
        // STATE TRANSITIONS
        // =====================================================================

        private void ShowRequestButton()
        {
            _requestPauseButton.transform.parent.gameObject.SetActive(true);
            _votePanelContainer.SetActive(false);
            _pausedPanelContainer.SetActive(false);
        }

        private void ShowVotePanel()
        {
            _requestPauseButton.transform.parent.gameObject.SetActive(false);
            _votePanelContainer.SetActive(true);
            _pausedPanelContainer.SetActive(false);

            _approveButton.gameObject.SetActive(!_hasVoted);
            _denyButton.gameObject.SetActive(!_hasVoted);
            _waitingForVotesText.SetActive(_hasVoted);
        }

        private void ShowPausedPanel()
        {
            _requestPauseButton.transform.parent.gameObject.SetActive(false);
            _votePanelContainer.SetActive(false);
            _pausedPanelContainer.SetActive(true);
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnRequestPauseClicked()
        {
            if (_pausesRemaining > 0)
            {
                OnPauseRequested?.Invoke();
            }
        }

        private void OnVoteClicked(bool approve)
        {
            _hasVoted = true;
            _approveButton.gameObject.SetActive(false);
            _denyButton.gameObject.SetActive(false);
            _waitingForVotesText.SetActive(true);

            OnVoteSubmitted?.Invoke(approve);
        }

        private void OnResumeClicked()
        {
            OnResumeRequested?.Invoke();
        }

        private void HandlePauseRequested(PauseRequest request)
        {
            _requesterId = request.RequesterId;
            ShowVote(request.RequesterUsername, 1, request.TotalPlayers, request.VoteTimeout);
        }

        private void HandleVoteUpdated(int approveCount, int denyCount, int totalPlayers)
        {
            UpdateVoteProgress(approveCount, totalPlayers);
        }

        private void HandleGamePaused(float duration)
        {
            _isPaused = true;
            _isVoting = false;

            var localPlayerId = SocialGameManager.GetLocalPlayer()?.Id;
            bool canResume = localPlayerId == _requesterId;

            ShowPaused("Player", duration, canResume);
        }

        private void HandleGameResumed()
        {
            _isPaused = false;
            _isVoting = false;
            ShowRequestButton();
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_pausesRemainingText != null) _pausesRemainingText.color = theme.TextSecondary;
            if (_requesterText != null) _requesterText.color = theme.TextSecondary;
            if (_voteTimerText != null) _voteTimerText.color = theme.WarningColor;
            if (_pausedTitle != null) _pausedTitle.color = theme.WarningColor;
            if (_pauseReasonText != null) _pauseReasonText.color = theme.TextSecondary;
        }
    }

    /// <summary>
    /// Small indicator showing a player's vote status.
    /// </summary>
    public class VoteIndicatorUI : MonoBehaviour
    {
        public Image AvatarImage;
        public Image StatusIndicator;
        public TextMeshProUGUI NameText;

        public void SetVoteStatus(bool? voted, bool? approved)
        {
            if (!voted.HasValue)
            {
                StatusIndicator.color = Color.gray;
            }
            else if (approved == true)
            {
                StatusIndicator.color = Color.green;
            }
            else
            {
                StatusIndicator.color = Color.red;
            }
        }
    }
}
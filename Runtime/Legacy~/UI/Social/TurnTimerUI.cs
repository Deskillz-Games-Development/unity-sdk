// =============================================================================
// Deskillz SDK for Unity - Turn Timer UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Social;

namespace Deskillz.UI.Social
{
    /// <summary>
    /// UI component displaying the turn timer in social games.
    /// Shows countdown with visual feedback and warnings.
    /// </summary>
    public class TurnTimerUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when timer expires</summary>
        public event Action OnTimerExpired;

        /// <summary>Called when warning threshold reached</summary>
        public event Action OnWarningThreshold;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _container;
        private Image _background;
        private Image _progressRing;
        private Image _progressFill;
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _labelText;
        private Image _warningPulse;

        // Optional player indicator
        private Image _playerAvatar;
        private TextMeshProUGUI _playerNameText;

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        [Header("Timer Settings")]
        public float WarningThreshold = 10f;
        public float CriticalThreshold = 5f;
        public bool ShowPlayerInfo = true;
        public bool PulseOnWarning = true;
        public bool PlaySoundOnWarning = true;

        [Header("Colors")]
        public Color NormalColor = new Color(0.3f, 0.7f, 0.3f);
        public Color WarningColor = new Color(1f, 0.7f, 0f);
        public Color CriticalColor = new Color(1f, 0.3f, 0.3f);

        // =====================================================================
        // STATE
        // =====================================================================

        private float _totalTime;
        private float _remainingTime;
        private bool _isRunning;
        private bool _isMyTurn;
        private bool _warningTriggered;
        private float _pulseTime;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            _container = GetComponent<RectTransform>();
            if (_container == null)
            {
                _container = gameObject.AddComponent<RectTransform>();
            }

            _container.sizeDelta = new Vector2(120, 120);

            // Background circle
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(transform, false);

            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _background = bgGO.AddComponent<Image>();
            _background.color = new Color(0.1f, 0.1f, 0.15f);
            _background.type = Image.Type.Filled;
            _background.fillMethod = Image.FillMethod.Radial360;
            _background.fillOrigin = (int)Image.Origin360.Top;
            _background.fillClockwise = true;
            _background.fillAmount = 1f;

            // Progress ring background
            var ringBgGO = new GameObject("RingBackground");
            ringBgGO.transform.SetParent(transform, false);

            var ringBgRect = ringBgGO.AddComponent<RectTransform>();
            ringBgRect.anchorMin = Vector2.zero;
            ringBgRect.anchorMax = Vector2.one;
            ringBgRect.offsetMin = new Vector2(8, 8);
            ringBgRect.offsetMax = new Vector2(-8, -8);

            _progressRing = ringBgGO.AddComponent<Image>();
            _progressRing.color = new Color(0.2f, 0.2f, 0.25f);

            // Progress fill
            var fillGO = new GameObject("ProgressFill");
            fillGO.transform.SetParent(transform, false);

            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(8, 8);
            fillRect.offsetMax = new Vector2(-8, -8);

            _progressFill = fillGO.AddComponent<Image>();
            _progressFill.color = NormalColor;
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Radial360;
            _progressFill.fillOrigin = (int)Image.Origin360.Top;
            _progressFill.fillClockwise = false;
            _progressFill.fillAmount = 1f;

            // Center area (for timer text)
            var centerGO = new GameObject("Center");
            centerGO.transform.SetParent(transform, false);

            var centerRect = centerGO.AddComponent<RectTransform>();
            centerRect.anchorMin = Vector2.zero;
            centerRect.anchorMax = Vector2.one;
            centerRect.offsetMin = new Vector2(20, 20);
            centerRect.offsetMax = new Vector2(-20, -20);

            var centerBg = centerGO.AddComponent<Image>();
            centerBg.color = new Color(0.08f, 0.08f, 0.12f);

            var centerLayout = centerGO.AddComponent<VerticalLayoutGroup>();
            centerLayout.childAlignment = TextAnchor.MiddleCenter;
            centerLayout.childForceExpandHeight = false;

            // Timer text
            _timerText = UIComponents.CreateText(centerGO.transform, "30", 28);
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.fontStyle = FontStyles.Bold;

            // Label
            _labelText = UIComponents.CreateText(centerGO.transform, "YOUR TURN", 8);
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.color = _theme?.TextSecondary ?? Color.gray;

            // Warning pulse overlay
            var pulseGO = new GameObject("WarningPulse");
            pulseGO.transform.SetParent(transform, false);

            var pulseRect = pulseGO.AddComponent<RectTransform>();
            pulseRect.anchorMin = Vector2.zero;
            pulseRect.anchorMax = Vector2.one;
            pulseRect.offsetMin = Vector2.zero;
            pulseRect.offsetMax = Vector2.zero;

            _warningPulse = pulseGO.AddComponent<Image>();
            _warningPulse.color = new Color(1f, 0f, 0f, 0f);
            _warningPulse.raycastTarget = false;

            // Player info (optional, positioned above timer)
            if (ShowPlayerInfo)
            {
                CreatePlayerInfo();
            }

            // Subscribe to events
            SocialGameManager.OnTurnStarted += HandleTurnStarted;
            SocialGameManager.OnTurnTimerTick += HandleTimerTick;
            SocialGameManager.OnTurnTimerExpired += HandleTimerExpired;
        }

        private void CreatePlayerInfo()
        {
            var infoGO = new GameObject("PlayerInfo");
            infoGO.transform.SetParent(transform, false);

            var infoRect = infoGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 1f);
            infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.anchoredPosition = new Vector2(0, 10);
            infoRect.sizeDelta = new Vector2(100, 30);

            var infoLayout = infoGO.AddComponent<HorizontalLayoutGroup>();
            infoLayout.spacing = 6;
            infoLayout.childAlignment = TextAnchor.MiddleCenter;
            infoLayout.childForceExpandWidth = false;

            // Avatar
            var avatarGO = new GameObject("Avatar");
            avatarGO.transform.SetParent(infoGO.transform, false);

            var avatarLayout = avatarGO.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 24;
            avatarLayout.preferredHeight = 24;

            _playerAvatar = avatarGO.AddComponent<Image>();
            _playerAvatar.color = Color.gray;

            // Name
            _playerNameText = UIComponents.CreateText(infoGO.transform, "Player", 12);
            _playerNameText.fontStyle = FontStyles.Bold;
        }

        private void OnDestroy()
        {
            SocialGameManager.OnTurnStarted -= HandleTurnStarted;
            SocialGameManager.OnTurnTimerTick -= HandleTimerTick;
            SocialGameManager.OnTurnTimerExpired -= HandleTimerExpired;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Start the timer with specified duration.
        /// </summary>
        public void StartTimer(float duration, bool isMyTurn = true, string playerName = null)
        {
            _totalTime = duration;
            _remainingTime = duration;
            _isRunning = true;
            _isMyTurn = isMyTurn;
            _warningTriggered = false;

            _labelText.text = isMyTurn ? "YOUR TURN" : "WAITING";

            if (_playerNameText != null && !string.IsNullOrEmpty(playerName))
            {
                _playerNameText.text = playerName;
            }

            UpdateDisplay();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void StopTimer()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Hide the timer.
        /// </summary>
        public void HideTimer()
        {
            _isRunning = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Set remaining time manually (for sync with server).
        /// </summary>
        public void SetRemainingTime(float time)
        {
            _remainingTime = time;
            UpdateDisplay();
        }

        // =====================================================================
        // UPDATE
        // =====================================================================

        private void Update()
        {
            if (!_isRunning) return;

            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
                _isRunning = false;
                OnTimerExpired?.Invoke();
            }

            UpdateDisplay();

            // Check warning threshold
            if (!_warningTriggered && _remainingTime <= WarningThreshold)
            {
                _warningTriggered = true;
                OnWarningThreshold?.Invoke();
            }

            // Pulse effect
            if (PulseOnWarning && _remainingTime <= CriticalThreshold && _remainingTime > 0)
            {
                _pulseTime += Time.deltaTime * 4f;
                float alpha = (Mathf.Sin(_pulseTime) + 1f) * 0.15f;
                _warningPulse.color = new Color(1f, 0f, 0f, alpha);
            }
            else
            {
                _warningPulse.color = new Color(1f, 0f, 0f, 0f);
            }
        }

        private void UpdateDisplay()
        {
            // Update timer text
            int seconds = Mathf.CeilToInt(_remainingTime);
            _timerText.text = seconds.ToString();

            // Update progress fill
            float progress = _totalTime > 0 ? _remainingTime / _totalTime : 0;
            _progressFill.fillAmount = progress;

            // Update colors based on remaining time
            Color timerColor;
            if (_remainingTime <= CriticalThreshold)
            {
                timerColor = CriticalColor;
                _progressFill.color = CriticalColor;
            }
            else if (_remainingTime <= WarningThreshold)
            {
                timerColor = WarningColor;
                _progressFill.color = WarningColor;
            }
            else
            {
                timerColor = NormalColor;
                _progressFill.color = NormalColor;
            }

            _timerText.color = timerColor;
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandleTurnStarted(float timeLimit)
        {
            if (timeLimit > 0)
            {
                StartTimer(timeLimit, true);
            }
        }

        private void HandleTimerTick(float remaining)
        {
            // Sync with server time
            if (Mathf.Abs(_remainingTime - remaining) > 1f)
            {
                _remainingTime = remaining;
            }
        }

        private void HandleTimerExpired()
        {
            _isRunning = false;
            _remainingTime = 0;
            UpdateDisplay();
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_background != null) _background.color = new Color(0.1f, 0.1f, 0.15f);
            if (_labelText != null) _labelText.color = theme.TextSecondary;

            NormalColor = theme.SuccessColor;
            WarningColor = theme.WarningColor;
            CriticalColor = theme.ErrorColor;
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Social Game Settings UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Social;
using Deskillz.Host;

namespace Deskillz.UI.Social
{
    /// <summary>
    /// UI panel for configuring social game room settings.
    /// Allows setting point value, rake settings, timer options, and pause rules.
    /// </summary>
    public class SocialGameSettingsUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when settings are confirmed</summary>
        public event Action<SocialGameSettings> OnSettingsConfirmed;

        /// <summary>Called when cancel is clicked</summary>
        public event Action OnCancelled;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private TextMeshProUGUI _titleText;
        private Button _closeButton;

        // Point value
        private TextMeshProUGUI _pointValueLabel;
        private TMP_InputField _pointValueInput;
        private Slider _pointValueSlider;
        private TextMeshProUGUI _minBuyInText;

        // Rake settings
        private TextMeshProUGUI _rakeLabel;
        private Slider _rakePercentSlider;
        private TextMeshProUGUI _rakePercentText;
        private TMP_InputField _rakeCapInput;

        // Turn timer
        private Toggle _turnTimerToggle;
        private Slider _turnTimerSlider;
        private TextMeshProUGUI _turnTimerText;

        // Pause settings
        private Toggle _pauseEnabledToggle;
        private Slider _maxPausesSlider;
        private TextMeshProUGUI _maxPausesText;
        private Slider _pauseDurationSlider;
        private TextMeshProUGUI _pauseDurationText;

        // Revenue preview
        private TextMeshProUGUI _hostSharePreviewText;
        private TextMeshProUGUI _estimatedEarningsText;

        // Buttons
        private Button _confirmButton;
        private Button _cancelButton;

        // =====================================================================
        // STATE
        // =====================================================================

        private SocialGameSettings _settings;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Background overlay
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0, 0, 0, 0.85f);

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;

            // Modal container
            var modalGO = new GameObject("ModalContainer");
            modalGO.transform.SetParent(transform, false);

            var modalRect = modalGO.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(380, 600);

            var modalBg = modalGO.AddComponent<Image>();
            modalBg.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            var modalLayout = modalGO.AddComponent<VerticalLayoutGroup>();
            modalLayout.padding = new RectOffset(20, 20, 20, 20);
            modalLayout.spacing = 16;
            modalLayout.childForceExpandWidth = true;
            modalLayout.childForceExpandHeight = false;

            // Create sections
            CreateHeader(modalGO.transform);
            CreatePointValueSection(modalGO.transform);
            CreateRakeSection(modalGO.transform);
            CreateTurnTimerSection(modalGO.transform);
            CreatePauseSection(modalGO.transform);
            CreateRevenuePreview(modalGO.transform);
            CreateButtons(modalGO.transform);

            // Initialize default settings
            _settings = new SocialGameSettings();
            UpdateUI();
        }

        private void CreateHeader(Transform parent)
        {
            var headerRow = new GameObject("Header");
            headerRow.transform.SetParent(parent, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;

            _titleText = UIComponents.CreateText(headerRow.transform, "Social Game Settings", 18);
            _titleText.fontStyle = FontStyles.Bold;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(headerRow.transform, false);

            var closeLayout = closeGO.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 30;
            closeLayout.preferredHeight = 30;

            _closeButton = closeGO.AddComponent<Button>();
            _closeButton.onClick.AddListener(() => OnCancelled?.Invoke());

            var closeText = UIComponents.CreateText(closeGO.transform, "X", 18);
            closeText.alignment = TextAlignmentOptions.Center;
        }

        private void CreatePointValueSection(Transform parent)
        {
            var section = CreateSection(parent, "Point Value");

            // Slider row
            var sliderRow = new GameObject("SliderRow");
            sliderRow.transform.SetParent(section.transform, false);

            var sliderLayout = sliderRow.AddComponent<HorizontalLayoutGroup>();
            sliderLayout.spacing = 12;
            sliderLayout.childForceExpandWidth = false;

            // Slider
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(sliderRow.transform, false);

            var sliderLayoutElement = sliderGO.AddComponent<LayoutElement>();
            sliderLayoutElement.flexibleWidth = 1;
            sliderLayoutElement.preferredHeight = 30;

            _pointValueSlider = UIComponents.CreateSlider(sliderGO.transform);
            _pointValueSlider.minValue = 0.1f;
            _pointValueSlider.maxValue = 100f;
            _pointValueSlider.value = 1f;
            _pointValueSlider.onValueChanged.AddListener(OnPointValueChanged);

            // Input field
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(sliderRow.transform, false);

            var inputLayout = inputGO.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 80;
            inputLayout.preferredHeight = 30;

            _pointValueInput = UIComponents.CreateInputField(inputGO.transform, "$1.00");
            _pointValueInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            _pointValueInput.onEndEdit.AddListener(OnPointValueInputChanged);

            // Min buy-in info
            _minBuyInText = UIComponents.CreateText(section.transform, "Min Buy-In: $50.00", 11);
            _minBuyInText.color = _theme?.TextSecondary ?? Color.gray;
        }

        private void CreateRakeSection(Transform parent)
        {
            var section = CreateSection(parent, "Rake Settings");

            // Rake percent row
            var rakeRow = new GameObject("RakeRow");
            rakeRow.transform.SetParent(section.transform, false);

            var rakeLayout = rakeRow.AddComponent<HorizontalLayoutGroup>();
            rakeLayout.spacing = 12;
            rakeLayout.childForceExpandWidth = false;

            var rakeLabel = UIComponents.CreateText(rakeRow.transform, "Rake:", 14);

            var sliderGO = new GameObject("RakeSlider");
            sliderGO.transform.SetParent(rakeRow.transform, false);

            var sliderLayout = sliderGO.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1;
            sliderLayout.preferredHeight = 30;

            _rakePercentSlider = UIComponents.CreateSlider(sliderGO.transform);
            _rakePercentSlider.minValue = 1f;
            _rakePercentSlider.maxValue = 10f;
            _rakePercentSlider.value = 5f;
            _rakePercentSlider.wholeNumbers = true;
            _rakePercentSlider.onValueChanged.AddListener(OnRakeChanged);

            _rakePercentText = UIComponents.CreateText(rakeRow.transform, "5%", 14);
            _rakePercentText.fontStyle = FontStyles.Bold;

            // Rake cap row
            var capRow = new GameObject("CapRow");
            capRow.transform.SetParent(section.transform, false);

            var capLayout = capRow.AddComponent<HorizontalLayoutGroup>();
            capLayout.spacing = 12;
            capLayout.childForceExpandWidth = false;

            var capLabel = UIComponents.CreateText(capRow.transform, "Rake Cap:", 14);

            var capInputGO = new GameObject("CapInput");
            capInputGO.transform.SetParent(capRow.transform, false);

            var capInputLayout = capInputGO.AddComponent<LayoutElement>();
            capInputLayout.preferredWidth = 80;
            capInputLayout.preferredHeight = 30;

            _rakeCapInput = UIComponents.CreateInputField(capInputGO.transform, "$50.00");
            _rakeCapInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            _rakeCapInput.onEndEdit.AddListener(OnRakeCapChanged);
        }

        private void CreateTurnTimerSection(Transform parent)
        {
            var section = CreateSection(parent, "Turn Timer");

            // Enable toggle row
            var toggleRow = new GameObject("ToggleRow");
            toggleRow.transform.SetParent(section.transform, false);

            var toggleLayout = toggleRow.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 12;
            toggleLayout.childForceExpandWidth = false;

            var toggleLabel = UIComponents.CreateText(toggleRow.transform, "Enable Turn Timer:", 14);

            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(toggleRow.transform, false);

            _turnTimerToggle = UIComponents.CreateToggle(toggleGO.transform);
            _turnTimerToggle.isOn = true;
            _turnTimerToggle.onValueChanged.AddListener(OnTurnTimerToggleChanged);

            // Timer duration row
            var durationRow = new GameObject("DurationRow");
            durationRow.transform.SetParent(section.transform, false);

            var durationLayout = durationRow.AddComponent<HorizontalLayoutGroup>();
            durationLayout.spacing = 12;
            durationLayout.childForceExpandWidth = false;

            var durationLabel = UIComponents.CreateText(durationRow.transform, "Time:", 14);

            var timerSliderGO = new GameObject("TimerSlider");
            timerSliderGO.transform.SetParent(durationRow.transform, false);

            var timerSliderLayout = timerSliderGO.AddComponent<LayoutElement>();
            timerSliderLayout.flexibleWidth = 1;
            timerSliderLayout.preferredHeight = 30;

            _turnTimerSlider = UIComponents.CreateSlider(timerSliderGO.transform);
            _turnTimerSlider.minValue = 10f;
            _turnTimerSlider.maxValue = 120f;
            _turnTimerSlider.value = 30f;
            _turnTimerSlider.wholeNumbers = true;
            _turnTimerSlider.onValueChanged.AddListener(OnTurnTimerChanged);

            _turnTimerText = UIComponents.CreateText(durationRow.transform, "30s", 14);
            _turnTimerText.fontStyle = FontStyles.Bold;
        }

        private void CreatePauseSection(Transform parent)
        {
            var section = CreateSection(parent, "Pause Settings");

            // Enable toggle
            var toggleRow = new GameObject("ToggleRow");
            toggleRow.transform.SetParent(section.transform, false);

            var toggleLayout = toggleRow.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.spacing = 12;
            toggleLayout.childForceExpandWidth = false;

            var toggleLabel = UIComponents.CreateText(toggleRow.transform, "Allow Pauses:", 14);

            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(toggleRow.transform, false);

            _pauseEnabledToggle = UIComponents.CreateToggle(toggleGO.transform);
            _pauseEnabledToggle.isOn = true;
            _pauseEnabledToggle.onValueChanged.AddListener(OnPauseToggleChanged);

            // Max pauses row
            var maxRow = new GameObject("MaxPausesRow");
            maxRow.transform.SetParent(section.transform, false);

            var maxLayout = maxRow.AddComponent<HorizontalLayoutGroup>();
            maxLayout.spacing = 12;
            maxLayout.childForceExpandWidth = false;

            var maxLabel = UIComponents.CreateText(maxRow.transform, "Max per Player:", 14);

            var maxSliderGO = new GameObject("MaxSlider");
            maxSliderGO.transform.SetParent(maxRow.transform, false);

            var maxSliderLayout = maxSliderGO.AddComponent<LayoutElement>();
            maxSliderLayout.flexibleWidth = 1;
            maxSliderLayout.preferredHeight = 30;

            _maxPausesSlider = UIComponents.CreateSlider(maxSliderGO.transform);
            _maxPausesSlider.minValue = 1f;
            _maxPausesSlider.maxValue = 5f;
            _maxPausesSlider.value = 3f;
            _maxPausesSlider.wholeNumbers = true;
            _maxPausesSlider.onValueChanged.AddListener(v => _maxPausesText.text = $"{(int)v}");

            _maxPausesText = UIComponents.CreateText(maxRow.transform, "3", 14);
            _maxPausesText.fontStyle = FontStyles.Bold;

            // Pause duration row
            var durationRow = new GameObject("DurationRow");
            durationRow.transform.SetParent(section.transform, false);

            var durationLayout = durationRow.AddComponent<HorizontalLayoutGroup>();
            durationLayout.spacing = 12;
            durationLayout.childForceExpandWidth = false;

            var durationLabel = UIComponents.CreateText(durationRow.transform, "Max Duration:", 14);

            var durationSliderGO = new GameObject("DurationSlider");
            durationSliderGO.transform.SetParent(durationRow.transform, false);

            var durationSliderLayout = durationSliderGO.AddComponent<LayoutElement>();
            durationSliderLayout.flexibleWidth = 1;
            durationSliderLayout.preferredHeight = 30;

            _pauseDurationSlider = UIComponents.CreateSlider(durationSliderGO.transform);
            _pauseDurationSlider.minValue = 1f;
            _pauseDurationSlider.maxValue = 10f;
            _pauseDurationSlider.value = 5f;
            _pauseDurationSlider.wholeNumbers = true;
            _pauseDurationSlider.onValueChanged.AddListener(v => _pauseDurationText.text = $"{(int)v}m");

            _pauseDurationText = UIComponents.CreateText(durationRow.transform, "5m", 14);
            _pauseDurationText.fontStyle = FontStyles.Bold;
        }

        private void CreateRevenuePreview(Transform parent)
        {
            var section = CreateSection(parent, "Revenue Preview");

            var row = new GameObject("PreviewRow");
            row.transform.SetParent(section.transform, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 16;
            rowLayout.childForceExpandWidth = true;

            // Host share
            var hostContainer = new GameObject("HostShare");
            hostContainer.transform.SetParent(row.transform, false);

            var hostLayout = hostContainer.AddComponent<VerticalLayoutGroup>();
            hostLayout.childAlignment = TextAnchor.MiddleCenter;

            var hostLabel = UIComponents.CreateText(hostContainer.transform, "Your Share", 11);
            hostLabel.alignment = TextAlignmentOptions.Center;
            hostLabel.color = _theme?.TextSecondary ?? Color.gray;

            _hostSharePreviewText = UIComponents.CreateText(hostContainer.transform, "20%", 20);
            _hostSharePreviewText.alignment = TextAlignmentOptions.Center;
            _hostSharePreviewText.fontStyle = FontStyles.Bold;
            _hostSharePreviewText.color = _theme?.SuccessColor ?? Color.green;

            // Estimated earnings
            var estContainer = new GameObject("EstEarnings");
            estContainer.transform.SetParent(row.transform, false);

            var estLayout = estContainer.AddComponent<VerticalLayoutGroup>();
            estLayout.childAlignment = TextAnchor.MiddleCenter;

            var estLabel = UIComponents.CreateText(estContainer.transform, "Est. per 10 rounds", 11);
            estLabel.alignment = TextAlignmentOptions.Center;
            estLabel.color = _theme?.TextSecondary ?? Color.gray;

            _estimatedEarningsText = UIComponents.CreateText(estContainer.transform, "$5.00", 20);
            _estimatedEarningsText.alignment = TextAlignmentOptions.Center;
            _estimatedEarningsText.fontStyle = FontStyles.Bold;
            _estimatedEarningsText.color = _theme?.SuccessColor ?? Color.green;
        }

        private void CreateButtons(Transform parent)
        {
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(parent, false);

            var buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 16;
            buttonLayout.childForceExpandWidth = true;

            // Cancel button
            var cancelGO = new GameObject("CancelButton");
            cancelGO.transform.SetParent(buttonRow.transform, false);

            var cancelLayout = cancelGO.AddComponent<LayoutElement>();
            cancelLayout.preferredHeight = 44;

            var cancelBg = cancelGO.AddComponent<Image>();
            cancelBg.color = new Color(0.3f, 0.3f, 0.35f);

            _cancelButton = cancelGO.AddComponent<Button>();
            _cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());

            var cancelText = UIComponents.CreateText(cancelGO.transform, "Cancel", 16);
            cancelText.alignment = TextAlignmentOptions.Center;

            // Confirm button
            var confirmGO = new GameObject("ConfirmButton");
            confirmGO.transform.SetParent(buttonRow.transform, false);

            var confirmLayout = confirmGO.AddComponent<LayoutElement>();
            confirmLayout.preferredHeight = 44;

            var confirmBg = confirmGO.AddComponent<Image>();
            confirmBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f);

            _confirmButton = confirmGO.AddComponent<Button>();
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            var confirmText = UIComponents.CreateText(confirmGO.transform, "Create Room", 16);
            confirmText.alignment = TextAlignmentOptions.Center;
            confirmText.fontStyle = FontStyles.Bold;
        }

        private GameObject CreateSection(Transform parent, string title)
        {
            var sectionGO = new GameObject($"Section_{title}");
            sectionGO.transform.SetParent(parent, false);

            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 8;
            sectionLayout.childForceExpandWidth = true;

            var titleText = UIComponents.CreateText(sectionGO.transform, title, 12);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = _theme?.TextSecondary ?? Color.gray;

            return sectionGO;
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnPointValueChanged(float value)
        {
            _settings.PointValue = (decimal)value;
            _pointValueInput.text = $"${value:F2}";
            UpdateMinBuyIn();
            UpdateRevenuePreview();
        }

        private void OnPointValueInputChanged(string value)
        {
            if (decimal.TryParse(value.Replace("$", ""), out var pointValue))
            {
                pointValue = Mathf.Clamp((float)pointValue, 0.1f, 100f);
                _settings.PointValue = pointValue;
                _pointValueSlider.value = (float)pointValue;
                UpdateMinBuyIn();
                UpdateRevenuePreview();
            }
        }

        private void OnRakeChanged(float value)
        {
            _settings.RakePercent = value;
            _rakePercentText.text = $"{(int)value}%";
            UpdateRevenuePreview();
        }

        private void OnRakeCapChanged(string value)
        {
            if (decimal.TryParse(value.Replace("$", ""), out var cap))
            {
                _settings.RakeCap = cap;
            }
        }

        private void OnTurnTimerToggleChanged(bool enabled)
        {
            _settings.TurnTimerEnabled = enabled;
            _turnTimerSlider.interactable = enabled;
            _turnTimerText.color = enabled ? Color.white : Color.gray;
        }

        private void OnTurnTimerChanged(float value)
        {
            _settings.TurnTimerSeconds = (int)value;
            _turnTimerText.text = $"{(int)value}s";
        }

        private void OnPauseToggleChanged(bool enabled)
        {
            _settings.PauseEnabled = enabled;
            _maxPausesSlider.interactable = enabled;
            _pauseDurationSlider.interactable = enabled;
        }

        private void OnConfirmClicked()
        {
            // Gather all settings
            _settings.MaxPausesPerPlayer = (int)_maxPausesSlider.value;
            _settings.MaxPauseDurationMinutes = (int)_pauseDurationSlider.value;

            OnSettingsConfirmed?.Invoke(_settings);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void UpdateUI()
        {
            _pointValueSlider.value = (float)_settings.PointValue;
            _pointValueInput.text = $"${_settings.PointValue:F2}";
            _rakePercentSlider.value = _settings.RakePercent;
            _rakePercentText.text = $"{(int)_settings.RakePercent}%";
            _rakeCapInput.text = $"${_settings.RakeCap:F2}";
            _turnTimerToggle.isOn = _settings.TurnTimerEnabled;
            _turnTimerSlider.value = _settings.TurnTimerSeconds;
            _turnTimerText.text = $"{_settings.TurnTimerSeconds}s";
            _pauseEnabledToggle.isOn = _settings.PauseEnabled;
            _maxPausesSlider.value = _settings.MaxPausesPerPlayer;
            _maxPausesText.text = $"{_settings.MaxPausesPerPlayer}";
            _pauseDurationSlider.value = _settings.MaxPauseDurationMinutes;
            _pauseDurationText.text = $"{_settings.MaxPauseDurationMinutes}m";

            UpdateMinBuyIn();
            UpdateRevenuePreview();
        }

        private void UpdateMinBuyIn()
        {
            var minBuyIn = _settings.PointValue * 50;
            _minBuyInText.text = $"Min Buy-In: ${minBuyIn:F2}";
        }

        private void UpdateRevenuePreview()
        {
            var hostTier = HostManager.CurrentProfile?.SocialTier ?? HostTier.Bronze;
            var hostShare = HostManager.GetHostSharePercent(hostTier, RoomRevenueType.Social, true);
            _hostSharePreviewText.text = $"{hostShare:F0}%";

            // Estimate for 10 rounds
            var estimate = RakeCalculator.EstimateSessionEarnings(new SessionEstimateParams
            {
                PointValue = _settings.PointValue,
                PlayerCount = 4,
                ExpectedRounds = 10,
                RakePercent = _settings.RakePercent,
                RakeCap = _settings.RakeCap,
                HostTier = hostTier
            });

            _estimatedEarningsText.text = $"${estimate.HostEarnings:F2}";
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_hostSharePreviewText != null) _hostSharePreviewText.color = theme.SuccessColor;
            if (_estimatedEarningsText != null) _estimatedEarningsText.color = theme.SuccessColor;
            if (_minBuyInText != null) _minBuyInText.color = theme.TextSecondary;
        }
    }

    /// <summary>
    /// Social game settings data structure.
    /// </summary>
    [Serializable]
    public class SocialGameSettings
    {
        public decimal PointValue = 1.00m;
        public float RakePercent = 5f;
        public decimal RakeCap = 50m;
        public bool TurnTimerEnabled = true;
        public int TurnTimerSeconds = 30;
        public bool PauseEnabled = true;
        public int MaxPausesPerPlayer = 3;
        public int MaxPauseDurationMinutes = 5;
        public int PauseCooldownMinutes = 15;
    }
}
// =============================================================================
// Deskillz SDK for Unity - Rebuy Modal UI
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
    /// Modal dialog for rebuy after busting in social games.
    /// Shows current session stats and rebuy options.
    /// </summary>
    public class RebuyModalUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when rebuy is confirmed</summary>
        public event Action<decimal, string> OnRebuyConfirmed;

        /// <summary>Called when player chooses to leave</summary>
        public event Action OnLeaveClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _overlay;
        private RectTransform _modalContainer;
        private Image _modalBackground;

        // Header
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;

        // Session summary
        private TextMeshProUGUI _totalBuyInText;
        private TextMeshProUGUI _roundsPlayedText;
        private TextMeshProUGUI _netProfitLossText;

        // Rebuy options
        private TextMeshProUGUI _minRebuyText;
        private TMP_InputField _amountInput;
        private Button[] _presetButtons;
        private TMP_Dropdown _currencyDropdown;

        // Wallet balance
        private TextMeshProUGUI _walletBalanceText;

        // Buttons
        private Button _rebuyButton;
        private TextMeshProUGUI _rebuyButtonText;
        private Button _leaveButton;

        // Timer
        private TextMeshProUGUI _timerText;
        private float _timeRemaining;
        private bool _timerActive;

        // Loading
        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private decimal _selectedAmount;
        private string _selectedCurrency = "USDT";
        private decimal _minRebuy;
        private decimal _walletBalance;
        private BuyInOptions _options;
        private bool _isProcessing;

        private const float DEFAULT_DECISION_TIME = 60f; // 60 seconds to decide

        private readonly string[] _currencies = { "USDT", "USDC", "BNB" };

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Overlay (non-dismissable)
            _overlay = gameObject.AddComponent<Image>();
            _overlay.color = new Color(0, 0, 0, 0.9f);

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;

            // Modal container
            var modalGO = new GameObject("Modal");
            modalGO.transform.SetParent(transform, false);

            _modalContainer = modalGO.AddComponent<RectTransform>();
            _modalContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _modalContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _modalContainer.sizeDelta = new Vector2(340, 440);

            _modalBackground = modalGO.AddComponent<Image>();
            _modalBackground.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            var modalLayout = modalGO.AddComponent<VerticalLayoutGroup>();
            modalLayout.padding = new RectOffset(20, 20, 20, 20);
            modalLayout.spacing = 16;
            modalLayout.childForceExpandWidth = true;
            modalLayout.childForceExpandHeight = false;

            CreateHeader(modalGO.transform);
            CreateSessionSummary(modalGO.transform);
            CreateRebuySection(modalGO.transform);
            CreateButtons(modalGO.transform);
            CreateLoadingIndicator(modalGO.transform);
        }

        private void CreateHeader(Transform parent)
        {
            var headerContainer = new GameObject("Header");
            headerContainer.transform.SetParent(parent, false);

            var headerLayout = headerContainer.AddComponent<VerticalLayoutGroup>();
            headerLayout.spacing = 4;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;

            // Warning icon placeholder
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(headerContainer.transform, false);

            var iconLayout = iconGO.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 48;
            iconLayout.preferredHeight = 48;

            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = _theme?.WarningColor ?? new Color(1f, 0.6f, 0f);

            _titleText = UIComponents.CreateText(headerContainer.transform, "You're Out!", 24);
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = _theme?.WarningColor ?? new Color(1f, 0.6f, 0f);

            _subtitleText = UIComponents.CreateText(headerContainer.transform, "Your balance has reached zero", 14);
            _subtitleText.alignment = TextAlignmentOptions.Center;
            _subtitleText.color = _theme?.TextSecondary ?? Color.gray;

            // Timer
            _timerText = UIComponents.CreateText(headerContainer.transform, "1:00", 16);
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.color = _theme?.ErrorColor ?? Color.red;
        }

        private void CreateSessionSummary(Transform parent)
        {
            var summaryContainer = new GameObject("Summary");
            summaryContainer.transform.SetParent(parent, false);

            var bg = summaryContainer.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);

            var summaryLayout = summaryContainer.AddComponent<HorizontalLayoutGroup>();
            summaryLayout.padding = new RectOffset(12, 12, 10, 10);
            summaryLayout.spacing = 12;
            summaryLayout.childForceExpandWidth = true;

            // Total buy-in
            CreateStatItem(summaryContainer.transform, "Total Buy-In", out _, out _totalBuyInText);

            // Rounds played
            CreateStatItem(summaryContainer.transform, "Rounds", out _, out _roundsPlayedText);

            // Net P/L
            CreateStatItem(summaryContainer.transform, "Net P/L", out _, out _netProfitLossText);
        }

        private void CreateStatItem(Transform parent, string label, out TextMeshProUGUI labelText, out TextMeshProUGUI valueText)
        {
            var container = new GameObject($"Stat_{label}");
            container.transform.SetParent(parent, false);

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;

            labelText = UIComponents.CreateText(container.transform, label, 10);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = _theme?.TextSecondary ?? Color.gray;

            valueText = UIComponents.CreateText(container.transform, "$0", 14);
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.fontStyle = FontStyles.Bold;
        }

        private void CreateRebuySection(Transform parent)
        {
            var rebuyContainer = new GameObject("RebuySection");
            rebuyContainer.transform.SetParent(parent, false);

            var sectionLayout = rebuyContainer.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 10;

            // Wallet balance
            var balanceRow = new GameObject("BalanceRow");
            balanceRow.transform.SetParent(rebuyContainer.transform, false);

            var balanceLayout = balanceRow.AddComponent<HorizontalLayoutGroup>();
            balanceLayout.childForceExpandWidth = false;

            var balanceLabel = UIComponents.CreateText(balanceRow.transform, "Wallet:", 14);
            balanceLabel.color = _theme?.TextSecondary ?? Color.gray;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(balanceRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            _walletBalanceText = UIComponents.CreateText(balanceRow.transform, "$0.00", 16);
            _walletBalanceText.fontStyle = FontStyles.Bold;
            _walletBalanceText.color = _theme?.SuccessColor ?? Color.green;

            // Min rebuy info
            _minRebuyText = UIComponents.CreateText(rebuyContainer.transform, "Minimum rebuy: $50.00", 12);
            _minRebuyText.color = _theme?.TextSecondary ?? Color.gray;

            // Amount input
            var inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(rebuyContainer.transform, false);

            var inputLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputLayout.spacing = 8;

            var inputGO = new GameObject("AmountInput");
            inputGO.transform.SetParent(inputRow.transform, false);

            var inputElement = inputGO.AddComponent<LayoutElement>();
            inputElement.flexibleWidth = 1;
            inputElement.preferredHeight = 40;

            _amountInput = UIComponents.CreateInputField(inputGO.transform, "$100.00");
            _amountInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            _amountInput.onValueChanged.AddListener(OnAmountChanged);

            var dropdownGO = new GameObject("CurrencyDropdown");
            dropdownGO.transform.SetParent(inputRow.transform, false);

            var dropdownLayout = dropdownGO.AddComponent<LayoutElement>();
            dropdownLayout.preferredWidth = 100;
            dropdownLayout.preferredHeight = 40;

            _currencyDropdown = UIComponents.CreateDropdown(dropdownGO.transform, _currencies);
            _currencyDropdown.onValueChanged.AddListener(i => _selectedCurrency = _currencies[i]);

            // Preset buttons
            var presetsRow = new GameObject("PresetsRow");
            presetsRow.transform.SetParent(rebuyContainer.transform, false);

            var presetsLayout = presetsRow.AddComponent<HorizontalLayoutGroup>();
            presetsLayout.spacing = 6;
            presetsLayout.childForceExpandWidth = true;

            _presetButtons = new Button[3];
            string[] presetLabels = { "Min", "100x", "200x" };

            for (int i = 0; i < 3; i++)
            {
                var presetGO = new GameObject($"Preset{i}");
                presetGO.transform.SetParent(presetsRow.transform, false);

                var presetElement = presetGO.AddComponent<LayoutElement>();
                presetElement.preferredHeight = 32;

                var presetBg = presetGO.AddComponent<Image>();
                presetBg.color = new Color(0.2f, 0.2f, 0.25f);

                int index = i;
                _presetButtons[i] = presetGO.AddComponent<Button>();
                _presetButtons[i].onClick.AddListener(() => OnPresetClicked(index));

                var presetText = UIComponents.CreateText(presetGO.transform, presetLabels[i], 12);
                presetText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateButtons(Transform parent)
        {
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(parent, false);

            var buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12;
            buttonLayout.childForceExpandWidth = true;

            // Leave button
            var leaveGO = new GameObject("LeaveButton");
            leaveGO.transform.SetParent(buttonRow.transform, false);

            var leaveLayout = leaveGO.AddComponent<LayoutElement>();
            leaveLayout.preferredHeight = 48;

            var leaveBg = leaveGO.AddComponent<Image>();
            leaveBg.color = new Color(0.4f, 0.2f, 0.2f);

            _leaveButton = leaveGO.AddComponent<Button>();
            _leaveButton.onClick.AddListener(() => OnLeaveClicked?.Invoke());

            var leaveText = UIComponents.CreateText(leaveGO.transform, "Leave Game", 14);
            leaveText.alignment = TextAlignmentOptions.Center;

            var leaveTextRect = leaveText.GetComponent<RectTransform>();
            leaveTextRect.anchorMin = Vector2.zero;
            leaveTextRect.anchorMax = Vector2.one;

            // Rebuy button
            var rebuyGO = new GameObject("RebuyButton");
            rebuyGO.transform.SetParent(buttonRow.transform, false);

            var rebuyLayout = rebuyGO.AddComponent<LayoutElement>();
            rebuyLayout.preferredHeight = 48;

            var rebuyBg = rebuyGO.AddComponent<Image>();
            rebuyBg.color = _theme?.SuccessColor ?? new Color(0.2f, 0.7f, 0.3f);

            _rebuyButton = rebuyGO.AddComponent<Button>();
            _rebuyButton.onClick.AddListener(OnRebuyClicked);

            _rebuyButtonText = UIComponents.CreateText(rebuyGO.transform, "Rebuy", 16);
            _rebuyButtonText.alignment = TextAlignmentOptions.Center;
            _rebuyButtonText.fontStyle = FontStyles.Bold;

            var rebuyTextRect = _rebuyButtonText.GetComponent<RectTransform>();
            rebuyTextRect.anchorMin = Vector2.zero;
            rebuyTextRect.anchorMax = Vector2.one;
        }

        private void CreateLoadingIndicator(Transform parent)
        {
            _loadingIndicator = UIComponents.CreateLoadingIndicator(parent);
            _loadingIndicator.SetActive(false);
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Show the rebuy modal with session info.
        /// </summary>
        public void Show(decimal pointValue, decimal walletBalance, decimal totalBuyIn, int roundsPlayed, decimal netPL)
        {
            _options = RakeCalculator.CalculateBuyInOptions(pointValue);
            _minRebuy = _options.MinBuyIn;
            _walletBalance = walletBalance;
            _selectedAmount = _options.DefaultBuyIn;

            // Update session summary
            _totalBuyInText.text = $"${totalBuyIn:F2}";
            _roundsPlayedText.text = roundsPlayed.ToString();
            _netProfitLossText.text = netPL >= 0 ? $"+${netPL:F2}" : $"-${Math.Abs(netPL):F2}";
            _netProfitLossText.color = netPL >= 0 
                ? (_theme?.SuccessColor ?? Color.green) 
                : (_theme?.ErrorColor ?? Color.red);

            // Update rebuy info
            _walletBalanceText.text = $"${walletBalance:F2}";
            _minRebuyText.text = $"Minimum rebuy: ${_minRebuy:F2}";
            _amountInput.text = $"${_selectedAmount:F2}";

            // Update button state
            _rebuyButton.interactable = walletBalance >= _minRebuy;

            // Start timer
            StartTimer(DEFAULT_DECISION_TIME);

            base.Show();
        }

        // =====================================================================
        // TIMER
        // =====================================================================

        private void StartTimer(float seconds)
        {
            _timeRemaining = seconds;
            _timerActive = true;
            UpdateTimerDisplay();
        }

        private void Update()
        {
            if (_timerActive && _timeRemaining > 0)
            {
                _timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();

                if (_timeRemaining <= 0)
                {
                    _timerActive = false;
                    OnTimerExpired();
                }
            }
        }

        private void UpdateTimerDisplay()
        {
            int seconds = Mathf.CeilToInt(_timeRemaining);
            int mins = seconds / 60;
            int secs = seconds % 60;
            _timerText.text = $"{mins}:{secs:D2}";

            // Change color when low
            if (_timeRemaining < 10)
            {
                _timerText.color = _theme?.ErrorColor ?? Color.red;
            }
        }

        private void OnTimerExpired()
        {
            // Auto-leave when timer expires
            OnLeaveClicked?.Invoke();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnAmountChanged(string value)
        {
            if (decimal.TryParse(value.Replace("$", ""), out var amount))
            {
                _selectedAmount = amount;
                UpdateButtonState();
            }
        }

        private void OnPresetClicked(int index)
        {
            if (_options != null)
            {
                decimal[] presets = { _options.MinBuyIn, _options.DefaultBuyIn, _options.DefaultBuyIn * 2 };
                if (index < presets.Length)
                {
                    _selectedAmount = presets[index];
                    _amountInput.text = $"${_selectedAmount:F2}";
                    UpdateButtonState();
                }
            }
        }

        private void OnRebuyClicked()
        {
            if (_isProcessing) return;

            if (_selectedAmount < _minRebuy)
            {
                // Show error
                return;
            }

            if (_selectedAmount > _walletBalance)
            {
                // Show error
                return;
            }

            SetProcessing(true);
            OnRebuyConfirmed?.Invoke(_selectedAmount, _selectedCurrency);
        }

        private void UpdateButtonState()
        {
            _rebuyButton.interactable = 
                _selectedAmount >= _minRebuy && 
                _selectedAmount <= _walletBalance && 
                !_isProcessing;
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;
            _loadingIndicator.SetActive(processing);
            _rebuyButton.interactable = !processing;
            _leaveButton.interactable = !processing;
            _amountInput.interactable = !processing;

            if (processing)
            {
                _timerActive = false;
            }
        }

        /// <summary>
        /// Called when rebuy completes.
        /// </summary>
        public void OnRebuyComplete(bool success, string message = null)
        {
            SetProcessing(false);

            if (success)
            {
                Hide();
            }
            else
            {
                // Resume timer
                _timerActive = true;
            }
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_modalBackground != null) _modalBackground.color = theme.CardBackground;
            if (_titleText != null) _titleText.color = theme.WarningColor;
            if (_subtitleText != null) _subtitleText.color = theme.TextSecondary;
            if (_walletBalanceText != null) _walletBalanceText.color = theme.SuccessColor;
            if (_timerText != null && _timeRemaining >= 10) _timerText.color = theme.WarningColor;
        }
    }
}
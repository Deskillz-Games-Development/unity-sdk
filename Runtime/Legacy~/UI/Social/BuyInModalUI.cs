// =============================================================================
// Deskillz SDK for Unity - Buy-In Modal UI
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
    /// Modal dialog for buy-in to social game rooms.
    /// Shows balance, buy-in options, and currency selection.
    /// </summary>
    public class BuyInModalUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when buy-in is confirmed</summary>
        public event Action<decimal, string> OnBuyInConfirmed;

        /// <summary>Called when modal is cancelled</summary>
        public event Action OnCancelled;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _overlay;
        private RectTransform _modalContainer;
        private Image _modalBackground;
        private TextMeshProUGUI _titleText;
        private Button _closeButton;

        // Balance section
        private TextMeshProUGUI _walletBalanceLabel;
        private TextMeshProUGUI _walletBalanceText;

        // Room info
        private TextMeshProUGUI _pointValueText;
        private TextMeshProUGUI _minBuyInText;

        // Amount selection
        private TMP_InputField _amountInput;
        private Button[] _presetButtons;
        private TextMeshProUGUI[] _presetTexts;

        // Currency selection
        private TMP_Dropdown _currencyDropdown;

        // Summary
        private TextMeshProUGUI _summaryAmountText;
        private TextMeshProUGUI _warningText;

        // Buttons
        private Button _confirmButton;
        private TextMeshProUGUI _confirmButtonText;
        private Button _cancelButton;

        // Loading
        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private decimal _selectedAmount;
        private string _selectedCurrency = "USDT";
        private decimal _minBuyIn;
        private decimal _walletBalance;
        private BuyInOptions _options;
        private bool _isProcessing;

        private readonly string[] _currencies = { "USDT", "USDC", "BNB", "ETH", "BTC" };

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Overlay
            _overlay = gameObject.AddComponent<Image>();
            _overlay.color = new Color(0, 0, 0, 0.8f);

            var overlayButton = gameObject.AddComponent<Button>();
            overlayButton.onClick.AddListener(() => OnCancelled?.Invoke());

            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;

            // Modal container
            var modalGO = new GameObject("Modal");
            modalGO.transform.SetParent(transform, false);

            _modalContainer = modalGO.AddComponent<RectTransform>();
            _modalContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _modalContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _modalContainer.sizeDelta = new Vector2(340, 480);

            _modalBackground = modalGO.AddComponent<Image>();
            _modalBackground.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            // Prevent clicks from closing
            var modalButton = modalGO.AddComponent<Button>();
            modalButton.transition = Selectable.Transition.None;

            var modalLayout = modalGO.AddComponent<VerticalLayoutGroup>();
            modalLayout.padding = new RectOffset(20, 20, 20, 20);
            modalLayout.spacing = 16;
            modalLayout.childForceExpandWidth = true;
            modalLayout.childForceExpandHeight = false;

            // Create sections
            CreateHeader(modalGO.transform);
            CreateBalanceSection(modalGO.transform);
            CreateRoomInfoSection(modalGO.transform);
            CreateAmountSection(modalGO.transform);
            CreateCurrencySection(modalGO.transform);
            CreateSummarySection(modalGO.transform);
            CreateButtons(modalGO.transform);
            CreateLoadingIndicator(modalGO.transform);
        }

        private void CreateHeader(Transform parent)
        {
            var headerRow = new GameObject("Header");
            headerRow.transform.SetParent(parent, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;

            _titleText = UIComponents.CreateText(headerRow.transform, "Buy In", 20);
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

            var closeText = UIComponents.CreateText(closeGO.transform, "X", 20);
            closeText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateBalanceSection(Transform parent)
        {
            var balanceRow = new GameObject("BalanceRow");
            balanceRow.transform.SetParent(parent, false);

            var bg = balanceRow.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);

            var balanceLayout = balanceRow.AddComponent<HorizontalLayoutGroup>();
            balanceLayout.padding = new RectOffset(12, 12, 8, 8);
            balanceLayout.childForceExpandWidth = false;

            _walletBalanceLabel = UIComponents.CreateText(balanceRow.transform, "Wallet Balance:", 14);
            _walletBalanceLabel.color = _theme?.TextSecondary ?? Color.gray;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(balanceRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            _walletBalanceText = UIComponents.CreateText(balanceRow.transform, "$0.00", 16);
            _walletBalanceText.fontStyle = FontStyles.Bold;
            _walletBalanceText.color = _theme?.SuccessColor ?? Color.green;
        }

        private void CreateRoomInfoSection(Transform parent)
        {
            var infoRow = new GameObject("InfoRow");
            infoRow.transform.SetParent(parent, false);

            var infoLayout = infoRow.AddComponent<HorizontalLayoutGroup>();
            infoLayout.spacing = 20;
            infoLayout.childForceExpandWidth = true;

            // Point value
            var pointContainer = new GameObject("PointValue");
            pointContainer.transform.SetParent(infoRow.transform, false);

            var pointLayout = pointContainer.AddComponent<VerticalLayoutGroup>();
            pointLayout.childAlignment = TextAnchor.MiddleCenter;

            var pointLabel = UIComponents.CreateText(pointContainer.transform, "Point Value", 11);
            pointLabel.alignment = TextAlignmentOptions.Center;
            pointLabel.color = _theme?.TextSecondary ?? Color.gray;

            _pointValueText = UIComponents.CreateText(pointContainer.transform, "$1.00", 16);
            _pointValueText.alignment = TextAlignmentOptions.Center;
            _pointValueText.fontStyle = FontStyles.Bold;

            // Min buy-in
            var minContainer = new GameObject("MinBuyIn");
            minContainer.transform.SetParent(infoRow.transform, false);

            var minLayout = minContainer.AddComponent<VerticalLayoutGroup>();
            minLayout.childAlignment = TextAnchor.MiddleCenter;

            var minLabel = UIComponents.CreateText(minContainer.transform, "Minimum", 11);
            minLabel.alignment = TextAlignmentOptions.Center;
            minLabel.color = _theme?.TextSecondary ?? Color.gray;

            _minBuyInText = UIComponents.CreateText(minContainer.transform, "$50.00", 16);
            _minBuyInText.alignment = TextAlignmentOptions.Center;
            _minBuyInText.fontStyle = FontStyles.Bold;
        }

        private void CreateAmountSection(Transform parent)
        {
            var amountSection = new GameObject("AmountSection");
            amountSection.transform.SetParent(parent, false);

            var sectionLayout = amountSection.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 10;

            var labelText = UIComponents.CreateText(amountSection.transform, "Buy-In Amount", 12);
            labelText.color = _theme?.TextSecondary ?? Color.gray;

            // Input field
            var inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(amountSection.transform, false);

            var inputLayout = inputRow.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = 44;

            _amountInput = UIComponents.CreateInputField(inputRow.transform, "$100.00");
            _amountInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            _amountInput.onValueChanged.AddListener(OnAmountInputChanged);

            // Preset buttons
            var presetsRow = new GameObject("PresetsRow");
            presetsRow.transform.SetParent(amountSection.transform, false);

            var presetsLayout = presetsRow.AddComponent<HorizontalLayoutGroup>();
            presetsLayout.spacing = 8;
            presetsLayout.childForceExpandWidth = true;

            _presetButtons = new Button[4];
            _presetTexts = new TextMeshProUGUI[4];

            string[] presetLabels = { "Min", "100x", "200x", "500x" };

            for (int i = 0; i < 4; i++)
            {
                var presetGO = new GameObject($"Preset{i}");
                presetGO.transform.SetParent(presetsRow.transform, false);

                var presetLayout = presetGO.AddComponent<LayoutElement>();
                presetLayout.preferredHeight = 36;

                var presetBg = presetGO.AddComponent<Image>();
                presetBg.color = new Color(0.2f, 0.2f, 0.25f);

                int index = i;
                _presetButtons[i] = presetGO.AddComponent<Button>();
                _presetButtons[i].onClick.AddListener(() => OnPresetClicked(index));

                _presetTexts[i] = UIComponents.CreateText(presetGO.transform, presetLabels[i], 12);
                _presetTexts[i].alignment = TextAlignmentOptions.Center;

                var textRect = _presetTexts[i].GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
            }
        }

        private void CreateCurrencySection(Transform parent)
        {
            var currencyRow = new GameObject("CurrencyRow");
            currencyRow.transform.SetParent(parent, false);

            var rowLayout = currencyRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childForceExpandWidth = false;

            var label = UIComponents.CreateText(currencyRow.transform, "Pay with:", 14);

            var dropdownGO = new GameObject("CurrencyDropdown");
            dropdownGO.transform.SetParent(currencyRow.transform, false);

            var dropdownLayout = dropdownGO.AddComponent<LayoutElement>();
            dropdownLayout.preferredWidth = 120;
            dropdownLayout.preferredHeight = 36;

            _currencyDropdown = UIComponents.CreateDropdown(dropdownGO.transform, _currencies);
            _currencyDropdown.onValueChanged.AddListener(OnCurrencyChanged);
        }

        private void CreateSummarySection(Transform parent)
        {
            var summaryContainer = new GameObject("Summary");
            summaryContainer.transform.SetParent(parent, false);

            var bg = summaryContainer.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.2f, 0.15f);

            var summaryLayout = summaryContainer.AddComponent<VerticalLayoutGroup>();
            summaryLayout.padding = new RectOffset(12, 12, 10, 10);
            summaryLayout.spacing = 4;
            summaryLayout.childAlignment = TextAnchor.MiddleCenter;

            var label = UIComponents.CreateText(summaryContainer.transform, "You will receive", 12);
            label.alignment = TextAlignmentOptions.Center;
            label.color = _theme?.TextSecondary ?? Color.gray;

            _summaryAmountText = UIComponents.CreateText(summaryContainer.transform, "$100.00", 24);
            _summaryAmountText.alignment = TextAlignmentOptions.Center;
            _summaryAmountText.fontStyle = FontStyles.Bold;
            _summaryAmountText.color = _theme?.SuccessColor ?? Color.green;

            var pointsLabel = UIComponents.CreateText(summaryContainer.transform, "(100 points)", 12);
            pointsLabel.alignment = TextAlignmentOptions.Center;
            pointsLabel.color = _theme?.TextSecondary ?? Color.gray;

            // Warning text (hidden by default)
            _warningText = UIComponents.CreateText(parent, "", 12);
            _warningText.alignment = TextAlignmentOptions.Center;
            _warningText.color = _theme?.ErrorColor ?? Color.red;
            _warningText.gameObject.SetActive(false);
        }

        private void CreateButtons(Transform parent)
        {
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(parent, false);

            var buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12;
            buttonLayout.childForceExpandWidth = true;

            // Cancel button
            var cancelGO = new GameObject("CancelButton");
            cancelGO.transform.SetParent(buttonRow.transform, false);

            var cancelLayout = cancelGO.AddComponent<LayoutElement>();
            cancelLayout.preferredHeight = 48;

            var cancelBg = cancelGO.AddComponent<Image>();
            cancelBg.color = new Color(0.3f, 0.3f, 0.35f);

            _cancelButton = cancelGO.AddComponent<Button>();
            _cancelButton.onClick.AddListener(() => OnCancelled?.Invoke());

            var cancelText = UIComponents.CreateText(cancelGO.transform, "Cancel", 16);
            cancelText.alignment = TextAlignmentOptions.Center;

            var cancelTextRect = cancelText.GetComponent<RectTransform>();
            cancelTextRect.anchorMin = Vector2.zero;
            cancelTextRect.anchorMax = Vector2.one;

            // Confirm button
            var confirmGO = new GameObject("ConfirmButton");
            confirmGO.transform.SetParent(buttonRow.transform, false);

            var confirmLayout = confirmGO.AddComponent<LayoutElement>();
            confirmLayout.preferredHeight = 48;

            var confirmBg = confirmGO.AddComponent<Image>();
            confirmBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f);

            _confirmButton = confirmGO.AddComponent<Button>();
            _confirmButton.onClick.AddListener(OnConfirmClicked);

            _confirmButtonText = UIComponents.CreateText(confirmGO.transform, "Buy In", 16);
            _confirmButtonText.alignment = TextAlignmentOptions.Center;
            _confirmButtonText.fontStyle = FontStyles.Bold;

            var confirmTextRect = _confirmButtonText.GetComponent<RectTransform>();
            confirmTextRect.anchorMin = Vector2.zero;
            confirmTextRect.anchorMax = Vector2.one;
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
        /// Configure and show the buy-in modal.
        /// </summary>
        public void Show(decimal pointValue, decimal walletBalance)
        {
            _options = RakeCalculator.CalculateBuyInOptions(pointValue);
            _minBuyIn = _options.MinBuyIn;
            _walletBalance = walletBalance;
            _selectedAmount = _options.DefaultBuyIn;

            // Update UI
            _pointValueText.text = $"${pointValue:F2}";
            _minBuyInText.text = $"${_minBuyIn:F2}";
            _walletBalanceText.text = $"${walletBalance:F2}";

            // Update preset buttons
            for (int i = 0; i < _options.SuggestedAmounts.Length && i < _presetTexts.Length; i++)
            {
                _presetTexts[i].text = $"${_options.SuggestedAmounts[i]:F0}";
            }

            _amountInput.text = $"${_selectedAmount:F2}";
            UpdateSummary();

            base.Show();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnAmountInputChanged(string value)
        {
            if (decimal.TryParse(value.Replace("$", ""), out var amount))
            {
                _selectedAmount = amount;
                UpdateSummary();
            }
        }

        private void OnPresetClicked(int index)
        {
            if (_options != null && index < _options.SuggestedAmounts.Length)
            {
                _selectedAmount = _options.SuggestedAmounts[index];
                _amountInput.text = $"${_selectedAmount:F2}";
                UpdateSummary();

                // Highlight selected preset
                for (int i = 0; i < _presetButtons.Length; i++)
                {
                    var bg = _presetButtons[i].GetComponent<Image>();
                    bg.color = i == index 
                        ? (_theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f))
                        : new Color(0.2f, 0.2f, 0.25f);
                }
            }
        }

        private void OnCurrencyChanged(int index)
        {
            if (index < _currencies.Length)
            {
                _selectedCurrency = _currencies[index];
            }
        }

        private void OnConfirmClicked()
        {
            if (_isProcessing) return;

            var validation = RakeCalculator.ValidateBuyIn(_selectedAmount, _options.PointValue);

            if (!validation.IsValid)
            {
                ShowWarning(validation.ErrorMessage);
                return;
            }

            if (_selectedAmount > _walletBalance)
            {
                ShowWarning("Insufficient wallet balance");
                return;
            }

            SetProcessing(true);
            OnBuyInConfirmed?.Invoke(_selectedAmount, _selectedCurrency);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void UpdateSummary()
        {
            _summaryAmountText.text = $"${_selectedAmount:F2}";

            // Calculate points
            if (_options != null && _options.PointValue > 0)
            {
                var points = (int)(_selectedAmount / _options.PointValue);
                var pointsLabel = _summaryAmountText.transform.parent.GetChild(2);
                if (pointsLabel != null)
                {
                    pointsLabel.GetComponent<TextMeshProUGUI>().text = $"({points} points)";
                }
            }

            // Validate and update button state
            var validation = RakeCalculator.ValidateBuyIn(_selectedAmount, _options?.PointValue ?? 1m);
            _confirmButton.interactable = validation.IsValid && _selectedAmount <= _walletBalance;

            if (!validation.IsValid)
            {
                ShowWarning(validation.ErrorMessage);
            }
            else if (_selectedAmount > _walletBalance)
            {
                ShowWarning("Insufficient balance");
            }
            else
            {
                HideWarning();
            }
        }

        private void ShowWarning(string message)
        {
            _warningText.text = message;
            _warningText.gameObject.SetActive(true);
        }

        private void HideWarning()
        {
            _warningText.gameObject.SetActive(false);
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;
            _loadingIndicator.SetActive(processing);
            _confirmButton.interactable = !processing;
            _cancelButton.interactable = !processing;
            _amountInput.interactable = !processing;
            _currencyDropdown.interactable = !processing;
        }

        /// <summary>
        /// Called when buy-in completes (success or failure).
        /// </summary>
        public void OnBuyInComplete(bool success, string message = null)
        {
            SetProcessing(false);

            if (success)
            {
                Hide();
            }
            else if (!string.IsNullOrEmpty(message))
            {
                ShowWarning(message);
            }
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_modalBackground != null) _modalBackground.color = theme.CardBackground;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_walletBalanceLabel != null) _walletBalanceLabel.color = theme.TextSecondary;
            if (_walletBalanceText != null) _walletBalanceText.color = theme.SuccessColor;
            if (_summaryAmountText != null) _summaryAmountText.color = theme.SuccessColor;
            if (_warningText != null) _warningText.color = theme.ErrorColor;
        }
    }
}
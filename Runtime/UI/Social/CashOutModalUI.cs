// =============================================================================
// Deskillz SDK for Unity - Cash Out Modal UI
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
    /// Modal dialog for cashing out of a social game session.
    /// Shows session summary and final balance.
    /// </summary>
    public class CashOutModalUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when cash out is confirmed</summary>
        public event Action OnCashOutConfirmed;

        /// <summary>Called when cancelled (stay in game)</summary>
        public event Action OnCancelled;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _overlay;
        private RectTransform _modalContainer;
        private Image _modalBackground;

        // Header
        private TextMeshProUGUI _titleText;
        private Button _closeButton;

        // Session summary
        private TextMeshProUGUI _totalBuyInLabel;
        private TextMeshProUGUI _totalBuyInText;
        private TextMeshProUGUI _currentBalanceLabel;
        private TextMeshProUGUI _currentBalanceText;
        private TextMeshProUGUI _roundsPlayedLabel;
        private TextMeshProUGUI _roundsPlayedText;
        private TextMeshProUGUI _roundsWonLabel;
        private TextMeshProUGUI _roundsWonText;

        // Net result
        private TextMeshProUGUI _netResultLabel;
        private TextMeshProUGUI _netResultText;
        private Image _resultBackground;

        // Info text
        private TextMeshProUGUI _infoText;

        // Buttons
        private Button _cashOutButton;
        private TextMeshProUGUI _cashOutButtonText;
        private Button _stayButton;

        // Loading
        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private decimal _currentBalance;
        private decimal _totalBuyIn;
        private decimal _netProfitLoss;
        private bool _isProcessing;

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
            _modalContainer.sizeDelta = new Vector2(340, 400);

            _modalBackground = modalGO.AddComponent<Image>();
            _modalBackground.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            // Prevent overlay click from closing
            var modalButton = modalGO.AddComponent<Button>();
            modalButton.transition = Selectable.Transition.None;

            var modalLayout = modalGO.AddComponent<VerticalLayoutGroup>();
            modalLayout.padding = new RectOffset(20, 20, 20, 20);
            modalLayout.spacing = 16;
            modalLayout.childForceExpandWidth = true;
            modalLayout.childForceExpandHeight = false;

            CreateHeader(modalGO.transform);
            CreateSessionSummary(modalGO.transform);
            CreateNetResult(modalGO.transform);
            CreateInfoSection(modalGO.transform);
            CreateButtons(modalGO.transform);
            CreateLoadingIndicator(modalGO.transform);
        }

        private void CreateHeader(Transform parent)
        {
            var headerRow = new GameObject("Header");
            headerRow.transform.SetParent(parent, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;

            _titleText = UIComponents.CreateText(headerRow.transform, "Cash Out", 20);
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

        private void CreateSessionSummary(Transform parent)
        {
            var summaryContainer = new GameObject("Summary");
            summaryContainer.transform.SetParent(parent, false);

            var bg = summaryContainer.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);

            var summaryLayout = summaryContainer.AddComponent<VerticalLayoutGroup>();
            summaryLayout.padding = new RectOffset(16, 16, 12, 12);
            summaryLayout.spacing = 8;

            // Total buy-in row
            CreateSummaryRow(summaryContainer.transform, "Total Buy-In:", out _totalBuyInLabel, out _totalBuyInText);

            // Current balance row
            CreateSummaryRow(summaryContainer.transform, "Current Balance:", out _currentBalanceLabel, out _currentBalanceText);

            // Divider
            var divider = new GameObject("Divider");
            divider.transform.SetParent(summaryContainer.transform, false);

            var dividerLayout = divider.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1;

            var dividerImage = divider.AddComponent<Image>();
            dividerImage.color = new Color(0.3f, 0.3f, 0.35f);

            // Stats row
            var statsRow = new GameObject("StatsRow");
            statsRow.transform.SetParent(summaryContainer.transform, false);

            var statsLayout = statsRow.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 16;
            statsLayout.childForceExpandWidth = true;

            // Rounds played
            var roundsContainer = new GameObject("RoundsPlayed");
            roundsContainer.transform.SetParent(statsRow.transform, false);

            var roundsLayout = roundsContainer.AddComponent<VerticalLayoutGroup>();
            roundsLayout.childAlignment = TextAnchor.MiddleCenter;

            _roundsPlayedLabel = UIComponents.CreateText(roundsContainer.transform, "Rounds", 10);
            _roundsPlayedLabel.alignment = TextAlignmentOptions.Center;
            _roundsPlayedLabel.color = _theme?.TextSecondary ?? Color.gray;

            _roundsPlayedText = UIComponents.CreateText(roundsContainer.transform, "0", 16);
            _roundsPlayedText.alignment = TextAlignmentOptions.Center;
            _roundsPlayedText.fontStyle = FontStyles.Bold;

            // Rounds won
            var wonContainer = new GameObject("RoundsWon");
            wonContainer.transform.SetParent(statsRow.transform, false);

            var wonLayout = wonContainer.AddComponent<VerticalLayoutGroup>();
            wonLayout.childAlignment = TextAnchor.MiddleCenter;

            _roundsWonLabel = UIComponents.CreateText(wonContainer.transform, "Won", 10);
            _roundsWonLabel.alignment = TextAlignmentOptions.Center;
            _roundsWonLabel.color = _theme?.TextSecondary ?? Color.gray;

            _roundsWonText = UIComponents.CreateText(wonContainer.transform, "0", 16);
            _roundsWonText.alignment = TextAlignmentOptions.Center;
            _roundsWonText.fontStyle = FontStyles.Bold;
            _roundsWonText.color = _theme?.SuccessColor ?? Color.green;
        }

        private void CreateSummaryRow(Transform parent, string label, out TextMeshProUGUI labelText, out TextMeshProUGUI valueText)
        {
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childForceExpandWidth = false;

            labelText = UIComponents.CreateText(row.transform, label, 14);
            labelText.color = _theme?.TextSecondary ?? Color.gray;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            valueText = UIComponents.CreateText(row.transform, "$0.00", 14);
            valueText.fontStyle = FontStyles.Bold;
        }

        private void CreateNetResult(Transform parent)
        {
            var resultContainer = new GameObject("NetResult");
            resultContainer.transform.SetParent(parent, false);

            _resultBackground = resultContainer.AddComponent<Image>();

            var resultLayout = resultContainer.AddComponent<VerticalLayoutGroup>();
            resultLayout.padding = new RectOffset(16, 16, 12, 12);
            resultLayout.childAlignment = TextAnchor.MiddleCenter;

            _netResultLabel = UIComponents.CreateText(resultContainer.transform, "Net Profit/Loss", 12);
            _netResultLabel.alignment = TextAlignmentOptions.Center;

            _netResultText = UIComponents.CreateText(resultContainer.transform, "$0.00", 32);
            _netResultText.alignment = TextAlignmentOptions.Center;
            _netResultText.fontStyle = FontStyles.Bold;
        }

        private void CreateInfoSection(Transform parent)
        {
            _infoText = UIComponents.CreateText(parent, 
                "Your balance will be returned to your wallet immediately.", 12);
            _infoText.alignment = TextAlignmentOptions.Center;
            _infoText.color = _theme?.TextSecondary ?? Color.gray;
        }

        private void CreateButtons(Transform parent)
        {
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(parent, false);

            var buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 12;
            buttonLayout.childForceExpandWidth = true;

            // Stay button
            var stayGO = new GameObject("StayButton");
            stayGO.transform.SetParent(buttonRow.transform, false);

            var stayLayout = stayGO.AddComponent<LayoutElement>();
            stayLayout.preferredHeight = 48;

            var stayBg = stayGO.AddComponent<Image>();
            stayBg.color = new Color(0.3f, 0.3f, 0.35f);

            _stayButton = stayGO.AddComponent<Button>();
            _stayButton.onClick.AddListener(() => OnCancelled?.Invoke());

            var stayText = UIComponents.CreateText(stayGO.transform, "Stay", 16);
            stayText.alignment = TextAlignmentOptions.Center;

            var stayTextRect = stayText.GetComponent<RectTransform>();
            stayTextRect.anchorMin = Vector2.zero;
            stayTextRect.anchorMax = Vector2.one;

            // Cash out button
            var cashOutGO = new GameObject("CashOutButton");
            cashOutGO.transform.SetParent(buttonRow.transform, false);

            var cashOutLayout = cashOutGO.AddComponent<LayoutElement>();
            cashOutLayout.preferredHeight = 48;

            var cashOutBg = cashOutGO.AddComponent<Image>();
            cashOutBg.color = _theme?.SuccessColor ?? new Color(0.2f, 0.7f, 0.3f);

            _cashOutButton = cashOutGO.AddComponent<Button>();
            _cashOutButton.onClick.AddListener(OnCashOutClicked);

            _cashOutButtonText = UIComponents.CreateText(cashOutGO.transform, "Cash Out", 16);
            _cashOutButtonText.alignment = TextAlignmentOptions.Center;
            _cashOutButtonText.fontStyle = FontStyles.Bold;

            var cashOutTextRect = _cashOutButtonText.GetComponent<RectTransform>();
            cashOutTextRect.anchorMin = Vector2.zero;
            cashOutTextRect.anchorMax = Vector2.one;
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
        /// Show the cash out modal with session summary.
        /// </summary>
        public void Show(decimal currentBalance, decimal totalBuyIn, int roundsPlayed, int roundsWon)
        {
            _currentBalance = currentBalance;
            _totalBuyIn = totalBuyIn;
            _netProfitLoss = currentBalance - totalBuyIn;

            // Update UI
            _totalBuyInText.text = $"${totalBuyIn:F2}";
            _currentBalanceText.text = $"${currentBalance:F2}";
            _currentBalanceText.color = _theme?.SuccessColor ?? Color.green;

            _roundsPlayedText.text = roundsPlayed.ToString();
            _roundsWonText.text = roundsWon.ToString();

            // Net result
            if (_netProfitLoss >= 0)
            {
                _netResultText.text = $"+${_netProfitLoss:F2}";
                _netResultText.color = _theme?.SuccessColor ?? Color.green;
                _resultBackground.color = new Color(0.1f, 0.25f, 0.15f);
            }
            else
            {
                _netResultText.text = $"-${Math.Abs(_netProfitLoss):F2}";
                _netResultText.color = _theme?.ErrorColor ?? Color.red;
                _resultBackground.color = new Color(0.25f, 0.1f, 0.1f);
            }

            // Disable cash out if no balance
            _cashOutButton.interactable = currentBalance > 0;

            base.Show();
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnCashOutClicked()
        {
            if (_isProcessing) return;

            SetProcessing(true);
            OnCashOutConfirmed?.Invoke();
        }

        private void SetProcessing(bool processing)
        {
            _isProcessing = processing;
            _loadingIndicator.SetActive(processing);
            _cashOutButton.interactable = !processing && _currentBalance > 0;
            _stayButton.interactable = !processing;
            _closeButton.interactable = !processing;
        }

        /// <summary>
        /// Called when cash out completes.
        /// </summary>
        public void OnCashOutComplete(bool success, decimal amount = 0)
        {
            SetProcessing(false);

            if (success)
            {
                // Could show success animation
                Hide();
            }
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_modalBackground != null) _modalBackground.color = theme.CardBackground;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_totalBuyInLabel != null) _totalBuyInLabel.color = theme.TextSecondary;
            if (_currentBalanceLabel != null) _currentBalanceLabel.color = theme.TextSecondary;
            if (_currentBalanceText != null) _currentBalanceText.color = theme.SuccessColor;
            if (_roundsPlayedLabel != null) _roundsPlayedLabel.color = theme.TextSecondary;
            if (_roundsWonLabel != null) _roundsWonLabel.color = theme.TextSecondary;
            if (_roundsWonText != null) _roundsWonText.color = theme.SuccessColor;
            if (_infoText != null) _infoText.color = theme.TextSecondary;
        }
    }
}
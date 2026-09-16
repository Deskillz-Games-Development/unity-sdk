// =============================================================================
// Deskillz SDK for Unity - Host Earnings UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Host;

namespace Deskillz.UI.Host
{
    /// <summary>
    /// UI component displaying host earnings summary.
    /// Shows available balance, pending balance, and recent transactions.
    /// </summary>
    public class HostEarningsUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when withdraw button is clicked</summary>
        public event Action OnWithdrawClicked;

        /// <summary>Called when view history is clicked</summary>
        public event Action OnViewHistoryClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private TextMeshProUGUI _titleText;

        // Main balance
        private TextMeshProUGUI _availableLabel;
        private TextMeshProUGUI _availableAmountText;
        private TextMeshProUGUI _pendingLabel;
        private TextMeshProUGUI _pendingAmountText;
        private Button _withdrawButton;
        private TextMeshProUGUI _withdrawButtonText;

        // Stats row
        private TextMeshProUGUI _todayLabel;
        private TextMeshProUGUI _todayAmountText;
        private TextMeshProUGUI _weekLabel;
        private TextMeshProUGUI _weekAmountText;
        private TextMeshProUGUI _monthLabel;
        private TextMeshProUGUI _monthAmountText;

        // Breakdown
        private TextMeshProUGUI _esportsEarningsText;
        private TextMeshProUGUI _socialEarningsText;
        private TextMeshProUGUI _bonusEarningsText;

        // Recent transactions
        private RectTransform _transactionsContainer;
        private TextMeshProUGUI _noTransactionsText;
        private Button _viewHistoryButton;
        private List<TransactionItemUI> _transactionItems = new List<TransactionItemUI>();

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const int MAX_RECENT_TRANSACTIONS = 3;
        private const decimal MIN_WITHDRAWAL = 10m;

        // =====================================================================
        // STATE
        // =====================================================================

        private HostEarnings _earnings;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            // Main layout
            var verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(12, 12, 12, 12);
            verticalLayout.spacing = 12;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            // Header
            CreateHeader();

            // Balance section
            CreateBalanceSection();

            // Stats row
            CreateStatsRow();

            // Breakdown section
            CreateBreakdownSection();

            // Recent transactions
            CreateTransactionsSection();
        }

        private void CreateHeader()
        {
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(transform, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;

            _titleText = UIComponents.CreateText(headerRow.transform, "Earnings", 16);
            _titleText.fontStyle = FontStyles.Bold;
        }

        private void CreateBalanceSection()
        {
            var balanceRow = new GameObject("BalanceRow");
            balanceRow.transform.SetParent(transform, false);

            var balanceLayout = balanceRow.AddComponent<HorizontalLayoutGroup>();
            balanceLayout.spacing = 16;
            balanceLayout.childForceExpandWidth = false;
            balanceLayout.childAlignment = TextAnchor.MiddleLeft;

            // Available balance
            var availableContainer = new GameObject("AvailableContainer");
            availableContainer.transform.SetParent(balanceRow.transform, false);

            var availableLayout = availableContainer.AddComponent<VerticalLayoutGroup>();
            availableLayout.spacing = 2;

            _availableLabel = UIComponents.CreateText(availableContainer.transform, "Available", 11);
            _availableLabel.color = _theme?.TextSecondary ?? Color.gray;

            _availableAmountText = UIComponents.CreateText(availableContainer.transform, "$0.00", 28);
            _availableAmountText.fontStyle = FontStyles.Bold;
            _availableAmountText.color = _theme?.SuccessColor ?? Color.green;

            // Pending balance
            var pendingContainer = new GameObject("PendingContainer");
            pendingContainer.transform.SetParent(balanceRow.transform, false);

            var pendingLayout = pendingContainer.AddComponent<VerticalLayoutGroup>();
            pendingLayout.spacing = 2;

            _pendingLabel = UIComponents.CreateText(pendingContainer.transform, "Pending", 11);
            _pendingLabel.color = _theme?.TextSecondary ?? Color.gray;

            _pendingAmountText = UIComponents.CreateText(pendingContainer.transform, "$0.00", 18);
            _pendingAmountText.color = _theme?.WarningColor ?? new Color(1f, 0.8f, 0f);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(balanceRow.transform, false);
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Withdraw button
            var withdrawGO = new GameObject("WithdrawButton");
            withdrawGO.transform.SetParent(balanceRow.transform, false);

            var withdrawLayout = withdrawGO.AddComponent<LayoutElement>();
            withdrawLayout.preferredWidth = 100;
            withdrawLayout.preferredHeight = 36;

            var withdrawBg = withdrawGO.AddComponent<Image>();
            withdrawBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f);

            _withdrawButton = withdrawGO.AddComponent<Button>();
            _withdrawButton.onClick.AddListener(OnWithdrawButtonClicked);

            _withdrawButtonText = UIComponents.CreateText(withdrawGO.transform, "Withdraw", 14);
            _withdrawButtonText.alignment = TextAlignmentOptions.Center;
            _withdrawButtonText.fontStyle = FontStyles.Bold;

            var withdrawTextRect = _withdrawButtonText.GetComponent<RectTransform>();
            withdrawTextRect.anchorMin = Vector2.zero;
            withdrawTextRect.anchorMax = Vector2.one;
        }

        private void CreateStatsRow()
        {
            var statsRow = new GameObject("StatsRow");
            statsRow.transform.SetParent(transform, false);

            var statsLayout = statsRow.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 16;
            statsLayout.childForceExpandWidth = true;

            // Today
            CreateStatItem(statsRow.transform, "Today", out _todayLabel, out _todayAmountText);

            // This Week
            CreateStatItem(statsRow.transform, "This Week", out _weekLabel, out _weekAmountText);

            // This Month
            CreateStatItem(statsRow.transform, "This Month", out _monthLabel, out _monthAmountText);
        }

        private void CreateStatItem(Transform parent, string label, out TextMeshProUGUI labelText, out TextMeshProUGUI amountText)
        {
            var container = new GameObject($"Stat_{label}");
            container.transform.SetParent(parent, false);

            var bg = container.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;

            labelText = UIComponents.CreateText(container.transform, label, 10);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = _theme?.TextSecondary ?? Color.gray;

            amountText = UIComponents.CreateText(container.transform, "$0.00", 14);
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.fontStyle = FontStyles.Bold;
        }

        private void CreateBreakdownSection()
        {
            var breakdownRow = new GameObject("BreakdownRow");
            breakdownRow.transform.SetParent(transform, false);

            var breakdownLayout = breakdownRow.AddComponent<HorizontalLayoutGroup>();
            breakdownLayout.spacing = 16;
            breakdownLayout.childForceExpandWidth = true;

            // Esports earnings
            var esportsContainer = CreateBreakdownItem(breakdownRow.transform, "Esports", new Color(0.4f, 0.6f, 1f));
            _esportsEarningsText = esportsContainer.GetComponentInChildren<TextMeshProUGUI>();

            // Social earnings
            var socialContainer = CreateBreakdownItem(breakdownRow.transform, "Social", new Color(0.4f, 0.8f, 0.4f));
            _socialEarningsText = socialContainer.GetComponentInChildren<TextMeshProUGUI>();

            // Bonus earnings
            var bonusContainer = CreateBreakdownItem(breakdownRow.transform, "Bonuses", new Color(1f, 0.7f, 0.3f));
            _bonusEarningsText = bonusContainer.GetComponentInChildren<TextMeshProUGUI>();
        }

        private GameObject CreateBreakdownItem(Transform parent, string label, Color accentColor)
        {
            var container = new GameObject($"Breakdown_{label}");
            container.transform.SetParent(parent, false);

            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Color indicator
            var indicator = new GameObject("Indicator");
            indicator.transform.SetParent(container.transform, false);

            var indicatorLayout = indicator.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 4;
            indicatorLayout.preferredHeight = 16;

            var indicatorImage = indicator.AddComponent<Image>();
            indicatorImage.color = accentColor;

            // Label
            var labelText = UIComponents.CreateText(container.transform, label, 11);
            labelText.color = _theme?.TextSecondary ?? Color.gray;

            // Amount
            var amountText = UIComponents.CreateText(container.transform, "$0", 12);
            amountText.fontStyle = FontStyles.Bold;

            return container;
        }

        private void CreateTransactionsSection()
        {
            var sectionContainer = new GameObject("TransactionsSection");
            sectionContainer.transform.SetParent(transform, false);

            var sectionLayout = sectionContainer.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 6;

            // Header
            var headerRow = new GameObject("TransactionsHeader");
            headerRow.transform.SetParent(sectionContainer.transform, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;

            var titleText = UIComponents.CreateText(headerRow.transform, "Recent Transactions", 12);
            titleText.fontStyle = FontStyles.Bold;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            var viewAllGO = new GameObject("ViewHistoryButton");
            viewAllGO.transform.SetParent(headerRow.transform, false);

            _viewHistoryButton = viewAllGO.AddComponent<Button>();
            _viewHistoryButton.onClick.AddListener(() => OnViewHistoryClicked?.Invoke());

            var viewAllText = UIComponents.CreateText(viewAllGO.transform, "View All", 11);
            viewAllText.color = _theme?.AccentColor ?? new Color(0.4f, 0.8f, 1f);

            // Transactions container
            var containerGO = new GameObject("TransactionsContainer");
            containerGO.transform.SetParent(sectionContainer.transform, false);

            _transactionsContainer = containerGO.AddComponent<RectTransform>();

            var containerLayout = containerGO.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 4;

            // No transactions text
            _noTransactionsText = UIComponents.CreateText(containerGO.transform, "No recent transactions", 12);
            _noTransactionsText.alignment = TextAlignmentOptions.Center;
            _noTransactionsText.color = _theme?.TextSecondary ?? Color.gray;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the earnings data to display.
        /// </summary>
        public void SetEarnings(HostEarnings earnings)
        {
            _earnings = earnings;

            if (earnings == null)
            {
                ClearEarnings();
                return;
            }

            // Main balances
            _availableAmountText.text = $"${earnings.Available:N2}";
            _pendingAmountText.text = $"${earnings.Pending:N2}";

            // Enable/disable withdraw button
            _withdrawButton.interactable = earnings.Available >= MIN_WITHDRAWAL;

            // Stats
            _todayAmountText.text = $"${earnings.Today:N2}";
            _weekAmountText.text = $"${earnings.ThisWeek:N2}";
            _monthAmountText.text = $"${earnings.ThisMonth:N2}";

            // Breakdown
            UpdateBreakdownText(_esportsEarningsText, earnings.EsportsEarnings);
            UpdateBreakdownText(_socialEarningsText, earnings.SocialEarnings);
            UpdateBreakdownText(_bonusEarningsText, earnings.BonusEarnings);

            // Recent transactions
            RefreshTransactions(earnings.RecentTransactions);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void ClearEarnings()
        {
            _availableAmountText.text = "$0.00";
            _pendingAmountText.text = "$0.00";
            _todayAmountText.text = "$0.00";
            _weekAmountText.text = "$0.00";
            _monthAmountText.text = "$0.00";
            _withdrawButton.interactable = false;

            ClearTransactions();
        }

        private void UpdateBreakdownText(TextMeshProUGUI text, decimal amount)
        {
            if (text != null)
            {
                // Find the amount text (second TMP in parent)
                var parent = text.transform.parent;
                var texts = parent.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 1)
                {
                    texts[1].text = $"${amount:N0}";
                }
            }
        }

        private void RefreshTransactions(List<HostTransaction> transactions)
        {
            ClearTransactions();

            if (transactions == null || transactions.Count == 0)
            {
                _noTransactionsText.gameObject.SetActive(true);
                return;
            }

            _noTransactionsText.gameObject.SetActive(false);

            int count = Mathf.Min(transactions.Count, MAX_RECENT_TRANSACTIONS);
            for (int i = 0; i < count; i++)
            {
                var transaction = transactions[i];
                var item = CreateTransactionItem(transaction);
                _transactionItems.Add(item);
            }
        }

        private void ClearTransactions()
        {
            foreach (var item in _transactionItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _transactionItems.Clear();
            _noTransactionsText.gameObject.SetActive(true);
        }

        private TransactionItemUI CreateTransactionItem(HostTransaction transaction)
        {
            var itemGO = new GameObject($"Transaction_{transaction.Id}");
            itemGO.transform.SetParent(_transactionsContainer, false);

            var item = itemGO.AddComponent<TransactionItemUI>();
            item.Initialize(_theme);
            item.SetTransaction(transaction);

            return item;
        }

        private void OnWithdrawButtonClicked()
        {
            if (_earnings != null && _earnings.Available >= MIN_WITHDRAWAL)
            {
                OnWithdrawClicked?.Invoke();
            }
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null) _background.color = theme.CardBackground;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_availableLabel != null) _availableLabel.color = theme.TextSecondary;
            if (_availableAmountText != null) _availableAmountText.color = theme.SuccessColor;
            if (_pendingLabel != null) _pendingLabel.color = theme.TextSecondary;
            if (_pendingAmountText != null) _pendingAmountText.color = theme.WarningColor;
            if (_todayLabel != null) _todayLabel.color = theme.TextSecondary;
            if (_weekLabel != null) _weekLabel.color = theme.TextSecondary;
            if (_monthLabel != null) _monthLabel.color = theme.TextSecondary;
            if (_noTransactionsText != null) _noTransactionsText.color = theme.TextSecondary;

            foreach (var item in _transactionItems)
            {
                item?.ApplyTheme(theme);
            }
        }
    }

    /// <summary>
    /// Individual transaction item UI component.
    /// </summary>
    public class TransactionItemUI : UIPanel
    {
        private Image _background;
        private Image _typeIndicator;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProUGUI _amountText;
        private TextMeshProUGUI _timeText;

        private HostTransaction _transaction;

        protected override void SetupLayout()
        {
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.15f, 0.15f, 0.2f);

            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Type indicator
            var indicatorGO = new GameObject("Indicator");
            indicatorGO.transform.SetParent(transform, false);

            var indicatorLayout = indicatorGO.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 4;
            indicatorLayout.preferredHeight = 24;

            _typeIndicator = indicatorGO.AddComponent<Image>();

            // Description
            var descContainer = new GameObject("DescContainer");
            descContainer.transform.SetParent(transform, false);

            var descLayout = descContainer.AddComponent<LayoutElement>();
            descLayout.flexibleWidth = 1;

            _descriptionText = UIComponents.CreateText(descContainer.transform, "Transaction", 12);

            // Amount
            _amountText = UIComponents.CreateText(transform, "+$0.00", 14);
            _amountText.fontStyle = FontStyles.Bold;

            // Time
            _timeText = UIComponents.CreateText(transform, "Just now", 10);
            _timeText.color = _theme?.TextSecondary ?? Color.gray;
        }

        public void SetTransaction(HostTransaction transaction)
        {
            _transaction = transaction;

            _descriptionText.text = transaction.Description ?? transaction.Type;
            _amountText.text = transaction.Amount >= 0 ? $"+${transaction.Amount:N2}" : $"-${Math.Abs(transaction.Amount):N2}";
            _amountText.color = transaction.Amount >= 0 
                ? (_theme?.SuccessColor ?? Color.green) 
                : (_theme?.ErrorColor ?? Color.red);

            _timeText.text = GetRelativeTime(transaction.CreatedAt);

            // Set indicator color based on type
            _typeIndicator.color = GetTypeColor(transaction.Type);
        }

        private Color GetTypeColor(string type)
        {
            if (type.Contains("settlement") || type.Contains("rake"))
                return new Color(0.4f, 0.8f, 0.4f);
            if (type.Contains("withdrawal"))
                return new Color(1f, 0.5f, 0.3f);
            if (type.Contains("bonus"))
                return new Color(1f, 0.8f, 0.2f);
            return new Color(0.4f, 0.6f, 1f);
        }

        private string GetRelativeTime(DateTime time)
        {
            var diff = DateTime.UtcNow - time;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return time.ToString("MMM d");
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_background != null) _background.color = new Color(0.15f, 0.15f, 0.2f);
            if (_descriptionText != null) _descriptionText.color = theme.TextPrimary;
            if (_timeText != null) _timeText.color = theme.TextSecondary;
        }
    }
}
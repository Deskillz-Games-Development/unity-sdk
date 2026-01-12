// =============================================================================
// Deskillz SDK for Unity - Host Tier Progress UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Host;

namespace Deskillz.UI.Host
{
    /// <summary>
    /// UI component displaying host tier progress.
    /// Shows current tier, progress to next tier, and tier benefits.
    /// </summary>
    public class HostTierProgressUI : UIPanel
    {
        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _tierNameText;
        private TextMeshProUGUI _tierIconText;
        private Image _tierIconBackground;
        private Slider _progressBar;
        private Image _progressFill;
        private TextMeshProUGUI _progressText;
        private TextMeshProUGUI _currentValueText;
        private TextMeshProUGUI _nextTierText;
        private TextMeshProUGUI _sharePercentText;
        private TextMeshProUGUI _bonusText;
        private TextMeshProUGUI _daysUntilResetText;

        // =====================================================================
        // STATE
        // =====================================================================

        private RoomRevenueType _tierType = RoomRevenueType.Esports;
        private HostTier _currentTier = HostTier.Bronze;
        private float _progress = 0f;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            var rectTransform = GetComponent<RectTransform>();

            // Main layout
            var verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(12, 12, 12, 12);
            verticalLayout.spacing = 8;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            // Header row
            CreateHeaderRow();

            // Tier display
            CreateTierDisplay();

            // Progress section
            CreateProgressSection();

            // Benefits section
            CreateBenefitsSection();
        }

        private void CreateHeaderRow()
        {
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(transform, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;

            _titleText = UIComponents.CreateText(headerRow.transform, "Esports Tier", 14);
            _titleText.fontStyle = FontStyles.Bold;

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            _daysUntilResetText = UIComponents.CreateText(headerRow.transform, "", 11);
            _daysUntilResetText.color = _theme?.TextSecondary ?? Color.gray;
            _daysUntilResetText.alignment = TextAlignmentOptions.Right;
        }

        private void CreateTierDisplay()
        {
            var tierRow = new GameObject("TierRow");
            tierRow.transform.SetParent(transform, false);

            var tierLayout = tierRow.AddComponent<HorizontalLayoutGroup>();
            tierLayout.spacing = 12;
            tierLayout.childForceExpandWidth = false;
            tierLayout.childAlignment = TextAnchor.MiddleLeft;

            // Tier icon
            var iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(tierRow.transform, false);

            var iconLayout = iconContainer.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 40;
            iconLayout.preferredHeight = 40;

            _tierIconBackground = iconContainer.AddComponent<Image>();
            _tierIconBackground.color = GetTierColor(HostTier.Bronze);

            _tierIconText = UIComponents.CreateText(iconContainer.transform, "[B]", 16);
            _tierIconText.alignment = TextAlignmentOptions.Center;
            _tierIconText.fontStyle = FontStyles.Bold;

            var iconTextRect = _tierIconText.GetComponent<RectTransform>();
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.offsetMin = Vector2.zero;
            iconTextRect.offsetMax = Vector2.zero;

            // Tier name and share
            var infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(tierRow.transform, false);

            var infoLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 2;
            infoLayout.childForceExpandHeight = false;

            _tierNameText = UIComponents.CreateText(infoContainer.transform, "Bronze", 16);
            _tierNameText.fontStyle = FontStyles.Bold;

            _sharePercentText = UIComponents.CreateText(infoContainer.transform, "15% share", 12);
            _sharePercentText.color = _theme?.SuccessColor ?? Color.green;
        }

        private void CreateProgressSection()
        {
            var progressContainer = new GameObject("ProgressContainer");
            progressContainer.transform.SetParent(transform, false);

            var progressLayout = progressContainer.AddComponent<VerticalLayoutGroup>();
            progressLayout.spacing = 4;

            // Progress labels row
            var labelsRow = new GameObject("LabelsRow");
            labelsRow.transform.SetParent(progressContainer.transform, false);

            var labelsLayout = labelsRow.AddComponent<HorizontalLayoutGroup>();
            labelsLayout.childForceExpandWidth = false;

            _currentValueText = UIComponents.CreateText(labelsRow.transform, "0", 11);
            _currentValueText.color = _theme?.TextSecondary ?? Color.gray;

            var labelSpacer = new GameObject("Spacer");
            labelSpacer.transform.SetParent(labelsRow.transform, false);
            var labelSpacerLayout = labelSpacer.AddComponent<LayoutElement>();
            labelSpacerLayout.flexibleWidth = 1;

            _nextTierText = UIComponents.CreateText(labelsRow.transform, "Next: Silver", 11);
            _nextTierText.color = _theme?.TextSecondary ?? Color.gray;
            _nextTierText.alignment = TextAlignmentOptions.Right;

            // Progress bar
            var barContainer = new GameObject("BarContainer");
            barContainer.transform.SetParent(progressContainer.transform, false);

            var barLayout = barContainer.AddComponent<LayoutElement>();
            barLayout.preferredHeight = 12;

            _progressBar = UIComponents.CreateProgressBar(barContainer.transform);

            // Progress percentage
            _progressText = UIComponents.CreateText(progressContainer.transform, "0%", 11);
            _progressText.alignment = TextAlignmentOptions.Center;
            _progressText.color = _theme?.TextSecondary ?? Color.gray;
        }

        private void CreateBenefitsSection()
        {
            var benefitsContainer = new GameObject("BenefitsContainer");
            benefitsContainer.transform.SetParent(transform, false);

            var benefitsLayout = benefitsContainer.AddComponent<HorizontalLayoutGroup>();
            benefitsLayout.spacing = 8;
            benefitsLayout.childForceExpandWidth = true;

            // Bonus indicator
            var bonusContainer = new GameObject("BonusContainer");
            bonusContainer.transform.SetParent(benefitsContainer.transform, false);

            var bonusBg = bonusContainer.AddComponent<Image>();
            bonusBg.color = new Color(0.2f, 0.4f, 0.2f, 0.5f);

            var bonusLayout = bonusContainer.AddComponent<HorizontalLayoutGroup>();
            bonusLayout.padding = new RectOffset(8, 8, 4, 4);
            bonusLayout.spacing = 4;
            bonusLayout.childAlignment = TextAnchor.MiddleCenter;

            var bonusLabel = UIComponents.CreateText(bonusContainer.transform, "Bonus:", 10);
            bonusLabel.color = _theme?.TextSecondary ?? Color.gray;

            _bonusText = UIComponents.CreateText(bonusContainer.transform, "+0%", 12);
            _bonusText.fontStyle = FontStyles.Bold;
            _bonusText.color = _theme?.SuccessColor ?? Color.green;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the tier type (Esports or Social).
        /// </summary>
        public void SetTierType(RoomRevenueType type)
        {
            _tierType = type;
            _titleText.text = type == RoomRevenueType.Social ? "Social Tier" : "Esports Tier";
        }

        /// <summary>
        /// Set the current tier to display.
        /// </summary>
        public void SetTier(HostTier tier)
        {
            _currentTier = tier;

            var tierColor = GetTierColor(tier);
            _tierIconBackground.color = tierColor;
            _tierIconText.text = GetTierIcon(tier);
            _tierNameText.text = HostProfile.GetTierName(tier);

            // Get share percentage
            float sharePercent = HostManager.GetHostSharePercent(tier, _tierType, false);
            _sharePercentText.text = $"{sharePercent:F0}% share";

            // Update next tier text
            if (tier < HostTier.Elite)
            {
                var nextTier = (HostTier)((int)tier + 1);
                _nextTierText.text = $"Next: {HostProfile.GetTierName(nextTier)}";
                _nextTierText.gameObject.SetActive(true);
            }
            else
            {
                _nextTierText.text = "Max Tier!";
            }

            // Update bonus
            float bonus = HostManager.CalculateTotalBonus();
            _bonusText.text = bonus > 0 ? $"+{bonus:F1}%" : "+0%";

            // Update progress bar color
            if (_progressFill != null)
            {
                _progressFill.color = tierColor;
            }
        }

        /// <summary>
        /// Set progress to next tier (0-100).
        /// </summary>
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp(progress, 0f, 100f);

            if (_progressBar != null)
            {
                _progressBar.value = _progress / 100f;
            }

            _progressText.text = $"{_progress:F0}%";
        }

        /// <summary>
        /// Set the current threshold value.
        /// </summary>
        public void SetCurrentValue(int value, string unit = "players")
        {
            _currentValueText.text = $"{value} {unit}";
        }

        /// <summary>
        /// Set days until tier reset.
        /// </summary>
        public void SetDaysUntilReset(int days)
        {
            if (days > 0)
            {
                _daysUntilResetText.text = $"Resets in {days}d";
                _daysUntilResetText.gameObject.SetActive(true);
            }
            else
            {
                _daysUntilResetText.gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private string GetTierIcon(HostTier tier)
        {
            return tier switch
            {
                HostTier.Bronze => "[B]",
                HostTier.Silver => "[S]",
                HostTier.Gold => "[G]",
                HostTier.Platinum => "[P]",
                HostTier.Diamond => "[D]",
                HostTier.Elite => "[E]",
                _ => "[B]"
            };
        }

        private Color GetTierColor(HostTier tier)
        {
            return tier switch
            {
                HostTier.Bronze => new Color(0.8f, 0.5f, 0.2f),
                HostTier.Silver => new Color(0.75f, 0.75f, 0.75f),
                HostTier.Gold => new Color(1f, 0.84f, 0f),
                HostTier.Platinum => new Color(0.9f, 0.9f, 1f),
                HostTier.Diamond => new Color(0.6f, 0.85f, 1f),
                HostTier.Elite => new Color(0.8f, 0.4f, 1f),
                _ => Color.gray
            };
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null) _background.color = theme.CardBackground;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_tierNameText != null) _tierNameText.color = theme.TextPrimary;
            if (_sharePercentText != null) _sharePercentText.color = theme.SuccessColor;
            if (_progressText != null) _progressText.color = theme.TextSecondary;
            if (_currentValueText != null) _currentValueText.color = theme.TextSecondary;
            if (_nextTierText != null) _nextTierText.color = theme.TextSecondary;
            if (_bonusText != null) _bonusText.color = theme.SuccessColor;
            if (_daysUntilResetText != null) _daysUntilResetText.color = theme.TextSecondary;
        }
    }
}
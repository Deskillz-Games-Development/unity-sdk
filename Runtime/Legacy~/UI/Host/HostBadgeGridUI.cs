// =============================================================================
// Deskillz SDK for Unity - Host Badge Grid UI
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
    /// UI component displaying host badges in a grid layout.
    /// Shows earned badges with details and bonus information.
    /// </summary>
    public class HostBadgeGridUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when a badge is clicked</summary>
        public event Action<HostBadge> OnBadgeClicked;

        /// <summary>Called when view all is clicked</summary>
        public event Action OnViewAllClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _countText;
        private Button _viewAllButton;
        private RectTransform _gridContainer;
        private TextMeshProUGUI _noBadgesText;
        private TextMeshProUGUI _totalBonusText;

        private List<BadgeItemUI> _badgeItems = new List<BadgeItemUI>();

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const int MAX_DISPLAY_BADGES = 8;
        private const float BADGE_SIZE = 60f;
        private const float BADGE_SPACING = 8f;

        // =====================================================================
        // STATE
        // =====================================================================

        private List<HostBadge> _badges = new List<HostBadge>();

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
            verticalLayout.spacing = 8;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            // Header
            CreateHeader();

            // Badge grid
            CreateBadgeGrid();

            // Footer with total bonus
            CreateFooter();
        }

        private void CreateHeader()
        {
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(transform, false);

            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childForceExpandWidth = false;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;

            _titleText = UIComponents.CreateText(headerRow.transform, "Badges", 14);
            _titleText.fontStyle = FontStyles.Bold;

            _countText = UIComponents.CreateText(headerRow.transform, "(0)", 12);
            _countText.color = _theme?.TextSecondary ?? Color.gray;

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerRow.transform, false);
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // View all button
            var viewAllGO = new GameObject("ViewAllButton");
            viewAllGO.transform.SetParent(headerRow.transform, false);

            _viewAllButton = viewAllGO.AddComponent<Button>();
            _viewAllButton.onClick.AddListener(() => OnViewAllClicked?.Invoke());

            var viewAllText = UIComponents.CreateText(viewAllGO.transform, "View All", 12);
            viewAllText.color = _theme?.AccentColor ?? new Color(0.4f, 0.8f, 1f);
        }

        private void CreateBadgeGrid()
        {
            var gridGO = new GameObject("BadgeGrid");
            gridGO.transform.SetParent(transform, false);

            _gridContainer = gridGO.AddComponent<RectTransform>();

            var gridLayout = gridGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(BADGE_SIZE, BADGE_SIZE + 20); // Extra for label
            gridLayout.spacing = new Vector2(BADGE_SPACING, BADGE_SPACING);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            var layoutElement = gridGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = (BADGE_SIZE + 20 + BADGE_SPACING) * 2;

            // No badges text
            _noBadgesText = UIComponents.CreateText(gridGO.transform, "No badges earned yet", 14);
            _noBadgesText.alignment = TextAlignmentOptions.Center;
            _noBadgesText.color = _theme?.TextSecondary ?? Color.gray;

            var noBadgesRect = _noBadgesText.GetComponent<RectTransform>();
            noBadgesRect.anchorMin = Vector2.zero;
            noBadgesRect.anchorMax = Vector2.one;
            noBadgesRect.offsetMin = Vector2.zero;
            noBadgesRect.offsetMax = Vector2.zero;

            _noBadgesText.gameObject.SetActive(false);
        }

        private void CreateFooter()
        {
            var footerRow = new GameObject("FooterRow");
            footerRow.transform.SetParent(transform, false);

            var footerLayout = footerRow.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childForceExpandWidth = false;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;

            var bonusLabel = UIComponents.CreateText(footerRow.transform, "Total Badge Bonus:", 12);
            bonusLabel.color = _theme?.TextSecondary ?? Color.gray;

            _totalBonusText = UIComponents.CreateText(footerRow.transform, "+0%", 14);
            _totalBonusText.fontStyle = FontStyles.Bold;
            _totalBonusText.color = _theme?.SuccessColor ?? Color.green;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the badges to display.
        /// </summary>
        public void SetBadges(List<HostBadge> badges)
        {
            _badges = badges ?? new List<HostBadge>();

            // Clear existing items
            foreach (var item in _badgeItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _badgeItems.Clear();

            // Update count
            _countText.text = $"({_badges.Count})";

            // Show/hide no badges text
            _noBadgesText.gameObject.SetActive(_badges.Count == 0);

            // Create badge items (limited to MAX_DISPLAY_BADGES)
            int displayCount = Mathf.Min(_badges.Count, MAX_DISPLAY_BADGES);
            for (int i = 0; i < displayCount; i++)
            {
                var badge = _badges[i];
                var badgeItem = CreateBadgeItem(badge);
                _badgeItems.Add(badgeItem);
            }

            // Calculate total bonus
            float totalBonus = 0f;
            foreach (var badge in _badges)
            {
                if (badge.IsActive)
                {
                    totalBonus += badge.BonusPercent;
                }
            }
            totalBonus = Mathf.Min(totalBonus, HostManager.MAX_BONUS_PERCENT);
            _totalBonusText.text = totalBonus > 0 ? $"+{totalBonus:F1}%" : "+0%";

            // Show view all if more badges than displayed
            _viewAllButton.gameObject.SetActive(_badges.Count > MAX_DISPLAY_BADGES);
        }

        /// <summary>
        /// Add a newly earned badge with animation.
        /// </summary>
        public void AddBadge(HostBadge badge)
        {
            _badges.Add(badge);
            SetBadges(_badges);

            // Could add animation here
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private BadgeItemUI CreateBadgeItem(HostBadge badge)
        {
            var itemGO = new GameObject($"Badge_{badge.Code}");
            itemGO.transform.SetParent(_gridContainer, false);

            var badgeItem = itemGO.AddComponent<BadgeItemUI>();
            badgeItem.Initialize(_theme);
            badgeItem.SetBadge(badge);
            badgeItem.OnClicked += () => OnBadgeClicked?.Invoke(badge);

            return badgeItem;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null) _background.color = theme.CardBackground;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_countText != null) _countText.color = theme.TextSecondary;
            if (_noBadgesText != null) _noBadgesText.color = theme.TextSecondary;
            if (_totalBonusText != null) _totalBonusText.color = theme.SuccessColor;

            foreach (var item in _badgeItems)
            {
                item?.ApplyTheme(theme);
            }
        }
    }

    /// <summary>
    /// Individual badge item UI component.
    /// </summary>
    public class BadgeItemUI : UIPanel
    {
        public event Action OnClicked;

        private Image _background;
        private Image _iconImage;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _bonusText;
        private Image _expiryIndicator;
        private Button _button;

        private HostBadge _badge;

        protected override void SetupLayout()
        {
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.2f, 0.2f, 0.25f);

            _button = gameObject.AddComponent<Button>();
            _button.onClick.AddListener(() => OnClicked?.Invoke());

            var verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(4, 4, 4, 4);
            verticalLayout.spacing = 2;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            // Icon container
            var iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(transform, false);

            var iconLayout = iconContainer.AddComponent<LayoutElement>();
            iconLayout.preferredHeight = 40;

            _iconImage = iconContainer.AddComponent<Image>();
            _iconImage.color = Color.white;

            // Expiry indicator
            var expiryGO = new GameObject("ExpiryIndicator");
            expiryGO.transform.SetParent(iconContainer.transform, false);

            var expiryRect = expiryGO.AddComponent<RectTransform>();
            expiryRect.anchorMin = new Vector2(1, 1);
            expiryRect.anchorMax = new Vector2(1, 1);
            expiryRect.pivot = new Vector2(1, 1);
            expiryRect.sizeDelta = new Vector2(12, 12);

            _expiryIndicator = expiryGO.AddComponent<Image>();
            _expiryIndicator.color = new Color(1f, 0.6f, 0f);
            _expiryIndicator.gameObject.SetActive(false);

            // Name
            _nameText = UIComponents.CreateText(transform, "Badge", 9);
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.enableWordWrapping = true;
            _nameText.overflowMode = TextOverflowModes.Ellipsis;

            // Bonus
            _bonusText = UIComponents.CreateText(transform, "+0%", 10);
            _bonusText.alignment = TextAlignmentOptions.Center;
            _bonusText.fontStyle = FontStyles.Bold;
            _bonusText.color = _theme?.SuccessColor ?? Color.green;
        }

        public void SetBadge(HostBadge badge)
        {
            _badge = badge;

            _nameText.text = badge.Name;
            _bonusText.text = badge.BonusPercent > 0 ? $"+{badge.BonusPercent:F1}%" : "";
            _bonusText.gameObject.SetActive(badge.BonusPercent > 0);

            // Set category color
            _background.color = GetCategoryColor(badge.Category);

            // Show expiry indicator for performance badges
            _expiryIndicator.gameObject.SetActive(badge.ExpiresAt.HasValue);

            // Dim if expired
            if (!badge.IsActive)
            {
                var color = _background.color;
                color.a = 0.5f;
                _background.color = color;
            }

            // Load icon if available
            if (!string.IsNullOrEmpty(badge.IconUrl))
            {
                LoadIcon(badge.IconUrl);
            }
            else
            {
                SetDefaultIcon(badge.Category);
            }
        }

        private Color GetCategoryColor(BadgeCategory category)
        {
            return category switch
            {
                BadgeCategory.Achievement => new Color(0.3f, 0.3f, 0.5f),
                BadgeCategory.Performance => new Color(0.3f, 0.5f, 0.3f),
                BadgeCategory.Exclusive => new Color(0.5f, 0.3f, 0.5f),
                _ => new Color(0.25f, 0.25f, 0.3f)
            };
        }

        private void SetDefaultIcon(BadgeCategory category)
        {
            // Set a default color based on category
            _iconImage.color = category switch
            {
                BadgeCategory.Achievement => new Color(1f, 0.84f, 0f),
                BadgeCategory.Performance => new Color(0.4f, 0.8f, 0.4f),
                BadgeCategory.Exclusive => new Color(0.8f, 0.4f, 1f),
                _ => Color.gray
            };
        }

        private void LoadIcon(string url)
        {
            // In production, load image from URL
            _iconImage.color = Color.white;
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_nameText != null) _nameText.color = theme.TextPrimary;
            if (_bonusText != null) _bonusText.color = theme.SuccessColor;
        }
    }
}
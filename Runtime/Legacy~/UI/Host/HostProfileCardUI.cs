// =============================================================================
// Deskillz SDK for Unity - Host Profile Card UI
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
    /// UI card displaying host profile information.
    /// Shows avatar, name, level, verification status, and streak.
    /// </summary>
    public class HostProfileCardUI : UIPanel
    {
        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private Image _avatarImage;
        private Image _avatarBorder;
        private TextMeshProUGUI _usernameText;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _levelTitleText;
        private Image _verifiedBadge;
        private TextMeshProUGUI _verifiedText;
        private TextMeshProUGUI _streakText;
        private Image _streakIcon;
        private TextMeshProUGUI _ratingText;
        private Image[] _ratingStars;

        // Level progress
        private Slider _levelProgressBar;
        private TextMeshProUGUI _levelProgressText;

        // Stats row
        private TextMeshProUGUI _totalRoomsText;
        private TextMeshProUGUI _totalPlayersText;
        private TextMeshProUGUI _totalEarningsText;

        // =====================================================================
        // STATE
        // =====================================================================

        private HostProfile _profile;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = _theme?.CardBackground ?? new Color(0.12f, 0.12f, 0.18f);

            // Add rounded corners if available
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 120);

            // Main layout
            var horizontalLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(16, 16, 16, 16);
            horizontalLayout.spacing = 16;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            // Avatar section
            CreateAvatarSection();

            // Info section
            CreateInfoSection();

            // Stats section
            CreateStatsSection();
        }

        private void CreateAvatarSection()
        {
            var avatarContainer = new GameObject("AvatarContainer");
            avatarContainer.transform.SetParent(transform, false);

            var avatarLayout = avatarContainer.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 80;
            avatarLayout.preferredHeight = 80;

            // Avatar border (shows tier color)
            _avatarBorder = avatarContainer.AddComponent<Image>();
            _avatarBorder.color = GetTierColor(HostTier.Bronze);

            // Avatar mask
            var maskGO = new GameObject("AvatarMask");
            maskGO.transform.SetParent(avatarContainer.transform, false);

            var maskRect = maskGO.AddComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = new Vector2(3, 3);
            maskRect.offsetMax = new Vector2(-3, -3);

            var mask = maskGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var maskImage = maskGO.AddComponent<Image>();

            // Avatar image
            var avatarGO = new GameObject("Avatar");
            avatarGO.transform.SetParent(maskGO.transform, false);

            var avatarRect = avatarGO.AddComponent<RectTransform>();
            avatarRect.anchorMin = Vector2.zero;
            avatarRect.anchorMax = Vector2.one;
            avatarRect.offsetMin = Vector2.zero;
            avatarRect.offsetMax = Vector2.zero;

            _avatarImage = avatarGO.AddComponent<Image>();
            _avatarImage.color = Color.gray;

            // Verified badge overlay
            var verifiedGO = new GameObject("VerifiedBadge");
            verifiedGO.transform.SetParent(avatarContainer.transform, false);

            var verifiedRect = verifiedGO.AddComponent<RectTransform>();
            verifiedRect.anchorMin = new Vector2(1, 0);
            verifiedRect.anchorMax = new Vector2(1, 0);
            verifiedRect.pivot = new Vector2(1, 0);
            verifiedRect.sizeDelta = new Vector2(24, 24);
            verifiedRect.anchoredPosition = new Vector2(4, -4);

            _verifiedBadge = verifiedGO.AddComponent<Image>();
            _verifiedBadge.color = _theme?.SuccessColor ?? Color.green;
            _verifiedBadge.gameObject.SetActive(false);
        }

        private void CreateInfoSection()
        {
            var infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(transform, false);

            var infoLayout = infoContainer.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;

            var verticalLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 4;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childAlignment = TextAnchor.UpperLeft;

            // Username row
            var usernameRow = new GameObject("UsernameRow");
            usernameRow.transform.SetParent(infoContainer.transform, false);

            var usernameLayout = usernameRow.AddComponent<HorizontalLayoutGroup>();
            usernameLayout.spacing = 8;
            usernameLayout.childForceExpandWidth = false;

            _usernameText = UIComponents.CreateText(usernameRow.transform, "Username", 18);
            _usernameText.fontStyle = FontStyles.Bold;

            _verifiedText = UIComponents.CreateText(usernameRow.transform, "[18+]", 12);
            _verifiedText.color = _theme?.SuccessColor ?? Color.green;
            _verifiedText.gameObject.SetActive(false);

            // Level row
            var levelRow = new GameObject("LevelRow");
            levelRow.transform.SetParent(infoContainer.transform, false);

            var levelLayout = levelRow.AddComponent<HorizontalLayoutGroup>();
            levelLayout.spacing = 8;
            levelLayout.childForceExpandWidth = false;

            _levelText = UIComponents.CreateText(levelRow.transform, "Level 1", 14);
            _levelText.color = _theme?.AccentColor ?? new Color(0.4f, 0.8f, 1f);

            _levelTitleText = UIComponents.CreateText(levelRow.transform, "Newcomer", 14);
            _levelTitleText.color = _theme?.TextSecondary ?? Color.gray;

            // Level progress
            var progressContainer = new GameObject("ProgressContainer");
            progressContainer.transform.SetParent(infoContainer.transform, false);

            var progressLayout = progressContainer.AddComponent<LayoutElement>();
            progressLayout.preferredHeight = 20;

            var progressHLayout = progressContainer.AddComponent<HorizontalLayoutGroup>();
            progressHLayout.spacing = 8;
            progressHLayout.childForceExpandWidth = false;
            progressHLayout.childAlignment = TextAnchor.MiddleLeft;

            var sliderGO = new GameObject("ProgressBar");
            sliderGO.transform.SetParent(progressContainer.transform, false);

            var sliderLayout = sliderGO.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1;
            sliderLayout.preferredHeight = 8;

            _levelProgressBar = UIComponents.CreateProgressBar(sliderGO.transform);

            _levelProgressText = UIComponents.CreateText(progressContainer.transform, "0%", 12);
            _levelProgressText.color = _theme?.TextSecondary ?? Color.gray;

            // Streak row
            var streakRow = new GameObject("StreakRow");
            streakRow.transform.SetParent(infoContainer.transform, false);

            var streakLayout = streakRow.AddComponent<HorizontalLayoutGroup>();
            streakLayout.spacing = 4;
            streakLayout.childForceExpandWidth = false;

            var streakIconGO = new GameObject("StreakIcon");
            streakIconGO.transform.SetParent(streakRow.transform, false);

            var streakIconLayout = streakIconGO.AddComponent<LayoutElement>();
            streakIconLayout.preferredWidth = 16;
            streakIconLayout.preferredHeight = 16;

            _streakIcon = streakIconGO.AddComponent<Image>();
            _streakIcon.color = new Color(1f, 0.6f, 0f); // Orange for fire

            _streakText = UIComponents.CreateText(streakRow.transform, "0 day streak", 12);
            _streakText.color = _theme?.TextSecondary ?? Color.gray;
        }

        private void CreateStatsSection()
        {
            var statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(transform, false);

            var statsLayout = statsContainer.AddComponent<LayoutElement>();
            statsLayout.preferredWidth = 100;

            var verticalLayout = statsContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 4;
            verticalLayout.childAlignment = TextAnchor.UpperRight;

            // Rating
            var ratingRow = new GameObject("RatingRow");
            ratingRow.transform.SetParent(statsContainer.transform, false);

            var ratingLayout = ratingRow.AddComponent<HorizontalLayoutGroup>();
            ratingLayout.spacing = 2;
            ratingLayout.childForceExpandWidth = false;
            ratingLayout.childAlignment = TextAnchor.MiddleRight;

            _ratingStars = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var starGO = new GameObject($"Star{i}");
                starGO.transform.SetParent(ratingRow.transform, false);

                var starLayout = starGO.AddComponent<LayoutElement>();
                starLayout.preferredWidth = 14;
                starLayout.preferredHeight = 14;

                _ratingStars[i] = starGO.AddComponent<Image>();
                _ratingStars[i].color = Color.gray;
            }

            _ratingText = UIComponents.CreateText(statsContainer.transform, "0.0", 12);
            _ratingText.alignment = TextAlignmentOptions.Right;
            _ratingText.color = _theme?.TextSecondary ?? Color.gray;

            // Total earnings
            _totalEarningsText = UIComponents.CreateText(statsContainer.transform, "$0.00", 16);
            _totalEarningsText.fontStyle = FontStyles.Bold;
            _totalEarningsText.alignment = TextAlignmentOptions.Right;
            _totalEarningsText.color = _theme?.SuccessColor ?? Color.green;

            // Total rooms
            _totalRoomsText = UIComponents.CreateText(statsContainer.transform, "0 rooms", 12);
            _totalRoomsText.alignment = TextAlignmentOptions.Right;
            _totalRoomsText.color = _theme?.TextSecondary ?? Color.gray;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the profile to display.
        /// </summary>
        public void SetProfile(HostProfile profile)
        {
            _profile = profile;

            if (profile == null)
            {
                ClearProfile();
                return;
            }

            // Username
            _usernameText.text = profile.Username ?? "Unknown";

            // Verification
            _verifiedBadge.gameObject.SetActive(profile.IsVerified);
            _verifiedText.gameObject.SetActive(profile.IsVerified);

            // Level
            _levelText.text = $"Level {profile.Level}";
            _levelTitleText.text = profile.GetLevelTitle();

            // Avatar border color based on highest tier
            var highestTier = (HostTier)Math.Max((int)profile.EsportsTier, (int)profile.SocialTier);
            _avatarBorder.color = GetTierColor(highestTier);

            // Streak
            if (profile.CurrentStreak > 0)
            {
                _streakText.text = $"{profile.CurrentStreak} day streak";
                _streakIcon.color = GetStreakColor(profile.CurrentStreak);
            }
            else
            {
                _streakText.text = "No streak";
                _streakIcon.color = Color.gray;
            }

            // Rating
            SetRating(profile.Rating, profile.RatingCount);

            // Stats
            _totalEarningsText.text = $"${profile.TotalEarnings:N2}";
            _totalRoomsText.text = $"{profile.TotalRoomsCreated} rooms";

            // Load avatar image
            if (!string.IsNullOrEmpty(profile.AvatarUrl))
            {
                LoadAvatar(profile.AvatarUrl);
            }
        }

        /// <summary>
        /// Set level progress (0-100).
        /// </summary>
        public void SetLevelProgress(float progress)
        {
            progress = Mathf.Clamp01(progress / 100f);
            _levelProgressBar.value = progress;
            _levelProgressText.text = $"{progress * 100:F0}%";
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void ClearProfile()
        {
            _usernameText.text = "Not logged in";
            _levelText.text = "Level 0";
            _levelTitleText.text = "";
            _verifiedBadge.gameObject.SetActive(false);
            _verifiedText.gameObject.SetActive(false);
            _streakText.text = "";
            _totalEarningsText.text = "$0.00";
            _totalRoomsText.text = "0 rooms";
            _ratingText.text = "0.0";
            _levelProgressBar.value = 0;
            _levelProgressText.text = "0%";
        }

        private void SetRating(float rating, int count)
        {
            _ratingText.text = count > 0 ? $"{rating:F1} ({count})" : "No ratings";

            for (int i = 0; i < 5; i++)
            {
                if (i < Mathf.FloorToInt(rating))
                {
                    _ratingStars[i].color = new Color(1f, 0.8f, 0f); // Gold
                }
                else if (i < rating)
                {
                    _ratingStars[i].color = new Color(1f, 0.8f, 0f, 0.5f); // Half gold
                }
                else
                {
                    _ratingStars[i].color = Color.gray;
                }
            }
        }

        private void LoadAvatar(string url)
        {
            // In a real implementation, use UnityWebRequestTexture
            // For now, just set a placeholder color
            _avatarImage.color = new Color(0.3f, 0.3f, 0.4f);
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

        private Color GetStreakColor(int streak)
        {
            if (streak >= 30) return new Color(1f, 0.2f, 0.2f); // Red hot
            if (streak >= 14) return new Color(1f, 0.5f, 0f);   // Orange
            if (streak >= 7) return new Color(1f, 0.7f, 0f);    // Yellow-orange
            if (streak >= 3) return new Color(1f, 0.85f, 0f);   // Yellow
            return new Color(0.8f, 0.8f, 0.8f);                  // Gray
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null) _background.color = theme.CardBackground;
            if (_usernameText != null) _usernameText.color = theme.TextPrimary;
            if (_levelText != null) _levelText.color = theme.AccentColor;
            if (_levelTitleText != null) _levelTitleText.color = theme.TextSecondary;
            if (_verifiedText != null) _verifiedText.color = theme.SuccessColor;
            if (_verifiedBadge != null) _verifiedBadge.color = theme.SuccessColor;
            if (_streakText != null) _streakText.color = theme.TextSecondary;
            if (_ratingText != null) _ratingText.color = theme.TextSecondary;
            if (_totalEarningsText != null) _totalEarningsText.color = theme.SuccessColor;
            if (_totalRoomsText != null) _totalRoomsText.color = theme.TextSecondary;
            if (_levelProgressText != null) _levelProgressText.color = theme.TextSecondary;
        }
    }
}
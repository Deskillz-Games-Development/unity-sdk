// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz.UI
{
    /// <summary>
    /// Defines the visual theme for Deskillz UI components.
    /// Can be customized via ScriptableObject or code.
    /// </summary>
    [CreateAssetMenu(fileName = "DeskillzTheme", menuName = "Deskillz/UI Theme")]
    public class DeskillzTheme : ScriptableObject
    {
        // =============================================================================
        // COLORS
        // =============================================================================

        [Header("Primary Colors")]
        [Tooltip("Main brand color")]
        public Color PrimaryColor = new Color(0.4f, 0.2f, 1f);        // Purple

        [Tooltip("Secondary brand color")]
        public Color SecondaryColor = new Color(0.2f, 0.8f, 0.6f);    // Teal

        [Tooltip("Accent color for highlights")]
        public Color AccentColor = new Color(1f, 0.6f, 0.2f);         // Orange

        [Header("Status Colors")]
        [Tooltip("Success/positive color")]
        public Color SuccessColor = new Color(0.2f, 0.8f, 0.4f);      // Green

        [Tooltip("Warning color")]
        public Color WarningColor = new Color(1f, 0.8f, 0.2f);        // Yellow

        [Tooltip("Error/negative color")]
        public Color ErrorColor = new Color(1f, 0.3f, 0.3f);          // Red

        [Tooltip("Info color")]
        public Color InfoColor = new Color(0.3f, 0.6f, 1f);           // Blue

        [Header("Background Colors")]
        [Tooltip("Main background")]
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.15f);  // Dark

        [Tooltip("Panel/card background")]
        public Color PanelColor = new Color(0.15f, 0.15f, 0.2f);

        [Tooltip("Overlay background")]
        public Color OverlayColor = new Color(0f, 0f, 0f, 0.8f);

        [Header("Text Colors")]
        [Tooltip("Primary text")]
        public Color TextPrimary = Color.white;

        [Tooltip("Secondary text")]
        public Color TextSecondary = new Color(0.7f, 0.7f, 0.7f);

        [Tooltip("Disabled text")]
        public Color TextDisabled = new Color(0.4f, 0.4f, 0.4f);

        [Tooltip("Text on primary color")]
        public Color TextOnPrimary = Color.white;

        // =============================================================================
        // TYPOGRAPHY
        // =============================================================================

        [Header("Typography")]
        [Tooltip("Primary font (leave null for default)")]
        public Font PrimaryFont;

        [Tooltip("Score/number font (leave null for default)")]
        public Font ScoreFont;

        [Tooltip("Title text size")]
        public int TitleFontSize = 48;

        [Tooltip("Heading text size")]
        public int HeadingFontSize = 32;

        [Tooltip("Body text size")]
        public int BodyFontSize = 24;

        [Tooltip("Small text size")]
        public int SmallFontSize = 18;

        [Tooltip("Score display size")]
        public int ScoreFontSize = 64;

        // =============================================================================
        // SPACING
        // =============================================================================

        [Header("Spacing")]
        [Tooltip("Small padding")]
        public float PaddingSmall = 8f;

        [Tooltip("Medium padding")]
        public float PaddingMedium = 16f;

        [Tooltip("Large padding")]
        public float PaddingLarge = 24f;

        [Tooltip("Corner radius for panels")]
        public float CornerRadius = 12f;

        [Tooltip("Border width")]
        public float BorderWidth = 2f;

        // =============================================================================
        // ANIMATION
        // =============================================================================

        [Header("Animation")]
        [Tooltip("Fast animation duration")]
        public float AnimationFast = 0.15f;

        [Tooltip("Normal animation duration")]
        public float AnimationNormal = 0.3f;

        [Tooltip("Slow animation duration")]
        public float AnimationSlow = 0.5f;

        [Tooltip("Score count-up speed (points per second)")]
        public float ScoreAnimationSpeed = 1000f;

        // =============================================================================
        // BUTTON STYLES
        // =============================================================================

        [Header("Button Styles")]
        public ButtonStyle PrimaryButton = new ButtonStyle
        {
            NormalColor = new Color(0.4f, 0.2f, 1f),
            HoverColor = new Color(0.5f, 0.3f, 1f),
            PressedColor = new Color(0.3f, 0.15f, 0.8f),
            DisabledColor = new Color(0.3f, 0.3f, 0.3f),
            TextColor = Color.white,
            Height = 56f
        };

        public ButtonStyle SecondaryButton = new ButtonStyle
        {
            NormalColor = new Color(0.2f, 0.2f, 0.25f),
            HoverColor = new Color(0.25f, 0.25f, 0.3f),
            PressedColor = new Color(0.15f, 0.15f, 0.2f),
            DisabledColor = new Color(0.15f, 0.15f, 0.15f),
            TextColor = Color.white,
            Height = 48f
        };

        public ButtonStyle DangerButton = new ButtonStyle
        {
            NormalColor = new Color(0.8f, 0.2f, 0.2f),
            HoverColor = new Color(0.9f, 0.3f, 0.3f),
            PressedColor = new Color(0.6f, 0.15f, 0.15f),
            DisabledColor = new Color(0.4f, 0.2f, 0.2f),
            TextColor = Color.white,
            Height = 48f
        };

        // =============================================================================
        // ICONS
        // =============================================================================

        [Header("Icons")]
        public Sprite WinIcon;
        public Sprite LoseIcon;
        public Sprite TieIcon;
        public Sprite CoinIcon;
        public Sprite TrophyIcon;
        public Sprite TimerIcon;
        public Sprite PauseIcon;
        public Sprite PlayIcon;
        public Sprite CloseIcon;

        // =============================================================================
        // SOUNDS
        // =============================================================================

        [Header("Sounds")]
        public AudioClip ButtonClickSound;
        public AudioClip NotificationSound;
        public AudioClip WinSound;
        public AudioClip LoseSound;
        public AudioClip CountdownSound;
        public AudioClip ScoreUpdateSound;

        // =============================================================================
        // FACTORY METHODS
        // =============================================================================

        /// <summary>
        /// Create a default theme instance.
        /// </summary>
        public static DeskillzTheme CreateDefault()
        {
            var theme = CreateInstance<DeskillzTheme>();
            theme.name = "DefaultTheme";
            return theme;
        }

        /// <summary>
        /// Create a dark theme.
        /// </summary>
        public static DeskillzTheme CreateDark()
        {
            var theme = CreateInstance<DeskillzTheme>();
            theme.name = "DarkTheme";
            theme.BackgroundColor = new Color(0.08f, 0.08f, 0.1f);
            theme.PanelColor = new Color(0.12f, 0.12f, 0.15f);
            return theme;
        }

        /// <summary>
        /// Create a light theme.
        /// </summary>
        public static DeskillzTheme CreateLight()
        {
            var theme = CreateInstance<DeskillzTheme>();
            theme.name = "LightTheme";
            theme.BackgroundColor = new Color(0.95f, 0.95f, 0.97f);
            theme.PanelColor = Color.white;
            theme.TextPrimary = new Color(0.1f, 0.1f, 0.1f);
            theme.TextSecondary = new Color(0.4f, 0.4f, 0.4f);
            return theme;
        }

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        /// <summary>
        /// Get color for notification type.
        /// </summary>
        public Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => SuccessColor,
                NotificationType.Warning => WarningColor,
                NotificationType.Error => ErrorColor,
                NotificationType.Info => InfoColor,
                _ => InfoColor
            };
        }

        /// <summary>
        /// Get color for match outcome.
        /// </summary>
        public Color GetOutcomeColor(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Win => SuccessColor,
                MatchOutcome.Loss => ErrorColor,
                MatchOutcome.Tie => WarningColor,
                MatchOutcome.Forfeit => ErrorColor,
                _ => TextSecondary
            };
        }

        /// <summary>
        /// Get icon for match outcome.
        /// </summary>
        public Sprite GetOutcomeIcon(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Win => WinIcon,
                MatchOutcome.Loss => LoseIcon,
                MatchOutcome.Tie => TieIcon,
                _ => null
            };
        }

        /// <summary>
        /// Lerp between two themes.
        /// </summary>
        public static DeskillzTheme Lerp(DeskillzTheme a, DeskillzTheme b, float t)
        {
            var result = CreateInstance<DeskillzTheme>();

            result.PrimaryColor = Color.Lerp(a.PrimaryColor, b.PrimaryColor, t);
            result.SecondaryColor = Color.Lerp(a.SecondaryColor, b.SecondaryColor, t);
            result.AccentColor = Color.Lerp(a.AccentColor, b.AccentColor, t);
            result.BackgroundColor = Color.Lerp(a.BackgroundColor, b.BackgroundColor, t);
            result.PanelColor = Color.Lerp(a.PanelColor, b.PanelColor, t);
            result.TextPrimary = Color.Lerp(a.TextPrimary, b.TextPrimary, t);
            result.TextSecondary = Color.Lerp(a.TextSecondary, b.TextSecondary, t);

            return result;
        }
    }

    // =============================================================================
    // BUTTON STYLE
    // =============================================================================

    /// <summary>
    /// Style definition for buttons.
    /// </summary>
    [Serializable]
    public class ButtonStyle
    {
        public Color NormalColor = Color.white;
        public Color HoverColor = Color.white;
        public Color PressedColor = Color.gray;
        public Color DisabledColor = Color.gray;
        public Color TextColor = Color.black;
        public float Height = 48f;
        public float CornerRadius = 8f;
    }
}

// =============================================================================
// Deskillz SDK for Unity - Theme Configuration
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using UnityEngine;

namespace Deskillz.UI
{
    /// <summary>
    /// Theme configuration for Deskillz UI components.
    /// Defines colors, fonts, and styling options.
    /// </summary>
    [System.Serializable]
    public class DeskillzTheme
    {
        // =====================================================================
        // PRIMARY COLORS
        // =====================================================================

        /// <summary>Primary brand color (buttons, highlights)</summary>
        public Color PrimaryColor = new Color(0.2f, 0.6f, 1f);

        /// <summary>Secondary brand color</summary>
        public Color SecondaryColor = new Color(0.6f, 0.4f, 1f);

        /// <summary>Accent color for highlights</summary>
        public Color AccentColor = new Color(0.4f, 0.8f, 1f);

        // =====================================================================
        // BACKGROUND COLORS
        // =====================================================================

        /// <summary>Main background color</summary>
        public Color BackgroundPrimary = new Color(0.06f, 0.06f, 0.1f);

        /// <summary>Secondary background color</summary>
        public Color BackgroundSecondary = new Color(0.1f, 0.1f, 0.15f);

        /// <summary>Card/panel background color</summary>
        public Color CardBackground = new Color(0.12f, 0.12f, 0.18f);

        /// <summary>Input field background color</summary>
        public Color InputBackground = new Color(0.15f, 0.15f, 0.2f);

        /// <summary>Modal overlay color</summary>
        public Color OverlayColor = new Color(0, 0, 0, 0.8f);

        // =====================================================================
        // TEXT COLORS
        // =====================================================================

        /// <summary>Primary text color</summary>
        public Color TextPrimary = Color.white;

        /// <summary>Secondary/muted text color</summary>
        public Color TextSecondary = new Color(0.6f, 0.6f, 0.65f);

        /// <summary>Disabled text color</summary>
        public Color TextDisabled = new Color(0.4f, 0.4f, 0.45f);

        /// <summary>Link text color</summary>
        public Color TextLink = new Color(0.4f, 0.8f, 1f);

        // =====================================================================
        // STATUS COLORS
        // =====================================================================

        /// <summary>Success/positive color</summary>
        public Color SuccessColor = new Color(0.2f, 0.8f, 0.4f);

        /// <summary>Warning color</summary>
        public Color WarningColor = new Color(1f, 0.7f, 0f);

        /// <summary>Error/danger color</summary>
        public Color ErrorColor = new Color(1f, 0.3f, 0.3f);

        /// <summary>Info color</summary>
        public Color InfoColor = new Color(0.3f, 0.7f, 1f);

        // =====================================================================
        // TIER COLORS
        // =====================================================================

        /// <summary>Bronze tier color</summary>
        public Color TierBronze = new Color(0.8f, 0.5f, 0.2f);

        /// <summary>Silver tier color</summary>
        public Color TierSilver = new Color(0.75f, 0.75f, 0.75f);

        /// <summary>Gold tier color</summary>
        public Color TierGold = new Color(1f, 0.84f, 0f);

        /// <summary>Platinum tier color</summary>
        public Color TierPlatinum = new Color(0.9f, 0.9f, 1f);

        /// <summary>Diamond tier color</summary>
        public Color TierDiamond = new Color(0.6f, 0.85f, 1f);

        /// <summary>Elite tier color</summary>
        public Color TierElite = new Color(0.8f, 0.4f, 1f);

        // =====================================================================
        // BORDER & DIVIDER
        // =====================================================================

        /// <summary>Border color</summary>
        public Color BorderColor = new Color(0.25f, 0.25f, 0.3f);

        /// <summary>Divider color</summary>
        public Color DividerColor = new Color(0.2f, 0.2f, 0.25f);

        // =====================================================================
        // BUTTON COLORS
        // =====================================================================

        /// <summary>Button hover color multiplier</summary>
        public float ButtonHoverMultiplier = 1.1f;

        /// <summary>Button pressed color multiplier</summary>
        public float ButtonPressedMultiplier = 0.9f;

        /// <summary>Button disabled alpha</summary>
        public float ButtonDisabledAlpha = 0.5f;

        // =====================================================================
        // SPACING
        // =====================================================================

        /// <summary>Small spacing (4px)</summary>
        public int SpacingSmall = 4;

        /// <summary>Medium spacing (8px)</summary>
        public int SpacingMedium = 8;

        /// <summary>Large spacing (16px)</summary>
        public int SpacingLarge = 16;

        /// <summary>Extra large spacing (24px)</summary>
        public int SpacingXLarge = 24;

        // =====================================================================
        // CORNER RADIUS
        // =====================================================================

        /// <summary>Small corner radius</summary>
        public float CornerRadiusSmall = 4f;

        /// <summary>Medium corner radius</summary>
        public float CornerRadiusMedium = 8f;

        /// <summary>Large corner radius</summary>
        public float CornerRadiusLarge = 12f;

        // =====================================================================
        // PRESETS
        // =====================================================================

        /// <summary>Default dark theme</summary>
        public static DeskillzTheme Default => new DeskillzTheme();

        /// <summary>Light theme</summary>
        public static DeskillzTheme Light => new DeskillzTheme
        {
            BackgroundPrimary = new Color(0.95f, 0.95f, 0.97f),
            BackgroundSecondary = new Color(0.9f, 0.9f, 0.92f),
            CardBackground = Color.white,
            InputBackground = new Color(0.92f, 0.92f, 0.94f),
            TextPrimary = new Color(0.1f, 0.1f, 0.15f),
            TextSecondary = new Color(0.4f, 0.4f, 0.45f),
            BorderColor = new Color(0.8f, 0.8f, 0.82f),
            DividerColor = new Color(0.85f, 0.85f, 0.87f)
        };

        /// <summary>High contrast theme</summary>
        public static DeskillzTheme HighContrast => new DeskillzTheme
        {
            PrimaryColor = new Color(0f, 0.8f, 1f),
            BackgroundPrimary = Color.black,
            BackgroundSecondary = new Color(0.05f, 0.05f, 0.05f),
            CardBackground = new Color(0.08f, 0.08f, 0.08f),
            TextPrimary = Color.white,
            TextSecondary = new Color(0.8f, 0.8f, 0.8f),
            BorderColor = new Color(0.5f, 0.5f, 0.5f)
        };

        // =====================================================================
        // HELPER METHODS
        // =====================================================================

        /// <summary>
        /// Get tier color by tier level.
        /// </summary>
        public Color GetTierColor(int tierLevel)
        {
            return tierLevel switch
            {
                0 => TierBronze,
                1 => TierSilver,
                2 => TierGold,
                3 => TierPlatinum,
                4 => TierDiamond,
                5 => TierElite,
                _ => TierBronze
            };
        }

        /// <summary>
        /// Create a darkened version of a color.
        /// </summary>
        public Color Darken(Color color, float amount = 0.2f)
        {
            return new Color(
                color.r * (1 - amount),
                color.g * (1 - amount),
                color.b * (1 - amount),
                color.a
            );
        }

        /// <summary>
        /// Create a lightened version of a color.
        /// </summary>
        public Color Lighten(Color color, float amount = 0.2f)
        {
            return new Color(
                Mathf.Min(1f, color.r + amount),
                Mathf.Min(1f, color.g + amount),
                Mathf.Min(1f, color.b + amount),
                color.a
            );
        }

        /// <summary>
        /// Create a semi-transparent version of a color.
        /// </summary>
        public Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
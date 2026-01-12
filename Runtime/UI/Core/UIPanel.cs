// =============================================================================
// Deskillz SDK for Unity - UI Panel Base Class
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Deskillz.UI
{
    /// <summary>
    /// Base class for all Deskillz SDK UI panels.
    /// Provides common functionality for theming, visibility, and lifecycle.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Fired when panel is shown</summary>
        public event Action OnShown;

        /// <summary>Fired when panel is hidden</summary>
        public event Action OnHidden;

        // =====================================================================
        // STATE
        // =====================================================================

        /// <summary>Current theme</summary>
        protected DeskillzTheme _theme;

        /// <summary>Whether the panel is currently visible</summary>
        public bool IsVisible { get; protected set; }

        /// <summary>Whether the panel has been initialized</summary>
        public bool IsInitialized { get; protected set; }

        // =====================================================================
        // LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Initialize the panel with an optional theme.
        /// </summary>
        public virtual void Initialize(DeskillzTheme theme = null)
        {
            if (IsInitialized) return;

            _theme = theme ?? DeskillzTheme.Default;

            SetupLayout();
            ApplyTheme(_theme);

            IsInitialized = true;
        }

        /// <summary>
        /// Override to setup the panel's UI layout.
        /// </summary>
        protected abstract void SetupLayout();

        /// <summary>
        /// Apply a theme to the panel.
        /// </summary>
        public virtual void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
        }

        // =====================================================================
        // VISIBILITY
        // =====================================================================

        /// <summary>
        /// Show the panel.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
            OnShown?.Invoke();
        }

        /// <summary>
        /// Hide the panel.
        /// </summary>
        public virtual void Hide()
        {
            gameObject.SetActive(false);
            IsVisible = false;
            OnHidden?.Invoke();
        }

        /// <summary>
        /// Toggle panel visibility.
        /// </summary>
        public virtual void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        /// <summary>
        /// Create the main container RectTransform.
        /// </summary>
        protected RectTransform CreateContainer()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return rectTransform;
        }
    }

    /// <summary>
    /// Theme configuration for Deskillz UI components.
    /// </summary>
    [Serializable]
    public class DeskillzTheme
    {
        // =====================================================================
        // COLORS
        // =====================================================================

        /// <summary>Primary brand color</summary>
        public Color PrimaryColor = new Color(0.2f, 0.5f, 0.8f);

        /// <summary>Secondary brand color</summary>
        public Color SecondaryColor = new Color(0.6f, 0.3f, 0.8f);

        /// <summary>Accent/highlight color</summary>
        public Color AccentColor = new Color(0.4f, 0.8f, 1f);

        /// <summary>Success/positive color</summary>
        public Color SuccessColor = new Color(0.3f, 0.75f, 0.3f);

        /// <summary>Warning color</summary>
        public Color WarningColor = new Color(1f, 0.7f, 0f);

        /// <summary>Error/negative color</summary>
        public Color ErrorColor = new Color(0.9f, 0.3f, 0.3f);

        /// <summary>Primary text color</summary>
        public Color TextPrimary = Color.white;

        /// <summary>Secondary/muted text color</summary>
        public Color TextSecondary = new Color(0.6f, 0.6f, 0.65f);

        /// <summary>Panel/card background color</summary>
        public Color CardBackground = new Color(0.12f, 0.12f, 0.18f);

        /// <summary>Overlay background color</summary>
        public Color OverlayBackground = new Color(0, 0, 0, 0.8f);

        /// <summary>Input field background</summary>
        public Color InputBackground = new Color(0.15f, 0.15f, 0.2f);

        /// <summary>Button disabled color</summary>
        public Color DisabledColor = new Color(0.3f, 0.3f, 0.35f);

        // =====================================================================
        // TIER COLORS
        // =====================================================================

        public Color BronzeColor = new Color(0.8f, 0.5f, 0.2f);
        public Color SilverColor = new Color(0.75f, 0.75f, 0.75f);
        public Color GoldColor = new Color(1f, 0.84f, 0f);
        public Color PlatinumColor = new Color(0.9f, 0.9f, 1f);
        public Color DiamondColor = new Color(0.6f, 0.85f, 1f);
        public Color EliteColor = new Color(0.8f, 0.4f, 1f);

        // =====================================================================
        // PRESETS
        // =====================================================================

        /// <summary>Default dark theme</summary>
        public static DeskillzTheme Default => new DeskillzTheme();

        /// <summary>Light theme variant</summary>
        public static DeskillzTheme Light => new DeskillzTheme
        {
            TextPrimary = new Color(0.1f, 0.1f, 0.15f),
            TextSecondary = new Color(0.4f, 0.4f, 0.45f),
            CardBackground = new Color(0.95f, 0.95f, 0.97f),
            InputBackground = new Color(0.9f, 0.9f, 0.92f),
            OverlayBackground = new Color(0, 0, 0, 0.5f)
        };

        /// <summary>High contrast theme for accessibility</summary>
        public static DeskillzTheme HighContrast => new DeskillzTheme
        {
            PrimaryColor = new Color(0f, 0.6f, 1f),
            SuccessColor = new Color(0f, 1f, 0f),
            WarningColor = Color.yellow,
            ErrorColor = Color.red,
            TextPrimary = Color.white,
            TextSecondary = new Color(0.8f, 0.8f, 0.8f),
            CardBackground = Color.black
        };
    }
}
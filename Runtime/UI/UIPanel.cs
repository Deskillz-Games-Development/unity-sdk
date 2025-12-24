// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Deskillz.UI
{
    /// <summary>
    /// Base class for all Deskillz UI panels.
    /// Provides common functionality for show/hide, animation, and theming.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Panel Settings")]
        [SerializeField] protected bool _animateOnShow = true;
        [SerializeField] protected bool _animateOnHide = true;
        [SerializeField] protected AnimationType _showAnimation = AnimationType.Fade;
        [SerializeField] protected AnimationType _hideAnimation = AnimationType.Fade;

        // =============================================================================
        // STATE
        // =============================================================================

        protected RectTransform _rectTransform;
        protected CanvasGroup _canvasGroup;
        protected DeskillzTheme _theme;
        protected bool _isVisible;
        protected bool _isAnimating;
        protected Coroutine _animationCoroutine;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when panel becomes visible.</summary>
        public event Action OnShown;

        /// <summary>Fired when panel becomes hidden.</summary>
        public event Action OnHidden;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Whether panel is currently visible.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Whether panel is currently animating.</summary>
        public bool IsAnimating => _isAnimating;

        /// <summary>RectTransform component.</summary>
        public RectTransform RectTransform => _rectTransform;

        /// <summary>Current theme.</summary>
        public DeskillzTheme Theme => _theme;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected virtual void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize the panel with a theme.
        /// </summary>
        public virtual void Initialize(DeskillzTheme theme)
        {
            _theme = theme;
            SetupLayout();
            ApplyTheme(theme);
        }

        /// <summary>
        /// Setup the panel layout. Override to create UI elements.
        /// </summary>
        protected abstract void SetupLayout();

        /// <summary>
        /// Apply theme to panel elements.
        /// </summary>
        public abstract void ApplyTheme(DeskillzTheme theme);

        // =============================================================================
        // SHOW/HIDE
        // =============================================================================

        /// <summary>
        /// Show the panel.
        /// </summary>
        public virtual void Show()
        {
            if (_isVisible && !_isAnimating) return;

            gameObject.SetActive(true);

            if (_animateOnShow)
            {
                PlayAnimation(_showAnimation, true);
            }
            else
            {
                SetVisibleImmediate(true);
            }
        }

        /// <summary>
        /// Hide the panel.
        /// </summary>
        public virtual void Hide()
        {
            if (!_isVisible && !_isAnimating) return;

            if (_animateOnHide)
            {
                PlayAnimation(_hideAnimation, false);
            }
            else
            {
                SetVisibleImmediate(false);
            }
        }

        /// <summary>
        /// Toggle visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Set visibility immediately without animation.
        /// </summary>
        public void SetVisibleImmediate(bool visible)
        {
            StopAnimation();

            _isVisible = visible;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;

            if (!visible)
            {
                gameObject.SetActive(false);
            }

            if (visible)
                OnShown?.Invoke();
            else
                OnHidden?.Invoke();
        }

        // =============================================================================
        // ANIMATION
        // =============================================================================

        protected void PlayAnimation(AnimationType type, bool showing)
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimateCoroutine(type, showing));
        }

        protected void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
            _isAnimating = false;
        }

        protected virtual IEnumerator AnimateCoroutine(AnimationType type, bool showing)
        {
            _isAnimating = true;
            float duration = _theme?.AnimationNormal ?? 0.3f;
            float elapsed = 0f;

            // Initial state
            if (showing)
            {
                SetupAnimationStart(type);
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Ease in/out
                t = showing ? EaseOutCubic(t) : EaseInCubic(t);

                ApplyAnimationFrame(type, showing ? t : 1f - t);

                yield return null;
            }

            // Final state
            if (showing)
            {
                _isVisible = true;
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                OnShown?.Invoke();
            }
            else
            {
                _isVisible = false;
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
                OnHidden?.Invoke();
            }

            _isAnimating = false;
            _animationCoroutine = null;
        }

        protected virtual void SetupAnimationStart(AnimationType type)
        {
            switch (type)
            {
                case AnimationType.Fade:
                    _canvasGroup.alpha = 0f;
                    break;

                case AnimationType.Scale:
                    _canvasGroup.alpha = 0f;
                    transform.localScale = Vector3.one * 0.8f;
                    break;

                case AnimationType.SlideUp:
                    _canvasGroup.alpha = 0f;
                    _rectTransform.anchoredPosition += new Vector2(0, -50);
                    break;

                case AnimationType.SlideDown:
                    _canvasGroup.alpha = 0f;
                    _rectTransform.anchoredPosition += new Vector2(0, 50);
                    break;
            }
        }

        protected virtual void ApplyAnimationFrame(AnimationType type, float t)
        {
            switch (type)
            {
                case AnimationType.Fade:
                    _canvasGroup.alpha = t;
                    break;

                case AnimationType.Scale:
                    _canvasGroup.alpha = t;
                    transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                    break;

                case AnimationType.SlideUp:
                case AnimationType.SlideDown:
                    _canvasGroup.alpha = t;
                    // Position is handled in start
                    break;
            }
        }

        // =============================================================================
        // EASING
        // =============================================================================

        protected static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        protected static float EaseInCubic(float t) => t * t * t;
        protected static float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        // =============================================================================
        // UI HELPERS
        // =============================================================================

        /// <summary>
        /// Create a text element.
        /// </summary>
        protected Text CreateText(string name, Transform parent, string content = "")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();

            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = _theme?.TextPrimary ?? Color.white;
            text.fontSize = _theme?.BodyFontSize ?? 24;

            if (_theme?.PrimaryFont != null)
            {
                text.font = _theme.PrimaryFont;
            }
            else
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return text;
        }

        /// <summary>
        /// Create an image element.
        /// </summary>
        protected Image CreateImage(string name, Transform parent, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();

            image.color = color ?? Color.white;

            return image;
        }

        /// <summary>
        /// Create a button element.
        /// </summary>
        protected Button CreateButton(string name, Transform parent, string label, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, _theme?.PrimaryButton.Height ?? 48);

            var image = go.AddComponent<Image>();
            image.color = _theme?.PrimaryButton.NormalColor ?? Color.white;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // Set button colors
            var colors = button.colors;
            colors.normalColor = _theme?.PrimaryButton.NormalColor ?? Color.white;
            colors.highlightedColor = _theme?.PrimaryButton.HoverColor ?? Color.white;
            colors.pressedColor = _theme?.PrimaryButton.PressedColor ?? Color.gray;
            colors.disabledColor = _theme?.PrimaryButton.DisabledColor ?? Color.gray;
            button.colors = colors;

            // Add label
            var labelText = CreateText("Label", go.transform, label);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.sizeDelta = Vector2.zero;
            labelText.color = _theme?.PrimaryButton.TextColor ?? Color.white;

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            return button;
        }

        /// <summary>
        /// Set anchor to fill parent.
        /// </summary>
        protected void SetAnchorFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }

    // =============================================================================
    // ANIMATION TYPE
    // =============================================================================

    /// <summary>
    /// Types of panel animations.
    /// </summary>
    public enum AnimationType
    {
        None,
        Fade,
        Scale,
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight
    }
}

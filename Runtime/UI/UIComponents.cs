// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Deskillz.UI
{
    // =============================================================================
    // COUNTDOWN UI
    // =============================================================================

    /// <summary>
    /// Displays match countdown before game starts.
    /// </summary>
    public class CountdownUI : UIPanel
    {
        // Configuration
        [Header("Countdown Settings")]
        [SerializeField] private bool _animateNumbers = true;
        [SerializeField] private float _numberDisplayTime = 0.8f;

        // UI References
        private Text _countdownText;
        private Text _goText;
        private Coroutine _animationCoroutine;

        protected override void SetupLayout()
        {
            var rect = GetComponent<RectTransform>();
            SetAnchorFill(rect);

            // Background (semi-transparent)
            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);

            // Countdown number
            _countdownText = CreateText("CountdownNumber", transform, "3");
            _countdownText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _countdownText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _countdownText.rectTransform.sizeDelta = new Vector2(200, 200);
            _countdownText.alignment = TextAnchor.MiddleCenter;
            _countdownText.fontSize = 120;
            _countdownText.fontStyle = FontStyle.Bold;
            _countdownText.color = _theme?.PrimaryColor ?? Color.white;

            // GO text
            _goText = CreateText("GoText", transform, "GO!");
            _goText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _goText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _goText.rectTransform.sizeDelta = new Vector2(300, 150);
            _goText.alignment = TextAnchor.MiddleCenter;
            _goText.fontSize = 100;
            _goText.fontStyle = FontStyle.Bold;
            _goText.color = _theme?.SuccessColor ?? Color.green;
            _goText.gameObject.SetActive(false);
        }

        public void ShowNumber(int number)
        {
            if (number <= 0)
            {
                ShowGo();
                return;
            }

            gameObject.SetActive(true);
            _countdownText.gameObject.SetActive(true);
            _goText.gameObject.SetActive(false);

            _countdownText.text = number.ToString();

            if (_animateNumbers)
            {
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }
                _animationCoroutine = StartCoroutine(AnimateNumber());
            }
        }

        private void ShowGo()
        {
            _countdownText.gameObject.SetActive(false);
            _goText.gameObject.SetActive(true);

            if (_animateNumbers)
            {
                StartCoroutine(AnimateGo());
            }
            else
            {
                StartCoroutine(HideAfterDelay(1f));
            }
        }

        private IEnumerator AnimateNumber()
        {
            _countdownText.transform.localScale = Vector3.one * 1.5f;
            _countdownText.color = new Color(_countdownText.color.r, _countdownText.color.g, _countdownText.color.b, 1);

            float elapsed = 0;
            while (elapsed < _numberDisplayTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _numberDisplayTime;

                // Scale down
                _countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one * 0.8f, t);

                // Fade out slightly at end
                if (t > 0.7f)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    _countdownText.color = new Color(_countdownText.color.r, _countdownText.color.g, _countdownText.color.b, 1 - fadeT * 0.5f);
                }

                yield return null;
            }

            // Play sound if available
            if (_theme?.CountdownSound != null)
            {
                AudioSource.PlayClipAtPoint(_theme.CountdownSound, Camera.main?.transform.position ?? Vector3.zero);
            }
        }

        private IEnumerator AnimateGo()
        {
            _goText.transform.localScale = Vector3.zero;

            // Scale up
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.3f;
                t = 1 - Mathf.Pow(1 - t, 3); // Ease out
                _goText.transform.localScale = Vector3.one * t;
                yield return null;
            }
            _goText.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(0.5f);

            // Fade out
            elapsed = 0;
            var color = _goText.color;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                _goText.color = new Color(color.r, color.g, color.b, 1 - elapsed / 0.3f);
                yield return null;
            }

            Hide();
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_countdownText != null) _countdownText.color = theme.PrimaryColor;
            if (_goText != null) _goText.color = theme.SuccessColor;
        }
    }

    // =============================================================================
    // NOTIFICATION UI
    // =============================================================================

    /// <summary>
    /// Displays toast notifications.
    /// </summary>
    public class NotificationUI : MonoBehaviour
    {
        // Configuration
        [Header("Notification Settings")]
        [SerializeField] private int _maxNotifications = 3;
        [SerializeField] private float _defaultDuration = 3f;
        [SerializeField] private NotificationPosition _position = NotificationPosition.TopCenter;

        // State
        private RectTransform _container;
        private DeskillzTheme _theme;
        private readonly Queue<NotificationData> _pendingQueue = new Queue<NotificationData>();
        private readonly List<NotificationItem> _activeNotifications = new List<NotificationItem>();

        public void Initialize(DeskillzTheme theme)
        {
            _theme = theme;

            // Container for notifications
            _container = gameObject.AddComponent<RectTransform>();
            SetupPosition();

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
        }

        private void SetupPosition()
        {
            switch (_position)
            {
                case NotificationPosition.TopCenter:
                    _container.anchorMin = new Vector2(0.5f, 1);
                    _container.anchorMax = new Vector2(0.5f, 1);
                    _container.pivot = new Vector2(0.5f, 1);
                    _container.anchoredPosition = new Vector2(0, -20);
                    break;
                case NotificationPosition.TopRight:
                    _container.anchorMin = new Vector2(1, 1);
                    _container.anchorMax = new Vector2(1, 1);
                    _container.pivot = new Vector2(1, 1);
                    _container.anchoredPosition = new Vector2(-20, -20);
                    break;
                case NotificationPosition.BottomCenter:
                    _container.anchorMin = new Vector2(0.5f, 0);
                    _container.anchorMax = new Vector2(0.5f, 0);
                    _container.pivot = new Vector2(0.5f, 0);
                    _container.anchoredPosition = new Vector2(0, 100);
                    break;
            }
            _container.sizeDelta = new Vector2(400, 0);
        }

        public void Show(string message, NotificationType type = NotificationType.Info, float duration = -1)
        {
            if (duration < 0) duration = _defaultDuration;

            var data = new NotificationData
            {
                Message = message,
                Type = type,
                Duration = duration
            };

            if (_activeNotifications.Count >= _maxNotifications)
            {
                _pendingQueue.Enqueue(data);
            }
            else
            {
                CreateNotification(data);
            }
        }

        private void CreateNotification(NotificationData data)
        {
            var go = new GameObject("Notification");
            go.transform.SetParent(_container, false);

            var item = go.AddComponent<NotificationItem>();
            item.Initialize(_theme, data, OnNotificationDismissed);
            _activeNotifications.Add(item);
        }

        private void OnNotificationDismissed(NotificationItem item)
        {
            _activeNotifications.Remove(item);
            Destroy(item.gameObject);

            // Show next queued notification
            if (_pendingQueue.Count > 0)
            {
                CreateNotification(_pendingQueue.Dequeue());
            }
        }

        public void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
        }
    }

    public enum NotificationPosition
    {
        TopCenter,
        TopRight,
        BottomCenter
    }

    internal struct NotificationData
    {
        public string Message;
        public NotificationType Type;
        public float Duration;
    }

    internal class NotificationItem : MonoBehaviour
    {
        private Image _background;
        private Text _messageText;
        private Button _closeButton;
        private Action<NotificationItem> _onDismiss;
        private float _duration;

        public void Initialize(DeskillzTheme theme, NotificationData data, Action<NotificationItem> onDismiss)
        {
            _onDismiss = onDismiss;
            _duration = data.Duration;

            var rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);

            _background = gameObject.AddComponent<Image>();
            _background.color = theme?.GetNotificationColor(data.Type) ?? Color.white;

            // Message
            var textGO = new GameObject("Message");
            textGO.transform.SetParent(transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 5);
            textRect.offsetMax = new Vector2(-40, -5);

            _messageText = textGO.AddComponent<Text>();
            _messageText.text = data.Message;
            _messageText.alignment = TextAnchor.MiddleLeft;
            _messageText.fontSize = 18;
            _messageText.color = Color.white;
            _messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Close button
            var closeGO = new GameObject("Close");
            closeGO.transform.SetParent(transform, false);

            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-10, 0);
            closeRect.sizeDelta = new Vector2(30, 30);

            var closeImage = closeGO.AddComponent<Image>();
            closeImage.color = new Color(1, 1, 1, 0.3f);

            _closeButton = closeGO.AddComponent<Button>();
            _closeButton.targetGraphic = closeImage;
            _closeButton.onClick.AddListener(Dismiss);

            var closeText = new GameObject("X").AddComponent<Text>();
            closeText.transform.SetParent(closeRect, false);
            closeText.rectTransform.anchorMin = Vector2.zero;
            closeText.rectTransform.anchorMax = Vector2.one;
            closeText.rectTransform.sizeDelta = Vector2.zero;
            closeText.text = "✕";
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.fontSize = 16;
            closeText.color = Color.white;
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Auto-dismiss
            StartCoroutine(AutoDismiss());

            // Animate in
            StartCoroutine(AnimateIn());
        }

        private IEnumerator AnimateIn()
        {
            var canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            transform.localScale = new Vector3(1, 0, 1);

            float elapsed = 0;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.2f;
                canvasGroup.alpha = t;
                transform.localScale = new Vector3(1, t, 1);
                yield return null;
            }

            canvasGroup.alpha = 1;
            transform.localScale = Vector3.one;
        }

        private IEnumerator AutoDismiss()
        {
            yield return new WaitForSeconds(_duration);
            Dismiss();
        }

        public void Dismiss()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateOut());
        }

        private IEnumerator AnimateOut()
        {
            var canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.15f;
                canvasGroup.alpha = 1 - t;
                yield return null;
            }

            _onDismiss?.Invoke(this);
        }
    }

    // =============================================================================
    // PAUSE MENU UI
    // =============================================================================

    /// <summary>
    /// Pause menu with resume, settings, and quit options.
    /// </summary>
    public class PauseMenuUI : UIPanel
    {
        // UI References
        private Text _titleText;
        private Button _resumeButton;
        private Button _settingsButton;
        private Button _forfeitButton;

        // Events
        public event Action OnResume;
        public event Action OnSettings;
        public event Action OnForfeit;

        protected override void SetupLayout()
        {
            var rect = GetComponent<RectTransform>();
            SetAnchorFill(rect);

            // Overlay
            var overlayImage = gameObject.AddComponent<Image>();
            overlayImage.color = _theme?.OverlayColor ?? new Color(0, 0, 0, 0.8f);

            // Panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);

            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 350);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = _theme?.PanelColor ?? new Color(0.15f, 0.15f, 0.2f);

            // Title
            _titleText = CreateText("Title", panelRect, "PAUSED");
            _titleText.rectTransform.anchorMin = new Vector2(0, 1);
            _titleText.rectTransform.anchorMax = new Vector2(1, 1);
            _titleText.rectTransform.pivot = new Vector2(0.5f, 1);
            _titleText.rectTransform.anchoredPosition = new Vector2(0, -20);
            _titleText.rectTransform.sizeDelta = new Vector2(0, 60);
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.fontSize = _theme?.HeadingFontSize ?? 32;
            _titleText.fontStyle = FontStyle.Bold;

            // Button container
            var buttonContainer = new GameObject("Buttons");
            buttonContainer.transform.SetParent(panelRect, false);

            var buttonRect = buttonContainer.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.offsetMin = new Vector2(40, 40);
            buttonRect.offsetMax = new Vector2(-40, -90);

            var layout = buttonContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Resume button
            _resumeButton = CreateButton("Resume", buttonRect.transform, "RESUME", OnResumeClicked);
            _resumeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);

            // Settings button
            _settingsButton = CreateButton("Settings", buttonRect.transform, "SETTINGS", OnSettingsClicked);
            _settingsButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
            _settingsButton.GetComponent<Image>().color = _theme?.SecondaryButton.NormalColor ?? Color.gray;

            // Forfeit button
            _forfeitButton = CreateButton("Forfeit", buttonRect.transform, "FORFEIT MATCH", OnForfeitClicked);
            _forfeitButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
            _forfeitButton.GetComponent<Image>().color = _theme?.DangerButton.NormalColor ?? Color.red;
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_titleText != null) _titleText.color = theme.TextPrimary;

            if (_resumeButton != null)
            {
                _resumeButton.GetComponent<Image>().color = theme.PrimaryButton.NormalColor;
            }

            if (_settingsButton != null)
            {
                _settingsButton.GetComponent<Image>().color = theme.SecondaryButton.NormalColor;
            }

            if (_forfeitButton != null)
            {
                _forfeitButton.GetComponent<Image>().color = theme.DangerButton.NormalColor;
            }
        }

        private void OnResumeClicked()
        {
            OnResume?.Invoke();
            Deskillz.Match.MatchController.Instance?.ResumeMatch();
            Hide();
        }

        private void OnSettingsClicked()
        {
            OnSettings?.Invoke();
        }

        private void OnForfeitClicked()
        {
            OnForfeit?.Invoke();
            Deskillz.Match.MatchController.Instance?.ForfeitMatch();
            Hide();
        }
    }
}

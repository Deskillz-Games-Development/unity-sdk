// =============================================================================
// Deskillz SDK for Unity - Room Player Card
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Rooms;

namespace Deskillz.UI.Rooms
{
    /// <summary>
    /// Individual player card component for the room lobby.
    /// Displays player info, ready status, and host controls.
    /// </summary>
    public class RoomPlayerCard : MonoBehaviour
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when kick button is clicked (host only)</summary>
        public event Action OnKickClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _rectTransform;
        private Image _background;
        private Image _avatarImage;
        private TextMeshProUGUI _usernameText;
        private TextMeshProUGUI _statusText;
        private Image _readyIndicator;
        private Image _hostBadge;
        private Button _kickButton;

        // =====================================================================
        // STATE
        // =====================================================================

        private RoomPlayer _player;
        private DeskillzTheme _theme;
        private bool _canKick;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the player card with a theme.
        /// </summary>
        public void Initialize(DeskillzTheme theme)
        {
            _theme = theme;
            _rectTransform = gameObject.AddComponent<RectTransform>();

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 70;
            layoutElement.minHeight = 70;

            SetupLayout();
            ApplyTheme(theme);
        }

        private void SetupLayout()
        {
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.15f, 0.15f, 0.18f);

            // Card layout
            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 12;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Ready indicator (left side)
            var readyContainer = new GameObject("ReadyIndicator").AddComponent<RectTransform>();
            readyContainer.SetParent(_rectTransform, false);
            _readyIndicator = readyContainer.gameObject.AddComponent<Image>();
            _readyIndicator.color = new Color(0.4f, 0.4f, 0.45f); // Gray = not ready
            var readyLayout = readyContainer.gameObject.AddComponent<LayoutElement>();
            readyLayout.preferredWidth = 8;
            readyLayout.preferredHeight = 50;

            // Avatar container
            var avatarContainer = new GameObject("AvatarContainer").AddComponent<RectTransform>();
            avatarContainer.SetParent(_rectTransform, false);
            
            var avatarMask = avatarContainer.gameObject.AddComponent<Mask>();
            avatarMask.showMaskGraphic = true;
            var avatarBg = avatarContainer.gameObject.AddComponent<Image>();
            avatarBg.color = new Color(0.2f, 0.2f, 0.25f);

            var avatarLayout = avatarContainer.gameObject.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 50;
            avatarLayout.preferredHeight = 50;

            // Avatar image
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(avatarContainer, false);
            var avatarRect = avatarGo.AddComponent<RectTransform>();
            avatarRect.anchorMin = Vector2.zero;
            avatarRect.anchorMax = Vector2.one;
            avatarRect.sizeDelta = Vector2.zero;
            _avatarImage = avatarGo.AddComponent<Image>();
            _avatarImage.color = new Color(0.3f, 0.3f, 0.35f);

            // Info section
            var infoSection = new GameObject("Info").AddComponent<RectTransform>();
            infoSection.SetParent(_rectTransform, false);
            
            var infoLayout = infoSection.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 4;
            infoLayout.childForceExpandHeight = false;
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            var infoLayoutElement = infoSection.gameObject.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1;

            // Username row (with host badge)
            var usernameRow = new GameObject("UsernameRow").AddComponent<RectTransform>();
            usernameRow.SetParent(infoSection, false);
            
            var usernameRowLayout = usernameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            usernameRowLayout.spacing = 8;
            usernameRowLayout.childForceExpandWidth = false;
            usernameRowLayout.childAlignment = TextAnchor.MiddleLeft;
            
            var usernameRowLayoutElement = usernameRow.gameObject.AddComponent<LayoutElement>();
            usernameRowLayoutElement.preferredHeight = 24;

            // Username
            _usernameText = UIComponents.CreateText(usernameRow, "Username", "Player Name", 16, TextAlignmentOptions.Left);
            _usernameText.fontStyle = FontStyles.Bold;

            // Host badge
            var badgeGo = new GameObject("HostBadge");
            badgeGo.transform.SetParent(usernameRow.transform, false);
            var badgeRect = badgeGo.AddComponent<RectTransform>();
            _hostBadge = badgeGo.AddComponent<Image>();
            _hostBadge.color = new Color(1f, 0.8f, 0.2f);
            var badgeLayoutElement = badgeGo.AddComponent<LayoutElement>();
            badgeLayoutElement.preferredWidth = 50;
            badgeLayoutElement.preferredHeight = 18;

            var badgeText = UIComponents.CreateText(badgeRect, "BadgeText", "HOST", 10, TextAlignmentOptions.Center);
            badgeText.rectTransform.anchorMin = Vector2.zero;
            badgeText.rectTransform.anchorMax = Vector2.one;
            badgeText.rectTransform.sizeDelta = Vector2.zero;
            badgeText.color = Color.black;
            badgeText.fontStyle = FontStyles.Bold;

            _hostBadge.gameObject.SetActive(false);

            // Status text
            _statusText = UIComponents.CreateText(infoSection, "Status", "Not Ready", 12, TextAlignmentOptions.Left);
            _statusText.color = new Color(0.6f, 0.6f, 0.6f);
            var statusLayout = _statusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 18;

            // Kick button (host only)
            _kickButton = UIComponents.CreateButton(_rectTransform, "KickButton", "✕", new Vector2(36, 36));
            _kickButton.onClick.AddListener(() => OnKickClicked?.Invoke());
            UIComponents.SetButtonColor(_kickButton, new Color(0.7f, 0.3f, 0.3f));
            var kickLayout = _kickButton.gameObject.AddComponent<LayoutElement>();
            kickLayout.preferredWidth = 36;
            kickLayout.preferredHeight = 36;
            _kickButton.gameObject.SetActive(false);
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the player data to display.
        /// </summary>
        /// <param name="player">Player data</param>
        /// <param name="canKick">Whether kick button should be shown</param>
        public void SetPlayer(RoomPlayer player, bool canKick = false)
        {
            _player = player;
            _canKick = canKick;
            UpdateDisplay();
        }

        /// <summary>
        /// Update ready status.
        /// </summary>
        public void SetReady(bool isReady)
        {
            if (_player != null)
            {
                _player.IsReady = isReady;
                UpdateReadyStatus();
            }
        }

        /// <summary>
        /// Apply theme to this card.
        /// </summary>
        public void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (theme == null) return;

            _usernameText.color = theme.TextPrimary;
        }

        // =====================================================================
        // DISPLAY
        // =====================================================================

        private void UpdateDisplay()
        {
            if (_player == null) return;

            // Username
            _usernameText.text = _player.Username;

            // Host badge
            _hostBadge.gameObject.SetActive(_player.IsAdmin);

            // Highlight current user
            if (_player.IsCurrentUser)
            {
                _usernameText.text = $"{_player.Username} (You)";
                _background.color = new Color(0.18f, 0.18f, 0.22f);
            }
            else
            {
                _background.color = new Color(0.15f, 0.15f, 0.18f);
            }

            // Kick button (only for non-hosts, non-self)
            _kickButton.gameObject.SetActive(_canKick);

            // Ready status
            UpdateReadyStatus();

            // Load avatar
            LoadAvatar();
        }

        private void UpdateReadyStatus()
        {
            if (_player == null) return;

            if (_player.IsReady)
            {
                _statusText.text = "Ready";
                _statusText.color = new Color(0.3f, 0.9f, 0.4f);
                _readyIndicator.color = new Color(0.3f, 0.9f, 0.4f);
            }
            else
            {
                _statusText.text = "Not Ready";
                _statusText.color = new Color(0.6f, 0.6f, 0.6f);
                _readyIndicator.color = new Color(0.4f, 0.4f, 0.45f);
            }
        }

        private void LoadAvatar()
        {
            if (_player == null || string.IsNullOrEmpty(_player.AvatarUrl))
            {
                // Use default avatar
                _avatarImage.color = GetAvatarColor(_player?.Username ?? "");
                return;
            }

            // Load avatar from URL (simplified - would use proper image loading in production)
            StartCoroutine(LoadAvatarFromUrl(_player.AvatarUrl));
        }

        private System.Collections.IEnumerator LoadAvatarFromUrl(string url)
        {
            using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    _avatarImage.sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    _avatarImage.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Generate a consistent color based on username.
        /// </summary>
        private Color GetAvatarColor(string username)
        {
            if (string.IsNullOrEmpty(username))
                return new Color(0.3f, 0.3f, 0.35f);

            // Generate hue from username hash
            int hash = username.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.5f, 0.6f);
        }
    }

    // =========================================================================
    // UI COMPONENTS HELPER CLASS
    // =========================================================================

    /// <summary>
    /// Helper class for creating common UI components.
    /// </summary>
    public static class UIComponents
    {
        /// <summary>
        /// Create a TextMeshProUGUI component.
        /// </summary>
        public static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        /// <summary>
        /// Create a Button component.
        /// </summary>
        public static Button CreateButton(RectTransform parent, string name, string text, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // Button text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        /// <summary>
        /// Set button background color.
        /// </summary>
        public static void SetButtonColor(Button button, Color color)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.1f;
            colors.pressedColor = color * 0.9f;
            colors.selectedColor = color;
            colors.disabledColor = color * 0.5f;
            button.colors = colors;
        }

        /// <summary>
        /// Create an InputField component.
        /// </summary>
        public static TMP_InputField CreateInputField(RectTransform parent, string name, string placeholder, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f);

            // Text Area
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(go.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);
            textArea.AddComponent<RectMask2D>();

            // Placeholder
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(textArea.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            var placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 14;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;

            // Input Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textArea.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var inputText = textGo.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 14;
            inputText.color = Color.white;
            inputText.alignment = TextAlignmentOptions.Left;

            // InputField
            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;

            return inputField;
        }

        /// <summary>
        /// Create a Dropdown component.
        /// </summary>
        public static TMP_Dropdown CreateDropdown(RectTransform parent, string name, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-30, 0);

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;

            // Arrow
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRect = arrowGo.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            var arrowText = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 12;
            arrowText.color = Color.white;
            arrowText.alignment = TextAlignmentOptions.Center;

            // Template (simplified)
            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(go.transform, false);
            var templateRect = templateGo.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);

            var templateImage = templateGo.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.2f);

            var templateScroll = templateGo.AddComponent<ScrollRect>();
            templateScroll.horizontal = false;

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportGo.AddComponent<Image>().color = Color.clear;
            viewportGo.AddComponent<Mask>();
            templateScroll.viewport = viewportRect;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            templateScroll.content = contentRect;

            // Item
            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRect = itemGo.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 30);

            var itemToggle = itemGo.AddComponent<Toggle>();
            itemToggle.isOn = false;

            // Item label
            var itemLabelGo = new GameObject("Item Label");
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var itemLabelRect = itemLabelGo.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);

            var itemLabelText = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 14;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAlignmentOptions.Left;

            templateGo.SetActive(false);

            // Dropdown
            var dropdown = go.AddComponent<TMP_Dropdown>();
            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;

            return dropdown;
        }

        /// <summary>
        /// Create a Slider component.
        /// </summary>
        public static Slider CreateSlider(RectTransform parent, string name, float min, float max, float value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 20);

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill Area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(10, 0);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.5f, 0.9f);

            // Handle Area
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;

            // Slider component
            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            return slider;
        }

        /// <summary>
        /// Create a Toggle component.
        /// </summary>
        public static Toggle CreateToggle(RectTransform parent, string name, bool isOn)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(30, 30);

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Checkmark
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.sizeDelta = Vector2.zero;
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.9f, 0.4f);

            // Toggle component
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = isOn;

            return toggle;
        }
    }
}
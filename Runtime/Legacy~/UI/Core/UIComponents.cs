// =============================================================================
// Deskillz SDK for Unity - UI Components Factory
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Deskillz.UI
{
    /// <summary>
    /// Factory class for creating common UI components.
    /// Provides consistent styling and setup across all Deskillz UI panels.
    /// </summary>
    public static class UIComponents
    {
        // =====================================================================
        // TEXT
        // =====================================================================

        /// <summary>
        /// Create a TextMeshPro text component.
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string text, int fontSize = 14)
        {
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(parent, false);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        // =====================================================================
        // BUTTONS
        // =====================================================================

        /// <summary>
        /// Create a standard button.
        /// </summary>
        public static Button CreateButton(Transform parent, string text, Color bgColor)
        {
            var buttonGO = new GameObject("Button");
            buttonGO.transform.SetParent(parent, false);

            var bg = buttonGO.AddComponent<Image>();
            bg.color = bgColor;

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = bg;

            var buttonText = CreateText(buttonGO.transform, text, 14);
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontStyle = FontStyles.Bold;

            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>
        /// Create an icon button.
        /// </summary>
        public static Button CreateIconButton(Transform parent, string iconName, System.Action onClick)
        {
            var buttonGO = new GameObject($"IconButton_{iconName}");
            buttonGO.transform.SetParent(parent, false);

            var rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(32, 32);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.35f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            // Icon placeholder text
            var iconText = CreateText(buttonGO.transform, GetIconChar(iconName), 16);
            iconText.alignment = TextAlignmentOptions.Center;

            var textRect = iconText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;

            return button;
        }

        private static string GetIconChar(string iconName)
        {
            return iconName switch
            {
                "back" => "<",
                "close" => "X",
                "settings" => "*",
                "refresh" => "R",
                "menu" => "=",
                "search" => "?",
                "add" => "+",
                "remove" => "-",
                "eye" => "o",
                _ => "?"
            };
        }

        // =====================================================================
        // INPUT FIELDS
        // =====================================================================

        /// <summary>
        /// Create a text input field.
        /// </summary>
        public static TMP_InputField CreateInputField(Transform parent, string placeholder = "")
        {
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(parent, false);

            var rect = inputGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = inputGO.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);

            // Text area
            var textAreaGO = new GameObject("TextArea");
            textAreaGO.transform.SetParent(inputGO.transform, false);

            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);

            // Text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14;
            textComponent.color = Color.white;

            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);

            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.fontStyle = FontStyles.Italic;

            // Input field component
            var inputField = inputGO.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;
            inputField.fontAsset = textComponent.font;

            return inputField;
        }

        // =====================================================================
        // SLIDERS
        // =====================================================================

        /// <summary>
        /// Create a slider.
        /// </summary>
        public static Slider CreateSlider(Transform parent)
        {
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(parent, false);

            var sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            var slider = sliderGO.AddComponent<Slider>();

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);

            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill area
            var fillAreaGO = new GameObject("FillArea");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);

            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);

            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f);

            // Handle area
            var handleAreaGO = new GameObject("HandleArea");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);

            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            // Handle
            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);

            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = Color.white;

            // Setup slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return slider;
        }

        /// <summary>
        /// Create a progress bar (slider without handle).
        /// </summary>
        public static Slider CreateProgressBar(Transform parent)
        {
            var slider = CreateSlider(parent);
            slider.interactable = false;

            // Hide handle
            var handle = slider.handleRect;
            if (handle != null)
            {
                handle.gameObject.SetActive(false);
            }

            return slider;
        }

        // =====================================================================
        // TOGGLES
        // =====================================================================

        /// <summary>
        /// Create a toggle.
        /// </summary>
        public static Toggle CreateToggle(Transform parent)
        {
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(parent, false);

            var toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(40, 24);

            var toggle = toggleGO.AddComponent<Toggle>();

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(toggleGO.transform, false);

            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.35f);

            // Checkmark
            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(bgGO.transform, false);

            var checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0);
            checkmarkRect.anchorMax = new Vector2(0.5f, 1);
            checkmarkRect.offsetMin = new Vector2(2, 2);
            checkmarkRect.offsetMax = new Vector2(-2, -2);

            var checkmarkImage = checkmarkGO.AddComponent<Image>();
            checkmarkImage.color = new Color(0.2f, 0.6f, 1f);

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkmarkImage;

            return toggle;
        }

        // =====================================================================
        // DROPDOWNS
        // =====================================================================

        /// <summary>
        /// Create a dropdown.
        /// </summary>
        public static TMP_Dropdown CreateDropdown(Transform parent, string[] options)
        {
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(parent, false);

            var dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.anchorMin = Vector2.zero;
            dropdownRect.anchorMax = Vector2.one;
            dropdownRect.offsetMin = Vector2.zero;
            dropdownRect.offsetMax = Vector2.zero;

            var bg = dropdownGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);

            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-30, 0);

            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;

            // Arrow
            var arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(dropdownGO.transform, false);

            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-5, 0);

            var arrowText = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowText.text = "v";
            arrowText.fontSize = 12;
            arrowText.alignment = TextAlignmentOptions.Center;

            // Template (simplified)
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);

            var templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);

            var templateBg = templateGO.AddComponent<Image>();
            templateBg.color = new Color(0.15f, 0.15f, 0.2f);

            var templateScroll = templateGO.AddComponent<ScrollRect>();

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);

            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewportGO.AddComponent<Image>();

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);

            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);

            // Item template
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO.transform, false);

            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 30);

            var itemToggle = itemGO.AddComponent<Toggle>();

            var itemBg = new GameObject("ItemBackground");
            itemBg.transform.SetParent(itemGO.transform, false);

            var itemBgRect = itemBg.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;

            var itemBgImage = itemBg.AddComponent<Image>();
            itemBgImage.color = new Color(0.2f, 0.2f, 0.25f);

            var itemLabelGO = new GameObject("ItemLabel");
            itemLabelGO.transform.SetParent(itemGO.transform, false);

            var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);

            var itemLabelText = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 14;
            itemLabelText.color = Color.white;

            // Setup dropdown
            dropdown.targetGraphic = bg;
            dropdown.captionText = labelText;
            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            templateScroll.content = contentRect;
            templateScroll.viewport = viewportRect;

            // Add options
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));

            return dropdown;
        }

        // =====================================================================
        // CONTAINERS
        // =====================================================================

        /// <summary>
        /// Create a section header.
        /// </summary>
        public static GameObject CreateSectionHeader(Transform parent, string title)
        {
            var headerGO = new GameObject("SectionHeader");
            headerGO.transform.SetParent(parent, false);

            var layout = headerGO.AddComponent<LayoutElement>();
            layout.preferredHeight = 24;

            var text = CreateText(headerGO.transform, title.ToUpper(), 11);
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.6f, 0.6f, 0.65f);

            return headerGO;
        }

        /// <summary>
        /// Create a header bar.
        /// </summary>
        public static GameObject CreateHeader(Transform parent, string title)
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(parent, false);

            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 60);

            var bg = headerGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f);

            var layout = headerGO.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;

            var text = CreateText(headerGO.transform, title, 20);
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;

            return headerGO;
        }

        /// <summary>
        /// Create a loading indicator.
        /// </summary>
        public static GameObject CreateLoadingIndicator(Transform parent)
        {
            var loadingGO = new GameObject("LoadingIndicator");
            loadingGO.transform.SetParent(parent, false);

            var rect = loadingGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(60, 60);

            // Spinner background
            var spinnerBg = loadingGO.AddComponent<Image>();
            spinnerBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // Spinner (animated via script)
            var spinnerGO = new GameObject("Spinner");
            spinnerGO.transform.SetParent(loadingGO.transform, false);

            var spinnerRect = spinnerGO.AddComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRect.sizeDelta = new Vector2(32, 32);

            var spinnerImage = spinnerGO.AddComponent<Image>();
            spinnerImage.color = new Color(0.2f, 0.6f, 1f);

            // Add rotation animation
            var spinner = spinnerGO.AddComponent<SpinnerAnimation>();

            return loadingGO;
        }
    }

    /// <summary>
    /// Simple spinner animation component.
    /// </summary>
    public class SpinnerAnimation : MonoBehaviour
    {
        public float rotationSpeed = 360f;

        private void Update()
        {
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}
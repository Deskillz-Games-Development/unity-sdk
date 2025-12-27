// =============================================================================
// Deskillz SDK for Unity - Create Room UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Rooms;

namespace Deskillz.UI.Rooms
{
    /// <summary>
    /// UI panel for creating a new private room.
    /// Allows configuration of room name, entry fee, player count, etc.
    /// </summary>
    public class CreateRoomUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when room is successfully created</summary>
        public event Action<PrivateRoom> OnRoomCreated;

        /// <summary>Called when back button is clicked</summary>
        public event Action OnBackClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _container;
        private TextMeshProUGUI _titleText;
        private Button _backButton;

        // Form fields
        private TMP_InputField _nameInput;
        private TMP_InputField _descriptionInput;
        private TMP_InputField _entryFeeInput;
        private TMP_Dropdown _currencyDropdown;
        private Slider _minPlayersSlider;
        private TextMeshProUGUI _minPlayersText;
        private Slider _maxPlayersSlider;
        private TextMeshProUGUI _maxPlayersText;
        private TMP_Dropdown _visibilityDropdown;
        private TMP_Dropdown _modeDropdown;
        private Toggle _inviteRequiredToggle;

        // Actions
        private Button _createButton;
        private Button _cancelButton;
        private TextMeshProUGUI _errorText;
        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isCreating;
        private readonly string[] _currencies = { "USDT", "USDC", "BTC", "ETH", "BNB", "SOL", "XRP" };

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Setup container
            _container = CreateContainer();

            // Header
            CreateHeader();

            // Form
            CreateForm();

            // Action buttons
            CreateActionButtons();

            // Loading indicator
            CreateLoadingIndicator();
        }

        private RectTransform CreateContainer()
        {
            var container = new GameObject("Container").AddComponent<RectTransform>();
            container.SetParent(_rectTransform, false);
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(500, 650);

            // Background
            var bg = container.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);

            // Add rounded corners effect (simple border)
            var outline = container.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.3f, 0.35f);
            outline.effectDistance = new Vector2(2, 2);

            return container;
        }

        private void CreateHeader()
        {
            var header = new GameObject("Header").AddComponent<RectTransform>();
            header.SetParent(_container, false);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 60);
            header.anchoredPosition = Vector2.zero;

            // Back button
            _backButton = UIComponents.CreateButton(header, "Back", "←", new Vector2(50, 40));
            var backRect = _backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.anchoredPosition = new Vector2(35, 0);
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            // Title
            _titleText = UIComponents.CreateText(header, "Title", "Create Room", 22, TextAlignmentOptions.Center);
            _titleText.rectTransform.anchorMin = new Vector2(0.15f, 0);
            _titleText.rectTransform.anchorMax = new Vector2(0.85f, 1);
            _titleText.rectTransform.sizeDelta = Vector2.zero;
            _titleText.fontStyle = FontStyles.Bold;
        }

        private void CreateForm()
        {
            var form = new GameObject("Form").AddComponent<RectTransform>();
            form.SetParent(_container, false);
            form.anchorMin = new Vector2(0, 0);
            form.anchorMax = new Vector2(1, 1);
            form.offsetMin = new Vector2(20, 80);
            form.offsetMax = new Vector2(-20, -70);

            var scrollRect = form.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
            viewport.SetParent(form, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = Vector2.zero;
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewport;

            // Content
            var content = new GameObject("Content").AddComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 0);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = content;

            // Form fields
            CreateFormField(content, "Room Name *", out _nameInput, "Enter room name...");
            CreateFormField(content, "Description (optional)", out _descriptionInput, "Enter description...", 80);

            // Entry Fee row
            CreateEntryFeeRow(content);

            // Player count sliders
            CreatePlayerCountSliders(content);

            // Visibility dropdown
            CreateDropdownField(content, "Visibility", out _visibilityDropdown, 
                new[] { "Unlisted (Code Only)", "Public", "Private (Invite Only)" });

            // Game Mode dropdown
            CreateDropdownField(content, "Game Mode", out _modeDropdown, 
                new[] { "Synchronous (Real-time)", "Asynchronous (Turn-based)" });

            // Invite required toggle
            CreateToggleField(content, "Require Approval to Join", out _inviteRequiredToggle);

            // Error text
            _errorText = UIComponents.CreateText(content, "Error", "", 14, TextAlignmentOptions.Center);
            _errorText.color = new Color(1f, 0.4f, 0.4f);
            _errorText.gameObject.SetActive(false);
        }

        private void CreateFormField(RectTransform parent, string label, out TMP_InputField input, string placeholder, float height = 40)
        {
            var field = new GameObject(label.Replace(" ", "")).AddComponent<RectTransform>();
            field.SetParent(parent, false);
            
            var fieldLayout = field.gameObject.AddComponent<LayoutElement>();
            fieldLayout.preferredHeight = height + 25;

            var vertLayout = field.gameObject.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 5;
            vertLayout.childForceExpandHeight = false;

            // Label
            var labelText = UIComponents.CreateText(field, "Label", label, 14, TextAlignmentOptions.Left);
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;

            // Input
            input = UIComponents.CreateInputField(field, "Input", placeholder, new Vector2(0, height));
            var inputLayout = input.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = height;
            inputLayout.flexibleWidth = 1;

            if (height > 40)
            {
                input.lineType = TMP_InputField.LineType.MultiLineNewline;
            }
        }

        private void CreateEntryFeeRow(RectTransform parent)
        {
            var row = new GameObject("EntryFeeRow").AddComponent<RectTransform>();
            row.SetParent(parent, false);

            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 65;

            var vertLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 5;
            vertLayout.childForceExpandHeight = false;

            // Label
            var labelText = UIComponents.CreateText(row, "Label", "Entry Fee *", 14, TextAlignmentOptions.Left);
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;

            // Input row
            var inputRow = new GameObject("InputRow").AddComponent<RectTransform>();
            inputRow.SetParent(row, false);
            
            var horizLayout = inputRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.spacing = 10;
            horizLayout.childForceExpandWidth = false;
            horizLayout.childForceExpandHeight = true;

            var inputRowLayout = inputRow.gameObject.AddComponent<LayoutElement>();
            inputRowLayout.preferredHeight = 40;

            // Fee input
            _entryFeeInput = UIComponents.CreateInputField(inputRow, "FeeInput", "0.00", new Vector2(150, 40));
            _entryFeeInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            var feeLayout = _entryFeeInput.gameObject.AddComponent<LayoutElement>();
            feeLayout.preferredWidth = 150;

            // Currency dropdown
            _currencyDropdown = UIComponents.CreateDropdown(inputRow, "CurrencyDropdown", new Vector2(120, 40));
            _currencyDropdown.options = new List<TMP_Dropdown.OptionData>();
            foreach (var currency in _currencies)
            {
                _currencyDropdown.options.Add(new TMP_Dropdown.OptionData(currency));
            }
            var currencyLayout = _currencyDropdown.gameObject.AddComponent<LayoutElement>();
            currencyLayout.preferredWidth = 120;
        }

        private void CreatePlayerCountSliders(RectTransform parent)
        {
            // Min players
            var minRow = new GameObject("MinPlayersRow").AddComponent<RectTransform>();
            minRow.SetParent(parent, false);
            
            var minRowLayout = minRow.gameObject.AddComponent<LayoutElement>();
            minRowLayout.preferredHeight = 50;

            var minVertLayout = minRow.gameObject.AddComponent<VerticalLayoutGroup>();
            minVertLayout.spacing = 5;
            minVertLayout.childForceExpandHeight = false;

            var minLabel = UIComponents.CreateText(minRow, "Label", "Min Players: 2", 14, TextAlignmentOptions.Left);
            minLabel.color = new Color(0.7f, 0.7f, 0.7f);
            _minPlayersText = minLabel;
            var minLabelLayout = minLabel.gameObject.AddComponent<LayoutElement>();
            minLabelLayout.preferredHeight = 20;

            _minPlayersSlider = UIComponents.CreateSlider(minRow, "MinPlayersSlider", 2, 10, 2);
            _minPlayersSlider.wholeNumbers = true;
            _minPlayersSlider.onValueChanged.AddListener(v => {
                _minPlayersText.text = $"Min Players: {(int)v}";
                if (_maxPlayersSlider.value < v) _maxPlayersSlider.value = v;
            });
            var minSliderLayout = _minPlayersSlider.gameObject.AddComponent<LayoutElement>();
            minSliderLayout.preferredHeight = 25;

            // Max players
            var maxRow = new GameObject("MaxPlayersRow").AddComponent<RectTransform>();
            maxRow.SetParent(parent, false);

            var maxRowLayout = maxRow.gameObject.AddComponent<LayoutElement>();
            maxRowLayout.preferredHeight = 50;

            var maxVertLayout = maxRow.gameObject.AddComponent<VerticalLayoutGroup>();
            maxVertLayout.spacing = 5;
            maxVertLayout.childForceExpandHeight = false;

            var maxLabel = UIComponents.CreateText(maxRow, "Label", "Max Players: 10", 14, TextAlignmentOptions.Left);
            maxLabel.color = new Color(0.7f, 0.7f, 0.7f);
            _maxPlayersText = maxLabel;
            var maxLabelLayout = maxLabel.gameObject.AddComponent<LayoutElement>();
            maxLabelLayout.preferredHeight = 20;

            _maxPlayersSlider = UIComponents.CreateSlider(maxRow, "MaxPlayersSlider", 2, 10, 10);
            _maxPlayersSlider.wholeNumbers = true;
            _maxPlayersSlider.onValueChanged.AddListener(v => {
                _maxPlayersText.text = $"Max Players: {(int)v}";
                if (_minPlayersSlider.value > v) _minPlayersSlider.value = v;
            });
            var maxSliderLayout = _maxPlayersSlider.gameObject.AddComponent<LayoutElement>();
            maxSliderLayout.preferredHeight = 25;
        }

        private void CreateDropdownField(RectTransform parent, string label, out TMP_Dropdown dropdown, string[] options)
        {
            var field = new GameObject(label.Replace(" ", "")).AddComponent<RectTransform>();
            field.SetParent(parent, false);

            var fieldLayout = field.gameObject.AddComponent<LayoutElement>();
            fieldLayout.preferredHeight = 65;

            var vertLayout = field.gameObject.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 5;
            vertLayout.childForceExpandHeight = false;

            // Label
            var labelText = UIComponents.CreateText(field, "Label", label, 14, TextAlignmentOptions.Left);
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;

            // Dropdown
            dropdown = UIComponents.CreateDropdown(field, "Dropdown", new Vector2(0, 40));
            dropdown.options = new List<TMP_Dropdown.OptionData>();
            foreach (var option in options)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            }
            var dropdownLayout = dropdown.gameObject.AddComponent<LayoutElement>();
            dropdownLayout.preferredHeight = 40;
            dropdownLayout.flexibleWidth = 1;
        }

        private void CreateToggleField(RectTransform parent, string label, out Toggle toggle)
        {
            var field = new GameObject(label.Replace(" ", "")).AddComponent<RectTransform>();
            field.SetParent(parent, false);

            var fieldLayout = field.gameObject.AddComponent<LayoutElement>();
            fieldLayout.preferredHeight = 35;

            var horizLayout = field.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.spacing = 10;
            horizLayout.childForceExpandWidth = false;
            horizLayout.childAlignment = TextAnchor.MiddleLeft;

            // Toggle
            toggle = UIComponents.CreateToggle(field, "Toggle", false);
            var toggleLayout = toggle.gameObject.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 30;
            toggleLayout.preferredHeight = 30;

            // Label
            var labelText = UIComponents.CreateText(field, "Label", label, 14, TextAlignmentOptions.Left);
            labelText.color = new Color(0.8f, 0.8f, 0.8f);
            var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
        }

        private void CreateActionButtons()
        {
            var buttonBar = new GameObject("ButtonBar").AddComponent<RectTransform>();
            buttonBar.SetParent(_container, false);
            buttonBar.anchorMin = new Vector2(0, 0);
            buttonBar.anchorMax = new Vector2(1, 0);
            buttonBar.pivot = new Vector2(0.5f, 0);
            buttonBar.sizeDelta = new Vector2(0, 70);
            buttonBar.anchoredPosition = Vector2.zero;

            var layout = buttonBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 10, 15);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Cancel button
            _cancelButton = UIComponents.CreateButton(buttonBar, "CancelButton", "Cancel", new Vector2(150, 45));
            _cancelButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            UIComponents.SetButtonColor(_cancelButton, new Color(0.4f, 0.4f, 0.45f));

            // Create button
            _createButton = UIComponents.CreateButton(buttonBar, "CreateButton", "Create Room", new Vector2(150, 45));
            _createButton.onClick.AddListener(OnCreateClicked);
            UIComponents.SetButtonColor(_createButton, new Color(0.2f, 0.7f, 0.3f));
        }

        private void CreateLoadingIndicator()
        {
            _loadingIndicator = new GameObject("Loading");
            var loadingRect = _loadingIndicator.AddComponent<RectTransform>();
            loadingRect.SetParent(_container, false);
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.sizeDelta = new Vector2(200, 100);

            var bg = _loadingIndicator.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            var loadingText = UIComponents.CreateText(loadingRect, "LoadingText", "Creating room...", 16, TextAlignmentOptions.Center);
            loadingText.rectTransform.anchorMin = Vector2.zero;
            loadingText.rectTransform.anchorMax = Vector2.one;
            loadingText.rectTransform.sizeDelta = Vector2.zero;

            _loadingIndicator.SetActive(false);
        }

        // =====================================================================
        // SHOW / HIDE
        // =====================================================================

        public override void Show()
        {
            base.Show();
            ResetForm();
        }

        private void ResetForm()
        {
            _nameInput.text = "";
            _descriptionInput.text = "";
            _entryFeeInput.text = "1.00";
            _currencyDropdown.value = 0;
            _minPlayersSlider.value = 2;
            _maxPlayersSlider.value = 2;
            _visibilityDropdown.value = 0;
            _modeDropdown.value = 0;
            _inviteRequiredToggle.isOn = false;
            _errorText.gameObject.SetActive(false);
        }

        // =====================================================================
        // CREATE ROOM
        // =====================================================================

        private void OnCreateClicked()
        {
            if (_isCreating) return;

            // Validate
            if (!ValidateForm()) return;

            // Build config
            var config = new CreateRoomConfig
            {
                Name = _nameInput.text.Trim(),
                Description = _descriptionInput.text.Trim(),
                EntryFee = decimal.Parse(_entryFeeInput.text),
                EntryCurrency = _currencies[_currencyDropdown.value],
                MinPlayers = (int)_minPlayersSlider.value,
                MaxPlayers = (int)_maxPlayersSlider.value,
                Visibility = GetVisibility(),
                Mode = _modeDropdown.value == 0 ? RoomMode.Sync : RoomMode.Async,
                InviteRequired = _inviteRequiredToggle.isOn
            };

            // Create room
            SetLoading(true);

            DeskillzRooms.CreateRoom(config,
                room =>
                {
                    SetLoading(false);
                    OnRoomCreated?.Invoke(room);
                    DeskillzLogger.Debug($"[CreateRoomUI] Room created: {room.RoomCode}");
                },
                error =>
                {
                    SetLoading(false);
                    ShowError(error.Message);
                });
        }

        private bool ValidateForm()
        {
            // Room name required
            if (string.IsNullOrWhiteSpace(_nameInput.text))
            {
                ShowError("Room name is required");
                return false;
            }

            if (_nameInput.text.Trim().Length < 3)
            {
                ShowError("Room name must be at least 3 characters");
                return false;
            }

            // Entry fee validation
            if (!decimal.TryParse(_entryFeeInput.text, out decimal entryFee) || entryFee < 0)
            {
                ShowError("Invalid entry fee");
                return false;
            }

            // Player count validation
            if (_minPlayersSlider.value > _maxPlayersSlider.value)
            {
                ShowError("Min players cannot exceed max players");
                return false;
            }

            return true;
        }

        private RoomVisibility GetVisibility()
        {
            return _visibilityDropdown.value switch
            {
                0 => RoomVisibility.Unlisted,
                1 => RoomVisibility.PublicListed,
                2 => RoomVisibility.Private,
                _ => RoomVisibility.Unlisted
            };
        }

        private void ShowError(string message)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }

        private void SetLoading(bool loading)
        {
            _isCreating = loading;
            _loadingIndicator?.SetActive(loading);
            _createButton.interactable = !loading;
            _cancelButton.interactable = !loading;
            _backButton.interactable = !loading;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        protected override void ApplyTheme(DeskillzTheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            _titleText.color = theme.TextPrimary;
        }
    }
}
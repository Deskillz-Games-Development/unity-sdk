// =============================================================================
// Deskillz SDK for Unity - Join Room UI
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
    /// UI panel for joining a room by code.
    /// Shows a simple dialog to enter a room code and preview room details.
    /// </summary>
    public class JoinRoomUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when room is successfully joined</summary>
        public event Action<PrivateRoom> OnRoomJoined;

        /// <summary>Called when back button is clicked</summary>
        public event Action OnBackClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _container;
        private TextMeshProUGUI _titleText;
        private Button _backButton;

        // Code input
        private TMP_InputField _codeInput;
        private Button _lookupButton;

        // Preview section
        private RectTransform _previewSection;
        private TextMeshProUGUI _roomNameText;
        private TextMeshProUGUI _roomHostText;
        private TextMeshProUGUI _roomPlayersText;
        private TextMeshProUGUI _roomEntryFeeText;
        private TextMeshProUGUI _roomModeText;

        // Actions
        private Button _joinButton;
        private Button _cancelButton;
        private TextMeshProUGUI _errorText;
        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private PrivateRoom _previewRoom;
        private bool _isLoading;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Setup container
            _container = CreateContainer();

            // Header
            CreateHeader();

            // Code input section
            CreateCodeInput();

            // Preview section
            CreatePreviewSection();

            // Error text
            CreateErrorText();

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
            container.sizeDelta = new Vector2(420, 480);

            // Background
            var bg = container.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);

            // Border
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
            _titleText = UIComponents.CreateText(header, "Title", "Join Room", 22, TextAlignmentOptions.Center);
            _titleText.rectTransform.anchorMin = new Vector2(0.15f, 0);
            _titleText.rectTransform.anchorMax = new Vector2(0.85f, 1);
            _titleText.rectTransform.sizeDelta = Vector2.zero;
            _titleText.fontStyle = FontStyles.Bold;
        }

        private void CreateCodeInput()
        {
            var section = new GameObject("CodeSection").AddComponent<RectTransform>();
            section.SetParent(_container, false);
            section.anchorMin = new Vector2(0, 1);
            section.anchorMax = new Vector2(1, 1);
            section.pivot = new Vector2(0.5f, 1);
            section.sizeDelta = new Vector2(-40, 100);
            section.anchoredPosition = new Vector2(0, -70);

            var layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // Label
            var labelText = UIComponents.CreateText(section, "Label", "Enter Room Code", 14, TextAlignmentOptions.Center);
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            var labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;

            // Input row
            var inputRow = new GameObject("InputRow").AddComponent<RectTransform>();
            inputRow.SetParent(section, false);

            var horizLayout = inputRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizLayout.spacing = 10;
            horizLayout.childForceExpandWidth = false;
            horizLayout.childForceExpandHeight = true;
            horizLayout.childAlignment = TextAnchor.MiddleCenter;

            var inputRowLayout = inputRow.gameObject.AddComponent<LayoutElement>();
            inputRowLayout.preferredHeight = 50;

            // Code input field
            _codeInput = UIComponents.CreateInputField(inputRow, "CodeInput", "DSKZ-XXXX", new Vector2(200, 50));
            _codeInput.characterLimit = 9; // DSKZ-XXXX format
            _codeInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            _codeInput.onValueChanged.AddListener(OnCodeChanged);
            _codeInput.onEndEdit.AddListener(_ => LookupRoom());
            
            // Style the input for code entry
            _codeInput.textComponent.fontSize = 24;
            _codeInput.textComponent.alignment = TextAlignmentOptions.Center;
            _codeInput.textComponent.fontStyle = FontStyles.Bold;
            _codeInput.textComponent.characterSpacing = 5;
            
            var codeLayout = _codeInput.gameObject.AddComponent<LayoutElement>();
            codeLayout.preferredWidth = 200;

            // Lookup button
            _lookupButton = UIComponents.CreateButton(inputRow, "LookupButton", "→", new Vector2(50, 50));
            _lookupButton.onClick.AddListener(LookupRoom);
            UIComponents.SetButtonColor(_lookupButton, new Color(0.3f, 0.5f, 0.9f));
            var lookupLayout = _lookupButton.gameObject.AddComponent<LayoutElement>();
            lookupLayout.preferredWidth = 50;

            // Hint text
            var hintText = UIComponents.CreateText(section, "Hint", "Enter the code shared by your friend", 12, TextAlignmentOptions.Center);
            hintText.color = new Color(0.5f, 0.5f, 0.5f);
            var hintLayout = hintText.gameObject.AddComponent<LayoutElement>();
            hintLayout.preferredHeight = 18;
        }

        private void CreatePreviewSection()
        {
            _previewSection = new GameObject("PreviewSection").AddComponent<RectTransform>();
            _previewSection.SetParent(_container, false);
            _previewSection.anchorMin = new Vector2(0, 1);
            _previewSection.anchorMax = new Vector2(1, 1);
            _previewSection.pivot = new Vector2(0.5f, 1);
            _previewSection.sizeDelta = new Vector2(-40, 200);
            _previewSection.anchoredPosition = new Vector2(0, -180);

            // Background
            var bg = _previewSection.gameObject.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.18f);

            var layout = _previewSection.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(20, 20, 15, 15);
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            // Room preview title
            var previewTitle = UIComponents.CreateText(_previewSection, "PreviewTitle", "Room Preview", 14, TextAlignmentOptions.Left);
            previewTitle.color = new Color(0.6f, 0.6f, 0.6f);
            previewTitle.fontStyle = FontStyles.Bold;
            var titleLayout = previewTitle.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 20;

            // Divider
            var divider = new GameObject("Divider").AddComponent<RectTransform>();
            divider.SetParent(_previewSection, false);
            var dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(0.3f, 0.3f, 0.35f);
            var dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1;

            // Room name
            _roomNameText = UIComponents.CreateText(_previewSection, "RoomName", "---", 20, TextAlignmentOptions.Left);
            _roomNameText.fontStyle = FontStyles.Bold;
            var nameLayout = _roomNameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 28;

            // Host
            _roomHostText = UIComponents.CreateText(_previewSection, "Host", "Host: ---", 14, TextAlignmentOptions.Left);
            _roomHostText.color = new Color(0.7f, 0.7f, 0.7f);
            var hostLayout = _roomHostText.gameObject.AddComponent<LayoutElement>();
            hostLayout.preferredHeight = 20;

            // Players
            _roomPlayersText = UIComponents.CreateText(_previewSection, "Players", "Players: ---", 14, TextAlignmentOptions.Left);
            _roomPlayersText.color = new Color(0.7f, 0.7f, 0.7f);
            var playersLayout = _roomPlayersText.gameObject.AddComponent<LayoutElement>();
            playersLayout.preferredHeight = 20;

            // Entry fee
            _roomEntryFeeText = UIComponents.CreateText(_previewSection, "EntryFee", "Entry Fee: ---", 16, TextAlignmentOptions.Left);
            _roomEntryFeeText.color = new Color(0.3f, 0.9f, 0.4f);
            _roomEntryFeeText.fontStyle = FontStyles.Bold;
            var feeLayout = _roomEntryFeeText.gameObject.AddComponent<LayoutElement>();
            feeLayout.preferredHeight = 22;

            // Mode
            _roomModeText = UIComponents.CreateText(_previewSection, "Mode", "Mode: ---", 14, TextAlignmentOptions.Left);
            _roomModeText.color = new Color(0.7f, 0.7f, 0.7f);
            var modeLayout = _roomModeText.gameObject.AddComponent<LayoutElement>();
            modeLayout.preferredHeight = 20;

            // Initially hidden
            _previewSection.gameObject.SetActive(false);
        }

        private void CreateErrorText()
        {
            _errorText = UIComponents.CreateText(_container, "Error", "", 14, TextAlignmentOptions.Center);
            _errorText.rectTransform.anchorMin = new Vector2(0, 0);
            _errorText.rectTransform.anchorMax = new Vector2(1, 0);
            _errorText.rectTransform.pivot = new Vector2(0.5f, 0);
            _errorText.rectTransform.sizeDelta = new Vector2(-40, 30);
            _errorText.rectTransform.anchoredPosition = new Vector2(0, 85);
            _errorText.color = new Color(1f, 0.4f, 0.4f);
            _errorText.gameObject.SetActive(false);
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
            _cancelButton = UIComponents.CreateButton(buttonBar, "CancelButton", "Cancel", new Vector2(140, 45));
            _cancelButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            UIComponents.SetButtonColor(_cancelButton, new Color(0.4f, 0.4f, 0.45f));

            // Join button
            _joinButton = UIComponents.CreateButton(buttonBar, "JoinButton", "Join Room", new Vector2(140, 45));
            _joinButton.onClick.AddListener(OnJoinClicked);
            _joinButton.interactable = false;
            UIComponents.SetButtonColor(_joinButton, new Color(0.2f, 0.7f, 0.3f));
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

            var loadingText = UIComponents.CreateText(loadingRect, "LoadingText", "Loading...", 16, TextAlignmentOptions.Center);
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

        /// <summary>
        /// Show with a pre-filled room code.
        /// </summary>
        public void Show(string roomCode)
        {
            base.Show();
            ResetForm();

            if (!string.IsNullOrEmpty(roomCode))
            {
                _codeInput.text = roomCode.ToUpper();
                LookupRoom();
            }
        }

        private void ResetForm()
        {
            _codeInput.text = "";
            _previewRoom = null;
            _previewSection.gameObject.SetActive(false);
            _joinButton.interactable = false;
            _errorText.gameObject.SetActive(false);
        }

        // =====================================================================
        // CODE INPUT
        // =====================================================================

        private void OnCodeChanged(string value)
        {
            // Auto-format to uppercase
            if (_codeInput.text != value.ToUpper())
            {
                _codeInput.text = value.ToUpper();
            }

            // Auto-insert hyphen after DSKZ
            if (value.Length == 4 && !value.Contains("-"))
            {
                _codeInput.text = value + "-";
                _codeInput.caretPosition = 5;
            }

            // Clear preview if code changed
            if (_previewRoom != null && _previewRoom.RoomCode != value)
            {
                _previewRoom = null;
                _previewSection.gameObject.SetActive(false);
                _joinButton.interactable = false;
            }

            _errorText.gameObject.SetActive(false);
        }

        private void LookupRoom()
        {
            var code = _codeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(code) || code.Length < 8)
            {
                return;
            }

            SetLoading(true);

            DeskillzRooms.GetRoomByCode(code,
                room =>
                {
                    SetLoading(false);
                    ShowPreview(room);
                },
                error =>
                {
                    SetLoading(false);
                    ShowError(error.Message);
                    _previewSection.gameObject.SetActive(false);
                    _joinButton.interactable = false;
                });
        }

        private void ShowPreview(PrivateRoom room)
        {
            _previewRoom = room;

            _roomNameText.text = room.Name;
            _roomHostText.text = $"Host: {room.Host?.Username ?? "Unknown"}";
            _roomPlayersText.text = $"Players: {room.CurrentPlayers}/{room.MaxPlayers}";
            _roomEntryFeeText.text = $"Entry Fee: ${room.EntryFee:F2} {room.EntryCurrency}";
            _roomModeText.text = $"Mode: {(room.Mode == RoomMode.Sync ? "Real-time" : "Turn-based")}";

            _previewSection.gameObject.SetActive(true);
            _joinButton.interactable = room.CanJoin;

            if (!room.CanJoin)
            {
                if (room.IsFull)
                {
                    ShowError("Room is full");
                }
                else if (room.Status != RoomStatus.Waiting)
                {
                    ShowError("Room is no longer accepting players");
                }
            }
        }

        // =====================================================================
        // JOIN ROOM
        // =====================================================================

        private void OnJoinClicked()
        {
            if (_isLoading || _previewRoom == null) return;

            SetLoading(true);

            DeskillzRooms.JoinRoom(_previewRoom.RoomCode,
                room =>
                {
                    SetLoading(false);
                    OnRoomJoined?.Invoke(room);
                    DeskillzLogger.Debug($"[JoinRoomUI] Joined room: {room.RoomCode}");
                },
                error =>
                {
                    SetLoading(false);
                    ShowError(error.Message);
                });
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void ShowError(string message)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }

        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            _loadingIndicator?.SetActive(loading);
            _joinButton.interactable = !loading && _previewRoom?.CanJoin == true;
            _cancelButton.interactable = !loading;
            _backButton.interactable = !loading;
            _lookupButton.interactable = !loading;
            _codeInput.interactable = !loading;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        protected override void ApplyTheme(DeskillzTheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            _titleText.color = theme.TextPrimary;
            _roomNameText.color = theme.TextPrimary;
        }
    }
}
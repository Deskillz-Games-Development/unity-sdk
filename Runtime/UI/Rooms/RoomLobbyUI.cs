// =============================================================================
// Deskillz SDK for Unity - Room Lobby UI (Waiting Room)
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
    /// UI panel for the room lobby (waiting room).
    /// Shows player list, ready status, room code, and controls.
    /// </summary>
    public class RoomLobbyUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when leave button is clicked</summary>
        public event Action OnLeaveClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _container;

        // Header
        private TextMeshProUGUI _roomNameText;
        private TextMeshProUGUI _roomCodeText;
        private Button _copyCodeButton;
        private Button _shareButton;

        // Room info
        private TextMeshProUGUI _entryFeeText;
        private TextMeshProUGUI _prizePoolText;
        private TextMeshProUGUI _modeText;
        private TextMeshProUGUI _statusText;

        // Player list
        private ScrollRect _playerScrollRect;
        private RectTransform _playerListContent;
        private TextMeshProUGUI _playerCountText;
        private List<RoomPlayerCard> _playerCards = new List<RoomPlayerCard>();

        // Ready section
        private Toggle _readyToggle;
        private TextMeshProUGUI _readyStatusText;
        private Slider _readyProgressBar;

        // Countdown
        private RectTransform _countdownSection;
        private TextMeshProUGUI _countdownText;

        // Action buttons
        private Button _readyButton;
        private Button _startButton;
        private Button _leaveButton;
        private Button _cancelButton;

        // Chat (optional)
        private RectTransform _chatSection;
        private ScrollRect _chatScrollRect;
        private RectTransform _chatContent;
        private TMP_InputField _chatInput;
        private Button _sendChatButton;

        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private PrivateRoom _room;
        private bool _isHost;
        private bool _isReady;
        private int _countdownSeconds;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Main container
            _container = CreateContainer();

            // Header with room info
            CreateHeader();

            // Room stats bar
            CreateRoomInfoBar();

            // Player list
            CreatePlayerList();

            // Ready section
            CreateReadySection();

            // Countdown display
            CreateCountdownSection();

            // Chat section (optional)
            CreateChatSection();

            // Action buttons
            CreateActionButtons();

            // Loading indicator
            CreateLoadingIndicator();

            // Subscribe to room events
            SubscribeToEvents();
        }

        private RectTransform CreateContainer()
        {
            var container = new GameObject("Container").AddComponent<RectTransform>();
            container.SetParent(_rectTransform, false);
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.sizeDelta = Vector2.zero;
            container.offsetMin = new Vector2(20, 20);
            container.offsetMax = new Vector2(-20, -20);

            // Background
            var bg = container.gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);

            return container;
        }

        private void CreateHeader()
        {
            var header = new GameObject("Header").AddComponent<RectTransform>();
            header.SetParent(_container, false);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 80);
            header.anchoredPosition = Vector2.zero;

            var headerBg = header.gameObject.AddComponent<Image>();
            headerBg.color = new Color(0.08f, 0.08f, 0.1f);

            var layout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 5;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // Room name
            _roomNameText = UIComponents.CreateText(header, "RoomName", "Room Name", 24, TextAlignmentOptions.Center);
            _roomNameText.fontStyle = FontStyles.Bold;
            var nameLayout = _roomNameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 32;

            // Code row
            var codeRow = new GameObject("CodeRow").AddComponent<RectTransform>();
            codeRow.SetParent(header, false);
            
            var codeLayout = codeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            codeLayout.spacing = 10;
            codeLayout.childForceExpandWidth = false;
            codeLayout.childAlignment = TextAnchor.MiddleCenter;
            
            var codeRowLayout = codeRow.gameObject.AddComponent<LayoutElement>();
            codeRowLayout.preferredHeight = 30;

            // Room code
            _roomCodeText = UIComponents.CreateText(codeRow, "RoomCode", "DSKZ-XXXX", 18, TextAlignmentOptions.Center);
            _roomCodeText.color = new Color(0.5f, 0.8f, 1f);
            _roomCodeText.fontStyle = FontStyles.Bold;

            // Copy button
            _copyCodeButton = UIComponents.CreateButton(codeRow, "CopyButton", "📋", new Vector2(35, 28));
            _copyCodeButton.onClick.AddListener(CopyRoomCode);
            UIComponents.SetButtonColor(_copyCodeButton, new Color(0.25f, 0.25f, 0.3f));

            // Share button
            _shareButton = UIComponents.CreateButton(codeRow, "ShareButton", "Share", new Vector2(70, 28));
            _shareButton.onClick.AddListener(ShareRoom);
            UIComponents.SetButtonColor(_shareButton, new Color(0.3f, 0.5f, 0.9f));
        }

        private void CreateRoomInfoBar()
        {
            var infoBar = new GameObject("InfoBar").AddComponent<RectTransform>();
            infoBar.SetParent(_container, false);
            infoBar.anchorMin = new Vector2(0, 1);
            infoBar.anchorMax = new Vector2(1, 1);
            infoBar.pivot = new Vector2(0.5f, 1);
            infoBar.sizeDelta = new Vector2(0, 50);
            infoBar.anchoredPosition = new Vector2(0, -85);

            var infoBg = infoBar.gameObject.AddComponent<Image>();
            infoBg.color = new Color(0.12f, 0.12f, 0.15f);

            var layout = infoBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 8, 8);
            layout.spacing = 30;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Entry fee
            var feeContainer = CreateInfoItem(infoBar, "EntryFee", "Entry Fee");
            _entryFeeText = feeContainer.GetComponentInChildren<TextMeshProUGUI>();

            // Prize pool
            var prizeContainer = CreateInfoItem(infoBar, "PrizePool", "Prize Pool");
            _prizePoolText = prizeContainer.GetComponentInChildren<TextMeshProUGUI>();

            // Mode
            var modeContainer = CreateInfoItem(infoBar, "Mode", "Mode");
            _modeText = modeContainer.GetComponentInChildren<TextMeshProUGUI>();

            // Status
            var statusContainer = CreateInfoItem(infoBar, "Status", "Status");
            _statusText = statusContainer.GetComponentInChildren<TextMeshProUGUI>();
        }

        private RectTransform CreateInfoItem(RectTransform parent, string name, string label)
        {
            var container = new GameObject(name).AddComponent<RectTransform>();
            container.SetParent(parent, false);

            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var labelText = UIComponents.CreateText(container, "Label", label, 10, TextAlignmentOptions.Center);
            labelText.color = new Color(0.5f, 0.5f, 0.5f);

            var valueText = UIComponents.CreateText(container, "Value", "---", 14, TextAlignmentOptions.Center);
            valueText.fontStyle = FontStyles.Bold;

            return container;
        }

        private void CreatePlayerList()
        {
            var listContainer = new GameObject("PlayerListContainer").AddComponent<RectTransform>();
            listContainer.SetParent(_container, false);
            listContainer.anchorMin = new Vector2(0, 0);
            listContainer.anchorMax = new Vector2(1, 1);
            listContainer.offsetMin = new Vector2(15, 180);
            listContainer.offsetMax = new Vector2(-15, -145);

            // Header
            var listHeader = new GameObject("ListHeader").AddComponent<RectTransform>();
            listHeader.SetParent(listContainer, false);
            listHeader.anchorMin = new Vector2(0, 1);
            listHeader.anchorMax = new Vector2(1, 1);
            listHeader.pivot = new Vector2(0.5f, 1);
            listHeader.sizeDelta = new Vector2(0, 30);

            _playerCountText = UIComponents.CreateText(listHeader, "PlayerCount", "Players (0/0)", 16, TextAlignmentOptions.Left);
            _playerCountText.rectTransform.anchorMin = Vector2.zero;
            _playerCountText.rectTransform.anchorMax = Vector2.one;
            _playerCountText.rectTransform.offsetMin = new Vector2(10, 0);
            _playerCountText.fontStyle = FontStyles.Bold;

            // Scroll view
            var scrollContainer = new GameObject("ScrollContainer").AddComponent<RectTransform>();
            scrollContainer.SetParent(listContainer, false);
            scrollContainer.anchorMin = new Vector2(0, 0);
            scrollContainer.anchorMax = new Vector2(1, 1);
            scrollContainer.offsetMin = new Vector2(0, 0);
            scrollContainer.offsetMax = new Vector2(0, -35);

            _playerScrollRect = scrollContainer.gameObject.AddComponent<ScrollRect>();
            _playerScrollRect.horizontal = false;
            _playerScrollRect.vertical = true;
            _playerScrollRect.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
            viewport.SetParent(scrollContainer, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = Vector2.zero;
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _playerScrollRect.viewport = viewport;

            // Content
            _playerListContent = new GameObject("Content").AddComponent<RectTransform>();
            _playerListContent.SetParent(viewport, false);
            _playerListContent.anchorMin = new Vector2(0, 1);
            _playerListContent.anchorMax = new Vector2(1, 1);
            _playerListContent.pivot = new Vector2(0.5f, 1);
            _playerListContent.sizeDelta = new Vector2(0, 0);

            var contentLayout = _playerListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = false;

            var fitter = _playerListContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _playerScrollRect.content = _playerListContent;
        }

        private void CreateReadySection()
        {
            var readySection = new GameObject("ReadySection").AddComponent<RectTransform>();
            readySection.SetParent(_container, false);
            readySection.anchorMin = new Vector2(0, 0);
            readySection.anchorMax = new Vector2(1, 0);
            readySection.pivot = new Vector2(0.5f, 0);
            readySection.sizeDelta = new Vector2(0, 50);
            readySection.anchoredPosition = new Vector2(0, 115);

            var layout = readySection.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 5, 5);
            layout.spacing = 15;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Ready status text
            _readyStatusText = UIComponents.CreateText(readySection, "ReadyStatus", "0/0 Ready", 16, TextAlignmentOptions.Left);
            var statusLayout = _readyStatusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 100;

            // Progress bar
            _readyProgressBar = UIComponents.CreateSlider(readySection, "ReadyProgress", 0, 1, 0);
            _readyProgressBar.interactable = false;
            var progressLayout = _readyProgressBar.gameObject.AddComponent<LayoutElement>();
            progressLayout.flexibleWidth = 1;
            progressLayout.preferredHeight = 20;

            // Set progress bar colors
            var fillArea = _readyProgressBar.fillRect?.GetComponent<Image>();
            if (fillArea != null)
            {
                fillArea.color = new Color(0.3f, 0.9f, 0.4f);
            }
        }

        private void CreateCountdownSection()
        {
            _countdownSection = new GameObject("CountdownSection").AddComponent<RectTransform>();
            _countdownSection.SetParent(_container, false);
            _countdownSection.anchorMin = new Vector2(0.5f, 0.5f);
            _countdownSection.anchorMax = new Vector2(0.5f, 0.5f);
            _countdownSection.sizeDelta = new Vector2(200, 150);

            var bg = _countdownSection.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);

            var layout = _countdownSection.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var titleText = UIComponents.CreateText(_countdownSection, "Title", "Match Starting", 18, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;

            _countdownText = UIComponents.CreateText(_countdownSection, "Countdown", "5", 60, TextAlignmentOptions.Center);
            _countdownText.fontStyle = FontStyles.Bold;
            _countdownText.color = new Color(0.3f, 0.9f, 0.4f);

            _countdownSection.gameObject.SetActive(false);
        }

        private void CreateChatSection()
        {
            _chatSection = new GameObject("ChatSection").AddComponent<RectTransform>();
            _chatSection.SetParent(_container, false);
            _chatSection.anchorMin = new Vector2(1, 0);
            _chatSection.anchorMax = new Vector2(1, 1);
            _chatSection.pivot = new Vector2(1, 0.5f);
            _chatSection.sizeDelta = new Vector2(280, 0);
            _chatSection.offsetMin = new Vector2(-295, 80);
            _chatSection.offsetMax = new Vector2(-15, -90);

            var chatBg = _chatSection.gameObject.AddComponent<Image>();
            chatBg.color = new Color(0.08f, 0.08f, 0.1f);

            // Chat header
            var chatHeader = UIComponents.CreateText(_chatSection, "ChatHeader", "Chat", 14, TextAlignmentOptions.Left);
            chatHeader.rectTransform.anchorMin = new Vector2(0, 1);
            chatHeader.rectTransform.anchorMax = new Vector2(1, 1);
            chatHeader.rectTransform.pivot = new Vector2(0.5f, 1);
            chatHeader.rectTransform.sizeDelta = new Vector2(-20, 25);
            chatHeader.rectTransform.anchoredPosition = new Vector2(0, -5);
            chatHeader.fontStyle = FontStyles.Bold;

            // Chat scroll area
            var chatScrollContainer = new GameObject("ChatScroll").AddComponent<RectTransform>();
            chatScrollContainer.SetParent(_chatSection, false);
            chatScrollContainer.anchorMin = new Vector2(0, 0);
            chatScrollContainer.anchorMax = new Vector2(1, 1);
            chatScrollContainer.offsetMin = new Vector2(5, 45);
            chatScrollContainer.offsetMax = new Vector2(-5, -30);

            _chatScrollRect = chatScrollContainer.gameObject.AddComponent<ScrollRect>();
            _chatScrollRect.horizontal = false;
            _chatScrollRect.vertical = true;

            var chatViewport = new GameObject("Viewport").AddComponent<RectTransform>();
            chatViewport.SetParent(chatScrollContainer, false);
            chatViewport.anchorMin = Vector2.zero;
            chatViewport.anchorMax = Vector2.one;
            chatViewport.sizeDelta = Vector2.zero;
            chatViewport.gameObject.AddComponent<Image>().color = Color.clear;
            chatViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _chatScrollRect.viewport = chatViewport;

            _chatContent = new GameObject("Content").AddComponent<RectTransform>();
            _chatContent.SetParent(chatViewport, false);
            _chatContent.anchorMin = new Vector2(0, 0);
            _chatContent.anchorMax = new Vector2(1, 0);
            _chatContent.pivot = new Vector2(0.5f, 0);

            var chatContentLayout = _chatContent.gameObject.AddComponent<VerticalLayoutGroup>();
            chatContentLayout.spacing = 5;
            chatContentLayout.padding = new RectOffset(5, 5, 5, 5);
            chatContentLayout.childForceExpandHeight = false;

            var chatFitter = _chatContent.gameObject.AddComponent<ContentSizeFitter>();
            chatFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _chatScrollRect.content = _chatContent;

            // Chat input
            var inputRow = new GameObject("InputRow").AddComponent<RectTransform>();
            inputRow.SetParent(_chatSection, false);
            inputRow.anchorMin = new Vector2(0, 0);
            inputRow.anchorMax = new Vector2(1, 0);
            inputRow.pivot = new Vector2(0.5f, 0);
            inputRow.sizeDelta = new Vector2(-10, 35);
            inputRow.anchoredPosition = new Vector2(0, 5);

            var inputLayout = inputRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            inputLayout.spacing = 5;
            inputLayout.childForceExpandHeight = true;

            _chatInput = UIComponents.CreateInputField(inputRow, "ChatInput", "Type message...", new Vector2(200, 35));
            _chatInput.characterLimit = 200;
            var chatInputLayout = _chatInput.gameObject.AddComponent<LayoutElement>();
            chatInputLayout.flexibleWidth = 1;

            _sendChatButton = UIComponents.CreateButton(inputRow, "SendButton", "→", new Vector2(40, 35));
            _sendChatButton.onClick.AddListener(SendChatMessage);
            var sendLayout = _sendChatButton.gameObject.AddComponent<LayoutElement>();
            sendLayout.preferredWidth = 40;

            // Hide chat by default (can be enabled)
            _chatSection.gameObject.SetActive(false);
        }

        private void CreateActionButtons()
        {
            var buttonBar = new GameObject("ButtonBar").AddComponent<RectTransform>();
            buttonBar.SetParent(_container, false);
            buttonBar.anchorMin = new Vector2(0, 0);
            buttonBar.anchorMax = new Vector2(1, 0);
            buttonBar.pivot = new Vector2(0.5f, 0);
            buttonBar.sizeDelta = new Vector2(0, 100);
            buttonBar.anchoredPosition = Vector2.zero;

            var buttonBg = buttonBar.gameObject.AddComponent<Image>();
            buttonBg.color = new Color(0.08f, 0.08f, 0.1f);

            var layout = buttonBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 15, 15);
            layout.spacing = 20;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Leave button
            _leaveButton = UIComponents.CreateButton(buttonBar, "LeaveButton", "Leave Room", new Vector2(140, 50));
            _leaveButton.onClick.AddListener(() => OnLeaveClicked?.Invoke());
            UIComponents.SetButtonColor(_leaveButton, new Color(0.7f, 0.3f, 0.3f));

            // Cancel button (host only)
            _cancelButton = UIComponents.CreateButton(buttonBar, "CancelButton", "Cancel Room", new Vector2(140, 50));
            _cancelButton.onClick.AddListener(OnCancelClicked);
            UIComponents.SetButtonColor(_cancelButton, new Color(0.6f, 0.4f, 0.2f));

            // Ready button
            _readyButton = UIComponents.CreateButton(buttonBar, "ReadyButton", "Ready", new Vector2(140, 50));
            _readyButton.onClick.AddListener(OnReadyClicked);
            UIComponents.SetButtonColor(_readyButton, new Color(0.3f, 0.7f, 0.3f));

            // Start button (host only)
            _startButton = UIComponents.CreateButton(buttonBar, "StartButton", "Start Match", new Vector2(140, 50));
            _startButton.onClick.AddListener(OnStartClicked);
            _startButton.interactable = false;
            UIComponents.SetButtonColor(_startButton, new Color(0.2f, 0.6f, 0.9f));
        }

        private void CreateLoadingIndicator()
        {
            _loadingIndicator = new GameObject("Loading");
            var loadingRect = _loadingIndicator.AddComponent<RectTransform>();
            loadingRect.SetParent(_container, false);
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.sizeDelta = new Vector2(150, 80);

            var bg = _loadingIndicator.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            var loadingText = UIComponents.CreateText(loadingRect, "LoadingText", "Loading...", 16, TextAlignmentOptions.Center);
            loadingText.rectTransform.anchorMin = Vector2.zero;
            loadingText.rectTransform.anchorMax = Vector2.one;

            _loadingIndicator.SetActive(false);
        }

        // =====================================================================
        // EVENTS
        // =====================================================================

        private void SubscribeToEvents()
        {
            DeskillzRooms.OnPlayerJoined += HandlePlayerJoined;
            DeskillzRooms.OnPlayerLeft += HandlePlayerLeft;
            DeskillzRooms.OnPlayerReadyChanged += HandlePlayerReadyChanged;
            DeskillzRooms.OnCountdownStarted += HandleCountdownStarted;
            DeskillzRooms.OnCountdownTick += HandleCountdownTick;
            DeskillzRooms.OnChatReceived += HandleChatReceived;
        }

        private void OnDestroy()
        {
            DeskillzRooms.OnPlayerJoined -= HandlePlayerJoined;
            DeskillzRooms.OnPlayerLeft -= HandlePlayerLeft;
            DeskillzRooms.OnPlayerReadyChanged -= HandlePlayerReadyChanged;
            DeskillzRooms.OnCountdownStarted -= HandleCountdownStarted;
            DeskillzRooms.OnCountdownTick -= HandleCountdownTick;
            DeskillzRooms.OnChatReceived -= HandleChatReceived;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the room to display.
        /// </summary>
        public void SetRoom(PrivateRoom room)
        {
            _room = room;
            _isHost = room.IsCurrentUserHost;
            _isReady = room.GetCurrentPlayer()?.IsReady ?? false;

            UpdateDisplay();
            UpdatePlayerList();
            UpdateButtons();
        }

        /// <summary>
        /// Update the room display with new data.
        /// </summary>
        public void UpdateRoom(PrivateRoom room)
        {
            _room = room;
            _isHost = room.IsCurrentUserHost;

            UpdateDisplay();
            UpdatePlayerList();
            UpdateButtons();
        }

        // =====================================================================
        // UI UPDATES
        // =====================================================================

        private void UpdateDisplay()
        {
            if (_room == null) return;

            _roomNameText.text = _room.Name;
            _roomCodeText.text = _room.RoomCode;
            _entryFeeText.text = $"${_room.EntryFee:F2} {_room.EntryCurrency}";
            _prizePoolText.text = $"${_room.PrizePool:F2}";
            _modeText.text = _room.Mode == RoomMode.Sync ? "Real-time" : "Turn-based";
            _statusText.text = _room.Status.ToString();
            _playerCountText.text = $"Players ({_room.CurrentPlayers}/{_room.MaxPlayers})";

            // Update ready progress
            int readyCount = _room.ReadyPlayerCount;
            int totalPlayers = _room.CurrentPlayers;
            _readyStatusText.text = $"{readyCount}/{totalPlayers} Ready";
            _readyProgressBar.value = totalPlayers > 0 ? (float)readyCount / totalPlayers : 0;
        }

        private void UpdatePlayerList()
        {
            if (_room == null) return;

            // Clear existing cards
            foreach (var card in _playerCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _playerCards.Clear();

            // Create new cards
            foreach (var player in _room.Players)
            {
                var card = CreatePlayerCard(player);
                _playerCards.Add(card);
            }
        }

        private RoomPlayerCard CreatePlayerCard(RoomPlayer player)
        {
            var cardGo = new GameObject($"PlayerCard_{player.Id}");
            cardGo.transform.SetParent(_playerListContent, false);

            var card = cardGo.AddComponent<RoomPlayerCard>();
            card.Initialize(_theme);
            card.SetPlayer(player, _isHost && !player.IsCurrentUser);
            card.OnKickClicked += () => KickPlayer(player.Id);

            return card;
        }

        private void UpdateButtons()
        {
            // Show/hide host-only buttons
            _cancelButton.gameObject.SetActive(_isHost);
            _startButton.gameObject.SetActive(_isHost);

            // Update ready button text
            var readyButtonText = _readyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (readyButtonText != null)
            {
                readyButtonText.text = _isReady ? "Not Ready" : "Ready";
            }
            UIComponents.SetButtonColor(_readyButton, _isReady 
                ? new Color(0.6f, 0.4f, 0.2f) 
                : new Color(0.3f, 0.7f, 0.3f));

            // Enable start button only when all ready
            _startButton.interactable = _room?.CanStart ?? false;
        }

        // =====================================================================
        // ACTIONS
        // =====================================================================

        private void OnReadyClicked()
        {
            _isReady = !_isReady;
            DeskillzRooms.SetReady(_isReady);
            UpdateButtons();
        }

        private void OnStartClicked()
        {
            if (!_isHost || _room == null) return;

            DeskillzRooms.StartMatch(
                () => DeskillzLogger.Debug("[RoomLobbyUI] Start match requested"),
                error => DeskillzLogger.Error($"[RoomLobbyUI] Failed to start: {error.Message}")
            );
        }

        private void OnCancelClicked()
        {
            if (!_isHost) return;

            DeskillzRooms.CancelRoom(
                () => DeskillzLogger.Debug("[RoomLobbyUI] Room cancelled"),
                error => DeskillzLogger.Error($"[RoomLobbyUI] Failed to cancel: {error.Message}")
            );
        }

        private void KickPlayer(string playerId)
        {
            if (!_isHost) return;

            DeskillzRooms.KickPlayer(playerId,
                () => DeskillzLogger.Debug($"[RoomLobbyUI] Kicked player {playerId}"),
                error => DeskillzLogger.Error($"[RoomLobbyUI] Failed to kick: {error.Message}")
            );
        }

        private void CopyRoomCode()
        {
            if (_room == null) return;

            GUIUtility.systemCopyBuffer = _room.RoomCode;
            DeskillzLogger.Debug($"[RoomLobbyUI] Copied room code: {_room.RoomCode}");
        }

        private void ShareRoom()
        {
            if (_room == null) return;

            var shareUrl = $"https://deskillz.games/room/{_room.RoomCode}";
            GUIUtility.systemCopyBuffer = shareUrl;
            DeskillzLogger.Debug($"[RoomLobbyUI] Copied share link: {shareUrl}");
        }

        private void SendChatMessage()
        {
            var message = _chatInput.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            DeskillzRooms.SendChat(message);
            _chatInput.text = "";
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandlePlayerJoined(RoomPlayer player)
        {
            if (!IsVisible) return;
            UpdateRoom(DeskillzRooms.CurrentRoom);
        }

        private void HandlePlayerLeft(string playerId)
        {
            if (!IsVisible) return;
            UpdateRoom(DeskillzRooms.CurrentRoom);
        }

        private void HandlePlayerReadyChanged(string playerId, bool isReady)
        {
            if (!IsVisible) return;

            // Update own ready state
            if (_room?.GetCurrentPlayer()?.Id == playerId)
            {
                _isReady = isReady;
            }

            UpdateRoom(DeskillzRooms.CurrentRoom);
        }

        private void HandleCountdownStarted(int seconds)
        {
            _countdownSeconds = seconds;
            _countdownText.text = seconds.ToString();
            _countdownSection.gameObject.SetActive(true);
        }

        private void HandleCountdownTick(int seconds)
        {
            _countdownSeconds = seconds;
            _countdownText.text = seconds.ToString();

            if (seconds <= 0)
            {
                _countdownSection.gameObject.SetActive(false);
            }
        }

        private void HandleChatReceived(string senderId, string username, string message)
        {
            if (!IsVisible || _chatContent == null) return;

            AddChatMessage(username, message);
        }

        private void AddChatMessage(string username, string message)
        {
            var msgText = UIComponents.CreateText(_chatContent, "ChatMsg", $"{username}: {message}", 12, TextAlignmentOptions.Left);
            msgText.color = Color.white;
            var layout = msgText.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 20;

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            _chatScrollRect.verticalNormalizedPosition = 0;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        protected override void ApplyTheme(DeskillzTheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            _roomNameText.color = theme.TextPrimary;

            foreach (var card in _playerCards)
            {
                card?.ApplyTheme(theme);
            }
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Spectator View UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Spectator;

namespace Deskillz.UI.Spectator
{
    /// <summary>
    /// Main UI for spectator mode.
    /// Provides room info, player tracking, chat, and view controls.
    /// </summary>
    public class SpectatorViewUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when exit spectator is clicked</summary>
        public event Action OnExitClicked;

        /// <summary>Called when a player is selected to follow</summary>
        public event Action<string> OnPlayerSelected;

        /// <summary>Called when view mode changes</summary>
        public event Action<SpectatorViewMode> OnViewModeChanged;

        /// <summary>Called when chat message is sent</summary>
        public event Action<string> OnChatSent;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        // Header
        private RectTransform _headerContainer;
        private TextMeshProUGUI _roomNameText;
        private TextMeshProUGUI _roomCodeText;
        private TextMeshProUGUI _viewerCountText;
        private Button _exitButton;

        // Game info bar
        private TextMeshProUGUI _gameNameText;
        private TextMeshProUGUI _roundText;
        private TextMeshProUGUI _potText;
        private TextMeshProUGUI _phaseText;

        // Score panel
        private SpectatorScorePanelUI _scorePanel;

        // View controls
        private Button _overviewButton;
        private Button _followButton;
        private TMP_Dropdown _playerDropdown;

        // Chat panel
        private RectTransform _chatContainer;
        private ScrollRect _chatScroll;
        private RectTransform _chatContent;
        private TMP_InputField _chatInput;
        private Button _sendChatButton;
        private Button _toggleChatButton;
        private List<ChatMessageUI> _chatMessages = new List<ChatMessageUI>();
        private bool _chatVisible = true;

        // Stream delay indicator
        private TextMeshProUGUI _delayText;

        // Room switcher (for multi-room)
        private RectTransform _roomSwitcherContainer;
        private List<RoomTabUI> _roomTabs = new List<RoomTabUI>();

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        private const int MAX_CHAT_MESSAGES = 50;
        private const float CHAT_PANEL_WIDTH = 280f;

        // =====================================================================
        // STATE
        // =====================================================================

        private SpectatorSession _currentSession;
        private SpectatorViewMode _currentViewMode = SpectatorViewMode.Overview;
        private string _followedPlayerId;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            CreateHeader();
            CreateGameInfoBar();
            CreateScorePanel();
            CreateViewControls();
            CreateChatPanel();
            CreateRoomSwitcher();

            // Subscribe to events
            SpectatorManager.OnConnected += HandleConnected;
            SpectatorManager.OnGameStateUpdated += HandleGameStateUpdated;
            SpectatorManager.OnPlayerUpdated += HandlePlayerUpdated;
            SpectatorManager.OnChatReceived += HandleChatReceived;
            SpectatorManager.OnSpectatorCountChanged += HandleSpectatorCountChanged;
            SpectatorManager.OnRoundEnded += HandleRoundEnded;
        }

        private void CreateHeader()
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(transform, false);

            _headerContainer = headerGO.AddComponent<RectTransform>();
            _headerContainer.anchorMin = new Vector2(0, 1);
            _headerContainer.anchorMax = new Vector2(1, 1);
            _headerContainer.pivot = new Vector2(0.5f, 1);
            _headerContainer.sizeDelta = new Vector2(0, 50);

            var headerBg = headerGO.AddComponent<Image>();
            headerBg.color = new Color(0, 0, 0, 0.7f);

            var headerLayout = headerGO.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(16, 16, 8, 8);
            headerLayout.spacing = 16;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childForceExpandWidth = false;

            // Exit button
            var exitGO = new GameObject("ExitButton");
            exitGO.transform.SetParent(headerGO.transform, false);

            var exitLayout = exitGO.AddComponent<LayoutElement>();
            exitLayout.preferredWidth = 80;
            exitLayout.preferredHeight = 32;

            var exitBg = exitGO.AddComponent<Image>();
            exitBg.color = new Color(0.4f, 0.2f, 0.2f);

            _exitButton = exitGO.AddComponent<Button>();
            _exitButton.onClick.AddListener(() => OnExitClicked?.Invoke());

            var exitText = UIComponents.CreateText(exitGO.transform, "Exit", 14);
            exitText.alignment = TextAlignmentOptions.Center;

            // Room info
            var roomInfoGO = new GameObject("RoomInfo");
            roomInfoGO.transform.SetParent(headerGO.transform, false);

            var roomInfoLayout = roomInfoGO.AddComponent<VerticalLayoutGroup>();
            roomInfoLayout.spacing = 2;

            _roomNameText = UIComponents.CreateText(roomInfoGO.transform, "Room Name", 16);
            _roomNameText.fontStyle = FontStyles.Bold;

            _roomCodeText = UIComponents.CreateText(roomInfoGO.transform, "DSKZ-XXXX", 11);
            _roomCodeText.color = _theme?.TextSecondary ?? Color.gray;

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(headerGO.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Viewer count
            var viewerGO = new GameObject("ViewerCount");
            viewerGO.transform.SetParent(headerGO.transform, false);

            var viewerLayout = viewerGO.AddComponent<HorizontalLayoutGroup>();
            viewerLayout.spacing = 4;
            viewerLayout.childAlignment = TextAnchor.MiddleRight;

            var eyeIcon = new GameObject("EyeIcon");
            eyeIcon.transform.SetParent(viewerGO.transform, false);

            var eyeLayout = eyeIcon.AddComponent<LayoutElement>();
            eyeLayout.preferredWidth = 16;
            eyeLayout.preferredHeight = 16;

            var eyeImage = eyeIcon.AddComponent<Image>();
            eyeImage.color = _theme?.TextSecondary ?? Color.gray;

            _viewerCountText = UIComponents.CreateText(viewerGO.transform, "0 watching", 12);
            _viewerCountText.color = _theme?.TextSecondary ?? Color.gray;

            // Stream delay
            _delayText = UIComponents.CreateText(headerGO.transform, "10s delay", 11);
            _delayText.color = _theme?.WarningColor ?? new Color(1f, 0.7f, 0f);
        }

        private void CreateGameInfoBar()
        {
            var infoBarGO = new GameObject("GameInfoBar");
            infoBarGO.transform.SetParent(transform, false);

            var infoBarRect = infoBarGO.AddComponent<RectTransform>();
            infoBarRect.anchorMin = new Vector2(0, 1);
            infoBarRect.anchorMax = new Vector2(1, 1);
            infoBarRect.pivot = new Vector2(0.5f, 1);
            infoBarRect.anchoredPosition = new Vector2(0, -50);
            infoBarRect.sizeDelta = new Vector2(0, 36);

            var infoBg = infoBarGO.AddComponent<Image>();
            infoBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            var infoLayout = infoBarGO.AddComponent<HorizontalLayoutGroup>();
            infoLayout.padding = new RectOffset(16, 16, 6, 6);
            infoLayout.spacing = 24;
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.childForceExpandWidth = false;

            // Game name
            _gameNameText = UIComponents.CreateText(infoBarGO.transform, "Game Name", 14);
            _gameNameText.fontStyle = FontStyles.Bold;

            // Round
            _roundText = UIComponents.CreateText(infoBarGO.transform, "Round 1", 14);

            // Phase
            _phaseText = UIComponents.CreateText(infoBarGO.transform, "In Progress", 14);
            _phaseText.color = _theme?.AccentColor ?? new Color(0.4f, 0.8f, 1f);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(infoBarGO.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Pot
            var potLabel = UIComponents.CreateText(infoBarGO.transform, "Pot:", 12);
            potLabel.color = _theme?.TextSecondary ?? Color.gray;

            _potText = UIComponents.CreateText(infoBarGO.transform, "$0.00", 16);
            _potText.fontStyle = FontStyles.Bold;
            _potText.color = _theme?.SuccessColor ?? Color.green;
        }

        private void CreateScorePanel()
        {
            var scorePanelGO = new GameObject("ScorePanel");
            scorePanelGO.transform.SetParent(transform, false);

            var scorePanelRect = scorePanelGO.AddComponent<RectTransform>();
            scorePanelRect.anchorMin = new Vector2(0, 0);
            scorePanelRect.anchorMax = new Vector2(0, 1);
            scorePanelRect.pivot = new Vector2(0, 0.5f);
            scorePanelRect.anchoredPosition = new Vector2(10, -50);
            scorePanelRect.sizeDelta = new Vector2(200, -120);

            _scorePanel = scorePanelGO.AddComponent<SpectatorScorePanelUI>();
            _scorePanel.Initialize(_theme);
            _scorePanel.OnPlayerClicked += playerId => OnPlayerSelected?.Invoke(playerId);
        }

        private void CreateViewControls()
        {
            var controlsGO = new GameObject("ViewControls");
            controlsGO.transform.SetParent(transform, false);

            var controlsRect = controlsGO.AddComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0.5f, 0);
            controlsRect.anchorMax = new Vector2(0.5f, 0);
            controlsRect.pivot = new Vector2(0.5f, 0);
            controlsRect.anchoredPosition = new Vector2(0, 10);
            controlsRect.sizeDelta = new Vector2(300, 40);

            var controlsBg = controlsGO.AddComponent<Image>();
            controlsBg.color = new Color(0, 0, 0, 0.7f);

            var controlsLayout = controlsGO.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.padding = new RectOffset(8, 8, 4, 4);
            controlsLayout.spacing = 8;
            controlsLayout.childAlignment = TextAnchor.MiddleCenter;

            // Overview button
            var overviewGO = new GameObject("OverviewButton");
            overviewGO.transform.SetParent(controlsGO.transform, false);

            var overviewLayout = overviewGO.AddComponent<LayoutElement>();
            overviewLayout.preferredWidth = 80;
            overviewLayout.preferredHeight = 28;

            var overviewBg = overviewGO.AddComponent<Image>();
            overviewBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.5f, 0.8f);

            _overviewButton = overviewGO.AddComponent<Button>();
            _overviewButton.onClick.AddListener(() => SetViewMode(SpectatorViewMode.Overview));

            var overviewText = UIComponents.CreateText(overviewGO.transform, "Overview", 12);
            overviewText.alignment = TextAlignmentOptions.Center;

            // Follow button
            var followGO = new GameObject("FollowButton");
            followGO.transform.SetParent(controlsGO.transform, false);

            var followLayout = followGO.AddComponent<LayoutElement>();
            followLayout.preferredWidth = 80;
            followLayout.preferredHeight = 28;

            var followBg = followGO.AddComponent<Image>();
            followBg.color = new Color(0.3f, 0.3f, 0.35f);

            _followButton = followGO.AddComponent<Button>();
            _followButton.onClick.AddListener(() => SetViewMode(SpectatorViewMode.FollowPlayer));

            var followText = UIComponents.CreateText(followGO.transform, "Follow", 12);
            followText.alignment = TextAlignmentOptions.Center;

            // Player dropdown
            var dropdownGO = new GameObject("PlayerDropdown");
            dropdownGO.transform.SetParent(controlsGO.transform, false);

            var dropdownLayout = dropdownGO.AddComponent<LayoutElement>();
            dropdownLayout.preferredWidth = 100;
            dropdownLayout.preferredHeight = 28;

            _playerDropdown = UIComponents.CreateDropdown(dropdownGO.transform, new string[] { "Select Player" });
            _playerDropdown.onValueChanged.AddListener(OnPlayerDropdownChanged);
        }

        private void CreateChatPanel()
        {
            _chatContainer = new GameObject("ChatPanel").AddComponent<RectTransform>();
            _chatContainer.transform.SetParent(transform, false);
            _chatContainer.anchorMin = new Vector2(1, 0);
            _chatContainer.anchorMax = new Vector2(1, 1);
            _chatContainer.pivot = new Vector2(1, 0.5f);
            _chatContainer.anchoredPosition = new Vector2(-10, -50);
            _chatContainer.sizeDelta = new Vector2(CHAT_PANEL_WIDTH, -120);

            var chatBg = _chatContainer.gameObject.AddComponent<Image>();
            chatBg.color = new Color(0, 0, 0, 0.7f);

            var chatLayout = _chatContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            chatLayout.padding = new RectOffset(8, 8, 8, 8);
            chatLayout.spacing = 8;

            // Chat header
            var chatHeader = new GameObject("ChatHeader");
            chatHeader.transform.SetParent(_chatContainer, false);

            var chatHeaderLayout = chatHeader.AddComponent<HorizontalLayoutGroup>();
            chatHeaderLayout.childForceExpandWidth = false;

            var chatTitle = UIComponents.CreateText(chatHeader.transform, "Chat", 12);
            chatTitle.fontStyle = FontStyles.Bold;

            var chatSpacer = new GameObject("Spacer");
            chatSpacer.transform.SetParent(chatHeader.transform, false);
            chatSpacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            var toggleGO = new GameObject("ToggleButton");
            toggleGO.transform.SetParent(chatHeader.transform, false);

            _toggleChatButton = toggleGO.AddComponent<Button>();
            _toggleChatButton.onClick.AddListener(ToggleChat);

            var toggleText = UIComponents.CreateText(toggleGO.transform, "Hide", 11);

            // Chat scroll
            var scrollGO = new GameObject("ChatScroll");
            scrollGO.transform.SetParent(_chatContainer, false);

            var scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1;

            _chatScroll = scrollGO.AddComponent<ScrollRect>();
            _chatScroll.vertical = true;
            _chatScroll.horizontal = false;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);

            _chatContent = contentGO.AddComponent<RectTransform>();
            _chatContent.anchorMin = new Vector2(0, 1);
            _chatContent.anchorMax = new Vector2(1, 1);
            _chatContent.pivot = new Vector2(0.5f, 1);

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _chatScroll.content = _chatContent;

            // Chat input
            var inputRow = new GameObject("InputRow");
            inputRow.transform.SetParent(_chatContainer, false);

            var inputRowLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.spacing = 4;

            var inputGO = new GameObject("ChatInput");
            inputGO.transform.SetParent(inputRow.transform, false);

            var inputLayout = inputGO.AddComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1;
            inputLayout.preferredHeight = 30;

            _chatInput = UIComponents.CreateInputField(inputGO.transform, "Type a message...");
            _chatInput.onSubmit.AddListener(OnChatSubmit);

            var sendGO = new GameObject("SendButton");
            sendGO.transform.SetParent(inputRow.transform, false);

            var sendLayout = sendGO.AddComponent<LayoutElement>();
            sendLayout.preferredWidth = 50;
            sendLayout.preferredHeight = 30;

            var sendBg = sendGO.AddComponent<Image>();
            sendBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.5f, 0.8f);

            _sendChatButton = sendGO.AddComponent<Button>();
            _sendChatButton.onClick.AddListener(() => OnChatSubmit(_chatInput.text));

            var sendText = UIComponents.CreateText(sendGO.transform, "Send", 12);
            sendText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateRoomSwitcher()
        {
            _roomSwitcherContainer = new GameObject("RoomSwitcher").AddComponent<RectTransform>();
            _roomSwitcherContainer.transform.SetParent(transform, false);
            _roomSwitcherContainer.anchorMin = new Vector2(0.5f, 1);
            _roomSwitcherContainer.anchorMax = new Vector2(0.5f, 1);
            _roomSwitcherContainer.pivot = new Vector2(0.5f, 1);
            _roomSwitcherContainer.anchoredPosition = new Vector2(0, -86);
            _roomSwitcherContainer.sizeDelta = new Vector2(400, 30);

            var switcherLayout = _roomSwitcherContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            switcherLayout.spacing = 4;
            switcherLayout.childAlignment = TextAnchor.MiddleCenter;

            // Initially hidden (shown when watching multiple rooms)
            _roomSwitcherContainer.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            SpectatorManager.OnConnected -= HandleConnected;
            SpectatorManager.OnGameStateUpdated -= HandleGameStateUpdated;
            SpectatorManager.OnPlayerUpdated -= HandlePlayerUpdated;
            SpectatorManager.OnChatReceived -= HandleChatReceived;
            SpectatorManager.OnSpectatorCountChanged -= HandleSpectatorCountChanged;
            SpectatorManager.OnRoundEnded -= HandleRoundEnded;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the spectator session to display.
        /// </summary>
        public void SetSession(SpectatorSession session)
        {
            _currentSession = session;

            _roomNameText.text = session.RoomName ?? "Unknown Room";
            _roomCodeText.text = session.RoomCode ?? "";
            _gameNameText.text = session.GameName ?? "Game";
            _viewerCountText.text = $"{session.SpectatorCount} watching";

            if (session.StreamDelay > 0)
            {
                _delayText.text = $"{session.StreamDelay:F0}s delay";
                _delayText.gameObject.SetActive(true);
            }
            else
            {
                _delayText.gameObject.SetActive(false);
            }

            // Update player dropdown
            UpdatePlayerDropdown(session.Players);

            // Update score panel
            _scorePanel.SetPlayers(session.Players);

            // Enable/disable chat
            _chatInput.interactable = session.ChatEnabled;
            _sendChatButton.interactable = session.ChatEnabled;

            // Update room switcher
            UpdateRoomSwitcher();
        }

        /// <summary>
        /// Set the view mode.
        /// </summary>
        public void SetViewMode(SpectatorViewMode mode, string playerId = null)
        {
            _currentViewMode = mode;
            _followedPlayerId = playerId;

            // Update button states
            var overviewBg = _overviewButton.GetComponent<Image>();
            var followBg = _followButton.GetComponent<Image>();

            if (mode == SpectatorViewMode.Overview)
            {
                overviewBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.5f, 0.8f);
                followBg.color = new Color(0.3f, 0.3f, 0.35f);
                _playerDropdown.interactable = false;
            }
            else
            {
                overviewBg.color = new Color(0.3f, 0.3f, 0.35f);
                followBg.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.5f, 0.8f);
                _playerDropdown.interactable = true;
            }

            OnViewModeChanged?.Invoke(mode);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void UpdatePlayerDropdown(List<SpectatorPlayerInfo> players)
        {
            _playerDropdown.ClearOptions();

            var options = new List<string> { "Select Player" };
            foreach (var player in players)
            {
                options.Add(player.Username);
            }

            _playerDropdown.AddOptions(options);
        }

        private void OnPlayerDropdownChanged(int index)
        {
            if (index > 0 && _currentSession != null && index <= _currentSession.Players.Count)
            {
                var player = _currentSession.Players[index - 1];
                _followedPlayerId = player.Id;
                OnPlayerSelected?.Invoke(player.Id);
            }
        }

        private void ToggleChat()
        {
            _chatVisible = !_chatVisible;
            _chatScroll.gameObject.SetActive(_chatVisible);
            _chatInput.transform.parent.gameObject.SetActive(_chatVisible);
        }

        private void OnChatSubmit(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            OnChatSent?.Invoke(message);
            _chatInput.text = "";
        }

        private void AddChatMessage(SpectatorChatMessage message)
        {
            var msgGO = new GameObject($"ChatMsg_{message.Id}");
            msgGO.transform.SetParent(_chatContent, false);

            var msgUI = msgGO.AddComponent<ChatMessageUI>();
            msgUI.Initialize(_theme);
            msgUI.SetMessage(message);

            _chatMessages.Add(msgUI);

            // Limit messages
            while (_chatMessages.Count > MAX_CHAT_MESSAGES)
            {
                var oldest = _chatMessages[0];
                _chatMessages.RemoveAt(0);
                Destroy(oldest.gameObject);
            }

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            _chatScroll.verticalNormalizedPosition = 0;
        }

        private void UpdateRoomSwitcher()
        {
            var watchedRooms = SpectatorManager.GetWatchedRooms();

            // Clear existing tabs
            foreach (var tab in _roomTabs)
            {
                Destroy(tab.gameObject);
            }
            _roomTabs.Clear();

            // Show if multiple rooms
            _roomSwitcherContainer.gameObject.SetActive(watchedRooms.Count > 1);

            if (watchedRooms.Count > 1)
            {
                foreach (var room in watchedRooms)
                {
                    var tabGO = new GameObject($"Tab_{room.RoomCode}");
                    tabGO.transform.SetParent(_roomSwitcherContainer, false);

                    var tab = tabGO.AddComponent<RoomTabUI>();
                    tab.Initialize(_theme);
                    tab.SetRoom(room);
                    tab.SetActive(room.RoomId == _currentSession?.RoomId);
                    tab.OnClicked += () => SpectatorManager.SwitchToRoom(room.RoomId);

                    _roomTabs.Add(tab);
                }
            }
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandleConnected(SpectatorSession session)
        {
            SetSession(session);
        }

        private void HandleGameStateUpdated(GameStateSnapshot state)
        {
            _roundText.text = $"Round {state.RoundNumber}";
            _phaseText.text = state.Phase ?? "In Progress";
            _potText.text = $"${state.CurrentPot:F2}";

            // Update paused state
            if (state.IsPaused)
            {
                _phaseText.text = "PAUSED";
                _phaseText.color = _theme?.WarningColor ?? new Color(1f, 0.7f, 0f);
            }
        }

        private void HandlePlayerUpdated(SpectatorPlayerInfo player)
        {
            _scorePanel.UpdatePlayer(player);
        }

        private void HandleChatReceived(SpectatorChatMessage message)
        {
            AddChatMessage(message);
        }

        private void HandleSpectatorCountChanged(int count)
        {
            _viewerCountText.text = $"{count} watching";
        }

        private void HandleRoundEnded(SpectatorRoundEnd roundEnd)
        {
            _scorePanel.HighlightWinner(roundEnd.WinnerId);
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_roomCodeText != null) _roomCodeText.color = theme.TextSecondary;
            if (_viewerCountText != null) _viewerCountText.color = theme.TextSecondary;
            if (_phaseText != null) _phaseText.color = theme.AccentColor;
            if (_potText != null) _potText.color = theme.SuccessColor;
            if (_delayText != null) _delayText.color = theme.WarningColor;

            _scorePanel?.ApplyTheme(theme);
        }
    }

    /// <summary>
    /// Chat message display component.
    /// </summary>
    public class ChatMessageUI : UIPanel
    {
        private TextMeshProUGUI _usernameText;
        private TextMeshProUGUI _messageText;

        protected override void SetupLayout()
        {
            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = false;

            _usernameText = UIComponents.CreateText(transform, "User:", 11);
            _usernameText.fontStyle = FontStyles.Bold;

            _messageText = UIComponents.CreateText(transform, "Message", 11);
        }

        public void SetMessage(SpectatorChatMessage message)
        {
            _usernameText.text = $"{message.Username}:";
            _usernameText.color = message.IsModerator 
                ? (_theme?.WarningColor ?? new Color(1f, 0.7f, 0f))
                : (_theme?.AccentColor ?? new Color(0.4f, 0.8f, 1f));

            _messageText.text = message.Content;
        }
    }

    /// <summary>
    /// Room tab for multi-room spectating.
    /// </summary>
    public class RoomTabUI : UIPanel
    {
        public event Action OnClicked;

        private Image _background;
        private TextMeshProUGUI _nameText;
        private Button _button;

        protected override void SetupLayout()
        {
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.2f, 0.2f, 0.25f);

            var layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 90;
            layout.preferredHeight = 26;

            _button = gameObject.AddComponent<Button>();
            _button.onClick.AddListener(() => OnClicked?.Invoke());

            _nameText = UIComponents.CreateText(transform, "Room", 11);
            _nameText.alignment = TextAlignmentOptions.Center;

            var textRect = _nameText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
        }

        public void SetRoom(SpectatorSession session)
        {
            _nameText.text = session.RoomCode;
        }

        public void SetActive(bool active)
        {
            _background.color = active 
                ? (_theme?.PrimaryColor ?? new Color(0.2f, 0.5f, 0.8f))
                : new Color(0.2f, 0.2f, 0.25f);
        }
    }
}
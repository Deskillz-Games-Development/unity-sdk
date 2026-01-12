// =============================================================================
// Deskillz SDK for Unity - Host Spectator View UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
// HOST-ONLY FEATURE: Main UI for hosts monitoring their private social rooms.
// Only the room creator can use this view.
// Shows board state and scores but NOT player hands (anti-cheat protection).
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Deskillz.UI.Host
{
    /// <summary>
    /// Main UI for host spectator mode.
    /// Provides room monitoring, score tracking, and multi-room switching.
    /// 
    /// IMPORTANT: This is a HOST-ONLY feature.
    /// - Only YOU (the room creator) can use this view
    /// - You can see: game board, scores, turn indicator, chat
    /// - You CANNOT see: player hands, hidden cards (anti-cheat)
    /// - Use for managing multiple social game rooms
    /// </summary>
    public class HostSpectatorViewUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when exit button is clicked</summary>
        public event Action OnExitClicked;

        /// <summary>Called when switching to another room</summary>
        public event Action<string> OnRoomSwitchRequested;

        /// <summary>Called when chat message is sent</summary>
        public event Action<string> OnChatSent;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        // Header
        private RectTransform _headerContainer;
        private TextMeshProUGUI _roomNameText;
        private TextMeshProUGUI _roomCodeText;
        private TextMeshProUGUI _hostBadgeText;
        private Button _exitButton;

        // Game info bar
        private TextMeshProUGUI _gameNameText;
        private TextMeshProUGUI _roundText;
        private TextMeshProUGUI _potText;
        private TextMeshProUGUI _phaseText;
        private TextMeshProUGUI _turnTimerText;

        // Score panel
        private HostSpectatorScorePanelUI _scorePanel;

        // Room switcher (for multi-room hosting)
        private HostRoomSwitcherUI _roomSwitcher;

        // Game board area
        private RectTransform _gameBoardContainer;
        private TextMeshProUGUI _boardStateText;

        // Chat panel
        private ScrollRect _chatScrollRect;
        private RectTransform _chatContent;
        private TMP_InputField _chatInput;
        private Button _chatSendButton;

        // Anti-cheat notice
        private RectTransform _antiCheatNotice;
        private TextMeshProUGUI _antiCheatText;

        // =====================================================================
        // STATE
        // =====================================================================

        private HostSpectatorSession _currentSession;
        private List<HostChatMessage> _chatMessages = new List<HostChatMessage>();
        private DeskillzTheme _theme;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Setup container
            var container = CreateContainer();

            // Create header with host badge
            CreateHeader(container);

            // Create anti-cheat notice
            CreateAntiCheatNotice(container);

            // Create game info bar
            CreateGameInfoBar(container);

            // Create main content area
            CreateMainContent(container);

            // Create chat panel
            CreateChatPanel(container);

            // Subscribe to events
            SubscribeToEvents();
        }

        private void CreateHeader(RectTransform parent)
        {
            _headerContainer = CreatePanel(parent, "Header", new Vector2(0, 1), new Vector2(1, 1));
            _headerContainer.sizeDelta = new Vector2(0, 60);
            _headerContainer.anchoredPosition = new Vector2(0, -30);

            // Host badge
            _hostBadgeText = CreateText(_headerContainer, "HOST MONITORING", 12);
            _hostBadgeText.color = new Color(1f, 0.8f, 0.2f); // Gold color
            _hostBadgeText.fontStyle = FontStyles.Bold;

            // Room name
            _roomNameText = CreateText(_headerContainer, "Room Name", 18);
            _roomNameText.fontStyle = FontStyles.Bold;

            // Room code
            _roomCodeText = CreateText(_headerContainer, "DSKZ-XXXX", 14);
            _roomCodeText.color = Color.gray;

            // Exit button
            _exitButton = CreateButton(_headerContainer, "Exit", OnExitButtonClicked);
        }

        private void CreateAntiCheatNotice(RectTransform parent)
        {
            _antiCheatNotice = CreatePanel(parent, "AntiCheatNotice", new Vector2(0, 1), new Vector2(1, 1));
            _antiCheatNotice.sizeDelta = new Vector2(0, 30);
            _antiCheatNotice.anchoredPosition = new Vector2(0, -75);

            var bg = _antiCheatNotice.gameObject.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            _antiCheatText = CreateText(_antiCheatNotice, 
                "HOST VIEW: You can see scores and board state. Player hands are HIDDEN for fair play.", 
                11);
            _antiCheatText.color = new Color(1f, 0.9f, 0.5f);
            _antiCheatText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateGameInfoBar(RectTransform parent)
        {
            var infoBar = CreatePanel(parent, "InfoBar", new Vector2(0, 1), new Vector2(1, 1));
            infoBar.sizeDelta = new Vector2(0, 40);
            infoBar.anchoredPosition = new Vector2(0, -115);

            _gameNameText = CreateText(infoBar, "Game Name", 14);
            _roundText = CreateText(infoBar, "Round 1/10", 14);
            _potText = CreateText(infoBar, "Pot: $0.00", 14);
            _phaseText = CreateText(infoBar, "Phase: Waiting", 14);
            _turnTimerText = CreateText(infoBar, "Turn: --", 14);
        }

        private void CreateMainContent(RectTransform parent)
        {
            var mainArea = CreatePanel(parent, "MainContent", new Vector2(0, 0), new Vector2(1, 1));
            mainArea.offsetMin = new Vector2(10, 200);
            mainArea.offsetMax = new Vector2(-10, -130);

            // Room switcher (top)
            _roomSwitcher = CreateRoomSwitcher(mainArea);

            // Score panel (left side)
            var scorePanelObj = new GameObject("ScorePanel");
            scorePanelObj.transform.SetParent(mainArea, false);
            _scorePanel = scorePanelObj.AddComponent<HostSpectatorScorePanelUI>();

            // Game board container (center)
            _gameBoardContainer = CreatePanel(mainArea, "GameBoard", new Vector2(0.3f, 0), new Vector2(1, 1));
            
            _boardStateText = CreateText(_gameBoardContainer, "Game board state will appear here.\n\nNote: Player hands are not visible.", 14);
            _boardStateText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateChatPanel(RectTransform parent)
        {
            var chatPanel = CreatePanel(parent, "ChatPanel", new Vector2(0, 0), new Vector2(1, 0));
            chatPanel.sizeDelta = new Vector2(0, 180);
            chatPanel.anchoredPosition = new Vector2(0, 90);

            // Chat scroll view
            var scrollObj = new GameObject("ChatScroll");
            scrollObj.transform.SetParent(chatPanel, false);
            _chatScrollRect = scrollObj.AddComponent<ScrollRect>();

            _chatContent = CreatePanel(chatPanel, "ChatContent", Vector2.zero, Vector2.one);

            // Chat input
            var inputObj = new GameObject("ChatInput");
            inputObj.transform.SetParent(chatPanel, false);
            _chatInput = inputObj.AddComponent<TMP_InputField>();
            _chatInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Send message to room...";

            _chatSendButton = CreateButton(chatPanel, "Send", OnChatSendClicked);
        }

        private HostRoomSwitcherUI CreateRoomSwitcher(RectTransform parent)
        {
            var switcherObj = new GameObject("RoomSwitcher");
            switcherObj.transform.SetParent(parent, false);
            return switcherObj.AddComponent<HostRoomSwitcherUI>();
        }

        // =====================================================================
        // EVENT SUBSCRIPTIONS
        // =====================================================================

        private void SubscribeToEvents()
        {
            HostSpectatorManager.OnGameStateUpdated += OnGameStateUpdated;
            HostSpectatorManager.OnScoreUpdated += OnScoreUpdated;
            HostSpectatorManager.OnRoundStarted += OnRoundStarted;
            HostSpectatorManager.OnRoundEnded += OnRoundEnded;
            HostSpectatorManager.OnTurnChanged += OnTurnChanged;
            HostSpectatorManager.OnChatReceived += OnChatReceived;
            HostSpectatorManager.OnRoomSwitched += OnRoomSwitched;
            HostSpectatorManager.OnGamePaused += OnGamePaused;
            HostSpectatorManager.OnGameResumed += OnGameResumed;
        }

        private void UnsubscribeFromEvents()
        {
            HostSpectatorManager.OnGameStateUpdated -= OnGameStateUpdated;
            HostSpectatorManager.OnScoreUpdated -= OnScoreUpdated;
            HostSpectatorManager.OnRoundStarted -= OnRoundStarted;
            HostSpectatorManager.OnRoundEnded -= OnRoundEnded;
            HostSpectatorManager.OnTurnChanged -= OnTurnChanged;
            HostSpectatorManager.OnChatReceived -= OnChatReceived;
            HostSpectatorManager.OnRoomSwitched -= OnRoomSwitched;
            HostSpectatorManager.OnGamePaused -= OnGamePaused;
            HostSpectatorManager.OnGameResumed -= OnGameResumed;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Initialize the view with a host spectator session.
        /// </summary>
        /// <param name="session">Your room's spectator session</param>
        public void Initialize(HostSpectatorSession session)
        {
            _currentSession = session;
            UpdateRoomInfo();
            UpdateGameState(session.CurrentState);
            UpdatePlayers(session.Players);
        }

        /// <summary>
        /// Update the displayed game state.
        /// Note: Player hands are NOT included (anti-cheat).
        /// </summary>
        /// <param name="state">Current game state snapshot</param>
        public void UpdateGameState(HostGameStateSnapshot state)
        {
            if (state == null) return;

            _roundText.text = $"Round {state.CurrentRound}/{state.TotalRounds}";
            _potText.text = $"Pot: ${state.CurrentPot:F2}";
            _phaseText.text = $"Phase: {state.Phase}";
            
            if (state.TurnTimeRemaining > 0)
            {
                _turnTimerText.text = $"Turn: {state.TurnTimeRemaining:F0}s";
            }
            else
            {
                _turnTimerText.text = "Turn: --";
            }

            // Update board state display (no hands visible)
            if (!string.IsNullOrEmpty(state.BoardState))
            {
                _boardStateText.text = state.BoardState;
            }

            // Update scores
            if (state.Scores != null)
            {
                _scorePanel?.UpdateScores(state.Scores);
            }
        }

        /// <summary>
        /// Update the player list.
        /// Note: Player hands/cards are NOT visible.
        /// </summary>
        /// <param name="players">List of players (public info only)</param>
        public void UpdatePlayers(List<HostPlayerInfo> players)
        {
            _scorePanel?.SetPlayers(players);
        }

        /// <summary>
        /// Add rooms to the room switcher for multi-room hosting.
        /// </summary>
        /// <param name="rooms">Your other active rooms</param>
        public void SetAvailableRooms(List<HostRoom> rooms)
        {
            _roomSwitcher?.SetRooms(rooms, _currentSession?.RoomId);
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnGameStateUpdated(HostGameStateSnapshot state)
        {
            UpdateGameState(state);
        }

        private void OnScoreUpdated(HostScoreUpdate update)
        {
            _scorePanel?.UpdatePlayerScore(update.PlayerId, update.NewScore);
        }

        private void OnRoundStarted(int roundNumber)
        {
            _roundText.text = $"Round {roundNumber}/{_currentSession?.CurrentState?.TotalRounds ?? 0}";
        }

        private void OnRoundEnded(HostRoundResult result)
        {
            // Show round result overlay
            ShowRoundResult(result);
        }

        private void OnTurnChanged(string playerId)
        {
            _scorePanel?.HighlightPlayer(playerId);
        }

        private void OnChatReceived(HostChatMessage message)
        {
            AddChatMessage(message);
        }

        private void OnRoomSwitched(HostSpectatorSession newSession)
        {
            _currentSession = newSession;
            UpdateRoomInfo();
            UpdateGameState(newSession.CurrentState);
            UpdatePlayers(newSession.Players);
        }

        private void OnGamePaused(float duration)
        {
            _phaseText.text = $"PAUSED ({duration:F0}s)";
        }

        private void OnGameResumed()
        {
            // Phase will be updated by next state update
        }

        // =====================================================================
        // UI HELPERS
        // =====================================================================

        private void UpdateRoomInfo()
        {
            if (_currentSession == null) return;

            _roomNameText.text = _currentSession.RoomName;
            _roomCodeText.text = _currentSession.RoomCode;
            _gameNameText.text = _currentSession.GameName;
        }

        private void AddChatMessage(HostChatMessage message)
        {
            _chatMessages.Add(message);

            // Create chat message UI
            var msgText = CreateText(_chatContent, $"[{message.SenderUsername}]: {message.Content}", 12);
            msgText.alignment = TextAlignmentOptions.Left;

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            _chatScrollRect.verticalNormalizedPosition = 0;
        }

        private void ShowRoundResult(HostRoundResult result)
        {
            // Display round result overlay
            DeskillzLogger.Debug($"Round {result.RoundNumber} ended. Winner: {result.WinnerUsername}, Pot: ${result.PotWon:F2}");
        }

        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================

        private void OnExitButtonClicked()
        {
            OnExitClicked?.Invoke();
        }

        private void OnChatSendClicked()
        {
            var message = _chatInput.text?.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                OnChatSent?.Invoke(message);
                _chatInput.text = "";
            }
        }

        // =====================================================================
        // UI CREATION HELPERS
        // =====================================================================

        private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private TextMeshProUGUI CreateText(RectTransform parent, string text, int fontSize)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            return tmp;
        }

        private Button CreateButton(RectTransform parent, string label, Action onClick)
        {
            var obj = new GameObject(label + "Button");
            obj.transform.SetParent(parent, false);
            var btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            var text = CreateText(obj.GetComponent<RectTransform>(), label, 14);
            return btn;
        }

        private RectTransform CreateContainer()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Room switcher UI for multi-room host monitoring.
    /// </summary>
    public class HostRoomSwitcherUI : MonoBehaviour
    {
        public event Action<string> OnRoomSelected;

        private List<HostRoom> _rooms = new List<HostRoom>();
        private string _currentRoomId;

        public void SetRooms(List<HostRoom> rooms, string currentRoomId)
        {
            _rooms = rooms ?? new List<HostRoom>();
            _currentRoomId = currentRoomId;
            RebuildUI();
        }

        private void RebuildUI()
        {
            // Clear existing buttons
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Create room tabs
            foreach (var room in _rooms)
            {
                CreateRoomTab(room);
            }
        }

        private void CreateRoomTab(HostRoom room)
        {
            var tabObj = new GameObject(room.Name);
            tabObj.transform.SetParent(transform, false);

            var btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnRoomSelected?.Invoke(room.Id));

            var text = tabObj.AddComponent<TextMeshProUGUI>();
            text.text = $"{room.Name} ({room.CurrentPlayers}/{room.MaxPlayers})";
            text.fontSize = 12;
            text.color = room.Id == _currentRoomId ? Color.yellow : Color.white;
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Room Switcher UI
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
    /// UI component for switching between multiple spectated rooms.
    /// Displays room tabs with live status indicators and quick switching.
    /// </summary>
    public class RoomSwitcherUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when a room tab is selected</summary>
        public event Action<string> OnRoomSelected;

        /// <summary>Called when add room button is clicked</summary>
        public event Action OnAddRoomClicked;

        /// <summary>Called when a room is closed</summary>
        public event Action<string> OnRoomClosed;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private RectTransform _tabsContainer;
        private ScrollRect _tabsScroll;
        private Button _addRoomButton;
        private TextMeshProUGUI _roomCountText;

        private List<RoomTabItemUI> _tabs = new List<RoomTabItemUI>();

        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        [Header("Settings")]
        public int MaxRooms = 4;
        public float TabWidth = 140f;
        public float TabSpacing = 8f;
        public bool ShowAddButton = true;
        public bool AllowClose = true;

        // =====================================================================
        // STATE
        // =====================================================================

        private string _activeRoomId;
        private Dictionary<string, SpectatorSession> _rooms = new Dictionary<string, SpectatorSession>();

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.sizeDelta = new Vector2(0, 56);

            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // Main layout
            var mainLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
            mainLayout.padding = new RectOffset(12, 12, 8, 8);
            mainLayout.spacing = 12;
            mainLayout.childAlignment = TextAnchor.MiddleLeft;
            mainLayout.childForceExpandWidth = false;
            mainLayout.childForceExpandHeight = true;

            // Room count label
            CreateRoomCountLabel();

            // Tabs scroll area
            CreateTabsScrollArea();

            // Add room button
            if (ShowAddButton)
            {
                CreateAddRoomButton();
            }

            // Subscribe to spectator events
            SpectatorManager.OnConnected += HandleRoomConnected;
            SpectatorManager.OnDisconnected += HandleRoomDisconnected;
            SpectatorManager.OnSpectatorCountChanged += HandleSpectatorCountChanged;
            SpectatorManager.OnRoundStarted += HandleRoundStarted;
            SpectatorManager.OnGamePaused += HandleGamePaused;
            SpectatorManager.OnGameResumed += HandleGameResumed;
        }

        private void CreateRoomCountLabel()
        {
            var labelContainer = new GameObject("RoomCountContainer");
            labelContainer.transform.SetParent(transform, false);

            var labelLayout = labelContainer.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 60;

            var verticalLayout = labelContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.MiddleCenter;

            var label = UIComponents.CreateText(labelContainer.transform, "ROOMS", 9);
            label.alignment = TextAlignmentOptions.Center;
            label.color = _theme?.TextSecondary ?? Color.gray;

            _roomCountText = UIComponents.CreateText(labelContainer.transform, "0/4", 14);
            _roomCountText.alignment = TextAlignmentOptions.Center;
            _roomCountText.fontStyle = FontStyles.Bold;
        }

        private void CreateTabsScrollArea()
        {
            var scrollGO = new GameObject("TabsScroll");
            scrollGO.transform.SetParent(transform, false);

            var scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.flexibleWidth = 1;

            _tabsScroll = scrollGO.AddComponent<ScrollRect>();
            _tabsScroll.horizontal = true;
            _tabsScroll.vertical = false;
            _tabsScroll.scrollSensitivity = 20f;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);

            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewportGO.AddComponent<Image>().color = Color.clear;

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);

            _tabsContainer = contentGO.AddComponent<RectTransform>();
            _tabsContainer.anchorMin = new Vector2(0, 0.5f);
            _tabsContainer.anchorMax = new Vector2(0, 0.5f);
            _tabsContainer.pivot = new Vector2(0, 0.5f);

            var contentLayout = contentGO.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = TabSpacing;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            _tabsScroll.content = _tabsContainer;
            _tabsScroll.viewport = viewportRect;
        }

        private void CreateAddRoomButton()
        {
            var addGO = new GameObject("AddRoomButton");
            addGO.transform.SetParent(transform, false);

            var addLayout = addGO.AddComponent<LayoutElement>();
            addLayout.preferredWidth = 40;
            addLayout.preferredHeight = 40;

            var addBg = addGO.AddComponent<Image>();
            addBg.color = new Color(0.2f, 0.5f, 0.3f);

            _addRoomButton = addGO.AddComponent<Button>();
            _addRoomButton.onClick.AddListener(OnAddButtonClicked);

            var addText = UIComponents.CreateText(addGO.transform, "+", 24);
            addText.alignment = TextAlignmentOptions.Center;
            addText.fontStyle = FontStyles.Bold;

            var textRect = addText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
        }

        private void OnDestroy()
        {
            SpectatorManager.OnConnected -= HandleRoomConnected;
            SpectatorManager.OnDisconnected -= HandleRoomDisconnected;
            SpectatorManager.OnSpectatorCountChanged -= HandleSpectatorCountChanged;
            SpectatorManager.OnRoundStarted -= HandleRoundStarted;
            SpectatorManager.OnGamePaused -= HandleGamePaused;
            SpectatorManager.OnGameResumed -= HandleGameResumed;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Add a room to the switcher.
        /// </summary>
        public void AddRoom(SpectatorSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.RoomId))
                return;

            if (_rooms.ContainsKey(session.RoomId))
            {
                // Update existing
                _rooms[session.RoomId] = session;
                UpdateTab(session.RoomId);
                return;
            }

            if (_rooms.Count >= MaxRooms)
            {
                Debug.LogWarning($"[RoomSwitcher] Max rooms ({MaxRooms}) reached");
                return;
            }

            _rooms[session.RoomId] = session;
            CreateTab(session);
            UpdateRoomCount();
            UpdateAddButton();

            // Auto-select if first room
            if (_rooms.Count == 1)
            {
                SetActiveRoom(session.RoomId);
            }
        }

        /// <summary>
        /// Remove a room from the switcher.
        /// </summary>
        public void RemoveRoom(string roomId)
        {
            if (!_rooms.ContainsKey(roomId))
                return;

            _rooms.Remove(roomId);
            RemoveTab(roomId);
            UpdateRoomCount();
            UpdateAddButton();

            // Select another room if active was removed
            if (_activeRoomId == roomId && _rooms.Count > 0)
            {
                var enumerator = _rooms.Keys.GetEnumerator();
                enumerator.MoveNext();
                SetActiveRoom(enumerator.Current);
            }
        }

        /// <summary>
        /// Set the active room.
        /// </summary>
        public void SetActiveRoom(string roomId)
        {
            if (!_rooms.ContainsKey(roomId))
                return;

            _activeRoomId = roomId;

            // Update tab states
            foreach (var tab in _tabs)
            {
                tab.SetActive(tab.RoomId == roomId);
            }

            OnRoomSelected?.Invoke(roomId);
        }

        /// <summary>
        /// Get all room IDs.
        /// </summary>
        public List<string> GetRoomIds()
        {
            return new List<string>(_rooms.Keys);
        }

        /// <summary>
        /// Check if a room exists.
        /// </summary>
        public bool HasRoom(string roomId)
        {
            return _rooms.ContainsKey(roomId);
        }

        /// <summary>
        /// Get room count.
        /// </summary>
        public int RoomCount => _rooms.Count;

        // =====================================================================
        // TAB MANAGEMENT
        // =====================================================================

        private void CreateTab(SpectatorSession session)
        {
            var tabGO = new GameObject($"Tab_{session.RoomId}");
            tabGO.transform.SetParent(_tabsContainer, false);

            var tab = tabGO.AddComponent<RoomTabItemUI>();
            tab.Initialize(_theme);
            tab.SetRoom(session, session.RoomId == _activeRoomId);
            tab.AllowClose = AllowClose;

            tab.OnClicked += () => SetActiveRoom(session.RoomId);
            tab.OnCloseClicked += () =>
            {
                OnRoomClosed?.Invoke(session.RoomId);
                RemoveRoom(session.RoomId);
            };

            var layout = tabGO.AddComponent<LayoutElement>();
            layout.preferredWidth = TabWidth;

            _tabs.Add(tab);
        }

        private void UpdateTab(string roomId)
        {
            var session = _rooms[roomId];
            foreach (var tab in _tabs)
            {
                if (tab.RoomId == roomId)
                {
                    tab.UpdateSession(session);
                    break;
                }
            }
        }

        private void RemoveTab(string roomId)
        {
            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                if (_tabs[i].RoomId == roomId)
                {
                    Destroy(_tabs[i].gameObject);
                    _tabs.RemoveAt(i);
                    break;
                }
            }
        }

        private void UpdateRoomCount()
        {
            _roomCountText.text = $"{_rooms.Count}/{MaxRooms}";
        }

        private void UpdateAddButton()
        {
            if (_addRoomButton != null)
            {
                _addRoomButton.interactable = _rooms.Count < MaxRooms;
            }
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void OnAddButtonClicked()
        {
            if (_rooms.Count < MaxRooms)
            {
                OnAddRoomClicked?.Invoke();
            }
        }

        private void HandleRoomConnected(SpectatorSession session)
        {
            AddRoom(session);
        }

        private void HandleRoomDisconnected()
        {
            // Handle disconnect - could mark tab as disconnected
        }

        private void HandleSpectatorCountChanged(int count)
        {
            // Update current room's spectator count
            if (!string.IsNullOrEmpty(_activeRoomId))
            {
                foreach (var tab in _tabs)
                {
                    if (tab.RoomId == _activeRoomId)
                    {
                        tab.SetSpectatorCount(count);
                        break;
                    }
                }
            }
        }

        private void HandleRoundStarted(int roundNumber)
        {
            // Update current room's round
            if (!string.IsNullOrEmpty(_activeRoomId))
            {
                foreach (var tab in _tabs)
                {
                    if (tab.RoomId == _activeRoomId)
                    {
                        tab.SetRound(roundNumber);
                        break;
                    }
                }
            }
        }

        private void HandleGamePaused(float duration)
        {
            if (!string.IsNullOrEmpty(_activeRoomId))
            {
                foreach (var tab in _tabs)
                {
                    if (tab.RoomId == _activeRoomId)
                    {
                        tab.SetPaused(true);
                        break;
                    }
                }
            }
        }

        private void HandleGameResumed()
        {
            if (!string.IsNullOrEmpty(_activeRoomId))
            {
                foreach (var tab in _tabs)
                {
                    if (tab.RoomId == _activeRoomId)
                    {
                        tab.SetPaused(false);
                        break;
                    }
                }
            }
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null)
                _background.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            if (_roomCountText != null)
                _roomCountText.color = theme.TextPrimary;

            foreach (var tab in _tabs)
            {
                tab?.ApplyTheme(theme);
            }
        }
    }

    /// <summary>
    /// Individual room tab item UI component.
    /// </summary>
    public class RoomTabItemUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        public event Action OnClicked;
        public event Action OnCloseClicked;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        public string RoomId { get; private set; }
        public bool AllowClose { get; set; } = true;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private Image _statusIndicator;
        private TextMeshProUGUI _roomNameText;
        private TextMeshProUGUI _gameNameText;
        private TextMeshProUGUI _statusText;
        private Button _tabButton;
        private Button _closeButton;
        private Image _activeIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isActive;
        private bool _isPaused;
        private int _spectatorCount;
        private int _currentRound;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.15f, 0.15f, 0.2f);

            // Tab button
            _tabButton = gameObject.AddComponent<Button>();
            _tabButton.onClick.AddListener(() => OnClicked?.Invoke());

            // Main layout
            var mainLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
            mainLayout.padding = new RectOffset(8, 8, 6, 6);
            mainLayout.spacing = 6;
            mainLayout.childForceExpandWidth = false;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childAlignment = TextAnchor.MiddleLeft;

            // Status indicator (live dot)
            var statusGO = new GameObject("StatusIndicator");
            statusGO.transform.SetParent(transform, false);

            var statusLayout = statusGO.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 8;
            statusLayout.preferredHeight = 8;

            _statusIndicator = statusGO.AddComponent<Image>();
            _statusIndicator.color = _theme?.SuccessColor ?? Color.green;

            // Info container
            var infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(transform, false);

            var infoLayout = infoContainer.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;

            var infoVertical = infoContainer.AddComponent<VerticalLayoutGroup>();
            infoVertical.spacing = 1;
            infoVertical.childForceExpandHeight = false;

            // Room name
            _roomNameText = UIComponents.CreateText(infoContainer.transform, "DSKZ-XXXX", 11);
            _roomNameText.fontStyle = FontStyles.Bold;

            // Game name / status
            var statusRow = new GameObject("StatusRow");
            statusRow.transform.SetParent(infoContainer.transform, false);

            var statusRowLayout = statusRow.AddComponent<HorizontalLayoutGroup>();
            statusRowLayout.spacing = 4;
            statusRowLayout.childForceExpandWidth = false;

            _gameNameText = UIComponents.CreateText(statusRow.transform, "Game", 9);
            _gameNameText.color = _theme?.TextSecondary ?? Color.gray;

            _statusText = UIComponents.CreateText(statusRow.transform, "", 9);
            _statusText.color = _theme?.TextSecondary ?? Color.gray;

            // Close button
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(transform, false);

            var closeLayout = closeGO.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 20;
            closeLayout.preferredHeight = 20;

            var closeBg = closeGO.AddComponent<Image>();
            closeBg.color = new Color(0.4f, 0.2f, 0.2f, 0.8f);

            _closeButton = closeGO.AddComponent<Button>();
            _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());

            var closeText = UIComponents.CreateText(closeGO.transform, "x", 12);
            closeText.alignment = TextAlignmentOptions.Center;

            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;

            // Active indicator (bottom border)
            var activeGO = new GameObject("ActiveIndicator");
            activeGO.transform.SetParent(transform, false);

            var activeRect = activeGO.AddComponent<RectTransform>();
            activeRect.anchorMin = new Vector2(0, 0);
            activeRect.anchorMax = new Vector2(1, 0);
            activeRect.pivot = new Vector2(0.5f, 0);
            activeRect.sizeDelta = new Vector2(0, 3);

            _activeIndicator = activeGO.AddComponent<Image>();
            _activeIndicator.color = _theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f);
            _activeIndicator.gameObject.SetActive(false);
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the room data for this tab.
        /// </summary>
        public void SetRoom(SpectatorSession session, bool isActive)
        {
            RoomId = session.RoomId;
            _roomNameText.text = session.RoomCode ?? session.RoomId.Substring(0, 8);
            _gameNameText.text = session.GameName ?? "Unknown Game";
            _spectatorCount = session.SpectatorCount;
            _currentRound = session.CurrentState?.RoundNumber ?? 0;

            SetActive(isActive);
            UpdateStatusText();

            _closeButton.gameObject.SetActive(AllowClose);
        }

        /// <summary>
        /// Update session data.
        /// </summary>
        public void UpdateSession(SpectatorSession session)
        {
            _spectatorCount = session.SpectatorCount;
            _currentRound = session.CurrentState?.RoundNumber ?? 0;
            _isPaused = session.CurrentState?.IsPaused ?? false;
            UpdateStatusText();
        }

        /// <summary>
        /// Set whether this tab is active.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;

            _background.color = active 
                ? (_theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f)) 
                : new Color(0.15f, 0.15f, 0.2f);

            _activeIndicator.gameObject.SetActive(active);
        }

        /// <summary>
        /// Set spectator count.
        /// </summary>
        public void SetSpectatorCount(int count)
        {
            _spectatorCount = count;
            UpdateStatusText();
        }

        /// <summary>
        /// Set current round.
        /// </summary>
        public void SetRound(int round)
        {
            _currentRound = round;
            UpdateStatusText();
        }

        /// <summary>
        /// Set paused state.
        /// </summary>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            _statusIndicator.color = paused 
                ? (_theme?.WarningColor ?? new Color(1f, 0.7f, 0f))
                : (_theme?.SuccessColor ?? Color.green);
            UpdateStatusText();
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void UpdateStatusText()
        {
            if (_isPaused)
            {
                _statusText.text = "PAUSED";
                _statusText.color = _theme?.WarningColor ?? new Color(1f, 0.7f, 0f);
            }
            else if (_currentRound > 0)
            {
                _statusText.text = $"R{_currentRound} | {_spectatorCount}";
                _statusText.color = _theme?.TextSecondary ?? Color.gray;
            }
            else
            {
                _statusText.text = $"{_spectatorCount} watching";
                _statusText.color = _theme?.TextSecondary ?? Color.gray;
            }
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (!_isActive && _background != null)
                _background.color = new Color(0.15f, 0.15f, 0.2f);

            if (_roomNameText != null)
                _roomNameText.color = theme.TextPrimary;

            if (_gameNameText != null)
                _gameNameText.color = theme.TextSecondary;

            if (_activeIndicator != null)
                _activeIndicator.color = theme.PrimaryColor;

            if (_statusIndicator != null && !_isPaused)
                _statusIndicator.color = theme.SuccessColor;
        }
    }
}
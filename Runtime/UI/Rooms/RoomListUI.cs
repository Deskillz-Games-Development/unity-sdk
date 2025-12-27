// =============================================================================
// Deskillz SDK for Unity - Room List UI
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
    /// UI panel for browsing public rooms.
    /// Shows a list of available rooms with filtering and sorting options.
    /// </summary>
    public class RoomListUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when create room button is clicked</summary>
        public event Action OnCreateRoomClicked;

        /// <summary>Called when join by code button is clicked</summary>
        public event Action OnJoinByCodeClicked;

        /// <summary>Called when a room is selected from the list</summary>
        public event Action<PrivateRoom> OnRoomSelected;

        /// <summary>Called when back button is clicked</summary>
        public event Action OnBackClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _container;
        private TextMeshProUGUI _titleText;
        private Button _backButton;
        private Button _createButton;
        private Button _joinCodeButton;
        private Button _refreshButton;
        private TMP_InputField _searchInput;
        private TMP_Dropdown _sortDropdown;
        private ScrollRect _scrollRect;
        private RectTransform _listContent;
        private TextMeshProUGUI _emptyText;
        private GameObject _loadingIndicator;

        // =====================================================================
        // STATE
        // =====================================================================

        private List<PrivateRoom> _rooms = new List<PrivateRoom>();
        private List<RoomCardUI> _roomCards = new List<RoomCardUI>();
        private string _searchFilter = "";
        private SortOption _currentSort = SortOption.Newest;
        private bool _isLoading;

        // =====================================================================
        // ENUMS
        // =====================================================================

        private enum SortOption
        {
            Newest,
            EntryFeeAsc,
            EntryFeeDesc,
            PlayersAsc,
            PlayersDesc
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Setup full-screen container
            _container = CreateContainer();

            // Header
            CreateHeader();

            // Search & Filter bar
            CreateSearchBar();

            // Room list
            CreateRoomList();

            // Empty state
            CreateEmptyState();

            // Loading indicator
            CreateLoadingIndicator();

            // Action buttons
            CreateActionButtons();
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
            header.sizeDelta = new Vector2(0, 60);
            header.anchoredPosition = Vector2.zero;

            // Back button
            _backButton = UIComponents.CreateButton(header, "Back", "<", new Vector2(50, 40));
            _backButton.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
            _backButton.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
            _backButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(35, 0);
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

            // Title
            _titleText = UIComponents.CreateText(header, "Title", "Private Rooms", 24, TextAlignmentOptions.Center);
            _titleText.rectTransform.anchorMin = new Vector2(0.2f, 0);
            _titleText.rectTransform.anchorMax = new Vector2(0.8f, 1);
            _titleText.rectTransform.sizeDelta = Vector2.zero;
            _titleText.fontStyle = FontStyles.Bold;

            // Refresh button
            _refreshButton = UIComponents.CreateButton(header, "Refresh", "↻", new Vector2(50, 40));
            _refreshButton.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
            _refreshButton.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
            _refreshButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-35, 0);
            _refreshButton.onClick.AddListener(RefreshRooms);
        }

        private void CreateSearchBar()
        {
            var searchBar = new GameObject("SearchBar").AddComponent<RectTransform>();
            searchBar.SetParent(_container, false);
            searchBar.anchorMin = new Vector2(0, 1);
            searchBar.anchorMax = new Vector2(1, 1);
            searchBar.pivot = new Vector2(0.5f, 1);
            searchBar.sizeDelta = new Vector2(0, 50);
            searchBar.anchoredPosition = new Vector2(0, -70);

            var layout = searchBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Search input
            _searchInput = UIComponents.CreateInputField(searchBar, "SearchInput", "Search rooms...", new Vector2(300, 40));
            _searchInput.onValueChanged.AddListener(OnSearchChanged);

            // Sort dropdown
            _sortDropdown = UIComponents.CreateDropdown(searchBar, "SortDropdown", new Vector2(150, 40));
            _sortDropdown.options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Newest"),
                new TMP_Dropdown.OptionData("Entry: Low-High"),
                new TMP_Dropdown.OptionData("Entry: High-Low"),
                new TMP_Dropdown.OptionData("Players: Low-High"),
                new TMP_Dropdown.OptionData("Players: High-Low")
            };
            _sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }

        private void CreateRoomList()
        {
            var listContainer = new GameObject("ListContainer").AddComponent<RectTransform>();
            listContainer.SetParent(_container, false);
            listContainer.anchorMin = new Vector2(0, 0);
            listContainer.anchorMax = new Vector2(1, 1);
            listContainer.offsetMin = new Vector2(10, 80);
            listContainer.offsetMax = new Vector2(-10, -130);

            _scrollRect = listContainer.gameObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.scrollSensitivity = 20;

            // Viewport
            var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
            viewport.SetParent(listContainer, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = Vector2.zero;
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = viewport;

            // Content
            _listContent = new GameObject("Content").AddComponent<RectTransform>();
            _listContent.SetParent(viewport, false);
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);
            _listContent.pivot = new Vector2(0.5f, 1);
            _listContent.sizeDelta = new Vector2(0, 0);

            var contentLayout = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;

            var fitter = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _listContent;
        }

        private void CreateEmptyState()
        {
            var emptyContainer = new GameObject("EmptyState").AddComponent<RectTransform>();
            emptyContainer.SetParent(_container, false);
            emptyContainer.anchorMin = new Vector2(0.5f, 0.5f);
            emptyContainer.anchorMax = new Vector2(0.5f, 0.5f);
            emptyContainer.sizeDelta = new Vector2(400, 200);

            _emptyText = UIComponents.CreateText(emptyContainer, "EmptyText", 
                "No rooms available.\n\nCreate a new room or join by code!", 
                18, TextAlignmentOptions.Center);
            _emptyText.rectTransform.anchorMin = Vector2.zero;
            _emptyText.rectTransform.anchorMax = Vector2.one;
            _emptyText.rectTransform.sizeDelta = Vector2.zero;
            _emptyText.color = new Color(0.6f, 0.6f, 0.6f);

            emptyContainer.gameObject.SetActive(false);
        }

        private void CreateLoadingIndicator()
        {
            _loadingIndicator = new GameObject("Loading");
            var loadingRect = _loadingIndicator.AddComponent<RectTransform>();
            loadingRect.SetParent(_container, false);
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.sizeDelta = new Vector2(100, 100);

            var loadingText = UIComponents.CreateText(loadingRect, "LoadingText", "Loading...", 16, TextAlignmentOptions.Center);
            loadingText.rectTransform.anchorMin = Vector2.zero;
            loadingText.rectTransform.anchorMax = Vector2.one;
            loadingText.rectTransform.sizeDelta = Vector2.zero;

            _loadingIndicator.SetActive(false);
        }

        private void CreateActionButtons()
        {
            var buttonBar = new GameObject("ButtonBar").AddComponent<RectTransform>();
            buttonBar.SetParent(_container, false);
            buttonBar.anchorMin = new Vector2(0, 0);
            buttonBar.anchorMax = new Vector2(1, 0);
            buttonBar.pivot = new Vector2(0.5f, 0);
            buttonBar.sizeDelta = new Vector2(0, 60);
            buttonBar.anchoredPosition = Vector2.zero;

            var layout = buttonBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Create Room button
            _createButton = UIComponents.CreateButton(buttonBar, "CreateButton", "Create Room", new Vector2(200, 45));
            _createButton.onClick.AddListener(() => OnCreateRoomClicked?.Invoke());
            UIComponents.SetButtonColor(_createButton, new Color(0.2f, 0.7f, 0.3f));

            // Join by Code button
            _joinCodeButton = UIComponents.CreateButton(buttonBar, "JoinCodeButton", "Join by Code", new Vector2(200, 45));
            _joinCodeButton.onClick.AddListener(() => OnJoinByCodeClicked?.Invoke());
            UIComponents.SetButtonColor(_joinCodeButton, new Color(0.3f, 0.5f, 0.9f));
        }

        // =====================================================================
        // SHOW / HIDE
        // =====================================================================

        public override void Show()
        {
            base.Show();
            RefreshRooms();
        }

        // =====================================================================
        // DATA MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Refresh the room list from the server.
        /// </summary>
        public void RefreshRooms()
        {
            if (_isLoading) return;

            SetLoading(true);

            DeskillzRooms.GetPublicRooms(
                rooms =>
                {
                    _rooms = rooms ?? new List<PrivateRoom>();
                    ApplyFiltersAndSort();
                    SetLoading(false);
                },
                error =>
                {
                    DeskillzLogger.Error($"[RoomListUI] Failed to load rooms: {error.Message}");
                    _rooms.Clear();
                    UpdateRoomCards();
                    SetLoading(false);
                });
        }

        private void ApplyFiltersAndSort()
        {
            var filtered = new List<PrivateRoom>(_rooms);

            // Apply search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.FindAll(r =>
                    r.Name.ToLower().Contains(_searchFilter.ToLower()) ||
                    r.RoomCode.ToLower().Contains(_searchFilter.ToLower()) ||
                    r.Host?.Username?.ToLower().Contains(_searchFilter.ToLower()) == true
                );
            }

            // Apply sort
            switch (_currentSort)
            {
                case SortOption.Newest:
                    filtered.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                    break;
                case SortOption.EntryFeeAsc:
                    filtered.Sort((a, b) => a.EntryFee.CompareTo(b.EntryFee));
                    break;
                case SortOption.EntryFeeDesc:
                    filtered.Sort((a, b) => b.EntryFee.CompareTo(a.EntryFee));
                    break;
                case SortOption.PlayersAsc:
                    filtered.Sort((a, b) => a.CurrentPlayers.CompareTo(b.CurrentPlayers));
                    break;
                case SortOption.PlayersDesc:
                    filtered.Sort((a, b) => b.CurrentPlayers.CompareTo(a.CurrentPlayers));
                    break;
            }

            UpdateRoomCards(filtered);
        }

        private void UpdateRoomCards(List<PrivateRoom> rooms = null)
        {
            rooms = rooms ?? _rooms;

            // Clear existing cards
            foreach (var card in _roomCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _roomCards.Clear();

            // Show empty state if no rooms
            _emptyText.transform.parent.gameObject.SetActive(rooms.Count == 0);
            _scrollRect.gameObject.SetActive(rooms.Count > 0);

            // Create new cards
            foreach (var room in rooms)
            {
                var card = CreateRoomCard(room);
                _roomCards.Add(card);
            }
        }

        private RoomCardUI CreateRoomCard(PrivateRoom room)
        {
            var cardGo = new GameObject($"RoomCard_{room.RoomCode}");
            cardGo.transform.SetParent(_listContent, false);

            var card = cardGo.AddComponent<RoomCardUI>();
            card.Initialize(_theme);
            card.SetRoom(room);
            card.OnJoinClicked += () => OnRoomSelected?.Invoke(room);

            return card;
        }

        // =====================================================================
        // UI CALLBACKS
        // =====================================================================

        private void OnSearchChanged(string value)
        {
            _searchFilter = value;
            ApplyFiltersAndSort();
        }

        private void OnSortChanged(int index)
        {
            _currentSort = (SortOption)index;
            ApplyFiltersAndSort();
        }

        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            _loadingIndicator?.SetActive(loading);
            _refreshButton.interactable = !loading;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        protected override void ApplyTheme(DeskillzTheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            // Apply theme to buttons, text, etc.
            _titleText.color = theme.TextPrimary;
            
            foreach (var card in _roomCards)
            {
                card?.ApplyTheme(theme);
            }
        }
    }

    /// <summary>
    /// Individual room card in the list.
    /// </summary>
    public class RoomCardUI : MonoBehaviour
    {
        public event Action OnJoinClicked;

        private RectTransform _rectTransform;
        private PrivateRoom _room;
        private DeskillzTheme _theme;

        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _codeText;
        private TextMeshProUGUI _hostText;
        private TextMeshProUGUI _playersText;
        private TextMeshProUGUI _entryFeeText;
        private Button _joinButton;

        public void Initialize(DeskillzTheme theme)
        {
            _theme = theme;
            _rectTransform = gameObject.AddComponent<RectTransform>();

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 100;
            layoutElement.minHeight = 100;

            SetupLayout();
        }

        private void SetupLayout()
        {
            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.18f);

            // Card layout
            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.spacing = 15;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Info section
            var infoSection = new GameObject("Info").AddComponent<RectTransform>();
            infoSection.SetParent(_rectTransform, false);
            var infoLayout = infoSection.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 4;
            infoLayout.childForceExpandHeight = false;
            var infoLayoutElement = infoSection.gameObject.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1;

            _nameText = UIComponents.CreateText(infoSection, "Name", "Room Name", 18, TextAlignmentOptions.Left);
            _nameText.fontStyle = FontStyles.Bold;

            _hostText = UIComponents.CreateText(infoSection, "Host", "Host: username", 14, TextAlignmentOptions.Left);
            _hostText.color = new Color(0.7f, 0.7f, 0.7f);

            _codeText = UIComponents.CreateText(infoSection, "Code", "DSKZ-XXXX", 12, TextAlignmentOptions.Left);
            _codeText.color = new Color(0.5f, 0.8f, 1f);

            // Stats section
            var statsSection = new GameObject("Stats").AddComponent<RectTransform>();
            statsSection.SetParent(_rectTransform, false);
            var statsLayout = statsSection.gameObject.AddComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 4;
            statsLayout.childForceExpandHeight = false;
            statsLayout.childAlignment = TextAnchor.MiddleRight;
            var statsLayoutElement = statsSection.gameObject.AddComponent<LayoutElement>();
            statsLayoutElement.preferredWidth = 120;

            _playersText = UIComponents.CreateText(statsSection, "Players", "2/10 players", 14, TextAlignmentOptions.Right);
            _entryFeeText = UIComponents.CreateText(statsSection, "EntryFee", "$5.00 USDT", 16, TextAlignmentOptions.Right);
            _entryFeeText.fontStyle = FontStyles.Bold;
            _entryFeeText.color = new Color(0.3f, 0.9f, 0.4f);

            // Join button
            _joinButton = UIComponents.CreateButton(_rectTransform, "JoinButton", "Join", new Vector2(80, 40));
            _joinButton.onClick.AddListener(() => OnJoinClicked?.Invoke());
            UIComponents.SetButtonColor(_joinButton, new Color(0.3f, 0.5f, 0.9f));
        }

        public void SetRoom(PrivateRoom room)
        {
            _room = room;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_room == null) return;

            _nameText.text = _room.Name;
            _codeText.text = _room.RoomCode;
            _hostText.text = $"Host: {_room.Host?.Username ?? "Unknown"}";
            _playersText.text = $"{_room.CurrentPlayers}/{_room.MaxPlayers} players";
            _entryFeeText.text = $"${_room.EntryFee:F2} {_room.EntryCurrency}";

            _joinButton.interactable = _room.CanJoin;
        }

        public void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (theme == null) return;

            _nameText.color = theme.TextPrimary;
        }
    }
}
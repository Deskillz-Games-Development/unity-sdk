// =============================================================================
// Deskillz SDK for Unity - Host Dashboard UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deskillz.Host;

namespace Deskillz.UI.Host
{
    /// <summary>
    /// Main dashboard panel for host management.
    /// Displays profile, earnings, active rooms, badges, and tier progress.
    /// </summary>
    public class HostDashboardUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when back button is clicked</summary>
        public event Action OnBackClicked;

        /// <summary>Called when withdraw button is clicked</summary>
        public event Action OnWithdrawClicked;

        /// <summary>Called when settings button is clicked</summary>
        public event Action OnSettingsClicked;

        /// <summary>Called when a room is selected</summary>
        public event Action<string> OnRoomSelected;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private RectTransform _container;
        private TextMeshProUGUI _titleText;
        private Button _backButton;
        private Button _settingsButton;
        private Button _refreshButton;

        // Profile section
        private HostProfileCardUI _profileCard;

        // Earnings section
        private HostEarningsUI _earningsPanel;

        // Tier section
        private HostTierProgressUI _esportsTierProgress;
        private HostTierProgressUI _socialTierProgress;

        // Badges section
        private HostBadgeGridUI _badgeGrid;

        // Active rooms section
        private ScrollRect _activeRoomsScroll;
        private RectTransform _activeRoomsContent;
        private TextMeshProUGUI _noActiveRoomsText;
        private List<ActiveRoomCardUI> _roomCards = new List<ActiveRoomCardUI>();

        // Stats section
        private TextMeshProUGUI _roomsThisMonthText;
        private TextMeshProUGUI _playersThisMonthText;
        private TextMeshProUGUI _avgPlayersText;
        private TextMeshProUGUI _completionRateText;

        // Loading
        private GameObject _loadingIndicator;
        private TextMeshProUGUI _errorText;

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isLoading;
        private HostProfile _currentProfile;
        private HostEarnings _currentEarnings;
        private List<ActiveRoomSummary> _activeRooms = new List<ActiveRoomSummary>();

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            _container = CreateContainer();

            // Header
            CreateHeader();

            // Create scrollable content
            var scrollView = CreateScrollView();

            // Profile section
            CreateProfileSection(scrollView);

            // Earnings section
            CreateEarningsSection(scrollView);

            // Tier progress section
            CreateTierSection(scrollView);

            // Active rooms section
            CreateActiveRoomsSection(scrollView);

            // Badges section
            CreateBadgesSection(scrollView);

            // Stats section
            CreateStatsSection(scrollView);

            // Loading indicator
            CreateLoadingIndicator();

            // Subscribe to events
            SubscribeToEvents();
        }

        private void CreateHeader()
        {
            var header = UIComponents.CreateHeader(_container, "Host Dashboard");
            _titleText = header.GetComponentInChildren<TextMeshProUGUI>();

            _backButton = UIComponents.CreateIconButton(header, "back", OnBackButtonClicked);
            _backButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);

            _settingsButton = UIComponents.CreateIconButton(header, "settings", OnSettingsButtonClicked);
            _settingsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60, 0);

            _refreshButton = UIComponents.CreateIconButton(header, "refresh", OnRefreshButtonClicked);
            _refreshButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
        }

        private ScrollRect CreateScrollView()
        {
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(_container, false);

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.offsetMin = new Vector2(0, 0);
            scrollRectTransform.offsetMax = new Vector2(0, -60); // Below header

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);

            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var verticalLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 16;
            verticalLayout.padding = new RectOffset(16, 16, 16, 16);
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childControlHeight = true;
            verticalLayout.childControlWidth = true;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;

            return scrollRect;
        }

        private void CreateProfileSection(ScrollRect scrollView)
        {
            var profileGO = new GameObject("ProfileSection");
            profileGO.transform.SetParent(scrollView.content, false);

            _profileCard = profileGO.AddComponent<HostProfileCardUI>();
            _profileCard.Initialize(_theme);

            var layoutElement = profileGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 120;
        }

        private void CreateEarningsSection(ScrollRect scrollView)
        {
            var earningsGO = new GameObject("EarningsSection");
            earningsGO.transform.SetParent(scrollView.content, false);

            _earningsPanel = earningsGO.AddComponent<HostEarningsUI>();
            _earningsPanel.Initialize(_theme);
            _earningsPanel.OnWithdrawClicked += () => OnWithdrawClicked?.Invoke();

            var layoutElement = earningsGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 200;
        }

        private void CreateTierSection(ScrollRect scrollView)
        {
            var tierSectionGO = new GameObject("TierSection");
            tierSectionGO.transform.SetParent(scrollView.content, false);

            var tierLayout = tierSectionGO.AddComponent<HorizontalLayoutGroup>();
            tierLayout.spacing = 16;
            tierLayout.childForceExpandWidth = true;
            tierLayout.childForceExpandHeight = true;

            var layoutElement = tierSectionGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 150;

            // Esports tier
            var esportsGO = new GameObject("EsportsTier");
            esportsGO.transform.SetParent(tierSectionGO.transform, false);
            _esportsTierProgress = esportsGO.AddComponent<HostTierProgressUI>();
            _esportsTierProgress.Initialize(_theme);
            _esportsTierProgress.SetTierType(RoomRevenueType.Esports);

            // Social tier
            var socialGO = new GameObject("SocialTier");
            socialGO.transform.SetParent(tierSectionGO.transform, false);
            _socialTierProgress = socialGO.AddComponent<HostTierProgressUI>();
            _socialTierProgress.Initialize(_theme);
            _socialTierProgress.SetTierType(RoomRevenueType.Social);
        }

        private void CreateActiveRoomsSection(ScrollRect scrollView)
        {
            var sectionGO = new GameObject("ActiveRoomsSection");
            sectionGO.transform.SetParent(scrollView.content, false);

            var verticalLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 8;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            var layoutElement = sectionGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 200;

            // Section header
            var headerGO = UIComponents.CreateSectionHeader(sectionGO.transform, "Active Rooms");

            // Rooms scroll
            var roomsScrollGO = new GameObject("RoomsScroll");
            roomsScrollGO.transform.SetParent(sectionGO.transform, false);

            _activeRoomsScroll = roomsScrollGO.AddComponent<ScrollRect>();
            var scrollRectTransform = roomsScrollGO.GetComponent<RectTransform>();
            scrollRectTransform.sizeDelta = new Vector2(0, 150);

            var roomsContentGO = new GameObject("RoomsContent");
            roomsContentGO.transform.SetParent(roomsScrollGO.transform, false);

            _activeRoomsContent = roomsContentGO.AddComponent<RectTransform>();
            _activeRoomsContent.anchorMin = new Vector2(0, 0.5f);
            _activeRoomsContent.anchorMax = new Vector2(0, 0.5f);
            _activeRoomsContent.pivot = new Vector2(0, 0.5f);

            var horizontalLayout = roomsContentGO.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 12;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;

            var contentFitter = roomsContentGO.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            _activeRoomsScroll.content = _activeRoomsContent;
            _activeRoomsScroll.horizontal = true;
            _activeRoomsScroll.vertical = false;

            // No rooms text
            _noActiveRoomsText = UIComponents.CreateText(sectionGO.transform, "No active rooms", 14);
            _noActiveRoomsText.alignment = TextAlignmentOptions.Center;
            _noActiveRoomsText.color = _theme?.TextSecondary ?? Color.gray;
            _noActiveRoomsText.gameObject.SetActive(false);
        }

        private void CreateBadgesSection(ScrollRect scrollView)
        {
            var badgesGO = new GameObject("BadgesSection");
            badgesGO.transform.SetParent(scrollView.content, false);

            _badgeGrid = badgesGO.AddComponent<HostBadgeGridUI>();
            _badgeGrid.Initialize(_theme);

            var layoutElement = badgesGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 180;
        }

        private void CreateStatsSection(ScrollRect scrollView)
        {
            var statsGO = new GameObject("StatsSection");
            statsGO.transform.SetParent(scrollView.content, false);

            var verticalLayout = statsGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 8;
            verticalLayout.childForceExpandWidth = true;

            var layoutElement = statsGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 150;

            // Section header
            UIComponents.CreateSectionHeader(statsGO.transform, "This Month's Stats");

            // Stats grid
            var gridGO = new GameObject("StatsGrid");
            gridGO.transform.SetParent(statsGO.transform, false);

            var gridLayout = gridGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150, 60);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            _roomsThisMonthText = CreateStatItem(gridGO.transform, "Rooms", "0");
            _playersThisMonthText = CreateStatItem(gridGO.transform, "Players", "0");
            _avgPlayersText = CreateStatItem(gridGO.transform, "Avg Players", "0");
            _completionRateText = CreateStatItem(gridGO.transform, "Completion", "0%");
        }

        private TextMeshProUGUI CreateStatItem(Transform parent, string label, string value)
        {
            var itemGO = new GameObject($"Stat_{label}");
            itemGO.transform.SetParent(parent, false);

            var bg = itemGO.AddComponent<Image>();
            bg.color = _theme?.CardBackground ?? new Color(0.15f, 0.15f, 0.2f);

            var verticalLayout = itemGO.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(8, 8, 8, 8);
            verticalLayout.childAlignment = TextAnchor.MiddleCenter;

            var valueText = UIComponents.CreateText(itemGO.transform, value, 24);
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;

            var labelText = UIComponents.CreateText(itemGO.transform, label, 12);
            labelText.color = _theme?.TextSecondary ?? Color.gray;
            labelText.alignment = TextAlignmentOptions.Center;

            return valueText;
        }

        private void CreateLoadingIndicator()
        {
            _loadingIndicator = UIComponents.CreateLoadingIndicator(_container);
            _loadingIndicator.SetActive(false);

            var errorGO = new GameObject("Error");
            errorGO.transform.SetParent(_container, false);
            _errorText = errorGO.AddComponent<TextMeshProUGUI>();
            _errorText.fontSize = 14;
            _errorText.color = _theme?.ErrorColor ?? Color.red;
            _errorText.alignment = TextAlignmentOptions.Center;
            _errorText.gameObject.SetActive(false);
        }

        private void SubscribeToEvents()
        {
            HostEvents.OnProfileLoaded += HandleProfileLoaded;
            HostEvents.OnEarningsLoaded += HandleEarningsLoaded;
            HostEvents.OnActiveRoomsUpdated += HandleActiveRoomsUpdated;
            HostEvents.OnBadgesUpdated += HandleBadgesUpdated;
            HostEvents.OnStatsLoaded += HandleStatsLoaded;
            HostEvents.OnSettlementReceived += HandleSettlement;
        }

        private void OnDestroy()
        {
            HostEvents.OnProfileLoaded -= HandleProfileLoaded;
            HostEvents.OnEarningsLoaded -= HandleEarningsLoaded;
            HostEvents.OnActiveRoomsUpdated -= HandleActiveRoomsUpdated;
            HostEvents.OnBadgesUpdated -= HandleBadgesUpdated;
            HostEvents.OnStatsLoaded -= HandleStatsLoaded;
            HostEvents.OnSettlementReceived -= HandleSettlement;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Show the dashboard and load data.
        /// </summary>
        public override void Show()
        {
            base.Show();
            LoadData();
        }

        /// <summary>
        /// Refresh all dashboard data.
        /// </summary>
        public void Refresh()
        {
            LoadData();
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        private void LoadData()
        {
            SetLoading(true);
            ClearError();

            // Load profile
            HostManager.GetProfile(
                profile => { },
                error => ShowError(error.Message)
            );

            // Load earnings
            HostManager.GetEarnings(
                earnings => { },
                error => { }
            );

            // Load active rooms
            HostManager.GetActiveRooms(
                rooms => { },
                error => { }
            );

            // Load stats
            HostManager.GetStats(
                stats => SetLoading(false),
                error => SetLoading(false)
            );
        }

        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            _loadingIndicator.SetActive(loading);
            _refreshButton.interactable = !loading;
        }

        private void ShowError(string message)
        {
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
            SetLoading(false);
        }

        private void ClearError()
        {
            _errorText.gameObject.SetActive(false);
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandleProfileLoaded(HostProfile profile)
        {
            _currentProfile = profile;
            _profileCard.SetProfile(profile);
            _esportsTierProgress.SetTier(profile.EsportsTier);
            _socialTierProgress.SetTier(profile.SocialTier);
        }

        private void HandleEarningsLoaded(HostEarnings earnings)
        {
            _currentEarnings = earnings;
            _earningsPanel.SetEarnings(earnings);
        }

        private void HandleActiveRoomsUpdated(List<ActiveRoomSummary> rooms)
        {
            _activeRooms = rooms;
            RefreshActiveRooms();
        }

        private void HandleBadgesUpdated(List<HostBadge> badges)
        {
            _badgeGrid.SetBadges(badges);
        }

        private void HandleStatsLoaded(HostStats stats)
        {
            _roomsThisMonthText.text = stats.RoomsThisMonth.ToString();
            _playersThisMonthText.text = stats.PlayersThisMonth.ToString();
            _avgPlayersText.text = stats.AvgPlayersPerRoom.ToString("F1");
            _completionRateText.text = $"{stats.CompletionRate:F0}%";

            _esportsTierProgress.SetProgress(stats.TierProgress);
            _socialTierProgress.SetProgress(stats.TierProgress);
        }

        private void HandleSettlement(string roomId, decimal amount, SettlementTrigger trigger)
        {
            // Refresh earnings on settlement
            HostManager.GetEarnings();
        }

        private void RefreshActiveRooms()
        {
            // Clear existing cards
            foreach (var card in _roomCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _roomCards.Clear();

            // Show/hide no rooms text
            _noActiveRoomsText.gameObject.SetActive(_activeRooms.Count == 0);
            _activeRoomsScroll.gameObject.SetActive(_activeRooms.Count > 0);

            // Create cards for active rooms
            foreach (var room in _activeRooms)
            {
                var cardGO = new GameObject($"RoomCard_{room.RoomCode}");
                cardGO.transform.SetParent(_activeRoomsContent, false);

                var card = cardGO.AddComponent<ActiveRoomCardUI>();
                card.Initialize(_theme);
                card.SetRoom(room);
                card.OnClicked += () => OnRoomSelected?.Invoke(room.Id);

                var layoutElement = cardGO.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 200;

                _roomCards.Add(card);
            }
        }

        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================

        private void OnBackButtonClicked()
        {
            OnBackClicked?.Invoke();
        }

        private void OnSettingsButtonClicked()
        {
            OnSettingsClicked?.Invoke();
        }

        private void OnRefreshButtonClicked()
        {
            Refresh();
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_titleText != null)
                _titleText.color = theme.TextPrimary;

            _profileCard?.ApplyTheme(theme);
            _earningsPanel?.ApplyTheme(theme);
            _esportsTierProgress?.ApplyTheme(theme);
            _socialTierProgress?.ApplyTheme(theme);
            _badgeGrid?.ApplyTheme(theme);

            foreach (var card in _roomCards)
            {
                card?.ApplyTheme(theme);
            }
        }
    }

    /// <summary>
    /// Card UI for displaying an active room summary.
    /// </summary>
    public class ActiveRoomCardUI : UIPanel
    {
        public event Action OnClicked;

        private Image _background;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _codeText;
        private TextMeshProUGUI _playersText;
        private TextMeshProUGUI _earningsText;
        private TextMeshProUGUI _roundText;
        private Image _statusIndicator;
        private Button _cardButton;

        private ActiveRoomSummary _room;

        protected override void SetupLayout()
        {
            _background = gameObject.AddComponent<Image>();
            _background.color = _theme?.CardBackground ?? new Color(0.15f, 0.15f, 0.2f);

            _cardButton = gameObject.AddComponent<Button>();
            _cardButton.onClick.AddListener(() => OnClicked?.Invoke());

            var verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(12, 12, 12, 12);
            verticalLayout.spacing = 4;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            // Room name
            _nameText = UIComponents.CreateText(transform, "Room Name", 14);
            _nameText.fontStyle = FontStyles.Bold;

            // Room code
            _codeText = UIComponents.CreateText(transform, "DSKZ-XXXX", 12);
            _codeText.color = _theme?.TextSecondary ?? Color.gray;

            // Players
            _playersText = UIComponents.CreateText(transform, "0/0 players", 12);

            // Earnings
            _earningsText = UIComponents.CreateText(transform, "$0.00", 16);
            _earningsText.fontStyle = FontStyles.Bold;
            _earningsText.color = _theme?.SuccessColor ?? Color.green;

            // Round
            _roundText = UIComponents.CreateText(transform, "Round 0", 12);
            _roundText.color = _theme?.TextSecondary ?? Color.gray;
        }

        public void SetRoom(ActiveRoomSummary room)
        {
            _room = room;

            _nameText.text = room.Name;
            _codeText.text = room.RoomCode;
            _playersText.text = $"{room.CurrentPlayers}/{room.MaxPlayers} players";
            _earningsText.text = $"${room.EstimatedEarnings:F2}";
            _roundText.text = room.IsPlaying ? $"Round {room.CurrentRound}" : "Waiting";
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_background != null) _background.color = theme.CardBackground;
            if (_codeText != null) _codeText.color = theme.TextSecondary;
            if (_earningsText != null) _earningsText.color = theme.SuccessColor;
            if (_roundText != null) _roundText.color = theme.TextSecondary;
        }
    }
}
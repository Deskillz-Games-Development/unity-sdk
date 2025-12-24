// =============================================================================
// Deskillz SDK for Unity - Stage Browser UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
//
// ⚠️  DEPRECATED - NEW LOBBY ARCHITECTURE
// ============================================
// As of SDK v2.0, matchmaking and stage browsing happens on the Deskillz website.
// Players join tournaments at https://deskillz.games, then your game is launched
// via deep link with the match data.
//
// This UI component is kept for backwards compatibility but will show a message
// directing users to the website.
//
// For the new architecture, see:
// - DeepLinkHandler.cs    (handles incoming deep links)
// - DeskillzBridge.cs     (simplified match flow)
// - DeskillzLobbyClient.cs (real-time lobby communication)
//
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using Deskillz.UI;

namespace Deskillz.Stage
{
    /// <summary>
    /// [DEPRECATED] UI for browsing and joining public stages.
    /// 
    /// NEW ARCHITECTURE: Matchmaking now happens on the Deskillz website.
    /// This component now displays a message directing users to the website.
    /// </summary>
    [Obsolete("Matchmaking now happens on the Deskillz website. Use DeskillzBridge for the new flow.")]
    public class StageBrowserUI : UIPanel
    {
        // =============================================================================
        // UI REFERENCES
        // =============================================================================

        private RectTransform _panel;
        private Text _titleText;
        private Text _messageText;
        private Button _openWebsiteButton;
        private Button _closeButton;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Website URL")]
        [SerializeField] private string _lobbyUrl = "https://deskillz.games";

        // =============================================================================
        // EVENTS (Deprecated but kept for compatibility)
        // =============================================================================

        [Obsolete("Use DeskillzBridge events instead")]
        public event Action OnCreateStage;
        
        [Obsolete("Use DeskillzBridge events instead")]
        public event Action OnJoinByCode;
        
        [Obsolete("Use DeskillzBridge events instead")]
        public event Action<StageInfo> OnStageSelected;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        protected override void SetupLayout()
        {
            var rect = GetComponent<RectTransform>();
            SetAnchorFill(rect);

            // Background overlay
            var overlay = gameObject.AddComponent<Image>();
            overlay.color = _theme?.OverlayColor ?? new Color(0, 0, 0, 0.9f);

            // Main panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);

            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.2f, 0.25f);
            _panel.anchorMax = new Vector2(0.8f, 0.75f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = _theme?.PanelColor ?? new Color(0.1f, 0.1f, 0.15f);

            CreateContent();
        }

        private void CreateContent()
        {
            // Title
            _titleText = CreateText("Title", _panel, "JOIN TOURNAMENTS");
            _titleText.rectTransform.anchorMin = new Vector2(0, 0.75f);
            _titleText.rectTransform.anchorMax = new Vector2(1, 0.95f);
            _titleText.rectTransform.offsetMin = new Vector2(20, 0);
            _titleText.rectTransform.offsetMax = new Vector2(-20, 0);
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.fontSize = _theme?.HeadingFontSize ?? 28;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.color = _theme?.TextPrimary ?? Color.white;

            // Message
            _messageText = CreateText("Message", _panel, 
                "Visit the Deskillz website to browse games,\njoin tournaments, and compete for prizes!\n\n" +
                "Once you join a tournament, this game will\nautomatically launch with your match.");
            _messageText.rectTransform.anchorMin = new Vector2(0, 0.35f);
            _messageText.rectTransform.anchorMax = new Vector2(1, 0.75f);
            _messageText.rectTransform.offsetMin = new Vector2(30, 0);
            _messageText.rectTransform.offsetMax = new Vector2(-30, 0);
            _messageText.alignment = TextAnchor.MiddleCenter;
            _messageText.fontSize = _theme?.BodyFontSize ?? 18;
            _messageText.color = _theme?.TextSecondary ?? new Color(0.8f, 0.8f, 0.8f);

            // Open Website Button
            _openWebsiteButton = CreateButton("OpenWebsite", _panel, "OPEN DESKILLZ.GAMES", OpenWebsite);
            var openRect = _openWebsiteButton.GetComponent<RectTransform>();
            openRect.anchorMin = new Vector2(0.25f, 0.15f);
            openRect.anchorMax = new Vector2(0.75f, 0.3f);
            openRect.offsetMin = Vector2.zero;
            openRect.offsetMax = Vector2.zero;
            _openWebsiteButton.GetComponent<Image>().color = _theme?.PrimaryColor ?? new Color(0.2f, 0.6f, 1f);

            // Close button
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(_panel, false);

            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-10, -10);
            closeRect.sizeDelta = new Vector2(40, 40);

            var closeImage = closeGO.AddComponent<Image>();
            closeImage.color = new Color(1, 1, 1, 0.3f);

            _closeButton = closeGO.AddComponent<Button>();
            _closeButton.targetGraphic = closeImage;
            _closeButton.onClick.AddListener(Hide);

            var closeText = CreateText("X", closeRect, "✕");
            closeText.rectTransform.anchorMin = Vector2.zero;
            closeText.rectTransform.anchorMax = Vector2.one;
            closeText.rectTransform.offsetMin = Vector2.zero;
            closeText.rectTransform.offsetMax = Vector2.zero;
            closeText.alignment = TextAnchor.MiddleCenter;
        }

        // =============================================================================
        // ACTIONS
        // =============================================================================

        private void OpenWebsite()
        {
            DeskillzLogger.Info($"Opening lobby website: {_lobbyUrl}");
            Application.OpenURL(_lobbyUrl);
        }

        // =============================================================================
        // PUBLIC API (Deprecated)
        // =============================================================================

        /// <summary>
        /// [DEPRECATED] Set the list of available stages.
        /// Matchmaking now happens on the website.
        /// </summary>
        [Obsolete("Matchmaking now happens on the Deskillz website")]
        public void SetStages(System.Collections.Generic.List<StageInfo> stages)
        {
            DeskillzLogger.Warning("StageBrowserUI.SetStages is deprecated. Matchmaking happens on deskillz.games");
        }

        /// <summary>
        /// [DEPRECATED] Refresh the stage list.
        /// Matchmaking now happens on the website.
        /// </summary>
        [Obsolete("Matchmaking now happens on the Deskillz website")]
        public void RefreshList()
        {
            DeskillzLogger.Warning("StageBrowserUI.RefreshList is deprecated. Matchmaking happens on deskillz.games");
        }

        /// <summary>
        /// Update the lobby URL.
        /// </summary>
        public void SetLobbyUrl(string url)
        {
            _lobbyUrl = url;
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_messageText != null) _messageText.color = theme.TextSecondary;
            if (_openWebsiteButton != null)
            {
                _openWebsiteButton.GetComponent<Image>().color = theme.PrimaryColor;
            }
        }
    }

    // =============================================================================
    // STAGE WAITING ROOM UI (Simplified for new architecture)
    // =============================================================================

    /// <summary>
    /// UI for the pre-match waiting room.
    /// Shows players who have joined and their ready status.
    /// This is used AFTER the website lobby has matched players.
    /// </summary>
    public class StageWaitingRoomUI : UIPanel
    {
        // =============================================================================
        // UI REFERENCES
        // =============================================================================

        private RectTransform _panel;
        private Text _titleText;
        private Text _statusText;
        private Text _countdownText;
        private RectTransform _playerListContainer;
        private Button _readyButton;
        private Button _leaveButton;

        private DeskillzTheme _theme;
        private System.Collections.Generic.List<PlayerListItem> _playerItems = 
            new System.Collections.Generic.List<PlayerListItem>();

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnReady;
        public event Action OnLeave;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        protected override void SetupLayout()
        {
            var rect = GetComponent<RectTransform>();
            SetAnchorFill(rect);

            // Background overlay
            var overlay = gameObject.AddComponent<Image>();
            overlay.color = _theme?.OverlayColor ?? new Color(0, 0, 0, 0.85f);

            // Main panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);

            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.15f, 0.1f);
            _panel.anchorMax = new Vector2(0.85f, 0.9f);
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = _theme?.PanelColor ?? new Color(0.12f, 0.12f, 0.18f);

            CreateHeader();
            CreatePlayerList();
            CreateFooter();
        }

        private void CreateHeader()
        {
            // Title
            _titleText = CreateText("Title", _panel, "MATCH FOUND!");
            _titleText.rectTransform.anchorMin = new Vector2(0, 0.88f);
            _titleText.rectTransform.anchorMax = new Vector2(1, 0.98f);
            _titleText.rectTransform.offsetMin = new Vector2(20, 0);
            _titleText.rectTransform.offsetMax = new Vector2(-20, 0);
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.fontSize = _theme?.HeadingFontSize ?? 32;
            _titleText.fontStyle = FontStyle.Bold;

            // Status
            _statusText = CreateText("Status", _panel, "Waiting for all players to ready up...");
            _statusText.rectTransform.anchorMin = new Vector2(0, 0.8f);
            _statusText.rectTransform.anchorMax = new Vector2(1, 0.88f);
            _statusText.rectTransform.offsetMin = new Vector2(20, 0);
            _statusText.rectTransform.offsetMax = new Vector2(-20, 0);
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.fontSize = _theme?.BodyFontSize ?? 18;
            _statusText.color = _theme?.TextSecondary ?? new Color(0.7f, 0.7f, 0.7f);

            // Countdown (hidden initially)
            _countdownText = CreateText("Countdown", _panel, "");
            _countdownText.rectTransform.anchorMin = new Vector2(0.35f, 0.72f);
            _countdownText.rectTransform.anchorMax = new Vector2(0.65f, 0.8f);
            _countdownText.rectTransform.offsetMin = Vector2.zero;
            _countdownText.rectTransform.offsetMax = Vector2.zero;
            _countdownText.alignment = TextAnchor.MiddleCenter;
            _countdownText.fontSize = 48;
            _countdownText.fontStyle = FontStyle.Bold;
            _countdownText.color = _theme?.SuccessColor ?? Color.green;
            _countdownText.gameObject.SetActive(false);
        }

        private void CreatePlayerList()
        {
            var containerGO = new GameObject("PlayerList");
            containerGO.transform.SetParent(_panel, false);

            _playerListContainer = containerGO.AddComponent<RectTransform>();
            _playerListContainer.anchorMin = new Vector2(0.1f, 0.2f);
            _playerListContainer.anchorMax = new Vector2(0.9f, 0.7f);
            _playerListContainer.offsetMin = Vector2.zero;
            _playerListContainer.offsetMax = Vector2.zero;

            var layout = containerGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(10, 10, 10, 10);

            var bg = containerGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f);
        }

        private void CreateFooter()
        {
            var footerGO = new GameObject("Footer");
            footerGO.transform.SetParent(_panel, false);

            var footerRect = footerGO.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0.15f);
            footerRect.offsetMin = new Vector2(20, 10);
            footerRect.offsetMax = new Vector2(-20, -10);

            var layout = footerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Leave button
            _leaveButton = CreateButton("Leave", footerRect, "LEAVE MATCH", () => OnLeave?.Invoke());
            var leaveRect = _leaveButton.GetComponent<RectTransform>();
            leaveRect.sizeDelta = new Vector2(150, 50);
            _leaveButton.GetComponent<Image>().color = _theme?.ErrorColor ?? new Color(0.8f, 0.2f, 0.2f);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(footerRect, false);
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Ready button
            _readyButton = CreateButton("Ready", footerRect, "READY", () => OnReady?.Invoke());
            var readyRect = _readyButton.GetComponent<RectTransform>();
            readyRect.sizeDelta = new Vector2(180, 50);
            _readyButton.GetComponent<Image>().color = _theme?.SuccessColor ?? new Color(0.2f, 0.8f, 0.2f);
        }

        // =============================================================================
        // PUBLIC METHODS
        // =============================================================================

        /// <summary>
        /// Update the player list display.
        /// </summary>
        public void UpdatePlayers(System.Collections.Generic.List<PlayerPresence> players)
        {
            // Clear existing
            foreach (var item in _playerItems)
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _playerItems.Clear();

            // Create new items
            foreach (var player in players)
            {
                var itemGO = new GameObject($"Player_{player.PlayerId}");
                itemGO.transform.SetParent(_playerListContainer, false);

                var item = itemGO.AddComponent<PlayerListItem>();
                item.Initialize(_theme, player);
                _playerItems.Add(item);
            }

            // Update status
            int readyCount = 0;
            foreach (var p in players) { if (p.IsReady) readyCount++; }
            _statusText.text = $"{readyCount}/{players.Count} players ready";
        }

        /// <summary>
        /// Show countdown before match starts.
        /// </summary>
        public void ShowCountdown(int seconds)
        {
            _countdownText.gameObject.SetActive(true);
            _countdownText.text = seconds.ToString();
            _statusText.text = "Match starting...";
        }

        /// <summary>
        /// Hide countdown.
        /// </summary>
        public void HideCountdown()
        {
            _countdownText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Update ready button state.
        /// </summary>
        public void SetReadyState(bool isReady)
        {
            var buttonText = _readyButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isReady ? "NOT READY" : "READY";
            }
            _readyButton.GetComponent<Image>().color = isReady 
                ? (_theme?.WarningColor ?? Color.yellow) 
                : (_theme?.SuccessColor ?? Color.green);
        }

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_titleText != null) _titleText.color = theme.TextPrimary;
            if (_statusText != null) _statusText.color = theme.TextSecondary;
            if (_countdownText != null) _countdownText.color = theme.SuccessColor;
        }
    }

    // =============================================================================
    // PLAYER LIST ITEM
    // =============================================================================

    public class PlayerListItem : MonoBehaviour
    {
        private Text _nameText;
        private Text _statusText;
        private Image _readyIndicator;

        public void Initialize(DeskillzTheme theme, PlayerPresence player)
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            var bg = gameObject.AddComponent<Image>();
            bg.color = player.IsLocalPlayer 
                ? new Color(0.2f, 0.25f, 0.35f) 
                : new Color(0.15f, 0.15f, 0.2f);

            // Ready indicator
            var indicatorGO = new GameObject("ReadyIndicator");
            indicatorGO.transform.SetParent(transform, false);

            var indicatorRect = indicatorGO.AddComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0, 0.2f);
            indicatorRect.anchorMax = new Vector2(0, 0.8f);
            indicatorRect.pivot = new Vector2(0, 0.5f);
            indicatorRect.anchoredPosition = new Vector2(15, 0);
            indicatorRect.sizeDelta = new Vector2(12, 0);

            _readyIndicator = indicatorGO.AddComponent<Image>();
            _readyIndicator.color = player.IsReady 
                ? (theme?.SuccessColor ?? Color.green) 
                : (theme?.TextSecondary ?? Color.gray);

            // Name
            _nameText = CreateText("Name", player.Username + (player.IsLocalPlayer ? " (You)" : ""));
            _nameText.rectTransform.anchorMin = new Vector2(0, 0);
            _nameText.rectTransform.anchorMax = new Vector2(0.7f, 1);
            _nameText.rectTransform.offsetMin = new Vector2(40, 0);
            _nameText.rectTransform.offsetMax = Vector2.zero;
            _nameText.alignment = TextAnchor.MiddleLeft;
            _nameText.fontSize = 20;
            _nameText.fontStyle = player.IsLocalPlayer ? FontStyle.Bold : FontStyle.Normal;
            _nameText.color = theme?.TextPrimary ?? Color.white;

            // Status
            string statusStr = player.IsReady ? "READY" : "NOT READY";
            if (player.IsNPC) statusStr = "NPC - " + statusStr;
            
            _statusText = CreateText("Status", statusStr);
            _statusText.rectTransform.anchorMin = new Vector2(0.7f, 0);
            _statusText.rectTransform.anchorMax = new Vector2(1, 1);
            _statusText.rectTransform.offsetMin = Vector2.zero;
            _statusText.rectTransform.offsetMax = new Vector2(-15, 0);
            _statusText.alignment = TextAnchor.MiddleRight;
            _statusText.fontSize = 16;
            _statusText.color = player.IsReady 
                ? (theme?.SuccessColor ?? Color.green) 
                : (theme?.TextSecondary ?? Color.gray);
        }

        private Text CreateText(string name, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return text;
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Spectator Score Panel UI
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
    /// UI panel displaying player scores in spectator mode.
    /// Shows player list with avatars, scores, and turn indicators.
    /// </summary>
    public class SpectatorScorePanelUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when a player row is clicked</summary>
        public event Action<string> OnPlayerClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private TextMeshProUGUI _titleText;
        private ScrollRect _scrollRect;
        private RectTransform _contentContainer;

        private List<PlayerScoreRowUI> _playerRows = new List<PlayerScoreRowUI>();

        // =====================================================================
        // STATE
        // =====================================================================

        private List<SpectatorPlayerInfo> _players = new List<SpectatorPlayerInfo>();
        private string _highlightedPlayerId;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0, 0, 0, 0.7f);

            var rectTransform = GetComponent<RectTransform>();

            // Main layout
            var verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(8, 8, 8, 8);
            verticalLayout.spacing = 6;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            // Title
            var titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(transform, false);

            var titleLayout = titleContainer.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 24;

            _titleText = UIComponents.CreateText(titleContainer.transform, "Players", 12);
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.alignment = TextAlignmentOptions.Center;

            // Scroll view for players
            var scrollGO = new GameObject("PlayerScroll");
            scrollGO.transform.SetParent(transform, false);

            var scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1;

            _scrollRect = scrollGO.AddComponent<ScrollRect>();
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;

            // Content container
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);

            _contentContainer = contentGO.AddComponent<RectTransform>();
            _contentContainer.anchorMin = new Vector2(0, 1);
            _contentContainer.anchorMax = new Vector2(1, 1);
            _contentContainer.pivot = new Vector2(0.5f, 1);

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _contentContainer;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the players to display.
        /// </summary>
        public void SetPlayers(List<SpectatorPlayerInfo> players)
        {
            _players = players ?? new List<SpectatorPlayerInfo>();

            // Clear existing rows
            foreach (var row in _playerRows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }
            _playerRows.Clear();

            // Sort by score descending
            var sortedPlayers = new List<SpectatorPlayerInfo>(_players);
            sortedPlayers.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Create rows
            int rank = 1;
            foreach (var player in sortedPlayers)
            {
                var row = CreatePlayerRow(player, rank);
                _playerRows.Add(row);
                rank++;
            }

            _titleText.text = $"Players ({_players.Count})";
        }

        /// <summary>
        /// Update a single player's info.
        /// </summary>
        public void UpdatePlayer(SpectatorPlayerInfo player)
        {
            // Find and update existing row
            foreach (var row in _playerRows)
            {
                if (row.PlayerId == player.Id)
                {
                    row.UpdateInfo(player);
                    break;
                }
            }

            // Re-sort if scores changed significantly
            // Could optimize this to only re-sort when rankings change
        }

        /// <summary>
        /// Highlight the winner of a round.
        /// </summary>
        public void HighlightWinner(string playerId)
        {
            _highlightedPlayerId = playerId;

            foreach (var row in _playerRows)
            {
                row.SetHighlighted(row.PlayerId == playerId);
            }
        }

        /// <summary>
        /// Clear winner highlight.
        /// </summary>
        public void ClearHighlight()
        {
            _highlightedPlayerId = null;

            foreach (var row in _playerRows)
            {
                row.SetHighlighted(false);
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private PlayerScoreRowUI CreatePlayerRow(SpectatorPlayerInfo player, int rank)
        {
            var rowGO = new GameObject($"PlayerRow_{player.Id}");
            rowGO.transform.SetParent(_contentContainer, false);

            var row = rowGO.AddComponent<PlayerScoreRowUI>();
            row.Initialize(_theme);
            row.SetPlayer(player, rank);
            row.OnClicked += () => OnPlayerClicked?.Invoke(player.Id);

            return row;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null) _background.color = new Color(0, 0, 0, 0.7f);
            if (_titleText != null) _titleText.color = theme.TextPrimary;

            foreach (var row in _playerRows)
            {
                row?.ApplyTheme(theme);
            }
        }
    }

    /// <summary>
    /// Individual player score row in the panel.
    /// </summary>
    public class PlayerScoreRowUI : UIPanel
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        public event Action OnClicked;

        // =====================================================================
        // UI REFERENCES
        // =====================================================================

        private Image _background;
        private Image _highlightOverlay;
        private TextMeshProUGUI _rankText;
        private Image _avatarImage;
        private Image _turnIndicator;
        private TextMeshProUGUI _usernameText;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _balanceText;
        private Image _statusIndicator;
        private Button _button;

        // =====================================================================
        // STATE
        // =====================================================================

        public string PlayerId { get; private set; }
        private bool _isHighlighted;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            var rectTransform = GetComponent<RectTransform>();

            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.15f, 0.15f, 0.2f);

            // Button for click
            _button = gameObject.AddComponent<Button>();
            _button.onClick.AddListener(() => OnClicked?.Invoke());

            // Layout
            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 44;

            // Rank
            _rankText = UIComponents.CreateText(transform, "#1", 12);
            _rankText.fontStyle = FontStyles.Bold;
            _rankText.color = _theme?.TextSecondary ?? Color.gray;

            var rankLayout = _rankText.gameObject.AddComponent<LayoutElement>();
            rankLayout.preferredWidth = 24;

            // Avatar container
            var avatarContainer = new GameObject("AvatarContainer");
            avatarContainer.transform.SetParent(transform, false);

            var avatarLayout = avatarContainer.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 32;
            avatarLayout.preferredHeight = 32;

            _avatarImage = avatarContainer.AddComponent<Image>();
            _avatarImage.color = Color.gray;

            // Turn indicator (positioned over avatar)
            var turnGO = new GameObject("TurnIndicator");
            turnGO.transform.SetParent(avatarContainer.transform, false);

            var turnRect = turnGO.AddComponent<RectTransform>();
            turnRect.anchorMin = new Vector2(1, 1);
            turnRect.anchorMax = new Vector2(1, 1);
            turnRect.pivot = new Vector2(1, 1);
            turnRect.sizeDelta = new Vector2(10, 10);
            turnRect.anchoredPosition = new Vector2(2, 2);

            _turnIndicator = turnGO.AddComponent<Image>();
            _turnIndicator.color = _theme?.AccentColor ?? new Color(0.4f, 0.8f, 1f);
            _turnIndicator.gameObject.SetActive(false);

            // Username
            var usernameContainer = new GameObject("UsernameContainer");
            usernameContainer.transform.SetParent(transform, false);

            var usernameContainerLayout = usernameContainer.AddComponent<LayoutElement>();
            usernameContainerLayout.flexibleWidth = 1;

            _usernameText = UIComponents.CreateText(usernameContainer.transform, "Player", 13);
            _usernameText.fontStyle = FontStyles.Bold;
            _usernameText.alignment = TextAlignmentOptions.Left;

            // Score
            _scoreText = UIComponents.CreateText(transform, "0", 16);
            _scoreText.fontStyle = FontStyles.Bold;
            _scoreText.alignment = TextAlignmentOptions.Right;

            var scoreLayout = _scoreText.gameObject.AddComponent<LayoutElement>();
            scoreLayout.preferredWidth = 50;

            // Balance (for social games)
            _balanceText = UIComponents.CreateText(transform, "$0", 12);
            _balanceText.color = _theme?.SuccessColor ?? Color.green;
            _balanceText.alignment = TextAlignmentOptions.Right;

            var balanceLayout = _balanceText.gameObject.AddComponent<LayoutElement>();
            balanceLayout.preferredWidth = 50;

            // Connection status
            var statusGO = new GameObject("StatusIndicator");
            statusGO.transform.SetParent(transform, false);

            var statusLayout = statusGO.AddComponent<LayoutElement>();
            statusLayout.preferredWidth = 8;
            statusLayout.preferredHeight = 8;

            _statusIndicator = statusGO.AddComponent<Image>();
            _statusIndicator.color = _theme?.SuccessColor ?? Color.green;

            // Highlight overlay (hidden by default)
            var highlightGO = new GameObject("HighlightOverlay");
            highlightGO.transform.SetParent(transform, false);
            highlightGO.transform.SetAsFirstSibling();

            var highlightRect = highlightGO.AddComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;

            _highlightOverlay = highlightGO.AddComponent<Image>();
            _highlightOverlay.color = new Color(1f, 0.8f, 0f, 0.2f);
            _highlightOverlay.raycastTarget = false;
            _highlightOverlay.gameObject.SetActive(false);
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the player info for this row.
        /// </summary>
        public void SetPlayer(SpectatorPlayerInfo player, int rank)
        {
            PlayerId = player.Id;

            _rankText.text = $"#{rank}";
            _usernameText.text = player.Username ?? "Unknown";
            _scoreText.text = player.Score.ToString();
            _balanceText.text = player.Balance > 0 ? $"${player.Balance:F2}" : "";

            // Turn indicator
            _turnIndicator.gameObject.SetActive(player.IsCurrentTurn);

            // Connection status
            _statusIndicator.color = player.IsConnected 
                ? (_theme?.SuccessColor ?? Color.green) 
                : Color.gray;

            // Rank color
            _rankText.color = rank switch
            {
                1 => new Color(1f, 0.84f, 0f), // Gold
                2 => new Color(0.75f, 0.75f, 0.75f), // Silver
                3 => new Color(0.8f, 0.5f, 0.2f), // Bronze
                _ => _theme?.TextSecondary ?? Color.gray
            };

            // Load avatar
            if (!string.IsNullOrEmpty(player.AvatarUrl))
            {
                LoadAvatar(player.AvatarUrl);
            }

            // Apply player color if specified
            if (!string.IsNullOrEmpty(player.ColorHex))
            {
                if (ColorUtility.TryParseHtmlString(player.ColorHex, out Color playerColor))
                {
                    _usernameText.color = playerColor;
                }
            }
        }

        /// <summary>
        /// Update player info (for real-time updates).
        /// </summary>
        public void UpdateInfo(SpectatorPlayerInfo player)
        {
            _scoreText.text = player.Score.ToString();
            _balanceText.text = player.Balance > 0 ? $"${player.Balance:F2}" : "";
            _turnIndicator.gameObject.SetActive(player.IsCurrentTurn);
            _statusIndicator.color = player.IsConnected 
                ? (_theme?.SuccessColor ?? Color.green) 
                : Color.gray;
        }

        /// <summary>
        /// Set highlighted state (for winner).
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;
            _highlightOverlay.gameObject.SetActive(highlighted);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void LoadAvatar(string url)
        {
            // In production, load image from URL
            _avatarImage.color = Color.gray;
        }

        // =====================================================================
        // THEME
        // =====================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_background != null) _background.color = new Color(0.15f, 0.15f, 0.2f);
            if (_usernameText != null) _usernameText.color = theme.TextPrimary;
            if (_balanceText != null) _balanceText.color = theme.SuccessColor;
            if (_turnIndicator != null) _turnIndicator.color = theme.AccentColor;
        }
    }
}
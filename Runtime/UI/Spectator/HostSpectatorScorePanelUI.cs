// =============================================================================
// Deskillz SDK for Unity - Host Spectator Score Panel UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
// HOST-ONLY FEATURE: Score panel for hosts monitoring their private social rooms.
// Shows player scores and turn indicators but NOT player hands (anti-cheat).
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Deskillz.UI.Host
{
    /// <summary>
    /// UI panel displaying player scores for host spectator mode.
    /// Shows player list with avatars, scores, and turn indicators.
    /// 
    /// IMPORTANT: This is HOST-ONLY.
    /// - Shows: Player names, scores, chip stacks, turn indicator
    /// - Does NOT show: Player hands, hidden cards, private info
    /// </summary>
    public class HostSpectatorScorePanelUI : UIPanel
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
        private TextMeshProUGUI _antiCheatNote;
        private ScrollRect _scrollRect;
        private RectTransform _contentContainer;

        private List<HostPlayerScoreRowUI> _playerRows = new List<HostPlayerScoreRowUI>();

        // =====================================================================
        // STATE
        // =====================================================================

        private List<HostPlayerInfo> _players = new List<HostPlayerInfo>();
        private string _highlightedPlayerId;
        private DeskillzTheme _theme;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        protected override void SetupLayout()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            _titleText = CreateText("PLAYER SCORES", 16);
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.alignment = TextAlignmentOptions.Center;

            // Anti-cheat note
            _antiCheatNote = CreateText("Hands hidden for fair play", 10);
            _antiCheatNote.color = new Color(1f, 0.9f, 0.5f);
            _antiCheatNote.fontStyle = FontStyles.Italic;
            _antiCheatNote.alignment = TextAlignmentOptions.Center;

            // Scroll rect for player list
            var scrollObj = new GameObject("PlayerScroll");
            scrollObj.transform.SetParent(transform, false);
            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var viewportRT = viewport.AddComponent<RectTransform>();
            viewport.AddComponent<Mask>();
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            _contentContainer = content.AddComponent<RectTransform>();
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _contentContainer;
            _scrollRect.viewport = viewportRT;
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        /// <summary>
        /// Set the list of players to display.
        /// Note: Player hands/cards are NOT visible (anti-cheat).
        /// </summary>
        /// <param name="players">List of players with public info only</param>
        public void SetPlayers(List<HostPlayerInfo> players)
        {
            _players = players ?? new List<HostPlayerInfo>();
            RebuildPlayerList();
        }

        /// <summary>
        /// Update scores from a snapshot.
        /// </summary>
        /// <param name="scores">List of player scores</param>
        public void UpdateScores(List<HostPlayerScore> scores)
        {
            if (scores == null) return;

            foreach (var score in scores)
            {
                UpdatePlayerScore(score.PlayerId, score.Score);
            }
        }

        /// <summary>
        /// Update a single player's score.
        /// </summary>
        /// <param name="playerId">Player ID</param>
        /// <param name="newScore">New score value</param>
        public void UpdatePlayerScore(string playerId, int newScore)
        {
            foreach (var row in _playerRows)
            {
                if (row.PlayerId == playerId)
                {
                    row.SetScore(newScore);
                    break;
                }
            }
        }

        /// <summary>
        /// Highlight the player whose turn it is.
        /// </summary>
        /// <param name="playerId">Current turn player ID</param>
        public void HighlightPlayer(string playerId)
        {
            _highlightedPlayerId = playerId;

            foreach (var row in _playerRows)
            {
                row.SetHighlighted(row.PlayerId == playerId);
            }
        }

        /// <summary>
        /// Apply a theme to the panel.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void SetTheme(DeskillzTheme theme)
        {
            _theme = theme;
            if (_background != null && theme != null)
            {
                _background.color = theme.BackgroundColor;
            }

            foreach (var row in _playerRows)
            {
                row.SetTheme(theme);
            }
        }

        // =====================================================================
        // PRIVATE METHODS
        // =====================================================================

        private void RebuildPlayerList()
        {
            // Clear existing rows
            foreach (var row in _playerRows)
            {
                if (row != null && row.gameObject != null)
                {
                    Destroy(row.gameObject);
                }
            }
            _playerRows.Clear();

            // Create new rows
            foreach (var player in _players)
            {
                var row = CreatePlayerRow(player);
                _playerRows.Add(row);
            }

            // Re-apply highlight
            if (!string.IsNullOrEmpty(_highlightedPlayerId))
            {
                HighlightPlayer(_highlightedPlayerId);
            }
        }

        private HostPlayerScoreRowUI CreatePlayerRow(HostPlayerInfo player)
        {
            var rowObj = new GameObject($"Player_{player.PlayerId}");
            rowObj.transform.SetParent(_contentContainer, false);

            var row = rowObj.AddComponent<HostPlayerScoreRowUI>();
            row.Initialize(player, _theme);
            row.OnClicked += () => OnPlayerClicked?.Invoke(player.PlayerId);

            return row;
        }

        private TextMeshProUGUI CreateText(string text, int fontSize)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(transform, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            return tmp;
        }
    }

    /// <summary>
    /// Individual player row in the host score panel.
    /// Shows player info and score but NOT their hand.
    /// </summary>
    public class HostPlayerScoreRowUI : MonoBehaviour
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        public event Action OnClicked;

        // =====================================================================
        // STATE
        // =====================================================================

        public string PlayerId { get; private set; }

        private Image _background;
        private Image _avatarImage;
        private TextMeshProUGUI _usernameText;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _chipStackText;
        private Image _turnIndicator;
        private Image _statusIndicator;
        private Button _button;

        private bool _isHighlighted;
        private DeskillzTheme _theme;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        public void Initialize(HostPlayerInfo player, DeskillzTheme theme)
        {
            PlayerId = player.PlayerId;
            _theme = theme;

            // Setup layout
            var rt = gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 50);

            var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            // Button for click handling
            _button = gameObject.AddComponent<Button>();
            _button.onClick.AddListener(() => OnClicked?.Invoke());

            // Turn indicator
            var turnObj = new GameObject("TurnIndicator");
            turnObj.transform.SetParent(transform, false);
            _turnIndicator = turnObj.AddComponent<Image>();
            _turnIndicator.color = Color.yellow;
            var turnRT = turnObj.GetComponent<RectTransform>();
            turnRT.sizeDelta = new Vector2(8, 8);
            var turnLE = turnObj.AddComponent<LayoutElement>();
            turnLE.preferredWidth = 8;
            _turnIndicator.gameObject.SetActive(false);

            // Avatar
            var avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(transform, false);
            _avatarImage = avatarObj.AddComponent<Image>();
            _avatarImage.color = Color.gray;
            var avatarRT = avatarObj.GetComponent<RectTransform>();
            avatarRT.sizeDelta = new Vector2(40, 40);
            var avatarLE = avatarObj.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 40;
            avatarLE.preferredHeight = 40;

            // Username
            var usernameObj = new GameObject("Username");
            usernameObj.transform.SetParent(transform, false);
            _usernameText = usernameObj.AddComponent<TextMeshProUGUI>();
            _usernameText.text = player.Username;
            _usernameText.fontSize = 14;
            _usernameText.color = Color.white;
            var usernameLE = usernameObj.AddComponent<LayoutElement>();
            usernameLE.flexibleWidth = 1;

            // Score
            var scoreObj = new GameObject("Score");
            scoreObj.transform.SetParent(transform, false);
            _scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            _scoreText.text = player.Score.ToString();
            _scoreText.fontSize = 16;
            _scoreText.fontStyle = FontStyles.Bold;
            _scoreText.color = Color.white;
            _scoreText.alignment = TextAlignmentOptions.Right;
            var scoreLE = scoreObj.AddComponent<LayoutElement>();
            scoreLE.preferredWidth = 60;

            // Chip stack
            var chipsObj = new GameObject("Chips");
            chipsObj.transform.SetParent(transform, false);
            _chipStackText = chipsObj.AddComponent<TextMeshProUGUI>();
            _chipStackText.text = $"${player.ChipStack:F2}";
            _chipStackText.fontSize = 12;
            _chipStackText.color = new Color(0.6f, 0.8f, 0.6f);
            _chipStackText.alignment = TextAlignmentOptions.Right;
            var chipsLE = chipsObj.AddComponent<LayoutElement>();
            chipsLE.preferredWidth = 70;

            // Status indicator (active/folded)
            var statusObj = new GameObject("Status");
            statusObj.transform.SetParent(transform, false);
            _statusIndicator = statusObj.AddComponent<Image>();
            _statusIndicator.color = player.IsActive ? Color.green : Color.red;
            var statusRT = statusObj.GetComponent<RectTransform>();
            statusRT.sizeDelta = new Vector2(12, 12);
            var statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredWidth = 12;

            // Load avatar if URL provided
            if (!string.IsNullOrEmpty(player.AvatarUrl))
            {
                LoadAvatar(player.AvatarUrl);
            }

            // Apply initial highlight state
            SetHighlighted(player.IsCurrentTurn);
        }

        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================

        public void SetScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }
        }

        public void SetChipStack(float chips)
        {
            if (_chipStackText != null)
            {
                _chipStackText.text = $"${chips:F2}";
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            _isHighlighted = highlighted;

            if (_turnIndicator != null)
            {
                _turnIndicator.gameObject.SetActive(highlighted);
            }

            if (_background != null)
            {
                _background.color = highlighted
                    ? new Color(0.3f, 0.3f, 0.1f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.8f);
            }
        }

        public void SetActive(bool isActive)
        {
            if (_statusIndicator != null)
            {
                _statusIndicator.color = isActive ? Color.green : Color.red;
            }

            if (_usernameText != null)
            {
                _usernameText.color = isActive ? Color.white : Color.gray;
            }
        }

        public void SetTheme(DeskillzTheme theme)
        {
            _theme = theme;
            // Apply theme colors if needed
        }

        // =====================================================================
        // PRIVATE METHODS
        // =====================================================================

        private void LoadAvatar(string url)
        {
            // Avatar loading would use UnityWebRequestTexture
            // For now, keep placeholder
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Phase 4: Pre-Built UI System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Deskillz.UI
{
    /// <summary>
    /// Displays live leaderboard during matches.
    /// </summary>
    public class LeaderboardUI : UIPanel
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Leaderboard Settings")]
        [SerializeField] private int _maxVisiblePlayers = 10;
        [SerializeField] private float _rowHeight = 60f;
        [SerializeField] private float _updateInterval = 1f;

        // =============================================================================
        // UI REFERENCES
        // =============================================================================

        private RectTransform _panel;
        private Text _titleText;
        private RectTransform _listContainer;
        private Button _closeButton;
        private List<LeaderboardRow> _rows = new List<LeaderboardRow>();

        // =============================================================================
        // STATE
        // =============================================================================

        private List<MatchPlayer> _players = new List<MatchPlayer>();
        private float _lastUpdateTime;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        protected override void SetupLayout()
        {
            // Main panel - centered overlay
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);

            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(500, 600);

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = _theme?.PanelColor ?? new Color(0.15f, 0.15f, 0.2f);

            // Background overlay
            var overlayGO = new GameObject("Overlay");
            overlayGO.transform.SetParent(transform, false);
            overlayGO.transform.SetAsFirstSibling();

            var overlayRect = overlayGO.AddComponent<RectTransform>();
            SetAnchorFill(overlayRect);

            var overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = _theme?.OverlayColor ?? new Color(0, 0, 0, 0.7f);

            var overlayButton = overlayGO.AddComponent<Button>();
            overlayButton.onClick.AddListener(Hide);

            // Title
            _titleText = CreateText("Title", _panel, "LEADERBOARD");
            _titleText.rectTransform.anchorMin = new Vector2(0, 1);
            _titleText.rectTransform.anchorMax = new Vector2(1, 1);
            _titleText.rectTransform.pivot = new Vector2(0.5f, 1);
            _titleText.rectTransform.sizeDelta = new Vector2(0, 60);
            _titleText.rectTransform.anchoredPosition = Vector2.zero;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.fontSize = _theme?.HeadingFontSize ?? 32;
            _titleText.fontStyle = FontStyle.Bold;

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
            closeText.fontSize = 24;

            // List container
            var listGO = new GameObject("ListContainer");
            listGO.transform.SetParent(_panel, false);

            _listContainer = listGO.AddComponent<RectTransform>();
            _listContainer.anchorMin = new Vector2(0, 0);
            _listContainer.anchorMax = new Vector2(1, 1);
            _listContainer.offsetMin = new Vector2(20, 20);
            _listContainer.offsetMax = new Vector2(-20, -70);

            // Add vertical layout
            var layout = listGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.padding = new RectOffset(0, 0, 10, 10);

            // Create row pool
            for (int i = 0; i < _maxVisiblePlayers; i++)
            {
                var row = CreateRow(i);
                _rows.Add(row);
                row.gameObject.SetActive(false);
            }
        }

        private LeaderboardRow CreateRow(int index)
        {
            var rowGO = new GameObject($"Row_{index}");
            rowGO.transform.SetParent(_listContainer, false);

            var rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, _rowHeight);

            var rowImage = rowGO.AddComponent<Image>();
            rowImage.color = new Color(1, 1, 1, 0.05f);

            var row = rowGO.AddComponent<LeaderboardRow>();
            row.Initialize(_theme);

            return row;
        }

        // =============================================================================
        // PUBLIC METHODS
        // =============================================================================

        /// <summary>
        /// Set players to display.
        /// </summary>
        public void SetPlayers(List<MatchPlayer> players)
        {
            _players = players ?? new List<MatchPlayer>();
            RefreshList();
        }

        /// <summary>
        /// Update player scores.
        /// </summary>
        public void UpdatePlayers(List<MatchPlayer> players)
        {
            _players = players ?? new List<MatchPlayer>();
            
            // Only refresh at intervals to avoid too many updates
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                RefreshList();
                _lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Refresh the leaderboard display.
        /// </summary>
        public void RefreshList()
        {
            // Sort by score (descending)
            var sorted = new List<MatchPlayer>(_players);
            var config = DeskillzConfig.Instance;
            
            if (config?.ScoreType == ScoreType.LowerIsBetter)
            {
                sorted.Sort((a, b) => a.Score.CompareTo(b.Score));
            }
            else
            {
                sorted.Sort((a, b) => b.Score.CompareTo(a.Score));
            }

            // Update rows
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i < sorted.Count)
                {
                    _rows[i].SetPlayer(i + 1, sorted[i]);
                    _rows[i].gameObject.SetActive(true);
                }
                else
                {
                    _rows[i].gameObject.SetActive(false);
                }
            }
        }

        // =============================================================================
        // THEME
        // =============================================================================

        public override void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (_titleText != null)
            {
                _titleText.color = theme.TextPrimary;
            }

            foreach (var row in _rows)
            {
                row.ApplyTheme(theme);
            }
        }
    }

    // =============================================================================
    // LEADERBOARD ROW
    // =============================================================================

    /// <summary>
    /// Single row in the leaderboard.
    /// </summary>
    public class LeaderboardRow : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Image _background;
        private Text _rankText;
        private Text _nameText;
        private Text _scoreText;
        private Image _avatarImage;
        private DeskillzTheme _theme;
        private bool _isLocalPlayer;

        public void Initialize(DeskillzTheme theme)
        {
            _theme = theme;
            _rectTransform = GetComponent<RectTransform>();
            _background = GetComponent<Image>();

            // Rank
            var rankGO = new GameObject("Rank");
            rankGO.transform.SetParent(transform, false);

            var rankRect = rankGO.AddComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0);
            rankRect.anchorMax = new Vector2(0, 1);
            rankRect.pivot = new Vector2(0, 0.5f);
            rankRect.anchoredPosition = new Vector2(10, 0);
            rankRect.sizeDelta = new Vector2(40, 0);

            _rankText = rankGO.AddComponent<Text>();
            _rankText.text = "1";
            _rankText.alignment = TextAnchor.MiddleCenter;
            _rankText.fontSize = 24;
            _rankText.fontStyle = FontStyle.Bold;
            _rankText.color = theme?.TextPrimary ?? Color.white;
            _rankText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Avatar placeholder
            var avatarGO = new GameObject("Avatar");
            avatarGO.transform.SetParent(transform, false);

            var avatarRect = avatarGO.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0, 0.5f);
            avatarRect.anchoredPosition = new Vector2(60, 0);
            avatarRect.sizeDelta = new Vector2(40, 40);

            _avatarImage = avatarGO.AddComponent<Image>();
            _avatarImage.color = theme?.SecondaryColor ?? Color.gray;

            // Name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(transform, false);

            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0.7f, 1);
            nameRect.offsetMin = new Vector2(110, 0);
            nameRect.offsetMax = new Vector2(0, 0);

            _nameText = nameGO.AddComponent<Text>();
            _nameText.text = "Player";
            _nameText.alignment = TextAnchor.MiddleLeft;
            _nameText.fontSize = 22;
            _nameText.color = theme?.TextPrimary ?? Color.white;
            _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Score
            var scoreGO = new GameObject("Score");
            scoreGO.transform.SetParent(transform, false);

            var scoreRect = scoreGO.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.7f, 0);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.offsetMin = new Vector2(0, 0);
            scoreRect.offsetMax = new Vector2(-10, 0);

            _scoreText = scoreGO.AddComponent<Text>();
            _scoreText.text = "0";
            _scoreText.alignment = TextAnchor.MiddleRight;
            _scoreText.fontSize = 24;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.color = theme?.TextPrimary ?? Color.white;
            _scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public void SetPlayer(int rank, MatchPlayer player)
        {
            _isLocalPlayer = player.IsLocalPlayer;

            _rankText.text = rank.ToString();
            _nameText.text = player.Username ?? "Player";
            _scoreText.text = Score.ScoreDisplay.Format(player.Score);

            // Highlight local player
            if (_isLocalPlayer)
            {
                _background.color = new Color(_theme?.PrimaryColor.r ?? 0.4f, 
                                               _theme?.PrimaryColor.g ?? 0.2f, 
                                               _theme?.PrimaryColor.b ?? 1f, 0.3f);
                _nameText.text += " (You)";
            }
            else
            {
                _background.color = new Color(1, 1, 1, 0.05f);
            }

            // Rank colors
            _rankText.color = rank switch
            {
                1 => new Color(1f, 0.84f, 0f),      // Gold
                2 => new Color(0.75f, 0.75f, 0.75f), // Silver
                3 => new Color(0.8f, 0.5f, 0.2f),   // Bronze
                _ => _theme?.TextPrimary ?? Color.white
            };
        }

        public void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;

            if (!_isLocalPlayer && _background != null)
            {
                _background.color = new Color(1, 1, 1, 0.05f);
            }

            if (_nameText != null)
            {
                _nameText.color = theme.TextPrimary;
            }

            if (_scoreText != null)
            {
                _scoreText.color = theme.TextPrimary;
            }
        }
    }
}

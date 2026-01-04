// =============================================================================
// Deskillz SDK for Unity - Auto-Updater UI
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
//
// Built-in UI for update prompts. Uses Unity's immediate mode GUI (IMGUI)
// for maximum compatibility. Can be replaced with custom UI by listening
// to DeskillzUpdater events instead.
//
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Built-in UI for update prompts using IMGUI.
    /// </summary>
    public class DeskillzUpdaterUI : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Settings")]
        [SerializeField] private bool _useCustomSkin = false;
        [SerializeField] private GUISkin _customSkin;

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private DeskillzUpdater _updater;
        private UpdateInfo _currentUpdate;
        private bool _isVisible;
        private bool _showReleaseNotes;
        
        // UI Styling
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _skipButtonStyle;
        private GUIStyle _releaseNotesStyle;
        private GUIStyle _versionStyle;
        private bool _stylesInitialized;

        // Animation
        private float _fadeAlpha;
        private float _targetAlpha;
        private const float FADE_SPEED = 5f;

        // Window dimensions
        private Rect _windowRect;
        private const float WINDOW_WIDTH = 400f;
        private const float WINDOW_HEIGHT_BASE = 280f;
        private const float WINDOW_HEIGHT_WITH_NOTES = 420f;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        public void Initialize(DeskillzUpdater updater)
        {
            _updater = updater;
            _isVisible = false;
            _fadeAlpha = 0f;
            _targetAlpha = 0f;

            // Center window
            float x = (Screen.width - WINDOW_WIDTH) / 2f;
            float y = (Screen.height - WINDOW_HEIGHT_BASE) / 2f;
            _windowRect = new Rect(x, y, WINDOW_WIDTH, WINDOW_HEIGHT_BASE);
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Show the update dialog.
        /// </summary>
        public void Show(UpdateInfo updateInfo)
        {
            _currentUpdate = updateInfo;
            _isVisible = true;
            _targetAlpha = 1f;
            _showReleaseNotes = false;

            // Adjust window height
            float height = string.IsNullOrEmpty(updateInfo.ReleaseNotes) 
                ? WINDOW_HEIGHT_BASE 
                : WINDOW_HEIGHT_BASE;
            
            float x = (Screen.width - WINDOW_WIDTH) / 2f;
            float y = (Screen.height - height) / 2f;
            _windowRect = new Rect(x, y, WINDOW_WIDTH, height);

            DeskillzLogger.Debug("Update UI shown");
        }

        /// <summary>
        /// Hide the update dialog.
        /// </summary>
        public void Hide()
        {
            _targetAlpha = 0f;
            DeskillzLogger.Debug("Update UI hidden");
        }

        /// <summary>
        /// Whether the UI is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible && _fadeAlpha > 0.01f;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Update()
        {
            // Animate fade
            if (Mathf.Abs(_fadeAlpha - _targetAlpha) > 0.01f)
            {
                _fadeAlpha = Mathf.MoveTowards(_fadeAlpha, _targetAlpha, Time.unscaledDeltaTime * FADE_SPEED);
            }
            else
            {
                _fadeAlpha = _targetAlpha;
                if (_targetAlpha == 0f)
                {
                    _isVisible = false;
                }
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _fadeAlpha < 0.01f || _currentUpdate == null)
                return;

            InitializeStyles();

            // Apply custom skin if set
            var oldSkin = GUI.skin;
            if (_useCustomSkin && _customSkin != null)
            {
                GUI.skin = _customSkin;
            }

            // Store original color
            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);

            // Draw background overlay
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", GUIStyle.none);
            GUI.color = new Color(0f, 0f, 0f, 0.7f * _fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);

            // Adjust window height for release notes
            if (_showReleaseNotes && !string.IsNullOrEmpty(_currentUpdate.ReleaseNotes))
            {
                _windowRect.height = WINDOW_HEIGHT_WITH_NOTES;
            }
            else
            {
                _windowRect.height = WINDOW_HEIGHT_BASE;
            }

            // Center window
            _windowRect.x = (Screen.width - _windowRect.width) / 2f;
            _windowRect.y = (Screen.height - _windowRect.height) / 2f;

            // Draw window
            _windowRect = GUI.Window(9999, _windowRect, DrawWindow, "", _windowStyle);

            // Restore
            GUI.color = oldColor;
            GUI.skin = oldSkin;
        }

        // =============================================================================
        // WINDOW DRAWING
        // =============================================================================

        private void DrawWindow(int windowId)
        {
            float padding = 20f;
            float y = padding;

            // Icon and Title
            string title = _currentUpdate.IsForced ? "⚠️ Update Required" : "🎮 Update Available";
            GUI.Label(new Rect(padding, y, _windowRect.width - padding * 2, 40), title, _titleStyle);
            y += 50f;

            // Version info
            string versionText = $"New Version: {_currentUpdate.LatestVersion}";
            GUI.Label(new Rect(padding, y, _windowRect.width - padding * 2, 25), versionText, _versionStyle);
            y += 30f;

            string currentText = $"Current Version: {_updater.CurrentVersion}";
            GUI.Label(new Rect(padding, y, _windowRect.width - padding * 2, 20), currentText, _labelStyle);
            y += 25f;

            // File size
            if (_currentUpdate.FileSize > 0)
            {
                string sizeText = $"Download Size: {_currentUpdate.FileSizeFormatted}";
                GUI.Label(new Rect(padding, y, _windowRect.width - padding * 2, 20), sizeText, _labelStyle);
                y += 25f;
            }

            // Description
            y += 10f;
            string description = _currentUpdate.IsForced
                ? "This update is required to continue playing. Please update to the latest version."
                : "A new version is available with improvements and bug fixes.";
            GUI.Label(new Rect(padding, y, _windowRect.width - padding * 2, 40), description, _labelStyle);
            y += 50f;

            // Release notes toggle
            if (!string.IsNullOrEmpty(_currentUpdate.ReleaseNotes))
            {
                string notesButtonText = _showReleaseNotes ? "▼ Hide Release Notes" : "▶ Show Release Notes";
                if (GUI.Button(new Rect(padding, y, 180, 25), notesButtonText, _skipButtonStyle))
                {
                    _showReleaseNotes = !_showReleaseNotes;
                }
                y += 30f;

                // Release notes content
                if (_showReleaseNotes)
                {
                    GUI.Label(
                        new Rect(padding, y, _windowRect.width - padding * 2, 100),
                        _currentUpdate.ReleaseNotes,
                        _releaseNotesStyle
                    );
                    y += 110f;
                }
            }

            // Buttons
            y = _windowRect.height - 70f;
            float buttonWidth = _currentUpdate.IsForced ? _windowRect.width - padding * 2 : 160f;
            float buttonHeight = 45f;

            // Update button
            if (GUI.Button(new Rect(padding, y, buttonWidth, buttonHeight), "Update Now", _buttonStyle))
            {
                _updater.StartUpdate();
            }

            // Skip button (only for optional updates)
            if (!_currentUpdate.IsForced)
            {
                float skipX = _windowRect.width - padding - 100f;
                if (GUI.Button(new Rect(skipX, y, 100f, buttonHeight), "Later", _skipButtonStyle))
                {
                    _updater.SkipUpdate();
                }
            }
        }

        // =============================================================================
        // STYLE INITIALIZATION
        // =============================================================================

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Window style
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = MakeTexture(2, 2, new Color(0.12f, 0.12f, 0.15f, 0.98f)) },
                border = new RectOffset(12, 12, 12, 12)
            };

            // Title style
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                wordWrap = true
            };

            // Version style
            _versionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };

            // Label style
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                wordWrap = true
            };

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { 
                    background = MakeTexture(2, 2, new Color(0.2f, 0.7f, 0.3f)), 
                    textColor = Color.white 
                },
                hover = { 
                    background = MakeTexture(2, 2, new Color(0.25f, 0.8f, 0.35f)), 
                    textColor = Color.white 
                },
                active = { 
                    background = MakeTexture(2, 2, new Color(0.15f, 0.6f, 0.25f)), 
                    textColor = Color.white 
                },
                padding = new RectOffset(20, 20, 10, 10)
            };

            // Skip button style
            _skipButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                normal = { 
                    background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.35f)), 
                    textColor = new Color(0.7f, 0.7f, 0.7f) 
                },
                hover = { 
                    background = MakeTexture(2, 2, new Color(0.4f, 0.4f, 0.45f)), 
                    textColor = Color.white 
                },
                active = { 
                    background = MakeTexture(2, 2, new Color(0.25f, 0.25f, 0.3f)), 
                    textColor = Color.white 
                },
                padding = new RectOffset(15, 15, 8, 8)
            };

            // Release notes style
            _releaseNotesStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { 
                    textColor = new Color(0.7f, 0.7f, 0.7f),
                    background = MakeTexture(2, 2, new Color(0.08f, 0.08f, 0.1f, 0.8f))
                },
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Private Room UI Manager
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using Deskillz.Rooms;

namespace Deskillz.UI.Rooms
{
    /// <summary>
    /// Main manager for all Private Room UI components.
    /// Provides one-call methods to show room-related screens.
    /// 
    /// Usage:
    /// <code>
    /// // Show room browser
    /// PrivateRoomUI.Instance.ShowRoomList();
    /// 
    /// // Show create room form
    /// PrivateRoomUI.Instance.ShowCreateRoom();
    /// 
    /// // Show join by code dialog
    /// PrivateRoomUI.Instance.ShowJoinRoom();
    /// </code>
    /// </summary>
    public class PrivateRoomUI : MonoBehaviour
    {
        // =====================================================================
        // SINGLETON
        // =====================================================================

        private static PrivateRoomUI _instance;

        public static PrivateRoomUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PrivateRoomUI>();
                    if (_instance == null)
                    {
                        CreateInstance();
                    }
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        // =====================================================================
        // UI COMPONENTS
        // =====================================================================

        [Header("UI Panels")]
        [SerializeField] private RoomListUI _roomList;
        [SerializeField] private CreateRoomUI _createRoom;
        [SerializeField] private JoinRoomUI _joinRoom;
        [SerializeField] private RoomLobbyUI _roomLobby;

        [Header("Settings")]
        [SerializeField] private bool _autoShowLobbyOnJoin = true;
        [SerializeField] private bool _autoHideOnMatchStart = true;

        // =====================================================================
        // STATE
        // =====================================================================

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private DeskillzTheme _theme;
        private bool _isInitialized;

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when any room UI panel is shown</summary>
        public event Action<string> OnPanelShown;

        /// <summary>Called when all room UI is hidden</summary>
        public event Action OnAllHidden;

        /// <summary>Called when room is created from UI</summary>
        public event Action<PrivateRoom> OnRoomCreated;

        /// <summary>Called when room is joined from UI</summary>
        public event Action<PrivateRoom> OnRoomJoined;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>Whether any room UI panel is visible</summary>
        public bool IsVisible => 
            (_roomList?.IsVisible ?? false) ||
            (_createRoom?.IsVisible ?? false) ||
            (_joinRoom?.IsVisible ?? false) ||
            (_roomLobby?.IsVisible ?? false);

        /// <summary>Current theme</summary>
        public DeskillzTheme Theme => _theme;

        /// <summary>Main canvas for room UI</summary>
        public Canvas Canvas => _canvas;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        private static void CreateInstance()
        {
            var go = new GameObject("PrivateRoomUI");
            _instance = go.AddComponent<PrivateRoomUI>();
            
            if (DeskillzUIManager.HasInstance)
            {
                go.transform.SetParent(DeskillzUIManager.Instance.transform);
            }
            
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Initialize the Private Room UI system.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Get theme from UIManager or create default
            _theme = DeskillzUIManager.HasInstance 
                ? DeskillzUIManager.Instance.Theme 
                : ScriptableObject.CreateInstance<DeskillzTheme>();

            // Setup canvas
            SetupCanvas();

            // Create UI components
            CreateUIComponents();

            // Subscribe to room events
            SubscribeToEvents();

            _isInitialized = true;
            DeskillzLogger.Debug("[PrivateRoomUI] Initialized");
        }

        private void SetupCanvas()
        {
            // Try to use existing canvas from UIManager
            if (DeskillzUIManager.HasInstance && DeskillzUIManager.Instance.Canvas != null)
            {
                _canvas = DeskillzUIManager.Instance.Canvas;
            }
            else
            {
                // Create our own canvas
                var canvasGo = new GameObject("RoomUICanvas");
                canvasGo.transform.SetParent(transform);

                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 150; // Above other UI

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // Add canvas group for fading
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void CreateUIComponents()
        {
            // Create Room List UI
            if (_roomList == null)
            {
                var go = new GameObject("RoomListUI");
                go.transform.SetParent(_canvas.transform, false);
                _roomList = go.AddComponent<RoomListUI>();
                _roomList.Initialize(_theme);
                _roomList.Hide();
                
                // Wire up navigation
                _roomList.OnCreateRoomClicked += ShowCreateRoom;
                _roomList.OnJoinByCodeClicked += ShowJoinRoom;
                _roomList.OnRoomSelected += HandleRoomSelected;
                _roomList.OnBackClicked += HideAll;
            }

            // Create Create Room UI
            if (_createRoom == null)
            {
                var go = new GameObject("CreateRoomUI");
                go.transform.SetParent(_canvas.transform, false);
                _createRoom = go.AddComponent<CreateRoomUI>();
                _createRoom.Initialize(_theme);
                _createRoom.Hide();
                
                _createRoom.OnRoomCreated += HandleRoomCreated;
                _createRoom.OnBackClicked += ShowRoomList;
            }

            // Create Join Room UI
            if (_joinRoom == null)
            {
                var go = new GameObject("JoinRoomUI");
                go.transform.SetParent(_canvas.transform, false);
                _joinRoom = go.AddComponent<JoinRoomUI>();
                _joinRoom.Initialize(_theme);
                _joinRoom.Hide();
                
                _joinRoom.OnRoomJoined += HandleRoomJoined;
                _joinRoom.OnBackClicked += ShowRoomList;
            }

            // Create Room Lobby UI
            if (_roomLobby == null)
            {
                var go = new GameObject("RoomLobbyUI");
                go.transform.SetParent(_canvas.transform, false);
                _roomLobby = go.AddComponent<RoomLobbyUI>();
                _roomLobby.Initialize(_theme);
                _roomLobby.Hide();
                
                _roomLobby.OnLeaveClicked += HandleLeaveLobby;
            }
        }

        private void SubscribeToEvents()
        {
            DeskillzRooms.OnRoomJoined += HandleDeskillzRoomJoined;
            DeskillzRooms.OnRoomUpdated += HandleRoomUpdated;
            DeskillzRooms.OnMatchLaunching += HandleMatchLaunching;
            DeskillzRooms.OnRoomCancelled += HandleRoomCancelled;
            DeskillzRooms.OnKicked += HandleKicked;
            DeskillzRooms.OnRoomLeft += HandleRoomLeft;
        }

        private void UnsubscribeFromEvents()
        {
            DeskillzRooms.OnRoomJoined -= HandleDeskillzRoomJoined;
            DeskillzRooms.OnRoomUpdated -= HandleRoomUpdated;
            DeskillzRooms.OnMatchLaunching -= HandleMatchLaunching;
            DeskillzRooms.OnRoomCancelled -= HandleRoomCancelled;
            DeskillzRooms.OnKicked -= HandleKicked;
            DeskillzRooms.OnRoomLeft -= HandleRoomLeft;
        }

        // =====================================================================
        // PUBLIC API - SHOW UI
        // =====================================================================

        /// <summary>
        /// Show the room list (browse public rooms).
        /// </summary>
        public void ShowRoomList()
        {
            if (!_isInitialized) Initialize();

            HideAllPanels();
            _roomList?.Show();
            OnPanelShown?.Invoke("RoomList");
        }

        /// <summary>
        /// Show the create room form.
        /// </summary>
        public void ShowCreateRoom()
        {
            if (!_isInitialized) Initialize();

            HideAllPanels();
            _createRoom?.Show();
            OnPanelShown?.Invoke("CreateRoom");
        }

        /// <summary>
        /// Show the join room dialog (enter code).
        /// </summary>
        public void ShowJoinRoom()
        {
            if (!_isInitialized) Initialize();

            HideAllPanels();
            _joinRoom?.Show();
            OnPanelShown?.Invoke("JoinRoom");
        }

        /// <summary>
        /// Show the join room dialog with a pre-filled code.
        /// </summary>
        public void ShowJoinRoom(string roomCode)
        {
            if (!_isInitialized) Initialize();

            HideAllPanels();
            _joinRoom?.Show(roomCode);
            OnPanelShown?.Invoke("JoinRoom");
        }

        /// <summary>
        /// Show the room lobby (waiting room).
        /// </summary>
        public void ShowRoomLobby()
        {
            if (!_isInitialized) Initialize();

            if (!DeskillzRooms.IsInRoom)
            {
                DeskillzLogger.Warning("[PrivateRoomUI] Cannot show lobby: not in a room");
                return;
            }

            HideAllPanels();
            _roomLobby?.SetRoom(DeskillzRooms.CurrentRoom);
            _roomLobby?.Show();
            OnPanelShown?.Invoke("RoomLobby");
        }

        /// <summary>
        /// Show the room lobby with a specific room.
        /// </summary>
        public void ShowRoomLobby(PrivateRoom room)
        {
            if (!_isInitialized) Initialize();

            HideAllPanels();
            _roomLobby?.SetRoom(room);
            _roomLobby?.Show();
            OnPanelShown?.Invoke("RoomLobby");
        }

        /// <summary>
        /// Hide all private room UI panels.
        /// </summary>
        public void HideAll()
        {
            HideAllPanels();
            OnAllHidden?.Invoke();
        }

        private void HideAllPanels()
        {
            _roomList?.Hide();
            _createRoom?.Hide();
            _joinRoom?.Hide();
            _roomLobby?.Hide();
        }

        // =====================================================================
        // QUICK ACTIONS
        // =====================================================================

        /// <summary>
        /// Quick create a room and show lobby.
        /// </summary>
        public void QuickCreateRoom(string roomName, decimal entryFee)
        {
            DeskillzRooms.QuickCreateRoom(roomName, entryFee,
                room =>
                {
                    ShowRoomLobby(room);
                    OnRoomCreated?.Invoke(room);
                },
                error =>
                {
                    ShowNotification($"Failed to create room: {error.Message}", NotificationType.Error);
                });
        }

        /// <summary>
        /// Quick join a room by code and show lobby.
        /// </summary>
        public void QuickJoinRoom(string roomCode)
        {
            DeskillzRooms.JoinRoom(roomCode,
                room =>
                {
                    ShowRoomLobby(room);
                    OnRoomJoined?.Invoke(room);
                },
                error =>
                {
                    ShowNotification($"Failed to join room: {error.Message}", NotificationType.Error);
                });
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private void HandleRoomSelected(PrivateRoom room)
        {
            // Preview room or join directly
            QuickJoinRoom(room.RoomCode);
        }

        private void HandleRoomCreated(PrivateRoom room)
        {
            if (_autoShowLobbyOnJoin)
            {
                ShowRoomLobby(room);
            }
            OnRoomCreated?.Invoke(room);
        }

        private void HandleRoomJoined(PrivateRoom room)
        {
            if (_autoShowLobbyOnJoin)
            {
                ShowRoomLobby(room);
            }
            OnRoomJoined?.Invoke(room);
        }

        private void HandleDeskillzRoomJoined(PrivateRoom room)
        {
            if (_autoShowLobbyOnJoin && !_roomLobby.IsVisible)
            {
                ShowRoomLobby(room);
            }
        }

        private void HandleRoomUpdated(PrivateRoom room)
        {
            if (_roomLobby?.IsVisible == true)
            {
                _roomLobby.UpdateRoom(room);
            }
        }

        private void HandleMatchLaunching(MatchLaunchData data)
        {
            if (_autoHideOnMatchStart)
            {
                HideAll();
            }

            ShowNotification("Match starting!", NotificationType.Success);
            
            // Match starts directly - DeskillzManager handles the transition
            DeskillzLogger.Debug($"[PrivateRoomUI] Match launching: {data.MatchId}");
        }

        private void HandleRoomCancelled(string reason)
        {
            HideAll();
            ShowNotification($"Room cancelled: {reason}", NotificationType.Warning);
        }

        private void HandleKicked(string reason)
        {
            HideAll();
            ShowNotification($"You were removed from the room: {reason}", NotificationType.Warning);
        }

        private void HandleRoomLeft()
        {
            if (_roomLobby?.IsVisible == true)
            {
                ShowRoomList();
            }
        }

        private void HandleLeaveLobby()
        {
            DeskillzRooms.LeaveRoom(
                () => ShowRoomList(),
                error => ShowNotification($"Failed to leave: {error.Message}", NotificationType.Error)
            );
        }

        // =====================================================================
        // NOTIFICATIONS
        // =====================================================================

        private void ShowNotification(string message, NotificationType type)
        {
            if (DeskillzUIManager.HasInstance)
            {
                DeskillzUIManager.Instance.ShowNotification(message, type);
            }
            else
            {
                DeskillzLogger.Debug($"[PrivateRoomUI] Notification ({type}): {message}");
            }
        }

        // =====================================================================
        // THEME
        // =====================================================================

        /// <summary>
        /// Apply a new theme to all room UI components.
        /// </summary>
        public void ApplyTheme(DeskillzTheme theme)
        {
            _theme = theme;
            _roomList?.ApplyTheme(theme);
            _createRoom?.ApplyTheme(theme);
            _joinRoom?.ApplyTheme(theme);
            _roomLobby?.ApplyTheme(theme);
        }
    }

    /// <summary>
    /// Notification types for room UI
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
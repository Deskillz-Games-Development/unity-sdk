// =============================================================================
// Deskillz SDK for Unity - Private Room Management API
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Rooms
{
    /// <summary>
    /// Main API for private room management in the Deskillz SDK.
    /// Allows players to create, join, and manage private rooms for custom matches.
    /// 
    /// Usage:
    /// <code>
    /// // Create a room
    /// DeskillzRooms.CreateRoom(new CreateRoomConfig {
    ///     Name = "My Room",
    ///     EntryFee = 5.00m,
    ///     EntryCurrency = "USDT"
    /// }, room => Debug.Log($"Room created: {room.RoomCode}"));
    /// 
    /// // Join a room
    /// DeskillzRooms.JoinRoom("DSKZ-AB3C", room => Debug.Log("Joined!"));
    /// 
    /// // Set ready
    /// DeskillzRooms.SetReady(true);
    /// </code>
    /// </summary>
    public static class DeskillzRooms
    {
        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Called when successfully joined a room</summary>
        public static event Action<PrivateRoom> OnRoomJoined;

        /// <summary>Called when room state updates (player changes, status changes)</summary>
        public static event Action<PrivateRoom> OnRoomUpdated;

        /// <summary>Called when a player joins the room</summary>
        public static event Action<RoomPlayer> OnPlayerJoined;

        /// <summary>Called when a player leaves the room</summary>
        public static event Action<string> OnPlayerLeft;

        /// <summary>Called when a player is kicked from the room</summary>
        public static event Action<string> OnPlayerKicked;

        /// <summary>Called when a player's ready status changes</summary>
        public static event Action<string, bool> OnPlayerReadyChanged;

        /// <summary>Called when all players are ready</summary>
        public static event Action OnAllPlayersReady;

        /// <summary>Called when countdown starts</summary>
        public static event Action<int> OnCountdownStarted;

        /// <summary>Called each countdown tick</summary>
        public static event Action<int> OnCountdownTick;

        /// <summary>Called when match is launching</summary>
        public static event Action<MatchLaunchData> OnMatchLaunching;

        /// <summary>Called when room is cancelled</summary>
        public static event Action<string> OnRoomCancelled;

        /// <summary>Called when current user is kicked</summary>
        public static event Action<string> OnKicked;

        /// <summary>Called when chat message is received</summary>
        public static event Action<string, string, string> OnChatReceived;

        /// <summary>Called when an error occurs</summary>
        public static event Action<RoomError> OnError;

        /// <summary>Called when leaving a room</summary>
        public static event Action OnRoomLeft;

        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>Currently connected room, or null if not in a room</summary>
        public static PrivateRoom CurrentRoom { get; private set; }

        /// <summary>Whether currently in a room</summary>
        public static bool IsInRoom => CurrentRoom != null;

        /// <summary>Whether current user is the room host</summary>
        public static bool IsHost => CurrentRoom?.IsCurrentUserHost ?? false;

        /// <summary>Whether WebSocket is connected</summary>
        public static bool IsConnected => _webSocket?.IsConnected ?? false;

        // =====================================================================
        // PRIVATE STATE
        // =====================================================================

        private static RoomWebSocket _webSocket;
        private static bool _isInitialized;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the room system. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Initialize()
        {
            if (_isInitialized) return;

            _webSocket = new RoomWebSocket();
            SubscribeToWebSocketEvents();
            _isInitialized = true;

            DeskillzLogger.Debug("[DeskillzRooms] Initialized");
        }

        /// <summary>
        /// Shutdown the room system. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Shutdown()
        {
            if (!_isInitialized) return;

            if (IsInRoom)
            {
                LeaveRoom();
            }

            UnsubscribeFromWebSocketEvents();
            _webSocket?.Disconnect();
            _webSocket = null;
            _isInitialized = false;

            DeskillzLogger.Debug("[DeskillzRooms] Shutdown");
        }

        // =====================================================================
        // ROOM CREATION
        // =====================================================================

        /// <summary>
        /// Create a new private room for the current game.
        /// </summary>
        /// <param name="config">Room configuration</param>
        /// <param name="onSuccess">Called with room data on success</param>
        /// <param name="onError">Called on error</param>
        public static void CreateRoom(
            CreateRoomConfig config,
            Action<PrivateRoom> onSuccess = null,
            Action<RoomError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            if (IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.AlreadyInRoom, "Already in a room. Leave first."));
                return;
            }

            RoomApiClient.CreateRoom(config,
                room =>
                {
                    CurrentRoom = room;
                    ConnectToRoom(room.Id);
                    onSuccess?.Invoke(room);
                    OnRoomJoined?.Invoke(room);
                    DeskillzLogger.Debug($"[DeskillzRooms] Created room: {room.RoomCode}");
                },
                error =>
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                });
        }

        /// <summary>
        /// Quick create a room with minimal configuration.
        /// Uses default settings for a 2-player match.
        /// </summary>
        /// <param name="roomName">Room display name</param>
        /// <param name="entryFee">Entry fee amount</param>
        /// <param name="onSuccess">Called with room data on success</param>
        /// <param name="onError">Called on error</param>
        public static void QuickCreateRoom(
            string roomName,
            decimal entryFee,
            Action<PrivateRoom> onSuccess = null,
            Action<RoomError> onError = null)
        {
            var defaultCurrency = DeskillzManager.Instance?.Config?.DefaultCurrency ?? "USDT";

            var config = new CreateRoomConfig
            {
                Name = roomName,
                EntryFee = entryFee,
                EntryCurrency = defaultCurrency,
                MinPlayers = 2,
                MaxPlayers = 2,
                Visibility = RoomVisibility.Unlisted
            };

            CreateRoom(config, onSuccess, onError);
        }

        // =====================================================================
        // ROOM DISCOVERY
        // =====================================================================

        /// <summary>
        /// Get public rooms for the current game.
        /// </summary>
        public static void GetPublicRooms(
            Action<List<PrivateRoom>> onSuccess,
            Action<RoomError> onError = null)
        {
            EnsureInitialized();

            var gameId = DeskillzManager.Instance?.Config?.GameId;
            if (string.IsNullOrEmpty(gameId))
            {
                onError?.Invoke(new RoomError(RoomError.Codes.ServerError, "Game ID not configured"));
                return;
            }

            RoomApiClient.GetPublicRooms(gameId, onSuccess, error =>
            {
                onError?.Invoke(error);
                OnError?.Invoke(error);
            });
        }

        /// <summary>
        /// Get rooms created by or joined by current user.
        /// </summary>
        public static void GetMyRooms(
            Action<List<PrivateRoom>> onSuccess,
            Action<RoomError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            RoomApiClient.GetMyRooms(onSuccess, error =>
            {
                onError?.Invoke(error);
                OnError?.Invoke(error);
            });
        }

        /// <summary>
        /// Get room details by code (preview before joining).
        /// </summary>
        public static void GetRoomByCode(
            string roomCode,
            Action<PrivateRoom> onSuccess,
            Action<RoomError> onError = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(roomCode))
            {
                onError?.Invoke(new RoomError(RoomError.Codes.InvalidCode, "Room code is required"));
                return;
            }

            RoomApiClient.GetRoomByCode(roomCode, onSuccess, error =>
            {
                onError?.Invoke(error);
                OnError?.Invoke(error);
            });
        }

        // =====================================================================
        // JOIN / LEAVE
        // =====================================================================

        /// <summary>
        /// Join a room by its code (e.g., "DSKZ-AB3C").
        /// </summary>
        public static void JoinRoom(
            string roomCode,
            Action<PrivateRoom> onSuccess = null,
            Action<RoomError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            if (IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.AlreadyInRoom, "Already in a room. Leave first."));
                return;
            }

            if (string.IsNullOrEmpty(roomCode))
            {
                onError?.Invoke(new RoomError(RoomError.Codes.InvalidCode, "Room code is required"));
                return;
            }

            RoomApiClient.JoinRoom(roomCode,
                room =>
                {
                    CurrentRoom = room;
                    ConnectToRoom(room.Id);
                    onSuccess?.Invoke(room);
                    OnRoomJoined?.Invoke(room);
                    DeskillzLogger.Debug($"[DeskillzRooms] Joined room: {room.RoomCode}");
                },
                error =>
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                });
        }

        /// <summary>
        /// Leave the current room.
        /// </summary>
        public static void LeaveRoom(
            Action onSuccess = null,
            Action<RoomError> onError = null)
        {
            if (!IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotInRoom, "Not in a room"));
                return;
            }

            var roomId = CurrentRoom.Id;
            var roomCode = CurrentRoom.RoomCode;

            RoomApiClient.LeaveRoom(roomId,
                () =>
                {
                    DisconnectFromRoom();
                    CurrentRoom = null;
                    onSuccess?.Invoke();
                    OnRoomLeft?.Invoke();
                    DeskillzLogger.Debug($"[DeskillzRooms] Left room: {roomCode}");
                },
                error =>
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                });
        }

        // =====================================================================
        // READY STATUS
        // =====================================================================

        /// <summary>
        /// Set ready status for current player.
        /// </summary>
        public static void SetReady(bool isReady)
        {
            if (!IsInRoom)
            {
                DeskillzLogger.Warning("[DeskillzRooms] Cannot set ready: not in a room");
                return;
            }

            _webSocket?.SendReady(CurrentRoom.Id, isReady);
        }

        /// <summary>
        /// Toggle ready status.
        /// </summary>
        public static void ToggleReady()
        {
            if (!IsInRoom) return;

            var currentPlayer = CurrentRoom.GetCurrentPlayer();
            if (currentPlayer != null)
            {
                SetReady(!currentPlayer.IsReady);
            }
        }

        // =====================================================================
        // HOST ACTIONS
        // =====================================================================

        /// <summary>
        /// Start the match (host only). Requires all players to be ready.
        /// </summary>
        public static void StartMatch(
            Action onSuccess = null,
            Action<RoomError> onError = null)
        {
            if (!IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotInRoom, "Not in a room"));
                return;
            }

            if (!IsHost)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotHost, "Only the host can start the match"));
                return;
            }

            if (!CurrentRoom.CanStart)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotReady, "Not all players are ready"));
                return;
            }

            _webSocket?.SendStart(CurrentRoom.Id);
            onSuccess?.Invoke();
        }

        /// <summary>
        /// Kick a player from the room (host only).
        /// </summary>
        public static void KickPlayer(
            string playerId,
            Action onSuccess = null,
            Action<RoomError> onError = null)
        {
            if (!IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotInRoom, "Not in a room"));
                return;
            }

            if (!IsHost)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotHost, "Only the host can kick players"));
                return;
            }

            RoomApiClient.KickPlayer(CurrentRoom.Id, playerId,
                () =>
                {
                    onSuccess?.Invoke();
                    DeskillzLogger.Debug($"[DeskillzRooms] Kicked player: {playerId}");
                },
                error =>
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                });
        }

        /// <summary>
        /// Cancel the room (host only).
        /// </summary>
        public static void CancelRoom(
            Action onSuccess = null,
            Action<RoomError> onError = null)
        {
            if (!IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotInRoom, "Not in a room"));
                return;
            }

            if (!IsHost)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotHost, "Only the host can cancel the room"));
                return;
            }

            RoomApiClient.CancelRoom(CurrentRoom.Id,
                () =>
                {
                    DisconnectFromRoom();
                    CurrentRoom = null;
                    onSuccess?.Invoke();
                    DeskillzLogger.Debug("[DeskillzRooms] Room cancelled");
                },
                error =>
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                });
        }

        // =====================================================================
        // CHAT
        // =====================================================================

        /// <summary>
        /// Send a chat message to the room.
        /// </summary>
        public static void SendChat(string message)
        {
            if (!IsInRoom || string.IsNullOrEmpty(message)) return;

            _webSocket?.SendChat(CurrentRoom.Id, message);
        }

        // =====================================================================
        // WEBSOCKET CONNECTION
        // =====================================================================

        private static void ConnectToRoom(string roomId)
        {
            _webSocket?.Connect(roomId);
        }

        private static void DisconnectFromRoom()
        {
            _webSocket?.Disconnect();
        }

        private static void SubscribeToWebSocketEvents()
        {
            if (_webSocket == null) return;

            _webSocket.OnStateUpdate += HandleRoomStateUpdate;
            _webSocket.OnPlayerJoined += HandlePlayerJoined;
            _webSocket.OnPlayerLeft += HandlePlayerLeft;
            _webSocket.OnPlayerKicked += HandlePlayerKicked;
            _webSocket.OnPlayerReady += HandlePlayerReady;
            _webSocket.OnAllReady += HandleAllReady;
            _webSocket.OnCountdownStarted += HandleCountdownStarted;
            _webSocket.OnCountdownTick += HandleCountdownTick;
            _webSocket.OnLaunching += HandleLaunching;
            _webSocket.OnCancelled += HandleCancelled;
            _webSocket.OnKicked += HandleKicked;
            _webSocket.OnChat += HandleChat;
            _webSocket.OnError += HandleError;
        }

        private static void UnsubscribeFromWebSocketEvents()
        {
            if (_webSocket == null) return;

            _webSocket.OnStateUpdate -= HandleRoomStateUpdate;
            _webSocket.OnPlayerJoined -= HandlePlayerJoined;
            _webSocket.OnPlayerLeft -= HandlePlayerLeft;
            _webSocket.OnPlayerKicked -= HandlePlayerKicked;
            _webSocket.OnPlayerReady -= HandlePlayerReady;
            _webSocket.OnAllReady -= HandleAllReady;
            _webSocket.OnCountdownStarted -= HandleCountdownStarted;
            _webSocket.OnCountdownTick -= HandleCountdownTick;
            _webSocket.OnLaunching -= HandleLaunching;
            _webSocket.OnCancelled -= HandleCancelled;
            _webSocket.OnKicked -= HandleKicked;
            _webSocket.OnChat -= HandleChat;
            _webSocket.OnError -= HandleError;
        }

        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================

        private static void HandleRoomStateUpdate(PrivateRoom room)
        {
            CurrentRoom = room;
            OnRoomUpdated?.Invoke(room);
        }

        private static void HandlePlayerJoined(RoomPlayer player)
        {
            if (CurrentRoom != null)
            {
                if (CurrentRoom.Players == null)
                    CurrentRoom.Players = new List<RoomPlayer>();

                if (!CurrentRoom.Players.Exists(p => p.Id == player.Id))
                {
                    CurrentRoom.Players.Add(player);
                    CurrentRoom.CurrentPlayers++;
                }
            }

            OnPlayerJoined?.Invoke(player);
            OnRoomUpdated?.Invoke(CurrentRoom);
        }

        private static void HandlePlayerLeft(string playerId)
        {
            if (CurrentRoom?.Players != null)
            {
                CurrentRoom.Players.RemoveAll(p => p.Id == playerId);
                CurrentRoom.CurrentPlayers = Math.Max(0, CurrentRoom.CurrentPlayers - 1);
            }

            OnPlayerLeft?.Invoke(playerId);
            OnRoomUpdated?.Invoke(CurrentRoom);
        }

        private static void HandlePlayerKicked(string playerId)
        {
            if (CurrentRoom?.Players != null)
            {
                CurrentRoom.Players.RemoveAll(p => p.Id == playerId);
                CurrentRoom.CurrentPlayers = Math.Max(0, CurrentRoom.CurrentPlayers - 1);
            }

            OnPlayerKicked?.Invoke(playerId);
            OnRoomUpdated?.Invoke(CurrentRoom);
        }

        private static void HandlePlayerReady(string playerId, bool isReady, bool allReady)
        {
            if (CurrentRoom?.Players != null)
            {
                var player = CurrentRoom.Players.Find(p => p.Id == playerId);
                if (player != null)
                {
                    player.IsReady = isReady;
                }
            }

            OnPlayerReadyChanged?.Invoke(playerId, isReady);
            OnRoomUpdated?.Invoke(CurrentRoom);

            if (allReady)
            {
                OnAllPlayersReady?.Invoke();
            }
        }

        private static void HandleAllReady(int playerCount)
        {
            OnAllPlayersReady?.Invoke();
        }

        private static void HandleCountdownStarted(int seconds)
        {
            if (CurrentRoom != null)
            {
                CurrentRoom.Status = RoomStatus.Countdown;
            }

            OnCountdownStarted?.Invoke(seconds);
            OnRoomUpdated?.Invoke(CurrentRoom);
        }

        private static void HandleCountdownTick(int seconds)
        {
            OnCountdownTick?.Invoke(seconds);
        }

        private static void HandleLaunching(MatchLaunchData data)
        {
            if (CurrentRoom != null)
            {
                CurrentRoom.Status = RoomStatus.Launching;
                data.RoomCode = CurrentRoom.RoomCode;
            }

            OnMatchLaunching?.Invoke(data);

            // Match starts directly - no deep link needed (already in app)
            // The game scene transition is handled by DeskillzManager
            DeskillzLogger.Debug($"[DeskillzRooms] Match launching: {data.MatchId}");
        }

        private static void HandleCancelled(string reason)
        {
            CurrentRoom = null;
            DisconnectFromRoom();

            OnRoomCancelled?.Invoke(reason);
            DeskillzLogger.Debug($"[DeskillzRooms] Room cancelled: {reason}");
        }

        private static void HandleKicked(string reason)
        {
            CurrentRoom = null;
            DisconnectFromRoom();

            OnKicked?.Invoke(reason);
            DeskillzLogger.Debug($"[DeskillzRooms] Kicked from room: {reason}");
        }

        private static void HandleChat(string senderId, string username, string message)
        {
            OnChatReceived?.Invoke(senderId, username, message);
        }

        private static void HandleError(string errorMessage)
        {
            OnError?.Invoke(new RoomError(RoomError.Codes.ServerError, errorMessage));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private static bool ValidateAuthentication(Action<RoomError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                onError?.Invoke(new RoomError(
                    RoomError.Codes.NotAuthenticated,
                    "Player not authenticated. Call Deskillz.Launch() first."
                ));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Refresh the current room state from the server.
        /// </summary>
        public static void RefreshRoom(
            Action<PrivateRoom> onSuccess = null,
            Action<RoomError> onError = null)
        {
            if (!IsInRoom)
            {
                onError?.Invoke(new RoomError(RoomError.Codes.NotInRoom, "Not in a room"));
                return;
            }

            RoomApiClient.GetRoomById(CurrentRoom.Id,
                room =>
                {
                    CurrentRoom = room;
                    onSuccess?.Invoke(room);
                    OnRoomUpdated?.Invoke(room);
                },
                error =>
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                });
        }
    }
}
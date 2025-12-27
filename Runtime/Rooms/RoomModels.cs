// =============================================================================
// Deskillz SDK for Unity - Private Room Models
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Rooms
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    /// <summary>
    /// Room visibility options
    /// </summary>
    public enum RoomVisibility
    {
        /// <summary>Listed publicly, anyone can see and join</summary>
        PublicListed,
        
        /// <summary>Not listed, but anyone with code can join</summary>
        Unlisted,
        
        /// <summary>Invite only, requires host approval</summary>
        Private
    }

    /// <summary>
    /// Room status states
    /// </summary>
    public enum RoomStatus
    {
        /// <summary>Room is waiting for players</summary>
        Waiting,
        
        /// <summary>All players ready, awaiting start</summary>
        ReadyCheck,
        
        /// <summary>Countdown in progress</summary>
        Countdown,
        
        /// <summary>Match is launching</summary>
        Launching,
        
        /// <summary>Match in progress</summary>
        InProgress,
        
        /// <summary>Match completed</summary>
        Completed,
        
        /// <summary>Room was cancelled</summary>
        Cancelled,
        
        /// <summary>Room expired (24h limit)</summary>
        Expired
    }

    /// <summary>
    /// Room game mode
    /// </summary>
    public enum RoomMode
    {
        /// <summary>Synchronous (real-time) gameplay</summary>
        Sync,
        
        /// <summary>Asynchronous (turn-based) gameplay</summary>
        Async
    }

    // =========================================================================
    // DATA MODELS
    // =========================================================================

    /// <summary>
    /// Private room data
    /// </summary>
    [Serializable]
    public class PrivateRoom
    {
        /// <summary>Unique room identifier</summary>
        public string Id;
        
        /// <summary>Room code for sharing (e.g., DSKZ-AB3C)</summary>
        public string RoomCode;
        
        /// <summary>Room display name</summary>
        public string Name;
        
        /// <summary>Optional room description</summary>
        public string Description;
        
        /// <summary>Room host information</summary>
        public RoomHost Host;
        
        /// <summary>Game information</summary>
        public RoomGame Game;
        
        /// <summary>Game mode (Sync/Async)</summary>
        public RoomMode Mode;
        
        /// <summary>Entry fee amount</summary>
        public decimal EntryFee;
        
        /// <summary>Entry fee currency code</summary>
        public string EntryCurrency;
        
        /// <summary>Current prize pool</summary>
        public decimal PrizePool;
        
        /// <summary>Minimum players to start</summary>
        public int MinPlayers;
        
        /// <summary>Maximum players allowed</summary>
        public int MaxPlayers;
        
        /// <summary>Current number of players</summary>
        public int CurrentPlayers;
        
        /// <summary>Current room status</summary>
        public RoomStatus Status;
        
        /// <summary>Room visibility setting</summary>
        public RoomVisibility Visibility;
        
        /// <summary>Whether join requests require approval</summary>
        public bool InviteRequired;
        
        /// <summary>List of players in room</summary>
        public List<RoomPlayer> Players;
        
        /// <summary>When the room was created</summary>
        public DateTime CreatedAt;
        
        /// <summary>When the room expires</summary>
        public DateTime ExpiresAt;

        // =====================================================================
        // COMPUTED PROPERTIES
        // =====================================================================

        /// <summary>
        /// Check if current user is the room host
        /// </summary>
        public bool IsCurrentUserHost
        {
            get
            {
                if (Host == null || DeskillzManager.Instance?.CurrentPlayer == null)
                    return false;
                return Host.Id == DeskillzManager.Instance.CurrentPlayer.Id;
            }
        }

        /// <summary>
        /// Check if room is full
        /// </summary>
        public bool IsFull => CurrentPlayers >= MaxPlayers;

        /// <summary>
        /// Check if room can be joined
        /// </summary>
        public bool CanJoin => Status == RoomStatus.Waiting && !IsFull;

        /// <summary>
        /// Check if all players are ready
        /// </summary>
        public bool AreAllPlayersReady
        {
            get
            {
                if (Players == null || Players.Count < MinPlayers)
                    return false;
                return Players.TrueForAll(p => p.IsReady);
            }
        }

        /// <summary>
        /// Check if match can be started
        /// </summary>
        public bool CanStart => IsCurrentUserHost && AreAllPlayersReady && CurrentPlayers >= MinPlayers;

        /// <summary>
        /// Get the current player's data in this room
        /// </summary>
        public RoomPlayer GetCurrentPlayer()
        {
            if (Players == null || DeskillzManager.Instance?.CurrentPlayer == null)
                return null;
            
            string currentId = DeskillzManager.Instance.CurrentPlayer.Id;
            return Players.Find(p => p.Id == currentId);
        }

        /// <summary>
        /// Get number of ready players
        /// </summary>
        public int ReadyPlayerCount
        {
            get
            {
                if (Players == null) return 0;
                return Players.FindAll(p => p.IsReady).Count;
            }
        }

        public override string ToString()
        {
            return $"Room({RoomCode}, {Name}, {CurrentPlayers}/{MaxPlayers}, {Status})";
        }
    }

    /// <summary>
    /// Room host information
    /// </summary>
    [Serializable]
    public class RoomHost
    {
        /// <summary>Host user ID</summary>
        public string Id;
        
        /// <summary>Host username</summary>
        public string Username;
        
        /// <summary>Host avatar URL</summary>
        public string AvatarUrl;

        public override string ToString()
        {
            return $"Host({Id}, {Username})";
        }
    }

    /// <summary>
    /// Room game information
    /// </summary>
    [Serializable]
    public class RoomGame
    {
        /// <summary>Game ID</summary>
        public string Id;
        
        /// <summary>Game name</summary>
        public string Name;
        
        /// <summary>Game icon URL</summary>
        public string IconUrl;

        public override string ToString()
        {
            return $"Game({Id}, {Name})";
        }
    }

    /// <summary>
    /// Player in a room
    /// </summary>
    [Serializable]
    public class RoomPlayer
    {
        /// <summary>Player user ID</summary>
        public string Id;
        
        /// <summary>Player username</summary>
        public string Username;
        
        /// <summary>Player avatar URL</summary>
        public string AvatarUrl;
        
        /// <summary>Whether player is ready</summary>
        public bool IsReady;
        
        /// <summary>Whether player is room admin/host</summary>
        public bool IsAdmin;
        
        /// <summary>When player joined the room</summary>
        public DateTime JoinedAt;

        /// <summary>
        /// Check if this is the current user
        /// </summary>
        public bool IsCurrentUser
        {
            get
            {
                if (DeskillzManager.Instance?.CurrentPlayer == null)
                    return false;
                return Id == DeskillzManager.Instance.CurrentPlayer.Id;
            }
        }

        public override string ToString()
        {
            return $"RoomPlayer({Id}, {Username}, Ready={IsReady})";
        }
    }

    // =========================================================================
    // REQUEST/RESPONSE MODELS
    // =========================================================================

    /// <summary>
    /// Configuration for creating a room
    /// </summary>
    [Serializable]
    public class CreateRoomConfig
    {
        /// <summary>Room display name</summary>
        public string Name;
        
        /// <summary>Optional description</summary>
        public string Description;
        
        /// <summary>Entry fee amount</summary>
        public decimal EntryFee;
        
        /// <summary>Entry fee currency code (BTC, ETH, USDT, etc.)</summary>
        public string EntryCurrency;
        
        /// <summary>Minimum players to start (default: 2)</summary>
        public int MinPlayers = 2;
        
        /// <summary>Maximum players allowed (default: 10)</summary>
        public int MaxPlayers = 10;
        
        /// <summary>Room visibility (default: Unlisted)</summary>
        public RoomVisibility Visibility = RoomVisibility.Unlisted;
        
        /// <summary>Game mode (default: Sync)</summary>
        public RoomMode Mode = RoomMode.Sync;
        
        /// <summary>Match duration in seconds (optional)</summary>
        public int? MatchDuration;
        
        /// <summary>Number of rounds (default: 1)</summary>
        public int RoundsCount = 1;
        
        /// <summary>Whether join requests need approval</summary>
        public bool InviteRequired = false;

        /// <summary>
        /// Convert to JSON-friendly dictionary
        /// </summary>
        internal CreateRoomRequest ToRequest(string gameId)
        {
            return new CreateRoomRequest
            {
                name = Name,
                description = Description,
                gameId = gameId,
                entryFee = EntryFee,
                entryCurrency = EntryCurrency,
                minPlayers = MinPlayers,
                maxPlayers = MaxPlayers,
                visibility = Visibility.ToString().ToUpper(),
                mode = Mode.ToString().ToUpper(),
                matchDuration = MatchDuration,
                roundsCount = RoundsCount,
                inviteRequired = InviteRequired
            };
        }
    }

    /// <summary>
    /// Internal request model for API
    /// </summary>
    [Serializable]
    internal class CreateRoomRequest
    {
        public string name;
        public string description;
        public string gameId;
        public decimal entryFee;
        public string entryCurrency;
        public int minPlayers;
        public int maxPlayers;
        public string visibility;
        public string mode;
        public int? matchDuration;
        public int roundsCount;
        public bool inviteRequired;
    }

    /// <summary>
    /// Match launch data received when match starts
    /// </summary>
    [Serializable]
    public class MatchLaunchData
    {
        /// <summary>Match session ID</summary>
        public string MatchId;
        
        /// <summary>Room code the match originated from</summary>
        public string RoomCode;
        
        /// <summary>Deep link URL (for website players)</summary>
        public string DeepLink;
        
        /// <summary>Launch token for authentication</summary>
        public string Token;
        
        /// <summary>Game session ID</summary>
        public string GameSessionId;

        public override string ToString()
        {
            return $"MatchLaunch({MatchId}, Room={RoomCode})";
        }
    }

    /// <summary>
    /// Room error information
    /// </summary>
    [Serializable]
    public class RoomError
    {
        /// <summary>Error code</summary>
        public string Code;
        
        /// <summary>Human-readable error message</summary>
        public string Message;

        public RoomError() { }

        public RoomError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return $"RoomError({Code}: {Message})";
        }

        // Common error codes
        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string NotInRoom = "NOT_IN_ROOM";
            public const string NotHost = "NOT_HOST";
            public const string RoomFull = "ROOM_FULL";
            public const string RoomNotFound = "ROOM_NOT_FOUND";
            public const string InvalidCode = "INVALID_CODE";
            public const string AlreadyInRoom = "ALREADY_IN_ROOM";
            public const string NotReady = "NOT_ALL_READY";
            public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
            public const string RoomExpired = "ROOM_EXPIRED";
            public const string RoomCancelled = "ROOM_CANCELLED";
            public const string NetworkError = "NETWORK_ERROR";
            public const string ServerError = "SERVER_ERROR";
        }
    }

    // =========================================================================
    // WEBSOCKET EVENT MODELS
    // =========================================================================

    /// <summary>
    /// Player joined event data
    /// </summary>
    [Serializable]
    internal class PlayerJoinedEvent
    {
        public string id;
        public string username;
        public string avatarUrl;
    }

    /// <summary>
    /// Player left event data
    /// </summary>
    [Serializable]
    internal class PlayerLeftEvent
    {
        public string id;
    }

    /// <summary>
    /// Player ready event data
    /// </summary>
    [Serializable]
    internal class PlayerReadyEvent
    {
        public string id;
        public bool isReady;
        public bool allReady;
    }

    /// <summary>
    /// Countdown event data
    /// </summary>
    [Serializable]
    internal class CountdownEvent
    {
        public int seconds;
    }

    /// <summary>
    /// Match launching event data
    /// </summary>
    [Serializable]
    internal class LaunchingEvent
    {
        public string matchId;
        public string deepLink;
        public string token;
        public string gameSessionId;
    }

    /// <summary>
    /// Room cancelled event data
    /// </summary>
    [Serializable]
    internal class CancelledEvent
    {
        public string reason;
    }

    /// <summary>
    /// Chat message event data
    /// </summary>
    [Serializable]
    internal class ChatEvent
    {
        public string id;
        public string username;
        public string message;
        public string timestamp;
    }
}
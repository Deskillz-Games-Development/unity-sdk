// =============================================================================
// Deskillz SDK for Unity - Spectator Models
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Spectator
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    /// <summary>
    /// Spectator connection state
    /// </summary>
    public enum SpectatorState
    {
        /// <summary>Not spectating any room</summary>
        Disconnected,

        /// <summary>Connecting to room</summary>
        Connecting,

        /// <summary>Connected and receiving updates</summary>
        Connected,

        /// <summary>Connection error occurred</summary>
        Error
    }

    /// <summary>
    /// Spectator view mode
    /// </summary>
    public enum SpectatorViewMode
    {
        /// <summary>View entire game board/state</summary>
        Overview,

        /// <summary>Follow a specific player</summary>
        FollowPlayer,

        /// <summary>Free camera (if supported)</summary>
        FreeCamera
    }

    // =========================================================================
    // DATA MODELS
    // =========================================================================

    /// <summary>
    /// Spectator session data
    /// </summary>
    [Serializable]
    public class SpectatorSession
    {
        /// <summary>Room ID being spectated</summary>
        public string RoomId;

        /// <summary>Room code</summary>
        public string RoomCode;

        /// <summary>Room name</summary>
        public string RoomName;

        /// <summary>Game name</summary>
        public string GameName;

        /// <summary>Game icon URL</summary>
        public string GameIconUrl;

        /// <summary>Host username</summary>
        public string HostUsername;

        /// <summary>Current spectator count</summary>
        public int SpectatorCount;

        /// <summary>Maximum spectators allowed</summary>
        public int MaxSpectators;

        /// <summary>Whether spectator chat is enabled</summary>
        public bool ChatEnabled;

        /// <summary>Current game state snapshot</summary>
        public GameStateSnapshot CurrentState;

        /// <summary>Players in the room</summary>
        public List<SpectatorPlayerInfo> Players = new List<SpectatorPlayerInfo>();

        /// <summary>Connection timestamp</summary>
        public DateTime ConnectedAt;

        /// <summary>Stream delay in seconds (for anti-cheating)</summary>
        public float StreamDelay;

        public override string ToString()
        {
            return $"SpectatorSession({RoomCode}, {GameName}, {SpectatorCount} viewers)";
        }
    }

    /// <summary>
    /// Player information visible to spectators
    /// </summary>
    [Serializable]
    public class SpectatorPlayerInfo
    {
        /// <summary>Player ID</summary>
        public string Id;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Player avatar URL</summary>
        public string AvatarUrl;

        /// <summary>Seat position</summary>
        public int SeatPosition;

        /// <summary>Current score</summary>
        public int Score;

        /// <summary>Current balance (for social games)</summary>
        public decimal Balance;

        /// <summary>Whether it's this player's turn</summary>
        public bool IsCurrentTurn;

        /// <summary>Whether player is connected</summary>
        public bool IsConnected;

        /// <summary>Player status indicator</summary>
        public string StatusText;

        /// <summary>Color for UI display</summary>
        public string ColorHex;

        public override string ToString()
        {
            return $"SpectatorPlayer({Username}, Score: {Score})";
        }
    }

    /// <summary>
    /// Game state snapshot for spectators
    /// </summary>
    [Serializable]
    public class GameStateSnapshot
    {
        /// <summary>Snapshot timestamp</summary>
        public DateTime Timestamp;

        /// <summary>Current round number</summary>
        public int RoundNumber;

        /// <summary>Game phase/stage</summary>
        public string Phase;

        /// <summary>Time remaining in current phase (seconds)</summary>
        public float TimeRemaining;

        /// <summary>Current pot/prize pool</summary>
        public decimal CurrentPot;

        /// <summary>Game-specific state data (JSON)</summary>
        public string GameData;

        /// <summary>Player scores</summary>
        public Dictionary<string, int> Scores = new Dictionary<string, int>();

        /// <summary>Recent actions for replay</summary>
        public List<GameAction> RecentActions = new List<GameAction>();

        /// <summary>Whether game is paused</summary>
        public bool IsPaused;

        /// <summary>Pause time remaining</summary>
        public float PauseTimeRemaining;
    }

    /// <summary>
    /// Game action for spectator display
    /// </summary>
    [Serializable]
    public class GameAction
    {
        /// <summary>Action ID</summary>
        public string Id;

        /// <summary>Player who performed the action</summary>
        public string PlayerId;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Action type/name</summary>
        public string ActionType;

        /// <summary>Action description</summary>
        public string Description;

        /// <summary>Action data (game-specific)</summary>
        public string Data;

        /// <summary>Action timestamp</summary>
        public DateTime Timestamp;

        /// <summary>Points/value associated with action</summary>
        public int? Points;
    }

    /// <summary>
    /// Spectator chat message
    /// </summary>
    [Serializable]
    public class SpectatorChatMessage
    {
        /// <summary>Message ID</summary>
        public string Id;

        /// <summary>Sender ID</summary>
        public string SenderId;

        /// <summary>Sender username</summary>
        public string Username;

        /// <summary>Message content</summary>
        public string Content;

        /// <summary>Whether sender is a moderator</summary>
        public bool IsModerator;

        /// <summary>Message timestamp</summary>
        public DateTime Timestamp;
    }

    /// <summary>
    /// Room available for spectating
    /// </summary>
    [Serializable]
    public class SpectatorRoom
    {
        /// <summary>Room ID</summary>
        public string Id;

        /// <summary>Room code</summary>
        public string RoomCode;

        /// <summary>Room name</summary>
        public string Name;

        /// <summary>Game name</summary>
        public string GameName;

        /// <summary>Game icon URL</summary>
        public string GameIconUrl;

        /// <summary>Host username</summary>
        public string HostUsername;

        /// <summary>Number of players</summary>
        public int PlayerCount;

        /// <summary>Current spectator count</summary>
        public int SpectatorCount;

        /// <summary>Maximum spectators</summary>
        public int MaxSpectators;

        /// <summary>Current pot/stakes</summary>
        public decimal CurrentPot;

        /// <summary>Entry fee (for context)</summary>
        public decimal EntryFee;

        /// <summary>Currency</summary>
        public string Currency;

        /// <summary>Current round</summary>
        public int CurrentRound;

        /// <summary>Room status</summary>
        public string Status;

        /// <summary>Whether room allows spectators</summary>
        public bool AllowsSpectators => SpectatorCount < MaxSpectators;

        /// <summary>Room started time</summary>
        public DateTime StartedAt;

        public override string ToString()
        {
            return $"SpectatorRoom({RoomCode}, {GameName}, {SpectatorCount}/{MaxSpectators} viewers)";
        }
    }

    /// <summary>
    /// Score update for spectators
    /// </summary>
    [Serializable]
    public class ScoreUpdate
    {
        /// <summary>Player ID</summary>
        public string PlayerId;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Previous score</summary>
        public int PreviousScore;

        /// <summary>New score</summary>
        public int NewScore;

        /// <summary>Score change</summary>
        public int Delta;

        /// <summary>New balance (for social games)</summary>
        public decimal? NewBalance;

        /// <summary>Update timestamp</summary>
        public DateTime Timestamp;
    }

    /// <summary>
    /// Round end data for spectators
    /// </summary>
    [Serializable]
    public class SpectatorRoundEnd
    {
        /// <summary>Round number</summary>
        public int RoundNumber;

        /// <summary>Winner player ID</summary>
        public string WinnerId;

        /// <summary>Winner username</summary>
        public string WinnerUsername;

        /// <summary>Pot won</summary>
        public decimal PotWon;

        /// <summary>Final scores</summary>
        public Dictionary<string, int> FinalScores = new Dictionary<string, int>();

        /// <summary>Round duration in seconds</summary>
        public float DurationSeconds;
    }

    /// <summary>
    /// Spectator error
    /// </summary>
    [Serializable]
    public class SpectatorError
    {
        /// <summary>Error code</summary>
        public string Code;

        /// <summary>Error message</summary>
        public string Message;

        public SpectatorError() { }

        public SpectatorError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return $"SpectatorError({Code}: {Message})";
        }

        // Common error codes
        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string RoomNotFound = "ROOM_NOT_FOUND";
            public const string RoomFull = "ROOM_FULL";
            public const string SpectatingDisabled = "SPECTATING_DISABLED";
            public const string AlreadySpectating = "ALREADY_SPECTATING";
            public const string NotSpectating = "NOT_SPECTATING";
            public const string ConnectionFailed = "CONNECTION_FAILED";
            public const string Disconnected = "DISCONNECTED";
            public const string NetworkError = "NETWORK_ERROR";
            public const string ServerError = "SERVER_ERROR";
        }
    }

    // =========================================================================
    // REQUEST/RESPONSE MODELS
    // =========================================================================

    /// <summary>
    /// Request to join as spectator
    /// </summary>
    [Serializable]
    internal class JoinSpectatorRequest
    {
        public string roomId;
    }

    /// <summary>
    /// Spectator rooms list response
    /// </summary>
    [Serializable]
    internal class SpectatorRoomsResponse
    {
        public List<SpectatorRoom> rooms;
        public int total;
    }

    /// <summary>
    /// Spectator chat request
    /// </summary>
    [Serializable]
    internal class SpectatorChatRequest
    {
        public string roomId;
        public string message;
    }
}
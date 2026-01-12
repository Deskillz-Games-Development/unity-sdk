// =============================================================================
// Deskillz SDK for Unity - Host Spectator Models
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
// HOST-ONLY FEATURE: Data models for host spectator mode.
// Only room hosts can spectate their own private social rooms.
// Hosts can see board/scores but NOT player hands (anti-cheat).
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Host
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    /// <summary>
    /// Host spectator connection state
    /// </summary>
    public enum HostSpectatorState
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
    /// Game category filter for host rooms
    /// </summary>
    public enum GameCategory
    {
        /// <summary>Social games with rake (host spectating allowed)</summary>
        Social,

        /// <summary>Esports tournaments (host spectating NOT allowed)</summary>
        Esports
    }

    // =========================================================================
    // FILTER MODELS
    // =========================================================================

    /// <summary>
    /// Filter for fetching host's own rooms.
    /// Only returns rooms YOU created.
    /// </summary>
    [Serializable]
    public class HostRoomFilter
    {
        /// <summary>Filter by game category (Social only for spectating)</summary>
        public GameCategory GameCategory = GameCategory.Social;

        /// <summary>Only return active rooms</summary>
        public bool IsActive = true;

        /// <summary>Optional game ID filter</summary>
        public string GameId;
    }

    // =========================================================================
    // ROOM MODELS
    // =========================================================================

    /// <summary>
    /// Host's room information for spectating.
    /// Only YOUR rooms are returned.
    /// </summary>
    [Serializable]
    public class HostRoom
    {
        /// <summary>Room ID</summary>
        public string Id;

        /// <summary>Room name</summary>
        public string Name;

        /// <summary>Room code for sharing</summary>
        public string RoomCode;

        /// <summary>Game ID</summary>
        public string GameId;

        /// <summary>Game name</summary>
        public string GameName;

        /// <summary>Current player count</summary>
        public int CurrentPlayers;

        /// <summary>Maximum players</summary>
        public int MaxPlayers;

        /// <summary>Current round number</summary>
        public int CurrentRound;

        /// <summary>Total rounds</summary>
        public int TotalRounds;

        /// <summary>Whether game is currently active</summary>
        public bool IsActive;

        /// <summary>Room creation timestamp</summary>
        public string CreatedAt;

        /// <summary>Current pot amount (if applicable)</summary>
        public float CurrentPot;

        /// <summary>Currency code</summary>
        public string Currency;
    }

    // =========================================================================
    // SESSION MODELS
    // =========================================================================

    /// <summary>
    /// Host spectator session data.
    /// Represents an active connection to YOUR room.
    /// </summary>
    [Serializable]
    public class HostSpectatorSession
    {
        /// <summary>Session ID</summary>
        public string SessionId;

        /// <summary>Room ID being monitored</summary>
        public string RoomId;

        /// <summary>Room name</summary>
        public string RoomName;

        /// <summary>Room code</summary>
        public string RoomCode;

        /// <summary>Game ID</summary>
        public string GameId;

        /// <summary>Game name</summary>
        public string GameName;

        /// <summary>Current game state (board/scores, NOT hands)</summary>
        public HostGameStateSnapshot CurrentState;

        /// <summary>Players in the room (public info only)</summary>
        public List<HostPlayerInfo> Players;

        /// <summary>Session start time</summary>
        public string StartedAt;
    }

    // =========================================================================
    // GAME STATE MODELS (HOST VIEW - NO HANDS)
    // =========================================================================

    /// <summary>
    /// Game state snapshot for host spectator.
    /// Contains board state and scores but NOT player hands.
    /// </summary>
    [Serializable]
    public class HostGameStateSnapshot
    {
        /// <summary>Current round number</summary>
        public int CurrentRound;

        /// <summary>Total rounds</summary>
        public int TotalRounds;

        /// <summary>Current game phase</summary>
        public string Phase;

        /// <summary>Current pot amount</summary>
        public float CurrentPot;

        /// <summary>Player whose turn it is</summary>
        public string CurrentTurnPlayerId;

        /// <summary>Turn time remaining in seconds</summary>
        public float TurnTimeRemaining;

        /// <summary>Whether game is paused</summary>
        public bool IsPaused;

        /// <summary>
        /// Board state (game-specific, serialized).
        /// NOTE: Does NOT include player hands or hidden cards.
        /// </summary>
        public string BoardState;

        /// <summary>Player scores (public information)</summary>
        public List<HostPlayerScore> Scores;

        /// <summary>Timestamp of this snapshot</summary>
        public string Timestamp;
    }

    /// <summary>
    /// Player information visible to host spectator.
    /// Does NOT include private info like cards/hands.
    /// </summary>
    [Serializable]
    public class HostPlayerInfo
    {
        /// <summary>Player ID</summary>
        public string PlayerId;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Player avatar URL</summary>
        public string AvatarUrl;

        /// <summary>Current score/points</summary>
        public int Score;

        /// <summary>Current chip stack (for poker-style games)</summary>
        public float ChipStack;

        /// <summary>Whether it's this player's turn</summary>
        public bool IsCurrentTurn;

        /// <summary>Whether player is still active (not folded/busted)</summary>
        public bool IsActive;

        /// <summary>Player's seat position</summary>
        public int SeatPosition;

        // NOTE: No Hand, Cards, or private info fields - anti-cheat protection
    }

    /// <summary>
    /// Player score information for host spectator.
    /// </summary>
    [Serializable]
    public class HostPlayerScore
    {
        /// <summary>Player ID</summary>
        public string PlayerId;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Current score</summary>
        public int Score;

        /// <summary>Rounds won</summary>
        public int RoundsWon;

        /// <summary>Current chip stack</summary>
        public float ChipStack;

        /// <summary>Position/rank</summary>
        public int Position;
    }

    // =========================================================================
    // EVENT MODELS
    // =========================================================================

    /// <summary>
    /// Score update event for host spectator.
    /// </summary>
    [Serializable]
    public class HostScoreUpdate
    {
        /// <summary>Player ID</summary>
        public string PlayerId;

        /// <summary>Previous score</summary>
        public int PreviousScore;

        /// <summary>New score</summary>
        public int NewScore;

        /// <summary>Score change delta</summary>
        public int Delta => NewScore - PreviousScore;

        /// <summary>Reason for change</summary>
        public string Reason;
    }

    /// <summary>
    /// Round result for host spectator.
    /// </summary>
    [Serializable]
    public class HostRoundResult
    {
        /// <summary>Round number</summary>
        public int RoundNumber;

        /// <summary>Winner player ID</summary>
        public string WinnerId;

        /// <summary>Winner username</summary>
        public string WinnerUsername;

        /// <summary>Pot won</summary>
        public float PotWon;

        /// <summary>Final scores for this round</summary>
        public List<HostPlayerScore> FinalScores;

        /// <summary>Round duration in seconds</summary>
        public float DurationSeconds;
    }

    /// <summary>
    /// Game end result for host spectator.
    /// </summary>
    [Serializable]
    public class HostGameEndResult
    {
        /// <summary>Winner player ID</summary>
        public string WinnerId;

        /// <summary>Winner username</summary>
        public string WinnerUsername;

        /// <summary>Final standings</summary>
        public List<HostPlayerScore> FinalStandings;

        /// <summary>Total pot distributed</summary>
        public float TotalPot;

        /// <summary>Your rake earnings from this game</summary>
        public float RakeEarnings;

        /// <summary>Game duration in seconds</summary>
        public float DurationSeconds;
    }

    /// <summary>
    /// Chat message for host spectator.
    /// </summary>
    [Serializable]
    public class HostChatMessage
    {
        /// <summary>Message ID</summary>
        public string MessageId;

        /// <summary>Sender player ID</summary>
        public string SenderId;

        /// <summary>Sender username</summary>
        public string SenderUsername;

        /// <summary>Message content</summary>
        public string Content;

        /// <summary>Timestamp</summary>
        public string Timestamp;
    }

    // =========================================================================
    // ERROR MODELS
    // =========================================================================

    /// <summary>
    /// Error information for host spectator operations.
    /// </summary>
    [Serializable]
    public class HostSpectatorError
    {
        /// <summary>Error code</summary>
        public string Code;

        /// <summary>Error message</summary>
        public string Message;

        public HostSpectatorError() { }

        public HostSpectatorError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>Known error codes</summary>
        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string NotAuthorized = "NOT_AUTHORIZED";
            public const string RoomNotFound = "ROOM_NOT_FOUND";
            public const string NotYourRoom = "NOT_YOUR_ROOM";
            public const string NotSocialRoom = "NOT_SOCIAL_ROOM";
            public const string AlreadyConnecting = "ALREADY_CONNECTING";
            public const string MaxRoomsReached = "MAX_ROOMS_REACHED";
            public const string NetworkError = "NETWORK_ERROR";
            public const string ServerError = "SERVER_ERROR";
            public const string ParseError = "PARSE_ERROR";
            public const string WebSocketError = "WEBSOCKET_ERROR";
        }
    }
}
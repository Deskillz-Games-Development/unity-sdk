// =============================================================================
// Deskillz SDK for Unity - Lobby Models
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
//
// These types support the new lobby architecture where matchmaking
// happens on the Deskillz website and games are launched via deep link.
//
// =============================================================================

using System;
using System.Collections.Generic;

namespace Deskillz
{
    // =============================================================================
    // MATCH INFO (Simplified match data from deep link)
    // =============================================================================

    /// <summary>
    /// Simplified match information received from the Deskillz lobby via deep link.
    /// This is a subset of MatchData focused on what's needed to start gameplay.
    /// </summary>
    [Serializable]
    public class MatchInfo
    {
        /// <summary>Unique match identifier.</summary>
        public string MatchId { get; set; }

        /// <summary>Tournament this match belongs to.</summary>
        public string TournamentId { get; set; }

        /// <summary>Authentication token for this match session.</summary>
        public string Token { get; set; }

        /// <summary>Match gameplay mode.</summary>
        public MatchMode Mode { get; set; }

        /// <summary>Entry fee amount.</summary>
        public decimal EntryFee { get; set; }

        /// <summary>Total prize pool.</summary>
        public decimal PrizePool { get; set; }

        /// <summary>Currency for entry/prizes.</summary>
        public Currency Currency { get; set; }

        /// <summary>Time limit in seconds (0 = no limit).</summary>
        public int TimeLimitSeconds { get; set; }

        /// <summary>Maximum players in this match.</summary>
        public int MaxPlayers { get; set; }

        /// <summary>Whether this is a real-time (synchronous) match.</summary>
        public bool IsRealtime => Mode == MatchMode.Synchronous || Mode == MatchMode.CustomStage;

        /// <summary>Whether this is a test/practice match.</summary>
        public bool IsTestMatch { get; set; }

        /// <summary>Custom parameters from deep link.</summary>
        public Dictionary<string, string> CustomParams { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get a custom parameter value.
        /// </summary>
        public string GetCustomParam(string key, string defaultValue = "")
        {
            return CustomParams.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Get a custom parameter as integer.
        /// </summary>
        public int GetCustomParamInt(string key, int defaultValue = 0)
        {
            if (CustomParams.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Convert to full MatchData for compatibility with existing code.
        /// </summary>
        public MatchData ToMatchData()
        {
            return new MatchData
            {
                MatchId = MatchId,
                TournamentId = TournamentId,
                Mode = Mode,
                EntryFee = EntryFee,
                PrizePool = PrizePool,
                Currency = Currency,
                TimeLimitSeconds = TimeLimitSeconds,
                Status = MatchStatus.Pending,
                CustomParams = CustomParams
            };
        }

        public override string ToString()
        {
            return $"MatchInfo({MatchId}, {Mode}, Entry: {EntryFee} {Currency})";
        }
    }

    // =============================================================================
    // PLAYER PRESENCE (Player in lobby/pre-match room)
    // =============================================================================

    /// <summary>
    /// Represents a player's presence in a lobby or pre-match room.
    /// Used for displaying player lists and ready status.
    /// </summary>
    [Serializable]
    public class PlayerPresence
    {
        /// <summary>Player's unique identifier.</summary>
        public string PlayerId { get; set; }

        /// <summary>Player's display name.</summary>
        public string Username { get; set; }

        /// <summary>Player's avatar URL.</summary>
        public string AvatarUrl { get; set; }

        /// <summary>Player's skill rating/ELO.</summary>
        public int Rating { get; set; }

        /// <summary>Whether the player is ready to start.</summary>
        public bool IsReady { get; set; }

        /// <summary>Whether this player is an NPC/bot.</summary>
        public bool IsNPC { get; set; }

        /// <summary>Whether this is the local player.</summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>When the player joined the room.</summary>
        public DateTime JoinedAt { get; set; }

        /// <summary>Player's connection status.</summary>
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Connected;

        public override string ToString()
        {
            return $"PlayerPresence({PlayerId}, {Username}, Ready: {IsReady})";
        }
    }

    // =============================================================================
    // MATCH STATE (Lobby/pre-match room state)
    // =============================================================================

    /// <summary>
    /// State of a match in the lobby/pre-match phase.
    /// Different from MatchStatus which is for in-game state.
    /// </summary>
    public enum MatchState
    {
        /// <summary>Waiting for players to join.</summary>
        Waiting,

        /// <summary>All players joined, waiting for ready check.</summary>
        ReadyCheck,

        /// <summary>All players ready, countdown started.</summary>
        Countdown,

        /// <summary>Game is launching.</summary>
        Launching,

        /// <summary>Match is in progress (handed off to game).</summary>
        InProgress,

        /// <summary>Match completed.</summary>
        Completed,

        /// <summary>Match was cancelled.</summary>
        Cancelled
    }

    // =============================================================================
    // LOBBY ROOM (Pre-match room state)
    // =============================================================================

    /// <summary>
    /// Represents a lobby room where players gather before a match.
    /// </summary>
    [Serializable]
    public class LobbyRoom
    {
        /// <summary>Room/Match identifier.</summary>
        public string RoomId { get; set; }

        /// <summary>Tournament this room belongs to.</summary>
        public string TournamentId { get; set; }

        /// <summary>Game being played.</summary>
        public string GameId { get; set; }

        /// <summary>Current state of the room.</summary>
        public MatchState State { get; set; }

        /// <summary>Players in the room.</summary>
        public List<PlayerPresence> Players { get; set; } = new List<PlayerPresence>();

        /// <summary>Maximum players allowed.</summary>
        public int MaxPlayers { get; set; }

        /// <summary>Minimum players required to start.</summary>
        public int MinPlayers { get; set; }

        /// <summary>Entry fee amount.</summary>
        public decimal EntryFee { get; set; }

        /// <summary>Currency for entry.</summary>
        public Currency Currency { get; set; }

        /// <summary>When the room was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Countdown seconds remaining (if in countdown).</summary>
        public int CountdownSeconds { get; set; }

        /// <summary>Number of players currently in room.</summary>
        public int PlayerCount => Players?.Count ?? 0;

        /// <summary>Number of ready players.</summary>
        public int ReadyCount
        {
            get
            {
                int count = 0;
                if (Players != null)
                {
                    foreach (var p in Players)
                    {
                        if (p.IsReady) count++;
                    }
                }
                return count;
            }
        }

        /// <summary>Whether all players are ready.</summary>
        public bool AllReady => PlayerCount > 0 && ReadyCount == PlayerCount;

        /// <summary>Whether room has minimum players.</summary>
        public bool HasMinPlayers => PlayerCount >= MinPlayers;

        /// <summary>Whether room is full.</summary>
        public bool IsFull => PlayerCount >= MaxPlayers;

        /// <summary>Whether room can start (all ready + min players).</summary>
        public bool CanStart => AllReady && HasMinPlayers;

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public PlayerPresence GetPlayer(string playerId)
        {
            if (Players == null) return null;
            foreach (var p in Players)
            {
                if (p.PlayerId == playerId) return p;
            }
            return null;
        }

        /// <summary>
        /// Check if a player is in the room.
        /// </summary>
        public bool HasPlayer(string playerId)
        {
            return GetPlayer(playerId) != null;
        }

        public override string ToString()
        {
            return $"LobbyRoom({RoomId}, {State}, {PlayerCount}/{MaxPlayers} players)";
        }
    }
}
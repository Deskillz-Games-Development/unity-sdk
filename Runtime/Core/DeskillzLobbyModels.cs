// =============================================================================
// Deskillz SDK for Unity - Lobby & Tournament Models
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity)
// =============================================================================
//
// Contains lobby architecture types + v3.5.2 tournament models.
//
// v3.5.2 additions:
//   - TournamentListing (full tournament data from API)
//   - TournamentRegistration (user's registration record)
//   - TournamentEnrollmentState (status + booking + schedule)
//   - TournamentSchedule (bracket info, rounds, tables)
//   - TournamentRound / TournamentTable / TableSeat models
//   - RoomInvite model
//
// =============================================================================

using System;
using System.Collections.Generic;

namespace Deskillz
{
    // =========================================================================
    // MATCH INFO (Simplified match data from deep link)
    // =========================================================================

    /// <summary>
    /// Simplified match information received from the Deskillz lobby via deep link.
    /// </summary>
    [Serializable]
    public class MatchInfo
    {
        public string MatchId { get; set; }
        public string TournamentId { get; set; }
        public string Token { get; set; }
        public MatchMode Mode { get; set; }
        public decimal EntryFee { get; set; }
        public decimal PrizePool { get; set; }
        public Currency Currency { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsRealtime => Mode == MatchMode.Synchronous || Mode == MatchMode.CustomStage;
        public bool IsTestMatch { get; set; }
        public Dictionary<string, string> CustomParams { get; set; } = new Dictionary<string, string>();

        public string GetCustomParam(string key, string defaultValue = "")
        {
            return CustomParams.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public int GetCustomParamInt(string key, int defaultValue = 0)
        {
            if (CustomParams.TryGetValue(key, out var value) && int.TryParse(value, out var result))
                return result;
            return defaultValue;
        }

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

        public override string ToString() => $"MatchInfo({MatchId}, {Mode}, Entry: {EntryFee} {Currency})";
    }

    // =========================================================================
    // PLAYER PRESENCE (Player in lobby/pre-match room)
    // =========================================================================

    /// <summary>
    /// Represents a player's presence in a lobby or pre-match room.
    /// </summary>
    [Serializable]
    public class PlayerPresence
    {
        public string PlayerId { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public int Rating { get; set; }
        public bool IsReady { get; set; }
        public bool IsNPC { get; set; }
        public bool IsLocalPlayer { get; set; }
        public DateTime JoinedAt { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Connected;

        public override string ToString() => $"PlayerPresence({PlayerId}, {Username}, Ready: {IsReady})";
    }

    // =========================================================================
    // MATCH STATE (Lobby/pre-match room state)
    // =========================================================================

    /// <summary>
    /// State of a match in the lobby/pre-match phase.
    /// </summary>
    public enum MatchState
    {
        Waiting,
        ReadyCheck,
        Countdown,
        Launching,
        InProgress,
        Completed,
        Cancelled
    }

    // =========================================================================
    // LOBBY ROOM (Pre-match room state)
    // =========================================================================

    /// <summary>
    /// Represents a lobby room where players gather before a match.
    /// </summary>
    [Serializable]
    public class LobbyRoom
    {
        public string RoomId { get; set; }
        public string TournamentId { get; set; }
        public string GameId { get; set; }
        public MatchState State { get; set; }
        public List<PlayerPresence> Players { get; set; } = new List<PlayerPresence>();
        public int MaxPlayers { get; set; }
        public int MinPlayers { get; set; }
        public decimal EntryFee { get; set; }
        public Currency Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CountdownSeconds { get; set; }

        public int PlayerCount => Players?.Count ?? 0;

        public int ReadyCount
        {
            get
            {
                int count = 0;
                if (Players != null)
                    foreach (var p in Players)
                        if (p.IsReady) count++;
                return count;
            }
        }

        public bool AllReady => PlayerCount > 0 && ReadyCount == PlayerCount;
        public bool HasMinPlayers => PlayerCount >= MinPlayers;
        public bool IsFull => PlayerCount >= MaxPlayers;
        public bool CanStart => AllReady && HasMinPlayers;

        public PlayerPresence GetPlayer(string playerId)
        {
            if (Players == null) return null;
            foreach (var p in Players)
                if (p.PlayerId == playerId) return p;
            return null;
        }

        public bool HasPlayer(string playerId) => GetPlayer(playerId) != null;

        public override string ToString() => $"LobbyRoom({RoomId}, {State}, {PlayerCount}/{MaxPlayers} players)";
    }

    // =========================================================================
    // TOURNAMENT LISTING (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Full tournament data from API listing (v3.5.2)
    /// Maps to bridge-types.ts Tournament (extended fields)
    /// </summary>
    [Serializable]
    public class TournamentListing
    {
        public string Id;
        public string Name;
        public string Description;
        public string GameId;
        public string GameName;
        public string GameIconUrl;
        public string Status;
        public string TournamentType;
        public decimal EntryFee;
        public string Currency;
        public decimal PrizePool;
        public int MaxPlayers;
        public int CurrentPlayers;
        public int MinPlayers;
        public int MinPlayersPerTable;
        public int MaxPlayersPerTable;
        public int MatchDurationSeconds;
        public string SocialGameType;
        public DateTime ScheduledStart;
        public DateTime? CheckInOpensAt;
        public DateTime? CheckInClosesAt;
        public DateTime? StartedAt;
        public DateTime? CompletedAt;
        public bool IsFeatured;
        public string CreatedById;
        public string[] Platforms;
        public List<PrizeDistribution> PrizeDistributions;

        public bool IsFull => CurrentPlayers >= MaxPlayers;
        public bool IsFree => EntryFee == 0;
        public bool IsOpen => Status == "OPEN" || Status == "SCHEDULED";
        public bool IsSocialGame => !string.IsNullOrEmpty(SocialGameType);

        public TournamentListing()
        {
            PrizeDistributions = new List<PrizeDistribution>();
        }

        public override string ToString() => $"TournamentListing({Id}, {Name}, {Status})";
    }

    /// <summary>
    /// Prize distribution entry for a tournament
    /// </summary>
    [Serializable]
    public class PrizeDistribution
    {
        public int Rank;
        public decimal Percentage;
        public decimal Amount;
    }

    // =========================================================================
    // TOURNAMENT REGISTRATION (v3.5.2)
    // =========================================================================

    /// <summary>
    /// User's registration record for a tournament (v3.5.2)
    /// </summary>
    [Serializable]
    public class TournamentRegistration
    {
        public string Id;
        public string TournamentId;
        public string TournamentName;
        public string GameName;
        public string Status;
        public string BookingStatus;
        public decimal EntryFee;
        public string Currency;
        public DateTime RegisteredAt;
        public DateTime? CheckedInAt;
        public DateTime ScheduledStart;
        public int? FinalRank;
        public decimal? PrizeWon;

        public bool IsActive =>
            Status == "REGISTERED" || Status == "CHECKED_IN" ||
            Status == "SEATED" || Status == "PLAYING";

        public override string ToString() => $"Registration({TournamentId}, {Status})";
    }

    // =========================================================================
    // TOURNAMENT ENROLLMENT STATE (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Current enrollment state for a user in a tournament (v3.5.2)
    /// Returned by GET /api/v1/tournaments/:id/my-status
    /// </summary>
    [Serializable]
    public class TournamentEnrollmentState
    {
        public string TournamentId;
        public string Status;
        public string BookingStatus;
        public bool IsRegistered;
        public bool IsCheckedIn;
        public bool CanCheckIn;
        public bool CanLeave;
        public DateTime? CheckInOpensAt;
        public DateTime? CheckInClosesAt;
        public TournamentSchedule Schedule;

        /// <summary>Parse Status to UserEnrollmentStatus enum</summary>
        public UserEnrollmentStatus GetEnrollmentStatus()
        {
            if (System.Enum.TryParse<UserEnrollmentStatus>(Status, true, out var result))
                return result;
            return UserEnrollmentStatus.NOT_REGISTERED;
        }

        /// <summary>Parse BookingStatus to BookingStatus enum</summary>
        public Deskillz.BookingStatus GetBookingStatus()
        {
            if (System.Enum.TryParse<Deskillz.BookingStatus>(BookingStatus, true, out var result))
                return result;
            return Deskillz.BookingStatus.REGISTERED;
        }

        public override string ToString() => $"EnrollmentState({TournamentId}, {Status}, Booking: {BookingStatus})";
    }

    // =========================================================================
    // TOURNAMENT SCHEDULE (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Tournament bracket/schedule information (v3.5.2)
    /// Returned by GET /api/v1/tournaments/:id/schedule
    /// </summary>
    [Serializable]
    public class TournamentSchedule
    {
        public string TournamentId;
        public int TotalRounds;
        public int CurrentRound;
        public int TotalPlayers;
        public int PlayersRemaining;
        public int SeatsPerTable;
        public int PlayersAdvancePerTable;
        public List<TournamentRound> Rounds;

        public TournamentSchedule()
        {
            Rounds = new List<TournamentRound>();
        }

        /// <summary>Get the current active round</summary>
        public TournamentRound GetCurrentRound()
        {
            if (Rounds == null) return null;
            foreach (var r in Rounds)
                if (r.RoundNumber == CurrentRound) return r;
            return null;
        }

        public override string ToString() => $"Schedule({TournamentId}, Round {CurrentRound}/{TotalRounds})";
    }

    /// <summary>
    /// A single round in a tournament bracket (v3.5.2)
    /// </summary>
    [Serializable]
    public class TournamentRound
    {
        public string Id;
        public int RoundNumber;
        public string Status;
        public int TotalTables;
        public int SeatsPerTable;
        public int PlayersRemaining;
        public DateTime? StartedAt;
        public DateTime? CompletedAt;
        public List<TournamentTable> Tables;

        public TournamentRound()
        {
            Tables = new List<TournamentTable>();
        }
    }

    /// <summary>
    /// A table within a tournament round (v3.5.2)
    /// </summary>
    [Serializable]
    public class TournamentTable
    {
        public string Id;
        public int TableNumber;
        public string Status;
        public string MatchId;
        public List<TableSeat> Seats;

        public TournamentTable()
        {
            Seats = new List<TableSeat>();
        }
    }

    /// <summary>
    /// A seat at a tournament table (v3.5.2)
    /// </summary>
    [Serializable]
    public class TableSeat
    {
        public int SeatNumber;
        public string PlayerId;
        public string Username;
        public string AvatarUrl;
        public bool IsNPC;
        public int? FinalScore;
        public int? FinalRank;
        public bool IsWinner;
        public string Status;
    }

    // =========================================================================
    // TABLE ASSIGNMENT (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Player's table assignment in the current round (v3.5.2)
    /// Returned by GET /api/v1/tournaments/:id/my-seat
    /// </summary>
    [Serializable]
    public class TableAssignment
    {
        public string TournamentId;
        public int RoundNumber;
        public string TableId;
        public int TableNumber;
        public int SeatNumber;
        public string MatchId;
        public List<TableSeat> Opponents;

        public TableAssignment()
        {
            Opponents = new List<TableSeat>();
        }

        public override string ToString() => $"TableAssignment(Round {RoundNumber}, Table {TableNumber}, Seat {SeatNumber})";
    }

    // =========================================================================
    // ROOM INVITE (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Private room invite (v3.5.2)
    /// </summary>
    [Serializable]
    public class RoomInvite
    {
        public string Id;
        public string RoomId;
        public string RoomCode;
        public string RoomName;
        public string GameName;
        public string SenderUsername;
        public string SenderAvatarUrl;
        public string Message;
        public decimal EntryFee;
        public string Currency;
        public int CurrentPlayers;
        public int MaxPlayers;
        public string Status;
        public DateTime CreatedAt;
        public DateTime? ExpiresAt;

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
        public bool IsPending => Status == "PENDING" && !IsExpired;

        public override string ToString() => $"RoomInvite({Id}, {RoomName}, from: {SenderUsername})";
    }
}
// =============================================================================
// Deskillz SDK for Unity - Phase 6: Custom Stage System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deskillz.Stage
{
    /// <summary>
    /// Represents a stage room instance with players and state.
    /// </summary>
    [Serializable]
    public class StageRoom
    {
        // =============================================================================
        // IDENTIFICATION
        // =============================================================================

        /// <summary>Unique stage ID.</summary>
        public string StageId;

        /// <summary>Invite code for joining.</summary>
        public string InviteCode;

        /// <summary>Host player ID.</summary>
        public string HostId;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        /// <summary>Stage configuration.</summary>
        public StageConfig Config;

        /// <summary>Stage rules.</summary>
        public StageRules Rules;

        // =============================================================================
        // STATE
        // =============================================================================

        /// <summary>Current status.</summary>
        public StageStatus Status = StageStatus.Waiting;

        /// <summary>When stage was created.</summary>
        public DateTime CreatedAt;

        /// <summary>When stage started.</summary>
        public DateTime? StartedAt;

        /// <summary>When stage ended.</summary>
        public DateTime? EndedAt;

        /// <summary>Current round (1-based).</summary>
        public int CurrentRound = 0;

        // =============================================================================
        // PLAYERS
        // =============================================================================

        /// <summary>List of players in the stage.</summary>
        [SerializeField]
        private List<StagePlayer> _players = new List<StagePlayer>();

        /// <summary>List of spectators.</summary>
        [SerializeField]
        private List<StageSpectator> _spectators = new List<StageSpectator>();

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Number of players in stage.</summary>
        public int PlayerCount => _players.Count;

        /// <summary>Number of spectators.</summary>
        public int SpectatorCount => _spectators.Count;

        /// <summary>Whether stage is full.</summary>
        public bool IsFull => _players.Count >= Config.MaxPlayers;

        /// <summary>Whether stage has minimum players.</summary>
        public bool HasMinimumPlayers => _players.Count >= Config.MinPlayers;

        /// <summary>Whether all players are ready.</summary>
        public bool AreAllPlayersReady => _players.Count > 0 && _players.All(p => p.IsReady);

        /// <summary>Number of ready players.</summary>
        public int ReadyPlayerCount => _players.Count(p => p.IsReady);

        /// <summary>Whether local player is the host.</summary>
        public bool IsLocalPlayerHost
        {
            get
            {
                var localId = Deskillz.CurrentPlayer?.Id ?? "local_player";
                return HostId == localId;
            }
        }

        /// <summary>Whether local player is in this stage.</summary>
        public bool ContainsLocalPlayer
        {
            get
            {
                var localId = Deskillz.CurrentPlayer?.Id ?? "local_player";
                return _players.Any(p => p.PlayerId == localId);
            }
        }

        /// <summary>All players (read-only).</summary>
        public IReadOnlyList<StagePlayer> Players => _players;

        /// <summary>All spectators (read-only).</summary>
        public IReadOnlyList<StageSpectator> Spectators => _spectators;

        /// <summary>Total prize pool.</summary>
        public decimal TotalPrizePool => Config.GetTotalPrizePool(_players.Count);

        /// <summary>Time remaining until timeout.</summary>
        public TimeSpan TimeRemaining
        {
            get
            {
                if (Status != StageStatus.Waiting) return TimeSpan.Zero;
                var timeout = CreatedAt.AddMinutes(Config.TimeoutMinutes);
                var remaining = timeout - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        /// <summary>Whether stage has timed out.</summary>
        public bool IsTimedOut => TimeRemaining <= TimeSpan.Zero && Status == StageStatus.Waiting;

        // =============================================================================
        // PLAYER MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Add a player to the stage.
        /// </summary>
        public bool AddPlayer(StagePlayer player)
        {
            if (IsFull) return false;
            if (_players.Any(p => p.PlayerId == player.PlayerId)) return false;
            if (Status != StageStatus.Waiting && !Config.AllowJoinInProgress) return false;

            _players.Add(player);
            return true;
        }

        /// <summary>
        /// Remove a player from the stage.
        /// </summary>
        public bool RemovePlayer(string playerId)
        {
            var player = _players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null) return false;

            _players.Remove(player);

            // Transfer host if host left
            if (playerId == HostId && _players.Count > 0)
            {
                TransferHost(_players[0].PlayerId);
            }

            return true;
        }

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public StagePlayer GetPlayer(string playerId)
        {
            return _players.FirstOrDefault(p => p.PlayerId == playerId);
        }

        /// <summary>
        /// Get local player.
        /// </summary>
        public StagePlayer GetLocalPlayer()
        {
            var localId = Deskillz.CurrentPlayer?.Id ?? "local_player";
            return GetPlayer(localId);
        }

        /// <summary>
        /// Get host player.
        /// </summary>
        public StagePlayer GetHost()
        {
            return GetPlayer(HostId);
        }

        /// <summary>
        /// Transfer host to another player.
        /// </summary>
        public void TransferHost(string newHostId)
        {
            var oldHost = GetPlayer(HostId);
            var newHost = GetPlayer(newHostId);

            if (oldHost != null) oldHost.IsHost = false;
            if (newHost != null) newHost.IsHost = true;

            HostId = newHostId;
        }

        /// <summary>
        /// Update player ready status.
        /// </summary>
        public void SetPlayerReady(string playerId, bool ready)
        {
            var player = GetPlayer(playerId);
            if (player != null)
            {
                player.IsReady = ready;
            }
        }

        /// <summary>
        /// Get players sorted by score.
        /// </summary>
        public List<StagePlayer> GetLeaderboard()
        {
            var sorted = new List<StagePlayer>(_players);

            if (Config.ScoreType == ScoreType.LowerIsBetter)
            {
                sorted.Sort((a, b) => a.Score.CompareTo(b.Score));
            }
            else
            {
                sorted.Sort((a, b) => b.Score.CompareTo(a.Score));
            }

            return sorted;
        }

        // =============================================================================
        // SPECTATOR MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Add a spectator.
        /// </summary>
        public bool AddSpectator(StageSpectator spectator)
        {
            if (!Config.AllowSpectators) return false;
            if (_spectators.Count >= Config.MaxSpectators) return false;
            if (_spectators.Any(s => s.UserId == spectator.UserId)) return false;

            _spectators.Add(spectator);
            return true;
        }

        /// <summary>
        /// Remove a spectator.
        /// </summary>
        public bool RemoveSpectator(string oderId)
        {
            var spectator = _spectators.FirstOrDefault(s => s.UserId == oderId);
            if (spectator == null) return false;

            _spectators.Remove(spectator);
            return true;
        }

        // =============================================================================
        // STATE MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Start the stage match.
        /// </summary>
        public void Start()
        {
            if (Status != StageStatus.Waiting) return;

            Status = StageStatus.InProgress;
            StartedAt = DateTime.UtcNow;
            CurrentRound = 1;
        }

        /// <summary>
        /// End the stage.
        /// </summary>
        public void End()
        {
            Status = StageStatus.Completed;
            EndedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancel the stage.
        /// </summary>
        public void Cancel()
        {
            Status = StageStatus.Cancelled;
            EndedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Advance to next round.
        /// </summary>
        public void NextRound()
        {
            if (CurrentRound < Config.Rounds)
            {
                CurrentRound++;
            }
            else
            {
                End();
            }
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        /// <summary>
        /// Get stage duration.
        /// </summary>
        public TimeSpan GetDuration()
        {
            if (StartedAt == null) return TimeSpan.Zero;
            var end = EndedAt ?? DateTime.UtcNow;
            return end - StartedAt.Value;
        }

        /// <summary>
        /// Get formatted player count.
        /// </summary>
        public string GetPlayerCountText()
        {
            return $"{_players.Count}/{Config.MaxPlayers}";
        }

        /// <summary>
        /// Get status display text.
        /// </summary>
        public string GetStatusText()
        {
            return Status switch
            {
                StageStatus.Waiting => "Waiting for players",
                StageStatus.Starting => "Starting soon",
                StageStatus.InProgress => $"Round {CurrentRound}/{Config.Rounds}",
                StageStatus.Completed => "Completed",
                StageStatus.Cancelled => "Cancelled",
                _ => Status.ToString()
            };
        }

        /// <summary>
        /// Check if player can join.
        /// </summary>
        public StageJoinResult CanJoin(string playerId, int? playerElo = null)
        {
            if (_players.Any(p => p.PlayerId == playerId))
                return StageJoinResult.AlreadyJoined;

            if (IsFull)
                return StageJoinResult.StageFull;

            if (Status != StageStatus.Waiting && !Config.AllowJoinInProgress)
                return StageJoinResult.StageStarted;

            if (IsTimedOut)
                return StageJoinResult.StageExpired;

            if (Config.SkillRestricted && playerElo.HasValue)
            {
                if (playerElo.Value < Config.MinElo)
                    return StageJoinResult.EloTooLow;
                if (playerElo.Value > Config.MaxElo)
                    return StageJoinResult.EloTooHigh;
            }

            return StageJoinResult.Success;
        }
    }

    // =============================================================================
    // STAGE STATUS
    // =============================================================================

    /// <summary>
    /// Stage lifecycle status.
    /// </summary>
    public enum StageStatus
    {
        /// <summary>Waiting for players.</summary>
        Waiting,

        /// <summary>About to start.</summary>
        Starting,

        /// <summary>Match in progress.</summary>
        InProgress,

        /// <summary>Match completed.</summary>
        Completed,

        /// <summary>Stage was cancelled.</summary>
        Cancelled,

        /// <summary>Stage timed out.</summary>
        TimedOut
    }

    // =============================================================================
    // STAGE JOIN RESULT
    // =============================================================================

    /// <summary>
    /// Result of attempting to join a stage.
    /// </summary>
    public enum StageJoinResult
    {
        Success,
        StageFull,
        StageStarted,
        StageExpired,
        AlreadyJoined,
        EloTooLow,
        EloTooHigh,
        PasswordRequired,
        PasswordIncorrect,
        NotInvited,
        Banned
    }

    // =============================================================================
    // STAGE PLAYER
    // =============================================================================

    /// <summary>
    /// Player in a stage.
    /// </summary>
    [Serializable]
    public class StagePlayer
    {
        /// <summary>Player ID.</summary>
        public string PlayerId;

        /// <summary>Display username.</summary>
        public string Username;

        /// <summary>Avatar URL.</summary>
        public string AvatarUrl;

        /// <summary>Whether this player is the host.</summary>
        public bool IsHost;

        /// <summary>Whether player is ready.</summary>
        public bool IsReady;

        /// <summary>Current score.</summary>
        public int Score;

        /// <summary>Final rank (after match).</summary>
        public int FinalRank;

        /// <summary>Prize won.</summary>
        public decimal PrizeWon;

        /// <summary>Player ELO rating.</summary>
        public int Elo;

        /// <summary>When player joined.</summary>
        public DateTime JoinedAt;

        /// <summary>Team ID (for team modes).</summary>
        public int TeamId;

        /// <summary>Connection status.</summary>
        public PlayerConnectionStatus ConnectionStatus = PlayerConnectionStatus.Connected;

        /// <summary>
        /// Get status display text.
        /// </summary>
        public string GetStatusText()
        {
            if (IsHost) return "Host";
            if (IsReady) return "Ready";
            return "Not Ready";
        }
    }

    /// <summary>
    /// Player connection status.
    /// </summary>
    public enum PlayerConnectionStatus
    {
        Connected,
        Connecting,
        Disconnected,
        TimedOut
    }

    // =============================================================================
    // STAGE SPECTATOR
    // =============================================================================

    /// <summary>
    /// Spectator in a stage.
    /// </summary>
    [Serializable]
    public class StageSpectator
    {
        /// <summary>User ID.</summary>
        public string UserId;

        /// <summary>Display username.</summary>
        public string Username;

        /// <summary>When spectator joined.</summary>
        public DateTime JoinedAt;
    }

    // =============================================================================
    // STAGE INFO (LIST VIEW)
    // =============================================================================

    /// <summary>
    /// Stage information for list display (lightweight).
    /// </summary>
    [Serializable]
    public class StageInfo
    {
        /// <summary>Stage ID.</summary>
        public string StageId;

        /// <summary>Stage name.</summary>
        public string Name;

        /// <summary>Host username.</summary>
        public string HostName;

        /// <summary>Game name.</summary>
        public string GameName;

        /// <summary>Current player count.</summary>
        public int PlayerCount;

        /// <summary>Maximum players.</summary>
        public int MaxPlayers;

        /// <summary>Entry fee.</summary>
        public decimal EntryFee;

        /// <summary>Currency.</summary>
        public string Currency;

        /// <summary>Status.</summary>
        public StageStatus Status;

        /// <summary>ELO range (if restricted).</summary>
        public string EloRange;

        /// <summary>Whether password protected.</summary>
        public bool HasPassword;

        /// <summary>
        /// Get formatted entry fee text.
        /// </summary>
        public string GetEntryFeeText()
        {
            return EntryFee > 0 ? $"{EntryFee} {Currency}" : "Free";
        }

        /// <summary>
        /// Get formatted player count.
        /// </summary>
        public string GetPlayerCountText()
        {
            return $"{PlayerCount}/{MaxPlayers}";
        }

        /// <summary>
        /// Check if stage is joinable.
        /// </summary>
        public bool IsJoinable => Status == StageStatus.Waiting && PlayerCount < MaxPlayers;
    }
}

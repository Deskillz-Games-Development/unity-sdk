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
    /// Extension methods and utilities for the stage system.
    /// </summary>
    public static class StageExtensions
    {
        // =============================================================================
        // STAGE ROOM EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get available slots in stage.
        /// </summary>
        public static int GetAvailableSlots(this StageRoom stage)
        {
            return stage.Config.MaxPlayers - stage.PlayerCount;
        }

        /// <summary>
        /// Check if stage accepts more players.
        /// </summary>
        public static bool CanAcceptPlayers(this StageRoom stage)
        {
            return !stage.IsFull && 
                   (stage.Status == StageStatus.Waiting || 
                    (stage.Status == StageStatus.InProgress && stage.Config.AllowJoinInProgress));
        }

        /// <summary>
        /// Get all ready players.
        /// </summary>
        public static IEnumerable<StagePlayer> GetReadyPlayers(this StageRoom stage)
        {
            return stage.Players.Where(p => p.IsReady);
        }

        /// <summary>
        /// Get all unready players.
        /// </summary>
        public static IEnumerable<StagePlayer> GetUnreadyPlayers(this StageRoom stage)
        {
            return stage.Players.Where(p => !p.IsReady);
        }

        /// <summary>
        /// Get players by team.
        /// </summary>
        public static IEnumerable<StagePlayer> GetTeam(this StageRoom stage, int teamId)
        {
            return stage.Players.Where(p => p.TeamId == teamId);
        }

        /// <summary>
        /// Check if all players from a team are ready.
        /// </summary>
        public static bool IsTeamReady(this StageRoom stage, int teamId)
        {
            var team = stage.GetTeam(teamId).ToList();
            return team.Count > 0 && team.All(p => p.IsReady);
        }

        /// <summary>
        /// Get prize for specific position.
        /// </summary>
        public static decimal GetPrizeForPosition(this StageRoom stage, int position)
        {
            var prizes = stage.Config.GetPrizeBreakdown(stage.PlayerCount);
            return prizes.TryGetValue(position, out var prize) ? prize : 0;
        }

        /// <summary>
        /// Format stage info for display.
        /// </summary>
        public static string ToDisplayString(this StageRoom stage)
        {
            return $"{stage.Config.Name} | {stage.GetPlayerCountText()} | {stage.GetStatusText()}";
        }

        // =============================================================================
        // STAGE CONFIG EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Check if config has entry fee.
        /// </summary>
        public static bool HasEntryFee(this StageConfig config)
        {
            return config.EntryFee > 0;
        }

        /// <summary>
        /// Get formatted entry fee.
        /// </summary>
        public static string GetFormattedEntryFee(this StageConfig config)
        {
            if (config.EntryFee <= 0) return "Free";
            return $"{config.EntryFee} {config.Currency}";
        }

        /// <summary>
        /// Get formatted time limit.
        /// </summary>
        public static string GetFormattedTimeLimit(this StageConfig config)
        {
            if (config.TimeLimitSeconds <= 0) return "No Limit";
            var ts = TimeSpan.FromSeconds(config.TimeLimitSeconds);
            return ts.TotalMinutes >= 1 ? $"{ts.TotalMinutes:F0} min" : $"{ts.TotalSeconds:F0} sec";
        }

        /// <summary>
        /// Get formatted player range.
        /// </summary>
        public static string GetPlayerRangeText(this StageConfig config)
        {
            if (config.MinPlayers == config.MaxPlayers)
                return $"{config.MaxPlayers} players";
            return $"{config.MinPlayers}-{config.MaxPlayers} players";
        }

        /// <summary>
        /// Get formatted ELO range.
        /// </summary>
        public static string GetEloRangeText(this StageConfig config)
        {
            if (!config.SkillRestricted) return "All skill levels";
            if (config.MinElo > 0 && config.MaxElo < 9999)
                return $"{config.MinElo} - {config.MaxElo} ELO";
            if (config.MinElo > 0)
                return $"{config.MinElo}+ ELO";
            if (config.MaxElo < 9999)
                return $"Under {config.MaxElo} ELO";
            return "All skill levels";
        }

        /// <summary>
        /// Create a modified copy with different entry fee.
        /// </summary>
        public static StageConfig WithEntryFee(this StageConfig config, decimal entryFee, string currency = null)
        {
            var copy = config.Clone();
            copy.EntryFee = entryFee;
            if (!string.IsNullOrEmpty(currency))
                copy.Currency = currency;
            return copy;
        }

        /// <summary>
        /// Create a modified copy with different player limits.
        /// </summary>
        public static StageConfig WithPlayerLimits(this StageConfig config, int min, int max)
        {
            var copy = config.Clone();
            copy.MinPlayers = min;
            copy.MaxPlayers = max;
            return copy;
        }

        // =============================================================================
        // STAGE PLAYER EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Check if player is local.
        /// </summary>
        public static bool IsLocalPlayer(this StagePlayer player)
        {
            var localId = Deskillz.CurrentPlayer?.Id ?? "local_player";
            return player.PlayerId == localId;
        }

        /// <summary>
        /// Get time since player joined.
        /// </summary>
        public static TimeSpan GetTimeSinceJoin(this StagePlayer player)
        {
            return DateTime.UtcNow - player.JoinedAt;
        }

        /// <summary>
        /// Get formatted join time.
        /// </summary>
        public static string GetJoinTimeText(this StagePlayer player)
        {
            var elapsed = player.GetTimeSinceJoin();
            if (elapsed.TotalSeconds < 60) return "Just now";
            if (elapsed.TotalMinutes < 60) return $"{elapsed.TotalMinutes:F0}m ago";
            return $"{elapsed.TotalHours:F0}h ago";
        }

        // =============================================================================
        // STAGE INFO EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Convert StageInfo to filter-friendly string.
        /// </summary>
        public static string ToSearchString(this StageInfo info)
        {
            return $"{info.Name} {info.HostName} {info.GameName}".ToLower();
        }

        /// <summary>
        /// Check if stage matches search query.
        /// </summary>
        public static bool MatchesSearch(this StageInfo info, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            return info.ToSearchString().Contains(query.ToLower());
        }

        // =============================================================================
        // LIST EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Filter stages by game.
        /// </summary>
        public static List<StageInfo> FilterByGame(this List<StageInfo> stages, string gameName)
        {
            return stages.Where(s => s.GameName.Equals(gameName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Filter stages by entry fee range.
        /// </summary>
        public static List<StageInfo> FilterByEntryFee(this List<StageInfo> stages, decimal min, decimal max)
        {
            return stages.Where(s => s.EntryFee >= min && s.EntryFee <= max).ToList();
        }

        /// <summary>
        /// Filter joinable stages only.
        /// </summary>
        public static List<StageInfo> FilterJoinable(this List<StageInfo> stages)
        {
            return stages.Where(s => s.IsJoinable).ToList();
        }

        /// <summary>
        /// Sort stages by player count.
        /// </summary>
        public static List<StageInfo> SortByPlayerCount(this List<StageInfo> stages, bool descending = true)
        {
            return descending 
                ? stages.OrderByDescending(s => s.PlayerCount).ToList()
                : stages.OrderBy(s => s.PlayerCount).ToList();
        }

        /// <summary>
        /// Sort stages by entry fee.
        /// </summary>
        public static List<StageInfo> SortByEntryFee(this List<StageInfo> stages, bool descending = false)
        {
            return descending
                ? stages.OrderByDescending(s => s.EntryFee).ToList()
                : stages.OrderBy(s => s.EntryFee).ToList();
        }
    }

    // =============================================================================
    // STAGE FILTER
    // =============================================================================

    /// <summary>
    /// Filter options for stage browser.
    /// </summary>
    [Serializable]
    public class StageFilter
    {
        /// <summary>Search query.</summary>
        public string SearchQuery;

        /// <summary>Filter by game name.</summary>
        public string GameName;

        /// <summary>Minimum entry fee.</summary>
        public decimal MinEntryFee;

        /// <summary>Maximum entry fee.</summary>
        public decimal MaxEntryFee = decimal.MaxValue;

        /// <summary>Only show joinable stages.</summary>
        public bool JoinableOnly = true;

        /// <summary>Only show free stages.</summary>
        public bool FreeOnly;

        /// <summary>Only show stages with available slots.</summary>
        public bool HasSlotsOnly = true;

        /// <summary>Game mode filter.</summary>
        public StageGameMode? GameMode;

        /// <summary>
        /// Apply filter to stage list.
        /// </summary>
        public List<StageInfo> Apply(List<StageInfo> stages)
        {
            var result = new List<StageInfo>(stages);

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                result = result.Where(s => s.MatchesSearch(SearchQuery)).ToList();
            }

            if (!string.IsNullOrEmpty(GameName))
            {
                result = result.FilterByGame(GameName);
            }

            if (JoinableOnly)
            {
                result = result.FilterJoinable();
            }

            if (FreeOnly)
            {
                result = result.Where(s => s.EntryFee == 0).ToList();
            }

            if (HasSlotsOnly)
            {
                result = result.Where(s => s.PlayerCount < s.MaxPlayers).ToList();
            }

            result = result.FilterByEntryFee(MinEntryFee, MaxEntryFee);

            return result;
        }

        /// <summary>
        /// Create default filter.
        /// </summary>
        public static StageFilter Default => new StageFilter();

        /// <summary>
        /// Create free-only filter.
        /// </summary>
        public static StageFilter FreeStages => new StageFilter { FreeOnly = true };

        /// <summary>
        /// Create paid-only filter.
        /// </summary>
        public static StageFilter PaidStages => new StageFilter { MinEntryFee = 0.01m };
    }

    // =============================================================================
    // STAGE SORT OPTIONS
    // =============================================================================

    /// <summary>
    /// Sort options for stage browser.
    /// </summary>
    public enum StageSortOption
    {
        /// <summary>Most players first.</summary>
        MostPlayers,

        /// <summary>Fewest players first.</summary>
        FewestPlayers,

        /// <summary>Lowest entry fee first.</summary>
        LowestFee,

        /// <summary>Highest entry fee first.</summary>
        HighestFee,

        /// <summary>Newest first.</summary>
        Newest,

        /// <summary>Almost full first.</summary>
        AlmostFull
    }

    /// <summary>
    /// Stage sorter utility.
    /// </summary>
    public static class StageSorter
    {
        /// <summary>
        /// Sort stages by option.
        /// </summary>
        public static List<StageInfo> Sort(List<StageInfo> stages, StageSortOption option)
        {
            return option switch
            {
                StageSortOption.MostPlayers => stages.SortByPlayerCount(true),
                StageSortOption.FewestPlayers => stages.SortByPlayerCount(false),
                StageSortOption.LowestFee => stages.SortByEntryFee(false),
                StageSortOption.HighestFee => stages.SortByEntryFee(true),
                StageSortOption.AlmostFull => stages.OrderByDescending(s => (float)s.PlayerCount / s.MaxPlayers).ToList(),
                _ => stages
            };
        }
    }

    // =============================================================================
    // STAGE EVENTS
    // =============================================================================

    /// <summary>
    /// Stage-related event data.
    /// </summary>
    public class StageEventData
    {
        public string StageId { get; set; }
        public string EventType { get; set; }
        public string PlayerId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// Common stage event types.
    /// </summary>
    public static class StageEventTypes
    {
        public const string PlayerJoined = "player_joined";
        public const string PlayerLeft = "player_left";
        public const string PlayerReady = "player_ready";
        public const string PlayerUnready = "player_unready";
        public const string PlayerKicked = "player_kicked";
        public const string HostChanged = "host_changed";
        public const string ConfigChanged = "config_changed";
        public const string CountdownStarted = "countdown_started";
        public const string StageStarted = "stage_started";
        public const string StageCancelled = "stage_cancelled";
        public const string StageCompleted = "stage_completed";
        public const string ChatMessage = "chat_message";
    }

    // =============================================================================
    // STAGE CHAT
    // =============================================================================

    /// <summary>
    /// Chat message in stage.
    /// </summary>
    [Serializable]
    public class StageChatMessage
    {
        public string MessageId;
        public string PlayerId;
        public string PlayerName;
        public string Content;
        public DateTime Timestamp;
        public ChatMessageType Type;

        public string GetFormattedTime()
        {
            return Timestamp.ToLocalTime().ToString("HH:mm");
        }
    }

    /// <summary>
    /// Chat message types.
    /// </summary>
    public enum ChatMessageType
    {
        Player,
        System,
        Host,
        Announcement
    }

    /// <summary>
    /// Simple stage chat manager.
    /// </summary>
    public class StageChatManager
    {
        private readonly List<StageChatMessage> _messages = new List<StageChatMessage>();
        private const int MAX_MESSAGES = 100;

        public event Action<StageChatMessage> OnMessageReceived;

        public IReadOnlyList<StageChatMessage> Messages => _messages;

        public void AddMessage(StageChatMessage message)
        {
            _messages.Add(message);

            while (_messages.Count > MAX_MESSAGES)
            {
                _messages.RemoveAt(0);
            }

            OnMessageReceived?.Invoke(message);
        }

        public void SendMessage(string content)
        {
            var message = new StageChatMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                PlayerId = Deskillz.CurrentPlayer?.Id,
                PlayerName = Deskillz.CurrentPlayer?.Username,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Type = ChatMessageType.Player
            };

            // Send through multiplayer
            if (Deskillz.Multiplayer.SyncManager.HasInstance)
            {
                Deskillz.Multiplayer.SyncManager.Instance.SendToAll(message);
            }

            AddMessage(message);
        }

        public void Clear()
        {
            _messages.Clear();
        }
    }
}

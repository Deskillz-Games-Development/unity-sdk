// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Player information received from Deskillz app
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>Unique player identifier</summary>
        public string Id { get; set; }
        
        /// <summary>Player display name</summary>
        public string Username { get; set; }
        
        /// <summary>Player avatar URL</summary>
        public string AvatarUrl { get; set; }
        
        /// <summary>Player's connected wallet address</summary>
        public string WalletAddress { get; set; }
        
        /// <summary>Player's role (Player, Developer, Admin)</summary>
        public PlayerRole Role { get; set; }
        
        /// <summary>Player's current level/rank</summary>
        public int Level { get; set; }
        
        /// <summary>Total games played</summary>
        public int TotalGamesPlayed { get; set; }
        
        /// <summary>Total wins</summary>
        public int TotalWins { get; set; }
        
        /// <summary>Win rate percentage</summary>
        public float WinRate => TotalGamesPlayed > 0 ? (float)TotalWins / TotalGamesPlayed * 100f : 0f;
        
        /// <summary>Player's country code (ISO 3166-1 alpha-2)</summary>
        public string CountryCode { get; set; }
        
        /// <summary>Whether this is the local player</summary>
        public bool IsLocalPlayer { get; set; }
        
        /// <summary>Authentication token for API calls</summary>
        internal string AuthToken { get; set; }

        public override string ToString()
        {
            return $"Player({Id}, {Username}, Level {Level})";
        }
    }

    /// <summary>
    /// Match configuration and state data
    /// </summary>
    [Serializable]
    public class MatchData
    {
        /// <summary>Unique match identifier</summary>
        public string MatchId { get; set; }
        
        /// <summary>Tournament ID this match belongs to</summary>
        public string TournamentId { get; set; }
        
        /// <summary>Game ID</summary>
        public string GameId { get; set; }
        
        /// <summary>Match gameplay mode</summary>
        public MatchMode Mode { get; set; }
        
        /// <summary>Current match status</summary>
        public MatchStatus Status { get; set; }
        
        /// <summary>Entry fee amount</summary>
        public decimal EntryFee { get; set; }
        
        /// <summary>Entry fee currency</summary>
        public Currency Currency { get; set; }
        
        /// <summary>Prize pool amount</summary>
        public decimal PrizePool { get; set; }
        
        /// <summary>Time limit in seconds (0 = no limit)</summary>
        public int TimeLimitSeconds { get; set; }
        
        /// <summary>Number of rounds (1 = single round)</summary>
        public int Rounds { get; set; }
        
        /// <summary>Current round number</summary>
        public int CurrentRound { get; set; }
        
        /// <summary>Score comparison type</summary>
        public ScoreType ScoreType { get; set; }
        
        /// <summary>When the match started (UTC)</summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>When the match ends (UTC) - for async</summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>Local player's current score</summary>
        public int LocalPlayerScore { get; set; }
        
        /// <summary>All players in this match</summary>
        public List<MatchPlayer> Players { get; set; } = new List<MatchPlayer>();
        
        /// <summary>Custom stage data (if Mode == CustomStage)</summary>
        public StageData Stage { get; set; }
        
        /// <summary>Additional match parameters from deep link</summary>
        public Dictionary<string, string> CustomParams { get; set; } = new Dictionary<string, string>();

        /// <summary>Time remaining in seconds (if timed match)</summary>
        public float TimeRemaining
        {
            get
            {
                if (TimeLimitSeconds <= 0 || StartTime == null) return -1f;
                var elapsed = (float)(DateTime.UtcNow - StartTime.Value).TotalSeconds;
                return Mathf.Max(0f, TimeLimitSeconds - elapsed);
            }
        }

        /// <summary>Whether this is an async match</summary>
        public bool IsAsync => Mode == MatchMode.Asynchronous;
        
        /// <summary>Whether this is a real-time match</summary>
        public bool IsRealtime => Mode == MatchMode.Synchronous || Mode == MatchMode.CustomStage;

        public override string ToString()
        {
            return $"Match({MatchId}, {Mode}, {Status}, {Players.Count} players)";
        }
    }

    /// <summary>
    /// Player within a match context
    /// </summary>
    [Serializable]
    public class MatchPlayer
    {
        /// <summary>Player ID</summary>
        public string PlayerId { get; set; }
        
        /// <summary>Player username</summary>
        public string Username { get; set; }
        
        /// <summary>Player avatar URL</summary>
        public string AvatarUrl { get; set; }
        
        /// <summary>Current score in this match</summary>
        public int Score { get; set; }
        
        /// <summary>Player's rank/position in this match</summary>
        public int Rank { get; set; }
        
        /// <summary>Whether player is still connected (real-time)</summary>
        public bool IsConnected { get; set; }
        
        /// <summary>Whether this is the local player</summary>
        public bool IsLocalPlayer { get; set; }
        
        /// <summary>Whether player has finished (async)</summary>
        public bool HasFinished { get; set; }
        
        /// <summary>Player's prize amount (after match)</summary>
        public decimal PrizeAmount { get; set; }

        public override string ToString()
        {
            return $"MatchPlayer({PlayerId}, {Username}, Score: {Score})";
        }
    }

    /// <summary>
    /// Match result data after completion
    /// </summary>
    [Serializable]
    public class MatchResult
    {
        /// <summary>Match identifier</summary>
        public string MatchId { get; set; }
        
        /// <summary>Outcome for the local player</summary>
        public MatchOutcome Outcome { get; set; }
        
        /// <summary>Local player's final score</summary>
        public int FinalScore { get; set; }
        
        /// <summary>Local player's final rank</summary>
        public int FinalRank { get; set; }
        
        /// <summary>Prize amount won (if any)</summary>
        public decimal PrizeWon { get; set; }
        
        /// <summary>Prize currency</summary>
        public Currency Currency { get; set; }
        
        /// <summary>All players' final standings</summary>
        public List<MatchPlayer> FinalStandings { get; set; } = new List<MatchPlayer>();
        
        /// <summary>Match duration in seconds</summary>
        public float DurationSeconds { get; set; }
        
        /// <summary>Experience points earned</summary>
        public int XpEarned { get; set; }
        
        /// <summary>Whether player leveled up</summary>
        public bool LeveledUp { get; set; }
        
        /// <summary>New level (if leveled up)</summary>
        public int NewLevel { get; set; }

        /// <summary>Opponent's score (for 1v1 matches)</summary>
        public int OpponentScore { get; set; }

        /// <summary>Whether local player won</summary>
        public bool IsWin => Outcome == MatchOutcome.Win;
        
        /// <summary>Whether local player lost</summary>
        public bool IsLoss => Outcome == MatchOutcome.Loss;

        public override string ToString()
        {
            return $"MatchResult({MatchId}, {Outcome}, Rank {FinalRank}, Prize {PrizeWon} {Currency})";
        }
    }

    /// <summary>
    /// Custom stage/room configuration
    /// </summary>
    [Serializable]
    public class StageData
    {
        /// <summary>Unique stage identifier</summary>
        public string StageId { get; set; }
        
        /// <summary>Stage invite code (e.g., "ABCD-1234")</summary>
        public string InviteCode { get; set; }
        
        /// <summary>Stage name set by creator</summary>
        public string Name { get; set; }
        
        /// <summary>Stage creator's player ID</summary>
        public string CreatorId { get; set; }
        
        /// <summary>Current admin's player ID</summary>
        public string AdminId { get; set; }
        
        /// <summary>Stage visibility setting</summary>
        public StageVisibility Visibility { get; set; }
        
        /// <summary>Maximum players allowed</summary>
        public int MaxPlayers { get; set; }
        
        /// <summary>Current number of players</summary>
        public int CurrentPlayers { get; set; }
        
        /// <summary>Entry fee amount</summary>
        public decimal EntryFee { get; set; }
        
        /// <summary>Entry fee currency</summary>
        public Currency Currency { get; set; }
        
        /// <summary>Number of rounds</summary>
        public int Rounds { get; set; }
        
        /// <summary>Time limit per round in seconds</summary>
        public int TimeLimitSeconds { get; set; }
        
        /// <summary>Players in this stage</summary>
        public List<StagePlayer> Players { get; set; } = new List<StagePlayer>();
        
        /// <summary>When the stage was created</summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>Whether stage is waiting for players</summary>
        public bool IsWaiting { get; set; }
        
        /// <summary>Whether the match has started</summary>
        public bool IsPlaying { get; set; }
        
        /// <summary>Password for private stages (null if no password)</summary>
        public string Password { get; set; }

        /// <summary>Whether local player is the admin</summary>
        public bool IsLocalPlayerAdmin { get; set; }
        
        /// <summary>Whether stage is full</summary>
        public bool IsFull => CurrentPlayers >= MaxPlayers;

        public override string ToString()
        {
            return $"Stage({StageId}, {Name}, {CurrentPlayers}/{MaxPlayers} players)";
        }
    }

    // NOTE: StagePlayer class is defined in StageRoom.cs
    // NOTE: StageConfig class is defined in StageConfig.cs
    // NOTE: NetworkMessage class is defined in NetworkMessage.cs
    // NOTE: PlayerState class is defined in PlayerState.cs

    /// <summary>
    /// SDK error information
    /// </summary>
    [Serializable]
    public class DeskillzError
    {
        /// <summary>Error code</summary>
        public ErrorCode Code { get; set; }
        
        /// <summary>Human-readable error message</summary>
        public string Message { get; set; }
        
        /// <summary>Additional details (optional)</summary>
        public string Details { get; set; }
        
        /// <summary>Whether this error is recoverable</summary>
        public bool IsRecoverable { get; set; }

        public DeskillzError() { }

        public DeskillzError(ErrorCode code, string message, bool recoverable = true)
        {
            Code = code;
            Message = message;
            IsRecoverable = recoverable;
        }

        public override string ToString()
        {
            return $"DeskillzError({Code}: {Message})";
        }

        /// <summary>Create from exception</summary>
        public static DeskillzError FromException(Exception ex)
        {
            return new DeskillzError(ErrorCode.Unknown, ex.Message)
            {
                Details = ex.StackTrace,
                IsRecoverable = false
            };
        }
    }

    /// <summary>
    /// Deep link parameters received when launching from Deskillz app
    /// </summary>
    [Serializable]
    internal class DeepLinkParams
    {
        public string MatchId { get; set; }
        public string TournamentId { get; set; }
        public string PlayerToken { get; set; }
        public MatchMode Mode { get; set; }
        public decimal EntryFee { get; set; }
        public Currency Currency { get; set; }
        public string OpponentId { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int Rounds { get; set; }
        public ScoreType ScoreType { get; set; }
        public string StageCode { get; set; }
        public Dictionary<string, string> CustomParams { get; set; }

        /// <summary>Parse from URL query string</summary>
        public static DeepLinkParams Parse(string url)
        {
            var result = new DeepLinkParams
            {
                CustomParams = new Dictionary<string, string>()
            };

            if (string.IsNullOrEmpty(url)) return result;

            try
            {
                var uri = new Uri(url);
                var query = uri.Query.TrimStart('?');
                var pairs = query.Split('&');

                foreach (var pair in pairs)
                {
                    var kv = pair.Split('=');
                    if (kv.Length != 2) continue;

                    var key = Uri.UnescapeDataString(kv[0]);
                    var value = Uri.UnescapeDataString(kv[1]);

                    switch (key.ToLower())
                    {
                        case "match_id": result.MatchId = value; break;
                        case "tournament_id": result.TournamentId = value; break;
                        case "player_token": result.PlayerToken = value; break;
                        case "mode": Enum.TryParse(value, true, out result.Mode); break;
                        case "entry_fee": decimal.TryParse(value, out result.EntryFee); break;
                        case "currency": Enum.TryParse(value, true, out result.Currency); break;
                        case "opponent_id": result.OpponentId = value; break;
                        case "time_limit": int.TryParse(value, out result.TimeLimitSeconds); break;
                        case "rounds": int.TryParse(value, out result.Rounds); break;
                        case "score_type": Enum.TryParse(value, true, out result.ScoreType); break;
                        case "stage_code": result.StageCode = value; break;
                        default: result.CustomParams[key] = value; break;
                    }
                }
            }
            catch (Exception)
            {
                // Invalid URL format, return empty params
            }

            return result;
        }
    }

    /// <summary>
    /// API response wrapper
    /// </summary>
    [Serializable]
    internal class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public DeskillzError Error { get; set; }
        public long Timestamp { get; set; }
    }
}
// =============================================================================
// Deskillz SDK for Unity - Data Models
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 2.7.0 (Self-Sufficient Architecture)
// =============================================================================
//
// MERGED: All models from main SDK + StackIt SDK
// This file replaces DeskillzModels.cs, DeskillzLobbyModels.cs, and 
// StackItDeskillzModels.cs to resolve all duplicate definition errors.
//
// DELETE these files after adding this one:
// - StackItDeskillzModels.cs
// - Any other game-specific *DeskillzModels.cs files
//
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz
{
    // =========================================================================
    // AUTHENTICATION MODELS
    // =========================================================================

    /// <summary>
    /// Authenticated user information.
    /// Used after successful login/signup.
    /// </summary>
    [Serializable]
    public class AuthUser
    {
        public string UserId;
        public string Username;
        public string Email;
        public string AvatarUrl;
        public int Level;
        public int TotalXp;
        public decimal Balance;
        public string WalletAddress;
        public bool IsVerified;
        public bool IsHost;
        public bool IsPremium;
        public AuthProvider Provider;
        public PlayerStats Stats;
        public DateTime CreatedAt;
        public DateTime LastLoginAt;
        
        // Compatibility aliases
        public string Id { get => UserId; set => UserId = value; }
        public bool EmailVerified { get => IsVerified; set => IsVerified = value; }
        
        public bool HasEmail => !string.IsNullOrEmpty(Email);
        public bool HasWallet => !string.IsNullOrEmpty(WalletAddress);
        
        public override string ToString() => $"AuthUser({UserId}, {Username}, Lvl {Level})";
    }

    /// <summary>
    /// Response from authentication operations.
    /// </summary>
    [Serializable]
    public class AuthResponse
    {
        public bool Success;
        public string Token;
        public string RefreshToken;
        public AuthUser User;
        public string Error;
        public string ErrorCode;
        public int ExpiresIn;
        public bool IsNewUser;
        
        // Compatibility aliases (camelCase for JSON)
        public string accessToken { get => Token; set => Token = value; }
        public string refreshToken { get => RefreshToken; set => RefreshToken = value; }
        public AuthUser user { get => User; set => User = value; }
        public long expiresIn { get => ExpiresIn; set => ExpiresIn = (int)value; }
        public bool isNewUser { get => IsNewUser; set => IsNewUser = value; }
    }

    /// <summary>
    /// Player statistics and performance data.
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public int TotalMatches;
        public int Wins;
        public int Losses;
        public int Draws;
        public int HighScore;
        public decimal TotalEarnings;
        public decimal TotalWagered;
        public int TournamentsPlayed;
        public int TournamentsWon;
        public int RoomsJoined;
        public int RoomsHosted;
        public int CurrentStreak;
        public int BestStreak;
        
        // Compatibility aliases
        public int TotalGamesPlayed { get => TotalMatches; set => TotalMatches = value; }
        public int TotalWins { get => Wins; set => Wins = value; }
        
        public float WinRate => TotalMatches > 0 ? (float)Wins / TotalMatches * 100f : 0f;
    }

    /// <summary>
    /// Player profile for leaderboards and display.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public string Username;
        public string AvatarUrl;
        public int Level;
        public int Rating;
        public string CountryCode;
        public PlayerStats Stats;
        public bool IsOnline;
        public bool IsVerified;
        
        public override string ToString() => $"PlayerProfile({PlayerId}, {Username})";
    }

    /// <summary>
    /// Legacy player data model for backward compatibility.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string Id;
        public string Username;
        public string AvatarUrl;
        public string WalletAddress;
        public PlayerRole Role;
        public int Level;
        public int TotalGamesPlayed;
        public int TotalWins;
        public string CountryCode;
        public bool IsLocalPlayer;
        internal string AuthToken;
        
        public float WinRate => TotalGamesPlayed > 0 ? (float)TotalWins / TotalGamesPlayed * 100f : 0f;
        
        public AuthUser ToAuthUser() => new AuthUser
        {
            UserId = Id,
            Username = Username,
            AvatarUrl = AvatarUrl,
            WalletAddress = WalletAddress,
            Level = Level,
            Stats = new PlayerStats { TotalMatches = TotalGamesPlayed, Wins = TotalWins }
        };
        
        public override string ToString() => $"PlayerData({Id}, {Username})";
    }

    // =========================================================================
    // WALLET MODELS
    // =========================================================================

    /// <summary>
    /// User's wallet balance information.
    /// </summary>
    [Serializable]
    public class WalletBalance
    {
        public decimal TotalBalance;
        public decimal AvailableBalance;
        public decimal PendingBalance;
        public decimal BonusBalance;
        public string Currency;
        public List<CurrencyBalance> Balances;
        
        public WalletBalance()
        {
            Balances = new List<CurrencyBalance>();
        }
    }

    /// <summary>
    /// Balance for a specific currency.
    /// </summary>
    [Serializable]
    public class CurrencyBalance
    {
        public string Currency;
        public decimal Amount;
        public string Symbol;
        public string Network;
        
        public override string ToString() => $"{Amount} {Symbol}";
    }

    /// <summary>
    /// Request to link a wallet to account.
    /// </summary>
    [Serializable]
    public class WalletLinkRequest
    {
        public string WalletAddress;
        public string Signature;
        public string Message;
        public string Nonce;
        public string Provider;
        
        // Compatibility aliases
        public string walletAddress { get => WalletAddress; set => WalletAddress = value; }
        public string signature { get => Signature; set => Signature = value; }
        public string message { get => Message; set => Message = value; }
        public string nonce { get => Nonce; set => Nonce = value; }

        public WalletLinkRequest() { }
        
        public WalletLinkRequest(string walletAddress, string signature, string message, string nonce)
        {
            WalletAddress = walletAddress;
            Signature = signature;
            Message = message;
            Nonce = nonce;
        }
    }

    /// <summary>
    /// Response from wallet link operation.
    /// </summary>
    [Serializable]
    public class WalletLinkResponse
    {
        public bool Success;
        public string WalletAddress;
        public WalletBalance Balance;
        public string Error;
    }

    // =========================================================================
    // TOURNAMENT MODELS
    // =========================================================================

    /// <summary>
    /// Tournament information.
    /// </summary>
    [Serializable]
    public class Tournament
    {
        public string TournamentId;
        public string Name;
        public string Description;
        public decimal EntryFee;
        public decimal PrizePool;
        public int CurrentPlayers;
        public int MaxPlayers;
        public int MinPlayers;
        public int MatchDuration;
        public DateTime ScheduledStart;
        public DateTime? EndTime;
        public TournamentStatus Status;
        public TournamentType Type;
        public bool IsFeatured;
        public bool IsJoined;
        public string GameId;
        public string ImageUrl;
        public Currency Currency;
        
        public bool IsFull => CurrentPlayers >= MaxPlayers;
        public bool CanJoin => Status == TournamentStatus.Open && !IsFull && !IsJoined;
        public bool IsFree => EntryFee == 0;
        
        public override string ToString() => $"Tournament({TournamentId}, {Name}, {Status})";
    }

    /// <summary>
    /// Tournament list response from API.
    /// </summary>
    [Serializable]
    public class TournamentListResponse
    {
        public List<Tournament> Tournaments;
        public int TotalCount;
        public int Page;
        public int PageSize;
        
        public TournamentListResponse()
        {
            Tournaments = new List<Tournament>();
        }
    }

    /// <summary>
    /// Tournament entry/registration data.
    /// </summary>
    [Serializable]
    public class TournamentEntry
    {
        public string EntryId;
        public string TournamentId;
        public string PlayerId;
        public string PlayerName;
        public string AvatarUrl;
        public int Score;
        public int Rank;
        public bool IsActive;
        public DateTime RegisteredAt;
        public DateTime? LastScoreAt;
        
        public override string ToString() => $"TournamentEntry({PlayerId}, Rank: {Rank}, Score: {Score})";
    }

    // =========================================================================
    // MATCH MODELS
    // =========================================================================

    /// <summary>
    /// Match configuration and state data.
    /// </summary>
    [Serializable]
    public class MatchData
    {
        public string MatchId;
        public string TournamentId;
        public string TournamentName;
        public string GameId;
        public int Duration;
        public int RandomSeed;
        public MatchMode Mode;
        public MatchType MatchType;
        public MatchStatus Status;
        public decimal EntryFee;
        public Currency Currency;
        public decimal PrizePool;
        public bool IsTestMatch;
        public bool IsPrivateRoom;
        public string RoomCode;
        public string OpponentId;
        public string OpponentName;
        public int Rounds;
        public int CurrentRound;
        public ScoreType ScoreType;
        public DateTime? StartTime;
        public DateTime? EndTime;
        public int LocalPlayerScore;
        public List<MatchPlayer> Players;
        public StageData Stage;
        public Dictionary<string, string> CustomParams;
        
        // Compatibility aliases
        public int TimeLimitSeconds { get => Duration; set => Duration = value; }
        
        public bool IsTimed => Duration > 0;
        public bool IsAsync => Mode == MatchMode.Asynchronous;
        public bool IsRealtime => Mode == MatchMode.Synchronous || Mode == MatchMode.CustomStage;
        
        public float TimeRemaining
        {
            get
            {
                if (Duration <= 0 || StartTime == null) return -1f;
                var elapsed = (float)(DateTime.UtcNow - StartTime.Value).TotalSeconds;
                return Mathf.Max(0f, Duration - elapsed);
            }
        }

        public MatchData()
        {
            Players = new List<MatchPlayer>();
            CustomParams = new Dictionary<string, string>();
        }
        
        public override string ToString() => $"MatchData({MatchId}, {Mode}, {Status})";
    }

    /// <summary>
    /// Player within a match context.
    /// </summary>
    [Serializable]
    public class MatchPlayer
    {
        public string PlayerId;
        public string Username;
        public string AvatarUrl;
        public int Score;
        public int Rank;
        public bool IsLocalPlayer;
        public bool IsNPC;
        public ConnectionState ConnectionState;
        public DateTime LastUpdate;
        
        public override string ToString() => $"MatchPlayer({PlayerId}, Score: {Score})";
    }

    /// <summary>
    /// Match result after completion.
    /// </summary>
    [Serializable]
    public class MatchResult
    {
        public string MatchId;
        public string TournamentId;
        public int FinalScore;
        public float Duration;
        public bool Success;
        public MatchOutcome Outcome;
        public int Rank;
        public int TotalPlayers;
        public decimal PrizeWon;
        public int XpEarned;
        public int OpponentScore;
        public string OpponentName;
        public string YourName;
        public string Error;
        
        // Compatibility aliases
        public bool SubmitSuccessful { get => Success; set => Success = value; }
        public int YourScore { get => FinalScore; set => FinalScore = value; }
        
        public override string ToString() => $"MatchResult({MatchId}, {Outcome}, Score: {FinalScore})";
    }

    /// <summary>
    /// Simplified match info from lobby/deep link.
    /// </summary>
    [Serializable]
    public class MatchInfo
    {
        public string MatchId;
        public string TournamentId;
        public string Token;
        public MatchMode Mode;
        public decimal EntryFee;
        public decimal PrizePool;
        public Currency Currency;
        public int TimeLimitSeconds;
        public int MaxPlayers;
        public bool IsRealtime => Mode == MatchMode.Synchronous || Mode == MatchMode.CustomStage;
        public bool IsTestMatch;
        public Dictionary<string, string> CustomParams;

        public MatchInfo()
        {
            CustomParams = new Dictionary<string, string>();
        }

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

        public MatchData ToMatchData() => new MatchData
        {
            MatchId = MatchId,
            TournamentId = TournamentId,
            Mode = Mode,
            EntryFee = EntryFee,
            PrizePool = PrizePool,
            Currency = Currency,
            Duration = TimeLimitSeconds,
            Status = MatchStatus.Pending,
            CustomParams = CustomParams
        };
    }

    /// <summary>
    /// Custom stage configuration data.
    /// </summary>
    [Serializable]
    public class StageData
    {
        public string StageId;
        public string Name;
        public string Code;
        public string HostId;
        public int MaxPlayers;
        public int CurrentPlayers;
        public StageVisibility Visibility;
        public string ConfigJson;
    }

    // =========================================================================
    // ROOM MODELS
    // =========================================================================

    /// <summary>
    /// Room configuration for creation.
    /// </summary>
    [Serializable]
    public class RoomConfig
    {
        public string RoomName = "Game Room";
        public int MaxPlayers = 4;
        public int MatchDuration = 60;
        public bool IsPrivate = true;
        public bool AllowSpectators = false;
        public decimal EntryFee = 0;
        public Currency Currency = Currency.Free;
    }

    /// <summary>
    /// Private room information.
    /// </summary>
    [Serializable]
    public class PrivateRoom
    {
        public string RoomId;
        public string RoomCode;
        public string Name;
        public string GameId;
        public string HostId;
        public string HostName;
        public int MaxPlayers;
        public int MatchDuration;
        public decimal EntryFee;
        public RoomStatus Status;
        public List<RoomPlayer> Players;
        public DateTime CreatedAt;
        public DateTime? StartedAt;
        public DateTime? EndedAt;
        
        public int CurrentPlayerCount => Players?.Count ?? 0;
        public bool IsFull => CurrentPlayerCount >= MaxPlayers;
        public bool CanStart => Status == RoomStatus.Waiting && CurrentPlayerCount >= 2;

        public PrivateRoom()
        {
            Players = new List<RoomPlayer>();
        }
        
        public override string ToString() => $"PrivateRoom({RoomId}, {Name}, {Status})";
    }

    /// <summary>
    /// Player in a room.
    /// </summary>
    [Serializable]
    public class RoomPlayer
    {
        public string PlayerId;
        public string DisplayName;
        public string AvatarUrl;
        public bool IsHost;
        public bool IsReady;
        public bool IsConnected;
        public int LastScore;
        public int TotalScore;
        public int MatchesPlayed;
        public DateTime JoinedAt;
        
        public override string ToString() => $"RoomPlayer({PlayerId}, Host: {IsHost}, Ready: {IsReady})";
    }

    /// <summary>
    /// Player presence in lobby/pre-match.
    /// </summary>
    [Serializable]
    public class PlayerPresence
    {
        public string PlayerId;
        public string Username;
        public string AvatarUrl;
        public int Rating;
        public bool IsReady;
        public bool IsNPC;
        public bool IsLocalPlayer;
        public DateTime JoinedAt;
        public ConnectionState ConnectionStatus = ConnectionState.Connected;
    }

    /// <summary>
    /// Lobby room state.
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

    /// <summary>
    /// Lobby room where players gather before a match.
    /// </summary>
    [Serializable]
    public class LobbyRoom
    {
        public string RoomId;
        public string TournamentId;
        public string GameId;
        public MatchState State;
        public List<PlayerPresence> Players;
        public int MaxPlayers;
        public int MinPlayers;
        public decimal EntryFee;
        public Currency Currency;
        public DateTime CreatedAt;
        public int CountdownSeconds;

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

        public LobbyRoom()
        {
            Players = new List<PlayerPresence>();
        }
    }

    // =========================================================================
    // HOST MODELS
    // =========================================================================

    /// <summary>
    /// Host profile and tier information.
    /// </summary>
    [Serializable]
    public class HostProfile
    {
        public string HostId;
        public string UserId;
        public string DisplayName;
        public string AvatarUrl;
        public int Level;
        public int TotalXp;
        public int XpToNextLevel;
        public HostTier EsportsTier;
        public HostTier SocialTier;
        public float EsportsTierProgress;
        public float SocialTierProgress;
        public decimal LifetimeEarnings;
        public decimal PendingEarnings;
        public decimal MonthlyEarnings;
        public int TotalRoomsHosted;
        public int TotalPlayersHosted;
        public int MonthlyRoomsHosted;
        public int MonthlyPlayersHosted;
        public int CurrentStreak;
        public int LongestStreak;
        public bool IsVerified;
        public bool IsFoundingHost;
        public List<HostBadge> Badges;
        public DateTime CreatedAt;
        public DateTime LastActiveAt;

        public HostProfile()
        {
            Badges = new List<HostBadge>();
        }
        
        public float GetEsportsRevenueShare() => EsportsTier switch
        {
            HostTier.Bronze => 50f,
            HostTier.Silver => 55f,
            HostTier.Gold => 60f,
            HostTier.Platinum => 65f,
            HostTier.Diamond => 70f,
            HostTier.Elite => 75f,
            _ => 50f
        };
        
        public float GetSocialRevenueShare() => SocialTier switch
        {
            HostTier.Bronze => 50f,
            HostTier.Silver => 53f,
            HostTier.Gold => 56f,
            HostTier.Platinum => 59f,
            HostTier.Diamond => 62f,
            HostTier.Elite => 65f,
            _ => 50f
        };
    }

    /// <summary>
    /// Host earnings breakdown.
    /// </summary>
    [Serializable]
    public class HostEarnings
    {
        public decimal TotalEarnings;
        public decimal PendingEarnings;
        public decimal PendingPayout;
        public decimal LastPayout;
        public decimal WithdrawnEarnings;
        public decimal ThisMonthEarnings;
        public decimal LastMonthEarnings;
        public decimal EsportsEarnings;
        public decimal SocialEarnings;
        public decimal BonusEarnings;
        public List<EarningsTransaction> RecentTransactions;

        public HostEarnings()
        {
            RecentTransactions = new List<EarningsTransaction>();
        }
    }

    /// <summary>
    /// Earnings transaction record.
    /// </summary>
    [Serializable]
    public class EarningsTransaction
    {
        public string TransactionId;
        public string RoomId;
        public string RoomName;
        public decimal Amount;
        public string Type;
        public DateTime CreatedAt;
    }

    /// <summary>
    /// Host achievement badge.
    /// </summary>
    [Serializable]
    public class HostBadge
    {
        public string BadgeId;
        public string Name;
        public string Description;
        public string IconUrl;
        public BadgeCategory Category;
        public float BonusPercent;
        public DateTime EarnedAt;
        public DateTime? ExpiresAt;
        
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    /// <summary>
    /// Withdrawal operation result.
    /// </summary>
    [Serializable]
    public class WithdrawalResult
    {
        public bool Success;
        public string TransactionId;
        public decimal Amount;
        public string WalletAddress;
        public string Status;
        public string Message;
        public DateTime ProcessedAt;
    }

    // =========================================================================
    // LEADERBOARD MODELS
    // =========================================================================

    /// <summary>
    /// Leaderboard entry.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public string PlayerId;
        public string DisplayName;
        public string AvatarUrl;
        public int Score;
        public int HighScore;
        public int MatchesPlayed;
        public int Wins;
        public bool IsCurrentPlayer;
    }

    // =========================================================================
    // ERROR & API MODELS
    // =========================================================================

    /// <summary>
    /// SDK error information.
    /// </summary>
    [Serializable]
    public class DeskillzError
    {
        public string Code;
        public ErrorCode ErrorCode;
        public string Message;
        public string Details;

        public DeskillzError() { }
        
        public DeskillzError(ErrorCode code, string message, string details = null)
        {
            ErrorCode = code;
            Code = code.ToString();
            Message = message;
            Details = details;
        }

        public override string ToString() => $"[{Code}] {Message}";
    }

    /// <summary>
    /// Generic API response wrapper.
    /// </summary>
    [Serializable]
    public class ApiResponse<T>
    {
        public bool Success;
        public T Data;
        public DeskillzError Error;
        public long Timestamp;
    }

    /// <summary>
    /// Generic success response.
    /// </summary>
    [Serializable]
    public class SuccessResponse
    {
        public bool success;
        public string message;
    }

    /// <summary>
    /// Nonce response for wallet signing.
    /// </summary>
    [Serializable]
    public class NonceResponse
    {
        public string nonce;
        public string message;
    }

    /// <summary>
    /// Forgot password request.
    /// </summary>
    [Serializable]
    public class ForgotPasswordRequest
    {
        public string email;

        public ForgotPasswordRequest() { }
        public ForgotPasswordRequest(string email) { this.email = email; }
    }

    /// <summary>
    /// Reset password request.
    /// </summary>
    [Serializable]
    public class ResetPasswordRequest
    {
        public string token;
        public string password;

        public ResetPasswordRequest() { }
        public ResetPasswordRequest(string token, string password)
        {
            this.token = token;
            this.password = password;
        }
    }

    /// <summary>
    /// Social auth request.
    /// </summary>
    [Serializable]
    public class SocialAuthRequest
    {
        public string provider;
        public string idToken;

        public SocialAuthRequest() { }
        public SocialAuthRequest(string provider, string idToken)
        {
            this.provider = provider;
            this.idToken = idToken;
        }
    }

    // =========================================================================
    // HELPER CLASSES
    // =========================================================================

    /// <summary>
    /// Static helper methods for host tier information.
    /// </summary>
    public static class HostTierInfo
    {
        public static string GetTierName(HostTier tier) => tier.ToString();

        public static Color GetTierColor(HostTier tier) => tier switch
        {
            HostTier.Bronze => new Color(0.8f, 0.5f, 0.2f),
            HostTier.Silver => new Color(0.75f, 0.75f, 0.75f),
            HostTier.Gold => new Color(1f, 0.84f, 0f),
            HostTier.Platinum => new Color(0.9f, 0.9f, 0.95f),
            HostTier.Diamond => new Color(0.6f, 0.8f, 1f),
            HostTier.Elite => new Color(1f, 0.4f, 0.7f),
            _ => Color.white
        };

        public static string GetTierIcon(HostTier tier) => tier switch
        {
            HostTier.Bronze => "🥉",
            HostTier.Silver => "🥈",
            HostTier.Gold => "🥇",
            HostTier.Platinum => "💎",
            HostTier.Diamond => "👑",
            HostTier.Elite => "🏆",
            _ => "🎖"
        };

        public static int GetRoomsRequiredForTier(HostTier tier) => tier switch
        {
            HostTier.Bronze => 0,
            HostTier.Silver => 10,
            HostTier.Gold => 50,
            HostTier.Platinum => 200,
            HostTier.Diamond => 500,
            HostTier.Elite => 1000,
            _ => 0
        };

        public static int GetPlayersRequiredForTier(HostTier tier) => tier switch
        {
            HostTier.Bronze => 0,
            HostTier.Silver => 50,
            HostTier.Gold => 250,
            HostTier.Platinum => 1000,
            HostTier.Diamond => 5000,
            HostTier.Elite => 10000,
            _ => 0
        };
    }
}
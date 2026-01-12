// =============================================================================
// Deskillz SDK for Unity - Social Game Models
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Social
{
    // =========================================================================
    // ENUMS
    // =========================================================================

    /// <summary>
    /// Social game types supported by the platform
    /// </summary>
    public enum SocialGameType
    {
        /// <summary>Big 2 (Dai Di) card game</summary>
        Big2,

        /// <summary>Mahjong tile game</summary>
        Mahjong,

        /// <summary>13-Card Poker (Chinese Poker)</summary>
        ThirteenCardPoker,

        /// <summary>Custom game defined by developer</summary>
        Custom
    }

    /// <summary>
    /// Player status in a social game session
    /// </summary>
    public enum SocialPlayerStatus
    {
        /// <summary>Player is waiting for next round</summary>
        Waiting,

        /// <summary>Player is actively in the current round</summary>
        Playing,

        /// <summary>Player is sitting out (still in room)</summary>
        SittingOut,

        /// <summary>Player has busted (balance = 0)</summary>
        Busted,

        /// <summary>Player has left the session</summary>
        Left
    }

    /// <summary>
    /// Session status
    /// </summary>
    public enum SocialSessionStatus
    {
        /// <summary>Session is active and accepting players</summary>
        Active,

        /// <summary>Session is paused</summary>
        Paused,

        /// <summary>Session is ending (final round)</summary>
        Ending,

        /// <summary>Session has ended</summary>
        Ended
    }

    /// <summary>
    /// Pause vote status
    /// </summary>
    public enum PauseVoteStatus
    {
        /// <summary>No active pause request</summary>
        None,

        /// <summary>Pause vote in progress</summary>
        Voting,

        /// <summary>Pause approved, game paused</summary>
        Paused,

        /// <summary>Pause vote rejected</summary>
        Rejected
    }

    // =========================================================================
    // DATA MODELS
    // =========================================================================

    /// <summary>
    /// Social game session data
    /// </summary>
    [Serializable]
    public class SocialGameSession
    {
        /// <summary>Session ID</summary>
        public string Id;

        /// <summary>Room ID this session belongs to</summary>
        public string RoomId;

        /// <summary>Room code</summary>
        public string RoomCode;

        /// <summary>Game type</summary>
        public SocialGameType GameType;

        /// <summary>Session status</summary>
        public SocialSessionStatus Status;

        /// <summary>Point value in USD</summary>
        public decimal PointValue;

        /// <summary>Rake percentage (typically 5%)</summary>
        public float RakePercent;

        /// <summary>Maximum rake per pot</summary>
        public decimal RakeCap;

        /// <summary>Current round number</summary>
        public int CurrentRound;

        /// <summary>Total rounds played</summary>
        public int TotalRounds;

        /// <summary>Total pot volume (sum of all pots)</summary>
        public decimal TotalPotVolume;

        /// <summary>Total rake collected</summary>
        public decimal TotalRake;

        /// <summary>Accumulated rake since last settlement</summary>
        public decimal AccumulatedRake;

        /// <summary>Time of last settlement</summary>
        public DateTime? LastSettlementAt;

        /// <summary>Turn timer in seconds (0 = no timer)</summary>
        public int TurnTimerSeconds;

        /// <summary>Maximum pauses per player per session</summary>
        public int MaxPausesPerPlayer;

        /// <summary>Pause cooldown in minutes</summary>
        public int PauseCooldownMinutes;

        /// <summary>Maximum pause duration in minutes</summary>
        public int MaxPauseDurationMinutes;

        /// <summary>Players in this session</summary>
        public List<SocialPlayer> Players = new List<SocialPlayer>();

        /// <summary>Current pause vote status</summary>
        public PauseVoteStatus PauseStatus;

        /// <summary>Session start time</summary>
        public DateTime StartedAt;

        /// <summary>Session end time (if ended)</summary>
        public DateTime? EndedAt;

        /// <summary>Whether it's the local player's turn</summary>
        public bool IsMyTurn;

        /// <summary>Remaining time on turn timer (seconds)</summary>
        public float TurnTimeRemaining;

        /// <summary>Current pause time remaining (seconds)</summary>
        public float PauseTimeRemaining;

        /// <summary>Get minimum buy-in (50x point value)</summary>
        public decimal MinBuyIn => PointValue * 50;

        /// <summary>Get default buy-in (100x point value)</summary>
        public decimal DefaultBuyIn => PointValue * 100;

        /// <summary>Get low balance warning threshold (20x point value)</summary>
        public decimal LowBalanceThreshold => PointValue * 20;

        public override string ToString()
        {
            return $"SocialSession({Id}, {GameType}, Round {CurrentRound}, {Players.Count} players)";
        }
    }

    /// <summary>
    /// Player in a social game session
    /// </summary>
    [Serializable]
    public class SocialPlayer
    {
        /// <summary>Player user ID</summary>
        public string Id;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Player avatar URL</summary>
        public string AvatarUrl;

        /// <summary>Current balance in session</summary>
        public decimal Balance;

        /// <summary>Total bought in this session</summary>
        public decimal TotalBuyIn;

        /// <summary>Total cashed out this session</summary>
        public decimal TotalCashOut;

        /// <summary>Number of rebuys this session</summary>
        public int RebuyCount;

        /// <summary>Player status</summary>
        public SocialPlayerStatus Status;

        /// <summary>Score for current round</summary>
        public int CurrentScore;

        /// <summary>Total score this session</summary>
        public int TotalScore;

        /// <summary>Rounds won this session</summary>
        public int RoundsWon;

        /// <summary>Net profit/loss this session</summary>
        public decimal NetProfitLoss;

        /// <summary>Number of pauses used this session</summary>
        public int PausesUsed;

        /// <summary>Last pause request time</summary>
        public DateTime? LastPauseAt;

        /// <summary>Seat position (0-3 for 4-player games)</summary>
        public int SeatPosition;

        /// <summary>Whether this is the local player</summary>
        public bool IsLocalPlayer;

        /// <summary>Whether player voted for current pause request</summary>
        public bool? PauseVote;

        /// <summary>Whether player has low balance</summary>
        public bool HasLowBalance => Balance > 0 && Balance < (CurrentSession?.LowBalanceThreshold ?? 0);

        /// <summary>Whether player needs to rebuy</summary>
        public bool NeedsRebuy => Balance <= 0 && Status != SocialPlayerStatus.Left;

        /// <summary>Reference to current session (set internally)</summary>
        internal SocialGameSession CurrentSession;

        /// <summary>Check if player can request pause</summary>
        public bool CanRequestPause()
        {
            if (CurrentSession == null) return false;
            if (PausesUsed >= CurrentSession.MaxPausesPerPlayer) return false;
            if (LastPauseAt.HasValue)
            {
                var cooldownEnd = LastPauseAt.Value.AddMinutes(CurrentSession.PauseCooldownMinutes);
                if (DateTime.UtcNow < cooldownEnd) return false;
            }
            return true;
        }

        public override string ToString()
        {
            return $"SocialPlayer({Id}, {Username}, Balance: ${Balance}, Status: {Status})";
        }
    }

    /// <summary>
    /// Round result data
    /// </summary>
    [Serializable]
    public class RoundResult
    {
        /// <summary>Round number</summary>
        public int RoundNumber;

        /// <summary>Winner player ID</summary>
        public string WinnerId;

        /// <summary>Winner username</summary>
        public string WinnerUsername;

        /// <summary>Total pot for this round</summary>
        public decimal TotalPot;

        /// <summary>Rake taken from pot</summary>
        public decimal RakeTaken;

        /// <summary>Net winnings (pot - rake)</summary>
        public decimal NetWinnings;

        /// <summary>All player scores this round</summary>
        public List<PlayerRoundScore> PlayerScores = new List<PlayerRoundScore>();

        /// <summary>Round duration in seconds</summary>
        public float DurationSeconds;

        /// <summary>Round completion time</summary>
        public DateTime CompletedAt;
    }

    /// <summary>
    /// Player score for a round
    /// </summary>
    [Serializable]
    public class PlayerRoundScore
    {
        /// <summary>Player ID</summary>
        public string PlayerId;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Score this round</summary>
        public int Score;

        /// <summary>Points won/lost</summary>
        public int PointDelta;

        /// <summary>Money won/lost</summary>
        public decimal MoneyDelta;

        /// <summary>Balance after this round</summary>
        public decimal BalanceAfter;
    }

    /// <summary>
    /// Rake transaction for settlement
    /// </summary>
    [Serializable]
    public class RakeTransaction
    {
        /// <summary>Transaction ID</summary>
        public string Id;

        /// <summary>Session ID</summary>
        public string SessionId;

        /// <summary>Round number</summary>
        public int RoundNumber;

        /// <summary>Pot amount</summary>
        public decimal PotAmount;

        /// <summary>Rake amount</summary>
        public decimal RakeAmount;

        /// <summary>Winner player ID</summary>
        public string WinnerId;

        /// <summary>Transaction timestamp</summary>
        public DateTime CreatedAt;
    }

    /// <summary>
    /// Settlement record
    /// </summary>
    [Serializable]
    public class Settlement
    {
        /// <summary>Settlement ID</summary>
        public string Id;

        /// <summary>Session ID</summary>
        public string SessionId;

        /// <summary>Total rake settled</summary>
        public decimal TotalRake;

        /// <summary>Host earnings from this settlement</summary>
        public decimal HostEarnings;

        /// <summary>Platform earnings</summary>
        public decimal PlatformEarnings;

        /// <summary>Developer earnings</summary>
        public decimal DeveloperEarnings;

        /// <summary>Settlement trigger</summary>
        public Host.SettlementTrigger Trigger;

        /// <summary>Number of rounds in this settlement</summary>
        public int RoundCount;

        /// <summary>Settlement timestamp</summary>
        public DateTime SettledAt;
    }

    /// <summary>
    /// Buy-in request
    /// </summary>
    [Serializable]
    public class BuyInRequest
    {
        /// <summary>Room ID</summary>
        public string roomId;

        /// <summary>Amount to buy in</summary>
        public decimal amount;

        /// <summary>Currency</summary>
        public string currency;
    }

    /// <summary>
    /// Buy-in response
    /// </summary>
    [Serializable]
    public class BuyInResponse
    {
        /// <summary>Transaction ID</summary>
        public string TransactionId;

        /// <summary>Amount bought in</summary>
        public decimal Amount;

        /// <summary>New balance</summary>
        public decimal NewBalance;

        /// <summary>Total bought in this session</summary>
        public decimal TotalBuyIn;
    }

    /// <summary>
    /// Cash out request
    /// </summary>
    [Serializable]
    public class CashOutRequest
    {
        /// <summary>Room ID</summary>
        public string roomId;
    }

    /// <summary>
    /// Cash out response
    /// </summary>
    [Serializable]
    public class CashOutResponse
    {
        /// <summary>Transaction ID</summary>
        public string TransactionId;

        /// <summary>Amount cashed out</summary>
        public decimal Amount;

        /// <summary>Currency</summary>
        public string Currency;

        /// <summary>Net profit/loss for session</summary>
        public decimal NetProfitLoss;
    }

    /// <summary>
    /// Pause request
    /// </summary>
    [Serializable]
    public class PauseRequest
    {
        /// <summary>Room ID</summary>
        public string roomId;

        /// <summary>Reason for pause (optional)</summary>
        public string reason;
    }

    /// <summary>
    /// Pause vote
    /// </summary>
    [Serializable]
    public class PauseVote
    {
        /// <summary>Room ID</summary>
        public string roomId;

        /// <summary>Whether player approves the pause</summary>
        public bool approve;
    }

    /// <summary>
    /// Pause status response
    /// </summary>
    [Serializable]
    public class PauseStatusResponse
    {
        /// <summary>Current pause status</summary>
        public PauseVoteStatus Status;

        /// <summary>Player who requested pause</summary>
        public string RequesterId;

        /// <summary>Requester username</summary>
        public string RequesterUsername;

        /// <summary>Reason for pause</summary>
        public string Reason;

        /// <summary>Votes received</summary>
        public Dictionary<string, bool> Votes;

        /// <summary>Votes needed to approve</summary>
        public int VotesNeeded;

        /// <summary>Time remaining to vote (seconds)</summary>
        public float VoteTimeRemaining;

        /// <summary>Pause time remaining (if paused)</summary>
        public float PauseTimeRemaining;
    }

    /// <summary>
    /// Session end summary
    /// </summary>
    [Serializable]
    public class SessionEndSummary
    {
        /// <summary>Session ID</summary>
        public string SessionId;

        /// <summary>Total rounds played</summary>
        public int TotalRounds;

        /// <summary>Session duration in minutes</summary>
        public float DurationMinutes;

        /// <summary>Total pot volume</summary>
        public decimal TotalPotVolume;

        /// <summary>Total rake collected</summary>
        public decimal TotalRake;

        /// <summary>Final player standings</summary>
        public List<SessionPlayerSummary> PlayerSummaries = new List<SessionPlayerSummary>();

        /// <summary>Session end time</summary>
        public DateTime EndedAt;
    }

    /// <summary>
    /// Player summary at session end
    /// </summary>
    [Serializable]
    public class SessionPlayerSummary
    {
        /// <summary>Player ID</summary>
        public string PlayerId;

        /// <summary>Player username</summary>
        public string Username;

        /// <summary>Final rank</summary>
        public int Rank;

        /// <summary>Rounds won</summary>
        public int RoundsWon;

        /// <summary>Total bought in</summary>
        public decimal TotalBuyIn;

        /// <summary>Total cashed out</summary>
        public decimal TotalCashOut;

        /// <summary>Net profit/loss</summary>
        public decimal NetProfitLoss;

        /// <summary>Highest balance achieved</summary>
        public decimal PeakBalance;

        /// <summary>Number of rebuys</summary>
        public int RebuyCount;
    }

    /// <summary>
    /// Social game error
    /// </summary>
    [Serializable]
    public class SocialGameError
    {
        /// <summary>Error code</summary>
        public string Code;

        /// <summary>Error message</summary>
        public string Message;

        public SocialGameError() { }

        public SocialGameError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return $"SocialGameError({Code}: {Message})";
        }

        // Common error codes
        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string NotInSession = "NOT_IN_SESSION";
            public const string SessionNotActive = "SESSION_NOT_ACTIVE";
            public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
            public const string BelowMinBuyIn = "BELOW_MIN_BUYIN";
            public const string NotYourTurn = "NOT_YOUR_TURN";
            public const string AlreadyVoted = "ALREADY_VOTED";
            public const string NoPauseActive = "NO_PAUSE_ACTIVE";
            public const string PauseLimitReached = "PAUSE_LIMIT_REACHED";
            public const string PauseCooldown = "PAUSE_COOLDOWN";
            public const string CannotLeaveInRound = "CANNOT_LEAVE_IN_ROUND";
            public const string MustRebuy = "MUST_REBUY";
            public const string NetworkError = "NETWORK_ERROR";
            public const string ServerError = "SERVER_ERROR";
        }
    }
}
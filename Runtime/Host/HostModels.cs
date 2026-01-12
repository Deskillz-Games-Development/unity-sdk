// =============================================================================
// Deskillz SDK for Unity - Host System Models
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
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
    /// Host tier levels based on performance
    /// </summary>
    public enum HostTier
    {
        /// <summary>Bronze: 2-4 players (Esports) or $0-$100/month (Social)</summary>
        Bronze = 0,

        /// <summary>Silver: 5-10 players (Esports) or $101-$500/month (Social)</summary>
        Silver = 1,

        /// <summary>Gold: 11-25 players (Esports) or $501-$2,000/month (Social)</summary>
        Gold = 2,

        /// <summary>Platinum: 26-50 players (Esports) or $2,001-$10,000/month (Social)</summary>
        Platinum = 3,

        /// <summary>Diamond: 51-100 players (Esports) or $10,001-$50,000/month (Social)</summary>
        Diamond = 4,

        /// <summary>Elite: 100+ players (Esports) or $50,000+/month (Social)</summary>
        Elite = 5
    }

    /// <summary>
    /// Badge category types
    /// </summary>
    public enum BadgeCategory
    {
        /// <summary>Permanent achievement badges</summary>
        Achievement,

        /// <summary>Monthly performance badges (reset each month)</summary>
        Performance,

        /// <summary>Special exclusive badges</summary>
        Exclusive
    }

    /// <summary>
    /// Room type for revenue calculation
    /// </summary>
    public enum RoomRevenueType
    {
        /// <summary>Esports room with entry fee (10% platform fee)</summary>
        Esports,

        /// <summary>Social room with rake (5% rake from pots)</summary>
        Social,

        /// <summary>Hybrid room with both entry fee and rake</summary>
        Hybrid
    }

    /// <summary>
    /// Settlement trigger types
    /// </summary>
    public enum SettlementTrigger
    {
        /// <summary>Accumulated rake >= $50</summary>
        Threshold,

        /// <summary>Every 10 rounds</summary>
        RoundCount,

        /// <summary>Every 30 minutes</summary>
        TimeElapsed,

        /// <summary>Player leaves the room</summary>
        PlayerLeave,

        /// <summary>Host manually triggers</summary>
        Manual,

        /// <summary>Session ends</summary>
        SessionEnd
    }

    /// <summary>
    /// Host notification types
    /// </summary>
    public enum HostNotificationType
    {
        /// <summary>Batch settlement completed</summary>
        BatchSettlement,

        /// <summary>Player left settlement</summary>
        PlayerLeftSettlement,

        /// <summary>Session complete with earnings</summary>
        SessionComplete,

        /// <summary>Tier upgraded</summary>
        TierUpgrade,

        /// <summary>Tier downgraded</summary>
        TierDowngrade,

        /// <summary>Badge earned</summary>
        BadgeEarned,

        /// <summary>Level up</summary>
        LevelUp,

        /// <summary>Withdrawal processed</summary>
        WithdrawalProcessed
    }

    // =========================================================================
    // DATA MODELS
    // =========================================================================

    /// <summary>
    /// Host profile data
    /// </summary>
    [Serializable]
    public class HostProfile
    {
        /// <summary>Unique host profile ID</summary>
        public string Id;

        /// <summary>User ID of the host</summary>
        public string UserId;

        /// <summary>Host display name</summary>
        public string Username;

        /// <summary>Host avatar URL</summary>
        public string AvatarUrl;

        /// <summary>Whether host is age verified (18+)</summary>
        public bool IsVerified;

        /// <summary>Current esports tier</summary>
        public HostTier EsportsTier;

        /// <summary>Current social tier</summary>
        public HostTier SocialTier;

        /// <summary>Current host level (1-10)</summary>
        public int Level;

        /// <summary>Total players hosted all-time</summary>
        public int TotalPlayersHosted;

        /// <summary>Total rooms created all-time</summary>
        public int TotalRoomsCreated;

        /// <summary>Total rooms completed all-time</summary>
        public int TotalRoomsCompleted;

        /// <summary>Total earnings all-time (USD)</summary>
        public decimal TotalEarnings;

        /// <summary>Available balance for withdrawal (USD)</summary>
        public decimal AvailableBalance;

        /// <summary>Pending balance (in escrow)</summary>
        public decimal PendingBalance;

        /// <summary>Monthly rake generated (for social tier calculation)</summary>
        public decimal MonthlyRakeGenerated;

        /// <summary>Current hosting streak in days</summary>
        public int CurrentStreak;

        /// <summary>Longest hosting streak achieved</summary>
        public int LongestStreak;

        /// <summary>Number of active rooms currently hosting</summary>
        public int ActiveRoomsCount;

        /// <summary>Host rating (1-5 stars)</summary>
        public float Rating;

        /// <summary>Number of ratings received</summary>
        public int RatingCount;

        /// <summary>When host profile was created</summary>
        public DateTime CreatedAt;

        /// <summary>Last activity timestamp</summary>
        public DateTime LastActiveAt;

        /// <summary>Earned badges</summary>
        public List<HostBadge> Badges = new List<HostBadge>();

        /// <summary>Notification preferences</summary>
        public HostNotificationSettings NotificationSettings;

        /// <summary>Get tier icon text for display</summary>
        public string GetEsportsTierIcon()
        {
            return GetTierIcon(EsportsTier);
        }

        /// <summary>Get tier icon text for display</summary>
        public string GetSocialTierIcon()
        {
            return GetTierIcon(SocialTier);
        }

        private string GetTierIcon(HostTier tier)
        {
            return tier switch
            {
                HostTier.Bronze => "[B]",
                HostTier.Silver => "[S]",
                HostTier.Gold => "[G]",
                HostTier.Platinum => "[P]",
                HostTier.Diamond => "[D]",
                HostTier.Elite => "[E]",
                _ => "[B]"
            };
        }

        /// <summary>Get tier name for display</summary>
        public static string GetTierName(HostTier tier)
        {
            return tier switch
            {
                HostTier.Bronze => "Bronze",
                HostTier.Silver => "Silver",
                HostTier.Gold => "Gold",
                HostTier.Platinum => "Platinum",
                HostTier.Diamond => "Diamond",
                HostTier.Elite => "Elite",
                _ => "Bronze"
            };
        }

        /// <summary>Get level title for display</summary>
        public string GetLevelTitle()
        {
            return Level switch
            {
                1 => "Newcomer",
                2 => "Beginner",
                3 => "Intermediate",
                4 => "Experienced",
                5 => "Advanced",
                6 => "Expert",
                7 => "Master",
                8 => "Grandmaster",
                9 => "Legend",
                10 => "Elite",
                _ => "Newcomer"
            };
        }

        public override string ToString()
        {
            return $"HostProfile({Id}, {Username}, Level {Level}, Esports: {EsportsTier}, Social: {SocialTier})";
        }
    }

    /// <summary>
    /// Host badge data
    /// </summary>
    [Serializable]
    public class HostBadge
    {
        /// <summary>Badge unique ID</summary>
        public string Id;

        /// <summary>Badge code/key</summary>
        public string Code;

        /// <summary>Badge display name</summary>
        public string Name;

        /// <summary>Badge description</summary>
        public string Description;

        /// <summary>Badge icon URL</summary>
        public string IconUrl;

        /// <summary>Badge category</summary>
        public BadgeCategory Category;

        /// <summary>Bonus percentage this badge provides</summary>
        public float BonusPercent;

        /// <summary>When the badge was earned</summary>
        public DateTime EarnedAt;

        /// <summary>When the badge expires (for performance badges)</summary>
        public DateTime? ExpiresAt;

        /// <summary>Whether badge is currently active</summary>
        public bool IsActive => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;

        public override string ToString()
        {
            return $"HostBadge({Code}, {Name}, +{BonusPercent}%)";
        }
    }

    /// <summary>
    /// Host tier configuration with revenue splits
    /// </summary>
    [Serializable]
    public class HostTierConfig
    {
        /// <summary>Tier level</summary>
        public HostTier Tier;

        /// <summary>Minimum threshold to reach this tier</summary>
        public int MinThreshold;

        /// <summary>Maximum threshold for this tier</summary>
        public int MaxThreshold;

        /// <summary>Host revenue share percentage</summary>
        public float HostSharePercent;

        /// <summary>Platform revenue share percentage</summary>
        public float PlatformSharePercent;

        /// <summary>Developer revenue share percentage</summary>
        public float DeveloperSharePercent;
    }

    /// <summary>
    /// Host earnings summary
    /// </summary>
    [Serializable]
    public class HostEarnings
    {
        /// <summary>Today's earnings</summary>
        public decimal Today;

        /// <summary>This week's earnings</summary>
        public decimal ThisWeek;

        /// <summary>This month's earnings</summary>
        public decimal ThisMonth;

        /// <summary>All-time earnings</summary>
        public decimal AllTime;

        /// <summary>Available for withdrawal</summary>
        public decimal Available;

        /// <summary>Pending (in escrow)</summary>
        public decimal Pending;

        /// <summary>Total withdrawn</summary>
        public decimal TotalWithdrawn;

        /// <summary>Earnings from esports rooms</summary>
        public decimal EsportsEarnings;

        /// <summary>Earnings from social rooms</summary>
        public decimal SocialEarnings;

        /// <summary>Bonus earnings (streak, volume, badges)</summary>
        public decimal BonusEarnings;

        /// <summary>Recent transactions</summary>
        public List<HostTransaction> RecentTransactions = new List<HostTransaction>();
    }

    /// <summary>
    /// Host transaction record
    /// </summary>
    [Serializable]
    public class HostTransaction
    {
        /// <summary>Transaction ID</summary>
        public string Id;

        /// <summary>Transaction type</summary>
        public string Type;

        /// <summary>Amount in USD</summary>
        public decimal Amount;

        /// <summary>Room ID (if applicable)</summary>
        public string RoomId;

        /// <summary>Room name (if applicable)</summary>
        public string RoomName;

        /// <summary>Description</summary>
        public string Description;

        /// <summary>Transaction timestamp</summary>
        public DateTime CreatedAt;

        /// <summary>Settlement trigger (if rake settlement)</summary>
        public SettlementTrigger? Trigger;
    }

    /// <summary>
    /// Host notification settings
    /// </summary>
    [Serializable]
    public class HostNotificationSettings
    {
        /// <summary>Enable push notifications</summary>
        public bool PushEnabled = true;

        /// <summary>Enable in-app notifications</summary>
        public bool InAppEnabled = true;

        /// <summary>Enable sound for notifications</summary>
        public bool SoundEnabled = true;

        /// <summary>Notify on batch settlements</summary>
        public bool BatchSettlements = true;

        /// <summary>Notify on player left settlements</summary>
        public bool PlayerLeftSettlements = true;

        /// <summary>Notify on session complete</summary>
        public bool SessionComplete = true;

        /// <summary>Notify on tier changes</summary>
        public bool TierChanges = true;

        /// <summary>Notify on badge earned</summary>
        public bool BadgeEarned = true;

        /// <summary>Notify on level up</summary>
        public bool LevelUp = true;
    }

    /// <summary>
    /// Host notification data
    /// </summary>
    [Serializable]
    public class HostNotification
    {
        /// <summary>Notification ID</summary>
        public string Id;

        /// <summary>Notification type</summary>
        public HostNotificationType Type;

        /// <summary>Notification title</summary>
        public string Title;

        /// <summary>Notification message</summary>
        public string Message;

        /// <summary>Associated amount (if applicable)</summary>
        public decimal? Amount;

        /// <summary>Associated room ID (if applicable)</summary>
        public string RoomId;

        /// <summary>Whether notification has been read</summary>
        public bool IsRead;

        /// <summary>Notification timestamp</summary>
        public DateTime CreatedAt;
    }

    /// <summary>
    /// Active room summary for host dashboard
    /// </summary>
    [Serializable]
    public class ActiveRoomSummary
    {
        /// <summary>Room ID</summary>
        public string Id;

        /// <summary>Room code</summary>
        public string RoomCode;

        /// <summary>Room name</summary>
        public string Name;

        /// <summary>Game name</summary>
        public string GameName;

        /// <summary>Current player count</summary>
        public int CurrentPlayers;

        /// <summary>Maximum players</summary>
        public int MaxPlayers;

        /// <summary>Room revenue type</summary>
        public RoomRevenueType RevenueType;

        /// <summary>Current pot/prize pool</summary>
        public decimal CurrentPot;

        /// <summary>Accumulated rake (for social rooms)</summary>
        public decimal AccumulatedRake;

        /// <summary>Estimated host earnings so far</summary>
        public decimal EstimatedEarnings;

        /// <summary>Room start time</summary>
        public DateTime StartedAt;

        /// <summary>Current round number</summary>
        public int CurrentRound;

        /// <summary>Whether room is currently in a game</summary>
        public bool IsPlaying;
    }

    /// <summary>
    /// Host statistics for dashboard
    /// </summary>
    [Serializable]
    public class HostStats
    {
        /// <summary>Total rooms this month</summary>
        public int RoomsThisMonth;

        /// <summary>Total players this month</summary>
        public int PlayersThisMonth;

        /// <summary>Average players per room</summary>
        public float AvgPlayersPerRoom;

        /// <summary>Average room duration in minutes</summary>
        public float AvgRoomDuration;

        /// <summary>Room completion rate</summary>
        public float CompletionRate;

        /// <summary>Most popular game</summary>
        public string TopGame;

        /// <summary>Peak hosting hour (0-23)</summary>
        public int PeakHour;

        /// <summary>Days until tier reset</summary>
        public int DaysUntilTierReset;

        /// <summary>Progress to next tier (0-100)</summary>
        public float TierProgress;

        /// <summary>Progress to next level (0-100)</summary>
        public float LevelProgress;
    }

    // =========================================================================
    // REQUEST/RESPONSE MODELS
    // =========================================================================

    /// <summary>
    /// Request to update notification settings
    /// </summary>
    [Serializable]
    public class UpdateNotificationSettingsRequest
    {
        public bool pushEnabled;
        public bool inAppEnabled;
        public bool soundEnabled;
        public bool batchSettlements;
        public bool playerLeftSettlements;
        public bool sessionComplete;
        public bool tierChanges;
        public bool badgeEarned;
        public bool levelUp;
    }

    /// <summary>
    /// Request to withdraw earnings
    /// </summary>
    [Serializable]
    public class WithdrawRequest
    {
        public decimal amount;
        public string currency;
        public string walletAddress;
    }

    /// <summary>
    /// API response wrapper for host data
    /// </summary>
    [Serializable]
    internal class HostApiResponse<T>
    {
        public bool success;
        public T data;
        public string error;
        public long timestamp;
    }

    /// <summary>
    /// Host error information
    /// </summary>
    [Serializable]
    public class HostError
    {
        /// <summary>Error code</summary>
        public string Code;

        /// <summary>Error message</summary>
        public string Message;

        public HostError() { }

        public HostError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            return $"HostError({Code}: {Message})";
        }

        // Common error codes
        public static class Codes
        {
            public const string NotAuthenticated = "NOT_AUTHENTICATED";
            public const string NotVerified = "NOT_VERIFIED";
            public const string ProfileNotFound = "PROFILE_NOT_FOUND";
            public const string InsufficientBalance = "INSUFFICIENT_BALANCE";
            public const string MinimumWithdrawal = "MINIMUM_WITHDRAWAL";
            public const string InvalidWallet = "INVALID_WALLET";
            public const string WithdrawalPending = "WITHDRAWAL_PENDING";
            public const string RateLimited = "RATE_LIMITED";
            public const string ServerError = "SERVER_ERROR";
            public const string NetworkError = "NETWORK_ERROR";
        }
    }
}
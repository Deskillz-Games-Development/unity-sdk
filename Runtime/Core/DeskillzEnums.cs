// =============================================================================
// Deskillz SDK for Unity - Enumerations
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.5.2 (Web SDK Parity)
// =============================================================================
//
// MERGED: All enums from main SDK + StackIt SDK + v3.5.2 additions
//
// v3.5.2 additions:
//   - SocialWinCondition (5 values)
//   - UserEnrollmentStatus (10 values)
//   - HostRole (PLAYER/SPECTATOR)
//   - EsportMatchMode (3 values)
//   - Cleaned Currency enum (removed unsupported ETH/MATIC)
//   - CurrencyLabels helper dictionary
//
// =============================================================================

using System.Collections.Generic;

namespace Deskillz
{
    // =========================================================================
    // SDK STATE
    // =========================================================================

    /// <summary>
    /// SDK initialization state
    /// </summary>
    public enum SDKState
    {
        Uninitialized,
        Initializing,
        Ready,
        Failed,
        TestMode
    }

    // =========================================================================
    // AUTHENTICATION
    // =========================================================================

    /// <summary>
    /// Authentication state for self-sufficient architecture
    /// </summary>
    public enum AuthState
    {
        NotAuthenticated,
        Authenticating,
        Authenticated,
        Error
    }

    /// <summary>
    /// Authentication provider types
    /// </summary>
    public enum AuthProvider
    {
        Email,
        Google,
        Apple,
        Facebook,
        Guest,
        Wallet
    }

    // =========================================================================
    // MATCH & GAMEPLAY
    // =========================================================================

    /// <summary>
    /// Match gameplay modes
    /// </summary>
    public enum MatchMode
    {
        Asynchronous,
        Synchronous,
        CustomStage
    }

    /// <summary>
    /// Match type classification
    /// </summary>
    public enum MatchType
    {
        Asynchronous,
        Synchronous,
        Practice,
        PrivateRoom
    }

    /// <summary>
    /// Current state of a match
    /// </summary>
    public enum MatchStatus
    {
        None,
        Pending,
        Countdown,
        InProgress,
        Paused,
        Processing,
        Completed,
        Cancelled,
        Forfeited
    }

    /// <summary>
    /// Match result outcomes
    /// </summary>
    public enum MatchOutcome
    {
        Pending,
        Win,
        Loss,
        Tie,
        Forfeit,
        Cancelled,
        // Aliases for compatibility
        Lose = Loss,
        Draw = Tie
    }

    /// <summary>
    /// Score comparison types
    /// </summary>
    public enum ScoreType
    {
        HigherIsBetter,
        LowerIsBetter
    }

    /// <summary>
    /// Esport match modes (v3.5.2)
    /// Maps to backend EsportMatchMode enum
    /// </summary>
    public enum EsportMatchMode
    {
        SINGLE_MATCH,
        BEST_OF_3,
        BEST_OF_5
    }

    // =========================================================================
    // TOURNAMENTS
    // =========================================================================

    /// <summary>
    /// Tournament lifecycle status
    /// </summary>
    public enum TournamentStatus
    {
        Draft,
        Scheduled,
        Open,
        CheckIn,
        Starting,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Tournament format types
    /// </summary>
    public enum TournamentType
    {
        Standard,
        Bracket,
        Leaderboard,
        HeadToHead,
        TimedChallenge,
        SingleElimination,
        FreeForAll,
        Blitz1v1,
        Duel1v1
    }

    /// <summary>
    /// User enrollment status within a tournament (v3.5.2)
    /// Maps to bridge-types.ts UserEnrollmentStatus
    /// </summary>
    public enum UserEnrollmentStatus
    {
        NOT_REGISTERED,
        REGISTERED,
        CHECKIN_OPEN,
        CHECKED_IN,
        SEATED,
        PLAYING,
        WON,
        ELIMINATED,
        DQ_NO_SHOW,
        DQ_DISCONNECT,
        STANDBY,
        SUBBED_IN
    }

    /// <summary>
    /// Booking status for tournament table seating (v3.5.2)
    /// </summary>
    public enum BookingStatus
    {
        REGISTERED,
        CHECKED_IN,
        SEATED,
        PLAYING,
        WON,
        ELIMINATED,
        DQ_NO_SHOW,
        DQ_DISCONNECT,
        STANDBY,
        SUBBED_IN
    }

    // =========================================================================
    // SOCIAL GAMES (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Win condition for social Quick Play sessions (v3.5.2)
    /// Maps to bridge-types.ts SocialWinCondition
    /// </summary>
    public enum SocialWinCondition
    {
        /// <summary>First player to reach a target score wins</summary>
        FIRST_TO_POINTS,

        /// <summary>Play a fixed number of rounds, highest score wins</summary>
        FIXED_ROUNDS,

        /// <summary>Play for a fixed time duration</summary>
        TIMED_SESSION,

        /// <summary>Single game, one and done</summary>
        SINGLE_GAME,

        /// <summary>No win condition, play until all players leave</summary>
        OPEN_ENDED
    }

    /// <summary>
    /// Social game type classification
    /// </summary>
    public enum SocialGameType
    {
        BIG_TWO,
        MAHJONG,
        CHINESE_POKER_13,
        DOU_DIZHU
    }

    // =========================================================================
    // ROOMS & STAGES
    // =========================================================================

    /// <summary>
    /// Private room status
    /// </summary>
    public enum RoomStatus
    {
        Waiting,
        Starting,
        InProgress,
        Completed,
        Closed,
        Cancelled
    }

    /// <summary>
    /// Custom stage visibility settings
    /// </summary>
    public enum StageVisibility
    {
        Private,
        FriendsOnly,
        Public
    }

    /// <summary>
    /// Custom stage admin actions
    /// </summary>
    public enum StageAdminAction
    {
        Kick,
        Start,
        Cancel,
        TransferAdmin,
        UpdateConfig
    }

    /// <summary>
    /// Host role when creating a room (v3.5.2)
    /// Determines whether the host plays or spectates
    /// </summary>
    public enum HostRole
    {
        /// <summary>Host participates as a player</summary>
        PLAYER,

        /// <summary>Host watches as a spectator (does not occupy a seat)</summary>
        SPECTATOR
    }

    // =========================================================================
    // HOST SYSTEM
    // =========================================================================

    /// <summary>
    /// Host tier levels for revenue sharing
    /// </summary>
    public enum HostTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3,
        Diamond = 4,
        Elite = 5
    }

    /// <summary>
    /// Badge categories for hosts
    /// </summary>
    public enum BadgeCategory
    {
        Achievement,
        Milestone,
        Special,
        Seasonal,
        Promotional
    }

    // =========================================================================
    // PLAYER & NETWORK
    // =========================================================================

    /// <summary>
    /// Player roles within the platform
    /// </summary>
    public enum PlayerRole
    {
        Player,
        Developer,
        Admin,
        Moderator
    }

    /// <summary>
    /// Connection state for real-time features
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }

    /// <summary>
    /// Network message types for multiplayer
    /// </summary>
    public enum MessageType
    {
        StateSync,
        GameAction,
        Chat,
        System,
        Custom,
        Ping,
        Pong
    }

    // =========================================================================
    // CURRENCY & PAYMENTS
    // =========================================================================

    /// <summary>
    /// Supported cryptocurrencies (BSC and TRON networks)
    /// v3.5.2: Removed unsupported ETH/MATIC
    /// </summary>
    public enum Currency
    {
        Free,
        USD,
        BNB,
        USDT_BSC,
        USDT_TRON,
        USDC_BSC,
        USDC_TRON
    }

    /// <summary>
    /// Display labels for currencies (v3.5.2)
    /// Maps to bridge-types.ts CURRENCY_LABELS
    /// </summary>
    public static class CurrencyLabels
    {
        private static readonly Dictionary<Currency, string> Labels = new Dictionary<Currency, string>
        {
            { Currency.Free, "Free" },
            { Currency.USD, "USD" },
            { Currency.BNB, "BNB" },
            { Currency.USDT_BSC, "USDT (BSC)" },
            { Currency.USDT_TRON, "USDT (Tron)" },
            { Currency.USDC_BSC, "USDC (BSC)" },
            { Currency.USDC_TRON, "USDC (Tron)" },
        };

        private static readonly Dictionary<Currency, string> Symbols = new Dictionary<Currency, string>
        {
            { Currency.Free, "" },
            { Currency.USD, "$" },
            { Currency.BNB, "BNB" },
            { Currency.USDT_BSC, "USDT" },
            { Currency.USDT_TRON, "USDT" },
            { Currency.USDC_BSC, "USDC" },
            { Currency.USDC_TRON, "USDC" },
        };

        private static readonly Dictionary<Currency, string> Networks = new Dictionary<Currency, string>
        {
            { Currency.Free, "" },
            { Currency.USD, "" },
            { Currency.BNB, "BSC" },
            { Currency.USDT_BSC, "BSC" },
            { Currency.USDT_TRON, "TRON" },
            { Currency.USDC_BSC, "BSC" },
            { Currency.USDC_TRON, "TRON" },
        };

        /// <summary>Get display label (e.g. "USDT (BSC)")</summary>
        public static string GetLabel(Currency currency) =>
            Labels.TryGetValue(currency, out var label) ? label : currency.ToString();

        /// <summary>Get symbol only (e.g. "USDT")</summary>
        public static string GetSymbol(Currency currency) =>
            Symbols.TryGetValue(currency, out var sym) ? sym : currency.ToString();

        /// <summary>Get network name (e.g. "BSC", "TRON")</summary>
        public static string GetNetwork(Currency currency) =>
            Networks.TryGetValue(currency, out var net) ? net : "";

        /// <summary>Format amount with currency symbol (e.g. "$5.00 USDT")</summary>
        public static string Format(decimal amount, Currency currency)
        {
            if (currency == Currency.Free) return "Free";
            var sym = GetSymbol(currency);
            var net = GetNetwork(currency);
            var formatted = $"{amount:F2} {sym}";
            if (!string.IsNullOrEmpty(net)) formatted += $" ({net})";
            return formatted;
        }
    }

    /// <summary>
    /// Transaction types for wallet history
    /// </summary>
    public enum TransactionType
    {
        Deposit,
        Withdrawal,
        EntryFee,
        Prize,
        Refund,
        Bonus,
        HostEarnings,
        RakeShare,
        Settlement
    }

    /// <summary>
    /// Transaction status
    /// </summary>
    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    // =========================================================================
    // QUICK PLAY (v3.5.2)
    // =========================================================================

    /// <summary>
    /// Quick play queue state
    /// </summary>
    public enum QuickPlayQueueState
    {
        IDLE,
        QUEUED,
        FOUND,
        READY,
        PLAYING
    }

    /// <summary>
    /// Quick play prize type
    /// </summary>
    public enum QuickPlayPrizeType
    {
        WINNER_TAKES_ALL,
        TOP_HALF,
        PROPORTIONAL
    }

    // =========================================================================
    // LOGGING & ERRORS
    // =========================================================================

    /// <summary>
    /// Logging levels for SDK debug output
    /// </summary>
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4,
        Verbose = 5
    }

    /// <summary>
    /// Error codes returned by the SDK
    /// </summary>
    public enum ErrorCode
    {
        None = 0,
        Unknown = 1,

        // SDK Errors (100-199)
        NotInitialized = 100,
        InvalidApiKey = 101,
        ApiKeyExpired = 102,
        ConfigurationError = 103,

        // Network Errors (200-299)
        NetworkError = 200,
        Timeout = 201,
        ServerError = 202,
        WebSocketError = 203,
        ConnectionLost = 204,

        // Match Errors (300-399)
        NoActiveMatch = 300,
        MatchInProgress = 301,
        InvalidScore = 302,
        ScoreSubmissionFailed = 303,
        MatchNotFound = 304,
        MatchExpired = 305,

        // Auth Errors (400-499)
        Unauthorized = 400,
        NotAuthenticated = 401,
        InvalidCredentials = 402,
        AccountLocked = 403,
        EmailNotVerified = 404,
        TokenExpired = 405,

        // Stage/Room Errors (500-599)
        StageNotFound = 500,
        StageFull = 501,
        NotStageAdmin = 502,
        InvalidStageCode = 503,
        RoomNotFound = 504,
        RoomClosed = 505,
        RoomFull = 506,
        AlreadyInRoom = 507,

        // Anti-Cheat Errors (600-699)
        AntiCheatViolation = 600,
        ScoreValidationFailed = 601,
        SuspiciousActivity = 602,

        // Wallet Errors (700-799)
        WalletNotConnected = 700,
        InsufficientBalance = 701,
        WalletLinkFailed = 702,
        WithdrawalFailed = 703,

        // Tournament Errors (800-899)
        TournamentNotFound = 800,
        TournamentFull = 801,
        AlreadyRegistered = 802,
        CheckInNotOpen = 803,
        NotRegistered = 804,

        // Quick Play Errors (900-999)
        AlreadyInQueue = 900,
        NotInQueue = 901,
        QueueTimeout = 902,
        MatchLaunchFailed = 903
    }
}
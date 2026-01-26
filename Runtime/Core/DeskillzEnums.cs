// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 2.7.0 (Self-Sufficient Architecture)
// =============================================================================
//
// MERGED: Includes all enums from main SDK + StackIt SDK
// This file replaces both DeskillzEnums.cs and StackItDeskillzModels.cs enums
//
// =============================================================================

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

    // =========================================================================
    // TOURNAMENTS
    // =========================================================================

    /// <summary>
    /// Tournament lifecycle status
    /// </summary>
    public enum TournamentStatus
    {
        Draft,
        Open,
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
        TimedChallenge
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
    /// Tournament entry fee currencies (BSC and TRON networks)
    /// </summary>
    public enum Currency
    {
        Free,
        USD,
        BNB,
        USDT_BSC,
        USDT_TRON,
        USDC_BSC,
        USDC_TRON,
        ETH,
        MATIC
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
        HostEarnings
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
        
        // Anti-Cheat Errors (600-699)
        AntiCheatViolation = 600,
        ScoreValidationFailed = 601,
        SuspiciousActivity = 602,
        
        // Wallet Errors (700-799)
        WalletNotConnected = 700,
        InsufficientBalance = 701,
        WalletLinkFailed = 702,
        WithdrawalFailed = 703
    }
}
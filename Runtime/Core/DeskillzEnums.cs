// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

namespace Deskillz
{
    /// <summary>
    /// SDK initialization state
    /// </summary>
    public enum SDKState
    {
        /// <summary>SDK has not been initialized</summary>
        Uninitialized,
        
        /// <summary>SDK is currently initializing</summary>
        Initializing,
        
        /// <summary>SDK is ready to use</summary>
        Ready,
        
        /// <summary>SDK initialization failed</summary>
        Failed,
        
        /// <summary>SDK is in test/sandbox mode</summary>
        TestMode
    }

    /// <summary>
    /// Match gameplay modes
    /// </summary>
    public enum MatchMode
    {
        /// <summary>Players play separately, scores compared after deadline</summary>
        Asynchronous,
        
        /// <summary>Real-time multiplayer, all players in same session</summary>
        Synchronous,
        
        /// <summary>Private room created by a player with custom rules</summary>
        CustomStage
    }

    /// <summary>
    /// Current state of a match
    /// </summary>
    public enum MatchStatus
    {
        /// <summary>No active match</summary>
        None,
        
        /// <summary>Waiting for match to start</summary>
        Pending,
        
        /// <summary>Countdown before match begins</summary>
        Countdown,
        
        /// <summary>Match is actively being played</summary>
        InProgress,
        
        /// <summary>Match has been paused</summary>
        Paused,
        
        /// <summary>Match has ended, processing results</summary>
        Processing,
        
        /// <summary>Match completed successfully</summary>
        Completed,
        
        /// <summary>Match was cancelled</summary>
        Cancelled,
        
        /// <summary>Player forfeited the match</summary>
        Forfeited
    }

    /// <summary>
    /// Player roles within the platform
    /// </summary>
    public enum PlayerRole
    {
        /// <summary>Regular player</summary>
        Player,
        
        /// <summary>Game developer with SDK access</summary>
        Developer,
        
        /// <summary>Platform administrator</summary>
        Admin
    }

    /// <summary>
    /// Connection state for real-time features
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>Not connected to server</summary>
        Disconnected,
        
        /// <summary>Attempting to connect</summary>
        Connecting,
        
        /// <summary>Successfully connected</summary>
        Connected,
        
        /// <summary>Connection lost, attempting reconnect</summary>
        Reconnecting,
        
        /// <summary>Connection failed</summary>
        Failed
    }

    /// <summary>
    /// Score comparison types
    /// </summary>
    public enum ScoreType
    {
        /// <summary>Higher score wins (most games)</summary>
        HigherIsBetter,
        
        /// <summary>Lower score wins (racing, golf)</summary>
        LowerIsBetter
    }

    /// <summary>
    /// Tournament entry fee currencies
    /// </summary>
    public enum Currency
    {
        /// <summary>Free to play</summary>
        Free,
        
        /// <summary>Bitcoin</summary>
        BTC,
        
        /// <summary>Ethereum</summary>
        ETH,
        
        /// <summary>Solana</summary>
        SOL,
        
        /// <summary>Ripple</summary>
        XRP,
        
        /// <summary>Binance Coin</summary>
        BNB,
        
        /// <summary>Tether USD</summary>
        USDT,
        
        /// <summary>USD Coin</summary>
        USDC
    }

    /// <summary>
    /// Match result outcomes
    /// </summary>
    public enum MatchOutcome
    {
        /// <summary>Result not yet determined</summary>
        Pending,
        
        /// <summary>Local player won</summary>
        Win,
        
        /// <summary>Local player lost</summary>
        Loss,
        
        /// <summary>Match ended in a tie</summary>
        Tie,
        
        /// <summary>Local player forfeited</summary>
        Forfeit,
        
        /// <summary>Match was cancelled</summary>
        Cancelled
    }

    /// <summary>
    /// Custom stage visibility settings
    /// </summary>
    public enum StageVisibility
    {
        /// <summary>Only invited players can join</summary>
        Private,
        
        /// <summary>Only friends can see and join</summary>
        FriendsOnly,
        
        /// <summary>Anyone can find and join</summary>
        Public
    }

    /// <summary>
    /// Custom stage admin actions
    /// </summary>
    public enum StageAdminAction
    {
        /// <summary>Kick a player from the stage</summary>
        Kick,
        
        /// <summary>Start the match</summary>
        Start,
        
        /// <summary>Cancel the stage</summary>
        Cancel,
        
        /// <summary>Transfer admin to another player</summary>
        TransferAdmin,
        
        /// <summary>Update stage configuration</summary>
        UpdateConfig
    }

    /// <summary>
    /// Logging levels for SDK debug output
    /// </summary>
    public enum LogLevel
    {
        /// <summary>No logging</summary>
        None = 0,
        
        /// <summary>Only errors</summary>
        Error = 1,
        
        /// <summary>Errors and warnings</summary>
        Warning = 2,
        
        /// <summary>Errors, warnings, and info</summary>
        Info = 3,
        
        /// <summary>All messages including debug</summary>
        Debug = 4,
        
        /// <summary>Everything including verbose traces</summary>
        Verbose = 5
    }

    /// <summary>
    /// Network message types for multiplayer
    /// </summary>
    public enum MessageType
    {
        /// <summary>Player state synchronization</summary>
        StateSync,
        
        /// <summary>Game action/input</summary>
        GameAction,
        
        /// <summary>Chat message</summary>
        Chat,
        
        /// <summary>System notification</summary>
        System,
        
        /// <summary>Custom game data</summary>
        Custom
    }

    /// <summary>
    /// Error codes returned by the SDK
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>No error</summary>
        None = 0,
        
        /// <summary>Unknown error occurred</summary>
        Unknown = 1,
        
        /// <summary>SDK not initialized</summary>
        NotInitialized = 100,
        
        /// <summary>Invalid API key</summary>
        InvalidApiKey = 101,
        
        /// <summary>API key expired</summary>
        ApiKeyExpired = 102,
        
        /// <summary>Network request failed</summary>
        NetworkError = 200,
        
        /// <summary>Request timed out</summary>
        Timeout = 201,
        
        /// <summary>Server returned error</summary>
        ServerError = 202,
        
        /// <summary>WebSocket connection failed</summary>
        WebSocketError = 203,
        
        /// <summary>No active match</summary>
        NoActiveMatch = 300,
        
        /// <summary>Match already in progress</summary>
        MatchInProgress = 301,
        
        /// <summary>Invalid score value</summary>
        InvalidScore = 302,
        
        /// <summary>Score submission failed</summary>
        ScoreSubmissionFailed = 303,
        
        /// <summary>Not authorized for this action</summary>
        Unauthorized = 400,
        
        /// <summary>Player not authenticated</summary>
        NotAuthenticated = 401,
        
        /// <summary>Stage not found</summary>
        StageNotFound = 500,
        
        /// <summary>Stage is full</summary>
        StageFull = 501,
        
        /// <summary>Not stage admin</summary>
        NotStageAdmin = 502,
        
        /// <summary>Invalid stage code</summary>
        InvalidStageCode = 503,
        
        /// <summary>Anti-cheat violation detected</summary>
        AntiCheatViolation = 600,
        
        /// <summary>Score validation failed</summary>
        ScoreValidationFailed = 601
    }
}

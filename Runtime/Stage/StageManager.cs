// =============================================================================
// Deskillz SDK for Unity - Stage Manager (Updated for Lobby Architecture)
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================
//
// LOBBY ARCHITECTURE UPDATE
// ============================================
// Matchmaking and stage creation now happens on the Deskillz website.
// This manager handles room state synchronization AFTER players are matched.
//
// Flow:
// 1. Players join via website → Game launched via deep link
// 2. DeskillzBridge receives match data
// 3. StageManager syncs room state for multiplayer matches
// 4. Game handles actual gameplay
//
// Removed: CreateStage, JoinByCode, RefreshStageList (moved to website)
// Kept: Room state sync, player management, ready status
//
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deskillz.Multiplayer;

namespace Deskillz.Stage
{
    /// <summary>
    /// Manages room state for multiplayer matches.
    /// Handles player presence, ready status, and room synchronization.
    /// 
    /// Note: Stage creation and matchmaking now happens on the Deskillz website.
    /// This manager handles the room state AFTER players are matched.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static StageManager _instance;

        public static StageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StageManager>();
                    if (_instance == null && DeskillzManager.HasInstance)
                    {
                        _instance = DeskillzManager.Instance.gameObject.AddComponent<StageManager>();
                    }
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Room Settings")]
        [Tooltip("Auto-ready local player when joining")]
        [SerializeField] private bool _autoReadyOnJoin = false;

        [Tooltip("Countdown before match starts (seconds)")]
        [SerializeField] private int _startCountdown = 3;

        // =============================================================================
        // STATE
        // =============================================================================

        private StageRoom _currentRoom;
        private bool _isLocalPlayerReady;
        private Coroutine _countdownCoroutine;

        // =============================================================================
        // EVENTS
        // =============================================================================

        /// <summary>Fired when joined a room (from deep link).</summary>
        public event Action<StageRoom> OnRoomJoined;

        /// <summary>Fired when left the room.</summary>
        public event Action OnRoomLeft;

        /// <summary>Fired when room state is updated.</summary>
        public event Action<StageRoom> OnRoomUpdated;

        /// <summary>Fired when a player joins the room.</summary>
        public event Action<StagePlayer> OnPlayerJoined;

        /// <summary>Fired when a player leaves the room.</summary>
        public event Action<string> OnPlayerLeft;

        /// <summary>Fired when a player's ready status changes.</summary>
        public event Action<string, bool> OnPlayerReadyChanged;

        /// <summary>Fired when all players are ready.</summary>
        public event Action OnAllPlayersReady;

        /// <summary>Fired when countdown to match start begins.</summary>
        public event Action<int> OnCountdownStarted;

        /// <summary>Fired each countdown tick.</summary>
        public event Action<int> OnCountdownTick;

        /// <summary>Fired when match is about to launch.</summary>
        public event Action OnMatchLaunching;

        /// <summary>Fired when room is cancelled.</summary>
        public event Action<string> OnRoomCancelled;

        /// <summary>Fired on error.</summary>
        public event Action<string> OnError;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Current room (if any).</summary>
        public StageRoom CurrentRoom => _currentRoom;

        /// <summary>Whether currently in a room.</summary>
        public bool IsInRoom => _currentRoom != null;

        /// <summary>Whether local player is ready.</summary>
        public bool IsLocalPlayerReady => _isLocalPlayerReady;

        /// <summary>Whether all players are ready.</summary>
        public bool AreAllPlayersReady => _currentRoom?.AreAllPlayersReady ?? false;

        /// <summary>Number of players in room.</summary>
        public int PlayerCount => _currentRoom?.PlayerCount ?? 0;

        /// <summary>Number of ready players.</summary>
        public int ReadyPlayerCount => _currentRoom?.ReadyPlayerCount ?? 0;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to lobby client events
            DeskillzLobbyClient.OnPlayerJoined += HandleLobbyPlayerJoined;
            DeskillzLobbyClient.OnPlayerLeft += HandleLobbyPlayerLeft;
            DeskillzLobbyClient.OnPlayerReadyChanged += HandleLobbyPlayerReadyChanged;
            DeskillzLobbyClient.OnAllPlayersReady += HandleLobbyAllReady;
            DeskillzLobbyClient.OnMatchStateChanged += HandleMatchStateChanged;
            DeskillzLobbyClient.OnMatchCancelled += HandleMatchCancelled;
            DeskillzLobbyClient.OnGameLaunch += HandleGameLaunch;

            // Subscribe to bridge events
            DeskillzBridge.OnMatchReceived += HandleMatchReceived;
        }

        private void OnDisable()
        {
            DeskillzLobbyClient.OnPlayerJoined -= HandleLobbyPlayerJoined;
            DeskillzLobbyClient.OnPlayerLeft -= HandleLobbyPlayerLeft;
            DeskillzLobbyClient.OnPlayerReadyChanged -= HandleLobbyPlayerReadyChanged;
            DeskillzLobbyClient.OnAllPlayersReady -= HandleLobbyAllReady;
            DeskillzLobbyClient.OnMatchStateChanged -= HandleMatchStateChanged;
            DeskillzLobbyClient.OnMatchCancelled -= HandleMatchCancelled;
            DeskillzLobbyClient.OnGameLaunch -= HandleGameLaunch;

            DeskillzBridge.OnMatchReceived -= HandleMatchReceived;
        }

        private void OnDestroy()
        {
            LeaveRoom();
            if (_instance == this) _instance = null;
        }

        // =============================================================================
        // ROOM MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Initialize a room from match data (called after deep link).
        /// </summary>
        public void InitializeRoom(MatchInfo matchInfo)
        {
            if (matchInfo == null)
            {
                DeskillzLogger.Error("Cannot initialize room: null match info");
                return;
            }

            _currentRoom = new StageRoom
            {
                StageId = matchInfo.MatchId,
                HostId = "", // Set by server
                Status = StageStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                Config = new StageConfig
                {
                    Name = matchInfo.TournamentId,
                    MaxPlayers = matchInfo.MaxPlayers,
                    MinPlayers = 2,
                    EntryFee = matchInfo.EntryFee,
                    Currency = matchInfo.Currency,
                    TimeLimitSeconds = matchInfo.TimeLimitSeconds
                }
            };

            // Add local player
            var localPlayer = new StagePlayer
            {
                PlayerId = Deskillz.CurrentPlayer?.Id ?? "local_player",
                Username = Deskillz.CurrentPlayer?.Username ?? "Player",
                IsHost = false,
                IsReady = _autoReadyOnJoin,
                JoinedAt = DateTime.UtcNow
            };
            _currentRoom.AddPlayer(localPlayer);
            _isLocalPlayerReady = _autoReadyOnJoin;

            DeskillzLogger.Debug($"Room initialized: {_currentRoom.StageId}");

            // Connect to lobby for real-time updates
            DeskillzLobbyClient.Connect(matchInfo.MatchId, matchInfo.Token);

            OnRoomJoined?.Invoke(_currentRoom);
        }

        /// <summary>
        /// Leave the current room.
        /// </summary>
        public void LeaveRoom()
        {
            if (_currentRoom == null) return;

            var roomId = _currentRoom.StageId;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            DeskillzLobbyClient.Disconnect();

            _currentRoom = null;
            _isLocalPlayerReady = false;

            DeskillzLogger.Debug($"Left room: {roomId}");
            OnRoomLeft?.Invoke();
        }

        // =============================================================================
        // READY STATUS
        // =============================================================================

        /// <summary>
        /// Set local player's ready status.
        /// </summary>
        public void SetReady(bool ready)
        {
            if (_currentRoom == null)
            {
                OnError?.Invoke("Not in a room");
                return;
            }

            _isLocalPlayerReady = ready;

            var localPlayerId = Deskillz.CurrentPlayer?.Id ?? "local_player";
            _currentRoom.SetPlayerReady(localPlayerId, ready);

            // Send to server
            if (ready)
            {
                DeskillzLobbyClient.SignalReady();
            }
            else
            {
                DeskillzLobbyClient.SignalNotReady();
            }

            OnPlayerReadyChanged?.Invoke(localPlayerId, ready);
            OnRoomUpdated?.Invoke(_currentRoom);

            DeskillzLogger.Debug($"Local player ready: {ready}");
        }

        /// <summary>
        /// Toggle local player's ready status.
        /// </summary>
        public void ToggleReady()
        {
            SetReady(!_isLocalPlayerReady);
        }

        // =============================================================================
        // COUNTDOWN
        // =============================================================================

        private void StartCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            OnCountdownStarted?.Invoke(_startCountdown);

            for (int i = _startCountdown; i > 0; i--)
            {
                OnCountdownTick?.Invoke(i);
                DeskillzLogger.Debug($"Match starting in {i}...");
                yield return new WaitForSeconds(1f);
            }

            OnMatchLaunching?.Invoke();
        }

        private void CancelCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
                DeskillzLogger.Debug("Countdown cancelled");
            }
        }

        // =============================================================================
        // EVENT HANDLERS - LOBBY CLIENT
        // =============================================================================

        private void HandleLobbyPlayerJoined(PlayerPresence presence)
        {
            if (_currentRoom == null) return;

            var player = new StagePlayer
            {
                PlayerId = presence.PlayerId,
                Username = presence.Username,
                AvatarUrl = presence.AvatarUrl,
                Elo = presence.Rating,
                IsReady = presence.IsReady,
                JoinedAt = presence.JoinedAt
            };

            if (_currentRoom.AddPlayer(player))
            {
                OnPlayerJoined?.Invoke(player);
                OnRoomUpdated?.Invoke(_currentRoom);
                DeskillzLogger.Debug($"Player joined: {presence.Username}");
            }
        }

        private void HandleLobbyPlayerLeft(string playerId)
        {
            if (_currentRoom == null) return;

            if (_currentRoom.RemovePlayer(playerId))
            {
                OnPlayerLeft?.Invoke(playerId);
                OnRoomUpdated?.Invoke(_currentRoom);
                DeskillzLogger.Debug($"Player left: {playerId}");

                // Cancel countdown if in progress
                CancelCountdown();
            }
        }

        private void HandleLobbyPlayerReadyChanged(string playerId, bool isReady)
        {
            if (_currentRoom == null) return;

            _currentRoom.SetPlayerReady(playerId, isReady);
            OnPlayerReadyChanged?.Invoke(playerId, isReady);
            OnRoomUpdated?.Invoke(_currentRoom);

            // Check if all ready
            if (_currentRoom.AreAllPlayersReady && _currentRoom.HasMinimumPlayers)
            {
                OnAllPlayersReady?.Invoke();
            }
        }

        private void HandleLobbyAllReady()
        {
            OnAllPlayersReady?.Invoke();
            StartCountdown();
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (_currentRoom == null) return;

            switch (state)
            {
                case MatchState.Countdown:
                    _currentRoom.Status = StageStatus.Starting;
                    StartCountdown();
                    break;

                case MatchState.Launching:
                    _currentRoom.Status = StageStatus.InProgress;
                    OnMatchLaunching?.Invoke();
                    break;

                case MatchState.Cancelled:
                    _currentRoom.Status = StageStatus.Cancelled;
                    CancelCountdown();
                    break;
            }

            OnRoomUpdated?.Invoke(_currentRoom);
        }

        private void HandleMatchCancelled(string reason)
        {
            CancelCountdown();
            
            if (_currentRoom != null)
            {
                _currentRoom.Status = StageStatus.Cancelled;
            }

            OnRoomCancelled?.Invoke(reason);
            DeskillzLogger.Warning($"Room cancelled: {reason}");
        }

        private void HandleGameLaunch()
        {
            DeskillzLogger.Info("Game launching from lobby!");
            OnMatchLaunching?.Invoke();
        }

        // =============================================================================
        // EVENT HANDLERS - BRIDGE
        // =============================================================================

        private void HandleMatchReceived(MatchInfo matchInfo)
        {
            // Only initialize room for real-time (sync) matches
            if (matchInfo.IsRealtime)
            {
                InitializeRoom(matchInfo);
            }
        }

        // =============================================================================
        // PLAYER QUERIES
        // =============================================================================

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public StagePlayer GetPlayer(string playerId)
        {
            return _currentRoom?.GetPlayer(playerId);
        }

        /// <summary>
        /// Get local player.
        /// </summary>
        public StagePlayer GetLocalPlayer()
        {
            return _currentRoom?.GetLocalPlayer();
        }

        /// <summary>
        /// Get all players in room.
        /// </summary>
        public IReadOnlyList<StagePlayer> GetAllPlayers()
        {
            return _currentRoom?.Players ?? new List<StagePlayer>().AsReadOnly();
        }

        /// <summary>
        /// Get players sorted by score/rank.
        /// </summary>
        public List<StagePlayer> GetLeaderboard()
        {
            return _currentRoom?.GetLeaderboard() ?? new List<StagePlayer>();
        }

        // =============================================================================
        // DEPRECATED METHODS (Moved to website)
        // =============================================================================

        /// <summary>
        /// [DEPRECATED] Create a new stage.
        /// Stage creation now happens on the Deskillz website.
        /// </summary>
        [Obsolete("Stage creation now happens on deskillz.games. See DeskillzBridge.")]
        public void CreateStage(StageConfig config, Action<StageRoom> onSuccess = null, Action<string> onError = null)
        {
            var error = "Stage creation has moved to deskillz.games. " +
                       "Players now create and join stages on the website, " +
                       "then your game is launched via deep link.";
            DeskillzLogger.Warning(error);
            onError?.Invoke(error);
        }

        /// <summary>
        /// [DEPRECATED] Join a stage by invite code.
        /// Joining stages now happens on the Deskillz website.
        /// </summary>
        [Obsolete("Joining stages now happens on deskillz.games. See DeskillzBridge.")]
        public void JoinByCode(string code, Action<StageRoom> onSuccess = null, Action<string> onError = null)
        {
            var error = "Joining stages has moved to deskillz.games. " +
                       "Players now join via the website, then your game is launched via deep link.";
            DeskillzLogger.Warning(error);
            onError?.Invoke(error);
        }

        /// <summary>
        /// [DEPRECATED] Refresh the list of public stages.
        /// Stage browsing now happens on the Deskillz website.
        /// </summary>
        [Obsolete("Stage browsing now happens on deskillz.games. See DeskillzBridge.")]
        public void RefreshStageList(Action<List<StageInfo>> onSuccess = null, Action<string> onError = null)
        {
            var error = "Stage browsing has moved to deskillz.games.";
            DeskillzLogger.Warning(error);
            onError?.Invoke(error);
        }

        /// <summary>
        /// Open the Deskillz website to join stages.
        /// </summary>
        public void OpenLobbyWebsite()
        {
            string url = Deskillz.TestMode 
                ? "https://staging.deskillz.games" 
                : "https://deskillz.games";
            
            Application.OpenURL(url);
            DeskillzLogger.Info($"Opening lobby: {url}");
        }
    }
}
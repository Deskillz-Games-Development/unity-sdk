// =============================================================================
// Deskillz SDK for Unity - Phase 5: Sync Multiplayer
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Multiplayer
{
    /// <summary>
    /// Extension methods and utilities for multiplayer.
    /// </summary>
    public static class MultiplayerExtensions
    {
        // =============================================================================
        // SYNC MANAGER EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Send a game event to all players.
        /// </summary>
        public static void SendGameEvent(this SyncManager sync, string eventType, object data = null, Vector3? position = null)
        {
            var eventMsg = new GameEventMessage
            {
                EventType = eventType,
                Data = data != null ? JsonUtility.ToJson(data) : null,
                Position = position ?? Vector3.zero
            };

            sync.SendToAll(eventMsg, DeliveryMode.Reliable);
        }

        /// <summary>
        /// Send a chat message.
        /// </summary>
        public static void SendChat(this SyncManager sync, string message)
        {
            sync.SendGameEvent(GameEvents.ChatMessage, new { text = message });
        }

        /// <summary>
        /// Send an emote.
        /// </summary>
        public static void SendEmote(this SyncManager sync, string emoteId)
        {
            sync.SendGameEvent(GameEvents.Emote, new { id = emoteId });
        }

        /// <summary>
        /// Check if a player is connected.
        /// </summary>
        public static bool IsPlayerConnected(this SyncManager sync, string playerId)
        {
            if (playerId == sync.LocalPlayerId) return true;
            return sync.RemotePlayers.ContainsKey(playerId);
        }

        /// <summary>
        /// Get all connected player IDs.
        /// </summary>
        public static List<string> GetAllPlayerIds(this SyncManager sync)
        {
            var ids = new List<string> { sync.LocalPlayerId };
            foreach (var remote in sync.RemotePlayers.Keys)
            {
                ids.Add(remote);
            }
            return ids;
        }

        /// <summary>
        /// Get the host player ID.
        /// </summary>
        public static string GetHostId(this SyncManager sync)
        {
            if (sync.IsHost) return sync.LocalPlayerId;

            foreach (var kvp in sync.RemotePlayers)
            {
                if (kvp.Value.IsHost) return kvp.Key;
            }

            return null;
        }

        // =============================================================================
        // PLAYER STATE EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get distance between two player states.
        /// </summary>
        public static float DistanceTo(this PlayerState state, PlayerState other)
        {
            if (state == null || other == null) return float.MaxValue;
            return Vector3.Distance(state.Position, other.Position);
        }

        /// <summary>
        /// Check if player is within range.
        /// </summary>
        public static bool IsWithinRange(this PlayerState state, Vector3 position, float range)
        {
            return Vector3.Distance(state.Position, position) <= range;
        }

        /// <summary>
        /// Check if player is looking at position.
        /// </summary>
        public static bool IsLookingAt(this PlayerState state, Vector3 position, float angleTolerance = 30f)
        {
            Vector3 forward = Quaternion.Euler(state.Rotation) * Vector3.forward;
            Vector3 toTarget = (position - state.Position).normalized;
            float angle = Vector3.Angle(forward, toTarget);
            return angle <= angleTolerance;
        }

        /// <summary>
        /// Get direction to another state.
        /// </summary>
        public static Vector3 DirectionTo(this PlayerState state, PlayerState other)
        {
            if (state == null || other == null) return Vector3.zero;
            return (other.Position - state.Position).normalized;
        }

        /// <summary>
        /// Check if input flag is active.
        /// </summary>
        public static bool HasInput(this PlayerState state, InputFlags flag)
        {
            return (state.Inputs & flag) != 0;
        }

        // =============================================================================
        // INPUT FLAGS EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Check if any of the specified flags are active.
        /// </summary>
        public static bool HasAny(this InputFlags flags, InputFlags check)
        {
            return (flags & check) != 0;
        }

        /// <summary>
        /// Check if all of the specified flags are active.
        /// </summary>
        public static bool HasAll(this InputFlags flags, InputFlags check)
        {
            return (flags & check) == check;
        }

        /// <summary>
        /// Add flags.
        /// </summary>
        public static InputFlags With(this InputFlags flags, InputFlags add)
        {
            return flags | add;
        }

        /// <summary>
        /// Remove flags.
        /// </summary>
        public static InputFlags Without(this InputFlags flags, InputFlags remove)
        {
            return flags & ~remove;
        }

        // =============================================================================
        // NETWORK MESSAGE EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Check if message is from local player.
        /// </summary>
        public static bool IsFromLocalPlayer(this NetworkMessage message)
        {
            return message.SenderId == SyncManager.Instance?.LocalPlayerId;
        }

        /// <summary>
        /// Check if message is targeted at local player.
        /// </summary>
        public static bool IsForLocalPlayer(this NetworkMessage message)
        {
            if (string.IsNullOrEmpty(message.TargetId)) return true; // Broadcast
            return message.TargetId == SyncManager.Instance?.LocalPlayerId;
        }

        /// <summary>
        /// Get age of message in seconds.
        /// </summary>
        public static float GetAge(this NetworkMessage message)
        {
            if (message.Timestamp <= 0) return 0;
            var sync = SyncManager.Instance;
            if (sync == null) return 0;
            return (float)(sync.ServerTime - message.Timestamp);
        }
    }

    // =============================================================================
    // NETWORKED OBJECT POOL
    // =============================================================================

    /// <summary>
    /// Pool for networked objects to avoid GC.
    /// </summary>
    public class NetworkObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly int _maxSize;

        public NetworkObjectPool(int initialSize = 10, int maxSize = 100)
        {
            _maxSize = maxSize;

            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(new T());
            }
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public T Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new T();
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            if (_pool.Count < _maxSize)
            {
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// Clear the pool.
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }

    // =============================================================================
    // NETWORK AUTHORITY
    // =============================================================================

    /// <summary>
    /// Defines who has authority over a networked object.
    /// </summary>
    public enum NetworkAuthority
    {
        /// <summary>Server/host controls this object.</summary>
        Server,

        /// <summary>Owning client controls this object.</summary>
        Owner,

        /// <summary>Any client can modify.</summary>
        Shared
    }

    /// <summary>
    /// Component for network authority management.
    /// </summary>
    public class NetworkIdentity : MonoBehaviour
    {
        [Header("Network Identity")]
        [SerializeField] private string _networkId;
        [SerializeField] private NetworkAuthority _authority = NetworkAuthority.Server;

        private string _ownerId;
        private bool _hasAuthority;

        /// <summary>Unique network ID.</summary>
        public string NetworkId => _networkId;

        /// <summary>Authority mode.</summary>
        public NetworkAuthority Authority => _authority;

        /// <summary>Owner player ID.</summary>
        public string OwnerId => _ownerId;

        /// <summary>Whether local client has authority.</summary>
        public bool HasAuthority => _hasAuthority;

        /// <summary>Whether this is locally controlled.</summary>
        public bool IsLocal => _ownerId == SyncManager.Instance?.LocalPlayerId;

        /// <summary>
        /// Initialize with owner.
        /// </summary>
        public void Initialize(string ownerId)
        {
            if (string.IsNullOrEmpty(_networkId))
            {
                _networkId = Guid.NewGuid().ToString("N");
            }

            _ownerId = ownerId;
            UpdateAuthority();
        }

        /// <summary>
        /// Set authority mode.
        /// </summary>
        public void SetAuthority(NetworkAuthority authority)
        {
            _authority = authority;
            UpdateAuthority();
        }

        /// <summary>
        /// Transfer ownership.
        /// </summary>
        public void TransferOwnership(string newOwnerId)
        {
            _ownerId = newOwnerId;
            UpdateAuthority();
        }

        private void UpdateAuthority()
        {
            var sync = SyncManager.Instance;
            if (sync == null) return;

            _hasAuthority = _authority switch
            {
                NetworkAuthority.Server => sync.IsHost,
                NetworkAuthority.Owner => _ownerId == sync.LocalPlayerId,
                NetworkAuthority.Shared => true,
                _ => false
            };
        }
    }

    // =============================================================================
    // NETWORK TIME
    // =============================================================================

    /// <summary>
    /// Synchronized network time utilities.
    /// </summary>
    public static class NetworkTime
    {
        /// <summary>
        /// Get synchronized server time.
        /// </summary>
        public static double ServerTime => SyncManager.Instance?.ServerTime ?? Time.realtimeSinceStartup;

        /// <summary>
        /// Get time since match started.
        /// </summary>
        public static float MatchTime => Deskillz.Match.MatchController.Instance?.TimeElapsed ?? 0f;

        /// <summary>
        /// Get local to server time offset.
        /// </summary>
        public static double TimeOffset
        {
            get
            {
                var sync = SyncManager.Instance;
                if (sync == null) return 0;
                return sync.ServerTime - Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Convert local time to server time.
        /// </summary>
        public static double ToServerTime(float localTime)
        {
            return localTime + TimeOffset;
        }

        /// <summary>
        /// Convert server time to local time.
        /// </summary>
        public static float ToLocalTime(double serverTime)
        {
            return (float)(serverTime - TimeOffset);
        }
    }

    // =============================================================================
    // SPAWN MANAGER
    // =============================================================================

    /// <summary>
    /// Manages networked spawning and despawning.
    /// </summary>
    public class NetworkSpawnManager : MonoBehaviour
    {
        private static NetworkSpawnManager _instance;
        public static NetworkSpawnManager Instance => _instance;

        private readonly Dictionary<string, GameObject> _spawnedObjects = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        private void Awake()
        {
            _instance = this;
        }

        /// <summary>
        /// Register a prefab for network spawning.
        /// </summary>
        public void RegisterPrefab(string prefabId, GameObject prefab)
        {
            _prefabs[prefabId] = prefab;
        }

        /// <summary>
        /// Spawn an object across the network.
        /// </summary>
        public GameObject Spawn(string prefabId, Vector3 position, Quaternion rotation, string ownerId = null)
        {
            if (!_prefabs.TryGetValue(prefabId, out var prefab))
            {
                DeskillzLogger.Error($"Prefab not registered: {prefabId}");
                return null;
            }

            var networkId = Guid.NewGuid().ToString("N");
            var obj = Instantiate(prefab, position, rotation);

            var identity = obj.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                identity = obj.AddComponent<NetworkIdentity>();
            }

            identity.Initialize(ownerId ?? SyncManager.Instance?.LocalPlayerId);
            _spawnedObjects[networkId] = obj;

            // Notify other players
            SyncManager.Instance?.SendToAll(new SpawnMessage
            {
                NetworkId = networkId,
                PrefabId = prefabId,
                Position = position,
                Rotation = rotation.eulerAngles,
                OwnerId = ownerId
            }, DeliveryMode.Reliable);

            return obj;
        }

        /// <summary>
        /// Despawn a networked object.
        /// </summary>
        public void Despawn(string networkId)
        {
            if (_spawnedObjects.TryGetValue(networkId, out var obj))
            {
                Destroy(obj);
                _spawnedObjects.Remove(networkId);

                // Notify other players
                SyncManager.Instance?.SendToAll(new DespawnMessage
                {
                    NetworkId = networkId
                }, DeliveryMode.Reliable);
            }
        }

        /// <summary>
        /// Get a spawned object by network ID.
        /// </summary>
        public GameObject GetSpawnedObject(string networkId)
        {
            _spawnedObjects.TryGetValue(networkId, out var obj);
            return obj;
        }
    }

    [Serializable]
    internal class SpawnMessage
    {
        public string NetworkId;
        public string PrefabId;
        public Vector3 Position;
        public Vector3 Rotation;
        public string OwnerId;
    }

    [Serializable]
    internal class DespawnMessage
    {
        public string NetworkId;
    }
}

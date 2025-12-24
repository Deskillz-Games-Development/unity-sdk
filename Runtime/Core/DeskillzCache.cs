// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Local caching and persistence for the Deskillz SDK.
    /// Handles offline data storage and retrieval.
    /// </summary>
    public static class DeskillzCache
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string CACHE_FOLDER = "DeskillzCache";
        private const string PLAYER_CACHE_KEY = "deskillz_player";
        private const string MATCH_CACHE_KEY = "deskillz_match";
        private const string PENDING_SCORES_KEY = "deskillz_pending_scores";
        private const string SESSION_KEY = "deskillz_session";

        // =============================================================================
        // PRIVATE STATE
        // =============================================================================

        private static readonly Dictionary<string, object> _memoryCache = new Dictionary<string, object>();
        private static readonly object _lockObject = new object();
        private static string _cachePath;
        private static bool _initialized;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize the cache system.
        /// </summary>
        internal static void Initialize()
        {
            if (_initialized) return;

            lock (_lockObject)
            {
                if (_initialized) return;

                _cachePath = Path.Combine(Application.persistentDataPath, CACHE_FOLDER);
                
                try
                {
                    if (!Directory.Exists(_cachePath))
                    {
                        Directory.CreateDirectory(_cachePath);
                    }
                    _initialized = true;
                    DeskillzLogger.Debug($"Cache initialized at: {_cachePath}");
                }
                catch (Exception ex)
                {
                    DeskillzLogger.Error("Failed to initialize cache", ex);
                    // Continue without file caching, use memory only
                    _initialized = true;
                }
            }
        }

        // =============================================================================
        // PLAYER DATA CACHING
        // =============================================================================

        /// <summary>
        /// Cache player data for quick access and offline support.
        /// </summary>
        internal static void CachePlayer(PlayerData player)
        {
            if (player == null) return;
            
            Set(PLAYER_CACHE_KEY, player);
            DeskillzLogger.Verbose($"Cached player: {player.Username}");
        }

        /// <summary>
        /// Get cached player data.
        /// </summary>
        internal static PlayerData GetCachedPlayer()
        {
            return Get<PlayerData>(PLAYER_CACHE_KEY);
        }

        /// <summary>
        /// Clear cached player data.
        /// </summary>
        internal static void ClearPlayer()
        {
            Remove(PLAYER_CACHE_KEY);
            DeskillzLogger.Verbose("Cleared cached player");
        }

        // =============================================================================
        // MATCH DATA CACHING
        // =============================================================================

        /// <summary>
        /// Cache current match data.
        /// </summary>
        internal static void CacheMatch(MatchData match)
        {
            if (match == null) return;
            
            Set(MATCH_CACHE_KEY, match);
            DeskillzLogger.Verbose($"Cached match: {match.MatchId}");
        }

        /// <summary>
        /// Get cached match data.
        /// </summary>
        internal static MatchData GetCachedMatch()
        {
            return Get<MatchData>(MATCH_CACHE_KEY);
        }

        /// <summary>
        /// Clear cached match data.
        /// </summary>
        internal static void ClearMatch()
        {
            Remove(MATCH_CACHE_KEY);
            DeskillzLogger.Verbose("Cleared cached match");
        }

        // =============================================================================
        // PENDING SCORES (Offline Support)
        // =============================================================================

        /// <summary>
        /// Queue a score for submission when connection is restored.
        /// </summary>
        internal static void QueuePendingScore(string matchId, int score, long timestamp)
        {
            var pending = GetPendingScores();
            pending.Add(new PendingScore
            {
                MatchId = matchId,
                Score = score,
                Timestamp = timestamp
            });
            
            SetPersistent(PENDING_SCORES_KEY, pending);
            DeskillzLogger.Info($"Queued pending score: {score} for match {matchId}");
        }

        /// <summary>
        /// Get all pending scores.
        /// </summary>
        internal static List<PendingScore> GetPendingScores()
        {
            return GetPersistent<List<PendingScore>>(PENDING_SCORES_KEY) ?? new List<PendingScore>();
        }

        /// <summary>
        /// Remove a pending score after successful submission.
        /// </summary>
        internal static void RemovePendingScore(string matchId)
        {
            var pending = GetPendingScores();
            pending.RemoveAll(p => p.MatchId == matchId);
            SetPersistent(PENDING_SCORES_KEY, pending);
            DeskillzLogger.Verbose($"Removed pending score for match {matchId}");
        }

        /// <summary>
        /// Clear all pending scores.
        /// </summary>
        internal static void ClearPendingScores()
        {
            RemovePersistent(PENDING_SCORES_KEY);
            DeskillzLogger.Verbose("Cleared all pending scores");
        }

        // =============================================================================
        // SESSION DATA
        // =============================================================================

        /// <summary>
        /// Save session data.
        /// </summary>
        internal static void SaveSession(string sessionToken, DateTime expiry)
        {
            var session = new SessionData
            {
                Token = sessionToken,
                Expiry = expiry.Ticks
            };
            SetPersistent(SESSION_KEY, session);
        }

        /// <summary>
        /// Get saved session if still valid.
        /// </summary>
        internal static string GetValidSession()
        {
            var session = GetPersistent<SessionData>(SESSION_KEY);
            if (session == null) return null;

            var expiry = new DateTime(session.Expiry);
            if (DateTime.UtcNow >= expiry)
            {
                RemovePersistent(SESSION_KEY);
                return null;
            }

            return session.Token;
        }

        /// <summary>
        /// Clear session data.
        /// </summary>
        internal static void ClearSession()
        {
            RemovePersistent(SESSION_KEY);
        }

        // =============================================================================
        // GENERIC MEMORY CACHE
        // =============================================================================

        /// <summary>
        /// Set a value in memory cache.
        /// </summary>
        public static void Set<T>(string key, T value)
        {
            lock (_lockObject)
            {
                _memoryCache[key] = value;
            }
        }

        /// <summary>
        /// Get a value from memory cache.
        /// </summary>
        public static T Get<T>(string key)
        {
            lock (_lockObject)
            {
                if (_memoryCache.TryGetValue(key, out var value))
                {
                    return (T)value;
                }
                return default;
            }
        }

        /// <summary>
        /// Check if key exists in memory cache.
        /// </summary>
        public static bool Has(string key)
        {
            lock (_lockObject)
            {
                return _memoryCache.ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove a value from memory cache.
        /// </summary>
        public static void Remove(string key)
        {
            lock (_lockObject)
            {
                _memoryCache.Remove(key);
            }
        }

        /// <summary>
        /// Clear all memory cache.
        /// </summary>
        public static void ClearMemory()
        {
            lock (_lockObject)
            {
                _memoryCache.Clear();
            }
            DeskillzLogger.Verbose("Memory cache cleared");
        }

        // =============================================================================
        // PERSISTENT STORAGE
        // =============================================================================

        /// <summary>
        /// Save data to persistent storage.
        /// </summary>
        public static void SetPersistent<T>(string key, T value)
        {
            Initialize();

            try
            {
                var json = JsonUtility.ToJson(new SerializableWrapper<T> { Value = value });
                var path = GetFilePath(key);
                File.WriteAllText(path, json);
                DeskillzLogger.Verbose($"Saved to persistent storage: {key}");
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to save {key} to persistent storage", ex);
            }
        }

        /// <summary>
        /// Load data from persistent storage.
        /// </summary>
        public static T GetPersistent<T>(string key)
        {
            Initialize();

            try
            {
                var path = GetFilePath(key);
                if (!File.Exists(path)) return default;

                var json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<SerializableWrapper<T>>(json);
                return wrapper.Value;
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to load {key} from persistent storage", ex);
                return default;
            }
        }

        /// <summary>
        /// Check if key exists in persistent storage.
        /// </summary>
        public static bool HasPersistent(string key)
        {
            Initialize();
            return File.Exists(GetFilePath(key));
        }

        /// <summary>
        /// Remove data from persistent storage.
        /// </summary>
        public static void RemovePersistent(string key)
        {
            Initialize();

            try
            {
                var path = GetFilePath(key);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    DeskillzLogger.Verbose($"Removed from persistent storage: {key}");
                }
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"Failed to remove {key} from persistent storage", ex);
            }
        }

        /// <summary>
        /// Clear all persistent storage.
        /// </summary>
        public static void ClearPersistent()
        {
            Initialize();

            try
            {
                if (Directory.Exists(_cachePath))
                {
                    Directory.Delete(_cachePath, true);
                    Directory.CreateDirectory(_cachePath);
                }
                DeskillzLogger.Info("Persistent storage cleared");
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Failed to clear persistent storage", ex);
            }
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        /// <summary>
        /// Clear all cached data (memory and persistent).
        /// </summary>
        public static void ClearAll()
        {
            ClearMemory();
            ClearPersistent();
            DeskillzLogger.Info("All cache cleared");
        }

        /// <summary>
        /// Clear session-specific data (keep persistent settings).
        /// </summary>
        internal static void ClearSessionData()
        {
            ClearMemory();
            ClearMatch();
            DeskillzLogger.Verbose("Session data cleared");
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private static string GetFilePath(string key)
        {
            // Sanitize key for filename
            var safeKey = key.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            return Path.Combine(_cachePath, $"{safeKey}.json");
        }

        // =============================================================================
        // SERIALIZATION HELPERS
        // =============================================================================

        [Serializable]
        private class SerializableWrapper<T>
        {
            public T Value;
        }

        [Serializable]
        internal class PendingScore
        {
            public string MatchId;
            public int Score;
            public long Timestamp;
        }

        [Serializable]
        private class SessionData
        {
            public string Token;
            public long Expiry;
        }
    }
}

// =============================================================================
// Deskillz SDK for Unity - Phase 3: Score System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Deskillz.Score
{
    /// <summary>
    /// Handles encryption and signing of score data.
    /// Uses AES-256-GCM for encryption and HMAC-SHA256 for signatures.
    /// </summary>
    public class ScoreEncryption
    {
        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const int KEY_SIZE = 256;           // AES-256
        private const int IV_SIZE = 12;             // GCM standard IV size
        private const int TAG_SIZE = 16;            // GCM auth tag size
        private const int SALT_SIZE = 16;           // Salt for key derivation
        private const int ITERATIONS = 10000;       // PBKDF2 iterations

        // =============================================================================
        // STATE
        // =============================================================================

        private byte[] _encryptionKey;
        private byte[] _signingKey;
        private string _matchId;
        private string _sessionToken;
        private bool _isInitialized;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Whether encryption is initialized.</summary>
        public bool IsInitialized => _isInitialized;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initialize encryption with match-specific keys.
        /// </summary>
        public void Initialize(string matchId, string sessionToken)
        {
            _matchId = matchId;
            _sessionToken = sessionToken;

            // Derive keys from match ID and session
            DeriveKeys();

            _isInitialized = true;
            DeskillzLogger.Debug("Score encryption initialized");
        }

        /// <summary>
        /// Initialize with a pre-shared key (from server).
        /// </summary>
        public void Initialize(byte[] preSharedKey)
        {
            if (preSharedKey == null || preSharedKey.Length < 32)
            {
                throw new ArgumentException("Invalid pre-shared key");
            }

            _encryptionKey = new byte[32];
            _signingKey = new byte[32];

            Array.Copy(preSharedKey, 0, _encryptionKey, 0, 32);

            // Derive signing key from encryption key
            using (var hmac = new HMACSHA256(preSharedKey))
            {
                _signingKey = hmac.ComputeHash(Encoding.UTF8.GetBytes("signing_key"));
            }

            _isInitialized = true;
        }

        private void DeriveKeys()
        {
            // Combine match ID, session token, and API key for key derivation
            var apiKey = DeskillzConfig.Instance?.ApiKey ?? "default_key";
            var keyMaterial = $"{_matchId}:{_sessionToken}:{apiKey}";

            // Generate salt from match ID
            var salt = new byte[SALT_SIZE];
            using (var sha = SHA256.Create())
            {
                var matchHash = sha.ComputeHash(Encoding.UTF8.GetBytes(_matchId));
                Array.Copy(matchHash, 0, salt, 0, SALT_SIZE);
            }

            // Derive encryption key using PBKDF2
            using (var pbkdf2 = new Rfc2898DeriveBytes(keyMaterial, salt, ITERATIONS, HashAlgorithmName.SHA256))
            {
                _encryptionKey = pbkdf2.GetBytes(32);  // 256 bits
                _signingKey = pbkdf2.GetBytes(32);     // 256 bits
            }
        }

        // =============================================================================
        // ENCRYPTION
        // =============================================================================

        /// <summary>
        /// Encrypt a score value.
        /// </summary>
        public string EncryptScore(int score, long timestamp)
        {
            if (!_isInitialized)
            {
                DeskillzLogger.Warning("Encryption not initialized, returning plaintext");
                return score.ToString();
            }

            try
            {
                // Create payload with score and timestamp
                var payload = $"{score}:{timestamp}:{_matchId}";
                return Encrypt(payload);
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Score encryption failed", ex);
                return score.ToString();
            }
        }

        /// <summary>
        /// Encrypt arbitrary data.
        /// </summary>
        public string Encrypt(string plaintext)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Encryption not initialized");
            }

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            // Generate random IV
            var iv = new byte[IV_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            // Encrypt using AES-GCM
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TAG_SIZE];

            using (var aesGcm = new AesGcm(_encryptionKey))
            {
                aesGcm.Encrypt(iv, plaintextBytes, ciphertext, tag);
            }

            // Combine IV + ciphertext + tag
            var combined = new byte[IV_SIZE + ciphertext.Length + TAG_SIZE];
            Array.Copy(iv, 0, combined, 0, IV_SIZE);
            Array.Copy(ciphertext, 0, combined, IV_SIZE, ciphertext.Length);
            Array.Copy(tag, 0, combined, IV_SIZE + ciphertext.Length, TAG_SIZE);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Decrypt data (for testing/verification).
        /// </summary>
        public string Decrypt(string encryptedBase64)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Encryption not initialized");
            }

            var combined = Convert.FromBase64String(encryptedBase64);

            if (combined.Length < IV_SIZE + TAG_SIZE)
            {
                throw new ArgumentException("Invalid encrypted data");
            }

            // Extract IV, ciphertext, and tag
            var iv = new byte[IV_SIZE];
            var ciphertext = new byte[combined.Length - IV_SIZE - TAG_SIZE];
            var tag = new byte[TAG_SIZE];

            Array.Copy(combined, 0, iv, 0, IV_SIZE);
            Array.Copy(combined, IV_SIZE, ciphertext, 0, ciphertext.Length);
            Array.Copy(combined, IV_SIZE + ciphertext.Length, tag, 0, TAG_SIZE);

            // Decrypt
            var plaintext = new byte[ciphertext.Length];

            using (var aesGcm = new AesGcm(_encryptionKey))
            {
                aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }

        // =============================================================================
        // SIGNING
        // =============================================================================

        /// <summary>
        /// Sign a payload to ensure integrity.
        /// </summary>
        public string SignPayload(SecureScorePayload payload)
        {
            if (!_isInitialized)
            {
                DeskillzLogger.Warning("Encryption not initialized, cannot sign");
                return string.Empty;
            }

            try
            {
                // Create canonical string for signing
                var canonical = BuildCanonicalString(payload);
                return Sign(canonical);
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error("Payload signing failed", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Sign arbitrary data.
        /// </summary>
        public string Sign(string data)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Encryption not initialized");
            }

            using (var hmac = new HMACSHA256(_signingKey))
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var hash = hmac.ComputeHash(dataBytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verify a signature.
        /// </summary>
        public bool VerifySignature(string data, string signature)
        {
            var expectedSignature = Sign(data);
            return SecureCompare(expectedSignature, signature);
        }

        private string BuildCanonicalString(SecureScorePayload payload)
        {
            // Create deterministic string for signing
            var sb = new StringBuilder();
            sb.Append(payload.MatchId);
            sb.Append(':');
            sb.Append(payload.PlayerId);
            sb.Append(':');
            sb.Append(payload.Score);
            sb.Append(':');
            sb.Append(payload.Timestamp);
            sb.Append(':');
            sb.Append(payload.Sequence);
            sb.Append(':');
            sb.Append(payload.GameTime.ToString("F3"));
            sb.Append(':');
            sb.Append(payload.Round);
            return sb.ToString();
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Generate a random token.
        /// </summary>
        public static string GenerateToken(int length = 32)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Hash data with SHA256.
        /// </summary>
        public static string Hash(string data)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks.
        /// </summary>
        private static bool SecureCompare(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }

        /// <summary>
        /// Clear sensitive data from memory.
        /// </summary>
        public void ClearKeys()
        {
            if (_encryptionKey != null)
            {
                Array.Clear(_encryptionKey, 0, _encryptionKey.Length);
                _encryptionKey = null;
            }

            if (_signingKey != null)
            {
                Array.Clear(_signingKey, 0, _signingKey.Length);
                _signingKey = null;
            }

            _isInitialized = false;
            DeskillzLogger.Debug("Encryption keys cleared");
        }

        // =============================================================================
        // OBFUSCATION HELPERS
        // =============================================================================

        /// <summary>
        /// Obfuscate a score value (for memory protection).
        /// </summary>
        public static int ObfuscateScore(int score, int key)
        {
            return score ^ key ^ (key >> 16);
        }

        /// <summary>
        /// Deobfuscate a score value.
        /// </summary>
        public static int DeobfuscateScore(int obfuscated, int key)
        {
            return obfuscated ^ key ^ (key >> 16);
        }

        /// <summary>
        /// Generate an obfuscation key.
        /// </summary>
        public static int GenerateObfuscationKey()
        {
            var bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToInt32(bytes, 0);
        }
    }

    // =============================================================================
    // SECURE SCORE PAYLOAD
    // =============================================================================

    /// <summary>
    /// Encrypted and signed score payload for server submission.
    /// </summary>
    [Serializable]
    public class SecureScorePayload
    {
        // Identifiers
        public string MatchId { get; set; }
        public string PlayerId { get; set; }

        // Score data
        public int Score { get; set; }
        public long Timestamp { get; set; }
        public int Sequence { get; set; }
        public bool IsCheckpoint { get; set; }

        // Game state
        public float GameTime { get; set; }
        public int Round { get; set; }

        // Client info
        public string ClientVersion { get; set; }
        public string Platform { get; set; }

        // Security
        public string EncryptedScore { get; set; }
        public string Signature { get; set; }
        public ValidationData ValidationData { get; set; }

        /// <summary>
        /// Check if payload has valid security data.
        /// </summary>
        public bool HasSecurityData => 
            !string.IsNullOrEmpty(EncryptedScore) && 
            !string.IsNullOrEmpty(Signature);
    }

    /// <summary>
    /// Additional validation data for anti-cheat.
    /// </summary>
    [Serializable]
    public class ValidationData
    {
        public string ScoreHash { get; set; }
        public string StateHash { get; set; }
        public int InputCount { get; set; }
        public float PlayTime { get; set; }
        public int ActionCount { get; set; }
        public string DeviceFingerprint { get; set; }
    }
}

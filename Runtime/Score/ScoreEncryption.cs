// =============================================================================
// Deskillz SDK for Unity -- score integrity helpers  [P1-3.7.0]
// AES-GCM transport encryption was removed (the .NET AES-GCM class is not in Unity's
// profile and the backend never decrypted it). The server-side check is the
// HMAC-SHA256 signature produced by ScoreSigner; this class keeps the local
// hashing / obfuscation helpers used by ScoreValidator and ScoreExtensions.
// =============================================================================
using System;
using System.Security.Cryptography;
using System.Text;

namespace Deskillz
{
    public class ScoreEncryption
    {
        private byte[] _signingKey;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public void Initialize(string matchId, string sessionToken)
        {
            var secret = LaunchSession.Current?.ScoreSecret;
            if (string.IsNullOrEmpty(secret)) secret = sessionToken ?? "";
            Initialize(Encoding.UTF8.GetBytes(secret + ":" + (matchId ?? "")));
        }

        public void Initialize(byte[] preSharedKey)
        {
            _signingKey = preSharedKey ?? new byte[0];
            _isInitialized = true;
        }

        public string SignPayload(SecureScorePayload payload)
        {
            if (payload == null) return null;
            return Sign(string.Join(":", new[] { payload.MatchId, payload.PlayerId, payload.Score.ToString(), payload.Timestamp.ToString(), payload.Sequence.ToString() }));
        }

        public string Sign(string data)
        {
            if (!_isInitialized) throw new InvalidOperationException("ScoreEncryption not initialized");
            using (var h = new HMACSHA256(_signingKey)) return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(data ?? "")));
        }

        public bool VerifySignature(string data, string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            var expected = Sign(data);
            if (expected.Length != signature.Length) return false;
            var diff = 0;
            for (var i = 0; i < expected.Length; i++) diff |= expected[i] ^ signature[i];
            return diff == 0;
        }

        public static string GenerateToken(int length = 32)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        public static string Hash(string data)
        {
            return ScoreSigner.Sha256Hex(data);
        }

        public void ClearKeys()
        {
            if (_signingKey != null) Array.Clear(_signingKey, 0, _signingKey.Length);
            _signingKey = null;
            _isInitialized = false;
        }

        public static int ObfuscateScore(int score, int key) => score ^ key;
        public static int DeobfuscateScore(int obfuscated, int key) => obfuscated ^ key;

        public static int GenerateObfuscationKey()
        {
            var b = new byte[4];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            return BitConverter.ToInt32(b, 0);
        }
    }

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
        public string Signature { get; set; }
        public ValidationData ValidationData { get; set; }

        /// <summary>
        /// Check if payload has valid security data.
        /// </summary>
        public bool HasSecurityData => !string.IsNullOrEmpty(Signature);
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

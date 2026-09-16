// =============================================================================
// Deskillz SDK for Unity -- score signing  [P1-3.7.0]
// Backend launch-token.service verifyScoreSignature:
//   message   = gameId:matchId:score:timestamp:userId
//   signature = HMAC-SHA256(scoreSecret, message) as lowercase hex
// scoreSecret comes from the launch exchange; the game key is the fallback
// used by non-launch flows (same order as the web bridge, H4).
// =============================================================================
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Deskillz.Tournaments;

namespace Deskillz
{
    [Serializable]
    public sealed class SignedScorePayload
    {
        public string matchId;
        public string gameId;
        public string playerId;
        public int score;
        public long timestamp;
        public string signature;
        public string hash;
    }

    public static class ScoreSigner
    {
        public static string HmacHex(string key, string message)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(key ?? "")))
            {
                return ToHex(h.ComputeHash(Encoding.UTF8.GetBytes(message ?? "")));
            }
        }

        public static string Sha256Hex(string text)
        {
            using (var s = SHA256.Create()) return ToHex(s.ComputeHash(Encoding.UTF8.GetBytes(text ?? "")));
        }

        private static string ToHex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            foreach (var x in b) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Build the signed body for POST /api/v1/tournaments/:id/score.</summary>
        public static SignedScorePayload Build(string matchId, int score, long timestampMs = 0)
        {
            var cfg = DeskillzManager.Instance?.Config;
            var session = LaunchSession.Current;
            var gameId = cfg?.GameId ?? "";
            var userId = session?.UserId ?? DeskillzManager.Instance?.CurrentPlayer?.Id ?? "";
            var ts = timestampMs > 0 ? timestampMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var secret = !string.IsNullOrEmpty(session?.ScoreSecret) ? session.ScoreSecret : (cfg?.ApiKey ?? "");
            var message = string.Join(":", new[] { gameId, matchId ?? "", score.ToString(), ts.ToString(), userId });
            var p = new SignedScorePayload
            {
                matchId = matchId,
                gameId = gameId,
                playerId = userId,
                score = score,
                timestamp = ts,
                signature = HmacHex(secret, message)
            };
            p.hash = Sha256Hex(DeskillzJson.Serialize(new { p.score, p.gameId, p.matchId, p.playerId, p.timestamp }));
            return p;
        }

        /// <summary>Resolve the tournament id for the current match (launch session first).</summary>
        public static string CurrentTournamentId()
        {
            var id = LaunchSession.Current?.TournamentId;
            if (string.IsNullOrEmpty(id)) id = DeskillzManager.Instance?.CurrentMatch?.TournamentId;
            return id;
        }

        /// <summary>POST the signed final score for the current tournament match.</summary>
        internal static IEnumerator SubmitTournamentScore(DeskillzNetwork network, string matchId, int score,
            Action onSuccess, Action<DeskillzError> onError)
        {
            if (network == null) { onError?.Invoke(new DeskillzError(ErrorCode.NotInitialized, "Network not available")); yield break; }
            var tournamentId = CurrentTournamentId();
            if (string.IsNullOrEmpty(tournamentId))
            {
                onError?.Invoke(new DeskillzError(ErrorCode.NoActiveMatch, "No tournament context: launch the game from a Deskillz tournament link", false));
                yield break;
            }
            var body = Build(matchId, score);
            yield return network.Post<ScoreSubmitAck>(
                $"/tournaments/{tournamentId}/score",
                body,
                _ => onSuccess?.Invoke(),
                err => onError?.Invoke(err));
        }
    }
}

// =============================================================================
// Deskillz SDK for Unity -- launch token exchange  [P1-3.7.0]
// POST /api/v1/auth/launch/exchange { token } -> { accessToken, scoreSecret, ... }
// The scoreSecret is per launch and keys the HMAC score signature (ScoreSigner).
// =============================================================================
using System;
using System.Collections;

namespace Deskillz
{
    [Serializable]
    public sealed class LaunchExchangeRequest
    {
        public string token;
        public LaunchExchangeRequest(string t) { token = t; }
    }

    [Serializable]
    public sealed class LaunchExchangeResponse
    {
        public string accessToken;
        public string scoreSecret;
        public string userId;
        public string matchId;
        public string tournamentId;
        public long expiresIn;
    }

    /// <summary>The authenticated game session created from a launch link.</summary>
    public sealed class LaunchSession
    {
        public static LaunchSession Current { get; private set; }

        public string MatchId { get; private set; }
        public string TournamentId { get; private set; }
        public string AccessToken { get; private set; }
        public string ScoreSecret { get; private set; }
        public string UserId { get; internal set; }
        public DateTime CreatedUtc { get; private set; }
        public DeepLinkParams Params { get; private set; }

        internal static void Clear() { Current = null; }

        /// <summary>Exchange the single-use launch token. Sets Current and the network Bearer token on success.</summary>
        internal static IEnumerator Exchange(DeskillzNetwork network, DeepLinkParams p,
            Action<LaunchSession> onSuccess, Action<DeskillzError> onError)
        {
            if (network == null) { onError?.Invoke(new DeskillzError(ErrorCode.NotInitialized, "Network not available")); yield break; }
            if (p == null || !p.IsLaunch) { onError?.Invoke(new DeskillzError(ErrorCode.MatchNotFound, "Launch URL has no matchId/token")); yield break; }

            yield return network.Post<LaunchExchangeResponse>(
                "/auth/launch/exchange",
                new LaunchExchangeRequest(p.PlayerToken),
                res =>
                {
                    if (res == null || string.IsNullOrEmpty(res.accessToken))
                    {
                        onError?.Invoke(new DeskillzError(ErrorCode.Unauthorized, "Launch exchange returned no access token"));
                        return;
                    }
                    var s = new LaunchSession
                    {
                        MatchId = string.IsNullOrEmpty(res.matchId) ? p.MatchId : res.matchId,
                        TournamentId = string.IsNullOrEmpty(res.tournamentId) ? p.TournamentId : res.tournamentId,
                        AccessToken = res.accessToken,
                        ScoreSecret = res.scoreSecret,
                        UserId = res.userId,
                        CreatedUtc = DateTime.UtcNow,
                        Params = p
                    };
                    Current = s;
                    network.SetAuthToken(res.accessToken);
                    DeskillzLogger.Info($"[Launch] session ready match={s.MatchId} tournament={s.TournamentId}");
                    onSuccess?.Invoke(s);
                },
                err => onError?.Invoke(err));
        }
    }
}

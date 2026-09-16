// =============================================================================
// Deskillz SDK for Unity -- launch URL parser  [P1-3.7.0]
// Accepts both forms the backend issues (tournament-checkin.service my-launch):
//   deepLink: deskillz://match?id=<matchId>&token=<launchToken>&tournament=<id>
//             &round=N&table=N[&gameplayMode=..][&gameRuleVariant=..]
//             [&mahjongVariant=..][&dur=<minutes>][&turnTimer=<seconds>]
//   webLink : <pwa>/index.html?matchId=<matchId>&token=..&tournamentId=<id>&...
// `token` is a SINGLE-USE launch token: exchange it via LaunchSession, never
// send it as a Bearer token.
// =============================================================================
using System;
using System.Collections.Generic;

namespace Deskillz
{
    public sealed class DeepLinkParams
    {
        public string MatchId;
        /// <summary>Single-use launch token (exchange it; not a JWT).</summary>
        public string PlayerToken;
        public string TournamentId;
        public int RoundNumber;
        public int TableNumber;
        public string GameplayMode;
        public string GameRuleVariant;
        public string MahjongVariant;
        public int DurationMinutes;
        public int TurnTimerSeconds;
        public MatchMode Mode = MatchMode.Asynchronous;
        public decimal EntryFee;
        public Currency Currency = Currency.Free;
        public int TimeLimitSeconds;
        public int Rounds = 1;
        public ScoreType ScoreType = ScoreType.HigherIsBetter;
        public Dictionary<string, string> CustomParams = new Dictionary<string, string>();

        public bool IsLaunch => !string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(PlayerToken);

        public static DeepLinkParams Parse(string url)
        {
            var p = new DeepLinkParams();
            if (string.IsNullOrEmpty(url)) return p;
            var q = url.IndexOf('?');
            if (q < 0) return p;
            var query = url.Substring(q + 1);
            var hash = query.IndexOf('#');
            if (hash >= 0) query = query.Substring(0, hash);
            foreach (var pair in query.Split('&'))
            {
                if (pair.Length == 0) continue;
                var eq = pair.IndexOf('=');
                var key = Uri.UnescapeDataString(eq < 0 ? pair : pair.Substring(0, eq));
                var val = eq < 0 ? "" : Uri.UnescapeDataString(pair.Substring(eq + 1).Replace('+', ' '));
                p.CustomParams[key] = val;
                switch (key)
                {
                    case "id":
                    case "matchId": p.MatchId = val; break;
                    case "token": p.PlayerToken = val; break;
                    case "tournament":
                    case "tournamentId": p.TournamentId = val; break;
                    case "round": int.TryParse(val, out p.RoundNumber); break;
                    case "table": int.TryParse(val, out p.TableNumber); break;
                    case "gameplayMode": p.GameplayMode = val; break;
                    case "gameRuleVariant": p.GameRuleVariant = val; break;
                    case "mahjongVariant": p.MahjongVariant = val; break;
                    case "dur": int.TryParse(val, out p.DurationMinutes); break;
                    case "turnTimer": int.TryParse(val, out p.TurnTimerSeconds); break;
                }
            }
            var mode = (p.GameplayMode ?? "").ToUpperInvariant();
            p.Mode = mode.Contains("SYNC") && !mode.Contains("ASYNC") || mode.Contains("REALTIME") || mode.Contains("DUEL")
                ? MatchMode.Synchronous
                : MatchMode.Asynchronous;
            p.TimeLimitSeconds = p.DurationMinutes > 0 ? p.DurationMinutes * 60 : 0;
            return p;
        }
    }
}

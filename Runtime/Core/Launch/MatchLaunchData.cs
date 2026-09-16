// =============================================================================
// Deskillz SDK for Unity -- Lobby launch model  [P1-3.7.0 fixC]
// The model DeepLinkHandler / DeskillzBridge / AuthSceneController were written
// against (it had been lost; only Deskillz.Rooms.MatchLaunchData survived, which
// is the private-room launch event and a different shape).
// =============================================================================
using System;
using System.Collections.Generic;

namespace Deskillz
{
    [Serializable]
    public class MatchLaunchData
    {
        public string RawDeepLink;
        public string MatchId;
        public string Token;
        public string TournamentId;
        public string GameId;
        public DateTime ReceivedAt;
        public MatchMode Mode = MatchMode.Asynchronous;
        public decimal EntryFee;
        public decimal PrizePool;
        public int TimeLimitSeconds;
        public int MaxPlayers;
        public Currency Currency = Currency.Free;
        public Dictionary<string, string> CustomParams = new Dictionary<string, string>();

        public bool IsRealtime => Mode == MatchMode.Synchronous || Mode == MatchMode.CustomStage;
        public bool IsValid => !string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(Token);
    }
}

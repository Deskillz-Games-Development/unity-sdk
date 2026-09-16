// =============================================================================
// Deskillz SDK for Unity -- GET /api/v1/tournaments/:id/my-launch models  [P1-3.7.0]
// live=true  : the table is running (LIVE payload)
// live=false : N228 lobby payload (phase CHECKIN | SEATED | ABORTED)
// =============================================================================
using System;
using System.Collections.Generic;

namespace Deskillz.Tournaments
{
    [Serializable]
    public sealed class LobbyRosterEntry
    {
        public string userId;
        public string username;
        public string avatarUrl;
        public string checkedInAt;
        public int seatNumber;
    }

    [Serializable]
    public sealed class LobbyMe
    {
        public string userId;
        public bool checkedIn;
        public int seatNumber;
    }

    [Serializable]
    public sealed class MyLaunch
    {
        public bool live;
        public string tournamentId;
        // ---- live payload ----
        public string matchId;
        public string deepLink;
        public string webLink;
        public string token;
        public int roundNumber;
        public int tableNumber;
        public string gameName;
        public string gameSlug;
        public string gameplayMode;
        public string gameRuleVariant;
        public string mahjongVariant;
        public int matchDuration;
        public int turnTimerSeconds;
        // ---- lobby payload (N228) ----
        public string phase;
        public string scheduledStart;
        public string checkinOpensAt;
        public string checkinClosesAt;
        public int tableSize;
        public string lobbyLink;
        public LobbyMe me;
        public List<LobbyRosterEntry> roster;
        public string abortReason;

        public bool IsSeated => !live && phase == "SEATED";
        public bool IsAborted => !live && phase == "ABORTED";
    }

    [Serializable]
    public sealed class ScoreSubmitAck
    {
        public bool success;
        public string message;
        public string status;
    }
}

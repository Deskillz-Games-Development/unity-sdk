// =============================================================================
// Deskillz SDK QuickStart  [P2-3.7.0]
// Drop this on an empty GameObject in your first scene. It shows the whole loop:
// initialize -> wait for a launch -> play -> submit the score -> end the match.
// Test without the platform: tick "Test Mode" on the DeskillzConfig asset.
// =============================================================================
using UnityEngine;
using Deskillz;

public class DeskillzQuickStart : MonoBehaviour
{
    [Tooltip("Points the player scored in this session. Hook your own scoring in.")]
    public int score;

    private void Start()
    {
        // 1. Initialize once. Reads Assets/Resources/DeskillzConfig.asset
        //    (Deskillz > Create Config Asset in the Editor menu).
        Deskillz.Deskillz.Initialize();

        // 2. When a player opens the game from a Deskillz tournament / quick play /
        //    private room link, the SDK exchanges the launch token and raises OnMatchReady.
        DeskillzEvents.OnMatchReady += match =>
        {
            Debug.Log($"[Deskillz] match ready: {match.MatchId} mode={match.Mode}");
            Deskillz.Deskillz.StartMatch();   // raises OnMatchStart; start your gameplay here
        };

        DeskillzEvents.OnScoreSubmitted += s => Debug.Log($"[Deskillz] score accepted: {s}");
        DeskillzEvents.OnMatchComplete += r => Debug.Log($"[Deskillz] match closed: {r.Outcome}");
        DeskillzEvents.OnError += e => Debug.LogWarning($"[Deskillz] {e.Message}");
    }

    /// <summary>Call this when your game is over.</summary>
    public void FinishGame()
    {
        Deskillz.Deskillz.SubmitScore(score);   // HMAC-signed POST /api/v1/tournaments/:id/score
        Deskillz.Deskillz.EndMatch();           // closes the local match; the platform settles the table
    }

    // Realtime (synchronous games): the SDK joins the match room for you.
    // Send state to the other seats and receive theirs:
    //   Deskillz.Deskillz.Multiplayer.SetLocalState(new { x = 1, y = 2 });
    //   DeskillzEvents.OnSocketMessage += (eventName, json) => { /* game:state, game:action ... */ };
}

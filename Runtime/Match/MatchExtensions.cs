// =============================================================================
// Deskillz SDK for Unity - Phase 2: Match System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;

namespace Deskillz.Match
{
    /// <summary>
    /// Extension methods and utilities for match system.
    /// </summary>
    public static class MatchExtensions
    {
        // =============================================================================
        // MATCH DATA EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get the opponent player in a 1v1 match.
        /// </summary>
        public static MatchPlayer GetOpponent(this MatchData match)
        {
            if (match?.Players == null) return null;
            return match.Players.Find(p => !p.IsLocalPlayer);
        }

        /// <summary>
        /// Get the local player from match data.
        /// </summary>
        public static MatchPlayer GetLocalPlayer(this MatchData match)
        {
            if (match?.Players == null) return null;
            return match.Players.Find(p => p.IsLocalPlayer);
        }

        /// <summary>
        /// Check if local player is winning.
        /// </summary>
        public static bool IsLocalPlayerWinning(this MatchData match)
        {
            var local = match?.GetLocalPlayer();
            var opponent = match?.GetOpponent();
            
            if (local == null || opponent == null) return false;

            var config = DeskillzConfig.Instance;
            if (config.ScoreType == ScoreType.HigherIsBetter)
            {
                return local.Score > opponent.Score;
            }
            return local.Score < opponent.Score;
        }

        /// <summary>
        /// Get score difference (positive if winning, negative if losing).
        /// </summary>
        public static int GetScoreDifference(this MatchData match)
        {
            var local = match?.GetLocalPlayer();
            var opponent = match?.GetOpponent();
            
            if (local == null || opponent == null) return 0;

            var diff = local.Score - opponent.Score;
            
            var config = DeskillzConfig.Instance;
            if (config.ScoreType == ScoreType.LowerIsBetter)
            {
                diff = -diff;
            }
            
            return diff;
        }

        /// <summary>
        /// Get formatted entry fee (e.g., "5.00 USDT").
        /// </summary>
        public static string GetFormattedEntryFee(this MatchData match)
        {
            if (match == null) return "Free";
            if (match.Currency == Currency.Free || match.EntryFee == 0)
                return "Free";
            
            return $"{match.EntryFee:F2} {match.Currency}";
        }

        /// <summary>
        /// Get formatted prize pool.
        /// </summary>
        public static string GetFormattedPrizePool(this MatchData match)
        {
            if (match == null || match.PrizePool == 0) return "N/A";
            return $"{match.PrizePool:F2} {match.Currency}";
        }

        /// <summary>
        /// Get formatted time remaining.
        /// </summary>
        public static string GetFormattedTimeRemaining(this MatchData match)
        {
            var remaining = match?.TimeRemaining ?? -1;
            if (remaining < 0) return "--:--";
            
            var minutes = Mathf.FloorToInt(remaining / 60f);
            var seconds = Mathf.FloorToInt(remaining % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        // =============================================================================
        // MATCH RESULT EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get a human-readable outcome string.
        /// </summary>
        public static string GetOutcomeText(this MatchResult result)
        {
            return result?.Outcome switch
            {
                MatchOutcome.Win => "Victory!",
                MatchOutcome.Loss => "Defeat",
                MatchOutcome.Tie => "Draw",
                MatchOutcome.Forfeit => "Forfeited",
                MatchOutcome.Cancelled => "Cancelled",
                MatchOutcome.Pending => "Awaiting Results...",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get formatted prize won.
        /// </summary>
        public static string GetFormattedPrize(this MatchResult result)
        {
            if (result == null || result.PrizeWon == 0) return "No Prize";
            return $"+{result.PrizeWon:F2} {result.Currency}";
        }

        /// <summary>
        /// Get formatted duration.
        /// </summary>
        public static string GetFormattedDuration(this MatchResult result)
        {
            if (result == null) return "0:00";
            var duration = TimeSpan.FromSeconds(result.DurationSeconds);
            return $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
        }

        /// <summary>
        /// Check if player received any prize.
        /// </summary>
        public static bool HasPrize(this MatchResult result)
        {
            return result?.PrizeWon > 0;
        }

        // =============================================================================
        // MATCH STATUS EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Check if status represents an active match.
        /// </summary>
        public static bool IsActive(this MatchStatus status)
        {
            return status == MatchStatus.InProgress || status == MatchStatus.Paused;
        }

        /// <summary>
        /// Check if status is a terminal state.
        /// </summary>
        public static bool IsTerminal(this MatchStatus status)
        {
            return status == MatchStatus.Completed ||
                   status == MatchStatus.Cancelled ||
                   status == MatchStatus.Forfeited;
        }

        /// <summary>
        /// Get a display-friendly status string.
        /// </summary>
        public static string ToDisplayString(this MatchStatus status)
        {
            return status switch
            {
                MatchStatus.None => "No Match",
                MatchStatus.Pending => "Ready to Start",
                MatchStatus.Countdown => "Starting...",
                MatchStatus.InProgress => "In Progress",
                MatchStatus.Paused => "Paused",
                MatchStatus.Processing => "Processing...",
                MatchStatus.Completed => "Completed",
                MatchStatus.Cancelled => "Cancelled",
                MatchStatus.Forfeited => "Forfeited",
                _ => "Unknown"
            };
        }

        // =============================================================================
        // MATCH MODE EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get a display-friendly mode string.
        /// </summary>
        public static string ToDisplayString(this MatchMode mode)
        {
            return mode switch
            {
                MatchMode.Asynchronous => "Turn-Based",
                MatchMode.Synchronous => "Real-Time",
                MatchMode.CustomStage => "Custom Match",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Check if mode requires real-time connection.
        /// </summary>
        public static bool RequiresRealtime(this MatchMode mode)
        {
            return mode == MatchMode.Synchronous || mode == MatchMode.CustomStage;
        }

        // =============================================================================
        // CURRENCY EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get currency symbol.
        /// </summary>
        public static string GetSymbol(this Currency currency)
        {
            return currency switch
            {
                Currency.BTC => "₿",
                Currency.ETH => "Ξ",
                Currency.SOL => "◎",
                Currency.XRP => "✕",
                Currency.BNB => "BNB",
                Currency.USDT => "$",
                Currency.USDC => "$",
                Currency.Free => "",
                _ => ""
            };
        }

        /// <summary>
        /// Get full currency name.
        /// </summary>
        public static string GetFullName(this Currency currency)
        {
            return currency switch
            {
                Currency.BTC => "Bitcoin",
                Currency.ETH => "Ethereum",
                Currency.SOL => "Solana",
                Currency.XRP => "Ripple",
                Currency.BNB => "Binance Coin",
                Currency.USDT => "Tether USD",
                Currency.USDC => "USD Coin",
                Currency.Free => "Free",
                _ => currency.ToString()
            };
        }

        /// <summary>
        /// Check if currency is a stablecoin.
        /// </summary>
        public static bool IsStablecoin(this Currency currency)
        {
            return currency == Currency.USDT || currency == Currency.USDC;
        }

        /// <summary>
        /// Get decimal precision for display.
        /// </summary>
        public static int GetDecimalPrecision(this Currency currency)
        {
            return currency switch
            {
                Currency.BTC => 8,
                Currency.ETH => 6,
                Currency.SOL => 4,
                _ => 2
            };
        }

        /// <summary>
        /// Format amount with appropriate precision.
        /// </summary>
        public static string FormatAmount(this Currency currency, decimal amount)
        {
            var precision = currency.GetDecimalPrecision();
            var format = $"F{precision}";
            return amount.ToString(format);
        }

        // =============================================================================
        // MATCH PLAYER EXTENSIONS
        // =============================================================================

        /// <summary>
        /// Get display name (username or "Unknown").
        /// </summary>
        public static string GetDisplayName(this MatchPlayer player)
        {
            if (player == null) return "Unknown";
            return string.IsNullOrEmpty(player.Username) ? "Player" : player.Username;
        }

        /// <summary>
        /// Get player status text.
        /// </summary>
        public static string GetStatusText(this MatchPlayer player)
        {
            if (player == null) return "Unknown";
            
            if (player.HasFinished) return "Finished";
            if (!player.IsConnected) return "Disconnected";
            return "Playing";
        }
    }

    // =============================================================================
    // MATCH HELPERS
    // =============================================================================

    /// <summary>
    /// Static helper methods for match operations.
    /// </summary>
    public static class MatchHelpers
    {
        /// <summary>
        /// Calculate prize distribution for multiple players.
        /// </summary>
        public static decimal[] CalculatePrizeDistribution(decimal prizePool, int playerCount, float platformFeePercent = 5f)
        {
            if (playerCount <= 0) return Array.Empty<decimal>();

            var netPool = prizePool * (1 - platformFeePercent / 100f);

            // Standard distribution based on placement
            // 1st: 60%, 2nd: 25%, 3rd: 10%, 4th: 5% (for 4+ players)
            var percentages = playerCount switch
            {
                1 => new[] { 1.0f },
                2 => new[] { 0.70f, 0.30f },
                3 => new[] { 0.60f, 0.25f, 0.15f },
                4 => new[] { 0.50f, 0.25f, 0.15f, 0.10f },
                _ => CalculateExtendedDistribution(playerCount)
            };

            var prizes = new decimal[playerCount];
            for (int i = 0; i < playerCount && i < percentages.Length; i++)
            {
                prizes[i] = Math.Round(netPool * (decimal)percentages[i], 2);
            }

            return prizes;
        }

        private static float[] CalculateExtendedDistribution(int count)
        {
            // For 5+ players, top 50% gets prizes
            var paidPositions = Math.Max(2, count / 2);
            var percentages = new float[count];
            
            // Exponential decay distribution
            float total = 0;
            for (int i = 0; i < paidPositions; i++)
            {
                percentages[i] = (float)Math.Pow(0.6, i);
                total += percentages[i];
            }
            
            // Normalize
            for (int i = 0; i < paidPositions; i++)
            {
                percentages[i] /= total;
            }
            
            return percentages;
        }

        /// <summary>
        /// Validate score against plausibility checks.
        /// </summary>
        public static bool IsScorePlausible(int score, float durationSeconds, int maxScorePerSecond = 1000)
        {
            if (score < 0) return false;
            if (durationSeconds <= 0) return true;

            // Check if score is achievable in the time given
            var maxPossible = (int)(durationSeconds * maxScorePerSecond);
            return score <= maxPossible;
        }

        /// <summary>
        /// Generate a match ID.
        /// </summary>
        public static string GenerateMatchId()
        {
            return $"match_{Guid.NewGuid():N}";
        }
    }
}

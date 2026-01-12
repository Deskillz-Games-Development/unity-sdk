// =============================================================================
// Deskillz SDK for Unity - Rake Calculator
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using UnityEngine;
using Deskillz.Host;

namespace Deskillz.Social
{
    /// <summary>
    /// Client-side utility for rake calculations and revenue estimations.
    /// Provides preview calculations for social game rooms.
    /// 
    /// Usage:
    /// <code>
    /// // Calculate rake for a pot
    /// var result = RakeCalculator.CalculateRake(100m, 5f, 50m);
    /// Debug.Log($"Rake: ${result.RakeAmount}, Net: ${result.NetWinnings}");
    /// 
    /// // Estimate host earnings
    /// var estimate = RakeCalculator.EstimateHostEarnings(1000m, HostTier.Gold);
    /// Debug.Log($"Host earnings: ${estimate}");
    /// </code>
    /// </summary>
    public static class RakeCalculator
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================

        /// <summary>Default rake percentage for social games</summary>
        public const float DEFAULT_RAKE_PERCENT = 5f;

        /// <summary>Default rake cap per pot</summary>
        public const decimal DEFAULT_RAKE_CAP = 50m;

        /// <summary>Minimum rake amount (to avoid dust)</summary>
        public const decimal MIN_RAKE_AMOUNT = 0.01m;

        /// <summary>Settlement threshold amount</summary>
        public const decimal SETTLEMENT_THRESHOLD = 50m;

        /// <summary>Settlement round interval</summary>
        public const int SETTLEMENT_ROUND_INTERVAL = 10;

        /// <summary>Settlement time interval in minutes</summary>
        public const int SETTLEMENT_TIME_INTERVAL = 30;

        // =====================================================================
        // RAKE CALCULATION
        // =====================================================================

        /// <summary>
        /// Calculate rake for a single pot.
        /// </summary>
        /// <param name="potAmount">Total pot amount</param>
        /// <param name="rakePercent">Rake percentage (default 5%)</param>
        /// <param name="rakeCap">Maximum rake per pot (default $50)</param>
        /// <returns>Rake calculation result</returns>
        public static RakeResult CalculateRake(
            decimal potAmount,
            float rakePercent = DEFAULT_RAKE_PERCENT,
            decimal rakeCap = DEFAULT_RAKE_CAP)
        {
            if (potAmount <= 0)
            {
                return new RakeResult
                {
                    PotAmount = 0,
                    RakeAmount = 0,
                    NetWinnings = 0,
                    RakePercent = rakePercent,
                    RakeCap = rakeCap,
                    WasCapped = false
                };
            }

            // Calculate raw rake
            decimal rawRake = potAmount * (decimal)(rakePercent / 100f);

            // Apply cap
            decimal actualRake = Math.Min(rawRake, rakeCap);

            // Ensure minimum rake
            if (actualRake < MIN_RAKE_AMOUNT && actualRake > 0)
            {
                actualRake = MIN_RAKE_AMOUNT;
            }

            // Round to 2 decimal places
            actualRake = Math.Round(actualRake, 2);

            return new RakeResult
            {
                PotAmount = potAmount,
                RakeAmount = actualRake,
                NetWinnings = potAmount - actualRake,
                RakePercent = rakePercent,
                RakeCap = rakeCap,
                WasCapped = rawRake > rakeCap,
                EffectiveRakePercent = potAmount > 0 ? (float)(actualRake / potAmount * 100) : 0
            };
        }

        /// <summary>
        /// Calculate rake for a round based on player scores.
        /// </summary>
        /// <param name="scores">Array of player scores (points)</param>
        /// <param name="pointValue">Value per point in USD</param>
        /// <param name="rakePercent">Rake percentage</param>
        /// <param name="rakeCap">Maximum rake per pot</param>
        /// <returns>Rake calculation result</returns>
        public static RakeResult CalculateRoundRake(
            int[] scores,
            decimal pointValue,
            float rakePercent = DEFAULT_RAKE_PERCENT,
            decimal rakeCap = DEFAULT_RAKE_CAP)
        {
            if (scores == null || scores.Length == 0)
            {
                return CalculateRake(0, rakePercent, rakeCap);
            }

            // Calculate pot from point transfers
            // In most social games, points are zero-sum (winner gains what losers lose)
            int totalPositive = 0;
            foreach (int score in scores)
            {
                if (score > 0)
                {
                    totalPositive += score;
                }
            }

            decimal potAmount = totalPositive * pointValue;
            return CalculateRake(potAmount, rakePercent, rakeCap);
        }

        // =====================================================================
        // REVENUE SPLIT CALCULATION
        // =====================================================================

        /// <summary>
        /// Calculate revenue split for a given rake amount.
        /// </summary>
        /// <param name="rakeAmount">Total rake to split</param>
        /// <param name="tier">Host tier</param>
        /// <param name="includeBonus">Whether to include host bonuses</param>
        /// <returns>Revenue split breakdown</returns>
        public static RevenueSplit CalculateRevenueSplit(
            decimal rakeAmount,
            HostTier tier,
            bool includeBonus = true)
        {
            // Get base share percentages for social games
            var tierConfig = GetSocialTierConfig(tier);

            float hostPercent = tierConfig.HostSharePercent;
            float platformPercent = tierConfig.PlatformSharePercent;
            float developerPercent = tierConfig.DeveloperSharePercent;

            // Apply bonuses if requested
            if (includeBonus && HostManager.HasProfile)
            {
                float bonus = HostManager.CalculateTotalBonus();
                hostPercent = Math.Min(hostPercent + bonus, HostManager.MAX_SOCIAL_SHARE);

                // Reduce platform share by bonus amount
                platformPercent = Math.Max(0, platformPercent - bonus);
            }

            // Calculate amounts
            decimal hostAmount = Math.Round(rakeAmount * (decimal)(hostPercent / 100f), 2);
            decimal developerAmount = Math.Round(rakeAmount * (decimal)(developerPercent / 100f), 2);
            decimal platformAmount = rakeAmount - hostAmount - developerAmount;

            // Ensure platform amount doesn't go negative
            if (platformAmount < 0)
            {
                platformAmount = 0;
                hostAmount = rakeAmount - developerAmount;
            }

            return new RevenueSplit
            {
                TotalRake = rakeAmount,
                HostAmount = hostAmount,
                HostPercent = hostPercent,
                PlatformAmount = platformAmount,
                PlatformPercent = platformPercent,
                DeveloperAmount = developerAmount,
                DeveloperPercent = developerPercent,
                BonusApplied = includeBonus ? HostManager.CalculateTotalBonus() : 0
            };
        }

        /// <summary>
        /// Estimate host earnings for a given rake amount.
        /// </summary>
        /// <param name="rakeAmount">Total rake amount</param>
        /// <param name="tier">Host tier (uses current profile tier if null)</param>
        /// <returns>Estimated host earnings</returns>
        public static decimal EstimateHostEarnings(decimal rakeAmount, HostTier? tier = null)
        {
            var actualTier = tier ?? HostManager.CurrentProfile?.SocialTier ?? HostTier.Bronze;
            var split = CalculateRevenueSplit(rakeAmount, actualTier, true);
            return split.HostAmount;
        }

        // =====================================================================
        // SESSION ESTIMATIONS
        // =====================================================================

        /// <summary>
        /// Estimate session earnings based on expected parameters.
        /// </summary>
        /// <param name="estimate">Session estimation parameters</param>
        /// <returns>Session earnings estimate</returns>
        public static SessionEarningsEstimate EstimateSessionEarnings(SessionEstimateParams estimate)
        {
            // Calculate average pot per round
            // In social games, average pot depends on point value and typical point swings
            decimal avgPotPerRound = estimate.PointValue * estimate.AvgPointSwingPerRound * estimate.PlayerCount / 2;

            // Calculate total pots
            decimal totalPots = avgPotPerRound * estimate.ExpectedRounds;

            // Calculate rake
            decimal rakePerRound = Math.Min(avgPotPerRound * (decimal)(estimate.RakePercent / 100f), estimate.RakeCap);
            decimal totalRake = rakePerRound * estimate.ExpectedRounds;

            // Get host earnings
            var split = CalculateRevenueSplit(totalRake, estimate.HostTier, true);

            return new SessionEarningsEstimate
            {
                ExpectedRounds = estimate.ExpectedRounds,
                ExpectedDurationMinutes = estimate.ExpectedRounds * estimate.AvgMinutesPerRound,
                TotalPotVolume = totalPots,
                TotalRake = totalRake,
                HostEarnings = split.HostAmount,
                PlatformEarnings = split.PlatformAmount,
                DeveloperEarnings = split.DeveloperAmount,
                HostSharePercent = split.HostPercent,
                EstimatedSettlements = (int)Math.Ceiling(totalRake / SETTLEMENT_THRESHOLD)
            };
        }

        /// <summary>
        /// Calculate expected settlements for a session.
        /// </summary>
        /// <param name="expectedRake">Expected total rake</param>
        /// <param name="expectedRounds">Expected number of rounds</param>
        /// <param name="expectedMinutes">Expected session duration in minutes</param>
        /// <returns>Expected number of settlements</returns>
        public static int EstimateSettlementCount(decimal expectedRake, int expectedRounds, int expectedMinutes)
        {
            int thresholdSettlements = (int)Math.Floor(expectedRake / SETTLEMENT_THRESHOLD);
            int roundSettlements = expectedRounds / SETTLEMENT_ROUND_INTERVAL;
            int timeSettlements = expectedMinutes / SETTLEMENT_TIME_INTERVAL;

            // Return max of different trigger types (they can overlap)
            return Math.Max(Math.Max(thresholdSettlements, roundSettlements), timeSettlements) + 1; // +1 for session end
        }

        // =====================================================================
        // TIER HELPERS
        // =====================================================================

        /// <summary>
        /// Get social tier configuration.
        /// </summary>
        /// <param name="tier">Host tier</param>
        /// <returns>Tier configuration</returns>
        public static HostTierConfig GetSocialTierConfig(HostTier tier)
        {
            foreach (var config in HostManager.SocialTiers)
            {
                if (config.Tier == tier)
                {
                    return config;
                }
            }
            return HostManager.SocialTiers[0]; // Default to Bronze
        }

        /// <summary>
        /// Calculate tier from monthly rake.
        /// </summary>
        /// <param name="monthlyRake">Monthly rake in USD</param>
        /// <returns>Calculated tier</returns>
        public static HostTier CalculateTierFromRake(decimal monthlyRake)
        {
            return HostManager.CalculateSocialTier(monthlyRake);
        }

        /// <summary>
        /// Get rake needed for next tier upgrade.
        /// </summary>
        /// <param name="currentTier">Current tier</param>
        /// <param name="currentMonthlyRake">Current monthly rake</param>
        /// <returns>Additional rake needed for upgrade (0 if at max tier)</returns>
        public static decimal GetRakeToNextTier(HostTier currentTier, decimal currentMonthlyRake)
        {
            if (currentTier == HostTier.Elite) return 0;

            int nextTierIndex = (int)currentTier + 1;
            if (nextTierIndex >= HostManager.SocialTiers.Length) return 0;

            var nextTier = HostManager.SocialTiers[nextTierIndex];
            decimal threshold = nextTier.MinThreshold;

            return Math.Max(0, threshold - currentMonthlyRake);
        }

        // =====================================================================
        // BUY-IN CALCULATIONS
        // =====================================================================

        /// <summary>
        /// Calculate buy-in options for a given point value.
        /// </summary>
        /// <param name="pointValue">Point value in USD</param>
        /// <returns>Buy-in options</returns>
        public static BuyInOptions CalculateBuyInOptions(decimal pointValue)
        {
            return new BuyInOptions
            {
                PointValue = pointValue,
                MinBuyIn = pointValue * 50,
                DefaultBuyIn = pointValue * 100,
                LowBalanceThreshold = pointValue * 20,
                SuggestedAmounts = new[]
                {
                    pointValue * 50,   // Minimum
                    pointValue * 100,  // Default
                    pointValue * 200,  // Double
                    pointValue * 500   // High roller
                }
            };
        }

        /// <summary>
        /// Validate a buy-in amount.
        /// </summary>
        /// <param name="amount">Amount to buy in</param>
        /// <param name="pointValue">Point value</param>
        /// <returns>Validation result</returns>
        public static BuyInValidation ValidateBuyIn(decimal amount, decimal pointValue)
        {
            var options = CalculateBuyInOptions(pointValue);

            if (amount < options.MinBuyIn)
            {
                return new BuyInValidation
                {
                    IsValid = false,
                    ErrorCode = SocialGameError.Codes.BelowMinBuyIn,
                    ErrorMessage = $"Minimum buy-in is ${options.MinBuyIn}",
                    MinBuyIn = options.MinBuyIn
                };
            }

            return new BuyInValidation
            {
                IsValid = true,
                Amount = amount,
                MinBuyIn = options.MinBuyIn
            };
        }
    }

    // =========================================================================
    // RESULT MODELS
    // =========================================================================

    /// <summary>
    /// Rake calculation result
    /// </summary>
    [Serializable]
    public class RakeResult
    {
        /// <summary>Original pot amount</summary>
        public decimal PotAmount;

        /// <summary>Rake taken</summary>
        public decimal RakeAmount;

        /// <summary>Net winnings after rake</summary>
        public decimal NetWinnings;

        /// <summary>Rake percentage used</summary>
        public float RakePercent;

        /// <summary>Rake cap applied</summary>
        public decimal RakeCap;

        /// <summary>Whether rake was capped</summary>
        public bool WasCapped;

        /// <summary>Effective rake percentage after cap</summary>
        public float EffectiveRakePercent;

        public override string ToString()
        {
            return $"RakeResult(Pot: ${PotAmount}, Rake: ${RakeAmount} ({EffectiveRakePercent:F1}%), Net: ${NetWinnings})";
        }
    }

    /// <summary>
    /// Revenue split breakdown
    /// </summary>
    [Serializable]
    public class RevenueSplit
    {
        /// <summary>Total rake being split</summary>
        public decimal TotalRake;

        /// <summary>Host earnings amount</summary>
        public decimal HostAmount;

        /// <summary>Host percentage</summary>
        public float HostPercent;

        /// <summary>Platform earnings amount</summary>
        public decimal PlatformAmount;

        /// <summary>Platform percentage</summary>
        public float PlatformPercent;

        /// <summary>Developer earnings amount</summary>
        public decimal DeveloperAmount;

        /// <summary>Developer percentage</summary>
        public float DeveloperPercent;

        /// <summary>Bonus percentage applied</summary>
        public float BonusApplied;

        public override string ToString()
        {
            return $"RevenueSplit(Host: ${HostAmount} ({HostPercent}%), Platform: ${PlatformAmount}, Dev: ${DeveloperAmount})";
        }
    }

    /// <summary>
    /// Parameters for session earnings estimation
    /// </summary>
    [Serializable]
    public class SessionEstimateParams
    {
        /// <summary>Point value in USD</summary>
        public decimal PointValue = 1.00m;

        /// <summary>Number of players</summary>
        public int PlayerCount = 4;

        /// <summary>Expected number of rounds</summary>
        public int ExpectedRounds = 10;

        /// <summary>Average minutes per round</summary>
        public float AvgMinutesPerRound = 5f;

        /// <summary>Average point swing per round (winner's score)</summary>
        public int AvgPointSwingPerRound = 10;

        /// <summary>Rake percentage</summary>
        public float RakePercent = RakeCalculator.DEFAULT_RAKE_PERCENT;

        /// <summary>Rake cap per pot</summary>
        public decimal RakeCap = RakeCalculator.DEFAULT_RAKE_CAP;

        /// <summary>Host tier for calculations</summary>
        public HostTier HostTier = HostTier.Bronze;
    }

    /// <summary>
    /// Session earnings estimate result
    /// </summary>
    [Serializable]
    public class SessionEarningsEstimate
    {
        /// <summary>Expected number of rounds</summary>
        public int ExpectedRounds;

        /// <summary>Expected duration in minutes</summary>
        public float ExpectedDurationMinutes;

        /// <summary>Expected total pot volume</summary>
        public decimal TotalPotVolume;

        /// <summary>Expected total rake</summary>
        public decimal TotalRake;

        /// <summary>Expected host earnings</summary>
        public decimal HostEarnings;

        /// <summary>Expected platform earnings</summary>
        public decimal PlatformEarnings;

        /// <summary>Expected developer earnings</summary>
        public decimal DeveloperEarnings;

        /// <summary>Host share percentage</summary>
        public float HostSharePercent;

        /// <summary>Expected number of settlements</summary>
        public int EstimatedSettlements;

        public override string ToString()
        {
            return $"SessionEstimate({ExpectedRounds} rounds, Host: ${HostEarnings}, Rake: ${TotalRake})";
        }
    }

    /// <summary>
    /// Buy-in options
    /// </summary>
    [Serializable]
    public class BuyInOptions
    {
        /// <summary>Point value</summary>
        public decimal PointValue;

        /// <summary>Minimum buy-in (50x)</summary>
        public decimal MinBuyIn;

        /// <summary>Default buy-in (100x)</summary>
        public decimal DefaultBuyIn;

        /// <summary>Low balance warning threshold (20x)</summary>
        public decimal LowBalanceThreshold;

        /// <summary>Suggested buy-in amounts</summary>
        public decimal[] SuggestedAmounts;
    }

    /// <summary>
    /// Buy-in validation result
    /// </summary>
    [Serializable]
    public class BuyInValidation
    {
        /// <summary>Whether buy-in is valid</summary>
        public bool IsValid;

        /// <summary>Validated amount</summary>
        public decimal Amount;

        /// <summary>Minimum buy-in</summary>
        public decimal MinBuyIn;

        /// <summary>Error code if invalid</summary>
        public string ErrorCode;

        /// <summary>Error message if invalid</summary>
        public string ErrorMessage;
    }
}
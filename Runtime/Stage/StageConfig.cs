// =============================================================================
// Deskillz SDK for Unity - Phase 6: Custom Stage System
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Stage
{
    /// <summary>
    /// Configuration for a custom stage.
    /// </summary>
    [Serializable]
    public class StageConfig
    {
        // =============================================================================
        // BASIC INFO
        // =============================================================================

        /// <summary>Stage display name.</summary>
        public string Name;

        /// <summary>Stage description.</summary>
        public string Description;

        /// <summary>Game ID to play.</summary>
        public string GameId;

        /// <summary>Game display name.</summary>
        public string GameName;

        // =============================================================================
        // PLAYER SETTINGS
        // =============================================================================

        /// <summary>Minimum players required to start.</summary>
        public int MinPlayers = 2;

        /// <summary>Maximum players allowed.</summary>
        public int MaxPlayers = 8;

        /// <summary>Allow spectators.</summary>
        public bool AllowSpectators = false;

        /// <summary>Maximum spectators.</summary>
        public int MaxSpectators = 10;

        // =============================================================================
        // MATCH SETTINGS
        // =============================================================================

        /// <summary>Entry fee amount (0 for free).</summary>
        public decimal EntryFee = 0;

        /// <summary>Entry fee currency.</summary>
        public string Currency = "USDT";

        /// <summary>Game mode (sync or async).</summary>
        public StageGameMode GameMode = StageGameMode.Synchronous;

        /// <summary>Number of rounds.</summary>
        public int Rounds = 1;

        /// <summary>Time limit per round (seconds, 0 for no limit).</summary>
        public int TimeLimitSeconds = 300;

        /// <summary>Score type.</summary>
        public ScoreType ScoreType = ScoreType.HigherIsBetter;

        // =============================================================================
        // VISIBILITY SETTINGS
        // =============================================================================

        /// <summary>Stage visibility.</summary>
        public StageVisibility Visibility = StageVisibility.Private;

        /// <summary>Require password to join.</summary>
        public bool RequirePassword = false;

        /// <summary>Password for private stages.</summary>
        public string Password;

        /// <summary>Allow join in progress.</summary>
        public bool AllowJoinInProgress = false;

        // =============================================================================
        // SKILL SETTINGS
        // =============================================================================

        /// <summary>Enable skill-based restrictions.</summary>
        public bool SkillRestricted = false;

        /// <summary>Minimum ELO rating to join.</summary>
        public int MinElo = 0;

        /// <summary>Maximum ELO rating to join.</summary>
        public int MaxElo = 9999;

        // =============================================================================
        // PRIZE SETTINGS
        // =============================================================================

        /// <summary>Prize distribution type.</summary>
        public PrizeDistribution PrizeDistribution = PrizeDistribution.WinnerTakesAll;

        /// <summary>Custom prize percentages (for custom distribution).</summary>
        public float[] CustomPrizePercentages;

        /// <summary>Platform fee percentage.</summary>
        public float PlatformFeePercent = 5f;

        // =============================================================================
        // ADVANCED SETTINGS
        // =============================================================================

        /// <summary>Auto-start when minimum players ready.</summary>
        public bool AutoStart = true;

        /// <summary>Auto-start countdown (seconds).</summary>
        public int AutoStartCountdown = 10;

        /// <summary>Stage timeout (minutes).</summary>
        public int TimeoutMinutes = 30;

        /// <summary>Allow rematch voting.</summary>
        public bool AllowRematch = true;

        /// <summary>Custom rules as JSON.</summary>
        public string CustomRulesJson;

        // =============================================================================
        // FACTORY METHODS
        // =============================================================================

        /// <summary>
        /// Create a default stage configuration.
        /// </summary>
        public static StageConfig CreateDefault(string gameName)
        {
            return new StageConfig
            {
                Name = $"{gameName} Stage",
                GameName = gameName,
                MinPlayers = 2,
                MaxPlayers = 8,
                GameMode = StageGameMode.Synchronous,
                Visibility = StageVisibility.Private,
                EntryFee = 0,
                Currency = "USDT"
            };
        }

        /// <summary>
        /// Create a quick play configuration.
        /// </summary>
        public static StageConfig CreateQuickPlay(string gameName, int maxPlayers = 4)
        {
            return new StageConfig
            {
                Name = $"Quick {gameName}",
                GameName = gameName,
                MinPlayers = 2,
                MaxPlayers = maxPlayers,
                GameMode = StageGameMode.Synchronous,
                Visibility = StageVisibility.Public,
                AutoStart = true,
                AutoStartCountdown = 5,
                EntryFee = 0
            };
        }

        /// <summary>
        /// Create a tournament configuration.
        /// </summary>
        public static StageConfig CreateTournament(string gameName, decimal entryFee, string currency, int maxPlayers = 16)
        {
            return new StageConfig
            {
                Name = $"{gameName} Tournament",
                GameName = gameName,
                MinPlayers = 4,
                MaxPlayers = maxPlayers,
                GameMode = StageGameMode.Synchronous,
                Visibility = StageVisibility.Public,
                EntryFee = entryFee,
                Currency = currency,
                PrizeDistribution = PrizeDistribution.TopThree,
                Rounds = 3
            };
        }

        /// <summary>
        /// Create a private match configuration.
        /// </summary>
        public static StageConfig CreatePrivateMatch(string gameName, string password = null)
        {
            return new StageConfig
            {
                Name = $"Private {gameName}",
                GameName = gameName,
                MinPlayers = 2,
                MaxPlayers = 2,
                Visibility = StageVisibility.Private,
                RequirePassword = !string.IsNullOrEmpty(password),
                Password = password,
                AutoStart = false
            };
        }

        // =============================================================================
        // VALIDATION
        // =============================================================================

        /// <summary>
        /// Validate the configuration.
        /// </summary>
        /// <returns>Error message if invalid, null if valid.</returns>
        public string Validate()
        {
            if (string.IsNullOrEmpty(Name))
                return "Stage name is required";

            if (Name.Length > 50)
                return "Stage name must be 50 characters or less";

            if (MinPlayers < 1)
                return "Minimum players must be at least 1";

            if (MaxPlayers < MinPlayers)
                return "Maximum players must be >= minimum players";

            if (MaxPlayers > 100)
                return "Maximum players cannot exceed 100";

            if (EntryFee < 0)
                return "Entry fee cannot be negative";

            if (EntryFee > 0 && string.IsNullOrEmpty(Currency))
                return "Currency is required when entry fee is set";

            if (Rounds < 1)
                return "Rounds must be at least 1";

            if (Rounds > 10)
                return "Rounds cannot exceed 10";

            if (TimeLimitSeconds < 0)
                return "Time limit cannot be negative";

            if (TimeoutMinutes < 1)
                return "Timeout must be at least 1 minute";

            if (RequirePassword && string.IsNullOrEmpty(Password))
                return "Password is required when password protection is enabled";

            if (SkillRestricted && MinElo > MaxElo)
                return "Minimum ELO cannot exceed maximum ELO";

            if (PlatformFeePercent < 0 || PlatformFeePercent > 50)
                return "Platform fee must be between 0% and 50%";

            if (CustomPrizePercentages != null)
            {
                float total = 0;
                foreach (var p in CustomPrizePercentages)
                {
                    if (p < 0) return "Prize percentages cannot be negative";
                    total += p;
                }
                if (Math.Abs(total - 100f) > 0.01f)
                    return "Prize percentages must total 100%";
            }

            return null;
        }

        // =============================================================================
        // PRIZE CALCULATION
        // =============================================================================

        /// <summary>
        /// Calculate total prize pool.
        /// </summary>
        public decimal GetTotalPrizePool(int playerCount)
        {
            var gross = EntryFee * playerCount;
            var fee = gross * (decimal)(PlatformFeePercent / 100f);
            return gross - fee;
        }

        /// <summary>
        /// Get prize breakdown for each position.
        /// </summary>
        public Dictionary<int, decimal> GetPrizeBreakdown(int playerCount)
        {
            var prizes = new Dictionary<int, decimal>();
            var pool = GetTotalPrizePool(playerCount);

            if (pool <= 0) return prizes;

            float[] percentages = GetPrizePercentages(playerCount);

            for (int i = 0; i < percentages.Length; i++)
            {
                var amount = pool * (decimal)(percentages[i] / 100f);
                if (amount > 0)
                {
                    prizes[i + 1] = Math.Round(amount, 2);
                }
            }

            return prizes;
        }

        private float[] GetPrizePercentages(int playerCount)
        {
            if (CustomPrizePercentages != null && CustomPrizePercentages.Length > 0)
            {
                return CustomPrizePercentages;
            }

            return PrizeDistribution switch
            {
                PrizeDistribution.WinnerTakesAll => new float[] { 100f },
                PrizeDistribution.TopTwo => new float[] { 70f, 30f },
                PrizeDistribution.TopThree => new float[] { 50f, 30f, 20f },
                PrizeDistribution.TopFive => new float[] { 40f, 25f, 18f, 10f, 7f },
                PrizeDistribution.TopTen => new float[] { 30f, 20f, 15f, 10f, 7f, 5f, 4f, 4f, 3f, 2f },
                PrizeDistribution.EvenSplit => CreateEvenSplit(Math.Min(playerCount, MaxPlayers)),
                _ => new float[] { 100f }
            };
        }

        private float[] CreateEvenSplit(int count)
        {
            var percentages = new float[count];
            var each = 100f / count;
            for (int i = 0; i < count; i++)
            {
                percentages[i] = each;
            }
            return percentages;
        }

        // =============================================================================
        // SERIALIZATION
        // =============================================================================

        /// <summary>
        /// Convert to JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Create from JSON string.
        /// </summary>
        public static StageConfig FromJson(string json)
        {
            return JsonUtility.FromJson<StageConfig>(json);
        }

        /// <summary>
        /// Create a copy.
        /// </summary>
        public StageConfig Clone()
        {
            return FromJson(ToJson());
        }
    }

    // =============================================================================
    // ENUMS
    // =============================================================================

    /// <summary>
    /// Stage visibility options.
    /// </summary>
    public enum StageVisibility
    {
        /// <summary>Only visible via invite code.</summary>
        Private,

        /// <summary>Visible in public stage list.</summary>
        Public,

        /// <summary>Visible to friends only.</summary>
        FriendsOnly,

        /// <summary>Unlisted but joinable with link.</summary>
        Unlisted
    }

    /// <summary>
    /// Stage game mode.
    /// </summary>
    public enum StageGameMode
    {
        /// <summary>Real-time synchronous play.</summary>
        Synchronous,

        /// <summary>Turn-based asynchronous play.</summary>
        Asynchronous,

        /// <summary>Players compete against each other's recorded sessions.</summary>
        GhostRace
    }

    /// <summary>
    /// Prize distribution types.
    /// </summary>
    public enum PrizeDistribution
    {
        /// <summary>Winner gets 100%.</summary>
        WinnerTakesAll,

        /// <summary>Top 2 split (70/30).</summary>
        TopTwo,

        /// <summary>Top 3 split (50/30/20).</summary>
        TopThree,

        /// <summary>Top 5 split.</summary>
        TopFive,

        /// <summary>Top 10 split.</summary>
        TopTen,

        /// <summary>Even split among all players.</summary>
        EvenSplit,

        /// <summary>Custom percentages.</summary>
        Custom
    }

    // =============================================================================
    // STAGE RULES
    // =============================================================================

    /// <summary>
    /// Custom rules that can be applied to a stage.
    /// </summary>
    [Serializable]
    public class StageRules
    {
        /// <summary>Enable power-ups.</summary>
        public bool PowerUpsEnabled = true;

        /// <summary>Available power-up IDs.</summary>
        public string[] AvailablePowerUps;

        /// <summary>Enable handicap system.</summary>
        public bool HandicapEnabled = false;

        /// <summary>Friendly fire enabled.</summary>
        public bool FriendlyFire = false;

        /// <summary>Respawn enabled.</summary>
        public bool RespawnEnabled = true;

        /// <summary>Respawn delay (seconds).</summary>
        public float RespawnDelay = 3f;

        /// <summary>Starting lives (0 for unlimited).</summary>
        public int StartingLives = 0;

        /// <summary>Score to win (0 for time-based).</summary>
        public int ScoreToWin = 0;

        /// <summary>Custom game parameters.</summary>
        public Dictionary<string, string> CustomParams = new Dictionary<string, string>();

        /// <summary>
        /// Get a custom parameter value.
        /// </summary>
        public T GetParam<T>(string key, T defaultValue = default)
        {
            if (CustomParams.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set a custom parameter value.
        /// </summary>
        public void SetParam<T>(string key, T value)
        {
            CustomParams[key] = value?.ToString() ?? "";
        }
    }

    // =============================================================================
    // STAGE PRESETS
    // =============================================================================

    /// <summary>
    /// Pre-configured stage presets.
    /// </summary>
    public static class StagePresets
    {
        /// <summary>
        /// Casual free-to-play preset.
        /// </summary>
        public static StageConfig Casual(string gameName) => new StageConfig
        {
            Name = $"Casual {gameName}",
            GameName = gameName,
            MinPlayers = 2,
            MaxPlayers = 8,
            EntryFee = 0,
            Visibility = StageVisibility.Public,
            AutoStart = true,
            AutoStartCountdown = 5
        };

        /// <summary>
        /// Competitive ranked preset.
        /// </summary>
        public static StageConfig Competitive(string gameName, decimal entryFee, string currency) => new StageConfig
        {
            Name = $"Competitive {gameName}",
            GameName = gameName,
            MinPlayers = 2,
            MaxPlayers = 4,
            EntryFee = entryFee,
            Currency = currency,
            Visibility = StageVisibility.Public,
            SkillRestricted = true,
            PrizeDistribution = PrizeDistribution.WinnerTakesAll
        };

        /// <summary>
        /// Party mode preset (friends).
        /// </summary>
        public static StageConfig Party(string gameName) => new StageConfig
        {
            Name = $"{gameName} Party",
            GameName = gameName,
            MinPlayers = 2,
            MaxPlayers = 16,
            EntryFee = 0,
            Visibility = StageVisibility.FriendsOnly,
            AllowSpectators = true,
            MaxSpectators = 20,
            AutoStart = false
        };

        /// <summary>
        /// High stakes preset.
        /// </summary>
        public static StageConfig HighStakes(string gameName, decimal entryFee, string currency) => new StageConfig
        {
            Name = $"High Stakes {gameName}",
            GameName = gameName,
            MinPlayers = 2,
            MaxPlayers = 2,
            EntryFee = entryFee,
            Currency = currency,
            Visibility = StageVisibility.Private,
            RequirePassword = true,
            PrizeDistribution = PrizeDistribution.WinnerTakesAll,
            SkillRestricted = true,
            MinElo = 1500
        };
    }
}

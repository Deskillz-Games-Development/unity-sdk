// =============================================================================
// Deskillz SDK for Unity - Host System Events
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;

namespace Deskillz.Host
{
    /// <summary>
    /// Static event hub for host system events.
    /// Subscribe to these events to receive host-related notifications.
    /// </summary>
    public static class HostEvents
    {
        // =====================================================================
        // PROFILE EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when host profile is loaded or updated
        /// </summary>
        public static event Action<HostProfile> OnProfileLoaded;

        /// <summary>
        /// Fired when host profile update fails
        /// </summary>
        public static event Action<HostError> OnProfileError;

        /// <summary>
        /// Fired when age verification status changes
        /// </summary>
        public static event Action<bool> OnVerificationStatusChanged;

        // =====================================================================
        // TIER EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when esports tier changes
        /// </summary>
        public static event Action<HostTier, HostTier> OnEsportsTierChanged;

        /// <summary>
        /// Fired when social tier changes
        /// </summary>
        public static event Action<HostTier, HostTier> OnSocialTierChanged;

        /// <summary>
        /// Fired when tier progress updates
        /// </summary>
        public static event Action<float> OnTierProgressUpdated;

        // =====================================================================
        // LEVEL EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when host level increases
        /// </summary>
        public static event Action<int, int> OnLevelUp;

        /// <summary>
        /// Fired when level progress updates
        /// </summary>
        public static event Action<float> OnLevelProgressUpdated;

        // =====================================================================
        // BADGE EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when a new badge is earned
        /// </summary>
        public static event Action<HostBadge> OnBadgeEarned;

        /// <summary>
        /// Fired when a badge expires (performance badges)
        /// </summary>
        public static event Action<HostBadge> OnBadgeExpired;

        /// <summary>
        /// Fired when badges list is updated
        /// </summary>
        public static event Action<System.Collections.Generic.List<HostBadge>> OnBadgesUpdated;

        // =====================================================================
        // EARNINGS EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when earnings data is loaded
        /// </summary>
        public static event Action<HostEarnings> OnEarningsLoaded;

        /// <summary>
        /// Fired when new earnings are received
        /// </summary>
        public static event Action<decimal, string> OnEarningsReceived;

        /// <summary>
        /// Fired when available balance changes
        /// </summary>
        public static event Action<decimal> OnBalanceChanged;

        /// <summary>
        /// Fired when withdrawal is initiated
        /// </summary>
        public static event Action<decimal, string> OnWithdrawalInitiated;

        /// <summary>
        /// Fired when withdrawal completes
        /// </summary>
        public static event Action<decimal, string> OnWithdrawalCompleted;

        /// <summary>
        /// Fired when withdrawal fails
        /// </summary>
        public static event Action<HostError> OnWithdrawalFailed;

        // =====================================================================
        // STREAK EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when hosting streak increases
        /// </summary>
        public static event Action<int> OnStreakIncreased;

        /// <summary>
        /// Fired when hosting streak is broken
        /// </summary>
        public static event Action OnStreakBroken;

        /// <summary>
        /// Fired when streak bonus is applied
        /// </summary>
        public static event Action<float> OnStreakBonusApplied;

        // =====================================================================
        // ROOM EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when active rooms list is updated
        /// </summary>
        public static event Action<System.Collections.Generic.List<ActiveRoomSummary>> OnActiveRoomsUpdated;

        /// <summary>
        /// Fired when a room session completes
        /// </summary>
        public static event Action<string, decimal> OnRoomSessionCompleted;

        /// <summary>
        /// Fired when settlement occurs in a room
        /// </summary>
        public static event Action<string, decimal, SettlementTrigger> OnSettlementReceived;

        // =====================================================================
        // NOTIFICATION EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when a host notification is received
        /// </summary>
        public static event Action<HostNotification> OnNotificationReceived;

        /// <summary>
        /// Fired when notification settings are updated
        /// </summary>
        public static event Action<HostNotificationSettings> OnNotificationSettingsUpdated;

        // =====================================================================
        // STATS EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when host stats are loaded
        /// </summary>
        public static event Action<HostStats> OnStatsLoaded;

        // =====================================================================
        // INTERNAL INVOCATION METHODS
        // =====================================================================

        internal static void InvokeProfileLoaded(HostProfile profile)
        {
            OnProfileLoaded?.Invoke(profile);
        }

        internal static void InvokeProfileError(HostError error)
        {
            OnProfileError?.Invoke(error);
        }

        internal static void InvokeVerificationStatusChanged(bool isVerified)
        {
            OnVerificationStatusChanged?.Invoke(isVerified);
        }

        internal static void InvokeEsportsTierChanged(HostTier oldTier, HostTier newTier)
        {
            OnEsportsTierChanged?.Invoke(oldTier, newTier);
        }

        internal static void InvokeSocialTierChanged(HostTier oldTier, HostTier newTier)
        {
            OnSocialTierChanged?.Invoke(oldTier, newTier);
        }

        internal static void InvokeTierProgressUpdated(float progress)
        {
            OnTierProgressUpdated?.Invoke(progress);
        }

        internal static void InvokeLevelUp(int oldLevel, int newLevel)
        {
            OnLevelUp?.Invoke(oldLevel, newLevel);
        }

        internal static void InvokeLevelProgressUpdated(float progress)
        {
            OnLevelProgressUpdated?.Invoke(progress);
        }

        internal static void InvokeBadgeEarned(HostBadge badge)
        {
            OnBadgeEarned?.Invoke(badge);
        }

        internal static void InvokeBadgeExpired(HostBadge badge)
        {
            OnBadgeExpired?.Invoke(badge);
        }

        internal static void InvokeBadgesUpdated(System.Collections.Generic.List<HostBadge> badges)
        {
            OnBadgesUpdated?.Invoke(badges);
        }

        internal static void InvokeEarningsLoaded(HostEarnings earnings)
        {
            OnEarningsLoaded?.Invoke(earnings);
        }

        internal static void InvokeEarningsReceived(decimal amount, string roomId)
        {
            OnEarningsReceived?.Invoke(amount, roomId);
        }

        internal static void InvokeBalanceChanged(decimal newBalance)
        {
            OnBalanceChanged?.Invoke(newBalance);
        }

        internal static void InvokeWithdrawalInitiated(decimal amount, string currency)
        {
            OnWithdrawalInitiated?.Invoke(amount, currency);
        }

        internal static void InvokeWithdrawalCompleted(decimal amount, string txHash)
        {
            OnWithdrawalCompleted?.Invoke(amount, txHash);
        }

        internal static void InvokeWithdrawalFailed(HostError error)
        {
            OnWithdrawalFailed?.Invoke(error);
        }

        internal static void InvokeStreakIncreased(int newStreak)
        {
            OnStreakIncreased?.Invoke(newStreak);
        }

        internal static void InvokeStreakBroken()
        {
            OnStreakBroken?.Invoke();
        }

        internal static void InvokeStreakBonusApplied(float bonusPercent)
        {
            OnStreakBonusApplied?.Invoke(bonusPercent);
        }

        internal static void InvokeActiveRoomsUpdated(System.Collections.Generic.List<ActiveRoomSummary> rooms)
        {
            OnActiveRoomsUpdated?.Invoke(rooms);
        }

        internal static void InvokeRoomSessionCompleted(string roomId, decimal earnings)
        {
            OnRoomSessionCompleted?.Invoke(roomId, earnings);
        }

        internal static void InvokeSettlementReceived(string roomId, decimal amount, SettlementTrigger trigger)
        {
            OnSettlementReceived?.Invoke(roomId, amount, trigger);
        }

        internal static void InvokeNotificationReceived(HostNotification notification)
        {
            OnNotificationReceived?.Invoke(notification);
        }

        internal static void InvokeNotificationSettingsUpdated(HostNotificationSettings settings)
        {
            OnNotificationSettingsUpdated?.Invoke(settings);
        }

        internal static void InvokeStatsLoaded(HostStats stats)
        {
            OnStatsLoaded?.Invoke(stats);
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        /// <summary>
        /// Clear all event subscriptions. Called during SDK shutdown.
        /// </summary>
        internal static void ClearAllSubscriptions()
        {
            OnProfileLoaded = null;
            OnProfileError = null;
            OnVerificationStatusChanged = null;
            OnEsportsTierChanged = null;
            OnSocialTierChanged = null;
            OnTierProgressUpdated = null;
            OnLevelUp = null;
            OnLevelProgressUpdated = null;
            OnBadgeEarned = null;
            OnBadgeExpired = null;
            OnBadgesUpdated = null;
            OnEarningsLoaded = null;
            OnEarningsReceived = null;
            OnBalanceChanged = null;
            OnWithdrawalInitiated = null;
            OnWithdrawalCompleted = null;
            OnWithdrawalFailed = null;
            OnStreakIncreased = null;
            OnStreakBroken = null;
            OnStreakBonusApplied = null;
            OnActiveRoomsUpdated = null;
            OnRoomSessionCompleted = null;
            OnSettlementReceived = null;
            OnNotificationReceived = null;
            OnNotificationSettingsUpdated = null;
            OnStatsLoaded = null;
        }
    }
}
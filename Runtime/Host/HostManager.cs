// =============================================================================
// Deskillz SDK for Unity - Host Manager
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deskillz.Host
{
    /// <summary>
    /// Main API for host system management in the Deskillz SDK.
    /// Allows players to become hosts, track earnings, manage tiers, and view badges.
    /// 
    /// Usage:
    /// <code>
    /// // Get host profile
    /// HostManager.GetProfile(
    ///     profile => Debug.Log($"Host level: {profile.Level}"),
    ///     error => Debug.LogError(error.Message)
    /// );
    /// 
    /// // Subscribe to events
    /// HostEvents.OnTierChanged += (oldTier, newTier) => 
    ///     Debug.Log($"Tier upgraded: {oldTier} -> {newTier}");
    /// </code>
    /// </summary>
    public static class HostManager
    {
        // =====================================================================
        // PROPERTIES
        // =====================================================================

        /// <summary>Current host profile (cached)</summary>
        public static HostProfile CurrentProfile { get; private set; }

        /// <summary>Current host earnings (cached)</summary>
        public static HostEarnings CurrentEarnings { get; private set; }

        /// <summary>Whether the current user has a host profile</summary>
        public static bool HasProfile => CurrentProfile != null;

        /// <summary>Whether the host is age verified</summary>
        public static bool IsVerified => CurrentProfile?.IsVerified ?? false;

        /// <summary>Active rooms currently being hosted</summary>
        public static List<ActiveRoomSummary> ActiveRooms { get; private set; } = new List<ActiveRoomSummary>();

        /// <summary>Whether the host manager is initialized</summary>
        public static bool IsInitialized { get; private set; }

        // =====================================================================
        // TIER CONFIGURATION (STATIC)
        // =====================================================================

        /// <summary>
        /// Esports tier configuration (based on player count per room)
        /// </summary>
        public static readonly HostTierConfig[] EsportsTiers = new[]
        {
            new HostTierConfig { Tier = HostTier.Bronze, MinThreshold = 2, MaxThreshold = 4, HostSharePercent = 15f, PlatformSharePercent = 25f, DeveloperSharePercent = 60f },
            new HostTierConfig { Tier = HostTier.Silver, MinThreshold = 5, MaxThreshold = 10, HostSharePercent = 18f, PlatformSharePercent = 22f, DeveloperSharePercent = 60f },
            new HostTierConfig { Tier = HostTier.Gold, MinThreshold = 11, MaxThreshold = 25, HostSharePercent = 20f, PlatformSharePercent = 20f, DeveloperSharePercent = 60f },
            new HostTierConfig { Tier = HostTier.Platinum, MinThreshold = 26, MaxThreshold = 50, HostSharePercent = 23f, PlatformSharePercent = 17f, DeveloperSharePercent = 60f },
            new HostTierConfig { Tier = HostTier.Diamond, MinThreshold = 51, MaxThreshold = 100, HostSharePercent = 25f, PlatformSharePercent = 15f, DeveloperSharePercent = 60f },
            new HostTierConfig { Tier = HostTier.Elite, MinThreshold = 101, MaxThreshold = int.MaxValue, HostSharePercent = 28f, PlatformSharePercent = 12f, DeveloperSharePercent = 60f }
        };

        /// <summary>
        /// Social tier configuration (based on monthly rake generated)
        /// </summary>
        public static readonly HostTierConfig[] SocialTiers = new[]
        {
            new HostTierConfig { Tier = HostTier.Bronze, MinThreshold = 0, MaxThreshold = 100, HostSharePercent = 15f, PlatformSharePercent = 35f, DeveloperSharePercent = 50f },
            new HostTierConfig { Tier = HostTier.Silver, MinThreshold = 101, MaxThreshold = 500, HostSharePercent = 18f, PlatformSharePercent = 32f, DeveloperSharePercent = 50f },
            new HostTierConfig { Tier = HostTier.Gold, MinThreshold = 501, MaxThreshold = 2000, HostSharePercent = 20f, PlatformSharePercent = 30f, DeveloperSharePercent = 50f },
            new HostTierConfig { Tier = HostTier.Platinum, MinThreshold = 2001, MaxThreshold = 10000, HostSharePercent = 22f, PlatformSharePercent = 28f, DeveloperSharePercent = 50f },
            new HostTierConfig { Tier = HostTier.Diamond, MinThreshold = 10001, MaxThreshold = 50000, HostSharePercent = 24f, PlatformSharePercent = 26f, DeveloperSharePercent = 50f },
            new HostTierConfig { Tier = HostTier.Elite, MinThreshold = 50001, MaxThreshold = int.MaxValue, HostSharePercent = 26f, PlatformSharePercent = 24f, DeveloperSharePercent = 50f }
        };

        /// <summary>Maximum bonus percentage from all sources</summary>
        public const float MAX_BONUS_PERCENT = 6f;

        /// <summary>Maximum total host share for esports (with bonuses)</summary>
        public const float MAX_ESPORTS_SHARE = 34f;

        /// <summary>Maximum total host share for social (with bonuses)</summary>
        public const float MAX_SOCIAL_SHARE = 32f;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Initialize the host manager. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;

            IsInitialized = true;
            DeskillzLogger.Debug("[HostManager] Initialized");
        }

        /// <summary>
        /// Shutdown the host manager. Called automatically by DeskillzManager.
        /// </summary>
        internal static void Shutdown()
        {
            if (!IsInitialized) return;

            CurrentProfile = null;
            CurrentEarnings = null;
            ActiveRooms.Clear();
            HostEvents.ClearAllSubscriptions();
            IsInitialized = false;

            DeskillzLogger.Debug("[HostManager] Shutdown");
        }

        // =====================================================================
        // PROFILE MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Get the current user's host profile.
        /// Creates a new profile if one doesn't exist.
        /// </summary>
        /// <param name="onSuccess">Called with profile on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetProfile(
            Action<HostProfile> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetProfile(
                profile =>
                {
                    CurrentProfile = profile;
                    HostEvents.InvokeProfileLoaded(profile);
                    onSuccess?.Invoke(profile);
                    DeskillzLogger.Debug($"[HostManager] Profile loaded: {profile}");
                },
                error =>
                {
                    if (error.Code == HostError.Codes.ProfileNotFound)
                    {
                        // Create new profile
                        CreateProfile(onSuccess, onError);
                    }
                    else
                    {
                        HostEvents.InvokeProfileError(error);
                        onError?.Invoke(error);
                    }
                }
            );
        }

        /// <summary>
        /// Create a new host profile for the current user.
        /// </summary>
        /// <param name="onSuccess">Called with profile on success</param>
        /// <param name="onError">Called on error</param>
        public static void CreateProfile(
            Action<HostProfile> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.CreateProfile(
                profile =>
                {
                    CurrentProfile = profile;
                    HostEvents.InvokeProfileLoaded(profile);
                    onSuccess?.Invoke(profile);
                    DeskillzLogger.Debug($"[HostManager] Profile created: {profile}");
                },
                error =>
                {
                    HostEvents.InvokeProfileError(error);
                    onError?.Invoke(error);
                }
            );
        }

        /// <summary>
        /// Verify age (18+ confirmation) for hosting.
        /// Required before hosting social/rake-based games.
        /// </summary>
        /// <param name="confirmed">Whether user confirms they are 18+</param>
        /// <param name="onSuccess">Called with updated profile on success</param>
        /// <param name="onError">Called on error</param>
        public static void VerifyAge(
            bool confirmed,
            Action<HostProfile> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.VerifyAge(confirmed,
                profile =>
                {
                    var wasVerified = CurrentProfile?.IsVerified ?? false;
                    CurrentProfile = profile;

                    if (!wasVerified && profile.IsVerified)
                    {
                        HostEvents.InvokeVerificationStatusChanged(true);
                    }

                    HostEvents.InvokeProfileLoaded(profile);
                    onSuccess?.Invoke(profile);
                    DeskillzLogger.Debug($"[HostManager] Age verified: {profile.IsVerified}");
                },
                error =>
                {
                    HostEvents.InvokeProfileError(error);
                    onError?.Invoke(error);
                }
            );
        }

        /// <summary>
        /// Refresh the current profile from server.
        /// </summary>
        public static void RefreshProfile(
            Action<HostProfile> onSuccess = null,
            Action<HostError> onError = null)
        {
            GetProfile(onSuccess, onError);
        }

        // =====================================================================
        // EARNINGS MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Get host earnings summary.
        /// </summary>
        /// <param name="onSuccess">Called with earnings on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetEarnings(
            Action<HostEarnings> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetEarnings(
                earnings =>
                {
                    CurrentEarnings = earnings;
                    HostEvents.InvokeEarningsLoaded(earnings);
                    onSuccess?.Invoke(earnings);
                    DeskillzLogger.Debug($"[HostManager] Earnings loaded: Available ${earnings.Available}");
                },
                error =>
                {
                    onError?.Invoke(error);
                }
            );
        }

        /// <summary>
        /// Get earnings transaction history.
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="limit">Items per page</param>
        /// <param name="onSuccess">Called with transactions on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetEarningsHistory(
            int page = 1,
            int limit = 20,
            Action<List<HostTransaction>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetEarningsHistory(page, limit, onSuccess, onError);
        }

        /// <summary>
        /// Request withdrawal of available balance.
        /// </summary>
        /// <param name="amount">Amount to withdraw in USD</param>
        /// <param name="currency">Cryptocurrency to receive (USDT, USDC, BNB, etc.)</param>
        /// <param name="walletAddress">Destination wallet address</param>
        /// <param name="onSuccess">Called with withdrawal response on success</param>
        /// <param name="onError">Called on error</param>
        public static void RequestWithdrawal(
            decimal amount,
            string currency,
            string walletAddress,
            Action<WithdrawalResponse> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            // Validate amount
            if (CurrentEarnings != null && amount > CurrentEarnings.Available)
            {
                onError?.Invoke(new HostError(
                    HostError.Codes.InsufficientBalance,
                    $"Insufficient balance. Available: ${CurrentEarnings.Available}"
                ));
                return;
            }

            var request = new WithdrawRequest
            {
                amount = amount,
                currency = currency,
                walletAddress = walletAddress
            };

            HostApiClient.RequestWithdrawal(request,
                response =>
                {
                    HostEvents.InvokeWithdrawalInitiated(amount, currency);
                    onSuccess?.Invoke(response);
                    DeskillzLogger.Debug($"[HostManager] Withdrawal requested: ${amount} {currency}");

                    // Refresh earnings to update available balance
                    GetEarnings();
                },
                error =>
                {
                    HostEvents.InvokeWithdrawalFailed(error);
                    onError?.Invoke(error);
                }
            );
        }

        // =====================================================================
        // BADGE MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Get all badges earned by the host.
        /// </summary>
        /// <param name="onSuccess">Called with badges on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetBadges(
            Action<List<HostBadge>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetBadges(
                badges =>
                {
                    if (CurrentProfile != null)
                    {
                        CurrentProfile.Badges = badges;
                    }
                    HostEvents.InvokeBadgesUpdated(badges);
                    onSuccess?.Invoke(badges);
                },
                onError
            );
        }

        /// <summary>
        /// Get badges that are available but not yet earned.
        /// </summary>
        /// <param name="onSuccess">Called with available badges on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetAvailableBadges(
            Action<List<HostBadge>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetAvailableBadges(onSuccess, onError);
        }

        /// <summary>
        /// Calculate total bonus percentage from active badges.
        /// </summary>
        /// <returns>Total bonus percentage (capped at MAX_BONUS_PERCENT)</returns>
        public static float CalculateBadgeBonus()
        {
            if (CurrentProfile?.Badges == null) return 0f;

            float total = 0f;
            foreach (var badge in CurrentProfile.Badges)
            {
                if (badge.IsActive)
                {
                    total += badge.BonusPercent;
                }
            }

            return Mathf.Min(total, MAX_BONUS_PERCENT);
        }

        // =====================================================================
        // TIER MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Get tier configuration for a revenue type.
        /// </summary>
        /// <param name="revenueType">Esports or Social</param>
        /// <returns>Array of tier configurations</returns>
        public static HostTierConfig[] GetTierConfig(RoomRevenueType revenueType)
        {
            return revenueType == RoomRevenueType.Social ? SocialTiers : EsportsTiers;
        }

        /// <summary>
        /// Calculate tier based on player count (for esports rooms).
        /// </summary>
        /// <param name="playerCount">Number of players in room</param>
        /// <returns>Calculated tier</returns>
        public static HostTier CalculateEsportsTier(int playerCount)
        {
            foreach (var tier in EsportsTiers)
            {
                if (playerCount >= tier.MinThreshold && playerCount <= tier.MaxThreshold)
                {
                    return tier.Tier;
                }
            }
            return HostTier.Bronze;
        }

        /// <summary>
        /// Calculate tier based on monthly rake (for social rooms).
        /// </summary>
        /// <param name="monthlyRake">Monthly rake generated in USD</param>
        /// <returns>Calculated tier</returns>
        public static HostTier CalculateSocialTier(decimal monthlyRake)
        {
            int rakeAmount = (int)monthlyRake;
            foreach (var tier in SocialTiers)
            {
                if (rakeAmount >= tier.MinThreshold && rakeAmount <= tier.MaxThreshold)
                {
                    return tier.Tier;
                }
            }
            return HostTier.Bronze;
        }

        /// <summary>
        /// Get host share percentage for a tier.
        /// </summary>
        /// <param name="tier">Host tier</param>
        /// <param name="revenueType">Revenue type</param>
        /// <param name="includeBonus">Whether to include badge/streak bonuses</param>
        /// <returns>Host share percentage</returns>
        public static float GetHostSharePercent(HostTier tier, RoomRevenueType revenueType, bool includeBonus = true)
        {
            var tiers = GetTierConfig(revenueType);
            float baseShare = 15f;

            foreach (var config in tiers)
            {
                if (config.Tier == tier)
                {
                    baseShare = config.HostSharePercent;
                    break;
                }
            }

            if (includeBonus)
            {
                float bonus = CalculateTotalBonus();
                float maxShare = revenueType == RoomRevenueType.Social ? MAX_SOCIAL_SHARE : MAX_ESPORTS_SHARE;
                return Mathf.Min(baseShare + bonus, maxShare);
            }

            return baseShare;
        }

        /// <summary>
        /// Get tier history.
        /// </summary>
        /// <param name="limit">Maximum entries to return</param>
        /// <param name="onSuccess">Called with history on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetTierHistory(
            int limit = 10,
            Action<List<TierHistoryEntry>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetTierHistory(limit, onSuccess, onError);
        }

        // =====================================================================
        // BONUS CALCULATION
        // =====================================================================

        /// <summary>
        /// Calculate streak bonus percentage.
        /// </summary>
        /// <returns>Streak bonus (0-2%)</returns>
        public static float CalculateStreakBonus()
        {
            if (CurrentProfile == null) return 0f;

            int streak = CurrentProfile.CurrentStreak;

            if (streak >= 30) return 2.0f;
            if (streak >= 14) return 1.5f;
            if (streak >= 7) return 1.0f;
            if (streak >= 3) return 0.5f;

            return 0f;
        }

        /// <summary>
        /// Calculate volume bonus percentage based on monthly earnings.
        /// </summary>
        /// <returns>Volume bonus (0-2%)</returns>
        public static float CalculateVolumeBonus()
        {
            if (CurrentEarnings == null) return 0f;

            decimal monthlyRevenue = CurrentEarnings.ThisMonth;

            if (monthlyRevenue >= 50000) return 2.0f;
            if (monthlyRevenue >= 20000) return 1.5f;
            if (monthlyRevenue >= 5000) return 1.0f;
            if (monthlyRevenue >= 1000) return 0.5f;

            return 0f;
        }

        /// <summary>
        /// Calculate total bonus from all sources.
        /// </summary>
        /// <returns>Total bonus percentage (capped at MAX_BONUS_PERCENT)</returns>
        public static float CalculateTotalBonus()
        {
            float streakBonus = CalculateStreakBonus();
            float volumeBonus = CalculateVolumeBonus();
            float badgeBonus = CalculateBadgeBonus();

            return Mathf.Min(streakBonus + volumeBonus + badgeBonus, MAX_BONUS_PERCENT);
        }

        // =====================================================================
        // ACTIVE ROOMS
        // =====================================================================

        /// <summary>
        /// Get active rooms currently being hosted.
        /// </summary>
        /// <param name="onSuccess">Called with active rooms on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetActiveRooms(
            Action<List<ActiveRoomSummary>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetActiveRooms(
                rooms =>
                {
                    ActiveRooms = rooms;
                    HostEvents.InvokeActiveRoomsUpdated(rooms);
                    onSuccess?.Invoke(rooms);
                },
                onError
            );
        }

        /// <summary>
        /// Get room hosting history.
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="limit">Items per page</param>
        /// <param name="onSuccess">Called with room history on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetRoomHistory(
            int page = 1,
            int limit = 20,
            Action<List<RoomHistoryEntry>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetRoomHistory(page, limit, onSuccess, onError);
        }

        // =====================================================================
        // STATISTICS
        // =====================================================================

        /// <summary>
        /// Get host statistics.
        /// </summary>
        /// <param name="onSuccess">Called with stats on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetStats(
            Action<HostStats> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetStats(
                stats =>
                {
                    HostEvents.InvokeStatsLoaded(stats);
                    onSuccess?.Invoke(stats);
                },
                onError
            );
        }

        // =====================================================================
        // NOTIFICATIONS
        // =====================================================================

        /// <summary>
        /// Get notification settings.
        /// </summary>
        /// <param name="onSuccess">Called with settings on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetNotificationSettings(
            Action<HostNotificationSettings> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetNotificationSettings(onSuccess, onError);
        }

        /// <summary>
        /// Update notification settings.
        /// </summary>
        /// <param name="settings">New settings</param>
        /// <param name="onSuccess">Called with updated settings on success</param>
        /// <param name="onError">Called on error</param>
        public static void UpdateNotificationSettings(
            HostNotificationSettings settings,
            Action<HostNotificationSettings> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            var request = new UpdateNotificationSettingsRequest
            {
                pushEnabled = settings.PushEnabled,
                inAppEnabled = settings.InAppEnabled,
                soundEnabled = settings.SoundEnabled,
                batchSettlements = settings.BatchSettlements,
                playerLeftSettlements = settings.PlayerLeftSettlements,
                sessionComplete = settings.SessionComplete,
                tierChanges = settings.TierChanges,
                badgeEarned = settings.BadgeEarned,
                levelUp = settings.LevelUp
            };

            HostApiClient.UpdateNotificationSettings(request,
                updatedSettings =>
                {
                    if (CurrentProfile != null)
                    {
                        CurrentProfile.NotificationSettings = updatedSettings;
                    }
                    HostEvents.InvokeNotificationSettingsUpdated(updatedSettings);
                    onSuccess?.Invoke(updatedSettings);
                },
                onError
            );
        }

        /// <summary>
        /// Get recent notifications.
        /// </summary>
        /// <param name="limit">Maximum notifications to return</param>
        /// <param name="onSuccess">Called with notifications on success</param>
        /// <param name="onError">Called on error</param>
        public static void GetNotifications(
            int limit = 20,
            Action<List<HostNotification>> onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.GetNotifications(limit, onSuccess, onError);
        }

        /// <summary>
        /// Mark a notification as read.
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void MarkNotificationRead(
            string notificationId,
            Action onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.MarkNotificationRead(notificationId, onSuccess, onError);
        }

        /// <summary>
        /// Mark all notifications as read.
        /// </summary>
        /// <param name="onSuccess">Called on success</param>
        /// <param name="onError">Called on error</param>
        public static void MarkAllNotificationsRead(
            Action onSuccess = null,
            Action<HostError> onError = null)
        {
            EnsureInitialized();

            if (!ValidateAuthentication(onError)) return;

            HostApiClient.MarkAllNotificationsRead(onSuccess, onError);
        }

        // =====================================================================
        // INTERNAL HELPERS
        // =====================================================================

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
        }

        private static bool ValidateAuthentication(Action<HostError> onError)
        {
            if (DeskillzManager.Instance?.CurrentPlayer == null)
            {
                onError?.Invoke(new HostError(
                    HostError.Codes.NotAuthenticated,
                    "User not authenticated. Please log in first."
                ));
                return false;
            }
            return true;
        }

        // =====================================================================
        // WEBSOCKET EVENT HANDLERS (Called by internal systems)
        // =====================================================================

        /// <summary>
        /// Handle tier change notification from WebSocket
        /// </summary>
        internal static void HandleTierChange(HostTier oldTier, HostTier newTier, RoomRevenueType type)
        {
            if (CurrentProfile != null)
            {
                if (type == RoomRevenueType.Esports)
                {
                    CurrentProfile.EsportsTier = newTier;
                    HostEvents.InvokeEsportsTierChanged(oldTier, newTier);
                }
                else
                {
                    CurrentProfile.SocialTier = newTier;
                    HostEvents.InvokeSocialTierChanged(oldTier, newTier);
                }
            }

            DeskillzLogger.Debug($"[HostManager] Tier changed: {oldTier} -> {newTier} ({type})");
        }

        /// <summary>
        /// Handle badge earned notification from WebSocket
        /// </summary>
        internal static void HandleBadgeEarned(HostBadge badge)
        {
            if (CurrentProfile != null)
            {
                CurrentProfile.Badges.Add(badge);
            }

            HostEvents.InvokeBadgeEarned(badge);
            DeskillzLogger.Debug($"[HostManager] Badge earned: {badge.Name}");
        }

        /// <summary>
        /// Handle level up notification from WebSocket
        /// </summary>
        internal static void HandleLevelUp(int oldLevel, int newLevel)
        {
            if (CurrentProfile != null)
            {
                CurrentProfile.Level = newLevel;
            }

            HostEvents.InvokeLevelUp(oldLevel, newLevel);
            DeskillzLogger.Debug($"[HostManager] Level up: {oldLevel} -> {newLevel}");
        }

        /// <summary>
        /// Handle settlement notification from WebSocket
        /// </summary>
        internal static void HandleSettlement(string roomId, decimal amount, SettlementTrigger trigger)
        {
            if (CurrentEarnings != null)
            {
                CurrentEarnings.Available += amount;
            }

            HostEvents.InvokeSettlementReceived(roomId, amount, trigger);
            HostEvents.InvokeEarningsReceived(amount, roomId);
            DeskillzLogger.Debug($"[HostManager] Settlement received: ${amount} from room {roomId} ({trigger})");
        }

        /// <summary>
        /// Handle notification from WebSocket
        /// </summary>
        internal static void HandleNotification(HostNotification notification)
        {
            HostEvents.InvokeNotificationReceived(notification);
            DeskillzLogger.Debug($"[HostManager] Notification: {notification.Type} - {notification.Title}");
        }
    }
}
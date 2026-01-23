// =============================================================================
// Deskillz SDK for Unity - Authentication Manager
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.0.0 (Self-Sufficient Architecture)
// =============================================================================
//
// This is the main authentication manager for the Deskillz SDK.
// Use this class to handle all user authentication within your game.
//
// Features:
// - Email/password login and registration
// - Social login (Google, Apple, Facebook)
// - Wallet connection (optional, for paid tournaments)
// - Automatic token refresh
// - Persistent login (Remember Me)
//
// =============================================================================

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Main authentication manager for the Deskillz SDK.
    /// Handles user login, registration, and session management.
    /// </summary>
    public static class DeskillzAuth
    {
        // =====================================================================
        // CONSTANTS
        // =====================================================================
        
        private const string PREF_ACCESS_TOKEN = "deskillz_access_token";
        private const string PREF_REFRESH_TOKEN = "deskillz_refresh_token";
        private const string PREF_USER_ID = "deskillz_user_id";
        private const string PREF_REMEMBER_ME = "deskillz_remember_me";
        private const string PREF_TOKEN_EXPIRY = "deskillz_token_expiry";
        
        // Token refresh threshold (5 minutes before expiry)
        private const int TOKEN_REFRESH_THRESHOLD_SECONDS = 300;
        
        // =====================================================================
        // STATE PROPERTIES
        // =====================================================================
        
        private static AuthUser _currentUser;
        private static string _accessToken;
        private static string _refreshToken;
        private static DateTime _tokenExpiry;
        private static bool _isInitialized;
        
        /// <summary>
        /// Current authenticated user. Null if not logged in.
        /// </summary>
        public static AuthUser CurrentUser => _currentUser;
        
        /// <summary>
        /// Whether a user is currently authenticated.
        /// </summary>
        public static bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_accessToken);
        
        /// <summary>
        /// Current authentication state.
        /// </summary>
        public static AuthState State { get; private set; } = AuthState.NotAuthenticated;
        
        /// <summary>
        /// Get current access token for API calls.
        /// Automatically refreshes if expired.
        /// </summary>
        public static string AccessToken => _accessToken;
        
        /// <summary>
        /// Whether the user has a connected wallet.
        /// </summary>
        public static bool HasWallet => _currentUser?.HasWallet ?? false;
        
        /// <summary>
        /// Connected wallet address (null if none).
        /// </summary>
        public static string WalletAddress => _currentUser?.WalletAddress;
        
        // =====================================================================
        // EVENTS
        // =====================================================================
        
        /// <summary>
        /// Fired when login succeeds.
        /// </summary>
        public static event Action<AuthUser> OnLoginSuccess;
        
        /// <summary>
        /// Fired when logout occurs.
        /// </summary>
        public static event Action OnLogout;
        
        /// <summary>
        /// Fired when authentication error occurs.
        /// </summary>
        public static event Action<string> OnAuthError;
        
        /// <summary>
        /// Fired when wallet is connected to account.
        /// </summary>
        public static event Action<string> OnWalletConnected;
        
        /// <summary>
        /// Fired when wallet is disconnected from account.
        /// </summary>
        public static event Action OnWalletDisconnected;
        
        /// <summary>
        /// Fired when sign up succeeds.
        /// </summary>
        public static event Action<AuthUser> OnSignUpSuccess;
        
        /// <summary>
        /// Fired when password reset email is sent.
        /// </summary>
        public static event Action OnPasswordResetSent;
        
        /// <summary>
        /// Fired when auth state changes.
        /// </summary>
        public static event Action<AuthState, AuthUser> OnAuthStateChanged;
        
        // =====================================================================
        // INITIALIZATION
        // =====================================================================
        
        /// <summary>
        /// Initialize the auth system. Called automatically by SDK.
        /// Restores previous session if "Remember Me" was enabled.
        /// </summary>
        public static async void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            DeskillzLogger.Info("[DeskillzAuth] Initializing authentication system");
            
            // Check for existing session
            if (HasSavedSession())
            {
                DeskillzLogger.Info("[DeskillzAuth] Found saved session, attempting restore");
                await TryRestoreSession();
            }
            else
            {
                SetState(AuthState.NotAuthenticated);
            }
        }
        
        private static bool HasSavedSession()
        {
            return PlayerPrefs.GetInt(PREF_REMEMBER_ME, 0) == 1 &&
                   !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_REFRESH_TOKEN, ""));
        }
        
        private static async Task TryRestoreSession()
        {
            SetState(AuthState.Authenticating);
            
            try
            {
                _refreshToken = PlayerPrefs.GetString(PREF_REFRESH_TOKEN, "");
                
                if (string.IsNullOrEmpty(_refreshToken))
                {
                    SetState(AuthState.NotAuthenticated);
                    return;
                }
                
                // Try to refresh the token
                var response = await AuthService.RefreshToken(_refreshToken);
                
                HandleAuthSuccess(response, true);
                DeskillzLogger.Info("[DeskillzAuth] Session restored successfully");
            }
            catch (Exception ex)
            {
                DeskillzLogger.Warning($"[DeskillzAuth] Failed to restore session: {ex.Message}");
                ClearSavedSession();
                SetState(AuthState.NotAuthenticated);
            }
        }
        
        // =====================================================================
        // LOGIN METHODS
        // =====================================================================
        
        /// <summary>
        /// Login with email and password.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        /// <param name="rememberMe">Whether to persist login across sessions</param>
        public static async Task<AuthUser> Login(string email, string password, bool rememberMe = false)
        {
            DeskillzLogger.Info($"[DeskillzAuth] Logging in: {email}");
            SetState(AuthState.Authenticating);
            
            try
            {
                ValidateEmail(email);
                ValidatePassword(password);
                
                var request = new LoginRequest(email, password);
                var response = await AuthService.Login(request);
                
                HandleAuthSuccess(response, rememberMe);
                
                DeskillzLogger.Info($"[DeskillzAuth] Login successful: {_currentUser.Username}");
                OnLoginSuccess?.Invoke(_currentUser);
                
                return _currentUser;
            }
            catch (DeskillzAuthException ex)
            {
                HandleAuthError(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                HandleAuthError($"Login failed: {ex.Message}");
                throw new DeskillzAuthException(ex.Message);
            }
        }
        
        /// <summary>
        /// Login with social provider (Google, Apple, Facebook).
        /// </summary>
        /// <param name="provider">Provider name: "google", "apple", "facebook"</param>
        /// <param name="idToken">ID token from the social provider</param>
        public static async Task<AuthUser> SocialLogin(string provider, string idToken)
        {
            DeskillzLogger.Info($"[DeskillzAuth] Social login via: {provider}");
            SetState(AuthState.Authenticating);
            
            try
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    throw new DeskillzAuthException("ID token is required");
                }
                
                var request = new SocialAuthRequest(provider.ToLower(), idToken);
                var response = await AuthService.SocialLogin(request);
                
                HandleAuthSuccess(response, true); // Social logins always remember
                
                if (response.isNewUser)
                {
                    DeskillzLogger.Info($"[DeskillzAuth] New user created via {provider}: {_currentUser.Username}");
                    OnSignUpSuccess?.Invoke(_currentUser);
                }
                else
                {
                    DeskillzLogger.Info($"[DeskillzAuth] Social login successful: {_currentUser.Username}");
                    OnLoginSuccess?.Invoke(_currentUser);
                }
                
                return _currentUser;
            }
            catch (DeskillzAuthException ex)
            {
                HandleAuthError(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                HandleAuthError($"Social login failed: {ex.Message}");
                throw new DeskillzAuthException(ex.Message);
            }
        }
        
        // =====================================================================
        // SIGNUP METHODS
        // =====================================================================
        
        /// <summary>
        /// Register a new user with email and password.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">Password (min 8 characters)</param>
        /// <param name="username">Desired username (3-20 characters)</param>
        public static async Task<AuthUser> SignUp(string email, string password, string username)
        {
            DeskillzLogger.Info($"[DeskillzAuth] Registering: {email}, username: {username}");
            SetState(AuthState.Authenticating);
            
            try
            {
                ValidateEmail(email);
                ValidatePassword(password);
                ValidateUsername(username);
                
                var request = new SignUpRequest(email, password, username);
                var response = await AuthService.SignUp(request);
                
                HandleAuthSuccess(response, true); // Auto-login after signup
                
                DeskillzLogger.Info($"[DeskillzAuth] Registration successful: {_currentUser.Username}");
                OnSignUpSuccess?.Invoke(_currentUser);
                
                return _currentUser;
            }
            catch (DeskillzAuthException ex)
            {
                HandleAuthError(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                HandleAuthError($"Registration failed: {ex.Message}");
                throw new DeskillzAuthException(ex.Message);
            }
        }
        
        // =====================================================================
        // LOGOUT
        // =====================================================================
        
        /// <summary>
        /// Logout the current user.
        /// </summary>
        public static void Logout()
        {
            DeskillzLogger.Info("[DeskillzAuth] Logging out");
            
            _currentUser = null;
            _accessToken = null;
            _refreshToken = null;
            _tokenExpiry = DateTime.MinValue;
            
            ClearSavedSession();
            SetState(AuthState.NotAuthenticated);
            
            OnLogout?.Invoke();
        }
        
        // =====================================================================
        // PASSWORD RESET
        // =====================================================================
        
        /// <summary>
        /// Request a password reset email.
        /// </summary>
        /// <param name="email">User's email address</param>
        public static async Task ForgotPassword(string email)
        {
            DeskillzLogger.Info($"[DeskillzAuth] Requesting password reset for: {email}");
            
            try
            {
                ValidateEmail(email);
                await AuthService.ForgotPassword(email);
                
                DeskillzLogger.Info("[DeskillzAuth] Password reset email sent");
                OnPasswordResetSent?.Invoke();
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[DeskillzAuth] Password reset failed: {ex.Message}");
                throw new DeskillzAuthException(ex.Message);
            }
        }
        
        // =====================================================================
        // WALLET MANAGEMENT
        // =====================================================================
        
        /// <summary>
        /// Link a wallet to the current account.
        /// Required for paid tournaments.
        /// </summary>
        /// <param name="walletAddress">Wallet address to link</param>
        /// <param name="signature">Signature from wallet</param>
        /// <param name="message">Message that was signed</param>
        /// <param name="nonce">Nonce used in signature</param>
        public static async Task<AuthUser> LinkWallet(string walletAddress, string signature, string message, string nonce)
        {
            DeskillzLogger.Info($"[DeskillzAuth] Linking wallet: {walletAddress}");
            
            if (!IsAuthenticated)
            {
                throw new DeskillzAuthException("Must be logged in to link wallet");
            }
            
            try
            {
                var request = new WalletLinkRequest(walletAddress, signature, message, nonce);
                var user = await AuthService.LinkWallet(request, _accessToken);
                
                _currentUser = user;
                DeskillzLogger.Info("[DeskillzAuth] Wallet linked successfully");
                OnWalletConnected?.Invoke(walletAddress);
                
                return _currentUser;
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[DeskillzAuth] Failed to link wallet: {ex.Message}");
                throw new DeskillzAuthException(ex.Message);
            }
        }
        
        /// <summary>
        /// Disconnect the wallet from the current account.
        /// </summary>
        public static async Task DisconnectWallet()
        {
            DeskillzLogger.Info("[DeskillzAuth] Disconnecting wallet");
            
            if (!IsAuthenticated)
            {
                throw new DeskillzAuthException("Must be logged in to disconnect wallet");
            }
            
            if (!HasWallet)
            {
                DeskillzLogger.Warning("[DeskillzAuth] No wallet connected");
                return;
            }
            
            try
            {
                var user = await AuthService.DisconnectWallet(_accessToken);
                _currentUser = user;
                
                DeskillzLogger.Info("[DeskillzAuth] Wallet disconnected successfully");
                OnWalletDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[DeskillzAuth] Failed to disconnect wallet: {ex.Message}");
                throw new DeskillzAuthException(ex.Message);
            }
        }
        
        /// <summary>
        /// Check if wallet is required for an action and prompt if needed.
        /// </summary>
        /// <param name="actionDescription">Description of the action requiring wallet</param>
        /// <param name="onWalletConnected">Callback when wallet is connected</param>
        /// <returns>True if wallet already connected, false if prompt shown</returns>
        public static bool RequireWallet(string actionDescription, Action onWalletConnected)
        {
            if (HasWallet)
            {
                return true;
            }
            
            DeskillzLogger.Info($"[DeskillzAuth] Wallet required for: {actionDescription}");
            
            // Fire event to show wallet connection UI
            DeskillzEvents.RaiseWalletRequired(actionDescription, onWalletConnected);
            
            return false;
        }
        
        // =====================================================================
        // TOKEN MANAGEMENT
        // =====================================================================
        
        /// <summary>
        /// Get a valid access token, refreshing if necessary.
        /// </summary>
        public static async Task<string> GetAccessToken()
        {
            if (!IsAuthenticated)
            {
                return null;
            }
            
            // Check if token needs refresh
            if (IsTokenExpiringSoon())
            {
                await RefreshAccessToken();
            }
            
            return _accessToken;
        }
        
        private static bool IsTokenExpiringSoon()
        {
            return _tokenExpiry != DateTime.MinValue &&
                   DateTime.UtcNow.AddSeconds(TOKEN_REFRESH_THRESHOLD_SECONDS) >= _tokenExpiry;
        }
        
        private static async Task RefreshAccessToken()
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                DeskillzLogger.Warning("[DeskillzAuth] No refresh token available");
                Logout();
                return;
            }
            
            try
            {
                DeskillzLogger.Info("[DeskillzAuth] Refreshing access token");
                var response = await AuthService.RefreshToken(_refreshToken);
                
                _accessToken = response.accessToken;
                _refreshToken = response.refreshToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(response.expiresIn);
                
                // Update saved tokens if remember me is enabled
                if (PlayerPrefs.GetInt(PREF_REMEMBER_ME, 0) == 1)
                {
                    SaveTokens();
                }
                
                DeskillzLogger.Info("[DeskillzAuth] Token refreshed successfully");
            }
            catch (Exception ex)
            {
                DeskillzLogger.Error($"[DeskillzAuth] Token refresh failed: {ex.Message}");
                Logout();
            }
        }
        
        // =====================================================================
        // INTERNAL HELPERS
        // =====================================================================
        
        private static void HandleAuthSuccess(AuthResponse response, bool rememberMe)
        {
            _currentUser = response.user;
            _accessToken = response.accessToken;
            _refreshToken = response.refreshToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(response.expiresIn);
            
            if (rememberMe)
            {
                SaveSession();
            }
            
            SetState(AuthState.Authenticated);
        }
        
        private static void HandleAuthError(string message)
        {
            DeskillzLogger.Error($"[DeskillzAuth] Auth error: {message}");
            SetState(AuthState.Error);
            OnAuthError?.Invoke(message);
        }
        
        private static void SetState(AuthState newState)
        {
            if (State != newState)
            {
                State = newState;
                OnAuthStateChanged?.Invoke(newState, _currentUser);
            }
        }
        
        private static void SaveSession()
        {
            PlayerPrefs.SetInt(PREF_REMEMBER_ME, 1);
            SaveTokens();
            
            if (_currentUser != null)
            {
                PlayerPrefs.SetString(PREF_USER_ID, _currentUser.Id);
            }
            
            PlayerPrefs.Save();
        }
        
        private static void SaveTokens()
        {
            PlayerPrefs.SetString(PREF_ACCESS_TOKEN, _accessToken ?? "");
            PlayerPrefs.SetString(PREF_REFRESH_TOKEN, _refreshToken ?? "");
            PlayerPrefs.SetString(PREF_TOKEN_EXPIRY, _tokenExpiry.ToString("O"));
            PlayerPrefs.Save();
        }
        
        private static void ClearSavedSession()
        {
            PlayerPrefs.DeleteKey(PREF_ACCESS_TOKEN);
            PlayerPrefs.DeleteKey(PREF_REFRESH_TOKEN);
            PlayerPrefs.DeleteKey(PREF_USER_ID);
            PlayerPrefs.DeleteKey(PREF_REMEMBER_ME);
            PlayerPrefs.DeleteKey(PREF_TOKEN_EXPIRY);
            PlayerPrefs.Save();
        }
        
        // =====================================================================
        // VALIDATION HELPERS
        // =====================================================================
        
        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new DeskillzAuthException("Email is required");
            }
            
            if (!email.Contains("@") || !email.Contains("."))
            {
                throw new DeskillzAuthException("Invalid email format");
            }
        }
        
        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new DeskillzAuthException("Password is required");
            }
            
            if (password.Length < 8)
            {
                throw new DeskillzAuthException("Password must be at least 8 characters");
            }
        }
        
        private static void ValidateUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new DeskillzAuthException("Username is required");
            }
            
            if (username.Length < 3 || username.Length > 20)
            {
                throw new DeskillzAuthException("Username must be 3-20 characters");
            }
            
            // Check for valid characters (alphanumeric and underscore)
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    throw new DeskillzAuthException("Username can only contain letters, numbers, and underscores");
                }
            }
        }
    }
    
    // =========================================================================
    // AUTH STATE ENUM
    // =========================================================================
    
    /// <summary>
    /// Authentication state.
    /// </summary>
    public enum AuthState
    {
        /// <summary>No user is authenticated</summary>
        NotAuthenticated,
        
        /// <summary>Authentication in progress</summary>
        Authenticating,
        
        /// <summary>User is authenticated</summary>
        Authenticated,
        
        /// <summary>Authentication error occurred</summary>
        Error
    }
}
// =============================================================================
// Deskillz SDK for Unity - Authentication Service
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.0.0 (Self-Sufficient Architecture)
// =============================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Deskillz
{
    /// <summary>
    /// Handles all authentication API calls to the Deskillz backend.
    /// This is an internal service used by DeskillzAuth.
    /// </summary>
    public static class AuthService
    {
        // =====================================================================
        // API ENDPOINTS
        // =====================================================================
        
        private static string BaseUrl => DeskillzConfig.Instance?.ApiBaseUrl ?? "https://api.deskillz.games";
        
        private const string REGISTER_ENDPOINT = "/api/v1/auth/register";
        private const string LOGIN_ENDPOINT = "/api/v1/auth/login";
        private const string SOCIAL_AUTH_ENDPOINT = "/api/v1/auth/social";
        private const string REFRESH_ENDPOINT = "/api/v1/auth/refresh";
        private const string ME_ENDPOINT = "/api/v1/auth/me";
        private const string FORGOT_PASSWORD_ENDPOINT = "/api/v1/auth/forgot-password";
        private const string RESET_PASSWORD_ENDPOINT = "/api/v1/auth/reset-password";
        private const string WALLET_LINK_ENDPOINT = "/api/v1/auth/wallet/link";
        private const string WALLET_DISCONNECT_ENDPOINT = "/api/v1/auth/wallet/disconnect";
        private const string WALLET_NONCE_ENDPOINT = "/api/v1/auth/nonce";

        // =====================================================================
        // PUBLIC API METHODS
        // =====================================================================

        /// <summary>
        /// Register a new user with email and password.
        /// </summary>
        public static async Task<AuthResponse> SignUp(SignUpRequest request)
        {
            DeskillzLogger.Info($"[AuthService] Registering user: {request.email}");
            
            var json = JsonConvert.SerializeObject(request);
            var response = await PostRequest<AuthResponse>(REGISTER_ENDPOINT, json);
            
            DeskillzLogger.Info($"[AuthService] Registration successful: {response.user?.Username}");
            return response;
        }

        /// <summary>
        /// Login with email and password.
        /// </summary>
        public static async Task<AuthResponse> Login(LoginRequest request)
        {
            DeskillzLogger.Info($"[AuthService] Logging in user: {request.email}");
            
            var json = JsonConvert.SerializeObject(request);
            var response = await PostRequest<AuthResponse>(LOGIN_ENDPOINT, json);
            
            DeskillzLogger.Info($"[AuthService] Login successful: {response.user?.Username}");
            return response;
        }

        /// <summary>
        /// Login with social provider (Google, Apple, Facebook).
        /// </summary>
        public static async Task<AuthResponse> SocialLogin(SocialAuthRequest request)
        {
            DeskillzLogger.Info($"[AuthService] Social login via: {request.provider}");
            
            var json = JsonConvert.SerializeObject(request);
            var response = await PostRequest<AuthResponse>(SOCIAL_AUTH_ENDPOINT, json);
            
            DeskillzLogger.Info($"[AuthService] Social login successful: {response.user?.Username}");
            return response;
        }

        /// <summary>
        /// Refresh access token using refresh token.
        /// </summary>
        public static async Task<AuthResponse> RefreshToken(string refreshToken)
        {
            DeskillzLogger.Info("[AuthService] Refreshing access token");
            
            var body = new { refreshToken = refreshToken };
            var json = JsonConvert.SerializeObject(body);
            var response = await PostRequest<AuthResponse>(REFRESH_ENDPOINT, json);
            
            DeskillzLogger.Info("[AuthService] Token refreshed successfully");
            return response;
        }

        /// <summary>
        /// Get current authenticated user data.
        /// </summary>
        public static async Task<AuthUser> GetMe(string accessToken)
        {
            DeskillzLogger.Info("[AuthService] Fetching current user");
            
            var response = await GetRequest<AuthUser>(ME_ENDPOINT, accessToken);
            
            DeskillzLogger.Info($"[AuthService] Got user: {response?.Username}");
            return response;
        }

        /// <summary>
        /// Request password reset email.
        /// </summary>
        public static async Task ForgotPassword(string email)
        {
            DeskillzLogger.Info($"[AuthService] Requesting password reset for: {email}");
            
            var body = new ForgotPasswordRequest(email);
            var json = JsonConvert.SerializeObject(body);
            await PostRequest<SuccessResponse>(FORGOT_PASSWORD_ENDPOINT, json);
            
            DeskillzLogger.Info("[AuthService] Password reset email sent");
        }

        /// <summary>
        /// Reset password with token from email.
        /// </summary>
        public static async Task ResetPassword(string token, string newPassword)
        {
            DeskillzLogger.Info("[AuthService] Resetting password");
            
            var body = new ResetPasswordRequest(token, newPassword);
            var json = JsonConvert.SerializeObject(body);
            await PostRequest<SuccessResponse>(RESET_PASSWORD_ENDPOINT, json);
            
            DeskillzLogger.Info("[AuthService] Password reset successful");
        }

        /// <summary>
        /// Link wallet to existing account.
        /// </summary>
        public static async Task<AuthUser> LinkWallet(WalletLinkRequest request, string accessToken)
        {
            DeskillzLogger.Info($"[AuthService] Linking wallet: {request.walletAddress}");
            
            var json = JsonConvert.SerializeObject(request);
            var response = await PostRequestWithAuth<AuthUser>(WALLET_LINK_ENDPOINT, json, accessToken);
            
            DeskillzLogger.Info("[AuthService] Wallet linked successfully");
            return response;
        }

        /// <summary>
        /// Disconnect wallet from account.
        /// </summary>
        public static async Task<AuthUser> DisconnectWallet(string accessToken)
        {
            DeskillzLogger.Info("[AuthService] Disconnecting wallet");
            
            var response = await PostRequestWithAuth<AuthUser>(WALLET_DISCONNECT_ENDPOINT, "{}", accessToken);
            
            DeskillzLogger.Info("[AuthService] Wallet disconnected successfully");
            return response;
        }

        /// <summary>
        /// Get nonce for wallet signature (SIWE).
        /// </summary>
        public static async Task<NonceResponse> GetWalletNonce(string walletAddress)
        {
            DeskillzLogger.Info($"[AuthService] Getting nonce for wallet: {walletAddress}");
            
            var endpoint = $"{WALLET_NONCE_ENDPOINT}?walletAddress={Uri.EscapeDataString(walletAddress)}";
            var response = await GetRequest<NonceResponse>(endpoint);
            
            DeskillzLogger.Info("[AuthService] Got nonce");
            return response;
        }

        // =====================================================================
        // INTERNAL HTTP HELPERS
        // =====================================================================

        private static async Task<T> PostRequest<T>(string endpoint, string jsonBody)
        {
            var url = BaseUrl + endpoint;
            
            using (var request = new UnityWebRequest(url, "POST"))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                
                // Add game ID for tracking
                if (DeskillzConfig.Instance != null)
                {
                    request.SetRequestHeader("X-Game-Id", DeskillzConfig.Instance.GameId);
                }
                
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                return HandleResponse<T>(request);
            }
        }

        private static async Task<T> PostRequestWithAuth<T>(string endpoint, string jsonBody, string accessToken)
        {
            var url = BaseUrl + endpoint;
            
            using (var request = new UnityWebRequest(url, "POST"))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                
                if (DeskillzConfig.Instance != null)
                {
                    request.SetRequestHeader("X-Game-Id", DeskillzConfig.Instance.GameId);
                }
                
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                return HandleResponse<T>(request);
            }
        }

        private static async Task<T> GetRequest<T>(string endpoint, string accessToken = null)
        {
            var url = BaseUrl + endpoint;
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Accept", "application/json");
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                }
                
                if (DeskillzConfig.Instance != null)
                {
                    request.SetRequestHeader("X-Game-Id", DeskillzConfig.Instance.GameId);
                }
                
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                return HandleResponse<T>(request);
            }
        }

        private static T HandleResponse<T>(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseText = request.downloadHandler.text;
                DeskillzLogger.Debug($"[AuthService] Response: {responseText}");
                
                try
                {
                    // Try to parse as the expected type
                    return JsonConvert.DeserializeObject<T>(responseText);
                }
                catch (JsonException ex)
                {
                    DeskillzLogger.Error($"[AuthService] JSON parse error: {ex.Message}");
                    throw new DeskillzAuthException("Invalid response format from server");
                }
            }
            else
            {
                var errorText = request.downloadHandler?.text ?? request.error;
                DeskillzLogger.Error($"[AuthService] Request failed ({request.responseCode}): {errorText}");
                
                // Try to parse error response
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(errorText);
                    throw new DeskillzAuthException(errorResponse?.message ?? errorText, (int)request.responseCode);
                }
                catch (JsonException)
                {
                    throw new DeskillzAuthException(errorText, (int)request.responseCode);
                }
            }
        }
    }

    /// <summary>
    /// Authentication-specific exception.
    /// </summary>
    public class DeskillzAuthException : Exception
    {
        public int StatusCode { get; }

        public DeskillzAuthException(string message, int statusCode = 0) 
            : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Check if this is an unauthorized error (token expired).
        /// </summary>
        public bool IsUnauthorized => StatusCode == 401;

        /// <summary>
        /// Check if this is a conflict error (email/username taken).
        /// </summary>
        public bool IsConflict => StatusCode == 409;

        /// <summary>
        /// Check if this is a validation error.
        /// </summary>
        public bool IsValidation => StatusCode == 400;
    }

    /// <summary>
    /// Error response from API.
    /// </summary>
    [Serializable]
    internal class ErrorResponse
    {
        public string message { get; set; }
        public string error { get; set; }
        public int statusCode { get; set; }
    }
}
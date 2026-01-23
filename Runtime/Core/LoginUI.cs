// =============================================================================
// Deskillz SDK for Unity - Login UI
// Copyright (c) 2024-2026 Deskillz.Games. All rights reserved.
// Version: 3.0.0 (Self-Sufficient Architecture)
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Deskillz.UI
{
    /// <summary>
    /// Login screen UI component.
    /// Attach this to your login panel/canvas.
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        // =====================================================================
        // UI REFERENCES
        // =====================================================================
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        
        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button googleButton;
        [SerializeField] private Button appleButton;
        [SerializeField] private Button facebookButton;
        
        [Header("Toggle")]
        [SerializeField] private Toggle rememberMeToggle;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TextMeshProUGUI versionText;
        
        [Header("Navigation")]
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private GameObject forgotPasswordPanel;
        
        // =====================================================================
        // EVENTS
        // =====================================================================
        
        /// <summary>
        /// Fired when login succeeds. Use to navigate to lobby.
        /// </summary>
        public event Action<AuthUser> OnLoginSuccess;
        
        /// <summary>
        /// Fired when user wants to switch to sign up.
        /// </summary>
        public event Action OnShowSignUp;
        
        /// <summary>
        /// Fired when user wants to reset password.
        /// </summary>
        public event Action OnShowForgotPassword;
        
        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================
        
        private void Awake()
        {
            SetupUI();
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void OnEnable()
        {
            ClearError();
            ClearInputs();
            
            // Check if already logged in
            if (DeskillzAuth.IsAuthenticated)
            {
                OnLoginSuccess?.Invoke(DeskillzAuth.CurrentUser);
            }
        }
        
        // =====================================================================
        // SETUP
        // =====================================================================
        
        private void SetupUI()
        {
            // Setup button listeners
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            if (forgotPasswordButton != null)
                forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
            
            if (signUpButton != null)
                signUpButton.onClick.AddListener(OnSignUpClicked);
            
            if (googleButton != null)
                googleButton.onClick.AddListener(() => OnSocialLoginClicked("google"));
            
            if (appleButton != null)
                appleButton.onClick.AddListener(() => OnSocialLoginClicked("apple"));
            
            if (facebookButton != null)
                facebookButton.onClick.AddListener(() => OnSocialLoginClicked("facebook"));
            
            // Setup input validation
            if (emailInput != null)
                emailInput.onValueChanged.AddListener(_ => ClearError());
            
            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(_ => ClearError());
                passwordInput.contentType = TMP_InputField.ContentType.Password;
            }
            
            // Setup version text
            if (versionText != null)
                versionText.text = $"v{DeskillzConfig.SDK_VERSION}";
            
            // Hide loading initially
            SetLoading(false);
        }
        
        private void SubscribeToEvents()
        {
            DeskillzAuth.OnLoginSuccess += HandleLoginSuccess;
            DeskillzAuth.OnAuthError += HandleAuthError;
            DeskillzAuth.OnAuthStateChanged += HandleAuthStateChanged;
        }
        
        private void UnsubscribeFromEvents()
        {
            DeskillzAuth.OnLoginSuccess -= HandleLoginSuccess;
            DeskillzAuth.OnAuthError -= HandleAuthError;
            DeskillzAuth.OnAuthStateChanged -= HandleAuthStateChanged;
        }
        
        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================
        
        private async void OnLoginClicked()
        {
            ClearError();
            
            string email = emailInput?.text?.Trim() ?? "";
            string password = passwordInput?.text ?? "";
            bool rememberMe = rememberMeToggle?.isOn ?? false;
            
            // Client-side validation
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email address");
                return;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password");
                return;
            }
            
            SetLoading(true);
            
            try
            {
                var user = await DeskillzAuth.Login(email, password, rememberMe);
                // Success handled by event
            }
            catch (DeskillzAuthException ex)
            {
                ShowError(GetFriendlyErrorMessage(ex.Message));
            }
            catch (Exception ex)
            {
                ShowError("Login failed. Please try again.");
                DeskillzLogger.Error($"[LoginUI] Unexpected error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }
        
        private void OnForgotPasswordClicked()
        {
            OnShowForgotPassword?.Invoke();
            
            if (forgotPasswordPanel != null)
            {
                gameObject.SetActive(false);
                forgotPasswordPanel.SetActive(true);
            }
        }
        
        private void OnSignUpClicked()
        {
            OnShowSignUp?.Invoke();
            
            if (signUpPanel != null)
            {
                gameObject.SetActive(false);
                signUpPanel.SetActive(true);
            }
        }
        
        private void OnSocialLoginClicked(string provider)
        {
            ClearError();
            
            // Social login requires platform-specific implementation
            // This is a placeholder that shows what would happen
            DeskillzLogger.Info($"[LoginUI] Social login requested: {provider}");
            
            ShowError($"{provider} login coming soon. Use email for now.");
            
            // In a real implementation, you would:
            // 1. Call the platform's OAuth SDK (Google Sign-In, Sign in with Apple, etc.)
            // 2. Get the ID token from the provider
            // 3. Call DeskillzAuth.SocialLogin(provider, idToken)
            
            // Example (placeholder):
            // var idToken = await GoogleSignIn.GetIdToken();
            // await DeskillzAuth.SocialLogin("google", idToken);
        }
        
        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================
        
        private void HandleLoginSuccess(AuthUser user)
        {
            DeskillzLogger.Info($"[LoginUI] Login successful: {user.Username}");
            SetLoading(false);
            OnLoginSuccess?.Invoke(user);
        }
        
        private void HandleAuthError(string message)
        {
            SetLoading(false);
            ShowError(GetFriendlyErrorMessage(message));
        }
        
        private void HandleAuthStateChanged(AuthState state, AuthUser user)
        {
            switch (state)
            {
                case AuthState.Authenticating:
                    SetLoading(true);
                    break;
                case AuthState.Authenticated:
                    SetLoading(false);
                    break;
                case AuthState.Error:
                case AuthState.NotAuthenticated:
                    SetLoading(false);
                    break;
            }
        }
        
        // =====================================================================
        // UI HELPERS
        // =====================================================================
        
        private void SetLoading(bool isLoading)
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(isLoading);
            
            if (loginButton != null)
                loginButton.interactable = !isLoading;
            
            if (googleButton != null)
                googleButton.interactable = !isLoading;
            
            if (appleButton != null)
                appleButton.interactable = !isLoading;
            
            if (facebookButton != null)
                facebookButton.interactable = !isLoading;
        }
        
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }
        
        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }
        
        private void ClearInputs()
        {
            if (emailInput != null)
                emailInput.text = "";
            
            if (passwordInput != null)
                passwordInput.text = "";
        }
        
        private string GetFriendlyErrorMessage(string message)
        {
            // Convert technical errors to user-friendly messages
            if (message.Contains("401") || message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                return "Invalid email or password. Please try again.";
            
            if (message.Contains("404"))
                return "Account not found. Please sign up first.";
            
            if (message.Contains("network", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                return "Connection error. Please check your internet and try again.";
            
            if (message.Contains("email", StringComparison.OrdinalIgnoreCase))
                return "Please enter a valid email address.";
            
            if (message.Contains("password", StringComparison.OrdinalIgnoreCase))
                return "Password must be at least 8 characters.";
            
            return message;
        }
        
        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================
        
        /// <summary>
        /// Pre-fill the email field (e.g., from deep link or previous session).
        /// </summary>
        public void SetEmail(string email)
        {
            if (emailInput != null)
                emailInput.text = email;
        }
        
        /// <summary>
        /// Show the login panel.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Hide the login panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
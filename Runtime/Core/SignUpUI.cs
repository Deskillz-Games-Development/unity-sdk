// =============================================================================
// Deskillz SDK for Unity - Sign Up UI
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
    /// Sign up screen UI component.
    /// Attach this to your sign up panel/canvas.
    /// </summary>
    public class SignUpUI : MonoBehaviour
    {
        // =====================================================================
        // UI REFERENCES
        // =====================================================================
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField confirmPasswordInput;
        
        [Header("Buttons")]
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button googleButton;
        [SerializeField] private Button appleButton;
        [SerializeField] private Button facebookButton;
        
        [Header("Toggle")]
        [SerializeField] private Toggle termsToggle;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button privacyButton;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private GameObject loadingOverlay;
        
        [Header("Password Strength")]
        [SerializeField] private Slider passwordStrengthSlider;
        [SerializeField] private TextMeshProUGUI passwordStrengthText;
        
        [Header("Navigation")]
        [SerializeField] private GameObject loginPanel;
        
        [Header("URLs")]
        [SerializeField] private string termsUrl = "https://deskillz.games/terms";
        [SerializeField] private string privacyUrl = "https://deskillz.games/privacy";
        
        // =====================================================================
        // EVENTS
        // =====================================================================
        
        /// <summary>
        /// Fired when sign up succeeds. Use to navigate to lobby.
        /// </summary>
        public event Action<AuthUser> OnSignUpSuccess;
        
        /// <summary>
        /// Fired when user wants to switch to login.
        /// </summary>
        public event Action OnShowLogin;
        
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
            UpdateSignUpButtonState();
        }
        
        // =====================================================================
        // SETUP
        // =====================================================================
        
        private void SetupUI()
        {
            // Setup button listeners
            if (signUpButton != null)
                signUpButton.onClick.AddListener(OnSignUpClicked);
            
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            if (googleButton != null)
                googleButton.onClick.AddListener(() => OnSocialSignUpClicked("google"));
            
            if (appleButton != null)
                appleButton.onClick.AddListener(() => OnSocialSignUpClicked("apple"));
            
            if (facebookButton != null)
                facebookButton.onClick.AddListener(() => OnSocialSignUpClicked("facebook"));
            
            if (termsButton != null)
                termsButton.onClick.AddListener(() => Application.OpenURL(termsUrl));
            
            if (privacyButton != null)
                privacyButton.onClick.AddListener(() => Application.OpenURL(privacyUrl));
            
            // Setup input validation
            if (usernameInput != null)
            {
                usernameInput.onValueChanged.AddListener(_ => {
                    ClearError();
                    UpdateSignUpButtonState();
                });
                usernameInput.characterLimit = 20;
            }
            
            if (emailInput != null)
            {
                emailInput.onValueChanged.AddListener(_ => {
                    ClearError();
                    UpdateSignUpButtonState();
                });
            }
            
            if (passwordInput != null)
            {
                passwordInput.onValueChanged.AddListener(OnPasswordChanged);
                passwordInput.contentType = TMP_InputField.ContentType.Password;
            }
            
            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.onValueChanged.AddListener(_ => {
                    ClearError();
                    UpdateSignUpButtonState();
                });
                confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
            }
            
            if (termsToggle != null)
                termsToggle.onValueChanged.AddListener(_ => UpdateSignUpButtonState());
            
            // Hide loading initially
            SetLoading(false);
            UpdatePasswordStrength("");
        }
        
        private void SubscribeToEvents()
        {
            DeskillzAuth.OnSignUpSuccess += HandleSignUpSuccess;
            DeskillzAuth.OnAuthError += HandleAuthError;
            DeskillzAuth.OnAuthStateChanged += HandleAuthStateChanged;
        }
        
        private void UnsubscribeFromEvents()
        {
            DeskillzAuth.OnSignUpSuccess -= HandleSignUpSuccess;
            DeskillzAuth.OnAuthError -= HandleAuthError;
            DeskillzAuth.OnAuthStateChanged -= HandleAuthStateChanged;
        }
        
        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================
        
        private async void OnSignUpClicked()
        {
            ClearError();
            
            string username = usernameInput?.text?.Trim() ?? "";
            string email = emailInput?.text?.Trim() ?? "";
            string password = passwordInput?.text ?? "";
            string confirmPassword = confirmPasswordInput?.text ?? "";
            
            // Client-side validation
            if (!ValidateInputs(username, email, password, confirmPassword))
                return;
            
            SetLoading(true);
            
            try
            {
                var user = await DeskillzAuth.SignUp(email, password, username);
                // Success handled by event
            }
            catch (DeskillzAuthException ex)
            {
                ShowError(GetFriendlyErrorMessage(ex.Message));
            }
            catch (Exception ex)
            {
                ShowError("Sign up failed. Please try again.");
                DeskillzLogger.Error($"[SignUpUI] Unexpected error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }
        
        private void OnLoginClicked()
        {
            OnShowLogin?.Invoke();
            
            if (loginPanel != null)
            {
                gameObject.SetActive(false);
                loginPanel.SetActive(true);
            }
        }
        
        private void OnSocialSignUpClicked(string provider)
        {
            ClearError();
            
            DeskillzLogger.Info($"[SignUpUI] Social sign up requested: {provider}");
            ShowError($"{provider} sign up coming soon. Use email for now.");
            
            // Same as LoginUI - requires platform-specific OAuth implementation
        }
        
        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================
        
        private void HandleSignUpSuccess(AuthUser user)
        {
            DeskillzLogger.Info($"[SignUpUI] Sign up successful: {user.Username}");
            SetLoading(false);
            OnSignUpSuccess?.Invoke(user);
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
                case AuthState.Error:
                case AuthState.NotAuthenticated:
                    SetLoading(false);
                    break;
            }
        }
        
        // =====================================================================
        // VALIDATION
        // =====================================================================
        
        private bool ValidateInputs(string username, string email, string password, string confirmPassword)
        {
            // Username validation
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Please enter a username");
                return false;
            }
            
            if (username.Length < 3)
            {
                ShowError("Username must be at least 3 characters");
                return false;
            }
            
            if (username.Length > 20)
            {
                ShowError("Username cannot exceed 20 characters");
                return false;
            }
            
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    ShowError("Username can only contain letters, numbers, and underscores");
                    return false;
                }
            }
            
            // Email validation
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email address");
                return false;
            }
            
            if (!email.Contains("@") || !email.Contains("."))
            {
                ShowError("Please enter a valid email address");
                return false;
            }
            
            // Password validation
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter a password");
                return false;
            }
            
            if (password.Length < 8)
            {
                ShowError("Password must be at least 8 characters");
                return false;
            }
            
            // Confirm password
            if (password != confirmPassword)
            {
                ShowError("Passwords do not match");
                return false;
            }
            
            // Terms agreement
            if (termsToggle != null && !termsToggle.isOn)
            {
                ShowError("Please agree to the Terms of Service");
                return false;
            }
            
            return true;
        }
        
        private void UpdateSignUpButtonState()
        {
            if (signUpButton == null) return;
            
            bool hasUsername = !string.IsNullOrEmpty(usernameInput?.text);
            bool hasEmail = !string.IsNullOrEmpty(emailInput?.text);
            bool hasPassword = !string.IsNullOrEmpty(passwordInput?.text);
            bool hasConfirm = !string.IsNullOrEmpty(confirmPasswordInput?.text);
            bool acceptedTerms = termsToggle?.isOn ?? true;
            
            signUpButton.interactable = hasUsername && hasEmail && hasPassword && hasConfirm && acceptedTerms;
        }
        
        // =====================================================================
        // PASSWORD STRENGTH
        // =====================================================================
        
        private void OnPasswordChanged(string password)
        {
            ClearError();
            UpdateSignUpButtonState();
            UpdatePasswordStrength(password);
        }
        
        private void UpdatePasswordStrength(string password)
        {
            if (passwordStrengthSlider == null && passwordStrengthText == null)
                return;
            
            int strength = CalculatePasswordStrength(password);
            string strengthLabel = GetPasswordStrengthLabel(strength);
            Color strengthColor = GetPasswordStrengthColor(strength);
            
            if (passwordStrengthSlider != null)
            {
                passwordStrengthSlider.value = strength / 4f;
                
                // Update slider color
                var fill = passwordStrengthSlider.fillRect?.GetComponent<Image>();
                if (fill != null)
                    fill.color = strengthColor;
            }
            
            if (passwordStrengthText != null)
            {
                passwordStrengthText.text = string.IsNullOrEmpty(password) ? "" : strengthLabel;
                passwordStrengthText.color = strengthColor;
            }
        }
        
        private int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;
            
            int strength = 0;
            
            // Length checks
            if (password.Length >= 8) strength++;
            if (password.Length >= 12) strength++;
            
            // Complexity checks
            bool hasLower = false, hasUpper = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                if (char.IsLower(c)) hasLower = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }
            
            if (hasLower && hasUpper) strength++;
            if (hasDigit) strength++;
            if (hasSpecial) strength++;
            
            return Mathf.Clamp(strength, 0, 4);
        }
        
        private string GetPasswordStrengthLabel(int strength)
        {
            return strength switch
            {
                0 => "Very Weak",
                1 => "Weak",
                2 => "Fair",
                3 => "Strong",
                4 => "Very Strong",
                _ => ""
            };
        }
        
        private Color GetPasswordStrengthColor(int strength)
        {
            return strength switch
            {
                0 => Color.red,
                1 => new Color(1f, 0.5f, 0f), // Orange
                2 => Color.yellow,
                3 => new Color(0.5f, 1f, 0f), // Light green
                4 => Color.green,
                _ => Color.white
            };
        }
        
        // =====================================================================
        // UI HELPERS
        // =====================================================================
        
        private void SetLoading(bool isLoading)
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(isLoading);
            
            if (signUpButton != null)
                signUpButton.interactable = !isLoading && (termsToggle?.isOn ?? true);
            
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
            if (usernameInput != null) usernameInput.text = "";
            if (emailInput != null) emailInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            if (termsToggle != null) termsToggle.isOn = false;
        }
        
        private string GetFriendlyErrorMessage(string message)
        {
            if (message.Contains("409") || message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
                return "Email or username already taken. Please try another.";
            
            if (message.Contains("email", StringComparison.OrdinalIgnoreCase) && 
                message.Contains("taken", StringComparison.OrdinalIgnoreCase))
                return "This email is already registered. Try logging in instead.";
            
            if (message.Contains("username", StringComparison.OrdinalIgnoreCase) && 
                message.Contains("taken", StringComparison.OrdinalIgnoreCase))
                return "This username is already taken. Please choose another.";
            
            if (message.Contains("network", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                return "Connection error. Please check your internet and try again.";
            
            return message;
        }
        
        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================
        
        /// <summary>
        /// Show the sign up panel.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Hide the sign up panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
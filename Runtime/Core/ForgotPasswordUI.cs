// =============================================================================
// Deskillz SDK for Unity - Forgot Password UI
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
    /// Forgot password screen UI component.
    /// Attach this to your forgot password panel/canvas.
    /// </summary>
    public class ForgotPasswordUI : MonoBehaviour
    {
        // =====================================================================
        // UI REFERENCES
        // =====================================================================
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField emailInput;
        
        [Header("Buttons")]
        [SerializeField] private Button sendResetButton;
        [SerializeField] private Button backToLoginButton;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private TextMeshProUGUI successText;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private TextMeshProUGUI instructionsText;
        
        [Header("Navigation")]
        [SerializeField] private GameObject loginPanel;
        
        // =====================================================================
        // EVENTS
        // =====================================================================
        
        /// <summary>
        /// Fired when password reset email is sent successfully.
        /// </summary>
        public event Action OnResetEmailSent;
        
        /// <summary>
        /// Fired when user wants to go back to login.
        /// </summary>
        public event Action OnBackToLogin;
        
        // =====================================================================
        // STATE
        // =====================================================================
        
        private bool _emailSent = false;
        
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
            ResetState();
        }
        
        // =====================================================================
        // SETUP
        // =====================================================================
        
        private void SetupUI()
        {
            // Setup button listeners
            if (sendResetButton != null)
                sendResetButton.onClick.AddListener(OnSendResetClicked);
            
            if (backToLoginButton != null)
                backToLoginButton.onClick.AddListener(OnBackToLoginClicked);
            
            // Setup input validation
            if (emailInput != null)
                emailInput.onValueChanged.AddListener(_ => ClearMessages());
            
            // Set instructions text
            if (instructionsText != null)
                instructionsText.text = "Enter your email address and we'll send you a link to reset your password.";
            
            // Hide loading initially
            SetLoading(false);
        }
        
        private void SubscribeToEvents()
        {
            DeskillzAuth.OnPasswordResetSent += HandlePasswordResetSent;
            DeskillzAuth.OnAuthError += HandleAuthError;
        }
        
        private void UnsubscribeFromEvents()
        {
            DeskillzAuth.OnPasswordResetSent -= HandlePasswordResetSent;
            DeskillzAuth.OnAuthError -= HandleAuthError;
        }
        
        // =====================================================================
        // BUTTON HANDLERS
        // =====================================================================
        
        private async void OnSendResetClicked()
        {
            ClearMessages();
            
            string email = emailInput?.text?.Trim() ?? "";
            
            // Client-side validation
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email address");
                return;
            }
            
            if (!email.Contains("@") || !email.Contains("."))
            {
                ShowError("Please enter a valid email address");
                return;
            }
            
            SetLoading(true);
            
            try
            {
                await DeskillzAuth.ForgotPassword(email);
                // Success handled by event
            }
            catch (DeskillzAuthException ex)
            {
                ShowError(GetFriendlyErrorMessage(ex.Message));
            }
            catch (Exception ex)
            {
                ShowError("Failed to send reset email. Please try again.");
                DeskillzLogger.Error($"[ForgotPasswordUI] Unexpected error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }
        
        private void OnBackToLoginClicked()
        {
            OnBackToLogin?.Invoke();
            
            if (loginPanel != null)
            {
                gameObject.SetActive(false);
                loginPanel.SetActive(true);
            }
        }
        
        // =====================================================================
        // EVENT HANDLERS
        // =====================================================================
        
        private void HandlePasswordResetSent()
        {
            _emailSent = true;
            SetLoading(false);
            ShowSuccess("Password reset email sent! Check your inbox and spam folder.");
            
            // Update button text
            if (sendResetButton != null)
            {
                var buttonText = sendResetButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "Resend Email";
            }
            
            OnResetEmailSent?.Invoke();
        }
        
        private void HandleAuthError(string message)
        {
            SetLoading(false);
            ShowError(GetFriendlyErrorMessage(message));
        }
        
        // =====================================================================
        // UI HELPERS
        // =====================================================================
        
        private void SetLoading(bool isLoading)
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(isLoading);
            
            if (sendResetButton != null)
                sendResetButton.interactable = !isLoading;
        }
        
        private void ShowError(string message)
        {
            if (successText != null)
                successText.gameObject.SetActive(false);
            
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }
        
        private void ShowSuccess(string message)
        {
            if (errorText != null)
                errorText.gameObject.SetActive(false);
            
            if (successText != null)
            {
                successText.text = message;
                successText.gameObject.SetActive(true);
            }
        }
        
        private void ClearMessages()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
            
            if (successText != null)
            {
                successText.text = "";
                successText.gameObject.SetActive(false);
            }
        }
        
        private void ResetState()
        {
            _emailSent = false;
            ClearMessages();
            
            if (emailInput != null)
                emailInput.text = "";
            
            // Reset button text
            if (sendResetButton != null)
            {
                var buttonText = sendResetButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = "Send Reset Link";
            }
        }
        
        private string GetFriendlyErrorMessage(string message)
        {
            if (message.Contains("404") || message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return "No account found with this email address.";
            
            if (message.Contains("network", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                return "Connection error. Please check your internet and try again.";
            
            if (message.Contains("rate", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("too many", StringComparison.OrdinalIgnoreCase))
                return "Too many requests. Please wait a few minutes before trying again.";
            
            return message;
        }
        
        // =====================================================================
        // PUBLIC METHODS
        // =====================================================================
        
        /// <summary>
        /// Pre-fill the email field.
        /// </summary>
        public void SetEmail(string email)
        {
            if (emailInput != null)
                emailInput.text = email;
        }
        
        /// <summary>
        /// Show the forgot password panel.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Hide the forgot password panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
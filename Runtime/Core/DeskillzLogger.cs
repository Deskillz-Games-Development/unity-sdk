// =============================================================================
// Deskillz SDK for Unity
// Copyright (c) 2024 Deskillz.Games. All rights reserved.
// =============================================================================

using System;
using System.Text;
using UnityEngine;

namespace Deskillz
{
    /// <summary>
    /// Centralized logging utility for the Deskillz SDK.
    /// Provides configurable log levels and formatted output.
    /// </summary>
    public static class DeskillzLogger
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        private static LogLevel _logLevel = LogLevel.Info;
        private static bool _includeTimestamp = true;
        private static bool _includeStackTrace = false;
        private static string _prefix = "[Deskillz]";

        /// <summary>
        /// Current log level. Messages below this level will be ignored.
        /// </summary>
        public static LogLevel Level
        {
            get => _logLevel;
            set => _logLevel = value;
        }

        /// <summary>
        /// Whether to include timestamps in log messages.
        /// </summary>
        public static bool IncludeTimestamp
        {
            get => _includeTimestamp;
            set => _includeTimestamp = value;
        }

        /// <summary>
        /// Whether to include stack traces for errors.
        /// </summary>
        public static bool IncludeStackTrace
        {
            get => _includeStackTrace;
            set => _includeStackTrace = value;
        }

        /// <summary>
        /// Prefix for all log messages.
        /// </summary>
        public static string Prefix
        {
            get => _prefix;
            set => _prefix = value ?? "[Deskillz]";
        }

        // =============================================================================
        // LOGGING METHODS
        // =============================================================================

        /// <summary>
        /// Log an error message. Always displayed unless logging is disabled.
        /// </summary>
        public static void Error(string message, Exception exception = null)
        {
            if (_logLevel < LogLevel.Error) return;
            
            var formattedMessage = FormatMessage("ERROR", message);
            
            if (exception != null)
            {
                formattedMessage += $"\nException: {exception.GetType().Name}: {exception.Message}";
                if (_includeStackTrace)
                {
                    formattedMessage += $"\n{exception.StackTrace}";
                }
            }
            
            Debug.LogError(formattedMessage);
        }

        /// <summary>
        /// Log an error message with format parameters.
        /// </summary>
        public static void Error(string format, params object[] args)
        {
            if (_logLevel < LogLevel.Error) return;
            Error(string.Format(format, args));
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warning(string message)
        {
            if (_logLevel < LogLevel.Warning) return;
            Debug.LogWarning(FormatMessage("WARN", message));
        }

        /// <summary>
        /// Log a warning message with format parameters.
        /// </summary>
        public static void Warning(string format, params object[] args)
        {
            if (_logLevel < LogLevel.Warning) return;
            Warning(string.Format(format, args));
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public static void Info(string message)
        {
            if (_logLevel < LogLevel.Info) return;
            Debug.Log(FormatMessage("INFO", message));
        }

        /// <summary>
        /// Log an info message with format parameters.
        /// </summary>
        public static void Info(string format, params object[] args)
        {
            if (_logLevel < LogLevel.Info) return;
            Info(string.Format(format, args));
        }

        /// <summary>
        /// Log a debug message. Only displayed when log level is Debug or higher.
        /// </summary>
        public static void Debug(string message)
        {
            if (_logLevel < LogLevel.Debug) return;
            UnityEngine.Debug.Log(FormatMessage("DEBUG", message));
        }

        /// <summary>
        /// Log a debug message with format parameters.
        /// </summary>
        public static void Debug(string format, params object[] args)
        {
            if (_logLevel < LogLevel.Debug) return;
            Debug(string.Format(format, args));
        }

        /// <summary>
        /// Log a verbose message. Only displayed at highest log level.
        /// </summary>
        public static void Verbose(string message)
        {
            if (_logLevel < LogLevel.Verbose) return;
            UnityEngine.Debug.Log(FormatMessage("VERBOSE", message));
        }

        /// <summary>
        /// Log a verbose message with format parameters.
        /// </summary>
        public static void Verbose(string format, params object[] args)
        {
            if (_logLevel < LogLevel.Verbose) return;
            Verbose(string.Format(format, args));
        }

        // =============================================================================
        // SPECIALIZED LOGGING
        // =============================================================================

        /// <summary>
        /// Log a network request.
        /// </summary>
        internal static void LogRequest(string method, string url, string body = null)
        {
            if (_logLevel < LogLevel.Debug) return;

            var sb = new StringBuilder();
            sb.AppendLine($"→ {method} {url}");
            
            if (!string.IsNullOrEmpty(body) && _logLevel >= LogLevel.Verbose)
            {
                sb.AppendLine($"Body: {TruncateForLog(body, 500)}");
            }
            
            UnityEngine.Debug.Log(FormatMessage("HTTP", sb.ToString()));
        }

        /// <summary>
        /// Log a network response.
        /// </summary>
        internal static void LogResponse(string method, string url, int statusCode, string body = null, float durationMs = 0)
        {
            if (_logLevel < LogLevel.Debug) return;

            var sb = new StringBuilder();
            sb.Append($"← {method} {url} [{statusCode}]");
            
            if (durationMs > 0)
            {
                sb.Append($" ({durationMs:F0}ms)");
            }
            
            if (!string.IsNullOrEmpty(body) && _logLevel >= LogLevel.Verbose)
            {
                sb.AppendLine();
                sb.Append($"Response: {TruncateForLog(body, 500)}");
            }
            
            if (statusCode >= 200 && statusCode < 300)
            {
                UnityEngine.Debug.Log(FormatMessage("HTTP", sb.ToString()));
            }
            else
            {
                Debug.LogWarning(FormatMessage("HTTP", sb.ToString()));
            }
        }

        /// <summary>
        /// Log a WebSocket event.
        /// </summary>
        internal static void LogWebSocket(string eventType, string message = null)
        {
            if (_logLevel < LogLevel.Debug) return;

            var sb = new StringBuilder();
            sb.Append($"⚡ {eventType}");
            
            if (!string.IsNullOrEmpty(message) && _logLevel >= LogLevel.Verbose)
            {
                sb.Append($": {TruncateForLog(message, 300)}");
            }
            
            UnityEngine.Debug.Log(FormatMessage("WS", sb.ToString()));
        }

        /// <summary>
        /// Log an SDK event.
        /// </summary>
        internal static void LogEvent(string eventName, string details = null)
        {
            if (_logLevel < LogLevel.Debug) return;

            var message = $"📢 {eventName}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            
            UnityEngine.Debug.Log(FormatMessage("EVENT", message));
        }

        /// <summary>
        /// Log match-related activity.
        /// </summary>
        internal static void LogMatch(string action, string matchId, string details = null)
        {
            if (_logLevel < LogLevel.Info) return;

            var message = $"🎮 {action}";
            if (!string.IsNullOrEmpty(matchId))
            {
                message += $" (Match: {matchId.Substring(0, Math.Min(8, matchId.Length))}...)";
            }
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            
            UnityEngine.Debug.Log(FormatMessage("MATCH", message));
        }

        /// <summary>
        /// Log score-related activity.
        /// </summary>
        internal static void LogScore(string action, int score, string details = null)
        {
            if (_logLevel < LogLevel.Info) return;

            var message = $"🏆 {action}: {score}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - {details}";
            }
            
            UnityEngine.Debug.Log(FormatMessage("SCORE", message));
        }

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        private static string FormatMessage(string level, string message)
        {
            var sb = new StringBuilder();
            
            sb.Append(_prefix);
            sb.Append(' ');
            
            if (_includeTimestamp)
            {
                sb.Append('[');
                sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                sb.Append("] ");
            }
            
            sb.Append('[');
            sb.Append(level);
            sb.Append("] ");
            
            sb.Append(message);
            
            return sb.ToString();
        }

        private static string TruncateForLog(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "...";
        }

        // =============================================================================
        // CONFIGURATION HELPERS
        // =============================================================================

        /// <summary>
        /// Configure logging for development (verbose output).
        /// </summary>
        public static void ConfigureForDevelopment()
        {
            Level = LogLevel.Verbose;
            IncludeTimestamp = true;
            IncludeStackTrace = true;
        }

        /// <summary>
        /// Configure logging for production (minimal output).
        /// </summary>
        public static void ConfigureForProduction()
        {
            Level = LogLevel.Warning;
            IncludeTimestamp = false;
            IncludeStackTrace = false;
        }

        /// <summary>
        /// Disable all logging.
        /// </summary>
        public static void Disable()
        {
            Level = LogLevel.None;
        }

        /// <summary>
        /// Reset to default configuration.
        /// </summary>
        public static void Reset()
        {
            Level = LogLevel.Info;
            IncludeTimestamp = true;
            IncludeStackTrace = false;
            Prefix = "[Deskillz]";
        }
    }
}

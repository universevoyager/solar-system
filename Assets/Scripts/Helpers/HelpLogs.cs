using UnityEngine;

namespace Assets.Scripts.Helpers.Debugging
{
    /// <summary>
    /// Lightweight wrapper for Unity console logging with a caller tag.
    /// </summary>
    public static class HelpLogs
    {
        /// <summary>
        /// Log a standard message.
        /// </summary>
        public static void Log(string _caller, string _message)
        {
            Debug.Log($"[{_caller}] {_message}");
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warn(string _caller, string _message)
        {
            Debug.LogWarning($"[{_caller}] {_message}");
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void Error(string _caller, string _message)
        {
            Debug.LogError($"[{_caller}] {_message}");
        }
    }
}
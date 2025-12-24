using UnityEngine;

namespace Assets.Scripts.Helpers.Debugging
{
    public static class HelpLogs
    {
        public static void Log(string _caller, string _message)
        {
            Debug.Log($"[{_caller}] {_message}");
        }

        public static void Warn(string _caller, string _message)
        {
            Debug.LogWarning($"[{_caller}] {_message}");
        }

        public static void Error(string _caller, string _message)
        {
            Debug.LogError($"[{_caller}] {_message}");
        }
    }
}
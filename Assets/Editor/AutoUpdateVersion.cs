#if UNITY_EDITOR
using System;
using System.Globalization;
using Assets.Scripts.Helpers.Debugging;
using UnityEditor;
using UnityEngine;

public static class AutoUpdateVersion
{
    // Month/day without leading zeros
    const string Format = "yyyy.M.d";

    [InitializeOnLoadMethod]
    static void Init()
    {
        // Avoid AssetDatabase ops during InitializeOnLoad*
        EditorApplication.delayCall += UpdateIfNeeded;
    }

    static void UpdateIfNeeded()
    {
        DateTime _nowPresentDate = DateTime.Today;

        string _current = PlayerSettings.bundleVersion?.Trim();
        if (!DateTime.TryParseExact(_current, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var _out_currentDate))
        {
            _out_currentDate = DateTime.MinValue;
        }

        if (_nowPresentDate > _out_currentDate)
        {
            PlayerSettings.bundleVersion = _nowPresentDate.ToString(Format, CultureInfo.InvariantCulture);
            AssetDatabase.SaveAssets();
            HelpLogs.Log("[Auto Version]", $"Updated product version to {Application.version}");
        }
    }
}
#endif
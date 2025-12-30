#if UNITY_EDITOR
using System;
using System.Globalization;
using Assets.Scripts.Helpers.Debugging;
using UnityEditor;
using UnityEngine;

public static class AutoDateVersion
{
    const string Format = "yyyy.MM.dd";

    [InitializeOnLoadMethod]
    static void Init()
    {
        // Avoid AssetDatabase ops during InitializeOnLoad* (because optimizations, even in da editor yeeeahhh)
        // run once after editor finishes loading.
        EditorApplication.delayCall += UpdateIfNeeded;
    }

    static void UpdateIfNeeded()
    {
        DateTime today = DateTime.Today;

        string current = PlayerSettings.bundleVersion?.Trim();
        if (!DateTime.TryParseExact(current, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var currentDate))
        {
            currentDate = DateTime.MinValue;
        }

        if (today > currentDate)
        {
            PlayerSettings.bundleVersion = today.ToString(Format, CultureInfo.InvariantCulture);
            AssetDatabase.SaveAssets();
            HelpLogs.Log("[Auto Version]", $"Updated product version to {Application.version}");
        }
    }
}
#endif
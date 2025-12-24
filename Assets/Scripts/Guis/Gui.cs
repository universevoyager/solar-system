using Assets.Scripts.Helpers.Debugging;
using UnityEngine;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// main graphical user interface for all canvas menus / panels
    /// </summary>
    public static partial class Gui
    {
        private static Canvas canvas = null;


        public static void Initialize()
        {
            if(canvas != null)
            {
                Debug.LogWarning($"[GUI] Already initialized");
                return;
            }

            canvas = GameObject.FindFirstObjectByType<Canvas>();
            DeallocatePanels();
            AllocatePanels(canvas);

            HelpLogs.Log("Gui", "Initialized");
        }
    }
}
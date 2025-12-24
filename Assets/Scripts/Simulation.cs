using Assets.Scripts.Helpers.Debugging;
using UnityEngine;

namespace Assets.Scripts
{
    public static class Simulation
    {
        public static float ScaleRatio {get; private set;} = 1;

#region Start / Initialize
        /// <summary>
        /// 
        /// </summary>
        /// <param name="_whoAwakensMe"></param>
        public static void Start(OnAwake _whoAwakensMe)
        {
            Guis.Gui.Initialize();
            Guis.Gui.Show(_panelToShow: Guis.Panel.Start_Intro);
            HelpLogs.Log("Simulation", "Started");
        }
#endregion
    }
}
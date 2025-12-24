using UnityEngine;

namespace Assets.Scripts
{
    [DisallowMultipleComponent]
    public class OnAwake : MonoBehaviour
    {
#region Awake (by Unity)
        /// <summary>
        /// When app loads this is the first awake() that is being called by unity engine
        /// This awake() will start simulation and remove itself
        /// </summary>
        void Awake()
        {
            Helpers.Debugging.HelpLogs.Log(name, "Awaken");
            Simulation.Start(_whoAwakensMe: this);
            Destroy(gameObject);
        }
#endregion
    }
}
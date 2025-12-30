using UnityEngine;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Root namespace for GUI helpers and UI utilities.
    /// </summary>
    public static partial class Gui
    {
        #region Initialization
        /// <summary>
        /// Allocate cached panel and runtime control widgets.
        /// </summary>
        public static void Initialize()
        {
            EnsureRuntimeWidgets();
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Clear cached panel and runtime control widgets.
        /// </summary>
        public static void UnInitialize()
        {
            DeallocateInteractionWidgets();
        }
        #endregion
    }
}

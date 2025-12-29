using UnityEngine;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Root namespace for GUI helpers and UI utilities.
    /// </summary>
    public static partial class Gui
    {
#region Initialize
        /// <summary>
        /// Allocate cached panel and runtime control widgets.
        /// </summary>
        public static void Initialize()
        {
            AllocateInteractionWidgets();
        }
#endregion

#region UnInitialize
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

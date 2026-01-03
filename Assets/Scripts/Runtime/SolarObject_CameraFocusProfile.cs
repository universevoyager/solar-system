#nullable enable
using System;

namespace Assets.Scripts.Runtime
{
    public sealed partial class SolarObject
    {
        #region Camera Focus Profile
        /// <summary>
        /// Focus profile used by the camera to select zoom ranges.
        /// </summary>
        [Serializable]
        public enum CameraFocusProfile
        {
            Auto,
            Star,
            Moon,
            DwarfPlanet,
            Terrestrial,
            GasGiant,
            IceGiant,
        }
        #endregion
    }
}

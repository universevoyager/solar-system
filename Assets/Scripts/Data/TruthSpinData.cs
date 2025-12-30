#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Spin parameters for truth data.
    /// </summary>
    [Serializable]
    public sealed class TruthSpinData
    {
        #region Properties
        // Sidereal rotation period (days).
        [JsonProperty("sidereal_rotation_period_days")]
        public double? SiderealRotationPeriodDays { get; set; }

        // Sidereal rotation period (hours).
        [JsonProperty("sidereal_rotation_period_hours")]
        public double? SiderealRotationPeriodHours { get; set; }

        // Spin direction multiplier (1 = prograde, -1 = retrograde).
        [JsonProperty("spin_direction")]
        public double? SpinDirection { get; set; }

        // Axial tilt in degrees.
        [JsonProperty("axial_tilt_deg")]
        public double? AxialTiltDeg { get; set; }
        #endregion
    }
}

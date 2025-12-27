#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Optional spawn overrides for position, scale, and initial angle.
    /// </summary>
    [Serializable]
    public sealed class SpawnData
    {
        // Optional world-space position override (Unity units).
        [JsonProperty("position_unity")]
        public double[]? PositionUnity { get; set; }

        // Optional local scale override (Unity units).
        [JsonProperty("scale_unity")]
        public double[]? ScaleUnity { get; set; }

        // Initial orbit angle for circular models (degrees).
        [JsonProperty("initial_angle_deg")]
        public double? InitialAngleDeg { get; set; }
    }
}
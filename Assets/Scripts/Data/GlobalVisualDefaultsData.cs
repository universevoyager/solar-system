#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Global visual defaults applied to all solar objects.
    /// </summary>
    [Serializable]
    public sealed class GlobalVisualDefaultsData
    {
        // Base conversion from kilometers to Unity units.
        [JsonProperty("distance_km_per_unity_unit")]
        public double DistanceKmPerUnityUnit { get; set; } = 1_000_000.0;

        // Global orbit distance scale.
        [JsonProperty("global_distance_multiplier")]
        public double GlobalDistanceMultiplier { get; set; } = 1.0;

        // Global radius scale.
        [JsonProperty("global_radius_multiplier")]
        public double GlobalRadiusMultiplier { get; set; } = 1.0;

        // Default orbit path segments for line rendering.
        [JsonProperty("orbit_path_segments_default")]
        public int OrbitPathSegmentsDefault { get; set; } = 256;

        // Extra clearance to prevent moon overlap in Unity units.
        [JsonProperty("moon_clearance_unity")]
        public float MoonClearanceUnity { get; set; } = 0.02f;
    }
}
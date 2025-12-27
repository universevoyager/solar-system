#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Clamp ranges for visual multipliers.
    /// </summary>
    [Serializable]
    public sealed class VisualClampsData
    {
        // Optional dataset comment.
        [JsonProperty("__comment")]
        public string? Comment { get; set; }

        // Minimum allowed radius multiplier.
        [JsonProperty("radius_multiplier_min")]
        public double RadiusMultiplierMin { get; set; } = 0.01;

        // Maximum allowed radius multiplier.
        [JsonProperty("radius_multiplier_max")]
        public double RadiusMultiplierMax { get; set; } = 1000.0;

        // Minimum allowed distance multiplier.
        [JsonProperty("distance_multiplier_min")]
        public double DistanceMultiplierMin { get; set; } = 0.01;

        // Maximum allowed distance multiplier.
        [JsonProperty("distance_multiplier_max")]
        public double DistanceMultiplierMax { get; set; } = 1000.0;
    }
}
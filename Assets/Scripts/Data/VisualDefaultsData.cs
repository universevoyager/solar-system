#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Per-object visual overrides.
    /// </summary>
    [Serializable]
    public sealed class VisualDefaultsData
    {
        // Per-object radius multiplier.
        [JsonProperty("radius_multiplier")]
        public double RadiusMultiplier { get; set; } = 1.0;

        // Per-object distance multiplier.
        [JsonProperty("distance_multiplier")]
        public double DistanceMultiplier { get; set; } = 1.0;
    }
}
#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Root dataset container.
    /// </summary>
    [Serializable]
    public sealed class SolarSystemData
    {
        // Raw metadata block from the dataset.
        [JsonProperty("meta")]
        public object? Meta { get; set; }

        // Shared visual defaults for all objects.
        [JsonProperty("global_visual_defaults")]
        public GlobalVisualDefaultsData? GlobalVisualDefaults { get; set; }

        // All solar object entries.
        [JsonProperty("solar_objects")]
        public List<SolarObjectData>? SolarObjects { get; set; }
    }
}
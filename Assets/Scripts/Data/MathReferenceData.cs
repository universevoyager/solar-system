#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Reference lists for supported orbit and spin models.
    /// </summary>
    [Serializable]
    public sealed class MathReferenceData
    {
        // Optional dataset comment.
        [JsonProperty("__comment")]
        public string? Comment { get; set; }

        // Supported orbit models and their required fields.
        [JsonProperty("orbit_models")]
        public Dictionary<string, List<string>>? OrbitModels { get; set; }

        // Supported spin model fields.
        [JsonProperty("spin_model")]
        public List<string>? SpinModel { get; set; }
    }
}
#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Source attribution entry for dataset metadata.
    /// </summary>
    [Serializable]
    public sealed class SourceData
    {
        // Source name or organization.
        [JsonProperty("name")]
        public string? Name { get; set; }

        // Source URL for attribution.
        [JsonProperty("url")]
        public string? Url { get; set; }
    }
}
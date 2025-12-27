#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Dataset metadata, units, and sources.
    /// </summary>
    [Serializable]
    public sealed class MetaData
    {
        // Dataset schema version string.
        [JsonProperty("schema_version")]
        public string? SchemaVersion { get; set; }

        // Human-readable dataset name.
        [JsonProperty("dataset_name")]
        public string? DatasetName { get; set; }

        // Free-form notes about the dataset.
        [JsonProperty("notes")]
        public List<string>? Notes { get; set; }

        // Unit definitions for values used in the dataset.
        [JsonProperty("units")]
        public Dictionary<string, string>? Units { get; set; }

        // Named constants used by the dataset.
        [JsonProperty("constants")]
        public Dictionary<string, double>? Constants { get; set; }

        // Coordinate system conventions for the dataset.
        [JsonProperty("coordinate_conventions")]
        public CoordinateConventionsData? CoordinateConventions { get; set; }

        // Source attribution list.
        [JsonProperty("sources")]
        public List<SourceData>? Sources { get; set; }
    }
}
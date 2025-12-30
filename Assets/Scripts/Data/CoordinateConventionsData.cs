#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Coordinate system conventions and axis hints.
    /// </summary>
    [Serializable]
    public sealed class CoordinateConventionsData
    {
        #region Properties
        // Optional dataset comment.
        [JsonProperty("__comment")]
        public string? Comment { get; set; }

        // Axis label treated as "up" in Unity coordinates.
        [JsonProperty("unity_up_axis")]
        public string? UnityUpAxis { get; set; }

        // Axis label used for initial spawn alignment.
        [JsonProperty("unity_spawn_axis")]
        public string? UnitySpawnAxis { get; set; }

        // Axis labels that define the orbit plane (e.g., ["x", "z"]).
        [JsonProperty("orbit_plane_axes")]
        public List<string>? OrbitPlaneAxes { get; set; }
        #endregion
    }
}
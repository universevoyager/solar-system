#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Per-object dataset entry.
    /// </summary>
    [Serializable]
    public sealed class SolarObjectData
    {
        #region Properties
        // Unique object id (used for lookup and prefab matching).
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        // Object type (e.g., planet, moon, dwarf_planet).
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        // Optional display name shown in the scene.
        [JsonProperty("display_name")]
        public string? DisplayName { get; set; }

        [JsonProperty("order_from_sun")]
        public int? OrderFromSun { get; set; }

        // Parent object id for orbiting solar objects.
        [JsonProperty("primary_id")]
        public string? PrimaryId { get; set; }

        // Marks the reference solar object (Sun).
        [JsonProperty("is_reference")]
        public bool IsReference { get; set; } = false;

        // Optional flag for hypothetical objects (e.g., Planet X).
        [JsonProperty("is_hypothetical")]
        public bool IsHypothetical { get; set; } = false;

        [JsonProperty("truth_physical")]
        public TruthPhysicalData? TruthPhysical { get; set; }

        [JsonProperty("truth_spin")]
        public TruthSpinData? TruthSpin { get; set; }

        [JsonProperty("truth_orbit")]
        public TruthOrbitData? TruthOrbit { get; set; }

        // Per-object visual overrides.
        [JsonProperty("visual_defaults")]
        public VisualDefaultsData? VisualDefaults { get; set; }

        // Optional spawn overrides.
        [JsonProperty("spawn")]
        public SpawnData? Spawn { get; set; }

        // Optional per-object override for aligning moon orbits to primary tilt.
        [JsonProperty("align_to_primary_tilt")]
        public bool? AlignOrbitToPrimaryTilt { get; set; }

        // Optional per-object override for tidal locking.
        [JsonProperty("tidal_lock")]
        public bool? TidalLock { get; set; }
        #endregion
    }
}
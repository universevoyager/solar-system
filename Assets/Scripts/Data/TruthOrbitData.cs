#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Orbital parameters for truth data.
    /// </summary>
    [Serializable]
    public sealed class TruthOrbitData
    {
        #region Properties
        // Orbit model name (keplerian only).
        [JsonProperty("model")]
        public string Model { get; set; } = "keplerian";

        // Orbital period (days).
        [JsonProperty("orbital_period_days")]
        public double? OrbitalPeriodDays { get; set; }

        // Orbital period (years).
        [JsonProperty("orbital_period_years")]
        public double? OrbitalPeriodYears { get; set; }

        // Semi-major axis (kilometers).
        [JsonProperty("semi_major_axis_km")]
        public double? SemiMajorAxisKm { get; set; }

        // Semi-major axis (astronomical units).
        [JsonProperty("semi_major_axis_AU")]
        public double? SemiMajorAxisAU { get; set; }

        // Keplerian extras (required when model == "keplerian").
        [JsonProperty("eccentricity")]
        public double? Eccentricity { get; set; }

        [JsonProperty("inclination_deg")]
        public double? InclinationDeg { get; set; }

        [JsonProperty("longitude_ascending_node_deg")]
        public double? LongitudeAscendingNodeDeg { get; set; }

        [JsonProperty("argument_periapsis_deg")]
        public double? ArgumentPeriapsisDeg { get; set; }

        [JsonProperty("mean_anomaly_deg")]
        public double? MeanAnomalyDeg { get; set; }
        #endregion
    }
}
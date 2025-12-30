#nullable enable
using System;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    /// <summary>
    /// Physical parameters for truth data.
    /// </summary>
    [Serializable]
    public sealed class TruthPhysicalData
    {
        #region Properties
        // Mean radius in kilometers.
        [JsonProperty("mean_radius_km")]
        public double? MeanRadiusKm { get; set; }
        #endregion
    }
}
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Data;
using Assets.Scripts.Helpers.Debugging;
using Assets.Scripts.Loading;

namespace Assets.Scripts.Runtime
{
    public sealed partial class SolarSystemSimulator
    {
        #region Debug Logging
        /// <summary>
        /// Log the resolved spawn data for each solar object once at startup.
        /// </summary>
        private void LogSpawnedSolarObjects(SolarSystemJsonLoader.Result _db)
        {
            if (!logSpawnedSolarObjectData || spawnDataLogged)
            {
                return;
            }

            spawnDataLogged = true;

            List<SolarObjectData> _sorted = new List<SolarObjectData>(_db.ById.Values);
            _sorted.Sort((_a, _b) =>
            {
                int _ar = _a.IsReference ? 0 : 1;
                int _br = _b.IsReference ? 0 : 1;
                if (_ar != _br)
                {
                    return _ar.CompareTo(_br);
                }

                int _ao = _a.OrderFromSun ?? int.MaxValue;
                int _bo = _b.OrderFromSun ?? int.MaxValue;
                int _cmp = _ao.CompareTo(_bo);
                if (_cmp != 0)
                {
                    return _cmp;
                }

                return string.Compare(_a.Id, _b.Id, StringComparison.OrdinalIgnoreCase);
            });

            for (int _i = 0; _i < _sorted.Count; _i++)
            {
                SolarObjectData _data = _sorted[_i];
                HelpLogs.Log("Simulator", BuildSpawnLog(_data));
            }
        }

        /// <summary>
        /// Build a debug log line for the dataset values used by a solar object.
        /// </summary>
        private string BuildSpawnLog(SolarObjectData _data)
        {
            string _name = string.IsNullOrWhiteSpace(_data.DisplayName) ? _data.Id : _data.DisplayName;
            TruthPhysicalData? _physical = _data.TruthPhysical;
            TruthSpinData? _spin = _data.TruthSpin;
            TruthOrbitData? _orbit = _data.TruthOrbit;
            VisualDefaultsData? _visual = _data.VisualDefaults;
            SpawnData? _spawn = _data.Spawn;

            string _spinPeriod = FormatSpinPeriod(_spin);
            string _spinDir = _spin != null ? FormatSpinDirection(_spin.SpinDirection) : "n/a";
            string _tilt = _spin?.AxialTiltDeg.HasValue == true ? $"{_spin.AxialTiltDeg:0.###} deg" : "n/a";
            string _orbitPeriod = FormatOrbitPeriod(_orbit);
            string _semiMajor = FormatSemiMajorAxis(_orbit);
            string _ecc = _orbit?.Eccentricity.HasValue == true ? $"{_orbit.Eccentricity:0.#####}" : "n/a";
            string _inc = _orbit?.InclinationDeg.HasValue == true ? $"{_orbit.InclinationDeg:0.###} deg" : "n/a";
            string _lan = _orbit?.LongitudeAscendingNodeDeg.HasValue == true
                ? $"{_orbit.LongitudeAscendingNodeDeg:0.###} deg"
                : "n/a";
            string _arg = _orbit?.ArgumentPeriapsisDeg.HasValue == true
                ? $"{_orbit.ArgumentPeriapsisDeg:0.###} deg"
                : "n/a";
            string _mean = _orbit?.MeanAnomalyDeg.HasValue == true
                ? $"{_orbit.MeanAnomalyDeg:0.###} deg"
                : "n/a";

            StringBuilder _sb = new StringBuilder(256);
            _sb.AppendLine($"Spawn data: {_name} ({_data.Id})");
            _sb.AppendLine($"Type: {_data.Type} | Primary: {_data.PrimaryId ?? "none"} | Order: {_data.OrderFromSun?.ToString() ?? "n/a"} | Hypothetical: {_data.IsHypothetical}");
            _sb.AppendLine($"Physical: mean_radius_km={_physical?.MeanRadiusKm?.ToString("0.###") ?? "n/a"}");
            _sb.AppendLine($"Spin: period={_spinPeriod} | direction={_spinDir} | axial_tilt={_tilt}");
            _sb.AppendLine($"Orbit: period={_orbitPeriod} | a={_semiMajor} | e={_ecc} | i={_inc} | LAN={_lan} | arg_periapsis={_arg} | mean_anomaly={_mean}");
            string _radiusMult = _visual != null ? _visual.RadiusMultiplier.ToString("0.###") : "n/a";
            string _distanceMult = _visual != null ? _visual.DistanceMultiplier.ToString("0.###") : "n/a";
            _sb.AppendLine($"Visual defaults: radius_multiplier={_radiusMult} | distance_multiplier={_distanceMult}");
            _sb.AppendLine($"Spawn: initial_angle_deg={_spawn?.InitialAngleDeg?.ToString("0.###") ?? "n/a"} | position_unity={FormatArray(_spawn?.PositionUnity)} | scale_unity={FormatArray(_spawn?.ScaleUnity)}");
            _sb.AppendLine($"Tidal lock: override={FormatNullableBoolOrAuto(_data.TidalLock)} | align_to_primary_tilt={FormatNullableBoolOrAuto(_data.AlignOrbitToPrimaryTilt)}");
            return _sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Format a spin period from the dataset.
        /// </summary>
        private static string FormatSpinPeriod(TruthSpinData? _spin)
        {
            if (_spin == null)
            {
                return "n/a";
            }

            if (_spin.SiderealRotationPeriodHours.HasValue)
            {
                return $"{_spin.SiderealRotationPeriodHours:0.###} h";
            }

            if (_spin.SiderealRotationPeriodDays.HasValue)
            {
                return $"{_spin.SiderealRotationPeriodDays:0.###} d";
            }

            return "n/a";
        }

        /// <summary>
        /// Format orbit period in days (or years with day conversion).
        /// </summary>
        private static string FormatOrbitPeriod(TruthOrbitData? _orbit)
        {
            if (_orbit == null)
            {
                return "n/a";
            }

            if (_orbit.OrbitalPeriodDays.HasValue)
            {
                return $"{_orbit.OrbitalPeriodDays:0.###} d";
            }

            if (_orbit.OrbitalPeriodYears.HasValue)
            {
                double _years = _orbit.OrbitalPeriodYears.Value;
                double _days = _years * 365.25;
                return $"{_years:0.###} y ({_days:0.###} d)";
            }

            return "n/a";
        }

        /// <summary>
        /// Format semi-major axis values for logging.
        /// </summary>
        private static string FormatSemiMajorAxis(TruthOrbitData? _orbit)
        {
            if (_orbit == null)
            {
                return "n/a";
            }

            if (_orbit.SemiMajorAxisAU.HasValue)
            {
                return $"{_orbit.SemiMajorAxisAU:0.######} AU";
            }

            if (_orbit.SemiMajorAxisKm.HasValue)
            {
                return $"{_orbit.SemiMajorAxisKm:0.###} km";
            }

            return "n/a";
        }

        /// <summary>
        /// Format spin direction with sign and meaning.
        /// </summary>
        private static string FormatSpinDirection(double? _spinDirection)
        {
            if (!_spinDirection.HasValue)
            {
                return "1 (default/prograde)";
            }

            return _spinDirection.Value < 0.0 ? "-1 (retrograde)" : "1 (prograde)";
        }

        /// <summary>
        /// Format optional boolean values with an auto/default label.
        /// </summary>
        private static string FormatNullableBoolOrAuto(bool? _value)
        {
            return _value.HasValue ? _value.Value.ToString() : "auto";
        }

        /// <summary>
        /// Format a numeric array for logging.
        /// </summary>
        private static string FormatArray(double[]? _values)
        {
            if (_values == null || _values.Length == 0)
            {
                return "n/a";
            }

            StringBuilder _sb = new StringBuilder();
            for (int _i = 0; _i < _values.Length; _i++)
            {
                if (_i > 0)
                {
                    _sb.Append(", ");
                }
                _sb.Append(_values[_i].ToString("0.###"));
            }

            return $"({_sb})";
        }
        #endregion
    }
}

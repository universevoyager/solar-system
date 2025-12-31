#nullable enable
using System;
using Assets.Scripts.Data;
using Assets.Scripts.Helpers.Debugging;
using UnityEngine;

namespace Assets.Scripts.Runtime
{
    public sealed partial class SolarObject
    {
        #region Initialization and Caching
        /// <summary>
        /// Apply explicit spawn position/scale for the reference object.
        /// </summary>
        private void ApplyReferenceSpawn(SolarObjectData _data)
        {
            if (_data.Spawn?.PositionUnity is double[] _pos && _pos.Length == 3)
            {
                transform.position = new Vector3((float)_pos[0], (float)_pos[1], (float)_pos[2]);
            }
            else
            {
                transform.position = Vector3.zero;
            }

            if (_data.Spawn?.ScaleUnity is double[] _scale && _scale.Length == 3)
            {
                transform.localScale = new Vector3((float)_scale[0], (float)_scale[1], (float)_scale[2]);
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Cache the physical radius for scaling.
        /// </summary>
        private void CacheRadius(SolarObjectData _data)
        {
            radiusKm = _data.TruthPhysical?.MeanRadiusKm ?? 1.0;
        }

        /// <summary>
        /// Apply scaling based on the shared visual context.
        /// </summary>
        private void ApplyScaleFromContext()
        {
            if (visualContext == null)
            {
                return;
            }

            double _radiusRatio = radiusKm / Math.Max(1e-9, visualContext.ReferenceSolarObjectRadiusKm);

            // Unity scale represents diameter.
            double _diameterUnity =
                visualContext.ReferenceSolarObjectDiameterUnity *
                _radiusRatio *
                visualContext.GlobalRadiusScale *
                GetRadiusMultiplier();

            solarObjectDiameterUnity = (float)Math.Max(1e-6, _diameterUnity);
            transform.localScale = Vector3.one * solarObjectDiameterUnity;
        }

        /// <summary>
        /// Compute the final radius multiplier based on realism and simulation profile.
        /// </summary>
        private double GetRadiusMultiplier()
        {
            double _multiplier = 1.0;

            if (visualContext == null)
            {
                return dataRadiusMultiplier;
            }

            float _defaultsWeight = Mathf.Clamp01(visualContext.VisualDefaultsBlend);
            if (_defaultsWeight > 0f)
            {
                _multiplier = LerpDouble(1.0, dataRadiusMultiplier, _defaultsWeight);
            }

            float _simulationWeight = Mathf.Clamp01(visualContext.SimulationScaleBlend);
            if (_simulationWeight > 0f)
            {
                double _profileMultiplier =
                    visualContext.SimulationRadiusScaleGlobal *
                    GetSimulationTypeRadiusScale();
                _multiplier *= LerpDouble(1.0, _profileMultiplier, _simulationWeight);
            }

            return _multiplier;
        }

        /// <summary>
        /// Compute the final distance multiplier based on realism and simulation profile.
        /// </summary>
        private double GetDistanceMultiplier()
        {
            double _multiplier = 1.0;

            if (visualContext == null)
            {
                return dataDistanceMultiplier;
            }

            float _defaultsWeight = Mathf.Clamp01(visualContext.VisualDefaultsBlend);
            if (_defaultsWeight > 0f)
            {
                _multiplier = LerpDouble(1.0, dataDistanceMultiplier, _defaultsWeight);
            }

            float _simulationWeight = Mathf.Clamp01(visualContext.SimulationScaleBlend);
            if (_simulationWeight > 0f)
            {
                double _profileMultiplier = 1.0;
                if (string.Equals(primaryId, "sun", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(type, "planet", StringComparison.OrdinalIgnoreCase))
                    {
                        _profileMultiplier *= visualContext.SimulationPlanetDistanceScaleGlobal;

                        if (orderFromSun >= visualContext.SimulationOuterPlanetMinOrderIndex)
                        {
                            _profileMultiplier *= visualContext.SimulationOuterPlanetDistanceScale;
                        }
                        else if (orderFromSun > 0 && orderFromSun <= visualContext.SimulationInnerPlanetMaxOrderIndex)
                        {
                            double _bias = visualContext.SimulationInnerPlanetSpacingBiasPerOrder;
                            _profileMultiplier *= 1.0 + _bias * (orderFromSun - 1);
                        }
                    }
                    else if (string.Equals(type, "dwarf_planet", StringComparison.OrdinalIgnoreCase))
                    {
                        _profileMultiplier *= visualContext.SimulationPlanetDistanceScaleGlobal;

                        double _auThreshold = visualContext.SimulationDwarfOuterDistanceAuCutoff;
                        bool _isOuter = _auThreshold > 0.0 &&
                            semiMajorAxisKm >= _auThreshold * 149_597_870.7;

                        if (_isOuter)
                        {
                            _profileMultiplier *= visualContext.SimulationOuterPlanetDistanceScale;
                        }
                        else
                        {
                            _profileMultiplier *= visualContext.SimulationInnerDwarfDistanceScale;
                        }
                    }
                }

                if (string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
                {
                    _profileMultiplier *= visualContext.SimulationMoonOrbitDistanceScale;
                }

                _multiplier *= LerpDouble(1.0, _profileMultiplier, _simulationWeight);
            }

            return _multiplier;
        }

        /// <summary>
        /// Blend two double values.
        /// </summary>
        private static double LerpDouble(double _from, double _to, float _t)
        {
            return _from + (_to - _from) * _t;
        }

        /// <summary>
        /// Resolve per-type radius scaling used by the Simulation profile.
        /// </summary>
        private double GetSimulationTypeRadiusScale()
        {
            if (visualContext == null)
            {
                return 1.0;
            }

            if (string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
            {
                return visualContext.SimulationMoonRadiusScale;
            }

            if (string.Equals(type, "dwarf_planet", StringComparison.OrdinalIgnoreCase))
            {
                return visualContext.SimulationDwarfRadiusScale;
            }

            if (string.Equals(type, "planet", StringComparison.OrdinalIgnoreCase))
            {
                bool _small = radiusKm <= visualContext.SimulationSmallPlanetRadiusKmCutoff;
                return _small
                    ? visualContext.SimulationSmallPlanetRadiusScale
                    : visualContext.SimulationLargePlanetRadiusScale;
            }

            return visualContext.SimulationOtherRadiusScale;
        }

        /// <summary>
        /// Cache spin period and axial tilt.
        /// </summary>
        private void CacheSpin(SolarObjectData _data)
        {
            double? _periodDays = _data.TruthSpin?.SiderealRotationPeriodDays;
            double? _periodHours = _data.TruthSpin?.SiderealRotationPeriodHours;
            double? _spinDirection = _data.TruthSpin?.SpinDirection;

            if (_periodHours.HasValue)
            {
                rotationPeriodSeconds = Math.Abs(_periodHours.Value) * 3600.0;
            }
            else if (_periodDays.HasValue)
            {
                rotationPeriodSeconds = Math.Abs(_periodDays.Value) * 86400.0;
            }
            else
            {
                rotationPeriodSeconds = 0.0;
            }

            hasSpin = rotationPeriodSeconds > 0.0;
            axialTiltDeg = (float)(_data.TruthSpin?.AxialTiltDeg ?? 0.0);
            spinDirection = _spinDirection.HasValue && _spinDirection.Value < 0.0 ? -1.0f : 1.0f;
        }

        /// <summary>
        /// Apply spin rotation for the current simulation time.
        /// </summary>
        private void ApplySpin(double _simulationTimeSeconds)
        {
            if (ShouldTidalLock())
            {
                ApplyTidalLockRotation();
                return;
            }

            transform.rotation = ComputeSpinRotation(_simulationTimeSeconds);
        }

        /// <summary>
        /// Compute the spin rotation for the current simulation time.
        /// </summary>
        private Quaternion ComputeSpinRotation(double _simulationTimeSeconds)
        {
            // Unity's positive Y rotation appears clockwise from +Y; invert to align prograde with orbit direction.
            double _spinCycles = (_simulationTimeSeconds / Math.Max(1e-9, rotationPeriodSeconds)) *
                -GetEffectiveSpinDirection();
            float _spinDeg = (float)((_spinCycles - Math.Floor(_spinCycles)) * 360.0);

            Quaternion _tiltRotation = Quaternion.AngleAxis(axialTiltDeg, Vector3.right);
            Quaternion _spinRotation = Quaternion.AngleAxis(_spinDeg, Vector3.up);

            return _tiltRotation * _spinRotation;
        }

        #region Tidal Lock
        /// <summary>
        /// True when this object should stay face-locked to its primary.
        /// </summary>
        private bool ShouldTidalLock()
        {
            if (!enableTidalLock)
            {
                return false;
            }

            if (!hasOrbit || !hasSpin)
            {
                return false;
            }

            if (primaryTransform == null)
            {
                return false;
            }

            if (tidalLockOverride.HasValue)
            {
                return tidalLockOverride.Value;
            }

            if (tidalLockOnlyForMoons &&
                !string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (rotationPeriodSeconds <= 0.0 || orbitalPeriodSeconds <= 0.0)
            {
                return false;
            }

            double _difference = Math.Abs(rotationPeriodSeconds - orbitalPeriodSeconds);
            double _ratio = _difference / Math.Max(1e-9, orbitalPeriodSeconds);

            return _ratio <= Math.Max(0.0, tidalLockPeriodTolerance);
        }

        /// <summary>
        /// Orient the object so the same face points at its primary.
        /// </summary>
        private void ApplyTidalLockRotation()
        {
            if (primaryTransform == null)
            {
                return;
            }

            Vector3 _toPrimary = primaryTransform.position - transform.position;
            if (_toPrimary.sqrMagnitude <= 1e-10f)
            {
                return;
            }

            Vector3 _axis = GetTidalLockAxis();
            if (_axis.sqrMagnitude <= 1e-10f)
            {
                _axis = Vector3.up;
            }
            else
            {
                _axis.Normalize();
            }

            Vector3 _direction = _toPrimary.normalized;
            if (Vector3.Cross(_axis, _direction).sqrMagnitude <= 1e-8f)
            {
                _axis = Vector3.up;

                if (Vector3.Cross(_axis, _direction).sqrMagnitude <= 1e-8f)
                {
                    _axis = Vector3.forward;
                }
            }

            Quaternion _lookRotation = Quaternion.LookRotation(_direction, _axis);
            Quaternion _offsetRotation = Quaternion.AngleAxis(tidalLockFacingOffsetDeg, Vector3.up);

            transform.rotation = _lookRotation * _offsetRotation;
        }

        /// <summary>
        /// Compute the tidal-lock rotation axis from axial tilt.
        /// </summary>
        private Vector3 GetTidalLockAxis()
        {
            Quaternion _tiltRotation = Quaternion.AngleAxis(axialTiltDeg, Vector3.right);
            return _tiltRotation * Vector3.up;
        }
        #endregion

        /// <summary>
        /// Cache orbit model and parameters.
        /// </summary>
        private void CacheOrbit(SolarObjectData _data)
        {
            TruthOrbitData _orbit = _data.TruthOrbit!;

            if (_orbit.OrbitalPeriodDays.HasValue)
            {
                orbitalPeriodSeconds = Math.Abs(_orbit.OrbitalPeriodDays.Value) * 86400.0;
            }
            else
            {
                orbitalPeriodSeconds = Math.Abs(_orbit.OrbitalPeriodYears!.Value) * 365.25 * 86400.0;
            }

            if (_orbit.SemiMajorAxisKm.HasValue)
            {
                semiMajorAxisKm = Math.Abs(_orbit.SemiMajorAxisKm.Value);
            }
            else
            {
                semiMajorAxisKm = Math.Abs(_orbit.SemiMajorAxisAU!.Value) * 149_597_870.7;
            }

            double _initialAngleDeg = _data.Spawn?.InitialAngleDeg ?? 0.0;
            initialMeanAnomalyOffsetRad = DegToRad(_initialAngleDeg);
            if (Math.Abs(_initialAngleDeg) > 1e-6)
            {
                HelpLogs.Warn(
                    "SolarObject",
                    $"'{id}' uses spawn.initial_angle_deg = {_initialAngleDeg:0.###} (applied to mean anomaly)."
                );
            }

            hasOrbit = semiMajorAxisKm > 0.0 && orbitalPeriodSeconds > 0.0;

            eccentricity = _orbit.Eccentricity!.Value;
            inclinationRad = DegToRad(_orbit.InclinationDeg!.Value);
            longitudeAscendingNodeRad = DegToRad(_orbit.LongitudeAscendingNodeDeg!.Value);
            argumentPeriapsisRad = DegToRad(_orbit.ArgumentPeriapsisDeg!.Value);
            meanAnomalyRad = DegToRad(_orbit.MeanAnomalyDeg!.Value);
        }

        /// <summary>
        /// Enforce a minimum orbit radius so moons do not overlap their primary.
        /// </summary>
        private void CacheMoonOverlapGuard()
        {
            minOrbitRadiusUnity = 0.0f;
            if (visualContext == null)
            {
                return;
            }

            if (primaryTransform == null)
            {
                return;
            }

            float _primaryDiameterUnity = primaryTransform.localScale.x;

            float _requiredMinCenterDistance =
                (_primaryDiameterUnity * 0.5f) +
                (solarObjectDiameterUnity * 0.5f) +
                visualContext.MoonClearanceUnity;

            minOrbitRadiusUnity = _requiredMinCenterDistance;
        }
        #endregion
    }
}

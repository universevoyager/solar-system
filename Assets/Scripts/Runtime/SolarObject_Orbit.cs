#nullable enable
using System;
using UnityEngine;

namespace Assets.Scripts.Runtime
{
    public sealed partial class SolarObject
    {
        #region Orbit Calculation
        /// <summary>
        /// Compute the local orbit offset for the current time.
        /// </summary>
        private Vector3 ComputeOrbitOffsetUnity(double _simulationTimeSeconds)
        {
            if (visualContext == null)
            {
                return Vector3.zero;
            }

            if (!hasOrbit)
            {
                return Vector3.zero;
            }

            double _distanceMultiplier = visualContext.GlobalDistanceScale * GetDistanceMultiplier();

            double _aUnity =
                (semiMajorAxisKm / Math.Max(1e-9, visualContext.KilometersPerUnityUnit)) * _distanceMultiplier;

            Vector3 _offset = ComputeKeplerianOffsetUnity(_simulationTimeSeconds, _aUnity);
            _offset = ApplyOrbitRadiusClamp(_offset);
            return ApplyPrimaryTiltToOrbit(_offset);
        }

        /// <summary>
        /// Compute the local orbit offset using Keplerian elements.
        /// </summary>
        private Vector3 ComputeKeplerianOffsetUnity(double _simulationTimeSeconds, double _aUnity)
        {
            double _n = TwoPi() / Math.Max(1e-9, orbitalPeriodSeconds);

            double _M = WrapAngleRad(meanAnomalyRad + initialMeanAnomalyOffsetRad + _n * _simulationTimeSeconds);
            double _E = SolveEccentricAnomaly(_M, eccentricity);

            double _r = 1.0 - eccentricity * Math.Cos(_E);
            double _rUnity = _aUnity * _r;

            double _cosNu = (Math.Cos(_E) - eccentricity) /
                Math.Max(1e-9, (1.0 - eccentricity * Math.Cos(_E)));
            double _sinNu = (Math.Sqrt(Math.Max(0.0, 1.0 - eccentricity * eccentricity)) * Math.Sin(_E)) /
                Math.Max(1e-9, (1.0 - eccentricity * Math.Cos(_E)));
            double _nu = Math.Atan2(_sinNu, _cosNu);

            double _arg = argumentPeriapsisRad + _nu;
            double _cosO = Math.Cos(longitudeAscendingNodeRad);
            double _sinO = Math.Sin(longitudeAscendingNodeRad);
            double _cosI = Math.Cos(inclinationRad);
            double _sinI = Math.Sin(inclinationRad);
            double _cosArg = Math.Cos(_arg);
            double _sinArg = Math.Sin(_arg);

            // Astro XYZ -> Unity XZY (Z becomes Unity Y).
            double _xAstro = _rUnity * (_cosO * _cosArg - _sinO * _sinArg * _cosI);
            double _yAstro = _rUnity * (_sinO * _cosArg + _cosO * _sinArg * _cosI);
            double _zAstro = _rUnity * (_sinArg * _sinI);

            return new Vector3((float)_xAstro, (float)_zAstro, (float)_yAstro);
        }

        /// <summary>
        /// Enforce a minimum orbit radius to avoid overlap with the primary.
        /// </summary>
        private Vector3 ApplyOrbitRadiusClamp(Vector3 _offset)
        {
            if (minOrbitRadiusUnity <= 0.0f)
            {
                return _offset;
            }

            float _distance = _offset.magnitude;
            if (_distance >= minOrbitRadiusUnity)
            {
                return _offset;
            }

            if (_distance <= 1e-6f)
            {
                return new Vector3(minOrbitRadiusUnity, 0.0f, 0.0f);
            }

            return _offset * (minOrbitRadiusUnity / _distance);
        }

        /// <summary>
        /// Optionally align moon orbits to the primary axial tilt.
        /// </summary>
        private Vector3 ApplyPrimaryTiltToOrbit(Vector3 _offset)
        {
            if (visualContext == null)
            {
                return _offset;
            }

            if (!string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
            {
                return _offset;
            }

            bool _align = alignOrbitToPrimaryTilt ?? visualContext.AlignMoonOrbitsToPrimaryAxialTilt;
            if (!_align)
            {
                return _offset;
            }

            if (primarySolarObject == null)
            {
                return _offset;
            }

            float _tilt = primarySolarObject.AxialTiltDeg;
            if (Mathf.Approximately(_tilt, 0.0f))
            {
                return _offset;
            }

            Quaternion _tiltRotation = Quaternion.AngleAxis(_tilt, Vector3.right);
            return _tiltRotation * _offset;
        }
        #endregion
    }
}

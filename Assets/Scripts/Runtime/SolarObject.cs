#nullable enable
using System;
using UnityEngine;
using Assets.Scripts.Data;
using Assets.Scripts.Helpers.Debugging;

namespace Assets.Scripts.Runtime
{
    /// <summary>
    /// Runtime behavior for a single solar object (orbit, spin, and line visuals).
    /// </summary>
    public sealed class SolarObject : MonoBehaviour
    {
        #region Types
        [Serializable]
        public sealed class VisualContext
        {
            // Reference solar object and global scale settings.
            public double ReferenceSolarObjectRadiusKm = 695700.0; // Sun radius km
            public float ReferenceSolarObjectDiameterUnity = 1.0f; // Sun diameter in Unity units (from actual sun instance)

            public double DistanceKmPerUnityUnit = 1_000_000.0;
            public double GlobalDistanceMultiplier = 1.0;
            public double GlobalRadiusMultiplier = 1.0;

            public int OrbitPathSegments = 256;
            public float MoonClearanceUnity = 0.02f;

            // Global runtime line toggles and scale.
            public bool ShowOrbitLines = true;
            public bool ShowSpinAxisLines = true;
            public bool ShowWorldUpLines = true;
            public bool ShowSpinDirectionLines = true;
            public float RuntimeLineWidthScale = 1.0f;

            // Use per-object visual_defaults multipliers when true.
            public bool UseVisualDefaults = true;

            // Apply extra scaling for the Simulation preset.
            public bool UseSimulationScaleProfile = false;
            public double SimulationRadiusScaleAll = 1.0;
            public double SimulationSmallPlanetRadiusScale = 1.0;
            public double SimulationLargePlanetRadiusScale = 1.0;
            public double SimulationMoonRadiusScale = 1.0;
            public double SimulationDwarfRadiusScale = 1.0;
            public double SimulationOtherRadiusScale = 1.0;
            public double SimulationSmallPlanetRadiusKmThreshold = 9000.0;
            public double SimulationInnerPlanetSpacingBias = 0.0;
            public int SimulationInnerPlanetMaxOrder = 0;
            public double SimulationPlanetDistanceScaleAll = 1.0;
            public double SimulationOuterPlanetDistanceScale = 1.0;
            public int SimulationOuterPlanetMinOrder = 0;
            public double SimulationDwarfOuterDistanceAuThreshold = 0.0;
            public double SimulationInnerDwarfDistanceScale = 1.0;
            public double SimulationMoonDistanceScale = 1.0;
            public bool AlignMoonOrbitsToPrimaryTilt = true;
        }
        #endregion

        #region Serialized Fields
        [Header("Runtime Lines")]
        [SerializeField] private bool drawOrbitRuntime = true;
        [SerializeField] private bool drawAxisRuntime = true;
        [SerializeField] private bool drawWorldUpRuntime = true;
        [SerializeField] private bool drawSpinDirectionRuntime = true;
        [SerializeField] private float orbitLineWidth = 0.06f;
        [SerializeField] private float axisLineWidth = 0.03f;
        [SerializeField] private float spinDirectionLineWidth = 0.02f;
        [SerializeField] private Color orbitLineColor = new Color(0.2f, 0.7f, 1.0f, 0.9f);
        [SerializeField] private Color moonOrbitLineColor = new Color(0.3f, 0.9f, 0.5f, 0.9f);
        [SerializeField] private Color dwarfOrbitLineColor = new Color(0.9f, 0.5f, 0.9f, 0.9f);
        [SerializeField] private Color hypotheticalOrbitLineColor = new Color(1.0f, 0.2f, 0.2f, 0.95f);
        [SerializeField] private Color axisLineColor = new Color(1.0f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color worldUpLineColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        [SerializeField] private Color spinDirectionProgradeColor = new Color(0.2f, 1.0f, 0.4f, 0.9f);
        [SerializeField] private Color spinDirectionRetrogradeColor = new Color(1.0f, 0.3f, 0.2f, 0.9f);

        [Header("Spin Direction Arc")]
        [SerializeField] private float spinDirectionArcRadiusMultiplier = 1.1f;
        [SerializeField] private float spinDirectionArcAngleDeg = 240.0f;
        [SerializeField] private int spinDirectionArcSegments = 24;
        [SerializeField] private float spinDirectionArrowHeadLengthMultiplier = 0.2f;
        [SerializeField] private float spinDirectionArrowHeadAngleDeg = 25.0f;

        [Header("Axis Line Distance Scaling")]
        [SerializeField] private bool scaleAxisLinesByCameraDistance = true;
        [SerializeField] private float axisLineDistanceReference = 1.5f;
        [SerializeField] private float axisLineDistanceMinScale = 0.15f;
        [SerializeField] private float axisLineDistanceMaxScale = 6.0f;
        [SerializeField] private float axisLineDistanceExponent = 1.35f;

        [Header("Axis Line Size Scaling")]
        [SerializeField] private float axisLineSizeReference = 0.15f;
        [SerializeField] private float axisLineSizeMinScale = 1.3f;
        [SerializeField] private float axisLineSizeMaxScale = 2.2f;
        [SerializeField] private float axisLineSizeWidthMaxScale = 1.6f;
        [SerializeField] private float axisLineSizeStarMaxScale = 1.4f;
        // Global axis-line length multiplier (all objects).
        [SerializeField] private float axisLineLengthMultiplier = 0.5f;
        // Global axis-line thickness multiplier.
        [SerializeField] private float axisLineThicknessMultiplier = 1.6f;
        [SerializeField] private float axisLineStarMaxLengthMultiplier = 0.6f;
        [SerializeField] private float axisLineSmallBodyDiameterThreshold = 0.25f;
        [SerializeField] private float axisLineSmallBodyLengthScale = 1.6f;

        [Header("Orbit Line Distance Scaling")]
        [SerializeField] private bool scaleOrbitLinesByCameraDistance = true;
        [SerializeField] private float orbitLineDistanceReference = 6.0f;
        [SerializeField] private float orbitLineDistanceMinScale = 0.12f;
        [SerializeField] private float orbitLineDistanceMaxScale = 20.0f;
        [SerializeField] private float orbitLineDistanceExponent = 1.6f;
        [SerializeField] private float orbitLineDistanceExponentNear = 0.85f;
        // Extra boost so orbits thicken earlier with distance.
        [SerializeField] private float orbitLineDistanceScaleBoost = 1.4f;
        // Slow down thinning when near the camera.
        [SerializeField] private float orbitLineDistanceNearExponentMultiplier = 0.7f;
        // Extra headroom for distant orbit thickness.
        [SerializeField] private float orbitLineDistanceMaxScaleMultiplier = 1.4f;

        [Header("Hypothetical Orbit Boost")]
        [SerializeField] private float hypotheticalOrbitLineFocusDistance = 10.0f;
        [SerializeField] private float hypotheticalOrbitLineFocusScaleMultiplier = 3.0f;
        [SerializeField] private float hypotheticalOrbitLineFarScale = 30.0f;

        [Header("Tidal Lock")]
        // Enable automatic tidal locking when periods match within tolerance.
        [SerializeField] private bool enableTidalLock = true;
        // Restrict tidal lock to moons only (unless overridden per object).
        [SerializeField] private bool tidalLockOnlyForMoons = true;
        // Acceptable period mismatch ratio for tidal lock.
        [SerializeField] private float tidalLockPeriodTolerance = 0.02f;
        // Optional facing offset in degrees for tidal-locked objects.
        [SerializeField] private float tidalLockFacingOffsetDeg = 0.0f;

        [Header("UI")]
        // Sprite used by runtime UI grids (e.g., focus button avatars).
        [SerializeField] private Sprite? avatarSprite;
        #endregion

        #region Runtime State
        // Identity and role.
        private string id = string.Empty;
        private bool isReference = false;
        private bool isHypothetical = false;
        private string type = string.Empty;
        private string primaryId = string.Empty;
        private int orderFromSun = -1;
        private SolarObject? primarySolarObject;
        private bool? alignOrbitToPrimaryTilt;
        private bool? tidalLockOverride;

        // Cached references.
        private Transform? primaryTransform;
        private VisualContext? visualContext;

        // Physical and visual scale.
        private double radiusKm = 1.0;
        private double dataRadiusMultiplier = 1.0;
        private double dataDistanceMultiplier = 1.0;
        private float solarObjectDiameterUnity = 0.1f;

        // Spin state.
        private bool hasSpin = false;
        private double rotationPeriodSeconds = 0.0;
        private float axialTiltDeg = 0.0f;
        private float spinDirection = 1.0f;

        // Orbit state.
        private bool hasOrbit = false;
        private double orbitalPeriodSeconds = 0.0;
        private double semiMajorAxisKm = 0.0;
        private double initialMeanAnomalyOffsetRad = 0.0;

        // Keplerian elements (radians).
        private double eccentricity = 0.0;
        private double inclinationRad = 0.0;
        private double argumentPeriapsisRad = 0.0;
        private double longitudeAscendingNodeRad = 0.0;
        private double meanAnomalyRad = 0.0;

        // Orbit line cache.
        private Vector3[]? orbitPoints;
        private Vector3[]? orbitWorldPoints;
        private bool orbitPointsDirty = true;
        private float minOrbitRadiusUnity = 0.0f;

        // Runtime renderers.
        private LineRenderer? orbitLine;
        private LineRenderer? axisLine;
        private LineRenderer? worldUpLine;
        private LineRenderer? spinDirectionLine;
        private Vector3 lastPrimaryPosition;
        private bool hasPrimaryPosition = false;

        private static Material? lineMaterial;
        private static Camera? lineScaleCamera;
        private bool lineStylesDirty = true;
        private float axisLineDistanceScale = 1.0f;
        private float orbitLineDistanceScale = 1.0f;
        #endregion

        #region Public API
        /// <summary>
        /// Dataset id for this solar object.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Dataset id for the primary object (or empty if none).
        /// </summary>
        public string PrimaryId => primaryId;

        /// <summary>
        /// True when the object is a moon.
        /// </summary>
        public bool IsMoon => string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when the object is marked as hypothetical in the dataset.
        /// </summary>
        public bool IsHypothetical => isHypothetical;

        /// <summary>
        /// Primary solar object instance (when available).
        /// </summary>
        public SolarObject? PrimarySolarObject => primarySolarObject;

        /// <summary>
        /// Current diameter in Unity units (includes global radius multiplier).
        /// </summary>
        public float DiameterUnity => solarObjectDiameterUnity;

        /// <summary>
        /// Axial tilt in degrees.
        /// </summary>
        public float AxialTiltDeg => axialTiltDeg;

        /// <summary>
        /// Sprite used for UI avatar buttons.
        /// </summary>
        public Sprite? AvatarSprite => avatarSprite;

        /// <summary>
        /// Diameter normalized to ignore the global radius multiplier.
        /// </summary>
        public float BaseDiameterUnity
        {
            get
            {
                if (visualContext == null)
                {
                    return solarObjectDiameterUnity;
                }

                double _global = Math.Max(1e-6, visualContext.GlobalRadiusMultiplier);
                return (float)(solarObjectDiameterUnity / _global);
            }
        }

        /// <summary>
        /// Initialize from dataset and shared visual context.
        /// </summary>
        public void Initialize(
            SolarObjectData _data,
            Transform? _primaryTransform,
            SolarObject? _primarySolarObject,
            VisualContext _visualContext
        )
        {
            primaryTransform = _primaryTransform;
            primarySolarObject = _primarySolarObject;
            visualContext = _visualContext;
            lineStylesDirty = true;

            id = _data.Id;
            isReference = _data.IsReference;
            isHypothetical = _data.IsHypothetical;
            type = _data.Type ?? string.Empty;
            primaryId = _data.PrimaryId ?? string.Empty;
            orderFromSun = _data.OrderFromSun ?? -1;
            alignOrbitToPrimaryTilt = _data.AlignOrbitToPrimaryTilt;
            tidalLockOverride = _data.TidalLock;

            name = string.IsNullOrWhiteSpace(_data.DisplayName) ? _data.Id : _data.DisplayName;

            dataRadiusMultiplier = _data.VisualDefaults?.RadiusMultiplier ?? 1.0;
            dataDistanceMultiplier = _data.VisualDefaults?.DistanceMultiplier ?? 1.0;

            if (isReference)
            {
                ApplyReferenceSpawn(_data);
                CacheSpin(_data);
                hasOrbit = false;
                orbitPointsDirty = true;
                return;
            }

            CacheRadius(_data);
            CacheSpin(_data);
            CacheOrbit(_data);

            ApplyScaleFromContext();
            CacheMoonOverlapGuard();

            Vector3 _primaryPosition = _primaryTransform != null ? _primaryTransform.position : Vector3.zero;
            transform.position = _primaryPosition + ComputeOrbitOffsetUnity(0.0);

            orbitPointsDirty = true;
        }

        /// <summary>
        /// Advance orbit/spin based on simulation time.
        /// </summary>
        public void Simulate(double _simulationTimeSeconds)
        {
            if (visualContext == null)
            {
                return;
            }

            if (hasOrbit)
            {
                Vector3 _primaryPosition = primaryTransform != null ? primaryTransform.position : Vector3.zero;
                transform.position = _primaryPosition + ComputeOrbitOffsetUnity(_simulationTimeSeconds);
            }

            if (hasSpin)
            {
                ApplySpin(_simulationTimeSeconds);
            }

            UpdateRuntimeRenderers();
        }

        /// <summary>
        /// Re-apply visual scaling after global changes.
        /// </summary>
        public void RefreshVisuals(VisualContext _visualContext)
        {
            visualContext = _visualContext;
            lineStylesDirty = true;

            if (!isReference)
            {
                ApplyScaleFromContext();
                CacheMoonOverlapGuard();
            }

            orbitPointsDirty = true;
        }

        /// <summary>
        /// Mark runtime line widths as needing a refresh.
        /// </summary>
        public void MarkLineStylesDirty()
        {
            lineStylesDirty = true;
        }
        #endregion

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
                visualContext.GlobalRadiusMultiplier *
                GetRadiusMultiplier();

            solarObjectDiameterUnity = (float)Math.Max(1e-6, _diameterUnity);
            transform.localScale = Vector3.one * solarObjectDiameterUnity;
        }

        /// <summary>
        /// Compute the final radius multiplier based on preset and simulation profile.
        /// </summary>
        private double GetRadiusMultiplier()
        {
            double _multiplier = 1.0;

            if (visualContext == null)
            {
                return dataRadiusMultiplier;
            }

            if (visualContext.UseVisualDefaults)
            {
                _multiplier = dataRadiusMultiplier;
            }

            if (visualContext.UseSimulationScaleProfile)
            {
                _multiplier *=
                    visualContext.SimulationRadiusScaleAll *
                    GetSimulationTypeRadiusScale();
            }

            return _multiplier;
        }

        /// <summary>
        /// Compute the final distance multiplier based on preset and simulation profile.
        /// </summary>
        private double GetDistanceMultiplier()
        {
            double _multiplier = 1.0;

            if (visualContext == null)
            {
                return dataDistanceMultiplier;
            }

            if (visualContext.UseVisualDefaults)
            {
                _multiplier = dataDistanceMultiplier;
            }

            if (visualContext.UseSimulationScaleProfile &&
                string.Equals(primaryId, "sun", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(type, "planet", StringComparison.OrdinalIgnoreCase))
                {
                    _multiplier *= visualContext.SimulationPlanetDistanceScaleAll;

                    if (orderFromSun >= visualContext.SimulationOuterPlanetMinOrder)
                    {
                        _multiplier *= visualContext.SimulationOuterPlanetDistanceScale;
                    }
                    else if (orderFromSun > 0 && orderFromSun <= visualContext.SimulationInnerPlanetMaxOrder)
                    {
                        double _bias = visualContext.SimulationInnerPlanetSpacingBias;
                        _multiplier *= 1.0 + _bias * (orderFromSun - 1);
                    }
                }
                else if (string.Equals(type, "dwarf_planet", StringComparison.OrdinalIgnoreCase))
                {
                    _multiplier *= visualContext.SimulationPlanetDistanceScaleAll;

                    double _auThreshold = visualContext.SimulationDwarfOuterDistanceAuThreshold;
                    bool _isOuter = _auThreshold > 0.0 &&
                        semiMajorAxisKm >= _auThreshold * 149_597_870.7;

                    if (_isOuter)
                    {
                        _multiplier *= visualContext.SimulationOuterPlanetDistanceScale;
                    }
                    else
                    {
                        _multiplier *= visualContext.SimulationInnerDwarfDistanceScale;
                    }
                }
            }

            if (visualContext.UseSimulationScaleProfile &&
                string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
            {
                _multiplier *= visualContext.SimulationMoonDistanceScale;
            }

            return _multiplier;
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
                bool _small = radiusKm <= visualContext.SimulationSmallPlanetRadiusKmThreshold;
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

            double _distanceMultiplier = visualContext.GlobalDistanceMultiplier * GetDistanceMultiplier();

            double _aUnity =
                (semiMajorAxisKm / Math.Max(1e-9, visualContext.DistanceKmPerUnityUnit)) * _distanceMultiplier;

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

            bool _align = alignOrbitToPrimaryTilt ?? visualContext.AlignMoonOrbitsToPrimaryTilt;
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

        #region Runtime Lines
        /// <summary>
        /// Update runtime line renderers for orbits and axes.
        /// </summary>
        private void UpdateRuntimeRenderers()
        {
            if (visualContext == null)
            {
                return;
            }

            bool _drawOrbit = drawOrbitRuntime && visualContext.ShowOrbitLines;
            bool _drawSpinAxis = drawAxisRuntime && visualContext.ShowSpinAxisLines;
            bool _drawWorldUp = drawWorldUpRuntime && visualContext.ShowWorldUpLines;
            bool _drawSpinDirection = drawSpinDirectionRuntime && visualContext.ShowSpinDirectionLines;

            bool _axisScaleChanged = UpdateAxisLineDistanceScale();
            bool _orbitScaleChanged = UpdateOrbitLineDistanceScale();
            if (_axisScaleChanged || _orbitScaleChanged)
            {
                lineStylesDirty = true;
            }

            if (_drawOrbit)
            {
                UpdateOrbitLine();
            }
            else if (orbitLine != null)
            {
                orbitLine.enabled = false;
            }

            if (_drawSpinAxis || _drawWorldUp)
            {
                UpdateAxisLines(_drawSpinAxis, _drawWorldUp);
            }
            else
            {
                DeactivateAxisLines();
            }

            UpdateSpinDirectionLine(_drawSpinDirection);

            if (lineStylesDirty && ApplyRuntimeLineStyles())
            {
                lineStylesDirty = false;
            }
        }

        /// <summary>
        /// Build or update the orbit line renderer.
        /// </summary>
        private void UpdateOrbitLine()
        {
            if (!hasOrbit)
            {
                if (orbitLine != null)
                {
                    orbitLine.enabled = false;
                }

                return;
            }

            EnsureRuntimeRenderers();
            if (orbitLine == null || visualContext == null)
            {
                return;
            }

            ApplyOrbitColor();

            int _segments = Math.Max(64, visualContext.OrbitPathSegments);

            bool _rebuild = orbitPoints == null || orbitPoints.Length != _segments || orbitPointsDirty;
            if (_rebuild)
            {
                orbitPoints = new Vector3[_segments];
                orbitWorldPoints = new Vector3[_segments];
                orbitPointsDirty = false;

                for (int _i = 0; _i < _segments; _i++)
                {
                    double _t = (double)_i / _segments;
                    orbitPoints[_i] = ComputeOrbitOffsetUnity(_t * orbitalPeriodSeconds);
                }
            }

            if (orbitPoints == null)
            {
                return;
            }

            if (orbitWorldPoints == null || orbitWorldPoints.Length != orbitPoints.Length)
            {
                orbitWorldPoints = new Vector3[orbitPoints.Length];
            }

            Vector3 _primaryPosition = primaryTransform != null ? primaryTransform.position : Vector3.zero;
            bool _primaryMoved = !hasPrimaryPosition || _primaryPosition != lastPrimaryPosition;

            if (_rebuild || _primaryMoved)
            {
                for (int _i = 0; _i < orbitPoints.Length; _i++)
                {
                    orbitWorldPoints[_i] = _primaryPosition + orbitPoints[_i];
                }

                orbitLine.positionCount = orbitWorldPoints.Length;
                orbitLine.loop = true;
                orbitLine.SetPositions(orbitWorldPoints);

                lastPrimaryPosition = _primaryPosition;
                hasPrimaryPosition = true;
            }

            orbitLine.enabled = true;
        }

        /// <summary>
        /// Apply orbit line color based on object type.
        /// </summary>
        private void ApplyOrbitColor()
        {
            if (orbitLine == null)
            {
                return;
            }

            Color _color = orbitLineColor;
            if (isHypothetical)
            {
                _color = hypotheticalOrbitLineColor;
            }
            else if (string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
            {
                _color = moonOrbitLineColor;
            }
            else if (string.Equals(type, "dwarf_planet", StringComparison.OrdinalIgnoreCase))
            {
                _color = dwarfOrbitLineColor;
            }

            if (orbitLine.startColor != _color || orbitLine.endColor != _color)
            {
                orbitLine.startColor = _color;
                orbitLine.endColor = _color;
            }
        }

        /// <summary>
        /// Update axis and world-up lines.
        /// </summary>
        private void UpdateAxisLines(bool _drawSpinAxis, bool _drawWorldUp)
        {
            EnsureRuntimeRenderers();
            if (axisLine == null || worldUpLine == null)
            {
                return;
            }

            Vector3 _p = transform.position;
            Vector3 _axis = transform.rotation * Vector3.up;
            float _len = GetAxisLineLength();

            if (_drawSpinAxis)
            {
                axisLine.positionCount = 2;
                axisLine.SetPosition(0, _p - _axis * _len);
                axisLine.SetPosition(1, _p + _axis * _len);
                axisLine.enabled = true;
            }
            else
            {
                axisLine.enabled = false;
            }

            if (_drawWorldUp)
            {
                worldUpLine.positionCount = 2;
                worldUpLine.SetPosition(0, _p - Vector3.up * _len);
                worldUpLine.SetPosition(1, _p + Vector3.up * _len);
                worldUpLine.enabled = true;
            }
            else
            {
                worldUpLine.enabled = false;
            }
        }

        /// <summary>
        /// Update the curved spin-direction arc line.
        /// </summary>
        private void UpdateSpinDirectionLine(bool _drawSpinDirection)
        {
            if (!_drawSpinDirection || !hasSpin)
            {
                if (spinDirectionLine != null)
                {
                    spinDirectionLine.enabled = false;
                }

                return;
            }

            EnsureRuntimeRenderers();
            if (spinDirectionLine == null)
            {
                return;
            }

            Vector3 _axis = transform.rotation * Vector3.up;
            if (_axis.sqrMagnitude <= 1e-8f)
            {
                spinDirectionLine.enabled = false;
                return;
            }

            _axis.Normalize();
            bool _axisFlipped = false;
            if (Vector3.Dot(_axis, Vector3.up) < 0.0f)
            {
                _axis = -_axis;
                _axisFlipped = true;
            }

            Vector3 _reference = Mathf.Abs(Vector3.Dot(_axis, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
            Vector3 _right = Vector3.Cross(_axis, _reference).normalized;
            Vector3 _forward = Vector3.Cross(_axis, _right).normalized;

            float _radius = GetSpinDirectionArcRadius();
            if (_radius <= 1e-6f)
            {
                spinDirectionLine.enabled = false;
                return;
            }

            float _angleDeg = Mathf.Clamp(spinDirectionArcAngleDeg, 30.0f, 330.0f);
            float _angleRad = _angleDeg * Mathf.Deg2Rad;
            int _segments = Mathf.Max(6, spinDirectionArcSegments);

            float _start = -_angleRad * 0.5f;
            float _end = _angleRad * 0.5f;
            float _directionSign = GetSpinDirectionSign();
            if (_axisFlipped)
            {
                _directionSign *= -1.0f;
            }
            if (_directionSign < 0.0f)
            {
                float _temp = _start;
                _start = _end;
                _end = _temp;
            }

            int _arcCount = _segments + 1;
            int _count = _arcCount + 3;
            spinDirectionLine.positionCount = _count;

            Vector3 _center = transform.position;
            for (int _i = 0; _i < _arcCount; _i++)
            {
                float _t = _segments == 0 ? 0.0f : (float)_i / _segments;
                float _angle = Mathf.Lerp(_start, _end, _t);
                Vector3 _offset = (_right * Mathf.Cos(_angle) + _forward * Mathf.Sin(_angle)) * _radius;
                spinDirectionLine.SetPosition(_i, _center + _offset);
            }

            Vector3 _endOffset = (_right * Mathf.Cos(_end) + _forward * Mathf.Sin(_end)) * _radius;
            Vector3 _endPoint = _center + _endOffset;
            float _direction = _end >= _start ? 1.0f : -1.0f;
            Vector3 _tangent = (-Mathf.Sin(_end) * _right + Mathf.Cos(_end) * _forward) * _direction;
            Vector3 _arrowDir = -_tangent.normalized;

            float _arrowLen = Mathf.Max(0.001f, _radius * Mathf.Clamp(spinDirectionArrowHeadLengthMultiplier, 0.05f, 0.5f));
            float _arrowAngle = Mathf.Clamp(spinDirectionArrowHeadAngleDeg, 5.0f, 60.0f);
            Vector3 _arrowLeft = Quaternion.AngleAxis(_arrowAngle, _axis) * _arrowDir;
            Vector3 _arrowRight = Quaternion.AngleAxis(-_arrowAngle, _axis) * _arrowDir;

            spinDirectionLine.SetPosition(_arcCount, _endPoint + _arrowLeft * _arrowLen);
            spinDirectionLine.SetPosition(_arcCount + 1, _endPoint);
            spinDirectionLine.SetPosition(_arcCount + 2, _endPoint + _arrowRight * _arrowLen);

            Color _color = spinDirection >= 0.0f ? spinDirectionProgradeColor : spinDirectionRetrogradeColor;
            if (spinDirectionLine.startColor != _color || spinDirectionLine.endColor != _color)
            {
                spinDirectionLine.startColor = _color;
                spinDirectionLine.endColor = _color;
            }

            spinDirectionLine.enabled = true;
        }

        /// <summary>
        /// Compute axis line length from scale and solar object type.
        /// </summary>
        private float GetAxisLineLength()
        {
            float _baseLen = transform.localScale.x * 0.5f;
            float _typeScale = GetAxisLineTypeScale();
            float _sizeScale = GetAxisLineSizeScale();
            float _smallBodyScale = GetAxisLineSmallBodyLengthScale();
            float _length = _baseLen * _typeScale * _sizeScale * _smallBodyScale * axisLineDistanceScale;
            float _lengthMultiplier = Mathf.Clamp(axisLineLengthMultiplier, 0.1f, 2.0f);
            _length *= _lengthMultiplier;

            if (string.Equals(type, "star", StringComparison.OrdinalIgnoreCase))
            {
                float _cap = Mathf.Clamp(axisLineStarMaxLengthMultiplier, 0.1f, 3.0f);
                float _minLen = _baseLen * 1.1f;
                float _maxLen = _baseLen * _cap;
                if (_maxLen < _minLen)
                {
                    _maxLen = _minLen;
                }

                _length = Mathf.Clamp(_length, _minLen, _maxLen);
            }

            return Mathf.Max(0.1f, _length);
        }

        /// <summary>
        /// Compute the radius for the spin-direction arc.
        /// </summary>
        private float GetSpinDirectionArcRadius()
        {
            float _baseLen = transform.localScale.x * 0.5f;
            float _sizeScale = GetAxisLineSizeScale();
            float _distanceScale = Mathf.Clamp(axisLineDistanceScale, 1.0f, 3.0f);
            float _multiplier = Mathf.Clamp(spinDirectionArcRadiusMultiplier, 0.1f, 5.0f);
            float _radius = _baseLen * _sizeScale * _distanceScale * _multiplier;
            float _minRadius = _baseLen * Mathf.Max(1.05f, _multiplier);
            return Mathf.Max(_radius, _minRadius);
        }

        /// <summary>
        /// Resolve the spin-direction sign used for the arc.
        /// </summary>
        private float GetSpinDirectionSign()
        {
            float _direction = GetEffectiveSpinDirection();
            return _direction >= 0.0f ? -1.0f : 1.0f;
        }

        /// <summary>
        /// Resolve the effective spin direction sign for the current axial tilt.
        /// </summary>
        private float GetEffectiveSpinDirection()
        {
            float _direction = spinDirection;
            if (axialTiltDeg > 90.0f)
            {
                _direction *= -1.0f;
            }

            return _direction;
        }

        /// <summary>
        /// Per-type length scaling to keep lines readable across object classes.
        /// </summary>
        private float GetAxisLineTypeScale()
        {
            if (string.Equals(type, "moon", StringComparison.OrdinalIgnoreCase))
            {
                return 0.6f;
            }

            if (string.Equals(type, "dwarf_planet", StringComparison.OrdinalIgnoreCase))
            {
                return 0.85f;
            }

            if (string.Equals(type, "star", StringComparison.OrdinalIgnoreCase))
            {
                return 1.6f;
            }

            return 1.0f;
        }

        /// <summary>
        /// Deactivate axis lines when not in use.
        /// </summary>
        private void DeactivateAxisLines()
        {
            if (axisLine != null)
            {
                axisLine.enabled = false;
            }

            if (worldUpLine != null)
            {
                worldUpLine.enabled = false;
            }
        }

        /// <summary>
        /// Create line renderers on demand.
        /// </summary>
        private void EnsureRuntimeRenderers()
        {
            bool _drawOrbit = drawOrbitRuntime && (visualContext?.ShowOrbitLines ?? true);
            bool _drawAxis = drawAxisRuntime && (visualContext?.ShowSpinAxisLines ?? true);
            bool _drawWorldUp = drawWorldUpRuntime && (visualContext?.ShowWorldUpLines ?? true);
            bool _drawSpinDirection = drawSpinDirectionRuntime && (visualContext?.ShowSpinDirectionLines ?? true);

            if (orbitLine == null && _drawOrbit)
            {
                orbitLine = CreateLineRenderer("OrbitLine", orbitLineColor, orbitLineWidth, true);
            }

            if ((axisLine == null && _drawAxis) || (worldUpLine == null && _drawWorldUp))
            {
                axisLine = CreateLineRenderer("AxisLine", axisLineColor, axisLineWidth, false);
                worldUpLine = CreateLineRenderer("WorldUpLine", worldUpLineColor, axisLineWidth, false);
            }

            if (spinDirectionLine == null && _drawSpinDirection)
            {
                spinDirectionLine = CreateLineRenderer(
                    "SpinDirectionLine",
                    spinDirectionProgradeColor,
                    spinDirectionLineWidth,
                    false
                );
            }
        }

        /// <summary>
        /// Scale line widths based on global distance/radius settings.
        /// </summary>
        private bool ApplyRuntimeLineStyles()
        {
            float _scale = GetLineWidthScale();
            bool _applied = false;

            if (orbitLine != null)
            {
                float _width = Mathf.Max(0.0001f, orbitLineWidth * _scale * orbitLineDistanceScale);
                orbitLine.startWidth = _width;
                orbitLine.endWidth = _width;
                _applied = true;
            }

            if (axisLine != null)
            {
                float _widthScale = GetAxisLineWidthScale();
                float _thicknessMultiplier = Mathf.Clamp(axisLineThicknessMultiplier, 0.1f, 5.0f);
                float _width = Mathf.Max(
                    0.0001f,
                    axisLineWidth * _scale * 0.5f * _widthScale * _thicknessMultiplier
                );
                axisLine.startWidth = _width;
                axisLine.endWidth = _width;
                _applied = true;
            }

            if (worldUpLine != null)
            {
                float _widthScale = GetAxisLineWidthScale();
                float _thicknessMultiplier = Mathf.Clamp(axisLineThicknessMultiplier, 0.1f, 5.0f);
                float _width = Mathf.Max(
                    0.0001f,
                    axisLineWidth * _scale * 0.5f * _widthScale * _thicknessMultiplier
                );
                worldUpLine.startWidth = _width;
                worldUpLine.endWidth = _width;
                _applied = true;
            }

            if (spinDirectionLine != null)
            {
                float _widthScale = Mathf.Max(0.8f, GetAxisLineWidthScale());
                float _width = Mathf.Max(0.0001f, spinDirectionLineWidth * _scale * _widthScale);
                spinDirectionLine.startWidth = _width;
                spinDirectionLine.endWidth = _width;
                _applied = true;
            }

            return _applied;
        }

        /// <summary>
        /// Compute a width scale from global distance/radius multipliers.
        /// </summary>
        private float GetLineWidthScale()
        {
            if (visualContext == null)
            {
                return 1.0f;
            }

            float _distance = (float)visualContext.GlobalDistanceMultiplier;
            float _radius = (float)visualContext.GlobalRadiusMultiplier;
            float _avg = (_distance + _radius) * 0.5f;

            float _base = Mathf.Clamp(_avg, 0.2f, 2.0f);
            return _base * Mathf.Clamp(visualContext.RuntimeLineWidthScale, 0.1f, 2.0f);
        }

        /// <summary>
        /// Compute the axis/world-up line width scale.
        /// </summary>
        private float GetAxisLineWidthScale()
        {
            float _scale = axisLineDistanceScale;
            float _sizeScale = GetAxisLineSizeWidthScale();
            _scale *= _sizeScale;

            if (string.Equals(type, "star", StringComparison.OrdinalIgnoreCase) &&
                visualContext != null &&
                visualContext.RuntimeLineWidthScale < 1.0f)
            {
                _scale *= 2.0f;
            }

            return _scale;
        }

        /// <summary>
        /// Compute axis line length scale based on object size.
        /// </summary>
        private float GetAxisLineSizeScale()
        {
            if (visualContext != null && visualContext.RuntimeLineWidthScale < 1.0f)
            {
                return 1.0f;
            }

            float _reference = Mathf.Max(0.001f, axisLineSizeReference);
            float _scale = transform.localScale.x / _reference;
            _scale = Mathf.Clamp(_scale, axisLineSizeMinScale, axisLineSizeMaxScale);

            if (string.Equals(type, "star", StringComparison.OrdinalIgnoreCase))
            {
                _scale = Mathf.Min(_scale, axisLineSizeStarMaxScale);
            }

            return _scale;
        }

        /// <summary>
        /// Clamp axis line width scaling to avoid overly thick lines.
        /// </summary>
        private float GetAxisLineSizeWidthScale()
        {
            float _scale = GetAxisLineSizeScale();
            return Mathf.Min(_scale, axisLineSizeWidthMaxScale);
        }

        /// <summary>
        /// Apply extra axis line length for small planets if enabled.
        /// </summary>
        private float GetAxisLineSmallBodyLengthScale()
        {
            if (visualContext != null && visualContext.RuntimeLineWidthScale < 1.0f)
            {
                return 1.0f;
            }

            if (!string.Equals(type, "planet", StringComparison.OrdinalIgnoreCase))
            {
                return 1.0f;
            }

            float _threshold = Mathf.Max(0.01f, axisLineSmallBodyDiameterThreshold);
            if (BaseDiameterUnity >= _threshold)
            {
                return 1.0f;
            }

            return axisLineSmallBodyLengthScale;
        }

        /// <summary>
        /// Update axis line scaling based on camera distance.
        /// </summary>
        private bool UpdateAxisLineDistanceScale()
        {
            float _scale = 1.0f;

            if (scaleAxisLinesByCameraDistance)
            {
                Camera? _camera = GetLineScaleCamera();
                if (_camera != null)
                {
                    float _reference = Mathf.Max(0.01f, axisLineDistanceReference);
                    float _distance = Vector3.Distance(_camera.transform.position, transform.position);
                    float _raw = Mathf.Max(1e-4f, _distance / _reference);
                    float _exponent = Mathf.Max(0.1f, axisLineDistanceExponent);
                    _scale = Mathf.Pow(_raw, _exponent);
                    _scale = Mathf.Clamp(_scale, axisLineDistanceMinScale, axisLineDistanceMaxScale);
                }
            }

            if (Mathf.Approximately(axisLineDistanceScale, _scale))
            {
                return false;
            }

            axisLineDistanceScale = _scale;
            return true;
        }

        /// <summary>
        /// Update orbit line scaling based on camera distance.
        /// </summary>
        private bool UpdateOrbitLineDistanceScale()
        {
            float _scale = 1.0f;

            if (scaleOrbitLinesByCameraDistance)
            {
                Camera? _camera = GetLineScaleCamera();
                if (_camera != null)
                {
                    float _reference = Mathf.Max(0.01f, orbitLineDistanceReference);
                    float _distance = Vector3.Distance(_camera.transform.position, transform.position);
                    float _boost = Mathf.Max(0.1f, orbitLineDistanceScaleBoost);
                    float _raw = Mathf.Max(1e-4f, (_distance / _reference) * _boost);
                    float _nearExponentMultiplier = Mathf.Max(0.1f, orbitLineDistanceNearExponentMultiplier);
                    float _exponent = _raw <= 1.0f
                        ? Mathf.Max(0.1f, orbitLineDistanceExponentNear * _nearExponentMultiplier)
                        : Mathf.Max(0.1f, orbitLineDistanceExponent);
                    _scale = Mathf.Pow(_raw, _exponent);

                    float _maxScaleMultiplier = Mathf.Max(0.1f, orbitLineDistanceMaxScaleMultiplier);
                    float _maxScale = orbitLineDistanceMaxScale * _maxScaleMultiplier;
                    if (isHypothetical)
                    {
                        float _focusDistance = Mathf.Max(0.01f, hypotheticalOrbitLineFocusDistance);
                        if (_distance > _focusDistance)
                        {
                            float _farScale = Mathf.Max(_maxScale, hypotheticalOrbitLineFarScale);
                            _scale = _farScale;
                            _maxScale = _farScale;
                        }
                        else
                        {
                            float _mult = Mathf.Max(1.0f, hypotheticalOrbitLineFocusScaleMultiplier);
                            _scale *= _mult;
                        }
                    }

                    _scale = Mathf.Clamp(_scale, orbitLineDistanceMinScale, _maxScale);
                }
            }

            if (Mathf.Approximately(orbitLineDistanceScale, _scale))
            {
                return false;
            }

            orbitLineDistanceScale = _scale;
            return true;
        }

        /// <summary>
        /// Resolve or build a shared line material.
        /// </summary>
        private static Material GetLineMaterial()
        {
            if (lineMaterial != null)
            {
                return lineMaterial;
            }

            Shader _shader = Shader.Find("Sprites/Default");
            if (_shader == null)
            {
                _shader = Shader.Find("Unlit/Color");
            }

            lineMaterial = new Material(_shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            return lineMaterial;
        }

        /// <summary>
        /// Create a new LineRenderer child with standard settings.
        /// </summary>
        private LineRenderer CreateLineRenderer(string _name, Color _color, float _width, bool _loop)
        {
            GameObject _go = new GameObject(_name);
            _go.transform.SetParent(transform, false);

            LineRenderer _lr = _go.AddComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.material = GetLineMaterial();
            _lr.startColor = _color;
            _lr.endColor = _color;
            _lr.startWidth = _width;
            _lr.endWidth = _width;
            _lr.loop = _loop;
            _lr.positionCount = 0;
            _lr.enabled = false;

            return _lr;
        }
        #endregion

        #region Helpers
        private static double DegToRad(double _deg) => _deg * (Math.PI / 180.0);
        private static double TwoPi() => Math.PI * 2.0;

        private static double WrapAngleRad(double _rad)
        {
            double _twoPi = TwoPi();
            _rad %= _twoPi;
            if (_rad < 0.0)
            {
                _rad += _twoPi;
            }
            return _rad;
        }

        /// <summary>
        /// Resolve the camera used for line distance scaling.
        /// </summary>
        private static Camera? GetLineScaleCamera()
        {
            Camera _camera = Camera.main;
            if (_camera != null)
            {
                lineScaleCamera = _camera;
                return _camera;
            }

            if (lineScaleCamera != null && lineScaleCamera.enabled)
            {
                return lineScaleCamera;
            }

            Camera[] _all = Resources.FindObjectsOfTypeAll<Camera>();
            for (int _i = 0; _i < _all.Length; _i++)
            {
                Camera _candidate = _all[_i];
                if (_candidate == null)
                {
                    continue;
                }

                if (!_candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!_candidate.enabled || !_candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                lineScaleCamera = _candidate;
                return _candidate;
            }

            return null;
        }

        /// <summary>
        /// Solve Kepler's equation for eccentric anomaly.
        /// </summary>
        private static double SolveEccentricAnomaly(double _M, double _e)
        {
            double _E = (_e < 0.8) ? _M : Math.PI;

            for (int _i = 0; _i < 10; _i++)
            {
                double _f = _E - _e * Math.Sin(_E) - _M;
                double _fp = 1.0 - _e * Math.Cos(_E);
                _E -= _f / Math.Max(1e-12, _fp);
            }

            return _E;
        }
        #endregion
    }
}

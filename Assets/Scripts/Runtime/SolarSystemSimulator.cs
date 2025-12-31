#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Assets.Scripts.Data;
using Assets.Scripts.Loading;
using Assets.Scripts.Guis;
using Assets.Scripts.Helpers.Debugging;
using TMPro;

namespace Assets.Scripts.Runtime
{
    /// <summary>
    /// Loads the dataset, spawns solar objects, and advances the simulation.
    /// </summary>
    public sealed class SolarSystemSimulator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("JSON (Resources)")]
        // Resources path (no extension) to the solar system dataset.
        [SerializeField] private string resourcesJsonPathWithoutExtension =
            "SolarSystemData_J2000_Keplerian_all_moons";

        [Header("Prefabs (Resources)")]
        // Resources folder that contains planet/moon prefabs.
        [SerializeField] private string prefabsResourcesFolder = "SolarObjects";

        [Header("Runtime Controls")]
        [Tooltip("Enable runtime UI buttons and value labels for live tuning.")]
        [SerializeField] private bool enableRuntimeControls = true;

        [Header("Time")]
        // Simulation speed multiplier (sim seconds per real second).
        private float timeScale = 1.0f;

        [Header("Sun Light Presets")]
        // Normal visual preset values for the Sun point light.
        [SerializeField] private float sunLightNormalIntensity = 30000.0f;
        [SerializeField] private float sunLightNormalRange = 1000.0f;
        // Minimal visual preset values for the Sun point light.
        [SerializeField] private float sunLightMinimalIntensity = 25.0f;
        [SerializeField] private float sunLightMinimalRange = 1000.0f;

        [Header("Hypothetical Objects")]
        [SerializeField] private bool showHypotheticalObjects = false;

        [Header("Debug")]
        // Log per-object spawn data after initialization.
        [SerializeField] private bool logSpawnedSolarObjectData = true;

        [Header("Simulation Scale Profile")]
        // Scaling knobs used only when the Simulation preset is active.
        [SerializeField] private float simulationRadiusScaleAll = 0.8f;
        [SerializeField] private float simulationSmallPlanetRadiusScale = 0.625f;
        [SerializeField] private float simulationLargePlanetRadiusScale = 1.0f;
        [SerializeField] private float simulationMoonRadiusScale = 0.4f;
        [SerializeField] private float simulationDwarfRadiusScale = 0.625f;
        [SerializeField] private float simulationOtherRadiusScale = 0.8f;
        // Threshold (km) that separates small vs large planet scaling.
        [SerializeField] private float simulationSmallPlanetRadiusKmThreshold = 9000.0f;
        // Extra spacing bias for inner planets (order-based).
        [SerializeField] private float simulationInnerPlanetSpacingBias = 0.15f;
        // Highest order index considered "inner" for spacing bias.
        [SerializeField] private int simulationInnerPlanetMaxOrder = 4;
        [SerializeField] private float simulationPlanetDistanceScaleAll = 1.0f;
        // Extra distance scaling for outer planets.
        [SerializeField] private float simulationOuterPlanetDistanceScale = 2.0f;
        // Lowest order index considered "outer" for scaling.
        [SerializeField] private int simulationOuterPlanetMinOrder = 5;
        // AU cutoff to treat dwarf planets as outer objects.
        [SerializeField] private float simulationDwarfOuterDistanceAuThreshold = 5.0f;
        // Extra distance scaling for inner dwarf planets.
        [SerializeField] private float simulationInnerDwarfDistanceScale = 1.45f;
        // Per-moon distance multiplier used only in Simulation.
        [SerializeField] private float simulationMoonDistanceScale = 0.9f;
        // Align moon orbits to primary axial tilt unless overridden per moon.
        [SerializeField] private bool alignMoonOrbitsToPrimaryTilt = true;

        #endregion

        #region Runtime State
        // Accumulated simulation time in seconds.
        private double simulationTimeSeconds = 0.0;
        // Loaded datasets and lookup tables.
        private SolarSystemJsonLoader.Result? simulationDatabase;
        private SolarSystemJsonLoader.Result? activeDatabase;

        // Spawned SolarObject instances keyed by id.
        private readonly Dictionary<string, SolarObject> instancesById =
            new(StringComparer.OrdinalIgnoreCase);

        // Spawned solar objects in display order for UI.
        private readonly List<SolarObject> orderedSolarObjects = new();

        /// <summary>
        /// Read-only list of spawned solar objects in display order.
        /// </summary>
        public IReadOnlyList<SolarObject> OrderedSolarObjects => orderedSolarObjects;

        /// <summary>
        /// Raised after solar objects are spawned and initialized.
        /// </summary>
        public event Action<IReadOnlyList<SolarObject>>? SolarObjectsReady;

        /// <summary>
        /// Raised when the visual preset changes.
        /// </summary>
        public event Action<int>? VisualPresetChanged;

        /// <summary>
        /// Current visibility state for hypothetical objects.
        /// </summary>
        public bool ShowHypotheticalObjects => showHypotheticalObjects;

        /// <summary>
        /// Current visual preset index.
        /// </summary>
        public int VisualPresetLevelIndex => visualPresetLevelIndex;

        /// <summary>
        /// Current simulation time scale.
        /// </summary>
        public float TimeScale => timeScale;

        // Prefabs found in Resources by name.
        private readonly Dictionary<string, GameObject> prefabsByName =
            new(StringComparer.OrdinalIgnoreCase);

        // Global visual scaling and defaults shared by all solar objects.
        private readonly SolarObject.VisualContext visualContext = new();
        private double defaultGlobalDistanceMultiplier = 1.0;
        private double defaultGlobalRadiusMultiplier = 1.0;
        private int defaultOrbitSegments = 256;

        // Guard for runtime controls initialization.
        private bool guiInitialized = false;
        // Cached reference to the Sun point light.
        private Light? sunPointLight = null;
        private bool sunPointLightLookupAttempted = false;

        // Time label update throttle.
        private float timeLabelUpdateTimer = 0.0f;

        // One-time guard for spawn data logging.
        private bool spawnDataLogged = false;

        #endregion

        #region Runtime Control Levels
        // Human-readable labels for control levels.
        private readonly string[] visualPresetLevelNames = { "Realistic", "Simulation" };

        // Level values mapped to control indices.
        // Index 0 = default/realistic, Index 1 = simulation (unless noted).
        private readonly float[] timeScaleLevelValues = new float[4];
        private readonly float[] visualPresetDistanceValues = new float[2];
        private readonly float[] visualPresetRadiusValues = new float[2];
        private readonly int[] visualPresetOrbitSegmentsValues = new int[2];

        // Active indices for each control level.
        private int timeScaleLevelIndex = 0;
        private int visualPresetLevelIndex = 0;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Load data, build context, and spawn all solar objects.
        /// </summary>
        private void Awake()
        {
            Application.targetFrameRate = 60;

            visualPresetLevelIndex = enableRuntimeControls ? 1 : 0;

            simulationDatabase = SolarSystemJsonLoader.LoadOrLog(resourcesJsonPathWithoutExtension);
            if (simulationDatabase == null)
            {
                HelpLogs.Error(
                    "Simulator",
                    $"Failed to load dataset '{resourcesJsonPathWithoutExtension}'. Simulator is not active."
                );
                enabled = false;
                return;
            }

            activeDatabase = simulationDatabase;

            if (activeDatabase == null)
            {
                HelpLogs.Error("Simulator", "No dataset available to initialize the simulator.");
                enabled = false;
                return;
            }

            LoadPrefabsFromResources();

            ApplyDatabase(activeDatabase, visualPresetLevelIndex, true);
            ApplyHypotheticalVisibility();
            SolarObjectsReady?.Invoke(orderedSolarObjects);

            HelpLogs.Log("Simulator", $"Ready. Objects spawned: {instancesById.Count}");
        }

        /// <summary>
        /// Subscribe to runtime UI events.
        /// </summary>
        private void OnEnable()
        {
            if (!enableRuntimeControls)
            {
                return;
            }

            Gui.TimeScaleStepRequested += HandleTimeScaleStepRequested;
            Gui.VisualPresetStepRequested += HandleVisualPresetStepRequested;
            Gui.OrbitLinesToggled += HandleOrbitLinesToggled;
            Gui.SpinAxisToggled += HandleSpinAxisToggled;
            Gui.WorldUpToggled += HandleWorldUpToggled;
            Gui.SpinDirectionToggled += HandleSpinDirectionToggled;
            Gui.HypotheticalToggleChanged += HandleHypotheticalToggleChanged;
        }

        /// <summary>
        /// Initialize runtime controls after scene objects are ready.
        /// </summary>
        private void Start()
        {
            if (!enableRuntimeControls)
            {
                return;
            }

            Gui.Initialize();
            SetupRuntimeGui();
            guiInitialized = true;

            UpdateAppVersionText();
            ApplyVisualPresetLevel(visualPresetLevelIndex, true);
            UpdateTimeScaleText();
            UpdateHypotheticalToggleText();
        }

        /// <summary>
        /// Cleanup runtime UI references.
        /// Unsubscribe from runtime UI events.
        /// </summary>
        private void OnDestroy()
        {
            if (!enableRuntimeControls)
            {
                return;
            }

            Gui.TimeScaleStepRequested -= HandleTimeScaleStepRequested;
            Gui.VisualPresetStepRequested -= HandleVisualPresetStepRequested;
            Gui.OrbitLinesToggled -= HandleOrbitLinesToggled;
            Gui.SpinAxisToggled -= HandleSpinAxisToggled;
            Gui.WorldUpToggled -= HandleWorldUpToggled;
            Gui.SpinDirectionToggled -= HandleSpinDirectionToggled;
            Gui.HypotheticalToggleChanged -= HandleHypotheticalToggleChanged;

            Gui.UnInitialize();
        }

        /// <summary>
        /// Advance simulation and update runtime labels.
        /// </summary>
        private void Update()
        {
            if (activeDatabase == null)
            {
                return;
            }

            // Advance simulation clock and update solar objects.
            simulationTimeSeconds += Time.deltaTime * timeScale;

            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                _pair.Value.Simulate(simulationTimeSeconds);
            }

            if (enableRuntimeControls && guiInitialized)
            {
                timeLabelUpdateTimer += Time.deltaTime;
                if (timeLabelUpdateTimer >= 1.0f)
                {
                    timeLabelUpdateTimer = 0.0f;
                    UpdateTimeScaleText();
                }
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Build global visual settings from the dataset.
        /// </summary>
        private void BuildVisualContext(SolarSystemJsonLoader.Result _db)
        {
            GlobalVisualDefaultsData? _defaults = _db.Data.GlobalVisualDefaults;
            if (_defaults != null)
            {
                visualContext.DistanceKmPerUnityUnit = _defaults.DistanceKmPerUnityUnit;
                defaultGlobalDistanceMultiplier = _defaults.GlobalDistanceMultiplier;
                defaultGlobalRadiusMultiplier = _defaults.GlobalRadiusMultiplier;
                defaultOrbitSegments = Math.Max(64, _defaults.OrbitPathSegmentsDefault);

                visualContext.GlobalDistanceMultiplier = defaultGlobalDistanceMultiplier;
                visualContext.GlobalRadiusMultiplier = defaultGlobalRadiusMultiplier;
                visualContext.OrbitPathSegments = defaultOrbitSegments;
                visualContext.MoonClearanceUnity = _defaults.MoonClearanceUnity;
            }

            visualContext.SimulationRadiusScaleAll = simulationRadiusScaleAll;
            visualContext.SimulationSmallPlanetRadiusScale = simulationSmallPlanetRadiusScale;
            visualContext.SimulationLargePlanetRadiusScale = simulationLargePlanetRadiusScale;
            visualContext.SimulationMoonRadiusScale = simulationMoonRadiusScale;
            visualContext.SimulationDwarfRadiusScale = simulationDwarfRadiusScale;
            visualContext.SimulationOtherRadiusScale = simulationOtherRadiusScale;
            visualContext.SimulationSmallPlanetRadiusKmThreshold = simulationSmallPlanetRadiusKmThreshold;
            visualContext.SimulationInnerPlanetSpacingBias = simulationInnerPlanetSpacingBias;
            visualContext.SimulationInnerPlanetMaxOrder = simulationInnerPlanetMaxOrder;
            visualContext.SimulationPlanetDistanceScaleAll = simulationPlanetDistanceScaleAll;
            visualContext.SimulationOuterPlanetDistanceScale = simulationOuterPlanetDistanceScale;
            visualContext.SimulationOuterPlanetMinOrder = simulationOuterPlanetMinOrder;
            visualContext.SimulationDwarfOuterDistanceAuThreshold = simulationDwarfOuterDistanceAuThreshold;
            visualContext.SimulationInnerDwarfDistanceScale = simulationInnerDwarfDistanceScale;
            visualContext.SimulationMoonDistanceScale = simulationMoonDistanceScale;
            visualContext.AlignMoonOrbitsToPrimaryTilt = alignMoonOrbitsToPrimaryTilt;

            if (_db.ById.TryGetValue("sun", out SolarObjectData _sunData))
            {
                visualContext.ReferenceSolarObjectRadiusKm = _sunData.TruthPhysical?.MeanRadiusKm ?? 695700.0;
            }
        }

        #region Dataset Management
        /// <summary>
        /// Resolve the dataset to use for a given preset index.
        /// </summary>
        private SolarSystemJsonLoader.Result? GetDatabaseForPreset(int _presetIndex)
        {
            return simulationDatabase;
        }

        /// <summary>
        /// Switch the active dataset for a preset when needed.
        /// </summary>
        private bool EnsureActiveDatabaseForPreset(int _presetIndex)
        {
            SolarSystemJsonLoader.Result? _target = GetDatabaseForPreset(_presetIndex);
            if (_target == null || ReferenceEquals(_target, activeDatabase))
            {
                return false;
            }

            ApplyDatabase(_target, _presetIndex, false);
            return true;
        }

        /// <summary>
        /// Apply a dataset and refresh or respawn solar objects as required.
        /// </summary>
        private void ApplyDatabase(SolarSystemJsonLoader.Result _db, int _presetIndex, bool _forceRespawn)
        {
            activeDatabase = _db;
            BuildVisualContext(_db);
            UpdateVisualPresetDefaultsFromContext();
            ApplyVisualPresetValues(_presetIndex);

            bool _canReinit = CanReinitializeInPlace(_db);
            if (_forceRespawn || !_canReinit)
            {
                DestroyAllInstances();
                SpawnAll(_db);
            }

            InitializeAllTwoPass(_db);
            RebuildOrderedSolarObjects(_db);
            ApplyHypotheticalVisibility();
            LogSpawnedSolarObjects(_db);

            for (int _i = 0; _i < orderedSolarObjects.Count; _i++)
            {
                orderedSolarObjects[_i].Simulate(simulationTimeSeconds);
            }
        }

        /// <summary>
        /// Decide whether existing instances can be reinitialized in place.
        /// </summary>
        private bool CanReinitializeInPlace(SolarSystemJsonLoader.Result _db)
        {
            if (instancesById.Count == 0)
            {
                return false;
            }

            if (instancesById.Count != _db.ById.Count)
            {
                return false;
            }

            foreach (string _id in _db.ById.Keys)
            {
                if (!instancesById.ContainsKey(_id))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Destroy all spawned solar objects and clear cached lists.
        /// </summary>
        private void DestroyAllInstances()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                if (_pair.Value != null)
                {
                    Destroy(_pair.Value.gameObject);
                }
            }

            instancesById.Clear();
            orderedSolarObjects.Clear();
        }

        /// <summary>
        /// Rebuild the ordered list used by UI and focus systems.
        /// </summary>
        private void RebuildOrderedSolarObjects(SolarSystemJsonLoader.Result _db)
        {
            orderedSolarObjects.Clear();

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
                if (instancesById.TryGetValue(_data.Id, out SolarObject _instance))
                {
                    orderedSolarObjects.Add(_instance);
                }
            }
        }
        #endregion

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

        /// <summary>
        /// Load all prefabs from Resources into a name lookup table.
        /// </summary>
        private void LoadPrefabsFromResources()
        {
            prefabsByName.Clear();

            GameObject[] _prefabs = Resources.LoadAll<GameObject>(prefabsResourcesFolder);
            if (_prefabs.Length == 0)
            {
                HelpLogs.Warn(
                    "Simulator",
                    $"No prefabs found in Resources/{prefabsResourcesFolder}."
                );
            }
            for (int _i = 0; _i < _prefabs.Length; _i++)
            {
                GameObject _p = _prefabs[_i];
                if (_p == null)
                {
                    continue;
                }

                if (prefabsByName.ContainsKey(_p.name))
                {
                    continue;
                }

                prefabsByName.Add(_p.name, _p);
            }
        }

        /// <summary>
        /// Spawn all solar objects from the dataset.
        /// </summary>
        private void SpawnAll(SolarSystemJsonLoader.Result _db)
        {
            instancesById.Clear();
            orderedSolarObjects.Clear();

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

                GameObject _prefab = GetPrefabOrTemplate(_data);
                GameObject _go = Instantiate(_prefab);
                _go.name = string.IsNullOrWhiteSpace(_data.DisplayName) ? _data.Id : _data.DisplayName;

                SolarObject _solarObject = _go.GetComponent<SolarObject>();
                if (_solarObject == null)
                {
                    _solarObject = _go.AddComponent<SolarObject>();
                }

                instancesById[_data.Id] = _solarObject;
                orderedSolarObjects.Add(_solarObject);
            }
        }

        /// <summary>
        /// Initialize SolarObject instances (reference first, then dependents).
        /// </summary>
        private void InitializeAllTwoPass(SolarSystemJsonLoader.Result _db)
        {
            // Pass 1: reference solar object (Sun).
            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                SolarObjectData _data = _db.ById[_pair.Key];
                if (!_data.IsReference)
                {
                    continue;
                }

                _pair.Value.Initialize(_data, null, null, visualContext);
            }

            if (instancesById.TryGetValue("sun", out SolarObject _sun))
            {
                visualContext.ReferenceSolarObjectDiameterUnity = _sun.transform.localScale.x;
            }

            // Pass 2: all other solar objects, resolved to their primary transforms.
            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                SolarObjectData _data = _db.ById[_pair.Key];
                if (_data.IsReference)
                {
                    continue;
                }

                Transform? _primaryTransform = null;
                SolarObject? _primarySolarObject = null;
                if (!string.IsNullOrWhiteSpace(_data.PrimaryId) &&
                    instancesById.TryGetValue(_data.PrimaryId, out SolarObject _primary))
                {
                    _primaryTransform = _primary.transform;
                    _primarySolarObject = _primary;
                }

                _pair.Value.Initialize(_data, _primaryTransform, _primarySolarObject, visualContext);
            }
        }

        /// <summary>
        /// Resolve a prefab by id/name, falling back to Template or a primitive.
        /// </summary>
        private GameObject GetPrefabOrTemplate(SolarObjectData _data)
        {
            if (prefabsByName.TryGetValue(_data.Id, out GameObject _p))
            {
                return _p;
            }

            if (!string.IsNullOrWhiteSpace(_data.DisplayName) &&
                prefabsByName.TryGetValue(_data.DisplayName, out GameObject _pd))
            {
                return _pd;
            }

            if (prefabsByName.TryGetValue("Template", out GameObject _t))
            {
                HelpLogs.Warn("Simulator", $"Prefab missing for '{_data.Id}'. Using 'Template'.");
                return _t;
            }

            HelpLogs.Warn(
                "Simulator",
                $"Prefab missing for '{_data.Id}' and no Template found. Using Unity sphere."
            );
            return GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        #endregion

        #region Runtime Controls
        /// <summary>
        /// Configure runtime control values and defaults.
        /// </summary>
        private void UpdateVisualPresetDefaultsFromContext()
        {
            visualPresetDistanceValues[0] = (float)defaultGlobalDistanceMultiplier;
            visualPresetDistanceValues[1] = 0.02f;

            visualPresetRadiusValues[0] = (float)defaultGlobalRadiusMultiplier;
            visualPresetRadiusValues[1] = 0.25f;

            visualPresetOrbitSegmentsValues[0] = 128;
            visualPresetOrbitSegmentsValues[1] = 64;
        }

        /// <summary>
        /// Apply a preset to the shared visual context.
        /// </summary>
        private void ApplyVisualPresetValues(int _levelIndex)
        {
            int _clamped = Mathf.Clamp(_levelIndex, 0, visualPresetLevelNames.Length - 1);
            bool _isRealistic = _clamped == 0;
            visualContext.GlobalDistanceMultiplier = visualPresetDistanceValues[_clamped];
            visualContext.GlobalRadiusMultiplier = visualPresetRadiusValues[_clamped];
            visualContext.OrbitPathSegments = visualPresetOrbitSegmentsValues[_clamped];
            visualContext.RuntimeLineWidthScale = _clamped == 0 ? 1.0f : 0.25f;
            visualContext.UseVisualDefaults = !enableRuntimeControls || !_isRealistic;
            visualContext.UseSimulationScaleProfile = enableRuntimeControls && !_isRealistic;
        }

        /// <summary>
        /// Initialize runtime UI state and sync toggle defaults.
        /// </summary>
        private void SetupRuntimeGui()
        {
            if (!enableRuntimeControls)
            {
                return;
            }

            // Discrete control levels that map to curated values.
            timeScaleLevelValues[0] = timeScale;
            timeScaleLevelValues[1] = 1_000.0f;
            timeScaleLevelValues[2] = 10_000.0f;
            timeScaleLevelValues[3] = 200_000.0f;

            timeScaleLevelIndex = 1;
            timeScale = timeScaleLevelValues[timeScaleLevelIndex];

            UpdateVisualPresetDefaultsFromContext();
            visualPresetLevelIndex = 1;

            if (Gui.OrbitLinesToggle != null)
            {
                visualContext.ShowOrbitLines = Gui.OrbitLinesToggle.isOn;
            }

            if (Gui.SpinAxisToggle != null)
            {
                visualContext.ShowSpinAxisLines = Gui.SpinAxisToggle.isOn;
            }

            if (Gui.WorldUpToggle != null)
            {
                visualContext.ShowWorldUpLines = Gui.WorldUpToggle.isOn;
            }

            if (Gui.SpinDirectionToggle != null)
            {
                visualContext.ShowSpinDirectionLines = Gui.SpinDirectionToggle.isOn;
            }

            if (Gui.HypotheticalToggle != null)
            {
                showHypotheticalObjects = Gui.HypotheticalToggle.isOn;
                ApplyHypotheticalVisibility();
                SolarObjectsReady?.Invoke(orderedSolarObjects);
            }
        }

        /// <summary>
        /// Step the time scale level by a delta.
        /// </summary>
        private void HandleTimeScaleStepRequested(int _delta)
        {
            if (!guiInitialized)
            {
                return;
            }

            int _step = _delta == 0 ? 0 : Math.Sign(_delta);
            int _targetIndex = Mathf.Clamp(
                timeScaleLevelIndex + _step,
                0,
                timeScaleLevelValues.Length - 1
            );

            if (_targetIndex == timeScaleLevelIndex)
            {
                return;
            }

            timeScaleLevelIndex = _targetIndex;
            timeScale = timeScaleLevelValues[timeScaleLevelIndex];

            UpdateTimeScaleText();
        }

        /// <summary>
        /// Step the visual preset level by a delta.
        /// </summary>
        private void HandleVisualPresetStepRequested(int _delta)
        {
            if (!guiInitialized)
            {
                return;
            }

            int _step = _delta == 0 ? 0 : Math.Sign(_delta);
            int _targetIndex = Mathf.Clamp(
                visualPresetLevelIndex + _step,
                0,
                visualPresetLevelNames.Length - 1
            );

            if (_targetIndex == visualPresetLevelIndex)
            {
                return;
            }

            ApplyVisualPresetLevel(_targetIndex, true);
        }

        /// <summary>
        /// Toggle orbit line rendering for all objects.
        /// </summary>
        private void HandleOrbitLinesToggled(bool _enabled)
        {
            if (!guiInitialized)
            {
                return;
            }

            visualContext.ShowOrbitLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Toggle spin axis line rendering for all objects.
        /// </summary>
        private void HandleSpinAxisToggled(bool _enabled)
        {
            if (!guiInitialized)
            {
                return;
            }

            visualContext.ShowSpinAxisLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Toggle world-up line rendering for all objects.
        /// </summary>
        private void HandleWorldUpToggled(bool _enabled)
        {
            if (!guiInitialized)
            {
                return;
            }

            visualContext.ShowWorldUpLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Toggle spin-direction arc rendering for all objects.
        /// </summary>
        private void HandleSpinDirectionToggled(bool _enabled)
        {
            if (!guiInitialized)
            {
                return;
            }

            visualContext.ShowSpinDirectionLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Handle the Planet X toggle and update visibility.
        /// </summary>
        private void HandleHypotheticalToggleChanged(bool _enabled)
        {
            if (!guiInitialized)
            {
                return;
            }

            if (showHypotheticalObjects == _enabled)
            {
                return;
            }

            showHypotheticalObjects = _enabled;
            ApplyHypotheticalVisibility();
            UpdateHypotheticalToggleText();
            SolarObjectsReady?.Invoke(orderedSolarObjects);
        }

        /// <summary>
        /// Re-apply visual scaling to all objects.
        /// </summary>
        private void RefreshAllVisuals()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                _pair.Value.RefreshVisuals(visualContext);
            }
        }

        /// <summary>
        /// Mark runtime line widths as dirty for all objects.
        /// </summary>
        private void MarkAllLineStylesDirty()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                _pair.Value.MarkLineStylesDirty();
            }
        }

        /// <summary>
        /// Activate or hide hypothetical solar objects in the scene.
        /// </summary>
        private void ApplyHypotheticalVisibility()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in instancesById)
            {
                SolarObject _object = _pair.Value;
                if (!_object.IsHypothetical)
                {
                    continue;
                }

                if (_object.gameObject.activeSelf != showHypotheticalObjects)
                {
                    _object.gameObject.SetActive(showHypotheticalObjects);
                }
            }
        }

        /// <summary>
        /// Update the time scale label.
        /// </summary>
        private void UpdateTimeScaleText()
        {
            double _simDays = simulationTimeSeconds / 86400.0;

            if (Gui.TimeScaleValueText != null)
            {
                Gui.TimeScaleValueText.text = $"{timeScale:0.##}x\n({_simDays:0.00} d)";
            }
        }

        /// <summary>
        /// Update the application version label once at startup.
        /// </summary>
        private void UpdateAppVersionText()
        {
            if (Gui.AppVersionText == null)
            {
                return;
            }

            string _version = string.IsNullOrWhiteSpace(Application.version) ? "0.0.0" : Application.version;
            Gui.AppVersionText.text = $"v{_version}";
        }

        /// <summary>
        /// Update the visual preset label with all current settings.
        /// </summary>
        private void UpdateVisualPresetText()
        {
            if (Gui.VisualPresetValueText == null)
            {
                return;
            }

            string _label = visualPresetLevelNames[
                Mathf.Clamp(visualPresetLevelIndex, 0, visualPresetLevelNames.Length - 1)
            ];

            Gui.VisualPresetValueText.text = _label;
        }

        /// <summary>
        /// Update the Planet X toggle label text.
        /// </summary>
        private void UpdateHypotheticalToggleText()
        {
            TextMeshProUGUI? _text = null;
            if (Gui.HypotheticalToggle != null)
            {
                Gui.HypotheticalToggle.SetIsOnWithoutNotify(showHypotheticalObjects);
                _text = Gui.HypotheticalToggle.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (_text == null)
            {
                return;
            }

            _text.text = showHypotheticalObjects ? "Planet X: On" : "Planet X: Off";
        }

        /// <summary>
        /// Apply a visual preset and optionally refresh visuals.
        /// </summary>
        private void ApplyVisualPresetLevel(int _levelIndex, bool _refreshVisuals)
        {
            int _clamped = Mathf.Clamp(_levelIndex, 0, visualPresetLevelNames.Length - 1);
            bool _datasetChanged = EnsureActiveDatabaseForPreset(_clamped);
            if (_clamped == visualPresetLevelIndex && !_refreshVisuals && !_datasetChanged)
            {
                return;
            }

            visualPresetLevelIndex = _clamped;
            ApplyVisualPresetValues(visualPresetLevelIndex);

            VisualPresetChanged?.Invoke(visualPresetLevelIndex);

            string _label = visualPresetLevelNames[
                Mathf.Clamp(visualPresetLevelIndex, 0, visualPresetLevelNames.Length - 1)
            ];

            HelpLogs.Log(
                "Simulator",
                $"Visual preset {_label}: distance {visualContext.GlobalDistanceMultiplier:0.###}, " +
                $"radius {visualContext.GlobalRadiusMultiplier:0.###}, segments {visualContext.OrbitPathSegments}"
            );

            ApplySunLightPreset();

            if (_refreshVisuals)
            {
                RefreshAllVisuals();
            }

            MarkAllLineStylesDirty();
            UpdateVisualPresetText();

            if (_datasetChanged)
            {
                SolarObjectsReady?.Invoke(orderedSolarObjects);
            }
        }

        /// <summary>
        /// Apply the Sun point light values based on the current visual preset.
        /// </summary>
        private void ApplySunLightPreset()
        {
            Light? _sunLight = GetSunPointLight();
            if (_sunLight == null)
            {
                return;
            }

            if (_sunLight.type != LightType.Point)
            {
                HelpLogs.Warn("Simulator", "Sun light is not a Point light. Check the Sun prefab.");
            }

            bool _isNormal = visualPresetLevelIndex == 0;
            float _targetIntensity = _isNormal ? sunLightNormalIntensity : sunLightMinimalIntensity;
            float _targetRange = _isNormal ? sunLightNormalRange : sunLightMinimalRange;
            float _targetIndirect = 0.0f;

            bool _needsUpdate =
                !Mathf.Approximately(_sunLight.intensity, _targetIntensity) ||
                !Mathf.Approximately(_sunLight.range, _targetRange) ||
                !Mathf.Approximately(_sunLight.bounceIntensity, _targetIndirect);

            if (_needsUpdate)
            {
                string _presetLabel = visualPresetLevelNames[
                    Mathf.Clamp(visualPresetLevelIndex, 0, visualPresetLevelNames.Length - 1)
                ];

                HelpLogs.Warn(
                    "Simulator",
                    $"Sun light values did not match preset '{_presetLabel}'. Applying intensity " +
                    $"{_targetIntensity:0.###}, range {_targetRange:0.###}."
                );
            }

            _sunLight.intensity = _targetIntensity;
            _sunLight.range = _targetRange;
            _sunLight.bounceIntensity = _targetIndirect;
        }

        /// <summary>
        /// Locate the Sun point light once and cache it for future updates.
        /// </summary>
        private Light? GetSunPointLight()
        {
            if (sunPointLight != null)
            {
                return sunPointLight;
            }

            if (sunPointLightLookupAttempted)
            {
                return null;
            }

            sunPointLightLookupAttempted = true;

            if (!instancesById.TryGetValue("sun", out SolarObject _sun))
            {
                HelpLogs.Warn("Simulator", "Sun solar object not found. Cannot resolve its point light.");
                return null;
            }

            Light[] _lights = _sun.GetComponentsInChildren<Light>(true);
            if (_lights.Length == 0)
            {
                HelpLogs.Warn("Simulator", "No Light component found under the Sun solar object.");
                return null;
            }

            for (int _i = 0; _i < _lights.Length; _i++)
            {
                if (_lights[_i].type == LightType.Point)
                {
                    sunPointLight = _lights[_i];
                    return sunPointLight;
                }
            }

            sunPointLight = _lights[0];
            HelpLogs.Warn("Simulator", "Sun light is not Point. Using the first Light found.");
            return sunPointLight;
        }
        #endregion

    }
}

#nullable enable
using System;
using System.Collections.Generic;
using Assets.Scripts.Guis;
using Assets.Scripts.Helpers.Debugging;
using Assets.Scripts.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Scripts.Cameras
{
    /// <summary>
    /// Switches between focus and overview Cinemachine virtual cameras.
    /// </summary>
    public sealed class SolarSystemCameraController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Virtual Cameras")]
        // Auto-located by name if left empty.
        private CinemachineCamera? focusVirtualCamera;
        // Auto-located by name if left empty.
        private CinemachineCamera? overviewVirtualCamera;

        [Header("Virtual Camera Names")]
        [SerializeField] private string focusVirtualCameraName = "Follow-SolarObject-VCinemachine";
        [SerializeField] private string overviewVirtualCameraName = "Overview-SolarSystem-VCinemachine";

        [Header("Proxy Targets")]
        // Optional proxy objects used for smooth camera motion.
        private Transform? focusProxy;
        // Optional proxy objects used for smooth camera motion.
        private Transform? overviewProxy;

        [Header("Proxy Names")]
        [SerializeField] private string focusProxyName = "Focus_Proxy";
        [SerializeField] private string overviewProxyName = "Overview_Proxy";

        // Overview target assigned at runtime (usually the Sun).
        private Transform? overviewTarget;
        private Transform? focusTarget;

        [Header("Proxy Smoothing")]
        // Smoothly move proxies toward their targets.
        [SerializeField] private bool smoothProxyMovement = true;
        [SerializeField] private float proxySmoothTime = 0.2f;
        [SerializeField] private float proxyMaxSpeed = 200f;
        [SerializeField] private float proxyMinSpeed = 2f;
        [SerializeField] private float proxySpeedDistanceRange = 30f;

        [Header("Zoom Smoothing")]
        // Smoothly interpolate camera distance changes.
        [SerializeField] private bool smoothZoomDistance = true;
        [SerializeField] private float zoomSmoothTime = 0.16f;
        [SerializeField] private float zoomMaxSpeed = 200f;
        [SerializeField] private float zoomMinSpeed = 0.1f;
        [SerializeField] private float zoomSpeedDistanceRange = 3f;
        [SerializeField] private float focusSwitchSmoothTimeMultiplier = 2.0f;
        [SerializeField] private float focusSwitchSpeedMultiplier = 0.35f;
        [SerializeField] private float focusSwitchSmoothDuration = 0.6f;

        [Header("Priorities")]
        [SerializeField] private int focusPriority = 20;
        [SerializeField] private int overviewPriority = 10;

        [Header("Focus Distance")]
        // Base distances used for focus camera framing.
        [SerializeField] private float focusDistanceSmall = 0.35f;
        [SerializeField] private float focusDistanceMedium = 0.6f;
        [SerializeField] private float focusDistanceSun = 1.8f;
        [SerializeField] private float focusMediumSizeThreshold = 0.3f;

        [Header("Zoom Controls")]
        // Focus zoom limits expressed as multipliers of base distance.
        [SerializeField] private float focusZoomMinMultiplier = 0.15f;
        [SerializeField] private float focusZoomMaxMultiplier = 0.75f;
        // Absolute minimum distance for the Sun (before allowance).
        [SerializeField] private float focusZoomMinDistanceSun = 1.5f;
        // Extra min-distance padding for large bodies in Simulation.
        [SerializeField] private float simulationLargeBodyMinDistanceOffset = 0.0765f;
        // Allow extra zoom-in on large bodies beyond base minimum.
        [SerializeField] private float focusLargeBodyZoomInAllowance = 0.0f;
        // Allow extra zoom-in on the Sun beyond the base minimum.
        [SerializeField] private float focusSunZoomInAllowance = 0.5390625f;
        // Hard clamp to prevent camera from going too close.
        [SerializeField] private float focusZoomAbsoluteMinDistance = 0.05f;
        [SerializeField] private float overviewZoomMinDistance = 2f;
        [SerializeField] private float overviewZoomMaxDistance = 20f;
        [SerializeField] private float overviewDefaultDistance = 5f;
        [SerializeField] private float overviewZoomSpeedMultiplier = 5f;
        [SerializeField] private float focusZoomStep = 0.015f;
        [SerializeField] private float zoomStep = 0.1f;

        [Header("Realistic Preset Overrides")]
        // Overrides used when Realistic preset is active.
        [SerializeField] private float realisticFocusZoomMinMultiplier = 0.6f;
        [SerializeField] private float realisticOverviewZoomMinDistance = 50f;
        [SerializeField] private float realisticOverviewZoomMaxDistance = 125f;
        [SerializeField] private float realisticOverviewZoomSpeedMultiplier = 10f;

        [Header("Orbit Controls")]
        // Orbit step size in world units (scaled to degrees by radius).
        [SerializeField] private float orbitStep = 0.1f;
        [SerializeField] private float focusOrbitMaxOffset = 1f;
        [SerializeField] private float overviewOrbitMaxOffset = 5f;
        [SerializeField] private float orbitMaxPitchDegrees = 80f;
        [SerializeField] private float orbitRadiusMaxFactor = 0.5f;
        [SerializeField] private float orbitStepMaxDegrees = 10f;
        // Faster orbit steps when overview is far away.
        [SerializeField] private float overviewOrbitStepDistanceMultiplier = 2.5f;
        [SerializeField] private float overviewOrbitStepBoostDistance = 25f;

        #endregion

        #region Constants
        private const float HighSpeedTimeScaleThreshold = 200000f;
        private const float HighSpeedProxySmoothTimeMultiplier = 0.2f;
        private const float HighSpeedProxySpeedMultiplier = 4f;
        private const float HighSpeedZoomSmoothTimeMultiplier = 0.25f;
        private const float HighSpeedZoomSpeedMultiplier = 3f;
        #endregion

        #region Runtime State
        private bool isInitialized = false;
        private CinemachinePositionComposer? focusPositionComposer;
        private CinemachinePositionComposer? overviewPositionComposer;
        private SolarObject? focusSolarObject;
        private float overviewBaseDistance = 0f;
        private float focusZoomOffset = 0f;
        private float overviewZoomOffset = 0f;
        private Vector3 focusOrbitOffset = Vector3.zero;
        private Vector3 overviewOrbitOffset = Vector3.zero;
        private float focusOrbitYaw = 0f;
        private float focusOrbitPitch = 0f;
        private float overviewOrbitYaw = 0f;
        private float overviewOrbitPitch = 0f;
        private bool focusOrbitInitialized = false;
        private bool overviewOrbitInitialized = false;
        private bool isRealisticPreset = false;
        private SolarSystemSimulator? simulator = null;
        private Vector3 focusProxyVelocity = Vector3.zero;
        private Vector3 overviewProxyVelocity = Vector3.zero;
        private float focusDesiredDistance = 0f;
        private float overviewDesiredDistance = 0f;
        private float focusDistanceVelocity = 0f;
        private float overviewDistanceVelocity = 0f;
        private float focusSwitchTimer = 0f;
        #endregion

        #region Public API
        /// <summary>
        /// True when the overview target is assigned.
        /// </summary>
        public bool HasOverviewTarget => overviewTarget != null;

        /// <summary>
        /// Initialize references and apply the default overview.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                HelpLogs.Warn("Camera", "SolarSystemCameraController already initialized.");
                return;
            }

            if (focusVirtualCamera == null)
            {
                focusVirtualCamera = FindVirtualCameraByName(focusVirtualCameraName);
            }

            if (overviewVirtualCamera == null)
            {
                overviewVirtualCamera = FindVirtualCameraByName(overviewVirtualCameraName);
            }

            if (focusVirtualCamera != null)
            {
                focusPositionComposer = focusVirtualCamera.GetComponent<CinemachinePositionComposer>();
                if (focusPositionComposer == null)
                {
                    HelpLogs.Warn("Camera", "Focus camera missing CinemachinePositionComposer.");
                }
                else
                {
                    focusDesiredDistance = focusPositionComposer.CameraDistance;
                }
            }

            if (overviewVirtualCamera != null)
            {
                overviewPositionComposer = overviewVirtualCamera.GetComponent<CinemachinePositionComposer>();
                if (overviewPositionComposer == null)
                {
                    HelpLogs.Warn("Camera", "Overview camera missing CinemachinePositionComposer.");
                }
                else
                {
                    overviewBaseDistance = overviewDefaultDistance;
                    overviewPositionComposer.CameraDistance = overviewDefaultDistance;
                    overviewDesiredDistance = overviewPositionComposer.CameraDistance;
                }
            }

            EnsureProxies();

            if (overviewTarget == null)
            {
                EnsureOverviewTarget();
            }

            if (focusVirtualCamera == null && overviewVirtualCamera == null)
            {
                HelpLogs.Warn("Camera", "No Cinemachine virtual cameras found or one is not active.");
                return;
            }

            ApplyOverviewCameraTargets();

            ShowOverview();
            isInitialized = true;
        }

        /// <summary>
        /// Focus the camera on the requested solar object.
        /// </summary>
        public void FocusOn(SolarObject _solarObject)
        {
            if (_solarObject == null)
            {
                HelpLogs.Warn("Camera", "Focus request ignored because solar object is null.");
                return;
            }

            if (focusVirtualCamera == null)
            {
                HelpLogs.Warn("Camera", "Focus virtual camera not found.");
                return;
            }

            if (IsHighSpeedMode() && _solarObject.IsMoon)
            {
                SolarObject? _primary = _solarObject.PrimarySolarObject;
                if (_primary == null && simulator != null && !string.IsNullOrWhiteSpace(_solarObject.PrimaryId))
                {
                    IReadOnlyList<SolarObject> _objects = simulator.OrderedSolarObjects;
                    for (int _i = 0; _i < _objects.Count; _i++)
                    {
                        SolarObject _candidate = _objects[_i];
                        if (string.Equals(_candidate.Id, _solarObject.PrimaryId, StringComparison.OrdinalIgnoreCase))
                        {
                            _primary = _candidate;
                            break;
                        }
                    }
                }

                string _moonName = _solarObject.name;
                if (_primary != null)
                {
                    HelpLogs.Warn(
                        "Camera",
                        $"High time scale active. Moon '{_moonName}' focus redirected to primary '{_primary.name}'."
                    );
                    _solarObject = _primary;
                }
                else
                {
                    HelpLogs.Warn(
                        "Camera",
                        $"High time scale active. Moon '{_moonName}' focus requested but primary not found."
                    );
                }
            }

            bool _isNewTarget = focusSolarObject == null || focusSolarObject != _solarObject;
            focusTarget = _solarObject.transform;
            focusSolarObject = _solarObject;

            if (_isNewTarget)
            {
                bool _focusActive = IsFocusActive();
                SetFocusZoomToMinimum(_solarObject);

                focusProxyVelocity = Vector3.zero;
                focusDistanceVelocity = 0f;
                focusSwitchTimer = Mathf.Max(0f, focusSwitchSmoothDuration);

                if (_focusActive || !focusOrbitInitialized)
                {
                    SyncOrbitToCameraView(
                        focusTarget,
                        GetFocusOrbitRadius(),
                        ref focusOrbitYaw,
                        ref focusOrbitPitch,
                        ref focusOrbitOffset,
                        ref focusOrbitInitialized
                    );
                }
            }

            ApplyFocusDistance(_solarObject);
            RefreshFocusOrbitOffset();

            ApplyFocusCameraTargets();

            if (_isNewTarget)
            {
                ResetCameraState(focusVirtualCamera);
            }

            SetCameraPriority(focusVirtualCamera, overviewVirtualCamera);
        }

        /// <summary>
        /// Switch to the overview camera.
        /// </summary>
        public void ShowOverview()
        {
            if (overviewVirtualCamera == null)
            {
                HelpLogs.Warn("Camera", "Overview virtual camera not found.");
                return;
            }

            bool _wasFocusActive = IsFocusActive();

            if (overviewTarget != null)
            {
                // No immediate proxy update; handled in LateUpdate for smoothing.
            }
            else
            {
                EnsureOverviewTarget();
            }

            ApplyOverviewCameraTargets();
            ApplyOverviewDistance();
            if (overviewTarget != null && (_wasFocusActive || !overviewOrbitInitialized))
            {
                SyncOrbitToCameraView(
                    overviewTarget,
                    GetOverviewOrbitRadius(),
                    ref overviewOrbitYaw,
                    ref overviewOrbitPitch,
                    ref overviewOrbitOffset,
                    ref overviewOrbitInitialized
                );
            }

            overviewProxyVelocity = Vector3.zero;
            overviewDistanceVelocity = 0f;

            ResetCameraState(overviewVirtualCamera);
            SetCameraPriority(overviewVirtualCamera, focusVirtualCamera);
        }

        /// <summary>
        /// Assign the overview target explicitly.
        /// </summary>
        public void SetOverviewTarget(Transform? _target)
        {
            if (_target == null)
            {
                HelpLogs.Warn("Camera", "Overview target is null; keeping previous target.");
                return;
            }

            overviewTarget = _target;
            ApplyOverviewCameraTargets();
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Gui.CameraOrbitStepRequested += HandleCameraOrbitStepRequested;
            Gui.CameraZoomStepRequested += HandleCameraZoomStepRequested;

            simulator = FindFirstObjectByType<SolarSystemSimulator>();
            if (simulator != null)
            {
                simulator.VisualPresetChanged += HandleVisualPresetChanged;
            }
        }

        private void Start()
        {
            Initialize();
            if (simulator != null)
            {
                ApplyVisualPreset(simulator.VisualPresetLevelIndex);
            }
        }

        private void LateUpdate()
        {
            UpdateProxyTransform(focusProxy, focusTarget, focusOrbitOffset, ref focusProxyVelocity);
            UpdateProxyTransform(overviewProxy, overviewTarget, overviewOrbitOffset, ref overviewProxyVelocity);
            UpdateCameraDistances();
            UpdateOrbitOffsetsForDistances();

            if (focusSwitchTimer > 0f)
            {
                focusSwitchTimer = Mathf.Max(0f, focusSwitchTimer - Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            Gui.CameraOrbitStepRequested -= HandleCameraOrbitStepRequested;
            Gui.CameraZoomStepRequested -= HandleCameraZoomStepRequested;

            if (simulator != null)
            {
                simulator.VisualPresetChanged -= HandleVisualPresetChanged;
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Apply active/inactive priorities to Cinemachine cameras.
        /// </summary>
        private void SetCameraPriority(CinemachineCamera _active, CinemachineCamera? _inactive)
        {
            int _high = Math.Max(focusPriority, overviewPriority);
            int _low = Math.Min(focusPriority, overviewPriority);

            _active.Priority = _high;
            if (_inactive != null)
            {
                _inactive.Priority = _low;
            }
        }

        /// <summary>
        /// Reset Cinemachine state to force an immediate recompute.
        /// </summary>
        private void ResetCameraState(CinemachineCamera _camera)
        {
            _camera.PreviousStateIsValid = false;
        }

        /// <summary>
        /// Find a Cinemachine camera by name in the scene.
        /// </summary>
        private CinemachineCamera? FindVirtualCameraByName(string _name)
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                return null;
            }

            GameObject _direct = GameObject.Find(_name);
            if (_direct != null && _direct.TryGetComponent(out CinemachineCamera _directCam))
            {
                return _directCam;
            }

            CinemachineCamera[] _all = Resources.FindObjectsOfTypeAll<CinemachineCamera>();
            for (int _i = 0; _i < _all.Length; _i++)
            {
                CinemachineCamera _cam = _all[_i];
                if (!_cam.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (string.Equals(_cam.name, _name, StringComparison.OrdinalIgnoreCase))
                {
                    return _cam;
                }
            }

            HelpLogs.Warn("Camera", $"Missing Cinemachine camera named '{_name}'.");
            return null;
        }

        /// <summary>
        /// Ensure proxy transforms are resolved by name.
        /// </summary>
        private void EnsureProxies()
        {
            if (focusProxy == null)
            {
                focusProxy = FindProxyByName(focusProxyName);
                if (focusProxy == null)
                {
                    HelpLogs.Warn("Camera", $"Focus proxy '{focusProxyName}' not found.");
                }
            }

            if (overviewProxy == null)
            {
                overviewProxy = FindProxyByName(overviewProxyName);
                if (overviewProxy == null)
                {
                    HelpLogs.Warn("Camera", $"Overview proxy '{overviewProxyName}' not found.");
                }
            }
        }

        /// <summary>
        /// Assign focus camera follow/look-at targets.
        /// </summary>
        private void ApplyFocusCameraTargets()
        {
            if (focusVirtualCamera == null)
            {
                return;
            }

            Transform? _followTarget = focusProxy ?? focusTarget;
            Transform? _lookAtTarget = focusTarget ?? focusProxy;
            if (_followTarget == null)
            {
                return;
            }

            focusVirtualCamera.Follow = _followTarget;
            focusVirtualCamera.LookAt = _lookAtTarget;
        }

        /// <summary>
        /// Assign overview camera follow/look-at targets.
        /// </summary>
        private void ApplyOverviewCameraTargets()
        {
            if (overviewVirtualCamera == null)
            {
                return;
            }

            Transform? _followTarget = overviewProxy ?? overviewTarget;
            Transform? _lookAtTarget = overviewTarget ?? overviewProxy;
            if (_followTarget == null)
            {
                return;
            }

            overviewVirtualCamera.Follow = _followTarget;
            overviewVirtualCamera.LookAt = _lookAtTarget;
        }

        /// <summary>
        /// Move proxy transforms toward targets with optional smoothing.
        /// </summary>
        private void UpdateProxyTransform(
            Transform? _proxy,
            Transform? _target,
            Vector3 _offset,
            ref Vector3 _velocity
        )
        {
            if (_proxy == null || _target == null)
            {
                return;
            }

            Vector3 _targetPosition = _target.position + _offset;
            float _smoothTime = proxySmoothTime;
            float _minSpeed = proxyMinSpeed;
            float _maxSpeedValue = proxyMaxSpeed;
            float _range = proxySpeedDistanceRange;
            if (IsHighSpeedMode())
            {
                _smoothTime *= HighSpeedProxySmoothTimeMultiplier;
                _minSpeed *= HighSpeedProxySpeedMultiplier;
                _maxSpeedValue *= HighSpeedProxySpeedMultiplier;
            }

            if (smoothProxyMovement && _smoothTime > 0f)
            {
                float _maxSpeed = GetAdaptiveSpeed(
                    Vector3.Distance(_proxy.position, _targetPosition),
                    _minSpeed,
                    _maxSpeedValue,
                    _range
                );

                _proxy.position = Vector3.SmoothDamp(
                    _proxy.position,
                    _targetPosition,
                    ref _velocity,
                    _smoothTime,
                    _maxSpeed,
                    Time.deltaTime
                );
            }
            else
            {
                _proxy.position = _targetPosition;
                _velocity = Vector3.zero;
            }

            _proxy.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Locate a proxy object by name in the scene.
        /// </summary>
        private Transform? FindProxyByName(string _name)
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                return null;
            }

            GameObject _proxyObject = GameObject.Find(_name);
            if (_proxyObject != null)
            {
                return _proxyObject.transform;
            }

            return null;
        }

        /// <summary>
        /// Apply focus distance clamps and update desired distance.
        /// </summary>
        private void ApplyFocusDistance(SolarObject _solarObject)
        {
            if (focusVirtualCamera == null)
            {
                return;
            }

            if (focusPositionComposer == null)
            {
                focusPositionComposer = focusVirtualCamera.GetComponent<CinemachinePositionComposer>();
                if (focusPositionComposer == null)
                {
                    HelpLogs.Warn("Camera", "Focus camera missing CinemachinePositionComposer.");
                    return;
                }
            }

            float _baseDistance = GetFocusDistance(_solarObject);
            float _targetDistance = _baseDistance + focusZoomOffset;
            float _minMultiplier = Mathf.Max(0f, GetFocusMinMultiplier());
            float _maxMultiplier = Mathf.Max(_minMultiplier, focusZoomMaxMultiplier);
            float _minDistance = GetFocusMinDistance(_solarObject, _baseDistance, _minMultiplier);
            float _maxDistance = _baseDistance * _maxMultiplier;
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
            focusZoomOffset = _targetDistance - _baseDistance;
            focusDesiredDistance = _targetDistance;

            if (!smoothZoomDistance || zoomSmoothTime <= 0f)
            {
                if (!Mathf.Approximately(focusPositionComposer.CameraDistance, _targetDistance))
                {
                    focusPositionComposer.CameraDistance = _targetDistance;
                }

                focusDistanceVelocity = 0f;
            }
        }

        /// <summary>
        /// Apply overview distance clamps and update desired distance.
        /// </summary>
        private void ApplyOverviewDistance()
        {
            if (overviewVirtualCamera == null)
            {
                return;
            }

            if (overviewPositionComposer == null)
            {
                overviewPositionComposer = overviewVirtualCamera.GetComponent<CinemachinePositionComposer>();
                if (overviewPositionComposer == null)
                {
                    HelpLogs.Warn("Camera", "Overview camera missing CinemachinePositionComposer.");
                    return;
                }
            }

            if (overviewBaseDistance <= 0f)
            {
                overviewBaseDistance = overviewPositionComposer.CameraDistance;
            }

            float _distance = overviewBaseDistance + overviewZoomOffset;
            float _minDistance = overviewZoomMinDistance;
            float _maxDistance = overviewZoomMaxDistance;
            if (isRealisticPreset)
            {
                _minDistance = Mathf.Max(0f, realisticOverviewZoomMinDistance);
                _maxDistance = Mathf.Max(_minDistance, realisticOverviewZoomMaxDistance);
            }

            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            overviewZoomOffset = _distance - overviewBaseDistance;
            overviewDesiredDistance = _distance;

            if (!smoothZoomDistance || zoomSmoothTime <= 0f)
            {
                if (!Mathf.Approximately(overviewPositionComposer.CameraDistance, _distance))
                {
                    overviewPositionComposer.CameraDistance = _distance;
                }

                overviewDistanceVelocity = 0f;
            }
        }

        /// <summary>
        /// Snap focus zoom to the minimum allowed distance for a new target.
        /// </summary>
        private void SetFocusZoomToMinimum(SolarObject _solarObject)
        {
            float _baseDistance = GetFocusDistance(_solarObject);
            float _minMultiplier = Mathf.Max(0f, GetFocusMinMultiplier());
            float _minDistance = GetFocusMinDistance(_solarObject, _baseDistance, _minMultiplier);
            focusZoomOffset = _minDistance - _baseDistance;
        }

        /// <summary>
        /// Select the base focus distance for the target size.
        /// </summary>
        private float GetFocusDistance(SolarObject _solarObject)
        {
            if (string.Equals(_solarObject.Id, "sun", StringComparison.OrdinalIgnoreCase))
            {
                return focusDistanceSun;
            }

            float _diameter = _solarObject.transform.localScale.x;
            if (_diameter >= focusMediumSizeThreshold)
            {
                return focusDistanceMedium;
            }

            return focusDistanceSmall;
        }

        /// <summary>
        /// Compute the minimum focus distance for a target.
        /// </summary>
        private float GetFocusMinDistance(SolarObject _solarObject, float _baseDistance, float _minMultiplier)
        {
            if (string.Equals(_solarObject.Id, "sun", StringComparison.OrdinalIgnoreCase))
            {
                float _sunMin = focusZoomMinDistanceSun - focusSunZoomInAllowance;
                return Mathf.Max(focusZoomAbsoluteMinDistance, _sunMin);
            }

            float _minDistance = _baseDistance * _minMultiplier;
            if (!isRealisticPreset && IsLargeBody(_solarObject))
            {
                _minDistance += simulationLargeBodyMinDistanceOffset;
            }

            if (IsLargeBody(_solarObject))
            {
                _minDistance -= focusLargeBodyZoomInAllowance;
            }

            return Mathf.Max(focusZoomAbsoluteMinDistance, _minDistance);
        }

        /// <summary>
        /// Determine if an object should be treated as a large body.
        /// </summary>
        private bool IsLargeBody(SolarObject _solarObject)
        {
            float _baseDiameter = _solarObject.BaseDiameterUnity;
            return _baseDiameter >= focusMediumSizeThreshold;
        }

        /// <summary>
        /// Resolve the focus zoom minimum multiplier for the active preset.
        /// </summary>
        private float GetFocusMinMultiplier()
        {
            if (isRealisticPreset)
            {
                return Mathf.Max(focusZoomMinMultiplier, realisticFocusZoomMinMultiplier);
            }

            return focusZoomMinMultiplier;
        }

        /// <summary>
        /// True when the focus camera has priority over overview.
        /// </summary>
        private bool IsFocusActive()
        {
            if (focusVirtualCamera == null)
            {
                return false;
            }

            if (overviewVirtualCamera == null)
            {
                return true;
            }

            return focusVirtualCamera.Priority >= overviewVirtualCamera.Priority;
        }

        /// <summary>
        /// Apply a discrete orbit step around the active target.
        /// </summary>
        private void HandleCameraOrbitStepRequested(Vector2 _direction)
        {
            if (_direction == Vector2.zero)
            {
                return;
            }

            bool _useFocus = IsFocusActive();
            Transform? _target = _useFocus ? focusTarget : overviewTarget;
            Transform? _proxy = _useFocus ? focusProxy : overviewProxy;
            if (_target == null || _proxy == null)
            {
                HelpLogs.Warn("Camera", "Orbit request ignored because target or proxy is missing.");
                return;
            }

            if (_useFocus)
            {
                float _cameraDistance = GetFocusCameraDistance();
                float _radius = GetOrbitRadiusFromDistance(_cameraDistance, focusOrbitMaxOffset);
                EnsureOrbitInitialized(
                    _target,
                    _radius,
                    ref focusOrbitYaw,
                    ref focusOrbitPitch,
                    ref focusOrbitOffset,
                    ref focusOrbitInitialized
                );
                float _step = GetOrbitStepDegrees(_radius);
                focusOrbitYaw = WrapAngle(focusOrbitYaw + _direction.x * _step);
                focusOrbitPitch = Mathf.Clamp(
                    focusOrbitPitch + _direction.y * _step,
                    -orbitMaxPitchDegrees,
                    orbitMaxPitchDegrees
                );
                focusOrbitOffset = GetOrbitOffset(_radius, focusOrbitYaw, focusOrbitPitch);
                if (focusSolarObject != null)
                {
                    ApplyFocusDistance(focusSolarObject);
                }
            }
            else
            {
                float _cameraDistance = GetOverviewCameraDistance();
                float _radius = GetOrbitRadiusFromDistance(_cameraDistance, overviewOrbitMaxOffset);
                EnsureOrbitInitialized(
                    _target,
                    _radius,
                    ref overviewOrbitYaw,
                    ref overviewOrbitPitch,
                    ref overviewOrbitOffset,
                    ref overviewOrbitInitialized
                );
                float _step = GetOrbitStepDegrees(_radius);
                _step *= GetOverviewOrbitStepMultiplier(_cameraDistance);
                overviewOrbitYaw = WrapAngle(overviewOrbitYaw + _direction.x * _step);
                overviewOrbitPitch = Mathf.Clamp(
                    overviewOrbitPitch + _direction.y * _step,
                    -orbitMaxPitchDegrees,
                    orbitMaxPitchDegrees
                );
                overviewOrbitOffset = GetOrbitOffset(_radius, overviewOrbitYaw, overviewOrbitPitch);
            }
        }

        /// <summary>
        /// Apply a discrete zoom step for focus or overview.
        /// </summary>
        private void HandleCameraZoomStepRequested(int _delta)
        {
            if (_delta == 0)
            {
                return;
            }

            bool _useFocus = IsFocusActive();
            if (_useFocus)
            {
                if (focusSolarObject == null)
                {
                    HelpLogs.Warn("Camera", "Zoom request ignored because focus target is missing.");
                    return;
                }

                focusZoomOffset += _delta * focusZoomStep;
                ApplyFocusDistance(focusSolarObject);
                RefreshFocusOrbitOffset();
            }
            else
            {
                overviewZoomOffset += _delta * zoomStep * GetOverviewZoomSpeedMultiplier();
                ApplyOverviewDistance();
                RefreshOverviewOrbitOffset();
            }
        }

        /// <summary>
        /// Convert orbit step size to degrees based on radius.
        /// </summary>
        private float GetOrbitStepDegrees(float _radius)
        {
            if (_radius <= 0f)
            {
                return 0f;
            }

            float _radians = orbitStep / _radius;
            float _degrees = Mathf.Rad2Deg * _radians;
            float _max = Mathf.Max(0.01f, orbitStepMaxDegrees);
            return Mathf.Min(_degrees, _max);
        }

        /// <summary>
        /// Increase orbit step size as overview distance grows.
        /// </summary>
        private float GetOverviewOrbitStepMultiplier(float _cameraDistance)
        {
            float _distance = Mathf.Max(0f, _cameraDistance);
            float _start = Mathf.Max(1f, overviewOrbitStepBoostDistance);
            if (_distance <= _start)
            {
                return 1.0f;
            }

            float _t = Mathf.Clamp01((_distance - _start) / _start);
            float _target = Mathf.Max(1.0f, overviewOrbitStepDistanceMultiplier);
            return Mathf.Lerp(1.0f, _target, _t);
        }

        /// <summary>
        /// Initialize orbit offsets from the current camera view if needed.
        /// </summary>
        private void EnsureOrbitInitialized(
            Transform _target,
            float _radius,
            ref float _yaw,
            ref float _pitch,
            ref Vector3 _offset,
            ref bool _initialized
        )
        {
            if (_initialized)
            {
                return;
            }

            if (!TrySyncOrbitToCameraView(_target, _radius, ref _yaw, ref _pitch, ref _offset, ref _initialized))
            {
                _yaw = 0f;
                _pitch = 0f;
                _offset = GetOrbitOffset(_radius, _yaw, _pitch);
                _initialized = true;
            }
        }

        /// <summary>
        /// Sync orbit yaw/pitch to the current camera direction.
        /// </summary>
        private void SyncOrbitToCameraView(
            Transform _target,
            float _radius,
            ref float _yaw,
            ref float _pitch,
            ref Vector3 _offset,
            ref bool _initialized
        )
        {
            TrySyncOrbitToCameraView(_target, _radius, ref _yaw, ref _pitch, ref _offset, ref _initialized);
        }

        /// <summary>
        /// Try to sync orbit yaw/pitch to the current camera direction.
        /// </summary>
        private bool TrySyncOrbitToCameraView(
            Transform _target,
            float _radius,
            ref float _yaw,
            ref float _pitch,
            ref Vector3 _offset,
            ref bool _initialized
        )
        {
            if (!TryGetCameraDirection(_target, out Vector3 _direction))
            {
                return false;
            }

            SetOrbitFromDirection(_direction, _radius, ref _yaw, ref _pitch, ref _offset);
            _initialized = true;
            return true;
        }

        /// <summary>
        /// Wrap degrees into a stable range.
        /// </summary>
        private float WrapAngle(float _angle)
        {
            if (_angle >= 360f || _angle <= -360f)
            {
                _angle %= 360f;
            }

            return _angle;
        }

        /// <summary>
        /// Resolve the camera direction vector from the target.
        /// </summary>
        private bool TryGetCameraDirection(Transform _target, out Vector3 _direction)
        {
            _direction = Vector3.zero;
            Camera _camera = Camera.main;
            if (_camera == null)
            {
                return false;
            }

            _direction = _camera.transform.position - _target.position;
            if (_direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            _direction.Normalize();
            return true;
        }

        /// <summary>
        /// Smoothly update focus and overview camera distances.
        /// </summary>
        private void UpdateCameraDistances()
        {
            if (focusPositionComposer != null)
            {
                float _target = focusDesiredDistance > 0f
                    ? focusDesiredDistance
                    : focusPositionComposer.CameraDistance;
                float _smoothTime = zoomSmoothTime;
                float _speedMultiplier = 1.0f;
                if (focusSwitchTimer > 0f)
                {
                    float _duration = Mathf.Max(0.01f, focusSwitchSmoothDuration);
                    float _blend = Mathf.Clamp01(focusSwitchTimer / _duration);
                    _smoothTime *= Mathf.Lerp(1.0f, Mathf.Max(1.0f, focusSwitchSmoothTimeMultiplier), _blend);
                    _speedMultiplier *= Mathf.Lerp(1.0f, Mathf.Clamp01(focusSwitchSpeedMultiplier), _blend);
                }

                if (IsHighSpeedMode())
                {
                    _smoothTime *= HighSpeedZoomSmoothTimeMultiplier;
                    _speedMultiplier *= HighSpeedZoomSpeedMultiplier;
                }

                UpdateCameraDistance(
                    focusPositionComposer,
                    _target,
                    ref focusDistanceVelocity,
                    _speedMultiplier,
                    _smoothTime
                );
            }

            if (overviewPositionComposer != null)
            {
                float _target = overviewDesiredDistance > 0f
                    ? overviewDesiredDistance
                    : overviewPositionComposer.CameraDistance;
                float _smoothTime = zoomSmoothTime;
                float _speedMultiplier = GetOverviewZoomSpeedMultiplier();
                if (IsHighSpeedMode())
                {
                    _smoothTime *= HighSpeedZoomSmoothTimeMultiplier;
                    _speedMultiplier *= HighSpeedZoomSpeedMultiplier;
                }

                UpdateCameraDistance(
                    overviewPositionComposer,
                    _target,
                    ref overviewDistanceVelocity,
                    _speedMultiplier,
                    _smoothTime
                );
            }
        }

        /// <summary>
        /// Smooth or snap a Cinemachine camera distance to the target.
        /// </summary>
        private void UpdateCameraDistance(
            CinemachinePositionComposer _composer,
            float _target,
            ref float _velocity,
            float _speedMultiplier,
            float _smoothTime
        )
        {
            if (!smoothZoomDistance || _smoothTime <= 0f)
            {
                if (!Mathf.Approximately(_composer.CameraDistance, _target))
                {
                    _composer.CameraDistance = _target;
                }

                _velocity = 0f;
                return;
            }

            float _distance = Mathf.SmoothDamp(
                _composer.CameraDistance,
                _target,
                ref _velocity,
                _smoothTime,
                GetAdaptiveSpeed(
                    Mathf.Abs(_composer.CameraDistance - _target),
                    zoomMinSpeed * _speedMultiplier,
                    zoomMaxSpeed * _speedMultiplier,
                    zoomSpeedDistanceRange
                ),
                Time.deltaTime
            );

            _composer.CameraDistance = _distance;
        }

        /// <summary>
        /// Map distance-to-target into a smooth speed range.
        /// </summary>
        private float GetAdaptiveSpeed(float _distance, float _minSpeed, float _maxSpeed, float _range)
        {
            float _safeRange = Mathf.Max(0.001f, _range);
            float _t = Mathf.Clamp01(_distance / _safeRange);
            _t = _t * _t * (3f - 2f * _t);
            float _min = Mathf.Max(0f, _minSpeed);
            float _max = Mathf.Max(_min, _maxSpeed);
            return Mathf.Lerp(_min, _max, _t);
        }

        /// <summary>
        /// True when the simulation is running at very high time scale.
        /// </summary>
        private bool IsHighSpeedMode()
        {
            if (simulator == null)
            {
                return false;
            }

            return simulator.TimeScale >= HighSpeedTimeScaleThreshold;
        }

        /// <summary>
        /// Resolve the overview zoom speed multiplier for the active preset.
        /// </summary>
        private float GetOverviewZoomSpeedMultiplier()
        {
            if (isRealisticPreset)
            {
                return Mathf.Max(1.0f, realisticOverviewZoomSpeedMultiplier);
            }

            return Mathf.Max(1.0f, overviewZoomSpeedMultiplier);
        }

        /// <summary>
        /// Recompute orbit offsets when camera distance changes.
        /// </summary>
        private void UpdateOrbitOffsetsForDistances()
        {
            if (focusOrbitInitialized && focusTarget != null)
            {
                float _radius = GetOrbitRadiusFromDistance(GetFocusCameraDistance(), focusOrbitMaxOffset);
                focusOrbitOffset = GetOrbitOffset(_radius, focusOrbitYaw, focusOrbitPitch);
            }

            if (overviewOrbitInitialized && overviewTarget != null)
            {
                float _radius = GetOrbitRadiusFromDistance(GetOverviewCameraDistance(), overviewOrbitMaxOffset);
                overviewOrbitOffset = GetOrbitOffset(_radius, overviewOrbitYaw, overviewOrbitPitch);
            }
        }

        /// <summary>
        /// Convert a direction vector into yaw/pitch orbit values.
        /// </summary>
        private void SetOrbitFromDirection(
            Vector3 _direction,
            float _radius,
            ref float _yaw,
            ref float _pitch,
            ref Vector3 _offset
        )
        {
            Vector3 _forward = -_direction;
            float _yawRad = Mathf.Atan2(_forward.x, _forward.z);
            float _pitchRad = -Mathf.Asin(Mathf.Clamp(_forward.y, -1f, 1f));

            _yaw = WrapAngle(Mathf.Rad2Deg * _yawRad);
            _pitch = Mathf.Clamp(Mathf.Rad2Deg * _pitchRad, -orbitMaxPitchDegrees, orbitMaxPitchDegrees);
            _offset = GetOrbitOffset(_radius, _yaw, _pitch);
        }

        /// <summary>
        /// Sync focus zoom offset from the current camera distance.
        /// </summary>
        private void SyncFocusZoomFromCurrentView(SolarObject _solarObject)
        {
            float _currentDistance = GetFocusCameraDistance();
            float _baseDistance = GetFocusDistance(_solarObject);
            focusZoomOffset = _currentDistance - _baseDistance;
        }

        /// <summary>
        /// Compute the focus orbit radius from camera distance.
        /// </summary>
        private float GetFocusOrbitRadius()
        {
            float _cameraDistance = GetFocusCameraDistance();
            return GetOrbitRadiusFromDistance(_cameraDistance, focusOrbitMaxOffset);
        }

        /// <summary>
        /// Compute the overview orbit radius from camera distance.
        /// </summary>
        private float GetOverviewOrbitRadius()
        {
            float _cameraDistance = GetOverviewCameraDistance();
            return GetOrbitRadiusFromDistance(_cameraDistance, overviewOrbitMaxOffset);
        }

        /// <summary>
        /// Get the current focus camera distance.
        /// </summary>
        private float GetFocusCameraDistance()
        {
            if (focusPositionComposer != null)
            {
                return focusPositionComposer.CameraDistance;
            }

            if (focusSolarObject != null)
            {
                return GetFocusDistance(focusSolarObject);
            }

            return focusDistanceSmall;
        }

        /// <summary>
        /// Get the current overview camera distance.
        /// </summary>
        private float GetOverviewCameraDistance()
        {
            if (overviewPositionComposer != null)
            {
                return overviewPositionComposer.CameraDistance;
            }

            return overviewDefaultDistance;
        }

        /// <summary>
        /// Convert camera distance to an orbit radius with a max cap.
        /// </summary>
        private float GetOrbitRadiusFromDistance(float _cameraDistance, float _maxOffset)
        {
            float _factor = Mathf.Clamp01(orbitRadiusMaxFactor);
            float _radius = _cameraDistance * _factor;
            _radius = Mathf.Min(_radius, _maxOffset);
            return Mathf.Max(0f, _radius);
        }

        /// <summary>
        /// Refresh the focus orbit offset after zoom changes.
        /// </summary>
        private void RefreshFocusOrbitOffset()
        {
            if (!focusOrbitInitialized)
            {
                return;
            }

            if (focusTarget == null || focusProxy == null)
            {
                return;
            }

            float _radius = GetFocusOrbitRadius();
            focusOrbitOffset = GetOrbitOffset(_radius, focusOrbitYaw, focusOrbitPitch);
        }

        /// <summary>
        /// Refresh the overview orbit offset after zoom changes.
        /// </summary>
        private void RefreshOverviewOrbitOffset()
        {
            if (!overviewOrbitInitialized)
            {
                return;
            }

            if (overviewTarget == null || overviewProxy == null)
            {
                return;
            }

            float _radius = GetOverviewOrbitRadius();
            overviewOrbitOffset = GetOrbitOffset(_radius, overviewOrbitYaw, overviewOrbitPitch);
        }

        /// <summary>
        /// Convert yaw/pitch orbit values into a local offset.
        /// </summary>
        private Vector3 GetOrbitOffset(float _radius, float _yawDegrees, float _pitchDegrees)
        {
            Quaternion _rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            return _rotation * Vector3.back * _radius;
        }

        /// <summary>
        /// Try to assign the Sun as overview target from the simulator.
        /// </summary>
        private void TryAssignOverviewTargetFromSimulator()
        {
            SolarSystemSimulator _simulator = FindFirstObjectByType<SolarSystemSimulator>();
            if (_simulator == null)
            {
                return;
            }

            IReadOnlyList<SolarObject> _objects = _simulator.OrderedSolarObjects;
            for (int _i = 0; _i < _objects.Count; _i++)
            {
                SolarObject _object = _objects[_i];
                if (string.Equals(_object.Id, "sun", StringComparison.OrdinalIgnoreCase))
                {
                    overviewTarget = _object.transform;
                    return;
                }
            }
        }

        /// <summary>
        /// Ensure an overview target is set.
        /// </summary>
        private void EnsureOverviewTarget()
        {
            if (overviewTarget != null)
            {
                return;
            }

            TryAssignOverviewTargetFromSimulator();
        }

        /// <summary>
        /// React to simulator visual preset changes.
        /// </summary>
        private void HandleVisualPresetChanged(int _presetIndex)
        {
            ApplyVisualPreset(_presetIndex);
        }

        /// <summary>
        /// Apply preset-specific camera overrides.
        /// </summary>
        private void ApplyVisualPreset(int _presetIndex)
        {
            bool _wasRealistic = isRealisticPreset;
            isRealisticPreset = _presetIndex == 0;

            if (isRealisticPreset == _wasRealistic)
            {
                return;
            }

            ApplyOverviewDistance();
            if (focusSolarObject != null)
            {
                ApplyFocusDistance(focusSolarObject);
            }

            RefreshOverviewOrbitOffset();
            RefreshFocusOrbitOffset();
        }
        #endregion
    }
}

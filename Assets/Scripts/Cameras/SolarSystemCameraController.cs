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
        [SerializeField] private CinemachineCamera? focusVirtualCamera;
        [SerializeField] private CinemachineCamera? overviewVirtualCamera;

        [Header("Virtual Camera Names")]
        [SerializeField] private string focusVirtualCameraName = "Follow-SolarObject-VCinemachine";
        [SerializeField] private string overviewVirtualCameraName = "Overview-SolarSystem-VCinemachine";

        [Header("Proxy Targets")]
        [SerializeField] private Transform? focusProxy;
        [SerializeField] private Transform? overviewProxy;

        [Header("Proxy Names")]
        [SerializeField] private string focusProxyName = "Focus_Proxy";
        [SerializeField] private string overviewProxyName = "Overview_Proxy";

        // Overview target assigned at runtime (usually the Sun).
        private Transform? overviewTarget;
        private Transform? focusTarget;

        [Header("Proxy Smoothing")]
        [SerializeField] private bool smoothProxyMovement = true;
        [SerializeField] private float proxySmoothTime = 0.2f;
        [SerializeField] private float proxyMaxSpeed = 200f;
        [SerializeField] private float proxyMinSpeed = 2f;
        [SerializeField] private float proxySpeedDistanceRange = 30f;

        [Header("Zoom Smoothing")]
        [SerializeField] private bool smoothZoomDistance = true;
        [SerializeField] private float zoomSmoothTime = 0.16f;
        [SerializeField] private float zoomMaxSpeed = 200f;
        [SerializeField] private float zoomMinSpeed = 0.1f;
        [SerializeField] private float zoomSpeedDistanceRange = 3f;

        [Header("Priorities")]
        [SerializeField] private int focusPriority = 20;
        [SerializeField] private int overviewPriority = 10;

        [Header("Focus Distance")]
        [SerializeField] private float focusDistanceSmall = 0.35f;
        [SerializeField] private float focusDistanceMedium = 0.6f;
        [SerializeField] private float focusDistanceSun = 1.8f;
        [SerializeField] private float focusMediumSizeThreshold = 0.3f;

        [Header("Zoom Controls")]
        [SerializeField] private float focusZoomMinMultiplier = 0.15f;
        [SerializeField] private float focusZoomMaxMultiplier = 3f;
        [SerializeField] private float focusZoomMinDistanceSun = 1.5f;
        [SerializeField] private float simulationLargeBodyMinDistanceOffset = 0.15f;
        [SerializeField] private float focusLargeBodyZoomInAllowance = 0.0f;
        [SerializeField] private float focusSunZoomInAllowance = 0.25f;
        [SerializeField] private float focusZoomAbsoluteMinDistance = 0.05f;
        [SerializeField] private float overviewZoomMinDistance = 2f;
        [SerializeField] private float overviewZoomMaxDistance = 10f;
        [SerializeField] private float overviewDefaultDistance = 5f;
        [SerializeField] private float zoomStep = 0.2f;

        [Header("Realistic Preset Overrides")]
        [SerializeField] private float realisticFocusZoomMinMultiplier = 0.6f;
        [SerializeField] private float realisticOverviewZoomMinDistance = 50f;
        [SerializeField] private float realisticOverviewZoomMaxDistance = 500f;
        [SerializeField] private float realisticOverviewZoomSpeedMultiplier = 10f;

        [Header("Orbit Controls")]
        [SerializeField] private float orbitStep = 0.1f;
        [SerializeField] private float focusOrbitMaxOffset = 1f;
        [SerializeField] private float overviewOrbitMaxOffset = 5f;
        [SerializeField] private float orbitMaxPitchDegrees = 80f;
        [SerializeField] private float orbitRadiusMaxFactor = 0.5f;
        [SerializeField] private float orbitStepMaxDegrees = 10f;

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
                HelpLogs.Warn("Camera", "No Cinemachine virtual cameras found. Camera controls are disabled.");
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

            bool _isNewTarget = focusSolarObject == null || focusSolarObject != _solarObject;
            focusTarget = _solarObject.transform;
            focusSolarObject = _solarObject;

            if (_isNewTarget)
            {
                bool _focusActive = IsFocusActive();
                if (_focusActive)
                {
                    SyncFocusZoomFromCurrentView(_solarObject);
                }

                focusProxyVelocity = Vector3.zero;
                focusDistanceVelocity = 0f;

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
                InvalidateCameraState(focusVirtualCamera);
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

            InvalidateCameraState(overviewVirtualCamera);
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
        private void OnEnable()
        {
            Gui.CameraOrbitStepRequested += HandleCameraOrbitStepRequested;
            Gui.CameraZoomStepRequested += HandleCameraZoomStepRequested;

            simulator = FindFirstObjectByType<SolarSystemSimulator>();
            if (simulator != null)
            {
                simulator.VisualPresetChanged += HandleVisualPresetChanged;
            }
        }

        private void OnDisable()
        {
            Gui.CameraOrbitStepRequested -= HandleCameraOrbitStepRequested;
            Gui.CameraZoomStepRequested -= HandleCameraZoomStepRequested;

            if (simulator != null)
            {
                simulator.VisualPresetChanged -= HandleVisualPresetChanged;
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
        }
        #endregion

        #region Helpers
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

        private void InvalidateCameraState(CinemachineCamera _camera)
        {
            _camera.PreviousStateIsValid = false;
        }

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
            if (smoothProxyMovement && proxySmoothTime > 0f)
            {
                float _maxSpeed = GetAdaptiveSpeed(
                    Vector3.Distance(_proxy.position, _targetPosition),
                    proxyMinSpeed,
                    proxyMaxSpeed,
                    proxySpeedDistanceRange
                );

                _proxy.position = Vector3.SmoothDamp(
                    _proxy.position,
                    _targetPosition,
                    ref _velocity,
                    proxySmoothTime,
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

        private bool IsLargeBody(SolarObject _solarObject)
        {
            float _baseDiameter = _solarObject.BaseDiameterUnity;
            return _baseDiameter >= focusMediumSizeThreshold;
        }

        private float GetFocusMinMultiplier()
        {
            if (isRealisticPreset)
            {
                return Mathf.Max(focusZoomMinMultiplier, realisticFocusZoomMinMultiplier);
            }

            return focusZoomMinMultiplier;
        }

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
                overviewOrbitYaw = WrapAngle(overviewOrbitYaw + _direction.x * _step);
                overviewOrbitPitch = Mathf.Clamp(
                    overviewOrbitPitch + _direction.y * _step,
                    -orbitMaxPitchDegrees,
                    orbitMaxPitchDegrees
                );
                overviewOrbitOffset = GetOrbitOffset(_radius, overviewOrbitYaw, overviewOrbitPitch);
            }
        }

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

                focusZoomOffset += _delta * zoomStep;
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

        private float WrapAngle(float _angle)
        {
            if (_angle >= 360f || _angle <= -360f)
            {
                _angle %= 360f;
            }

            return _angle;
        }

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

        private void UpdateCameraDistances()
        {
            if (focusPositionComposer != null)
            {
                float _target = focusDesiredDistance > 0f
                    ? focusDesiredDistance
                    : focusPositionComposer.CameraDistance;
                UpdateCameraDistance(focusPositionComposer, _target, ref focusDistanceVelocity, 1.0f);
            }

            if (overviewPositionComposer != null)
            {
                float _target = overviewDesiredDistance > 0f
                    ? overviewDesiredDistance
                    : overviewPositionComposer.CameraDistance;
                UpdateCameraDistance(
                    overviewPositionComposer,
                    _target,
                    ref overviewDistanceVelocity,
                    GetOverviewZoomSpeedMultiplier()
                );
            }
        }

        private void UpdateCameraDistance(
            CinemachinePositionComposer _composer,
            float _target,
            ref float _velocity,
            float _speedMultiplier
        )
        {
            if (!smoothZoomDistance || zoomSmoothTime <= 0f)
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
                zoomSmoothTime,
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

        private float GetAdaptiveSpeed(float _distance, float _minSpeed, float _maxSpeed, float _range)
        {
            float _safeRange = Mathf.Max(0.001f, _range);
            float _t = Mathf.Clamp01(_distance / _safeRange);
            _t = _t * _t * (3f - 2f * _t);
            float _min = Mathf.Max(0f, _minSpeed);
            float _max = Mathf.Max(_min, _maxSpeed);
            return Mathf.Lerp(_min, _max, _t);
        }

        private float GetOverviewZoomSpeedMultiplier()
        {
            if (isRealisticPreset)
            {
                return Mathf.Max(1.0f, realisticOverviewZoomSpeedMultiplier);
            }

            return 1.0f;
        }

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

        private void SyncFocusZoomFromCurrentView(SolarObject _solarObject)
        {
            float _currentDistance = GetFocusCameraDistance();
            float _baseDistance = GetFocusDistance(_solarObject);
            focusZoomOffset = _currentDistance - _baseDistance;
        }

        private float GetFocusOrbitRadius()
        {
            float _cameraDistance = GetFocusCameraDistance();
            return GetOrbitRadiusFromDistance(_cameraDistance, focusOrbitMaxOffset);
        }

        private float GetOverviewOrbitRadius()
        {
            float _cameraDistance = GetOverviewCameraDistance();
            return GetOrbitRadiusFromDistance(_cameraDistance, overviewOrbitMaxOffset);
        }

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

        private float GetOverviewCameraDistance()
        {
            if (overviewPositionComposer != null)
            {
                return overviewPositionComposer.CameraDistance;
            }

            return overviewDefaultDistance;
        }

        private float GetOrbitRadiusFromDistance(float _cameraDistance, float _maxOffset)
        {
            float _factor = Mathf.Clamp01(orbitRadiusMaxFactor);
            float _radius = _cameraDistance * _factor;
            _radius = Mathf.Min(_radius, _maxOffset);
            return Mathf.Max(0f, _radius);
        }

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

        private Vector3 GetOrbitOffset(float _radius, float _yawDegrees, float _pitchDegrees)
        {
            Quaternion _rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            return _rotation * Vector3.back * _radius;
        }

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

        private void EnsureOverviewTarget()
        {
            if (overviewTarget != null)
            {
                return;
            }

            TryAssignOverviewTargetFromSimulator();
        }

        private void HandleVisualPresetChanged(int _presetIndex)
        {
            ApplyVisualPreset(_presetIndex);
        }

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
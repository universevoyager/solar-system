#nullable enable
using System;
using System.Collections.Generic;
using Assets.Scripts.Helpers.Debugging;
using Assets.Scripts.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Scripts.Cameras
{
    public sealed partial class SolarSystemCamera
    {
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
            float _smoothTime = proxySmoothSeconds;
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
            float _maxMultiplier = Mathf.Max(_minMultiplier, focusZoomMaxDistanceMultiplier);
            float _minDistance = GetFocusMinDistance(_solarObject, _baseDistance, _minMultiplier);
            float _maxDistance = _baseDistance * _maxMultiplier;
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
            focusZoomOffset = _targetDistance - _baseDistance;
            focusDesiredDistance = _targetDistance;

            if (!smoothZoomDistance || zoomSmoothSeconds <= 0f)
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
            float _realism = RealismLevel01;
            float _minDistance = Mathf.Lerp(overviewZoomMinDistance, realisticOverviewZoomMinDistance, _realism);
            float _maxDistance = Mathf.Lerp(overviewZoomMaxDistance, realisticOverviewZoomMaxDistance, _realism);
            _minDistance = Mathf.Max(0f, _minDistance);
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);

            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            overviewZoomOffset = _distance - overviewBaseDistance;
            overviewDesiredDistance = _distance;

            if (!smoothZoomDistance || zoomSmoothSeconds <= 0f)
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
                float _sunMin = focusZoomMinDistanceForSun - focusSunZoomInAllowance;
                return Mathf.Max(focusZoomAbsoluteMinDistance, _sunMin);
            }

            float _minDistance = _baseDistance * _minMultiplier;
            if (IsLargeBody(_solarObject))
            {
                float _simulationWeight = 1.0f - RealismLevel01;
                _minDistance += simulationLargeBodyMinDistanceBoost * _simulationWeight;
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
        /// Resolve the focus zoom minimum multiplier for the current realism blend.
        /// </summary>
        private float GetFocusMinMultiplier()
        {
            float _realism = RealismLevel01;
            float _min = Mathf.Lerp(focusZoomMinDistanceMultiplier, realisticFocusZoomMinDistanceMultiplier, _realism);
            return Mathf.Max(0f, _min);
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
        /// Smoothly update focus and overview camera distances.
        /// </summary>
        private void UpdateCameraDistances()
        {
            if (focusPositionComposer != null)
            {
                float _target = focusDesiredDistance > 0f
                    ? focusDesiredDistance
                    : focusPositionComposer.CameraDistance;
                float _smoothTime = zoomSmoothSeconds;
                float _speedMultiplier = 1.0f;
                if (focusSwitchTimer > 0f)
                {
                    float _duration = Mathf.Max(0.01f, focusSwitchSmoothSeconds);
                    float _blend = Mathf.Clamp01(focusSwitchTimer / _duration);
                    _smoothTime *= Mathf.Lerp(1.0f, Mathf.Max(1.0f, focusSwitchSmoothTimeScale), _blend);
                    _speedMultiplier *= Mathf.Lerp(1.0f, Mathf.Clamp01(focusSwitchSpeedScale), _blend);
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
                float _smoothTime = zoomSmoothSeconds;
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
        /// Resolve the overview zoom speed multiplier for the current realism blend.
        /// </summary>
        private float GetOverviewZoomSpeedMultiplier()
        {
            float _realism = RealismLevel01;
            float _speed = Mathf.Lerp(overviewZoomSpeedScale, realisticOverviewZoomSpeedScale, _realism);
            return Mathf.Max(1.0f, _speed);
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
        /// React to simulator realism changes.
        /// </summary>
        private void HandleRealismLevelChanged(float _level)
        {
            ApplyRealismLevel(_level);
        }

        /// <summary>
        /// Apply realism-driven camera overrides.
        /// </summary>
        private void ApplyRealismLevel(float _level)
        {
            float _clamped = Mathf.Clamp01(_level);
            if (Mathf.Approximately(_clamped, realismLevel))
            {
                return;
            }

            realismLevel = _clamped;

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

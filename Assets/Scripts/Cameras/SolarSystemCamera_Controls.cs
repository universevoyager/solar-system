#nullable enable
using Assets.Scripts.Helpers.Debugging;
using Assets.Scripts.Runtime;
using UnityEngine;

namespace Assets.Scripts.Cameras
{
    public sealed partial class SolarSystemCamera
    {
        #region Controls
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

                focusZoomOffset += _delta * focusZoomStepSize;
                ApplyFocusDistance(focusSolarObject);
                RefreshFocusOrbitOffset();
            }
            else
            {
                overviewZoomOffset += _delta * overviewZoomStepSize * GetOverviewZoomSpeedMultiplier();
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

            float _radians = orbitStepSize / _radius;
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
            float _start = Mathf.Max(1f, overviewOrbitStepBoostStartDistance);
            if (_distance <= _start)
            {
                return 1.0f;
            }

            float _t = Mathf.Clamp01((_distance - _start) / _start);
            float _target = Mathf.Max(1.0f, overviewOrbitStepDistanceScale);
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
        /// Convert camera distance to an orbit radius with a max cap.
        /// </summary>
        private float GetOrbitRadiusFromDistance(float _cameraDistance, float _maxOffset)
        {
            float _factor = Mathf.Clamp01(orbitRadiusMaxDistanceFactor);
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
        #endregion
    }
}

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
                HelpLogs.Warn("Camera", "SolarSystemCamera already initialized.");
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
                focusSwitchTimer = Mathf.Max(0f, focusSwitchSmoothSeconds);

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
    }
}

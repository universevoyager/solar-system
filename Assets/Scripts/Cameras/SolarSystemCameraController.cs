#nullable enable
using System;
using System.Collections.Generic;
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

        [Header("Priorities")]
        [SerializeField] private int focusPriority = 20;
        [SerializeField] private int overviewPriority = 10;

        [Header("Focus Distance")]
        [SerializeField] private float focusDistanceSmall = 0.2f;
        [SerializeField] private float focusDistanceMedium = 0.3f;
        [SerializeField] private float focusDistanceSun = 1.5f;
        [SerializeField] private float focusMediumSizeThreshold = 0.3f;
        #endregion

        #region Runtime State
        private bool isInitialized = false;
        private CinemachinePositionComposer? focusPositionComposer;
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

            focusTarget = _solarObject.transform;
            UpdateProxyTransform(focusProxy, focusTarget);
            ApplyFocusCameraTargets();
            ApplyFocusDistance(_solarObject);

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

            if (overviewTarget != null)
            {
                UpdateProxyTransform(overviewProxy, overviewTarget);
            }
            else
            {
                EnsureOverviewTarget();
                if (overviewTarget != null)
                {
                    UpdateProxyTransform(overviewProxy, overviewTarget);
                }
            }

            ApplyOverviewCameraTargets();
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

            UpdateProxyTransform(overviewProxy, overviewTarget);
            ApplyOverviewCameraTargets();
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            UpdateProxyTransform(focusProxy, focusTarget);
            UpdateProxyTransform(overviewProxy, overviewTarget);
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

            Transform? _target = focusProxy ?? focusTarget;
            if (_target == null)
            {
                return;
            }

            focusVirtualCamera.Follow = _target;
            focusVirtualCamera.LookAt = _target;
        }

        private void ApplyOverviewCameraTargets()
        {
            if (overviewVirtualCamera == null)
            {
                return;
            }

            Transform? _target = overviewProxy ?? overviewTarget;
            if (_target == null)
            {
                return;
            }

            overviewVirtualCamera.Follow = _target;
            overviewVirtualCamera.LookAt = _target;
        }

        private void UpdateProxyTransform(Transform? _proxy, Transform? _target)
        {
            if (_proxy == null || _target == null)
            {
                return;
            }

            _proxy.position = _target.position;
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

            float _distance = GetFocusDistance(_solarObject);
            if (!Mathf.Approximately(focusPositionComposer.CameraDistance, _distance))
            {
                focusPositionComposer.CameraDistance = _distance;
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
        #endregion
    }
}

#nullable enable
using Assets.Scripts.Runtime;
using Unity.Cinemachine;
using UnityEngine;

namespace Assets.Scripts.Cameras
{
    /// <summary>
    /// Switches between focus and overview Cinemachine virtual cameras.
    /// </summary>
    public sealed partial class SolarSystemCamera : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Virtual Cameras")]
        // Auto-located by name if left empty.
        private CinemachineCamera? focusVirtualCamera;
        // Auto-located by name if left empty.
        private CinemachineCamera? overviewVirtualCamera;

        [Header("Virtual Camera Names")]
        [Tooltip("Scene name for the focus camera. Example: Follow-SolarObject-VCinemachine")]
        [SerializeField] private string focusVirtualCameraName = "Follow-SolarObject-VCinemachine";
        [Tooltip("Scene name for the overview camera. Example: Overview-SolarSystem-VCinemachine")]
        [SerializeField] private string overviewVirtualCameraName = "Overview-SolarSystem-VCinemachine";

        [Header("Proxy Targets")]
        // Optional proxy objects used for smooth camera motion.
        private Transform? focusProxy;
        // Optional proxy objects used for smooth camera motion.
        private Transform? overviewProxy;

        [Header("Proxy Names")]
        [Tooltip("Scene name for the focus proxy. Example: Focus_Proxy")]
        [SerializeField] private string focusProxyName = "Focus_Proxy";
        [Tooltip("Scene name for the overview proxy. Example: Overview_Proxy")]
        [SerializeField] private string overviewProxyName = "Overview_Proxy";

        // Overview target assigned at runtime (usually the Sun).
        private Transform? overviewTarget;
        private Transform? focusTarget;

        [Header("Proxy Smoothing")]
        [Tooltip("Smoothly move proxies toward their targets. Example: true. When false, proxies snap instantly")]
        [SerializeField] private bool smoothProxyMovement = true;
        [Tooltip("Proxy smooth time in seconds. Higher = slower, lower = snappier. Example: 0.2")]
        [Range(0f, 5f)]
        [SerializeField] private float proxySmoothSeconds = 0.2f;
        [Tooltip("Proxy max speed. Higher = faster travel, lower = slower. Example: 200")]
        [Range(0f, 1000f)]
        [SerializeField] private float proxyMaxSpeed = 200f;
        [Tooltip("Proxy min speed. Higher = less lag, lower = more damping. Example: 2")]
        [Range(0f, 100f)]
        [SerializeField] private float proxyMinSpeed = 2f;
        [Tooltip("Distance range for adaptive proxy speed. Higher = smoother ramp, lower = more aggressive. Example: 30")]
        [Range(0.1f, 500f)]
        [SerializeField] private float proxySpeedDistanceRange = 30f;

        [Header("Zoom Smoothing")]
        [Tooltip("Smoothly interpolate camera distance changes. Example: true. When false, zoom snaps")]
        [SerializeField] private bool smoothZoomDistance = true;
        [Tooltip("Zoom smooth time in seconds. Higher = slower, lower = snappier. Example: 0.16")]
        [Range(0f, 5f)]
        [SerializeField] private float zoomSmoothSeconds = 0.16f;
        [Tooltip("Zoom max speed. Higher = faster zoom, lower = slower. Example: 200")]
        [Range(0f, 1000f)]
        [SerializeField] private float zoomMaxSpeed = 200f;
        [Tooltip("Zoom min speed. Higher = less damping, lower = more damping. Example: 0.1")]
        [Range(0f, 100f)]
        [SerializeField] private float zoomMinSpeed = 0.1f;
        [Tooltip("Distance range for adaptive zoom speed. Higher = smoother ramp, lower = more aggressive. Example: 3")]
        [Range(0.1f, 100f)]
        [SerializeField] private float zoomSpeedDistanceRange = 3f;
        [Tooltip("Extra smoothing when switching focus targets. Higher = slower transition. Example: 2")]
        [Range(0.1f, 5f)]
        [SerializeField] private float focusSwitchSmoothTimeScale = 2.0f;
        [Tooltip("Speed multiplier when switching focus targets. Lower = slower transition. Example: 0.35")]
        [Range(0f, 1f)]
        [SerializeField] private float focusSwitchSpeedScale = 0.35f;
        [Tooltip("Duration for focus switch smoothing in seconds. Higher = longer transition. Example: 0.6")]
        [Range(0f, 5f)]
        [SerializeField] private float focusSwitchSmoothSeconds = 0.6f;

        [Header("Priorities")]
        [Tooltip("Cinemachine priority for focus camera. Higher = more likely active. Example: 20")]
        [Range(0, 100)]
        [SerializeField] private int focusPriority = 20;
        [Tooltip("Cinemachine priority for overview camera. Higher = more likely active. Example: 10")]
        [Range(0, 100)]
        [SerializeField] private int overviewPriority = 10;

        [Header("Focus Distance")]
        [Tooltip("Base focus distance for small objects. Higher = farther framing. Example: 0.35")]
        [Range(0.05f, 50f)]
        [SerializeField] private float focusDistanceSmall = 0.35f;
        [Tooltip("Base focus distance for medium objects. Higher = farther framing. Example: 0.6")]
        [Range(0.05f, 50f)]
        [SerializeField] private float focusDistanceMedium = 0.6f;
        [Tooltip("Base focus distance for the Sun. Higher = farther framing. Example: 1.8")]
        [Range(0.05f, 200f)]
        [SerializeField] private float focusDistanceSun = 1.8f;
        [Tooltip("Diameter threshold for medium objects. Higher = fewer objects treated as medium. Example: 0.3")]
        [Range(0.01f, 5f)]
        [SerializeField] private float focusMediumSizeThreshold = 0.3f;

        [Header("Zoom Controls")]
        [Tooltip("Focus zoom minimum multiplier of base distance. Higher = less zoom-in allowed. Example: 0.15")]
        [Range(0f, 1f)]
        [SerializeField] private float focusZoomMinDistanceMultiplier = 0.15f;
        [Tooltip("Focus zoom maximum multiplier of base distance. Higher = more zoom-out allowed. Example: 1.5")]
        [Range(0f, 10f)]
        [SerializeField] private float focusZoomMaxDistanceMultiplier = 1.5f;
        [Tooltip("Absolute minimum focus distance for the Sun. Higher = keeps camera farther. Example: 1.5")]
        [Range(0.1f, 200f)]
        [SerializeField] private float focusZoomMinDistanceForSun = 1.5f;
        [Tooltip("Extra min distance for large bodies in simulation. Higher = keeps camera farther. Example: 0.0765")]
        [Range(0f, 5f)]
        [SerializeField] private float simulationLargeBodyMinDistanceBoost = 0.0765f;
        [Tooltip("Extra zoom-in allowance for large bodies. Higher = allows closer zoom. Example: 0")]
        [Range(0f, 5f)]
        [SerializeField] private float focusLargeBodyZoomInAllowance = 0.0f;
        [Tooltip("Extra zoom-in allowance for the Sun. Higher = allows closer zoom. Example: 0.539")]
        [Range(0f, 5f)]
        [SerializeField] private float focusSunZoomInAllowance = 0.5390625f;
        [Tooltip("Hard clamp to prevent camera from going too close. Higher = stricter clamp. Example: 0.05")]
        [Range(0.01f, 10f)]
        [SerializeField] private float focusZoomAbsoluteMinDistance = 0.05f;
        [Tooltip("Overview minimum camera distance. Higher = keeps overview farther. Example: 2")]
        [Range(0.1f, 500f)]
        [SerializeField] private float overviewZoomMinDistance = 2f;
        [Tooltip("Overview maximum camera distance. Higher = allows farther zoom out. Example: 40")]
        [Range(1f, 5000f)]
        [SerializeField] private float overviewZoomMaxDistance = 40f;
        [Tooltip("Overview default distance. Higher = start farther out. Example: 5")]
        [Range(0.1f, 500f)]
        [SerializeField] private float overviewDefaultDistance = 5f;
        [Tooltip("Overview zoom speed multiplier. Higher = faster zoom steps. Example: 5")]
        [Range(1f, 50f)]
        [SerializeField] private float overviewZoomSpeedScale = 5f;
        [Tooltip("Focus zoom step per click. Higher = bigger steps. Example: 0.015")]
        [Range(0.001f, 1f)]
        [SerializeField] private float focusZoomStepSize = 0.015f;
        [Tooltip("Overview zoom step per click. Higher = bigger steps. Example: 0.1")]
        [Range(0.001f, 5f)]
        [SerializeField] private float overviewZoomStepSize = 0.1f;

        [Header("Realism Overrides")]
        [Tooltip("Focus zoom min multiplier when realism is 1. Higher = less zoom-in allowed. Example: 0.6")]
        [Range(0f, 2f)]
        [SerializeField] private float realisticFocusZoomMinDistanceMultiplier = 0.6f;
        [Tooltip("Overview min distance when realism is 1. Higher = keeps overview farther. Example: 50")]
        [Range(0.1f, 1000f)]
        [SerializeField] private float realisticOverviewZoomMinDistance = 50f;
        [Tooltip("Overview max distance when realism is 1. Higher = allows farther zoom out. Example: 1000")]
        [Range(1f, 10000f)]
        [SerializeField] private float realisticOverviewZoomMaxDistance = 1000f;
        [Tooltip("Overview zoom speed multiplier when realism is 1. Higher = faster zoom steps. Example: 25")]
        [Range(1f, 100f)]
        [SerializeField] private float realisticOverviewZoomSpeedScale = 25f;

        [Header("Orbit Controls")]
        [Tooltip("Orbit step size in world units. Higher = larger orbit moves. Example: 0.1")]
        [Range(0.01f, 5f)]
        [SerializeField] private float orbitStepSize = 0.1f;
        [Tooltip("Max focus orbit offset in world units. Higher = wider orbit range. Example: 1")]
        [Range(0f, 50f)]
        [SerializeField] private float focusOrbitMaxOffset = 1f;
        [Tooltip("Max overview orbit offset in world units. Higher = wider orbit range. Example: 5")]
        [Range(0f, 200f)]
        [SerializeField] private float overviewOrbitMaxOffset = 5f;
        [Tooltip("Max orbit pitch in degrees. Higher = allow closer to top/bottom. Example: 80")]
        [Range(0f, 89f)]
        [SerializeField] private float orbitMaxPitchDegrees = 80f;
        [Tooltip("Max orbit radius factor of camera distance. Higher = larger orbit radius. Example: 0.5")]
        [Range(0f, 1f)]
        [SerializeField] private float orbitRadiusMaxDistanceFactor = 0.5f;
        [Tooltip("Max orbit step in degrees per input. Higher = larger steps. Example: 10")]
        [Range(0.1f, 45f)]
        [SerializeField] private float orbitStepMaxDegrees = 10f;
        [Tooltip("Overview orbit step multiplier when far away. Higher = faster orbiting. Example: 4")]
        [Range(1f, 20f)]
        [SerializeField] private float overviewOrbitStepDistanceScale = 4f;
        [Tooltip("Distance threshold for overview orbit step boost. Higher = boost starts later. Example: 25")]
        [Range(0.1f, 500f)]
        [SerializeField] private float overviewOrbitStepBoostStartDistance = 25f;

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
        private float realismLevel = 0f;
        private SolarSystemSimulator? simulator = null;
        private Vector3 focusProxyVelocity = Vector3.zero;
        private Vector3 overviewProxyVelocity = Vector3.zero;
        private float focusDesiredDistance = 0f;
        private float overviewDesiredDistance = 0f;
        private float focusDistanceVelocity = 0f;
        private float overviewDistanceVelocity = 0f;
        private float focusSwitchTimer = 0f;
        private float RealismLevel01 => Mathf.Clamp01(realismLevel);
        #endregion
    }
}

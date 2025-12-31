#nullable enable
using Assets.Scripts.Guis;
using Assets.Scripts.Runtime;
using UnityEngine;

namespace Assets.Scripts.Cameras
{
    public sealed partial class SolarSystemCamera
    {
        #region Unity Lifecycle
        private void Awake()
        {
            Gui.CameraOrbitStepRequested += HandleCameraOrbitStepRequested;
            Gui.CameraZoomStepRequested += HandleCameraZoomStepRequested;

            simulator = FindFirstObjectByType<SolarSystemSimulator>();
            if (simulator != null)
            {
                simulator.RealismLevelChanged += HandleRealismLevelChanged;
            }
        }

        private void Start()
        {
            Initialize();
            if (simulator != null)
            {
                ApplyRealismLevel(simulator.RealismLevel);
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
                simulator.RealismLevelChanged -= HandleRealismLevelChanged;
            }
        }
        #endregion
    }
}

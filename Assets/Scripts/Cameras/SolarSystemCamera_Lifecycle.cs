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
            if (!isInitialized || mainCamera == null)
            {
                return;
            }

            float _dt = Time.unscaledDeltaTime;
            if (_dt <= 0f)
            {
                return;
            }

            if (isTransitioning)
            {
                UpdateTransition(_dt);
            }
            else
            {
                UpdateCameraMovement(_dt);
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

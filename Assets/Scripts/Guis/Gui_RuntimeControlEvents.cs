#nullable enable
using Assets.Scripts.Helpers.Debugging;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Binds button and toggle events to the runtime GUI notification hooks.
    /// </summary>
    public sealed class Gui_RuntimeControlEvents : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private Toggle? orbitLinesToggle;
        [SerializeField] private Toggle? spinAxisToggle;
        [SerializeField] private Toggle? worldUpToggle;
        [SerializeField] private Toggle? spinDirectionToggle;
        [SerializeField] private Button? timeScaleMinusButton;
        [SerializeField] private Button? timeScalePlusButton;
        [SerializeField] private Button? visualPresetMinusButton;
        [SerializeField] private Button? visualPresetPlusButton;
        [SerializeField] private Button? cameraOrbitUpButton;
        [SerializeField] private Button? cameraOrbitDownButton;
        [SerializeField] private Button? cameraOrbitLeftButton;
        [SerializeField] private Button? cameraOrbitRightButton;
        [SerializeField] private Button? cameraZoomInButton;
        [SerializeField] private Button? cameraZoomOutButton;
        [SerializeField] private Toggle? planetXToggle;
        #endregion

        #region Runtime State
        private bool isBound = false;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Bind UI events once the scene is ready.
        /// </summary>
        private void Start()
        {
            Bind();
        }

        /// <summary>
        /// Unbind UI events on teardown.
        /// </summary>
        private void OnDestroy()
        {
            Unbind();
        }
        #endregion

        #region Binding
        /// <summary>
        /// Bind button and toggle events once.
        /// </summary>
        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            if (!Gui.EnsureRuntimeWidgets())
            {
                HelpLogs.Warn("Gui", "Runtime widgets not found; can not bind button events.");
                return;
            }

            if (orbitLinesToggle == null)
            {
                orbitLinesToggle = Gui.OrbitLinesToggle;
            }

            if (spinAxisToggle == null)
            {
                spinAxisToggle = Gui.SpinAxisToggle;
            }

            if (worldUpToggle == null)
            {
                worldUpToggle = Gui.WorldUpToggle;
            }

            if (spinDirectionToggle == null)
            {
                spinDirectionToggle = Gui.SpinDirectionToggle;
            }

            if (timeScaleMinusButton == null)
            {
                timeScaleMinusButton = Gui.TimeScaleMinusButton;
            }

            if (timeScalePlusButton == null)
            {
                timeScalePlusButton = Gui.TimeScalePlusButton;
            }

            if (visualPresetMinusButton == null)
            {
                visualPresetMinusButton = Gui.VisualPresetMinusButton;
            }

            if (visualPresetPlusButton == null)
            {
                visualPresetPlusButton = Gui.VisualPresetPlusButton;
            }

            if (cameraOrbitUpButton == null)
            {
                cameraOrbitUpButton = Gui.CameraOrbitUpButton;
            }

            if (cameraOrbitDownButton == null)
            {
                cameraOrbitDownButton = Gui.CameraOrbitDownButton;
            }

            if (cameraOrbitLeftButton == null)
            {
                cameraOrbitLeftButton = Gui.CameraOrbitLeftButton;
            }

            if (cameraOrbitRightButton == null)
            {
                cameraOrbitRightButton = Gui.CameraOrbitRightButton;
            }

            if (cameraZoomInButton == null)
            {
                cameraZoomInButton = Gui.CameraZoomInButton;
            }

            if (cameraZoomOutButton == null)
            {
                cameraZoomOutButton = Gui.CameraZoomOutButton;
            }

            if (planetXToggle == null)
            {
                planetXToggle = Gui.HypotheticalToggle;
            }

            bool boundAny = false;

            if (orbitLinesToggle != null)
            {
                orbitLinesToggle.onValueChanged.AddListener(HandleOrbitLinesChanged);
                boundAny = true;
            }
            else
            {
                HelpLogs.Warn("Gui", "Missing OrbitLinesToggle for runtime events.");
            }

            if (spinAxisToggle != null)
            {
                spinAxisToggle.onValueChanged.AddListener(HandleSpinAxisChanged);
                boundAny = true;
            }
            else
            {
                HelpLogs.Warn("Gui", "Missing SpinAxisToggle for runtime events.");
            }

            if (worldUpToggle != null)
            {
                worldUpToggle.onValueChanged.AddListener(HandleWorldUpChanged);
                boundAny = true;
            }
            else
            {
                HelpLogs.Warn("Gui", "Missing WorldUpToggle for runtime events.");
            }

            if (spinDirectionToggle != null)
            {
                spinDirectionToggle.onValueChanged.AddListener(HandleSpinDirectionChanged);
                boundAny = true;
            }
            else
            {
                HelpLogs.Warn("Gui", "Missing SpinDirectionToggle for runtime events.");
            }

            bool timeScaleButtonsBound = false;
            if (timeScaleMinusButton != null)
            {
                timeScaleMinusButton.onClick.AddListener(HandleTimeScaleMinus);
                timeScaleButtonsBound = true;
            }

            if (timeScalePlusButton != null)
            {
                timeScalePlusButton.onClick.AddListener(HandleTimeScalePlus);
                timeScaleButtonsBound = true;
            }

            if (!timeScaleButtonsBound)
            {
                HelpLogs.Warn("Gui", "Missing time scale buttons.");
            }

            bool presetButtonsBound = false;
            if (visualPresetMinusButton != null)
            {
                visualPresetMinusButton.onClick.AddListener(HandleVisualPresetMinus);
                presetButtonsBound = true;
            }

            if (visualPresetPlusButton != null)
            {
                visualPresetPlusButton.onClick.AddListener(HandleVisualPresetPlus);
                presetButtonsBound = true;
            }

            if (!presetButtonsBound)
            {
                HelpLogs.Warn("Gui", "Missing visual preset buttons.");
            }

            bool cameraOrbitButtonsBound = false;
            if (cameraOrbitUpButton != null)
            {
                cameraOrbitUpButton.onClick.AddListener(HandleCameraOrbitUp);
                cameraOrbitButtonsBound = true;
            }

            if (cameraOrbitDownButton != null)
            {
                cameraOrbitDownButton.onClick.AddListener(HandleCameraOrbitDown);
                cameraOrbitButtonsBound = true;
            }

            if (cameraOrbitLeftButton != null)
            {
                cameraOrbitLeftButton.onClick.AddListener(HandleCameraOrbitLeft);
                cameraOrbitButtonsBound = true;
            }

            if (cameraOrbitRightButton != null)
            {
                cameraOrbitRightButton.onClick.AddListener(HandleCameraOrbitRight);
                cameraOrbitButtonsBound = true;
            }

            if (!cameraOrbitButtonsBound)
            {
                HelpLogs.Warn("Gui", "Missing camera orbit buttons.");
            }

            bool cameraZoomButtonsBound = false;
            if (cameraZoomInButton != null)
            {
                cameraZoomInButton.onClick.AddListener(HandleCameraZoomIn);
                cameraZoomButtonsBound = true;
            }

            if (cameraZoomOutButton != null)
            {
                cameraZoomOutButton.onClick.AddListener(HandleCameraZoomOut);
                cameraZoomButtonsBound = true;
            }

            if (!cameraZoomButtonsBound)
            {
                HelpLogs.Warn("Gui", "Missing camera zoom buttons.");
            }

            if (planetXToggle != null)
            {
                planetXToggle.onValueChanged.AddListener(HandlePlanetXToggleChanged);
                boundAny = true;
            }

            isBound =
                boundAny ||
                timeScaleButtonsBound ||
                presetButtonsBound ||
                cameraOrbitButtonsBound ||
                cameraZoomButtonsBound;
        }

        /// <summary>
        /// Unbind button and toggle events when this component is destroyed.
        /// </summary>
        public void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            if (orbitLinesToggle != null)
            {
                orbitLinesToggle.onValueChanged.RemoveListener(HandleOrbitLinesChanged);
            }

            if (spinAxisToggle != null)
            {
                spinAxisToggle.onValueChanged.RemoveListener(HandleSpinAxisChanged);
            }

            if (worldUpToggle != null)
            {
                worldUpToggle.onValueChanged.RemoveListener(HandleWorldUpChanged);
            }

            if (spinDirectionToggle != null)
            {
                spinDirectionToggle.onValueChanged.RemoveListener(HandleSpinDirectionChanged);
            }

            if (timeScaleMinusButton != null)
            {
                timeScaleMinusButton.onClick.RemoveListener(HandleTimeScaleMinus);
            }

            if (timeScalePlusButton != null)
            {
                timeScalePlusButton.onClick.RemoveListener(HandleTimeScalePlus);
            }

            if (visualPresetMinusButton != null)
            {
                visualPresetMinusButton.onClick.RemoveListener(HandleVisualPresetMinus);
            }

            if (visualPresetPlusButton != null)
            {
                visualPresetPlusButton.onClick.RemoveListener(HandleVisualPresetPlus);
            }

            if (cameraOrbitUpButton != null)
            {
                cameraOrbitUpButton.onClick.RemoveListener(HandleCameraOrbitUp);
            }

            if (cameraOrbitDownButton != null)
            {
                cameraOrbitDownButton.onClick.RemoveListener(HandleCameraOrbitDown);
            }

            if (cameraOrbitLeftButton != null)
            {
                cameraOrbitLeftButton.onClick.RemoveListener(HandleCameraOrbitLeft);
            }

            if (cameraOrbitRightButton != null)
            {
                cameraOrbitRightButton.onClick.RemoveListener(HandleCameraOrbitRight);
            }

            if (cameraZoomInButton != null)
            {
                cameraZoomInButton.onClick.RemoveListener(HandleCameraZoomIn);
            }

            if (cameraZoomOutButton != null)
            {
                cameraZoomOutButton.onClick.RemoveListener(HandleCameraZoomOut);
            }

            if (planetXToggle != null)
            {
                planetXToggle.onValueChanged.RemoveListener(HandlePlanetXToggleChanged);
            }

            isBound = false;
        }
        #endregion

        #region Event Handlers
        private void HandleOrbitLinesChanged(bool _enabled)
        {
            Gui.NotifyOrbitLinesToggled(_enabled);
        }

        private void HandleSpinAxisChanged(bool _enabled)
        {
            Gui.NotifySpinAxisToggled(_enabled);
        }

        private void HandleWorldUpChanged(bool _enabled)
        {
            Gui.NotifyWorldUpToggled(_enabled);
        }

        private void HandleSpinDirectionChanged(bool _enabled)
        {
            Gui.NotifySpinDirectionToggled(_enabled);
        }

        private void HandleTimeScaleMinus()
        {
            Gui.NotifyTimeScaleStepRequested(-1);
        }

        private void HandleTimeScalePlus()
        {
            Gui.NotifyTimeScaleStepRequested(1);
        }

        private void HandleVisualPresetMinus()
        {
            Gui.NotifyVisualPresetStepRequested(-1);
        }

        private void HandleVisualPresetPlus()
        {
            Gui.NotifyVisualPresetStepRequested(1);
        }

        private void HandleCameraOrbitUp()
        {
            Gui.NotifyCameraOrbitStepRequested(Vector2.up);
        }

        private void HandleCameraOrbitDown()
        {
            Gui.NotifyCameraOrbitStepRequested(Vector2.down);
        }

        private void HandleCameraOrbitLeft()
        {
            // Intentional swap for natural screen-space feel.
            Gui.NotifyCameraOrbitStepRequested(Vector2.right);
        }

        private void HandleCameraOrbitRight()
        {
            // Intentional swap for natural screen-space feel.
            Gui.NotifyCameraOrbitStepRequested(Vector2.left);
        }

        private void HandleCameraZoomIn()
        {
            Gui.NotifyCameraZoomStepRequested(1);
        }

        private void HandleCameraZoomOut()
        {
            Gui.NotifyCameraZoomStepRequested(-1);
        }

        private void HandlePlanetXToggleChanged(bool _enabled)
        {
            Gui.NotifyHypotheticalToggleChanged(_enabled);
        }

        #endregion
    }
}

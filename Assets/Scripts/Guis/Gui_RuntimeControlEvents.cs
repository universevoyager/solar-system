#nullable enable
using Assets.Scripts.Helpers.Debugging;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Binds slider events to the runtime GUI notification hooks.
    /// </summary>
    public sealed class Gui_RuntimeControlEvents : MonoBehaviour
    {
        [SerializeField] private Slider? timeScaleSlider;
        [SerializeField] private Slider? visualPresetSlider;
        [SerializeField] private Toggle? orbitLinesToggle;
        [SerializeField] private Toggle? spinAxisToggle;
        [SerializeField] private Toggle? worldUpToggle;

        private bool isBound = false;

        private void Start()
        {
            Bind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Bind slider change events once.
        /// </summary>
        public void Bind()
        {
            if (isBound)
            {
                return;
            }

            if (!Gui.EnsureRuntimeWidgets())
            {
                HelpLogs.Warn("Gui", "Runtime widgets not found; can not bind slider events.");
                return;
            }

            if (timeScaleSlider == null)
            {
                timeScaleSlider = Gui.TimeScaleSlider;
            }

            if (visualPresetSlider == null)
            {
                visualPresetSlider = Gui.VisualPresetSlider;
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

            bool boundAny = false;

            if (timeScaleSlider != null)
            {
                timeScaleSlider.onValueChanged.AddListener(HandleTimeScaleChanged);
                boundAny = true;
            }
            else
            {
                HelpLogs.Warn("Gui", "Missing TimeScaleSlider for runtime events.");
            }

            if (visualPresetSlider != null)
            {
                visualPresetSlider.onValueChanged.AddListener(HandleVisualPresetChanged);
                boundAny = true;
            }
            else
            {
                HelpLogs.Warn("Gui", "Missing VisualPresetSlider for runtime events.");
            }

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

            isBound = boundAny;
        }

        /// <summary>
        /// Unbind slider change events when this component is destroyed.
        /// </summary>
        public void Unbind()
        {
            if (!isBound)
            {
                return;
            }

            if (timeScaleSlider != null)
            {
                timeScaleSlider.onValueChanged.RemoveListener(HandleTimeScaleChanged);
            }

            if (visualPresetSlider != null)
            {
                visualPresetSlider.onValueChanged.RemoveListener(HandleVisualPresetChanged);
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

            isBound = false;
        }

        private void HandleTimeScaleChanged(float _value)
        {
            Gui.NotifyTimeScaleSliderChanged(_value);
        }

        private void HandleVisualPresetChanged(float _value)
        {
            Gui.NotifyVisualPresetSliderChanged(_value);
        }

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
    }
}

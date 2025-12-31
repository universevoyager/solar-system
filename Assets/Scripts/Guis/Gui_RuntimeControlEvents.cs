#nullable enable
using System;
using System.Collections.Generic;
using Assets.Scripts.Helpers.Debugging;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Binds button and toggle events to the runtime GUI notification hooks.
    /// </summary>
    public sealed class Gui_RuntimeControlEvents : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("Optional override. Leave empty to auto-bind Gui.OrbitLinesToggle. Example: OrbitLinesToggle")]
        [SerializeField] private Toggle? orbitLinesToggle;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.SpinAxisToggle. Example: SpinAxisToggle")]
        [SerializeField] private Toggle? spinAxisToggle;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.WorldUpToggle. Example: WorldUpToggle")]
        [SerializeField] private Toggle? worldUpToggle;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.SpinDirectionToggle. Example: SpinDirectionToggle")]
        [SerializeField] private Toggle? spinDirectionToggle;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.TimeScaleMinusButton. Example: TimeScaleMinusButton")]
        [SerializeField] private Button? timeScaleMinusButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.TimeScalePlusButton. Example: TimeScalePlusButton")]
        [SerializeField] private Button? timeScalePlusButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.RealismMinusButton. Example: RealismMinusButton")]
        [SerializeField] private Button? realismMinusButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.RealismPlusButton. Example: RealismPlusButton")]
        [SerializeField] private Button? realismPlusButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.CameraOrbitUpButton. Example: CameraOrbitUpButton")]
        [SerializeField] private Button? cameraOrbitUpButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.CameraOrbitDownButton. Example: CameraOrbitDownButton")]
        [SerializeField] private Button? cameraOrbitDownButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.CameraOrbitLeftButton. Example: CameraOrbitLeftButton")]
        [SerializeField] private Button? cameraOrbitLeftButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.CameraOrbitRightButton. Example: CameraOrbitRightButton")]
        [SerializeField] private Button? cameraOrbitRightButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.CameraZoomInButton. Example: CameraZoomInButton")]
        [SerializeField] private Button? cameraZoomInButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.CameraZoomOutButton. Example: CameraZoomOutButton")]
        [SerializeField] private Button? cameraZoomOutButton;
        [Tooltip("Optional override. Leave empty to auto-bind Gui.HypotheticalToggle. Example: HypotheticalToggleButton")]
        [SerializeField] private Toggle? planetXToggle;

        [Header("Hold Repeat")]
        [Tooltip("Allow press-and-hold repeat for buttons. Example: true")]
        [SerializeField] private bool enableHoldRepeat = true;
        [Tooltip("Seconds before repeat starts. Higher = longer delay, lower = faster repeat start. Example: 0.35")]
        [Range(0.05f, 2f)]
        [SerializeField] private float holdRepeatInitialDelay = 0.35f;
        [Tooltip("Seconds between repeats while held. Lower = faster repeats, higher = slower. Example: 0.08")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float holdRepeatInterval = 0.08f;
        #endregion

        #region Runtime State
        private bool isBound = false;
        private readonly List<HoldRepeatState> holdRepeats = new();
        #endregion

        #region Hold Repeat Types
        private sealed class HoldRepeatState
        {
            public Button? Button;
            public Action? Action;
            public bool IsHeld;
            public float NextRepeatTime;
            public EventTrigger? Trigger;
            public EventTrigger.Entry? PointerDownEntry;
            public EventTrigger.Entry? PointerUpEntry;
            public EventTrigger.Entry? PointerExitEntry;
        }
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

        private void Update()
        {
            if (!enableHoldRepeat || holdRepeats.Count == 0)
            {
                return;
            }

            float _now = Time.unscaledTime;
            float _interval = Mathf.Max(0.01f, holdRepeatInterval);
            for (int _i = 0; _i < holdRepeats.Count; _i++)
            {
                HoldRepeatState _state = holdRepeats[_i];
                if (!_state.IsHeld || _state.Action == null)
                {
                    continue;
                }

                if (_state.Button == null ||
                    !_state.Button.interactable ||
                    !_state.Button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (_now < _state.NextRepeatTime)
                {
                    continue;
                }

                _state.Action.Invoke();
                _state.NextRepeatTime = _now + _interval;
            }
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

            if (realismMinusButton == null)
            {
                realismMinusButton = Gui.RealismMinusButton;
            }

            if (realismPlusButton == null)
            {
                realismPlusButton = Gui.RealismPlusButton;
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
                RegisterHoldRepeat(timeScaleMinusButton, HandleTimeScaleMinus);
            }

            if (timeScalePlusButton != null)
            {
                timeScalePlusButton.onClick.AddListener(HandleTimeScalePlus);
                timeScaleButtonsBound = true;
                RegisterHoldRepeat(timeScalePlusButton, HandleTimeScalePlus);
            }

            if (!timeScaleButtonsBound)
            {
                HelpLogs.Warn("Gui", "Missing time scale buttons.");
            }

            bool realismButtonsBound = false;
            if (realismMinusButton != null)
            {
                realismMinusButton.onClick.AddListener(HandleRealismMinus);
                realismButtonsBound = true;
                RegisterHoldRepeat(realismMinusButton, HandleRealismMinus);
            }

            if (realismPlusButton != null)
            {
                realismPlusButton.onClick.AddListener(HandleRealismPlus);
                realismButtonsBound = true;
                RegisterHoldRepeat(realismPlusButton, HandleRealismPlus);
            }

            if (!realismButtonsBound)
            {
                HelpLogs.Warn("Gui", "Missing realism buttons.");
            }

            bool cameraOrbitButtonsBound = false;
            if (cameraOrbitUpButton != null)
            {
                cameraOrbitUpButton.onClick.AddListener(HandleCameraOrbitUp);
                cameraOrbitButtonsBound = true;
                RegisterHoldRepeat(cameraOrbitUpButton, HandleCameraOrbitUp);
            }

            if (cameraOrbitDownButton != null)
            {
                cameraOrbitDownButton.onClick.AddListener(HandleCameraOrbitDown);
                cameraOrbitButtonsBound = true;
                RegisterHoldRepeat(cameraOrbitDownButton, HandleCameraOrbitDown);
            }

            if (cameraOrbitLeftButton != null)
            {
                cameraOrbitLeftButton.onClick.AddListener(HandleCameraOrbitLeft);
                cameraOrbitButtonsBound = true;
                RegisterHoldRepeat(cameraOrbitLeftButton, HandleCameraOrbitLeft);
            }

            if (cameraOrbitRightButton != null)
            {
                cameraOrbitRightButton.onClick.AddListener(HandleCameraOrbitRight);
                cameraOrbitButtonsBound = true;
                RegisterHoldRepeat(cameraOrbitRightButton, HandleCameraOrbitRight);
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
                RegisterHoldRepeat(cameraZoomInButton, HandleCameraZoomIn);
            }

            if (cameraZoomOutButton != null)
            {
                cameraZoomOutButton.onClick.AddListener(HandleCameraZoomOut);
                cameraZoomButtonsBound = true;
                RegisterHoldRepeat(cameraZoomOutButton, HandleCameraZoomOut);
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
                realismButtonsBound ||
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

            if (realismMinusButton != null)
            {
                realismMinusButton.onClick.RemoveListener(HandleRealismMinus);
            }

            if (realismPlusButton != null)
            {
                realismPlusButton.onClick.RemoveListener(HandleRealismPlus);
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

            ClearHoldRepeats();
            isBound = false;
        }
        #endregion

        #region Hold Repeat Helpers
        private void RegisterHoldRepeat(Button? _button, Action _action)
        {
            if (!enableHoldRepeat || _button == null)
            {
                return;
            }

            for (int _i = 0; _i < holdRepeats.Count; _i++)
            {
                if (holdRepeats[_i].Button == _button)
                {
                    return;
                }
            }

            HoldRepeatState _state = new HoldRepeatState
            {
                Button = _button,
                Action = _action,
                IsHeld = false,
                NextRepeatTime = 0f
            };

            EventTrigger _trigger = _button.GetComponent<EventTrigger>();
            if (_trigger == null)
            {
                _trigger = _button.gameObject.AddComponent<EventTrigger>();
            }

            if (_trigger.triggers == null)
            {
                _trigger.triggers = new List<EventTrigger.Entry>();
            }

            _state.Trigger = _trigger;
            _state.PointerDownEntry = AddTrigger(_trigger, EventTriggerType.PointerDown, () => StartHold(_state));
            _state.PointerUpEntry = AddTrigger(_trigger, EventTriggerType.PointerUp, () => StopHold(_state));
            _state.PointerExitEntry = AddTrigger(_trigger, EventTriggerType.PointerExit, () => StopHold(_state));
            holdRepeats.Add(_state);
        }

        private void StartHold(HoldRepeatState _state)
        {
            if (_state.Button == null || !_state.Button.interactable)
            {
                return;
            }

            _state.IsHeld = true;
            _state.NextRepeatTime = Time.unscaledTime + Mathf.Max(0f, holdRepeatInitialDelay);
        }

        private void StopHold(HoldRepeatState _state)
        {
            _state.IsHeld = false;
        }

        private void ClearHoldRepeats()
        {
            for (int _i = 0; _i < holdRepeats.Count; _i++)
            {
                HoldRepeatState _state = holdRepeats[_i];
                _state.IsHeld = false;

                if (_state.Trigger == null || _state.Trigger.triggers == null)
                {
                    continue;
                }

                if (_state.PointerDownEntry != null)
                {
                    _state.Trigger.triggers.Remove(_state.PointerDownEntry);
                }

                if (_state.PointerUpEntry != null)
                {
                    _state.Trigger.triggers.Remove(_state.PointerUpEntry);
                }

                if (_state.PointerExitEntry != null)
                {
                    _state.Trigger.triggers.Remove(_state.PointerExitEntry);
                }
            }

            holdRepeats.Clear();
        }

        private static EventTrigger.Entry AddTrigger(EventTrigger _trigger, EventTriggerType _type, Action _action)
        {
            EventTrigger.Entry _entry = new EventTrigger.Entry
            {
                eventID = _type
            };
            _entry.callback.AddListener(_ => _action());
            _trigger.triggers.Add(_entry);
            return _entry;
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

        private void HandleRealismMinus()
        {
            Gui.NotifyRealismStepRequested(-1);
        }

        private void HandleRealismPlus()
        {
            Gui.NotifyRealismStepRequested(1);
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

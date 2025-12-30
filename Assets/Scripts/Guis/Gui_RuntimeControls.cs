#nullable enable
using System;
using System.Collections.Generic;
using Assets.Scripts.Helpers.Debugging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Runtime control UI bindings for buttons, toggles, and value labels.
    /// </summary>
    public static partial class Gui
    {
        #region Runtime Widgets
        public static TextMeshProUGUI? TimeScaleValueText { get; private set; }
        public static TextMeshProUGUI? VisualPresetValueText { get; private set; }
        public static TextMeshProUGUI? AppVersionText { get; private set; }

        public static Toggle? OrbitLinesToggle { get; private set; }
        public static Toggle? SpinAxisToggle { get; private set; }
        public static Toggle? WorldUpToggle { get; private set; }
        public static Toggle? SpinDirectionToggle { get; private set; }
        public static Toggle? HypotheticalToggle { get; private set; }
        public static Button? TimeScaleMinusButton { get; private set; }
        public static Button? TimeScalePlusButton { get; private set; }
        public static Button? VisualPresetMinusButton { get; private set; }
        public static Button? VisualPresetPlusButton { get; private set; }
        public static Button? CameraOrbitUpButton { get; private set; }
        public static Button? CameraOrbitDownButton { get; private set; }
        public static Button? CameraOrbitLeftButton { get; private set; }
        public static Button? CameraOrbitRightButton { get; private set; }
        public static Button? CameraZoomInButton { get; private set; }
        public static Button? CameraZoomOutButton { get; private set; }
        private static bool runtimeWidgetsAllocated = false;
        #endregion

        #region Lookups
        private static readonly Dictionary<string, TextMeshProUGUI> textsByName =
            new Dictionary<string, TextMeshProUGUI>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Toggle> togglesByName =
            new Dictionary<string, Toggle>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Button> buttonsByName =
            new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Events
        public static event Action<int>? TimeScaleStepRequested;
        public static event Action<int>? VisualPresetStepRequested;
        public static event Action<Vector2>? CameraOrbitStepRequested;
        public static event Action<int>? CameraZoomStepRequested;
        public static event Action<bool>? OrbitLinesToggled;
        public static event Action<bool>? SpinAxisToggled;
        public static event Action<bool>? WorldUpToggled;
        public static event Action<bool>? SpinDirectionToggled;
        public static event Action<bool>? HypotheticalToggleChanged;
        #endregion

        #region Public Helpers
        /// <summary>
        /// Ensure runtime widgets are allocated at least once.
        /// </summary>
        public static bool EnsureRuntimeWidgets()
        {
            if (runtimeWidgetsAllocated)
            {
                return true;
            }

            AllocateInteractionWidgets();
            return runtimeWidgetsAllocated;
        }

        /// <summary>
        /// Relay a time scale step request to listeners.
        /// </summary>
        public static void NotifyTimeScaleStepRequested(int _delta)
        {
            TimeScaleStepRequested?.Invoke(_delta);
        }

        /// <summary>
        /// Relay a visual preset step request to listeners.
        /// </summary>
        public static void NotifyVisualPresetStepRequested(int _delta)
        {
            VisualPresetStepRequested?.Invoke(_delta);
        }

        /// <summary>
        /// Relay a camera orbit step request to listeners.
        /// </summary>
        public static void NotifyCameraOrbitStepRequested(Vector2 _direction)
        {
            CameraOrbitStepRequested?.Invoke(_direction);
        }

        /// <summary>
        /// Relay a camera zoom step request to listeners.
        /// </summary>
        public static void NotifyCameraZoomStepRequested(int _delta)
        {
            CameraZoomStepRequested?.Invoke(_delta);
        }

        /// <summary>
        /// Relay orbit lines toggle changes to listeners.
        /// </summary>
        public static void NotifyOrbitLinesToggled(bool _enabled)
        {
            OrbitLinesToggled?.Invoke(_enabled);
        }

        /// <summary>
        /// Relay spin axis toggle changes to listeners.
        /// </summary>
        public static void NotifySpinAxisToggled(bool _enabled)
        {
            SpinAxisToggled?.Invoke(_enabled);
        }

        /// <summary>
        /// Relay world-up line toggle changes to listeners.
        /// </summary>
        public static void NotifyWorldUpToggled(bool _enabled)
        {
            WorldUpToggled?.Invoke(_enabled);
        }

        /// <summary>
        /// Relay spin-direction toggle changes to listeners.
        /// </summary>
        public static void NotifySpinDirectionToggled(bool _enabled)
        {
            SpinDirectionToggled?.Invoke(_enabled);
        }

        /// <summary>
        /// Relay hypothetical object toggle changes to listeners.
        /// </summary>
        public static void NotifyHypotheticalToggleChanged(bool _enabled)
        {
            HypotheticalToggleChanged?.Invoke(_enabled);
        }

        #endregion

        #region Allocate and Deallocate
        /// <summary>
        /// Discover and cache all runtime control widgets in the active canvas.
        /// </summary>
        private static void AllocateInteractionWidgets()
        {
            Canvas[] _canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            List<Canvas> _sceneCanvases = new List<Canvas>();
            for (int _i = 0; _i < _canvases.Length; _i++)
            {
                Canvas _canvas = _canvases[_i];
                if (_canvas == null)
                {
                    continue;
                }

                if (!_canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                _sceneCanvases.Add(_canvas);
            }

            if (_sceneCanvases.Count == 0)
            {
                HelpLogs.Warn("Gui", "Can not locate any canvas in this scene");
                return;
            }

            textsByName.Clear();
            togglesByName.Clear();
            buttonsByName.Clear();

            for (int _c = 0; _c < _sceneCanvases.Count; _c++)
            {
                Canvas _canvas = _sceneCanvases[_c];

                TextMeshProUGUI[] _texts = _canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int _i = 0; _i < _texts.Length; _i++)
                {
                    TextMeshProUGUI _text = _texts[_i];
                    if (_text == null)
                    {
                        continue;
                    }

                    if (textsByName.ContainsKey(_text.name))
                    {
                        continue;
                    }

                    textsByName.Add(_text.name, _text);
                }

                Toggle[] _toggles = _canvas.GetComponentsInChildren<Toggle>(true);
                for (int _i = 0; _i < _toggles.Length; _i++)
                {
                    Toggle _toggle = _toggles[_i];
                    if (_toggle == null)
                    {
                        continue;
                    }

                    if (togglesByName.ContainsKey(_toggle.name))
                    {
                        continue;
                    }

                    togglesByName.Add(_toggle.name, _toggle);
                }

                Button[] _buttons = _canvas.GetComponentsInChildren<Button>(true);
                for (int _i = 0; _i < _buttons.Length; _i++)
                {
                    Button _button = _buttons[_i];
                    if (_button == null)
                    {
                        continue;
                    }

                    if (buttonsByName.ContainsKey(_button.name))
                    {
                        continue;
                    }

                    buttonsByName.Add(_button.name, _button);
                }
            }

            AppVersionText = GetTextByName("AppVersionText");
            TimeScaleValueText = GetTextByName("TimeScaleValueText");
            VisualPresetValueText = GetTextByName("VisualPresetValueText");

            OrbitLinesToggle = GetToggleByName("OrbitLinesToggle");
            SpinAxisToggle = GetToggleByName("SpinAxisToggle");
            WorldUpToggle = GetToggleByName("WorldUpToggle");
            SpinDirectionToggle = GetToggleByName("SpinDirectionToggle");
            HypotheticalToggle = TryGetToggleByName("HypotheticalToggleButton");
            TimeScaleMinusButton = TryGetButtonByName("TimeScaleMinusButton");
            TimeScalePlusButton = TryGetButtonByName("TimeScalePlusButton");
            VisualPresetMinusButton = TryGetButtonByName("VisualPresetMinusButton");
            VisualPresetPlusButton = TryGetButtonByName("VisualPresetPlusButton");
            CameraOrbitUpButton = TryGetButtonByName("CameraOrbitUpButton");
            CameraOrbitDownButton = TryGetButtonByName("CameraOrbitDownButton");
            CameraOrbitLeftButton = TryGetButtonByName("CameraOrbitLeftButton");
            CameraOrbitRightButton = TryGetButtonByName("CameraOrbitRightButton");
            CameraZoomInButton = TryGetButtonByName("CameraZoomInButton");
            CameraZoomOutButton = TryGetButtonByName("CameraZoomOutButton");

            bool _hasTimeScaleButtons = TimeScaleMinusButton != null || TimeScalePlusButton != null;
            bool _hasPresetButtons = VisualPresetMinusButton != null || VisualPresetPlusButton != null;
            bool _hasCameraOrbitButtons =
                CameraOrbitUpButton != null ||
                CameraOrbitDownButton != null ||
                CameraOrbitLeftButton != null ||
                CameraOrbitRightButton != null;
            bool _hasCameraZoomButtons = CameraZoomInButton != null || CameraZoomOutButton != null;
            bool _hasPlanetXControl = HypotheticalToggle != null;

            if (!_hasTimeScaleButtons)
            {
                HelpLogs.Warn("Gui", "Missing time scale buttons.");
            }

            if (!_hasPresetButtons)
            {
                HelpLogs.Warn("Gui", "Missing visual preset buttons.");
            }

            if (!_hasCameraOrbitButtons)
            {
                HelpLogs.Warn("Gui", "Missing camera orbit buttons.");
            }

            if (!_hasCameraZoomButtons)
            {
                HelpLogs.Warn("Gui", "Missing camera zoom buttons.");
            }

            if (!_hasPlanetXControl)
            {
                HelpLogs.Warn("Gui", "Missing HypotheticalToggleButton (optional).");
            }

            HelpLogs.Log(
                "Gui",
                $"Allocated {textsByName.Count} texts, {togglesByName.Count} toggles, " +
                $"{buttonsByName.Count} buttons across {_sceneCanvases.Count} canvases."
            );
            runtimeWidgetsAllocated = true;
        }

        /// <summary>
        /// Clear cached references to runtime control widgets.
        /// </summary>
        private static void DeallocateInteractionWidgets()
        {
            textsByName.Clear();
            togglesByName.Clear();
            buttonsByName.Clear();

            AppVersionText = null;
            TimeScaleValueText = null;
            VisualPresetValueText = null;

            OrbitLinesToggle = null;
            SpinAxisToggle = null;
            WorldUpToggle = null;
            SpinDirectionToggle = null;
            HypotheticalToggle = null;
            TimeScaleMinusButton = null;
            TimeScalePlusButton = null;
            VisualPresetMinusButton = null;
            VisualPresetPlusButton = null;
            CameraOrbitUpButton = null;
            CameraOrbitDownButton = null;
            CameraOrbitLeftButton = null;
            CameraOrbitRightButton = null;
            CameraZoomInButton = null;
            CameraZoomOutButton = null;
            HelpLogs.Log("Gui", "Deallocated interaction widgets");
            runtimeWidgetsAllocated = false;
        }
        #endregion

        #region Lookup Helpers
        /// <summary>
        /// Resolve a TextMeshProUGUI by name with a warning when missing.
        /// </summary>
        private static TextMeshProUGUI? GetTextByName(string _name)
        {
            if (textsByName.TryGetValue(_name, out TextMeshProUGUI _text))
            {
                return _text;
            }

            HelpLogs.Warn("Gui", $"Missing TextMeshProUGUI '{_name}' in canvas");
            return null;
        }

        /// <summary>
        /// Resolve a Toggle by name with a warning when missing.
        /// </summary>
        private static Toggle? GetToggleByName(string _name)
        {
            if (togglesByName.TryGetValue(_name, out Toggle _toggle))
            {
                return _toggle;
            }

            HelpLogs.Warn("Gui", $"Missing Toggle '{_name}' in canvas");
            return null;
        }

        /// <summary>
        /// Try to resolve a Button by name without warnings.
        /// </summary>
        private static Button? TryGetButtonByName(string _name)
        {
            if (buttonsByName.TryGetValue(_name, out Button _button))
            {
                return _button;
            }

            return null;
        }

        /// <summary>
        /// Try to resolve a Toggle by name without warnings.
        /// </summary>
        private static Toggle? TryGetToggleByName(string _name)
        {
            if (togglesByName.TryGetValue(_name, out Toggle _toggle))
            {
                return _toggle;
            }

            return null;
        }
        #endregion
    }
}

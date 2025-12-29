#nullable enable
using System;
using System.Collections.Generic;
using Assets.Scripts.Cameras;
using Assets.Scripts.Helpers.Debugging;
using Assets.Scripts.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Guis
{
    /// <summary>
    /// Builds a grid of focus buttons for all spawned solar objects.
    /// </summary>
    public sealed class Gui_SolarObjectGrid : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Scene Names")]
        [SerializeField] private string gridLayoutGroupName = "SolarObjects_View_Interaction_GridLayoutGroup";
        [SerializeField] private string focusButtonTemplateName = "Focus_SolarObject_VCinemachine_Button";
        [SerializeField] private string overviewButtonName = "View_SolarSystem_Overview_VCinemachine_Button";
        [SerializeField] private string buttonTextChildName = "Text";

        [Header("Scene References")]
        [SerializeField] private SolarSystemSimulator? simulator;
        [SerializeField] private SolarSystemCameraController? cameraController;
        #endregion

        #region Runtime State
        private RectTransform? gridRoot;
        private Button? focusButtonTemplate;
        private Button? overviewButton;
        private readonly List<Button> spawnedButtons = new();
        private bool isInitialized = false;
        private bool isBound = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UnbindSimulator();
            UnbindOverviewButton();
            ClearSpawnedButtons();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Find scene references and build the solar object grid.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                HelpLogs.Warn("Gui", "Solar object grid already initialized; skipping duplicate setup.");
                return;
            }

            if (!ResolveSceneReferences())
            {
                return;
            }

            BindSimulator();

            if (simulator != null)
            {
                BuildGrid(simulator.OrderedSolarObjects);
                TryAssignOverviewTarget(simulator.OrderedSolarObjects);
            }

            BindOverviewButton();

            isInitialized = true;
        }
        #endregion

        #region Scene Resolution
        private bool ResolveSceneReferences()
        {
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<SolarSystemCameraController>();
                if (cameraController == null)
                {
                    HelpLogs.Warn("Gui", "SolarSystemCameraController not found in scene.");
                }
            }

            if (simulator == null)
            {
                simulator = FindFirstObjectByType<SolarSystemSimulator>();
                if (simulator == null)
                {
                    HelpLogs.Warn("Gui", "SolarSystemSimulator not found in scene.");
                }
            }

            gridRoot = FindRectTransformByName(gridLayoutGroupName);
            if (gridRoot == null)
            {
                HelpLogs.Warn("Gui", $"Missing grid layout group '{gridLayoutGroupName}'.");
                return false;
            }

            focusButtonTemplate = FindButtonByName(focusButtonTemplateName);
            if (focusButtonTemplate == null)
            {
                HelpLogs.Warn("Gui", $"Missing focus button template '{focusButtonTemplateName}'.");
                return false;
            }

            overviewButton = FindButtonByName(overviewButtonName);
            if (overviewButton == null)
            {
                HelpLogs.Warn("Gui", $"Missing overview button '{overviewButtonName}'.");
            }

            return true;
        }

        private RectTransform? FindRectTransformByName(string _name)
        {
            Transform? _transform = FindTransformByName(_name);
            if (_transform == null)
            {
                return null;
            }

            RectTransform? _rect = _transform.GetComponent<RectTransform>();
            if (_rect == null)
            {
                HelpLogs.Warn("Gui", $"Object '{_name}' is not a RectTransform.");
                return null;
            }

            return _rect;
        }

        private Button? FindButtonByName(string _name)
        {
            Transform? _transform = FindTransformByName(_name);
            if (_transform == null)
            {
                return null;
            }

            Button? _button = _transform.GetComponent<Button>();
            if (_button == null)
            {
                HelpLogs.Warn("Gui", $"Object '{_name}' is missing a Button component.");
                return null;
            }

            return _button;
        }

        private Transform? FindTransformByName(string _name)
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                return null;
            }

            Canvas _canvas = FindFirstObjectByType<Canvas>();
            if (_canvas == null)
            {
                HelpLogs.Warn("Gui", "Can not locate canvas on this scene");
                return null;
            }

            Transform[] _children = _canvas.GetComponentsInChildren<Transform>(true);
            for (int _i = 0; _i < _children.Length; _i++)
            {
                Transform _child = _children[_i];
                if (string.Equals(_child.name, _name, StringComparison.OrdinalIgnoreCase))
                {
                    return _child;
                }
            }

            return null;
        }
        #endregion

        #region Simulator Binding
        private void BindSimulator()
        {
            if (simulator == null || isBound)
            {
                return;
            }

            simulator.SolarObjectsReady += HandleSolarObjectsReady;
            isBound = true;
        }

        private void UnbindSimulator()
        {
            if (simulator == null || !isBound)
            {
                return;
            }

            simulator.SolarObjectsReady -= HandleSolarObjectsReady;
            isBound = false;
        }

        private void HandleSolarObjectsReady(IReadOnlyList<SolarObject> _objects)
        {
            BuildGrid(_objects);
            TryAssignOverviewTarget(_objects);
        }
        #endregion

        #region Grid Building
        private void BuildGrid(IReadOnlyList<SolarObject> _objects)
        {
            if (gridRoot == null || focusButtonTemplate == null)
            {
                return;
            }

            ClearSpawnedButtons();
            focusButtonTemplate.gameObject.SetActive(false);

            if (_objects.Count == 0)
            {
                HelpLogs.Warn("Gui", "No solar objects available for the focus grid.");
                return;
            }

            for (int _i = 0; _i < _objects.Count; _i++)
            {
                SolarObject _object = _objects[_i];

                Button _button = Instantiate(focusButtonTemplate, gridRoot);
                _button.name = $"Focus_{_object.name}";
                _button.gameObject.SetActive(true);
                _button.enabled = true;
                _button.interactable = true;

                SetButtonText(_button, _object.name);

                SolarObject _target = _object;
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => HandleFocusClicked(_target));

                spawnedButtons.Add(_button);
            }

            HelpLogs.Log("Gui", $"Spawned {spawnedButtons.Count} solar object focus buttons.");
        }

        private void ClearSpawnedButtons()
        {
            for (int _i = 0; _i < spawnedButtons.Count; _i++)
            {
                Button _button = spawnedButtons[_i];
                if (_button == null)
                {
                    continue;
                }

                _button.onClick.RemoveAllListeners();
                Destroy(_button.gameObject);
            }

            spawnedButtons.Clear();
        }

        private void SetButtonText(Button _button, string _label)
        {
            TextMeshProUGUI? _text = null;

            if (!string.IsNullOrWhiteSpace(buttonTextChildName))
            {
                Transform _child = _button.transform.Find(buttonTextChildName);
                if (_child != null)
                {
                    _text = _child.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_text == null)
            {
                _text = _button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (_text == null)
            {
                HelpLogs.Warn("Gui", $"Button '{_button.name}' is missing a TextMeshProUGUI child.");
                return;
            }

            _text.text = _label;
        }
        #endregion

        #region Button Handlers
        private void BindOverviewButton()
        {
            if (overviewButton == null)
            {
                return;
            }

            overviewButton.onClick.AddListener(HandleOverviewClicked);
        }

        private void UnbindOverviewButton()
        {
            if (overviewButton == null)
            {
                return;
            }

            overviewButton.onClick.RemoveListener(HandleOverviewClicked);
        }

        private void HandleFocusClicked(SolarObject _object)
        {
            HelpLogs.Log("Gui", $"Focus button pressed: {_object.name}");
            if (cameraController == null)
            {
                HelpLogs.Warn("Gui", "SolarSystemCameraController not found for focus action.");
                return;
            }

            cameraController.FocusOn(_object);
        }

        private void HandleOverviewClicked()
        {
            HelpLogs.Log("Gui", "Overview button pressed.");
            if (cameraController == null)
            {
                HelpLogs.Warn("Gui", "SolarSystemCameraController not found for overview action.");
                return;
            }

            cameraController.ShowOverview();
        }
        #endregion

        #region Overview Target
        private void TryAssignOverviewTarget(IReadOnlyList<SolarObject> _objects)
        {
            if (cameraController == null || cameraController.HasOverviewTarget)
            {
                return;
            }

            for (int _i = 0; _i < _objects.Count; _i++)
            {
                SolarObject _object = _objects[_i];
                if (string.Equals(_object.Id, "sun", StringComparison.OrdinalIgnoreCase))
                {
                    cameraController.SetOverviewTarget(_object.transform);
                    return;
                }
            }
        }
        #endregion
    }
}
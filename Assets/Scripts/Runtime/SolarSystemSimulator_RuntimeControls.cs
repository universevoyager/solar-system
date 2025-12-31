#nullable enable
using System;
using System.Collections.Generic;
using Assets.Scripts.Guis;
using Assets.Scripts.Helpers.Debugging;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Runtime
{
    public sealed partial class SolarSystemSimulator
    {
        #region Runtime Controls
        private static readonly string[] monthNames =
        {
            "Jan",
            "Feb",
            "Mar",
            "Apr",
            "May",
            "Jun",
            "Jul",
            "Aug",
            "Sep",
            "Oct",
            "Nov",
            "Dec",
        };

        private static readonly int[] monthDayCounts =
        {
            31,
            28,
            31,
            30,
            31,
            30,
            31,
            31,
            30,
            31,
            30,
            31,
        };

        /// <summary>
        /// Apply realism blending to the shared visual context.
        /// </summary>
        private void ApplyRealismValues(float _level)
        {
            float _realism = Mathf.Clamp01(_level);
            int _realismSegments =
                realismOrbitLineSegmentsOverride > 0
                    ? realismOrbitLineSegmentsOverride
                    : defaultOrbitLineSegments;
            int _segments = Mathf.RoundToInt(Mathf.Lerp(simulationOrbitLineSegments, _realismSegments, _realism));

            visualContext.GlobalDistanceScale = LerpDouble(
                simulationGlobalDistanceScale,
                defaultGlobalDistanceScale,
                _realism
            );
            visualContext.GlobalRadiusScale = LerpDouble(
                simulationGlobalRadiusScale,
                defaultGlobalRadiusScale,
                _realism
            );
            visualContext.OrbitLineSegments = Math.Max(8, _segments);
            visualContext.RuntimeLineWidthScale = Mathf.Lerp(
                simulationLineWidthScale,
                realismLineWidthScale,
                _realism
            );
            visualContext.VisualDefaultsBlend = 1.0f - _realism;
            visualContext.SimulationScaleBlend = 1.0f - _realism;
        }

        private static double LerpDouble(double _from, double _to, float _t)
        {
            return _from + (_to - _from) * _t;
        }

        /// <summary>
        /// Initialize runtime UI state and sync toggle defaults.
        /// </summary>
        private void SetupRuntimeGui()
        {
            if (!enableRuntimeControls)
            {
                return;
            }

            // Discrete control levels that map to curated values.
            timeScaleLevels[0] = timeScale;
            timeScaleLevels[1] = 1_000.0f;
            timeScaleLevels[2] = 10_000.0f;
            timeScaleLevels[3] = 1_000_000.0f;

            timeScaleLevelIndex = 1;
            timeScale = timeScaleLevels[timeScaleLevelIndex];
            realismLevel = Mathf.Clamp01(realismLevel);

            if (Gui.OrbitLinesToggle != null)
            {
                visualContext.ShowOrbitLines = Gui.OrbitLinesToggle.isOn;
            }

            if (Gui.SpinAxisToggle != null)
            {
                visualContext.ShowSpinAxisLines = Gui.SpinAxisToggle.isOn;
            }

            if (Gui.WorldUpToggle != null)
            {
                visualContext.ShowWorldUpLines = Gui.WorldUpToggle.isOn;
            }

            if (Gui.SpinDirectionToggle != null)
            {
                visualContext.ShowSpinDirectionLines = Gui.SpinDirectionToggle.isOn;
            }

            if (Gui.HypotheticalToggle != null)
            {
                showHypotheticalObjects = Gui.HypotheticalToggle.isOn;
                ApplyHypotheticalVisibility();
                SolarObjectsReady?.Invoke(solarObjectsOrdered);
            }
        }

        /// <summary>
        /// Step the time scale level by a delta.
        /// </summary>
        private void HandleTimeScaleStepRequested(int _delta)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            int _step = _delta == 0 ? 0 : Math.Sign(_delta);
            int _targetIndex = Mathf.Clamp(
                timeScaleLevelIndex + _step,
                0,
                timeScaleLevels.Length - 1
            );

            if (_targetIndex == timeScaleLevelIndex)
            {
                return;
            }

            timeScaleLevelIndex = _targetIndex;
            timeScale = timeScaleLevels[timeScaleLevelIndex];

            UpdateTimeScaleText();
        }

        /// <summary>
        /// Step the realism level by a delta.
        /// </summary>
        private void HandleRealismStepRequested(int _delta)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            int _step = _delta == 0 ? 0 : Math.Sign(_delta);
            if (_step == 0)
            {
                return;
            }

            float _stepSize = Mathf.Max(0.0f, realismStep);
            if (_stepSize <= 0.0f)
            {
                return;
            }
            float _target = Mathf.Clamp01(realismLevel + _stepSize * _step);
            if (Mathf.Approximately(_target, realismLevel))
            {
                return;
            }

            ApplyRealismLevel(_target, true);
        }

        /// <summary>
        /// Toggle orbit line rendering for all objects.
        /// </summary>
        private void HandleOrbitLinesToggled(bool _enabled)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            visualContext.ShowOrbitLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Toggle spin axis line rendering for all objects.
        /// </summary>
        private void HandleSpinAxisToggled(bool _enabled)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            visualContext.ShowSpinAxisLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Toggle world-up line rendering for all objects.
        /// </summary>
        private void HandleWorldUpToggled(bool _enabled)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            visualContext.ShowWorldUpLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Toggle spin-direction arc rendering for all objects.
        /// </summary>
        private void HandleSpinDirectionToggled(bool _enabled)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            visualContext.ShowSpinDirectionLines = _enabled;
            MarkAllLineStylesDirty();
        }

        /// <summary>
        /// Handle the Planet X toggle and update visibility.
        /// </summary>
        private void HandleHypotheticalToggleChanged(bool _enabled)
        {
            if (!runtimeControlsInitialized)
            {
                return;
            }

            if (showHypotheticalObjects == _enabled)
            {
                return;
            }

            showHypotheticalObjects = _enabled;
            ApplyHypotheticalVisibility();
            UpdateHypotheticalToggleText();
            SolarObjectsReady?.Invoke(solarObjectsOrdered);
        }

        /// <summary>
        /// Re-apply visual scaling to all objects.
        /// </summary>
        private void RefreshAllVisuals()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in solarObjectsById)
            {
                _pair.Value.RefreshVisuals(visualContext);
            }
        }

        /// <summary>
        /// Mark runtime line widths as dirty for all objects.
        /// </summary>
        private void MarkAllLineStylesDirty()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in solarObjectsById)
            {
                _pair.Value.MarkLineStylesDirty();
            }
        }

        /// <summary>
        /// Activate or hide hypothetical solar objects in the scene.
        /// </summary>
        private void ApplyHypotheticalVisibility()
        {
            foreach (KeyValuePair<string, SolarObject> _pair in solarObjectsById)
            {
                SolarObject _object = _pair.Value;
                if (!_object.IsHypothetical)
                {
                    continue;
                }

                if (_object.gameObject.activeSelf != showHypotheticalObjects)
                {
                    _object.gameObject.SetActive(showHypotheticalObjects);
                }
            }
        }

        /// <summary>
        /// Update the time scale label.
        /// </summary>
        private void UpdateTimeScaleText()
        {
            if (Gui.TimeScaleValueText != null)
            {
                string _date = FormatSimulationDate(simulationTimeSeconds);
                Gui.TimeScaleValueText.text = $"{timeScale:0.##}x\n({_date})";
            }
        }

        /// <summary>
        /// Convert simulation seconds into a month/day label (no years).
        /// </summary>
        private static string FormatSimulationDate(double _simulationTimeSeconds)
        {
            if (_simulationTimeSeconds <= 0.0)
            {
                return "1 Jan";
            }

            int _dayIndex = (int)Math.Floor(_simulationTimeSeconds / 86400.0);
            if (_dayIndex < 0)
            {
                _dayIndex = 0;
            }

            int _dayOfYear = _dayIndex % 365;
            int _monthIndex = 0;
            int _dayInMonth = _dayOfYear + 1;

            for (int _i = 0; _i < monthDayCounts.Length; _i++)
            {
                int _daysInMonth = monthDayCounts[_i];
                if (_dayInMonth <= _daysInMonth)
                {
                    _monthIndex = _i;
                    break;
                }

                _dayInMonth -= _daysInMonth;
            }

            _monthIndex = Mathf.Clamp(_monthIndex, 0, monthNames.Length - 1);
            return $"{_dayInMonth} {monthNames[_monthIndex]}";
        }

        /// <summary>
        /// Update the application version label once at startup.
        /// </summary>
        private void UpdateAppVersionText()
        {
            if (Gui.AppVersionText == null)
            {
                return;
            }

            string _version = string.IsNullOrWhiteSpace(Application.version) ? "0.0.0" : Application.version;
            Gui.AppVersionText.text = $"v{_version}";
        }

        /// <summary>
        /// Update the realism label with the current blend percent.
        /// </summary>
        private void UpdateRealismText()
        {
            if (Gui.RealismValueText == null)
            {
                return;
            }

            int _percent = Mathf.RoundToInt(RealismLevel01 * 100f);
            Gui.RealismValueText.text = $"Realism {_percent}%";
        }

        /// <summary>
        /// Update the Planet X toggle label text.
        /// </summary>
        private void UpdateHypotheticalToggleText()
        {
            TextMeshProUGUI? _text = null;
            if (Gui.HypotheticalToggle != null)
            {
                Gui.HypotheticalToggle.SetIsOnWithoutNotify(showHypotheticalObjects);
                _text = Gui.HypotheticalToggle.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (_text == null)
            {
                return;
            }

            _text.text = showHypotheticalObjects ? "Planet X: On" : "Planet X: Off";
        }

        /// <summary>
        /// Apply a realism blend and optionally refresh visuals.
        /// </summary>
        private void ApplyRealismLevel(float _level, bool _refreshVisuals)
        {
            float _clamped = Mathf.Clamp01(_level);
            if (Mathf.Approximately(_clamped, realismLevel) && !_refreshVisuals)
            {
                return;
            }

            realismLevel = _clamped;
            ApplyRealismValues(realismLevel);

            RealismLevelChanged?.Invoke(realismLevel);

            HelpLogs.Log(
                "Simulator",
                $"Realism {realismLevel:0.##}: distance {visualContext.GlobalDistanceScale:0.###}, " +
                $"radius {visualContext.GlobalRadiusScale:0.###}, segments {visualContext.OrbitLineSegments}"
            );

            ApplySunLightRealism();

            if (_refreshVisuals)
            {
                RefreshAllVisuals();
            }

            MarkAllLineStylesDirty();
            UpdateRealismText();
        }

        /// <summary>
        /// Apply the Sun point light values based on the current realism blend.
        /// </summary>
        private void ApplySunLightRealism()
        {
            Light? _sunLight = GetSunPointLight();
            if (_sunLight == null)
            {
                return;
            }

            if (_sunLight.type != LightType.Point)
            {
                HelpLogs.Warn("Simulator", "Sun light is not a Point light. Check the Sun prefab.");
            }

            float _realism = RealismLevel01;
            float _targetIntensity = Mathf.Lerp(sunLightSimulationIntensity, sunLightRealisticIntensity, _realism);
            float _targetRange = Mathf.Lerp(sunLightSimulationRange, sunLightRealisticRange, _realism);
            float _targetIndirect = 0.0f;

            bool _needsUpdate =
                !Mathf.Approximately(_sunLight.intensity, _targetIntensity) ||
                !Mathf.Approximately(_sunLight.range, _targetRange) ||
                !Mathf.Approximately(_sunLight.bounceIntensity, _targetIndirect);

            if (_needsUpdate)
            {
                HelpLogs.Warn(
                    "Simulator",
                    $"Sun light values did not match realism {realismLevel:0.##}. Applying intensity " +
                    $"{_targetIntensity:0.###}, range {_targetRange:0.###}."
                );
            }

            _sunLight.intensity = _targetIntensity;
            _sunLight.range = _targetRange;
            _sunLight.bounceIntensity = _targetIndirect;
        }

        /// <summary>
        /// Locate the Sun point light once and cache it for future updates.
        /// </summary>
        private Light? GetSunPointLight()
        {
            if (sunPointLight != null)
            {
                return sunPointLight;
            }

            if (sunPointLightLookupAttempted)
            {
                return null;
            }

            sunPointLightLookupAttempted = true;

            if (!solarObjectsById.TryGetValue("sun", out SolarObject _sun))
            {
                HelpLogs.Warn("Simulator", "Sun solar object not found. Cannot resolve its point light.");
                return null;
            }

            Light[] _lights = _sun.GetComponentsInChildren<Light>(true);
            if (_lights.Length == 0)
            {
                HelpLogs.Warn("Simulator", "No Light component found under the Sun solar object.");
                return null;
            }

            for (int _i = 0; _i < _lights.Length; _i++)
            {
                if (_lights[_i].type == LightType.Point)
                {
                    sunPointLight = _lights[_i];
                    return sunPointLight;
                }
            }

            sunPointLight = _lights[0];
            HelpLogs.Warn("Simulator", "Sun light is not Point. Using the first Light found.");
            return sunPointLight;
        }
        #endregion
    }
}

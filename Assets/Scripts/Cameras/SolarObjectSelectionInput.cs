#nullable enable
using Assets.Scripts.Helpers.Debugging;
using Assets.Scripts.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Assets.Scripts.Cameras
{
    /// <summary>
    /// Select solar objects by raycasting from the screen position.
    /// </summary>
    public sealed class SolarObjectSelectionInput : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Selection")]
        private Camera? raycastCamera;
        private SolarSystemCamera? cameraController;
        [Tooltip("Sphere cast radius in world units. Higher = easier selection, lower = more precise. Example: 0.2")]
        [Range(0.001f, 10f)]
        [SerializeField] private float sphereCastRadius = 0.2f;
        [Tooltip("Max selection distance in world units. Higher = reach farther objects. Example: 5000")]
        [Range(1f, 100000f)]
        [SerializeField] private float maxRayDistance = 5000f;
        [Tooltip("Layer mask for selectable solar objects. Example: Everything")]
        [SerializeField] private LayerMask selectionLayerMask = ~0;
        [Tooltip("Ignore clicks/taps over UI. Example: true")]
        [SerializeField] private bool ignoreUi = true;
        [Tooltip("Focus the camera on the selected solar object. Example: true")]
        [SerializeField] private bool focusOnSelect = true;
        [Tooltip("Log selection debug messages. Example: true")]
        [SerializeField] private bool logSelections = true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            raycastCamera = Camera.main;

            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<SolarSystemCamera>();
                if (cameraController == null)
                {
                    HelpLogs.Warn("Selection", "SolarSystemCamera not found. Selection will only log hits.");
                }
            }
        }

        private void Update()
        {
            if (!TryGetPointerDown(out Vector2 _screenPos, out int _pointerId))
            {
                return;
            }

            if (ignoreUi && IsPointerOverUi(_pointerId))
            {
                return;
            }

            if (raycastCamera == null)
            {
                raycastCamera = Camera.main;
                if (raycastCamera == null)
                {
                    HelpLogs.Warn("Selection", "No camera available for raycasting.");
                    return;
                }
            }

            Ray _ray = raycastCamera.ScreenPointToRay(_screenPos);
            bool _hit = Physics.SphereCast(
                _ray,
                Mathf.Max(0.001f, sphereCastRadius),
                out RaycastHit _hitInfo,
                maxRayDistance,
                selectionLayerMask,
                QueryTriggerInteraction.Ignore
            );

            if (!_hit)
            {
                if (logSelections)
                {
                    HelpLogs.Log("Selection", "No solar object hit.");
                }
                return;
            }

            SolarObject? _solarObject = _hitInfo.collider.GetComponentInParent<SolarObject>();
            if (_solarObject == null)
            {
                if (logSelections)
                {
                    HelpLogs.Log("Selection", $"Hit '{_hitInfo.collider.name}' but no SolarObject found.");
                }
                return;
            }

            if (logSelections)
            {
                HelpLogs.Log("Selection", $"Selected '{_solarObject.name}' ({_solarObject.Id}).");
            }

            if (focusOnSelect && cameraController != null)
            {
                cameraController.FocusOn(_solarObject);
            }
        }
        #endregion

        #region Input Helpers
        private static bool IsPointerOverUi(int _pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (_pointerId >= 0)
            {
                return EventSystem.current.IsPointerOverGameObject(_pointerId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool TryGetPointerDown(out Vector2 _screenPos, out int _pointerId)
        {
            _screenPos = Vector2.zero;
            _pointerId = -1;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _screenPos = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                foreach (TouchControl _touch in Touchscreen.current.touches)
                {
                    if (_touch == null)
                    {
                        continue;
                    }

                    if (_touch.press.wasPressedThisFrame)
                    {
                        _screenPos = _touch.position.ReadValue();
                        _pointerId = _touch.touchId.ReadValue();
                        return true;
                    }
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                _screenPos = Input.mousePosition;
                return true;
            }
#endif

            return false;
        }
        #endregion
    }
}

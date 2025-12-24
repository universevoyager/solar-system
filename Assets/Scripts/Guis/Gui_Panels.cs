#if !UNITY_SERVER || UNITY_EDITOR
using Assets.Scripts.Helpers.Debugging;
using UnityEngine;

namespace Assets.Scripts.Guis
{
    public enum Panel : sbyte
    {
        None = -1,
        Start_Intro = 0, Start_Menu
    }

    public static partial class Gui
    {
        private static readonly System.Collections.Generic.List<GameObject> panels = new();


#region Show & Hide
        /// <summary>
        /// shows specific panel
        /// </summary>
        /// <param name="_panelToShow"></param>
        /// <param name="_hideAllOtherPanels"></param>
        public static void Show(Panel _panelToShow, bool _hideAllOtherPanels = true)
        {
            if (_hideAllOtherPanels)
            {
                Hide();
            }

            foreach (GameObject _panel in panels)
            {
                if(_panel.name == _panelToShow.ToString())
                {
                    _panel.SetActive(true);
                }
            }
        }

        /// <summary>
        /// hides specific panel
        /// </summary>
        /// <param name="_panelToHide"></param>
        public static void Hide(Panel _panelToHide)
        {
            foreach (GameObject _panel in panels)
            {
                if(_panel.name == _panelToHide.ToString())
                {
                    _panel.SetActive(false);
                }
            }
        }

        /// <summary>
        /// hides all panels
        /// </summary>
        public static void Hide()
        {
            foreach (GameObject _panel in panels)
            {
                _panel.SetActive(false);
            }
        }
#endregion



#region Allocate & Deallocate
        public static void AllocatePanels(Canvas _canvas)
        {
            if(_canvas == null)
            {
                HelpLogs.Warn("Gui", "Can not locate canvas on this scene");
                return;
            }

            foreach (Transform _canvasChild in _canvas.transform)
            {
                if(_canvasChild.GetComponent<RectTransform>() != null)
                {
                    HelpLogs.Log("Gui", $"Allocator found {_canvasChild.name} (as RectTransform) on {_canvas.name}");
                    panels.Add(_canvasChild.gameObject);
                }
            }

            HelpLogs.Log("Gui", $"Allocated {panels.Count} panels on canvas {_canvas.name}");
        }

        public static void DeallocatePanels()
        {
            panels.Clear();
            HelpLogs.Log("Gui", "Deallocated panels");
        }
#endregion
    }
}
#endif
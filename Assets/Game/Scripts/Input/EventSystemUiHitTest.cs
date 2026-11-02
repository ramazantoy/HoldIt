using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace HoldIt.Input
{
    /// <summary>
    /// Asks the root prefab's EventSystem. Only interface raycasters count: UI Toolkit panels (Unity gives each panel a
    /// PanelRaycaster while an EventSystem exists) and uGUI canvases; a physics raycaster added for the world never makes
    /// the game area "UI". The query uses the position it is given, so it does not depend on whether the EventSystem has
    /// processed this frame yet. Elements with picking mode Ignore are not hit.
    /// </summary>
    internal sealed class EventSystemUiHitTest : IUiHitTest
    {
        private readonly List<RaycastResult> _results = new List<RaycastResult>();
        private PointerEventData _eventData;
        private EventSystem _eventDataOwner;

        public bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (_eventDataOwner != eventSystem)
            {
                _eventData = new PointerEventData(eventSystem);
                _eventDataOwner = eventSystem;
            }
            _eventData.position = screenPosition;

            eventSystem.RaycastAll(_eventData, _results);
            var hit = false;
            foreach (var result in _results)
                hit |= result.module is PanelRaycaster || result.module is GraphicRaycaster;
            _results.Clear();
            return hit;
        }
    }
}

using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>
    /// Fits its RectTransform to <see cref="Screen.safeArea"/>, keeping a screen's content out of a notch, a camera cutout or
    /// rounded corners. Put it on a full-stretch child of the screen and place the content under it; backgrounds stay on the
    /// parent so they still fill the screen. Rechecks every frame: Android can report the insets late, or change them without
    /// a resize (system bars).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _appliedArea;
        private Vector2Int _appliedScreen;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                Apply();
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            var area = Screen.safeArea;
            var screen = new Vector2(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = area.position / screen;
            rect.anchorMax = (area.position + area.size) / screen;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _appliedArea = area;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}

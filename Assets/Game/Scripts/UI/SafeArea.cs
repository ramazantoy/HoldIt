using UnityEngine;
using UnityEngine.UIElements;

namespace HoldIt.UI
{
    /// <summary>
    /// Keeps a screen's content out of the parts of the display the game cannot use: a notch, a camera cutout, rounded
    /// corners. Every screen's UXML has a full-size container named <see cref="ContainerName"/>; it gets the padding.
    /// </summary>
    public static class SafeArea
    {
        public const string ContainerName = "screen";

        // Android can report the insets after the first layout, or change them without resizing the panel (system bars).
        private const long RecheckMilliseconds = 500;

        /// <summary>
        /// Call once the document has built its tree (the view's OnEnable). Updates when the panel resizes, and when the safe
        /// area changes while the panel size stays the same.
        /// </summary>
        public static void Track(VisualElement documentRoot)
        {
            var container = documentRoot.Required<VisualElement>(ContainerName);
            var applied = Rect.zero;
            container.RegisterCallback<GeometryChangedEvent>(_ => applied = Apply(container));
            container.schedule.Execute(() =>
            {
                if (Screen.safeArea != applied)
                    applied = Apply(container);
            }).Every(RecheckMilliseconds);
        }

        /// <returns>The safe area it applied; <see cref="Rect.zero"/> while the container is not on a panel yet.</returns>
        private static Rect Apply(VisualElement container)
        {
            var panel = container.panel;
            if (panel == null)
                return Rect.zero;

            var safe = Screen.safeArea;
            // ScreenToPanel takes positions measured from the screen's top-left corner; Screen.safeArea is from the bottom-left.
            var topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, Screen.height - safe.yMax));
            var bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, Screen.height - safe.yMin));
            var size = panel.visualTree.layout.size;

            // Padding does not change the container's own size, so this does not trigger another GeometryChangedEvent.
            container.style.paddingLeft = Mathf.Max(0f, topLeft.x);
            container.style.paddingTop = Mathf.Max(0f, topLeft.y);
            container.style.paddingRight = Mathf.Max(0f, size.x - bottomRight.x);
            container.style.paddingBottom = Mathf.Max(0f, size.y - bottomRight.y);
            return safe;
        }
    }
}

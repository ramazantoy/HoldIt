using System;
using UnityEngine.UIElements;

namespace HoldIt.UI
{
    public static class VisualElementExtensions
    {
        /// <summary>
        /// The descendant named <paramref name="name"/>; throws when the UXML has none, so a renamed element fails when the
        /// screen opens instead of leaving a dead button.
        /// </summary>
        public static T Required<T>(this VisualElement root, string name) where T : VisualElement =>
            root.Q<T>(name) ?? throw new InvalidOperationException(
                $"No {typeof(T).Name} named '{name}' under '{root.name}'. Rename it back in the UXML, or update the view.");

        /// <summary>Shows or hides the element and its children; hidden, it takes no space and no pointer input.</summary>
        public static void SetVisible(this VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

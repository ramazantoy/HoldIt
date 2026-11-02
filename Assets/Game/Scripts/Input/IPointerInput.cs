using UnityEngine;

namespace HoldIt.Input
{
    /// <summary>
    /// The player's finger (or mouse) for gameplay, from the Gameplay map's Point and Press. Gameplay code knows input only
    /// through this service and its messages: <see cref="PointerDown"/>, <see cref="PointerMoved"/> and <see cref="PointerUp"/>.
    /// A press that starts over the UI is ignored until it is released, even when it is dragged off the UI.
    /// </summary>
    public interface IPointerInput
    {
        /// <summary>True while a press that started outside the UI is held.</summary>
        bool IsPressed { get; }

        /// <summary>The pointer's last known screen position, in pixels.</summary>
        Vector2 Position { get; }
    }
}

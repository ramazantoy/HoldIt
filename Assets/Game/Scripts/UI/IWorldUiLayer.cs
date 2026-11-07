using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>
    /// The layer of the gameplay HUD that holds what points at a place in the world: speech bubbles and the "+1" over a
    /// character. Its elements are drawn in screen space, so they keep their size, and they fade with the HUD.
    /// </summary>
    public interface IWorldUiLayer
    {
        /// <summary>What an element is spawned under; its elements are centred on their anchors, so place them with <see cref="TryPlace"/>.</summary>
        RectTransform Root { get; }

        /// <summary>Where a world point lands in <see cref="Root"/>, in canvas units. False when the point is behind the camera.</summary>
        bool TryPlace(Vector3 world, out Vector2 local);
    }
}

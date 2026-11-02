using UnityEngine;

namespace HoldIt.Input
{
    /// <summary>A press started outside the UI, at a screen position in pixels.</summary>
    public readonly struct PointerDown
    {
        public readonly Vector2 Position;

        public PointerDown(Vector2 position) => Position = position;
    }
}

using UnityEngine;

namespace HoldIt.Input
{
    /// <summary>
    /// The held press ended at a screen position, in pixels: released, or cut off when an input mode change disabled the
    /// Gameplay map (pause, scene load).
    /// </summary>
    public readonly struct PointerUp
    {
        public readonly Vector2 Position;

        public PointerUp(Vector2 position) => Position = position;
    }
}

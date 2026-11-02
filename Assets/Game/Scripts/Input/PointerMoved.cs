using UnityEngine;

namespace HoldIt.Input
{
    /// <summary>The held press moved to a new screen position, in pixels. At most once per frame.</summary>
    public readonly struct PointerMoved
    {
        public readonly Vector2 Position;

        public PointerMoved(Vector2 position) => Position = position;
    }
}

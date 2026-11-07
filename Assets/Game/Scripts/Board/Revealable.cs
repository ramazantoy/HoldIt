using UnityEngine;

namespace HoldIt.Board
{
    /// <summary>A piece of the standing board that grows in and shrinks out: its transform and the scale it has when shown.</summary>
    public readonly struct Revealable
    {
        public Revealable(Transform transform, Vector3 scale)
        {
            Transform = transform;
            Scale = scale;
        }

        public Transform Transform { get; }
        public Vector3 Scale { get; }
    }
}

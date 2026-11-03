using HoldIt.Characters;
using UnityEngine;

namespace HoldIt.Drawing
{
    /// <summary>A character that can be picked, as the screen shows it, in pixels.</summary>
    public readonly struct PickCandidate
    {
        public readonly CharacterId Id;
        public readonly Vector2 Feet;
        public readonly Vector2 Head;

        /// <summary>How far from the feet-to-head line a press still picks it.</summary>
        public readonly float Radius;

        public PickCandidate(CharacterId id, Vector2 feet, Vector2 head, float radius)
        {
            Id = id;
            Feet = feet;
            Head = head;
            Radius = radius;
        }
    }
}

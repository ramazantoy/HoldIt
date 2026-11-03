using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How a character in its stall sits on the toilet, facing the door, until the stall frees.</summary>
    [Serializable]
    public struct SitStyle
    {
        [SerializeField, Tooltip("Where the character sits, from the toilet's base against the back wall, toward the door, in tiles.")]
        private float _seatOffset;

        [SerializeField, Tooltip("How far the whole character sinks to sit, in tiles.")]
        private float _drop;

        [SerializeField, Range(0f, 120f), Tooltip("How far the legs swing forward, in degrees.")]
        private float _legAngle;

        [SerializeField, Min(0.01f), Tooltip("How long it takes to get from the end of the way in onto the seat, in seconds.")]
        private float _settleSeconds;

        public SitStyle(float seatOffset, float drop, float legAngle, float settleSeconds)
        {
            _seatOffset = seatOffset;
            _drop = drop;
            _legAngle = legAngle;
            _settleSeconds = settleSeconds;
        }

        public static SitStyle Default => new SitStyle(0.3f, 0.12f, 85f, 0.25f);

        public float SeatOffset => _seatOffset;
        public float Drop => _drop;
        public float LegAngle => _legAngle;
        public float SettleSeconds => _settleSeconds;
    }
}

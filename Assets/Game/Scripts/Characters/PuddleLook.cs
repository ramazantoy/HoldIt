using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>The puddle an accident leaves (<see cref="PuddleCurve"/>). Only a sight: it blocks nobody.</summary>
    [Serializable]
    public struct PuddleLook
    {
        [SerializeField, Min(0.1f), Tooltip("How long the puddle stays, in seconds of game time, its fade included.")]
        private float _seconds;

        [SerializeField, Min(0.01f), Tooltip("How long it takes to spread to its full size, in seconds.")]
        private float _growSeconds;

        [SerializeField, Range(0f, 1f), Tooltip("Its size when it appears, as a share of the full one.")]
        private float _growFrom;

        [SerializeField, Min(0.01f), Tooltip("How long it fades at the end, in seconds.")]
        private float _fadeSeconds;

        [SerializeField, Min(0.01f), Tooltip("The smallest puddle across, in tiles; each one is somewhere between this and the largest.")]
        private float _minSize;

        [SerializeField, Min(0.01f), Tooltip("The largest puddle across, in tiles.")]
        private float _maxSize;

        [SerializeField, Tooltip("Its colour; the alpha is its opacity before it fades.")]
        private Color _color;

        public PuddleLook(float seconds, float growSeconds, float growFrom, float fadeSeconds, float minSize, float maxSize,
            Color color)
        {
            _seconds = seconds;
            _growSeconds = growSeconds;
            _growFrom = growFrom;
            _fadeSeconds = fadeSeconds;
            _minSize = minSize;
            _maxSize = maxSize;
            _color = color;
        }

        /// <summary>The prototype's: 12 s, spreading from 30 % in 0.5 s, fading over the last 3 s, 0.56 to 0.88 across.</summary>
        public static PuddleLook Default => new PuddleLook(12f, 0.5f, 0.3f, 3f, 0.56f, 0.88f,
            new Color(0xf2 / 255f, 0xd6 / 255f, 0x41 / 255f, 0.85f));

        public float Seconds => _seconds;
        public float GrowSeconds => _growSeconds;
        public float GrowFrom => _growFrom;
        public float FadeSeconds => _fadeSeconds;
        public float MinSize => _minSize;
        public float MaxSize => _maxSize;
        public Color Color => _color;
    }
}

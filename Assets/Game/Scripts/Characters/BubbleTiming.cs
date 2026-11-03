using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How long a speech bubble pops, stays and fades (<see cref="BubbleCurve"/>), in seconds of game time.</summary>
    [Serializable]
    public struct BubbleTiming
    {
        [SerializeField, Min(0.01f), Tooltip("How long the bubble takes to pop up to full size.")]
        private float _pop;

        [SerializeField, Min(0f), Tooltip("When the bubble starts to fade, counted from when it was shown.")]
        private float _fadeAt;

        [SerializeField, Min(0.01f), Tooltip("How long the fade lasts.")]
        private float _fade;

        public BubbleTiming(float pop, float fadeAt, float fade)
        {
            _pop = pop;
            _fadeAt = fadeAt;
            _fade = fade;
        }

        /// <summary>The prototype's bubble: pops in 0.18 s, fades from 1.2 s over 0.3 s.</summary>
        public static BubbleTiming Default => new BubbleTiming(0.18f, 1.2f, 0.3f);

        public float Pop => _pop;
        public float FadeAt => _fadeAt;
        public float Fade => _fade;
    }
}

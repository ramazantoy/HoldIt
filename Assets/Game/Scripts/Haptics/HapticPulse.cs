using System;
using UnityEngine;

namespace HoldIt.Haptics
{
    /// <summary>One buzz for phones that cannot play the predefined effects (Android 8 and 9).</summary>
    [Serializable]
    public struct HapticPulse
    {
        [SerializeField, Min(1), Tooltip("How long the motor runs, in milliseconds.")]
        private int _milliseconds;

        [SerializeField, Range(1, 255), Tooltip("How strongly it runs, 1 to 255.")]
        private int _amplitude;

        public HapticPulse(int milliseconds, int amplitude)
        {
            _milliseconds = milliseconds;
            _amplitude = amplitude;
        }

        public int Milliseconds => _milliseconds;
        public int Amplitude => _amplitude;
    }
}

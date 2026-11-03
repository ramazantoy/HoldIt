using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How a character's view goes away: in a puff once relieved, or spinning after an accident (<see cref="ExitCurve"/>).</summary>
    [Serializable]
    public struct ExitLook
    {
        [SerializeField, Min(0.01f), Tooltip("How long the puff takes, in seconds.")]
        private float _puffSeconds;

        [SerializeField, Min(1f), Tooltip("How big the character swells before it shrinks away in the puff.")]
        private float _puffPeak;

        [SerializeField, Range(0.01f, 0.99f), Tooltip("When in the puff it is biggest, as a share of its time.")]
        private float _puffPeakAt;

        [SerializeField, Min(0.01f), Tooltip("How long a character with an accident spins and shrinks away, in seconds.")]
        private float _accidentSeconds;

        [SerializeField, Tooltip("How fast it spins meanwhile, in degrees per second.")]
        private float _accidentSpin;

        public ExitLook(float puffSeconds, float puffPeak, float puffPeakAt, float accidentSeconds, float accidentSpin)
        {
            _puffSeconds = puffSeconds;
            _puffPeak = puffPeak;
            _puffPeakAt = puffPeakAt;
            _accidentSeconds = accidentSeconds;
            _accidentSpin = accidentSpin;
        }

        /// <summary>A 0.3 s puff to 1.15 at a third of it; the prototype's accident, 0.5 s at 12 rad/s.</summary>
        public static ExitLook Default => new ExitLook(0.3f, 1.15f, 0.33f, 0.5f, 690f);

        public float PuffSeconds => _puffSeconds;
        public float PuffPeak => _puffPeak;
        public float PuffPeakAt => _puffPeakAt;
        public float AccidentSeconds => _accidentSeconds;
        public float AccidentSpin => _accidentSpin;
    }
}

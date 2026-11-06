using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How a character waiting at a taken stall's door knocks (<see cref="KnockPose"/>). Angles are in degrees.</summary>
    [Serializable]
    public struct KnockStyle
    {
        [SerializeField, Range(0f, 120f), Tooltip("How far forward the knocking arm is raised.")]
        private float _raise;

        [SerializeField, Range(0f, 45f), Tooltip("How far the hand pulls back between two knocks.")]
        private float _pullBack;

        [SerializeField, Min(1), Tooltip("Knocks in a row.")]
        private int _beats;

        [SerializeField, Min(0.05f), Tooltip("Seconds from one knock to the next.")]
        private float _beatSeconds;

        [SerializeField, Min(0.1f), Tooltip("Seconds of rest after the last knock of a row.")]
        private float _pauseSeconds;

        [SerializeField, Min(0f), Tooltip("Seconds the arm takes to come down after a row, and to go up again before the next.")]
        private float _lowerSeconds;

        [SerializeField, Min(0.1f), Tooltip("How quickly the knocking fades in and out when the wait starts and ends: an exponential rate per second.")]
        private float _blendRate;

        public KnockStyle(float raise, float pullBack, int beats, float beatSeconds, float pauseSeconds, float lowerSeconds,
            float blendRate)
        {
            _raise = raise;
            _pullBack = pullBack;
            _beats = beats;
            _beatSeconds = beatSeconds;
            _pauseSeconds = pauseSeconds;
            _lowerSeconds = lowerSeconds;
            _blendRate = blendRate;
        }

        /// <summary>Three knocks 0.16 s apart, then 1.1 s of rest.</summary>
        public static KnockStyle Default => new KnockStyle(75f, 18f, 3, 0.16f, 1.1f, 0.2f, 10f);

        public float Raise => _raise;
        public float PullBack => _pullBack;
        public int Beats => _beats;
        public float BeatSeconds => _beatSeconds;
        public float PauseSeconds => _pauseSeconds;
        public float LowerSeconds => _lowerSeconds;
        public float BlendRate => _blendRate;
    }
}

using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How patience shows: the bar's colour (<see cref="PatienceColor"/>) and the shaking when it is nearly out (<see cref="PatienceShake"/>).</summary>
    [Serializable]
    public struct PatienceLook
    {
        [SerializeField, Range(0f, 1f), Tooltip("The bar's hue with all its patience left; it slides to 0 (red) as patience runs out.")]
        private float _calmHue;

        [SerializeField, Range(0f, 1f), Tooltip("The bar's saturation (HSL).")]
        private float _saturation;

        [SerializeField, Range(0f, 1f), Tooltip("The bar's lightness (HSL).")]
        private float _lightness;

        [SerializeField, Range(0f, 1f), Tooltip("The patience used up past which the character starts to shake, harder up to the end.")]
        private float _shakeFrom;

        [SerializeField, Min(0f), Tooltip("How far the body shakes sideways at the end, in tiles.")]
        private float _shakeAmplitude;

        [SerializeField, Min(0f), Tooltip("How fast it shakes, in radians per second.")]
        private float _shakeFrequency;

        public PatienceLook(float calmHue, float saturation, float lightness, float shakeFrom, float shakeAmplitude,
            float shakeFrequency)
        {
            _calmHue = calmHue;
            _saturation = saturation;
            _lightness = lightness;
            _shakeFrom = shakeFrom;
            _shakeAmplitude = shakeAmplitude;
            _shakeFrequency = shakeFrequency;
        }

        /// <summary>The prototype's: green to red at 85 % saturation and 50 % lightness, shaking past 70 %.</summary>
        public static PatienceLook Default => new PatienceLook(0.33f, 0.85f, 0.5f, 0.7f, 0.03f, 47f);

        public float CalmHue => _calmHue;
        public float Saturation => _saturation;
        public float Lightness => _lightness;
        public float ShakeFrom => _shakeFrom;
        public float ShakeAmplitude => _shakeAmplitude;
        public float ShakeFrequency => _shakeFrequency;
    }
}

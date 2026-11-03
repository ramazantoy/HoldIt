using System;
using HoldIt.Characters;
using UnityEngine;

namespace HoldIt.Levels
{
    /// <summary>How often each body size comes in, relative to the others: only the ratios count.</summary>
    [Serializable]
    public struct SizeWeights
    {
        [SerializeField, Min(0f)] private float _thin;
        [SerializeField, Min(0f)] private float _normal;
        [SerializeField, Min(0f)] private float _fat;

        public SizeWeights(float thin, float normal, float fat)
        {
            _thin = thin;
            _normal = normal;
            _fat = fat;
        }

        /// <summary>The GDD's mix: 30 % thin, 45 % normal, 25 % fat.</summary>
        public static SizeWeights Default => new SizeWeights(0.30f, 0.45f, 0.25f);

        public float Thin => _thin;
        public float Normal => _normal;
        public float Fat => _fat;
        public float Total => _thin + _normal + _fat;

        public float Of(BodySize size) => size switch
        {
            BodySize.Thin => _thin,
            BodySize.Normal => _normal,
            _ => _fat,
        };
    }
}

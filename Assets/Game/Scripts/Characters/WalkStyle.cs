using System;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How a walking character swings its limbs (<see cref="WalkPose"/>). Angles are in degrees.</summary>
    [Serializable]
    public struct WalkStyle
    {
        [SerializeField, Min(0.01f), Tooltip("Full walk cycles (a step with each leg) per tile walked.")]
        private float _cyclesPerTile;

        [SerializeField, Range(0f, 90f), Tooltip("How far each leg swings forward and back.")]
        private float _legSwing;

        [SerializeField, Range(0f, 90f), Tooltip("How far each arm swings, against the leg on its side.")]
        private float _armSwing;

        [SerializeField, Min(0f), Tooltip("How high the body rises while the legs pass each other, in tiles.")]
        private float _bob;

        [SerializeField, Range(0f, 30f), Tooltip("How far the body rocks from side to side.")]
        private float _sway;

        [SerializeField, Min(0.1f), Tooltip("How quickly the walk fades in and out: an exponential rate per second.")]
        private float _blendRate;

        public WalkStyle(float cyclesPerTile, float legSwing, float armSwing, float bob, float sway, float blendRate)
        {
            _cyclesPerTile = cyclesPerTile;
            _legSwing = legSwing;
            _armSwing = armSwing;
            _bob = bob;
            _sway = sway;
            _blendRate = blendRate;
        }

        /// <summary>The prototype's walk: about 0.8 cycles a tile, legs ±40°.</summary>
        public static WalkStyle Default => new WalkStyle(0.8f, 40f, 30f, 0.04f, 4f, 10f);

        public float CyclesPerTile => _cyclesPerTile;
        public float LegSwing => _legSwing;
        public float ArmSwing => _armSwing;
        public float Bob => _bob;
        public float Sway => _sway;
        public float BlendRate => _blendRate;
    }
}

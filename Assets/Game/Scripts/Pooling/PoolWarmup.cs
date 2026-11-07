using AF.Core;
using HoldIt.Characters;
using HoldIt.Drawing;
using UnityEngine;

namespace HoldIt.Pooling
{
    /// <summary>
    /// How many instances of each pooled prefab are made while the intro plays. More are made when a level needs them, so
    /// these only have to cover the usual crowd.
    /// </summary>
    [CreateAssetMenu(fileName = "PoolWarmup", menuName = "HoldIt/Pool Warmup")]
    public sealed class PoolWarmup : ScriptableObject
    {
        [SerializeField, Required, Tooltip("Its marker prefab is the one the paths are drawn with.")]
        private DrawingConfig _drawing;

        [SerializeField, Min(0), Tooltip("Of each of the six characters (gender and size).")]
        private int _characters = 10;

        [SerializeField, Min(0), Tooltip("Marks on the board at once: the cells of every drawn and walked path.")]
        private int _markers = 60;

        [SerializeField, Min(0)] private int _speechBubbles = 8;
        [SerializeField, Min(0)] private int _floatTexts = 4;
        [SerializeField, Min(0)] private int _puffs = 6;
        [SerializeField, Min(0)] private int _puddles = 6;

        public DrawingConfig Drawing => _drawing;
        public int Characters => _characters;
        public int Markers => _markers;
        public int SpeechBubbles => _speechBubbles;
        public int FloatTexts => _floatTexts;
        public int Puffs => _puffs;
        public int Puddles => _puddles;
    }
}

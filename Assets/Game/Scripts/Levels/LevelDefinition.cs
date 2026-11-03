using AF.Core;
using HoldIt.Board;
using UnityEngine;

namespace HoldIt.Levels
{
    /// <summary>
    /// One level: the map it is played on and how busy it gets. Several levels can share a map. The levels' order is
    /// <see cref="LevelList"/>'s; "HoldIt/Levels/Generate" writes a first set from a curve, then each is tuned by hand.
    /// "AF/Validate All" reports a level that cannot be played.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Levels/Level", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField, Required, Tooltip("The map this level is played on.")]
        private MapDefinition _map;

        [SerializeField, Min(1), Tooltip("How many people come in. The level is won once all of them have come and gone, unless the lives run out first.")]
        private int _crowd = 8;

        [SerializeField, Min(0.1f), Tooltip("Seconds between the first two arrivals.")]
        private float _spawnStart = 4.5f;

        [SerializeField, Min(0.1f), Tooltip("Seconds between the last two arrivals. The gaps shrink evenly from the first one to this.")]
        private float _spawnEnd = 3.2f;

        [SerializeField, Min(1), Tooltip("The most characters on the board at once; nobody comes in while it is full.")]
        private int _maxCharacters = 8;

        [SerializeField, Tooltip("How often each body size comes in, relative to the others.")]
        private SizeWeights _sizes = SizeWeights.Default;

        public MapDefinition Map => _map;
        public int Crowd => _crowd;
        public float SpawnStart => _spawnStart;
        public float SpawnEnd => _spawnEnd;
        public int MaxCharacters => _maxCharacters;
        public SizeWeights Sizes => _sizes;
    }
}

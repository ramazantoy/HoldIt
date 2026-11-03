using System;
using HoldIt.Levels;
using UnityEngine;

namespace HoldIt.Editor
{
    /// <summary>
    /// How the generated levels get busier: every number goes evenly from the first level's value to the last's. Fat
    /// characters join from a later level, so the first levels teach the doors before the narrow passages.
    /// </summary>
    [Serializable]
    public struct LevelRamp
    {
        [SerializeField, Tooltip("How many come in: the first level's, the last's.")]
        private Vector2Int _crowd;

        [SerializeField, Tooltip("Seconds between a level's first two arrivals: the first level's, the last's.")]
        private Vector2 _spawnStart;

        [SerializeField, Tooltip("Seconds between a level's last two arrivals: the first level's, the last's.")]
        private Vector2 _spawnEnd;

        [SerializeField, Tooltip("The most characters on the board at once: the first level's, the last's.")]
        private Vector2Int _maxCharacters;

        [SerializeField, Range(0f, 1f), Tooltip("Thin characters' weight, the same in every level.")]
        private float _thin;

        [SerializeField, Tooltip("Fat characters' weight: in the first level they come in, in the last.")]
        private Vector2 _fat;

        [SerializeField, Min(1), Tooltip("The first level with Fat characters; before it there are none. Normal ones take what thin and fat leave.")]
        private int _fatFromLevel;

        public LevelRamp(Vector2Int crowd, Vector2 spawnStart, Vector2 spawnEnd, Vector2Int maxCharacters, float thin,
            Vector2 fat, int fatFromLevel)
        {
            _crowd = crowd;
            _spawnStart = spawnStart;
            _spawnEnd = spawnEnd;
            _maxCharacters = maxCharacters;
            _thin = thin;
            _fat = fat;
            _fatFromLevel = fatFromLevel;
        }

        /// <summary>M6's first set: 6 to 36 people, gaps 4.6–3.4 s down to 2.6–1.5 s, 8 to 14 on the board, Fat from level 3.</summary>
        public static LevelRamp Default => new LevelRamp(new Vector2Int(6, 36), new Vector2(4.6f, 2.6f),
            new Vector2(3.4f, 1.5f), new Vector2Int(8, 14), 0.30f, new Vector2(0.15f, 0.35f), 3);

        /// <summary>The numbers of level <paramref name="index"/> (from 0) of <paramref name="count"/>.</summary>
        public LevelNumbers At(int index, int count)
        {
            if (count < 1 || index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Level {index} is not one of {count}.");

            var t = count > 1 ? index / (count - 1f) : 0f;
            var spawnStart = Hundredths(Mathf.Lerp(_spawnStart.x, _spawnStart.y, t));
            var spawnEnd = Mathf.Min(spawnStart, Hundredths(Mathf.Lerp(_spawnEnd.x, _spawnEnd.y, t)));

            var level = index + 1;
            var fat = 0f;
            if (level >= _fatFromLevel)
            {
                var fatLevels = count - _fatFromLevel;
                var fatT = fatLevels > 0 ? (level - _fatFromLevel) / (float)fatLevels : 0f;
                fat = Hundredths(Mathf.Lerp(_fat.x, _fat.y, fatT));
            }
            var normal = Mathf.Max(0f, Hundredths(1f - _thin - fat));

            return new LevelNumbers(
                Mathf.RoundToInt(Mathf.Lerp(_crowd.x, _crowd.y, t)),
                spawnStart,
                spawnEnd,
                Mathf.RoundToInt(Mathf.Lerp(_maxCharacters.x, _maxCharacters.y, t)),
                new SizeWeights(_thin, normal, fat));
        }

        private static float Hundredths(float value) => Mathf.Round(value * 100f) / 100f;
    }

    /// <summary>One generated level's numbers.</summary>
    public readonly struct LevelNumbers
    {
        public readonly int Crowd;
        public readonly float SpawnStart;
        public readonly float SpawnEnd;
        public readonly int MaxCharacters;
        public readonly SizeWeights Sizes;

        public LevelNumbers(int crowd, float spawnStart, float spawnEnd, int maxCharacters, SizeWeights sizes)
        {
            Crowd = crowd;
            SpawnStart = spawnStart;
            SpawnEnd = spawnEnd;
            MaxCharacters = maxCharacters;
            Sizes = sizes;
        }
    }
}

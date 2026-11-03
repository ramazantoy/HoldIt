using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Levels
{
    /// <summary>Every level, in the order they are played. The root scope holds it (<see cref="ILevelCursor"/>).</summary>
    [CreateAssetMenu(menuName = "HoldIt/Levels/Level List", fileName = "Levels")]
    public sealed class LevelList : ScriptableObject
    {
        [SerializeField, Tooltip("The levels in the order they are played. \"AF/Validate All\" reports an empty list or a missing level.")]
        private LevelDefinition[] _levels = Array.Empty<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => _levels;
    }
}

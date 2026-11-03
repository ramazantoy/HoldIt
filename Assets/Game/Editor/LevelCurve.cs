using HoldIt.Board;
using UnityEngine;

namespace HoldIt.Editor
{
    /// <summary>
    /// What "HoldIt/Levels/Generate" writes the levels from (<see cref="LevelGenerator"/>): how many, the map a new level
    /// starts on, and how they get busier. Editor only.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Levels/Level Curve", fileName = "LevelCurve")]
    public sealed class LevelCurve : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("How many levels to write.")]
        private int _count = 30;

        [SerializeField, Tooltip("The map a level gets when it is written for the first time; an existing level keeps its own.")]
        private MapDefinition _map;

        [SerializeField, Tooltip("How the levels get busier, from the first to the last.")]
        private LevelRamp _ramp = LevelRamp.Default;

        public int Count => _count;
        public MapDefinition Map => _map;
        public LevelRamp Ramp => _ramp;
    }
}

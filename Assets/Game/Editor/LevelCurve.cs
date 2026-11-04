using UnityEngine;

namespace HoldIt.Editor
{
    /// <summary>
    /// What "HoldIt/Levels/Generate" writes the levels' numbers from (<see cref="LevelGenerator"/>): how many, and how they
    /// get busier. Maps are not its business: every level has its own, bound by "HoldIt/Maps/Generate" or by hand. Editor only.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Levels/Level Curve", fileName = "LevelCurve")]
    public sealed class LevelCurve : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("How many levels to write.")]
        private int _count = 30;

        [SerializeField, Tooltip("How the levels get busier, from the first to the last.")]
        private LevelRamp _ramp = LevelRamp.Default;

        public int Count => _count;
        public LevelRamp Ramp => _ramp;
    }
}

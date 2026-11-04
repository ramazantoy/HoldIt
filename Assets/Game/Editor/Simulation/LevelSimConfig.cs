using UnityEngine;

namespace HoldIt.Editor.Simulation
{
    /// <summary>
    /// How the simulated levels are measured (<see cref="LevelSim"/>, <see cref="Difficulty"/>, <see cref="LevelTuner"/>) and
    /// how hard they should come out. Difficulty is headroom: how much more pressure (closer arrivals, a fuller board) a
    /// level takes before a player drawing at the reference pace stops winning. Editor only.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Levels/Level Sim Config", fileName = "LevelSimConfig")]
    public sealed class LevelSimConfig : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Plays of every level and pace, each with its own spawn seed.")]
        private int _seeds = 12;

        [SerializeField, Range(0.01f, 0.2f), Tooltip("Seconds of game time per simulated frame.")]
        private float _timeStep = 0.05f;

        [SerializeField, Min(10f), Tooltip("Game seconds after which a play counts as stuck.")]
        private float _timeLimit = 600f;

        [SerializeField, Range(0.05f, 1f), Tooltip("Share of the plays the player must win to count as winning.")]
        private float _winShare = 0.8f;

        [SerializeField, Min(0.1f), Tooltip("Seconds between paths of the player the levels are tuned for: a person's steady drawing pace.")]
        private float _referencePace = 1.5f;

        [SerializeField, Tooltip("The fastest and slowest paces tried for a level's needed pace, in seconds between paths.")]
        private Vector2 _paceRange = new Vector2(0.4f, 6f);

        [SerializeField, Min(2), Tooltip("Paces tried from the slowest to the fastest, evenly apart on a log scale.")]
        private int _paceSteps = 12;

        [SerializeField, Min(0), Tooltip("Halvings between the slowest winning pace tried and the next slower one.")]
        private int _refineSteps = 3;

        [SerializeField, Tooltip("How many times its own pressure each level should take before the reference player stops winning: the first level's, the last's.")]
        private Vector2 _targetHeadroom = new Vector2(1.8f, 1.1f);

        [SerializeField, Tooltip("Levels that bring something new in (a map band's first level): their target headroom is larger by the ease below.")]
        private int[] _easedLevels = { 3, 4, 6, 9, 12, 16, 21, 26 };

        [SerializeField, Min(0f), Tooltip("Added to an eased level's target headroom: the numbers step back where something new comes in.")]
        private float _ease = 0.15f;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far from its target a level's headroom may be, as a part of the target.")]
        private float _targetTolerance = 0.1f;

        [SerializeField, Tooltip("The pressures searched for the most the reference player still wins; pressure divides the gaps and multiplies the most characters on the board.")]
        private Vector2 _pressureRange = new Vector2(0.5f, 4f);

        [SerializeField, Min(1), Tooltip("Halvings of the pressure range in that search.")]
        private int _pressureSteps = 8;

        public int Seeds => _seeds;
        public float TimeStep => _timeStep;
        public float TimeLimit => _timeLimit;
        public float WinShare => _winShare;
        public float ReferencePace => _referencePace;
        public float FastestPace => Mathf.Min(_paceRange.x, _paceRange.y);
        public float SlowestPace => Mathf.Max(_paceRange.x, _paceRange.y);
        public int PaceSteps => _paceSteps;
        public int RefineSteps => _refineSteps;
        public float TargetTolerance => _targetTolerance;
        public float LeastPressure => Mathf.Max(0.01f, Mathf.Min(_pressureRange.x, _pressureRange.y));
        public float MostPressure => Mathf.Max(_pressureRange.x, _pressureRange.y);
        public int PressureSteps => _pressureSteps;

        /// <summary>
        /// The target headroom of level <paramref name="index"/> (from 0) of <paramref name="count"/>: evenly between the
        /// ends, larger by the ease on an eased level.
        /// </summary>
        public float TargetHeadroom(int index, int count)
        {
            var headroom = Mathf.Lerp(_targetHeadroom.x, _targetHeadroom.y, count > 1 ? index / (count - 1f) : 0f);
            return System.Array.IndexOf(_easedLevels, index + 1) >= 0 ? headroom + _ease : headroom;
        }
    }
}

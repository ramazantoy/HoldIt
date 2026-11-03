using UnityEngine;

namespace HoldIt.Levels
{
    /// <summary>The rules every level shares: the lives, and how its end is shown.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Levels/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Lives at the start of every level. Each accident costs one; the level is lost with the last. The HUD has three hearts and the result screen three stars: add to them for more.")]
        private int _lives = 3;

        [SerializeField, Min(0f), Tooltip("Seconds the board keeps going once the level ends, so the last accident plays out, before it pauses under the result screen.")]
        private float _endDelay = 0.7f;

        public int Lives => _lives;
        public float EndDelay => _endDelay;
    }
}

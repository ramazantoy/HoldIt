using UnityEngine;

namespace HoldIt.Spawning
{
    /// <summary>How arrivals are timed and mixed in every level; how many and how often is each level's own.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Spawning/Spawn Config", fileName = "SpawnConfig")]
    public sealed class SpawnConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Seconds from the level's start to the first arrival.")]
        private float _firstDelay = 1.0f;

        [SerializeField, Min(0.05f), Tooltip("Seconds until the next try when nobody can come in: the board is at its limit, every entrance or lobby cell is taken, or no way leads there.")]
        private float _retryDelay = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("The share of men among the arrivals.")]
        private float _maleShare = 0.5f;

        public float FirstDelay => _firstDelay;
        public float RetryDelay => _retryDelay;
        public float MaleShare => _maleShare;
    }
}

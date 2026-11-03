using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How characters move. Speeds are in tiles per second, which on the grid are world units too.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Characters/Character Config", fileName = "CharacterConfig")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f), Tooltip("Thin characters' walking speed, in tiles per second.")]
        private float _thinSpeed = 3.0f;

        [SerializeField, Min(0.1f), Tooltip("Normal characters' walking speed, in tiles per second.")]
        private float _normalSpeed = 2.6f;

        [SerializeField, Min(0.1f), Tooltip("Fat characters' walking speed, in tiles per second.")]
        private float _fatSpeed = 2.0f;

        [SerializeField, Min(0f), Tooltip("How long a character waits before a cell someone else stands on, in seconds; then it stops where it is.")]
        private float _blockedWaitSeconds = 0.6f;

        [SerializeField, Min(0.1f), Tooltip("How fast every character walks from its restroom's door into a stall, in world units per second.")]
        private float _stallWalkSpeed = 2.4f;

        public float BlockedWaitSeconds => _blockedWaitSeconds;
        public float StallWalkSpeed => _stallWalkSpeed;

        public float WalkSpeed(BodySize size) => size switch
        {
            BodySize.Thin => _thinSpeed,
            BodySize.Normal => _normalSpeed,
            _ => _fatSpeed,
        };
    }
}

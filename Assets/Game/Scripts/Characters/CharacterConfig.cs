using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>How characters move. Speeds are in tiles per second.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Characters/Character Config", fileName = "CharacterConfig")]
    public sealed class CharacterConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f), Tooltip("Thin characters' walking speed, in tiles per second.")]
        private float _thinSpeed = 3.0f;

        [SerializeField, Min(0.1f), Tooltip("Normal characters' walking speed, in tiles per second.")]
        private float _normalSpeed = 2.6f;

        [SerializeField, Min(0.1f), Tooltip("Fat characters' walking speed, in tiles per second.")]
        private float _fatSpeed = 2.0f;

        public float WalkSpeed(BodySize size) => size switch
        {
            BodySize.Thin => _thinSpeed,
            BodySize.Normal => _normalSpeed,
            _ => _fatSpeed,
        };
    }
}

using AF.Core;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>
    /// The prefab of every gender and size, and how their views move. Swapping a prefab here restyles that character.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Characters/Character Set", fileName = "CharacterSet")]
    public sealed class CharacterSet : ScriptableObject
    {
        [SerializeField, Required] private CharacterView _manThin;
        [SerializeField, Required] private CharacterView _manNormal;
        [SerializeField, Required] private CharacterView _manFat;
        [SerializeField, Required] private CharacterView _womanThin;
        [SerializeField, Required] private CharacterView _womanNormal;
        [SerializeField, Required] private CharacterView _womanFat;

        [SerializeField, Min(0.1f), Tooltip("How quickly a character turns to where it faces: an exponential rate per second, the larger the sharper.")]
        private float _turnRate = 14f;

        [SerializeField, Tooltip("How walking characters swing their limbs.")]
        private WalkStyle _walk = WalkStyle.Default;

        [SerializeField, Tooltip("How speech bubbles pop up and fade.")]
        private BubbleTiming _bubble = BubbleTiming.Default;

        public float TurnRate => _turnRate;
        public WalkStyle Walk => _walk;
        public BubbleTiming Bubble => _bubble;

        public CharacterView Prefab(Gender gender, BodySize size) => gender == Gender.Male
            ? size switch { BodySize.Thin => _manThin, BodySize.Normal => _manNormal, _ => _manFat }
            : size switch { BodySize.Thin => _womanThin, BodySize.Normal => _womanNormal, _ => _womanFat };
    }
}

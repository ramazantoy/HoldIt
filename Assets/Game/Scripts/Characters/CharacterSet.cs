using AF.Core;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>
    /// The prefab of every gender and size, how their views move, show their patience and leave, and the puddle an accident
    /// leaves. Swapping a prefab here restyles that character.
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

        [SerializeField, Tooltip("How a character waiting at a taken stall's door knocks.")]
        private KnockStyle _knock = KnockStyle.Default;

        [SerializeField, Min(0.01f), Tooltip("How long a new character takes to pop in, in seconds.")]
        private float _spawnPopSeconds = 0.28f;

        [SerializeField, Tooltip("How speech bubbles pop up and fade.")]
        private BubbleTiming _bubble = BubbleTiming.Default;

        [SerializeField, Tooltip("How the patience bar colours and when a character starts to shake.")]
        private PatienceLook _patience = PatienceLook.Default;

        [SerializeField, Tooltip("How a character in its stall sits until the stall frees.")]
        private SitStyle _sit = SitStyle.Default;

        [SerializeField, Tooltip("How a character goes: in a puff once relieved, spinning after an accident.")]
        private ExitLook _exit = ExitLook.Default;

        [SerializeField, Required, Tooltip("What an accident leaves on the floor.")]
        private PuddleView _puddle;

        [SerializeField, Tooltip("How the puddle spreads, stays and fades.")]
        private PuddleLook _puddleLook = PuddleLook.Default;

        [SerializeField, Required, Tooltip("The cloud that bursts where a relieved character goes.")]
        private PuffView _puff;

        [SerializeField, Min(0.1f), Tooltip("How long a puff stays before it goes back to its pool, in seconds; at least its particles' life.")]
        private float _puffSeconds = 0.7f;

        [SerializeField, Tooltip("How high above the character's feet the puff bursts, in tiles.")]
        private float _puffHeight = 0.5f;

        [SerializeField, Required, Tooltip("The text that rises over a character when it is served.")]
        private FloatTextView _floatText;

        [SerializeField, Min(0.1f), Tooltip("How long that text rises and fades, in seconds.")]
        private float _floatSeconds = 1f;

        [SerializeField, Min(0f), Tooltip("How far it rises, in tiles.")]
        private float _floatRise = 0.9f;

        [SerializeField, Min(0f), Tooltip("How far above a character's head anchor it starts, in tiles.")]
        private float _floatStartHeight = 0.3f;

        public PuffView Puff => _puff;
        public float PuffSeconds => _puffSeconds;
        public float PuffHeight => _puffHeight;
        public FloatTextView FloatText => _floatText;
        public float FloatSeconds => _floatSeconds;
        public float FloatRise => _floatRise;
        public float FloatStartHeight => _floatStartHeight;
        public float TurnRate => _turnRate;
        public WalkStyle Walk => _walk;
        public KnockStyle Knock => _knock;
        public float SpawnPopSeconds => _spawnPopSeconds;
        public BubbleTiming Bubble => _bubble;
        public PatienceLook Patience => _patience;
        public SitStyle Sit => _sit;
        public ExitLook Exit => _exit;
        public PuddleView Puddle => _puddle;
        public PuddleLook PuddleLook => _puddleLook;

        public CharacterView Prefab(Gender gender, BodySize size) => gender == Gender.Male
            ? size switch { BodySize.Thin => _manThin, BodySize.Normal => _manNormal, _ => _manFat }
            : size switch { BodySize.Thin => _womanThin, BodySize.Normal => _womanNormal, _ => _womanFat };
    }
}

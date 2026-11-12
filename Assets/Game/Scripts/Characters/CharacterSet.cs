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

        [SerializeField, Required, Tooltip("The bubble that is drawn in the HUD's world layer when a character speaks.")]
        private ScreenSpeechBubble _speechBubble;

        [SerializeField, Tooltip("Where a bubble sits against the speaker's head anchor, in canvas units (up is positive).")]
        private Vector2 _bubbleScreenOffset = new Vector2(0f, 90f);

        [SerializeField, Tooltip("Padding around a bubble's text, in canvas units.")]
        private Vector2 _bubblePadding = new Vector2(40f, 20f);

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
        private BurstView _puff;

        [SerializeField, Tooltip("How high above the character's feet the puff bursts, in tiles.")]
        private float _puffHeight = 0.5f;

        [SerializeField, Required, Tooltip("The dust that rises around a character as it arrives on the board.")]
        private BurstView _dust;

        [SerializeField, Min(0f), Tooltip("How high above the floor the dust bursts, in tiles.")]
        private float _dustHeight = 0.05f;

        [SerializeField, Required, Tooltip("The drops that fly where an accident happens.")]
        private BurstView _splash;

        [SerializeField, Min(0f), Tooltip("How high above the floor the splash bursts, in tiles.")]
        private float _splashHeight = 0.1f;

        [SerializeField, Required, Tooltip("The sparkles at a character's head when it gets into a stall.")]
        private BurstView _sparkle;

        [SerializeField, Required, Tooltip("The drops that run off a character close to an accident (a looping system that follows it).")]
        private BurstView _sweat;

        [SerializeField, Range(0f, 1f), Tooltip("The patience used up from which a waiting character starts to sweat.")]
        private float _sweatFrom = 0.55f;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far under that it falls before the sweat stops, so it does not blink at the edge.")]
        private float _sweatMargin = 0.05f;

        [SerializeField, Tooltip("How high above a character's head anchor the sweat comes from, in tiles.")]
        private float _sweatHeight = 0.05f;

        [SerializeField, Required, Tooltip("The text that rises over a character when it is served (drawn in the HUD's world layer).")]
        private FloatTextView _floatText;

        [SerializeField, Min(0.1f), Tooltip("How long that text rises and fades, in seconds.")]
        private float _floatSeconds = 1f;

        [SerializeField, Min(0f), Tooltip("How far it rises, in tiles.")]
        private float _floatRise = 0.9f;

        [SerializeField, Min(0f), Tooltip("How far above a character's head anchor it starts, in tiles.")]
        private float _floatStartHeight = 0.3f;

        public BurstView Puff => _puff;
        public float PuffHeight => _puffHeight;
        public BurstView Dust => _dust;
        public float DustHeight => _dustHeight;
        public BurstView Splash => _splash;
        public float SplashHeight => _splashHeight;
        public BurstView Sparkle => _sparkle;
        public BurstView Sweat => _sweat;
        public float SweatFrom => _sweatFrom;
        public float SweatMargin => _sweatMargin;
        public float SweatHeight => _sweatHeight;
        public FloatTextView FloatText => _floatText;
        public float FloatSeconds => _floatSeconds;
        public float FloatRise => _floatRise;
        public float FloatStartHeight => _floatStartHeight;
        public float TurnRate => _turnRate;
        public WalkStyle Walk => _walk;
        public KnockStyle Knock => _knock;
        public float SpawnPopSeconds => _spawnPopSeconds;
        public BubbleTiming Bubble => _bubble;
        public ScreenSpeechBubble SpeechBubble => _speechBubble;
        public Vector2 BubbleScreenOffset => _bubbleScreenOffset;
        public Vector2 BubblePadding => _bubblePadding;
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

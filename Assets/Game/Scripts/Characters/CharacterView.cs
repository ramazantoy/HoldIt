using AF.Core;
using HoldIt.Board;
using HoldIt.Feedback;
using HoldIt.Restrooms;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// Shows one character while it is on the board. Every frame it stands where its model is, between the step's two cells
    /// or on its way into a stall, turns smoothly to where it walks and keeps facing that way when it stops. Its limbs
    /// follow the walk cycle (<see cref="WalkPose"/>), driven by the distance the view moved, so they rest while the
    /// character waits or the game is paused. Its patience shows on the bar over its head (<see cref="PatienceBar"/>), and
    /// it shakes once nearly out (<see cref="PatienceShake"/>). It only reads the model, so a step that stops or turns back
    /// needs nothing here. Once the character has left the board the view stops following it and its
    /// <see cref="CharacterExit"/> plays the way out.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public sealed class CharacterView : MonoBehaviour, IPoolable
    {
        private const float MovedEpsilon = 1e-5f;

        // Just below zero, so the knock at time zero counts when the wait begins.
        private const float NotKnocking = -1e-4f;

        [SerializeField, Required] private CharacterRig _rig;
        [SerializeField, Required] private SpeechBubble _bubble;
        [SerializeField, Required] private PatienceBar _bar;
        [SerializeField, Required] private CharacterExit _exit;

        private ICharacters _characters;
        private ITileGrid _grid;
        private IGameClock _clock;
        private IFeedback _feedback;
        private CharacterSet _set;
        private CharacterId _id;
        private bool _following;
        private StallVisit _visit;
        private RestPose _rest;
        private Vector3 _lastPosition;
        private float _phase;
        private float _blend;
        private Vector3 _restScale;
        private float _knockTime;
        private float _knockBlend;

        public CharacterId Id => _id;

        /// <summary>Where the top of the character is, for things that rise over it.</summary>
        public Vector3 HeadPosition => _rig.AnchorTop.position;

        [Inject]
        public void Construct(IGameClock clock, CharacterSet set)
        {
            _clock = clock;
            _set = set;
        }

        /// <summary>
        /// Follows the character from now on, starting where it is and facing where it faces. The level's and the scene's
        /// services come with it, and are let go in <see cref="OnDespawned"/>: the view is pooled in the root.
        /// </summary>
        public void Bind(CharacterId id, LevelViewContext context)
        {
            _id = id;
            _characters = context.Characters;
            _grid = context.Grid;
            _feedback = context.Feedback;
            _bar.Bind(context.Projection);
            _exit.Bind(context.Restrooms);
            _bubble.Bind(context);
            if (!_characters.TryGet(id, out var character))
                return;

            _following = true;
            transform.SetPositionAndRotation(PositionOf(character), RotationOf(character));
            _lastPosition = transform.position;
        }

        /// <summary>Says why the character gave up a step.</summary>
        public void Say(StepRefusal reason)
        {
            switch (reason)
            {
                case StepRefusal.WrongDoor:
                    _bubble.Show(BubbleLine.WrongDoor);
                    break;
                case StepRefusal.TooWide:
                    _bubble.Show(BubbleLine.CantFit);
                    break;
                case StepRefusal.Blocked:
                    _bubble.Show(BubbleLine.Blocked);
                    break;
            }
        }

        /// <summary>
        /// The character went into its stall and left the board: the view sits down on the stall's toilet until
        /// <see cref="Relieve"/>. False when the view never saw it walk in; then it goes at once.
        /// </summary>
        public bool TrySit(out StallVisit visit)
        {
            visit = _following ? _visit : null;
            StopFollowing();
            if (visit == null)
            {
                _exit.Relieve();
                return false;
            }
            _exit.Sit(visit);
            return true;
        }

        /// <summary>Its stall freed: it says it is relieved and goes in a puff.</summary>
        public void Relieve()
        {
            StopFollowing();
            _exit.Relieve();
        }

        /// <summary>Its patience ran out and it left the board: it says so, spins and shrinks away where it is.</summary>
        public void Fail()
        {
            StopFollowing();
            _exit.Fail();
        }

        public void OnSpawned(PoolLife life)
        {
            _following = false;
            _visit = null;
            _phase = 0f;
            _blend = 0f;
            _knockTime = NotKnocking;
            _knockBlend = 0f;
            _rest.Apply(_rig, default, 0f);
            transform.localScale = Vector3.zero;
            Tween.Scale(transform, Vector3.zero, _restScale, _set.SpawnPopSeconds, Ease.OutBack)
                .SetCancellationToken(life.Token);
        }

        public void OnDespawned()
        {
            _following = false;
            _visit = null;
            _id = default;
            _characters = null;
            _grid = null;
            _feedback = null;
        }

        private void Awake()
        {
            _rest = RestPose.Of(_rig);
            _restScale = transform.localScale;
        }

        private void StopFollowing()
        {
            _following = false;
            _bar.Hide();
        }

        private void LateUpdate()
        {
            if (!_following || !_characters.TryGet(_id, out var character))
                return;

            var deltaTime = _clock.DeltaTime;
            var inStall = character.State == CharacterState.InStall;
            if (inStall)
                _visit = character.Visit;

            var position = PositionOf(character);
            var turn = 1f - Mathf.Exp(-_set.TurnRate * deltaTime);
            transform.SetPositionAndRotation(position, Quaternion.Slerp(transform.rotation, RotationOf(character), turn));

            var style = _set.Walk;
            var moved = Vector3.Distance(position, _lastPosition);
            _lastPosition = position;
            _phase = Mathf.Repeat(_phase + moved * style.CyclesPerTile, 1f);
            var target = moved > MovedEpsilon ? 1f : 0f;
            _blend = Mathf.Lerp(_blend, target, 1f - Mathf.Exp(-style.BlendRate * deltaTime));
            var shake = inStall ? 0f : PatienceShake.Offset(_clock.Time, character.Patience, _set.Patience);
            _rest.Apply(_rig, Knock(WalkPose.At(_phase, _blend, style), character.State == CharacterState.Queued, deltaTime), shake);

            if (inStall)
                _bar.Hide();
            else
                _bar.Show(character.Patience);
        }

        /// <summary>
        /// Raises the right arm and knocks while the character is queued at a taken stall's door; the arm comes down when
        /// the wait ends. The clock only runs while queued, so the first knock lands the moment the character arrives.
        /// </summary>
        private WalkPose Knock(WalkPose pose, bool queued, float deltaTime)
        {
            var style = _set.Knock;
            if (queued)
            {
                var before = _knockTime;
                _knockTime += deltaTime;
                if (KnockPose.Hits(before, _knockTime, style) > 0)
                    _feedback.Play(FeedbackKind.Knock);
            }
            else
                _knockTime = NotKnocking;

            _knockBlend = Mathf.Lerp(_knockBlend, queued ? 1f : 0f, 1f - Mathf.Exp(-style.BlendRate * deltaTime));
            if (_knockBlend < MovedEpsilon)
                return pose;
            return pose.WithArmRight(pose.ArmRight + KnockPose.Arm(_knockTime, style) * _knockBlend);
        }

        private Vector3 PositionOf(ICharacter character)
        {
            if (character.State == CharacterState.InStall)
                return character.Visit.Line.PointAt(character.VisitDistance);

            var space = _grid.Space;
            return Vector3.Lerp(space.CellCenter(character.Cell), space.CellCenter(character.Next), character.StepProgress);
        }

        private static Quaternion RotationOf(ICharacter character) => character.State == CharacterState.InStall
            ? Quaternion.LookRotation(character.Visit.Line.DirectionAt(character.VisitDistance))
            : character.Facing.Rotation();
    }
}

using AF.Core;
using HoldIt.Board;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// Shows one character: every frame it stands where its model is, between the step's two cells, turns smoothly to
    /// where it walks and keeps facing that way when it stops. Its limbs follow the walk cycle (<see cref="WalkPose"/>),
    /// driven by the distance the view moved, so they rest while the character waits or the game is paused. It only reads
    /// the model, so a step that stops or turns back needs nothing here. Pooled: <see cref="Bind"/> follows a character
    /// from its spawn.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public sealed class CharacterView : MonoBehaviour, IPoolable
    {
        private const float MovedEpsilon = 1e-5f;

        [SerializeField, Required] private CharacterRig _rig;
        [SerializeField, Required] private SpeechBubble _bubble;

        private ICharacters _characters;
        private ITileGrid _grid;
        private IGameClock _clock;
        private CharacterSet _set;
        private CharacterId _id;
        private bool _bound;
        private RestPose _rest;
        private Vector3 _lastPosition;
        private float _phase;
        private float _blend;

        public CharacterId Id => _id;

        [Inject]
        public void Construct(ICharacters characters, ITileGrid grid, IGameClock clock, CharacterSet set)
        {
            _characters = characters;
            _grid = grid;
            _clock = clock;
            _set = set;
        }

        /// <summary>Follows the character from now on, starting where it is and facing where it faces.</summary>
        public void Bind(CharacterId id)
        {
            _id = id;
            _bound = _characters.TryGet(id, out var character);
            if (!_bound)
                return;

            transform.SetPositionAndRotation(PositionOf(character), character.Facing.Rotation());
            _lastPosition = transform.position;
        }

        /// <summary>Says why the character gave up a step.</summary>
        public void Say(StepRefusal reason) => _bubble.Show(reason);

        public void OnSpawned(PoolLife life)
        {
            _bound = false;
            _phase = 0f;
            _blend = 0f;
            _rest.Apply(_rig, default);
        }

        public void OnDespawned()
        {
            _bound = false;
            _id = default;
        }

        private void Awake() => _rest = RestPose.Of(_rig);

        private void LateUpdate()
        {
            if (!_bound || !_characters.TryGet(_id, out var character))
                return;

            var deltaTime = _clock.DeltaTime;
            var position = PositionOf(character);
            var turn = 1f - Mathf.Exp(-_set.TurnRate * deltaTime);
            transform.SetPositionAndRotation(position,
                Quaternion.Slerp(transform.rotation, character.Facing.Rotation(), turn));

            var style = _set.Walk;
            var moved = Vector3.Distance(position, _lastPosition);
            _lastPosition = position;
            _phase = Mathf.Repeat(_phase + moved * style.CyclesPerTile, 1f);
            var target = moved > MovedEpsilon ? 1f : 0f;
            _blend = Mathf.Lerp(_blend, target, 1f - Mathf.Exp(-style.BlendRate * deltaTime));
            _rest.Apply(_rig, WalkPose.At(_phase, _blend, style));
        }

        private Vector3 PositionOf(ICharacter character)
        {
            var space = _grid.Space;
            return Vector3.Lerp(space.CellCenter(character.Cell), space.CellCenter(character.Next), character.StepProgress);
        }

        /// <summary>The prefab's own local pose of the moving parts, which the walk is added on top of.</summary>
        private struct RestPose
        {
            private Vector3 _body;
            private Quaternion _bodyRotation;
            private Quaternion _legLeft;
            private Quaternion _legRight;
            private Quaternion _armLeft;
            private Quaternion _armRight;

            public static RestPose Of(CharacterRig rig) => new RestPose
            {
                _body = rig.Body.localPosition,
                _bodyRotation = rig.Body.localRotation,
                _legLeft = rig.LegLeft.localRotation,
                _legRight = rig.LegRight.localRotation,
                _armLeft = rig.ArmLeft.localRotation,
                _armRight = rig.ArmRight.localRotation,
            };

            /// <summary>Forward is +Z, and a positive X rotation swings a hanging limb back, so the pose's angles flip.</summary>
            public void Apply(CharacterRig rig, WalkPose pose)
            {
                rig.Body.localPosition = _body + Vector3.up * pose.BodyRise;
                rig.Body.localRotation = _bodyRotation * Quaternion.Euler(0f, 0f, pose.BodyRock);
                rig.LegLeft.localRotation = _legLeft * Quaternion.Euler(-pose.LegLeft, 0f, 0f);
                rig.LegRight.localRotation = _legRight * Quaternion.Euler(-pose.LegRight, 0f, 0f);
                rig.ArmLeft.localRotation = _armLeft * Quaternion.Euler(-pose.ArmLeft, 0f, 0f);
                rig.ArmRight.localRotation = _armRight * Quaternion.Euler(-pose.ArmRight, 0f, 0f);
            }
        }
    }
}

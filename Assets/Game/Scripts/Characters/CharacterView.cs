using AF.Core;
using HoldIt.Board;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// Shows one character: every frame it stands where its model is, between the step's two cells, and turns smoothly to
    /// where it walks; Idle characters face the camera (South). It only reads the model, so a step that stops or turns back
    /// needs nothing here. Pooled: <see cref="Bind"/> follows a character from its spawn.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public sealed class CharacterView : MonoBehaviour, IPoolable
    {
        private ICharacters _characters;
        private ITileGrid _grid;
        private IGameClock _clock;
        private CharacterSet _set;
        private CharacterId _id;
        private bool _bound;

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
            if (_bound)
                transform.SetPositionAndRotation(PositionOf(character), RotationOf(character));
        }

        public void OnSpawned(PoolLife life) => _bound = false;

        public void OnDespawned()
        {
            _bound = false;
            _id = default;
        }

        private void LateUpdate()
        {
            if (!_bound || !_characters.TryGet(_id, out var character))
                return;

            var turn = 1f - Mathf.Exp(-_set.TurnRate * _clock.DeltaTime);
            transform.SetPositionAndRotation(PositionOf(character),
                Quaternion.Slerp(transform.rotation, RotationOf(character), turn));
        }

        private Vector3 PositionOf(ICharacter character)
        {
            var space = _grid.Space;
            return Vector3.Lerp(space.CellCenter(character.Cell), space.CellCenter(character.Next), character.StepProgress);
        }

        private static Quaternion RotationOf(ICharacter character) =>
            (character.State == CharacterState.Idle ? Direction.South : character.Facing).Rotation();
    }
}

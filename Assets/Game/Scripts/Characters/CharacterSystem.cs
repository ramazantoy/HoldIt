using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Paths;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Characters
{
    /// <summary>
    /// Drives every character (the presenter the GDD calls CharacterController): adds them, hands them their paths, and
    /// advances them each frame with game time, so they stop while the game is paused. It is also their step gate: the
    /// tile rules (<see cref="StepRules"/>) for walkers, then the cell claims (<see cref="CellClaims"/>) for everyone.
    /// </summary>
    internal sealed class CharacterSystem : ICharacters, ITickable, IStepGate
    {
        private readonly List<Character> _characters = new List<Character>();
        private readonly Dictionary<CharacterId, Character> _byId = new Dictionary<CharacterId, Character>();
        private readonly CellClaims _claims = new CellClaims();
        private readonly ITileGrid _grid;
        private readonly CharacterConfig _config;
        private readonly IGameClock _clock;
        private readonly IPublisher<CharacterSpawned> _spawned;
        private readonly IPublisher<CharacterStateChanged> _stateChanged;
        private readonly IPublisher<StepRefused> _refused;
        private int _lastId;

        public CharacterSystem(ITileGrid grid, CharacterConfig config, IGameClock clock,
            IPublisher<CharacterSpawned> spawned, IPublisher<CharacterStateChanged> stateChanged,
            IPublisher<StepRefused> refused)
        {
            _grid = grid;
            _config = config;
            _clock = clock;
            _spawned = spawned;
            _stateChanged = stateChanged;
            _refused = refused;
        }

        public IReadOnlyList<ICharacter> All => _characters;

        public bool TryGet(CharacterId id, out ICharacter character)
        {
            var found = _byId.TryGetValue(id, out var model);
            character = model;
            return found;
        }

        public CharacterId Spawn(Gender gender, BodySize size, Cell cell)
        {
            if (!_grid.IsWalkable(cell))
                throw new ArgumentException($"A character cannot stand on {cell}: it is not walkable.", nameof(cell));
            if (!_claims.IsFree(cell))
                throw new ArgumentException($"A character cannot stand on {cell}: someone holds or reserved it.", nameof(cell));

            var id = new CharacterId(++_lastId);
            var character = new Character(id, gender, size, cell, _config.WalkSpeed(size), _config.BlockedWaitSeconds,
                _config.ReturnSpeedFactor);
            _characters.Add(character);
            _byId.Add(id, character);
            _claims.Occupy(cell, id);
            _spawned.Publish(new CharacterSpawned(id, gender, size, cell));
            return id;
        }

        public bool TryWalk(CharacterId id, CellPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!_byId.TryGetValue(id, out var character) || !CanEndOn(id, path.End) || !character.TryWalk(path))
                return false;

            Reserve(path.Start, id);
            Reserve(path.End, id);
            _stateChanged.Publish(new CharacterStateChanged(id, character.State));
            return true;
        }

        public bool CanEndOn(CharacterId id, Cell cell) =>
            _grid.InBounds(cell) && (_grid.At(cell).Type == TileType.Door || _claims.IsFreeFor(cell, id));

        public bool IsFree(Cell cell) => _grid.InBounds(cell) && _claims.IsFree(cell);

        public void Tick()
        {
            var deltaTime = _clock.DeltaTime;
            for (var i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                var before = character.State;
                var refusal = character.Advance(deltaTime, this);
                if (character.State != before)
                {
                    UpdateReservations(character);
                    _stateChanged.Publish(new CharacterStateChanged(character.Id, character.State));
                }
                if (refusal != StepRefusal.None)
                    _refused.Publish(new StepRefused(character.Id, refusal));
            }
        }

        StepVerdict IStepGate.TryEnter(ICharacter character, Cell next)
        {
            if (character.State == CharacterState.Walking)
            {
                var rule = StepRules.Check(_grid.At(next), character.Gender, character.Size);
                if (rule != StepVerdict.Enter)
                    return rule;
            }
            return _claims.Occupy(next, character.Id) ? StepVerdict.Enter : StepVerdict.Wait;
        }

        void IStepGate.Leave(ICharacter character, Cell cell) => _claims.Vacate(cell, character.Id);

        /// <summary>
        /// An Idle character needs no reservation; one walking back keeps only the cell it returns to, the end of its way
        /// back, which it reserved when it set out.
        /// </summary>
        private void UpdateReservations(Character character)
        {
            _claims.UnreserveAll(character.Id);
            if (character.State == CharacterState.Returning)
                Reserve(character.Route.End, character.Id);
        }

        private void Reserve(Cell cell, CharacterId id)
        {
            if (_grid.At(cell).Type != TileType.Door)
                _claims.Reserve(cell, id);
        }
    }
}

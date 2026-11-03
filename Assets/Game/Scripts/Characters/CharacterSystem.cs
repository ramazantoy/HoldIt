using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Paths;
using HoldIt.Restrooms;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Characters
{
    /// <summary>
    /// Drives every character (the presenter the GDD calls CharacterController): adds them, hands them their paths, and
    /// advances them each frame with game time, so they stop while the game is paused. It is also their step gate: the
    /// tile rules (<see cref="StepRules"/>) for walkers, then the cell claims (<see cref="CellClaims"/>) for everyone. At
    /// its restroom's door a character takes a free stall and walks in, or queues on the door until one frees; once in
    /// the stall it leaves the board.
    /// </summary>
    internal sealed class CharacterSystem : ICharacters, ITickable, IStepGate
    {
        /// <summary>How far into its way in a visitor is when it steps off the door's cell.</summary>
        private const float HalfTile = 0.5f;

        private readonly List<Character> _characters = new List<Character>();
        private readonly Dictionary<CharacterId, Character> _byId = new Dictionary<CharacterId, Character>();
        private readonly List<Character> _inside = new List<Character>();
        private readonly CellClaims _claims = new CellClaims();
        private readonly ITileGrid _grid;
        private readonly CharacterConfig _config;
        private readonly IRestrooms _restrooms;
        private readonly IGameClock _clock;
        private readonly IPublisher<CharacterSpawned> _spawned;
        private readonly IPublisher<CharacterStateChanged> _stateChanged;
        private readonly IPublisher<StepRefused> _refused;
        private readonly IPublisher<CharacterRemoved> _removed;
        private int _lastId;

        public CharacterSystem(ITileGrid grid, CharacterConfig config, IRestrooms restrooms, IGameClock clock,
            IPublisher<CharacterSpawned> spawned, IPublisher<CharacterStateChanged> stateChanged,
            IPublisher<StepRefused> refused, IPublisher<CharacterRemoved> removed)
        {
            _grid = grid;
            _config = config;
            _restrooms = restrooms;
            _clock = clock;
            _spawned = spawned;
            _stateChanged = stateChanged;
            _refused = refused;
            _removed = removed;
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
            if (IsDoor(cell))
                throw new ArgumentException($"A character cannot start on {cell}: it is a door.", nameof(cell));
            if (!_claims.IsFree(cell))
                throw new ArgumentException($"A character cannot stand on {cell}: someone holds or reserved it.", nameof(cell));

            var id = new CharacterId(++_lastId);
            var character = new Character(id, gender, size, cell, _config.WalkSpeed(size), _config.BlockedWaitSeconds,
                _config.ReturnSpeedFactor, _config.StallWalkSpeed);
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
            _grid.InBounds(cell) && (IsDoor(cell) || _claims.IsFreeFor(cell, id));

        public bool IsFree(Cell cell) => _grid.InBounds(cell) && _claims.IsFree(cell);

        public void Tick()
        {
            var deltaTime = _clock.DeltaTime;
            for (var i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                var before = character.State;
                var refusal = character.Advance(deltaTime, this);
                AtTheDoor(character);
                if (character.State != before)
                {
                    UpdateReservations(character);
                    _stateChanged.Publish(new CharacterStateChanged(character.Id, character.State));
                }
                if (refusal != StepRefusal.None)
                    _refused.Publish(new StepRefused(character.Id, refusal));
                if (character.IsInside)
                    _inside.Add(character);
            }

            try
            {
                foreach (var character in _inside)
                    Remove(character);
            }
            finally
            {
                _inside.Clear();
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
        /// A character that stopped on its door goes into a free stall or queues there, before anyone hears it stopped; a
        /// queued one tries again every frame. One walking in lets go of the door once it has stepped off it.
        /// </summary>
        private void AtTheDoor(Character character)
        {
            switch (character.State)
            {
                case CharacterState.Idle when IsDoorOf(character.Cell, character.Gender):
                    if (!TryEnterStall(character))
                        character.Queue();
                    break;
                case CharacterState.Queued:
                    TryEnterStall(character);
                    break;
                case CharacterState.InStall when character.VisitDistance >= HalfTile:
                    _claims.Vacate(character.Cell, character.Id);
                    break;
            }
        }

        private bool TryEnterStall(Character character)
        {
            if (!_restrooms.TryTake(character.Gender, out var visit))
                return false;
            character.EnterStall(visit);
            return true;
        }

        /// <summary>
        /// The visitor is in: it leaves the board, letting go of everything it held, and then its stall shuts, so whoever
        /// hears of either sees both done. Once only: a character already gone is skipped.
        /// </summary>
        private void Remove(Character character)
        {
            if (!_byId.Remove(character.Id))
                return;
            _characters.Remove(character);
            _claims.Vacate(character.Cell, character.Id);
            _claims.UnreserveAll(character.Id);
            _restrooms.Shut(character.Visit.Gender, character.Visit.Stall);
            _removed.Publish(new CharacterRemoved(character.Id));
        }

        /// <summary>
        /// Only a character walking back keeps a reservation: the cell it returns to, the end of its way back, which it
        /// reserved when it set out. Idle, queued and visiting characters need none.
        /// </summary>
        private void UpdateReservations(Character character)
        {
            _claims.UnreserveAll(character.Id);
            if (character.State == CharacterState.Returning)
                Reserve(character.Route.End, character.Id);
        }

        private void Reserve(Cell cell, CharacterId id)
        {
            if (!IsDoor(cell))
                _claims.Reserve(cell, id);
        }

        private bool IsDoor(Cell cell) => _grid.At(cell).Type == TileType.Door;

        private bool IsDoorOf(Cell cell, Gender gender)
        {
            var tile = _grid.At(cell);
            return tile.Type == TileType.Door && tile.Gender == gender;
        }
    }
}

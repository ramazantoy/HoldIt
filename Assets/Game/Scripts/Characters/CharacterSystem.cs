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
    /// tile rules (<see cref="StepRules"/>), then the cell claims (<see cref="CellClaims"/>). At
    /// its restroom's door a character takes a free stall and walks in, or queues on the door until one frees; once in
    /// the stall it leaves the board; one that stops right in front of its door with a stall free steps onto it itself.
    /// Everyone else loses patience by the frame; one that runs out has an accident and
    /// leaves the board where it is.
    /// </summary>
    internal sealed class CharacterSystem : ICharacters, ITickable, IStepGate
    {
        /// <summary>How far into its way in a visitor is when it steps off the door's cell.</summary>
        private const float HalfTile = 0.5f;

        private static readonly Direction[] _directions =
            { Direction.North, Direction.East, Direction.South, Direction.West };

        private readonly List<Character> _characters = new List<Character>();
        private readonly Dictionary<CharacterId, Character> _byId = new Dictionary<CharacterId, Character>();
        private readonly List<Character> _inside = new List<Character>();
        private readonly List<Character> _accidents = new List<Character>();
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
                _config.StallWalkSpeed);
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

            if (!IsDoor(path.End))
                _claims.Reserve(path.End, id);
            _stateChanged.Publish(new CharacterStateChanged(id, character.State));
            return true;
        }

        public bool CanEndOn(CharacterId id, Cell cell) =>
            _grid.InBounds(cell) && (IsDoor(cell) || _claims.IsFreeFor(cell, id));

        public bool IsFree(Cell cell) => _grid.InBounds(cell) && _claims.IsFree(cell);

        public void Tick()
        {
            var deltaTime = _clock.DeltaTime;
            var patienceLost = deltaTime / _config.PatienceSeconds;
            for (var i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                var before = character.State;
                var refusal = character.Advance(deltaTime, this);
                AtTheDoor(character);
                character.LosePatience(patienceLost);
                if (character.State != before)
                {
                    // Only a walker keeps a reservation, and nobody starts walking here.
                    _claims.UnreserveAll(character.Id);
                    _stateChanged.Publish(new CharacterStateChanged(character.Id, character.State));
                }
                if (refusal != StepRefusal.None)
                    _refused.Publish(new StepRefused(character.Id, refusal));
                if (character.IsInside)
                    _inside.Add(character);
                else if (character.IsOutOfPatience)
                    _accidents.Add(character);
            }

            try
            {
                foreach (var character in _inside)
                    Remove(character, RemovalReason.EnteredStall);
                foreach (var character in _accidents)
                    Remove(character, RemovalReason.Accident);
            }
            finally
            {
                _inside.Clear();
                _accidents.Clear();
            }
        }

        StepVerdict IStepGate.TryEnter(ICharacter character, Cell next)
        {
            var rule = StepRules.Check(_grid.At(next), character.Gender, character.Size);
            if (rule != StepVerdict.Enter)
                return rule;
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
                case CharacterState.Idle:
                    if (!IsDoorOf(character.Cell, character.Gender))
                        StepOntoDoorInFront(character);
                    else if (!TryEnterStall(character))
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

        /// <summary>
        /// A character standing right in front of its own restroom's door, with a stall free, steps onto the door by itself:
        /// the player need not draw that last tile. It does nothing when no stall is free (queueing on the door is the
        /// player's choice) or someone holds the door.
        /// </summary>
        private void StepOntoDoorInFront(Character character)
        {
            if (!HasFreeStall(character.Gender))
                return;

            foreach (var direction in _directions)
            {
                var door = character.Cell.Step(direction);
                if (!_grid.InBounds(door) || !IsDoorOf(door, character.Gender) || !_claims.IsFree(door))
                    continue;
                if (StepRules.Check(_grid.At(door), character.Gender, character.Size) != StepVerdict.Enter)
                    continue;

                // Its last path's end was reserved for it; it is on its way off that cell now.
                _claims.UnreserveAll(character.Id);
                if (character.TryWalk(new CellPath(new[] { character.Cell, door })))
                    return;
            }
        }

        private bool HasFreeStall(Gender gender)
        {
            for (var i = 0; i < _restrooms.StallCount; i++)
            {
                if (_restrooms.StateOf(gender, i) == StallState.Free)
                    return true;
            }
            return false;
        }

        private bool TryEnterStall(Character character)
        {
            if (!_restrooms.TryTake(character.Gender, out var visit))
                return false;
            character.EnterStall(visit);
            return true;
        }

        /// <summary>
        /// The character leaves the board, letting go of everything it held, the cell it was stepping into as well; a visitor
        /// that is in then shuts its stall, so whoever hears of either sees both done. Once only: a character already gone is
        /// skipped.
        /// </summary>
        private void Remove(Character character, RemovalReason reason)
        {
            if (!_byId.Remove(character.Id))
                return;
            _characters.Remove(character);
            _claims.Vacate(character.Cell, character.Id);
            _claims.Vacate(character.Next, character.Id);
            _claims.UnreserveAll(character.Id);
            if (reason == RemovalReason.EnteredStall)
                _restrooms.Shut(character.Visit.Gender, character.Visit.Stall);
            _removed.Publish(new CharacterRemoved(character.Id, reason));
        }

        private bool IsDoor(Cell cell) => _grid.At(cell).Type == TileType.Door;

        private bool IsDoorOf(Cell cell, Gender gender)
        {
            var tile = _grid.At(cell);
            return tile.Type == TileType.Door && tile.Gender == gender;
        }
    }
}

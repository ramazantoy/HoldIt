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
    /// advances them each frame with game time, so they stop while the game is paused.
    /// </summary>
    internal sealed class CharacterSystem : ICharacters, ITickable
    {
        private readonly List<Character> _characters = new List<Character>();
        private readonly Dictionary<CharacterId, Character> _byId = new Dictionary<CharacterId, Character>();
        private readonly ITileGrid _grid;
        private readonly CharacterConfig _config;
        private readonly IGameClock _clock;
        private readonly IPublisher<CharacterSpawned> _spawned;
        private readonly IPublisher<CharacterStateChanged> _stateChanged;
        private int _lastId;

        public CharacterSystem(ITileGrid grid, CharacterConfig config, IGameClock clock,
            IPublisher<CharacterSpawned> spawned, IPublisher<CharacterStateChanged> stateChanged)
        {
            _grid = grid;
            _config = config;
            _clock = clock;
            _spawned = spawned;
            _stateChanged = stateChanged;
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

            var id = new CharacterId(++_lastId);
            var character = new Character(id, gender, size, cell, _config.WalkSpeed(size));
            _characters.Add(character);
            _byId.Add(id, character);
            _spawned.Publish(new CharacterSpawned(id, gender, size, cell));
            return id;
        }

        public bool TryWalk(CharacterId id, CellPath path)
        {
            if (!_byId.TryGetValue(id, out var character) || !character.TryWalk(path))
                return false;
            _stateChanged.Publish(new CharacterStateChanged(id, character.State));
            return true;
        }

        public void Tick()
        {
            var deltaTime = _clock.DeltaTime;
            for (var i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                var before = character.State;
                character.Advance(deltaTime);
                if (character.State != before)
                    _stateChanged.Publish(new CharacterStateChanged(character.Id, character.State));
            }
        }
    }
}

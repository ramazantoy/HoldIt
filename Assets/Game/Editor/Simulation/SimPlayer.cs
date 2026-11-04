using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;
using HoldIt.Restrooms;

namespace HoldIt.Editor.Simulation
{
    /// <summary>
    /// A player who knows the rules but has one finger and limited attention: at most one path every
    /// <see cref="DrawInterval"/> seconds. It picks the idle character closest to an accident and draws the shortest way to
    /// its own door, never through the other door and, for a Fat one, never through a narrow passage. It never draws
    /// through characters standing still (waiting is better than a jam), and sends no more to a restroom than its free stalls
    /// plus one who can wait at the door; the rest wait in place.
    /// </summary>
    public sealed class SimPlayer
    {
        private readonly ITileGrid _grid;
        private readonly ICharacters _characters;
        private readonly IRestrooms _restrooms;
        private readonly HashSet<Cell> _standing = new HashSet<Cell>();
        private readonly List<ICharacter> _idle = new List<ICharacter>();
        private float _cooldown;

        public SimPlayer(ITileGrid grid, ICharacters characters, IRestrooms restrooms, float drawInterval)
        {
            if (drawInterval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(drawInterval), drawInterval, "A player needs time to draw.");
            _grid = grid;
            _characters = characters;
            _restrooms = restrooms;
            DrawInterval = drawInterval;
        }

        /// <summary>Seconds between two paths at the most.</summary>
        public float DrawInterval { get; }

        /// <summary>How many paths it has drawn.</summary>
        public int Drawn { get; private set; }

        public void Tick(float deltaTime)
        {
            _cooldown -= deltaTime;
            if (_cooldown > 0f)
                return;
            if (TryDrawOne())
            {
                _cooldown = DrawInterval;
                Drawn++;
            }
        }

        private bool TryDrawOne()
        {
            _idle.Clear();
            _standing.Clear();
            foreach (var character in _characters.All)
            {
                if (character.State == CharacterState.Idle)
                    _idle.Add(character);
                if (character.State == CharacterState.Idle || character.State == CharacterState.Queued)
                    _standing.Add(character.Cell);
            }
            // Most urgent first; the same order every time for the same board.
            _idle.Sort((a, b) => a.Patience != b.Patience ? b.Patience.CompareTo(a.Patience) : a.Id.Value.CompareTo(b.Id.Value));

            foreach (var character in _idle)
            {
                if (!HasRoom(character.Gender))
                    continue;
                var door = _grid.DoorsOf(character.Gender)[0];
                var path = PathFinder.Shortest(_grid, character.Cell, door, cell => CanCross(character, cell));
                if (path != null && _characters.TryWalk(character.Id, path))
                    return true;
            }
            return false;
        }

        // Free stalls, plus one to wait on the door; everyone already on the way there or waiting counts.
        private bool HasRoom(Gender gender)
        {
            var free = 0;
            for (var stall = 0; stall < _restrooms.StallCount; stall++)
            {
                if (_restrooms.StateOf(gender, stall) == StallState.Free)
                    free++;
            }

            var door = _grid.DoorsOf(gender)[0];
            var coming = 0;
            foreach (var other in _characters.All)
            {
                if (other.Gender != gender)
                    continue;
                if (other.State == CharacterState.Queued ||
                    (other.State == CharacterState.Walking && other.Route != null && other.Route.End == door))
                    coming++;
            }
            return coming < free + 1;
        }

        private bool CanCross(ICharacter character, Cell cell)
        {
            if (!PathRules.IsDrawable(_grid, cell) || _standing.Contains(cell))
                return false;
            var tile = _grid.At(cell);
            if (tile.Type == TileType.Door && tile.Gender != character.Gender)
                return false;
            return tile.Type != TileType.Narrow || character.Size != BodySize.Fat;
        }
    }
}

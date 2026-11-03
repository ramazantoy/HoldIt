using System;
using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Levels;
using VContainer.Unity;

namespace HoldIt.Spawning
{
    /// <summary>
    /// Brings the level's crowd in, one by one at a free entrance, and walks each to a free lobby cell
    /// (<see cref="ArrivalPlanner"/>). The first comes after a short delay, the rest at the level's gaps
    /// (<see cref="SpawnSchedule"/>). When nobody can come in (the board is at the level's limit, or no entrance, lobby cell
    /// or way is free) it tries again a little later; the try does not count. It stops once the whole crowd is in or the
    /// level has ended, and runs on game time, so it waits while the game is paused.
    /// </summary>
    internal sealed class Spawner : ITickable
    {
        private readonly ICharacters _characters;
        private readonly ITileGrid _grid;
        private readonly IGameClock _clock;
        private readonly LevelDefinition _level;
        private readonly SpawnConfig _config;
        private readonly ILevelRun _run;
        private readonly Random _random;
        private float _wait;

        public Spawner(ICharacters characters, ITileGrid grid, IGameClock clock, LevelDefinition level, SpawnConfig config,
            ILevelRun run, Random random)
        {
            _characters = characters;
            _grid = grid;
            _clock = clock;
            _level = level;
            _config = config;
            _run = run;
            _random = random;
            _wait = config.FirstDelay;
        }

        /// <summary>How many have come in so far.</summary>
        public int Arrived { get; private set; }

        public bool IsDone => Arrived >= _level.Crowd;

        public void Tick()
        {
            if (IsDone || _run.Outcome != LevelOutcome.Playing)
                return;

            _wait -= _clock.DeltaTime;
            if (_wait > 0f)
                return;

            if (!TryArrive())
            {
                _wait = _config.RetryDelay;
                return;
            }

            Arrived++;
            // Added, not set: the time past the due moment counts toward the next gap.
            if (!IsDone)
                _wait += SpawnSchedule.GapAfter(Arrived, _level.Crowd, _level.SpawnStart, _level.SpawnEnd);
        }

        private bool TryArrive()
        {
            if (_characters.All.Count >= _level.MaxCharacters)
                return false;

            var size = SpawnRoll.Size(_random, _level.Sizes);
            if (!ArrivalPlanner.TryPlan(_grid, _characters.IsFree, size, _random, out var path))
                return false;

            var id = _characters.Spawn(SpawnRoll.Gender(_random, _config.MaleShare), size, path.Start);
            if (!_characters.TryWalk(id, path))
                Log.Warning($"Arriving {id} did not take the path {path}; it waits at the entrance.");
            return true;
        }
    }
}

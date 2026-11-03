using System;
using AF.Core;
using HoldIt.Board;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// Keeps both restrooms' stalls and their time with game time, so stalls stay busy while the game is paused. Lays out
    /// the stalls from the map once (<see cref="RestroomLayout"/>).
    /// </summary>
    internal sealed class RestroomSystem : IRestrooms, ITickable
    {
        private static readonly Gender[] _genders = { Gender.Male, Gender.Female };

        private readonly Stall[][] _stalls = new Stall[_genders.Length][];
        private readonly RestroomLayout[] _layouts = new RestroomLayout[_genders.Length];
        private readonly RestroomConfig _config;
        private readonly IGameClock _clock;
        private readonly IPublisher<StallStateChanged> _changed;

        /// <param name="stallCount">How many stalls each restroom has; the map's.</param>
        public RestroomSystem(ITileGrid grid, RestroomConfig config, int stallCount, IGameClock clock,
            IPublisher<StallStateChanged> changed)
        {
            _config = config;
            _clock = clock;
            _changed = changed;
            StallCount = stallCount;

            foreach (var gender in _genders)
            {
                _layouts[(int)gender] = RestroomLayout.Build(grid, gender, stallCount, config.Geometry);
                var stalls = new Stall[stallCount];
                for (var i = 0; i < stallCount; i++)
                    stalls[i] = new Stall();
                _stalls[(int)gender] = stalls;
            }
        }

        public int StallCount { get; }

        public StallState StateOf(Gender gender, int stall) => StallAt(gender, stall).State;

        public RestroomLayout LayoutOf(Gender gender) => _layouts[(int)gender];

        public bool TryTake(Gender gender, out StallVisit visit)
        {
            var stalls = _stalls[(int)gender];
            for (var i = 0; i < stalls.Length; i++)
            {
                if (stalls[i].State != StallState.Free)
                    continue;

                stalls[i].Take();
                visit = new StallVisit(gender, i, _layouts[(int)gender].Stalls[i].Entry);
                _changed.Publish(new StallStateChanged(gender, i, StallState.Taken));
                return true;
            }

            visit = null;
            return false;
        }

        public void Shut(Gender gender, int stall)
        {
            StallAt(gender, stall).Shut();
            _changed.Publish(new StallStateChanged(gender, stall, StallState.Shut));
        }

        public void Tick()
        {
            var deltaTime = _clock.DeltaTime;
            foreach (var gender in _genders)
            {
                var stalls = _stalls[(int)gender];
                for (var i = 0; i < stalls.Length; i++)
                {
                    if (stalls[i].Advance(deltaTime, _config.StallSeconds))
                        _changed.Publish(new StallStateChanged(gender, i, StallState.Free));
                }
            }
        }

        private Stall StallAt(Gender gender, int stall)
        {
            var stalls = _stalls[(int)gender];
            if (stall < 0 || stall >= stalls.Length)
                throw new ArgumentOutOfRangeException(nameof(stall),
                    $"A restroom has stalls 0 to {stalls.Length - 1}, not {stall}.");
            return stalls[stall];
        }
    }
}

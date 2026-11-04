using System;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Spawning;
using MessagePipe;
using VContainer;

namespace HoldIt.Editor.Simulation
{
    /// <summary>
    /// Plays a level without a scene, as fast as the CPU allows: the game's own rules (<see cref="RestroomSystem"/>,
    /// <see cref="CharacterSystem"/>, <see cref="Spawner"/>, <see cref="LevelRun"/>) on a fixed time step, ticked in the
    /// gameplay scene's order, with a <see cref="SimPlayer"/> drawing the paths. Only the level's numbers are read from
    /// <see cref="LevelDefinition"/>; the map comes as a grid, so a map can be tried with any level's numbers.
    /// </summary>
    public static class LevelSim
    {
        /// <param name="drawInterval">The player's seconds between paths; 0 or less plays with nobody drawing.</param>
        public static SimResult Run(ITileGrid grid, int stallCount, LevelDefinition level, SimRules rules,
            float drawInterval, int seed, float timeStep, float timeLimit)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));
            if (timeStep <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "The time step must be positive.");

            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<StallStateChanged>(messagePipe);
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<StepRefused>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
            builder.RegisterMessageBroker<ServedChanged>(messagePipe);
            builder.RegisterMessageBroker<LivesChanged>(messagePipe);
            builder.RegisterMessageBroker<LevelEnded>(messagePipe);

            using var container = builder.Build();
            var clock = new SimClock(timeStep);
            var restrooms = new RestroomSystem(grid, rules.Restrooms, stallCount, clock,
                container.Resolve<IPublisher<StallStateChanged>>());
            var characters = new CharacterSystem(grid, rules.Characters, restrooms, clock,
                container.Resolve<IPublisher<CharacterSpawned>>(), container.Resolve<IPublisher<CharacterStateChanged>>(),
                container.Resolve<IPublisher<StepRefused>>(), container.Resolve<IPublisher<CharacterRemoved>>());
            var run = new LevelRun(level, rules.Run, characters, container.Resolve<ISubscriber<CharacterSpawned>>(),
                container.Resolve<ISubscriber<CharacterStateChanged>>(), container.Resolve<ISubscriber<CharacterRemoved>>(),
                container.Resolve<IPublisher<ServedChanged>>(), container.Resolve<IPublisher<LivesChanged>>(),
                container.Resolve<IPublisher<LevelEnded>>());
            var spawner = new Spawner(characters, grid, clock, level, rules.Spawning, run, new Random(seed));
            var player = drawInterval > 0f ? new SimPlayer(grid, characters, restrooms, drawInterval) : null;

            var watch = new Watch(clock);
            var bag = DisposableBag.CreateBuilder();
            container.Resolve<ISubscriber<StepRefused>>().Subscribe(watch.Refused).AddTo(bag);
            container.Resolve<ISubscriber<CharacterSpawned>>().Subscribe(_ => watch.Arrived()).AddTo(bag);
            container.Resolve<ISubscriber<CharacterRemoved>>().Subscribe(watch.Removed).AddTo(bag);
            container.Resolve<ISubscriber<CharacterStateChanged>>()
                .Subscribe(message => watch.StateChanged(message, characters)).AddTo(bag);
            using var subscriptions = bag.Build();

            run.Initialize();
            try
            {
                while (run.Outcome == LevelOutcome.Playing && clock.Time < timeLimit)
                {
                    clock.Advance();
                    // The player's input comes before the systems, as the pointer's does in the scene.
                    player?.Tick(timeStep);
                    restrooms.Tick();
                    characters.Tick();
                    spawner.Tick();
                    watch.CountBoard(characters.All.Count);
                }
            }
            finally
            {
                run.Dispose();
            }

            var delay = watch.LastArrival - Scheduled(level, rules.Spawning);
            return new SimResult(run.Outcome, run.Stars, run.Served, watch.Accidents, clock.Time, watch.MeanPatienceServed,
                watch.PeakBoard, watch.RuleRefusals, watch.Blocked, Math.Max(0f, delay));
        }

        /// <summary>When the last arrival is due if nothing ever stops the spawner.</summary>
        private static float Scheduled(LevelDefinition level, SpawnConfig config)
        {
            var due = config.FirstDelay;
            for (var arrived = 1; arrived < level.Crowd; arrived++)
                due += SpawnSchedule.GapAfter(arrived, level.Crowd, level.SpawnStart, level.SpawnEnd);
            return due;
        }

        private sealed class Watch
        {
            private readonly SimClock _clock;

            public Watch(SimClock clock) => _clock = clock;

            public int Accidents { get; private set; }
            public int RuleRefusals { get; private set; }
            public int Blocked { get; private set; }
            public int PeakBoard { get; private set; }
            public float LastArrival { get; private set; }
            public float MeanPatienceServed => _served > 0 ? _patienceServed / _served : 0f;

            private float _patienceServed;
            private int _served;

            public void StateChanged(CharacterStateChanged message, ICharacters characters)
            {
                if (message.State != CharacterState.InStall || !characters.TryGet(message.Id, out var character))
                    return;
                _patienceServed += character.Patience;
                _served++;
            }

            public void CountBoard(int count)
            {
                if (count > PeakBoard)
                    PeakBoard = count;
            }

            public void Arrived() => LastArrival = _clock.Time;

            public void Removed(CharacterRemoved message)
            {
                if (message.Reason == RemovalReason.Accident)
                    Accidents++;
            }

            public void Refused(StepRefused message)
            {
                if (message.Reason == StepRefusal.Blocked)
                    Blocked++;
                else if (message.Reason != StepRefusal.None)
                    RuleRefusals++;
            }
        }
    }
}

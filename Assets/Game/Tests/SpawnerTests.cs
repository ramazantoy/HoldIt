using System;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Levels;
using HoldIt.Spawning;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;
using Random = System.Random;

namespace HoldIt.Tests
{
    /// <summary>Arrivals on Map 1 (<see cref="TestMaps.Lobby"/>), with real characters and a fake clock.</summary>
    public class SpawnerTests
    {
        private IObjectResolver _container;
        private ITileGrid _grid;
        private CharacterConfig _characterConfig;
        private SpawnConfig _config;
        private FakeClock _clock;
        private FakeLevelRun _run;
        private CharacterSystem _characters;
        private LevelDefinition _level;
        private Spawner _spawner;

        [SetUp]
        public void CreateSpawner()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<StepRefused>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
            _container = builder.Build();

            _grid = MapParser.Parse(TestMaps.Lobby);
            _characterConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            _config = ScriptableObject.CreateInstance<SpawnConfig>();
            _clock = new FakeClock();
            _run = new FakeLevelRun();
            _characters = new CharacterSystem(_grid, _characterConfig, new FakeRestrooms(), _clock,
                _container.Resolve<IPublisher<CharacterSpawned>>(), _container.Resolve<IPublisher<CharacterStateChanged>>(),
                _container.Resolve<IPublisher<StepRefused>>(), _container.Resolve<IPublisher<CharacterRemoved>>());
            Use(TestLevels.Level(crowd: 4, maxCharacters: 8, spawnStart: 3f, spawnEnd: 1f));
        }

        [TearDown]
        public void DisposeSpawner()
        {
            _container.Dispose();
            UnityEngine.Object.DestroyImmediate(_level);
            UnityEngine.Object.DestroyImmediate(_characterConfig);
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void BeforeTheFirstDelay_NobodyComes()
        {
            Tick(_config.FirstDelay - 0.01f);

            Assert.AreEqual(0, _spawner.Arrived);
            Assert.AreEqual(0, _characters.All.Count);
        }

        [Test]
        public void AtTheFirstDelay_OneComesInAndWalksToTheLobby()
        {
            Tick(_config.FirstDelay);

            Assert.AreEqual(1, _spawner.Arrived);
            var character = _characters.All[0];
            Assert.AreEqual(TileType.Entrance, _grid.At(character.Cell).Type);
            Assert.AreEqual(CharacterState.Walking, character.State);
            Assert.AreEqual(TileType.Lobby, _grid.At(character.Route.End).Type);
        }

        [Test]
        public void TheNextComesAfterTheLevelsGap()
        {
            Tick(_config.FirstDelay);
            Walk();

            Tick(_level.SpawnStart - 0.01f);
            Assert.AreEqual(1, _spawner.Arrived);
            Tick(0.02f);
            Assert.AreEqual(2, _spawner.Arrived);
        }

        [Test]
        public void TheTimePastTheDueMoment_CountsTowardTheNextGap()
        {
            Tick(_config.FirstDelay + 1f);
            Walk();

            Tick(_level.SpawnStart - 1f + 0.01f);

            Assert.AreEqual(2, _spawner.Arrived);
        }

        [Test]
        public void TheWholeCrowd_ComesAndThenNobodyElse()
        {
            Run(20f);

            Assert.AreEqual(_level.Crowd, _spawner.Arrived);
            Assert.IsTrue(_spawner.IsDone);
            Assert.AreEqual(_level.Crowd, _characters.All.Count);
        }

        [Test]
        public void AtTheBoardsLimit_NobodyComesUntilThereIsRoom()
        {
            Use(TestLevels.Level(crowd: 4, maxCharacters: 1, spawnStart: 1f, spawnEnd: 1f));

            for (var i = 0; i < 100; i++)
                Tick(0.1f);

            Assert.AreEqual(1, _spawner.Arrived);
            Assert.AreEqual(1, _characters.All.Count);
        }

        [Test]
        public void AtTheBoardsLimit_ComesAgainOnceSomeoneLeaves()
        {
            Use(TestLevels.Level(crowd: 4, maxCharacters: 1, spawnStart: 1f, spawnEnd: 1f));
            Tick(_config.FirstDelay);
            Tick(_level.SpawnStart);
            Assert.AreEqual(1, _spawner.Arrived, "The board is full.");

            _clock.DeltaTime = _characterConfig.PatienceSeconds;
            _characters.Tick();
            Assert.AreEqual(0, _characters.All.Count, "The first one had an accident.");
            Tick(_config.RetryDelay);

            Assert.AreEqual(2, _spawner.Arrived);
        }

        [Test]
        public void EveryEntranceTaken_TriesAgainAfterTheRetryDelay()
        {
            var blockers = new[] { _characters.Spawn(Gender.Male, BodySize.Normal, new Cell(4, 13)),
                _characters.Spawn(Gender.Male, BodySize.Normal, new Cell(5, 13)) };
            Use(TestLevels.Level(crowd: 4, maxCharacters: 8));

            Tick(_config.FirstDelay);
            Assert.AreEqual(0, _spawner.Arrived);

            Assert.IsTrue(_characters.TryWalk(blockers[1], new Paths.CellPath(new[] { new Cell(5, 13), new Cell(5, 12), new Cell(5, 11) })));
            Walk();
            Tick(_config.RetryDelay - 0.01f);
            Assert.AreEqual(0, _spawner.Arrived);
            Tick(0.02f);
            Assert.AreEqual(1, _spawner.Arrived);
        }

        [Test]
        public void OnceTheLevelHasEnded_NobodyComes()
        {
            _run.Outcome = LevelOutcome.Lost;

            Tick(_config.FirstDelay + 10f);

            Assert.AreEqual(0, _spawner.Arrived);
        }

        [Test]
        public void WhilePaused_NobodyComes()
        {
            for (var i = 0; i < 10; i++)
                Tick(0f);

            Assert.AreEqual(0, _spawner.Arrived);
        }

        [Test]
        public void TheLevelsWeights_PickWhoComes()
        {
            Use(TestLevels.Level(crowd: 3, maxCharacters: 8, spawnStart: 2f, spawnEnd: 2f,
                sizes: new SizeWeights(0f, 0f, 1f)));

            Run(20f);

            Assert.AreEqual(3, _spawner.Arrived);
            foreach (var character in _characters.All)
                Assert.AreEqual(BodySize.Fat, character.Size);
        }

        private void Use(LevelDefinition level)
        {
            if (_level != null)
                UnityEngine.Object.DestroyImmediate(_level);
            _level = level;
            _spawner = new Spawner(_characters, _grid, _clock, _level, _config, _run, new Random(11));
        }

        private void Tick(float deltaTime)
        {
            _clock.DeltaTime = deltaTime;
            _spawner.Tick();
        }

        /// <summary>The spawner and the characters together, a tenth of a second at a time.</summary>
        private void Run(float seconds)
        {
            for (var time = 0f; time < seconds; time += 0.1f)
            {
                Tick(0.1f);
                _characters.Tick();
            }
        }

        /// <summary>Lets everyone walk to where they are going, the clock's time only passing for them.</summary>
        private void Walk()
        {
            _clock.DeltaTime = 0.1f;
            for (var i = 0; i < 100; i++)
                _characters.Tick();
        }
    }
}

using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    /// <summary>The characters on Map 1 (<see cref="TestMaps.Lobby"/>), with real MessagePipe brokers and a fake clock.</summary>
    public class CharacterSystemTests
    {
        private static readonly Cell _lobby = new Cell(0, 10);

        private IObjectResolver _container;
        private CharacterConfig _config;
        private FakeClock _clock;
        private CharacterSystem _system;
        private List<CharacterSpawned> _spawned;
        private List<CharacterStateChanged> _changed;
        private List<CharacterState> _stateWhenAnnounced;
        private IDisposable _subscriptions;

        [SetUp]
        public void CreateSystem()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            _container = builder.Build();

            _config = ScriptableObject.CreateInstance<CharacterConfig>();
            _clock = new FakeClock();
            _system = new CharacterSystem(MapParser.Parse(TestMaps.Lobby), _config, _clock,
                _container.Resolve<IPublisher<CharacterSpawned>>(), _container.Resolve<IPublisher<CharacterStateChanged>>());

            _spawned = new List<CharacterSpawned>();
            _changed = new List<CharacterStateChanged>();
            _stateWhenAnnounced = new List<CharacterState>();
            var bag = DisposableBag.CreateBuilder();
            _container.Resolve<ISubscriber<CharacterSpawned>>().Subscribe(_spawned.Add).AddTo(bag);
            _container.Resolve<ISubscriber<CharacterStateChanged>>().Subscribe(message =>
            {
                _changed.Add(message);
                _system.TryGet(message.Id, out var character);
                _stateWhenAnnounced.Add(character.State);
            }).AddTo(bag);
            _subscriptions = bag.Build();
        }

        [TearDown]
        public void DisposeSystem()
        {
            _subscriptions.Dispose();
            _container.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void Spawn_AddsAnIdleCharacterAndAnnouncesIt()
        {
            var id = _system.Spawn(Gender.Female, BodySize.Thin, _lobby);

            Assert.IsTrue(_system.TryGet(id, out var character));
            Assert.AreEqual(CharacterState.Idle, character.State);
            Assert.AreEqual(_lobby, character.Cell);
            Assert.AreEqual(Gender.Female, character.Gender);
            Assert.AreEqual(BodySize.Thin, character.Size);
            CollectionAssert.AreEqual(new[] { character }, _system.All);

            Assert.AreEqual(1, _spawned.Count);
            Assert.AreEqual(id, _spawned[0].Id);
            Assert.AreEqual(Gender.Female, _spawned[0].Gender);
            Assert.AreEqual(BodySize.Thin, _spawned[0].Size);
            Assert.AreEqual(_lobby, _spawned[0].Cell);
        }

        [Test]
        public void Spawn_GivesEveryCharacterItsOwnId()
        {
            var first = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            var second = _system.Spawn(Gender.Male, BodySize.Normal, new Cell(1, 10));

            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(default(CharacterId), first);
        }

        [Test]
        public void Spawn_WalksAtItsSizesSpeed()
        {
            foreach (BodySize size in Enum.GetValues(typeof(BodySize)))
            {
                _system.TryGet(_system.Spawn(Gender.Male, size, _lobby), out var character);
                Assert.AreEqual(_config.WalkSpeed(size), character.Speed, size.ToString());
            }
        }

        [Test]
        public void Spawn_OnAWall_Throws()
        {
            Assert.Throws<ArgumentException>(() => _system.Spawn(Gender.Male, BodySize.Normal, new Cell(1, 5)));
            Assert.AreEqual(0, _spawned.Count);
        }

        [Test]
        public void TryWalk_AnnouncesWalkingOnceTheCharacterWalks()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);

            Assert.IsTrue(_system.TryWalk(id, North(3)));

            Assert.AreEqual(1, _changed.Count);
            Assert.AreEqual(id, _changed[0].Id);
            Assert.AreEqual(CharacterState.Walking, _changed[0].State);
            Assert.AreEqual(CharacterState.Walking, _stateWhenAnnounced[0]);
        }

        [Test]
        public void TryWalk_ARefusedPath_AnnouncesNothing()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);

            Assert.IsFalse(_system.TryWalk(id, new CellPath(new[] { new Cell(1, 10), new Cell(1, 9) })));
            Assert.IsFalse(_system.TryWalk(new CharacterId(99), North(2)));
            Assert.AreEqual(0, _changed.Count);
        }

        [Test]
        public void Tick_MovesByTheClocksDeltaTime()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.TryWalk(id, North(3));
            _system.TryGet(id, out var character);

            _clock.DeltaTime = 0.1f;
            _system.Tick();

            Assert.AreEqual(character.Speed * 0.1f, character.StepProgress, 1e-5f);
        }

        [Test]
        public void Tick_WhilePaused_DoesNotMove()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.TryWalk(id, North(3));
            _system.TryGet(id, out var character);

            _clock.DeltaTime = 0f;
            _system.Tick();

            Assert.AreEqual(0f, character.StepProgress);
            Assert.AreEqual(_lobby, character.Cell);
        }

        [Test]
        public void Tick_ToThePathsEnd_AnnouncesIdleOnceTheCharacterStops()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.TryWalk(id, North(2));

            _clock.DeltaTime = 10f;
            _system.Tick();

            Assert.AreEqual(2, _changed.Count);
            Assert.AreEqual(CharacterState.Idle, _changed[1].State);
            Assert.AreEqual(CharacterState.Idle, _stateWhenAnnounced[1]);
        }

        [Test]
        public void Tick_WithoutAStateChange_AnnouncesNothing()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.TryWalk(id, North(3));
            _changed.Clear();

            _clock.DeltaTime = 0.1f;
            _system.Tick();

            Assert.AreEqual(0, _changed.Count);
        }

        /// <summary>A path north up column 0 from the lobby, <paramref name="count"/> cells long.</summary>
        private static CellPath North(int count)
        {
            var cells = new Cell[count];
            for (var i = 0; i < count; i++)
                cells[i] = new Cell(_lobby.Col, _lobby.Row - i);
            return new CellPath(cells);
        }
    }
}

using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Levels;
using HoldIt.Paths;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    /// <summary>The run of a level of three, its character messages sent by hand, with real brokers.</summary>
    public class LevelRunTests
    {
        private const int Crowd = 3;

        private IObjectResolver _container;
        private LevelDefinition _level;
        private RunConfig _config;
        private FakeCharacters _characters;
        private LevelRun _run;
        private List<int> _served;
        private List<int> _lives;
        private List<LevelEnded> _ended;
        private List<string> _order;
        private IDisposable _subscriptions;

        [SetUp]
        public void CreateRun()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
            builder.RegisterMessageBroker<ServedChanged>(messagePipe);
            builder.RegisterMessageBroker<LivesChanged>(messagePipe);
            builder.RegisterMessageBroker<LevelEnded>(messagePipe);
            _container = builder.Build();

            _level = TestLevels.Level(Crowd);
            _config = ScriptableObject.CreateInstance<RunConfig>();
            _characters = new FakeCharacters();
            _run = new LevelRun(_level, _config, _characters,
                _container.Resolve<ISubscriber<CharacterSpawned>>(), _container.Resolve<ISubscriber<CharacterStateChanged>>(),
                _container.Resolve<ISubscriber<CharacterRemoved>>(), _container.Resolve<IPublisher<ServedChanged>>(),
                _container.Resolve<IPublisher<LivesChanged>>(), _container.Resolve<IPublisher<LevelEnded>>());
            _run.Initialize();

            _served = new List<int>();
            _lives = new List<int>();
            _ended = new List<LevelEnded>();
            _order = new List<string>();
            var bag = DisposableBag.CreateBuilder();
            _container.Resolve<ISubscriber<ServedChanged>>().Subscribe(message =>
            {
                _served.Add(message.Served);
                Assert.AreEqual(message.Served, _run.Served, "The run changes before it is announced.");
                _order.Add("served");
            }).AddTo(bag);
            _container.Resolve<ISubscriber<LivesChanged>>().Subscribe(message =>
            {
                _lives.Add(message.Lives);
                Assert.AreEqual(message.Lives, _run.Lives, "The run changes before it is announced.");
                _order.Add("lives");
            }).AddTo(bag);
            _container.Resolve<ISubscriber<LevelEnded>>().Subscribe(message =>
            {
                _ended.Add(message);
                Assert.AreEqual(message.Outcome, _run.Outcome, "The run changes before it is announced.");
                _order.Add("ended");
            }).AddTo(bag);
            _subscriptions = bag.Build();
        }

        [TearDown]
        public void DisposeRun()
        {
            _subscriptions.Dispose();
            _run.Dispose();
            _container.Dispose();
            UnityEngine.Object.DestroyImmediate(_level);
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void New_IsPlayingWithEveryLife()
        {
            Assert.AreEqual(LevelOutcome.Playing, _run.Outcome);
            Assert.AreEqual(_config.Lives, _run.Lives);
            Assert.AreEqual(_config.Lives, _run.MaxLives);
            Assert.AreEqual(Crowd, _run.Crowd);
            Assert.AreEqual(0, _run.Served);
            Assert.AreEqual(0, _run.Stars);
        }

        [Test]
        public void InStall_CountsAsServed()
        {
            var id = Arrive();

            Publish(new CharacterStateChanged(id, CharacterState.InStall));

            Assert.AreEqual(1, _run.Served);
            CollectionAssert.AreEqual(new[] { 1 }, _served);
        }

        [Test]
        public void OtherStates_DoNotCount()
        {
            var id = Arrive();

            Publish(new CharacterStateChanged(id, CharacterState.Walking));
            Publish(new CharacterStateChanged(id, CharacterState.Queued));
            Publish(new CharacterStateChanged(id, CharacterState.Idle));

            Assert.AreEqual(0, _run.Served);
            Assert.AreEqual(0, _served.Count);
        }

        [Test]
        public void Accident_CostsALife()
        {
            var id = Arrive();

            Leave(id, RemovalReason.Accident);

            Assert.AreEqual(_config.Lives - 1, _run.Lives);
            CollectionAssert.AreEqual(new[] { _config.Lives - 1 }, _lives);
            Assert.AreEqual(LevelOutcome.Playing, _run.Outcome);
        }

        [Test]
        public void EnteringAStall_CostsNoLife()
        {
            var id = Arrive();

            Leave(id, RemovalReason.EnteredStall);

            Assert.AreEqual(_config.Lives, _run.Lives);
            Assert.AreEqual(0, _lives.Count);
        }

        [Test]
        public void TheLastLife_LosesTheLevelWithoutStars()
        {
            for (var i = 0; i < _config.Lives; i++)
                Leave(Arrive(), RemovalReason.Accident);

            Assert.AreEqual(LevelOutcome.Lost, _run.Outcome);
            Assert.AreEqual(0, _run.Lives);
            Assert.AreEqual(0, _run.Stars);
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(LevelOutcome.Lost, _ended[0].Outcome);
            Assert.AreEqual(0, _ended[0].Stars);
            Assert.AreEqual("ended", _order[_order.Count - 1]);
            Assert.AreEqual("lives", _order[_order.Count - 2]);
        }

        [Test]
        public void EveryoneServed_WinsWithEveryLifeAsStars()
        {
            for (var i = 0; i < Crowd; i++)
                Serve(Arrive());

            Assert.AreEqual(LevelOutcome.Won, _run.Outcome);
            Assert.AreEqual(_config.Lives, _run.Stars);
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(LevelOutcome.Won, _ended[0].Outcome);
            Assert.AreEqual(_config.Lives, _ended[0].Stars);
            Assert.AreEqual(Crowd, _ended[0].Served);
        }

        [Test]
        public void AnAccidentOnTheWay_WinsWithAStarLess()
        {
            Serve(Arrive());
            Leave(Arrive(), RemovalReason.Accident);
            Serve(Arrive());

            Assert.AreEqual(LevelOutcome.Won, _run.Outcome);
            Assert.AreEqual(_config.Lives - 1, _run.Stars);
            Assert.AreEqual(2, _ended[0].Served);
        }

        [Test]
        public void TheLastOneHavingAnAccident_StillWinsWithTheLivesLeft()
        {
            Serve(Arrive());
            Serve(Arrive());
            Leave(Arrive(), RemovalReason.Accident);

            Assert.AreEqual(LevelOutcome.Won, _run.Outcome);
            Assert.AreEqual(_config.Lives - 1, _run.Stars);
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(2, _ended[0].Served);
        }

        [Test]
        public void TheBoardEmptyBeforeEveryoneCame_IsNotAWin()
        {
            Serve(Arrive());
            Serve(Arrive());

            Assert.AreEqual(LevelOutcome.Playing, _run.Outcome);
            Assert.AreEqual(0, _ended.Count);
        }

        [Test]
        public void EveryoneCameButSomeoneIsStillOnTheBoard_IsNotAWin()
        {
            var first = Arrive();
            var second = Arrive();
            Arrive();
            Serve(first);
            Serve(second);

            Assert.AreEqual(LevelOutcome.Playing, _run.Outcome);
        }

        [Test]
        public void AfterTheEnd_NothingCounts()
        {
            for (var i = 0; i < _config.Lives; i++)
                Leave(Arrive(), RemovalReason.Accident);
            var late = Arrive();

            Serve(late);
            Leave(Arrive(), RemovalReason.Accident);

            Assert.AreEqual(0, _run.Served);
            Assert.AreEqual(0, _run.Lives);
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(_config.Lives, _lives.Count);
        }

        [Test]
        public void Dispose_StopsCounting()
        {
            var id = Arrive();
            _run.Dispose();

            Publish(new CharacterStateChanged(id, CharacterState.InStall));

            Assert.AreEqual(0, _run.Served);
        }

        private CharacterId Arrive()
        {
            var id = _characters.Add();
            Publish(new CharacterSpawned(id, Gender.Male, BodySize.Normal, new Cell(0, 0)));
            return id;
        }

        /// <summary>Goes into a stall: InStall, then off the board.</summary>
        private void Serve(CharacterId id)
        {
            Publish(new CharacterStateChanged(id, CharacterState.InStall));
            Leave(id, RemovalReason.EnteredStall);
        }

        private void Leave(CharacterId id, RemovalReason reason)
        {
            _characters.Remove(id);
            Publish(new CharacterRemoved(id, reason));
        }

        private void Publish<T>(T message) => _container.Resolve<IPublisher<T>>().Publish(message);

        /// <summary>Only the characters on the board: added and removed by hand.</summary>
        private sealed class FakeCharacters : ICharacters
        {
            private readonly List<ICharacter> _all = new List<ICharacter>();
            private int _lastId;

            public IReadOnlyList<ICharacter> All => _all;

            public CharacterId Add()
            {
                var id = new CharacterId(++_lastId);
                _all.Add(new Character(id, Gender.Male, BodySize.Normal, new Cell(0, 0), 1f, 0f, 1f));
                return id;
            }

            public void Remove(CharacterId id) => _all.RemoveAll(character => character.Id == id);

            public bool TryGet(CharacterId id, out ICharacter character)
            {
                character = _all.Find(each => each.Id == id);
                return character != null;
            }

            public CharacterId Spawn(Gender gender, BodySize size, Cell cell) => throw new NotSupportedException();
            public bool TryWalk(CharacterId id, CellPath path) => throw new NotSupportedException();
            public bool CanEndOn(CharacterId id, Cell cell) => throw new NotSupportedException();
            public bool IsFree(Cell cell) => throw new NotSupportedException();
        }
    }
}

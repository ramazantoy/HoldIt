using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Restrooms;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    /// <summary>The stalls of Map 1 (<see cref="TestMaps.Lobby"/>), two a restroom, with a real broker and a fake clock.</summary>
    public class RestroomSystemTests
    {
        private IObjectResolver _container;
        private RestroomConfig _config;
        private FakeClock _clock;
        private RestroomSystem _system;
        private List<StallStateChanged> _changed;
        private List<StallState> _stateWhenAnnounced;
        private IDisposable _subscription;

        [SetUp]
        public void CreateSystem()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<StallStateChanged>(messagePipe);
            _container = builder.Build();

            _config = ScriptableObject.CreateInstance<RestroomConfig>();
            _clock = new FakeClock();
            _system = new RestroomSystem(MapParser.Parse(TestMaps.Lobby), _config, 2, _clock,
                _container.Resolve<IPublisher<StallStateChanged>>());

            _changed = new List<StallStateChanged>();
            _stateWhenAnnounced = new List<StallState>();
            _subscription = _container.Resolve<ISubscriber<StallStateChanged>>().Subscribe(message =>
            {
                _changed.Add(message);
                _stateWhenAnnounced.Add(_system.StateOf(message.Gender, message.Stall));
            });
        }

        [TearDown]
        public void DisposeSystem()
        {
            _subscription.Dispose();
            _container.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void New_EveryStallIsFreeAndLaidOut()
        {
            Assert.AreEqual(2, _system.StallCount);
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                Assert.AreEqual(StallState.Free, _system.StateOf(gender, 0));
                Assert.AreEqual(StallState.Free, _system.StateOf(gender, 1));
                Assert.AreEqual(gender, _system.LayoutOf(gender).Gender);
                Assert.AreEqual(2, _system.LayoutOf(gender).Stalls.Count);
            }
        }

        [Test]
        public void TryTake_TakesTheFirstFreeStallAndGivesItsWayIn()
        {
            Assert.IsTrue(_system.TryTake(Gender.Male, out var first));
            Assert.IsTrue(_system.TryTake(Gender.Male, out var second));

            Assert.AreEqual(0, first.Stall);
            Assert.AreEqual(1, second.Stall);
            Assert.AreEqual(Gender.Male, first.Gender);
            Assert.AreSame(_system.LayoutOf(Gender.Male).Stalls[1].Entry, second.Line);
            Assert.AreEqual(StallState.Taken, _system.StateOf(Gender.Male, 0));
            Assert.AreEqual(StallState.Free, _system.StateOf(Gender.Female, 0));
        }

        [Test]
        public void TryTake_EveryStallBusy_IsRefusedAndAnnouncesNothing()
        {
            _system.TryTake(Gender.Female, out _);
            _system.TryTake(Gender.Female, out _);
            _changed.Clear();

            Assert.IsFalse(_system.TryTake(Gender.Female, out var visit));
            Assert.IsNull(visit);
            Assert.AreEqual(0, _changed.Count);
            Assert.IsTrue(_system.TryTake(Gender.Male, out _), "The other restroom is not affected.");
        }

        [Test]
        public void Tick_TimeUpButNotShut_KeepsTheStall()
        {
            _system.TryTake(Gender.Male, out _);

            Advance(_config.StallSeconds * 2f);

            Assert.AreEqual(StallState.Taken, _system.StateOf(Gender.Male, 0));
        }

        [Test]
        public void Tick_ShutAndTimeUp_FreesTheStall()
        {
            _system.TryTake(Gender.Male, out _);
            Advance(1f);
            _system.Shut(Gender.Male, 0);

            Advance(_config.StallSeconds - 1.1f);
            Assert.AreEqual(StallState.Shut, _system.StateOf(Gender.Male, 0));
            Advance(0.2f);

            Assert.AreEqual(StallState.Free, _system.StateOf(Gender.Male, 0));
        }

        [Test]
        public void Tick_ShutLateAfterTheTimeIsUp_FreesOnTheNextTick()
        {
            _system.TryTake(Gender.Male, out _);
            Advance(_config.StallSeconds + 1f);
            _system.Shut(Gender.Male, 0);

            Advance(0f);

            Assert.AreEqual(StallState.Free, _system.StateOf(Gender.Male, 0));
        }

        [Test]
        public void Changes_AreAnnouncedInOrderOnceMade()
        {
            _system.TryTake(Gender.Female, out _);
            _system.Shut(Gender.Female, 0);
            Advance(_config.StallSeconds);

            CollectionAssert.AreEqual(new[] { StallState.Taken, StallState.Shut, StallState.Free },
                _changed.ConvertAll(message => message.State));
            CollectionAssert.AreEqual(_changed.ConvertAll(message => message.State), _stateWhenAnnounced);
            Assert.IsTrue(_changed.TrueForAll(message => message.Gender == Gender.Female && message.Stall == 0));
        }

        [Test]
        public void Shut_AStallNotTaken_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _system.Shut(Gender.Male, 0));
            _system.TryTake(Gender.Male, out _);
            _system.Shut(Gender.Male, 0);
            Assert.Throws<InvalidOperationException>(() => _system.Shut(Gender.Male, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _system.Shut(Gender.Male, 2));
        }

        [Test]
        public void Tick_WhilePaused_ChangesNothing()
        {
            _system.TryTake(Gender.Male, out _);
            _system.Shut(Gender.Male, 0);
            _changed.Clear();

            for (var i = 0; i < 100; i++)
                Advance(0f);

            Assert.AreEqual(StallState.Shut, _system.StateOf(Gender.Male, 0));
            Assert.AreEqual(0, _changed.Count);
        }

        [Test]
        public void New_WithStallsThatDoNotFit_Throws()
        {
            Assert.Throws<ArgumentException>(() => new RestroomSystem(MapParser.Parse(TestMaps.Lobby), _config, 4, _clock,
                _container.Resolve<IPublisher<StallStateChanged>>()));
        }

        private void Advance(float seconds)
        {
            _clock.DeltaTime = seconds;
            _system.Tick();
        }
    }
}

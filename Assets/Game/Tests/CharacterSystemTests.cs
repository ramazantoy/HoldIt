using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Paths;
using HoldIt.Restrooms;
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
        private static readonly Cell _maleDoor = new Cell(2, 3);

        private IObjectResolver _container;
        private ITileGrid _grid;
        private CharacterConfig _config;
        private FakeClock _clock;
        private FakeRestrooms _restrooms;
        private CharacterSystem _system;
        private List<CharacterSpawned> _spawned;
        private List<CharacterStateChanged> _changed;
        private List<CharacterState> _stateWhenAnnounced;
        private List<StepRefused> _refused;
        private List<CharacterState> _stateWhenRefused;
        private List<CharacterId> _removed;
        private List<bool> _goneWhenRemoved;
        private IDisposable _subscriptions;

        [SetUp]
        public void CreateSystem()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<StepRefused>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
            _container = builder.Build();

            _grid = MapParser.Parse(TestMaps.Lobby);
            _config = ScriptableObject.CreateInstance<CharacterConfig>();
            _clock = new FakeClock();
            _restrooms = new FakeRestrooms();
            _system = new CharacterSystem(_grid, _config, _restrooms, _clock,
                _container.Resolve<IPublisher<CharacterSpawned>>(), _container.Resolve<IPublisher<CharacterStateChanged>>(),
                _container.Resolve<IPublisher<StepRefused>>(), _container.Resolve<IPublisher<CharacterRemoved>>());

            _spawned = new List<CharacterSpawned>();
            _changed = new List<CharacterStateChanged>();
            _stateWhenAnnounced = new List<CharacterState>();
            _refused = new List<StepRefused>();
            _stateWhenRefused = new List<CharacterState>();
            _removed = new List<CharacterId>();
            _goneWhenRemoved = new List<bool>();
            var bag = DisposableBag.CreateBuilder();
            _container.Resolve<ISubscriber<CharacterSpawned>>().Subscribe(_spawned.Add).AddTo(bag);
            _container.Resolve<ISubscriber<CharacterStateChanged>>().Subscribe(message =>
            {
                _changed.Add(message);
                _system.TryGet(message.Id, out var character);
                _stateWhenAnnounced.Add(character.State);
            }).AddTo(bag);
            _container.Resolve<ISubscriber<StepRefused>>().Subscribe(message =>
            {
                _refused.Add(message);
                _system.TryGet(message.Id, out var character);
                _stateWhenRefused.Add(character.State);
            }).AddTo(bag);
            _container.Resolve<ISubscriber<CharacterRemoved>>().Subscribe(message =>
            {
                _removed.Add(message.Id);
                _goneWhenRemoved.Add(!_system.TryGet(message.Id, out _));
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
            var col = 0;
            foreach (BodySize size in Enum.GetValues(typeof(BodySize)))
            {
                _system.TryGet(_system.Spawn(Gender.Male, size, new Cell(col++, 10)), out var character);
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
        public void Spawn_OnADoor_Throws()
        {
            Assert.Throws<ArgumentException>(() => _system.Spawn(Gender.Male, BodySize.Normal, _maleDoor));
            Assert.AreEqual(0, _spawned.Count);
        }

        [Test]
        public void Spawn_WhereSomeoneStands_Throws()
        {
            _system.Spawn(Gender.Male, BodySize.Normal, _lobby);

            Assert.Throws<ArgumentException>(() => _system.Spawn(Gender.Female, BodySize.Thin, _lobby));
            Assert.AreEqual(1, _system.All.Count);
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
        public void TryWalk_ToWhereSomeoneStands_IsRefused()
        {
            var walker = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            var other = _system.Spawn(Gender.Female, BodySize.Normal, new Cell(0, 8));

            Assert.IsFalse(_system.CanEndOn(walker, new Cell(0, 8)));
            Assert.IsFalse(_system.TryWalk(walker, North(3)));
            Assert.IsTrue(_system.CanEndOn(other, new Cell(0, 8)));
        }

        [Test]
        public void TryWalk_ToWhereSomeoneIsGoing_IsRefused()
        {
            var first = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            var second = _system.Spawn(Gender.Female, BodySize.Normal, new Cell(0, 7));
            _system.TryWalk(first, North(3));

            Assert.IsFalse(_system.IsFree(new Cell(0, 8)));
            Assert.IsFalse(_system.TryWalk(second, new CellPath(new[] { new Cell(0, 7), new Cell(0, 8) })));
        }

        [Test]
        public void TryWalk_KeepsOnlyTheEnd_SoTheStartFreesOnceLeft()
        {
            var walker = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            var other = _system.Spawn(Gender.Female, BodySize.Normal, new Cell(1, 10));
            _system.TryWalk(walker, North(3));
            _clock.DeltaTime = 0.6f;
            _system.Tick();

            Assert.AreNotEqual(_lobby, Character(walker).Cell);
            Assert.IsTrue(_system.CanEndOn(other, _lobby));
            Assert.IsTrue(_system.IsFree(_lobby));
            Assert.IsFalse(_system.CanEndOn(other, new Cell(0, 8)));
        }

        [Test]
        public void TryWalk_ToTheSameDoor_LetsBothGo()
        {
            var first = _system.Spawn(Gender.Male, BodySize.Normal, new Cell(2, 9));
            var second = _system.Spawn(Gender.Male, BodySize.Normal, new Cell(3, 9));

            Assert.IsTrue(_system.TryWalk(first, ToMaleDoor(new Cell(2, 9))));
            Assert.IsTrue(_system.CanEndOn(second, _maleDoor));
            Assert.IsTrue(_system.TryWalk(second, ToMaleDoor(new Cell(3, 9))));
        }

        [Test]
        public void Tick_MovesByTheClocksDeltaTime()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.TryWalk(id, North(3));

            _clock.DeltaTime = 0.1f;
            _system.Tick();

            Assert.AreEqual(Character(id).Speed * 0.1f, Character(id).StepProgress, 1e-5f);
        }

        [Test]
        public void Tick_WhilePaused_DoesNotMove()
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.TryWalk(id, North(3));

            _clock.DeltaTime = 0f;
            _system.Tick();

            Assert.AreEqual(0f, Character(id).StepProgress);
            Assert.AreEqual(_lobby, Character(id).Cell);
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
            Assert.AreEqual(0, _refused.Count);
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

        [Test]
        public void Tick_TwoCrossingPaths_NeverShareACell()
        {
            var across = _system.Spawn(Gender.Male, BodySize.Thin, new Cell(0, 9));
            var up = _system.Spawn(Gender.Female, BodySize.Fat, new Cell(2, 11));
            _system.TryWalk(across, Path((0, 9), (1, 9), (2, 9), (3, 9), (4, 9)));
            _system.TryWalk(up, Path((2, 11), (2, 10), (2, 9), (1, 9), (1, 10)));

            _clock.DeltaTime = 0.02f;
            for (var frame = 0; frame < 500; frame++)
            {
                _system.Tick();
                var a = Character(across);
                var b = Character(up);
                Assert.IsFalse(a.Cell == b.Cell || a.Cell == b.Next || a.Next == b.Cell || a.Next == b.Next,
                    $"Frame {frame}: {a.Cell}->{a.Next} and {b.Cell}->{b.Next} overlap.");
            }
        }

        [Test]
        public void Tick_BlockedTooLong_StopsWhereItIsAndSaysSoAfterward()
        {
            var walker = _system.Spawn(Gender.Male, BodySize.Normal, _lobby);
            _system.Spawn(Gender.Female, BodySize.Normal, new Cell(0, 9));
            var other = _system.Spawn(Gender.Female, BodySize.Thin, new Cell(5, 10));
            _system.TryWalk(walker, North(3));

            _clock.DeltaTime = _config.BlockedWaitSeconds * 0.5f;
            _system.Tick();
            Assert.AreEqual(CharacterState.Walking, Character(walker).State);
            _system.Tick();

            Assert.AreEqual(CharacterState.Idle, Character(walker).State);
            Assert.AreEqual(_lobby, Character(walker).Cell);
            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(walker, _refused[0].Id);
            Assert.AreEqual(StepRefusal.Blocked, _refused[0].Reason);
            Assert.AreEqual(CharacterState.Idle, _stateWhenRefused[0]);
            Assert.IsTrue(_system.CanEndOn(other, new Cell(0, 8)), "Its end is no longer reserved.");
        }

        [Test]
        public void Tick_AtTheWrongDoor_StopsInFrontOfItAndSaysSo()
        {
            var start = new Cell(3, 7);
            var walker = _system.Spawn(Gender.Female, BodySize.Normal, start);
            var other = _system.Spawn(Gender.Male, BodySize.Normal, new Cell(5, 10));
            _system.TryWalk(walker, Path((3, 7), (3, 6), (3, 5), (3, 4), (2, 4), (2, 3)));

            _clock.DeltaTime = 4f / _config.WalkSpeed(BodySize.Normal) + 0.01f;
            _system.Tick();

            Assert.AreEqual(CharacterState.Idle, Character(walker).State);
            Assert.AreEqual(new Cell(2, 4), Character(walker).Cell);
            Assert.AreEqual(Direction.North, Character(walker).Facing);
            Assert.AreEqual(StepRefusal.WrongDoor, _refused[0].Reason);
            Assert.AreEqual(CharacterState.Idle, _stateWhenRefused[0]);
            Assert.AreEqual(CharacterState.Idle, _changed[_changed.Count - 1].State);
            Assert.IsTrue(_system.CanEndOn(other, start), "Nothing is kept for it.");
            Assert.IsTrue(_system.IsFree(_maleDoor), "It never stepped onto the door.");

            _clock.DeltaTime = 10f;
            _system.Tick();

            Assert.AreEqual(new Cell(2, 4), Character(walker).Cell);
            Assert.AreEqual(1, _refused.Count);
        }

        [Test]
        public void Tick_FatAtANarrowPassage_IsRefusedButThinPasses()
        {
            var fat = _system.Spawn(Gender.Male, BodySize.Fat, new Cell(4, 9));
            var thin = _system.Spawn(Gender.Male, BodySize.Thin, new Cell(5, 9));
            _system.TryWalk(fat, Path((4, 9), (4, 8), (4, 7)));
            _system.TryWalk(thin, Path((5, 9), (5, 8), (5, 7)));

            _clock.DeltaTime = 10f;
            _system.Tick();

            Assert.AreEqual(new Cell(4, 9), Character(fat).Cell);
            Assert.AreEqual(new Cell(5, 7), Character(thin).Cell);
            Assert.AreEqual(1, _refused.Count);
            Assert.AreEqual(fat, _refused[0].Id);
            Assert.AreEqual(StepRefusal.TooWide, _refused[0].Reason);
        }

        [Test]
        public void Tick_AtItsDoorWithAFreeStall_GoesInWithoutStoppingFirst()
        {
            var id = WalkToMaleDoor(new Cell(2, 4));

            Assert.AreEqual(CharacterState.InStall, Character(id).State);
            Assert.AreEqual(0, Character(id).Visit.Stall);
            Assert.AreEqual(StallState.Taken, _restrooms.StateOf(Gender.Male, 0));
            CollectionAssert.AreEqual(new[] { CharacterState.Walking, CharacterState.InStall }, States());
            CollectionAssert.AreEqual(States(), _stateWhenAnnounced);
        }

        [Test]
        public void Tick_WalkingIn_HoldsTheDoorUntilItHasSteppedOffIt()
        {
            var id = WalkToMaleDoor(new Cell(2, 4));

            _clock.DeltaTime = 0.4f / _config.StallWalkSpeed;
            _system.Tick();
            Assert.IsFalse(_system.IsFree(_maleDoor));

            _clock.DeltaTime = 0.2f / _config.StallWalkSpeed;
            _system.Tick();
            Assert.IsTrue(_system.IsFree(_maleDoor));
            Assert.AreEqual(CharacterState.InStall, Character(id).State);
        }

        [Test]
        public void Tick_OnceInside_ShutsTheStallAndRemovesTheCharacter()
        {
            var id = WalkToMaleDoor(new Cell(2, 4));

            _clock.DeltaTime = _restrooms.LineLength / _config.StallWalkSpeed + 0.01f;
            _system.Tick();

            CollectionAssert.AreEqual(new[] { (Gender.Male, 0) }, _restrooms.Shuts);
            CollectionAssert.AreEqual(new[] { id }, _removed);
            Assert.IsTrue(_goneWhenRemoved[0], "It is gone before it is announced.");
            Assert.IsFalse(_system.TryGet(id, out _));
            Assert.AreEqual(0, _system.All.Count);
            Assert.IsTrue(_system.IsFree(_maleDoor));
        }

        [Test]
        public void Tick_InsideBeforeSteppingOffTheDoor_StillLetsGoOfIt()
        {
            _restrooms.LineLength = 0.3f;
            WalkToMaleDoor(new Cell(2, 4));

            _clock.DeltaTime = 1f;
            _system.Tick();

            Assert.AreEqual(1, _removed.Count);
            Assert.IsTrue(_system.IsFree(_maleDoor));
        }

        [Test]
        public void Tick_AfterTheRemoval_LeavesTheStallAlone()
        {
            WalkToMaleDoor(new Cell(2, 4));
            _clock.DeltaTime = 10f;

            for (var i = 0; i < 3; i++)
                _system.Tick();

            Assert.AreEqual(1, _restrooms.Shuts.Count);
            Assert.AreEqual(1, _removed.Count);
        }

        [Test]
        public void Tick_WithTheRealRestrooms_WalksInAndShutsItsStall()
        {
            // Brokers of its own: the set-up's subscribers look characters up in the other system.
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<StallStateChanged>(messagePipe);
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<StepRefused>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
            using var container = builder.Build();
            var restroomConfig = ScriptableObject.CreateInstance<RestroomConfig>();
            try
            {
                var restrooms = new RestroomSystem(_grid, restroomConfig, 2, _clock,
                    container.Resolve<IPublisher<StallStateChanged>>());
                var system = new CharacterSystem(_grid, _config, restrooms, _clock,
                    container.Resolve<IPublisher<CharacterSpawned>>(),
                    container.Resolve<IPublisher<CharacterStateChanged>>(),
                    container.Resolve<IPublisher<StepRefused>>(), container.Resolve<IPublisher<CharacterRemoved>>());
                var id = system.Spawn(Gender.Female, BodySize.Fat, new Cell(7, 4));
                system.TryWalk(id, new CellPath(new[] { new Cell(7, 4), new Cell(7, 3) }));

                _clock.DeltaTime = 0.05f;
                var doorFreedWhileIn = false;
                for (var frame = 0; frame < 200 && system.All.Count > 0; frame++)
                {
                    system.Tick();
                    doorFreedWhileIn |= system.All.Count > 0 && system.IsFree(new Cell(7, 3));
                }

                Assert.AreEqual(0, system.All.Count);
                Assert.IsTrue(doorFreedWhileIn, "It lets go of the door on the way in.");
                Assert.AreEqual(StallState.Shut, restrooms.StateOf(Gender.Female, 0));
                Assert.AreEqual(StallState.Free, restrooms.StateOf(Gender.Female, 1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restroomConfig);
            }
        }

        [Test]
        public void Tick_AtItsDoorWithEveryStallBusy_QueuesThereAndHoldsTheDoor()
        {
            _restrooms.Fill(Gender.Male);
            var id = WalkToMaleDoor(new Cell(2, 4));

            Assert.AreEqual(CharacterState.Queued, Character(id).State);
            Assert.AreEqual(_maleDoor, Character(id).Cell);
            CollectionAssert.AreEqual(new[] { CharacterState.Walking, CharacterState.Queued }, States());
            Assert.IsFalse(_system.IsFree(_maleDoor));

            _clock.DeltaTime = 10f;
            _system.Tick();
            Assert.AreEqual(CharacterState.Queued, Character(id).State);
            Assert.AreEqual(0, _removed.Count);
        }

        [Test]
        public void Tick_ToADoorSomeoneQueuesOn_WaitsThenStopsBlocked()
        {
            _restrooms.Fill(Gender.Male);
            WalkToMaleDoor(new Cell(2, 4));
            var second = _system.Spawn(Gender.Male, BodySize.Normal, new Cell(1, 4));
            _system.TryWalk(second, Path((1, 4), (2, 4), (2, 3)));

            _clock.DeltaTime = 1f / _config.WalkSpeed(BodySize.Normal) + 0.01f;
            _system.Tick();
            Assert.AreEqual(new Cell(2, 4), Character(second).Cell);
            _clock.DeltaTime = _config.BlockedWaitSeconds;
            _system.Tick();

            Assert.AreEqual(CharacterState.Idle, Character(second).State);
            Assert.AreEqual(new Cell(2, 4), Character(second).Cell);
            Assert.AreEqual(StepRefusal.Blocked, _refused[_refused.Count - 1].Reason);
        }

        [Test]
        public void Tick_QueuedWhenAStallFrees_GoesIntoIt()
        {
            _restrooms.Fill(Gender.Male);
            var id = WalkToMaleDoor(new Cell(2, 4));
            _restrooms.Free(Gender.Male, 1);

            _clock.DeltaTime = 0.01f;
            _system.Tick();

            Assert.AreEqual(CharacterState.InStall, Character(id).State);
            Assert.AreEqual(1, Character(id).Visit.Stall);
            Assert.AreEqual(CharacterState.InStall, _changed[_changed.Count - 1].State);
        }

        [Test]
        public void Tick_TheOtherRestroomsStallsBeingBusy_DoesNotQueue()
        {
            _restrooms.Fill(Gender.Female);
            var id = WalkToMaleDoor(new Cell(2, 4));

            Assert.AreEqual(CharacterState.InStall, Character(id).State);
        }

        /// <summary>A Normal man walks from <paramref name="from"/>, next to the men's door, onto it.</summary>
        private CharacterId WalkToMaleDoor(Cell from)
        {
            var id = _system.Spawn(Gender.Male, BodySize.Normal, from);
            Assert.IsTrue(_system.TryWalk(id, new CellPath(new[] { from, _maleDoor })));
            _clock.DeltaTime = 1f / _config.WalkSpeed(BodySize.Normal) + 0.01f;
            _system.Tick();
            return id;
        }

        private List<CharacterState> States() => _changed.ConvertAll(message => message.State);

        private ICharacter Character(CharacterId id)
        {
            _system.TryGet(id, out var character);
            return character;
        }

        /// <summary>A path north up column 0 from the lobby, <paramref name="count"/> cells long.</summary>
        private static CellPath North(int count)
        {
            var cells = new Cell[count];
            for (var i = 0; i < count; i++)
                cells[i] = new Cell(_lobby.Col, _lobby.Row - i);
            return new CellPath(cells);
        }

        private CellPath ToMaleDoor(Cell from) =>
            PathFinder.Shortest(_grid, from, _maleDoor,
                cell => PathRules.IsDrawable(_grid, cell) && (cell == _maleDoor || _grid.At(cell).Type != TileType.Door));

        private static CellPath Path(params (int Col, int Row)[] cells)
        {
            var path = new Cell[cells.Length];
            for (var i = 0; i < cells.Length; i++)
                path[i] = new Cell(cells[i].Col, cells[i].Row);
            return new CellPath(path);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Gameplay;
using HoldIt.Levels;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    public class LevelSessionTests
    {
        private FakeFactory _factory;
        private FakeReveal _reveal;
        private readonly List<string> _log = new List<string>();
        private FakeLevelCursor _cursor;
        private FakeInputModes _input;
        private FakePause _pause;
        private readonly List<UniTaskCompletionSource> _frames = new List<UniTaskCompletionSource>();
        private LevelSession _session;
        private IObjectResolver _bus;
        private IPublisher<SceneLoadingChanged> _loading;

        [SetUp]
        public void SetUpSession()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<SceneLoadingChanged>(messagePipe);
            _bus = builder.Build();
            _loading = _bus.Resolve<IPublisher<SceneLoadingChanged>>();

            _log.Clear();
            _reveal = new FakeReveal(_log);
            _factory = new FakeFactory(this);
            _cursor = new FakeLevelCursor();
            _input = new FakeInputModes();
            _pause = new FakePause();
            _frames.Clear();
            _session = new LevelSession(_factory, _cursor, _input, _pause, _reveal,
                _bus.Resolve<ISubscriber<SceneLoadingChanged>>(), WaitForFrame);
            _session.Start();
            // The first level comes in under a blocked input, like every one after it.
            Step();
            _log.Clear();
        }

        [TearDown]
        public void TearDownSession()
        {
            _session.Dispose();
            ((IDisposable)_bus).Dispose();
        }

        [Test]
        public void Start_BuildsTheCurrentLevelAndBringsItIn()
        {
            Assert.AreEqual(1, _factory.Levels.Count);
            Assert.IsTrue(_session.IsOpen);
            Assert.IsFalse(_session.IsChanging);
            Assert.AreEqual(1, _reveal.Shows);
            Assert.AreEqual(0, _reveal.Hides, "There is no old board to take out.");
        }

        [Test]
        public void Start_KeepsTheInputBlockedAndThePausedUntilTheBoardIsIn()
        {
            var input = new FakeInputModes();
            var pause = new FakePause();
            var session = new LevelSession(new FakeFactory(this), _cursor, input, pause, new FakeReveal(new List<string>()),
                _bus.Resolve<ISubscriber<SceneLoadingChanged>>(), WaitForFrame);

            session.Start();

            Assert.IsTrue(session.IsChanging);
            CollectionAssert.AreEqual(new[] { InputModes.Blocked }, input.Requests);
            Assert.IsTrue(pause.IsPaused);

            Step();

            Assert.IsFalse(session.IsChanging);
            Assert.AreEqual(0, input.Requests.Count);
            Assert.IsFalse(pause.IsPaused);
            session.Dispose();
        }

        [Test]
        public void Change_TakesTheOldBoardOutThenBuildsThenBringsTheNewOneIn()
        {
            _session.Retry().Forget();
            Step();
            Step();

            CollectionAssert.AreEqual(new[] { "hide", "build", "show" }, _log);
        }

        [Test]
        public void Retry_TakesTheOldLevelDownAndBuildsANewOne()
        {
            _session.Retry().Forget();
            Step();
            Step();

            Assert.AreEqual(2, _factory.Levels.Count);
            Assert.IsTrue(_factory.Levels[0].IsDisposed);
            Assert.IsFalse(_factory.Levels[1].IsDisposed);
            Assert.AreEqual(0, _cursor.Index);
        }

        [Test]
        public void Change_TakesTheOldLevelsContentOutWithTheBoard()
        {
            var piece = new GameObject("Piece").transform;
            _factory.Levels[0].Content.Add(piece);

            _session.Retry().Forget();
            Step();
            Step();

            CollectionAssert.AreEqual(new[] { piece }, _reveal.HiddenContent);
            UnityEngine.Object.DestroyImmediate(piece.gameObject);
        }

        [Test]
        public void Retry_TakesTheOldOneDownBeforeBuildingTheNew()
        {
            _session.Retry().Forget();
            Step();

            Assert.IsTrue(_factory.Levels[0].IsDisposed);
            Assert.AreEqual(2, _factory.Levels.Count);
            Assert.IsTrue(_factory.OldWasDownWhenBuilding);
        }

        [Test]
        public void Next_MovesTheCursorBeforeBuilding()
        {
            _session.Next().Forget();
            Step();
            Step();

            Assert.AreEqual(2, _factory.Levels.Count);
            Assert.AreEqual(1, _factory.CursorIndexAtBuild[1]);
            Assert.AreEqual(1, _cursor.Index);
        }

        [Test]
        public void Change_BlocksTheInputAndPausesUntilTheNewLevelIsUp()
        {
            _session.Retry().Forget();

            Assert.IsTrue(_session.IsChanging);
            CollectionAssert.AreEqual(new[] { InputModes.Blocked }, _input.Requests);
            Assert.IsTrue(_pause.IsPaused);

            Step();
            Assert.IsTrue(_session.IsChanging);
            Assert.IsTrue(_pause.IsPaused);

            Step();
            Assert.IsFalse(_session.IsChanging);
            Assert.AreEqual(0, _input.Requests.Count);
            Assert.IsFalse(_pause.IsPaused);
        }

        [Test]
        public void Change_WhileChanging_DoesNothing()
        {
            _session.Retry().Forget();
            _session.Retry().Forget();
            _session.Next().Forget();
            Step();
            Step();

            Assert.AreEqual(2, _factory.Levels.Count);
            Assert.AreEqual(0, _cursor.Index);
            Assert.AreEqual(0, _frames.Count);
        }

        [Test]
        public void Next_WithTheNextLocked_DoesNothing()
        {
            _cursor.Open = 1;

            _session.Next().Forget();

            Assert.IsFalse(_session.IsChanging);
            Assert.AreEqual(1, _factory.Levels.Count);
            Assert.AreEqual(0, _cursor.Index);
            Assert.AreEqual(0, _input.Requests.Count);
        }

        [Test]
        public void Change_ThatCanBeDoneAgainAfterwards()
        {
            _session.Retry().Forget();
            Step();
            Step();
            _session.Retry().Forget();
            Step();
            Step();

            Assert.AreEqual(3, _factory.Levels.Count);
            Assert.IsTrue(_factory.Levels[1].IsDisposed);
            Assert.IsFalse(_factory.Levels[2].IsDisposed);
        }

        [Test]
        public void Dispose_TakesTheLevelDown()
        {
            _session.Dispose();

            Assert.IsTrue(_factory.Levels[0].IsDisposed);
            Assert.IsFalse(_session.IsOpen);
        }

        [Test]
        public void Dispose_DuringAChange_ReleasesTheInputAndThePause()
        {
            var change = _session.Retry();

            _session.Dispose();

            Assert.AreEqual(UniTaskStatus.Canceled, change.Status);
            Assert.AreEqual(0, _input.Requests.Count);
            Assert.IsFalse(_pause.IsPaused);
            Assert.IsFalse(_session.IsChanging);
        }

        [Test]
        public void SceneLoading_TakesTheLevelDownBeforeTheSceneGoes()
        {
            _loading.Publish(new SceneLoadingChanged(true, "Menu"));

            Assert.IsTrue(_factory.Levels[0].IsDisposed);
            Assert.IsFalse(_session.IsOpen);
        }

        [Test]
        public void SceneLoading_DoneLeavesTheLevelBeCalled()
        {
            _loading.Publish(new SceneLoadingChanged(false, "Menu"));

            Assert.IsFalse(_factory.Levels[0].IsDisposed);
            Assert.IsTrue(_session.IsOpen);
        }

        [Test]
        public void SceneLoading_DuringAChange_StopsItAndReleasesTheInputAndThePause()
        {
            var change = _session.Retry();

            _loading.Publish(new SceneLoadingChanged(true, "Menu"));

            Assert.AreEqual(UniTaskStatus.Canceled, change.Status);
            Assert.AreEqual(0, _input.Requests.Count);
            Assert.IsFalse(_pause.IsPaused);
            Assert.IsTrue(_factory.Levels[0].IsDisposed);
        }

        [Test]
        public void SceneLoading_AfterTheSessionIsDisposed_DoesNothing()
        {
            _session.Dispose();

            Assert.DoesNotThrow(() => _loading.Publish(new SceneLoadingChanged(true, "Menu")));
            Assert.AreEqual(1, _factory.Levels.Count);
        }

        // The oldest frame still waited for passes.
        private void Step()
        {
            var frame = _frames[0];
            _frames.RemoveAt(0);
            frame.TrySetResult();
        }

        private UniTask WaitForFrame(CancellationToken token)
        {
            var frame = new UniTaskCompletionSource();
            _frames.Add(frame);
            token.Register(() =>
            {
                _frames.Remove(frame);
                frame.TrySetCanceled(token);
            });
            return frame.Task;
        }

        private sealed class FakeFactory : ILevelScopeFactory
        {
            private readonly LevelSessionTests _test;

            public FakeFactory(LevelSessionTests test) => _test = test;

            public List<FakeLevel> Levels { get; } = new List<FakeLevel>();
            public List<int> CursorIndexAtBuild { get; } = new List<int>();
            public bool OldWasDownWhenBuilding { get; private set; }

            public ILevel Create(LevelDefinition level)
            {
                if (Levels.Count > 0)
                    OldWasDownWhenBuilding = Levels[Levels.Count - 1].IsDisposed;
                _test._log.Add("build");
                CursorIndexAtBuild.Add(_test._cursor.Index);
                var created = new FakeLevel();
                Levels.Add(created);
                return created;
            }
        }

        private sealed class FakeReveal : IBoardReveal
        {
            private readonly List<string> _log;

            public FakeReveal(List<string> log) => _log = log;

            public int Shows { get; private set; }
            public int Hides { get; private set; }

            public UniTask Show(CancellationToken cancellation)
            {
                Shows++;
                _log.Add("show");
                return UniTask.CompletedTask;
            }

            public List<Transform> HiddenContent { get; } = new List<Transform>();

            public UniTask Hide(IReadOnlyList<Transform> content, CancellationToken cancellation)
            {
                HiddenContent.AddRange(content);
                Hides++;
                _log.Add("hide");
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeLevel : ILevel
        {
            public bool IsDisposed { get; private set; }
            public List<Transform> Content { get; } = new List<Transform>();

            public void CollectContent(List<Transform> into) => into.AddRange(Content);

            public void Dispose() => IsDisposed = true;
        }
    }
}

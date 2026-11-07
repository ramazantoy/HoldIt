using System;
using Cysharp.Threading.Tasks;
using HoldIt.Gameplay;
using HoldIt.Levels;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class LevelEndFlowTests
    {
        private FakeResultView _view;
        private FakeLevelRun _run;
        private FakeLevelCursor _cursor;
        private RunConfig _config;
        private FakeClock _clock;
        private FakePause _pause;
        private FakeInputModes _input;
        private FakeSceneLoader _scenes;
        private FakeLevelSession _session;
        private Broker<LevelEnded> _ended;
        private BackBroker _back;
        private LevelEndFlow _flow;

        [SetUp]
        public void SetUpFlow()
        {
            _view = new FakeResultView();
            _run = new FakeLevelRun();
            _cursor = new FakeLevelCursor();
            _config = ScriptableObject.CreateInstance<RunConfig>();
            _clock = new FakeClock();
            _pause = new FakePause();
            _input = new FakeInputModes();
            _scenes = new FakeSceneLoader();
            _session = new FakeLevelSession();
            _ended = new Broker<LevelEnded>();
            _back = new BackBroker();
            _flow = new LevelEndFlow(_view, _run, _cursor, _config, _clock, _pause, _input, _scenes, _session,
                _ended.Subscriber, _back.Subscriber);
            _flow.Initialize();
            _flow.Start();
        }

        [TearDown]
        public void TearDownFlow()
        {
            _flow.Dispose();
            _ended.Dispose();
            _back.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void Start_HidesTheScreen()
        {
            Assert.AreEqual(1, _view.Hides);
            Assert.IsNull(_view.Shown);
        }

        [Test]
        public void WhilePlaying_ShowsNothing()
        {
            Tick(10f);

            Assert.IsNull(_view.Shown);
            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(0, _input.Requests.Count);
        }

        [Test]
        public void End_StopsDrawingAtOnce()
        {
            End(LevelOutcome.Lost);

            CollectionAssert.AreEqual(new[] { InputModes.Menu }, _input.Requests);
            Assert.IsFalse(_pause.IsPaused);
            Assert.IsNull(_view.Shown);
        }

        [Test]
        public void End_AfterTheDelay_PausesUnderTheScreen()
        {
            End(LevelOutcome.Lost);

            Tick(_config.EndDelay * 0.5f);
            Assert.IsNull(_view.Shown);
            Tick(_config.EndDelay * 0.5f + 0.01f);

            Assert.IsTrue(_pause.IsPaused);
            Assert.IsTrue(_view.Shown.HasValue);
            Assert.IsTrue(_flow.IsShowing);
        }

        [Test]
        public void End_TheDelayWaitsForGameTime()
        {
            End(LevelOutcome.Lost);

            for (var i = 0; i < 10; i++)
                Tick(0f);

            Assert.IsNull(_view.Shown);
        }

        [Test]
        public void Screen_ShowsTheRunAndTheLevel()
        {
            _cursor.Index = 1;
            _run.Served = 7;
            _run.Crowd = 9;
            _run.Stars = 2;

            ShowResult(LevelOutcome.Won);

            var result = _view.Shown.Value;
            Assert.AreEqual(LevelOutcome.Won, result.Outcome);
            Assert.AreEqual(2, result.Level);
            Assert.AreEqual(2, result.Stars);
            Assert.AreEqual(3, result.MaxStars);
            Assert.AreEqual(7, result.Served);
            Assert.AreEqual(9, result.Crowd);
            Assert.IsTrue(result.HasNext);
            Assert.IsTrue(result.NextOpen);
            Assert.IsFalse(result.IsGameDone);
        }

        [Test]
        public void Screen_OnTheLastLevelWon_IsTheGamesEnd()
        {
            _cursor.Index = _cursor.Count - 1;

            ShowResult(LevelOutcome.Won);

            Assert.IsFalse(_view.Shown.Value.HasNext);
            Assert.IsFalse(_view.Shown.Value.NextOpen);
            Assert.IsTrue(_view.Shown.Value.IsGameDone);
        }

        [Test]
        public void End_Twice_ShowsOnce()
        {
            ShowResult(LevelOutcome.Lost);
            End(LevelOutcome.Lost);
            Tick(10f);

            Assert.AreEqual(1, _view.Shows);
            Assert.AreEqual(1, _input.Requests.Count);
            Assert.AreEqual(1, _pause.ActiveReasons.Count);
        }

        [Test]
        public void Next_AsksTheSessionOnce()
        {
            ShowResult(LevelOutcome.Won);

            _view.ClickNext();
            _view.ClickNext();

            Assert.AreEqual(1, _session.Nexts);
            Assert.AreEqual(0, _session.Retries);
            Assert.AreEqual(0, _scenes.Loads.Count, "A level change does not load the scene.");
        }

        [Test]
        public void Next_AfterALoss_WithTheNextOpen_MovesOn()
        {
            ShowResult(LevelOutcome.Lost);

            Assert.IsTrue(_view.Shown.Value.NextOpen, "A level played again may have its next one open already.");
            _view.ClickNext();

            Assert.AreEqual(1, _session.Nexts);
        }

        [Test]
        public void Next_WithTheNextLocked_IsHiddenAndDoesNothing()
        {
            _cursor.Open = 1;
            ShowResult(LevelOutcome.Lost);

            Assert.IsTrue(_view.Shown.Value.HasNext);
            Assert.IsFalse(_view.Shown.Value.NextOpen);
            _view.ClickNext();

            Assert.AreEqual(0, _session.Nexts);
        }

        [Test]
        public void Next_OnTheLastLevel_DoesNothing()
        {
            _cursor.Index = _cursor.Count - 1;
            ShowResult(LevelOutcome.Won);

            _view.ClickNext();

            Assert.AreEqual(0, _session.Nexts);
        }

        [Test]
        public void Retry_AsksTheSessionOnce()
        {
            ShowResult(LevelOutcome.Lost);

            _view.ClickRetry();
            _view.ClickRetry();

            Assert.AreEqual(1, _session.Retries);
            Assert.AreEqual(0, _session.Nexts);
            Assert.AreEqual(0, _scenes.Loads.Count, "A level change does not load the scene.");
        }

        [Test]
        public void MenuOrBack_LoadsTheMenuOnce()
        {
            ShowResult(LevelOutcome.Lost);

            _view.ClickMenu();
            _back.Press();
            _view.ClickRetry();

            CollectionAssert.AreEqual(new[] { "MenuState Menu" }, _scenes.Loads);
            Assert.AreEqual(0, _session.Retries);
        }

        [Test]
        public void Buttons_BeforeTheScreenShows_DoNothing()
        {
            End(LevelOutcome.Won);

            _back.Press();
            _view.ClickRetry();
            _view.ClickNext();

            Assert.AreEqual(0, _session.Nexts);
            Assert.AreEqual(0, _session.Retries);
        }

        [Test]
        public void Dispose_ReleasesThePauseAndTheInputMode()
        {
            ShowResult(LevelOutcome.Lost);

            _flow.Dispose();

            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(0, _input.Requests.Count);
        }

        private void End(LevelOutcome outcome)
        {
            _run.Outcome = outcome;
            _ended.Publish(new LevelEnded(outcome, _run.Stars, _run.Served));
        }

        private void ShowResult(LevelOutcome outcome)
        {
            End(outcome);
            Tick(_config.EndDelay + 0.01f);
        }

        private void Tick(float deltaTime)
        {
            _clock.DeltaTime = deltaTime;
            _flow.Tick();
        }

        private sealed class FakeLevelSession : ILevelSession
        {
            public int Retries { get; private set; }
            public int Nexts { get; private set; }
            public bool IsChanging => false;

            public UniTask Retry()
            {
                Retries++;
                return UniTask.CompletedTask;
            }

            public UniTask Next()
            {
                Nexts++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeResultView : ILevelResultView
        {
            public event Action NextClicked;
            public event Action RetryClicked;
            public event Action MenuClicked;

            public LevelResult? Shown { get; private set; }
            public int Shows { get; private set; }
            public int Hides { get; private set; }

            public void Show(LevelResult result)
            {
                Shown = result;
                Shows++;
            }

            public void Hide() => Hides++;

            public void ClickNext() => NextClicked?.Invoke();
            public void ClickRetry() => RetryClicked?.Invoke();
            public void ClickMenu() => MenuClicked?.Invoke();
        }
    }
}

using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Intro;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class IntroFlowTests
    {
        private FakeVideo _video;
        private FakeBoot _boot;
        private FakeSceneLoader _scenes;
        private IntroFlow _flow;

        [SetUp]
        public void SetUpFlow()
        {
            _video = new FakeVideo();
            _boot = new FakeBoot();
            _scenes = new FakeSceneLoader();
            _flow = new IntroFlow(_video, _boot, _scenes);
        }

        [Test]
        public void OpensTheMenu_OnlyOnceTheVideoAndTheBootAreBothDone()
        {
            var running = _flow.StartAsync(CancellationToken.None);
            Assert.IsTrue(_video.Started);

            _video.Finish();
            CollectionAssert.IsEmpty(_scenes.Loads, "The boot is still running.");

            _boot.Ready.TrySetResult();
            CollectionAssert.AreEqual(new[] { "MenuState Menu" }, _scenes.Loads);
            Assert.AreEqual(UniTaskStatus.Succeeded, running.Status);
        }

        [Test]
        public void FailedBoot_StaysInTheIntro()
        {
            var running = _flow.StartAsync(CancellationToken.None);
            _video.Finish();

            _boot.Ready.TrySetException(new InvalidOperationException("A boot step failed."));

            CollectionAssert.IsEmpty(_scenes.Loads);
            Assert.AreEqual(UniTaskStatus.Succeeded, running.Status, "The failure is AppBoot's to log, not the intro's.");
        }

        [Test]
        public void Cancelled_EndsCancelled_WithoutOpeningTheMenu()
        {
            using var stop = new CancellationTokenSource();
            var running = _flow.StartAsync(stop.Token);

            stop.Cancel(); // Play stopped, or the app quit, during the intro.

            Assert.AreEqual(UniTaskStatus.Canceled, running.Status, "The cancellation is not swallowed.");
            CollectionAssert.IsEmpty(_scenes.Loads);
        }

        private sealed class FakeVideo : IIntroVideo
        {
            private readonly UniTaskCompletionSource _finished = new UniTaskCompletionSource();

            public bool Started { get; private set; }

            public UniTask Play(CancellationToken cancellation)
            {
                Started = true;
                return _finished.Task.AttachExternalCancellation(cancellation);
            }

            public void Finish() => _finished.TrySetResult();
        }

        private sealed class FakeBoot : IAppBoot
        {
            public readonly UniTaskCompletionSource Ready = new UniTaskCompletionSource();

            public bool IsReady => Ready.Task.Status == UniTaskStatus.Succeeded;

            public UniTask WaitReady(CancellationToken cancellation) => Ready.Task.AttachExternalCancellation(cancellation);
        }
    }
}

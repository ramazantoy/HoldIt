using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AF.Core.Tests
{
    // PrimeTween updates from EditorApplication.update in Edit Mode, so these run the real tweens.
    public class TweenExtensionsTests
    {
        private GameObject _target;

        [SetUp]
        public void SetUp() => _target = new GameObject("Tweened");

        [TearDown]
        public void TearDown()
        {
            Tween.StopAll(_target.transform);
            Object.DestroyImmediate(_target);
        }

        [UnityTest]
        public IEnumerator Cancelling_StopsTheTween_AndTheAwaitThrows()
        {
            using var cancellation = new CancellationTokenSource();
            var tween = Tween.PositionX(_target.transform, 10f, 10f);
            var task = tween.ToUniTask(cancellation.Token);

            cancellation.Cancel();
            yield return WaitFor(() => task.Status != UniTaskStatus.Pending);

            Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
            Assert.IsFalse(tween.isAlive);
            Assert.Less(_target.transform.position.x, 10f, "Stopped where it was, not completed.");
        }

        [Test]
        public void AlreadyCancelledToken_StopsTheTween_AndTheAwaitThrows()
        {
            var tween = Tween.PositionX(_target.transform, 10f, 10f);

            var task = tween.ToUniTask(new CancellationToken(true));

            Assert.AreEqual(UniTaskStatus.Canceled, task.Status, "PrimeTween's own await returns normally here.");
            Assert.IsFalse(tween.isAlive);
        }

        [UnityTest]
        public IEnumerator FinishedTween_Completes()
        {
            var tween = Tween.PositionX(_target.transform, 1f, 0.05f);
            var task = tween.ToUniTask(CancellationToken.None);

            yield return WaitFor(() => task.Status != UniTaskStatus.Pending);

            Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
            Assert.AreEqual(1f, _target.transform.position.x, 0.0001f);
        }

        [Test]
        public void DeadTween_CompletesAtOnce()
        {
            Assert.AreEqual(UniTaskStatus.Succeeded, default(Tween).ToUniTask(CancellationToken.None).Status);
            Assert.AreEqual(UniTaskStatus.Canceled, default(Sequence).ToUniTask(new CancellationToken(true)).Status);
        }

        [UnityTest]
        public IEnumerator CancellingASequence_StopsIt_AndTheAwaitThrows()
        {
            using var cancellation = new CancellationTokenSource();
            var sequence = Sequence.Create().Chain(Tween.PositionX(_target.transform, 10f, 10f));
            var task = sequence.ToUniTask(cancellation.Token);

            cancellation.Cancel();
            yield return WaitFor(() => task.Status != UniTaskStatus.Pending);

            Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
            Assert.IsFalse(sequence.isAlive);
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            var deadline = EditorApplication.timeSinceStartup + 2.0;
            while (!condition() && EditorApplication.timeSinceStartup < deadline)
                yield return null;
        }
    }
}

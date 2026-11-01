using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Core.Tests
{
    public class AppBootTests
    {
        [Test]
        public void StartAsync_RunsStepsInOrder_ThenIsReady()
        {
            var order = new List<int>();
            var boot = new AppBoot(new IBootStep[] { new RecordingStep(order, 1), new RecordingStep(order, 2) });
            Assert.IsFalse(boot.IsReady);

            boot.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
            Assert.IsTrue(boot.IsReady);
            Assert.DoesNotThrow(() => boot.WaitReady(CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void StartAsync_WithoutSteps_IsReady()
        {
            var boot = new AppBoot(Array.Empty<IBootStep>());

            boot.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(boot.IsReady);
        }

        [Test]
        public void FailingStep_IsLogged_AndWaitReadyThrowsIt()
        {
            var failure = new InvalidOperationException("boot step failed");
            var order = new List<int>();
            var boot = new AppBoot(new IBootStep[] { new FailingStep(failure), new RecordingStep(order, 2) });
            LogAssert.Expect(LogType.Exception, new Regex("boot step failed"));

            boot.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(boot.IsReady);
            Assert.IsEmpty(order, "Steps after a failure do not run.");
            var thrown = Assert.Throws<InvalidOperationException>(() => boot.WaitReady(CancellationToken.None).GetAwaiter().GetResult());
            Assert.AreSame(failure, thrown);
        }

        [Test]
        public void CancelledBoot_GivesWaitersTheCancelledToken()
        {
            using var cancellation = new CancellationTokenSource();
            var boot = new AppBoot(new IBootStep[] { new WaitingStep() });
            var start = boot.StartAsync(cancellation.Token);

            cancellation.Cancel();
            start.GetAwaiter().GetResult();

            var thrown = Assert.Catch<OperationCanceledException>(() => boot.WaitReady(CancellationToken.None).GetAwaiter().GetResult());
            Assert.IsTrue(EntryPointErrors.IsCancellation(thrown), "An entry point waiting for the boot ends without an error.");
        }

        private sealed class WaitingStep : IBootStep
        {
            public UniTask Run(CancellationToken cancellation) => UniTask.Never(cancellation);
        }

        private sealed class RecordingStep : IBootStep
        {
            private readonly List<int> _order;
            private readonly int _id;

            public RecordingStep(List<int> order, int id)
            {
                _order = order;
                _id = id;
            }

            public UniTask Run(CancellationToken cancellation)
            {
                _order.Add(_id);
                return UniTask.CompletedTask;
            }
        }

        private sealed class FailingStep : IBootStep
        {
            private readonly Exception _exception;

            public FailingStep(Exception exception) => _exception = exception;

            public UniTask Run(CancellationToken cancellation) => UniTask.FromException(_exception);
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HoldIt.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class PoolWarmupStepTests
    {
        private FakePools _pools;
        private Transform _marker;
        private Transform _bubble;
        private readonly List<UniTaskCompletionSource> _frames = new List<UniTaskCompletionSource>();

        [SetUp]
        public void SetUpStep()
        {
            _pools = new FakePools();
            _marker = new GameObject("Marker").transform;
            _bubble = new GameObject("Bubble").transform;
            _frames.Clear();
        }

        [TearDown]
        public void TearDownStep()
        {
            Object.DestroyImmediate(_marker.gameObject);
            Object.DestroyImmediate(_bubble.gameObject);
        }

        [Test]
        public void Run_PrewarmsEveryPrefabThenSpawnsAndDespawnsOne()
        {
            var step = Step(WarmupItem.Of(_marker, 20), WarmupItem.Of(_bubble, 8));

            var running = step.Run(CancellationToken.None);
            PassFrames(2);

            Assert.AreEqual(UniTaskStatus.Succeeded, running.Status);
            CollectionAssert.AreEqual(new[]
            {
                "prewarm Marker 20", "spawn Marker", "despawn Marker",
                "prewarm Bubble 8", "spawn Bubble", "despawn Bubble",
            }, _pools.Log);
            Assert.AreEqual(20, _pools.Pool(_marker).CountInactive);
            Assert.AreEqual(0, _pools.Pool(_marker).CountActive, "The warm spawn is given back.");
        }

        [Test]
        public void Run_WarmsOnePrefabPerFrame()
        {
            var step = Step(WarmupItem.Of(_marker, 5), WarmupItem.Of(_bubble, 5));

            var running = step.Run(CancellationToken.None);

            CollectionAssert.AreEqual(new[] { "prewarm Marker 5", "spawn Marker", "despawn Marker" }, _pools.Log);
            Assert.AreEqual(UniTaskStatus.Pending, running.Status);

            PassFrames(1);
            Assert.AreEqual(6, _pools.Log.Count);
            Assert.AreEqual(UniTaskStatus.Pending, running.Status, "The last prefab is followed by a frame too.");

            PassFrames(1);
            Assert.AreEqual(UniTaskStatus.Succeeded, running.Status);
        }

        [Test]
        public void Run_LeavesAPrefabWithNoCountAlone()
        {
            var step = Step(WarmupItem.Of(_marker, 0), WarmupItem.Of(_bubble, 3));

            var running = step.Run(CancellationToken.None);
            PassFrames(1);

            Assert.AreEqual(UniTaskStatus.Succeeded, running.Status);
            CollectionAssert.AreEqual(new[] { "prewarm Bubble 3", "spawn Bubble", "despawn Bubble" }, _pools.Log);
        }

        [Test]
        public void Run_Cancelled_StopsBeforeTheNextPrefab()
        {
            var step = Step(WarmupItem.Of(_marker, 5), WarmupItem.Of(_bubble, 5));
            using var source = new CancellationTokenSource();

            var running = step.Run(source.Token);
            source.Cancel();

            Assert.AreEqual(UniTaskStatus.Canceled, running.Status);
            CollectionAssert.DoesNotContain(_pools.Log, "prewarm Bubble 5");
        }

        [Test]
        public void Run_WithNothingToWarm_Completes()
        {
            var running = Step().Run(CancellationToken.None);

            Assert.AreEqual(UniTaskStatus.Succeeded, running.Status);
            CollectionAssert.IsEmpty(_pools.Log);
        }

        private PoolWarmupStep Step(params WarmupItem[] items) => new PoolWarmupStep(_pools, items, WaitForFrame);

        private void PassFrames(int count)
        {
            for (var i = 0; i < count && _frames.Count > 0; i++)
            {
                var frame = _frames[0];
                _frames.RemoveAt(0);
                frame.TrySetResult();
            }
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
    }
}

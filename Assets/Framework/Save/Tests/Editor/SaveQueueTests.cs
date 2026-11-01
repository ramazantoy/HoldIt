using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AF.Save.Tests
{
    public class SaveQueueTests
    {
        [UnityTest]
        public IEnumerator Writes_RunInOrder_AndAWaitingWriteToTheSameEntryIsReplaced() => UniTask.ToCoroutine(async () =>
        {
            var storage = new GatedStorage();
            var queue = new SaveQueue(storage);

            var first = queue.Write("a", "1"); // running, held at the gate
            var second = queue.Write("b", "1");
            var third = queue.Write("a", "2");
            var fourth = queue.Write("a", "3"); // replaces the waiting "a" write
            storage.Gate.Set();
            await UniTask.WhenAll(first, second, third, fourth);

            CollectionAssert.AreEqual(new[] { "write a=1", "write b=1", "write a=3" }, storage.Calls.ToArray());
        });

        [UnityTest]
        public IEnumerator WriteQueuedAfterARead_IsNotMergedPastIt() => UniTask.ToCoroutine(async () =>
        {
            var storage = new GatedStorage();
            var queue = new SaveQueue(storage);

            var first = queue.Write("a", "1");
            var second = queue.Write("a", "2");
            var read = queue.Read("a");
            var third = queue.Write("a", "3");
            storage.Gate.Set();
            await UniTask.WhenAll(first, second, read.AsUniTask(), third);

            CollectionAssert.AreEqual(new[] { "write a=1", "write a=2", "read a", "write a=3" }, storage.Calls.ToArray());
        });

        [UnityTest]
        public IEnumerator FailedWrite_ThrowsToItsCaller_AndTheQueueGoesOn() => UniTask.ToCoroutine(async () =>
        {
            var storage = new GatedStorage { HoldFirst = false };
            var queue = new SaveQueue(storage);

            var failing = queue.Write("a", GatedStorage.Fail);
            var next = queue.Write("b", "1");

            var thrown = false;
            try
            {
                await failing;
            }
            catch (IOException)
            {
                thrown = true;
            }
            await next;

            Assert.IsTrue(thrown);
            CollectionAssert.AreEqual(new[] { "write b=1" }, storage.Calls.ToArray());
        });

        [UnityTest]
        public IEnumerator CancelledCaller_DoesNotDropTheWrite() => UniTask.ToCoroutine(async () =>
        {
            var storage = new GatedStorage();
            var queue = new SaveQueue(storage);
            using var cancellation = new CancellationTokenSource();

            var write = queue.Write("a", "1").AttachExternalCancellation(cancellation.Token);
            cancellation.Cancel();
            var cancelled = false;
            try
            {
                await write;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            storage.Gate.Set();
            await queue.Write("b", "1");

            Assert.IsTrue(cancelled);
            CollectionAssert.AreEqual(new[] { "write a=1", "write b=1" }, storage.Calls.ToArray());
        });

        [Test]
        public void FlushSync_FinishesEveryOperation_BeforeReturning()
        {
            var storage = new GatedStorage();
            var queue = new SaveQueue(storage);

            var first = queue.Write("a", "1"); // on the thread pool, held at the gate
            var second = queue.Write("b", "1");
            var third = queue.Write("c", "1");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(50);
                storage.Gate.Set();
            });

            queue.FlushSync();

            CollectionAssert.AreEqual(new[] { "write a=1", "write b=1", "write c=1" }, storage.Calls.ToArray());
            Assert.AreEqual(UniTaskStatus.Succeeded, first.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, second.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, third.Status);
        }

        /// <summary>Thread-safe storage whose first call waits at a gate, so later requests queue up behind it.</summary>
        private sealed class GatedStorage : ISaveStorage
        {
            public const string Fail = "fail";

            public readonly ManualResetEventSlim Gate = new ManualResetEventSlim(false);
            public readonly ConcurrentQueue<string> Calls = new ConcurrentQueue<string>();
            public bool HoldFirst = true;
            private int _count;

            public bool IsThreadSafe => true;

            public StorageRead Read(string entry)
            {
                Enter();
                Calls.Enqueue($"read {entry}");
                return new StorageRead(null, null);
            }

            public void Write(string entry, string content)
            {
                Enter();
                if (content == Fail)
                    throw new IOException("Disk full.");
                Calls.Enqueue($"write {entry}={content}");
            }

            public void Quarantine(string entry, bool move) => Enter();

            public IReadOnlyList<string> List(string prefix)
            {
                Enter();
                return Array.Empty<string>();
            }

            public void Delete(string entry) => Enter();

            private void Enter()
            {
                if (Interlocked.Increment(ref _count) == 1 && HoldFirst && !Gate.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The test never opened the gate.");
            }
        }
    }
}

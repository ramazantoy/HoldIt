using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AF.Save
{
    /// <summary>
    /// The single writer: runs storage operations one at a time, in the order they were requested. A thread-safe storage runs
    /// on the thread pool and its results are delivered on the main thread; any other storage runs inline on the main thread.
    /// A write still waiting for an entry is replaced by a newer write to that entry, and both callers get that write's result.
    /// Requests come from the main thread.
    /// </summary>
    internal sealed class SaveQueue
    {
        private readonly ISaveStorage _storage;
        private readonly List<Job> _pending = new List<Job>();
        private Job _running;
        private bool _pumping;

        public SaveQueue(ISaveStorage storage) => _storage = storage;

        /// <summary>Completes when the content is stored; throws the storage's exception if it is not.</summary>
        public UniTask Write(string entry, string content)
        {
            // Join the newest pending job for this entry only if it is a write: a read or delete queued after an older write
            // must still see that write, and must not see a newer one.
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Entry != entry)
                    continue;
                if (_pending[i].IsWrite)
                {
                    _pending[i].Content = content;
                    return _pending[i].Task;
                }
                break;
            }
            return Enqueue(new Job(entry, content));
        }

        public UniTask<StorageRead> Read(string entry) => Run(entry, (store, job) => store.Read(job.Entry));

        public UniTask Quarantine(string entry, bool move) => Run<object>(entry, (store, job) =>
        {
            store.Quarantine(job.Entry, move);
            return null;
        });

        public UniTask<IReadOnlyList<string>> List(string prefix) => Run(prefix, (store, job) => store.List(job.Entry));

        public UniTask Delete(string entry) => Run<object>(entry, (store, job) =>
        {
            store.Delete(job.Entry);
            return null;
        });

        /// <summary>
        /// Finishes every queued operation before returning, on the calling (main) thread: for the moment the app is suspended
        /// or quits, when nothing awaited is sure to complete. The operation already running on the thread pool never needs
        /// the main thread, so waiting for it cannot deadlock.
        /// </summary>
        public void FlushSync()
        {
            if (_running != null)
            {
                var job = _running;
                _running = null;
                job.WaitExecuted();
                job.Complete();
            }

            while (_pending.Count > 0)
            {
                var job = _pending[0];
                _pending.RemoveAt(0);
                job.Execute(_storage);
                job.Complete();
            }
        }

        private async UniTask<T> Run<T>(string entry, Func<ISaveStorage, Job, T> operation) =>
            (T)await Enqueue(new Job(entry, (store, job) => operation(store, job)));

        private UniTask<object> Enqueue(Job job)
        {
            _pending.Add(job);
            if (!_pumping)
                Pump().Forget();
            return job.Task;
        }

        private async UniTaskVoid Pump()
        {
            _pumping = true;
            try
            {
                while (_pending.Count > 0)
                {
                    var job = _pending[0];
                    _pending.RemoveAt(0);
                    _running = job;
                    if (_storage.IsThreadSafe)
                        await UniTask.RunOnThreadPool(() => job.Execute(_storage)); // returns to the main thread
                    else
                        job.Execute(_storage);

                    // FlushSync may have completed it already; Complete runs once.
                    if (_running == job)
                        _running = null;
                    job.Complete();
                }
            }
            finally
            {
                _pumping = false;
            }
        }

        private sealed class Job
        {
            public readonly string Entry;
            public readonly bool IsWrite;
            public string Content;

            private readonly Func<ISaveStorage, Job, object> _operation;
            private readonly ManualResetEventSlim _executed = new ManualResetEventSlim();
            private readonly UniTaskCompletionSource<object> _completion = new UniTaskCompletionSource<object>();
            private object _result;
            private Exception _error;
            private bool _completed;

            public Job(string entry, string content)
            {
                Entry = entry;
                Content = content;
                IsWrite = true;
                _operation = (store, job) =>
                {
                    store.Write(job.Entry, job.Content);
                    return null;
                };
            }

            public Job(string entry, Func<ISaveStorage, Job, object> operation)
            {
                Entry = entry;
                _operation = operation;
            }

            public UniTask<object> Task => _completion.Task;

            public void Execute(ISaveStorage storage)
            {
                try
                {
                    _result = _operation(storage, this);
                }
                catch (Exception exception)
                {
                    _error = exception;
                }
                finally
                {
                    _executed.Set();
                }
            }

            public void WaitExecuted() => _executed.Wait();

            /// <summary>Main thread only: callers' continuations run here.</summary>
            public void Complete()
            {
                if (_completed)
                    return;
                _completed = true;
                _executed.Dispose();
                if (_error != null)
                    _completion.TrySetException(_error);
                else
                    _completion.TrySetResult(_result);
            }
        }
    }
}

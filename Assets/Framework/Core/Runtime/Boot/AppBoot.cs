using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace AF.Core
{
    public sealed class AppBoot : IAppBoot, IAsyncStartable
    {
        private readonly IReadOnlyList<IBootStep> _steps;
        private readonly UniTaskCompletionSource _ready = new UniTaskCompletionSource();

        public AppBoot(IReadOnlyList<IBootStep> steps) => _steps = steps;

        public bool IsReady { get; private set; }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            try
            {
                foreach (var step in _steps)
                    await step.Run(cancellation);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // The root scope is being disposed (Play stopped, app quitting); not a boot failure. Waiters get the cancelled
                // token, so their entry points end quietly.
                _ready.TrySetCanceled(cancellation);
                return;
            }
            catch (Exception exception)
            {
                // Logged here once; waiters get the same exception from WaitReady.
                Log.Exception(exception);
                _ready.TrySetException(exception);
                return;
            }

            IsReady = true;
            _ready.TrySetResult();
            Log.Info("App boot ready");
        }

        public UniTask WaitReady(CancellationToken cancellation) => _ready.Task.AttachExternalCancellation(cancellation);
    }
}

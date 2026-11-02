using System;
using AF.Core;
using HoldIt.Input;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Logs the gameplay presses (adb logcat in a Development build) until path drawing (M2) takes them. Presses that start
    /// on the HUD never reach it.
    /// </summary>
    internal sealed class TouchLog : IInitializable, IDisposable
    {
        private readonly ISubscriber<PointerDown> _down;
        private readonly ISubscriber<PointerUp> _up;
        private IDisposable _subscriptions;

        public TouchLog(ISubscriber<PointerDown> down, ISubscriber<PointerUp> up)
        {
            _down = down;
            _up = up;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _down.Subscribe(message => Log.Info($"Touch down {message.Position}")).AddTo(bag);
            _up.Subscribe(message => Log.Info($"Touch up {message.Position}")).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();
    }
}

using System;
using AF.Core;
using HoldIt.Board;
using HoldIt.Input;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Logs the gameplay presses and the tile under them (adb logcat in a Development build) until path drawing (M5) takes
    /// them. Presses that start on the HUD never reach it.
    /// </summary>
    internal sealed class TouchLog : IInitializable, IDisposable
    {
        private readonly ISubscriber<PointerDown> _down;
        private readonly ISubscriber<PointerUp> _up;
        private readonly IBoardPicker _picker;
        private readonly ITileGrid _grid;
        private IDisposable _subscriptions;

        public TouchLog(ISubscriber<PointerDown> down, ISubscriber<PointerUp> up, IBoardPicker picker, ITileGrid grid)
        {
            _down = down;
            _up = up;
            _picker = picker;
            _grid = grid;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            _down.Subscribe(message => Log.Info($"Touch down {message.Position} {Describe(message.Position)}")).AddTo(bag);
            _up.Subscribe(message => Log.Info($"Touch up {message.Position} {Describe(message.Position)}")).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Dispose() => _subscriptions?.Dispose();

        private string Describe(Vector2 position) =>
            _picker.TryPick(position, out var cell) ? $"on {cell} {_grid.At(cell)}" : "off the board";
    }
}

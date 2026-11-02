using System;
using System.Collections.Generic;
using AF.Input;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace HoldIt.Input
{
    /// <summary>
    /// Turns the Gameplay map's Point and Press into <see cref="IPointerInput"/> and its messages. Input callbacks only record
    /// what happened, each press and release with the pointer's position at that moment; <see cref="Tick"/> replays them once
    /// per frame in order, decides whether a press counts (it does not when it starts over the UI) and publishes. A press the
    /// input mode cuts off ends like a release, because Press's canceled always arrives.
    /// </summary>
    internal sealed class PointerInput : IPointerInput, IInitializable, ITickable, IDisposable
    {
        private const string POINT_ACTION = "Gameplay/Point";
        private const string PRESS_ACTION = "Gameplay/Press";

        private readonly InputAction _point;
        private readonly InputAction _press;
        private readonly IUiHitTest _ui;
        private readonly IPublisher<PointerDown> _down;
        private readonly IPublisher<PointerMoved> _moved;
        private readonly IPublisher<PointerUp> _up;

        // Presses and releases since the last Tick, in the order the callbacks came in.
        private readonly Queue<(bool pressed, Vector2 position)> _presses = new Queue<(bool, Vector2)>();
        private Vector2 _latest;
        private IDisposable _subscriptions;

        public bool IsPressed { get; private set; }
        public Vector2 Position { get; private set; }

        public PointerInput(InputActionAsset actions, IUiHitTest ui, IPublisher<PointerDown> down, IPublisher<PointerMoved> moved,
            IPublisher<PointerUp> up)
        {
            _point = actions.FindAction(POINT_ACTION, throwIfNotFound: true);
            _press = actions.FindAction(PRESS_ACTION, throwIfNotFound: true);
            _ui = ui;
            _down = down;
            _moved = moved;
            _up = up;
        }

        public void Initialize()
        {
            var bag = DisposableBag.CreateBuilder();
            // Only performed: the action is canceled when its map is disabled, and its value then reads zero.
            _point.Subscribe(context => _latest = context.ReadValue<Vector2>()).AddTo(bag);
            _press.Subscribe(context => Record(true, context), canceled: context => Record(false, context)).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Tick()
        {
            while (_presses.Count > 0)
            {
                var (pressed, position) = _presses.Dequeue();
                if (pressed)
                    Press(position);
                else
                    Release(position);
            }

            MoveTo(_latest);
        }

        public void Dispose() => _subscriptions?.Dispose();

        // The device's state is written before action callbacks run, so its position is the one of this press or release,
        // whichever order Point and Press come in.
        private void Record(bool pressed, InputAction.CallbackContext context)
        {
            var position = context.control.device is Pointer pointer ? pointer.position.ReadValue() : _latest;
            _latest = position;
            _presses.Enqueue((pressed, position));
        }

        private void Press(Vector2 position)
        {
            if (IsPressed)
                return;

            MoveTo(position);
            if (_ui.IsOverUi(position))
                return; // Started over the UI: ignored until released.

            IsPressed = true;
            _down.Publish(new PointerDown(position));
        }

        private void Release(Vector2 position)
        {
            if (!IsPressed)
                return;

            MoveTo(position);
            IsPressed = false;
            _up.Publish(new PointerUp(position));
        }

        private void MoveTo(Vector2 position)
        {
            if (position == Position)
                return;

            Position = position;
            if (IsPressed)
                _moved.Publish(new PointerMoved(position));
        }
    }
}

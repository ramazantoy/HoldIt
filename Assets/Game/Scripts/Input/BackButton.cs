using System;
using AF.Input;
using MessagePipe;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace HoldIt.Input
{
    /// <summary>
    /// Publishes <see cref="BackPressed"/> when UI/Cancel performs. Root service: every mode the game has keeps the UI map on,
    /// so back works in menus, in gameplay and while paused.
    /// </summary>
    internal sealed class BackButton : IInitializable, IDisposable
    {
        private const string CANCEL_ACTION = "UI/Cancel";

        private readonly InputAction _cancel;
        private readonly IPublisher<BackPressed> _back;
        private IDisposable _subscription;

        public BackButton(InputActionAsset actions, IPublisher<BackPressed> back)
        {
            _cancel = actions.FindAction(CANCEL_ACTION, throwIfNotFound: true);
            _back = back;
        }

        public void Initialize() => _subscription = _cancel.Subscribe(_ => _back.Publish(new BackPressed()));

        public void Dispose() => _subscription?.Dispose();
    }
}

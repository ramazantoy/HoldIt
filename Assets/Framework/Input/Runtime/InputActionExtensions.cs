using System;
using UnityEngine.InputSystem;

namespace AF.Input
{
    public static class InputActionExtensions
    {
        /// <summary>
        /// Adds <paramref name="performed"/> (and <paramref name="canceled"/>) to the action; disposing the result removes them,
        /// so input subscriptions go into a <c>DisposableBag</c> like message subscriptions. Canceled also arrives when the
        /// action's map is disabled by a mode change, so "held" state reset there never sticks.
        /// </summary>
        public static IDisposable Subscribe(this InputAction action, Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (performed == null)
                throw new ArgumentNullException(nameof(performed));

            action.performed += performed;
            if (canceled != null)
                action.canceled += canceled;
            return new Subscription(action, performed, canceled);
        }

        private sealed class Subscription : IDisposable
        {
            private InputAction _action;
            private readonly Action<InputAction.CallbackContext> _performed;
            private readonly Action<InputAction.CallbackContext> _canceled;

            public Subscription(InputAction action, Action<InputAction.CallbackContext> performed, Action<InputAction.CallbackContext> canceled)
            {
                _action = action;
                _performed = performed;
                _canceled = canceled;
            }

            public void Dispose()
            {
                if (_action == null)
                    return;
                _action.performed -= _performed;
                if (_canceled != null)
                    _action.canceled -= _canceled;
                _action = null;
            }
        }
    }
}

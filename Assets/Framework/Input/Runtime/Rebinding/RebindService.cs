using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace AF.Input
{
    public sealed class RebindService : IRebindService, IDisposable
    {
        /// <summary>No map enabled while the player picks a control, so the control triggers nothing.</summary>
        internal static readonly InputMode RebindingMode = new InputMode("rebinding", CursorState.Free);

        private readonly InputActionAsset _actions;
        private readonly IInputModeService _modes;
        private readonly RebindOptions _options;
        private readonly IPublisher<BindingsChanged> _changed;
        private UniTaskCompletionSource<RebindResult> _running;
        private bool _disposed;

        public RebindService(InputActionAsset actions, IInputModeService modes, InputOptions options, IPublisher<BindingsChanged> changed)
        {
            _actions = actions;
            _modes = modes;
            _options = options.Rebind ?? new RebindOptions();
            _changed = changed;
        }

        public bool IsRebinding { get; private set; }

        // Not async itself, so a wrong argument throws at the call instead of inside the returned task.
        public UniTask<RebindResult> Rebind(InputAction action, int bindingIndex, CancellationToken cancellation = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RebindService));
            CheckBinding(action, bindingIndex);
            if (action.bindings[bindingIndex].isComposite)
                throw new ArgumentException($"Binding {bindingIndex} of {action} is a composite; rebind each of its parts.", nameof(bindingIndex));
            if (IsRebinding)
                throw new InvalidOperationException("Another rebind is still running.");
            if (cancellation.IsCancellationRequested)
                return UniTask.FromCanceled<RebindResult>(cancellation);

            IsRebinding = true;
            return Run(action, bindingIndex, cancellation);
        }

        private async UniTask<RebindResult> Run(InputAction action, int bindingIndex, CancellationToken cancellation)
        {
            var completion = _running = new UniTaskCompletionSource<RebindResult>();
            var cancelPressed = false;
            IDisposable mode = null;
            InputActionRebindingExtensions.RebindingOperation operation = null;
            IDisposable cancelControls = null;
            Action cancelAfterUpdate = null;
            var registration = default(CancellationTokenRegistration);
            RebindResult result;
            try
            {
                mode = _modes.Request(RebindingMode);
                operation = action.PerformInteractiveRebinding(bindingIndex);

                // Cancel controls are excluded from the candidates and watched separately, so they cancel whatever the scheme.
                foreach (var path in _options.CancelPaths.Concat(_options.ExcludedPaths))
                    operation.WithControlsExcluding(path);
                foreach (var path in DevicePathsOf(action.bindings[bindingIndex]))
                    operation.WithControlsHavingToMatchPath(path);
                if (_options.TimeoutSeconds > 0f)
                    operation.WithTimeout(_options.TimeoutSeconds);
                operation
                    .OnComplete(_ => completion.TrySetResult(RebindResult.Completed))
                    .OnCancel(_ => completion.TrySetResult(cancelPressed ? RebindResult.Canceled : RebindResult.TimedOut));

                // onAnyButtonPress sees an event before the device state is written. Cancelling there would bring the maps
                // back while the cancel control still reads as released, and the same press would then perform an action
                // bound to it (UI/Cancel closing the settings screen). Cancelling after the update finds it held instead, and
                // the input mode service keeps such an action off until the control is released.
                cancelControls = InputSystem.onAnyButtonPress.Call(control =>
                {
                    if (_options.CancelPaths.Any(path => InputControlPath.Matches(path, control)))
                        cancelPressed = true;
                });
                cancelAfterUpdate = () =>
                {
                    if (cancelPressed)
                        operation.Cancel();
                };
                InputSystem.onAfterUpdate += cancelAfterUpdate;

                // May run on another thread (CancelAfter's timer); the cleanup below returns to the main thread first.
                registration = cancellation.Register(() => completion.TrySetCanceled(cancellation));
                operation.Start();

                result = await completion.Task;
            }
            finally
            {
                // Completing, cancelling and timing out all run inside the operation's own callback on the main thread, so this
                // continues at once, as in Unity's rebinding sample.
                await UniTask.SwitchToMainThread();
                registration.Dispose();
                if (cancelAfterUpdate != null)
                    InputSystem.onAfterUpdate -= cancelAfterUpdate;
                cancelControls?.Dispose();
                if (operation != null)
                {
                    // Still waiting after a cancelled token or Dispose: stop it before letting go of it.
                    if (operation.started && !operation.completed && !operation.canceled)
                        operation.Cancel();
                    operation.Dispose();
                }
                mode?.Dispose();
                _running = null;
                IsRebinding = false;
            }

            // After the cleanup, so a subscriber can start the next rebind (the next part of a composite).
            if (result == RebindResult.Completed)
                _changed.Publish(new BindingsChanged(action));
            return result;
        }

        /// <summary>Ends a rebind still waiting for input; its task is cancelled.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _running?.TrySetCanceled();
        }

        public void Reset(InputAction action, int bindingIndex)
        {
            CheckBinding(action, bindingIndex);
            action.RemoveBindingOverride(bindingIndex);
            if (action.bindings[bindingIndex].isComposite)
            {
                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; i++)
                    action.RemoveBindingOverride(i);
            }
            _changed.Publish(new BindingsChanged(action));
        }

        public void ResetAll()
        {
            _actions.RemoveAllBindingOverrides();
            _changed.Publish(new BindingsChanged(null));
        }

        private void CheckBinding(InputAction action, int bindingIndex)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (action.actionMap?.asset != _actions)
                throw new ArgumentException($"{action} does not belong to {_actions.name}, the asset the input mode service owns.", nameof(action));
            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                throw new ArgumentOutOfRangeException(nameof(bindingIndex), bindingIndex, $"{action} has {action.bindings.Count} bindings.");
        }

        /// <summary>Device paths of the control schemes the binding belongs to; empty when it belongs to none.</summary>
        private IEnumerable<string> DevicePathsOf(InputBinding binding)
        {
            if (string.IsNullOrEmpty(binding.groups))
                yield break;
            foreach (var group in binding.groups.Split(InputBinding.Separator))
            {
                foreach (var scheme in _actions.controlSchemes)
                {
                    if (!string.Equals(scheme.bindingGroup, group, StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (var requirement in scheme.deviceRequirements)
                        yield return requirement.controlPath;
                }
            }
        }
    }
}

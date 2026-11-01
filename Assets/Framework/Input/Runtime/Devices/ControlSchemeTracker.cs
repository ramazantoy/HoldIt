using System;
using System.Linq;
using MessagePipe;
using UnityEngine.InputSystem;
using VContainer;

namespace AF.Input
{
    public sealed class ControlSchemeTracker : IControlSchemeTracker, IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly IPublisher<ControlSchemeChanged> _changed;
        private readonly Func<InputDevice, bool> _counts;
        private readonly Action<object, InputActionChange> _onActionChange;

        [Inject]
        public ControlSchemeTracker(InputActionAsset actions, IPublisher<ControlSchemeChanged> changed)
            : this(actions, changed, device => device.native)
        {
        }

        // Tests count the devices they add themselves, which the Input System marks as created by script.
        internal ControlSchemeTracker(InputActionAsset actions, IPublisher<ControlSchemeChanged> changed, Func<InputDevice, bool> counts)
        {
            _actions = actions;
            _changed = changed;
            _counts = counts;

            var devices = InputSystem.devices.Where(counts).ToList();
            foreach (var scheme in actions.controlSchemes)
            {
                using var match = scheme.PickDevicesFrom(devices);
                if (!match.isSuccessfulMatch)
                    continue;
                Set(scheme);
                break;
            }

            _onActionChange = OnActionChange;
            InputSystem.onActionChange += _onActionChange;
        }

        public string Current { get; private set; }
        public InputDevice Device { get; private set; }
        public bool UsesMouse { get; private set; } = true;

        public void Dispose() => InputSystem.onActionChange -= _onActionChange;

        private void OnActionChange(object target, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || target is not InputAction action || action.actionMap?.asset != _actions)
                return;

            // Pointer actions perform on every mouse move; only a different device can change the scheme.
            var device = action.activeControl?.device;
            if (device == null || device == Device || !_counts(device))
                return;
            Device = device;

            // With the other connected devices: a scheme that needs a keyboard and a mouse matches neither of them alone.
            var connected = InputSystem.devices.Where(_counts).ToList();
            var scheme = InputControlScheme.FindControlSchemeForDevices(connected, _actions.controlSchemes, mustIncludeDevice: device);
            if (scheme == null || scheme.Value.name == Current)
                return;

            var previous = Current;
            Set(scheme.Value);
            _changed.Publish(new ControlSchemeChanged(previous, Current));
        }

        private void Set(InputControlScheme scheme)
        {
            Current = scheme.name;
            UsesMouse = scheme.deviceRequirements.Any(requirement =>
            {
                var layout = InputControlPath.TryGetDeviceLayout(requirement.controlPath);
                return layout != null && InputSystem.IsFirstLayoutBasedOnSecond(layout, "Mouse");
            });
        }
    }
}

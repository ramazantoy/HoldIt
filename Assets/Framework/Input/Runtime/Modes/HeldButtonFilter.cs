using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace AF.Input
{
    /// <summary>
    /// Keeps a Button action disabled while a control it is bound to is still held from before its map was enabled, and enables
    /// it once every such control is released.
    /// </summary>
    /// <remarks>
    /// A Button action does no initial state check: enabled while its control is held, it waits, and the next change of that
    /// control that is past the press point performs it. A key or a mouse button only changes back to 0, so it never leaks. An
    /// analog trigger does: released slowly through the press point, it performs the action in the map that the press itself
    /// just switched to. Value actions are left alone, as their held value is meant to carry over (Unity checks their initial
    /// state), and so are pass-through actions, whose pointer controls are never at rest.
    /// </remarks>
    internal sealed class HeldButtonFilter : IDisposable
    {
        private readonly List<InputAction> _held = new List<InputAction>();
        private readonly Action _onAfterUpdate;
        private bool _hooked;

        public HeldButtonFilter() => _onAfterUpdate = OnAfterUpdate;

        public bool Contains(InputAction action) => _held.Contains(action);

        /// <summary>True when <paramref name="action"/> is a Button with a bound control pressed past its press point.</summary>
        public static bool IsHeld(InputAction action)
        {
            if (action.type != InputActionType.Button)
                return false;
            foreach (var control in action.controls)
            {
                if (control.EvaluateMagnitude() >= PressPoint(control))
                    return true;
            }
            return false;
        }

        /// <summary>Keeps the disabled action off until its controls are released.</summary>
        public void Hold(InputAction action)
        {
            _held.Add(action);
            if (!_hooked)
            {
                InputSystem.onAfterUpdate += _onAfterUpdate;
                _hooked = true;
            }
        }

        /// <summary>The map is disabled: its actions are no longer waiting to be enabled.</summary>
        public void Forget(InputActionMap map) => _held.RemoveAll(action => action.actionMap == map);

        public void Dispose()
        {
            _held.Clear();
            Unhook();
        }

        private void OnAfterUpdate()
        {
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                var action = _held[i];
                if (IsAnyControlPastReleasePoint(action))
                    continue;
                _held.RemoveAt(i);
                action.Enable();
            }
            if (_held.Count == 0)
                Unhook();
        }

        private void Unhook()
        {
            if (!_hooked)
                return;
            InputSystem.onAfterUpdate -= _onAfterUpdate;
            _hooked = false;
        }

        private static bool IsAnyControlPastReleasePoint(InputAction action)
        {
            var releasePoint = InputSystem.settings.buttonReleaseThreshold;
            foreach (var control in action.controls)
            {
                if (control.EvaluateMagnitude() >= PressPoint(control) * releasePoint)
                    return true;
            }
            return false;
        }

        private static float PressPoint(InputControl control) =>
            control is ButtonControl button ? button.pressPointOrDefault : InputSystem.settings.defaultButtonPressPoint;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Core;
using UnityEngine.InputSystem;

namespace AF.Input
{
    /// <summary>
    /// Finds actions whose enabled state differs from what the input mode service left: something enabled or disabled them
    /// behind its back (code, a PlayerInput, an InputSystemUIInputModule switched off and on again).
    /// </summary>
    internal static class InputModeAudit
    {
        /// <summary>Logs every difference since the last time the service applied <paramref name="applied"/>.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Check(InputActionAsset actions, InputMode applied, Func<InputAction, bool> isHeld)
        {
            foreach (var problem in FindDrift(actions, applied, isHeld))
                Log.Error(problem, actions);
        }

        public static List<string> FindDrift(InputActionAsset actions, InputMode applied, Func<InputAction, bool> isHeld)
        {
            var problems = new List<string>();
            var enabled = new List<string>();
            var disabled = new List<string>();
            foreach (var map in actions.actionMaps)
            {
                enabled.Clear();
                disabled.Clear();
                var mapOn = applied.Enables(map.name);
                foreach (var action in map.actions)
                {
                    var expected = mapOn && !isHeld(action);
                    if (action.enabled != expected)
                        (action.enabled ? enabled : disabled).Add(action.name);
                }
                if (enabled.Count > 0)
                    problems.Add(Describe(map, enabled, "enabled", applied));
                if (disabled.Count > 0)
                    problems.Add(Describe(map, disabled, "disabled", applied));
            }
            return problems;
        }

        private static string Describe(InputActionMap map, List<string> actions, string change, InputMode applied) =>
            $"Input: {map.name}/{{{string.Join(", ", actions)}}} {change} outside the input mode service while the mode was " +
            $"'{applied}'. Request a mode from IInputModeService instead of enabling or disabling maps or actions, and never switch " +
            "the InputSystemUIInputModule off and on: it enables its actions again.";
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Core.Editor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AF.Input.Editor
{
    /// <summary>
    /// Finds project code that enables or disables input actions or maps, switches a PlayerInput's map, or writes the cursor:
    /// all of that belongs to the input mode service. Project assemblies only (Assets/), tests, editor code and this module
    /// excluded; code marked <see cref="InputModeExemptAttribute"/> is skipped. What it cannot see (package code, reflection) is
    /// caught when the service next applies a mode, in the Editor and development builds.
    /// </summary>
    public sealed class DirectInputToggleValidator : IValidator
    {
        private const string ADVICE = "The input mode service is the only owner of action maps and the cursor: request a mode from " +
                                      "IInputModeService instead. Mark a deliberate exception, such as a standalone debug action, " +
                                      "with [InputModeExempt(\"reason\")].";

        public void ValidateProject(ValidationReport report) =>
            Check(DirectCallScan.ProjectTypes(typeof(InputModeService).Assembly), report);

        internal static void Check(IEnumerable<Type> types, ValidationReport report) =>
            DirectCallScan.Check(types, typeof(InputModeExemptAttribute), Describe, ADVICE, report);

        /// <summary>The call as reported, or null when it is allowed.</summary>
        private static string Describe(MethodBase target)
        {
            var type = target.DeclaringType;
            var name = target.Name;
            if ((type == typeof(InputAction) || type == typeof(InputActionMap) || type == typeof(InputActionAsset) ||
                 type == typeof(IInputActionCollection)) && (name == "Enable" || name == "Disable"))
                return $"{type.Name}.{name}()";
            if (type == typeof(PlayerInput) && (name == "SwitchCurrentActionMap" || name == "ActivateInput" || name == "DeactivateInput"))
                return $"PlayerInput.{name}()";
            if (type == typeof(Cursor) && (name == "set_lockState" || name == "set_visible"))
                return $"Cursor.{name.Substring(4)}";
            return null;
        }
    }
}

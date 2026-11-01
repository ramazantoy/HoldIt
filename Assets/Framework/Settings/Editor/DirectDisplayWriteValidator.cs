using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Core.Editor;
using AF.Input;
using UnityEngine;

namespace AF.Settings.Editor
{
    /// <summary>
    /// Finds project code that writes the screen resolution or fullscreen mode, the quality level, vsync, the target frame
    /// rate or the cursor confinement: all of that belongs to <see cref="IDisplaySettings"/>, which saves it and applies it
    /// in the right order. Project
    /// assemblies only (Assets/), tests, editor code and this module excluded; code marked
    /// <see cref="DisplaySettingsExemptAttribute"/> is skipped.
    /// </summary>
    public sealed class DirectDisplayWriteValidator : IValidator
    {
        private const string ADVICE = "IDisplaySettings is the only writer of the screen, quality, frame rate and cursor confinement settings: a direct " +
                                      "write is undone at the next apply and never saved. Mark a deliberate exception with " +
                                      "[DisplaySettingsExempt(\"reason\")].";

        public void ValidateProject(ValidationReport report) =>
            Check(DirectCallScan.ProjectTypes(typeof(DisplaySettings).Assembly), report);

        internal static void Check(IEnumerable<Type> types, ValidationReport report) =>
            DirectCallScan.Check(types, typeof(DisplaySettingsExemptAttribute), Describe, ADVICE, report);

        /// <summary>The call as reported, or null when it is allowed.</summary>
        private static string Describe(MethodBase target)
        {
            var type = target.DeclaringType;
            var name = target.Name;
            if (type == typeof(Screen) && (name == "SetResolution" || name == "set_fullScreen" || name == "set_fullScreenMode"))
                return name == "SetResolution" ? "Screen.SetResolution()" : $"Screen.{name.Substring(4)}";
            if (type == typeof(QualitySettings) &&
                (name == "SetQualityLevel" || name == "IncreaseLevel" || name == "DecreaseLevel" || name == "set_currentLevel"))
                return name == "set_currentLevel" ? "QualitySettings.currentLevel" : $"QualitySettings.{name}()";
            if (type == typeof(QualitySettings) && name == "set_vSyncCount")
                return "QualitySettings.vSyncCount";
            if (type == typeof(Application) && name == "set_targetFrameRate")
                return "Application.targetFrameRate";
            if ((type == typeof(IInputModeService) || type == typeof(InputModeService)) && name == "set_ConfineCursor")
                return "IInputModeService.ConfineCursor";
            return null;
        }
    }
}

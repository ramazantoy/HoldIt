using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace AF.Core.Editor
{
    /// <summary>
    /// Finds project code that loads or unloads scenes through Unity's <see cref="SceneManager"/>: scene changes belong to
    /// <see cref="ISceneLoader"/>. Project assemblies only (Assets/), tests and editor code excluded; code marked
    /// <see cref="SceneLoadExemptAttribute"/> is skipped.
    /// </summary>
    public sealed class DirectSceneLoadValidator : IValidator
    {
        private const string ADVICE = "Scene changes go through ISceneLoader, which blocks input, shows the loading screen and " +
                                      "unloads the old scenes before the new one is built. Mark a deliberate exception with " +
                                      "[SceneLoadExempt(\"reason\")].";

        public void ValidateProject(ValidationReport report) => Check(DirectCallScan.ProjectTypes(), report);

        internal static void Check(IEnumerable<Type> types, ValidationReport report) =>
            DirectCallScan.Check(types, typeof(SceneLoadExemptAttribute), Describe, ADVICE, report);

        /// <summary>The call as reported, or null when it is allowed.</summary>
        private static string Describe(MethodBase target)
        {
            if (target.DeclaringType != typeof(SceneManager))
                return null;
            var name = target.Name;
            return name == "LoadScene" || name == "LoadSceneAsync" || name == "UnloadScene" || name == "UnloadSceneAsync"
                ? $"SceneManager.{name}()"
                : null;
        }
    }
}

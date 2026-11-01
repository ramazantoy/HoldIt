using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AF.Core.Editor;
using UnityEditor;

namespace AF.Build.Editor
{
    /// <summary>
    /// The pre-build rules that read only the environment snapshot. Errors refuse the build; warnings go into its report.
    /// Validate All and the serialized layout check run next to these in <see cref="Builder.Check"/>.
    /// </summary>
    public static class BuildChecks
    {
        /// <summary>Unity's defaults and the template's own names. A game shipped with them shares its save folder with every other.</summary>
        public static readonly IReadOnlyList<string> PlaceholderCompanies = new[] { "DefaultCompany" };

        public static readonly IReadOnlyList<string> PlaceholderProducts = new[] { "TemplateProject", "My project", "New Unity Project" };

        public static readonly IReadOnlyList<string> PlaceholderIdentifierParts = new[] { "com.unity.template", "DefaultCompany", "com.Company.ProductName" };

        /// <summary>
        /// The one Player Settings define allowed. The Pipeline package's "Enable in Development &amp; Profiled Builds" toggle
        /// writes it; its assemblies also require a development-only symbol, so it can never reach a Release build.
        /// </summary>
        public static readonly IReadOnlyList<string> AllowedPlayerSettingsDefines = new[] { "ENABLE_RUNTIME_PIPELINE_DEV" };

        /// <summary>The Pipeline's runtime assembly: a local HTTP server that runs code sent to it.</summary>
        public const string PipelineRuntimeAssembly = "Unity.Pipeline.dll";

        private const int ListedDirtyAssets = 10;

        /// <summary>
        /// Rules on the player assemblies compiled with the build's own settings: a Release that would contain the Pipeline
        /// runtime is refused before it is built, on every platform, packages included.
        /// </summary>
        public static void EvaluatePlayerAssemblies(BuildRequest request, IReadOnlyCollection<string> playerAssemblies, ValidationReport report)
        {
            if (request.Kind == BuildKind.Release && playerAssemblies.Any(assembly => string.Equals(Path.GetFileName(assembly), PipelineRuntimeAssembly, StringComparison.OrdinalIgnoreCase)))
                report.Error("The Pipeline runtime compiles into this Release build: it carries a local HTTP server that runs code. " +
                             "Turn off Project Settings > Pipeline > Runtime > Enable in Release Builds.");
        }

        public static void Evaluate(BuildRequest request, BuildEnvironment environment, ValidationReport report)
        {
            var release = request.Kind == BuildKind.Release;

            if (environment.PlayingOrCompiling)
                report.Error("Builds run in Edit Mode with scripts compiled. Leave Play Mode and wait for compilation.");

            if (!BuildPaths.IsSupported(environment.Target))
                report.Error($"The active build target {environment.Target} is not supported by the build menu (Windows, macOS, Linux, Android, iOS).");
            else if (!environment.TargetInstalled)
                report.Error($"The {environment.Target} build support module is not installed. Add it in Unity Hub.");

            if (environment.EnabledSceneCount == 0)
                report.Error("Build Settings has no enabled scene.");

            foreach (var scene in environment.UnsavedScenes)
                report.Error($"{scene} has unsaved changes. The build uses the saved file while Validate All checked the open one; save or revert it first.");

            CheckIdentity(request, environment, report);
            CheckDefines(environment, report);

            if (environment.ActiveBuildProfile != null)
                report.Error($"The custom Build Profile {environment.ActiveBuildProfile} is active. Its scenes, defines and Player Settings " +
                             "overrides would bypass the build menu; switch back to the platform profile in the Build Profiles window.");

            if (environment.UsesAddressables && !environment.AddressablesSettingsExist)
                report.Error("The Addressables package is installed but has no settings, so there is no content to build. Create them in " +
                             "Window > Asset Management > Addressables > Groups.");
            else if (environment.UsesAddressables && environment.AddressablesBuildWithPlayer)
                report.Error("Addressables is set to build its content inside the player build. The build menu builds it itself, " +
                             "before the previous build is deleted, and reports it: set Build Addressables on Player Build to " +
                             "\"Do not Build Addressables content on Player build\" in the Addressables settings.");

            if (environment.DirtyAssets.Count > 0)
            {
                var listed = string.Join(", ", environment.DirtyAssets.Take(ListedDirtyAssets));
                var more = environment.DirtyAssets.Count > ListedDirtyAssets ? $" and {environment.DirtyAssets.Count - ListedDirtyAssets} more" : string.Empty;
                report.Warning($"Unsaved asset changes will be written to disk when the build starts and go into the build: {listed}{more}. " +
                               "Changes made in Play Mode (material keywords, ScriptableObject values) end up here.");
            }

            if (release && environment.Backend != ScriptingImplementation.IL2CPP)
                report.Warning($"Release build with the {environment.Backend} backend. The framework is verified on IL2CPP.");

            if (environment.Stripping is ManagedStrippingLevel.Low or ManagedStrippingLevel.Medium or ManagedStrippingLevel.High)
                report.Warning($"Managed stripping is {environment.Stripping}. The framework is verified at Minimal; code reached only by " +
                               "reflection (injected constructors, save data classes) needs [Preserve] or link.xml. See Build.md.");

            if (release && environment.GitStatus == null)
                report.Warning("The working tree state is unknown (git is missing or its status failed); the report cannot tell whether a commit reproduces this build.");
            else if (release && environment.GitStatus.Count > 0)
                report.Warning($"Release build from a working tree with {environment.GitStatus.Count} uncommitted change(s); the commit alone does not reproduce it.");
        }

        private static void CheckIdentity(BuildRequest request, BuildEnvironment environment, ValidationReport report)
        {
            var problems = new List<string>();
            if (PlaceholderCompanies.Contains(environment.CompanyName))
                problems.Add($"Company '{environment.CompanyName}'");
            if (PlaceholderProducts.Contains(environment.ProductName))
                problems.Add($"Product '{environment.ProductName}'");
            if (PlaceholderIdentifierParts.Any(part => environment.ApplicationIdentifier.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0))
                problems.Add($"identifier '{environment.ApplicationIdentifier}'");
            if (problems.Count == 0)
                return;

            var message = $"Placeholder names: {string.Join(", ", problems)}. Set them in Player Settings: persistentDataPath and the " +
                          "store identity come from them, and games with the same names share save data.";
            if (request.Kind == BuildKind.Release && !request.AllowPlaceholderIdentity)
                report.Error(message);
            else if (request.Kind == BuildKind.Release)
                report.Warning(message + " Allowed for this build by request (template verification only).");
            else
                report.Warning(message);
        }

        private static void CheckDefines(BuildEnvironment environment, ValidationReport report)
        {
            foreach (var entry in environment.PlayerSettingsDefines)
            {
                var leaked = entry.Value.Split(';')
                    .Select(symbol => symbol.Trim())
                    .Where(symbol => symbol.Length > 0 && !AllowedPlayerSettingsDefines.Contains(symbol))
                    .ToList();
                if (leaked.Count > 0)
                    report.Error($"Player Settings > {entry.Key} defines {string.Join(";", leaked)}. Defines come only from the build menu " +
                                 "(extraScriptingDefines); one left in Player Settings goes into every build. Remove it.");
            }

            foreach (var entry in environment.CompilerArguments)
            {
                var defining = entry.Value.Where(BuildEnvironment.DefinesSymbol).ToList();
                if (defining.Count > 0)
                    report.Error($"Player Settings > {entry.Key} > Additional Compiler Arguments define symbols ({string.Join(" ", defining)}). Defines come only from the build menu.");
            }

            foreach (var file in environment.DefiningResponseFiles)
                report.Error($"{file} defines symbols. Defines come only from the build menu; remove the -define lines.");
        }
    }
}

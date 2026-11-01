using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace AF.Core.Editor
{
    /// <summary>
    /// The root scope exists (VContainerSettings in Preloaded Assets with a RootScope prefab) and every scene has exactly
    /// one SceneScope attached to it.
    /// </summary>
    public sealed class ScopeSetupValidator : IValidator
    {
        public void ValidateProject(ValidationReport report) =>
            CheckSettings(PlayerSettings.GetPreloadedAssets().OfType<VContainerSettings>().ToList(), report);

        /// <summary>Checks the VContainerSettings found in Preloaded Assets.</summary>
        internal static void CheckSettings(IReadOnlyList<VContainerSettings> settings, ValidationReport report)
        {
            if (settings.Count == 0)
            {
                report.Error("No VContainerSettings in Player Settings > Preloaded Assets, so the root scope is never created.");
                return;
            }
            if (settings.Count > 1)
                report.Error("Preloaded Assets contains more than one VContainerSettings; VContainer uses only one of them.");

            var root = settings[0].RootLifetimeScope;
            var settingsPath = AssetDatabase.GetAssetPath(settings[0]);
            if (root == null)
                report.Error($"{settingsPath}: Root Lifetime Scope is empty.", settings[0]);
            else if (root is not RootScope)
                report.Error($"{settingsPath}: the root prefab {root.name} uses {root.GetType().Name}, which does not derive from RootScope.", root);
        }

        public void ValidateScene(Scene scene, ValidationReport report) =>
            CheckScene(ValidationTargets.FindInScene<SceneScope>(scene).ToList(), ValidationTargets.FindInScene<RootScope>(scene),
                scene.path, report);

        /// <summary>Checks the scopes found in one scene.</summary>
        internal static void CheckScene(IReadOnlyList<SceneScope> sceneScopes, IEnumerable<RootScope> rootScopes, string location,
            ValidationReport report)
        {
            if (sceneScopes.Count == 0)
                report.Error($"{location}: no SceneScope. Scene objects are not injected and the scene's boot target never applies.");
            else if (sceneScopes.Count > 1)
                report.Error($"{location}: {sceneScopes.Count} SceneScopes; a scene has exactly one.", sceneScopes[1]);

            foreach (var scope in sceneScopes)
            {
                if (!string.IsNullOrEmpty(scope.parentReference.TypeName))
                    report.Error($"{ValidationTargets.Describe(location, scope)}: Parent must be empty so the scope attaches to the root scope.", scope);
                if (!scope.autoRun)
                    report.Error($"{ValidationTargets.Describe(location, scope)}: Auto Run is off, so the scope is never built.", scope);
            }

            foreach (var rootScope in rootScopes)
                report.Error($"{ValidationTargets.Describe(location, rootScope)}: a RootScope belongs on the root prefab, not in a scene.", rootScope);
        }
    }
}

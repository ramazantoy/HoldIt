using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AF.Build.Editor
{
    /// <summary>
    /// What the pre-build rules look at, read from the Editor once. The rules only read this snapshot, so tests hand them a
    /// broken setup without touching the project's settings.
    /// </summary>
    public sealed class BuildEnvironment
    {
        private static readonly NamedBuildTarget[] _definedTargets =
        {
            NamedBuildTarget.Standalone, NamedBuildTarget.Server, NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.WebGL,
        };

        public BuildTarget Target { get; set; }
        public bool TargetInstalled { get; set; } = true;
        public bool PlayingOrCompiling { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ApplicationIdentifier { get; set; } = string.Empty;

        /// <summary>Player Settings scripting defines by target name; targets without defines are left out.</summary>
        public IReadOnlyDictionary<string, string> PlayerSettingsDefines { get; set; } = new Dictionary<string, string>();

        /// <summary>Additional compiler arguments by target name; targets without arguments are left out.</summary>
        public IReadOnlyDictionary<string, string[]> CompilerArguments { get; set; } = new Dictionary<string, string[]>();

        /// <summary><c>csc.rsp</c>/<c>mcs.rsp</c> files under Assets that define symbols.</summary>
        public IReadOnlyList<string> DefiningResponseFiles { get; set; } = Array.Empty<string>();

        /// <summary>Path of the active custom Build Profile; null while the classic platform profile is active.</summary>
        public string ActiveBuildProfile { get; set; }

        public IReadOnlyList<string> UnsavedScenes { get; set; } = Array.Empty<string>();

        /// <summary>Assets and project settings with unsaved changes. The build saves them to disk first.</summary>
        public IReadOnlyList<string> DirtyAssets { get; set; } = Array.Empty<string>();

        public int EnabledSceneCount { get; set; } = 1;
        public ScriptingImplementation Backend { get; set; } = ScriptingImplementation.IL2CPP;
        public ManagedStrippingLevel Stripping { get; set; } = ManagedStrippingLevel.Minimal;

        /// <summary><c>git status --porcelain</c> lines; null when git is unavailable.</summary>
        public IReadOnlyList<string> GitStatus { get; set; } = Array.Empty<string>();

        /// <summary>The Addressables package is installed: the build menu builds its content before the player.</summary>
        public bool UsesAddressables { get; set; }

        public bool AddressablesSettingsExist { get; set; } = true;

        /// <summary>Addressables would also build its content inside the player build (any setting but "do not build").</summary>
        public bool AddressablesBuildWithPlayer { get; set; }

        public static BuildEnvironment Capture(BuildTarget target)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            return new BuildEnvironment
            {
                Target = target,
                TargetInstalled = BuildPipeline.IsBuildTargetSupported(group, target),
                PlayingOrCompiling = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling,
                CompanyName = PlayerSettings.companyName,
                ProductName = PlayerSettings.productName,
                ApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(named),
                PlayerSettingsDefines = _definedTargets
                    .Select(definedTarget => (definedTarget.TargetName, PlayerSettings.GetScriptingDefineSymbols(definedTarget)))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Item2))
                    .ToDictionary(entry => entry.TargetName, entry => entry.Item2),
                CompilerArguments = _definedTargets
                    .Select(definedTarget => (definedTarget.TargetName, PlayerSettings.GetAdditionalCompilerArguments(definedTarget)))
                    .Where(entry => entry.Item2 != null && entry.Item2.Length > 0)
                    .ToDictionary(entry => entry.TargetName, entry => entry.Item2),
                DefiningResponseFiles = FindDefiningResponseFiles(),
                ActiveBuildProfile = activeProfile != null ? AssetDatabase.GetAssetPath(activeProfile) : null,
                UnsavedScenes = FindUnsavedScenes(),
                DirtyAssets = FindDirtyAssets(),
                EnabledSceneCount = EditorBuildSettings.scenes.Count(scene => scene.enabled),
                Backend = PlayerSettings.GetScriptingBackend(named),
                Stripping = PlayerSettings.GetManagedStrippingLevel(named),
                GitStatus = Git.Status(),
                UsesAddressables = AddressablesContent.Installed,
                AddressablesSettingsExist = AddressablesContent.SettingsExist,
                AddressablesBuildWithPlayer = AddressablesContent.BuildsWithPlayer,
            };
        }

        /// <summary>True for a compiler argument or response file line with an option that defines a symbol.</summary>
        public static bool DefinesSymbol(string argument) =>
            argument.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Any(option =>
                option.StartsWith("-define:", StringComparison.OrdinalIgnoreCase) ||
                option.StartsWith("/define:", StringComparison.OrdinalIgnoreCase) ||
                option.StartsWith("-d:", StringComparison.OrdinalIgnoreCase) ||
                option.StartsWith("/d:", StringComparison.OrdinalIgnoreCase));

        private static List<string> FindDefiningResponseFiles()
        {
            var assets = Application.dataPath;
            return new[] { "csc.rsp", "mcs.rsp" }
                .SelectMany(name => Directory.GetFiles(assets, name, SearchOption.AllDirectories))
                .Where(file => File.ReadAllLines(file).Any(DefinesSymbol))
                .Select(file => "Assets" + file.Substring(assets.Length).Replace('\\', '/'))
                .ToList();
        }

        private static List<string> FindUnsavedScenes()
        {
            var unsaved = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    unsaved.Add(string.IsNullOrEmpty(scene.path) ? "(untitled scene)" : scene.path);
            }
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene.isDirty)
                unsaved.Add(prefabStage.assetPath + " (prefab mode)");
            return unsaved;
        }

        private static List<string> FindDirtyAssets() =>
            Resources.FindObjectsOfTypeAll<Object>()
                .Where(asset => EditorUtility.IsPersistent(asset) && EditorUtility.IsDirty(asset))
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("ProjectSettings/", StringComparison.Ordinal))
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
    }
}

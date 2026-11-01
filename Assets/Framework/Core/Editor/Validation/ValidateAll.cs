using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace AF.Core.Editor
{
    /// <summary>Runs every <see cref="IValidator"/> over the project settings, the assets and the enabled build scenes.</summary>
    public static class ValidateAll
    {
        private const string TITLE = "Validate All";

        [MenuItem(FrameworkConstants.MenuRoot + TITLE)]
        private static void RunFromMenu() => Run().LogToConsole(TITLE);

        /// <summary>
        /// Checks the project and every enabled build scene. Closed scenes are opened additively and closed again;
        /// open scenes are checked as they are in memory, unsaved changes included. Edit Mode only.
        /// </summary>
        public static ValidationReport Run()
        {
            var report = new ValidationReport();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.Error($"{TITLE} runs in Edit Mode only.");
                return report;
            }

            var validators = CreateValidators();
            foreach (var validator in validators)
            {
                validator.ValidateProject(report);
                validator.ValidateAssets(report);
            }

            foreach (var buildScene in EditorBuildSettings.scenes.Where(buildScene => buildScene.enabled))
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScene.path) == null)
                {
                    report.Error($"Build Settings lists {buildScene.path}, but the scene file does not exist.");
                    continue;
                }

                var scene = SceneManager.GetSceneByPath(buildScene.path);
                var inHierarchy = scene.IsValid();
                var wasLoaded = inHierarchy && scene.isLoaded;
                if (!wasLoaded)
                    scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Additive);

                try
                {
                    foreach (var validator in validators)
                        validator.ValidateScene(scene, report);
                }
                finally
                {
                    if (!wasLoaded)
                        EditorSceneManager.CloseScene(scene, removeScene: !inHierarchy);
                }
            }
            return report;
        }

        /// <summary>
        /// Checks the project and the scenes that are currently loaded, without opening anything. Skips the slow asset checks
        /// (<see cref="IValidator.ValidateAssets"/>).
        /// </summary>
        public static ValidationReport RunOpenScenes()
        {
            var report = new ValidationReport();
            var validators = CreateValidators();
            foreach (var validator in validators)
                validator.ValidateProject(report);

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var validator in validators)
                    validator.ValidateScene(scene, report);
            }
            return report;
        }

        private static List<IValidator> CreateValidators() =>
            TypeCache.GetTypesDerivedFrom<IValidator>()
                .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => (IValidator)Activator.CreateInstance(type))
                .ToList();
    }
}

using System.Collections.Generic;
using System.Linq;
using AF.Core;
using AF.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace AF.Input.Editor
{
    /// <summary>
    /// The input setup the input mode service relies on: no project-wide actions, an update mode that runs while paused, one
    /// EventSystem on the root prefab whose UI input module uses the root scope's actions asset, and no component that enables
    /// action maps on its own (PlayerInput, an EventSystem in a scene or another prefab).
    /// </summary>
    public sealed class InputSetupValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        // ProjectWideActionsBuildProvider.EditorBuildSettingsActionsConfigKey, internal to the Input System.
        private const string PROJECT_WIDE_ACTIONS_KEY = "com.unity.input.settings.actions";

        public void ValidateProject(ValidationReport report)
        {
            EditorBuildSettings.TryGetConfigObject(PROJECT_WIDE_ACTIONS_KEY, out InputActionAsset projectWide);
            CheckProjectWideActions(projectWide, report);
            CheckUpdateMode(InputSystem.settings, report);

            var root = ValidationTargets.FindRootPrefab();
            if (root != null)
                CheckRoot(root, AssetDatabase.GetAssetPath(root), report);
        }

        public void ValidateScene(Scene scene, ValidationReport report)
        {
            var roots = scene.GetRootGameObjects();
            CheckNoEventSystem(roots, scene.path, report);
            CheckNoPlayerInput(roots, scene.path, report);
        }

        // Loads every prefab: too slow for every Play. A spawned prefab bypasses the service as a scene object would.
        public void ValidateAssets(ValidationReport report)
        {
            var root = ValidationTargets.FindRootPrefab();
            var rootPath = root != null ? AssetDatabase.GetAssetPath(root) : null;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ASSETS_FOLDER }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == rootPath)
                    continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    CheckPrefab(prefab, path, report);
            }
        }

        /// <summary>A prefab other than the root one: no EventSystem, input module or PlayerInput.</summary>
        internal static void CheckPrefab(GameObject prefab, string location, ValidationReport report)
        {
            CheckNoEventSystem(new[] { prefab }, location, report);
            CheckNoPlayerInput(new[] { prefab }, location, report);
        }

        internal static void CheckProjectWideActions(InputActionAsset projectWide, ValidationReport report)
        {
            if (projectWide == null)
                return;
            report.Error($"Project Settings > Input System Package: Project-wide Actions is set to {AssetDatabase.GetAssetPath(projectWide)}. " +
                         "Unity enables every map of that asset at startup, next to the input mode service. Set it to None.", projectWide);
        }

        internal static void CheckUpdateMode(InputSettings settings, ValidationReport report)
        {
            if (settings == null || settings.updateMode != InputSettings.UpdateMode.ProcessEventsInFixedUpdate)
                return;
            report.Error("Input System update mode is Process Events In Fixed Update: FixedUpdate does not run while the game is " +
                         "paused (time scale 0), so a pause menu could never be closed. Use Process Events In Dynamic Update.", settings);
        }

        internal static void CheckRoot(GameObject root, string location, ValidationReport report)
        {
            CheckNoPlayerInput(new[] { root }, location, report);

            var expected = FindRootActions(root, location, report);
            var eventSystems = root.GetComponentsInChildren<EventSystem>(true);
            if (eventSystems.Length == 0)
            {
                report.Error($"{location}: no EventSystem. Add a child with an EventSystem and an InputSystemUIInputModule that uses " +
                             "the root scope's actions asset; scenes do not have their own.", root);
                return;
            }
            if (eventSystems.Length > 1)
                report.Error($"{location}: {eventSystems.Length} EventSystems; the root prefab has exactly one.", eventSystems[1]);

            foreach (var module in root.GetComponentsInChildren<BaseInputModule>(true))
            {
                if (module is InputSystemUIInputModule uiModule)
                    CheckModule(uiModule, expected, location, report);
                else
                    report.Error($"{ValidationTargets.Describe(location, module)}: only InputSystemUIInputModule reads the Input System; " +
                                 "replace this module.", module);
            }
            if (eventSystems[0].GetComponent<InputSystemUIInputModule>() == null)
                report.Error($"{ValidationTargets.Describe(location, eventSystems[0])}: has no InputSystemUIInputModule.", eventSystems[0]);
        }

        internal static void CheckModule(InputSystemUIInputModule module, InputActionAsset expected, string location, ValidationReport report)
        {
            var where = ValidationTargets.Describe(location, module);
            var asset = module.actionsAsset;
            if (asset == null)
            {
                report.Error($"{where}: Actions Asset is empty, so the module uses Unity's default actions and ignores input modes " +
                             "and rebinds. Assign the root scope's actions asset.", module);
                return;
            }
            if (expected != null && asset != expected)
            {
                report.Error($"{where}: uses {asset.name}, but the root scope uses {expected.name}. UI input would ignore the input " +
                             "mode service and rebinds; assign the same asset.", module);
            }

            var references = new (string name, InputActionReference reference)[]
            {
                ("Point", module.point), ("Left Click", module.leftClick), ("Move", module.move),
                ("Submit", module.submit), ("Cancel", module.cancel), ("Scroll Wheel", module.scrollWheel),
            };
            foreach (var (name, reference) in references)
            {
                if (reference == null || reference.action == null)
                    report.Error($"{where}: the {name} action is not assigned.", module);
                else if (reference.asset != asset)
                    report.Error($"{where}: the {name} action comes from {reference.asset?.name ?? "another asset"}, not {asset.name}.", module);
            }
        }

        internal static void CheckNoEventSystem(IEnumerable<GameObject> roots, string location, ValidationReport report)
        {
            foreach (var root in roots)
            {
                foreach (var component in root.GetComponentsInChildren<EventSystem>(true).Cast<Component>()
                             .Concat(root.GetComponentsInChildren<BaseInputModule>(true)))
                {
                    report.Error($"{ValidationTargets.Describe(location, component)}: the EventSystem lives on the root prefab; delete this " +
                                 "one (Unity adds it when a Canvas is created). A second UI input module enables the UI actions on its own.", component);
                }
            }
        }

        internal static void CheckNoPlayerInput(IEnumerable<GameObject> roots, string location, ValidationReport report)
        {
            foreach (var root in roots)
            {
                foreach (var component in root.GetComponentsInChildren<PlayerInput>(true).Cast<Component>()
                             .Concat(root.GetComponentsInChildren<PlayerInputManager>(true)))
                {
                    report.Error($"{ValidationTargets.Describe(location, component)}: {component.GetType().Name} enables and switches " +
                                 "action maps itself, bypassing the input mode service. Request modes from IInputModeService instead.", component);
                }
            }
        }

        /// <summary>The actions asset the root scope's serialized fields reference, the one passed to UseInput.</summary>
        private static InputActionAsset FindRootActions(GameObject root, string location, ValidationReport report)
        {
            var scope = root.GetComponent<RootScope>();
            if (scope == null)
                return null;

            var assets = new List<InputActionAsset>();
            using (var serialized = new SerializedObject(scope))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is InputActionAsset asset &&
                        !assets.Contains(asset))
                        assets.Add(asset);
                }
            }

            if (assets.Count == 0)
            {
                report.Error($"{ValidationTargets.Describe(location, scope)}: references no InputActionAsset. Keep the actions asset " +
                             "passed to UseInput in a serialized field of the root scope.", scope);
                return null;
            }
            if (assets.Count > 1)
            {
                report.Error($"{ValidationTargets.Describe(location, scope)}: references {assets.Count} InputActionAssets " +
                             $"({string.Join(", ", assets.Select(asset => asset.name))}); the game has one.", scope);
            }
            return assets[0];
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace AF.Core.Editor
{
    /// <summary>Shared lookups for validators.</summary>
    public static class ValidationTargets
    {
        public static VContainerSettings FindVContainerSettings() =>
            PlayerSettings.GetPreloadedAssets().OfType<VContainerSettings>().FirstOrDefault();

        /// <summary>The root scope prefab assigned in VContainerSettings, or null.</summary>
        public static GameObject FindRootPrefab()
        {
            var settings = FindVContainerSettings();
            return settings != null && settings.RootLifetimeScope != null ? settings.RootLifetimeScope.gameObject : null;
        }

        public static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

        public static string HierarchyPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        /// <summary>"Assets/Scene.unity:Root/Child" style location for messages.</summary>
        public static string Describe(string location, Component component) =>
            $"{location}:{HierarchyPath(component.transform)} ({component.GetType().Name})";
    }
}

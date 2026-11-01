using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace AF.Core.Editor
{
    /// <summary>
    /// VContainer does not inject scene objects on its own. A MonoBehaviour with [Inject] members is injected only when a scope
    /// registers it: as a serialized field of the scope (registered with RegisterComponent) or under a GameObject listed in
    /// the scope's Auto Inject Game Objects. Anything else is reported. Registrations made only in code
    /// (RegisterComponentInHierarchy) are invisible here, which is why the template does not use them.
    /// Whether a scope field is really registered in code is checked when the scope is built (ScopeFieldAudit).
    /// </summary>
    public sealed class InjectRegistrationValidator : IValidator
    {
        private const string AUTO_INJECT_PROPERTY = "autoInjectGameObjects";

        public void ValidateProject(ValidationReport report)
        {
            var rootPrefab = ValidationTargets.FindRootPrefab();
            if (rootPrefab != null)
                Check(new[] { rootPrefab }, AssetDatabase.GetAssetPath(rootPrefab), report);
        }

        public void ValidateScene(Scene scene, ValidationReport report) => Check(scene.GetRootGameObjects(), scene.path, report);

        internal static void Check(IReadOnlyCollection<GameObject> roots, string location, ValidationReport report)
        {
            var registered = new HashSet<Component>();
            foreach (var scope in roots.SelectMany(root => root.GetComponentsInChildren<LifetimeScope>(true)))
                CollectRegistered(scope, registered);

            foreach (var behaviour in roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                // Missing scripts come back as null; ReferenceValidator reports them.
                if (behaviour == null || behaviour is LifetimeScope || registered.Contains(behaviour))
                    continue;
                if (!ScopeFieldAudit.HasInjectMembers(behaviour.GetType()))
                    continue;

                report.Error($"{ValidationTargets.Describe(location, behaviour)}: has [Inject] members but no scope registers it, so it is " +
                             "never injected. Make it a serialized field of the scope and register it with RegisterComponent, " +
                             "or add its GameObject to the scope's Auto Inject Game Objects.", behaviour);
            }
        }

        private static void CollectRegistered(LifetimeScope scope, HashSet<Component> registered)
        {
            using var serialized = new SerializedObject(scope);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                var value = property.objectReferenceValue;
                if (property.propertyPath.StartsWith(AUTO_INJECT_PROPERTY, StringComparison.Ordinal) && value is GameObject gameObject)
                    registered.UnionWith(gameObject.GetComponentsInChildren<Component>(true).Where(component => component != null));
                else if (value is Component component)
                    registered.Add(component);
            }
        }
    }
}

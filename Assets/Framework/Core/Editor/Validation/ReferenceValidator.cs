using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AF.Core.Editor
{
    /// <summary>Missing scripts, references to deleted objects ("Missing") and empty [Required] fields.</summary>
    public sealed class ReferenceValidator : IValidator
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<Type, string[]> _requiredFieldsCache = new Dictionary<Type, string[]>();

        public void ValidateProject(ValidationReport report)
        {
            var rootPrefab = ValidationTargets.FindRootPrefab();
            if (rootPrefab != null)
                Check(new[] { rootPrefab }, AssetDatabase.GetAssetPath(rootPrefab), report);
        }

        public void ValidateScene(Scene scene, ValidationReport report) => Check(scene.GetRootGameObjects(), scene.path, report);

        internal static void Check(IEnumerable<GameObject> roots, string location, ValidationReport report)
        {
            foreach (var transform in roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                var gameObject = transform.gameObject;
                var missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (missingScripts > 0)
                    report.Error($"{location}:{ValidationTargets.HierarchyPath(transform)}: {missingScripts} missing script(s).", gameObject);

                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component == null || component is Transform)
                        continue;
                    CheckComponent(component, location, report);
                }
            }
        }

        private static void CheckComponent(Component component, string location, ValidationReport report)
        {
            using var serialized = new SerializedObject(component);

            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && IsMissing(property))
                    report.Error($"{ValidationTargets.Describe(location, component)}: {property.propertyPath} points to a deleted object (Missing).", component);
            }

            foreach (var fieldName in RequiredFields(component.GetType()))
            {
                var required = serialized.FindProperty(fieldName);
                if (required is { propertyType: SerializedPropertyType.ObjectReference } && required.objectReferenceValue == null && !IsMissing(required))
                    report.Error($"{ValidationTargets.Describe(location, component)}: {fieldName} is [Required] but empty.", component);
            }
        }

        // A reference to a deleted object keeps its id while the value resolves to null.
        private static bool IsMissing(SerializedProperty property) =>
            property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0;

        private static string[] RequiredFields(Type type)
        {
            if (_requiredFieldsCache.TryGetValue(type, out var fields))
                return fields;

            var names = new List<string>();
            for (var current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
            {
                names.AddRange(current.GetFields(FieldFlags)
                    .Where(field => field.IsDefined(typeof(RequiredAttribute), true))
                    .Select(field => field.Name));
            }
            fields = names.ToArray();
            _requiredFieldsCache[type] = fields;
            return fields;
        }
    }
}

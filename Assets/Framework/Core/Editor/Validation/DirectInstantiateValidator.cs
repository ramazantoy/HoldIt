using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AF.Core.Editor
{
    /// <summary>
    /// Finds project code that copies GameObjects or components with <c>Object.Instantiate</c>, adds a component with
    /// [Inject] members, or adds one by <c>Type</c>: nothing injects the result. Which prefab is passed cannot be known from the code, so every such copy
    /// goes through <c>IObjectResolver.Instantiate</c> or an <see cref="IPools"/> pool. Copies whose type is known to be
    /// something else (a material, a ScriptableObject) are allowed. Project assemblies only (Assets/), tests and editor code
    /// excluded; code marked <see cref="InstantiateExemptAttribute"/> is skipped.
    /// </summary>
    public sealed class DirectInstantiateValidator : IValidator
    {
        private const string ADVICE = "Nothing injects the result. Copy prefabs with IObjectResolver.Instantiate or an IPools pool, and " +
                                      "inject an added component with IObjectResolver.Inject. Mark a deliberate exception " +
                                      "[InstantiateExempt(\"reason\")].";

        public void ValidateProject(ValidationReport report) => Check(DirectCallScan.ProjectTypes(), report);

        internal static void Check(IEnumerable<Type> types, ValidationReport report) =>
            DirectCallScan.Check(types, typeof(InstantiateExemptAttribute), Describe, ADVICE, report);

        /// <summary>The call as reported, or null when it is allowed.</summary>
        private static string Describe(MethodBase target)
        {
            if (target.DeclaringType == typeof(Object) && (target.Name == "Instantiate" || target.Name == "InstantiateAsync"))
            {
                // Generic overloads name the copied type; the others take a plain Object, which may be anything.
                if (!target.IsGenericMethod)
                    return $"Object.{target.Name}() with a plain Object";
                var copied = target.GetGenericArguments()[0];
                return MayBeGameObjectOrComponent(copied) ? $"Object.{target.Name}<{copied.Name}>()" : null;
            }

            if (target.DeclaringType == typeof(GameObject) && target.Name == "AddComponent" && !target.IsGenericMethod)
            {
                // The added type is a runtime value, which may be one with [Inject] members.
                return "GameObject.AddComponent() with a Type";
            }
            if (target.DeclaringType == typeof(GameObject) && target.Name == "AddComponent")
            {
                var added = target.GetGenericArguments()[0];
                return !added.IsGenericParameter && ScopeFieldAudit.HasInjectMembers(added)
                    ? $"GameObject.AddComponent<{added.Name}>(), whose [Inject] members stay empty"
                    : null;
            }
            return null;
        }

        private static bool MayBeGameObjectOrComponent(Type type)
        {
            if (type.IsGenericParameter)
            {
                var constraints = type.GetGenericParameterConstraints();
                return constraints.Length == 0 || constraints.All(MayBeGameObjectOrComponent);
            }
            return type == typeof(Object) || type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type) || type.IsInterface;
        }
    }
}

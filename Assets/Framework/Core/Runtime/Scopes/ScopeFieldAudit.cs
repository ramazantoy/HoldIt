using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AF.Core
{
    /// <summary>
    /// Finds serialized component fields of a scope whose component has [Inject] members but was never registered.
    /// Validation counts such a field as the scope's registration of that component; this checks that ConfigureGame or
    /// ConfigureScene really registered it. Components under Auto Inject Game Objects are injected anyway and are skipped.
    /// The check is by type: of two fields of the same type, one registration covers both.
    /// </summary>
    internal static class ScopeFieldAudit
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<Type, bool> _injectMembersCache = new Dictionary<Type, bool>();

        /// <summary>Logs every unregistered field once the scope's container is built.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Attach(IContainerBuilder builder, LifetimeScope scope, IReadOnlyList<GameObject> autoInjectGameObjects)
        {
            builder.RegisterBuildCallback(_ =>
            {
                foreach (var problem in FindUnregistered(builder, scope, autoInjectGameObjects))
                    Log.Error($"{scope.name}: {problem}", scope);
            });
        }

        public static List<string> FindUnregistered(IContainerBuilder builder, LifetimeScope scope, IReadOnlyList<GameObject> autoInjectGameObjects)
        {
            var problems = new List<string>();
            for (var type = scope.GetType(); type != null && type != typeof(LifetimeScope); type = type.BaseType)
            {
                foreach (var field in type.GetFields(MemberFlags).Where(IsSerialized))
                {
                    foreach (var component in ComponentsIn(field.GetValue(scope)))
                    {
                        // A prefab asset (a pool's prefab) is copied and injected by whoever instantiates it; it is never registered.
                        if (!component.gameObject.scene.IsValid())
                            continue;
                        var componentType = component.GetType();
                        if (!HasInjectMembers(componentType) || builder.Exists(componentType) || IsAutoInjected(component, autoInjectGameObjects))
                            continue;

                        problems.Add($"{field.Name} holds {componentType.Name}, which has [Inject] members but is not registered, so it is " +
                                     $"never injected. Register it with builder.RegisterComponent({field.Name}).");
                    }
                }
            }
            return problems;
        }

        /// <summary>True when the type or one of its base types up to MonoBehaviour declares an [Inject] member.</summary>
        internal static bool HasInjectMembers(Type type)
        {
            if (_injectMembersCache.TryGetValue(type, out var hasInject))
                return hasInject;

            hasInject = false;
            for (var current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
            {
                if (current.GetMembers(MemberFlags).Any(member => member.IsDefined(typeof(InjectAttribute), true)))
                {
                    hasInject = true;
                    break;
                }
            }
            _injectMembersCache[type] = hasInject;
            return hasInject;
        }

        private static bool IsSerialized(FieldInfo field) =>
            !field.IsInitOnly &&
            (field.IsPublic ? !field.IsDefined(typeof(NonSerializedAttribute)) : field.IsDefined(typeof(SerializeField)));

        private static IEnumerable<Component> ComponentsIn(object value)
        {
            var components = value switch
            {
                Component component => new[] { component },
                IEnumerable<Component> many => many,
                _ => Enumerable.Empty<Component>(),
            };
            return components.Where(component => component != null);
        }

        private static bool IsAutoInjected(Component component, IReadOnlyList<GameObject> autoInjectGameObjects) =>
            autoInjectGameObjects != null &&
            autoInjectGameObjects.Any(root => root != null && component.transform.IsChildOf(root.transform));
    }
}

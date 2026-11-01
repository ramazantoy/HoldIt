using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VContainer;
using VContainer.Unity;

namespace AF.Core
{
    /// <summary>
    /// Finds types that implement a VContainer entry-point interface (ITickable, IStartable…) but were registered without it,
    /// for example with <c>.As&lt;IFoo&gt;()</c>. VContainer never calls those; the mistake is otherwise silent.
    /// </summary>
    internal static class EntryPointAudit
    {
        private static readonly Type[] _entryPointInterfaces =
        {
            typeof(IInitializable), typeof(IPostInitializable),
            typeof(IStartable), typeof(IPostStartable), typeof(IAsyncStartable),
            typeof(IFixedTickable), typeof(IPostFixedTickable),
            typeof(ITickable), typeof(IPostTickable),
            typeof(ILateTickable), typeof(IPostLateTickable),
        };

        /// <summary>Logs every misregistered entry point once the scope's container is built.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Attach(IContainerBuilder builder, LifetimeScope scope)
        {
            if (builder is not ContainerBuilder containerBuilder)
                return;

            // A diagnostic must never stop the app it checks; EntryPointAuditTests fail on the layout change instead.
            if (!RegistrationAudit.LayoutKnown)
            {
                Log.Error($"{scope.name}: VContainer's RegistrationBuilder layout changed; entry point audit skipped. Update RegistrationAudit.", scope);
                return;
            }

            builder.RegisterBuildCallback(_ =>
            {
                foreach (var problem in FindMisregistered(containerBuilder))
                    Log.Error($"{scope.name}: {problem}", scope);
            });
        }

        public static List<string> FindMisregistered(ContainerBuilder builder)
        {
            var problems = new List<string>();
            foreach (var registration in RegistrationAudit.Read(builder))
            {
                var missing = _entryPointInterfaces
                    .Where(entryPoint => entryPoint.IsAssignableFrom(registration.Implementation))
                    .Where(entryPoint => !registration.IsRegisteredAs(entryPoint))
                    .Select(entryPoint => entryPoint.Name)
                    .ToList();
                if (missing.Count > 0)
                {
                    var implementation = registration.Implementation.Name;
                    problems.Add($"{implementation} implements {string.Join(", ", missing)} but is not registered as an entry point, " +
                                 $"so VContainer never calls it. Register it with builder.RegisterEntryPoint<{implementation}>().");
                }

                if (registration.Lifetime == Lifetime.Scoped)
                {
                    var entryPoints = _entryPointInterfaces
                        .Where(entryPoint => registration.IsRegisteredAs(entryPoint))
                        .Select(entryPoint => entryPoint.Name)
                        .ToList();
                    if (entryPoints.Count > 0)
                    {
                        problems.Add($"{registration.Implementation.Name} is a Scoped {string.Join(", ", entryPoints)}. VContainer runs the " +
                                     "entry points of the registering scope only, so the instances child scopes create never run. " +
                                     "Register it as a singleton, or move the entry point into a class of its own.");
                    }
                }
            }
            return problems;
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using AF.Core;
using VContainer;
using VContainer.Unity;

namespace AF.Save
{
    /// <summary>
    /// Finds savables that a scope registers in a way the save system cannot see: without the ISavable contract (for example
    /// with <c>.As&lt;IWallet&gt;()</c> only), which means they are never saved, or with a lifetime other than singleton,
    /// which means the registry holds a different instance from the one the game uses.
    /// </summary>
    internal static class SavableAudit
    {
        /// <summary>Logs every problem once the scope's container is built.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Attach(IContainerBuilder builder, LifetimeScope scope)
        {
            if (builder is not ContainerBuilder containerBuilder)
                return;

            var name = scope != null ? scope.name : "Container";
            if (!RegistrationAudit.LayoutKnown)
            {
                Log.Error($"{name}: VContainer's RegistrationBuilder layout changed; savable audit skipped. Update RegistrationAudit.", scope);
                return;
            }

            builder.RegisterBuildCallback(_ =>
            {
                foreach (var problem in FindProblems(containerBuilder))
                    Log.Error($"{name}: {problem}", scope);
            });
        }

        public static List<string> FindProblems(ContainerBuilder builder)
        {
            var problems = new List<string>();
            foreach (var registration in RegistrationAudit.Read(builder))
            {
                if (!typeof(ISavable).IsAssignableFrom(registration.Implementation))
                    continue;

                var type = registration.Implementation.Name;
                if (!registration.IsRegisteredAs(typeof(ISavable)))
                {
                    problems.Add($"{type} is a savable but is not registered as ISavable, so it is never saved or restored. " +
                                 $"Register it with builder.Register<{type}>(Lifetime.Singleton).AsImplementedInterfaces().");
                }
                else if (registration.Lifetime != Lifetime.Singleton)
                {
                    problems.Add($"{type} is a savable registered as {registration.Lifetime}; savables must be singletons, " +
                                 "or the save system and the game hold different instances.");
                }
            }
            return problems;
        }
    }
}

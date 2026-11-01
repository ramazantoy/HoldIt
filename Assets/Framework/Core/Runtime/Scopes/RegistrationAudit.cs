using System;
using System.Collections.Generic;
using System.Reflection;
using VContainer;

namespace AF.Core
{
    /// <summary>
    /// Reads a builder's registrations before it is built, for audits that catch registrations VContainer accepts but never
    /// uses. RegistrationBuilder keeps these fields protected internal and has no public way to read them, so this uses
    /// reflection; audits built on it run in the Editor and development builds only.
    /// </summary>
    public static class RegistrationAudit
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo _implementationTypeField = typeof(RegistrationBuilder).GetField("ImplementationType", FieldFlags);
        private static readonly FieldInfo _interfaceTypesField = typeof(RegistrationBuilder).GetField("InterfaceTypes", FieldFlags);
        private static readonly FieldInfo _lifetimeField = typeof(RegistrationBuilder).GetField("Lifetime", FieldFlags);

        /// <summary>False when a VContainer update renamed the fields; audits then report that and skip.</summary>
        public static bool LayoutKnown => _implementationTypeField != null && _interfaceTypesField != null && _lifetimeField != null;

        public readonly struct Registration
        {
            public readonly Type Implementation;
            public readonly Lifetime Lifetime;
            private readonly List<Type> _contracts;

            public Registration(Type implementation, List<Type> contracts, Lifetime lifetime)
            {
                Implementation = implementation;
                _contracts = contracts;
                Lifetime = lifetime;
            }

            /// <summary>True when resolving <paramref name="contract"/> can return this registration.</summary>
            public bool IsRegisteredAs(Type contract) =>
                _contracts == null ? contract == Implementation : _contracts.Contains(contract);
        }

        public static List<Registration> Read(ContainerBuilder builder)
        {
            if (!LayoutKnown)
                throw new InvalidOperationException("VContainer's RegistrationBuilder layout changed; update RegistrationAudit.");

            var registrations = new List<Registration>(builder.Count);
            for (var i = 0; i < builder.Count; i++)
            {
                var registration = builder[i];
                registrations.Add(new Registration(
                    (Type)_implementationTypeField.GetValue(registration),
                    (List<Type>)_interfaceTypesField.GetValue(registration),
                    (Lifetime)_lifetimeField.GetValue(registration)));
            }
            return registrations;
        }

        /// <summary>
        /// Registrations whose type implements <paramref name="contract"/> but that are registered without it, for example
        /// with <c>.As&lt;IFoo&gt;()</c> only. Resolving the contract, or a collection of it, never finds them.
        /// </summary>
        public static List<Registration> FindMissingContract(ContainerBuilder builder, Type contract)
        {
            var missing = new List<Registration>();
            foreach (var registration in Read(builder))
            {
                if (contract.IsAssignableFrom(registration.Implementation) && !registration.IsRegisteredAs(contract))
                    missing.Add(registration);
            }
            return missing;
        }
    }
}

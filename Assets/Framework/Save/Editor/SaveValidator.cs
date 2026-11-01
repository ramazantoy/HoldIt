using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AF.Core.Editor;
using Newtonsoft.Json;
using UnityEditor;

namespace AF.Save.Editor
{
    /// <summary>
    /// Checks savable types and their data classes, which otherwise fail only when a save is loaded: a Unity object used as a
    /// savable, and data the serializer writes but cannot read back. Test assemblies are skipped.
    /// </summary>
    public sealed class SaveValidator : IValidator
    {
        public void ValidateProject(ValidationReport report) =>
            Check(TypeCache.GetTypesDerivedFrom<ISavable>().Where(type => !IsTestAssembly(type.Assembly)), report);

        internal static void Check(IEnumerable<Type> savableTypes, ValidationReport report)
        {
            foreach (var type in savableTypes.OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    report.Error($"{type.FullName} is a Unity object and a savable. Savables are plain C# services registered in a " +
                                 "scope: a component can be destroyed before its state is captured, and an asset's state outlives Play Mode. " +
                                 "Keep the state in a service that the component reads and writes.");
                    continue;
                }
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                var dataType = DataTypeOf(type);
                if (dataType != null)
                    new DataTypeCheck(type, report).Run(dataType);
            }
        }

        private static Type DataTypeOf(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Savable<>))
                    return current.GetGenericArguments()[0];
            }
            return null;
        }

        private static bool IsTestAssembly(Assembly assembly) =>
            assembly.GetName().Name.IndexOf(".Tests", StringComparison.Ordinal) >= 0;

        /// <summary>Walks a data class the way Newtonsoft serializes it and reports what cannot round-trip.</summary>
        private sealed class DataTypeCheck
        {
            private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            private static readonly HashSet<Type> _simpleTypes = new HashSet<Type>
            {
                typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid),
            };

            private static readonly HashSet<Type> _dictionaryKeyTypes = new HashSet<Type> { typeof(string), typeof(int), typeof(long) };

            private readonly Type _savable;
            private readonly ValidationReport _report;
            private readonly HashSet<Type> _visited = new HashSet<Type>();

            public DataTypeCheck(Type savable, ValidationReport report)
            {
                _savable = savable;
                _report = report;
            }

            public void Run(Type dataType) => CheckObject(dataType, dataType.Name);

            private void CheckType(Type type, string path)
            {
                if (type.IsPrimitive || type.IsEnum || _simpleTypes.Contains(type) || type.IsGenericParameter)
                    return;

                var underlying = Nullable.GetUnderlyingType(type);
                if (underlying != null)
                {
                    CheckType(underlying, path);
                    return;
                }

                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    Error(path, $"references a Unity object ({type.Name}). Save an id or an asset key and look the object up on restore.");
                    return;
                }

                if (type.Namespace != null && (type.Namespace == "UnityEngine" || type.Namespace.StartsWith("UnityEngine.", StringComparison.Ordinal)))
                {
                    if (!UnityConverters.SupportedTypes.Contains(type))
                        Error(path, $"is {type.Name}, which the save serializer has no converter for. Store its values in plain fields.");
                    return;
                }

                if (type.IsArray)
                {
                    CheckType(type.GetElementType(), path + "[]");
                    return;
                }

                var dictionary = GenericArguments(type, typeof(IDictionary<,>)) ?? GenericArguments(type, typeof(IReadOnlyDictionary<,>));
                if (dictionary != null)
                {
                    var key = dictionary[0];
                    if (!key.IsEnum && !_dictionaryKeyTypes.Contains(key))
                        Error(path, $"has {key.Name} keys. Dictionary keys are written as strings; use string, int, long or an enum.");
                    CheckType(dictionary[1], path + "[value]");
                    return;
                }

                var element = GenericArguments(type, typeof(IEnumerable<>));
                if (element != null)
                {
                    CheckType(element[0], path + "[]");
                    return;
                }

                if (type == typeof(object) || type.IsInterface || type.IsAbstract || typeof(IEnumerable).IsAssignableFrom(type))
                {
                    Error(path, $"is {type.Name}, which is not a concrete type. Save data carries no type names, so the serializer " +
                                "cannot know which class to create. Use a concrete class.");
                    return;
                }

                CheckObject(type, path);
            }

            private void CheckObject(Type type, string path)
            {
                if (!_visited.Add(type))
                    return;

                if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null &&
                    !type.GetConstructors(MemberFlags).Any(constructor => constructor.IsDefined(typeof(JsonConstructorAttribute), true)))
                    Error(path, $"({type.Name}) has no public parameterless constructor, so it cannot be created on load.");

                foreach (var field in type.GetFields(MemberFlags))
                {
                    if (!IsSerialized(field))
                        continue;
                    var memberPath = path + "." + field.Name;
                    if (field.IsInitOnly && !field.IsDefined(typeof(JsonPropertyAttribute), true))
                        Error(memberPath, "is readonly, so it is written but never read back. Remove readonly.");
                    CheckType(field.FieldType, memberPath);
                }

                foreach (var property in type.GetProperties(MemberFlags))
                {
                    if (!IsSerialized(property))
                        continue;
                    var memberPath = path + "." + property.Name;
                    var setter = property.SetMethod;
                    if (setter == null || !setter.IsPublic && !property.IsDefined(typeof(JsonPropertyAttribute), true))
                        Error(memberPath, "has no public setter, so it is written but never read back. Add a setter, or mark it [JsonIgnore] if it is computed.");
                    CheckType(property.PropertyType, memberPath);
                }
            }

            private static bool IsSerialized(FieldInfo field)
            {
                if (field.IsDefined(typeof(JsonIgnoreAttribute), true) || field.IsDefined(typeof(NonSerializedAttribute), true))
                    return false;
                // Compiler-generated backing fields belong to properties, which are checked on their own.
                return (field.IsPublic && !field.Name.Contains("<")) || field.IsDefined(typeof(JsonPropertyAttribute), true);
            }

            private static bool IsSerialized(PropertyInfo property)
            {
                if (property.IsDefined(typeof(JsonIgnoreAttribute), true) || property.GetIndexParameters().Length > 0)
                    return false;
                return property.GetMethod is { IsPublic: true } || property.IsDefined(typeof(JsonPropertyAttribute), true);
            }

            private static Type[] GenericArguments(Type type, Type openInterface)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == openInterface)
                    return type.GetGenericArguments();
                var match = type.GetInterfaces().FirstOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == openInterface);
                return match?.GetGenericArguments();
            }

            private void Error(string path, string problem) =>
                _report.Error($"{_savable.FullName} save data: {path} {problem}");
        }
    }
}

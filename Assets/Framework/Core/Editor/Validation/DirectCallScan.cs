using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;

namespace AF.Core.Editor
{
    /// <summary>
    /// Finds project code that calls what only one framework service may call (input maps, scene loads). It reads the compiled
    /// method bodies, so it finds the exact method each call resolves to, not text that looks like one. Code marked with the
    /// module's exempt attribute is skipped; on a type the attribute covers every member, nested types and lambdas included.
    /// </summary>
    public static class DirectCallScan
    {
        private const BindingFlags MemberFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                                                 BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Reports every call that <paramref name="describe"/> names (it returns null for allowed calls) as
        /// "<c>Owner.Method calls {call}. {advice}</c>". <paramref name="describeBox"/> does the same for value types boxed
        /// (converted to <c>object</c> or an interface), reported as "<c>Owner.Method uses {boxing}</c>".
        /// </summary>
        public static void Check(IEnumerable<Type> types, Type exemptAttribute, Func<MethodBase, string> describe, string advice,
            ValidationReport report, Func<Type, string> describeBox = null)
        {
            foreach (var type in types.OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                List<MethodBase> methods;
                try
                {
                    methods = type.GetMethods(MemberFlags).Cast<MethodBase>().Concat(type.GetConstructors(MemberFlags)).ToList();
                }
                catch (Exception exception) when (exception is TypeLoadException || exception is FileNotFoundException)
                {
                    report.Warning($"{type.FullName} could not be read for direct calls and was skipped: {exception.Message}");
                    continue;
                }
                foreach (var method in methods)
                {
                    if (IsExempt(method, exemptAttribute))
                        continue;
                    var reported = new HashSet<string>();
                    var references = IlReferences.Of(method);
                    foreach (var target in references.Calls)
                    {
                        var call = describe(target);
                        if (call != null && reported.Add(call))
                            report.Error($"{OwnerName(method)} calls {call}. {advice}");
                    }
                    if (describeBox == null)
                        continue;
                    foreach (var boxed in references.Boxed)
                    {
                        var boxing = describeBox(boxed);
                        if (boxing != null && reported.Add(boxing))
                            report.Error($"{OwnerName(method)} uses {boxing}. {advice}");
                    }
                }
            }
        }

        /// <summary>Types of the project's player assemblies (Assets/ only, tests and editor code excluded).</summary>
        public static IEnumerable<Type> ProjectTypes(Assembly excluded = null)
        {
            var own = excluded?.GetName().Name;
            var names = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(assembly => assembly.name != own && assembly.sourceFiles.Length > 0 &&
                                   assembly.sourceFiles.All(file => file.StartsWith("Assets/", StringComparison.Ordinal)))
                .Select(assembly => assembly.name));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => names.Contains(assembly.GetName().Name)))
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }
                foreach (var type in types)
                    yield return type;
            }
        }

        private static bool IsExempt(MethodBase method, Type exemptAttribute)
        {
            if (method.IsDefined(exemptAttribute, false))
                return true;

            // Lambdas, local functions and async or iterator bodies compile into members and nested types named "<Owner>…".
            var owner = GeneratedOwner(method.Name);
            for (var type = method.DeclaringType; type != null; type = type.DeclaringType)
            {
                if (type.IsDefined(exemptAttribute, false))
                    return true;
                if (owner != null && type.GetMember(owner, MemberTypes.Method | MemberTypes.Constructor, MemberFlags)
                        .Any(member => member.IsDefined(exemptAttribute, false)))
                    return true;
                owner ??= GeneratedOwner(type.Name);
            }
            return false;
        }

        /// <summary>"Foo" for "<Foo>b__0", "<<Foo>b__0>b__1" or "<Foo>d__3"; null for other names and for "<>c".</summary>
        private static string GeneratedOwner(string name)
        {
            var start = 0;
            while (start < name.Length && name[start] == '<')
                start++;
            var end = name.IndexOf('>', start);
            return start > 0 && end > start ? name.Substring(start, end - start) : null;
        }

        private static string OwnerName(MethodBase method)
        {
            var type = method.DeclaringType;
            while (type != null && type.Name.StartsWith("<", StringComparison.Ordinal) && type.DeclaringType != null)
                type = type.DeclaringType;
            var name = GeneratedOwner(method.Name) ?? GeneratedOwner(method.DeclaringType?.Name ?? "") ?? method.Name;
            return $"{type?.FullName}.{name}";
        }

        /// <summary>
        /// The methods a method body calls and the value types it boxes, read from its IL. Kept for the domain's lifetime:
        /// project assemblies only change with a domain reload, and every direct-call validator reads the same methods.
        /// </summary>
        private sealed class IlReferences
        {
            private static readonly OpCode[] _oneByte = new OpCode[0x100];
            private static readonly OpCode[] _twoByte = new OpCode[0x100];
            private static readonly Dictionary<(Module, int), IlReferences> _cache = new Dictionary<(Module, int), IlReferences>();

            public readonly List<MethodBase> Calls = new List<MethodBase>();
            public readonly List<Type> Boxed = new List<Type>();

            static IlReferences()
            {
                foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.GetValue(null) is not OpCode code)
                        continue;
                    var value = (ushort)code.Value;
                    if (code.Size == 1)
                        _oneByte[value] = code;
                    else
                        _twoByte[value & 0xFF] = code;
                }
            }

            public static IlReferences Of(MethodBase method)
            {
                var key = (method.Module, method.MetadataToken);
                if (!_cache.TryGetValue(key, out var references))
                {
                    references = Read(method);
                    _cache.Add(key, references);
                }
                return references;
            }

            private static IlReferences Read(MethodBase method)
            {
                var references = new IlReferences();
                byte[] il;
                try
                {
                    il = method.GetMethodBody()?.GetILAsByteArray();
                }
                catch (Exception)
                {
                    return references;
                }
                if (il == null)
                    return references;

                var typeArguments = method.DeclaringType != null && method.DeclaringType.IsGenericType
                    ? method.DeclaringType.GetGenericArguments()
                    : null;
                var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

                var position = 0;
                while (position < il.Length)
                {
                    var first = il[position++];
                    var code = first == 0xFE && position < il.Length ? _twoByte[il[position++]] : _oneByte[first];
                    if (position + 4 <= il.Length)
                    {
                        try
                        {
                            if (code.OperandType == OperandType.InlineMethod)
                                references.Calls.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il, position), typeArguments, methodArguments));
                            else if (code == OpCodes.Box)
                                references.Boxed.Add(method.Module.ResolveType(BitConverter.ToInt32(il, position), typeArguments, methodArguments));
                        }
                        catch (Exception)
                        {
                            // A token that does not resolve here (a missing reference) cannot be one of the reported ones.
                        }
                    }
                    position += OperandSize(code, il, position);
                }
                return references;
            }

            private static int OperandSize(OpCode code, byte[] il, int position)
            {
                switch (code.OperandType)
                {
                    case OperandType.InlineNone:
                        return 0;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        return 1;
                    case OperandType.InlineVar:
                        return 2;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        return 8;
                    case OperandType.InlineSwitch:
                        return position + 4 <= il.Length ? 4 + 4 * BitConverter.ToInt32(il, position) : 4;
                    default:
                        return 4;
                }
            }
        }
    }
}

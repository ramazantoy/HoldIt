using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AF.Core.Editor;
using Mono.Cecil;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using Object = UnityEngine.Object;

namespace AF.Build.Editor
{
    /// <summary>
    /// Finds serialized fields that exist in the Editor but not in the player, or the other way round: a field inside
    /// <c>#if UNITY_EDITOR</c>, a platform define or a build-menu define. The player then reads scenes and prefabs with the
    /// wrong layout. Unity reports it late in the build; this compiles the player scripts with the build's own defines and
    /// names the field before the build starts.
    /// </summary>
    internal static class SerializedLayoutCheck
    {
        private const string TEMP_FOLDER = "Temp/BuildLayoutCheck";
        private const string EDITOR_ASSEMBLIES = "Library/ScriptAssemblies";

        /// <summary>Compares the layouts and returns the player assemblies the build compiles (file names), empty when the compile failed.</summary>
        public static IReadOnlyCollection<string> Run(BuildTarget target, BuildKind kind, ValidationReport report)
        {
            var projectAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(assembly => assembly.sourceFiles.Any(file => file.StartsWith("Assets/", StringComparison.Ordinal)))
                .Select(assembly => assembly.name)
                .ToList();

            var output = Path.GetFullPath(TEMP_FOLDER);
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
            try
            {
                var settings = new ScriptCompilationSettings
                {
                    target = target,
                    group = BuildPipeline.GetBuildTargetGroup(target),
                    subtarget = BuildPipeline.GetBuildTargetGroup(target) == BuildTargetGroup.Standalone ? (int)StandaloneBuildSubtarget.Player : 0,
                    options = kind == BuildKind.Development ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None,
                    extraScriptingDefines = BuildKinds.Defines(kind),
                };
                var result = PlayerBuildInterface.CompilePlayerScripts(settings, output);
                if (result.assemblies == null || result.assemblies.Count == 0)
                {
                    report.Error($"The player scripts for {target} ({kind}) did not compile; see the console.");
                    return Array.Empty<string>();
                }

                foreach (var name in projectAssemblies)
                {
                    var editorPath = Path.Combine(EDITOR_ASSEMBLIES, name + ".dll");
                    var playerPath = Path.Combine(output, name + ".dll");
                    // An assembly excluded from this player by its define constraints has no layout to compare.
                    if (!File.Exists(editorPath) || !File.Exists(playerPath))
                        continue;
                    var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == name);
                    if (loaded == null)
                    {
                        report.Error($"{name}: the Editor has not loaded this assembly, so its serialized layouts were not compared. Recompile and check again.");
                        continue;
                    }
                    foreach (var difference in Compare(ReadFields(editorPath), ReadFields(playerPath), IsUnitySerialized(loaded)))
                        report.Error($"{name}: {difference} The player reads scenes and prefabs saved by the Editor with a different layout. " +
                                     "Keep serialized fields outside #if blocks and put only the code that uses them inside.");
                }
                return result.assemblies;
            }
            finally
            {
                if (Directory.Exists(output))
                    Directory.Delete(output, recursive: true);
            }
        }

        /// <summary>Serialized field candidates of every type: "name: type", by type full name (nested types with '+').</summary>
        public static Dictionary<string, SortedSet<string>> ReadFields(string assemblyPath)
        {
            // From memory, so the file is not left locked.
            using var assembly = AssemblyDefinition.ReadAssembly(new MemoryStream(File.ReadAllBytes(assemblyPath)));
            var types = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var type in assembly.MainModule.GetTypes())
            {
                if (type.IsInterface || type.IsEnum)
                    continue;
                var fields = new SortedSet<string>(type.Fields.Where(IsSerializedCandidate).Select(field => $"{field.Name}: {field.FieldType.FullName}"),
                    StringComparer.Ordinal);
                types[type.FullName.Replace('/', '+')] = fields;
            }
            return types;
        }

        /// <summary>
        /// One line per type whose serialized fields differ. Types missing on one side are skipped: a type compiled out of the
        /// player entirely is not a layout problem.
        /// </summary>
        public static List<string> Compare(Dictionary<string, SortedSet<string>> editor, Dictionary<string, SortedSet<string>> player,
            Func<string, bool> isSerializedType)
        {
            var differences = new List<string>();
            foreach (var entry in editor.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                if (!player.TryGetValue(entry.Key, out var playerFields) || !isSerializedType(entry.Key))
                    continue;
                var editorOnly = entry.Value.Except(playerFields).ToList();
                var playerOnly = playerFields.Except(entry.Value).ToList();
                if (editorOnly.Count == 0 && playerOnly.Count == 0)
                    continue;
                var parts = new List<string>();
                if (editorOnly.Count > 0)
                    parts.Add("only in the Editor: " + string.Join(", ", editorOnly));
                if (playerOnly.Count > 0)
                    parts.Add("only in the player: " + string.Join(", ", playerOnly));
                differences.Add($"{entry.Key} serializes different fields ({string.Join("; ", parts)}).");
            }
            return differences;
        }

        private static bool IsSerializedCandidate(FieldDefinition field)
        {
            if (field.IsStatic || field.IsLiteral || field.IsInitOnly || field.IsNotSerialized)
                return false;
            return field.IsPublic || field.CustomAttributes.Any(attribute =>
                attribute.AttributeType.FullName is "UnityEngine.SerializeField" or "UnityEngine.SerializeReference");
        }

        /// <summary>
        /// Whether Unity serializes a type, decided on the Editor's loaded copy: a UnityEngine.Object or a [Serializable]
        /// class or struct. Base classes do not change between the Editor and a player.
        /// </summary>
        internal static Func<string, bool> IsUnitySerialized(System.Reflection.Assembly loaded) =>
            typeName =>
            {
                var type = loaded.GetType(typeName);
                return type != null && !type.IsEnum && (typeof(Object).IsAssignableFrom(type) || type.IsSerializable);
            };
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace AF.Build.Editor
{
    /// <summary>
    /// Where builds go, relative to the project root: <c>Builds/&lt;Platform&gt;/&lt;Kind&gt;/</c> is the folder to ship,
    /// <c>&lt;Kind&gt;-symbols/</c> keeps the debug folders that must not ship, <c>&lt;Kind&gt;-report.txt</c> the last
    /// result. Fixed paths, so tools and testers always find the latest build in the same place.
    /// </summary>
    public static class BuildPaths
    {
        public const string Root = "Builds";

        /// <summary>Null for a target the build menu does not support.</summary>
        public static string PlatformFolder(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows => "Windows",
            BuildTarget.StandaloneOSX => "macOS",
            BuildTarget.StandaloneLinux64 => "Linux",
            BuildTarget.Android => "Android",
            BuildTarget.iOS => "iOS",
            _ => null,
        };

        public static bool IsSupported(BuildTarget target) => PlatformFolder(target) != null;

        public static string OutputFolder(BuildTarget target, BuildKind kind) => $"{Root}/{PlatformFolder(target)}/{kind}";

        public static string SymbolsFolder(BuildTarget target, BuildKind kind) => OutputFolder(target, kind) + "-symbols";

        public static string ReportFile(BuildTarget target, BuildKind kind) => OutputFolder(target, kind) + "-report.txt";

        /// <summary>The path passed to the build: the executable, the app bundle, the package, or the Xcode project folder.</summary>
        public static string Location(BuildTarget target, BuildKind kind, string productName)
        {
            var folder = OutputFolder(target, kind);
            var name = FileName(productName);
            return target switch
            {
                BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows => $"{folder}/{name}.exe",
                BuildTarget.StandaloneOSX => $"{folder}/{name}.app",
                BuildTarget.StandaloneLinux64 => $"{folder}/{name}.x86_64",
                BuildTarget.Android => $"{folder}/{name}.{(EditorUserBuildSettings.buildAppBundle ? "aab" : "apk")}",
                _ => folder,
            };
        }

        /// <summary>True when the build is a single zipped package (.apk, .aab) rather than a folder of files.</summary>
        public static bool IsPackage(BuildTarget target) => target == BuildTarget.Android;

        /// <summary>True when <paramref name="path"/> is strictly inside <c>&lt;projectRoot&gt;/Builds/</c>.</summary>
        public static bool IsInsideRoot(string projectRoot, string path)
        {
            var root = Path.GetFullPath(Path.Combine(projectRoot, Root)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Deletes a build folder or file. Refuses anything outside <c>Builds/</c>, so a wrong path cannot wipe the project.</summary>
        public static void Delete(string projectRoot, string path)
        {
            if (!IsInsideRoot(projectRoot, path))
                throw new InvalidOperationException($"Refusing to delete {path}: only paths inside {Root}/ are deleted.");
            var full = Path.GetFullPath(Path.Combine(projectRoot, path));
            if (Directory.Exists(full))
                Directory.Delete(full, recursive: true);
            else if (File.Exists(full))
                File.Delete(full);
        }

        // Windows' set on every host: Path.GetInvalidFileNameChars differs per OS, and a build made on a Mac may be copied to Windows.
        private const string INVALID_FILE_NAME_CHARACTERS = "<>:\"/\\|?*";

        private static string FileName(string productName)
        {
            var name = new string(productName
                .Select(character => character < ' ' || INVALID_FILE_NAME_CHARACTERS.IndexOf(character) >= 0 ? '_' : character)
                .ToArray()).Trim();
            return name.Length > 0 ? name : "Game";
        }
    }
}

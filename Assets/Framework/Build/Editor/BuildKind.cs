using System;
using UnityEditor;

namespace AF.Build.Editor
{
    public enum BuildKind
    {
        /// <summary>Local test build: development player, Info/Warning logs, the Pipeline runtime. Never given to players.</summary>
        Development,

        /// <summary>The build players and testers get: no development player, no development logs, no Pipeline runtime.</summary>
        Release,
    }

    /// <summary>What each kind builds with. Defines reach the player only from here, through <c>extraScriptingDefines</c>.</summary>
    public static class BuildKinds
    {
        /// <summary>Compiles <c>Log.Info</c> and <c>Log.Warning</c> into a player.</summary>
        public const string ShowLogs = "SHOW_LOGS";

        public static BuildOptions Options(BuildKind kind) => kind == BuildKind.Development ? BuildOptions.Development : BuildOptions.None;

        public static string[] Defines(BuildKind kind) => kind == BuildKind.Development ? new[] { ShowLogs } : Array.Empty<string>();
    }

    public sealed class BuildRequest
    {
        public BuildRequest(BuildKind kind) => Kind = kind;

        public BuildKind Kind { get; }

        /// <summary>
        /// Turns the placeholder Company/Product error of a Release build into a warning. Only for verifying the template
        /// itself, whose names are the placeholders; a game sets real names instead. Not offered in the menu.
        /// </summary>
        public bool AllowPlaceholderIdentity { get; set; }
    }
}

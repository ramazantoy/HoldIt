using AF.Core;
using UnityEditor;

namespace AF.Build.Editor
{
    /// <summary>AF/Build: the build menu. Results go to the console and to the report next to the build.</summary>
    internal static class BuildMenu
    {
        private const string MENU = FrameworkConstants.MenuRoot + "Build/";

        [MenuItem(MENU + "Development")]
        private static void Development() => Builder.Build(BuildKind.Development);

        [MenuItem(MENU + "Release")]
        private static void Release() => Builder.Build(BuildKind.Release);

        [MenuItem(MENU + "Check Release")]
        private static void CheckRelease() => Builder.Check(new BuildRequest(BuildKind.Release)).LogToConsole("Build check (Release)");
    }
}

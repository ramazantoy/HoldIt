using AF.Core;
using UnityEditor;
using UnityEditor.Build;

namespace AF.Build.Editor
{
    /// <summary>
    /// Refuses player builds that do not come from <see cref="Builder"/>: Unity's Build Profiles window, the Pipeline's
    /// <c>build run</c>, other scripts. Those skip the pre-build checks and the build menu's defines. Player builds made by
    /// the Test Runner pass. Runs before every other preprocessor, so a refused build changes nothing.
    /// </summary>
    internal sealed class BuildGuard : IPreprocessBuildWithContext
    {
        public int callbackOrder => -10000;

        public void OnPreprocessBuild(BuildCallbackContext context)
        {
            if (context.IsPlayerBuild)
                Enforce(BuildSession.IsActive, context.Report.summary.options);
        }

        internal static bool IsAllowed(bool fromBuilder, BuildOptions options) =>
            fromBuilder || (options & BuildOptions.IncludeTestAssemblies) != 0;

        internal static void Enforce(bool fromBuilder, BuildOptions options)
        {
            if (!IsAllowed(fromBuilder, options))
                throw new BuildFailedException(
                    $"Build from {FrameworkConstants.MenuRoot}Build (or {typeof(Builder).FullName}.Build in code). It runs the pre-build " +
                    "checks and passes the build's defines; other ways of building skip both.");
        }
    }

    /// <summary>Marks a build started by <see cref="Builder"/>. SessionState, because a build can reload the domain.</summary>
    internal static class BuildSession
    {
        private const string ACTIVE_KEY = FrameworkConstants.Prefix + ".Build.Active";

        public static bool IsActive => SessionState.GetBool(ACTIVE_KEY, false);

        public static void Begin() => SessionState.SetBool(ACTIVE_KEY, true);

        public static void End() => SessionState.EraseBool(ACTIVE_KEY);
    }
}

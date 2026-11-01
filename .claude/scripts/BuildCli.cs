using AF.Build.Editor;

// The Editor takes no commands while it builds: run from Bash in the background with a long timeout.
// unity command script run --file .claude/scripts/BuildCli.cs --entry ClaudeBuild.Development --timeout 3600
public static class ClaudeBuild
{
    public static string Development() => Builder.Build(BuildKind.Development).ToString();

    public static string Release() => Builder.Build(BuildKind.Release).ToString();

    /// <summary>The template's own Release build: its Company/Product names are placeholders.</summary>
    public static string ReleaseWithPlaceholderNames() =>
        Builder.Build(new BuildRequest(BuildKind.Release) { AllowPlaceholderIdentity = true }).ToString();
}

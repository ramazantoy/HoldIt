using AF.Build.Editor;
using AF.Core.Editor;

// unity command script run --file .claude/scripts/Validate.cs --entry ClaudeValidate.All
// unity command script run --file .claude/scripts/Validate.cs --entry ClaudeValidate.Release --timeout 600
public static class ClaudeValidate
{
    /// <summary>AF > Validate All: the project and every enabled build scene.</summary>
    public static string All() => ValidateAll.Run().ToString();

    /// <summary>The Release pre-build checks without building (~30 s). The template's placeholder names are an expected error.</summary>
    public static string Release() => Builder.Check(new BuildRequest(BuildKind.Release)).ToString();

    public static string Development() => Builder.Check(new BuildRequest(BuildKind.Development)).ToString();
}

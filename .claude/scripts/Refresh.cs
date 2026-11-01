using UnityEditor;

public static class ClaudeRefresh
{
    public static string Run()
    {
        AssetDatabase.Refresh();
        return "refreshed";
    }
}

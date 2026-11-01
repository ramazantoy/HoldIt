using UnityEditor;
using UnityEngine;

// unity command script run --file .claude/scripts/PlayMode.cs --entry ClaudePlayMode.Enter
// unity command script run --file .claude/scripts/PlayMode.cs --entry ClaudePlayMode.Step --args '["10"]'
// Follow the transition with `unity command editor status` (playMode). Always end with Exit: it also clears the pause.
public static class ClaudePlayMode
{
    public static string Enter()
    {
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
        return "entering";
    }

    /// <summary>Starts paused, to step through the first frames.</summary>
    public static string EnterPaused()
    {
        EditorApplication.isPaused = true;
        EditorApplication.isPlaying = true;
        return "entering paused";
    }

    /// <summary>Advances frames one by one; the only way the player loop moves while the Editor is not focused.</summary>
    public static string Step(string frames)
    {
        EditorApplication.isPaused = true;
        for (var i = 0; i < int.Parse(frames); i++)
            EditorApplication.Step();
        return $"frame {Time.frameCount}";
    }

    public static string Exit()
    {
        EditorApplication.isPlaying = false;
        EditorApplication.isPaused = false;
        return "exiting";
    }
}

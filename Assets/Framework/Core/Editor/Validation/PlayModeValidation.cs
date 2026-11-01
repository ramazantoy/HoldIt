using System.Linq;
using UnityEditor;

namespace AF.Core.Editor
{
    /// <summary>
    /// Validates the open scenes when Play is pressed. It never blocks Play; it only logs. The check runs on the edit-time
    /// state and the results are logged after entering Play Mode, so "Clear on Play" does not wipe them. They cross the
    /// domain reload in between through SessionState, as text.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeValidation
    {
        private const string TITLE = "Play Mode validation";
        private const string PENDING_KEY = FrameworkConstants.Prefix + ".PlayModeValidation.Pending";
        private const char SEPARATOR = '\u001F';

        static PlayModeValidation() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    var report = ValidateAll.RunOpenScenes();
                    var errors = report.Issues.Where(issue => issue.Severity == ValidationSeverity.Error).Select(issue => issue.Message);
                    SessionState.SetString(PENDING_KEY, string.Join(SEPARATOR, errors));
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    var pending = SessionState.GetString(PENDING_KEY, string.Empty);
                    SessionState.EraseString(PENDING_KEY);
                    if (pending.Length == 0)
                        return;
                    foreach (var message in pending.Split(SEPARATOR))
                        Log.Error($"{TITLE}: {message}");
                    break;
            }
        }
    }
}

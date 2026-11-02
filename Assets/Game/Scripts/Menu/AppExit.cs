using UnityEngine;

namespace HoldIt.Menu
{
    internal sealed class AppExit : IAppExit
    {
        public bool HasQuitButton => !Application.isMobilePlatform;

        public void Quit()
        {
#if UNITY_EDITOR
            // Application.Quit does nothing in the Editor.
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}

using AF.Core;
using AF.Save;
using Cysharp.Threading.Tasks;
using HoldIt.Levels;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace HoldIt.Editor
{
    /// <summary>
    /// Debug menus for the saved level progress, in Play mode: open every level, or start over. Both write the global data
    /// at once. An open level select shows the change the next time it opens.
    /// </summary>
    internal static class ProgressDebugMenu
    {
        private const string UNLOCK_ALL = "HoldIt/Progress/Unlock All";
        private const string RESET = "HoldIt/Progress/Reset";

        [MenuItem(UNLOCK_ALL)]
        private static void UnlockAll() => Change(progress => progress.UnlockAll(), "every level is open");

        [MenuItem(RESET)]
        private static void Reset() => Change(progress => progress.ResetProgress(), "back to a new game's progress");

        [MenuItem(UNLOCK_ALL, true)]
        [MenuItem(RESET, true)]
        private static bool CanChange() => Application.isPlaying;

        private static void Change(System.Action<LevelProgress> change, string what)
        {
            var root = Object.FindAnyObjectByType<GameRootScope>();
            if (root == null || root.Container == null)
            {
                Log.Error("Progress: no game root scope is running.");
                return;
            }
            if (root.Container.Resolve<ILevelProgress>() is not LevelProgress progress)
            {
                Log.Error("Progress: the root scope's ILevelProgress is not the saved LevelProgress.");
                return;
            }
            change(progress);
            root.Container.Resolve<ISaveService>().SaveGlobal().Forget();
            Log.Info($"Progress: {what} ({progress.Unlocked} open).");
        }
    }
}

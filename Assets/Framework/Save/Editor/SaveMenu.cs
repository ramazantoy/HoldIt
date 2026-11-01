using System.IO;
using AF.Core;
using UnityEditor;
using UnityEngine;

namespace AF.Save.Editor
{
    /// <summary>Editor shortcuts for the save files of this machine. The Editor and a Windows build share the same folder.</summary>
    internal static class SaveMenu
    {
        private const string ROOT = FrameworkConstants.MenuRoot + "Save/";

        [MenuItem(ROOT + "Open Save Folder")]
        private static void OpenSaveFolder()
        {
            var folder = Path.Combine(Application.persistentDataPath, FrameworkConstants.SaveFolder);
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }

        [MenuItem(ROOT + "Delete All Saves...")]
        private static void DeleteAllSaves()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Log.Warning("Delete All Saves runs in Edit Mode only.");
                return;
            }

            var saves = Path.Combine(Application.persistentDataPath, FrameworkConstants.SaveFolder);
            var device = Path.Combine(Application.persistentDataPath, FrameworkConstants.DeviceSaveFolder);
            if (!EditorUtility.DisplayDialog("Delete All Saves",
                    $"Deletes these folders and the save entries in PlayerPrefs:\n\n{saves}\n{device}\n\nThis cannot be undone.",
                    "Delete", "Cancel"))
                return;

            foreach (var folder in new[] { saves, device })
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }

            var prefs = new PlayerPrefsSaveStorage();
            foreach (var entry in prefs.List(string.Empty))
                prefs.Delete(entry);

            Log.Info("All saves deleted.");
        }
    }
}

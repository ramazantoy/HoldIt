using System.Collections.Generic;
using System.Linq;
using AF.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AF.Audio.Editor
{
    /// <summary>
    /// Sound cues without clips or output, AudioSources without an output mixer group (they bypass the channel volumes) and
    /// AudioListeners in scenes or prefabs (the root scope holds the game's only one).
    /// </summary>
    public sealed class AudioValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        public void ValidateProject(ValidationReport report)
        {
            var rootPrefab = ValidationTargets.FindRootPrefab();
            if (rootPrefab != null)
                CheckObjects(new[] { rootPrefab }, AssetDatabase.GetAssetPath(rootPrefab), report);
        }

        public void ValidateScene(Scene scene, ValidationReport report) => CheckObjects(scene.GetRootGameObjects(), scene.path, report);

        // Loads every cue and prefab: too slow for every Play.
        public void ValidateAssets(ValidationReport report)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(SoundCue)}", new[] { ASSETS_FOLDER }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cue = AssetDatabase.LoadAssetAtPath<SoundCue>(path);
                if (cue != null)
                    CheckCue(cue, path, report);
            }

            var rootPrefab = ValidationTargets.FindRootPrefab();
            var rootPath = rootPrefab != null ? AssetDatabase.GetAssetPath(rootPrefab) : null;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ASSETS_FOLDER }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == rootPath)
                    continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    CheckObjects(new[] { prefab }, path, report);
            }
        }

        internal static void CheckCue(SoundCue cue, string location, ValidationReport report)
        {
            if (cue.Clips == null || cue.Clips.Length == 0)
                report.Error($"{location}: the sound cue has no clips.", cue);
            else if (cue.Clips.Any(clip => clip == null))
                report.Error($"{location}: the sound cue has an empty clip slot.", cue);
            if (cue.Output == null)
                report.Error($"{location}: the sound cue has no output mixer group, so the channel volumes would not apply to it.", cue);
        }

        internal static void CheckObjects(IEnumerable<GameObject> roots, string location, ValidationReport report)
        {
            foreach (var root in roots)
            {
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    report.Error($"{ValidationTargets.Describe(location, listener)}: an AudioListener. The root scope holds the game's only " +
                                 "one so sound stays audible while scenes change; remove this one (IAudioListener.Follow picks what it follows).",
                        listener);
                }
                foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                {
                    if (source.outputAudioMixerGroup == null)
                    {
                        report.Error($"{ValidationTargets.Describe(location, source)}: an AudioSource without an output mixer group, so the " +
                                     "channel volumes do not apply to it. Assign one.", source);
                    }
                }
            }
        }
    }
}

using System.IO;
using AF.Core;
using HoldIt.Levels;
using UnityEditor;

namespace HoldIt.Editor
{
    /// <summary>
    /// "HoldIt/Levels/Generate": writes the levels from the project's <see cref="LevelCurve"/> as Level_01, Level_02... in
    /// Assets/Game/Levels and lists them, in order, in the level list there. A level that exists already gets the curve's
    /// numbers but keeps its map, so a level moved to another map stays there; it asks before overwriting. Levels past the
    /// curve's count are left as they are, out of the list.
    /// </summary>
    public static class LevelGenerator
    {
        public const string FOLDER = "Assets/Game/Levels";
        public const string LIST_PATH = FOLDER + "/Levels.asset";

        private const string MENU = "HoldIt/Levels/Generate";

        [MenuItem(MENU)]
        private static void GenerateFromMenu()
        {
            var curve = FindCurve();
            if (curve == null)
            {
                EditorUtility.DisplayDialog("Generate levels", "There is no Level Curve asset in the project.", "OK");
                return;
            }

            var existing = CountExisting(curve.Count);
            if (existing > 0 && !EditorUtility.DisplayDialog("Generate levels",
                    $"{existing} of the {curve.Count} levels exist already. Overwrite their numbers? Their maps stay.",
                    "Overwrite", "Cancel"))
                return;

            Generate(curve);
        }

        /// <summary>Writes every level of the curve and the list, without asking, and returns the list.</summary>
        public static LevelList Generate(LevelCurve curve)
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(FOLDER), Path.GetFileName(FOLDER));

            var levels = new LevelDefinition[curve.Count];
            for (var i = 0; i < curve.Count; i++)
                levels[i] = Write(PathOf(i), curve.Ramp.At(i, curve.Count), curve);

            var list = LoadOrCreate<LevelList>(LIST_PATH);
            var serialized = new SerializedObject(list);
            var entries = serialized.FindProperty("_levels");
            entries.arraySize = levels.Length;
            for (var i = 0; i < levels.Length; i++)
                entries.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(list);

            Log.Info($"Wrote {levels.Length} levels to {FOLDER} and listed them in {LIST_PATH}.");
            return list;
        }

        public static string PathOf(int index) => $"{FOLDER}/Level_{index + 1:00}.asset";

        private static LevelDefinition Write(string path, LevelNumbers numbers, LevelCurve curve)
        {
            var level = LoadOrCreate<LevelDefinition>(path);
            var serialized = new SerializedObject(level);
            var map = serialized.FindProperty("_map");
            if (map.objectReferenceValue == null)
                map.objectReferenceValue = curve.Map;
            serialized.FindProperty("_crowd").intValue = numbers.Crowd;
            serialized.FindProperty("_spawnStart").floatValue = numbers.SpawnStart;
            serialized.FindProperty("_spawnEnd").floatValue = numbers.SpawnEnd;
            serialized.FindProperty("_maxCharacters").intValue = numbers.MaxCharacters;
            serialized.FindProperty("_sizes._thin").floatValue = numbers.Sizes.Thin;
            serialized.FindProperty("_sizes._normal").floatValue = numbers.Sizes.Normal;
            serialized.FindProperty("_sizes._fat").floatValue = numbers.Sizes.Fat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(level);
            return level;
        }

        private static T LoadOrCreate<T>(string path) where T : UnityEngine.ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = UnityEngine.ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static int CountExisting(int count)
        {
            var existing = 0;
            for (var i = 0; i < count; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(PathOf(i)) != null)
                    existing++;
            }
            return existing;
        }

        private static LevelCurve FindCurve()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(LevelCurve)}"))
            {
                var curve = AssetDatabase.LoadAssetAtPath<LevelCurve>(AssetDatabase.GUIDToAssetPath(guid));
                if (curve != null)
                    return curve;
            }
            return null;
        }
    }
}

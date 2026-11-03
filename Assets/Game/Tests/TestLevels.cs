using HoldIt.Board;
using HoldIt.Levels;
using UnityEditor;
using UnityEngine;

namespace HoldIt.Tests
{
    /// <summary>Level and map assets made in memory for the tests; the caller destroys them.</summary>
    internal static class TestLevels
    {
        public static LevelDefinition Level(int crowd, int maxCharacters = 8, float spawnStart = 4f, float spawnEnd = 2f,
            SizeWeights? sizes = null, MapDefinition map = null)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var serialized = new SerializedObject(level);
            serialized.FindProperty("_map").objectReferenceValue = map;
            serialized.FindProperty("_crowd").intValue = crowd;
            serialized.FindProperty("_maxCharacters").intValue = maxCharacters;
            serialized.FindProperty("_spawnStart").floatValue = spawnStart;
            serialized.FindProperty("_spawnEnd").floatValue = spawnEnd;
            var weights = sizes ?? SizeWeights.Default;
            serialized.FindProperty("_sizes._thin").floatValue = weights.Thin;
            serialized.FindProperty("_sizes._normal").floatValue = weights.Normal;
            serialized.FindProperty("_sizes._fat").floatValue = weights.Fat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return level;
        }

        public static LevelList List(params LevelDefinition[] levels)
        {
            var list = ScriptableObject.CreateInstance<LevelList>();
            var serialized = new SerializedObject(list);
            var entries = serialized.FindProperty("_levels");
            entries.arraySize = levels.Length;
            for (var i = 0; i < levels.Length; i++)
                entries.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return list;
        }

        public static MapDefinition Map(params string[] rows)
        {
            var map = ScriptableObject.CreateInstance<MapDefinition>();
            var serialized = new SerializedObject(map);
            var entries = serialized.FindProperty("_rows");
            entries.arraySize = rows.Length;
            for (var i = 0; i < rows.Length; i++)
                entries.GetArrayElementAtIndex(i).stringValue = rows[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return map;
        }
    }
}

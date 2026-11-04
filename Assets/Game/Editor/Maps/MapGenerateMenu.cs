using System;
using System.Collections.Generic;
using System.Text;
using AF.Core;
using HoldIt.Board;
using HoldIt.Levels;
using HoldIt.Restrooms;
using UnityEditor;

namespace HoldIt.Editor.Maps
{
    /// <summary>
    /// "HoldIt/Maps/Generate": makes every listed level's map from its band's recipe (<see cref="MapGeneratorConfig"/>) as
    /// Map_NN in Assets/Game/Board/Maps and puts it on Level_NN. Hand-made maps (<see cref="MapDefinition.HandMade"/>) are
    /// left as they are, the level's own and Map_NN alike; a hand-made map that several levels share stays with the first of
    /// them. It asks before overwriting generated maps.
    /// </summary>
    public static class MapGenerateMenu
    {
        public const string FOLDER = "Assets/Game/Board/Maps";

        private const string MENU = "HoldIt/Maps/Generate";
        private const string TITLE = "Generate maps";

        [MenuItem(MENU)]
        private static void GenerateFromMenu()
        {
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelGenerator.LIST_PATH);
            if (list == null)
            {
                EditorUtility.DisplayDialog(TITLE, $"There is no level list at {LevelGenerator.LIST_PATH}.", "OK");
                return;
            }

            var existing = 0;
            for (var i = 0; i < list.Levels.Count; i++)
            {
                var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(PathOf(i + 1));
                if (map != null && !map.HandMade)
                    existing++;
            }
            if (existing > 0 && !EditorUtility.DisplayDialog(TITLE,
                    $"{existing} generated maps exist already. Make them again? Hand-made maps stay.", "Generate", "Cancel"))
                return;

            EditorUtility.DisplayDialog(TITLE, Generate(), "OK");
        }

        /// <summary>Generates the listed levels' maps (all when <paramref name="only"/> is empty) without asking; returns a summary.</summary>
        public static string Generate(params int[] only)
        {
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelGenerator.LIST_PATH);
            var config = FindConfig();
            if (list == null || config == null)
                return "Generating maps needs the level list and a Map Generator Config asset.";

            var geometry = MapValidator.ProjectGeometry();
            var summary = new StringBuilder();
            var written = 0;
            // A hand-made map stays with the first level on it; the others on it get their own.
            var keptBy = new Dictionary<MapDefinition, int>();
            for (var i = 0; i < list.Levels.Count; i++)
            {
                var map = list.Levels[i] != null ? list.Levels[i].Map : null;
                if (map != null && map.HandMade && !keptBy.ContainsKey(map))
                    keptBy.Add(map, i + 1);
            }

            for (var i = 0; i < list.Levels.Count; i++)
            {
                var number = i + 1;
                if (only.Length > 0 && Array.IndexOf(only, number) < 0)
                    continue;
                var line = GenerateLevel(list.Levels[i], number, keptBy, config, geometry, ref written);
                summary.AppendLine(line);
                Log.Info(line);
            }
            return $"Wrote {written} maps to {FOLDER}.\n{summary}";
        }

        public static string PathOf(int number) => $"{FOLDER}/Map_{number:00}.asset";

        private static string GenerateLevel(LevelDefinition level, int number, Dictionary<MapDefinition, int> keptBy,
            MapGeneratorConfig config, StallGeometry geometry, ref int written)
        {
            if (level == null)
                return $"Level {number}: missing from the list.";
            if (level.Map != null && level.Map.HandMade && keptBy.TryGetValue(level.Map, out var keeper) && keeper == number)
                return $"Level {number}: keeps its hand-made map '{level.Map.name}'.";

            var path = PathOf(number);
            var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(path);
            if (map != null && map.HandMade)
            {
                Bind(level, map);
                return $"Level {number}: keeps the hand-made '{map.name}'.";
            }
            if (!config.TryBandOf(number, out var band))
                return $"Level {number}: no band holds it; its map is unchanged.";

            GeneratedMap generated;
            try
            {
                generated = MapGenerator.Generate(band.Recipe, geometry, config.Limits, config.SeedOf(number),
                    config.Candidates, config.Attempts);
            }
            catch (InvalidOperationException exception)
            {
                return $"Level {number} ({band.Name}): {exception.Message}";
            }

            if (map == null)
            {
                map = UnityEngine.ScriptableObject.CreateInstance<MapDefinition>();
                AssetDatabase.CreateAsset(map, path);
            }
            Write(map, generated.Rows, generated.Stalls, handMade: false);
            Bind(level, map);
            written++;
            return $"Level {number} ({band.Name}): {map.name}, seed {generated.Seed} try {generated.Attempt}, " +
                   $"{generated.Stalls} stall(s), {generated.Metrics}";
        }

        /// <summary>Writes a map's rows and stalls through its serialized fields and saves it.</summary>
        public static void Write(MapDefinition map, IReadOnlyList<string> rows, int stalls, bool handMade)
        {
            var serialized = new SerializedObject(map);
            var entries = serialized.FindProperty("_rows");
            entries.arraySize = rows.Count;
            for (var i = 0; i < rows.Count; i++)
                entries.GetArrayElementAtIndex(i).stringValue = rows[i];
            serialized.FindProperty("_stallsPerRestroom").intValue = stalls;
            serialized.FindProperty("_handMade").boolValue = handMade;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(map);
        }

        private static void Bind(LevelDefinition level, MapDefinition map)
        {
            if (level.Map == map)
                return;
            var serialized = new SerializedObject(level);
            serialized.FindProperty("_map").objectReferenceValue = map;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(level);
        }

        public static MapGeneratorConfig FindConfig()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(MapGeneratorConfig)}", new[] { "Assets" }))
            {
                var config = AssetDatabase.LoadAssetAtPath<MapGeneratorConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null)
                    return config;
            }
            return null;
        }
    }
}

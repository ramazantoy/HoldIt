using System.Collections.Generic;
using System.Linq;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Levels;
using UnityEditor;

namespace HoldIt.Editor
{
    /// <summary>
    /// Reports level lists that are empty or miss a level, and levels that cannot be played: no map, nobody coming, no room
    /// on the board, gaps that are not positive or grow over the level, or no body size to pick. Every level has its own
    /// map; two levels on the same map are reported too.
    /// </summary>
    public sealed class LevelValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        public void ValidateAssets(ValidationReport report)
        {
            foreach (var list in Load<LevelList>())
                CheckList(list, report);
            var levels = Load<LevelDefinition>().ToList();
            foreach (var level in levels)
                CheckLevel(level, report);
            CheckSharedMaps(levels, report);
        }

        /// <summary>One error per map that more than one of <paramref name="levels"/> is played on, naming them.</summary>
        internal static void CheckSharedMaps(IEnumerable<LevelDefinition> levels, ValidationReport report)
        {
            var byMap = new Dictionary<MapDefinition, List<LevelDefinition>>();
            var order = new List<MapDefinition>();
            foreach (var level in levels)
            {
                if (level == null || level.Map == null)
                    continue;
                if (!byMap.TryGetValue(level.Map, out var users))
                {
                    users = new List<LevelDefinition>();
                    byMap.Add(level.Map, users);
                    order.Add(level.Map);
                }
                users.Add(level);
            }

            foreach (var map in order)
            {
                var users = byMap[map];
                if (users.Count < 2)
                    continue;
                var names = string.Join(", ", users.Select(level => $"'{level.name}'").OrderBy(name => name));
                report.Error($"Map '{map.name}' is used by {users.Count} levels ({names}); every level has its own map.", map);
            }
        }

        internal static void CheckList(LevelList list, ValidationReport report)
        {
            if (list.Levels.Count == 0)
                report.Error($"Level list '{list.name}' is empty.", list);
            for (var i = 0; i < list.Levels.Count; i++)
            {
                if (list.Levels[i] == null)
                    report.Error($"Level list '{list.name}': level {i + 1} is missing.", list);
            }
        }

        internal static void CheckLevel(LevelDefinition level, ValidationReport report)
        {
            if (level.Map == null)
                report.Error($"Level '{level.name}' has no map.", level);
            if (level.Crowd < 1)
                report.Error($"Level '{level.name}' brings nobody (crowd {level.Crowd}).", level);
            if (level.MaxCharacters < 1)
                report.Error($"Level '{level.name}' lets nobody on the board (at most {level.MaxCharacters}).", level);
            if (level.SpawnStart <= 0f || level.SpawnEnd <= 0f)
                report.Error($"Level '{level.name}': the gaps between arrivals must be positive.", level);
            else if (level.SpawnEnd > level.SpawnStart)
                report.Error($"Level '{level.name}': the gaps between arrivals grow over the level; the last must not be longer than the first.", level);

            var sizes = level.Sizes;
            if (sizes.Thin < 0f || sizes.Normal < 0f || sizes.Fat < 0f)
                report.Error($"Level '{level.name}': a body size weight is negative.", level);
            else if (sizes.Total <= 0f)
                report.Error($"Level '{level.name}': every body size weight is 0.", level);
        }

        private static IEnumerable<T> Load<T>() where T : UnityEngine.Object
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { ASSETS_FOLDER }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    yield return asset;
            }
        }
    }
}

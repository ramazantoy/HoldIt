using System;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Restrooms;
using UnityEditor;

namespace HoldIt.Editor
{
    /// <summary>
    /// Reports maps that do not parse (<see cref="MapParser"/>), cannot be played (<see cref="MapCheck"/>), or whose
    /// restrooms cannot hold their stalls (<see cref="RestroomLayout.Problems"/>, with the project's restroom config).
    /// </summary>
    public sealed class MapValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        public void ValidateAssets(ValidationReport report)
        {
            var geometry = ProjectGeometry();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(MapDefinition)}", new[] { ASSETS_FOLDER }))
            {
                var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (map != null)
                    Check(map, geometry, report);
            }
        }

        internal static void Check(MapDefinition map, StallGeometry geometry, ValidationReport report)
        {
            ITileGrid grid;
            try
            {
                grid = MapParser.Parse(map.Rows);
            }
            catch (MapFormatException exception)
            {
                report.Error($"Map '{map.name}': {exception.Message}", map);
                return;
            }

            var problems = MapCheck.Problems(grid);
            foreach (var problem in problems)
                report.Error($"Map '{map.name}': {problem}", map);
            if (problems.Count > 0)
                return;

            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                foreach (var problem in RestroomLayout.Problems(grid, gender, map.StallsPerRestroom, geometry))
                    report.Error($"Map '{map.name}': {problem}", map);
            }
        }

        // The game has one restroom config; without one the defaults stand in.
        private static StallGeometry ProjectGeometry()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(RestroomConfig)}", new[] { ASSETS_FOLDER }))
            {
                var config = AssetDatabase.LoadAssetAtPath<RestroomConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null)
                    return config.Geometry;
            }
            return StallGeometry.Default;
        }
    }
}

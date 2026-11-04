using System;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Editor.Maps;
using HoldIt.Restrooms;
using UnityEditor;

namespace HoldIt.Editor
{
    /// <summary>
    /// Reports maps that do not parse (<see cref="MapParser"/>), cannot be played (<see cref="MapCheck"/>), or whose
    /// restrooms cannot hold their stalls (<see cref="RestroomLayout.Problems"/>, with the project's restroom config). A map
    /// beyond the size limits (<see cref="MapLimits"/>, from the project's map generator config) is a warning: it plays, but
    /// its tiles are smaller on a phone.
    /// </summary>
    public sealed class MapValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        public void ValidateAssets(ValidationReport report)
        {
            var geometry = ProjectGeometry();
            var limits = ProjectLimits();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(MapDefinition)}", new[] { ASSETS_FOLDER }))
            {
                var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (map != null)
                    Check(map, geometry, limits, report);
            }
        }

        internal static void Check(MapDefinition map, StallGeometry geometry, MapLimits limits, ValidationReport report)
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

            foreach (var problem in limits.Problems(grid.Cols, grid.Rows))
                report.Warning($"Map '{map.name}': {problem}", map);

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
        internal static StallGeometry ProjectGeometry()
        {
            var config = FindFirst<RestroomConfig>();
            return config != null ? config.Geometry : StallGeometry.Default;
        }

        // The map generator's config holds the limits; without one the defaults stand in.
        internal static MapLimits ProjectLimits()
        {
            var config = FindFirst<MapGeneratorConfig>();
            return config != null ? config.Limits : MapLimits.Default;
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { ASSETS_FOLDER }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    return asset;
            }
            return null;
        }
    }
}

using AF.Core.Editor;
using HoldIt.Board;
using UnityEditor;

namespace HoldIt.Editor
{
    /// <summary>Reports maps that do not parse (<see cref="MapParser"/>) or cannot be played (<see cref="MapCheck"/>).</summary>
    public sealed class MapValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        public void ValidateAssets(ValidationReport report)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(MapDefinition)}", new[] { ASSETS_FOLDER }))
            {
                var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (map != null)
                    Check(map, report);
            }
        }

        internal static void Check(MapDefinition map, ValidationReport report)
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

            foreach (var problem in MapCheck.Problems(grid))
                report.Error($"Map '{map.name}': {problem}", map);
        }
    }
}

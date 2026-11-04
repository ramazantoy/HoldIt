using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Editor.Maps
{
    /// <summary>
    /// What "HoldIt/Maps/Generate" makes the levels' maps from (<see cref="MapGenerator"/>): a recipe for each band of
    /// levels, how hard it looks for a good map, and the size limits every map is checked against ("AF/Validate All" too).
    /// Editor only.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Maps/Map Generator Config", fileName = "MapGeneratorConfig")]
    public sealed class MapGeneratorConfig : ScriptableObject
    {
        [SerializeField] private MapLimits _limits = MapLimits.Default;

        [SerializeField, Tooltip("Added to every level's number to make its seed; change it to get a fresh set of maps.")]
        private int _seed = 8000;

        [SerializeField, Min(1), Tooltip("Playable sketches compared for each map; the one whose walks are nearest the recipe's middle wins.")]
        private int _candidates = 12;

        [SerializeField, Min(1), Tooltip("Sketches tried for each map before giving up.")]
        private int _attempts = 4000;

        [SerializeField, Tooltip("Which recipe each level's map is made from, by level number. Level 1 keeps its hand-made Lobby.")]
        private MapBand[] _bands = MapBand.Defaults();

        public MapLimits Limits => _limits;
        public int Seed => _seed;
        public int Candidates => _candidates;
        public int Attempts => _attempts;
        public IReadOnlyList<MapBand> Bands => _bands;

        /// <summary>The band of level <paramref name="number"/> (from 1), or false when no band holds it.</summary>
        public bool TryBandOf(int number, out MapBand band)
        {
            foreach (var candidate in _bands)
            {
                if (number >= candidate.FirstLevel && number <= candidate.LastLevel)
                {
                    band = candidate;
                    return true;
                }
            }
            band = default;
            return false;
        }

        public int SeedOf(int number) => _seed + number;

        [ContextMenu("Reset Bands to M8's Defaults")]
        private void ResetBands()
        {
            UnityEditor.Undo.RecordObject(this, "Reset map bands");
            _bands = MapBand.Defaults();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    /// <summary>Levels <see cref="FirstLevel"/> to <see cref="LastLevel"/> get their maps from one recipe.</summary>
    [Serializable]
    public struct MapBand
    {
        [SerializeField, Tooltip("What the band brings in, for the reader.")]
        private string _name;

        [SerializeField, Min(1)] private int _firstLevel;
        [SerializeField, Min(1)] private int _lastLevel;
        [SerializeField] private MapRecipe _recipe;

        public MapBand(string name, int firstLevel, int lastLevel, MapRecipe recipe)
        {
            _name = name;
            _firstLevel = firstLevel;
            _lastLevel = lastLevel;
            _recipe = recipe;
        }

        public string Name => _name;
        public int FirstLevel => _firstLevel;
        public int LastLevel => _lastLevel;
        public MapRecipe Recipe => _recipe;

        /// <summary>
        /// M8's bands: open floor (2), a narrow shortcut once Fat characters come (3), a bottleneck (4–5), a second entrance
        /// (6–8), side doors and winding halls (9–11), one-stall restrooms (12–15), three stalls far apart (16–20), the
        /// mixes (21–25) and everything at once (26–30).
        /// </summary>
        public static MapBand[] Defaults()
        {
            var bottom = EntranceEdges.Bottom;
            var sides = EntranceEdges.Left | EntranceEdges.Right;
            var all = EntranceEdges.Bottom | EntranceEdges.Left | EntranceEdges.Right;
            return new[]
            {
                new MapBand("Open floor", 2, 2, new MapRecipe(
                    cols: V(10, 10), rows: V(12, 13), placement: RestroomPlacement.TopSideBySide, stalls: V(2, 2),
                    extraWidth: V(0, 2), entrances: V(1, 1), entranceEdges: bottom, lobby: V(16, 24), barriers: V(0, 0),
                    narrowGaps: V(0, 0), winding: false, blocks: V(2, 4), meanPath: F(6f, 11f), fatDetour: F(0f, 0f))),
                new MapBand("Narrow shortcut", 3, 3, new MapRecipe(
                    cols: V(10, 10), rows: V(13, 14), placement: RestroomPlacement.TopSideBySide, stalls: V(2, 2),
                    extraWidth: V(0, 2), entrances: V(1, 1), entranceEdges: bottom, lobby: V(16, 22), barriers: V(1, 1),
                    narrowGaps: V(1, 1), winding: false, blocks: V(1, 3), meanPath: F(8f, 12f), fatDetour: F(0.8f, 3f))),
                new MapBand("Bottleneck", 4, 5, new MapRecipe(
                    cols: V(9, 10), rows: V(13, 15), placement: RestroomPlacement.TopSideBySide, stalls: V(2, 2),
                    extraWidth: V(0, 1), entrances: V(1, 1), entranceEdges: bottom, lobby: V(14, 22), barriers: V(2, 2),
                    narrowGaps: V(1, 1), winding: false, blocks: V(0, 2), meanPath: F(9f, 13f), fatDetour: F(2.5f, 6f))),
                new MapBand("Second entrance", 6, 8, new MapRecipe(
                    cols: V(10, 10), rows: V(13, 15), placement: RestroomPlacement.Any, stalls: V(2, 2),
                    extraWidth: V(0, 1), entrances: V(2, 2), entranceEdges: sides, lobby: V(14, 22), barriers: V(1, 1),
                    narrowGaps: V(0, 1), winding: false, blocks: V(2, 4), meanPath: F(9f, 13f), fatDetour: F(0f, 4f))),
                new MapBand("Side doors, winding halls", 9, 11, new MapRecipe(
                    cols: V(10, 10), rows: V(15, 17), placement: RestroomPlacement.Sides, stalls: V(2, 2),
                    extraWidth: V(0, 1), entrances: V(1, 2), entranceEdges: bottom, lobby: V(12, 20), barriers: V(1, 1),
                    narrowGaps: V(0, 0), winding: true, blocks: V(1, 3), meanPath: F(13f, 20f), fatDetour: F(0f, 5f))),
                new MapBand("One stall", 12, 15, new MapRecipe(
                    cols: V(8, 10), rows: V(14, 17), placement: RestroomPlacement.Any, stalls: V(1, 1),
                    extraWidth: V(0, 1), entrances: V(1, 2), entranceEdges: all, lobby: V(12, 18), barriers: V(1, 2),
                    narrowGaps: V(1, 1), winding: true, blocks: V(1, 3), meanPath: F(11f, 17f), fatDetour: F(1f, 6f))),
                new MapBand("Three stalls, far apart", 16, 20, new MapRecipe(
                    cols: V(10, 10), rows: V(15, 17), placement: RestroomPlacement.TopAndSide, stalls: V(3, 3),
                    extraWidth: V(0, 0), entrances: V(2, 2), entranceEdges: all, lobby: V(14, 22), barriers: V(1, 2),
                    narrowGaps: V(1, 1), winding: false, blocks: V(1, 3), meanPath: F(12f, 18f), fatDetour: F(1f, 6f))),
                new MapBand("Mixes", 21, 25, new MapRecipe(
                    cols: V(9, 10), rows: V(15, 17), placement: RestroomPlacement.Any, stalls: V(1, 1),
                    extraWidth: V(0, 1), entrances: V(2, 2), entranceEdges: all, lobby: V(12, 18), barriers: V(2, 2),
                    narrowGaps: V(1, 2), winding: true, blocks: V(1, 3), meanPath: F(13f, 18f), fatDetour: F(2f, 7f))),
                new MapBand("Everything", 26, 30, new MapRecipe(
                    cols: V(9, 10), rows: V(16, 18), placement: RestroomPlacement.Any, stalls: V(1, 2),
                    extraWidth: V(0, 1), entrances: V(3, 3), entranceEdges: all, lobby: V(8, 12), barriers: V(2, 3),
                    narrowGaps: V(1, 2), winding: true, blocks: V(1, 3), meanPath: F(13f, 19f), fatDetour: F(2f, 8f))),
            };
        }

        private static Vector2Int V(int min, int max) => new Vector2Int(min, max);
        private static Vector2 F(float min, float max) => new Vector2(min, max);
    }
}

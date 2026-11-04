using System;
using System.Collections.Generic;
using System.Globalization;
using HoldIt.Board;
using HoldIt.Paths;

namespace HoldIt.Editor.Maps
{
    /// <summary>
    /// What a playable map (<see cref="MapCheck"/>) asks of the player, measured without playing it: its size, how many
    /// separate entrances and lobby cells it has, and how far its lobby is from the restrooms. Walks are the ones a player
    /// can draw (<see cref="PathRules.IsDrawable"/>): never through the other gender's door, and for Fat characters never
    /// through a narrow passage. Lengths are in steps.
    /// </summary>
    public readonly struct MapMetrics
    {
        public readonly int Cols;
        public readonly int Rows;

        /// <summary>Entrance tiles that touch side by side are one entrance.</summary>
        public readonly int Entrances;

        public readonly int LobbyCells;
        public readonly int NarrowCells;

        /// <summary>Steps from a lobby cell to its gender's door, over every lobby cell and both genders.</summary>
        public readonly float MeanPath;

        /// <summary><see cref="MeanPath"/> for Fat characters, over the lobby cells they can reach.</summary>
        public readonly float MeanFatPath;

        /// <summary>The longest of those walks, for anyone.</summary>
        public readonly int LongestPath;

        private MapMetrics(int cols, int rows, int entrances, int lobbyCells, int narrowCells, float meanPath,
            float meanFatPath, int longestPath)
        {
            Cols = cols;
            Rows = rows;
            Entrances = entrances;
            LobbyCells = lobbyCells;
            NarrowCells = narrowCells;
            MeanPath = meanPath;
            MeanFatPath = meanFatPath;
            LongestPath = longestPath;
        }

        /// <summary>How much farther Fat characters walk on average, because narrow passages send them round.</summary>
        public float FatDetour => MeanFatPath - MeanPath;

        /// <summary>Measures a map that passes <see cref="MapCheck"/>.</summary>
        public static MapMetrics Of(ITileGrid grid)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));

            var lobby = new List<Cell>();
            var narrow = 0;
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var col = 0; col < grid.Cols; col++)
                {
                    var type = grid.At(new Cell(col, row)).Type;
                    if (type == TileType.Lobby)
                        lobby.Add(new Cell(col, row));
                    else if (type == TileType.Narrow)
                        narrow++;
                }
            }

            var walk = new Walks();
            var fat = new Walks();
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                var doors = grid.DoorsOf(gender);
                if (doors.Count != 1)
                    continue;
                walk.Add(StepsFrom(grid, doors[0], narrowAllowed: true), lobby);
                fat.Add(StepsFrom(grid, doors[0], narrowAllowed: false), lobby);
            }

            return new MapMetrics(grid.Cols, grid.Rows, CountGroups(grid, grid.Entrances), lobby.Count, narrow,
                walk.Mean, fat.Mean, Math.Max(walk.Longest, fat.Longest));
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "{0}x{1}, {2} entrance(s), {3} lobby, {4} narrow, path {5:0.0} (Fat {6:0.0}, +{7:0.0}), longest {8}",
            Cols, Rows, Entrances, LobbyCells, NarrowCells, MeanPath, MeanFatPath, FatDetour, LongestPath);

        // Breadth-first from the door over drawable tiles; the other door is never walked through. Walks are reversible,
        // so a cell's distance from the door is its walk to it.
        private static Dictionary<Cell, int> StepsFrom(ITileGrid grid, Cell door, bool narrowAllowed)
        {
            var steps = new Dictionary<Cell, int> { [door] = 0 };
            var queue = new Queue<Cell>();
            queue.Enqueue(door);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var direction in Directions.All)
                {
                    var next = cell.Step(direction);
                    if (steps.ContainsKey(next) || !PathRules.IsDrawable(grid, next))
                        continue;
                    var type = grid.At(next).Type;
                    if (type == TileType.Door || (!narrowAllowed && type == TileType.Narrow))
                        continue;
                    steps[next] = steps[cell] + 1;
                    queue.Enqueue(next);
                }
            }
            return steps;
        }

        private static int CountGroups(ITileGrid grid, IReadOnlyList<Cell> cells)
        {
            var members = new HashSet<Cell>(cells);
            var seen = new HashSet<Cell>();
            var groups = 0;
            foreach (var start in cells)
            {
                if (!seen.Add(start))
                    continue;
                groups++;
                var queue = new Queue<Cell>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var direction in Directions.All)
                    {
                        var next = cell.Step(direction);
                        if (grid.InBounds(next) && members.Contains(next) && seen.Add(next))
                            queue.Enqueue(next);
                    }
                }
            }
            return groups;
        }

        private sealed class Walks
        {
            private int _total;
            private int _count;

            public int Longest { get; private set; }
            public float Mean => _count > 0 ? _total / (float)_count : 0f;

            public void Add(Dictionary<Cell, int> steps, List<Cell> cells)
            {
                foreach (var cell in cells)
                {
                    if (!steps.TryGetValue(cell, out var length))
                        continue;
                    _total += length;
                    _count++;
                    Longest = Math.Max(Longest, length);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using HoldIt.Board;
using HoldIt.Restrooms;
using UnityEngine;
using Random = System.Random;

namespace HoldIt.Editor.Maps
{
    /// <summary>
    /// Makes maps from a <see cref="MapRecipe"/>, in the text format <see cref="MapParser"/> reads. A sketch is laid out in
    /// bands from the top: the restrooms, then barriers (walls across the map with open and narrow gaps) and wall blocks,
    /// then the lobby by the entrances. Floor nobody can reach becomes wall, and half the sketches are mirrored. A sketch
    /// can still be unplayable, so <see cref="Generate"/> keeps only those without <see cref="Problems"/> and, of the first
    /// few, returns the one whose walks are nearest the recipe's middle. The same seed always gives the same map.
    /// </summary>
    public static class MapGenerator
    {
        private const float Epsilon = 1e-4f;

        /// <summary>The best of the first <paramref name="candidates"/> playable sketches in at most <paramref name="attempts"/> tries.</summary>
        /// <exception cref="InvalidOperationException">No sketch was playable.</exception>
        public static GeneratedMap Generate(MapRecipe recipe, StallGeometry geometry, MapLimits limits, int seed,
            int candidates, int attempts)
        {
            var random = new Random(seed);
            GeneratedMap best = null;
            var bestScore = float.MaxValue;
            var middle = (recipe.MeanPath.x + recipe.MeanPath.y) * 0.5f;
            var found = 0;
            for (var attempt = 0; attempt < attempts && found < candidates; attempt++)
            {
                var sketch = Sketch(recipe, geometry, random);
                if (sketch == null || Problems(sketch.Rows, sketch.Stalls, recipe, geometry, limits).Count > 0)
                    continue;

                found++;
                var metrics = MapMetrics.Of(MapParser.Parse(sketch.Rows));
                var score = Math.Abs(metrics.MeanPath - middle);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new GeneratedMap(sketch.Rows, sketch.Stalls, metrics, seed, attempt);
                }
            }

            return best ?? throw new InvalidOperationException(
                $"No playable map in {attempts} tries with seed {seed}; the recipe may ask for too much in too little room.");
        }

        /// <summary>
        /// Why a map does not do: it does not parse or is not playable (<see cref="MapCheck"/>), its stalls do not fit
        /// (<see cref="RestroomLayout.Problems"/>), it is beyond the limits, a lobby tile is out of Fat characters' reach, or
        /// it misses the recipe (entrances, lobby, walks). Empty when it does.
        /// </summary>
        public static IReadOnlyList<string> Problems(IReadOnlyList<string> rows, int stalls, MapRecipe recipe,
            StallGeometry geometry, MapLimits limits)
        {
            var problems = new List<string>();
            TileGrid grid;
            try
            {
                grid = MapParser.Parse(rows);
            }
            catch (MapFormatException exception)
            {
                problems.Add(exception.Message);
                return problems;
            }

            problems.AddRange(MapCheck.Problems(grid));
            if (problems.Count > 0)
                return problems;
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
                problems.AddRange(RestroomLayout.Problems(grid, gender, stalls, geometry));
            problems.AddRange(limits.Problems(grid.Cols, grid.Rows));
            if (!LobbyReachableForFat(grid))
                problems.Add("A lobby tile is out of Fat characters' reach.");

            var metrics = MapMetrics.Of(grid);
            if (!Within(metrics.Entrances, recipe.Entrances))
                problems.Add($"It has {metrics.Entrances} entrances, not {Range(recipe.Entrances)}.");
            if (!Within(metrics.LobbyCells, recipe.Lobby))
                problems.Add($"It has {metrics.LobbyCells} lobby tiles, not {Range(recipe.Lobby)}.");
            if (metrics.MeanPath < Mathf.Min(recipe.MeanPath.x, recipe.MeanPath.y) - Epsilon ||
                metrics.MeanPath > Mathf.Max(recipe.MeanPath.x, recipe.MeanPath.y) + Epsilon)
                problems.Add(string.Format(CultureInfo.InvariantCulture, "Its walks average {0:0.0} steps, not {1:0.0}–{2:0.0}.",
                    metrics.MeanPath, recipe.MeanPath.x, recipe.MeanPath.y));
            if (metrics.FatDetour < Mathf.Min(recipe.FatDetour.x, recipe.FatDetour.y) - Epsilon ||
                metrics.FatDetour > Mathf.Max(recipe.FatDetour.x, recipe.FatDetour.y) + Epsilon)
                problems.Add(string.Format(CultureInfo.InvariantCulture,
                    "Fat characters walk {0:0.0} steps more, not {1:0.0}–{2:0.0}.", metrics.FatDetour, recipe.FatDetour.x,
                    recipe.FatDetour.y));
            return problems;
        }

        /// <summary>One sketch from the recipe, or null when its parts did not fit; it may still have problems.</summary>
        public static MapSketch Sketch(MapRecipe recipe, StallGeometry geometry, Random random)
        {
            var cols = Pick(random, recipe.Cols);
            var rows = Pick(random, recipe.Rows);
            var stalls = Math.Max(1, Pick(random, recipe.Stalls));
            var across = Mathf.CeilToInt(RestroomLayout.MinWidth(stalls, geometry) - Epsilon) + Pick(random, recipe.ExtraWidth);
            var depth = Mathf.CeilToInt(RestroomLayout.MinDepth(geometry) - Epsilon);
            var canvas = new Canvas(cols, rows);

            var edges = EdgesFor(recipe.EntranceEdges, Pick(random, recipe.Entrances), random);
            var bottomWall = edges.Contains(EntranceEdges.Bottom);
            var lobbyTarget = Pick(random, recipe.Lobby);
            var lobbyRows = Mathf.Clamp(Mathf.CeilToInt(lobbyTarget / (float)cols) + 1, 2, 4);
            var lobbyBottom = bottomWall ? rows - 2 : rows - 1;
            var lobbyTop = lobbyBottom - lobbyRows + 1;

            // The restrooms end two rows above the lobby (one to walk out of a door onto), and above that two more for
            // each barrier the recipe needs at the least: the wall and a floor row.
            var leastBarriers = Math.Max(0, Math.Min(recipe.Barriers.x, recipe.Barriers.y));
            if (!TryPlaceRooms(canvas, recipe.Placement, across, depth, lobbyTop - 2 - 2 * leastBarriers, random,
                    out var lowestRoomRow))
                return null;
            if (!TryPlaceEntrances(canvas, edges, bottomWall, lobbyTop, lobbyBottom, random, out var entrances))
                return null;
            GrowLobby(canvas, entrances, lobbyTarget, lobbyTop, lobbyBottom);
            if (!TryPlaceBarriers(canvas, recipe, lowestRoomRow + 2, lobbyTop - 2, random))
                return null;
            PlaceBlocks(canvas, Pick(random, recipe.Blocks), lobbyTop - 1, random);
            canvas.WallOffUnreachable();
            if (random.Next(2) == 0)
                canvas.Mirror();
            return new MapSketch(canvas.ToRows(), stalls);
        }

        private static bool TryPlaceRooms(Canvas canvas, RestroomPlacement placement, int across, int depth, int lastRow,
            Random random, out int lowestRow)
        {
            lowestRow = 0;
            if (placement == RestroomPlacement.Any)
                placement = (RestroomPlacement)random.Next((int)RestroomPlacement.Any);
            var first = random.Next(2) == 0 ? Gender.Male : Gender.Female;
            var second = first == Gender.Male ? Gender.Female : Gender.Male;
            var cols = canvas.Cols;
            Room a, b;
            switch (placement)
            {
                case RestroomPlacement.TopSideBySide:
                {
                    var gap = cols - 2 * across;
                    if (gap < 0 || depth - 1 > lastRow)
                        return false;
                    var x0 = random.Next(gap + 1);
                    var x1 = x0 + across + random.Next(gap - x0 + 1);
                    a = Room.Top(first, x0, across, depth, random);
                    b = Room.Top(second, x1, across, depth, random);
                    break;
                }
                case RestroomPlacement.Sides:
                {
                    var maxY = lastRow - across + 1;
                    if (2 * depth + 1 > cols || maxY < 0)
                        return false;
                    a = Room.Side(first, Direction.West, cols, random.Next(maxY + 1), across, depth, random);
                    b = Room.Side(second, Direction.East, cols, random.Next(maxY + 1), across, depth, random);
                    break;
                }
                default:
                {
                    var maxY = lastRow - across + 1;
                    if (depth - 1 > lastRow || maxY < 0 || across > cols)
                        return false;
                    var topLeft = random.Next(2) == 0;
                    a = Room.Top(first, topLeft ? 0 : cols - across, across, depth, random);
                    var minY = Math.Min(depth, maxY);
                    b = Room.Side(second, topLeft ? Direction.East : Direction.West, cols, minY + random.Next(maxY - minY + 1),
                        across, depth, random);
                    break;
                }
            }

            if (a.Rect.Overlaps(b.Rect) || a.Door.IsNeighbour(b.Door) || !canvas.In(a.Outside) || !canvas.In(b.Outside) ||
                Contains(a.Rect, b.Outside) || Contains(b.Rect, a.Outside))
                return false;
            foreach (var room in new[] { a, b })
            {
                canvas.Paint(room);
                lowestRow = Math.Max(lowestRow, room.Rect.yMax - 1);
            }
            return true;
        }

        private static bool TryPlaceEntrances(Canvas canvas, List<EntranceEdges> edges, bool bottomWall, int lobbyTop,
            int lobbyBottom, Random random, out List<List<Cell>> entrances)
        {
            entrances = new List<List<Cell>>();
            if (bottomWall)
            {
                for (var col = 0; col < canvas.Cols; col++)
                    canvas[new Cell(col, canvas.Rows - 1)] = MapSymbols.Wall;
            }

            foreach (var edge in edges)
            {
                var placed = false;
                for (var attempt = 0; attempt < 12 && !placed; attempt++)
                {
                    var size = 1 + random.Next(2);
                    var cells = new List<Cell>();
                    if (edge == EntranceEdges.Bottom)
                    {
                        if (canvas.Cols - size - 1 < 1)
                            break;
                        var col = 1 + random.Next(canvas.Cols - size - 1);
                        for (var i = 0; i < size; i++)
                            cells.Add(new Cell(col + i, canvas.Rows - 1));
                    }
                    else
                    {
                        size = Math.Min(size, lobbyBottom - lobbyTop + 1);
                        var row = lobbyTop + random.Next(lobbyBottom - lobbyTop - size + 2);
                        var col = edge == EntranceEdges.Left ? 0 : canvas.Cols - 1;
                        for (var i = 0; i < size; i++)
                            cells.Add(new Cell(col, row + i));
                    }

                    if (!canvas.CanHoldEntrance(cells))
                        continue;
                    foreach (var cell in cells)
                        canvas[cell] = MapSymbols.Entrance;
                    entrances.Add(cells);
                    placed = true;
                }
                if (!placed)
                    return false;
            }
            return true;
        }

        // Round by round, every entrance's lobby grows by a tile, breadth-first, inside the lobby band.
        private static void GrowLobby(Canvas canvas, List<List<Cell>> entrances, int target, int top, int bottom)
        {
            var queues = new List<Queue<Cell>>();
            var seen = new HashSet<Cell>();
            foreach (var group in entrances)
            {
                var queue = new Queue<Cell>();
                foreach (var cell in group)
                {
                    foreach (var direction in Directions.All)
                    {
                        var next = cell.Step(direction);
                        if (seen.Add(next))
                            queue.Enqueue(next);
                    }
                }
                queues.Add(queue);
            }

            var grown = 0;
            var growing = true;
            while (grown < target && growing)
            {
                growing = false;
                foreach (var queue in queues)
                {
                    while (queue.Count > 0)
                    {
                        var cell = queue.Dequeue();
                        if (cell.Row < top || cell.Row > bottom || !canvas.IsOpenFloor(cell))
                            continue;
                        canvas[cell] = MapSymbols.Lobby;
                        grown++;
                        growing = true;
                        foreach (var direction in Directions.All)
                        {
                            var next = cell.Step(direction);
                            if (seen.Add(next))
                                queue.Enqueue(next);
                        }
                        break;
                    }
                    if (grown >= target)
                        return;
                }
            }
        }

        private static bool TryPlaceBarriers(Canvas canvas, MapRecipe recipe, int top, int bottom, Random random)
        {
            var wanted = Pick(random, recipe.Barriers);
            var least = Math.Min(recipe.Barriers.x, recipe.Barriers.y);
            if (wanted <= 0)
                return true;

            // Rows with a floor row between them; a row a door opens onto stays open.
            var candidates = new List<int>();
            for (var row = top; row <= bottom; row++)
            {
                if (!canvas.HasOpeningOn(row))
                    candidates.Add(row);
            }
            Shuffle(candidates, random);
            var chosen = new List<int>();
            foreach (var row in candidates)
            {
                if (chosen.Count == wanted)
                    break;
                if (chosen.TrueForAll(other => Math.Abs(other - row) >= 2))
                    chosen.Add(row);
            }
            if (chosen.Count < least)
                return false;

            chosen.Sort();
            var firstSide = random.Next(2);
            for (var i = 0; i < chosen.Count; i++)
            {
                var barrier = canvas.WallAcross(chosen[i]);
                if (barrier.Count < 3)
                    return false;
                var width = 1 + random.Next(2);
                if (recipe.Winding)
                {
                    var atLeft = (firstSide + i) % 2 == 0;
                    canvas.OpenGap(barrier, atLeft ? 0 : barrier.Count - width, width, MapSymbols.Floor);
                }
                else
                {
                    var gaps = 1 + random.Next(2);
                    for (var gap = 0; gap < gaps; gap++)
                    {
                        var atEnd = random.Next(2) == 0;
                        var start = atEnd ? (random.Next(2) == 0 ? 0 : barrier.Count - width) : random.Next(barrier.Count - width + 1);
                        canvas.OpenGap(barrier, start, width, MapSymbols.Floor);
                    }
                }

                var narrow = Pick(random, recipe.NarrowGaps);
                for (var gap = 0; gap < narrow; gap++)
                {
                    for (var attempt = 0; attempt < 12; attempt++)
                    {
                        var size = 1 + random.Next(2);
                        var start = random.Next(Math.Max(1, barrier.Count - size + 1));
                        if (canvas.CanNarrow(barrier, start, size))
                        {
                            canvas.OpenGap(barrier, start, size, MapSymbols.Narrow);
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private static readonly Vector2Int[] _blockShapes =
        {
            new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(1, 3), new Vector2Int(3, 1),
        };

        private static void PlaceBlocks(Canvas canvas, int count, int lastRow, Random random)
        {
            for (var block = 0; block < count; block++)
            {
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    var shape = _blockShapes[random.Next(_blockShapes.Length)];
                    if (lastRow - shape.y + 1 < 0 || canvas.Cols - shape.x < 0)
                        break;
                    var origin = new Cell(random.Next(canvas.Cols - shape.x + 1), random.Next(lastRow - shape.y + 2));
                    if (canvas.TryBlock(origin, shape))
                        break;
                }
            }
        }

        private static bool LobbyReachableForFat(ITileGrid grid)
        {
            var reached = new HashSet<Cell>(grid.Entrances);
            var queue = new Queue<Cell>(grid.Entrances);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var direction in Directions.All)
                {
                    var next = cell.Step(direction);
                    if (!grid.IsWalkable(next) || reached.Contains(next))
                        continue;
                    var type = grid.At(next).Type;
                    if (type == TileType.Door || type == TileType.Narrow)
                        continue;
                    reached.Add(next);
                    queue.Enqueue(next);
                }
            }

            for (var row = 0; row < grid.Rows; row++)
            {
                for (var col = 0; col < grid.Cols; col++)
                {
                    var cell = new Cell(col, row);
                    if (grid.At(cell).Type == TileType.Lobby && !reached.Contains(cell))
                        return false;
                }
            }
            return true;
        }

        // Different edges first, then again from the start.
        private static List<EntranceEdges> EdgesFor(EntranceEdges allowed, int count, Random random)
        {
            var edges = new List<EntranceEdges>();
            foreach (var edge in new[] { EntranceEdges.Bottom, EntranceEdges.Left, EntranceEdges.Right })
            {
                if ((allowed & edge) != 0)
                    edges.Add(edge);
            }
            if (edges.Count == 0)
                edges.Add(EntranceEdges.Bottom);
            Shuffle(edges, random);

            var picked = new List<EntranceEdges>();
            for (var i = 0; i < count; i++)
                picked.Add(edges[i % edges.Count]);
            return picked;
        }

        private static bool Contains(RectInt rect, Cell cell) => rect.Contains(new Vector2Int(cell.Col, cell.Row));

        private static int Pick(Random random, Vector2Int range)
        {
            var min = Math.Min(range.x, range.y);
            var max = Math.Max(range.x, range.y);
            return min + random.Next(max - min + 1);
        }

        private static bool Within(int value, Vector2Int range) =>
            value >= Math.Min(range.x, range.y) && value <= Math.Max(range.x, range.y);

        private static string Range(Vector2Int range) =>
            range.x == range.y ? range.x.ToString(CultureInfo.InvariantCulture) : $"{range.x}–{range.y}";

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>A restroom being placed: its tiles (x = column, y = row), its door and the side the door opens to.</summary>
        private readonly struct Room
        {
            public readonly Gender Gender;
            public readonly RectInt Rect;
            public readonly Cell Door;
            public readonly Direction Opening;

            private Room(Gender gender, RectInt rect, Cell door, Direction opening)
            {
                Gender = gender;
                Rect = rect;
                Door = door;
                Opening = opening;
            }

            public Cell Outside => Door.Step(Opening);

            public static Room Top(Gender gender, int x, int across, int depth, Random random) =>
                new Room(gender, new RectInt(x, 0, across, depth), new Cell(Along(x, across, random), depth - 1),
                    Direction.South);

            /// <param name="edge">West for a restroom against the left edge, East for the right.</param>
            public static Room Side(Gender gender, Direction edge, int cols, int y, int across, int depth, Random random)
            {
                var row = Along(y, across, random);
                return edge == Direction.West
                    ? new Room(gender, new RectInt(0, y, depth, across), new Cell(depth - 1, row), Direction.East)
                    : new Room(gender, new RectInt(cols - depth, y, depth, across), new Cell(cols - depth, row),
                        Direction.West);
            }

            // Off the corners when the wall is long enough, so the door frame has wall on both sides.
            private static int Along(int start, int length, Random random) =>
                length >= 3 ? start + 1 + random.Next(length - 2) : start + random.Next(length);
        }

        /// <summary>The map being sketched, with the tiles that must stay open: in front of doors and around gaps.</summary>
        private sealed class Canvas
        {
            private readonly char[,] _tiles;
            private readonly HashSet<Cell> _kept = new HashSet<Cell>();
            private readonly HashSet<int> _openingRows = new HashSet<int>();
            private readonly List<Cell> _doors = new List<Cell>();

            public Canvas(int cols, int rows)
            {
                Cols = cols;
                Rows = rows;
                _tiles = new char[rows, cols];
                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                        _tiles[row, col] = MapSymbols.Floor;
                }
            }

            public int Cols { get; }
            public int Rows { get; }

            public char this[Cell cell]
            {
                get => _tiles[cell.Row, cell.Col];
                set => _tiles[cell.Row, cell.Col] = value;
            }

            public bool In(Cell cell) => cell.Col >= 0 && cell.Col < Cols && cell.Row >= 0 && cell.Row < Rows;

            public bool IsOpenFloor(Cell cell) => In(cell) && this[cell] == MapSymbols.Floor && !_kept.Contains(cell);

            public bool HasOpeningOn(int row) => _openingRows.Contains(row);

            public void Paint(Room room)
            {
                foreach (var position in room.Rect.allPositionsWithin)
                    this[new Cell(position.x, position.y)] = MapSymbols.Building(room.Gender);
                this[room.Door] = MapSymbols.Door(room.Gender);
                _doors.Add(room.Door);

                // A door opens onto one tile only: any other floor beside it outside the restroom becomes wall.
                foreach (var direction in Directions.All)
                {
                    var beside = room.Door.Step(direction);
                    if (direction == room.Opening || !In(beside) || room.Rect.Contains(new Vector2Int(beside.Col, beside.Row)))
                        continue;
                    if (this[beside] == MapSymbols.Floor)
                        this[beside] = MapSymbols.Wall;
                }
                _kept.Add(room.Outside);
                _openingRows.Add(room.Outside.Row);
            }

            /// <summary>Entrance tiles go on floor (or the bottom wall) and never touch another entrance.</summary>
            public bool CanHoldEntrance(List<Cell> cells)
            {
                foreach (var cell in cells)
                {
                    if (!In(cell) || (this[cell] != MapSymbols.Floor && this[cell] != MapSymbols.Wall) || _kept.Contains(cell))
                        return false;
                    foreach (var direction in Directions.All)
                    {
                        var next = cell.Step(direction);
                        if (In(next) && this[next] == MapSymbols.Entrance)
                            return false;
                    }
                }
                return true;
            }

            /// <summary>Walls over the row's open floor.</summary>
            public Barrier WallAcross(int row)
            {
                var cols = new List<int>();
                for (var col = 0; col < Cols; col++)
                {
                    var cell = new Cell(col, row);
                    if (!IsOpenFloor(cell))
                        continue;
                    this[cell] = MapSymbols.Wall;
                    cols.Add(col);
                }
                return new Barrier(row, cols);
            }

            /// <summary>Opens <paramref name="size"/> of the barrier's tiles from <paramref name="start"/>, and keeps them and their approach clear.</summary>
            public void OpenGap(Barrier barrier, int start, int size, char symbol)
            {
                for (var i = Math.Max(0, start); i < Math.Min(barrier.Count, start + size); i++)
                {
                    var cell = new Cell(barrier.Cols[i], barrier.Row);
                    this[cell] = symbol;
                    _kept.Add(cell);
                    _kept.Add(cell.Step(Direction.North));
                    _kept.Add(cell.Step(Direction.South));
                }
            }

            /// <summary>A narrow gap goes on side by side wall tiles of the barrier, with wall on both sides of it.</summary>
            public bool CanNarrow(Barrier barrier, int start, int size)
            {
                if (start < 0 || start + size > barrier.Count)
                    return false;
                for (var i = start; i < start + size; i++)
                {
                    var col = barrier.Cols[i];
                    if (i > start && col != barrier.Cols[i - 1] + 1)
                        return false;
                    if (this[new Cell(col, barrier.Row)] != MapSymbols.Wall)
                        return false;
                }

                var left = new Cell(barrier.Cols[start] - 1, barrier.Row);
                var right = new Cell(barrier.Cols[start + size - 1] + 1, barrier.Row);
                return (!In(left) || this[left] == MapSymbols.Wall) && (!In(right) || this[right] == MapSymbols.Wall);
            }

            /// <summary>
            /// Puts a wall block on open floor that touches nothing kept clear and cuts nothing off from the entrances;
            /// false, and the canvas as it was, when it does not fit there.
            /// </summary>
            public bool TryBlock(Cell origin, Vector2Int shape)
            {
                var before = CountReachable();
                if (!TryWall(origin, shape, out var cells))
                    return false;
                if (CountReachable() == before - cells.Count)
                    return true;
                foreach (var cell in cells)
                    this[cell] = MapSymbols.Floor;
                return false;
            }

            private bool TryWall(Cell origin, Vector2Int shape, out List<Cell> cells)
            {
                cells = new List<Cell>();
                for (var dx = 0; dx < shape.x; dx++)
                {
                    for (var dy = 0; dy < shape.y; dy++)
                    {
                        var cell = new Cell(origin.Col + dx, origin.Row + dy);
                        if (!IsOpenFloor(cell))
                            return false;
                        foreach (var direction in Directions.All)
                        {
                            var next = cell.Step(direction);
                            if (_kept.Contains(next) || (In(next) && this[next] == MapSymbols.Entrance))
                                return false;
                        }
                        cells.Add(cell);
                    }
                }
                foreach (var cell in cells)
                    this[cell] = MapSymbols.Wall;
                return true;
            }

            /// <summary>Floor and lobby that cannot be walked to from an entrance (never through a door) become wall.</summary>
            public void WallOffUnreachable()
            {
                var reached = Reach();
                for (var row = 0; row < Rows; row++)
                {
                    for (var col = 0; col < Cols; col++)
                    {
                        var cell = new Cell(col, row);
                        var symbol = this[cell];
                        if ((symbol == MapSymbols.Floor || symbol == MapSymbols.Lobby || symbol == MapSymbols.Narrow) &&
                            !reached.Contains(cell))
                            this[cell] = MapSymbols.Wall;
                    }
                }
            }

            private int CountReachable() => Reach().Count;

            // Breadth-first from the entrances over walkable tiles; doors are reached but never walked through.
            private HashSet<Cell> Reach()
            {
                var reached = new HashSet<Cell>();
                var queue = new Queue<Cell>();
                for (var row = 0; row < Rows; row++)
                {
                    for (var col = 0; col < Cols; col++)
                    {
                        var cell = new Cell(col, row);
                        if (this[cell] == MapSymbols.Entrance && reached.Add(cell))
                            queue.Enqueue(cell);
                    }
                }
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var direction in Directions.All)
                    {
                        var next = cell.Step(direction);
                        if (!In(next) || reached.Contains(next) || !MapSymbols.IsWalkable(this[next]))
                            continue;
                        reached.Add(next);
                        if (this[next] != MapSymbols.MaleDoor && this[next] != MapSymbols.FemaleDoor)
                            queue.Enqueue(next);
                    }
                }
                return reached;
            }

            public void Mirror()
            {
                for (var row = 0; row < Rows; row++)
                {
                    for (var col = 0; col < Cols / 2; col++)
                        (_tiles[row, col], _tiles[row, Cols - 1 - col]) = (_tiles[row, Cols - 1 - col], _tiles[row, col]);
                }
            }

            public string[] ToRows()
            {
                var rows = new string[Rows];
                var line = new char[Cols];
                for (var row = 0; row < Rows; row++)
                {
                    for (var col = 0; col < Cols; col++)
                        line[col] = _tiles[row, col];
                    rows[row] = new string(line);
                }
                return rows;
            }

        }

        /// <summary>A wall across one row: the columns it was built on, left to right.</summary>
        private sealed class Barrier
        {
            public Barrier(int row, List<int> cols)
            {
                Row = row;
                Cols = cols;
            }

            public int Row { get; }
            public List<int> Cols { get; }
            public int Count => Cols.Count;
        }
    }

    /// <summary>A sketched map: its rows and how many stalls each restroom has.</summary>
    public sealed class MapSketch
    {
        public MapSketch(string[] rows, int stalls)
        {
            Rows = rows;
            Stalls = stalls;
        }

        public string[] Rows { get; }
        public int Stalls { get; }
    }

    /// <summary>A generated playable map, with its measurements and the seed and try it came from.</summary>
    public sealed class GeneratedMap
    {
        public GeneratedMap(string[] rows, int stalls, MapMetrics metrics, int seed, int attempt)
        {
            Rows = rows;
            Stalls = stalls;
            Metrics = metrics;
            Seed = seed;
            Attempt = attempt;
        }

        public string[] Rows { get; }
        public int Stalls { get; }
        public MapMetrics Metrics { get; }
        public int Seed { get; }
        public int Attempt { get; }
    }
}

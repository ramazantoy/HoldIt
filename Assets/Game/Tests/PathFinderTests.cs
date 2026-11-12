using System;
using HoldIt.Board;
using HoldIt.Paths;
using NUnit.Framework;

namespace HoldIt.Tests
{
    /// <summary>Routes on Map 1 (<see cref="TestMaps.Lobby"/>).</summary>
    public class PathFinderTests
    {
        private TileGrid _grid;

        [SetUp]
        public void ParseLobby() => _grid = MapParser.Parse(TestMaps.Lobby);

        [Test]
        public void Shortest_AStraightLine()
        {
            var path = PathFinder.Shortest(_grid, new Cell(0, 10), new Cell(0, 7), Drawable);

            Assert.That(Cells(path), Is.EqualTo(Cells((0, 10), (0, 9), (0, 8), (0, 7))));
        }

        [Test]
        public void Shortest_NeverStepsAcrossThePostsOfANarrowTile()
        {
            // From the narrow tile (4, 8) to its twin (5, 8) the way goes out and back, not through the post between them.
            var path = PathFinder.Shortest(_grid, new Cell(4, 8), new Cell(5, 8), Drawable);

            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 2);
            for (var i = 1; i < path.Count; i++)
                Assert.IsTrue(Passage.CanStep(_grid, path[i - 1], path[i]), $"{path[i - 1]} -> {path[i]}");
        }

        [Test]
        public void Shortest_GoesAroundAWall()
        {
            var path = PathFinder.Shortest(_grid, new Cell(3, 9), new Cell(3, 7), Drawable);

            Assert.That(Cells(path), Is.EqualTo(Cells((3, 9), (4, 9), (4, 8), (4, 7), (3, 7))), "through the narrow passage");
        }

        [Test]
        public void Shortest_EntersOnlyAllowedCells()
        {
            bool NoNarrow(Cell cell) => Drawable(cell) && _grid.At(cell).Type != TileType.Narrow;

            var path = PathFinder.Shortest(_grid, new Cell(3, 9), new Cell(3, 7), NoNarrow);

            Assert.That(Cells(path), Is.EqualTo(Cells((3, 9), (2, 9), (1, 9), (0, 9), (0, 8), (0, 7), (1, 7), (2, 7), (3, 7))));
        }

        [Test]
        public void Shortest_DoesNotAskAboutTheStart()
        {
            var path = PathFinder.Shortest(_grid, new Cell(4, 13), new Cell(4, 12), Drawable);

            Assert.That(Cells(path), Is.EqualTo(Cells((4, 13), (4, 12))), "an entrance is not drawable, but it is the start");
        }

        [Test]
        public void Shortest_ATargetTheConditionRefuses_IsNull()
        {
            bool NoDoors(Cell cell) => Drawable(cell) && _grid.At(cell).Type != TileType.Door;

            Assert.IsNull(PathFinder.Shortest(_grid, new Cell(2, 7), new Cell(2, 3), NoDoors));
            Assert.IsNotNull(PathFinder.Shortest(_grid, new Cell(2, 7), new Cell(2, 3), Drawable));
        }

        [Test]
        public void Shortest_AWallOrAnOutsideTarget_IsNull()
        {
            Assert.IsNull(PathFinder.Shortest(_grid, new Cell(3, 9), new Cell(2, 8), Drawable));
            Assert.IsNull(PathFinder.Shortest(_grid, new Cell(3, 9), new Cell(3, 20), Drawable));
        }

        [Test]
        public void Shortest_ToItsStart_IsNull()
        {
            Assert.IsNull(PathFinder.Shortest(_grid, new Cell(3, 9), new Cell(3, 9), Drawable));
        }

        [Test]
        public void Shortest_GivesTheSameRouteEveryTime()
        {
            var first = PathFinder.Shortest(_grid, new Cell(0, 11), new Cell(9, 4), Drawable);
            var second = PathFinder.Shortest(_grid, new Cell(0, 11), new Cell(9, 4), Drawable);

            Assert.That(Cells(second), Is.EqualTo(Cells(first)));
        }

        [Test]
        public void Shortest_WithoutACondition_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => PathFinder.Shortest(_grid, new Cell(0, 10), new Cell(0, 9), null));
        }

        private bool Drawable(Cell cell) => PathRules.IsDrawable(_grid, cell);

        private static Cell[] Cells(CellPath path)
        {
            Assert.IsNotNull(path, "no route");
            var cells = new Cell[path.Count];
            for (var i = 0; i < path.Count; i++)
                cells[i] = path[i];
            return cells;
        }

        private static Cell[] Cells(params (int Col, int Row)[] cells) =>
            Array.ConvertAll(cells, cell => new Cell(cell.Col, cell.Row));
    }
}

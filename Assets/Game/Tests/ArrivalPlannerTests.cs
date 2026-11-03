using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Spawning;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class ArrivalPlannerTests
    {
        /// <summary>The only way from the entrance to the lobby is through the narrow passage.</summary>
        private static readonly string[] _throughTheNarrow =
        {
            "LLL",
            "#n#",
            "...",
            "#E#",
        };

        [Test]
        public void TryPlan_GoesFromAnEntranceToALobbyCellOverWalkableCellsWithoutDoors()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);
            var random = new Random(1);

            for (var i = 0; i < 20; i++)
            {
                Assert.IsTrue(ArrivalPlanner.TryPlan(grid, _ => true, BodySize.Normal, random, out var path));
                Assert.AreEqual(TileType.Entrance, grid.At(path.Start).Type);
                Assert.AreEqual(TileType.Lobby, grid.At(path.End).Type);
                for (var step = 0; step < path.Count; step++)
                {
                    Assert.IsTrue(grid.IsWalkable(path[step]));
                    Assert.AreNotEqual(TileType.Door, grid.At(path[step]).Type);
                }
            }
        }

        [Test]
        public void TryPlan_TakesAFreeEntranceAndAFreeLobbyCellAndGoesAroundEveryone()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);
            var taken = new HashSet<Cell> { new Cell(4, 13), new Cell(6, 12), new Cell(4, 11) };
            for (var col = 0; col < grid.Cols; col++)
            {
                if (col != 9)
                    taken.Add(new Cell(col, 10));
            }

            Assert.IsTrue(ArrivalPlanner.TryPlan(grid, cell => !taken.Contains(cell), BodySize.Thin, new Random(3),
                out var path));

            Assert.AreEqual(new Cell(5, 13), path.Start);
            for (var step = 0; step < path.Count; step++)
                Assert.IsFalse(taken.Contains(path[step]), $"{path[step]} is taken.");
        }

        [Test]
        public void TryPlan_NoFreeEntrance_Fails()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.IsFalse(ArrivalPlanner.TryPlan(grid, cell => grid.At(cell).Type != TileType.Entrance,
                BodySize.Normal, new Random(1), out var path));
            Assert.IsNull(path);
        }

        [Test]
        public void TryPlan_NoFreeLobbyCell_Fails()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.IsFalse(ArrivalPlanner.TryPlan(grid, cell => grid.At(cell).Type != TileType.Lobby,
                BodySize.Normal, new Random(1), out _));
        }

        [Test]
        public void TryPlan_FatThroughANarrowPassage_Fails_ButNormalPasses()
        {
            var grid = MapParser.Parse(_throughTheNarrow);

            Assert.IsFalse(ArrivalPlanner.TryPlan(grid, _ => true, BodySize.Fat, new Random(1), out _));
            Assert.IsTrue(ArrivalPlanner.TryPlan(grid, _ => true, BodySize.Normal, new Random(1), out var path));
            CollectionAssert.Contains(Cells(path), new Cell(1, 1));
        }

        private static List<Cell> Cells(Paths.CellPath path)
        {
            var cells = new List<Cell>();
            for (var i = 0; i < path.Count; i++)
                cells.Add(path[i]);
            return cells;
        }
    }
}

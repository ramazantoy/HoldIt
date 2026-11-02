using System;
using HoldIt.Board;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class TileGridTests
    {
        private TileGrid _grid;

        [SetUp]
        public void ParseLobby() => _grid = MapParser.Parse(TestMaps.Lobby);

        [Test]
        public void InBounds_IsTrueOnlyInsideTheMap()
        {
            Assert.IsTrue(_grid.InBounds(new Cell(0, 0)));
            Assert.IsTrue(_grid.InBounds(new Cell(9, 13)));
            Assert.IsFalse(_grid.InBounds(new Cell(-1, 0)));
            Assert.IsFalse(_grid.InBounds(new Cell(0, -1)));
            Assert.IsFalse(_grid.InBounds(new Cell(10, 0)));
            Assert.IsFalse(_grid.InBounds(new Cell(0, 14)));
        }

        [Test]
        public void At_OutsideTheMap_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _grid.At(new Cell(10, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => _grid.At(new Cell(0, -1)));
        }

        [TestCase(0, 4, true, TestName = "Floor is walkable")]
        [TestCase(1, 12, true, TestName = "Lobby is walkable")]
        [TestCase(4, 13, true, TestName = "Entrance is walkable")]
        [TestCase(4, 8, true, TestName = "Narrow is walkable")]
        [TestCase(2, 3, true, TestName = "Door is walkable")]
        [TestCase(1, 5, false, TestName = "Wall is not walkable")]
        [TestCase(0, 0, false, TestName = "Building is not walkable")]
        [TestCase(-1, 4, false, TestName = "Outside is not walkable")]
        public void IsWalkable(int col, int row, bool walkable)
        {
            Assert.AreEqual(walkable, _grid.IsWalkable(new Cell(col, row)));
        }

        [Test]
        public void Entrances_AreListedInOrder()
        {
            CollectionAssert.AreEqual(new[] { new Cell(4, 13), new Cell(5, 13) }, _grid.Entrances);
        }

        [Test]
        public void DoorsOf_GivesEachGendersDoor()
        {
            CollectionAssert.AreEqual(new[] { new Cell(2, 3) }, _grid.DoorsOf(Gender.Male));
            CollectionAssert.AreEqual(new[] { new Cell(7, 3) }, _grid.DoorsOf(Gender.Female));
        }

        [Test]
        public void BuildingOf_IsTheRectangleOfItsTilesAndDoor()
        {
            Assert.AreEqual(new RectInt(0, 0, 5, 4), _grid.BuildingOf(Gender.Male));
            Assert.AreEqual(new RectInt(5, 0, 5, 4), _grid.BuildingOf(Gender.Female));
        }

        [Test]
        public void BuildingOf_WithoutOne_IsEmpty()
        {
            var grid = MapParser.Parse(new[] { "..", "LE" });

            Assert.AreEqual(0, grid.BuildingOf(Gender.Male).width);
            Assert.IsEmpty(grid.DoorsOf(Gender.Female));
        }

        [Test]
        public void Constructor_RejectsAWrongTileCount()
        {
            Assert.Throws<ArgumentException>(() => new TileGrid(2, 2, new Tile[3]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TileGrid(0, 2, new Tile[0]));
        }
    }
}

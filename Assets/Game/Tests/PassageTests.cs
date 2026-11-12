using HoldIt.Board;
using NUnit.Framework;

namespace HoldIt.Tests
{
    /// <summary>Which steps a narrow tile lets through: along its passage, never across its posts.</summary>
    public class PassageTests
    {
        private static readonly string[] _openFloor = { "...", ".n.", "..." };
        private static readonly string[] _eastWest = { "###", ".n.", "###" };

        [Test]
        public void Axis_AGapInAnEastWestWall_RunsNorthSouth()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.AreEqual(Direction.North, Passage.Axis(grid, new Cell(4, 8)));
        }

        [Test]
        public void Axis_ClosedNorthAndSouth_RunsEastWest()
        {
            var grid = MapParser.Parse(_eastWest);

            Assert.AreEqual(Direction.East, Passage.Axis(grid, new Cell(1, 1)));
        }

        [Test]
        public void CanStep_AlongANorthSouthPassage_IsOpen()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.IsTrue(Passage.CanStep(grid, new Cell(4, 9), new Cell(4, 8)));
            Assert.IsTrue(Passage.CanStep(grid, new Cell(4, 8), new Cell(4, 7)));
        }

        [Test]
        public void CanStep_BetweenTwoNarrowTilesSideBySide_MeetsThePost()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.IsFalse(Passage.CanStep(grid, new Cell(4, 8), new Cell(5, 8)));
            Assert.IsFalse(Passage.CanStep(grid, new Cell(5, 8), new Cell(4, 8)));
        }

        [Test]
        public void CanStep_IntoOrOutOfANarrowTileFromTheSide_MeetsThePost()
        {
            var grid = MapParser.Parse(_openFloor);

            Assert.IsFalse(Passage.CanStep(grid, new Cell(0, 1), new Cell(1, 1)));
            Assert.IsFalse(Passage.CanStep(grid, new Cell(1, 1), new Cell(2, 1)));
            Assert.IsTrue(Passage.CanStep(grid, new Cell(1, 0), new Cell(1, 1)));
            Assert.IsTrue(Passage.CanStep(grid, new Cell(1, 1), new Cell(1, 2)));
        }

        [Test]
        public void CanStep_AlongAnEastWestPassage_IsOpenAndAcrossItIsNot()
        {
            var grid = MapParser.Parse(_eastWest);

            Assert.IsTrue(Passage.CanStep(grid, new Cell(0, 1), new Cell(1, 1)));
            Assert.IsTrue(Passage.CanStep(grid, new Cell(1, 1), new Cell(2, 1)));
            Assert.IsFalse(Passage.CanStep(grid, new Cell(1, 1), new Cell(1, 0)));
        }

        [Test]
        public void CanStep_PlainTiles_IsAlwaysOpen()
        {
            var grid = MapParser.Parse(TestMaps.Lobby);

            Assert.IsTrue(Passage.CanStep(grid, new Cell(0, 9), new Cell(1, 9)));
            Assert.IsTrue(Passage.CanStep(grid, new Cell(0, 9), new Cell(0, 10)));
        }
    }
}

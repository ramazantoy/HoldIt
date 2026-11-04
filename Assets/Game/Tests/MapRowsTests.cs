using System;
using HoldIt.Board;
using HoldIt.Editor.Maps;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class MapRowsTests
    {
        private static readonly string[] _rows = { "MmM", "...", "LEL" };

        [Test]
        public void Set_ChangesOneTile_AndLeavesTheOldRowsAlone()
        {
            var painted = MapRows.Set(_rows, new Cell(2, 1), '#');

            CollectionAssert.AreEqual(new[] { "MmM", "..#", "LEL" }, painted);
            CollectionAssert.AreEqual(new[] { "MmM", "...", "LEL" }, _rows);
        }

        [Test]
        public void Set_OutsideTheRows_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MapRows.Set(_rows, new Cell(3, 0), '#'));
            Assert.Throws<ArgumentOutOfRangeException>(() => MapRows.Set(_rows, new Cell(0, 3), '#'));
            Assert.Throws<ArgumentOutOfRangeException>(() => MapRows.Set(_rows, new Cell(-1, 0), '#'));
        }

        [Test]
        public void Resize_Grows_WithFloorRightAndBelow()
        {
            var grown = MapRows.Resize(_rows, 4, 4);

            CollectionAssert.AreEqual(new[] { "MmM.", "....", "LEL.", "...." }, grown);
        }

        [Test]
        public void Resize_Shrinks_FromTheTopLeft()
        {
            CollectionAssert.AreEqual(new[] { "Mm", ".." }, MapRows.Resize(_rows, 2, 2));
        }

        [Test]
        public void Resize_StartsAnEmptyMapAsFloor()
        {
            CollectionAssert.AreEqual(new[] { "...", "..." }, MapRows.Resize(Array.Empty<string>(), 3, 2));
        }

        [Test]
        public void Parse_TakesOneRowALine_WithoutBlankLinesOrSpaces()
        {
            var rows = MapRows.Parse("  MmM \r\n\n...\r\nLEL\n\n");

            CollectionAssert.AreEqual(_rows, rows);
        }
    }
}

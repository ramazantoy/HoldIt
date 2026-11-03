using System;
using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Paths;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class CellPathTests
    {
        [Test]
        public void KeepsTheCellsInOrder()
        {
            var path = new CellPath(new[] { new Cell(0, 10), new Cell(1, 10), new Cell(1, 9) });

            Assert.AreEqual(3, path.Count);
            Assert.AreEqual(new Cell(0, 10), path.Start);
            Assert.AreEqual(new Cell(1, 10), path[1]);
            Assert.AreEqual(new Cell(1, 9), path.End);
        }

        [Test]
        public void CopiesTheCells()
        {
            var cells = new List<Cell> { new Cell(0, 10), new Cell(1, 10) };
            var path = new CellPath(cells);

            cells[1] = new Cell(0, 9);

            Assert.AreEqual(new Cell(1, 10), path.End);
        }

        [Test]
        public void Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CellPath(null));
        }

        [Test]
        public void FewerThanTwoCells_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CellPath(Array.Empty<Cell>()));
            Assert.Throws<ArgumentException>(() => new CellPath(new[] { new Cell(0, 10) }));
        }

        [Test]
        public void AGap_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CellPath(new[] { new Cell(0, 10), new Cell(2, 10) }));
        }

        [Test]
        public void ADiagonalStep_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CellPath(new[] { new Cell(0, 10), new Cell(1, 9) }));
        }

        [Test]
        public void StandingStill_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CellPath(new[] { new Cell(0, 10), new Cell(0, 10) }));
        }
    }
}

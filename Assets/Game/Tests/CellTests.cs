using HoldIt.Board;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class CellTests
    {
        private static readonly Cell _centre = new Cell(4, 7);

        [Test]
        public void IsNeighbour_IsTrueForTheFourSides()
        {
            foreach (var direction in Directions.All)
                Assert.IsTrue(_centre.IsNeighbour(_centre.Step(direction)), direction.ToString());
        }

        [TestCase(4, 7, TestName = "The cell itself is not a neighbour")]
        [TestCase(5, 6, TestName = "A corner is not a neighbour")]
        [TestCase(6, 7, TestName = "Two cells away is not a neighbour")]
        public void IsNeighbour_IsFalseOtherwise(int col, int row)
        {
            Assert.IsFalse(_centre.IsNeighbour(new Cell(col, row)));
        }

        [Test]
        public void TryDirectionTo_GivesTheSideOfEachNeighbour()
        {
            foreach (var direction in Directions.All)
            {
                Assert.IsTrue(_centre.TryDirectionTo(_centre.Step(direction), out var found), direction.ToString());
                Assert.AreEqual(direction, found);
            }
        }

        [Test]
        public void TryDirectionTo_ACellThatIsNotANeighbour_IsFalse()
        {
            Assert.IsFalse(_centre.TryDirectionTo(_centre, out _));
            Assert.IsFalse(_centre.TryDirectionTo(new Cell(5, 8), out _));
        }

        [Test]
        public void TryDirectionTo_NorthIsTheRowAbove()
        {
            Assert.IsTrue(_centre.TryDirectionTo(new Cell(4, 6), out var direction));
            Assert.AreEqual(Direction.North, direction);
        }
    }
}

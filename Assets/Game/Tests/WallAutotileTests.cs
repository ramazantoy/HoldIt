using HoldIt.Board;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class WallAutotileTests
    {
        private const int N = 1;
        private const int E = 2;
        private const int S = 4;
        private const int W = 8;

        // Every mask of wall neighbours, with the shape and facing that join exactly those sides.
        [TestCase(0, WallShape.Single, Direction.North)]
        [TestCase(N, WallShape.End, Direction.North)]
        [TestCase(E, WallShape.End, Direction.East)]
        [TestCase(S, WallShape.End, Direction.South)]
        [TestCase(W, WallShape.End, Direction.West)]
        [TestCase(N | S, WallShape.Straight, Direction.North)]
        [TestCase(E | W, WallShape.Straight, Direction.East)]
        [TestCase(N | E, WallShape.Corner, Direction.North)]
        [TestCase(E | S, WallShape.Corner, Direction.East)]
        [TestCase(S | W, WallShape.Corner, Direction.South)]
        [TestCase(W | N, WallShape.Corner, Direction.West)]
        [TestCase(N | E | S, WallShape.T, Direction.North)]
        [TestCase(E | S | W, WallShape.T, Direction.East)]
        [TestCase(S | W | N, WallShape.T, Direction.South)]
        [TestCase(W | N | E, WallShape.T, Direction.West)]
        [TestCase(N | E | S | W, WallShape.Cross, Direction.North)]
        public void Pick_JoinsExactlyTheWallSides(int wallSides, WallShape shape, Direction facing)
        {
            var picked = WallAutotile.Pick(wallSides);

            Assert.AreEqual(shape, picked.Shape);
            Assert.AreEqual(facing, picked.Facing);
        }

        [Test]
        public void Bit_FollowsTheDirections()
        {
            Assert.AreEqual(N, WallAutotile.Bit(Direction.North));
            Assert.AreEqual(E, WallAutotile.Bit(Direction.East));
            Assert.AreEqual(S, WallAutotile.Bit(Direction.South));
            Assert.AreEqual(W, WallAutotile.Bit(Direction.West));
        }
    }
}

using HoldIt.Board;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class RevealWaveTests
    {
        // A 10 wide, 6 deep board, centred at the origin like BoardSpace's.
        private static readonly Bounds Board = new Bounds(Vector3.zero, new Vector3(10f, 1f, 6f));

        [Test]
        public void Progress_StartsAtTheTopLeftAndEndsAtTheBottomRight()
        {
            // The map's top row is +Z, so top left is the smallest x with the largest z.
            Assert.AreEqual(0f, RevealWave.Progress(-5f, 3f, Board), 1e-5f);
            Assert.AreEqual(1f, RevealWave.Progress(5f, -3f, Board), 1e-5f);
        }

        [Test]
        public void Progress_GrowsAcrossAndDown()
        {
            var start = RevealWave.Progress(-5f, 3f, Board);
            var right = RevealWave.Progress(0f, 3f, Board);
            var down = RevealWave.Progress(-5f, 0f, Board);
            var both = RevealWave.Progress(0f, 0f, Board);

            Assert.Greater(right, start);
            Assert.Greater(down, start);
            Assert.Greater(both, right);
            Assert.Greater(both, down);
        }

        [Test]
        public void Progress_FollowsTheLengthOfTheSidesWalked()
        {
            // The top right is a board width from the start, the bottom left a board depth.
            var topRight = RevealWave.Progress(5f, 3f, Board);
            var bottomLeft = RevealWave.Progress(-5f, -3f, Board);

            Assert.AreEqual(10f / 16f, topRight, 1e-5f);
            Assert.AreEqual(6f / 16f, bottomLeft, 1e-5f);
        }

        [Test]
        public void Progress_OutsideTheBounds_CountsAsOnItsEdge()
        {
            Assert.AreEqual(0f, RevealWave.Progress(-50f, 30f, Board));
            Assert.AreEqual(1f, RevealWave.Progress(50f, -30f, Board));
        }

        [Test]
        public void Progress_OfAnEmptyBoard_IsZero()
        {
            Assert.AreEqual(0f, RevealWave.Progress(1f, 1f, new Bounds(Vector3.zero, Vector3.zero)));
        }

        [Test]
        public void DelayOf_ScalesProgressByTheTotal()
        {
            Assert.AreEqual(0f, RevealWave.DelayOf(-5f, 3f, Board, 1.2f), 1e-5f);
            Assert.AreEqual(1.2f, RevealWave.DelayOf(5f, -3f, Board, 1.2f), 1e-5f);
            Assert.AreEqual(0.6f, RevealWave.DelayOf(0f, 0f, Board, 1.2f), 1e-5f);
        }
    }
}

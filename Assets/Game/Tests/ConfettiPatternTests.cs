using HoldIt.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class ConfettiPatternTests
    {
        private static ConfettiPiece[] Make(int count = 24, int seed = 7, float spread = 120f, int colors = 5) =>
            ConfettiPattern.Make(count, seed, spread, 600f, 1300f, 540f, colors, 0.2f);

        [Test]
        public void Make_GivesTheCountAsked()
        {
            Assert.AreEqual(24, Make(24).Length);
            Assert.AreEqual(0, Make(0).Length);
            Assert.AreEqual(0, Make(-3).Length);
        }

        [Test]
        public void Make_SameSeedSamePieces_AnotherSeedAnother()
        {
            var a = Make(seed: 1);
            var b = Make(seed: 1);
            var c = Make(seed: 2);

            for (var i = 0; i < a.Length; i++)
                Assert.AreEqual(a[i].Velocity, b[i].Velocity);
            Assert.AreNotEqual(a[0].Velocity, c[0].Velocity);
        }

        [Test]
        public void Make_ThrowsUpwardsInsideTheFanAtASpeedInRange()
        {
            foreach (var piece in Make(spread: 100f))
            {
                Assert.Greater(piece.Velocity.y, 0f);
                var fromUp = Vector2.Angle(Vector2.up, piece.Velocity);
                Assert.LessOrEqual(fromUp, 50.01f);
                Assert.That(piece.Velocity.magnitude, Is.InRange(599.9f, 1300.1f));
            }
        }

        [Test]
        public void Make_GoesRoundTheColoursAndKeepsSpinAndDelayInRange()
        {
            var pieces = Make(colors: 3);

            for (var i = 0; i < pieces.Length; i++)
            {
                Assert.AreEqual(i % 3, pieces[i].Color);
                Assert.That(pieces[i].Spin, Is.InRange(-540.01f, 540.01f));
                Assert.That(pieces[i].Delay, Is.InRange(0f, 0.2f));
            }
        }

        [Test]
        public void PositionAt_IsAtTheStartUntilTheDelayPasses()
        {
            var piece = new ConfettiPiece(new Vector2(100f, 500f), 0f, 0, 0.5f);

            Assert.AreEqual(Vector2.zero, ConfettiPattern.PositionAt(piece, 0.3f, -1000f));
            Assert.AreEqual(Vector2.zero, ConfettiPattern.PositionAt(piece, 0.5f, -1000f));
        }

        [Test]
        public void PositionAt_RisesThenFallsUnderGravity()
        {
            var piece = new ConfettiPiece(new Vector2(0f, 1000f), 0f, 0, 0f);

            var rising = ConfettiPattern.PositionAt(piece, 0.5f, -1000f);
            var apex = ConfettiPattern.PositionAt(piece, 1f, -1000f);
            var falling = ConfettiPattern.PositionAt(piece, 2f, -1000f);

            Assert.AreEqual(375f, rising.y, 0.01f);
            Assert.AreEqual(500f, apex.y, 0.01f);
            Assert.AreEqual(0f, falling.y, 0.01f);
        }

        [Test]
        public void AlphaAt_IsHiddenBeforeTheDelayThenSolidThenFadesAtTheEnd()
        {
            var piece = new ConfettiPiece(Vector2.up, 0f, 0, 0.2f);

            Assert.AreEqual(0f, ConfettiPattern.AlphaAt(piece, 0.1f, 2f, 0.5f));
            Assert.AreEqual(1f, ConfettiPattern.AlphaAt(piece, 1f, 2f, 0.5f));
            Assert.AreEqual(0.5f, ConfettiPattern.AlphaAt(piece, 1.75f, 2f, 0.5f), 1e-4f);
            Assert.AreEqual(0f, ConfettiPattern.AlphaAt(piece, 2f, 2f, 0.5f));
        }

        [Test]
        public void AlphaAt_WithNoFade_CutsAtTheEnd()
        {
            var piece = new ConfettiPiece(Vector2.up, 0f, 0, 0f);

            Assert.AreEqual(1f, ConfettiPattern.AlphaAt(piece, 1.9f, 2f, 0f));
            Assert.AreEqual(0f, ConfettiPattern.AlphaAt(piece, 2f, 2f, 0f));
        }

        [Test]
        public void AngleAt_TurnsWithTheSpinFromTheDelay()
        {
            var piece = new ConfettiPiece(Vector2.up, 360f, 0, 0.5f);

            Assert.AreEqual(0f, ConfettiPattern.AngleAt(piece, 0.25f));
            Assert.AreEqual(180f, ConfettiPattern.AngleAt(piece, 1f), 1e-3f);
        }
    }
}

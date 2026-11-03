using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class BubbleCurveTests
    {
        private const float Tolerance = 1e-4f;
        private static readonly BubbleTiming _timing = new BubbleTiming(0.2f, 1.2f, 0.3f);

        [Test]
        public void TryAt_TheStart_IsInvisibleAndGrowing()
        {
            Assert.IsTrue(BubbleCurve.TryAt(0f, _timing, out var scale, out var alpha));
            Assert.AreEqual(0f, scale, Tolerance);
            Assert.AreEqual(1f, alpha, Tolerance);
        }

        [Test]
        public void TryAt_ThePop_OvershootsThenSettlesAtFullSize()
        {
            BubbleCurve.TryAt(0.15f, _timing, out var popping, out _);
            BubbleCurve.TryAt(0.5f, _timing, out var settled, out var alpha);

            Assert.Greater(popping, 1f);
            Assert.AreEqual(1f, settled, Tolerance);
            Assert.AreEqual(1f, alpha, Tolerance);
        }

        [Test]
        public void TryAt_TheFade_LowersTheAlphaAtFullSize()
        {
            Assert.IsTrue(BubbleCurve.TryAt(1.35f, _timing, out var scale, out var alpha));
            Assert.AreEqual(1f, scale, Tolerance);
            Assert.AreEqual(0.5f, alpha, Tolerance);
        }

        [Test]
        public void TryAt_AfterTheFade_IsOver()
        {
            Assert.IsFalse(BubbleCurve.TryAt(1.51f, _timing, out var scale, out var alpha));
            Assert.AreEqual(0f, scale);
            Assert.AreEqual(0f, alpha);
        }

        [Test]
        public void TryAt_BeforeTheStart_CountsAsTheStart()
        {
            Assert.IsTrue(BubbleCurve.TryAt(-1f, _timing, out var scale, out _));
            Assert.AreEqual(0f, scale, Tolerance);
        }
    }
}

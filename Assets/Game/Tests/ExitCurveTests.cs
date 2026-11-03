using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    /// <summary><see cref="ExitCurve"/> and <see cref="PuddleCurve"/> with their default looks.</summary>
    public class ExitCurveTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly ExitLook _exit = ExitLook.Default;
        private static readonly PuddleLook _puddle = PuddleLook.Default;

        [Test]
        public void TryPuff_SwellsToThePeakThenShrinksToNothing()
        {
            Assert.IsTrue(ExitCurve.TryPuff(0f, _exit, out var start));
            Assert.AreEqual(1f, start, Tolerance);

            Assert.IsTrue(ExitCurve.TryPuff(_exit.PuffSeconds * _exit.PuffPeakAt / 2f, _exit, out var swelling));
            Assert.AreEqual((1f + _exit.PuffPeak) / 2f, swelling, Tolerance);

            Assert.IsTrue(ExitCurve.TryPuff(_exit.PuffSeconds * _exit.PuffPeakAt, _exit, out var peak));
            Assert.AreEqual(_exit.PuffPeak, peak, Tolerance);

            Assert.IsTrue(ExitCurve.TryPuff(_exit.PuffSeconds * 0.99f, _exit, out var end));
            Assert.Less(end, 0.1f);

            Assert.IsFalse(ExitCurve.TryPuff(_exit.PuffSeconds, _exit, out var gone));
            Assert.AreEqual(0f, gone);
        }

        [Test]
        public void TryShrink_GoesEvenlyToNothing()
        {
            Assert.IsTrue(ExitCurve.TryShrink(0f, 0.5f, out var start));
            Assert.AreEqual(1f, start);
            Assert.IsTrue(ExitCurve.TryShrink(0.25f, 0.5f, out var half));
            Assert.AreEqual(0.5f, half, Tolerance);
            Assert.IsFalse(ExitCurve.TryShrink(0.5f, 0.5f, out var gone));
            Assert.AreEqual(0f, gone);
            Assert.IsFalse(ExitCurve.TryShrink(0f, 0f, out _));
        }

        [Test]
        public void PuddleTryAt_SpreadsFromItsFirstSize()
        {
            Assert.IsTrue(PuddleCurve.TryAt(0f, _puddle, out var scale, out var alpha));
            Assert.AreEqual(_puddle.GrowFrom, scale, Tolerance);
            Assert.AreEqual(1f, alpha);

            Assert.IsTrue(PuddleCurve.TryAt(_puddle.GrowSeconds, _puddle, out var grown, out _));
            Assert.AreEqual(1f, grown, Tolerance);
        }

        [Test]
        public void PuddleTryAt_FadesOverItsLastSeconds()
        {
            var fadeFrom = _puddle.Seconds - _puddle.FadeSeconds;

            Assert.IsTrue(PuddleCurve.TryAt(fadeFrom, _puddle, out _, out var before));
            Assert.AreEqual(1f, before, Tolerance);
            Assert.IsTrue(PuddleCurve.TryAt(fadeFrom + _puddle.FadeSeconds / 2f, _puddle, out var scale, out var half));
            Assert.AreEqual(0.5f, half, Tolerance);
            Assert.AreEqual(1f, scale, Tolerance);
        }

        [Test]
        public void PuddleTryAt_AfterItsTime_IsGone()
        {
            Assert.IsFalse(PuddleCurve.TryAt(_puddle.Seconds, _puddle, out var scale, out var alpha));
            Assert.AreEqual(0f, scale);
            Assert.AreEqual(0f, alpha);
        }
    }
}

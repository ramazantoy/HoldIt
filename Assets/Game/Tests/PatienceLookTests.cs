using HoldIt.Characters;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    /// <summary><see cref="PatienceColor"/> and <see cref="PatienceShake"/> with the prototype's look.</summary>
    public class PatienceLookTests
    {
        private const float Tolerance = 1e-3f;

        private static readonly PatienceLook _look = PatienceLook.Default;

        [Test]
        public void FromHsl_PrimaryHues_AreTheirColours()
        {
            AssertColor(Color.red, PatienceColor.FromHsl(0f, 1f, 0.5f));
            AssertColor(Color.green, PatienceColor.FromHsl(1f / 3f, 1f, 0.5f));
            AssertColor(Color.blue, PatienceColor.FromHsl(2f / 3f, 1f, 0.5f));
            AssertColor(new Color(0.5f, 0.5f, 0.5f), PatienceColor.FromHsl(0.4f, 0f, 0.5f));
        }

        [Test]
        public void At_FullPatience_IsGreen()
        {
            var color = PatienceColor.At(0f, _look);

            Assert.Greater(color.g, 0.9f);
            Assert.Less(color.r, 0.1f);
            Assert.Less(color.b, 0.1f);
        }

        [Test]
        public void At_Halfway_IsYellow()
        {
            var color = PatienceColor.At(0.5f, _look);

            Assert.AreEqual(color.r, color.g, 0.05f);
            Assert.Less(color.b, 0.1f);
        }

        [Test]
        public void At_OutOfPatience_IsRed()
        {
            var color = PatienceColor.At(1f, _look);

            AssertColor(new Color(0.925f, 0.075f, 0.075f), color);
            AssertColor(color, PatienceColor.At(2f, _look));
        }

        [Test]
        public void Urgency_IsNoneUpToTheThresholdAndFullAtTheEnd()
        {
            Assert.AreEqual(0f, PatienceShake.Urgency(0.5f, _look));
            Assert.AreEqual(0f, PatienceShake.Urgency(_look.ShakeFrom, _look));
            Assert.AreEqual(0.5f, PatienceShake.Urgency((_look.ShakeFrom + 1f) / 2f, _look), Tolerance);
            Assert.AreEqual(1f, PatienceShake.Urgency(1f, _look));
        }

        [Test]
        public void Offset_SwingsWithinTheAmplitude_OnlyPastTheThreshold()
        {
            var quarterSwing = Mathf.PI / 2f / _look.ShakeFrequency;

            Assert.AreEqual(0f, PatienceShake.Offset(quarterSwing, 0.6f, _look));
            Assert.AreEqual(_look.ShakeAmplitude, PatienceShake.Offset(quarterSwing, 1f, _look), Tolerance);
            Assert.AreEqual(-_look.ShakeAmplitude, PatienceShake.Offset(3f * quarterSwing, 1f, _look), Tolerance);
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, Tolerance, "r");
            Assert.AreEqual(expected.g, actual.g, Tolerance, "g");
            Assert.AreEqual(expected.b, actual.b, Tolerance, "b");
        }
    }
}

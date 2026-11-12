using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class BurstTimeTests
    {
        [Test]
        public void Of_IsTheDurationPlusTheLongestLife()
        {
            Assert.AreEqual(0.85f, BurstTime.Of(0.3f, 0.55f), 1e-5f);
        }

        [Test]
        public void Of_NeverLessThanATenthOfASecond()
        {
            Assert.AreEqual(0.1f, BurstTime.Of(0f, 0f), 1e-5f);
            Assert.AreEqual(0.1f, BurstTime.Of(-1f, 0.05f), 1e-5f);
        }

        [Test]
        public void Of_IgnoresNegativeParts()
        {
            Assert.AreEqual(0.5f, BurstTime.Of(0.5f, -2f), 1e-5f);
        }
    }
}

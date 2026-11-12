using HoldIt.Characters;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class SweatRuleTests
    {
        private const float From = 0.55f;
        private const float Margin = 0.05f;

        [Test]
        public void Next_NotSweating_StartsAtTheThreshold()
        {
            Assert.IsFalse(SweatRule.Next(false, 0.54f, From, Margin));
            Assert.IsTrue(SweatRule.Next(false, 0.55f, From, Margin));
            Assert.IsTrue(SweatRule.Next(false, 0.9f, From, Margin));
        }

        [Test]
        public void Next_Sweating_KeepsGoingUntilItFallsAMarginUnder()
        {
            // Not exactly 0.5: 0.55f - 0.05f is not 0.5 in floats.
            Assert.IsTrue(SweatRule.Next(true, 0.55f, From, Margin));
            Assert.IsTrue(SweatRule.Next(true, 0.505f, From, Margin));
            Assert.IsFalse(SweatRule.Next(true, 0.495f, From, Margin));
        }

        [Test]
        public void Next_AtTheEdge_DoesNotBlink()
        {
            // Hovering just under the threshold after it was reached keeps sweating; just over it does not start it early.
            Assert.IsTrue(SweatRule.Next(true, 0.53f, From, Margin));
            Assert.IsFalse(SweatRule.Next(false, 0.53f, From, Margin));
        }

        [Test]
        public void Next_AFreshCharacter_IsDry()
        {
            Assert.IsFalse(SweatRule.Next(false, 0f, From, Margin));
        }
    }
}

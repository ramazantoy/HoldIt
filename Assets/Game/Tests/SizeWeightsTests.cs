using HoldIt.Characters;
using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class SizeWeightsTests
    {
        [Test]
        public void Of_GivesEachSizesOwnWeight()
        {
            var weights = new SizeWeights(1f, 2f, 3f);

            Assert.AreEqual(1f, weights.Of(BodySize.Thin));
            Assert.AreEqual(2f, weights.Of(BodySize.Normal));
            Assert.AreEqual(3f, weights.Of(BodySize.Fat));
        }

        [Test]
        public void Total_AddsTheThree()
        {
            Assert.AreEqual(6f, new SizeWeights(1f, 2f, 3f).Total, 1e-6f);
        }

        [Test]
        public void Default_IsTheGddMix()
        {
            var weights = SizeWeights.Default;

            Assert.AreEqual(1f, weights.Total, 1e-5f);
            Assert.AreEqual(0.30f, weights.Of(BodySize.Thin), 1e-5f);
            Assert.AreEqual(0.45f, weights.Of(BodySize.Normal), 1e-5f);
            Assert.AreEqual(0.25f, weights.Of(BodySize.Fat), 1e-5f);
        }
    }
}

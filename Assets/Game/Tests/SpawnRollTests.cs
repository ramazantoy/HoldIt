using System;
using System.Collections.Generic;
using HoldIt.Characters;
using HoldIt.Levels;
using HoldIt.Spawning;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class SpawnRollTests
    {
        private const int Rolls = 10000;

        [Test]
        public void Size_FollowsTheWeights()
        {
            var counts = Count(new SizeWeights(1f, 2f, 1f));

            Assert.AreEqual(0.25, counts[BodySize.Thin] / (double)Rolls, 0.02);
            Assert.AreEqual(0.50, counts[BodySize.Normal] / (double)Rolls, 0.02);
            Assert.AreEqual(0.25, counts[BodySize.Fat] / (double)Rolls, 0.02);
        }

        [Test]
        public void Size_AZeroWeight_NeverComes()
        {
            var counts = Count(new SizeWeights(0.3f, 0.7f, 0f));

            Assert.AreEqual(0, counts[BodySize.Fat]);
            Assert.Greater(counts[BodySize.Thin], 0);
        }

        [Test]
        public void Size_EveryWeightZero_IsNormal()
        {
            var counts = Count(new SizeWeights(0f, 0f, 0f));

            Assert.AreEqual(Rolls, counts[BodySize.Normal]);
        }

        [Test]
        public void Gender_FollowsTheShareOfMen()
        {
            var random = new Random(7);
            var men = 0;
            for (var i = 0; i < Rolls; i++)
            {
                if (SpawnRoll.Gender(random, 0.3f) == Gender.Male)
                    men++;
            }

            Assert.AreEqual(0.3, men / (double)Rolls, 0.02);
            Assert.AreEqual(Gender.Female, SpawnRoll.Gender(random, 0f));
            Assert.AreEqual(Gender.Male, SpawnRoll.Gender(random, 1f));
        }

        private static Dictionary<BodySize, int> Count(SizeWeights weights)
        {
            var random = new Random(5);
            var counts = new Dictionary<BodySize, int>
            {
                { BodySize.Thin, 0 },
                { BodySize.Normal, 0 },
                { BodySize.Fat, 0 },
            };
            for (var i = 0; i < Rolls; i++)
                counts[SpawnRoll.Size(random, weights)]++;
            return counts;
        }
    }
}

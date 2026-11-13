using System;
using System.Collections.Generic;
using System.Linq;
using HoldIt.Backdrop;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class BagExtensionsTests
    {
        [Test]
        public void Draw_OneBag_GivesEveryNumberOnce()
        {
            var bag = new List<int>();
            var random = new Random(7);

            var drawn = Enumerable.Range(0, 5).Select(_ => bag.Draw(5, -1, random)).ToList();

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4 }, drawn);
            Assert.IsEmpty(bag);
        }

        [Test]
        public void Draw_AcrossManyBags_NeverRepeatsTheLastDraw()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var bag = new List<int>();
                var random = new Random(seed);
                var last = -1;
                for (var i = 0; i < 60; i++)
                {
                    var drawn = bag.Draw(4, last, random);
                    Assert.AreNotEqual(last, drawn, $"Seed {seed}, draw {i}.");
                    last = drawn;
                }
            }
        }

        [Test]
        public void Draw_EveryBag_HoldsEveryNumberOnce()
        {
            var bag = new List<int>();
            var random = new Random(3);
            var last = -1;

            for (var round = 0; round < 10; round++)
            {
                var drawn = new List<int>();
                for (var i = 0; i < 6; i++)
                {
                    last = bag.Draw(6, last, random);
                    drawn.Add(last);
                }
                CollectionAssert.AreEquivalent(Enumerable.Range(0, 6), drawn, $"Round {round}.");
            }
        }

        [Test]
        public void Draw_OneNumber_AlwaysGivesIt()
        {
            var bag = new List<int>();
            var random = new Random(1);

            Assert.AreEqual(0, bag.Draw(1, 0, random));
            Assert.AreEqual(0, bag.Draw(1, 0, random));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Draw_NothingToDrawFrom_Throws(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new List<int>().Draw(count, -1, new Random(1)));
        }
    }
}

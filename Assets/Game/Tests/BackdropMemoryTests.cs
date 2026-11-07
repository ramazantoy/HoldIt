using System;
using System.Collections.Generic;
using System.Linq;
using HoldIt.Backdrop;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class BackdropMemoryTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(6)]
        public void Draw_GivesEveryNumberOncePerBag(int count)
        {
            var bag = new List<int>();
            var random = new Random(3);
            var drawn = Enumerable.Range(0, count).Select(_ => bag.Draw(count, -1, random)).ToList();

            Assert.That(drawn.OrderBy(n => n), Is.EqualTo(Enumerable.Range(0, count)));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(6)]
        public void Draw_NeverRepeatsTheLastOneAcrossBags(int count)
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var bag = new List<int>();
                var random = new Random(seed);
                var previous = -1;
                for (var i = 0; i < count * 6; i++)
                {
                    var next = bag.Draw(count, previous, random);
                    Assert.That(next, Is.Not.EqualTo(previous), $"seed {seed}, draw {i}");
                    previous = next;
                }
            }
        }

        [Test]
        public void Draw_WithoutAnythingToDraw_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new List<int>().Draw(0, -1, new Random(1)));

        [Test]
        public void Choose_AtFirst_HasNoPaletteBefore() =>
            Assert.That(new BackdropMemory(new Random(1)).Current, Is.EqualTo(-1));

        [Test]
        public void Choose_ANewLevel_TakesAnotherPalette()
        {
            var memory = new BackdropMemory(new Random(5));
            var previous = memory.Choose(0, 4);
            for (var level = 1; level < 30; level++)
            {
                var next = memory.Choose(level, 4);
                Assert.That(next, Is.Not.EqualTo(previous), $"level {level}");
                Assert.That(memory.Current, Is.EqualTo(next));
                previous = next;
            }
        }

        [Test]
        public void Choose_TheSameLevelAgain_KeepsThePalette()
        {
            var memory = new BackdropMemory(new Random(5));
            var first = memory.Choose(2, 4);

            Assert.That(memory.Choose(2, 4), Is.EqualTo(first));
            Assert.That(memory.Choose(2, 4), Is.EqualTo(first));
        }
    }
}

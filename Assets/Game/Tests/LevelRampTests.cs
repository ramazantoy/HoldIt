using System;
using HoldIt.Editor;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class LevelRampTests
    {
        private const float Tolerance = 1e-4f;
        private const int Count = 30;

        private static readonly LevelRamp _ramp = LevelRamp.Default;

        [Test]
        public void At_TheFirstLevel_HasTheFirstValues()
        {
            var first = _ramp.At(0, Count);

            Assert.AreEqual(6, first.Crowd);
            Assert.AreEqual(4.6f, first.SpawnStart, Tolerance);
            Assert.AreEqual(3.4f, first.SpawnEnd, Tolerance);
            Assert.AreEqual(8, first.MaxCharacters);
            Assert.AreEqual(0f, first.Sizes.Fat);
            Assert.AreEqual(0.3f, first.Sizes.Thin, Tolerance);
            Assert.AreEqual(0.7f, first.Sizes.Normal, Tolerance);
        }

        [Test]
        public void At_TheLastLevel_HasTheLastValues()
        {
            var last = _ramp.At(Count - 1, Count);

            Assert.AreEqual(36, last.Crowd);
            Assert.AreEqual(2.6f, last.SpawnStart, Tolerance);
            Assert.AreEqual(1.5f, last.SpawnEnd, Tolerance);
            Assert.AreEqual(14, last.MaxCharacters);
            Assert.AreEqual(0.35f, last.Sizes.Fat, Tolerance);
            Assert.AreEqual(0.35f, last.Sizes.Normal, Tolerance);
        }

        [Test]
        public void At_FatComeFromTheirLevel()
        {
            Assert.AreEqual(0f, _ramp.At(1, Count).Sizes.Fat);
            Assert.AreEqual(0.15f, _ramp.At(2, Count).Sizes.Fat, Tolerance);
        }

        [Test]
        public void At_EveryLevel_IsAtLeastAsBusyAsTheOneBefore()
        {
            var before = _ramp.At(0, Count);
            for (var i = 1; i < Count; i++)
            {
                var level = _ramp.At(i, Count);
                Assert.GreaterOrEqual(level.Crowd, before.Crowd, $"Level {i + 1}");
                Assert.LessOrEqual(level.SpawnStart, before.SpawnStart, $"Level {i + 1}");
                Assert.LessOrEqual(level.SpawnEnd, before.SpawnEnd, $"Level {i + 1}");
                Assert.LessOrEqual(level.SpawnEnd, level.SpawnStart, $"Level {i + 1}");
                Assert.GreaterOrEqual(level.MaxCharacters, before.MaxCharacters, $"Level {i + 1}");
                Assert.GreaterOrEqual(level.Sizes.Fat, before.Sizes.Fat, $"Level {i + 1}");
                Assert.AreEqual(1f, level.Sizes.Total, Tolerance, $"Level {i + 1}");
                before = level;
            }
        }

        [Test]
        public void At_ASingleLevel_HasTheFirstValues()
        {
            Assert.AreEqual(6, _ramp.At(0, 1).Crowd);
        }

        [Test]
        public void At_NumbersAreInHundredths()
        {
            for (var i = 0; i < Count; i++)
            {
                var level = _ramp.At(i, Count);
                Assert.AreEqual(Mathf.Round(level.SpawnStart * 100f), level.SpawnStart * 100f, Tolerance);
                Assert.AreEqual(Mathf.Round(level.SpawnEnd * 100f), level.SpawnEnd * 100f, Tolerance);
            }
        }

        [Test]
        public void At_OutsideTheLevels_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ramp.At(Count, Count));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ramp.At(-1, Count));
        }
    }
}

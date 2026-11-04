using System.Collections.Generic;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Editor;
using HoldIt.Levels;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class LevelValidatorTests
    {
        private readonly List<Object> _made = new List<Object>();
        private MapDefinition _map;

        [SetUp]
        public void CreateMap() => _map = Keep(TestLevels.Map(TestMaps.Lobby));

        [TearDown]
        public void DestroyAssets()
        {
            foreach (var asset in _made)
                Object.DestroyImmediate(asset);
            _made.Clear();
        }

        [Test]
        public void PlayableLevel_IsNotReported()
        {
            var report = CheckLevel(TestLevels.Level(8, map: _map));

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void LevelWithoutAMap_IsReportedOnTheAsset()
        {
            var level = Keep(TestLevels.Level(8));
            var report = new ValidationReport();

            LevelValidator.CheckLevel(level, report);

            Assert.AreEqual(1, report.ErrorCount);
            StringAssert.Contains("no map", report.Issues[0].Message);
            Assert.AreSame(level, report.Issues[0].Context);
        }

        [Test]
        public void NobodyComingOrNoRoom_IsReported()
        {
            Assert.AreEqual(1, CheckLevel(TestLevels.Level(0, map: _map)).ErrorCount);
            Assert.AreEqual(1, CheckLevel(TestLevels.Level(8, maxCharacters: 0, map: _map)).ErrorCount);
        }

        [Test]
        public void GapsNotPositiveOrGrowing_AreReported()
        {
            var zero = CheckLevel(TestLevels.Level(8, spawnStart: 2f, spawnEnd: 0f, map: _map));
            Assert.AreEqual(1, zero.ErrorCount);
            StringAssert.Contains("positive", zero.Issues[0].Message);

            var growing = CheckLevel(TestLevels.Level(8, spawnStart: 2f, spawnEnd: 3f, map: _map));
            Assert.AreEqual(1, growing.ErrorCount);
            StringAssert.Contains("grow", growing.Issues[0].Message);

            Assert.AreEqual(0, CheckLevel(TestLevels.Level(8, spawnStart: 2f, spawnEnd: 2f, map: _map)).ErrorCount);
        }

        [Test]
        public void SizeWeightsNegativeOrAllZero_AreReported()
        {
            Assert.AreEqual(1, CheckLevel(TestLevels.Level(8, sizes: new SizeWeights(0f, 0f, 0f), map: _map)).ErrorCount);
            Assert.AreEqual(1, CheckLevel(TestLevels.Level(8, sizes: new SizeWeights(-1f, 1f, 1f), map: _map)).ErrorCount);
            Assert.AreEqual(0, CheckLevel(TestLevels.Level(8, sizes: new SizeWeights(0f, 0f, 1f), map: _map)).ErrorCount);
        }

        [Test]
        public void EmptyList_IsReported()
        {
            var list = Keep(TestLevels.List());
            var report = new ValidationReport();

            LevelValidator.CheckList(list, report);

            Assert.AreEqual(1, report.ErrorCount);
            StringAssert.Contains("empty", report.Issues[0].Message);
        }

        [Test]
        public void ListWithAMissingLevel_IsReported()
        {
            var list = Keep(TestLevels.List(Keep(TestLevels.Level(8, map: _map)), null));
            var report = new ValidationReport();

            LevelValidator.CheckList(list, report);

            Assert.AreEqual(1, report.ErrorCount);
            StringAssert.Contains("level 2", report.Issues[0].Message);
        }

        [Test]
        public void TwoLevelsOnOneMap_AreReportedOnceOnTheMap()
        {
            var first = Keep(TestLevels.Level(8, map: _map));
            first.name = "Level_A";
            var second = Keep(TestLevels.Level(8, map: _map));
            second.name = "Level_B";
            var report = new ValidationReport();

            LevelValidator.CheckSharedMaps(new[] { first, second }, report);

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("'Level_A'", report.Issues[0].Message);
            StringAssert.Contains("'Level_B'", report.Issues[0].Message);
            Assert.AreSame(_map, report.Issues[0].Context);
        }

        [Test]
        public void ThreeLevelsOnOneMap_AreOneError()
        {
            var levels = new[]
            {
                Keep(TestLevels.Level(8, map: _map)), Keep(TestLevels.Level(8, map: _map)),
                Keep(TestLevels.Level(8, map: _map)),
            };
            var report = new ValidationReport();

            LevelValidator.CheckSharedMaps(levels, report);

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("3 levels", report.Issues[0].Message);
        }

        [Test]
        public void LevelsOnTheirOwnMaps_AreNotReported()
        {
            // Two maps with the same rows and name are still two maps.
            var other = Keep(TestLevels.Map(TestMaps.Lobby));
            other.name = _map.name;
            var levels = new[] { Keep(TestLevels.Level(8, map: _map)), Keep(TestLevels.Level(8, map: other)) };
            var report = new ValidationReport();

            LevelValidator.CheckSharedMaps(levels, report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void LevelsWithoutAMap_DoNotShareOne()
        {
            var levels = new[] { Keep(TestLevels.Level(8)), Keep(TestLevels.Level(8)) };
            var report = new ValidationReport();

            LevelValidator.CheckSharedMaps(levels, report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        private ValidationReport CheckLevel(LevelDefinition level)
        {
            var report = new ValidationReport();
            LevelValidator.CheckLevel(Keep(level), report);
            return report;
        }

        private T Keep<T>(T asset) where T : Object
        {
            _made.Add(asset);
            return asset;
        }
    }
}

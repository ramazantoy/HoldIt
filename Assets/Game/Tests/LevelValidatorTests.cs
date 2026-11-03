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

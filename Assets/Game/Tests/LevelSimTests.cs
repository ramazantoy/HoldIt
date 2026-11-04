using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Editor.Simulation;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Spawning;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class LevelSimTests
    {
        private const float TimeStep = 0.05f;
        private const float TimeLimit = 600f;

        private readonly List<Object> _made = new List<Object>();
        private ITileGrid _lobby;
        private SimRules _rules;

        [SetUp]
        public void CreateRules()
        {
            _lobby = MapParser.Parse(TestMaps.Lobby);
            _rules = new SimRules(Keep(ScriptableObject.CreateInstance<CharacterConfig>()),
                Keep(ScriptableObject.CreateInstance<RestroomConfig>()), Keep(ScriptableObject.CreateInstance<SpawnConfig>()),
                Keep(ScriptableObject.CreateInstance<RunConfig>()));
        }

        [TearDown]
        public void DestroyAssets()
        {
            foreach (var asset in _made)
                Object.DestroyImmediate(asset);
            _made.Clear();
        }

        [Test]
        public void SameSeed_PlaysTheSameLevel()
        {
            var level = Keep(TestLevels.Level(12, maxCharacters: 10));

            var first = Run(level, pace: 1.5f, seed: 3);
            var second = Run(level, pace: 1.5f, seed: 3);

            Assert.AreEqual(first.Outcome, second.Outcome);
            Assert.AreEqual(first.Served, second.Served);
            Assert.AreEqual(first.Accidents, second.Accidents);
            Assert.AreEqual(first.Seconds, second.Seconds);
            Assert.AreEqual(first.Blocked, second.Blocked);
        }

        [Test]
        public void QuickPlayer_WinsAQuietLevel_WithEveryStar()
        {
            var level = Keep(TestLevels.Level(6, spawnStart: 4.5f, spawnEnd: 3.5f, sizes: new SizeWeights(0.3f, 0.7f, 0f)));

            for (var seed = 1; seed <= 3; seed++)
            {
                var result = Run(level, pace: 1f, seed: seed);

                Assert.AreEqual(LevelOutcome.Won, result.Outcome, $"Seed {seed}");
                Assert.AreEqual(3, result.Stars, $"Seed {seed}");
                Assert.AreEqual(6, result.Served, $"Seed {seed}");
            }
        }

        [Test]
        public void NobodyDrawing_LosesTheLevelToAccidents()
        {
            var level = Keep(TestLevels.Level(10));

            var result = Run(level, pace: 0f, seed: 1);

            Assert.AreEqual(LevelOutcome.Lost, result.Outcome);
            Assert.AreEqual(3, result.Accidents, "The default run has 3 lives.");
            Assert.AreEqual(0, result.Served);
            Assert.Greater(result.Seconds, 30f, "Nobody runs out of patience before 30 seconds.");
        }

        [Test]
        public void FatCrowd_IsNeverSentThroughANarrowPassageOrTheWrongDoor()
        {
            // Lobby's quickest way up for anyone goes through its narrow pair.
            var level = Keep(TestLevels.Level(12, sizes: new SizeWeights(0f, 0f, 1f)));

            for (var seed = 1; seed <= 3; seed++)
            {
                var result = Run(level, pace: 1f, seed: seed);

                Assert.AreEqual(0, result.RuleRefusals, $"Seed {seed}");
                Assert.AreEqual(LevelOutcome.Won, result.Outcome, $"Seed {seed}");
            }
        }

        private SimResult Run(LevelDefinition level, float pace, int seed) =>
            LevelSim.Run(_lobby, 2, level, _rules, pace, seed, TimeStep, TimeLimit);

        private T Keep<T>(T asset) where T : Object
        {
            _made.Add(asset);
            return asset;
        }
    }
}

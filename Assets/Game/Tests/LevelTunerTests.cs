using System.Collections.Generic;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Editor.Simulation;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Spawning;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldIt.Tests
{
    public class LevelTunerTests
    {
        private readonly List<Object> _made = new List<Object>();
        private ITileGrid _lobby;
        private SimRules _rules;
        private LevelSimConfig _config;
        private LevelDefinition _level;

        [SetUp]
        public void CreateLevel()
        {
            _lobby = MapParser.Parse(TestMaps.Lobby);
            _rules = new SimRules(Keep(ScriptableObject.CreateInstance<CharacterConfig>()),
                Keep(ScriptableObject.CreateInstance<RestroomConfig>()), Keep(ScriptableObject.CreateInstance<SpawnConfig>()),
                Keep(ScriptableObject.CreateInstance<RunConfig>()));
            _config = Keep(ScriptableObject.CreateInstance<LevelSimConfig>());
            var serialized = new SerializedObject(_config);
            serialized.FindProperty("_seeds").intValue = 6;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _level = Keep(TestLevels.Level(40, maxCharacters: 12, spawnStart: 3f, spawnEnd: 2f));
        }

        [TearDown]
        public void DestroyAssets()
        {
            foreach (var asset in _made)
                Object.DestroyImmediate(asset);
            _made.Clear();
        }

        [Test]
        public void FittedNumbers_HaveTheTargetHeadroom()
        {
            var numbers = Fit(headroom: 1.5f);
            Assert.LessOrEqual(numbers.SpawnEnd, numbers.SpawnStart, "The last gap is never longer than the first.");
            Assert.AreEqual(3f, _level.SpawnStart, "The level itself is not changed.");
            Assert.AreEqual(12, _level.MaxCharacters, "The level itself is not changed.");

            var fitted = Keep(Object.Instantiate(_level));
            numbers.WriteTo(fitted);
            var headroom = LevelTuner.Measure(_lobby, 2, fitted, _rules, _config);

            Assert.IsFalse(headroom.AtLeast, headroom.ToString());
            Assert.AreEqual(1.5f, headroom.Value, 1.5f * _config.TargetTolerance, $"{numbers}, measured {headroom}");
            Assert.GreaterOrEqual(SimBatch.Play(_lobby, 2, fitted, _rules, _config.ReferencePace, _config).WinShare,
                _config.WinShare, "The reference player wins the fitted level.");
        }

        [Test]
        public void MoreHeadroom_MeansLessPressure()
        {
            var tight = Fit(headroom: 1.1f);
            var loose = Fit(headroom: 1.8f);

            Assert.Greater(tight.Pressure, loose.Pressure, $"tight {tight}, loose {loose}");
            Assert.Less(tight.SpawnStart, loose.SpawnStart);
            Assert.GreaterOrEqual(tight.MaxCharacters, loose.MaxCharacters);
            Assert.AreEqual(1.8f / 1.1f, tight.Pressure / loose.Pressure, 1e-3f, "Both come from the level's most pressure.");
        }

        private LevelTuner.Numbers Fit(float headroom)
        {
            Assert.IsTrue(LevelTuner.TryFit(_lobby, 2, _level, _level.SpawnStart, _level.SpawnEnd, _level.MaxCharacters,
                headroom, _rules, _config, out var numbers));
            return numbers;
        }

        private T Keep<T>(T asset) where T : Object
        {
            _made.Add(asset);
            return asset;
        }
    }
}

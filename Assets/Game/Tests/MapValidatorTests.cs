using System.Linq;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldIt.Tests
{
    public class MapValidatorTests
    {
        private MapDefinition _map;

        [SetUp]
        public void CreateMap() => _map = ScriptableObject.CreateInstance<MapDefinition>();

        [TearDown]
        public void DestroyMap() => Object.DestroyImmediate(_map);

        [Test]
        public void PlayableMap_IsNotReported()
        {
            var report = Check(TestMaps.Lobby);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void MapThatDoesNotParse_IsReportedOnTheAsset()
        {
            var report = Check("...", ".x.");

            Assert.AreEqual(1, report.ErrorCount);
            StringAssert.Contains("Unknown tile 'x'", report.Issues[0].Message);
            Assert.AreSame(_map, report.Issues[0].Context);
        }

        [Test]
        public void MapThatCannotBePlayed_ReportsEachProblem()
        {
            var report = Check("MMMWWW", "MMMWWW", "......", "LLLLLL", "##EE##");

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            Assert.IsTrue(report.Issues.Any(issue => issue.Message.Contains("no men's door")));
            Assert.IsTrue(report.Issues.Any(issue => issue.Message.Contains("no women's door")));
        }

        private ValidationReport Check(params string[] rows)
        {
            var serialized = new SerializedObject(_map);
            var property = serialized.FindProperty("_rows");
            property.arraySize = rows.Length;
            for (var i = 0; i < rows.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = rows[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var report = new ValidationReport();
            MapValidator.Check(_map, report);
            return report;
        }
    }
}

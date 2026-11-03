using System.Linq;
using AF.Core.Editor;
using HoldIt.Board;
using HoldIt.Editor;
using HoldIt.Restrooms;
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

        [Test]
        public void PlayableMapWhoseRestroomsAreTooShallow_ReportsEachRestroom()
        {
            var report = Check(TestMaps.Small);

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            Assert.IsTrue(report.Issues.Any(issue => issue.Message.Contains("men's restroom is 2 tiles deep")));
            Assert.IsTrue(report.Issues.Any(issue => issue.Message.Contains("women's restroom is 2 tiles deep")));
            Assert.AreSame(_map, report.Issues[0].Context);
        }

        [Test]
        public void MapWithMoreStallsThanFit_IsReported()
        {
            var serialized = new SerializedObject(_map);
            serialized.FindProperty("_stallsPerRestroom").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var report = Check(TestMaps.Lobby);

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            Assert.IsTrue(report.Issues.All(issue => issue.Message.Contains("4 stalls need")));
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
            MapValidator.Check(_map, StallGeometry.Default, report);
            return report;
        }
    }
}

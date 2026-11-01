using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core.Editor;
using AF.Save.Editor;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace AF.Save.Tests
{
    public class SaveValidatorTests
    {
        [Test]
        public void UnityObjectSavable_IsAnError()
        {
            var report = Check(typeof(ComponentSavable), typeof(AssetSavable));

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            StringAssert.Contains(nameof(AssetSavable), report.Issues[0].Message);
            StringAssert.Contains(nameof(ComponentSavable), report.Issues[1].Message);
        }

        [Test]
        public void SupportedData_HasNoErrors()
        {
            var report = Check(typeof(GoodSavable), typeof(Counter));

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void UnsupportedData_ReportsEveryProblem()
        {
            var report = Check(typeof(BadSavable));
            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));

            StringAssert.Contains("BadData.shape is IShape", messages);
            StringAssert.Contains("BadData.target references a Unity object", messages);
            StringAssert.Contains("BadData.grid has Vector2Int keys", messages);
            StringAssert.Contains("BadData.area is Rect", messages);
            StringAssert.Contains("BadData.Total has no public setter", messages);
            StringAssert.Contains("BadData.seed is readonly", messages);
            StringAssert.Contains("BadData.nested (NoDefault) has no public parameterless constructor", messages);
            StringAssert.Contains("BadData.items[] is IShape", messages);
            StringAssert.Contains("BadData.outline is Outline", messages);
            StringAssert.Contains("BadData.anything is Object", messages);
            Assert.AreEqual(10, report.ErrorCount, messages);
        }

        private static ValidationReport Check(params Type[] types)
        {
            var report = new ValidationReport();
            SaveValidator.Check(types, report);
            return report;
        }

        public sealed class ComponentSavable : MonoBehaviour, ISavable
        {
            public string Key => "component";
            public SaveDocument Document => SaveDocument.Slot;
            public Type DataType => typeof(CounterData);
            public object CaptureData() => new CounterData();

            public void RestoreData(object data)
            {
            }

            public object CreateDefaultData() => new CounterData();
        }

        public sealed class AssetSavable : ScriptableObject, ISavable
        {
            public string Key => "asset";
            public SaveDocument Document => SaveDocument.Slot;
            public Type DataType => typeof(CounterData);
            public object CaptureData() => new CounterData();

            public void RestoreData(object data)
            {
            }

            public object CreateDefaultData() => new CounterData();
        }

        public enum Kind
        {
            Small,
            Large,
        }

        public sealed class Item
        {
            public string id;
            public Kind kind;
        }

        public sealed class GoodData
        {
            public int count;
            public float? optional;
            public string name;
            public DateTime createdUtc;
            public List<Item> items = new List<Item>();
            public Item[] slots;
            public Dictionary<string, int> byName = new Dictionary<string, int>();
            public Dictionary<Kind, List<int>> byKind = new Dictionary<Kind, List<int>>();
            public Vector3 position;
            public Quaternion rotation;
            public Color tint;
            public int Level { get; set; }

            [JsonIgnore]
            public int Doubled => count * 2;
        }

        public sealed class GoodSavable : Savable<GoodData>
        {
            public override string Key => "good";
            protected override GoodData Capture() => new GoodData();

            protected override void Restore(GoodData data)
            {
            }
        }

        public interface IShape
        {
        }

        public abstract class Outline
        {
        }

        public sealed class NoDefault
        {
            public NoDefault(int value) => this.value = value;
            public int value;
        }

        public sealed class BadData
        {
            public IShape shape;
            public Transform target;
            public Dictionary<Vector2Int, int> grid;
            public Rect area;
            public int Total => 1;
            public readonly int seed = 1;
            public NoDefault nested;
            public List<IShape> items;
            public Outline outline;
            public object anything;
        }

        public sealed class BadSavable : Savable<BadData>
        {
            public override string Key => "bad";
            protected override BadData Capture() => new BadData();

            protected override void Restore(BadData data)
            {
            }
        }
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AF.Save.Tests
{
    public class SaveJsonTests
    {
        public enum Mood
        {
            Calm,
            Angry,
        }

        public sealed class MoodData
        {
            public Mood mood;
        }

        public sealed class ListData
        {
            public List<int> values = new List<int> { 1, 2 };
        }

        public sealed class TextData
        {
            public string text;
            public float amount;
        }

        public sealed class NestedData
        {
            public TextData inner = new TextData();
        }

        public sealed class UnityData
        {
            public Vector2 v2;
            public Vector3 v3;
            public Vector2Int v2i;
            public Vector3Int v3i;
            public Quaternion rotation;
            public Color color;
        }

        [Test]
        public void Enums_AreWrittenAsStrings()
        {
            var token = SaveJson.ToToken(new MoodData { mood = Mood.Angry });

            Assert.AreEqual(JTokenType.String, token["mood"].Type);
            Assert.AreEqual("Angry", (string)token["mood"]);
            Assert.AreEqual(Mood.Angry, ((MoodData)SaveJson.FromToken(token, typeof(MoodData))).mood);
        }

        [Test]
        public void ListWithInitializer_IsReplaced_NotAppended()
        {
            var data = (ListData)SaveJson.FromToken(JObject.Parse("{\"values\":[3]}"), typeof(ListData));

            CollectionAssert.AreEqual(new[] { 3 }, data.values);
        }

        [Test]
        public void DateLikeString_SurvivesParsing()
        {
            const string text = "2026-10-30T12:00:00Z";
            var content = TestDocuments.Document(1, $"{{\"note\":{{\"text\":\"{text}\"}}}}");

            var sections = ParsedDocument.From(SaveFormat.Parse(content)).Sections;
            var data = (TextData)SaveJson.FromToken(sections["note"], typeof(TextData));

            Assert.AreEqual(text, data.text);
        }

        [Test]
        public void TypeNames_AreNotWritten()
        {
            var token = SaveJson.ToToken(new NestedData { inner = new TextData { text = "a" } });

            StringAssert.DoesNotContain("$type", token.ToString());
        }

        [Test]
        public void UnityTypes_RoundTrip_AsTheirComponents()
        {
            var data = new UnityData
            {
                v2 = new Vector2(1.5f, -2f),
                v3 = new Vector3(1f, 2f, 3f),
                v2i = new Vector2Int(4, 5),
                v3i = new Vector3Int(6, 7, 8),
                rotation = Quaternion.Euler(10f, 20f, 30f),
                color = new Color(0.1f, 0.2f, 0.3f, 0.4f),
            };

            var token = SaveJson.ToToken(data);
            var back = (UnityData)SaveJson.FromToken(JToken.Parse(token.ToString()), typeof(UnityData));

            Assert.AreEqual(3, ((JObject)token["v3"]).Count, "Only x, y and z are written.");
            Assert.AreEqual(data.v2, back.v2);
            Assert.AreEqual(data.v3, back.v3);
            Assert.AreEqual(data.v2i, back.v2i);
            Assert.AreEqual(data.v3i, back.v3i);
            Assert.AreEqual(data.rotation, back.rotation);
            Assert.AreEqual(data.color, back.color);
        }

        [Test]
        public void TurkishCulture_DoesNotChangeTheFormat()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                // Decimal comma and the dotted/dotless I: both have broken saves written under a Turkish locale.
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                var sections = new JObject { ["title-items"] = SaveJson.ToToken(new TextData { text = "I", amount = 1.5f }) };

                var content = SaveFormat.Write(1, new JObject(), sections);
                var back = (TextData)SaveJson.FromToken(ParsedDocument.From(SaveFormat.Parse(content)).Sections["title-items"], typeof(TextData));

                StringAssert.Contains("1.5", content);
                Assert.AreEqual(1.5f, back.amount);
                Assert.AreEqual("I", back.text);
                Assert.DoesNotThrow(() => new SaveRegistry().Register(new Counter("title-items")));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AF.Build.Editor;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Build.Tests
{
    public class SerializedLayoutTests
    {
        private const string FIXTURE_TYPE = "AF.Build.Tests.SerializedLayoutTests+LayoutFixture";
        private const string FIXTURE_FOLDER = "Temp/SerializedLayoutTests";
        private const float CompileTimeoutSeconds = 120f;

        [Serializable]
        public class LayoutFixture
        {
            public int publicField;
            [SerializeField] private int _serialized = 1;
            [SerializeReference] private object _reference = null;
            [NonSerialized] public int nonSerialized;
            private int _plain = 2;
            public static int staticField;
            public const int ConstField = 3;
            public readonly int readonlyField = 4;

            [field: SerializeField]
            public int Property { get; set; }

            public object Use() => _serialized + _plain + readonlyField + (_reference ?? 0).GetHashCode();
        }

        [Test]
        public void ReadFields_FollowsUnitySerializationRules()
        {
            var types = SerializedLayoutCheck.ReadFields(typeof(SerializedLayoutTests).Assembly.Location);

            CollectionAssert.AreEquivalent(new[]
            {
                "publicField: System.Int32",
                "_serialized: System.Int32",
                "_reference: System.Object",
                "<Property>k__BackingField: System.Int32",
            }, types[FIXTURE_TYPE]);
        }

        public class FixtureBehaviour : MonoBehaviour
        {
        }

        public enum FixtureEnum
        {
            A,
        }

        [Test]
        public void IsUnitySerialized_TakesUnityObjectsAndSerializableTypes()
        {
            var isSerialized = SerializedLayoutCheck.IsUnitySerialized(typeof(SerializedLayoutTests).Assembly);

            Assert.IsTrue(isSerialized(FIXTURE_TYPE));
            Assert.IsTrue(isSerialized("AF.Build.Tests.SerializedLayoutTests+FixtureBehaviour"));
            Assert.IsFalse(isSerialized("AF.Build.Tests.SerializedLayoutTests"));
            Assert.IsFalse(isSerialized("AF.Build.Tests.SerializedLayoutTests+FixtureEnum"));
            Assert.IsFalse(isSerialized("AF.Build.Tests.Missing"));
        }

        [Test]
        public void Compare_ReportsFieldsOnlyOnOneSide()
        {
            var editor = Layout(("Game.Door", new[] { "a: System.Int32", "editorOnly: System.Int32" }));
            var player = Layout(("Game.Door", new[] { "a: System.Int32", "playerOnly: System.Single" }));

            var difference = SerializedLayoutCheck.Compare(editor, player, _ => true);

            Assert.AreEqual(1, difference.Count);
            StringAssert.Contains("Game.Door", difference[0]);
            StringAssert.Contains("only in the Editor: editorOnly: System.Int32", difference[0]);
            StringAssert.Contains("only in the player: playerOnly: System.Single", difference[0]);
        }

        [Test]
        public void Compare_ReportsAChangedFieldType()
        {
            var editor = Layout(("Game.Door", new[] { "a: System.Int32" }));
            var player = Layout(("Game.Door", new[] { "a: System.Single" }));

            Assert.AreEqual(1, SerializedLayoutCheck.Compare(editor, player, _ => true).Count);
        }

        [Test]
        public void Compare_SkipsTypesUnityDoesNotSerialize_AndTypesOnOneSide()
        {
            var editor = Layout(("Game.Service", new[] { "editorOnly: System.Int32" }), ("Game.EditorTool", new[] { "a: System.Int32" }));
            var player = Layout(("Game.Service", new string[0]));

            CollectionAssert.IsEmpty(SerializedLayoutCheck.Compare(editor, player, type => type != "Game.Service"));
        }

        [UnityTest]
        public IEnumerator DefineDependentField_IsFound()
        {
            Directory.CreateDirectory(FIXTURE_FOLDER);
            var source = Path.Combine(FIXTURE_FOLDER, "DefineFixture.cs");
            File.WriteAllText(source,
                "namespace LayoutFixtures {\n" +
                "  public class Door : UnityEngine.MonoBehaviour {\n" +
                "    public int width;\n" +
                "#if LAYOUT_FLAG\n" +
                "    [UnityEngine.SerializeField] private int _flagged;\n" +
                "    public int Flagged => _flagged;\n" +
                "#endif\n" +
                "  }\n" +
                "}\n");
            try
            {
                var withFlag = Path.Combine(FIXTURE_FOLDER, "WithFlag", "LayoutFixtures.dll");
                var withoutFlag = Path.Combine(FIXTURE_FOLDER, "WithoutFlag", "LayoutFixtures.dll");
                yield return Compile(source, withFlag, new[] { "LAYOUT_FLAG" });
                yield return Compile(source, withoutFlag, new string[0]);

                var difference = SerializedLayoutCheck.Compare(SerializedLayoutCheck.ReadFields(withFlag),
                    SerializedLayoutCheck.ReadFields(withoutFlag), _ => true);

                Assert.AreEqual(1, difference.Count);
                StringAssert.Contains("LayoutFixtures.Door", difference[0]);
                StringAssert.Contains("only in the Editor: _flagged: System.Int32", difference[0]);
            }
            finally
            {
                Directory.Delete(FIXTURE_FOLDER, recursive: true);
            }
        }

        private static IEnumerator Compile(string source, string output, string[] defines)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var finished = false;
            CompilerMessage[] messages = null;
            // Deprecated in Unity 6.3 but still the Editor's own way to compile one file with chosen defines. If it is removed,
            // this test goes; the Compare and ReadFields tests still cover the check.
#pragma warning disable CS0618
            var builder = new AssemblyBuilder(output, source)
            {
                additionalDefines = defines,
                referencesOptions = ReferencesOptions.UseEngineModules,
            };
#pragma warning restore CS0618
            builder.buildFinished += (_, result) =>
            {
                messages = result;
                finished = true;
            };
            Assert.IsTrue(builder.Build(), "AssemblyBuilder did not start.");

            var deadline = Time.realtimeSinceStartup + CompileTimeoutSeconds;
            while (!finished && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsTrue(finished, "AssemblyBuilder did not finish in time.");
            Assert.IsTrue(File.Exists(output), "Compile failed: " + string.Join("\n", Array.ConvertAll(messages, message => message.message)));
        }

        private static Dictionary<string, SortedSet<string>> Layout(params (string type, string[] fields)[] types)
        {
            var layout = new Dictionary<string, SortedSet<string>>();
            foreach (var (type, fields) in types)
                layout[type] = new SortedSet<string>(fields, StringComparer.Ordinal);
            return layout;
        }
    }
}

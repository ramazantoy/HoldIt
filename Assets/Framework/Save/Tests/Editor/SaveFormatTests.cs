using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static AF.Save.Tests.TestDocuments;

namespace AF.Save.Tests
{
    public class SaveFormatTests
    {
        [Test]
        public void Steps_RunInOrder_FromTheStoredVersion()
        {
            var order = new List<int>();
            var schema = new SaveSchema().Step(1, _ => order.Add(1)).Step(2, _ => order.Add(2));

            var fromOne = SaveFormat.Interpret(new StorageRead(Document(1), null), schema);
            Assert.AreEqual(LoadStatus.Loaded, fromOne.Status);
            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
            Assert.AreEqual(3, fromOne.Document.Version);

            order.Clear();
            SaveFormat.Interpret(new StorageRead(Document(2), null), schema);
            CollectionAssert.AreEqual(new[] { 2 }, order);
        }

        [Test]
        public void Steps_OutOfOrder_Throw()
        {
            Assert.Throws<ArgumentException>(() => new SaveSchema().Step(2, _ => { }));
            Assert.Throws<ArgumentException>(() => new SaveSchema().Step(1, _ => { }).Step(1, _ => { }));
        }

        [Test]
        public void NewerVersion_IsTooNew_AndNothingIsQuarantined()
        {
            var read = SaveFormat.Interpret(new StorageRead(Document(3), Document(1)), new SaveSchema());

            Assert.AreEqual(LoadStatus.TooNew, read.Status);
            Assert.AreEqual(QuarantineMode.None, read.Quarantine);
        }

        [Test]
        public void ThrowingStep_IsFailed_NotCorrupt()
        {
            var schema = new SaveSchema().Step(1, _ => throw new InvalidOperationException("bug in step"));

            var read = SaveFormat.Interpret(new StorageRead(Document(1), null), schema);

            Assert.AreEqual(LoadStatus.Failed, read.Status);
            Assert.AreEqual(QuarantineMode.None, read.Quarantine);
            Assert.IsInstanceOf<SaveDataException>(read.Error);
        }

        [TestCase("{\"sections\":{}}", TestName = "MissingVersion_IsCorrupt")]
        [TestCase("{\"schemaVersion\":1,\"meta\":{}}", TestName = "MissingSections_IsCorrupt")]
        [TestCase("{\"schemaVersion\":1,\"meta\":{},\"sections\":{}}{}", TestName = "TrailingContent_IsCorrupt")]
        [TestCase("{\"schemaVersion\":1,\"meta\":{},\"sec", TestName = "Truncated_IsCorrupt")]
        [TestCase("\0\0\0\0", TestName = "ZeroedFile_IsCorrupt")]
        [TestCase("", TestName = "EmptyFile_IsCorrupt")]
        public void UnreadablePrimary_WithoutBackup_IsCorrupt_AndCopied(string content)
        {
            var read = SaveFormat.Interpret(new StorageRead(content, null), new SaveSchema());

            Assert.AreEqual(LoadStatus.Corrupt, read.Status);
            Assert.AreEqual(QuarantineMode.Copy, read.Quarantine);
            Assert.IsNotNull(read.Error);
        }

        [Test]
        public void UnreadablePrimary_FallsBackToBackup_AndMovesThePrimaryAside()
        {
            var read = SaveFormat.Interpret(new StorageRead("{broken", Document(1, "{\"wallet\":{\"value\":3}}")), new SaveSchema());

            Assert.AreEqual(LoadStatus.RecoveredFromBackup, read.Status);
            Assert.AreEqual(QuarantineMode.Move, read.Quarantine);
            Assert.AreEqual(3, (int)read.Document.Sections["wallet"]["value"]);
        }

        [Test]
        public void MissingPrimary_WithBackup_IsRecovered_WithoutQuarantine()
        {
            // The process died between the two renames of an atomic replace.
            var read = SaveFormat.Interpret(new StorageRead(null, Document(1)), new SaveSchema());

            Assert.AreEqual(LoadStatus.RecoveredFromBackup, read.Status);
            Assert.AreEqual(QuarantineMode.None, read.Quarantine);
        }

        [Test]
        public void BothCopiesUnreadable_IsCorrupt()
        {
            var read = SaveFormat.Interpret(new StorageRead("{broken", "also broken"), new SaveSchema());

            Assert.AreEqual(LoadStatus.Corrupt, read.Status);
            Assert.AreEqual(QuarantineMode.Copy, read.Quarantine);
        }

        [Test]
        public void ReadError_IsReadFailed_NotMissing()
        {
            var read = SaveFormat.Interpret(StorageRead.Failed(new System.IO.IOException("locked")), new SaveSchema());

            Assert.AreEqual(LoadStatus.ReadFailed, read.Status);
            Assert.AreEqual(QuarantineMode.None, read.Quarantine);
        }

        [Test]
        public void NoCopies_IsNotFound()
        {
            Assert.AreEqual(LoadStatus.NotFound, SaveFormat.Interpret(new StorageRead(null, null), new SaveSchema()).Status);
        }

        [Test]
        public void WrittenDocument_ParsesBack()
        {
            var sections = new JObject { ["wallet"] = new JObject { ["value"] = 5 } };
            var content = SaveFormat.Write(2, new JObject { ["appVersion"] = "1.0" }, sections);

            var document = ParsedDocument.From(SaveFormat.Parse(content));

            Assert.AreEqual(2, document.Version);
            Assert.AreEqual("1.0", (string)document.Meta["appVersion"]);
            Assert.IsTrue(JToken.DeepEquals(sections, document.Sections));
        }

        // The pattern every game migration step follows: one function, one test with the document before and after.
        private static void RenameGoldToWallet(JObject document) => SaveMigration.RenameSection(document, "gold", "wallet");

        [Test]
        public void ExampleStep_RenameGoldToWallet()
        {
            var document = JObject.Parse(Document(1, "{\"gold\":{\"value\":7},\"other\":{}}"));

            new SaveSchema().Step(1, RenameGoldToWallet).Migrate(document, 1);

            Assert.IsNull(SaveMigration.Section(document, "gold"));
            Assert.AreEqual(7, (int)SaveMigration.Section(document, "wallet")["value"]);
            Assert.IsNotNull(SaveMigration.Section(document, "other"));
            Assert.AreEqual(2, (int)document["schemaVersion"]);
        }
    }
}

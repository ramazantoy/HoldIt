using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AF.Save.Tests
{
    public class FileSaveStorageTests
    {
        private const string ENTRY = "saves/slot-1";
        private string _root;
        private FileSaveStorage _storage;

        private string PrimaryPath => Path.Combine(_root, "saves", "slot-1.json");

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "SaveTests-" + Guid.NewGuid().ToString("N"));
            _storage = new FileSaveStorage(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void SecondWrite_MovesTheFirstToBackup_AndLeavesNoTempFile()
        {
            _storage.Write(ENTRY, "one");
            Assert.AreEqual("one", _storage.Read(ENTRY).Primary);
            Assert.IsNull(_storage.Read(ENTRY).Backup);

            _storage.Write(ENTRY, "two");
            var read = _storage.Read(ENTRY);

            Assert.AreEqual("two", read.Primary);
            Assert.AreEqual("one", read.Backup);
            Assert.IsFalse(File.Exists(PrimaryPath + ".tmp"));
        }

        [Test]
        public void LeftoverTempFile_IsIgnored()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrimaryPath));
            File.WriteAllText(PrimaryPath + ".tmp", "half written");

            var read = _storage.Read(ENTRY);

            Assert.IsNull(read.Primary);
            Assert.IsNull(read.Backup);
            CollectionAssert.IsEmpty(_storage.List("saves/slot-"));
        }

        [Test]
        public void BackupOnly_IsReadAndListed()
        {
            _storage.Write(ENTRY, "one");
            _storage.Write(ENTRY, "two");
            File.Delete(PrimaryPath);

            Assert.AreEqual("one", _storage.Read(ENTRY).Backup);
            CollectionAssert.AreEqual(new[] { ENTRY }, _storage.List("saves/slot-"));
        }

        [Test]
        public void Quarantine_CopyKeepsThePrimary_MoveRemovesIt()
        {
            _storage.Write(ENTRY, "broken");

            _storage.Quarantine(ENTRY, move: false);
            Assert.AreEqual("broken", File.ReadAllText(PrimaryPath + ".corrupt"));
            Assert.IsTrue(File.Exists(PrimaryPath));

            _storage.Quarantine(ENTRY, move: true);
            Assert.AreEqual("broken", File.ReadAllText(PrimaryPath + ".corrupt"));
            Assert.IsFalse(File.Exists(PrimaryPath));
        }

        [Test]
        public void LockedFile_IsReadFailed_NotMissing()
        {
            _storage.Write(ENTRY, "one");

            using (new FileStream(PrimaryPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var read = _storage.Read(ENTRY);

                Assert.IsNotNull(read.Error);
                Assert.IsNull(read.Primary);
            }
            Assert.AreEqual("one", _storage.Read(ENTRY).Primary, "The file is intact once the lock is gone.");
        }

        [Test]
        public void List_ReturnsEntriesWithThePrefix()
        {
            _storage.Write("saves/slot-b", "b");
            _storage.Write("saves/slot-a", "a");
            _storage.Write("saves/global", "g");
            _storage.Write("local/device", "d");

            CollectionAssert.AreEqual(new[] { "saves/slot-a", "saves/slot-b" }, _storage.List("saves/slot-"));
        }

        [Test]
        public void Delete_RemovesEveryCopy()
        {
            _storage.Write(ENTRY, "one");
            _storage.Write(ENTRY, "two");
            _storage.Quarantine(ENTRY, move: false);

            _storage.Delete(ENTRY);

            Assert.IsEmpty(Directory.GetFiles(Path.GetDirectoryName(PrimaryPath)));
        }

        [Test]
        public void Content_IsUtf8WithoutBom()
        {
            _storage.Write(ENTRY, "ğüşİı");

            var bytes = File.ReadAllBytes(PrimaryPath);

            Assert.AreNotEqual(0xEF, bytes[0]);
            Assert.AreEqual("ğüşİı", _storage.Read(ENTRY).Primary);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace AF.Save.Tests
{
    public class PlayerPrefsSaveStorageTests
    {
        // Its own prefix, so the tests never touch the project's real entries.
        private const string PREFIX = "save-tests/";
        private const string ENTRY = "saves/slot-1";
        private const string OTHER = "saves/global";

        private PlayerPrefsSaveStorage _storage;

        [SetUp]
        public void SetUp() => _storage = new PlayerPrefsSaveStorage(PREFIX);

        [TearDown]
        public void TearDown()
        {
            _storage.Delete(ENTRY);
            _storage.Delete(OTHER);
            PlayerPrefs.DeleteKey(PREFIX + AF.Core.FrameworkConstants.SaveIndexKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void SecondWrite_MovesTheFirstToBackup()
        {
            _storage.Write(ENTRY, "one");
            _storage.Write(ENTRY, "two");

            var read = _storage.Read(ENTRY);
            Assert.AreEqual("two", read.Primary);
            Assert.AreEqual("one", read.Backup);
            Assert.IsNull(read.Error);
        }

        [Test]
        public void List_UsesTheIndex_AndDeleteRemovesFromIt()
        {
            _storage.Write(ENTRY, "one");
            _storage.Write(OTHER, "g");

            CollectionAssert.AreEqual(new[] { ENTRY }, _storage.List("saves/slot-"));

            _storage.Delete(ENTRY);

            CollectionAssert.IsEmpty(_storage.List("saves/slot-"));
            Assert.IsNull(_storage.Read(ENTRY).Primary);
            Assert.IsNull(_storage.Read(ENTRY).Backup);
        }

        [Test]
        public void Quarantine_Move_KeepsACopy_AndTheBackupStillLists()
        {
            _storage.Write(ENTRY, "good");
            _storage.Write(ENTRY, "broken");

            _storage.Quarantine(ENTRY, move: true);

            Assert.AreEqual("broken", PlayerPrefs.GetString(PREFIX + ENTRY + ".corrupt"));
            Assert.IsNull(_storage.Read(ENTRY).Primary);
            Assert.AreEqual("good", _storage.Read(ENTRY).Backup);
            CollectionAssert.AreEqual(new[] { ENTRY }, _storage.List("saves/"));
        }
    }
}

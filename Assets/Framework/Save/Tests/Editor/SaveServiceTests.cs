using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static AF.Save.Tests.TestDocuments;

namespace AF.Save.Tests
{
    public class SaveServiceTests
    {
        private const string SLOT_1 = "saves/slot-1";
        private const string GLOBAL_ENTRY = "saves/global";
        private const string DEVICE_ENTRY = "local/device";

        private MemoryStorage _storage;
        private SaveRegistry _registry;
        private SaveService _service;

        [SetUp]
        public void SetUp() => Create(new SaveOptions());

        private void Create(SaveOptions options)
        {
            _storage = new MemoryStorage();
            options.Storage = _storage;
            _registry = new SaveRegistry();
            _service = new SaveService(_registry, _storage, options);
        }

        [Test]
        public void Slot_RoundTrips()
        {
            var wallet = new Counter("wallet");
            _registry.Register(wallet);

            _service.NewSlot("1");
            wallet.Value = 42;
            Assert.IsTrue(_service.SaveSlot().Result().IsWritten);
            wallet.Value = 0;
            var result = _service.LoadSlot("1").Result();

            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            Assert.AreEqual(42, wallet.Value);
            Assert.AreEqual("1", _service.ActiveSlot);
            var stored = JObject.Parse(_storage.Primary[SLOT_1]);
            Assert.AreEqual(1, (int)stored["schemaVersion"]);
            Assert.AreEqual(42, (int)stored["sections"]["wallet"]["value"]);
        }

        [Test]
        public void NewSlot_GivesEverySavableItsDefault()
        {
            var wallet = new Counter("wallet", defaultValue: 7) { Value = 3 };
            _registry.Register(wallet);

            _service.NewSlot("1");

            Assert.AreEqual(7, wallet.Value);
            Assert.IsEmpty(_storage.Writes, "Nothing is written until a save.");
        }

        [Test]
        public void UnreadablePrimary_LoadsTheBackup_AndKeepsTheBrokenFile()
        {
            var wallet = new Counter("wallet");
            _registry.Register(wallet);
            _service.NewSlot("1");
            wallet.Value = 1;
            _service.SaveSlot().Result();
            wallet.Value = 2;
            _service.SaveSlot().Result();
            _storage.Primary[SLOT_1] = "{broken";
            LogAssert.Expect(LogType.Error, new Regex("loaded from its backup"));

            var result = _service.LoadSlot("1").Result();

            Assert.AreEqual(LoadStatus.RecoveredFromBackup, result.Status);
            Assert.AreEqual(1, wallet.Value);
            Assert.AreEqual("{broken", _storage.Corrupt[SLOT_1]);
            Assert.IsFalse(_storage.Primary.ContainsKey(SLOT_1), "Moved aside, so the next save keeps the good backup.");

            _service.SaveSlot().Result();
            Assert.AreEqual(1, (int)JObject.Parse(_storage.Backup[SLOT_1])["sections"]["wallet"]["value"]);
        }

        [Test]
        public void BothCopiesUnreadable_IsCorrupt_AndNothingIsDeleted()
        {
            _storage.Primary[SLOT_1] = "{broken";
            _storage.Backup[SLOT_1] = "also broken";
            LogAssert.Expect(LogType.Error, new Regex("are unreadable"));

            var result = _service.LoadSlot("1").Result();

            Assert.AreEqual(LoadStatus.Corrupt, result.Status);
            Assert.IsFalse(_service.HasSession);
            Assert.AreEqual("{broken", _storage.Primary[SLOT_1]);
            Assert.AreEqual("also broken", _storage.Backup[SLOT_1]);
            Assert.AreEqual("{broken", _storage.Corrupt[SLOT_1]);
        }

        [Test]
        public void TooNewSlot_IsNotLoaded_AndLeftUntouched()
        {
            _storage.Primary[SLOT_1] = Document(9);
            LogAssert.Expect(LogType.Error, new Regex("newer version"));

            var result = _service.LoadSlot("1").Result();

            Assert.AreEqual(LoadStatus.TooNew, result.Status);
            Assert.IsFalse(_service.HasSession);
            Assert.AreEqual(Document(9), _storage.Primary[SLOT_1]);
            Assert.IsEmpty(_storage.Corrupt);
        }

        [Test]
        public void TooNewGlobal_IsReadOnlyForTheSession()
        {
            var settings = new Counter("settings", SaveDocument.Global, defaultValue: 5);
            _registry.Register(settings);
            _storage.Primary[GLOBAL_ENTRY] = Document(9);
            LogAssert.Expect(LogType.Error, new Regex("newer version"));

            _service.LoadShared(default).GetAwaiter().GetResult();

            Assert.AreEqual(LoadStatus.TooNew, _service.GlobalStatus);
            Assert.AreEqual(5, settings.Value, "Defaults in memory.");
            Assert.AreEqual(SaveStatus.Refused, _service.SaveGlobal().Result().Status);
            Assert.AreEqual(Document(9), _storage.Primary[GLOBAL_ENTRY]);
            Assert.IsFalse(_storage.Writes.Contains(GLOBAL_ENTRY));
        }

        [Test]
        public void LockedGlobal_IsReadFailed_AndNeverOverwritten()
        {
            _registry.Register(new Counter("settings", SaveDocument.Global));
            _storage.Primary[GLOBAL_ENTRY] = Document(1);
            _storage.FailingReads.Add(GLOBAL_ENTRY);
            LogAssert.Expect(LogType.Error, new Regex("could not be read"));

            _service.LoadShared(default).GetAwaiter().GetResult();
            _service.FlushForSuspend();

            Assert.AreEqual(LoadStatus.ReadFailed, _service.GlobalStatus);
            Assert.AreEqual(LoadStatus.NotFound, _service.DeviceStatus);
            Assert.IsFalse(_storage.Writes.Contains(GLOBAL_ENTRY));
        }

        [Test]
        public void UnusableSection_FailsTheLoad_AndRestoresNothing()
        {
            var first = new Counter("first");
            var second = new Counter("second");
            _registry.Register(first);
            _registry.Register(second);
            _service.NewSlot("1");
            _service.SaveSlot().Result();
            var stored = JObject.Parse(_storage.Primary[SLOT_1]);
            stored["sections"]["second"]["value"] = "not a number";
            _storage.Primary[SLOT_1] = stored.ToString();
            first.Value = 10;
            second.Value = 20;
            LogAssert.Expect(LogType.Error, new Regex("could not use it"));

            var result = _service.LoadSlot("1").Result();

            Assert.AreEqual(LoadStatus.Failed, result.Status);
            Assert.IsInstanceOf<SaveDataException>(result.Error);
            Assert.AreEqual(10, first.Value);
            Assert.AreEqual(20, second.Value);
            Assert.IsEmpty(_storage.Corrupt, "Readable data is never quarantined.");
        }

        [Test]
        public void UnknownSections_ArePreserved()
        {
            _registry.Register(new Counter("wallet"));
            _storage.Primary[SLOT_1] = Document(1, "{\"wallet\":{\"value\":1},\"level-2\":{\"doors\":3}}");

            _service.LoadSlot("1").Result();
            _service.SaveSlot().Result();

            Assert.AreEqual(3, (int)JObject.Parse(_storage.Primary[SLOT_1])["sections"]["level-2"]["doors"]);
        }

        [Test]
        public void Migration_RunsOnLoad_AndTheSaveUsesTheNewVersion()
        {
            Create(new SaveOptions { Slot = new SaveSchema().Step(1, document => SaveMigration.RenameSection(document, "gold", "wallet")) });
            var wallet = new Counter("wallet");
            _registry.Register(wallet);
            _storage.Primary[SLOT_1] = Document(1, "{\"gold\":{\"value\":8}}");

            _service.LoadSlot("1").Result();
            _service.SaveSlot().Result();

            Assert.AreEqual(8, wallet.Value);
            var stored = JObject.Parse(_storage.Primary[SLOT_1]);
            Assert.AreEqual(2, (int)stored["schemaVersion"]);
            Assert.IsNull(stored["sections"]["gold"]);
        }

        [Test]
        public void DevSession_IsNeverWritten()
        {
            _service.StartDevSession();

            var result = _service.SaveSlot().Result();

            Assert.IsTrue(_service.HasSession);
            Assert.IsTrue(_service.IsDevSession);
            Assert.AreEqual(SaveStatus.Skipped, result.Status);
            Assert.AreEqual(SaveStatus.Skipped, _service.SaveSlot("1").Result().Status);
            _service.FlushForSuspend();
            Assert.IsEmpty(_storage.Writes);
        }

        [Test]
        public void WithoutSlots_EverySlotOperationThrows()
        {
            Create(new SaveOptions { UsesSlots = false });

            Assert.IsFalse(_service.UsesSlots);
            Assert.Throws<InvalidOperationException>(() => _service.NewSlot("1"));
            Assert.Throws<InvalidOperationException>(() => _service.LoadSlot("1").Result());
            Assert.Throws<InvalidOperationException>(() => _service.ListSlots().Result());
            Assert.Throws<InvalidOperationException>(() => _service.DeleteSlot("1").Result());
            Assert.Throws<InvalidOperationException>(() => _service.SaveSlot());
            Assert.Throws<InvalidOperationException>(() => _service.SaveSlot("1"));
            Assert.IsFalse(_service.HasSession);
            Assert.IsEmpty(_storage.Writes);
        }

        [Test]
        public void WithoutSlots_GlobalDataStillSaves()
        {
            Create(new SaveOptions { UsesSlots = false });

            Assert.DoesNotThrow(() => _service.SaveGlobal().Result());
            Assert.DoesNotThrow(() => _service.FlushForSuspend());
            Assert.DoesNotThrow(() => _service.CloseSlot());
        }

        [Test]
        public void WithSlots_IsTheDefault()
        {
            Assert.IsTrue(new SaveOptions().UsesSlots);
            Assert.IsTrue(_service.UsesSlots);
        }

        [Test]
        public void Suspend_WritesOnlyWhatChanged()
        {
            var settings = new Counter("settings", SaveDocument.Global);
            var wallet = new Counter("wallet");
            _registry.Register(settings);
            _registry.Register(wallet);
            _service.LoadShared(default).GetAwaiter().GetResult();
            _service.NewSlot("1");

            _service.FlushForSuspend();
            Assert.AreEqual(0, _storage.Writes.Count(entry => entry == GLOBAL_ENTRY), "Defaults are not written.");
            Assert.AreEqual(1, _storage.Writes.Count(entry => entry == SLOT_1), "A new slot is written the first time.");

            _service.FlushForSuspend();
            Assert.AreEqual(0, _storage.Writes.Count(entry => entry == GLOBAL_ENTRY));
            Assert.AreEqual(1, _storage.Writes.Count(entry => entry == SLOT_1), "Unchanged.");

            settings.Value = 1;
            wallet.Value = 1;
            _service.FlushForSuspend();
            Assert.AreEqual(1, _storage.Writes.Count(entry => entry == GLOBAL_ENTRY));
            Assert.AreEqual(2, _storage.Writes.Count(entry => entry == SLOT_1));
            Assert.IsFalse(_storage.Writes.Contains(DEVICE_ENTRY), "Device data did not change.");
        }

        [Test]
        public void Suspend_LeavesTheSlotAlone_WhenAutosaveIsOff()
        {
            Create(new SaveOptions { AutosaveSlotOnSuspend = false });
            _service.NewSlot("1");

            _service.FlushForSuspend();

            Assert.IsFalse(_storage.Writes.Contains(SLOT_1));
        }

        [Test]
        public void FailedWrite_ReturnsFailed_AndKeepsTheOldFile()
        {
            _service.NewSlot("1");
            _service.SaveSlot().Result();
            var before = _storage.Primary[SLOT_1];
            _storage.FailingWrites.Add(SLOT_1);
            LogAssert.Expect(LogType.Error, new Regex("writing saves/slot-1 failed"));

            var result = _service.SaveSlot().Result();

            Assert.AreEqual(SaveStatus.Failed, result.Status);
            Assert.IsFalse(result.IsWritten);
            Assert.AreEqual(before, _storage.Primary[SLOT_1]);
        }

        [Test]
        public void SaveAs_MovesTheSessionToTheNewSlot()
        {
            _service.NewSlot("1");
            _service.SaveSlot().Result();

            _service.SaveSlot("2").Result();

            Assert.AreEqual("2", _service.ActiveSlot);
            Assert.IsTrue(_storage.Primary.ContainsKey(SLOT_1));
            Assert.IsTrue(_storage.Primary.ContainsKey("saves/slot-2"));
        }

        [Test]
        public void SaveAs_WithACancelledCaller_StillMovesTheSession()
        {
            _service.NewSlot("1");
            using var cancellation = new System.Threading.CancellationTokenSource();
            cancellation.Cancel();

            try
            {
                _service.SaveSlot("2", cancellation.Token).Result();
            }
            catch (OperationCanceledException)
            {
            }

            Assert.IsTrue(_storage.Primary.ContainsKey("saves/slot-2"));
            Assert.AreEqual("2", _service.ActiveSlot, "The write reached the storage, so the session follows it.");
        }

        [Test]
        public void FailingCapture_WritesNothing()
        {
            _registry.Register(new Counter("wallet"));
            _registry.Register(new ThrowingSavable());
            _service.NewSlot("1");

            Assert.Throws<SaveDataException>(() => _service.SaveSlot().Result());
            Assert.IsEmpty(_storage.Writes);
        }

        [Test]
        public void UnchangedFloats_AreNotRewrittenOnSuspend()
        {
            _registry.Register(new VolumeSavable());
            _storage.Primary[GLOBAL_ENTRY] = Document(1, "{\"volume\":{\"value\":0.1}}");

            _service.LoadShared(default).GetAwaiter().GetResult();
            _service.FlushForSuspend();

            Assert.IsFalse(_storage.Writes.Contains(GLOBAL_ENTRY), "0.1f from the game and 0.1 from the file are the same value.");
        }

        [Test]
        public void HandEditedMeta_DoesNotBreakLoadingOrListing()
        {
            _storage.Primary[SLOT_1] = "{\"schemaVersion\":1,\"meta\":{\"playTimeSeconds\":\"long\",\"savedUtc\":5,\"appVersion\":{}},\"sections\":{}}";

            var result = _service.LoadSlot("1").Result();
            var slot = _service.ListSlots().Result().Single();

            Assert.AreEqual(LoadStatus.Loaded, result.Status);
            Assert.AreEqual(TimeSpan.Zero, _service.PlayTime);
            Assert.IsTrue(slot.CanLoad);
            Assert.IsNull(slot.SavedUtc);
            Assert.IsNull(slot.AppVersion);
        }

        [Test]
        public void ListSlots_ReportsEverySlot_WithItsStatus()
        {
            var summary = new Counter("summary");
            _registry.Register(summary);
            _service.NewSlot("a");
            summary.Value = 3;
            _service.SaveSlot().Result();
            _service.NewSlot("b");
            _service.SaveSlot().Result();
            _storage.Primary["saves/slot-c"] = "{broken";

            var slots = _service.ListSlots().Result();

            CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, slots.Select(slot => slot.Id));
            Assert.AreEqual("c", slots.Last().Id, "Unreadable slots have no save time and come last.");
            Assert.AreEqual(LoadStatus.Corrupt, slots.Last().Status);
            Assert.IsFalse(slots.Last().CanLoad);
            var a = slots.Single(slot => slot.Id == "a");
            Assert.IsTrue(a.CanLoad);
            Assert.IsNotNull(a.SavedUtc);
            Assert.AreEqual(Application.version, a.AppVersion);
            Assert.IsTrue(a.TryReadSection<CounterData>("summary", out var data));
            Assert.AreEqual(3, data.value);
            Assert.IsEmpty(_storage.Corrupt, "Listing never quarantines.");
        }

        [Test]
        public void PlayTime_IsSavedWithTheSlot()
        {
            _service.NewSlot("1");
            _service.AddPlayTime(12.5);
            _service.SaveSlot().Result();
            _service.CloseSlot();

            _service.LoadSlot("1").Result();

            Assert.AreEqual(12.5, _service.PlayTime.TotalSeconds, 0.001);
        }

        [Test]
        public void DeleteSlot_RemovesIt_AndClosesTheSessionIfActive()
        {
            _service.NewSlot("1");
            _service.SaveSlot().Result();

            _service.DeleteSlot("1").Result();

            Assert.IsFalse(_service.HasSession);
            Assert.IsFalse(_storage.Primary.ContainsKey(SLOT_1));
            Assert.IsFalse(_storage.Backup.ContainsKey(SLOT_1));
        }

        [TestCase("Slot1")]
        [TestCase("../global")]
        [TestCase("")]
        [TestCase(null)]
        public void InvalidSlotId_Throws(string slotId)
        {
            Assert.Throws<ArgumentException>(() => _service.NewSlot(slotId));
        }

        [Test]
        public void SaveSlot_WithoutASession_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _service.SaveSlot().Result());
        }

        public sealed class VolumeData
        {
            public float value = 1f;
        }

        private sealed class VolumeSavable : Savable<VolumeData>
        {
            private float _value;
            public override string Key => "volume";
            public override SaveDocument Document => SaveDocument.Global;
            protected override VolumeData Capture() => new VolumeData { value = _value };
            protected override void Restore(VolumeData data) => _value = data.value;
        }

        private sealed class ThrowingSavable : Savable<CounterData>
        {
            public override string Key => "throwing";
            protected override CounterData Capture() => throw new InvalidOperationException("capture bug");

            protected override void Restore(CounterData data)
            {
            }
        }

        [Test]
        public void SharedDocuments_LoadDuringBoot_AndRestoreTheirSavables()
        {
            var settings = new Counter("settings", SaveDocument.Global);
            var quality = new Counter("quality", SaveDocument.Device);
            _registry.Register(settings);
            _registry.Register(quality);
            _storage.Primary[GLOBAL_ENTRY] = Document(1, "{\"settings\":{\"value\":4}}");
            _storage.Primary[DEVICE_ENTRY] = Document(1, "{\"quality\":{\"value\":2}}");

            _service.LoadShared(default).GetAwaiter().GetResult();

            Assert.AreEqual(LoadStatus.Loaded, _service.GlobalStatus);
            Assert.AreEqual(LoadStatus.Loaded, _service.DeviceStatus);
            Assert.AreEqual(4, settings.Value);
            Assert.AreEqual(2, quality.Value);
        }
    }
}

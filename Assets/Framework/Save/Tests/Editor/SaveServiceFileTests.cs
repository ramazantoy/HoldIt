using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Save.Tests
{
    /// <summary>The service on real files, where writes run on the thread pool and results come back on the main thread.</summary>
    public class SaveServiceFileTests
    {
        private string _root;

        [SetUp]
        public void SetUp() => _root = Path.Combine(Application.temporaryCachePath, "SaveTests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [UnityTest]
        public IEnumerator Suspend_FinishesTheWriteInFlight_ThenWritesTheNewerState() => UniTask.ToCoroutine(async () =>
        {
            var registry = new SaveRegistry();
            var service = new SaveService(registry, new FileSaveStorage(_root), new SaveOptions());
            var wallet = new Counter("wallet");
            registry.Register(wallet);
            service.NewSlot("1");

            wallet.Value = 5;
            var inFlight = service.SaveSlot();
            wallet.Value = 6;
            service.FlushForSuspend();

            // Everything is on disk before FlushForSuspend returns; nothing here waited for a frame.
            var onDisk = JObject.Parse(File.ReadAllText(Path.Combine(_root, "saves", "slot-1.json")));
            Assert.AreEqual(6, (int)onDisk["sections"]["wallet"]["value"]);
            Assert.AreEqual(SaveStatus.Written, (await inFlight).Status);

            wallet.Value = 0;
            Assert.AreEqual(LoadStatus.Loaded, (await service.LoadSlot("1")).Status);
            Assert.AreEqual(6, wallet.Value);
        });
    }
}

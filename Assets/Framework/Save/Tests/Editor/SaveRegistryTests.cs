using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace AF.Save.Tests
{
    public class SaveRegistryTests
    {
        [Test]
        public void DuplicateKey_InTheSameDocument_Throws()
        {
            var registry = new SaveRegistry();
            registry.Register(new Counter("wallet"));

            Assert.Throws<InvalidOperationException>(() => registry.Register(new Counter("wallet")));
            Assert.DoesNotThrow(() => registry.Register(new Counter("wallet", SaveDocument.Global)));
        }

        [TestCase("Wallet")]
        [TestCase("")]
        [TestCase("my wallet")]
        [TestCase("-wallet")]
        public void InvalidKey_Throws(string key)
        {
            Assert.Throws<ArgumentException>(() => new SaveRegistry().Register(new Counter(key)));
        }

        [Test]
        public void Register_WhileTheDocumentIsLoaded_RestoresTheSectionOrTheDefault()
        {
            var registry = new SaveRegistry();
            Load(registry, "{\"saved\":{\"value\":5}}");

            var saved = new Counter("saved");
            var missing = new Counter("missing", defaultValue: 7);
            missing.Value = 99;
            registry.Register(saved);
            registry.Register(missing);

            Assert.AreEqual(5, saved.Value);
            Assert.AreEqual(7, missing.Value);
        }

        [Test]
        public void Register_WithoutALoadedDocument_LeavesTheStateAlone()
        {
            var counter = new Counter("wallet") { Value = 3 };

            new SaveRegistry().Register(counter);

            Assert.AreEqual(3, counter.Value);
        }

        [Test]
        public void ThrowingRestore_IsLogged_AndTheSavableStillRegisters()
        {
            var registry = new SaveRegistry();
            Load(registry, "{\"broken\":{\"value\":1}}");
            var broken = new BrokenRestore("broken");

            // Unity logs a wrapped exception by its innermost one; the SaveDataException naming the savable follows in the trace.
            LogAssert.Expect(LogType.Exception, new Regex("restore bug"));
            Assert.DoesNotThrow(() => registry.Register(broken));
            Assert.IsTrue(registry.IsRegistered(broken), "One broken savable must not stop the scene that registers it.");
        }

        [Test]
        public void UnreadableSection_ThrowsOnRegister_AndDoesNotRegister()
        {
            var registry = new SaveRegistry();
            Load(registry, "{\"wallet\":{\"value\":\"not a number\"}}");
            var counter = new Counter("wallet");

            Assert.Throws<SaveDataException>(() => registry.Register(counter));
            Assert.IsFalse(registry.IsRegistered(counter));
        }

        [Test]
        public void DisposingTheHandle_CapturesIntoTheLoadedDocument()
        {
            var registry = new SaveRegistry();
            var document = Load(registry, "{}");
            var counter = new Counter("wallet");
            var handle = registry.Register(counter);

            counter.Value = 12;
            handle.Dispose();

            Assert.IsFalse(registry.IsRegistered(counter));
            Assert.AreEqual(12, (int)document.Sections["wallet"]["value"]);
        }

        [Test]
        public void Capture_KeepsSectionsOfSavablesThatAreGone()
        {
            var registry = new SaveRegistry();
            var document = Load(registry, "{\"other-scene\":{\"value\":4}}");
            var wallet = new Counter("wallet");
            registry.Register(wallet);
            wallet.Value = 1;

            var sections = registry.Capture(document);

            Assert.AreEqual(4, (int)sections["other-scene"]["value"]);
            Assert.AreEqual(1, (int)sections["wallet"]["value"]);
        }

        [Test]
        public void ChildScope_LinksOnlyItsOwnSavables_AndCapturesThemWhenDisposed()
        {
            var builder = new ContainerBuilder();
            builder.Register<SaveRegistry>(Lifetime.Singleton).As<ISaveRegistry>().AsSelf();
            builder.Register<RootCounter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            SaveInstaller.LinkSavables(builder, null);
            using var root = builder.Build();

            var registry = root.Resolve<SaveRegistry>();
            Assert.AreEqual(1, registry.Count, "The root links its own savable when it is built.");
            var document = Load(registry, "{\"scene\":{\"value\":3}}");

            var child = root.CreateScope(scene =>
            {
                SaveInstaller.LinkSavables(scene, null);
                scene.Register<SceneCounter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            });
            var sceneCounter = child.Resolve<SceneCounter>();

            Assert.AreEqual(2, registry.Count, "The child links its savable, not the root's again.");
            Assert.AreEqual(3, sceneCounter.Value, "Restored while the scope was built.");

            sceneCounter.Value = 9;
            child.Dispose();

            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.IsRegistered(root.Resolve<RootCounter>()));
            Assert.AreEqual(9, (int)document.Sections["scene"]["value"]);
        }

        [Test]
        public void ChildScope_WithAnUnreadableSection_FailsToBuild_AndLeavesNothingRegistered()
        {
            var builder = new ContainerBuilder();
            builder.Register<SaveRegistry>(Lifetime.Singleton).As<ISaveRegistry>().AsSelf();
            builder.Register<RootCounter>(Lifetime.Singleton).AsImplementedInterfaces();
            SaveInstaller.LinkSavables(builder, null);
            using var root = builder.Build();
            var registry = root.Resolve<SaveRegistry>();
            Load(registry, "{\"other\":{\"value\":2},\"scene\":{\"value\":\"not a number\"}}");

            // A scene opened while a slot is loaded fails loudly instead of starting with the wrong state.
            var exception = Assert.Catch<Exception>(() => root.CreateScope(scene =>
            {
                SaveInstaller.LinkSavables(scene, null);
                scene.Register<OtherCounter>(Lifetime.Singleton).AsImplementedInterfaces();
                scene.Register<SceneCounter>(Lifetime.Singleton).AsImplementedInterfaces();
            }));

            while (exception != null && !(exception is SaveDataException))
                exception = exception.InnerException;
            Assert.IsInstanceOf<SaveDataException>(exception);
            Assert.AreEqual(1, registry.Count, "The savable linked before the failure is unlinked again.");
        }

        [Test]
        public void Audit_ReportsSavablesRegisteredWithoutTheContract_OrNotAsSingletons()
        {
            var builder = new ContainerBuilder();
            builder.Register<RootCounter>(Lifetime.Singleton).AsSelf();
            builder.Register<SceneCounter>(Lifetime.Transient).AsImplementedInterfaces();
            builder.Register<OtherCounter>(Lifetime.Singleton).AsImplementedInterfaces();

            var problems = SavableAudit.FindProblems(builder);

            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
            StringAssert.Contains(nameof(RootCounter), problems[0]);
            StringAssert.Contains("not registered as ISavable", problems[0]);
            StringAssert.Contains(nameof(SceneCounter), problems[1]);
            StringAssert.Contains("Transient", problems[1]);
        }

        private static DocumentState Load(SaveRegistry registry, string sections)
        {
            var document = new DocumentState(SaveDocument.Slot, "saves/slot-test", writable: true, JObject.Parse(sections));
            registry.Activate(document, registry.Prepare(document));
            return document;
        }

        public sealed class BrokenRestore : Counter
        {
            public BrokenRestore(string key) : base(key)
            {
            }

            protected override void Restore(CounterData data) => throw new InvalidOperationException("restore bug");
        }

        public sealed class RootCounter : Counter
        {
            public RootCounter() : base("root")
            {
            }
        }

        public sealed class SceneCounter : Counter
        {
            public SceneCounter() : base("scene")
            {
            }
        }

        public sealed class OtherCounter : Counter
        {
            public OtherCounter() : base("other")
            {
            }
        }
    }
}

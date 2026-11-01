using NUnit.Framework;
using VContainer;
using VContainer.Unity;

namespace AF.Core.Tests
{
    public class EntryPointAuditTests
    {
        [Test]
        public void TickableRegisteredUnderAnotherInterface_IsReported()
        {
            var builder = new ContainerBuilder();
            builder.Register<TickingThing>(Lifetime.Singleton).As<IThing>();

            var problems = EntryPointAudit.FindMisregistered(builder);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(nameof(TickingThing), problems[0]);
            StringAssert.Contains(nameof(ITickable), problems[0]);
        }

        [Test]
        public void TickableRegisteredAsSelf_IsReported()
        {
            var builder = new ContainerBuilder();
            builder.Register<TickingThing>(Lifetime.Singleton);

            Assert.AreEqual(1, EntryPointAudit.FindMisregistered(builder).Count);
        }

        [Test]
        public void RegisterEntryPoint_IsNotReported()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEntryPoint<TickingThing>();

            CollectionAssert.IsEmpty(EntryPointAudit.FindMisregistered(builder));
        }

        [Test]
        public void ScopedEntryPoint_IsReported()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEntryPoint<TickingThing>(Lifetime.Scoped);

            var problems = EntryPointAudit.FindMisregistered(builder);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("Scoped ITickable", problems[0]);
        }

        private interface IThing
        {
        }

        private sealed class TickingThing : IThing, ITickable
        {
            public void Tick()
            {
            }
        }
    }
}

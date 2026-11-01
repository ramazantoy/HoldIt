using System;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Platform.Tests
{
    public class PlatformInstallerTests
    {
        [Test]
        public void Mock_RegistersEveryContract()
        {
            var builder = CreateBuilder(out var messagePipe);
            builder.UsePlatform(messagePipe);

            using var container = builder.Build();
            Assert.IsInstanceOf<MockAchievements>(container.Resolve<IAchievements>());
            Assert.IsInstanceOf<MockPlatformOverlay>(container.Resolve<IPlatformOverlay>());
            Assert.AreSame(container.Resolve<IPlatformOverlay>(), container.Resolve<MockPlatformOverlay>());
        }

        [Test]
        public void BackendMissingAContract_FailsWithItsName()
        {
            var builder = CreateBuilder(out var messagePipe);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                builder.UsePlatform(messagePipe, backend: new AchievementsOnlyBackend()));
            StringAssert.Contains("Partial", exception.Message);
            StringAssert.Contains(nameof(IPlatformOverlay), exception.Message);
            StringAssert.DoesNotContain(nameof(IAchievements), exception.Message);
        }

        [Test]
        public void OverlayPause_IsRegisteredOnlyWhenWanted()
        {
            var builder = CreateBuilder(out var messagePipe);
            builder.UsePlatform(messagePipe);
            Assert.IsTrue(builder.Exists(typeof(OverlayPause)));

            builder = CreateBuilder(out messagePipe);
            builder.UsePlatform(messagePipe, new PlatformOptions { PauseOnOverlay = false });
            Assert.IsFalse(builder.Exists(typeof(OverlayPause)));
        }

        private static ContainerBuilder CreateBuilder(out MessagePipeOptions messagePipe)
        {
            var builder = new ContainerBuilder();
            messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            return builder;
        }

        private sealed class AchievementsOnlyBackend : IPlatformBackend
        {
            public string Name => "Partial";

            public void Install(IContainerBuilder builder, PlatformOptions options) =>
                builder.Register<MockAchievements>(Lifetime.Singleton).As<IAchievements>();
        }
    }
}

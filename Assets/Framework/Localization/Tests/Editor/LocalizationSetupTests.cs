using System;
using System.Linq;
using System.Threading;
using AF.Core;
using AF.Save;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using VContainer;

namespace AF.Localization.Tests
{
    public class LocalizationSetupTests
    {
        [Test]
        public void UseLocalization_NeedsUseSaveFirst()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();

            var exception = Assert.Throws<InvalidOperationException>(() => builder.UseLocalization(messagePipe));
            StringAssert.Contains("UseSave", exception.Message);
        }

        [Test]
        public void UseLocalization_AddsItsBootStepAfterTheOnesRegisteredBefore_AndTheSavable()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.Register<ISaveRegistry>(_ => throw new NotSupportedException(), Lifetime.Singleton);
            builder.Register<EarlierStep>(Lifetime.Singleton).As<IBootStep>();

            builder.UseLocalization(messagePipe);

            using var container = builder.Build();
            CollectionAssert.AreEqual(new[] { typeof(EarlierStep), typeof(LocalizationBootStep) },
                container.Resolve<System.Collections.Generic.IReadOnlyList<IBootStep>>().Select(step => step.GetType()),
                "Boot steps run in registration order: after UseSave, the saved language is restored first.");
            var savable = container.Resolve<System.Collections.Generic.IReadOnlyList<ISavable>>().Single();
            Assert.AreEqual("localization.locale", savable.Key);
            Assert.AreEqual(SaveDocument.Global, savable.Document);
            Assert.AreSame(container.Resolve<ILocaleService>(), savable);
        }

        private sealed class EarlierStep : IBootStep
        {
            public UniTask Run(CancellationToken cancellation) => UniTask.CompletedTask;
        }
    }
}

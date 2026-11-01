using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AF.Save;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Localization.Tests
{
    public class LocaleServiceTests
    {
        private IObjectResolver _container;
        private FakeLocalization _backend;
        private Locale _en;
        private Locale _tr;
        private Locale _de;
        private List<LocaleChanged> _changes;
        private IDisposable _subscription;
        private LocaleService _service;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<LocaleChanged>(messagePipe);
            _container = builder.Build();
            _en = Locale.CreateLocale("en");
            _tr = Locale.CreateLocale("tr");
            _de = Locale.CreateLocale("de");
            _backend = new FakeLocalization { Selected = _en };
            _backend.LocaleList.Add(_en);
            _backend.LocaleList.Add(_tr);
            _backend.LocaleList.Add(_de);
            _changes = new List<LocaleChanged>();
            _subscription = _container.Resolve<ISubscriber<LocaleChanged>>().Subscribe(_changes.Add);
            _service = new LocaleService(_backend, _container.Resolve<IPublisher<LocaleChanged>>());
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            _subscription.Dispose();
            _container.Dispose();
            Object.DestroyImmediate(_en);
            Object.DestroyImmediate(_tr);
            Object.DestroyImmediate(_de);
        }

        [Test]
        public void NothingSaved_KeepsTheStartupLocale_AsNoChange()
        {
            CollectionAssert.IsEmpty(_service.Available, "Empty before boot.");
            Restore(null);

            Initialize();

            Assert.AreSame(_en, _service.Current);
            CollectionAssert.AreEqual(new[] { _en, _tr, _de }, _service.Available);
            CollectionAssert.IsEmpty(_changes);
            Assert.IsNull(Capture().code);
        }

        [Test]
        public void SavedLocale_IsSelectedDuringBoot()
        {
            Restore("tr");

            Initialize();

            Assert.AreSame(_tr, _service.Current);
            Assert.AreEqual(1, _changes.Count, "The backend's own event after the switch publishes nothing more.");
            Assert.AreSame(_en, _changes[0].Previous);
            Assert.AreSame(_tr, _changes[0].Current);
            Assert.AreEqual("tr", Capture().code);
        }

        [Test]
        public void SavedLocaleWhoseTablesFailToLoad_GivesWayToTheStartupLocale_WithoutFailingTheBoot()
        {
            Restore("tr");
            _backend.Failing = _tr;
            LogAssert.Expect(LogType.Error, new Regex("the saved language 'tr' could not be loaded"));

            Initialize();

            Assert.AreSame(_en, _service.Current);
            Assert.AreSame(_en, _backend.Selected);
            CollectionAssert.AreEqual(new[] { "tr", "en" }, _backend.Selections);
            CollectionAssert.IsEmpty(_changes);
            Assert.AreEqual("tr", Capture().code, "The choice is synced; it may load on another device.");
        }

        [Test]
        public void SavedLocaleTheProjectNoLongerHas_LeavesTheStartupLocale_AndKeepsTheChoice()
        {
            Restore("fr");
            LogAssert.Expect(LogType.Warning, new Regex("the saved language 'fr' is not in the project"));

            Initialize();

            Assert.AreSame(_en, _service.Current);
            CollectionAssert.IsEmpty(_changes);
            Assert.AreEqual("fr", Capture().code);
        }

        [Test]
        public void Set_CompletesAndPublishes_OnceTheTablesAreLoaded()
        {
            Initialize();
            var hold = _backend.HoldNext();

            var setting = _service.Set(_tr);
            Assert.AreEqual(UniTaskStatus.Pending, setting.Status);
            Assert.AreSame(_en, _service.Current, "Current changes when the new tables are ready.");
            CollectionAssert.IsEmpty(_changes);

            hold.TrySetResult();
            Assert.AreEqual(UniTaskStatus.Succeeded, setting.Status);
            Assert.AreSame(_tr, _service.Current);
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual("tr", Capture().code);
        }

        [Test]
        public void OverlappingSets_SwitchOneAtATime_AndTheNewestWins()
        {
            Initialize();
            var hold = _backend.HoldNext();

            var first = _service.Set(_tr);
            var second = _service.Set(_de);
            CollectionAssert.AreEqual(new[] { "tr" }, _backend.Selections, "No new selection while the package loads the previous one.");

            hold.TrySetResult();

            CollectionAssert.AreEqual(new[] { "tr", "de" }, _backend.Selections);
            Assert.AreEqual(UniTaskStatus.Succeeded, first.Status);
            Assert.AreEqual(UniTaskStatus.Succeeded, second.Status);
            Assert.AreSame(_de, _service.Current);
            Assert.AreEqual(1, _changes.Count, "The overtaken switch publishes nothing.");
            Assert.AreSame(_de, _changes[0].Current);
            Assert.AreEqual("de", Capture().code);
        }

        [Test]
        public void ALateEventOfAnEarlierSwitch_IsIgnored()
        {
            Initialize();

            _backend.RaiseLate(_tr);

            Assert.AreSame(_en, _service.Current);
            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void CancellingSet_StopsTheWait_NotTheSwitch()
        {
            Initialize();
            var hold = _backend.HoldNext();
            using var cancellation = new CancellationTokenSource();

            var setting = _service.Set(_tr, cancellation.Token);
            cancellation.Cancel();
            Assert.AreEqual(UniTaskStatus.Canceled, setting.Status);

            hold.TrySetResult();
            Assert.AreSame(_tr, _service.Current);
            Assert.AreEqual(1, _changes.Count);
        }

        [Test]
        public void RestoreAfterBoot_ReplacesTheChoice()
        {
            Restore("tr");
            Initialize();

            Restore(null);

            Assert.AreSame(_en, _service.Current, "No choice: the startup locale.");
            Assert.AreSame(_en, _changes.Last().Current);
            Assert.IsNull(Capture().code);
        }

        [Test]
        public void Set_RejectsWhatItCannotDo()
        {
            Assert.Throws<InvalidOperationException>(() => _service.Set(_tr).GetAwaiter().GetResult(), "Before boot.");

            Initialize();
            var other = Locale.CreateLocale("fr");
            try
            {
                Assert.Throws<ArgumentException>(() => _service.Set(other).GetAwaiter().GetResult());
                Assert.Throws<ArgumentNullException>(() => _service.Set(null).GetAwaiter().GetResult());
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void Set_TheCurrentLocale_RecordsTheChoice_WithoutAChange()
        {
            Initialize();

            _service.Set(_en).GetAwaiter().GetResult();

            CollectionAssert.IsEmpty(_changes);
            Assert.AreEqual("en", Capture().code, "Picking the system language explicitly keeps it on another device.");
        }

        [Test]
        public void AChangeMadeElsewhere_IsFollowed_ButNotSaved()
        {
            Initialize();

            _backend.ChangeElsewhere(_tr);

            Assert.AreSame(_tr, _service.Current);
            Assert.AreEqual(1, _changes.Count);
            Assert.IsNull(Capture().code);
        }

        [Test]
        public void FailedInitialization_FailsTheBootStep()
        {
            _backend.InitializeError = new InvalidOperationException("Unity Localization failed to initialize");

            Assert.Throws<InvalidOperationException>(Initialize);
            CollectionAssert.IsEmpty(_service.Available);
        }

        [Test]
        public void NoStartupLocale_FailsTheBootStep()
        {
            _backend.Selected = null;

            var exception = Assert.Throws<InvalidOperationException>(Initialize);
            StringAssert.Contains("selected no locale", exception.Message);
        }

        [Test]
        public void Dispose_LeavesTheBackend()
        {
            _service.Dispose();

            Assert.AreEqual(0, _backend.Subscribers, "The package's event is static.");
            Assert.IsTrue(_backend.Disposed);
        }

        private void Initialize() => _service.Initialize(CancellationToken.None).GetAwaiter().GetResult();
        private void Restore(string code) => ((ISavable)_service).RestoreData(new LocaleData { code = code });
        private LocaleData Capture() => (LocaleData)((ISavable)_service).CaptureData();

        /// <summary>Behaves like Unity Localization where it matters: selecting the selected locale does nothing, the event
        /// comes once the tables are loaded, a load can fail, and an earlier switch's event can arrive late.</summary>
        private sealed class FakeLocalization : ILocalizationBackend
        {
            public readonly List<Locale> LocaleList = new List<Locale>();
            public readonly List<string> Selections = new List<string>();
            private UniTaskCompletionSource _hold;

            public event Action<Locale> SelectedChanged;

            public IReadOnlyList<Locale> Locales => LocaleList;
            public Locale Selected { get; set; }
            public Exception InitializeError { get; set; }

            /// <summary>Selecting this locale fails, like a table that cannot be loaded.</summary>
            public Locale Failing { get; set; }

            public bool Disposed { get; private set; }
            public int Subscribers => SelectedChanged?.GetInvocationList().Length ?? 0;

            public UniTask Initialize(CancellationToken cancellation) =>
                InitializeError != null ? UniTask.FromException(InitializeError) : UniTask.CompletedTask;

            /// <summary>The next selection waits until the returned source completes.</summary>
            public UniTaskCompletionSource HoldNext() => _hold = new UniTaskCompletionSource();

            public async UniTask Select(Locale locale, CancellationToken cancellation)
            {
                if (ReferenceEquals(Selected, locale))
                    return;
                Selections.Add(locale.Identifier.Code);
                Selected = locale;
                if (ReferenceEquals(locale, Failing))
                    throw new InvalidOperationException("Unity Localization failed to initialize: could not preload string tables.");
                var hold = _hold;
                _hold = null;
                if (hold != null)
                    await hold.Task.AttachExternalCancellation(cancellation);
                SelectedChanged?.Invoke(locale);
            }

            public void ChangeElsewhere(Locale locale)
            {
                Selected = locale;
                SelectedChanged?.Invoke(locale);
            }

            public void RaiseLate(Locale locale) => SelectedChanged?.Invoke(locale);

            public void Dispose() => Disposed = true;
        }
    }
}

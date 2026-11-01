using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AF.Core;
using AF.Save;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine.Localization;
using VContainer;

namespace AF.Localization
{
    /// <summary>
    /// The player's language, a framework-owned section of the global save data. Fields are only ever added; a breaking
    /// change uses a new key.
    /// </summary>
    public sealed class LocaleData
    {
        /// <summary>The locale code the player picked ("tr", "pt-BR"); null while the system language applies.</summary>
        public string code;
    }

    internal sealed class LocaleService : Savable<LocaleData>, ILocaleService, IDisposable
    {
        private readonly ILocalizationBackend _backend;
        private readonly IPublisher<LocaleChanged> _changed;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private string _chosenCode;
        private Locale _startup;
        private Locale _target;
        private UniTaskCompletionSource _switching;
        private bool _ready;

        [Inject]
        public LocaleService(IPublisher<LocaleChanged> changed) : this(new UnityLocalizationBackend(), changed)
        {
        }

        internal LocaleService(ILocalizationBackend backend, IPublisher<LocaleChanged> changed)
        {
            _backend = backend;
            _changed = changed;
            _backend.SelectedChanged += OnSelectedChanged;
        }

        public override string Key => "localization.locale";
        public override SaveDocument Document => SaveDocument.Global;

        public Locale Current { get; private set; }
        public IReadOnlyList<Locale> Available => _ready ? _backend.Locales : Array.Empty<Locale>();

        public async UniTask Set(Locale locale, CancellationToken cancellation = default)
        {
            if (locale == null)
                throw new ArgumentNullException(nameof(locale));
            if (!_ready)
                throw new InvalidOperationException("The language cannot change before the boot steps are done; await IAppBoot.WaitReady first.");
            if (!_backend.Locales.Contains(locale))
                throw new ArgumentException($"{locale} is not one of the project's locales; pick one of Available.", nameof(locale));

            _chosenCode = locale.Identifier.Code;
            await SwitchTo(locale).AttachExternalCancellation(cancellation);
        }

        public void Dispose()
        {
            if (_lifetime.IsCancellationRequested)
                return;
            _backend.SelectedChanged -= OnSelectedChanged;
            _backend.Dispose();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        /// <summary>
        /// The boot step: waits for the startup locale, then switches to the saved one if it differs. A saved language whose
        /// tables fail to load gives way to the startup one: the choice is synced, so failing here would fail every launch.
        /// </summary>
        internal async UniTask Initialize(CancellationToken cancellation)
        {
            await _backend.Initialize(cancellation);
            _startup = _backend.Selected ?? throw new InvalidOperationException(
                "Unity Localization selected no locale: the project has none, or its startup selectors match none of them " +
                $"(see the error above). {FrameworkConstants.Prefix} > Validate All checks the setup.");
            Current = _target = _startup;

            var source = "system";
            var saved = SavedLocale();
            if (saved != null)
            {
                try
                {
                    await SwitchTo(saved).AttachExternalCancellation(cancellation);
                    source = "saved";
                }
                catch (Exception exception) when (!(exception is OperationCanceledException))
                {
                    Log.Error($"Localization: the saved language '{_chosenCode}' could not be loaded ({exception.Message}); " +
                              $"{_startup.Identifier.Code} applies. The choice is kept.");
                    await SwitchTo(_startup).AttachExternalCancellation(cancellation);
                }
            }
            _ready = true;
            Log.Info($"Localization: {Current.Identifier.Code} ({source}), locales {string.Join(", ", _backend.Locales.Select(locale => locale.Identifier.Code))}");
        }

        protected override LocaleData Capture() => new LocaleData { code = _chosenCode };

        protected override void Restore(LocaleData data)
        {
            _chosenCode = string.IsNullOrEmpty(data.code) ? null : data.code;
            // Global data loads before the boot step, which applies it; this covers a later restore.
            if (_ready)
                SwitchTo(SavedLocale() ?? _startup).Forget();
        }

        /// <summary>The locale the player picked, when the project still has it; the choice is kept either way.</summary>
        private Locale SavedLocale()
        {
            if (_chosenCode == null)
                return null;
            var locale = _backend.Locales.FirstOrDefault(candidate => candidate.Identifier.Code == _chosenCode);
            if (locale == null)
            {
                Log.Warning($"Localization: the saved language '{_chosenCode}' is not in the project; {Current?.Identifier.Code} applies. " +
                            "The choice is kept until the player picks another one.");
            }
            return locale;
        }

        /// <summary>Switches to <paramref name="locale"/> after the switch in progress, if any; the newest target wins.</summary>
        private UniTask SwitchTo(Locale locale)
        {
            _target = locale;
            var switching = _switching;
            if (switching == null)
            {
                // A switch that completes at once clears the field before this returns.
                switching = _switching = new UniTaskCompletionSource();
                RunSwitches(switching).Forget();
            }
            return switching.Task;
        }

        // One switch at a time. Selecting a locale while the package still loads the previous one's tables lets that load fail
        // and its "selected locale changed" event arrive after the newer one.
        private async UniTaskVoid RunSwitches(UniTaskCompletionSource done)
        {
            try
            {
                while (true)
                {
                    var next = _target;
                    if (!ReferenceEquals(_backend.Selected, next))
                        await _backend.Select(next, _lifetime.Token);
                    if (!ReferenceEquals(_target, next))
                        continue;
                    _switching = null;
                    Commit(next);
                    done.TrySetResult();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                _switching = null;
                done.TrySetCanceled();
            }
            catch (Exception exception)
            {
                _switching = null;
                done.TrySetException(exception);
            }
        }

        private void OnSelectedChanged(Locale locale)
        {
            // A change made elsewhere, such as the Editor's Localization Scene Controls. While a switch runs, its events (and
            // late ones of a previous switch) are not followed: the switch commits its own result.
            if (!_ready || _switching != null || locale == null || !ReferenceEquals(locale, _backend.Selected))
                return;
            _target = locale;
            Commit(locale);
        }

        private void Commit(Locale locale)
        {
            if (ReferenceEquals(Current, locale))
                return;
            var previous = Current;
            Current = locale;
            _changed.Publish(new LocaleChanged(previous, locale));
        }
    }
}

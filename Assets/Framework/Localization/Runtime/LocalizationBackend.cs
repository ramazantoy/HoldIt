using System;
using System.Collections.Generic;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AF.Localization
{
    /// <summary>What <see cref="LocaleService"/> reads from and writes to Unity Localization; tests use their own.</summary>
    internal interface ILocalizationBackend : IDisposable
    {
        /// <summary>Raised after the selected locale changed and its preloaded tables are loaded, whoever changed it.</summary>
        event Action<Locale> SelectedChanged;

        IReadOnlyList<Locale> Locales { get; }

        /// <summary>The selected locale; null when the startup selectors found none.</summary>
        Locale Selected { get; }

        /// <summary>Loads the locales, picks the startup locale and loads its preloaded tables. Throws when that fails.</summary>
        UniTask Initialize(CancellationToken cancellation);

        /// <summary>Selects <paramref name="locale"/> and waits until its preloaded tables are loaded.</summary>
        UniTask Select(Locale locale, CancellationToken cancellation);
    }

    /// <summary>The only code that sets Unity Localization's selected locale.</summary>
    internal sealed class UnityLocalizationBackend : ILocalizationBackend
    {
        public UnityLocalizationBackend() => LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

        public event Action<Locale> SelectedChanged;

        public IReadOnlyList<Locale> Locales => LocalizationSettings.AvailableLocales.Locales;
        public Locale Selected => LocalizationSettings.SelectedLocale;

        public UniTask Initialize(CancellationToken cancellation) => WaitForTables(cancellation);

        public UniTask Select(Locale locale, CancellationToken cancellation)
        {
            // Releases the finished initialization and starts a new one that preloads the new locale's tables.
            LocalizationSettings.SelectedLocale = locale;
            return WaitForTables(cancellation);
        }

        // The event is static: a subscription left behind would outlive the root scope.
        public void Dispose() => LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;

        private static async UniTask WaitForTables(CancellationToken cancellation)
        {
            var operation = LocalizationSettings.InitializationOperation;
            try
            {
                await operation.WithCancellation(cancellation);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                throw Failed(exception.Message);
            }
            if (operation.Status != AsyncOperationStatus.Succeeded)
                throw Failed(operation.OperationException?.Message);
        }

        private static Exception Failed(string reason) =>
            new InvalidOperationException($"Unity Localization failed to initialize: {reason}. In a build, the Addressables content may be " +
                                          $"missing; build through {FrameworkConstants.Prefix} > Build, which builds it.");

        private void OnSelectedLocaleChanged(Locale locale) => SelectedChanged?.Invoke(locale);
    }
}

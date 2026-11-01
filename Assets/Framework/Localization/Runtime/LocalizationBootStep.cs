using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;

namespace AF.Localization
{
    /// <summary>
    /// Waits for Unity Localization during boot and applies the saved language, so the selected language's preloaded tables
    /// are loaded before any scene leaves the Boot state. Runs after the save system's step, which restores the saved language.
    /// </summary>
    internal sealed class LocalizationBootStep : IBootStep
    {
        private readonly LocaleService _locales;

        public LocalizationBootStep(LocaleService locales) => _locales = locales;

        public UniTask Run(CancellationToken cancellation) => _locales.Initialize(cancellation);
    }
}

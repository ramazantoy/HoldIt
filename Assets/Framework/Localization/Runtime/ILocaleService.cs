using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace AF.Localization
{
    /// <summary>
    /// The language the game shows, saved with the global data so it follows the player to other devices; the system
    /// language until the player picks one. The only writer of <c>LocalizationSettings.SelectedLocale</c>. Ready once the
    /// boot steps are done (<c>IAppBoot.WaitReady</c>): from then on the selected language's preloaded tables are loaded.
    /// </summary>
    public interface ILocaleService
    {
        /// <summary>The language in use; null until the boot steps are done.</summary>
        Locale Current { get; }

        /// <summary>The project's languages; empty until the boot steps are done. Show each with its own name
        /// (<c>locale.Identifier.CultureInfo.NativeName</c>), never by position in a list of your own.</summary>
        IReadOnlyList<Locale> Available { get; }

        /// <summary>
        /// Switches to <paramref name="locale"/>, one of <see cref="Available"/>, and remembers it as the player's choice.
        /// Completes when its preloaded tables are loaded and <see cref="LocaleChanged"/> is published. One switch runs at a
        /// time: a call made meanwhile waits for it, the newest call wins, and every call completes with the newest. Cancelling
        /// stops the wait, not the switch. Throws before the boot steps are done.
        /// </summary>
        UniTask Set(Locale locale, CancellationToken cancellation = default);
    }
}

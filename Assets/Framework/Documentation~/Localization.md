# Localization

The game's language through Unity Localization: chosen from the system language on the first launch, saved with the
global data once the player picks one, and ready before the game leaves its boot. Built on the Unity Localization
package, which loads its tables through Addressables; needs the Save module.

## Setup

```csharp
protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
{
    builder.UseSave();
    builder.UseLocalization(messagePipe); // after UseSave; throws otherwise
}
```

| Piece | Where |
|---|---|
| Localization Settings | `Assets/Game/Localization/LocalizationSettings.asset` (Project Settings > Localization) |
| Locales | `Assets/Game/Localization/Locales/`: English (`en`, the project locale) and Turkish (`tr`, falls back to English) |
| String tables | `Assets/Game/Localization/Tables/`: the `UI` collection, with one example entry (`menu.play`) |
| Addressables settings | `Assets/AddressableAssetsData/`; Unity Localization keeps one group per table language there |

The template's locales and table are examples: add the game's languages in Project Settings > Localization and its
tables in Window > Asset Management > Localization Tables.

## Choosing the language

The Localization Settings' **Startup Locale Selectors** pick the language on launch, in order:

1. **Command Line**: `-language=tr`, for testing a first launch; a language the player picked still wins at boot.
2. **System**: the device's language, when the project has it.
3. **Specific**: English, so a system language the game does not have still finds one.

Once the player picks a language, `ILocaleService` saves its code in the global data, which follows the player to their
other devices; from then on the boot step switches to it. The Player Pref Locale Selector is not used: it keeps a second
copy of the language in PlayerPrefs, on one device only, that wins over the saved one. Validation reports it.

```csharp
foreach (var locale in locales.Available)
    AddRow(locale.Identifier.CultureInfo.NativeName, () => locales.Set(locale).Forget());
```

Build the language list from `Available`, with each language's own name, never from a list of your own matched by
position: a new locale shifts every position.

`Set` saves the choice and completes once the new language's preloaded tables are loaded; `LocaleChanged {Previous,
Current}` is published then. One switch runs at a time, because Unity Localization cannot start one while it still loads
the previous: a call made meanwhile waits, the newest call wins, and every waiting call completes with it (pressing a
"next language" arrow quickly publishes one message, for the last language). Text bound through `LocalizeStringEvent` or `LocalizedString.StringChanged` updates itself;
the message is for code that builds strings by hand. Like the other settings, the choice reaches the disk when the
settings screen calls `ISaveService.SaveGlobal()` and when the app is suspended or quits (see Settings.md).

A saved language the project no longer has gives way to the startup selection, with a warning; one whose tables fail to
load does too, with an error, instead of stopping the boot (the choice is synced, so it would stop every launch on every
device). Either way the choice stays saved until the player picks another. A change made outside the service, such as the Editor's Localization Scene Controls
window during Play, is followed (`Current` and the message) but not saved.

## Ready at boot

The boot step runs after the save system's step, which restores the saved language. It waits for Unity Localization,
switches to the saved language if it differs from the startup one, and finishes once that language's preloaded tables
are loaded. After `IAppBoot.WaitReady`, `LocalizationSettings.StringDatabase.GetLocalizedString(table, key)` returns at
once for tables marked **Preload** (the `UI` table is); others load asynchronously. The fallback language's tables are
preloaded too (**Preload Behavior**: Preload Selected Locale And Fallbacks), so an untranslated entry shows the fallback
text at once. Mark the tables every screen needs as Preload, and leave large ones (dialogue) to load on demand.

Text in a scene shown during boot (an intro) may change once the saved language applies.

If Unity Localization fails to initialize, or finds no locale, the boot step fails and the game stays in its boot with
the error in the log. In a build this means the Addressables content is missing; the build menu builds it and refuses a
build without it (see Build.md).

## Rules

- `ILocaleService` is the only writer of `LocalizationSettings.SelectedLocale`. Validation reports project code that sets
  it; mark a deliberate exception, such as a debug menu, `[LocaleChangeExempt("reason")]`.
- Refer to strings with `LocalizedString` fields (or a `LocalizeStringEvent`) rather than table and key strings in code:
  validation checks the fields, it cannot check strings.
- Never cache a string read while its table is loading: the package returns its "No translation found" text then.
- Every locale table of a collection should have a value for every key. Validation lists the gaps as warnings (a key is
  usually added before it is translated); a missing value shows the fallback locale's text, or the package's "No
  translation found" message.

## Validation

`LocalizationValidator` reports:

- no active Localization Settings, no locales, no Addressables settings;
- a Player Pref Locale Selector, and a selector list that does not end with a Specific Locale Selector naming one of the
  project's locales;
- in scenes, the root prefab and (Validate All and the build check) every prefab and ScriptableObject: a localized string
  or asset field that names a table or entry that does not exist, or only one of the two;
- (Validate All and the build check) string table entries without a value in some locale, as warnings.

`DirectLocaleChangeValidator` reports project code that sets the selected locale.

## Builds

Unity Localization's tables are Addressables content. The build menu builds it for the active platform before building
the player, and fails the build when the content is not in the output. Addressables' own **Build Addressables on Player
Build** stays at "Do not build"; the build check refuses any other value. See Build.md.

The Localization Settings asset is listed in Player Settings > Preloaded Assets. The package adds it there for every
build and removes it afterwards in memory only, so the build would leave ProjectSettings.asset changed each time; listed,
the package leaves it alone.

## Removing the module

A game with one language can go without it: remove `builder.UseLocalization(...)`, the `Assets/Framework/Localization`
folder, `Assets/Game/Localization`, `Assets/AddressableAssetsData`, the Localization Settings entry in Preloaded Assets,
the `AF.Localization` reference from the game's assembly, and the `com.unity.localization` package (Addressables goes with
it unless the manifest lists it on its own). The build menu's content step compiles out without the Addressables
package.

## Not included

Fonts per language (TextMesh Pro font swapping for CJK through asset tables), voice-over language, a store's language
setting as a startup selector (comes with the Steam module), right-to-left text, and a "follow the system language"
choice once the player has picked one.

# Save

Versioned JSON save files with migrations, atomic writes, backups, and a single write queue. Global and device data
load during boot; slot data lives in a session that the game opens and closes. Built on Newtonsoft JSON and UniTask.

## Setup

```csharp
protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
{
    builder.UseSave(); // first, so later boot steps can read global data
}
```

`UseSave` takes optional `SaveOptions`: a `SaveSchema` per document (`Slot`, `Global`, `Device`), the `Storage`, and
`AutosaveSlotOnSuspend` (default on).

## Savables

A savable is a plain C# service whose state is one section of a save file. Derive from `Savable<TData>`:

```csharp
public sealed class WalletData
{
    public int coins;
}

public sealed class Wallet : Savable<WalletData>, IWallet
{
    private int _coins;

    public override string Key => "wallet";

    protected override WalletData Capture() => new WalletData { coins = _coins };
    protected override void Restore(WalletData data) => _coins = data.coins;
    protected override WalletData CreateDefault() => new WalletData { coins = 100 }; // optional; new game state
}
```

Register it as a singleton with its interfaces, in the root scope or a scene scope:

```csharp
builder.Register<Wallet>(Lifetime.Singleton).AsImplementedInterfaces();
```

The scope links its savables to the save registry when it is built and unlinks them when it is disposed. Nothing else
needs to be listed anywhere.

| Document | Holds | Loaded |
|---|---|---|
| `Slot` (default) | One play-through | By `LoadSlot` / `NewSlot` |
| `Global` | Shared by all slots and synced between devices: achievements, gameplay settings, input bindings, language | During boot |
| `Device` | This device only, never synced by the game or a store's cloud: display settings, channel volumes | During boot |

Override `Document` to choose another one.

### Rules

- `Key` is stable, unique within its document, 1-64 characters of `a-z`, `0-9`, `.`, `_` and `-`, starting with a letter
  or a digit. Never derive it from a type name. Two savables with the same key in a document throw when the second one
  registers.
- Every time a document becomes current, each of its savables gets `Restore(section ?? CreateDefault())`: when a slot is
  loaded or started, when global data loads, and when a savable registers while its document is loaded (a scene opening
  with a slot loaded). `Restore` replaces the whole state and must not throw. If something mirrors the state, publish your
  "changed" message from `Restore`.
- `Capture` reads only the savable's own fields. It also runs when a scene unloads, after its objects may be destroyed.
- A scene's savables are restored when its scope is built, before its entry points initialize. Components under Auto
  Inject Game Objects get their `[Inject] Construct` call before that restore. Read saved state in `Initialize`, `Start`
  or later, never in `Construct`.
- If a section does not match its data class when a scene opens with a slot loaded, the scene scope fails to build with a
  `SaveDataException`. `LoadSlot` can only check the savables that are registered when it runs.
- Savables are not MonoBehaviours or ScriptableObjects. A component that has saved state reads and writes a service.
- A savable registered without the `ISavable` contract (for example `.As<IWallet>()` only) is never saved; one registered
  with a lifetime other than singleton is a different instance from the game's. Scopes report both as errors when they are
  built (Editor and development builds).
- Something created at runtime that no scope owns registers itself: `ISaveRegistry.Register(savable)` returns a handle;
  disposing it captures the state and unregisters.

### Data classes

Public fields, a public parameterless constructor, concrete types. **AF > Validate All** reports what the serializer
would write but cannot read back:

- a Unity object reference (save an id or an asset key instead),
- an interface, abstract or `object` member (save data carries no type names),
- dictionary keys other than `string`, `int`, `long` or an enum,
- a Unity struct without a converter (supported: `Vector2`, `Vector3`, `Vector2Int`, `Vector3Int`, `Quaternion`, `Color`),
- a readonly field, or a property without a public setter (mark computed ones `[JsonIgnore]`),
- a class without a public parameterless constructor.

Enums are written as strings. Renaming an enum member or a field is a schema change and needs a migration step.

## Slots and sessions

```csharp
var slots = await save.ListSlots();             // newest first, with Status, SavedUtc, AppVersion, PlayTime
var result = await save.LoadSlot(slots[0].Id);  // "Continue"
if (!result.IsLoaded) { /* show result.Status */ }

save.NewSlot("2");                               // new game: every slot savable gets its default
var saved = await save.SaveSlot();               // writes the active slot
if (saved.IsWritten) { /* show "saved" */ }

await save.SaveSlot("auto");                     // save as: "auto" becomes the active slot
save.CloseSlot();                                // ends the session, nothing written (entering the menu does it too)
await save.DeleteSlot("2");
```

Slot ids are 1-32 characters of `a-z`, `0-9`, `_` and `-`, starting with a letter or a digit, and there is no limit on
how many a game uses. A slot that
cannot be loaded leaves the current session as it was. Slot operations throw `InvalidOperationException` while a load is
still running.

`NewSlot` and a save as (`SaveSlot(id)`) replace whatever the slot holds at their first write, an autosave on suspend
included, whatever its status: a damaged slot or one from a newer build too. The previous file survives as `.bak` for one
more write. Check `ListSlots` and ask the player before starting over an existing slot. Cancelling a save as stops the
wait only; once the write reaches the storage, that slot is the active one.

`DeleteSlot` removes every copy of the slot, the `.corrupt` one included.

A slot keeps the sections of savables that are not registered right now. Leaving level 1 for level 2 captures level 1's
state into the session, so a save in level 2 contains both. Reloading a scene restores the state it had when it
unloaded; to go back to the last save, call `LoadSlot(save.ActiveSlot)` before the load.

### Returning to the main menu

Entering `MenuState` closes the session (`CloseSlot`): the menu has no slot. A slot left open would be written by the next
autosave on suspend, with progress the player left without saving. To save on the way out, call `SaveSlot` before the
load, while the gameplay scene is still loaded:

```csharp
await save.SaveSlot();
await scenes.Load<MenuState>(Scenes.Menu);
```

`ISceneLoader` enters the menu state before the gameplay scene unloads, so nothing is captured into the closed slot.

For a load menu, `SlotInfo.TryReadSection<T>(key, out data)` reads one section without loading the slot. A game that
wants more than the date and play time keeps its own small "summary" savable for this.

`ISaveService.PlayTime` counts real time spent in `PlayingState` while not paused, and is saved with the slot.

### Dev session

Pressing Play in a gameplay scene enters `PlayingState` without a slot. The save service then starts a dev session: slot
data lives in memory and `SaveSlot` returns `Skipped`. To test saving, press Play in the menu scene. In a build, entering
gameplay without a slot is logged as an error, because the progress would be lost.

### Games without slots

A game whose progress lives in global data (levels cleared, settings) has no slots. Set `SaveOptions.UsesSlots = false`
in `UseSave`: entering gameplay then starts no session, so there is no dev session and no error in a build. Every slot
operation (`ListSlots`, `LoadSlot`, `NewSlot`, `SaveSlot`, `DeleteSlot`) throws `InvalidOperationException`; global and
device data are saved as usual, including on suspend.

## Writing

Every storage operation goes through one queue, in order. A save captures every savable on the main thread in the same
frame, then writes on a thread-pool thread; the result arrives on the main thread. A write still waiting in the queue for
the same file is replaced by the newer one, and both callers get its result. Cancelling the caller's token stops the
waiting, not the write.

`SaveResult.Status`:

| Status | Meaning |
|---|---|
| `Written` | The data reached the storage. Show "saved" feedback on this only. |
| `Skipped` | Nothing to write: a dev session, or unchanged data when the app was suspended. |
| `Refused` | The document is read-only for this session (see below). |
| `Failed` | The storage write failed (disk full, permissions). The previous file is intact. |

A savable that throws while capturing makes the save throw `SaveDataException`, and its document is not written.
`SaveGlobal` writes two documents: if a global savable throws, the device data is still written.

### Suspend and quit

When the app goes to the background (mobile `OnApplicationPause(true)`) or quits, the root scope publishes
`AppSuspending`. With Run In Background off, losing focus in the Editor's Play mode or in a windowed desktop player does
too (see Core.md). The save system then writes the global and device data if they changed, and the active slot too when
`AutosaveSlotOnSuspend` is on and it changed. These writes finish before the call returns: on mobile the process may be
frozen or killed right after. Turn `AutosaveSlotOnSuspend` off for checkpoint-only games.

A document is written on suspend only when its data differs from what was last written or loaded. When no global or
device file exists, the defaults captured during boot count as written, so no file is created until a setting changes
and a later build's new defaults still apply to players who never changed them. These writes run inside the
`OnApplicationPause` or `quitting` call, and so do the continuations of saves that were still waiting.

Returning to the main menu is not a suspend; see "Returning to the main menu" above.

## Files

```
<persistentDataPath>/saves/   global.json, slot-<id>.json
<persistentDataPath>/local/   device.json
```

Each file may have a `.bak` (the previous write), a `.tmp` (a write in progress, ignored on load) and a `.corrupt` (a
copy of a file that could not be read). For Steam Auto-Cloud, sync `saves/*.json` only and leave `local/` out. On mobile
the operating system's own backup (iCloud on iOS, Auto Backup on Android) may copy `persistentDataPath` and PlayerPrefs
to a new device, `local/` included; the template sets no backup rules.

```json
{
  "schemaVersion": 1,
  "meta": { "savedUtc": "2026-10-30T12:00:00.0000000Z", "appVersion": "1.0", "playTimeSeconds": 4520.5 },
  "sections": { "wallet": { "coins": 120 } }
}
```

A write goes to `.tmp`, is flushed to the disk, then replaces the file atomically; the old file becomes `.bak`.

The Editor and a Windows build of the same project share these folders. **AF > Save > Open Save Folder** opens them;
**AF > Save > Delete All Saves...** deletes them and the PlayerPrefs entries, after a confirmation.

## Damaged and unreadable files

Nothing is ever deleted automatically.

| What happened | What the save system does | Status |
|---|---|---|
| The file cannot be parsed, its backup can | Moves the file aside to `.corrupt` and loads the backup | `RecoveredFromBackup` |
| Neither can be parsed | Copies the file to `.corrupt`, leaves both in place | `Corrupt` |
| Written by a newer build (`schemaVersion` too high), for example after leaving a beta branch | Leaves it untouched | `TooNew` |
| The storage cannot be read: a lock held by an antivirus or a sync client after short retries, permissions | Leaves it untouched; it is not treated as damaged | `ReadFailed` |
| Readable, but a migration step or a data class fails on it | Leaves it untouched, restores nothing | `Failed` |
| No file | Defaults | `NotFound` |

For a slot, the game decides what to show; a `Corrupt` slot can be deleted or overwritten by the player. Global and
device data never stop the boot: they continue with defaults. While they are `TooNew`, `ReadFailed` or `Failed` they are
read-only for the session, so the file the game could not read is never overwritten with defaults. Every case except
`Loaded` and `NotFound` is logged as an error, which reaches Player.log in release builds.

## Schema versions and migrations

Each document type has its own version history. A document is written with the current version; older ones are migrated
on load, one step at a time, before any savable sees them.

```csharp
builder.UseSave(new SaveOptions
{
    Slot = new SaveSchema()
        .Step(1, SlotMigrations.V1ToV2)  // the slot schema is now version 2
        .Step(2, SlotMigrations.V2ToV3), // and 3
});

public static class SlotMigrations
{
    public static void V1ToV2(JObject document) => SaveMigration.RenameSection(document, "gold", "wallet");

    public static void V2ToV3(JObject document)
    {
        var wallet = (JObject)SaveMigration.Section(document, "wallet");
        if (wallet != null)
            wallet["coins"] = (int?)wallet["amount"] ?? 0; // field renamed
    }
}
```

- One step is one function that changes the document in place. Steps are added in order; a gap throws.
- Give every step a test with the document before and after it.
- Never edit a step after a version that uses it has shipped.
- When a schema version ships, keep a save file from that build in the game's tests and load it through the current
  chain.
- Framework-owned sections (`input.bindings`, `localization.locale`, `settings.display`, `settings.audio`) change only by
  adding fields; a breaking change uses a new key.

## Storage

`ISaveStorage` keeps entries such as `saves/slot-1` with their backup and quarantined copies. It is synchronous; the
queue runs it on the thread pool when `IsThreadSafe` is true and on the main thread otherwise.

| Storage | Use |
|---|---|
| `FileSaveStorage` (default) | Files under `persistentDataPath`. |
| `PlayerPrefsSaveStorage` | Small saves: casual mobile games, later WebGL. Main thread only; size limits per platform (1 MB in total on WebGL; the Windows registry is not meant for large values). |

```csharp
builder.UseSave(new SaveOptions { Storage = new PlayerPrefsSaveStorage() });
```

A platform store (Steam Remote Storage, consoles) implements `ISaveStorage`. Encryption or compression, if a game wants
them, wrap an existing storage; the template does neither. Encrypting a single-player save gives no real protection,
makes support and modding harder, and the JSON is small.

## Serializer

The save system uses its own `JsonSerializerSettings` (`SaveJson.CreateSettings()`); the global
`JsonConvert.DefaultSettings` is never set. Enums as strings, no type names, collections replaced on load instead of
appended to field initializers, date-like strings kept as strings, invariant culture.

# Platform

Store and platform services behind small interfaces: achievements and the store overlay. A backend implements all of
them for one store; the Mock backend is used in the Editor and in builds without a store. Built on VContainer and
MessagePipe; needs Core.

## Setup

```csharp
protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
{
    builder.UseSave();
    builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
    builder.UsePlatform(messagePipe, new PlatformOptions { AchievementIds = Achievements.All });
}
```

| Option | Default | Meaning |
|---|---|---|
| `AchievementIds` | empty | Every achievement id the game unlocks, exactly as defined in the store's back office. |
| `PauseOnOverlay` | `true` | Pause the game while the store overlay is open. |

`UsePlatform(messagePipe, options, backend)` installs `backend`; without one it installs `MockPlatform`. Choosing a
store backend per build comes with the store modules; until then every build uses the Mock.

## Contracts

| Interface | Use |
|---|---|
| `IAchievements` | `Unlock(id)`, `IsUnlocked(id)` |
| `IPlatformOverlay` | `IsOpen`, and the `PlatformOverlayChanged { IsOpen }` message when it changes |

Every backend registers every contract. A store without a feature registers a no-op implementation; `UsePlatform`
throws, naming the missing interface, when a backend leaves one out. A store that starts asynchronously registers an
`IBootStep` from its backend, so `IAppBoot.WaitReady` covers it.

### Achievements

- The game's save data is the truth; the store only mirrors it. After loading, unlock again everything the save says
  was earned: a store that was not ready yet may have missed it. `Unlock` is idempotent.
- An id missing from `AchievementIds` is logged as an error and ignored. Stores ignore unknown ids silently; the check
  makes a typo show up in the Editor. The Mock does the check; a store backend's `IAchievements` must do the same.
- Stats (progress counters) are not part of the template: their semantics differ per store and they cannot be undone.

### Overlay

With `PauseOnOverlay`, the game holds a pause request (`platform-overlay`) while the overlay is open. Input follows the
pause through the Input module's paused mode. Closing the overlay releases only that request: a pause menu the game
opened meanwhile stays open. A game that opens its pause menu when the overlay opens listens to
`PlatformOverlayChanged`.

## Mock

| Mock | Behaviour |
|---|---|
| `MockAchievements` | Keeps unlocks in memory for the session and logs them. `Unlocked` lists them; `ResetAll()` locks everything again. |
| `MockPlatformOverlay` | Never opens on its own. `SetOpen(bool)` opens and closes it, publishing `PlatformOverlayChanged` once per change. |

Both are also registered as their own type, so tests and Play Mode tools can resolve and drive them:

```csharp
container.Resolve<MockPlatformOverlay>().SetOpen(true); // the game pauses
```

## Adding a backend

Implement `IPlatformBackend` in its own assembly (for example `AF.Platform.Steam`, referencing `AF.Platform`):

```csharp
public sealed class SteamPlatform : IPlatformBackend
{
    public string Name => "Steam";

    public void Install(IContainerBuilder builder, PlatformOptions options)
    {
        builder.Register<SteamAchievements>(Lifetime.Singleton).As<IAchievements>();
        builder.Register<SteamOverlay>(Lifetime.Singleton).As<IPlatformOverlay>();
        builder.Register<SteamBoot>(Lifetime.Singleton).As<IBootStep>();
    }
}
```

Game code talks to the interfaces only; store SDK types stay inside the backend assembly, which is the only one built
with the store's define.

## Not included

User identity, DLC and ownership, rich presence, stats, cloud storage (Steam Auto-Cloud needs no code; a store API
plugs in as an `ISaveStorage`), virtual keyboard, handheld detection. They are added with the same pattern when a game
needs them.

# Settings

The player's display settings (resolution, window mode, vsync, frame rate limit, quality level, cursor confinement)
and the saving of the Audio module's channel volumes. Both are saved with this device's data, applied at startup while
the save data loads, and changed through services that apply at once and never write to disk themselves. Needs the
Save, Audio and Input modules.

## Setup

```csharp
protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
{
    builder.UseSave();
    builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
    builder.UseAudio(_audioMixer, messagePipe, AudioChannels.CreateOptions());
    builder.UseSettings(messagePipe); // after the three above; throws otherwise
}
```

`UseSettings` takes optional `SettingsOptions`: the defaults a player who never changed a setting gets. A frame rate
limit the setters would refuse (below 15, other than 0) throws there.

| Option | Default |
|---|---|
| `WindowMode` | `Borderless` |
| `VSync` | on |
| `FrameRateLimit` (desktop) | 0, no limit |
| `MobileFrameRateLimit` | 60 (Unity's own mobile default is 30); 0 is the display's refresh rate |
| `ConfineCursor` | off |

The resolution defaults to the display's own size, and the quality level to the level the app started with: the
platform's default level (Project Settings > Quality).

## Where each setting is saved

| Setting | Document | Key | Owner |
|---|---|---|---|
| Resolution, window mode, vsync, frame rate limit, quality, cursor confinement | Device | `settings.display` | Settings (`IDisplaySettings`) |
| Channel volumes, mute when unfocused | Device | `settings.audio` | Settings, from `IAudioVolumes` |
| Language | Global | `localization.locale` | Localization (`ILocaleService`) |
| Input bindings | Global | `input.bindings` | Input (`IRebindService`) |
| Gameplay settings | Global | the game's own key | the game (see below) |

Display and audio values depend on the hardware: a resolution or a volume chosen on a Steam Deck is wrong on a desktop
with headphones, so they stay on the device and are never synced. The language and the bindings belong to the player
and follow them to other devices.

## Display settings

```csharp
display.SetResolution(display.Resolutions[0]);
display.SetWindowMode(WindowMode.Windowed);
display.SetVSync(false);
display.SetFrameRateLimit(60);
display.SetQuality(display.QualityLevels[0]);
display.SetConfineCursor(true);
display.ResetToDefaults();
```

`IDisplaySettings.Current` is what is in effect. A settings screen builds its rows from the lists the service offers,
never from lists of its own:

| List | Contents |
|---|---|
| `Resolutions` | The display's sizes for the current window mode, largest first, each size once whatever its refresh rates. Up to the display's own size; in a window, only sizes that fit beside the taskbar with room for a title bar (on a 1080p display, 1600x900 and smaller). Read it again after a window mode change. |
| `WindowModes` | `Windowed` and `Borderless`; `Exclusive` too on Windows. Empty on mobile. |
| `QualityLevels` | The names of the quality levels this platform builds with. Levels are saved by name, not by position. |

A setter throws for a value its list does not offer, and for a frame rate limit below 15 other than 0. On mobile
(`HasWindow` is false) the resolution, window mode, vsync and cursor confinement do not apply and their setters throw.

While vsync is on, the frame rate limit is ignored. The quality level is applied first, because changing it resets vsync
to that level's value. Exclusive fullscreen uses the highest refresh rate the display offers for the chosen size. The display's own size is
the one read outside exclusive fullscreen (which switches the display to the game's resolution), or the primary display's
native size when the app starts in it. Each
step is applied on its own; one that throws is logged and the others still apply.

Cursor confinement keeps the cursor inside the window while the input mode's cursor is free or hidden (see Input.md);
the input mode service applies it. It takes effect on Windows and Linux.

Unity's own Alt+Enter fullscreen switch is turned off (Player Settings > Resolution and Presentation > Allow
Fullscreen Switch): it changed the window mode behind the settings, and the next apply undid it. A game that wants the
shortcut binds an input action to `SetWindowMode`.

### Unset and invalid values

A setting the player never chose is saved as null, and so is a choice equal to the default: the default applies, also a
later build's default. A choice that is not valid on this machine (a resolution larger than the display, a quality level
that was removed, exclusive fullscreen on a platform without it) gives way to the default for the session, with a warning
in the log. The choice itself stays saved until the player picks another, so plugging the larger monitor back in brings
the resolution back.

### In the Editor

The Game view keeps its own size and its own vsync toggle, so the Editor applies neither the resolution and window mode
nor vsync. The quality level and the frame rate limit apply; the quality level goes back to what it was when Play stops,
so Play never changes QualitySettings.asset.

The mobile behaviour (no window, `MobileFrameRateLimit`, `HasWindow` false) follows `Application.isMobilePlatform`, so
it runs in mobile players only: the Editor takes the desktop path even with Android or iOS as the build target, and so
does the Device Simulator, which simulates the `UnityEngine.Device` APIs that this module does not use.

## Volumes

Channel volumes live in `IAudioVolumes` (see Audio.md); this module saves them, keyed by mixer group name, and sets them
back at startup. Whatever sets a volume through `IAudioVolumes` is saved. A channel added since the file was written
starts at full volume; a saved volume of a channel that no longer exists is dropped with a warning.
`IAudioVolumes.MuteWhenUnfocused` is saved the same way.

## Changes and saving

Setters change the setting in memory, apply it and publish a "changed" message; they never write to disk. Messages are
published after the new value is stored and only when something changed:

| Message | Published by |
|---|---|
| `DisplaySettingsChanged {Previous, Current}` | `IDisplaySettings`, once per change, with the whole state |
| `AudioVolumeChanged {Channel, Volume}` | `IAudioVolumes` |
| `LocaleChanged {Previous, Current}` | `ILocaleService` (Localization.md) |
| `BindingsChanged {Action}` | `IRebindService` (Input.md) |

The data reaches the disk:

1. when the settings screen closes, if the game calls `ISaveService.SaveGlobal()`, which writes the global and the device
   data. Call it from the screen's own hide method, not from `OnDisable`: a screen hidden another way never runs it;
2. when the app goes to the background or quits: the save system writes the global and device data if they changed (see
   Save.md).

There is no timer: a slider dragged for a second changes memory only, and the write on suspend or quit happens anyway, so
the call on closing is insurance against a crash. A save requested while another runs is queued, never dropped.

A screen that wants a "keep these settings?" countdown after a resolution change stores `Current` before the change and
sets it back if the player does not confirm.

## Gameplay settings

Settings that belong to one game (subtitles, camera shake, difficulty aids) are the game's own savable in the global
document, with its own "changed" message, registered next to it:

```csharp
builder.RegisterMessageBroker<GameplaySettingsChanged>(messagePipe);
builder.Register<GameplaySettings>(Lifetime.Singleton).AsImplementedInterfaces();
```

```csharp
public sealed class GameplaySettingsData
{
    public bool subtitles = true;
}

public sealed class GameplaySettings : Savable<GameplaySettingsData>, IGameplaySettings
{
    private readonly IPublisher<GameplaySettingsChanged> _changed;
    private GameplaySettingsData _data = new GameplaySettingsData();

    public GameplaySettings(IPublisher<GameplaySettingsChanged> changed) => _changed = changed;

    public override string Key => "gameplay";
    public override SaveDocument Document => SaveDocument.Global;

    public bool Subtitles => _data.subtitles;

    public void SetSubtitles(bool on)
    {
        if (_data.subtitles == on)
            return;
        _data.subtitles = on;
        _changed.Publish(new GameplaySettingsChanged());
    }

    protected override GameplaySettingsData Capture() => _data;

    protected override void Restore(GameplaySettingsData data)
    {
        _data = data;
        _changed.Publish(new GameplaySettingsChanged());
    }
}
```

A value used as a multiplier (mouse sensitivity) gets a lower bound where it is read as well as on the slider: a value
loaded from a file is not clamped by the slider.

## Validation

`DirectDisplayWriteValidator` reports project code that calls `Screen.SetResolution`, sets `Screen.fullScreen` or
`Screen.fullScreenMode`, changes the quality level (`QualitySettings.SetQualityLevel`, `IncreaseLevel`, `DecreaseLevel`,
`currentLevel`), or sets `QualitySettings.vSyncCount`, `Application.targetFrameRate` or
`IInputModeService.ConfineCursor`: such a write is never saved and the next apply undoes it. Mark a deliberate
exception, such as a benchmark scene, `[DisplaySettingsExempt("reason")]`.

## Not included

Render scale, brightness or gamma, choosing the monitor, choosing the refresh rate, a resizable window (Player Settings
keeps it off; Unity remembers a dragged size itself, but the next startup applies the saved one), and the `-screen-*`
command line arguments (the startup apply overrides them).

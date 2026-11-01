# Audio

Sound cues played on pooled sources, a crossfading music player, channel volumes through an audio mixer, muting, and
the game's single AudioListener. Sounds belong to the scope that played them, so a scene's sounds stop with the scene.
Built on Unity's audio mixer, the Core module's pools and PrimeTween.

## Setup

```csharp
public sealed class GameRootScope : RootScope
{
    [SerializeField, Required] private AudioMixer _audioMixer;

    protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
    {
        builder.UseAudio(_audioMixer, messagePipe, AudioChannels.CreateOptions());
    }
}
```

| Piece | Where |
|---|---|
| Mixer | `Assets/Game/Audio/GameMixer.mixer`, referenced by the root scope. Groups Master > Music, SFX, UI; each group's volume is exposed as `MasterVolume`, `MusicVolume`, `SfxVolume`, `UiVolume`. |
| Channels | `Assets/Game/Scripts/AudioChannels.cs`: each channel names a mixer group and its exposed volume parameter. `Music` is the group the music plays through. |
| Runtime objects | Under the root scope: `Audio` (the listener, two music sources, the inactive voice template, and the sources playing now) and `[Pools]/Voice` (the sources waiting). |

`UseAudio` checks the mixer while the root scope is configured: a channel whose group (matched by its exact name) or
volume parameter is missing stops the build; a missing group's error lists the mixer's groups. To add a channel, add the
group to the mixer, expose its volume (right-click Volume in the group's inspector, then rename the parameter in the
Exposed Parameters list), and add an `AudioChannel` for it.

Options: `PrewarmVoices` (sources created up front, 8; the pool grows past it) and `MuteWhenUnfocused` (on).

## Sound cues

A `SoundCue` asset (**Create > AF > Sound Cue**) is a sound as the designer sets it up: one or more clips (one is picked
at random), the mixer group it plays through, volume, a pitch range, looping, the 3D distances and rolloff, and **Play
While Paused** for interface sounds. Code plays cues and never names a mixer group:

```csharp
[SerializeField, Required] private SoundCue _hitCue;

sounds.PlayAt(_hitCue, transform.position); // 3D, with the cue's distances
sounds.Play(_clickCue);                     // 2D
```

`ISounds` returns a `SoundHandle`. `IsPlaying` stays true until the sound ends, is stopped, or its scope ends;
`Stop(fadeSeconds)` stops it, with a fade if asked. A handle stays dead once its sound has ended, even after its source
plays another sound, so it is safe to keep. A looping cue plays until its handle stops it or its scope ends. A sound
stopped with a fade ends when the fade does, even if the fade is killed elsewhere.

Each play sets every field of the pooled source again, so nothing carries over from the sound it played before.

### Which scope a sound belongs to

Every scope resolves its own `ISounds`. Sounds played through a scene's instance stop at once when that scene's scope
ends, which happens under the loading screen (see Scenes in Core.md); an additive scene takes its own sounds with it.
Sounds played through the root's instance are not affected. Scene code whose sound must outlive the scene, such as a
jingle that plays over the scene change, asks for `IGlobalSounds`, which is the root's. To fade a scene's loops out before
it goes, stop their handles with a fade before calling `Load`.

## Music

```csharp
music.Play(_menuTheme);       // crossfades from the current track, 1 second by default
music.Play(_battleTheme, 2f);
music.Stop();
```

There is one music track. `Play` crossfades to the new clip, looped; playing the current track again does nothing, and
the last call wins. Music plays through the `Music` channel, keeps playing while the game is paused and across scene
changes, and fades in unscaled time. A game that wants it quieter in the pause menu uses a mixer snapshot. When the audio
output device changes, Unity stops every source; the current track starts again from its beginning.

## Volumes and muting

```csharp
volumes.Set(AudioChannels.Music, 0.5f); // linear 0–1, as a slider shows it
var handle = volumes.Mute("rewarded-ad");
handle.Dispose();
```

Volumes are linear from 0 to 1 and reach the mixer in decibels (0 is -80 dB). `AudioVolumeChanged {Channel, Volume}` is
published when a volume changes, for a slider that follows it. The Settings module saves the volumes and
`MuteWhenUnfocused` with the device data and sets them back at startup (see Settings.md); the Audio module itself keeps
them in memory only. Setting an exposed parameter from code takes it away from the mixer's snapshots for good, so do not
drive channel volumes with snapshots too.

`Mute(reason)` silences everything, music included, until the handle is disposed: an ad, a video. Like pause requests,
the game stays silent while any handle is alive. With `MuteWhenUnfocused` on, a desktop player is silent while its
window is not focused. The Editor ignores focus (it is unfocused whenever another tool has the keyboard), and so do
mobile players: there the on-screen keyboard and system overlays take the focus while the game is still on screen.

## Pause and loading

While the game is paused, `AudioListener.pause` is on: every source pauses, except music and cues marked **Play While
Paused**. A sound started during the pause waits and plays on resume. Fades of game sounds run in scaled time and freeze
with them; interface sounds and music fade in unscaled time.

The Audio module is the only writer of `AudioListener.pause` (it follows `IPauseService`) and of `AudioListener.volume`
(muting). In the Editor and development builds, a change made elsewhere is reported as an error.

When the app goes to the background on mobile, Unity pauses its own audio output; the module does nothing more. An ad or
a video that must silence the game takes a `Mute` handle.

## The listener

The game has one AudioListener, under the root scope, so the music and the root's sounds stay audible while scenes
change, and an additive scene never brings a second one. It follows the main camera after cameras move each frame;
`IAudioListener.Follow(target)` makes it follow something else (the player in a third-person game), and it goes back to
the main camera when that target is destroyed or set to null. Scenes and prefabs have no AudioListener.

## Validation

`AudioValidator` reports:

- sound cues without clips, with an empty clip slot, or without an output mixer group;
- AudioSources without an output mixer group in scenes, the root prefab and other prefabs: the channel volumes do not
  reach them;
- AudioListeners in scenes and prefabs.

Cues and prefabs are checked by **Validate All** and the build check, not on every Play.

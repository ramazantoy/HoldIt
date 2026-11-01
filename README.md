# Unity Game Template

[Türkçe](README.tr.md)

A Unity 6.3 (URP) project template for PC and mobile games. It carries the systems every game rewrites, ready on day one:
dependency injection scopes, game states, scene loading, saving, input modes, audio, object pools, player settings,
localization, platform services and a checked build pipeline. A new game starts with GitHub's **Use this template** and
builds on top.

## What it is not

- **No UI.** There are no menus, HUD or settings screens. The systems expose the services and messages a UI needs.
- **No gameplay systems.** Characters, combat, inventories and the like belong to each game.
- **Not a package.** The framework is source code under `Assets/Framework/`, copied into each game. There is no update
  flow between games; a fix made in a game is brought back to the template by comparing that one folder.

## Requirements

Unity **6000.3.27f2** (6.3 LTS) with the Universal Render Pipeline 17.3. Building needs the platform's build support:
Windows Build Support (IL2CPP) for Windows, Android Build Support for Android.

| Package | Version | Source | Used for |
|---|---|---|---|
| VContainer | 1.19.0 | OpenUPM | Dependency injection |
| UniTask | 2.5.11 | OpenUPM | Async |
| MessagePipe, MessagePipe.VContainer | 1.8.2 | OpenUPM | Messages |
| PrimeTween | 1.5.2 | npmjs | Tweens |
| Newtonsoft Json | 3.2.2 | Unity | Save files |
| Input System | 1.20.1 | Unity | Input |
| Localization (brings Addressables 2.10.3) | 1.5.13 | Unity | Languages |
| Mono.Cecil | 1.11.6 | Unity | Build checks (Editor only) |
| Pipeline | 0.9.1-exp.1 (experimental) | Unity | Unity CLI connection to the Editor and to Development builds |

OpenUPM packages are not signed: Unity asks about the missing signature when the project is first opened.

## Modules

Each module is one folder under `Assets/Framework/` with its own assemblies (`AF.<Module>`, plus `.Editor` and `.Tests`).

| Module | What it does | Needs | Documentation |
|---|---|---|---|
| Core | Root and scene scopes, game states, scene loading, pause and game time, boot steps, object pools, tween rules, logging, validation | — | [Core.md](Assets/Framework/Documentation~/Core.md) |
| Save | Versioned JSON saves with migrations, atomic writes, backups and one write queue; slots, global and device data | Core | [Save.md](Assets/Framework/Documentation~/Save.md) |
| Input | Input modes as the only owner of action maps and the cursor, control scheme tracking, rebinding | Core, Save | [Input.md](Assets/Framework/Documentation~/Input.md) |
| Audio | Sound cues on pooled sources, music with crossfade, mixer channel volumes, muting, the single AudioListener | Core | [Audio.md](Assets/Framework/Documentation~/Audio.md) |
| Settings | Display settings and saved channel volumes | Core, Save, Audio, Input | [Settings.md](Assets/Framework/Documentation~/Settings.md) |
| Localization | The game's language through Unity Localization, saved with the global data | Core, Save | [Localization.md](Assets/Framework/Documentation~/Localization.md) |
| Platform | Achievements and store overlay behind small interfaces; a Mock backend | Core | [Platform.md](Assets/Framework/Documentation~/Platform.md) |
| Build | One build menu with pre-build and post-build checks (Editor only) | Core | [Build.md](Assets/Framework/Documentation~/Build.md) |

`Assets/Game/` is the placeholder game: the `Game` assembly, the root scope prefab, the Intro, Menu and Gameplay scenes, the
audio mixer, the input actions and example localization assets. **AF > Validate All** checks the project and the build's
scenes; the build menu runs it too.

## Starting a new game

1. Create a repository from this template (**Use this template**) and clone it.
2. Open it with Unity 6000.3.27f2 and accept the missing-signature dialog for the OpenUPM packages.
3. Install the build support you need (Windows IL2CPP; for Android, Android Build Support with its SDK, NDK and OpenJDK).
   Switch the platform in **File > Build Profiles**; the build menu builds the active platform and never switches it.
4. In **Player Settings**, set Company Name, Product Name and Version. `persistentDataPath` and the application identifier
   (`com.<Company>.<Product>`) come from them; if the product name has spaces or symbols, check the identifier Player
   Settings shows.
5. Run **AF > Build > Check Release**: the placeholder-name error must be gone.
6. Rename the `Game` namespace in `Assets/Game/Scripts/` and the `Game` assembly's Root Namespace. Keep the assembly name.
7. Remove the modules the game does not need, if any: its `Use…` line in `GameRootScope`, its folder, and its reference in
   `Game.asmdef` ([Localization.md](Assets/Framework/Documentation~/Localization.md) lists the extra steps for that module).
8. Scenes: Intro (build index 0, Boot Target Manual), Menu (Boot Target Menu), Gameplay (Boot Target Playing). When you
   rename or add one, keep the build's scene list and `Assets/Game/Scripts/Scenes.cs` in step: nothing compares them, and
   a missing name throws at its first load. Adding a scene: [Core.md](Assets/Framework/Documentation~/Core.md).
9. Root prefab `Assets/Game/Prefabs/RootScope.prefab`: the game's root scope and the only EventSystem. A loading screen goes
   here too.
10. Audio: the mixer's groups (`Assets/Game/Audio/GameMixer.mixer`) and `AudioChannels.cs` match.
11. Input: the maps of `Assets/Game/Input/Controls.inputactions` and the modes in `InputModes.cs` match. In the template,
    Escape is bound to both `Gameplay/Pause` and `UI/Cancel`; decide what the game wants ([Input.md](Assets/Framework/Documentation~/Input.md)).
12. Localization: replace the example locales (English, Turkish) and the `UI` table's `menu.play` entry, and make the
    Specific Locale Selector name the game's main language. A game with one language can remove the module.
13. Mobile: set the Default Orientation (the template allows all four). Android API level 25, ARM64, IL2CPP and Minimal
    stripping are set; signing is not.
14. Quality levels: Mobile is the default for Android and iOS, PC for Windows, macOS and Linux.
15. Check that the console has no errors, the EditMode tests pass in the Test Runner and **AF > Validate All** reports
    nothing.
16. Build with **AF > Build > Development** and run it, then **AF > Build > Release**. The first build may rewrite URP's
    pipeline assets; commit those changes separately.

## Platform status

**Windows (IL2CPP, managed stripping Minimal): verified** with Development and Release builds. In the player: the root
scope and boot steps, global and device save data read and written back (including saved input binding overrides), the
intro-to-menu scene flow, struct messages through MessagePipe, the audio mixer and voice pool setup, muting while the
window is unfocused, saved display settings and language applied at startup, Unity Localization's content in the build,
and no Pipeline runtime in Release builds. Not exercised in a player because the template has no UI: slot saves from
gameplay, sound playback, pause and overlay messages. Exclusive fullscreen has not been tried.

**Mobile: not verified.** The code has mobile paths (the touch control scheme, a mobile frame rate default, saving when
the app goes to the background, a PlayerPrefs save storage, Android IL2CPP settings), but no Android or iOS build has been
made and nothing has run on a device. The first mobile game made from the template will test it, and fixes go back into
the template.

**macOS and Linux:** no build has been made.

**Steam:** there is no Steam module yet. Achievements and the overlay run on the Mock backend; a store backend plugs in
behind the same interfaces ([Platform.md](Assets/Framework/Documentation~/Platform.md)).

A Development build carries the Pipeline package's local HTTP server, which runs code sent to it. Never hand one out;
testers get Release builds.

## Authorship

Architecture, decisions and review by Ramazan Toy. Much of the code was written with Claude Code under that direction;
those commits carry a Co-Authored-By line. `CLAUDE.md` and `.claude/` hold the rules and the decision log the work follows.

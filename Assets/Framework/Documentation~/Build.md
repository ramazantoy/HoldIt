# Build

One way to build: **AF > Build**. It checks the project, builds the active platform with the right options and defines,
checks the result and writes a report next to it. Editor only (`AF.Build.Editor`).

## Menu

| Item | Does |
|---|---|
| **AF > Build > Development** | Development build of the active platform. |
| **AF > Build > Release** | Release build of the active platform. |
| **AF > Build > Check Release** | Runs the Release pre-build checks without building. |

The menu builds the active build target (Windows, macOS, Linux, Android, iOS) and never switches it: switching
reimports the project. Switch in the Build Profiles window. Standalone targets are always built as a player, never as a
dedicated server. There are no dialogs; results go to the console and to the report file.

## Kinds

| | Development | Release |
|---|---|---|
| Player | Development build | Release |
| Defines | `SHOW_LOGS` | none |
| `Log.Info`, `Log.Warning` | in the build | compiled out |
| `Log.Error`, `Log.Exception` | in the build | in the build |
| Pipeline runtime (CLI connection) | in the build while the Pipeline's development toggle is on (it is in the template, for Windows, macOS and Linux players only; Android and iOS Development builds have no CLI connection) | never; refused before building, and the build fails if it is found |
| URP Rendering Debugger | opens with Ctrl+Backspace or both sticks | compiled out |
| Placeholder Company/Product | warning | error |
| Meant for | local testing | players and testers |

A Development build carries a local HTTP server that runs code sent to it. Never hand one out; testers get Release
builds.

## Defines

Defines reach a player only from the build menu (`BuildKinds.Defines`, passed as `extraScriptingDefines`); the Editor
does not see them. A define in Player Settings, a `-define` in Additional Compiler Arguments or in a `csc.rsp` under
Assets refuses the build: one left there goes into every build without anyone noticing.

One Player Settings define is allowed: `ENABLE_RUNTIME_PIPELINE_DEV`, written by the Pipeline package's **Enable in
Development & Profiled Builds** toggle (Project Settings > Pipeline > Runtime). The Pipeline assemblies also require a
symbol only development builds have, so it never reaches a Release build. **Enable in Release Builds**
(`ENABLE_RUNTIME_PIPELINE`) refuses every build.

Code that depends on a define goes inside `#if`; serialized fields never do. A field that exists in the Editor but not
in the player (or the other way round) makes the player read scenes and prefabs with the wrong layout. This includes
`#if UNITY_EDITOR`.

## Output

| Path | Contents |
|---|---|
| `Builds/<Platform>/<Kind>/` | The build. Ship or upload this folder as it is; for iOS it is the Xcode project Unity writes, built and signed in Xcode. |
| `Builds/<Platform>/<Kind>-symbols/` | IL2CPP and Burst debug folders, moved out of the build. Keep them for crash symbolication; never ship them. |
| `Builds/<Platform>/<Kind>-report.txt` | The result: status, size, duration, version, commit, backend, stripping, defines, checks, notes. |

Paths are fixed, so the latest build of a kind is always in the same place. Every build deletes the report first, and
the folder and its symbols right before building; a refused build leaves the previous build in place with a report
saying it was refused. A failed Release build's folder is deleted, so it cannot be shipped by mistake; a failed
Development build is kept for inspection. Only paths inside `Builds/` are ever deleted, apart from the layout check's
scratch folder `Temp/BuildLayoutCheck`. `Builds/` is ignored by git.

## Pre-build checks

| Check | Development | Release |
|---|---|---|
| Play Mode, or scripts compiling | error | error |
| **AF > Validate All** errors | error | error |
| An open scene or prefab with unsaved changes (the build uses the saved file) | error | error |
| Placeholder names: Company `DefaultCompany`; Product `TemplateProject`, `My project`, `New Unity Project`; an identifier containing `com.unity.template`, `DefaultCompany` or `com.Company.ProductName` | warning | error |
| A define in Player Settings (except `ENABLE_RUNTIME_PIPELINE_DEV`), compiler arguments or `csc.rsp` | error | error |
| A custom Build Profile is active (its scenes, defines and Player Settings overrides bypass the menu) | error | error |
| A serialized field that differs between the Editor and this build | error | error |
| The Pipeline runtime compiles into the build (**Enable in Release Builds** is on) | | error |
| Unsupported or not installed build target; no enabled scene | error | error |
| Addressables installed without settings, or set to build its content inside the player build | error | error |
| Unsaved asset changes, which the build writes to disk and includes (changes made in Play Mode end up here) | warning | warning |
| Mono scripting backend | | warning |
| Managed stripping above Minimal | warning | warning |
| Uncommitted changes in the working tree, or its state unknown (git missing or failing) | | warning |

Placeholder names matter because `persistentDataPath` and the store identity are made from them: two games with the
same names share their save data. Set Company and Product Name in Player Settings before the first build; the
application identifier follows them.

The serialized layout check compiles the player scripts with the build's target, options and defines, reads them and
the Editor's assemblies with Mono.Cecil, and compares the serialized fields of every MonoBehaviour, ScriptableObject and
`[Serializable]` type in the project's assemblies. It takes about half a minute; Unity's own check fails much later in
the build, without naming the field.

## Addressables content

When the Addressables package is installed (Unity Localization brings it), the build menu builds the Addressables
content for the active platform after the checks and before the previous build is deleted, so content that fails to
build leaves the previous build in place. The report notes the content build's duration, or its error. Addressables'
own **Build Addressables on Player Build** must stay at "Do not Build Addressables content on Player build": with any
other value the content would be built twice, inside the player build where its failure is harder to read, and the
build check refuses it. Unity still copies the built content into the player (`StreamingAssets/aa/`), and the build
fails when it is not there: without it Unity Localization finds no language and the game stays in its boot.

The content step is compiled only with the Addressables package (`AF_ADDRESSABLES`, a version define of
`AF.Build.Editor`).

## After the build

- Debug folders that must not ship (`*_BackUpThisFolder_ButDontShipItWithYourGame`, `*_BurstDebugInformation_DoNotShip`)
  are moved to `<Kind>-symbols/`. An Android package is left as it is: nothing next to it is moved.
- A build folder is searched for the Pipeline runtime. In a Release build, finding it fails the build. A package
  (`.apk`, `.aab`) is not searched; the pre-build check covers it.
- With Addressables, a build folder without `StreamingAssets/aa/settings.json` fails the build. A package is not
  searched.
- Files the build created or changed in the project are listed in the report, including files that already had
  uncommitted changes. Unity and packages sometimes rewrite assets while building (URP rewrites its pipeline assets'
  shader prefiltering data; Addressables creates its profile and build pipeline settings on its first content build);
  review them before committing.
- An error during the build (a locked file, for example a player from the build folder still running) ends in a
  Failed report instead of a missing one.

## Only one way to build

A build that does not come from `Builder` is refused when it starts: the Build Profiles window's Build button, the
Pipeline's `build run` command and other scripts skip the checks and the defines. Player builds made by the Test
Runner are let through.

## From code and the CLI

```csharp
BuildOutcome outcome = Builder.Build(BuildKind.Release);              // build the active platform
ValidationReport report = Builder.Check(new BuildRequest(BuildKind.Release)); // checks only
```

`BuildOutcome.ToString()` is the report. From the Unity CLI, put a static wrapper in a `.cs` file outside `Assets/` and
run it with a long timeout; a build holds the Editor for minutes (a Windows IL2CPP build of the template takes about 12
minutes for Development and 5 for Release on a warm cache):

```csharp
public static class BuildCli
{
    public static string Release() => AF.Build.Editor.Builder.Build(AF.Build.Editor.BuildKind.Release).ToString();
}
```

```
unity command script run --file BuildCli.cs --entry BuildCli.Release --timeout 3600
```

## Running a Development build

The Pipeline runtime starts with the player and writes `.unity-pipeline-runtime-port` next to the executable. The CLI
connects through it:

```
unity command --runtime-path Builds/Windows/Development runtime status
unity command --runtime-path Builds/Windows/Development console get --tail 50
unity command --runtime-path Builds/Windows/Development runtime simulate_key --key Escape
unity command --runtime-path Builds/Windows/Development runtime quit
```

An IL2CPP player exposes the console, status, quit and simulated keyboard and pointer input. It cannot evaluate code:
`script eval` compiles inside the player, and an IL2CPP player has no assemblies to compile against. Check behaviour
through the log and the files the game writes. The player's log is `Player.log` in
`%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\` on Windows; `-logFile <path>` writes it elsewhere. The Editor uses
the same folder for its save data, so a test that changes saves there backs the folder up first. Simulated input is
reliable while the player window has focus.

## IL2CPP and stripping

Standalone, Android and iOS use IL2CPP with managed stripping at **Minimal**: only engine and .NET class libraries are
stripped, the project's and packages' assemblies are kept whole. The framework is verified on a Windows IL2CPP build at
that level: VContainer builds its objects by reflection, MessagePipe delivers struct messages, and save data
round-trips through Newtonsoft.

A game that raises stripping for a smaller binary keeps what is only reached by reflection:

- constructors VContainer calls: `[Inject]` on the constructor (it is a preserve attribute), or `[Preserve]`;
- save data classes and their fields: `[Preserve]` on the class, or a `link.xml` under `Assets/`:

```xml
<linker>
  <assembly fullname="Game" preserve="all"/>
</linker>
```

The pre-build check warns while stripping is above Minimal.

## Not included

Build flavors (demo, playtest), store upload, Android signing, version numbering. Flavors come with their own phase;
a store's upload step comes with its platform module.

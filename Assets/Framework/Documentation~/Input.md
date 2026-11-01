# Input

One owner for the Input System's action maps: an input mode service that enables the maps the current mode needs and
disables every other one, follows the game state and pause, and drives the cursor. Also a control scheme tracker for
button prompts, and rebinding saved with the global save data. Built on the Input System package, VContainer and
MessagePipe; needs the Save module.

## Setup

```csharp
public sealed class GameRootScope : RootScope
{
    [SerializeField, Required] private InputActionAsset _inputActions;

    protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
    {
        builder.UseSave();
        builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
    }
}
```

| Piece | Where |
|---|---|
| Actions asset | `Assets/Game/Input/Controls.inputactions`, referenced by the root scope. Starts with a Gameplay map (Move, Look, Pause), Unity's standard UI map, and the Keyboard&Mouse, Gamepad and Touch control schemes. |
| Modes | `Assets/Game/Scripts/InputModes.cs` |
| EventSystem | A child of the root prefab, with an `InputSystemUIInputModule` that uses the same actions asset. Scenes and other prefabs have none, so it is not a prefab of its own either. |
| Project settings | Project-wide Actions: None. Update mode: Process Events In Dynamic Update. |

Use the imported asset itself. Leave "Generate C# Class" off: the generated class builds its own copy of the asset, so
the UI input module, `InputActionReference` fields and rebinds would each see a different set of actions.

`UseInput` throws when `UseSave` has not been called before it.

## Modes

A mode is a set of action maps that are enabled together, plus what the cursor does meanwhile. The game defines its
modes once:

```csharp
public static class InputModes
{
    public static readonly InputMode Menu = new InputMode("menu", CursorState.Free, "UI");
    public static readonly InputMode Gameplay = new InputMode("gameplay", CursorState.Free, "Gameplay", "UI");
    public static readonly InputMode Inventory = new InputMode("inventory", CursorState.Free, "Inventory", "UI");

    public static InputOptions CreateOptions() =>
        new InputOptions { Paused = Menu }
            .ForState<MenuState>(Menu)
            .ForState<PlayingState>(Gameplay);
}
```

Code asks for a mode and keeps the handle while it needs it, like a pause request:

```csharp
var handle = inputModes.Request(InputModes.Inventory);
// ...
handle.Dispose();
```

Which mode is active:

1. The newest live request.
2. With none alive, the current game state's mode (`ForState`). Boot is always `InputMode.None`, which enables no map;
   `ForState<BootState>` throws. A registered game state without a mode fails when the root scope is built.
3. While the game is paused, `InputOptions.Paused` sits above every request made before the pause, so they come back
   on resume. Requests made during the pause (a settings screen, a rebind) go above it.

While `ISceneLoader` replaces the scenes (see Core.md), no mode is active, whatever is requested meanwhile: nothing takes
input under the loading screen. Requests made during the load (the new scene's `Start`, a platform overlay's pause) keep
their order and apply when it ends. The load ends after the new game state is entered, so the previous state's mode never
comes back in between.

Handles are released in any order: closing a dialog that opened before the pause menu removes only the dialog's
request. Disposing a handle twice is harmless. `InputModeChanged {Previous, Current}` is published when the active mode
changes, not on every request. `IInputModeService.Requests` lists the live requests for debugging.

A mode that lists a map the asset does not have throws: the paused mode and the game state modes when the root scope is
built, any other mode when it is first requested. Map names are case-sensitive. A player mode (a state class) requests
its input mode in `Enter` and disposes the handle in `Exit`. A request or a release made from inside an input callback or
an `InputModeChanged` handler is applied once the current change is done, and its own `InputModeChanged` follows.

### Rules

- Nothing else enables or disables a map or an action, and nothing else writes `Cursor.lockState` or `Cursor.visible`.
  Validation reports such calls in project code (see below).
- There are no always-on maps. A map every mode needs (screenshots, a debug key) is listed in each mode.
- Every time the mode changes, the maps of the new mode are enabled in full and every other map is disabled. An action
  that some code disabled on its own comes back; never use a disabled action as a guard.
- Input callbacks need no pause or mode checks: the gameplay map is off while paused.
- `canceled` always arrives, also when a mode change disables the action. Reset "held" state there.
- Discrete commands (jump, pause, submit) are Button actions. Continuous input (move, look) is a Value action.
  Pass-through is for pointer input.
- Two enabled actions bound to the same control both perform on one press, whatever map they are in. A key that opens a
  screen should not also close it while both maps are on.

In the Editor and development builds, the service reports every action that was enabled or disabled behind its back
when it next applies a mode, for example by a PlayerInput or by switching the UI input module off and on.

## Reading input

Resolve actions once, in a constructor, with `throwIfNotFound`, so a renamed action fails when the scope is built. Keep
the names in one place:

```csharp
public sealed class GameplayInput
{
    public readonly InputAction Move;
    public readonly InputAction Pause;

    public GameplayInput(InputActionAsset actions)
    {
        Move = actions.FindAction("Gameplay/Move", throwIfNotFound: true);
        Pause = actions.FindAction("Gameplay/Pause", throwIfNotFound: true);
    }
}
```

A component can use an `InputActionReference` field instead; it refers to the action by id and survives renames.

Callbacks go into a `DisposableBag` like message subscriptions:

```csharp
var bag = DisposableBag.CreateBuilder();
input.Pause.Subscribe(_ => OpenPauseMenu()).AddTo(bag);
input.Move.Subscribe(OnMove, canceled: _ => StopMoving()).AddTo(bag);
_subscriptions = bag.Build();
```

Polling (`ReadValue`, `WasPressedThisFrame`) works too.

## Held buttons across a mode change

The press that changes the mode never triggers a Button action of the new mode. When the service enables a map, a Button
action whose control is still held is kept disabled until that control is released, then enabled. Pass-through actions
(pointer input) are not held back.

Without this, an analog trigger leaks: a Button action enabled while its trigger is held is performed by the trigger's
next change past the press point, which happens as the player lets go. Keys and mouse buttons only change back to zero,
so they do not leak, but they are held back the same way. Value actions are never held back: holding a direction while
the pause menu closes keeps the character moving.

## Control schemes

`IControlSchemeTracker.Current` is the name of the control scheme of the actions asset the player is using, from the
device that last performed an action. `ControlSchemeChanged {Previous, Current}` is published when it changes;
switching between the keyboard and the mouse, or between two gamepads, publishes nothing. Before any input, it is the
first scheme whose devices are all connected. Button prompts read `Current` once when they start, then follow the
message; `Device` tells which gamepad it is.

Devices created by scripts never change the scheme. On-screen controls create a virtual gamepad and a virtual mouse
creates a mouse; both stand in for the device the player really uses, the touchscreen or the gamepad. The Device
Simulator's and Unity Remote's touchscreens are created by scripts too: they never select the Touch scheme, which needs
a real touchscreen. Only enabled actions count: in a mode with no action bound to the mouse, moving it does not switch
back to the keyboard and mouse scheme.

## Cursor

| `CursorState` | Cursor |
|---|---|
| `Free` | Visible and unlocked. Hidden while the current scheme has no mouse (gamepad, touch). |
| `Hidden` | Hidden, unlocked. |
| `Locked` | Hidden and locked to the window centre, for mouse look. |

The service applies the mode's cursor whenever the mode or the scheme changes, and frees it when the root scope is
disposed.

`IInputModeService.ConfineCursor` keeps a free or hidden cursor inside the game window (`CursorLockMode.Confined`); a
locked cursor is unaffected. It takes effect on Windows and Linux. The Settings module sets it from the player's display
settings and saves it (see Settings.md).

## Rebinding

```csharp
var result = await rebind.Rebind(action, bindingIndex, cancellation);
if (result == RebindResult.Completed) { /* refresh the row */ }

rebind.Reset(action, bindingIndex);
rebind.ResetAll();
await save.SaveGlobal(); // when the settings screen closes
```

- While the player picks a control, no map is enabled, so the control triggers nothing.
- Only devices of the binding's control scheme are accepted: a key cannot replace a gamepad binding.
- Escape and Start cancel (`Canceled`) and can never be bound. The cancelling press does not reach the actions bound to
  it when the maps come back. Pointer movement never completes a rebind. After 10 seconds without input it gives up
  (`TimedOut`). `InputOptions.Rebind` changes these.
- Cancelling the token throws `OperationCanceledException`; it may be cancelled from any thread. Disposing the root scope
  ends a rebind that is still waiting.
- A composite (a WASD group) is rebound one part at a time; `Reset` on the composite clears all of its parts.
- `BindingsChanged {Action}` is published after a rebind or a reset, and with a null action after the saved bindings
  are restored. After a rebind it comes once the maps are back, so a handler can start the next rebind.
- Any binding change makes the Input System re-resolve every map of the asset. Which actions are enabled is kept, so a
  map the mode disabled stays off; actions in progress are cancelled (their `canceled` arrives) and a held Button waits
  for a new press.

Overrides are saved in the global document under `input.bindings`, matched by binding id. They follow the player to
other devices with the gameplay settings; keyboard bindings name physical key positions, so they mean the same key on
another machine. Suspending or quitting writes them if they changed.

When the actions asset changes in an update:

- An override of a binding that no longer exists is dropped, with a warning in the Editor and in builds that show logs.
  Deleting a binding and creating it again gives it a new id, so edit a binding's path in place instead.
- Moving an action to another map keeps its overrides: binding ids do not change.
- A changed default path does not affect a player who rebound that binding.

## Touch

The Touch control scheme ships with the asset, and the UI map's pointer bindings work on touchscreens. Gameplay touch
controls (on-screen sticks and buttons, gestures) belong to each game.

## UI input

The root prefab's `EventSystem` is the only one, so its UI input module is enabled once for the whole app. A module
enables the actions it uses whenever it is enabled; one per scene would turn the UI map on behind the service at every
scene load. To block UI input, request a mode without the UI map; do not disable the module.

UI Toolkit panels use the EventSystem when there is one. Without it, Unity routes UI Toolkit input through its own
default actions, which ignore modes and rebinds.

## Validation

| Check | Reports |
|---|---|
| `InputSetupValidator` | Project-wide Actions assigned; update mode Process Events In Fixed Update (no input while paused); the root prefab without exactly one EventSystem with an `InputSystemUIInputModule`, another input module, a module whose actions asset differs from the root scope's or whose UI actions are missing or from another asset; an EventSystem or input module in a scene or another prefab; a `PlayerInput` or `PlayerInputManager` in a scene or any prefab (other prefabs are checked by **Validate All** and the build check, not on every Play) |
| `DirectInputToggleValidator` | Project code that calls `Enable`/`Disable` on an action, map, asset or `IInputActionCollection` (a generated class's own `Enable()` is reported through the asset call inside it), `PlayerInput.SwitchCurrentActionMap`, `ActivateInput` or `DeactivateInput`, or writes `Cursor.lockState`/`visible` |

`DirectInputToggleValidator` reads the compiled code of the project's assemblies (tests and editor code excluded), so
it finds the exact method each call resolves to. Code with a deliberate exception, such as a debug console with its own
standalone action, is marked `[InputModeExempt("reason")]`.

## Rendering Debugger

URP's Rendering Debugger enables its own "Debug Menu" actions in the Editor and in Development builds (Ctrl+Backspace,
or both sticks pressed). They belong to URP, not to the game's actions asset, so input modes do not cover them. Release
builds do not contain them.

## Not included

Local multiplayer (PlayerInput, InputUser), gameplay touch controls, binding conflict warnings, pausing when a gamepad
disconnects, and button prompt icons.

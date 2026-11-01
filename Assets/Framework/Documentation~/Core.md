# Core

The core every game starts from: dependency injection scopes, game flow, scene changes, pause, game time, a boot gate,
suspend and quit notification, object pools, tween rules, logging and validation. Built on VContainer, MessagePipe, UniTask
and PrimeTween.

## Layout

| Folder | Contents |
|---|---|
| `Assets/Framework/` | Framework code. `AF.*` namespaces and assemblies, one assembly per module plus its editor and test assemblies. |
| `Assets/Game/` | The game. `Game` assembly; its namespace is renamed per game. Holds the root scope prefab, `VContainerSettings` and the scenes. |

Framework code never references the game. A fix made inside one game is brought back to the template by comparing a
single folder.

Game code goes under `Assets/Game/` in the `Game` assembly, which already references the framework's runtime assemblies.
Editor scripts of the game go in a `Game.Editor` assembly, created with the first one.

## Scopes

| Scope | Lives | Zenject equivalent |
|---|---|---|
| `RootScope` | Whole app, created once | ProjectContext |
| `SceneScope` | One per scene, child of the root | SceneContext |

The game derives `GameRootScope` from `RootScope` and puts it on `Assets/Game/Prefabs/RootScope.prefab`.
`Assets/Game/Settings/VContainerSettings.asset` points at that prefab and is listed in Player Settings > Preloaded
Assets. VContainer then instantiates the root once, before the first scene's scopes are built, and keeps it alive across
scene loads. There is no bootstrap scene: Play works from any scene.

Every scene has exactly one `SceneScope`. Its **Boot Target** (Menu or Playing) is the state the game enters when Play
starts from that scene; **Manual** leaves it to the scene itself (an intro, see Scenes). Once the game has left Boot,
later scenes ignore it.

Registrations go in `GameRootScope.ConfigureGame` (app-wide) or in a `SceneScope` subclass's `ConfigureScene`
(scene-only). Both receive the `MessagePipeOptions` needed to register message types.

To add a scene:

1. Create it and add a GameObject with a `SceneScope` component. A scene with services of its own uses a subclass of
   `SceneScope` instead and registers them in `ConfigureScene`.
2. Set its Boot Target, add it to the build's scene list, and add its name to `Assets/Game/Scripts/Scenes.cs`.

Do not end a scope's file name with `LifetimeScope`: when such a script is created in the Editor, VContainer replaces
its contents with its own template.

### Rules

- Bind and inject through interfaces. A class takes its dependencies in a single constructor; a MonoBehaviour in a single
  `[Inject] Construct` method.
- A class implementing `ITickable`, `IStartable`, `IAsyncStartable` or another entry-point interface is registered with
  `builder.RegisterEntryPoint<T>()`. Registered with `.As<IFoo>()` only, VContainer never calls it. In the Editor and
  development builds every `RootScope` and `SceneScope` reports such registrations as errors when it is built.
- VContainer does not inject scene objects on its own. A component with `[Inject]` members must either be a serialized
  field of the scope, registered with `builder.RegisterComponent(field)`, or sit under a GameObject listed in the scope's
  **Auto Inject Game Objects**. Do not use `RegisterComponentInHierarchy`: validation cannot see it. In the Editor and
  development builds, a scope reports a serialized field whose component has `[Inject]` members but was never
  registered as an error when it is built.
- Copies of GameObjects and components are made with `IObjectResolver.Instantiate` or a pool (see Pools), which inject
  them. Which prefab code copies cannot be known before it runs, so validation reports every `Object.Instantiate` or
  `InstantiateAsync` of a GameObject or component, every `AddComponent` of a component with `[Inject]` members, and
  every `AddComponent(Type)`, whose type is only known at run time, in project code. Materials and ScriptableObjects may
  be copied freely. Mark a deliberate exception `[InstantiateExempt("reason")]`.
- A service registered once in the root with `Lifetime.Scoped` gives every scope that resolves it its own instance, built
  with that scope's registrations and disposed with it (`IPools` and the Audio module's `ISounds` work this way). Such a
  service is never an entry point: VContainer runs the entry points of the registering scope only, so the instances of
  other scopes would never run. Scope builds report it.

### Scene scope extensions

A framework module that needs something in every scene registers an `ISceneScopeExtension` in the root scope, like an
installer the root applies to each scene. Every `SceneScope` calls the extensions while it is configured, before its own
registrations and entry points, so a build callback added there runs before the scene's `IInitializable`s (but after
Auto Inject Game Objects are injected). The Save module uses one to link each scene's savables.

## Messages

MessagePipe's `IPublisher<T>` / `ISubscriber<T>` work like Zenject's `SignalBus.Fire` / `Subscribe`.

- Each message type is registered explicitly by the feature that owns it:
  `builder.RegisterMessageBroker<T>(messagePipe)`. There is no automatic registration (reflection-made generics can fail
  on IL2CPP for struct messages). An unregistered type fails loudly when resolved.
- Messages are `readonly struct`s. A "changed" message is published after the owning service has stored the new state,
  and it carries the new value.
- A subscriber that mirrors a state (UI visibility, music) reads the current value from the service once when it
  starts, then follows the messages. A subscriber that only reacts to transitions does not need to.
- Subscribe in the constructor or `Initialize`; publish in `Start` at the earliest. Dispose subscriptions, typically
  with a `DisposableBag`.
- Commands and "changed" notifications are separate message types. UI listens to "changed" only.
- Buffered publishers and subscribers are not used: for struct messages, a buffered subscriber receives `default(T)` on
  subscribe even if nothing was ever published.
- A synchronous handler that throws stops the publish and the exception reaches the publisher. That is intended: fix
  the handler. Exceptions from async handlers that nobody awaits reach `Log.Exception`.

## Game flow

`IGameManager` runs one `IGameState` at a time. The framework registers `BootState`, `MenuState` and `PlayingState`.

```csharp
await gameManager.GoTo<PlayingState>();
```

`GoTo` exits the current state, enters the next one, then publishes `GameStateChanged { Previous, Current }`. Going to
the current state does nothing. An unregistered state, or a `GoTo` while a transition is still running, throws
`InvalidOperationException`. To add a state, derive from `GameState` and register it with
`builder.Register<MyState>(Lifetime.Singleton).As<IGameState>()`.

If the current state's `Exit` throws, the game stays in it and nothing is published. If the next state's `Enter` throws,
the game is in the next state anyway: `GameStateChanged` is still published, so subscribers follow `Current`, then the
exception reaches the caller.

States do not load scenes. A change that needs another scene goes through `ISceneLoader.Load<TState>`, which changes the
state and the scenes together (see Scenes).

Pause is not a game state; the game stays in `PlayingState` while paused.

## Scenes

`ISceneLoader` changes scenes. Scenes are named as in the build's scene list; the game keeps the names in one place
(`Assets/Game/Scripts/Scenes.cs`).

```csharp
await scenes.Load<PlayingState>(Scenes.Gameplay); // menu to game: enters PlayingState, replaces every scene
await scenes.Load("Level2");                       // next level, same state
await scenes.Load(scenes.Current);                 // the same level again
await scenes.Load<MenuState>(Scenes.Menu);         // back to the main menu
```

`Load` replaces every loaded scene, in this order:

1. `IsLoading` turns on and `SceneLoadingChanged` is published: no input mode is active from here on (see Input.md).
2. The loading screen covers the old scene.
3. `Load<TState>` only: the game state changes. The old state exits while its scene still exists, and the new scene's
   entry points start in the new state.
4. Every old scene unloads: its scope is disposed and its savables are captured.
5. The new scene loads and its scope is built.
6. The loading screen uncovers it. `IsLoading` turns off and input follows the new state.

The old scopes are gone before the new scene wakes up, so reloading a scene never registers the same savable key twice,
whatever order Unity uses. A reloaded scene's savables come back with the state captured when it unloaded; to go back to
the last save, call `LoadSlot` with the active slot before the load.

One scene operation runs at a time; another call meanwhile throws `InvalidOperationException`. A scene that is not in
the build throws `ArgumentException` before anything starts. If a load fails before the old scenes unload (the loading
screen or the state change throws), the screen uncovers them again; once they are gone, it stays up and another `Load`
recovers. Either way input comes back and the exception reaches the caller. Quitting, or stopping Play, during a load
ends it: the awaited `Load` throws `OperationCanceledException`, which is not logged.

The methods take no cancellation token: a scene change cancelled halfway leaves no consistent state, and the token of a
button in the old scene would cancel it in the middle. Code in the scene being replaced is destroyed while it awaits the
load; it must not touch itself afterwards.

Do not call `SceneManager.LoadScene`, `LoadSceneAsync`, `UnloadSceneAsync` or `UnloadScene` directly: that skips the
input block, the loading screen and the unload order. Validation reports such calls; mark a deliberate exception
`[SceneLoadExempt("reason")]`.

### Loading screen

The game implements `ILoadingScreen` (`Show`, `SetProgress`, `Hide`) on a component under the root prefab, so it
survives scene changes, and registers it in `ConfigureGame`:

```csharp
[SerializeField, Required] private LoadingScreen _loadingScreen;

builder.RegisterComponent(_loadingScreen).As<ILoadingScreen>();
```

At most one may be registered; without one, scenes change without a screen. `SetProgress` receives the new scene's
progress, 0 to 1, every frame while it loads. The game may be paused during a load: animate with unscaled time, and await
tweens with `ToUniTask(cancellation)` (see Tweens).

### Additive scenes

```csharp
await scenes.LoadAdditive("Level1-Hud");
await scenes.UnloadAdditive("Level1-Hud");
```

An additive scene loads next to the main one, without the loading screen or an input block. Its `SceneScope` is a child
of the main scene's scope, so it can inject the main scene's services. It still has exactly one `SceneScope`; its Boot
Target is ignored. `Load` unloads it with the rest, before the main scene.

While an additive load runs, any scope that wakes up without a parent of its own (a prefab with its own scope,
instantiated by root code) also becomes a child of the main scene's scope. Avoid creating such scopes during an additive
load.

### Intro

The template's first build scene, `Intro`, has an `IntroScope` with Boot Target **Manual**: it does not leave Boot on its
own. Its `IntroFlow` waits for the intro's minimum length and for the boot steps, which run meanwhile, then calls
`Load<MenuState>(Scenes.Menu)`. A real intro plays its logos or video in place of the wait. Pressing Play in another
scene in the Editor skips the intro: that scene's Boot Target applies.

## Pause and game time

Pause is requested, not toggled. Every source holds its own handle; the game stays paused while any handle is alive.

```csharp
var handle = pause.Request("pause-menu");
// ...
handle.Dispose(); // resumes only if no other request is alive
```

`PauseChanged` is published when the game goes from running to paused and back. `IPauseService.ActiveReasons` lists who
is holding the game paused.

`IGameClock.Time` is game time that does not advance while paused. Compare against it instead of caching
`UnityEngine.Time.time`. The clock is the only writer of `Time.timeScale`: 0 while paused, otherwise
`IGameClock.Scale`. Use `Scale` for slow motion; never write `Time.timeScale` directly. UI that animates while paused
uses unscaled time.

While the game is paused, the Input module enables the maps of its paused mode instead of the gameplay ones (see
Input.md), so input callbacks need no pause checks.

## Pools

`IPools` hands out prefab pools, like the MemoryPools a Zenject installer binds. Every scope resolves its own: a scene's
objects and services get the scene's pools, root services get the root's. A pool injects its instances with the
registrations of its scope.

```csharp
[SerializeField, Required] private Bullet _bulletPrefab;

var bullets = pools.For(_bulletPrefab);              // one pool per prefab and component type in this scope
var bullet = bullets.Spawn(muzzle.position, muzzle.rotation);
bullets.Despawn(bullet);
```

Waiting instances sit inactive under `[Pools]/<prefab name>`, below the scope's GameObject. When the scope ends (its scene
unloads), its pools destroy every instance they created, spawned ones included, wherever they were moved. An instance of
a root pool that was parented under a scene object dies with that scene: the pool drops it and logs a warning. Despawn it
before the scene goes, or take it from the scene's pools.

### An instance's life

| Step | What runs |
|---|---|
| Created (first spawn, or `Prewarm`) | Copied under the inactive container and injected: `[Inject] Construct` runs before `Awake`. Nothing else runs yet, whether the prefab root is active or not. |
| `Spawn` | Moved to its parent and position, a new life begins, the object is activated (`Awake` the first time, `OnEnable`; under an inactive parent they wait until the parent is active), then `IPoolable.OnSpawned(life)`. |
| `Despawn` | The life's token is cancelled, `IPoolable.OnDespawned()`, the object is deactivated (`OnDisable`), and it goes back under the container. |

Despawning an instance that is already back in its pool throws; despawning one that was destroyed meanwhile, or whose
despawn is still pending, does nothing. A despawn asked for from the instance's own `Awake` or `OnEnable` takes effect
once the activation ends, and its `OnSpawned` never runs. Do not despawn from `OnDisable`: Unity refuses to move an
object while it is being deactivated, so "return to the pool when disabled" does not work; the scope's end takes care of
instances in an unloading scene.

- Subscribe in `OnEnable` and unsubscribe in `OnDisable`. A subscription made in `Construct` outlives every despawn.
- Reset state and toggle children in `IPoolable.OnSpawned` / `OnDespawned`: Unity ignores `SetActive` on children from
  inside `OnEnable` or `OnDisable`.
- Pass the life's token (`life.Token`) to every await and tween of that life. Despawning cancels it: UniTask awaits end at
  once; tweens stop at PrimeTween's next update, before they write a value, and their awaits throw then. By that time the
  instance may already be in its next life (the pool hands out the last despawned instance first), so code that runs
  after an await, in a `catch` or `finally` included, checks `life.IsCurrent` before touching the instance.
- Where a reference may outlive the life (a target another object follows, a timer that despawns later), hold the
  `PoolLife` (`PooledObject.Of(instance).Life`) rather than the instance: `IsCurrent` turns false and `Despawn()` does
  nothing once that life is over, even after the instance is spawned again.
- Do not let a tween's completion be the only way back to the pool; if the tween is stopped, the instance never returns.

In the Editor and development builds, spawning an instance whose previous life still runs a tween is reported as an
error. The pool is Unity's `ObjectPool<T>` underneath and keeps up to 10000 waiting instances per prefab.

## Tweens

Tweens use PrimeTween.

- Await a tween or sequence with `ToUniTask(cancellation)`. Cancelling stops the tween where it is and the await throws
  `OperationCanceledException`. PrimeTween's own await returns normally after `Stop()`, and when the token was cancelled
  before the await resumed: the code after it would run as if the animation had finished. Validation reports any other
  await of a tween in project code (`await tween`, `ToUniTask()`, the conversion to `UniTask`, `ToYieldInstruction`, a
  tween used as an `IEnumerator` such as `yield return tween`, and any other conversion of a tween to an interface or
  `object`). Mark a deliberate exception `[TweenAwaitExempt("reason")]`.
- Stop an awaited tween through its token, never with `Stop()`. Leave `PrimeTweenConfig.defaultCancellationMode` at Stop:
  with Complete, a cancelled tween jumps to its end value.
- Bind every tween on a pooled object to its life: `.SetCancellationToken(life.Token)`, or `.ToUniTask(life.Token)` when it
  is awaited; never both, PrimeTween takes one token per tween (see Pools).
- After an await or in `OnComplete`, check that what the tween was for is still yours before changing state:

```csharp
public async UniTask Close(CancellationToken cancellation)
{
    var opening = _openings;
    await Tween.Alpha(_group, 0f, 0.25f, useUnscaledTime: true).ToUniTask(cancellation);
    if (opening != _openings)
        return; // opened again meanwhile: hiding it now would hide the new opening
    _group.gameObject.SetActive(false);
}
```

- Tweens that run while the game is paused (interface, loading screen) pass `useUnscaledTime: true`. Gameplay tweens stay
  scaled and freeze with the game.
- A tween with a target stops on its own when the target is destroyed. One without (`Tween.Delay`, `Tween.Custom` without
  a target, sequence callbacks) started by scene code gets a token that ends with the scene: a component's
  `destroyCancellationToken`, or the token an entry point's `StartAsync` receives.

## Boot

The root scope is built synchronously. Asynchronous startup work (loading the save data, which applies the settings;
the language's tables; platform services) implements `IBootStep` and is registered in the root scope with
`builder.Register<T>(Lifetime.Singleton).As<IBootStep>()`. Steps run one after another in registration order.
Code that needs them done awaits `IAppBoot.WaitReady(ct)`. A failing step is logged once and its exception is rethrown to
every waiter; the game stays in Boot.

## Suspend and quit

The root scope publishes `AppSuspending` when the app goes to the background (mobile `OnApplicationPause(true)`) and
when it quits (`Application.quitting`, which in the Editor also fires when Play stops). While Player Settings > Run In
Background is off (the template's setting), Unity's documentation says `OnApplicationPause(true)` also comes when Play
mode in the Editor loses focus, and when a desktop player's window does if it is smaller than the screen: switching away
from the Editor during Play, or from a windowed game, is a suspend too. The process may be frozen or killed right after,
so a handler finishes its work synchronously; nothing it awaits is sure to complete. The Save module writes changed data
here.

## Logging

| Call | Compiled |
|---|---|
| `Log.Info`, `Log.Warning` | Editor, and builds that define `SHOW_LOGS` (Development builds from **AF > Build**, see Build.md) |
| `Log.Error`, `Log.Exception` | Always |

Exceptions from entry points and unobserved UniTask exceptions are routed to `Log.Exception`. An entry point's
`OperationCanceledException` whose token was cancelled (Play stopped, a scene unloaded while its async work ran) is the
normal end of that work and is not logged; one without a cancelled token is.

## Validation

**AF > Validate All** checks the project and every enabled build scene. Closed scenes are opened additively and closed
again; open scenes are checked as they are in memory. When Play is pressed, the open scenes are checked too; errors are
logged and Play is not blocked.

| Validator | Reports |
|---|---|
| `ScopeSetupValidator` | Missing VContainerSettings or root prefab, more than one VContainerSettings, a root prefab that does not derive from `RootScope`; a scene without exactly one `SceneScope`; a `SceneScope` with a parent set or Auto Run off; a `RootScope` inside a scene |
| `InjectRegistrationValidator` | Components with `[Inject]` members that no scope registers (scenes and the root prefab) |
| `ReferenceValidator` | Missing scripts, references to deleted objects, empty `[Required]` fields |
| `DirectSceneLoadValidator` | Project code that loads or unloads scenes through `SceneManager` instead of `ISceneLoader` |
| `DirectInstantiateValidator` | Project code that copies GameObjects or components with `Object.Instantiate`, adds a component with `[Inject]` members, or adds one by `Type` |
| `AwaitedTweenValidator` | Project code that awaits a tween other than through `ToUniTask(cancellation)` |

Mark a serialized object reference that must be assigned with `[Required]`. To add a check, implement `IValidator` in
an editor assembly; it is picked up automatically. Checks too slow for every Play (loading every prefab) go in
`ValidateAssets`, which only **Validate All** and the build check run. Modules add their own the same way (Save:
`SaveValidator`; Input: `InputSetupValidator`, `DirectInputToggleValidator`; Audio: `AudioValidator`; Settings:
`DirectDisplayWriteValidator`; Localization: `LocalizationValidator`, `DirectLocaleChangeValidator`).

`RegistrationAudit` reads a builder's registrations before it is built. The entry point check uses it, so does the Save
module's savable check, and a module can use it (`FindMissingContract`) to report types registered without the contract
it relies on. `DirectCallScan` reads the compiled method bodies of the project's assemblies (tests and editor code
excluded) and reports calls to methods only one service may call, and value types converted to an interface; the
direct-call validators use it. It reads each method once per domain reload.

## Utilities

`IsAlive()` is a null check that also sees destroyed Unity objects. Use it for interface-typed fields that may hold a
component, where `!= null` stays true after the object is destroyed.

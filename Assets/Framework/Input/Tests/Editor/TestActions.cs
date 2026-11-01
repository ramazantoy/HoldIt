using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AF.Core;
using AF.Save;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Object = UnityEngine.Object;

namespace AF.Input.Tests
{
    /// <summary>A small actions asset built in code, shaped like the game's: a gameplay map and a UI map over two schemes.</summary>
    internal static class TestActions
    {
        public const string KeyboardMouseScheme = "Keyboard&Mouse";
        public const string GamepadScheme = "Gamepad";

        public static readonly InputMode Gameplay = new InputMode("gameplay", CursorState.Locked, "Gameplay");
        public static readonly InputMode Menu = new InputMode("menu", CursorState.Free, "UI");
        public static readonly InputMode Both = new InputMode("both", CursorState.Free, "Gameplay", "UI");
        public static readonly InputMode Settings = new InputMode("settings", CursorState.Free, "UI");

        public static InputOptions Options() =>
            new InputOptions { Paused = Menu }
                .ForState<MenuState>(Menu)
                .ForState<PlayingState>(Gameplay);

        public static InputActionAsset Create()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "TestActions";
            asset.AddControlScheme(KeyboardMouseScheme).WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>");
            asset.AddControlScheme(GamepadScheme).WithRequiredDevice("<Gamepad>");

            var gameplay = asset.AddActionMap("Gameplay");
            var jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space", groups: KeyboardMouseScheme);
            jump.AddBinding("<Gamepad>/buttonSouth", groups: GamepadScheme);
            gameplay.AddAction("Fire", InputActionType.Button, "<Gamepad>/rightTrigger", groups: GamepadScheme);
            var pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape", groups: KeyboardMouseScheme);
            pause.AddBinding("<Gamepad>/start", groups: GamepadScheme);
            var move = gameplay.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick", groups: GamepadScheme);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", groups: KeyboardMouseScheme)
                .With("Down", "<Keyboard>/s", groups: KeyboardMouseScheme)
                .With("Left", "<Keyboard>/a", groups: KeyboardMouseScheme)
                .With("Right", "<Keyboard>/d", groups: KeyboardMouseScheme);

            var ui = asset.AddActionMap("UI");
            ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", groups: KeyboardMouseScheme);
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", groups: KeyboardMouseScheme);
            ui.AddAction("Navigate", InputActionType.PassThrough, "<Gamepad>/dpad", groups: GamepadScheme);
            ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll", groups: KeyboardMouseScheme);
            // The trigger stands in for any analog control a UI button may be bound to; it is what leaks across a map switch.
            var submit = ui.AddAction("Submit", InputActionType.Button, "<Gamepad>/rightTrigger", groups: GamepadScheme);
            submit.AddBinding("<Keyboard>/enter", groups: KeyboardMouseScheme);
            ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape", groups: KeyboardMouseScheme);
            return asset;
        }
    }

    /// <summary>The root registrations the input mode service needs, without a LifetimeScope or the player loop.</summary>
    internal sealed class InputTestContext : IDisposable
    {
        public readonly InputActionAsset Actions = TestActions.Create();
        public readonly IObjectResolver Container;
        public readonly IGameManager Game;
        public readonly IPauseService Pause;
        public readonly TestSceneLoader Scenes;
        public readonly ControlSchemeTracker Schemes;
        public readonly InputModeService Modes;
        public readonly List<InputModeChanged> ModeChanges = new List<InputModeChanged>();
        public readonly List<BindingsChanged> BindingChanges = new List<BindingsChanged>();
        public (CursorLockMode lockMode, bool visible) Cursor;
        private readonly IDisposable _subscriptions;

        /// <param name="countsDevice">Which devices the control scheme tracker counts; null keeps its own rule (not created by script).</param>
        public InputTestContext(InputOptions options = null, Func<InputDevice, bool> countsDevice = null)
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<GameStateChanged>(messagePipe);
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.RegisterMessageBroker<SceneLoadingChanged>(messagePipe);
            builder.RegisterMessageBroker<InputModeChanged>(messagePipe);
            builder.RegisterMessageBroker<ControlSchemeChanged>(messagePipe);
            builder.RegisterMessageBroker<BindingsChanged>(messagePipe);
            builder.Register<BootState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<MenuState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<PlayingState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<IGameManager, GameManager>(Lifetime.Singleton);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            builder.RegisterInstance(Actions);
            if (countsDevice == null)
                builder.Register<ControlSchemeTracker>(Lifetime.Singleton).AsSelf().As<IControlSchemeTracker>();
            else
                builder.Register(resolver => new ControlSchemeTracker(Actions, resolver.Resolve<IPublisher<ControlSchemeChanged>>(), countsDevice),
                    Lifetime.Singleton).AsSelf().As<IControlSchemeTracker>();
            Container = builder.Build();

            Game = Container.Resolve<IGameManager>();
            Pause = Container.Resolve<IPauseService>();
            Scenes = new TestSceneLoader(Container.Resolve<IPublisher<SceneLoadingChanged>>());
            Schemes = Container.Resolve<ControlSchemeTracker>();
            Modes = CreateService(options ?? TestActions.Options());

            var bag = DisposableBag.CreateBuilder();
            Container.Resolve<ISubscriber<InputModeChanged>>().Subscribe(ModeChanges.Add).AddTo(bag);
            Container.Resolve<ISubscriber<BindingsChanged>>().Subscribe(BindingChanges.Add).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public InputModeService CreateService(InputOptions options) => new InputModeService(Actions, options,
            Container.Resolve<IReadOnlyList<IGameState>>(), Game, Pause, Scenes, Schemes,
            Container.Resolve<IPublisher<InputModeChanged>>(), Container.Resolve<ISubscriber<GameStateChanged>>(),
            Container.Resolve<ISubscriber<PauseChanged>>(), Container.Resolve<ISubscriber<SceneLoadingChanged>>(),
            Container.Resolve<ISubscriber<ControlSchemeChanged>>(), (lockMode, visible) => Cursor = (lockMode, visible));

        public RebindService CreateRebind(InputOptions options = null) =>
            new RebindService(Actions, Modes, options ?? TestActions.Options(), Container.Resolve<IPublisher<BindingsChanged>>());

        public InputAction Action(string path) => Actions.FindAction(path, throwIfNotFound: true);

        public void GoTo<TState>() where TState : IGameState => Game.GoTo<TState>().GetAwaiter().GetResult();

        /// <summary>Started and playing, so the gameplay mode is active.</summary>
        public InputTestContext Playing()
        {
            Modes.Start();
            GoTo<PlayingState>();
            return this;
        }

        public IEnumerable<string> EnabledActions() =>
            Actions.actionMaps.SelectMany(map => map.actions).Where(action => action.enabled).Select(action => $"{action.actionMap.name}/{action.name}");

        public void Dispose()
        {
            _subscriptions.Dispose();
            Modes.Dispose();
            Container.Dispose();
            Object.DestroyImmediate(Actions);
        }
    }

    /// <summary>Stands in for the scene loader: only its loading flag and message.</summary>
    internal sealed class TestSceneLoader : ISceneLoader
    {
        private readonly IPublisher<SceneLoadingChanged> _loadingChanged;

        public TestSceneLoader(IPublisher<SceneLoadingChanged> loadingChanged) => _loadingChanged = loadingChanged;

        public string Current => "Test";
        public bool IsLoading { get; private set; }

        public void SetLoading(bool loading)
        {
            IsLoading = loading;
            _loadingChanged.Publish(new SceneLoadingChanged(loading, "Test"));
        }

        public UniTask Load(string scene) => throw new NotSupportedException();
        public UniTask Load<TState>(string scene) where TState : IGameState => throw new NotSupportedException();
        public UniTask LoadAdditive(string scene) => throw new NotSupportedException();
        public UniTask UnloadAdditive(string scene) => throw new NotSupportedException();
    }

    internal sealed class Recorder<T> : IPublisher<T>
    {
        public readonly List<T> Messages = new List<T>();

        public void Publish(T message) => Messages.Add(message);
    }

    /// <summary>In-memory storage that runs inline, so save calls complete synchronously.</summary>
    internal sealed class MemoryStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Primary = new Dictionary<string, string>();

        public bool IsThreadSafe => false;

        public StorageRead Read(string entry) => new StorageRead(Primary.TryGetValue(entry, out var content) ? content : null, null);

        public void Write(string entry, string content) => Primary[entry] = content;

        public void Quarantine(string entry, bool move) => throw new IOException("Not expected in these tests.");

        public IReadOnlyList<string> List(string prefix) =>
            Primary.Keys.Where(entry => entry.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(entry => entry, StringComparer.Ordinal).ToList();

        public void Delete(string entry) => Primary.Remove(entry);
    }
}

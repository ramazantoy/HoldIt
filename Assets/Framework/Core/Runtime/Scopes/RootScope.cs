using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AF.Core
{
    /// <summary>
    /// Project-wide scope, the VContainer counterpart of Zenject's ProjectContext. The game derives one class from it, puts it
    /// on a prefab and assigns that prefab to VContainerSettings, which is in Preloaded Assets. VContainer then creates it once,
    /// before the first scene's scopes, whichever scene Play starts from.
    /// </summary>
    public abstract class RootScope : LifetimeScope
    {
        private IPublisher<AppSuspending> _suspending;

        protected sealed override void Configure(IContainerBuilder builder)
        {
            // Must come before any RegisterEntryPoint: the first one registers VContainer's default handler if none exists.
            builder.RegisterEntryPointExceptionHandler(EntryPointErrors.Report);

            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<GameStateChanged>(messagePipe);
            builder.RegisterMessageBroker<PauseChanged>(messagePipe);
            builder.RegisterMessageBroker<AppSuspending>(messagePipe);
            builder.RegisterMessageBroker<SceneLoadingChanged>(messagePipe);

            builder.Register<BootState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<MenuState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<PlayingState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<IGameManager, GameManager>(Lifetime.Singleton);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IPauseService, PauseService>(Lifetime.Singleton);
            // Scoped: every scope that resolves it gets its own pools, injected with its own registrations.
            builder.Register<IPools, Pools>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameClock>();
            builder.RegisterEntryPoint<AppBoot>();

            ConfigureGame(builder, messagePipe);

            EntryPointAudit.Attach(builder, this);
            ScopeFieldAudit.Attach(builder, this, autoInjectGameObjects);
            builder.RegisterBuildCallback(container =>
            {
                _suspending = container.Resolve<IPublisher<AppSuspending>>();
                Application.quitting += OnQuitting;
                Log.Info($"Root scope built: {name}", this);
            });
        }

        /// <summary>
        /// Game-wide registrations. Message types owned by a game feature are registered here with
        /// <c>builder.RegisterMessageBroker&lt;T&gt;(messagePipe)</c>.
        /// </summary>
        protected abstract void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe);

        protected override void OnDestroy()
        {
            Application.quitting -= OnQuitting;
            _suspending = null;
            base.OnDestroy();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                PublishSuspending(SuspendReason.Background);
        }

        private void OnQuitting() => PublishSuspending(SuspendReason.Quit);

        private void PublishSuspending(SuspendReason reason)
        {
            try
            {
                _suspending?.Publish(new AppSuspending(reason));
            }
            catch (Exception exception)
            {
                // Nobody awaits a Unity message; log here so the failure is not lost on the way out.
                Log.Exception(exception, this);
            }
        }
    }
}

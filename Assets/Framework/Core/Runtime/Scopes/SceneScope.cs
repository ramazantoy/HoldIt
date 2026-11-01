using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace AF.Core
{
    /// <summary>Which game state Play enters when it starts from this scene.</summary>
    public enum BootTarget
    {
        Menu,
        Playing,

        /// <summary>The scene leaves Boot itself, for example an intro that loads the menu when it ends.</summary>
        Manual,
    }

    /// <summary>
    /// Scene-level scope (Zenject's SceneContext), a child of the root scope. Every scene has exactly one.
    /// VContainer does not inject scene objects on its own: a component with [Inject] members must be a serialized field of a
    /// scope subclass registered with RegisterComponent, or sit under a GameObject listed in Auto Inject Game Objects.
    /// </summary>
    public class SceneScope : LifetimeScope
    {
        [SerializeField, FormerlySerializedAs("bootTarget"), Tooltip("Game state entered when Play starts from this scene. Manual: the scene leaves Boot itself (an intro). Ignored once the game has left Boot.")]
        private BootTarget _bootTarget = BootTarget.Playing;

        protected sealed override void Configure(IContainerBuilder builder)
        {
            if (Parent == null)
                throw new InvalidOperationException(
                    $"{name} has no root scope. Assign the root prefab to VContainerSettings and add the settings to Preloaded Assets.");

            // A child scope does not see the root's handler when VContainer registers its default one; register it here too.
            builder.RegisterEntryPointExceptionHandler(EntryPointErrors.Report);

            // Before the first RegisterEntryPoint, which adds the callback that runs the entry points: extension callbacks
            // then run before the scene's IInitializables.
            foreach (var extension in Parent.Container.Resolve<IReadOnlyList<ISceneScopeExtension>>())
                extension.Configure(builder, this);

            // An additive scene's scope is a child of the main scene's scope (ISceneLoader.LoadAdditive); leaving Boot is the
            // main scene's job.
            if (Parent is RootScope)
                builder.RegisterEntryPoint<SceneEntry>().WithParameter(_bootTarget);

            ConfigureScene(builder, Parent.Container.Resolve<MessagePipeOptions>());

            EntryPointAudit.Attach(builder, this);
            ScopeFieldAudit.Attach(builder, this, autoInjectGameObjects);
            builder.RegisterBuildCallback(_ => Log.Info($"Scene scope built: {gameObject.scene.name}", this));
        }

        protected override void OnDestroy()
        {
            var scene = gameObject.scene.name;
            base.OnDestroy();
            Log.Info($"Scene scope disposed: {scene}");
        }

        /// <summary>
        /// Scene registrations. Message types owned by this scene are registered here with
        /// <c>builder.RegisterMessageBroker&lt;T&gt;(messagePipe)</c>.
        /// </summary>
        protected virtual void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
        }
    }
}

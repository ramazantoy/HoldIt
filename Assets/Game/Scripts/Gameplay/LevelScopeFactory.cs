using System;
using AF.Core;
using HoldIt.Backdrop;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Drawing;
using HoldIt.Feedback;
using HoldIt.Levels;
using HoldIt.Pooling;
using HoldIt.Restrooms;
using HoldIt.Spawning;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// Builds a level's scope as a child of the gameplay scene's. What outlasts a level (the views, the camera, the messages,
    /// the feedback) stays in the parent; the level's own services and its loan of the root's pools live in the child.
    /// </summary>
    internal sealed class LevelScopeFactory : ILevelScopeFactory
    {
        private readonly LifetimeScope _parent;
        private readonly LevelKit _kit;

        public LevelScopeFactory(LifetimeScope parent, LevelKit kit)
        {
            _parent = parent;
            _kit = kit;
        }

        public IDisposable Create(LevelDefinition level)
        {
            // Resolved here, in the parent: the feedback is scoped, and the child would get one of its own whose sounds end with it.
            var feedback = _parent.Container.Resolve<IFeedback>();
            return _parent.CreateChild(builder => Install(builder, level, feedback), $"Level {level.name}");
        }

        private void Install(IContainerBuilder builder, LevelDefinition level, IFeedback feedback)
        {
            // A child scope does not see the root's handler when VContainer registers its default one.
            builder.RegisterEntryPointExceptionHandler(exception => Log.Exception(exception));
            builder.RegisterInstance(feedback);

            builder.UseBackdrop(_kit.BackdropConfig);
            builder.UseBoard(level.Map);
            builder.UseRestrooms(_kit.RestroomConfig, _kit.StallSet, level.Map.StallsPerRestroom);
            builder.UseLevelPools();
            builder.UseCharacters(_kit.CharacterConfig);
            builder.UseLevelRun(_kit.RunConfig, level);
            builder.UseSpawning(_kit.SpawnConfig);
            builder.UseDrawing(_kit.DrawingConfig);
            builder.RegisterEntryPoint<HudPresenter>();
            builder.RegisterEntryPoint<GameplayFlow>();
            builder.RegisterEntryPoint<LevelEndFlow>();
            builder.RegisterEntryPoint<TouchLog>();
        }
    }
}

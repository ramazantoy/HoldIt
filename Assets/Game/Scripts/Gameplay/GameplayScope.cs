using AF.Core;
using HoldIt.Board;
using HoldIt.Input;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>The gameplay scene's scope: pointer input, the board, the HUD, and for now a log of the presses.</summary>
    public sealed class GameplayScope : SceneScope
    {
        [SerializeField, Required] private GameplayHudView _hud;

        [SerializeField, Required, Tooltip("The map played in this scene, until there is a level choice.")]
        private MapDefinition _map;

        [SerializeField, Required] private BoardView _board;
        [SerializeField, Required] private BoardCamera _boardCamera;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.UsePointerInput(messagePipe);
            builder.UseBoard(_map, _board, _boardCamera);
            builder.RegisterComponent(_hud).As<IGameplayHudView, IBoardInsets>();
            builder.RegisterEntryPoint<GameplayFlow>();
            builder.RegisterEntryPoint<TouchLog>();
        }
    }
}

using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Drawing;
using HoldIt.Input;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay scene's scope: pointer input, the board, the characters, path drawing, the HUD, and for now a log of
    /// the presses. Editor and development builds also get debug characters, on the lobby and coming in (until spawning).
    /// </summary>
    public sealed class GameplayScope : SceneScope
    {
        [SerializeField, Required] private GameplayHudView _hud;

        [SerializeField, Required, Tooltip("The map played in this scene, until there is a level choice.")]
        private MapDefinition _map;

        [SerializeField, Required] private BoardView _board;
        [SerializeField, Required] private BoardCamera _boardCamera;
        [SerializeField, Required] private CharacterConfig _characterConfig;
        [SerializeField, Required] private CharacterSet _characterSet;
        [SerializeField, Required] private DrawingConfig _drawingConfig;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.UsePointerInput(messagePipe);
            builder.UseBoard(_map, _board, _boardCamera);
            builder.UseCharacters(messagePipe, _characterConfig, _characterSet);
            builder.UseDrawing(_drawingConfig);
            builder.RegisterComponent(_hud).As<IGameplayHudView, IBoardInsets>();
            builder.RegisterEntryPoint<GameplayFlow>();
            builder.RegisterEntryPoint<TouchLog>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterEntryPoint<DebugCrowd>();
            builder.RegisterEntryPoint<DebugArrivals>();
#endif
        }
    }
}

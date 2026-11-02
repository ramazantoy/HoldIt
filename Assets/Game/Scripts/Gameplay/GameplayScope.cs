using AF.Core;
using HoldIt.Input;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>The gameplay scene's scope: pointer input, the HUD, and for now a log of the presses.</summary>
    public sealed class GameplayScope : SceneScope
    {
        [SerializeField, Required] private GameplayHudView _hud;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            builder.UsePointerInput(messagePipe);
            builder.RegisterComponent(_hud).As<IGameplayHudView>();
            builder.RegisterEntryPoint<GameplayFlow>();
            builder.RegisterEntryPoint<TouchLog>();
        }
    }
}

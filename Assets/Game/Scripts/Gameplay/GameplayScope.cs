using AF.Core;
using HoldIt.Backdrop;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Drawing;
using HoldIt.Feedback;
using HoldIt.Input;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Settings;
using HoldIt.Spawning;
using HoldIt.UI;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay scene's scope: what outlasts a level (the HUD, the pause, settings and result screens, the camera,
    /// the messages, the feedback). The root's current level (<see cref="ILevelCursor"/>) is played in a scope of its own
    /// below this one (<see cref="LevelSession"/>): its map's board and restrooms, the characters it brings in, the run
    /// that keeps its score, path drawing, the flows, and for now a log of the presses.
    /// </summary>
    public sealed class GameplayScope : SceneScope
    {
        [SerializeField, Required] private GameplayHudView _hud;
        [SerializeField, Required] private WorldUiLayer _worldUi;
        [SerializeField, Required] private PauseView _pauseScreen;
        [SerializeField, Required] private SettingsView _settingsScreen;
        [SerializeField, Required] private LevelResultView _resultScreen;
        [SerializeField, Required] private BoardView _board;
        [SerializeField, Required] private BoardCamera _boardCamera;
        [SerializeField, Required] private BoardRevealConfig _revealConfig;
        [SerializeField, Required] private RestroomView _restroomView;
        [SerializeField, Required] private RestroomConfig _restroomConfig;
        [SerializeField, Required] private StallSet _stallSet;
        [SerializeField, Required] private CharacterConfig _characterConfig;
        [SerializeField, Required] private RunConfig _runConfig;
        [SerializeField, Required] private SpawnConfig _spawnConfig;
        [SerializeField, Required] private DrawingConfig _drawingConfig;
        [SerializeField, Required] private BackdropConfig _backdropConfig;

        [SerializeField, Tooltip("The music while playing; empty for none.")]
        private AudioClip _music;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the music is while playing, under the player's music volume.")]
        private float _musicLevel = 0.5f;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            // What outlasts a level: the input, the views, the camera, the messages and the feedback. The level's own
            // services are built per level (LevelScopeFactory), in a child of this scope.
            builder.UsePointerInput(messagePipe);
            builder.UseBoardFrame(_board, _boardCamera);
            builder.UseRestroomsFrame(messagePipe, _restroomView);
            builder.UseCharacterMessages(messagePipe);
            builder.UseLevelRunMessages(messagePipe);
            builder.RegisterComponent(_hud).As<IGameplayHudView, IBoardInsets>();
            builder.RegisterComponent(_worldUi).As<IWorldUiLayer>();
            builder.RegisterComponent(_pauseScreen).As<IPauseView>();
            builder.UseSettingsScreen(_settingsScreen);
            builder.RegisterComponent(_resultScreen).As<ILevelResultView>();
            builder.RegisterInstance<ILevelScopeFactory>(new LevelScopeFactory(this,
                new LevelKit(_restroomConfig, _stallSet, _characterConfig, _runConfig, _spawnConfig, _drawingConfig,
                    _backdropConfig)));
            builder.RegisterInstance<IBoardReveal>(new BoardReveal(_board, _restroomView, _revealConfig));
            builder.RegisterEntryPoint<LevelSession>().As<ILevelSession>();
            builder.UseFeedbackEvents();
            builder.UseSceneMusic(_music, _musicLevel);
        }
    }
}

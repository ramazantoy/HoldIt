using AF.Core;
using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Drawing;
using HoldIt.Feedback;
using HoldIt.Input;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Settings;
using HoldIt.Spawning;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The gameplay scene's scope. It plays the root's current level (<see cref="ILevelCursor"/>): its map's board and
    /// restrooms, the characters it brings in, the run that keeps its score, path drawing, the HUD, the pause, settings
    /// and result screens, and for now a log of the presses.
    /// </summary>
    public sealed class GameplayScope : SceneScope
    {
        [SerializeField, Required] private GameplayHudView _hud;
        [SerializeField, Required] private PauseView _pauseScreen;
        [SerializeField, Required] private SettingsView _settingsScreen;
        [SerializeField, Required] private LevelResultView _resultScreen;
        [SerializeField, Required] private BoardView _board;
        [SerializeField, Required] private BoardCamera _boardCamera;
        [SerializeField, Required] private RestroomView _restroomView;
        [SerializeField, Required] private RestroomConfig _restroomConfig;
        [SerializeField, Required] private StallSet _stallSet;
        [SerializeField, Required] private CharacterConfig _characterConfig;
        [SerializeField, Required] private CharacterSet _characterSet;
        [SerializeField, Required] private RunConfig _runConfig;
        [SerializeField, Required] private SpawnConfig _spawnConfig;
        [SerializeField, Required] private DrawingConfig _drawingConfig;

        [SerializeField, Tooltip("The music while playing; empty for none.")]
        private AudioClip _music;

        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            // The root scope is built before any scene's, and outlives it.
            var level = Parent.Container.Resolve<ILevelCursor>().Current;

            builder.UsePointerInput(messagePipe);
            builder.UseBoard(level.Map, _board, _boardCamera);
            builder.UseRestrooms(messagePipe, _restroomConfig, _stallSet, level.Map.StallsPerRestroom, _restroomView);
            builder.UseCharacters(messagePipe, _characterConfig, _characterSet);
            builder.UseLevelRun(messagePipe, _runConfig, level);
            builder.UseSpawning(_spawnConfig);
            builder.UseDrawing(_drawingConfig);
            builder.RegisterComponent(_hud).As<IGameplayHudView, IBoardInsets>();
            builder.RegisterComponent(_pauseScreen).As<IPauseView>();
            builder.UseSettingsScreen(_settingsScreen);
            builder.RegisterComponent(_resultScreen).As<ILevelResultView>();
            builder.RegisterEntryPoint<HudPresenter>();
            builder.RegisterEntryPoint<GameplayFlow>();
            builder.RegisterEntryPoint<LevelEndFlow>();
            builder.RegisterEntryPoint<TouchLog>();
            builder.UseFeedbackEvents();
            builder.UseSceneMusic(_music);
        }
    }
}

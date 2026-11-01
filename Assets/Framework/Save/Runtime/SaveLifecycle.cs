using System;
using AF.Core;
using MessagePipe;
using VContainer.Unity;

namespace AF.Save
{
    /// <summary>
    /// Connects the save service to the app: starts a dev session when gameplay begins without a slot, closes the session when
    /// the game returns to the menu, counts play time, and writes before the app is suspended or quits.
    /// </summary>
    internal sealed class SaveLifecycle : ITickable, IDisposable
    {
        private readonly SaveService _save;
        private readonly IGameManager _game;
        private readonly IPauseService _pause;
        private readonly ISceneLoader _scenes;
        private readonly IDisposable _subscriptions;

        public SaveLifecycle(SaveService save, IGameManager game, IPauseService pause, ISceneLoader scenes,
            ISubscriber<GameStateChanged> stateChanged, ISubscriber<AppSuspending> suspending)
        {
            _save = save;
            _game = game;
            _pause = pause;
            _scenes = scenes;

            var bag = DisposableBag.CreateBuilder();
            stateChanged.Subscribe(OnStateChanged).AddTo(bag);
            suspending.Subscribe(_ => save.FlushForSuspend()).AddTo(bag);
            _subscriptions = bag.Build();
        }

        public void Tick()
        {
            if (_save.HasSession && _game.Current is PlayingState && !_pause.IsPaused && !_scenes.IsLoading)
                _save.AddPlayTime(UnityEngine.Time.unscaledDeltaTime);
        }

        public void Dispose() => _subscriptions.Dispose();

        private void OnStateChanged(GameStateChanged change)
        {
            if (change.Current is PlayingState && !_save.HasSession)
                _save.StartDevSession();

            // The menu has no session: a slot left open would be written by the next suspend autosave, progress the player
            // left without saving. ISceneLoader enters the menu before the gameplay scene unloads, so nothing is captured
            // into the closed slot either.
            if (change.Current is MenuState && _save.HasSession)
            {
                // Throwing here would stop the publish before the other subscribers (input) see the new state.
                if (_save.IsLoadingSlot)
                {
                    Log.Error("Save: the game returned to the menu while a slot was still loading; the slot stays open. Await LoadSlot first.");
                    return;
                }
                _save.CloseSlot();
                Log.Info("Save: slot closed on returning to the menu");
            }
        }
    }
}

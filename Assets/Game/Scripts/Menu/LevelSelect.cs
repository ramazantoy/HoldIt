using System;
using HoldIt.Levels;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>
    /// Drives the level select screen: every level with its best stars, open or locked (<see cref="ILevelProgress"/>). A tap
    /// on an open level chooses it; a locked one does nothing.
    /// </summary>
    internal sealed class LevelSelect : ILevelSelect, IInitializable, IStartable, IDisposable
    {
        private readonly ILevelSelectView _view;
        private readonly ILevelProgress _progress;
        private readonly ILevelCursor _cursor;
        private readonly RunConfig _config;

        public LevelSelect(ILevelSelectView view, ILevelProgress progress, ILevelCursor cursor, RunConfig config)
        {
            _view = view;
            _progress = progress;
            _cursor = cursor;
            _config = config;
        }

        public bool IsOpen { get; private set; }

        public event Action Closed;
        public event Action<int> LevelChosen;

        public void Initialize()
        {
            _view.LevelClicked += Choose;
            _view.BackClicked += Close;
        }

        // In Start, not Initialize: the view sets itself up in Awake, after the scope is built.
        public void Start() => _view.Hide();

        public void Dispose()
        {
            _view.LevelClicked -= Choose;
            _view.BackClicked -= Close;
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            _view.Show(LevelSlots.Build(_progress, _cursor.Count), _config.Lives);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            _view.Hide();
            Closed?.Invoke();
        }

        private void Choose(int index)
        {
            if (IsOpen && index < _cursor.Count && _progress.IsUnlocked(index))
                LevelChosen?.Invoke(index);
        }
    }
}

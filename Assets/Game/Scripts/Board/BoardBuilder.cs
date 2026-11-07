using System;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Board
{
    /// <summary>Builds the level's board in the scene's <see cref="BoardView"/> as the level opens, and takes it down as the level ends.</summary>
    internal sealed class BoardBuilder : IInitializable, IDisposable
    {
        private readonly BoardView _view;
        private readonly ITileGrid _grid;
        private readonly IObjectResolver _resolver;

        public BoardBuilder(BoardView view, ITileGrid grid, IObjectResolver resolver)
        {
            _view = view;
            _grid = grid;
            _resolver = resolver;
        }

        public void Initialize() => _view.Build(_grid, _resolver);

        public void Dispose()
        {
            // The view goes with the scene when the game leaves the gameplay scene.
            if (_view != null)
                _view.Clear();
        }
    }
}

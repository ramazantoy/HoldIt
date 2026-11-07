using System.Collections.Generic;
using AF.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Board
{
    /// <summary>
    /// Builds a map's pieces (<see cref="BoardLayout"/>) from the tile set, in world space (<see cref="BoardSpace"/>: the
    /// board's centre at the origin), under a root of its own below this object. The view stays; a level builds its board
    /// (<see cref="Build"/>) and takes it down again (<see cref="Clear"/>). A built board is hidden (every piece at scale
    /// zero) until its <see cref="Pieces"/> are grown in.
    /// </summary>
    public sealed class BoardView : MonoBehaviour, IBoardBounds
    {
        private const string ROOT_NAME = "Board";

        [SerializeField, Required] private TileSet _tileSet;

        private readonly List<Revealable> _pieces = new List<Revealable>();
        private Transform _root;

        public bool IsBuilt { get; private set; }
        public ITileGrid Grid { get; private set; }
        public Bounds Bounds { get; private set; }

        /// <summary>The standing board's pieces, with the scale each has when shown.</summary>
        public IReadOnlyList<Revealable> Pieces => _pieces;

        /// <summary>Builds <paramref name="grid"/>'s board; the pieces are copied with <paramref name="resolver"/>.</summary>
        public void Build(ITileGrid grid, IObjectResolver resolver)
        {
            Clear();
            _root = new GameObject(ROOT_NAME).transform;
            _root.SetParent(transform, false);

            var space = grid.Space;
            foreach (var piece in BoardLayout.Build(grid))
            {
                var prefab = _tileSet.Prefab(piece);
                var instance = resolver.Instantiate(prefab, space.CellCenter(piece.Cell), piece.Facing.Rotation(), _root);
                instance.name = $"{prefab.name} {piece.Cell}";
                _pieces.Add(new Revealable(instance.transform, instance.transform.localScale));
            }

            var margin = 2f * _tileSet.BaseMargin;
            var slab = resolver.Instantiate(_tileSet.BaseSlab, new Vector3(0f, _tileSet.BaseTop, 0f), Quaternion.identity, _root);
            slab.transform.localScale = new Vector3(grid.Cols + margin, _tileSet.BaseDepth, grid.Rows + margin);
            _pieces.Add(new Revealable(slab.transform, slab.transform.localScale));

            Grid = grid;
            // The camera fits the board as it is shown, so the bounds are taken before the pieces shrink to nothing.
            Bounds = RendererBounds();
            IsBuilt = true;
            foreach (var piece in _pieces)
                piece.Transform.localScale = Vector3.zero;
        }

        /// <summary>Takes the board down. It is gone at the end of the frame.</summary>
        public void Clear()
        {
            IsBuilt = false;
            Grid = null;
            Bounds = default;
            _pieces.Clear();
            if (_root == null)
                return;
            // Out of the hierarchy at once, so nothing finds the old pieces while they wait to be destroyed.
            _root.SetParent(null, false);
            Destroy(_root.gameObject);
            _root = null;
        }

        private Bounds RendererBounds()
        {
            var renderers = _root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}

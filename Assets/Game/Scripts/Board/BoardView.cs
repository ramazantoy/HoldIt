using AF.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Board
{
    /// <summary>
    /// Builds the current map's pieces (<see cref="BoardLayout"/>) from the tile set, in world space (<see cref="BoardSpace"/>:
    /// the board's centre at the origin), under this object.
    /// </summary>
    public sealed class BoardView : MonoBehaviour, IBoardBounds
    {
        [SerializeField, Required] private TileSet _tileSet;

        private ITileGrid _grid;
        private IObjectResolver _resolver;

        public bool IsBuilt { get; private set; }
        public Bounds Bounds { get; private set; }

        [Inject]
        public void Construct(ITileGrid grid, IObjectResolver resolver)
        {
            _grid = grid;
            _resolver = resolver;
        }

        private void Start() => Build();

        private void Build()
        {
            var space = _grid.Space;
            foreach (var piece in BoardLayout.Build(_grid))
            {
                var prefab = _tileSet.Prefab(piece.Kind, piece.Gender);
                var instance = _resolver.Instantiate(prefab, space.CellCenter(piece.Cell), piece.Facing.Rotation(), transform);
                instance.name = $"{prefab.name} {piece.Cell}";
            }

            var margin = 2f * _tileSet.BaseMargin;
            var slab = _resolver.Instantiate(_tileSet.BaseSlab, new Vector3(0f, _tileSet.BaseTop, 0f), Quaternion.identity, transform);
            slab.transform.localScale = new Vector3(_grid.Cols + margin, _tileSet.BaseDepth, _grid.Rows + margin);

            Bounds = RendererBounds();
            IsBuilt = true;
        }

        private Bounds RendererBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}

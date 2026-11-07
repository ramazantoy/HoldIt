using AF.Core;
using HoldIt.Board;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// The bar over a character's head that fills as its patience runs out, from the left, green through yellow to red
    /// (<see cref="PatienceColor"/>). Its character's view tells it the patience every frame, or hides it. It faces the
    /// camera, after the view has turned (execution order). Part of the character prefab, under Anchor_Top; hidden whenever
    /// the character is spawned.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PatienceBar : MonoBehaviour, IPoolable
    {
        private const float EmptyScale = 1e-4f;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Required] private GameObject _visual;

        [SerializeField, Required, Tooltip("The fill's pivot, at its left end: its X scale is the patience used up.")]
        private Transform _fill;

        [SerializeField, Required] private Renderer _fillRenderer;

        private IBoardProjection _projection;
        private CharacterSet _set;
        private MaterialPropertyBlock _block;
        private float _patience;

        [Inject]
        public void Construct(CharacterSet set) => _set = set;

        /// <summary>The camera it faces, until it is despawned; its view hands it over.</summary>
        public void Bind(IBoardProjection projection) => _projection = projection;

        /// <param name="patience">Patience used up, from 0 to 1.</param>
        public void Show(float patience)
        {
            _patience = Mathf.Clamp01(patience);
            if (!_visual.activeSelf)
                _visual.SetActive(true);
        }

        public void Hide()
        {
            if (_visual.activeSelf)
                _visual.SetActive(false);
        }

        public void OnSpawned(PoolLife life) => Hide();

        public void OnDespawned()
        {
            Hide();
            _projection = null;
        }

        private void LateUpdate()
        {
            if (!_visual.activeSelf)
                return;

            transform.rotation = _projection.Rotation;
            _fill.localScale = new Vector3(Mathf.Max(_patience, EmptyScale), 1f, 1f);
            _block ??= new MaterialPropertyBlock();
            _fillRenderer.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, PatienceColor.At(_patience, _set.Patience));
            _fillRenderer.SetPropertyBlock(_block);
        }
    }
}

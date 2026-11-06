using AF.Core;
using PrimeTween;
using UnityEngine;

namespace HoldIt.Drawing
{
    /// <summary>
    /// A rounded tile on one cell of a path, in the walker's colour or red where the path cannot end. It pops in, shrinks
    /// away and shakes when it turns red. Tweens move the <c>Mesh</c> child only; the root stays where <see cref="Place"/> put it.
    /// </summary>
    public sealed class PathMarker : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private MeshRenderer _renderer;
        [SerializeField, Required, Tooltip("The tile's mesh; the tweens move and scale this.")]
        private Transform _visual;
        [SerializeField, Required] private Material _male;
        [SerializeField, Required] private Material _female;
        [SerializeField, Required] private Material _blocked;

        private PoolLife _life;
        private Vector3 _restScale;
        private Tween _scaleTween;
        private Tween _shakeTween;
        private MarkerLook _look = (MarkerLook)(-1);

        /// <summary>This life of the mark; use it to give the mark back.</summary>
        internal PoolLife Life => _life;

        private void Awake() => _restScale = _visual.localScale;

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _look = (MarkerLook)(-1);
            _visual.localScale = _restScale;
            _visual.localPosition = Vector3.zero;
        }

        public void OnDespawned()
        {
        }

        public void Place(Vector3 position, MarkerLook look)
        {
            transform.position = position;
            if (look == _look)
                return;
            _look = look;
            _renderer.sharedMaterial = look switch
            {
                MarkerLook.Male => _male,
                MarkerLook.Female => _female,
                _ => _blocked,
            };
        }

        public void PopIn(float seconds)
        {
            _visual.localScale = Vector3.zero;
            StopScale();
            _scaleTween = Tween.Scale(_visual, Vector3.zero, _restScale, seconds, Ease.OutBack)
                .SetCancellationToken(_life.Token);
        }

        public void Vanish(float seconds)
        {
            StopScale();
            _scaleTween = Tween.Scale(_visual, _visual.localScale, Vector3.zero, seconds, Ease.InBack)
                .SetCancellationToken(_life.Token);
        }

        public void Shake(float seconds, float strength, float frequency)
        {
            if (_shakeTween.isAlive)
                _shakeTween.Stop();
            _visual.localPosition = Vector3.zero;
            _shakeTween = Tween.ShakeLocalPosition(_visual, new Vector3(strength, 0f, strength), seconds, frequency)
                .SetCancellationToken(_life.Token);
        }

        private void StopScale()
        {
            if (_scaleTween.isAlive)
                _scaleTween.Stop();
        }
    }
}

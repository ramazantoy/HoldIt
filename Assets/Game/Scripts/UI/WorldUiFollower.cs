using UnityEngine;
using VContainer;

namespace HoldIt.UI
{
    /// <summary>
    /// Keeps a rect of the <see cref="IWorldUiLayer"/> on a place in the world: a transform's position or a fixed point, plus
    /// a world offset and a screen offset (canvas units). It places itself after the camera has fitted and the characters
    /// have moved (execution order), so it never lags a frame behind. Part of a pooled UI prefab; the rect is centred on
    /// its anchor point, so the prefab's anchors are the centre of its parent.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldUiFollower : MonoBehaviour
    {
        private IWorldUiLayer _layer;
        private RectTransform _rect;
        private Transform _anchor;
        private Vector2 _screenOffset;
        private bool _following;

        /// <summary>Added to the anchor's position, or the fixed point itself when there is no anchor.</summary>
        public Vector3 WorldOffset { get; set; }

        [Inject]
        public void Construct(IWorldUiLayer layer) => _layer = layer;

        /// <summary>Follows <paramref name="anchor"/> (null: stays on <see cref="WorldOffset"/>), placing the rect at once.</summary>
        public void Follow(Transform anchor, Vector3 worldOffset, Vector2 screenOffset)
        {
            _anchor = anchor;
            WorldOffset = worldOffset;
            _screenOffset = screenOffset;
            _following = true;
            Place();
        }

        public void Stop()
        {
            _following = false;
            _anchor = null;
        }

        private void Awake() => _rect = (RectTransform)transform;

        private void LateUpdate()
        {
            if (_following)
                Place();
        }

        // Out of sight (behind the camera) it stays where it was.
        private void Place()
        {
            var world = _anchor != null ? _anchor.position + WorldOffset : WorldOffset;
            if (_layer.TryPlace(world, out var local))
                _rect.anchoredPosition = local + _screenOffset;
        }
    }
}

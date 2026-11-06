using AF.Core;
using HoldIt.Board;
using PrimeTween;
using TMPro;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// A short text that rises over a character and fades ("+1" when it is served), then goes back to its pool. It faces
    /// the camera; its rise and fade are tweens of this life, and it leaves by game time, not by the end of a tween.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class FloatTextView : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private TextMeshPro _text;

        private IGameClock _clock;
        private IBoardProjection _projection;
        private CharacterSet _set;
        private PoolLife _life;
        private float _age;

        [Inject]
        public void Construct(IGameClock clock, IBoardProjection projection, CharacterSet set)
        {
            _clock = clock;
            _projection = projection;
            _set = set;
        }

        /// <summary>Starts the text at <paramref name="position"/>; it rises by the set's distance while it fades.</summary>
        public void Show(string text, Vector3 position)
        {
            _text.text = text;
            SetAlpha(1f);
            transform.SetPositionAndRotation(position, _projection.Rotation);
            Tween.PositionY(transform, position.y, position.y + _set.FloatRise, _set.FloatSeconds, Ease.OutCubic)
                .SetCancellationToken(_life.Token);
            Tween.Custom(1f, 0f, _set.FloatSeconds, SetAlpha, Ease.InQuad)
                .SetCancellationToken(_life.Token);
        }

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _age = 0f;
        }

        public void OnDespawned()
        {
        }

        private void LateUpdate()
        {
            transform.rotation = _projection.Rotation;
            _age += _clock.DeltaTime;
            if (_age >= _set.FloatSeconds)
                _life.Despawn();
        }

        private void SetAlpha(float alpha) => _text.alpha = alpha;
    }
}

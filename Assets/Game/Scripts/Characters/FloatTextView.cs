using AF.Core;
using HoldIt.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// A short text that rises over a character and fades ("+1" when it is served), then goes back to its pool. It is drawn
    /// in the HUD's world layer (FloatTextUi prefab): it keeps its size on screen, and follows a point that rises in the
    /// world. Its rise and fade are tweens of this life, and it leaves by game time, not by the end of a tween.
    /// </summary>
    public sealed class FloatTextView : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private TextMeshProUGUI _text;
        [SerializeField, Required] private WorldUiFollower _follower;

        private IGameClock _clock;
        private CharacterSet _set;
        private PoolLife _life;
        private float _age;

        [Inject]
        public void Construct(IGameClock clock, CharacterSet set)
        {
            _clock = clock;
            _set = set;
        }

        /// <summary>Starts the text at <paramref name="position"/>; it rises by the set's distance while it fades.</summary>
        public void Show(string text, Vector3 position)
        {
            _text.text = text;
            SetAlpha(1f);
            _follower.Follow(null, position, Vector2.zero);
            Tween.Custom(0f, _set.FloatRise, _set.FloatSeconds, rise => _follower.WorldOffset = position + Vector3.up * rise,
                    Ease.OutCubic)
                .SetCancellationToken(_life.Token);
            Tween.Custom(1f, 0f, _set.FloatSeconds, SetAlpha, Ease.InQuad)
                .SetCancellationToken(_life.Token);
        }

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _age = 0f;
        }

        public void OnDespawned() => _follower.Stop();

        private void LateUpdate()
        {
            _age += _clock.DeltaTime;
            if (_age >= _set.FloatSeconds)
                _life.Despawn();
        }

        private void SetAlpha(float alpha) => _text.alpha = alpha;
    }
}

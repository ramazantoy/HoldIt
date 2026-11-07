using AF.Core;
using HoldIt.UI;
using TMPro;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// A speech bubble in the HUD's world layer (SpeechBubbleUi prefab), over the character it speaks for. It keeps its
    /// size on screen, pops up, stays a moment and fades (<see cref="BubbleCurve"/>) on game time, so it waits while the
    /// game is paused, then goes back to its pool by itself. Its <see cref="SpeechBubble"/> opens it and holds its
    /// <see cref="Life"/>.
    /// </summary>
    public sealed class ScreenSpeechBubble : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private RectTransform _rect;
        [SerializeField, Required] private CanvasGroup _group;
        [SerializeField, Required] private TextMeshProUGUI _text;
        [SerializeField, Required] private WorldUiFollower _follower;

        private IGameClock _clock;
        private CharacterSet _set;
        private float _shownAt;

        [Inject]
        public void Construct(IGameClock clock, CharacterSet set)
        {
            _clock = clock;
            _set = set;
        }

        /// <summary>This bubble's life in its pool; dead once it has faded or been closed.</summary>
        public PoolLife Life { get; private set; }

        /// <summary>Says <paramref name="text"/> over <paramref name="anchor"/> in <paramref name="layer"/>, starting the pop from nothing.</summary>
        public void Say(string text, Transform anchor, IWorldUiLayer layer)
        {
            _text.text = text;
            _rect.sizeDelta = _text.GetPreferredValues(text) + 2f * _set.BubblePadding;
            _shownAt = _clock.Time;
            Apply(0f, 0f);
            _follower.Follow(layer, anchor, Vector3.zero, _set.BubbleScreenOffset);
        }

        public void OnSpawned(PoolLife life)
        {
            Life = life;
            _rect.SetAsLastSibling();
            Apply(0f, 0f);
        }

        public void OnDespawned() => _follower.Stop();

        private void LateUpdate()
        {
            if (!BubbleCurve.TryAt(_clock.Time - _shownAt, _set.Bubble, out var scale, out var alpha))
            {
                Life.Despawn();
                return;
            }
            Apply(scale, alpha);
        }

        private void Apply(float scale, float alpha)
        {
            _rect.localScale = Vector3.one * scale;
            _group.alpha = alpha;
        }
    }
}

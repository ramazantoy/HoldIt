using AF.Core;
using HoldIt.Board;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// The bubble over a character's head that says why it gave up a step, that it is relieved, or that it had an accident
    /// (<see cref="BubbleLine"/>). It faces the camera, pops up, stays a moment and fades (<see cref="BubbleCurve"/>) on
    /// game time, so it waits while the game is paused. Part of the character prefab, under Anchor_Top; hidden whenever the
    /// character is spawned. It turns after the character's view has (execution order), so a spinning character's bubble
    /// still faces the camera.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class SpeechBubble : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private GameObject _visual;
        [SerializeField, Required] private SpriteRenderer _background;
        [SerializeField, Required] private TextMeshPro _text;

        [SerializeField, Tooltip("Padding around the text inside the bubble, in tiles.")]
        private Vector2 _padding = new Vector2(0.12f, 0.06f);

        [SerializeField] private LocalizedString _wrongDoor;
        [SerializeField] private LocalizedString _tooWide;
        [SerializeField] private LocalizedString _blocked;
        [SerializeField] private LocalizedString _relieved;
        [SerializeField] private LocalizedString _accident;

        private IGameClock _clock;
        private IBoardProjection _projection;
        private CharacterSet _set;
        private float _shownAt;
        private bool _showing;

        [Inject]
        public void Construct(IGameClock clock, IBoardProjection projection, CharacterSet set)
        {
            _clock = clock;
            _projection = projection;
            _set = set;
        }

        /// <summary>True from <see cref="Show"/> until the bubble has faded.</summary>
        public bool IsShowing => _showing;

        /// <summary>Shows the line, starting the bubble over if it was already up.</summary>
        public void Show(BubbleLine bubbleLine)
        {
            var line = LineFor(bubbleLine);
            if (line == null || line.IsEmpty)
            {
                Hide();
                return;
            }

            _text.text = line.GetLocalizedString();
            _text.ForceMeshUpdate();
            var size = (Vector2)_text.textBounds.size + 2f * _padding;
            _background.size = size;
            _shownAt = _clock.Time;
            _showing = true;
            _visual.SetActive(true);
            Apply(0f, 0f);
        }

        public void OnSpawned(PoolLife life) => Hide();

        public void OnDespawned() => Hide();

        private void LateUpdate()
        {
            if (!_showing)
                return;
            if (!BubbleCurve.TryAt(_clock.Time - _shownAt, _set.Bubble, out var scale, out var alpha))
            {
                Hide();
                return;
            }
            Apply(scale, alpha);
        }

        private void Apply(float scale, float alpha)
        {
            transform.rotation = _projection.Rotation;
            _visual.transform.localScale = Vector3.one * scale;
            var background = _background.color;
            background.a = alpha;
            _background.color = background;
            _text.alpha = alpha;
        }

        private void Hide()
        {
            _showing = false;
            _visual.SetActive(false);
        }

        private LocalizedString LineFor(BubbleLine line) => line switch
        {
            BubbleLine.WrongDoor => _wrongDoor,
            BubbleLine.CantFit => _tooWide,
            BubbleLine.Blocked => _blocked,
            BubbleLine.Relieved => _relieved,
            BubbleLine.Accident => _accident,
            _ => null,
        };
    }
}

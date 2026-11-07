using AF.Core;
using UnityEngine;
using UnityEngine.Localization;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// What a character says over its head: why it gave up a step, that it is relieved, or that it had an accident
    /// (<see cref="BubbleLine"/>). It holds the lines and the place of the bubble (part of the character prefab, under
    /// Anchor_Top); the bubble itself is a <see cref="ScreenSpeechBubble"/> in the HUD's world layer, so it is drawn in screen
    /// space. A new line takes over from the one still up.
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour, IPoolable
    {
        [SerializeField] private LocalizedString _wrongDoor;
        [SerializeField] private LocalizedString _tooWide;
        [SerializeField] private LocalizedString _blocked;
        [SerializeField] private LocalizedString _relieved;
        [SerializeField] private LocalizedString _accident;

        private LevelViewContext _context;
        private CharacterSet _set;
        private PoolLife _bubble;

        [Inject]
        public void Construct(CharacterSet set) => _set = set;

        /// <summary>The level and the scene it speaks in, until it is despawned; its view hands it over.</summary>
        public void Bind(LevelViewContext context) => _context = context;

        /// <summary>True from <see cref="Show"/> until the bubble has faded.</summary>
        public bool IsShowing => _bubble.IsCurrent;

        /// <summary>Shows the line, starting the bubble over if it was already up.</summary>
        public void Show(BubbleLine bubbleLine)
        {
            Hide();
            var line = LineFor(bubbleLine);
            if (line == null || line.IsEmpty)
                return;

            var bubble = _context.Pools.For(_set.SpeechBubble).Spawn(_context.Layer.Root);
            _bubble = bubble.Life;
            bubble.Say(line.GetLocalizedString(), transform, _context.Layer);
        }

        public void OnSpawned(PoolLife life) => Hide();

        public void OnDespawned()
        {
            Hide();
            _context = null;
        }

        private void Hide()
        {
            _bubble.Despawn();
            _bubble = default;
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

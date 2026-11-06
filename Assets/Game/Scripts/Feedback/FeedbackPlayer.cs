using AF.Audio;
using AF.Core;
using HoldIt.Haptics;

namespace HoldIt.Feedback
{
    /// <summary>Plays a kind's cue (2D) and vibration, once the table lets it through.</summary>
    internal sealed class FeedbackPlayer : IFeedback
    {
        private readonly FeedbackTable _table;
        private readonly ISounds _sounds;
        private readonly IGlobalSounds _globalSounds;
        private readonly IHaptics _haptics;
        private readonly IGameClock _clock;

        public FeedbackPlayer(FeedbackTable table, ISounds sounds, IGlobalSounds globalSounds, IHaptics haptics, IGameClock clock)
        {
            _table = table;
            _sounds = sounds;
            _globalSounds = globalSounds;
            _haptics = haptics;
            _clock = clock;
        }

        public void Play(FeedbackKind kind)
        {
            if (!_table.TryFire(kind, _clock.Time, out var entry))
                return;
            if (entry.Cue != null)
                (entry.OutlivesScene ? _globalSounds : _sounds).Play(entry.Cue);
            _haptics.Play(entry.Haptic);
        }
    }
}

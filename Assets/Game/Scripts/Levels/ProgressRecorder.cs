using System;
using AF.Save;
using Cysharp.Threading.Tasks;
using MessagePipe;
using VContainer.Unity;

namespace HoldIt.Levels
{
    /// <summary>
    /// Keeps a won level in the progress as it ends, and writes the global data at once, so the win survives a crash. A lost
    /// level changes nothing. The result screen reads the progress later (after the end delay), so it sees the win whatever
    /// order the subscribers of <see cref="LevelEnded"/> run in.
    /// </summary>
    internal sealed class ProgressRecorder : IInitializable, IDisposable
    {
        private readonly ILevelCursor _cursor;
        private readonly ILevelProgress _progress;
        private readonly ISaveService _save;
        private readonly ISubscriber<LevelEnded> _ended;
        private IDisposable _subscription;

        public ProgressRecorder(ILevelCursor cursor, ILevelProgress progress, ISaveService save,
            ISubscriber<LevelEnded> ended)
        {
            _cursor = cursor;
            _progress = progress;
            _save = save;
            _ended = ended;
        }

        public void Initialize() => _subscription = _ended.Subscribe(Record);

        public void Dispose() => _subscription?.Dispose();

        private void Record(LevelEnded message)
        {
            if (message.Outcome != LevelOutcome.Won)
                return;
            _progress.RecordWin(_cursor.Index, message.Stars);
            _save.SaveGlobal().Forget();
        }
    }
}

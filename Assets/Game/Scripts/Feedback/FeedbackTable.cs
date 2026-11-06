using System;
using System.Collections.Generic;
using AF.Core;

namespace HoldIt.Feedback
{
    /// <summary>
    /// Looks up a kind's entry and keeps kinds from repeating faster than their <see cref="FeedbackEntry.MinInterval"/>
    /// (a path growing by a cell a frame should tick, not buzz). A kind listed twice keeps its first entry.
    /// </summary>
    internal sealed class FeedbackTable
    {
        private readonly FeedbackEntry[] _byKind = new FeedbackEntry[Enum.GetValues(typeof(FeedbackKind)).Length];
        private readonly float[] _last;

        public FeedbackTable(IEnumerable<FeedbackEntry> entries)
        {
            _last = new float[_byKind.Length];
            Array.Fill(_last, float.NegativeInfinity);
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;
                var index = (int)entry.Kind;
                if (_byKind[index] != null)
                {
                    Log.Error($"Feedback set lists {entry.Kind} twice; the first entry is used.");
                    continue;
                }
                _byKind[index] = entry;
            }
        }

        /// <summary>True, with the entry, when the kind has one and is not repeating too fast; the time is then taken.</summary>
        public bool TryFire(FeedbackKind kind, float now, out FeedbackEntry entry)
        {
            var index = (int)kind;
            entry = _byKind[index];
            if (entry == null || now - _last[index] < entry.MinInterval)
                return false;
            _last[index] = now;
            return true;
        }
    }
}

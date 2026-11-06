using System.Collections.Generic;

namespace HoldIt.Drawing
{
    /// <summary>
    /// Compares the marks wanted this frame with the ones shown the frame before, so each mark can pop in once, vanish once
    /// and shake when it turns red. The lists are reused: read them before the next <see cref="Update"/>.
    /// </summary>
    internal sealed class MarkerDiff
    {
        private readonly Dictionary<MarkerKey, MarkerLook> _looks = new Dictionary<MarkerKey, MarkerLook>();
        private readonly HashSet<MarkerKey> _seen = new HashSet<MarkerKey>();

        public List<(MarkerKey Key, MarkerLook Look)> Added { get; } = new List<(MarkerKey, MarkerLook)>();
        public List<MarkerKey> Removed { get; } = new List<MarkerKey>();
        public List<(MarkerKey Key, MarkerLook Look)> Changed { get; } = new List<(MarkerKey, MarkerLook)>();

        /// <summary>Marks the diff currently knows.</summary>
        public int Count => _looks.Count;

        /// <summary>
        /// Takes the marks wanted now. A key listed twice counts once, the first look wins. <see cref="Added"/> and
        /// <see cref="Changed"/> keep the wanted order.
        /// </summary>
        public void Update(IReadOnlyList<(MarkerKey Key, MarkerLook Look)> wanted)
        {
            Added.Clear();
            Removed.Clear();
            Changed.Clear();
            _seen.Clear();

            for (var i = 0; i < wanted.Count; i++)
            {
                var (key, look) = wanted[i];
                if (!_seen.Add(key))
                    continue;
                if (!_looks.TryGetValue(key, out var before))
                {
                    _looks.Add(key, look);
                    Added.Add((key, look));
                }
                else if (before != look)
                {
                    _looks[key] = look;
                    Changed.Add((key, look));
                }
            }

            foreach (var key in _looks.Keys)
            {
                if (!_seen.Contains(key))
                    Removed.Add(key);
            }
            for (var i = 0; i < Removed.Count; i++)
                _looks.Remove(Removed[i]);
        }

        public void Clear()
        {
            _looks.Clear();
            Added.Clear();
            Removed.Clear();
            Changed.Clear();
        }
    }
}

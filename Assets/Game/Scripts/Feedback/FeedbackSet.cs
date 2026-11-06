using System.Collections.Generic;
using UnityEngine;

namespace HoldIt.Feedback
{
    /// <summary>
    /// The sound and the vibration of every <see cref="FeedbackKind"/>, one entry per kind. Cues are made in
    /// Create / AF / Sound Cue and set here; a kind without a cue is silent.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Feedback/Feedback Set", fileName = "FeedbackSet")]
    public sealed class FeedbackSet : ScriptableObject
    {
        [SerializeField] private FeedbackEntry[] _entries = System.Array.Empty<FeedbackEntry>();

        public IReadOnlyList<FeedbackEntry> Entries => _entries;
    }
}

using System;
using System.Collections.Generic;

namespace AF.Save
{
    /// <summary>
    /// Where save documents are kept. An entry is a name such as <c>saves/slot-1</c>; each has a primary copy, the previous
    /// copy as a backup, and possibly a quarantined <c>.corrupt</c> copy. Calls are synchronous; the save queue runs them one at
    /// a time and decides the thread from <see cref="IsThreadSafe"/>. Implement this for a platform store (Steam Remote
    /// Storage, consoles), or wrap an existing storage to add encryption or compression.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>True when the calls may run on a thread-pool thread. False keeps them on the main thread (PlayerPrefs).</summary>
        bool IsThreadSafe { get; }

        /// <summary>Reads both copies. An access failure is returned as <see cref="StorageRead.Error"/>, not thrown.</summary>
        StorageRead Read(string entry);

        /// <summary>Replaces the primary copy atomically: after a crash either the old or the new content is there. The old primary becomes the backup.</summary>
        void Write(string entry, string content);

        /// <summary>
        /// Keeps the primary copy aside as <c>.corrupt</c>, replacing an older one. With <paramref name="move"/> the primary is
        /// then removed, so the next write does not turn the unreadable copy into the backup.
        /// </summary>
        void Quarantine(string entry, bool move);

        /// <summary>Entries that start with <paramref name="prefix"/> and have a primary or a backup copy, in ordinal order.</summary>
        IReadOnlyList<string> List(string prefix);

        /// <summary>Removes every copy of the entry.</summary>
        void Delete(string entry);
    }

    public readonly struct StorageRead
    {
        /// <summary>Null when there is no primary copy.</summary>
        public readonly string Primary;

        /// <summary>Null when there is no backup copy.</summary>
        public readonly string Backup;

        /// <summary>Set when the storage could not be read; the contents are then unknown, not missing.</summary>
        public readonly Exception Error;

        public StorageRead(string primary, string backup)
        {
            Primary = primary;
            Backup = backup;
            Error = null;
        }

        private StorageRead(Exception error)
        {
            Primary = null;
            Backup = null;
            Error = error;
        }

        public static StorageRead Failed(Exception error) => new StorageRead(error);
    }
}

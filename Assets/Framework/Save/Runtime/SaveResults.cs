using System;

namespace AF.Save
{
    public enum LoadStatus
    {
        /// <summary>Nothing has been read yet.</summary>
        None,

        Loaded,

        /// <summary>The file was unreadable and its backup was loaded. The unreadable file was kept as <c>.corrupt</c>.</summary>
        RecoveredFromBackup,

        NotFound,

        /// <summary>The file and its backup are unreadable. Nothing was deleted; the file was copied to <c>.corrupt</c>.</summary>
        Corrupt,

        /// <summary>Written by a newer version of the game. Left untouched; a global or device document is read-only for the session.</summary>
        TooNew,

        /// <summary>The storage could not be read (a lock, permissions). Not treated as damage; left untouched.</summary>
        ReadFailed,

        /// <summary>
        /// The file is readable but the code could not use it: a migration step or a section's data class threw. The file is
        /// left untouched and no savable was restored. This is a bug to fix, not damaged data.
        /// </summary>
        Failed,
    }

    public readonly struct LoadResult
    {
        public readonly LoadStatus Status;

        /// <summary>The cause, for every status except Loaded and NotFound.</summary>
        public readonly Exception Error;

        public LoadResult(LoadStatus status, Exception error = null)
        {
            Status = status;
            Error = error;
        }

        /// <summary>True when the data is in use: <see cref="LoadStatus.Loaded"/> or <see cref="LoadStatus.RecoveredFromBackup"/>.</summary>
        public bool IsLoaded => Status == LoadStatus.Loaded || Status == LoadStatus.RecoveredFromBackup;

        public override string ToString() => Error == null ? Status.ToString() : $"{Status}: {Error.Message}";
    }

    public enum SaveStatus
    {
        Written,

        /// <summary>Nothing to write: a dev session, or unchanged data when the app was suspended.</summary>
        Skipped,

        /// <summary>The document is read-only for this session (written by a newer version, or unreadable).</summary>
        Refused,

        /// <summary>The storage write failed (disk full, permissions). The previous file is intact.</summary>
        Failed,
    }

    public readonly struct SaveResult
    {
        public readonly SaveStatus Status;
        public readonly Exception Error;

        public SaveResult(SaveStatus status, Exception error = null)
        {
            Status = status;
            Error = error;
        }

        /// <summary>True only when the data reached the storage. Show "saved" feedback on this, not on the call returning.</summary>
        public bool IsWritten => Status == SaveStatus.Written;

        public override string ToString() => Error == null ? Status.ToString() : $"{Status}: {Error.Message}";

        internal static SaveResult Combine(SaveResult first, SaveResult second)
        {
            if (first.Status == SaveStatus.Failed) return first;
            if (second.Status == SaveStatus.Failed) return second;
            if (first.Status == SaveStatus.Refused) return first;
            if (second.Status == SaveStatus.Refused) return second;
            return first.Status == SaveStatus.Written ? first : second;
        }
    }

    /// <summary>Save data that the code cannot use: a section that does not match its data class, a capture that failed.</summary>
    public sealed class SaveDataException : Exception
    {
        public SaveDataException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}

using System;
using System.Globalization;
using AF.Core;
using Newtonsoft.Json.Linq;

namespace AF.Save
{
    /// <summary>What a load menu shows about one slot, read without loading it.</summary>
    public sealed class SlotInfo
    {
        private readonly JObject _sections;

        internal SlotInfo(string id, DocumentRead read)
        {
            Id = id;
            Status = read.Status;
            Error = read.Error;
            if (read.Document == null)
                return;

            _sections = read.Document.Sections;
            var meta = read.Document.Meta;
            if (DateTime.TryParse(SaveFormat.ReadString(meta, SaveFormat.SavedUtcProperty), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var savedUtc))
                SavedUtc = savedUtc.ToUniversalTime();
            AppVersion = SaveFormat.ReadString(meta, SaveFormat.AppVersionProperty);
            PlayTime = TimeSpan.FromSeconds(SaveFormat.ReadPlayTime(meta));
        }

        public string Id { get; }

        /// <summary><see cref="LoadStatus.Loaded"/> or <see cref="LoadStatus.RecoveredFromBackup"/> when it can be loaded.</summary>
        public LoadStatus Status { get; }

        public Exception Error { get; }
        public bool CanLoad => Status == LoadStatus.Loaded || Status == LoadStatus.RecoveredFromBackup;
        public DateTime? SavedUtc { get; }

        /// <summary><c>Application.version</c> of the build that wrote it.</summary>
        public string AppVersion { get; }

        public TimeSpan PlayTime { get; }

        /// <summary>
        /// Reads one section without loading the slot, for example a "summary" savable the game keeps for its load menu.
        /// False when the slot is not loadable or the section is missing or does not match <typeparamref name="T"/>.
        /// </summary>
        public bool TryReadSection<T>(string key, out T data)
        {
            data = default;
            var section = _sections?[key];
            if (section == null || section.Type == JTokenType.Null)
                return false;
            try
            {
                data = (T)SaveJson.FromToken(section, typeof(T));
                return data != null;
            }
            catch (Exception exception)
            {
                Log.Exception(new SaveDataException($"Section '{key}' of slot '{Id}' cannot be read as {typeof(T).Name}.", exception));
                return false;
            }
        }

        public override string ToString() => $"{Id}: {Status}, saved {SavedUtc:u}, played {PlayTime}";
    }
}

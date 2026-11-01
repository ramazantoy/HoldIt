using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AF.Save
{
    /// <summary>A parsed document: header checked, migrated to the current version.</summary>
    internal sealed class ParsedDocument
    {
        public readonly int Version;
        public readonly JObject Meta;
        public readonly JObject Sections;

        private ParsedDocument(int version, JObject meta, JObject sections)
        {
            Version = version;
            Meta = meta;
            Sections = sections;
        }

        /// <summary>Throws <see cref="FormatException"/> when the header is missing or invalid.</summary>
        public static ParsedDocument From(JObject document) => new ParsedDocument(
            SaveFormat.VersionOf(document),
            document[SaveFormat.MetaProperty] as JObject ?? new JObject(),
            document[SaveFormat.SectionsProperty] as JObject ?? throw new FormatException("The document has no sections object."));
    }

    internal enum QuarantineMode
    {
        None,

        /// <summary>Keep a copy and leave the file in place: nothing better replaces it yet.</summary>
        Copy,

        /// <summary>Move the file aside, so the next write does not rotate it over the good backup.</summary>
        Move,
    }

    internal readonly struct DocumentRead
    {
        public readonly LoadStatus Status;
        public readonly ParsedDocument Document;
        public readonly Exception Error;
        public readonly QuarantineMode Quarantine;

        public DocumentRead(LoadStatus status, ParsedDocument document = null, Exception error = null, QuarantineMode quarantine = QuarantineMode.None)
        {
            Status = status;
            Document = document;
            Error = error;
            Quarantine = quarantine;
        }

        public bool IsLoaded => Status == LoadStatus.Loaded || Status == LoadStatus.RecoveredFromBackup;
    }

    /// <summary>
    /// The file format: <c>{ "schemaVersion": n, "meta": {...}, "sections": { "&lt;key&gt;": {...} } }</c>, indented UTF-8 JSON.
    /// </summary>
    internal static class SaveFormat
    {
        public const string VersionProperty = "schemaVersion";
        public const string MetaProperty = "meta";
        public const string SectionsProperty = "sections";
        public const string SavedUtcProperty = "savedUtc";
        public const string AppVersionProperty = "appVersion";
        public const string PlayTimeProperty = "playTimeSeconds";

        private static readonly JsonLoadSettings _loadSettings = new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
        };

        public static string Write(int version, JObject meta, JObject sections)
        {
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.Indented })
            {
                writer.WriteStartObject();
                writer.WritePropertyName(VersionProperty);
                writer.WriteValue(version);
                writer.WritePropertyName(MetaProperty);
                meta.WriteTo(writer);
                writer.WritePropertyName(SectionsProperty);
                sections.WriteTo(writer);
                writer.WriteEndObject();
            }
            return text.ToString();
        }

        /// <summary>Parses a document and checks its header. Throws for anything that is not a complete save document.</summary>
        public static JObject Parse(string content)
        {
            JObject document;
            using (var reader = new JsonTextReader(new StringReader(content)))
            {
                // Without these a string that looks like a date becomes a DateTime token and comes back reformatted.
                reader.DateParseHandling = DateParseHandling.None;
                reader.FloatParseHandling = FloatParseHandling.Double;
                document = JObject.Load(reader, _loadSettings);
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.Comment)
                        throw new FormatException("Unexpected content after the document.");
                }
            }
            ParsedDocument.From(document);
            return document;
        }

        /// <summary>Compact text of the sections, to tell whether anything changed since they were last written.</summary>
        public static string Snapshot(JObject sections) => sections.ToString(Formatting.None);

        /// <summary>A meta value, or null when it is missing or not a number. Meta never makes a readable file unusable.</summary>
        public static double? ReadDouble(JObject meta, string name) =>
            meta[name] is JValue { Type: JTokenType.Float or JTokenType.Integer } value ? value.Value<double>() : (double?)null;

        /// <summary>Saved play time in seconds; 0 when missing or out of range.</summary>
        public static double ReadPlayTime(JObject meta)
        {
            var seconds = ReadDouble(meta, PlayTimeProperty) ?? 0d;
            return seconds >= 0d && seconds < TimeSpan.MaxValue.TotalSeconds ? seconds : 0d;
        }

        /// <summary>A meta value, or null when it is missing or not a string.</summary>
        public static string ReadString(JObject meta, string name) =>
            meta[name] is JValue { Type: JTokenType.String } value ? value.Value<string>() : null;

        public static int VersionOf(JObject document)
        {
            if (document[VersionProperty] is JValue { Type: JTokenType.Integer } value)
            {
                var version = value.Value<long>();
                if (version >= 1 && version <= int.MaxValue)
                    return (int)version;
            }
            throw new FormatException($"The document has no valid {VersionProperty}.");
        }

        /// <summary>
        /// Decides what a storage read means: which copy to use, whether it needs migrating, what to quarantine. Pure; the
        /// caller performs the quarantine.
        /// </summary>
        public static DocumentRead Interpret(StorageRead read, SaveSchema schema)
        {
            if (read.Error != null)
                return new DocumentRead(LoadStatus.ReadFailed, error: read.Error);
            if (read.Primary == null && read.Backup == null)
                return new DocumentRead(LoadStatus.NotFound);

            Exception primaryError = null;
            if (read.Primary != null)
            {
                if (TryParse(read.Primary, out var primary, out primaryError))
                    return Upgrade(primary, schema, LoadStatus.Loaded, QuarantineMode.None, null);
            }

            Exception backupError = null;
            if (read.Backup != null && TryParse(read.Backup, out var backup, out backupError))
            {
                var quarantine = read.Primary != null ? QuarantineMode.Move : QuarantineMode.None;
                return Upgrade(backup, schema, LoadStatus.RecoveredFromBackup, quarantine, primaryError);
            }

            return new DocumentRead(LoadStatus.Corrupt, error: primaryError ?? backupError,
                quarantine: read.Primary != null ? QuarantineMode.Copy : QuarantineMode.None);
        }

        private static DocumentRead Upgrade(JObject document, SaveSchema schema, LoadStatus status, QuarantineMode quarantine, Exception primaryError)
        {
            var version = VersionOf(document);
            if (version > schema.CurrentVersion)
            {
                // Nothing is quarantined: the data is fine, this build is too old for it.
                return new DocumentRead(LoadStatus.TooNew, error: new InvalidOperationException(
                    $"Written with schema version {version}; this build reads up to {schema.CurrentVersion}."));
            }

            try
            {
                schema.Migrate(document, version);
                return new DocumentRead(status, ParsedDocument.From(document), primaryError, quarantine);
            }
            catch (Exception exception)
            {
                // A migration bug, not damaged data: no fallback and nothing quarantined.
                return new DocumentRead(LoadStatus.Failed, error: new SaveDataException(
                    $"Migrating from schema version {version} to {schema.CurrentVersion} failed.", exception));
            }
        }

        private static bool TryParse(string content, out JObject document, out Exception error)
        {
            try
            {
                document = Parse(content);
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is FormatException)
            {
                document = null;
                error = exception;
                return false;
            }
        }
    }
}

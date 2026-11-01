using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AF.Save
{
    /// <summary>
    /// Version history of one document type (slot, global or device). Every document is written with
    /// <see cref="CurrentVersion"/> and older ones are migrated on load, one step at a time.
    /// </summary>
    /// <example>
    /// <code>
    /// new SaveSchema()
    ///     .Step(1, SlotMigrations.V1ToV2)   // the document is now version 2
    ///     .Step(2, SlotMigrations.V2ToV3);  // CurrentVersion is 3
    /// </code>
    /// A step receives the whole document (<c>schemaVersion</c>, <c>meta</c>, <c>sections</c>) and changes it in place; see
    /// <see cref="SaveMigration"/>. Keep each step a single function with its own test, and never edit a step after a version
    /// that uses it has shipped.
    /// </example>
    public sealed class SaveSchema
    {
        private readonly List<Action<JObject>> _steps = new List<Action<JObject>>();

        /// <summary>1 without steps; each step adds one.</summary>
        public int CurrentVersion => _steps.Count + 1;

        /// <summary>Adds the migration from <paramref name="fromVersion"/> to the next version. Steps are added in order.</summary>
        public SaveSchema Step(int fromVersion, Action<JObject> migrate)
        {
            if (fromVersion != CurrentVersion)
                throw new ArgumentException(
                    $"Migration steps are added one version at a time, in order: expected Step({CurrentVersion}, ...), got Step({fromVersion}, ...).",
                    nameof(fromVersion));
            _steps.Add(migrate ?? throw new ArgumentNullException(nameof(migrate)));
            return this;
        }

        /// <summary>Runs the steps from <paramref name="fromVersion"/> to <see cref="CurrentVersion"/>, updating schemaVersion after each.</summary>
        internal void Migrate(JObject document, int fromVersion)
        {
            for (var version = fromVersion; version < CurrentVersion; version++)
            {
                _steps[version - 1](document);
                document[SaveFormat.VersionProperty] = version + 1;
            }
        }
    }

    /// <summary>Helpers for migration steps.</summary>
    public static class SaveMigration
    {
        /// <summary>The section stored under <paramref name="key"/>, or null.</summary>
        public static JToken Section(JObject document, string key) => Sections(document)[key];

        /// <summary>Moves a section to a new key. Does nothing when the old key is missing.</summary>
        public static void RenameSection(JObject document, string from, string to)
        {
            var sections = Sections(document);
            var section = sections[from];
            if (section == null)
                return;
            sections.Remove(from);
            sections[to] = section;
        }

        public static void RemoveSection(JObject document, string key) => Sections(document).Remove(key);

        private static JObject Sections(JObject document) =>
            document[SaveFormat.SectionsProperty] as JObject ?? throw new InvalidOperationException("The document has no sections object.");
    }
}

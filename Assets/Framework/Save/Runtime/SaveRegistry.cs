using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.Core;
using Newtonsoft.Json.Linq;

namespace AF.Save
{
    /// <summary>
    /// The savables that are alive right now. Scopes register theirs on their own (see <see cref="SavableScopeLink"/>);
    /// register by hand only what no scope owns, such as objects created at runtime.
    /// </summary>
    public interface ISaveRegistry
    {
        /// <summary>
        /// Adds a savable. If its document is loaded, the savable is restored at once from its section (or its default).
        /// Disposing the handle captures its state into the loaded document, then removes it. Throws when another registered
        /// savable already uses the same key in the same document.
        /// </summary>
        IDisposable Register(ISavable savable);

        bool IsRegistered(ISavable savable);
    }

    /// <summary>One loaded document: its sections in memory and what was last written.</summary>
    internal sealed class DocumentState
    {
        public readonly SaveDocument Kind;

        /// <summary>Storage entry; null for a dev session, which is never written. Changes on a save as.</summary>
        public string Entry;

        /// <summary>False when the stored data must be protected from this session: too new, unreadable or unusable.</summary>
        public readonly bool Writable;

        /// <summary>Every section, including those of savables that are not registered now (unloaded scenes).</summary>
        public JObject Sections;

        /// <summary>
        /// <see cref="SaveFormat.Snapshot"/> of the sections as last written, loaded or defaulted, to skip unchanged writes when the
        /// app is suspended. Null when there is nothing to compare with.
        /// </summary>
        public string WrittenContent;

        public DocumentState(SaveDocument kind, string entry, bool writable, JObject sections)
        {
            Kind = kind;
            Entry = entry;
            Writable = writable;
            Sections = sections;
        }
    }

    public sealed class SaveRegistry : ISaveRegistry
    {
        private static readonly Regex _keyPattern = new Regex("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);

        private readonly List<ISavable> _savables = new List<ISavable>();
        private readonly DocumentState[] _current = new DocumentState[Enum.GetValues(typeof(SaveDocument)).Length];

        public IDisposable Register(ISavable savable)
        {
            if (savable == null)
                throw new ArgumentNullException(nameof(savable));
            if (savable is UnityEngine.Object)
                throw new ArgumentException($"{savable.GetType().Name} is a Unity object. Savables are plain C# services registered in a scope.", nameof(savable));
            if (savable.Key == null || !_keyPattern.IsMatch(savable.Key))
                throw new ArgumentException($"{savable.GetType().Name} has the key '{savable.Key}'. Keys are 1-64 characters of a-z, 0-9, '.', '_' and '-'.", nameof(savable));
            if (_savables.Contains(savable))
                throw new InvalidOperationException($"{savable.GetType().Name} ('{savable.Key}') is already registered.");
            foreach (var other in _savables)
            {
                if (other.Document == savable.Document && string.Equals(other.Key, savable.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"{other.GetType().Name} and {savable.GetType().Name} both use the key '{savable.Key}' in the {savable.Document} document.");
            }

            var document = _current[(int)savable.Document];
            if (document != null)
                Restore(savable, ReadSection(document, savable));

            _savables.Add(savable);
            return new Registration(this, savable);
        }

        public bool IsRegistered(ISavable savable) => _savables.Contains(savable);

        internal int Count => _savables.Count;

        internal DocumentState Current(SaveDocument kind) => _current[(int)kind];

        /// <summary>
        /// Reads the data of every registered savable of the document's kind: its section, or its default. Throws
        /// <see cref="SaveDataException"/> before anything is restored, so a failure leaves every savable as it was.
        /// </summary>
        internal List<(ISavable savable, object data)> Prepare(DocumentState document)
        {
            var prepared = new List<(ISavable, object)>();
            foreach (var savable in _savables)
            {
                if (savable.Document == document.Kind)
                    prepared.Add((savable, ReadSection(document, savable)));
            }
            return prepared;
        }

        /// <summary>Makes the document current and restores the prepared data.</summary>
        internal void Activate(DocumentState document, List<(ISavable savable, object data)> prepared)
        {
            _current[(int)document.Kind] = document;
            foreach (var (savable, data) in prepared)
                Restore(savable, data);
        }

        // Restore must not throw. Log and carry on, so one broken savable neither leaves the others half-loaded nor stops a
        // scene from opening. A section that does not match its data class still throws, from ReadSection.
        private static void Restore(ISavable savable, object data)
        {
            try
            {
                savable.RestoreData(data);
            }
            catch (Exception exception)
            {
                Log.Exception(new SaveDataException($"{savable.GetType().Name} ('{savable.Key}') threw while restoring.", exception));
            }
        }

        /// <summary>Makes the document current without restoring anything (dev session, closing a slot with null).</summary>
        internal void SetCurrent(SaveDocument kind, DocumentState document) => _current[(int)kind] = document;

        /// <summary>
        /// The document's sections with every registered savable's state captured, as a new object. Throws
        /// <see cref="SaveDataException"/> when a savable fails, so a save never contains half a snapshot.
        /// </summary>
        internal JObject Capture(DocumentState document)
        {
            var sections = (JObject)document.Sections.DeepClone();
            foreach (var savable in _savables)
            {
                if (savable.Document == document.Kind)
                    sections[savable.Key] = CaptureSection(savable);
            }
            return sections;
        }

        private void Unregister(ISavable savable)
        {
            if (!_savables.Remove(savable))
                return;

            // Keep the state of a scene that unloads, so a later save in another scene still contains it.
            var document = _current[(int)savable.Document];
            if (document == null)
                return;
            try
            {
                document.Sections[savable.Key] = CaptureSection(savable);
            }
            catch (SaveDataException exception)
            {
                Log.Exception(exception);
            }
        }

        private static object ReadSection(DocumentState document, ISavable savable)
        {
            var section = document.Sections[savable.Key];
            if (section == null || section.Type == JTokenType.Null)
                return CreateDefault(savable);

            object data;
            try
            {
                data = SaveJson.FromToken(section, savable.DataType);
            }
            catch (Exception exception)
            {
                throw new SaveDataException(
                    $"Section '{savable.Key}' of {document.Entry ?? "the dev session"} cannot be read as {savable.DataType.Name} " +
                    $"({savable.GetType().Name}). Add a migration step for the change to the data class.", exception);
            }
            return data ?? CreateDefault(savable);
        }

        private static object CreateDefault(ISavable savable) =>
            savable.CreateDefaultData() ?? throw new SaveDataException($"{savable.GetType().Name} ('{savable.Key}') created a null default.");

        private static JToken CaptureSection(ISavable savable)
        {
            object data;
            try
            {
                data = savable.CaptureData();
            }
            catch (Exception exception)
            {
                throw new SaveDataException($"{savable.GetType().Name} ('{savable.Key}') threw while capturing its state.", exception);
            }
            if (data == null)
                throw new SaveDataException($"{savable.GetType().Name} ('{savable.Key}') captured null.");

            try
            {
                return SaveJson.ToToken(data);
            }
            catch (Exception exception)
            {
                throw new SaveDataException($"The data of {savable.GetType().Name} ('{savable.Key}') cannot be serialized.", exception);
            }
        }

        private sealed class Registration : IDisposable
        {
            private SaveRegistry _registry;
            private readonly ISavable _savable;

            public Registration(SaveRegistry registry, ISavable savable)
            {
                _registry = registry;
                _savable = savable;
            }

            public void Dispose()
            {
                _registry?.Unregister(_savable);
                _registry = null;
            }
        }
    }
}

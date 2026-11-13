using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AF.Save
{
    public sealed class SaveService : ISaveService
    {
        private static readonly Regex _slotIdPattern = new Regex("^[a-z0-9][a-z0-9_-]{0,31}$", RegexOptions.CultureInvariant);
        private const string SLOT_ENTRY_PREFIX = FrameworkConstants.SaveFolder + "/" + FrameworkConstants.SlotSavePrefix;
        private const string GLOBAL_ENTRY = FrameworkConstants.SaveFolder + "/" + FrameworkConstants.GlobalSaveName;
        private const string DEVICE_ENTRY = FrameworkConstants.DeviceSaveFolder + "/" + FrameworkConstants.DeviceSaveName;

        private readonly SaveRegistry _registry;
        private readonly SaveQueue _queue;
        private readonly SaveOptions _options;
        private readonly string _appVersion;
        private double _playTimeSeconds;
        private bool _loading;

        public SaveService(SaveRegistry registry, ISaveStorage storage, SaveOptions options)
        {
            _registry = registry;
            _options = options;
            _queue = new SaveQueue(storage);
            _appVersion = Application.version;
        }

        public LoadStatus GlobalStatus { get; private set; }
        public LoadStatus DeviceStatus { get; private set; }
        public bool HasSession => _registry.Current(SaveDocument.Slot) != null;
        public string ActiveSlot { get; private set; }
        public bool IsDevSession { get; private set; }

        /// <summary>False when the game was set up without slots (<see cref="SaveOptions.UsesSlots"/>): slot operations throw.</summary>
        public bool UsesSlots => _options.UsesSlots;

        public TimeSpan PlayTime => TimeSpan.FromSeconds(_playTimeSeconds);

        public async UniTask<IReadOnlyList<SlotInfo>> ListSlots(CancellationToken cancellation = default)
        {
            EnsureUsesSlots();
            var slots = new List<SlotInfo>();
            foreach (var entry in await _queue.List(SLOT_ENTRY_PREFIX).AttachExternalCancellation(cancellation))
            {
                var id = entry.Substring(SLOT_ENTRY_PREFIX.Length);
                if (!_slotIdPattern.IsMatch(id))
                    continue;
                var read = await _queue.Read(entry).AttachExternalCancellation(cancellation);
                slots.Add(new SlotInfo(id, SaveFormat.Interpret(read, _options.Slot)));
            }
            return slots
                .OrderByDescending(slot => slot.SavedUtc ?? DateTime.MinValue)
                .ThenBy(slot => slot.Id, StringComparer.Ordinal)
                .ToList();
        }

        public async UniTask<LoadResult> LoadSlot(string slotId, CancellationToken cancellation = default)
        {
            EnsureUsesSlots();
            var entry = SlotEntry(slotId);
            EnsureNotLoading();
            _loading = true;
            try
            {
                var read = await ReadDocument(entry, _options.Slot, cancellation);
                if (!read.IsLoaded)
                {
                    LogLoad(entry, read.Status, read.Error, writable: true);
                    return new LoadResult(read.Status, read.Error);
                }

                var document = new DocumentState(SaveDocument.Slot, entry, writable: true, read.Document.Sections)
                {
                    WrittenContent = SaveFormat.Snapshot(read.Document.Sections),
                };
                var playTime = SaveFormat.ReadPlayTime(read.Document.Meta);
                List<(ISavable, object)> prepared;
                try
                {
                    prepared = _registry.Prepare(document);
                }
                catch (SaveDataException exception)
                {
                    // The session stays as it was: nothing has been restored yet.
                    LogLoad(entry, LoadStatus.Failed, exception, writable: true);
                    return new LoadResult(LoadStatus.Failed, exception);
                }

                _registry.Activate(document, prepared);
                ActiveSlot = slotId;
                IsDevSession = false;
                _playTimeSeconds = playTime;
                LogLoad(entry, read.Status, read.Error, writable: true);
                return new LoadResult(read.Status, read.Error);
            }
            finally
            {
                _loading = false;
            }
        }

        public void NewSlot(string slotId)
        {
            EnsureUsesSlots();
            var entry = SlotEntry(slotId);
            EnsureNotLoading();
            var document = new DocumentState(SaveDocument.Slot, entry, writable: true, new JObject());
            _registry.Activate(document, _registry.Prepare(document));
            ActiveSlot = slotId;
            IsDevSession = false;
            _playTimeSeconds = 0d;
            Log.Info($"Save: new game in slot '{slotId}'");
        }

        public UniTask<SaveResult> SaveSlot(CancellationToken cancellation = default)
        {
            EnsureUsesSlots();
            if (IsDevSession)
                return SaveSlot(null, cancellation);
            if (ActiveSlot == null)
                throw new InvalidOperationException("No slot is open. Call LoadSlot or NewSlot first.");
            return SaveSlot(ActiveSlot, cancellation);
        }

        public UniTask<SaveResult> SaveSlot(string slotId, CancellationToken cancellation = default)
        {
            EnsureUsesSlots();
            EnsureNotLoading();
            var session = _registry.Current(SaveDocument.Slot) ??
                          throw new InvalidOperationException("No slot is open. Call LoadSlot or NewSlot first.");
            if (IsDevSession)
            {
                Log.Info("Save: dev session, slot not written. Start Play from the menu to test saving.");
                return UniTask.FromResult(new SaveResult(SaveStatus.Skipped));
            }

            // The token only stops the caller's wait: a save as that reaches the storage still moves the session.
            return SaveSlotCore(session, slotId, SlotEntry(slotId)).AttachExternalCancellation(cancellation);
        }

        private async UniTask<SaveResult> SaveSlotCore(DocumentState session, string slotId, string entry)
        {
            var result = await Write(session, entry, onlyIfChanged: false);

            // Save as: the session moves to the new slot once it is written there; on failure it stays where it was. The
            // same document object moves, so a scene that unloaded during the write keeps its captured state.
            if (result.IsWritten && entry != session.Entry && _registry.Current(SaveDocument.Slot) == session)
            {
                session.Entry = entry;
                ActiveSlot = slotId;
            }
            return result;
        }

        public void CloseSlot()
        {
            EnsureNotLoading();
            _registry.SetCurrent(SaveDocument.Slot, null);
            ActiveSlot = null;
            IsDevSession = false;
            _playTimeSeconds = 0d;
        }

        public async UniTask<SaveResult> DeleteSlot(string slotId, CancellationToken cancellation = default)
        {
            EnsureUsesSlots();
            var entry = SlotEntry(slotId);
            EnsureNotLoading();
            if (slotId == ActiveSlot)
                CloseSlot();
            try
            {
                await _queue.Delete(entry).AttachExternalCancellation(cancellation);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                Log.Error($"Save: deleting {entry} failed: {exception}");
                return new SaveResult(SaveStatus.Failed, exception);
            }
            Log.Info($"Save: deleted {entry}");
            return new SaveResult(SaveStatus.Written);
        }

        public async UniTask<SaveResult> SaveGlobal(CancellationToken cancellation = default)
        {
            var global = WriteShared(SaveDocument.Global, onlyIfChanged: false);
            var device = WriteShared(SaveDocument.Device, onlyIfChanged: false);
            var (globalResult, deviceResult) = await UniTask.WhenAll(global, device).AttachExternalCancellation(cancellation);
            return SaveResult.Combine(globalResult, deviceResult);
        }

        /// <summary>Boot step: loads the global and device documents and restores their savables. Never fails on data.</summary>
        internal async UniTask LoadShared(CancellationToken cancellation)
        {
            GlobalStatus = await LoadShared(SaveDocument.Global, GLOBAL_ENTRY, cancellation);
            DeviceStatus = await LoadShared(SaveDocument.Device, DEVICE_ENTRY, cancellation);
        }

        /// <summary>Gameplay was entered without a slot: keep slot data in memory only.</summary>
        internal void StartDevSession()
        {
            _registry.SetCurrent(SaveDocument.Slot, new DocumentState(SaveDocument.Slot, null, writable: false, new JObject()));
            ActiveSlot = null;
            IsDevSession = true;
            _playTimeSeconds = 0d;
            const string message = "Save: gameplay started without a slot; this session is kept in memory and never written.";
            if (Application.isEditor)
                Log.Info(message);
            else
                Log.Error(message + " Open a slot with LoadSlot or NewSlot before entering gameplay.");
        }

        internal void AddPlayTime(double seconds) => _playTimeSeconds += seconds;

        /// <summary>True while <see cref="LoadSlot"/> runs; other slot operations throw meanwhile.</summary>
        internal bool IsLoadingSlot => _loading;

        /// <summary>
        /// The app is going to the background or quitting: writes what changed and returns only when it is stored, because the
        /// process may be frozen or killed right after.
        /// </summary>
        internal void FlushForSuspend()
        {
            // Each write captures now and queues; FlushSync then stores everything on this thread.
            WriteShared(SaveDocument.Global, onlyIfChanged: true).Forget();
            WriteShared(SaveDocument.Device, onlyIfChanged: true).Forget();

            var session = _registry.Current(SaveDocument.Slot);
            if (_options.AutosaveSlotOnSuspend && session != null && !IsDevSession && !_loading && ActiveSlot != null)
                Write(session, session.Entry, onlyIfChanged: true).Forget();

            _queue.FlushSync();
        }

        private async UniTask<LoadStatus> LoadShared(SaveDocument kind, string entry, CancellationToken cancellation)
        {
            var read = await ReadDocument(entry, _options.SchemaOf(kind), cancellation);
            var status = read.Status;
            var error = read.Error;

            // A document too new, unreadable or unusable is protected for the session: defaults in memory, nothing written.
            var writable = status != LoadStatus.TooNew && status != LoadStatus.ReadFailed && status != LoadStatus.Failed;
            var document = new DocumentState(kind, entry, writable, read.IsLoaded ? read.Document.Sections : new JObject());
            if (read.IsLoaded)
                document.WrittenContent = SaveFormat.Snapshot(document.Sections);

            List<(ISavable, object)> prepared;
            try
            {
                prepared = _registry.Prepare(document);
            }
            catch (SaveDataException exception)
            {
                status = LoadStatus.Failed;
                error = exception;
                document = new DocumentState(kind, entry, writable: false, new JObject());
                prepared = _registry.Prepare(document);
            }

            _registry.Activate(document, prepared);

            // Defaults are not worth a file: written once something changes, so a later build's new defaults still apply.
            if (status == LoadStatus.NotFound)
            {
                try
                {
                    document.WrittenContent = SaveFormat.Snapshot(_registry.Capture(document));
                }
                catch (SaveDataException exception)
                {
                    Log.Exception(exception);
                }
            }

            LogLoad(entry, status, error, document.Writable);
            return status;
        }

        private async UniTask<DocumentRead> ReadDocument(string entry, SaveSchema schema, CancellationToken cancellation)
        {
            var read = SaveFormat.Interpret(await _queue.Read(entry).AttachExternalCancellation(cancellation), schema);
            if (read.Quarantine == QuarantineMode.None)
                return read;

            try
            {
                await _queue.Quarantine(entry, read.Quarantine == QuarantineMode.Move);
            }
            catch (Exception exception)
            {
                // The load result stands; only the kept copy is missing.
                Log.Error($"Save: keeping a copy of the unreadable {entry} failed: {exception}");
            }
            return read;
        }

        private UniTask<SaveResult> WriteShared(SaveDocument kind, bool onlyIfChanged)
        {
            var document = _registry.Current(kind);
            if (document == null)
            {
                if (!onlyIfChanged)
                    Log.Warning($"Save: {kind} data is not loaded yet; await IAppBoot.WaitReady before saving it.");
                return UniTask.FromResult(new SaveResult(SaveStatus.Refused));
            }
            return Write(document, document.Entry, onlyIfChanged);
        }

        /// <summary>Captures the document and writes it to <paramref name="entry"/>: its own entry, or another slot for a save as.</summary>
        private async UniTask<SaveResult> Write(DocumentState document, string entry, bool onlyIfChanged)
        {
            if (!document.Writable)
            {
                if (!onlyIfChanged)
                    Log.Warning($"Save: {entry} is read-only for this session; see the load error logged at startup.");
                return new SaveResult(SaveStatus.Refused);
            }

            // Capture on the main thread, in this frame, into a new object: later changes to the game cannot leak into this write.
            var sections = _registry.Capture(document);
            // Compared as written text: a float captured from the game and the double parsed from the file are equal there.
            var written = SaveFormat.Snapshot(sections);
            if (onlyIfChanged && written == document.WrittenContent)
                return new SaveResult(SaveStatus.Skipped);

            document.Sections = sections;
            var content = SaveFormat.Write(_options.SchemaOf(document.Kind).CurrentVersion, CreateMeta(document.Kind), sections);
            try
            {
                await _queue.Write(entry, content);
            }
            catch (Exception exception)
            {
                Log.Error($"Save: writing {entry} failed; the previous file is intact. {exception}");
                return new SaveResult(SaveStatus.Failed, exception);
            }

            document.WrittenContent = written;
            Log.Info($"Save: wrote {entry}");
            return new SaveResult(SaveStatus.Written);
        }

        private JObject CreateMeta(SaveDocument kind)
        {
            var meta = new JObject
            {
                [SaveFormat.SavedUtcProperty] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                [SaveFormat.AppVersionProperty] = _appVersion,
            };
            if (kind == SaveDocument.Slot)
                meta[SaveFormat.PlayTimeProperty] = Math.Round(_playTimeSeconds, 3);
            return meta;
        }

        private void EnsureUsesSlots()
        {
            if (!_options.UsesSlots)
                throw new InvalidOperationException("This game was set up without slots (SaveOptions.UsesSlots is false).");
        }

        private void EnsureNotLoading()
        {
            if (_loading)
                throw new InvalidOperationException("A slot is still loading; await LoadSlot before starting another slot operation.");
        }

        private static string SlotEntry(string slotId)
        {
            if (slotId == null || !_slotIdPattern.IsMatch(slotId))
                throw new ArgumentException($"Invalid slot id '{slotId}'. Use 1-32 characters of a-z, 0-9, '_' and '-'.", nameof(slotId));
            return SLOT_ENTRY_PREFIX + slotId;
        }

        private static void LogLoad(string entry, LoadStatus status, Exception error, bool writable)
        {
            var readOnly = writable ? string.Empty : " It is read-only for this session.";
            switch (status)
            {
                case LoadStatus.Loaded:
                    Log.Info($"Save: loaded {entry}");
                    break;
                case LoadStatus.NotFound:
                    Log.Info($"Save: {entry} not found; using defaults");
                    break;
                case LoadStatus.RecoveredFromBackup:
                    Log.Error($"Save: {entry} was unreadable and was loaded from its backup. The unreadable file was kept as .corrupt. {error?.Message}");
                    break;
                case LoadStatus.Corrupt:
                    Log.Error($"Save: {entry} and its backup are unreadable. Nothing was deleted; a copy was kept as .corrupt.{readOnly} {error?.Message}");
                    break;
                case LoadStatus.TooNew:
                    Log.Error($"Save: {entry} was written by a newer version of the game and was left untouched.{readOnly} {error?.Message}");
                    break;
                case LoadStatus.ReadFailed:
                    Log.Error($"Save: {entry} could not be read and was left untouched; it is not treated as damaged.{readOnly} {error}");
                    break;
                case LoadStatus.Failed:
                    Log.Error($"Save: {entry} is readable but the game could not use it; the file was left untouched.{readOnly} {error}");
                    break;
            }
        }
    }
}

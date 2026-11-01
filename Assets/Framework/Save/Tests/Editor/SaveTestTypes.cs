using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace AF.Save.Tests
{
    public sealed class CounterData
    {
        public int value;
    }

    /// <summary>A savable holding one number.</summary>
    public class Counter : Savable<CounterData>
    {
        private readonly string _key;
        private readonly SaveDocument _document;
        private readonly int _defaultValue;

        public Counter(string key, SaveDocument document = SaveDocument.Slot, int defaultValue = 0)
        {
            _key = key;
            _document = document;
            _defaultValue = defaultValue;
            Value = defaultValue;
        }

        public int Value { get; set; }
        public override string Key => _key;
        public override SaveDocument Document => _document;

        protected override CounterData Capture() => new CounterData { value = Value };
        protected override void Restore(CounterData data) => Value = data.value;
        protected override CounterData CreateDefault() => new CounterData { value = _defaultValue };
    }

    /// <summary>In-memory storage that runs inline, so service calls complete synchronously in tests.</summary>
    internal sealed class MemoryStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Primary = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Backup = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Corrupt = new Dictionary<string, string>();
        public readonly HashSet<string> FailingReads = new HashSet<string>();
        public readonly HashSet<string> FailingWrites = new HashSet<string>();
        public readonly List<string> Writes = new List<string>();

        public bool IsThreadSafe => false;

        public StorageRead Read(string entry) =>
            FailingReads.Contains(entry)
                ? StorageRead.Failed(new IOException("The file is used by another process."))
                : new StorageRead(Get(Primary, entry), Get(Backup, entry));

        public void Write(string entry, string content)
        {
            if (FailingWrites.Contains(entry))
                throw new IOException("Disk full.");
            if (Primary.TryGetValue(entry, out var previous))
                Backup[entry] = previous;
            Primary[entry] = content;
            Writes.Add(entry);
        }

        public void Quarantine(string entry, bool move)
        {
            if (!Primary.TryGetValue(entry, out var content))
                return;
            Corrupt[entry] = content;
            if (move)
                Primary.Remove(entry);
        }

        public IReadOnlyList<string> List(string prefix) =>
            Primary.Keys.Concat(Backup.Keys).Distinct()
                .Where(entry => entry.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

        public void Delete(string entry)
        {
            Primary.Remove(entry);
            Backup.Remove(entry);
            Corrupt.Remove(entry);
        }

        private static string Get(Dictionary<string, string> copies, string entry) => copies.TryGetValue(entry, out var content) ? content : null;
    }

    internal static class TestDocuments
    {
        public static string Document(int version, string sections = "{}") =>
            $"{{\"schemaVersion\":{version},\"meta\":{{}},\"sections\":{sections}}}";

        /// <summary>Runs a task that completes synchronously (inline storage) and returns its result.</summary>
        public static T Result<T>(this UniTask<T> task) => task.GetAwaiter().GetResult();
    }
}

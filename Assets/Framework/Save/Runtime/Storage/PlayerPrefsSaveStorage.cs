using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using UnityEngine;

namespace AF.Save
{
    /// <summary>
    /// Keeps each entry as PlayerPrefs strings: <c>&lt;entry&gt;</c>, <c>&lt;entry&gt;.bak</c> and <c>&lt;entry&gt;.corrupt</c>.
    /// For small saves (casual mobile games, later WebGL). PlayerPrefs is main-thread only, has platform size limits (1 MB in
    /// total on WebGL; the Windows registry is not meant for large values) and writes every key together on
    /// <c>PlayerPrefs.Save()</c>, which each call here ends with.
    /// </summary>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        private const string BACKUP_SUFFIX = ".bak";
        private const string CORRUPT_SUFFIX = ".corrupt";
        private const char INDEX_SEPARATOR = '\n';

        private readonly string _keyPrefix;

        public PlayerPrefsSaveStorage() : this(string.Empty)
        {
        }

        // Tests use their own prefix so they never touch the project's real entries.
        internal PlayerPrefsSaveStorage(string keyPrefix) => _keyPrefix = keyPrefix;

        public bool IsThreadSafe => false;

        public StorageRead Read(string entry) => new StorageRead(Get(Key(entry)), Get(Key(entry) + BACKUP_SUFFIX));

        public void Write(string entry, string content)
        {
            var key = Key(entry);
            if (PlayerPrefs.HasKey(key))
                PlayerPrefs.SetString(key + BACKUP_SUFFIX, PlayerPrefs.GetString(key));
            PlayerPrefs.SetString(key, content);

            var index = ReadIndex();
            if (index.Add(entry))
                WriteIndex(index);
            PlayerPrefs.Save();
        }

        public void Quarantine(string entry, bool move)
        {
            var key = Key(entry);
            if (!PlayerPrefs.HasKey(key))
                return;
            PlayerPrefs.SetString(key + CORRUPT_SUFFIX, PlayerPrefs.GetString(key));
            if (move)
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public IReadOnlyList<string> List(string prefix) =>
            ReadIndex()
                .Where(entry => entry.StartsWith(prefix, StringComparison.Ordinal))
                .Where(entry => PlayerPrefs.HasKey(Key(entry)) || PlayerPrefs.HasKey(Key(entry) + BACKUP_SUFFIX))
                .ToList();

        public void Delete(string entry)
        {
            var key = Key(entry);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + BACKUP_SUFFIX);
            PlayerPrefs.DeleteKey(key + CORRUPT_SUFFIX);

            var index = ReadIndex();
            if (index.Remove(entry))
                WriteIndex(index);
            PlayerPrefs.Save();
        }

        private string Key(string entry) => _keyPrefix + entry;

        private string IndexKey => _keyPrefix + FrameworkConstants.SaveIndexKey;

        private static string Get(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        private SortedSet<string> ReadIndex()
        {
            var value = PlayerPrefs.GetString(IndexKey, string.Empty);
            return new SortedSet<string>(value.Split(new[] { INDEX_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        }

        private void WriteIndex(SortedSet<string> index)
        {
            if (index.Count == 0)
                PlayerPrefs.DeleteKey(IndexKey);
            else
                PlayerPrefs.SetString(IndexKey, string.Join(INDEX_SEPARATOR.ToString(), index));
        }
    }
}

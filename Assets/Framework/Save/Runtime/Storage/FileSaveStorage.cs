using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AF.Core;
using UnityEngine;

namespace AF.Save
{
    /// <summary>
    /// Keeps each entry as <c>&lt;root&gt;/&lt;entry&gt;.json</c>, with <c>.bak</c> (previous write), <c>.tmp</c> (write in
    /// progress) and <c>.corrupt</c> (quarantined) next to it. The default storage.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string EXTENSION = FrameworkConstants.SaveFileExtension;
        private const string BACKUP_SUFFIX = ".bak";
        private const string TEMP_SUFFIX = ".tmp";
        private const string CORRUPT_SUFFIX = ".corrupt";
        private const int Attempts = 3;
        private const int RetryDelayMilliseconds = 50;

        private static readonly Encoding _utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly string _root;

        /// <summary>Stores under <c>Application.persistentDataPath</c>. Construct on the main thread.</summary>
        public FileSaveStorage() : this(Application.persistentDataPath)
        {
        }

        public FileSaveStorage(string root) => _root = root;

        public bool IsThreadSafe => true;

        public StorageRead Read(string entry)
        {
            var path = PathOf(entry);
            try
            {
                return new StorageRead(ReadIfExists(path), ReadIfExists(path + BACKUP_SUFFIX));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return StorageRead.Failed(exception);
            }
        }

        public void Write(string entry, string content)
        {
            var path = PathOf(entry);
            var temp = path + TEMP_SUFFIX;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Retry(() =>
            {
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var bytes = _utf8.GetBytes(content);
                    stream.Write(bytes, 0, bytes.Length);
                    // Through the OS cache to the disk, so a power loss right after the replace cannot leave an empty file.
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(path))
                    File.Replace(temp, path, path + BACKUP_SUFFIX, ignoreMetadataErrors: true);
                else
                    File.Move(temp, path);
            });
        }

        public void Quarantine(string entry, bool move)
        {
            var path = PathOf(entry);
            Retry(() =>
            {
                if (!File.Exists(path))
                    return;
                File.Copy(path, path + CORRUPT_SUFFIX, overwrite: true);
                if (move)
                    File.Delete(path);
            });
        }

        public IReadOnlyList<string> List(string prefix)
        {
            var slash = prefix.LastIndexOf('/');
            var folder = slash < 0 ? string.Empty : prefix.Substring(0, slash);
            var namePrefix = prefix.Substring(slash + 1);
            var directory = Path.Combine(_root, ToPlatformPath(folder));
            if (!Directory.Exists(directory))
                return Array.Empty<string>();

            var entries = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var name = Path.GetFileName(file);
                var stem = StemOf(name, EXTENSION + BACKUP_SUFFIX) ?? StemOf(name, EXTENSION);
                if (stem != null && stem.StartsWith(namePrefix, StringComparison.Ordinal))
                    entries.Add(folder.Length == 0 ? stem : folder + "/" + stem);
            }
            return entries.ToList();
        }

        public void Delete(string entry)
        {
            var path = PathOf(entry);
            Retry(() =>
            {
                foreach (var file in new[] { path, path + BACKUP_SUFFIX, path + TEMP_SUFFIX, path + CORRUPT_SUFFIX })
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            });
        }

        private string PathOf(string entry) => Path.Combine(_root, ToPlatformPath(entry) + EXTENSION);

        private static string ToPlatformPath(string entry) => entry.Replace('/', Path.DirectorySeparatorChar);

        private static string StemOf(string fileName, string suffix) =>
            fileName.EndsWith(suffix, StringComparison.Ordinal) ? fileName.Substring(0, fileName.Length - suffix.Length) : null;

        private static string ReadIfExists(string path)
        {
            string content = null;
            Retry(() => content = File.Exists(path) ? File.ReadAllText(path, _utf8) : null);
            return content;
        }

        // Antivirus scanners and cloud sync clients hold files briefly; a short retry rides that out. A lock that outlasts it
        // is reported as a failure, never as missing or damaged data.
        private static void Retry(Action action)
        {
            for (var attempt = 1;; attempt++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception exception) when (attempt < Attempts && IsTransient(exception))
                {
                    Thread.Sleep(RetryDelayMilliseconds * attempt);
                }
            }
        }

        private static bool IsTransient(Exception exception) =>
            exception is UnauthorizedAccessException ||
            exception is IOException && !(exception is FileNotFoundException) && !(exception is DirectoryNotFoundException);
    }
}

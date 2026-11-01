using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AF.Build.Editor
{
    /// <summary>Reads the commit and working tree state for build reports. Missing git is not an error: the report says so.</summary>
    internal static class Git
    {
        private const int TimeoutMilliseconds = 10000;

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>Short hash of HEAD, or null when git is unavailable.</summary>
        public static string Commit() => TryRun("rev-parse --short HEAD", out var output) ? output.Trim() : null;

        /// <summary>
        /// <c>git status --porcelain</c> lines, untracked files listed one by one; null when git is unavailable or the status
        /// cannot be read (for example while another tool holds the index lock).
        /// </summary>
        public static IReadOnlyList<string> Status() =>
            TryRun("status --porcelain --untracked-files=all", out var output)
                ? output.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 3).ToList()
                : null;

        /// <summary>The path of a porcelain line: <c>XY path</c>, or the new path of <c>XY old -> new</c>.</summary>
        public static string PathOf(string statusLine)
        {
            var path = statusLine.Substring(3);
            var arrow = path.IndexOf(" -> ", StringComparison.Ordinal);
            if (arrow >= 0)
                path = path.Substring(arrow + 4);
            return path.Trim('"');
        }

        private static bool TryRun(string arguments, out string output)
        {
            output = null;
            try
            {
                // stderr is not redirected: an unread redirected stream can fill up and block git.
                var start = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(start);
                if (process == null)
                    return false;
                var reading = process.StandardOutput.ReadToEndAsync();
                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    process.Kill();
                    return false;
                }
                output = reading.Result;
                return process.ExitCode == 0;
            }
            catch (Exception)
            {
                // No git on PATH or not a repository: the report shows the state as unknown.
                return false;
            }
        }
    }
}

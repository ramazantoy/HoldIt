using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Editor;
using UnityEditor;

namespace AF.Build.Editor
{
    public enum BuildStatus
    {
        /// <summary>A pre-build check failed; nothing was built and the previous build was left in place.</summary>
        Refused,
        Failed,
        Succeeded,
    }

    /// <summary>The result of <see cref="Builder.Build(BuildRequest)"/>. <see cref="ToString"/> is the report written next to the build.</summary>
    public sealed class BuildOutcome
    {
        private readonly List<string> _notes = new List<string>();

        public BuildOutcome(BuildRequest request, BuildTarget target)
        {
            Kind = request.Kind;
            Target = target;
        }

        public BuildKind Kind { get; }
        public BuildTarget Target { get; }
        public BuildStatus Status { get; set; } = BuildStatus.Refused;
        public ValidationReport Checks { get; set; } = new ValidationReport();
        public string Location { get; set; }
        public ulong SizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public string Version { get; set; }
        public string Commit { get; set; }
        public string Backend { get; set; }
        public string Stripping { get; set; }
        public string[] Defines { get; set; } = Array.Empty<string>();

        /// <summary>Post-build findings: moved debug folders, Pipeline runtime presence, files the build changed.</summary>
        public IReadOnlyList<string> Notes => _notes;

        public void AddNote(string note) => _notes.Add(note);

        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append($"{Kind} build for {Target}: {Status}");
            if (Status != BuildStatus.Refused)
                text.Append($" in {(int)Duration.TotalMinutes}:{Duration.Seconds:00}");
            text.AppendLine();
            if (Location != null)
                text.AppendLine($"Output: {Location}" +
                                (SizeBytes > 0 ? string.Format(CultureInfo.InvariantCulture, " ({0:0.0} MB)", SizeBytes / (1024.0 * 1024.0)) : string.Empty));
            text.AppendLine($"Version {Version} | commit {Commit ?? "unknown (git unavailable)"} | {Backend} | stripping {Stripping} | " +
                            $"defines {(Defines.Length > 0 ? string.Join(";", Defines) : "none")}");
            text.AppendLine($"Checks: {Checks}");
            if (_notes.Count > 0)
            {
                text.AppendLine("Notes:");
                foreach (var note in _notes)
                    text.AppendLine("- " + note);
            }
            return text.ToString();
        }
    }
}

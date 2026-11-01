using System.Collections.Generic;
using System.Linq;
using System.Text;
using Object = UnityEngine.Object;

namespace AF.Core.Editor
{
    public enum ValidationSeverity
    {
        Warning,
        Error,
    }

    public readonly struct ValidationIssue
    {
        public readonly ValidationSeverity Severity;
        public readonly string Message;
        public readonly Object Context;

        public ValidationIssue(ValidationSeverity severity, string message, Object context)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    public sealed class ValidationReport
    {
        private readonly List<ValidationIssue> _issues = new List<ValidationIssue>();

        public IReadOnlyList<ValidationIssue> Issues => _issues;
        public int ErrorCount => _issues.Count(issue => issue.Severity == ValidationSeverity.Error);
        public int WarningCount => _issues.Count(issue => issue.Severity == ValidationSeverity.Warning);

        /// <param name="message">Self-contained: include the scene or asset path, the context object may not outlive the check.</param>
        public void Error(string message, Object context = null) =>
            _issues.Add(new ValidationIssue(ValidationSeverity.Error, message, context));

        public void Warning(string message, Object context = null) =>
            _issues.Add(new ValidationIssue(ValidationSeverity.Warning, message, context));

        public void LogToConsole(string title)
        {
            foreach (var issue in _issues)
            {
                if (issue.Severity == ValidationSeverity.Error)
                    Log.Error($"{title}: {issue.Message}", issue.Context);
                else
                    Log.Warning($"{title}: {issue.Message}", issue.Context);
            }
            Log.Info($"{title}: {ErrorCount} error(s), {WarningCount} warning(s).");
        }

        public override string ToString()
        {
            var text = new StringBuilder($"{ErrorCount} error(s), {WarningCount} warning(s)");
            foreach (var issue in _issues)
                text.Append('\n').Append(issue);
            return text.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AF.Core;
using AF.Core.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace AF.Build.Editor
{
    /// <summary>
    /// The only way to build. Builds the active target: pre-build checks, then the player with the kind's options and
    /// defines, then post-build checks and a report next to the output. No dialogs, so the CLI can drive it.
    /// </summary>
    public static class Builder
    {
        private const string PIPELINE_RUNTIME_TYPE = "RuntimePipelineServer";
        private const string IL2CPP_METADATA = "global-metadata.dat";
        private const string UNREADABLE_FILE = "(unreadable)";

        private static readonly string[] _doNotShipSuffixes = { "_BackUpThisFolder_ButDontShipItWithYourGame", "_BurstDebugInformation_DoNotShip" };

        /// <summary>Runs every pre-build check for the active target without building.</summary>
        public static ValidationReport Check(BuildRequest request) =>
            Check(request, BuildEnvironment.Capture(EditorUserBuildSettings.activeBuildTarget));

        public static BuildOutcome Build(BuildKind kind) => Build(new BuildRequest(kind));

        public static BuildOutcome Build(BuildRequest request)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var kind = request.Kind;
            var root = Git.ProjectRoot;
            var environment = BuildEnvironment.Capture(target);
            var outcome = new BuildOutcome(request, target)
            {
                Version = PlayerSettings.bundleVersion,
                Commit = Git.Commit(),
                Backend = environment.Backend.ToString(),
                Stripping = environment.Stripping.ToString(),
                Defines = BuildKinds.Defines(kind),
            };
            if (outcome.Commit != null && environment.GitStatus == null)
                outcome.Commit += " (working tree state unknown)";
            else if (outcome.Commit != null && environment.GitStatus.Count > 0)
                outcome.Commit += "-dirty";

            var supported = BuildPaths.IsSupported(target);
            try
            {
                Run(request, environment, outcome, root, supported);
            }
            catch (Exception exception)
            {
                // A step around the player build (deleting the old report, the checks) still ends in a report.
                outcome.Status = BuildStatus.Failed;
                outcome.AddNote($"The build stopped with {exception.GetType().Name}: {exception.Message} Nothing was built; a previous " +
                                "build in the folder is unchanged.");
                Log.Exception(exception);
            }
            finally
            {
                Finish(outcome, root, supported);
            }
            return outcome;
        }

        private static void Run(BuildRequest request, BuildEnvironment environment, BuildOutcome outcome, string root, bool supported)
        {
            var target = environment.Target;
            var kind = request.Kind;

            // An old report must never sit next to a build it does not describe.
            if (supported)
                BuildPaths.Delete(root, BuildPaths.ReportFile(target, kind));

            outcome.Checks = Check(request, environment);
            if (outcome.Checks.ErrorCount > 0)
                return;

            var outputFolder = BuildPaths.OutputFolder(target, kind);
            var symbolsFolder = BuildPaths.SymbolsFolder(target, kind);
            var filesBefore = HashChangedFiles(root, environment.GitStatus);
            // Before the previous build is deleted: content that fails to build leaves it in place.
            if (environment.UsesAddressables && !BuildContent(outcome))
            {
                NoteChangedFiles(outcome, root, filesBefore);
                return;
            }
            try
            {
                BuildPaths.Delete(root, outputFolder);
                BuildPaths.Delete(root, symbolsFolder);
                outcome.Location = BuildPaths.Location(target, kind, environment.ProductName);
                var report = BuildPlayer(target, kind, outcome.Location);

                outcome.Duration = report.summary.totalTime;
                outcome.SizeBytes = report.summary.totalSize;
                outcome.Status = report.summary.result == BuildResult.Succeeded ? BuildStatus.Succeeded : BuildStatus.Failed;
                if (outcome.Status == BuildStatus.Failed)
                    outcome.AddNote($"Unity reported {report.summary.result} with {report.summary.totalErrors} error(s); see the console.");
                else
                    AfterBuild(outcome, root, Path.Combine(root, outputFolder), Path.Combine(root, symbolsFolder), environment.UsesAddressables);
            }
            catch (Exception exception)
            {
                // A locked file (a player from this folder still running) or a failed move must still end in a report.
                outcome.Status = BuildStatus.Failed;
                outcome.AddNote($"The build stopped with {exception.GetType().Name}: {exception.Message} " +
                                "If a player from this folder is still running, close it and build again.");
                Log.Exception(exception);
            }

            if (outcome.Status == BuildStatus.Failed && kind == BuildKind.Release)
                DiscardFailedRelease(outcome, root, outputFolder);
            NoteChangedFiles(outcome, root, filesBefore);
        }

        private static BuildReport BuildPlayer(BuildTarget target, BuildKind kind, string location)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = location,
                target = target,
                targetGroup = group,
                subtarget = group == BuildTargetGroup.Standalone ? (int)StandaloneBuildSubtarget.Player : 0,
                options = BuildKinds.Options(kind),
                extraScriptingDefines = BuildKinds.Defines(kind),
            };

            BuildSession.Begin();
            try
            {
                return BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                BuildSession.End();
            }
        }

        private static bool BuildContent(BuildOutcome outcome)
        {
            try
            {
                outcome.AddNote(AddressablesContent.Build(out var succeeded));
                if (!succeeded)
                    outcome.Status = BuildStatus.Failed;
                return succeeded;
            }
            catch (Exception exception)
            {
                outcome.Status = BuildStatus.Failed;
                outcome.AddNote($"Addressables content: the build stopped with {exception.GetType().Name}: {exception.Message}");
                Log.Exception(exception);
                return false;
            }
        }

        /// <summary>A failed Release must not be shipped by mistake, so its shipping folder goes; the report stays.</summary>
        private static void DiscardFailedRelease(BuildOutcome outcome, string root, string outputFolder)
        {
            try
            {
                BuildPaths.Delete(root, outputFolder);
                outcome.AddNote($"Deleted {outputFolder}/ so the failed Release cannot be shipped.");
            }
            catch (Exception exception)
            {
                outcome.AddNote($"Could not delete {outputFolder}/ ({exception.Message}). Do not ship it.");
            }
        }

        internal static ValidationReport Check(BuildRequest request, BuildEnvironment environment)
        {
            var report = new ValidationReport();
            BuildChecks.Evaluate(request, environment, report);
            if (environment.PlayingOrCompiling)
                return report;

            foreach (var issue in ValidateAll.Run().Issues)
            {
                if (issue.Severity == ValidationSeverity.Error)
                    report.Error("Validate All: " + issue.Message, issue.Context);
                else
                    report.Warning("Validate All: " + issue.Message, issue.Context);
            }

            if (BuildPaths.IsSupported(environment.Target) && environment.TargetInstalled)
            {
                var playerAssemblies = SerializedLayoutCheck.Run(environment.Target, request.Kind, report);
                BuildChecks.EvaluatePlayerAssemblies(request, playerAssemblies, report);
            }
            return report;
        }

        private static void AfterBuild(BuildOutcome outcome, string root, string outputFolder, string symbolsFolder, bool usesAddressables)
        {
            if (BuildPaths.IsPackage(outcome.Target))
            {
                // An .apk or .aab: its contents are zipped. The pre-build check already refused a Release that compiles the runtime in.
                var package = Path.Combine(root, outcome.Location);
                if (File.Exists(package))
                    outcome.SizeBytes = (ulong)new FileInfo(package).Length;
                outcome.AddNote("The output is a package; it was not searched for the Pipeline runtime or the Addressables content.");
                return;
            }

            if (usesAddressables && !AddressablesContent.IsIncluded(outputFolder))
            {
                // Without it Unity Localization finds no locale and the game stays in its boot.
                outcome.Status = BuildStatus.Failed;
                outcome.AddNote("The Addressables content is missing from the build (StreamingAssets/aa/settings.json).");
            }
            else if (usesAddressables)
            {
                outcome.AddNote("Addressables content: included.");
            }

            foreach (var folder in Directory.GetDirectories(outputFolder).Where(folder => _doNotShipSuffixes.Any(folder.EndsWith)))
            {
                Directory.CreateDirectory(symbolsFolder);
                var destination = Path.Combine(symbolsFolder, Path.GetFileName(folder));
                Directory.Move(folder, destination);
                outcome.AddNote($"Moved {Path.GetFileName(folder)} out of the shipping folder, to {Path.GetFileName(symbolsFolder)}/.");
            }
            // Unity's total counts the debug folders too; report what ships.
            outcome.SizeBytes = (ulong)new DirectoryInfo(outputFolder).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);

            var pipelineIncluded = ContainsPipelineRuntime(outputFolder);
            if (outcome.Kind == BuildKind.Release && pipelineIncluded)
            {
                outcome.Status = BuildStatus.Failed;
                outcome.AddNote("The Pipeline runtime is in this Release build: it carries a local HTTP server that runs code. " +
                                "Turn off Project Settings > Pipeline > Runtime > Enable in Release Builds.");
            }
            else
            {
                outcome.AddNote($"Pipeline runtime: {(pipelineIncluded ? "included" : "not included")}.");
            }
        }

        /// <summary>The runtime assembly (Mono) or its server type in the IL2CPP metadata.</summary>
        internal static bool ContainsPipelineRuntime(string outputFolder)
        {
            if (Directory.GetFiles(outputFolder, BuildChecks.PipelineRuntimeAssembly, SearchOption.AllDirectories).Length > 0)
                return true;
            var marker = Encoding.ASCII.GetBytes(PIPELINE_RUNTIME_TYPE);
            return Directory.GetFiles(outputFolder, IL2CPP_METADATA, SearchOption.AllDirectories)
                .Any(file => Contains(File.ReadAllBytes(file), marker));
        }

        internal static bool Contains(byte[] data, byte[] marker)
        {
            for (var i = 0; i <= data.Length - marker.Length; i++)
            {
                var match = true;
                for (var j = 0; j < marker.Length && match; j++)
                    match = data[i + j] == marker[j];
                if (match)
                    return true;
            }
            return false;
        }

        /// <summary>Content hashes of the files git lists as changed, so a file already modified and rewritten again shows up.</summary>
        private static Dictionary<string, string> HashChangedFiles(string root, IReadOnlyList<string> status)
        {
            if (status == null)
                return null;
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            using var sha = SHA1.Create();
            foreach (var path in status.Select(Git.PathOf))
            {
                hashes[path] = Hash(sha, Path.Combine(root, path));
            }
            return hashes;
        }

        // A file another process holds (an IDE, a sync client) must not fail a build that already succeeded.
        private static string Hash(SHA1 sha, string path)
        {
            try
            {
                return File.Exists(path) ? Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))) : null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return UNREADABLE_FILE;
            }
        }

        private static void NoteChangedFiles(BuildOutcome outcome, string root, Dictionary<string, string> before)
        {
            var after = HashChangedFiles(root, Git.Status());
            if (before == null || after == null)
            {
                outcome.AddNote("Git was unavailable, so files the build changed in the project are not listed.");
                return;
            }
            var changed = after
                .Where(entry => !before.TryGetValue(entry.Key, out var hash) || hash != entry.Value)
                .Select(entry => entry.Key)
                .ToList();
            if (changed.Count > 0)
                outcome.AddNote("The build changed project files; review them before committing: " + string.Join(", ", changed));
        }

        private static void Finish(BuildOutcome outcome, string root, bool supported)
        {
            if (supported)
            {
                var reportPath = Path.Combine(root, BuildPaths.ReportFile(outcome.Target, outcome.Kind));
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, outcome.ToString());
            }

            if (outcome.Status == BuildStatus.Succeeded)
                Log.Info(outcome.ToString());
            else
                Log.Error(outcome.ToString());
        }
    }
}

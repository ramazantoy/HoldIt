using System.Collections.Generic;
using System.Linq;
using AF.Build.Editor;
using AF.Core.Editor;
using NUnit.Framework;
using UnityEditor;

namespace AF.Build.Tests
{
    public class BuildChecksTests
    {
        [TestCase(BuildKind.Development)]
        [TestCase(BuildKind.Release)]
        public void CleanSetup_HasNoIssues(BuildKind kind)
        {
            var report = Evaluate(kind, Clean());

            Assert.AreEqual(0, report.Issues.Count, report.ToString());
        }

        [Test]
        public void PlayModeOrCompiling_IsAnError() =>
            AssertError(environment => environment.PlayingOrCompiling = true, "Edit Mode");

        [Test]
        public void UnsupportedTarget_IsAnError() =>
            AssertError(environment => environment.Target = BuildTarget.WebGL, "not supported");

        [Test]
        public void MissingBuildSupport_IsAnError() =>
            AssertError(environment => environment.TargetInstalled = false, "not installed");

        [Test]
        public void NoEnabledScene_IsAnError() =>
            AssertError(environment => environment.EnabledSceneCount = 0, "no enabled scene");

        [Test]
        public void UnsavedScene_IsAnError() =>
            AssertError(environment => environment.UnsavedScenes = new[] { "Assets/Game/Scenes/Menu.unity" }, "Menu.unity has unsaved changes");

        [Test]
        public void ActiveCustomBuildProfile_IsAnError() =>
            AssertError(environment => environment.ActiveBuildProfile = "Assets/Settings/Demo.asset", "Demo.asset is active");

        [TestCase("Company")]
        [TestCase("Product")]
        [TestCase("Identifier")]
        public void PlaceholderName_RefusesRelease(string field)
        {
            var report = Evaluate(BuildKind.Release, WithPlaceholder(field));

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("Placeholder names", report.Issues[0].Message);
        }

        [TestCase("Company")]
        [TestCase("Product")]
        [TestCase("Identifier")]
        public void PlaceholderName_WarnsInDevelopment(string field)
        {
            var report = Evaluate(BuildKind.Development, WithPlaceholder(field));

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
            Assert.AreEqual(1, report.WarningCount);
        }

        [Test]
        public void PlaceholderName_AllowedByRequest_IsAWarning()
        {
            var report = new ValidationReport();
            BuildChecks.Evaluate(new BuildRequest(BuildKind.Release) { AllowPlaceholderIdentity = true }, WithPlaceholder("Product"), report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
            StringAssert.Contains("by request", report.Issues.Single().Message);
        }

        [Test]
        public void PlayerSettingsDefine_IsAnError() =>
            AssertError(environment => environment.PlayerSettingsDefines = new Dictionary<string, string> { ["Standalone"] = "DEMO;STEAM" }, "DEMO;STEAM");

        [Test]
        public void PipelineDevelopmentToggle_IsAllowed()
        {
            var environment = Clean();
            environment.PlayerSettingsDefines = new Dictionary<string, string> { ["Standalone"] = "ENABLE_RUNTIME_PIPELINE_DEV", ["Server"] = "ENABLE_RUNTIME_PIPELINE_DEV" };

            Assert.AreEqual(0, Evaluate(BuildKind.Release, environment).Issues.Count);
        }

        [Test]
        public void PipelineReleaseToggle_IsAnError() =>
            AssertError(environment => environment.PlayerSettingsDefines =
                new Dictionary<string, string> { ["Standalone"] = "ENABLE_RUNTIME_PIPELINE_DEV;ENABLE_RUNTIME_PIPELINE" }, "defines ENABLE_RUNTIME_PIPELINE.");

        [Test]
        public void CompilerArgumentDefine_IsAnError() =>
            AssertError(environment => environment.CompilerArguments = new Dictionary<string, string[]> { ["Standalone"] = new[] { "-nowarn:0618", "-define:CHEATS" } },
                "-define:CHEATS");

        [Test]
        public void ResponseFileDefine_IsAnError() =>
            AssertError(environment => environment.DefiningResponseFiles = new[] { "Assets/csc.rsp" }, "Assets/csc.rsp");

        [TestCase("-define:A", true)]
        [TestCase("/define:A;B", true)]
        [TestCase("-d:A", true)]
        [TestCase("  /d:A", true)]
        [TestCase("-warnaserror -define:A", true)]
        [TestCase("-warnaserror\t/d:A", true)]
        [TestCase("-nowarn:0618", false)]
        [TestCase("-debug", false)]
        public void DefinesSymbol_RecognisesDefineArguments(string argument, bool expected) =>
            Assert.AreEqual(expected, BuildEnvironment.DefinesSymbol(argument));

        [TestCase(BuildKind.Development)]
        [TestCase(BuildKind.Release)]
        public void DirtyAssets_AreAWarning(BuildKind kind)
        {
            var environment = Clean();
            environment.DirtyAssets = Enumerable.Range(0, 12).Select(i => $"Assets/Materials/M{i}.mat").ToList();

            var report = Evaluate(kind, environment);

            Assert.AreEqual(0, report.ErrorCount);
            var warning = report.Issues.Single().Message;
            StringAssert.Contains("Assets/Materials/M0.mat", warning);
            StringAssert.Contains("and 2 more", warning);
        }

        [Test]
        public void MonoBackend_WarnsOnlyInRelease()
        {
            var environment = Clean();
            environment.Backend = ScriptingImplementation.Mono2x;

            Assert.AreEqual(0, Evaluate(BuildKind.Development, environment).Issues.Count);
            Assert.AreEqual(1, Evaluate(BuildKind.Release, environment).WarningCount);
        }

        [TestCase(ManagedStrippingLevel.Low, 1)]
        [TestCase(ManagedStrippingLevel.High, 1)]
        [TestCase(ManagedStrippingLevel.Minimal, 0)]
        public void StrippingAboveMinimal_IsAWarning(ManagedStrippingLevel level, int warnings)
        {
            var environment = Clean();
            environment.Stripping = level;

            Assert.AreEqual(warnings, Evaluate(BuildKind.Development, environment).WarningCount);
        }

        [Test]
        public void UncommittedChanges_WarnOnlyInRelease()
        {
            var environment = Clean();
            environment.GitStatus = new[] { " M Assets/Game/Scripts/GameRootScope.cs" };

            Assert.AreEqual(0, Evaluate(BuildKind.Development, environment).Issues.Count);
            Assert.AreEqual(1, Evaluate(BuildKind.Release, environment).WarningCount);

        }

        [Test]
        public void UnknownWorkingTree_WarnsOnlyInRelease()
        {
            var environment = Clean();
            environment.GitStatus = null;

            Assert.AreEqual(0, Evaluate(BuildKind.Development, environment).Issues.Count);
            StringAssert.Contains("state is unknown", Evaluate(BuildKind.Release, environment).Issues.Single().Message);
        }

        [Test]
        public void AddressablesWithoutSettings_IsAnError() =>
            AssertError(environment =>
            {
                environment.UsesAddressables = true;
                environment.AddressablesSettingsExist = false;
            }, "has no settings");

        [Test]
        public void AddressablesBuildingWithThePlayer_IsAnError() =>
            AssertError(environment =>
            {
                environment.UsesAddressables = true;
                environment.AddressablesBuildWithPlayer = true;
            }, "Do not Build Addressables content on Player build");

        [Test]
        public void AddressablesSettings_AreIgnoredWithoutThePackage()
        {
            var environment = Clean();
            environment.AddressablesSettingsExist = false;
            environment.AddressablesBuildWithPlayer = true;

            Assert.AreEqual(0, Evaluate(BuildKind.Release, environment).ErrorCount);
        }

        [Test]
        public void PipelineRuntimeInReleaseAssemblies_IsAnError()
        {
            var assemblies = new[] { "AF.Core.dll", "Unity.Pipeline.Attributes.dll", "Unity.Pipeline.dll" };

            var release = new ValidationReport();
            BuildChecks.EvaluatePlayerAssemblies(new BuildRequest(BuildKind.Release), assemblies, release);
            var development = new ValidationReport();
            BuildChecks.EvaluatePlayerAssemblies(new BuildRequest(BuildKind.Development), assemblies, development);

            Assert.AreEqual(1, release.ErrorCount);
            StringAssert.Contains("Pipeline runtime", release.Issues[0].Message);
            Assert.AreEqual(0, development.Issues.Count);
        }

        [Test]
        public void PipelineAttributesAlone_AreAllowedInRelease()
        {
            var report = new ValidationReport();
            BuildChecks.EvaluatePlayerAssemblies(new BuildRequest(BuildKind.Release), new[] { "Unity.Pipeline.Attributes.dll" }, report);

            Assert.AreEqual(0, report.Issues.Count);
        }

        private static BuildEnvironment Clean() => new BuildEnvironment
        {
            Target = BuildTarget.StandaloneWindows64,
            CompanyName = "Studio",
            ProductName = "Game",
            ApplicationIdentifier = "com.Studio.Game",
        };

        private static BuildEnvironment WithPlaceholder(string field)
        {
            var environment = Clean();
            switch (field)
            {
                case "Company":
                    environment.CompanyName = "DefaultCompany";
                    break;
                case "Product":
                    environment.ProductName = "TemplateProject";
                    break;
                default:
                    environment.ApplicationIdentifier = "com.Unity-Technologies.com.unity.template.urp-blank";
                    break;
            }
            return environment;
        }

        private static ValidationReport Evaluate(BuildKind kind, BuildEnvironment environment)
        {
            var report = new ValidationReport();
            BuildChecks.Evaluate(new BuildRequest(kind), environment, report);
            return report;
        }

        private static void AssertError(System.Action<BuildEnvironment> breakIt, string expected)
        {
            foreach (var kind in new[] { BuildKind.Development, BuildKind.Release })
            {
                var environment = Clean();
                breakIt(environment);
                var report = Evaluate(kind, environment);

                Assert.AreEqual(1, report.ErrorCount, $"{kind}: {report}");
                StringAssert.Contains(expected, report.Issues.First(issue => issue.Severity == ValidationSeverity.Error).Message);
            }
        }
    }
}

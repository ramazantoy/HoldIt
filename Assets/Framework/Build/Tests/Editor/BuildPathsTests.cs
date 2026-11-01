using System;
using System.IO;
using AF.Build.Editor;
using NUnit.Framework;
using UnityEditor;

namespace AF.Build.Tests
{
    public class BuildPathsTests
    {
        private string _projectRoot;

        [SetUp]
        public void SetUp()
        {
            _projectRoot = Path.Combine(Path.GetTempPath(), "BuildPathsTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_projectRoot, "Assets"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_projectRoot))
                Directory.Delete(_projectRoot, recursive: true);
        }

        [Test]
        public void Layout_FollowsPlatformAndKind()
        {
            Assert.AreEqual("Builds/Windows/Development/My Game.exe", BuildPaths.Location(BuildTarget.StandaloneWindows64, BuildKind.Development, "My Game"));
            Assert.AreEqual("Builds/Linux/Release/Game.x86_64", BuildPaths.Location(BuildTarget.StandaloneLinux64, BuildKind.Release, "Game"));
            Assert.AreEqual("Builds/Windows/Release-symbols", BuildPaths.SymbolsFolder(BuildTarget.StandaloneWindows64, BuildKind.Release));
            Assert.AreEqual("Builds/Windows/Release-report.txt", BuildPaths.ReportFile(BuildTarget.StandaloneWindows64, BuildKind.Release));
        }

        [Test]
        public void ProductName_IsMadeAValidFileName() =>
            Assert.AreEqual("Builds/Windows/Release/A_B_.exe", BuildPaths.Location(BuildTarget.StandaloneWindows64, BuildKind.Release, "A:B?"));

        [TestCase(BuildTarget.Android, true)]
        [TestCase(BuildTarget.StandaloneWindows64, false)]
        [TestCase(BuildTarget.StandaloneOSX, false)]
        [TestCase(BuildTarget.iOS, false)]
        public void OnlyAndroid_BuildsAPackage(BuildTarget target, bool expected) =>
            Assert.AreEqual(expected, BuildPaths.IsPackage(target));

        [Test]
        public void UnsupportedTarget_HasNoFolder()
        {
            Assert.IsNull(BuildPaths.PlatformFolder(BuildTarget.WebGL));
            Assert.IsFalse(BuildPaths.IsSupported(BuildTarget.WebGL));
        }

        [TestCase("Builds/Windows/Development", true)]
        [TestCase("Builds/Windows/Development-report.txt", true)]
        [TestCase("Builds", false)]
        [TestCase("Builds/", false)]
        [TestCase("", false)]
        [TestCase("Assets", false)]
        [TestCase("Builds2/Windows", false)]
        [TestCase("Builds/../Assets", false)]
        public void OnlyPathsInsideBuilds_AreInsideTheRoot(string path, bool expected) =>
            Assert.AreEqual(expected, BuildPaths.IsInsideRoot(_projectRoot, path));

        [Test]
        public void AbsolutePathElsewhere_IsOutsideTheRoot() =>
            Assert.IsFalse(BuildPaths.IsInsideRoot(_projectRoot, Path.Combine(Path.GetTempPath(), "Builds", "Windows")));

        [Test]
        public void Delete_RefusesPathsOutsideBuilds()
        {
            Assert.Throws<InvalidOperationException>(() => BuildPaths.Delete(_projectRoot, "Assets"));
            Assert.Throws<InvalidOperationException>(() => BuildPaths.Delete(_projectRoot, "Builds"));
            Assert.IsTrue(Directory.Exists(Path.Combine(_projectRoot, "Assets")));
        }

        [Test]
        public void Delete_RemovesFoldersAndFilesInsideBuilds()
        {
            var folder = Path.Combine(_projectRoot, "Builds", "Windows", "Release");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Game.exe"), "x");
            var report = Path.Combine(_projectRoot, "Builds", "Windows", "Release-report.txt");
            File.WriteAllText(report, "x");

            BuildPaths.Delete(_projectRoot, "Builds/Windows/Release");
            BuildPaths.Delete(_projectRoot, "Builds/Windows/Release-report.txt");
            BuildPaths.Delete(_projectRoot, "Builds/Windows/Missing");

            Assert.IsFalse(Directory.Exists(folder));
            Assert.IsFalse(File.Exists(report));
        }
    }
}

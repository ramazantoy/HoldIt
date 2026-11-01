using System.Linq;
using AF.Build.Editor;
using NUnit.Framework;
using UnityEditor;

namespace AF.Build.Tests
{
    public class BuildKindsTests
    {
        [Test]
        public void Development_IsADevelopmentPlayerWithLogs()
        {
            Assert.IsTrue((BuildKinds.Options(BuildKind.Development) & BuildOptions.Development) != 0);
            CollectionAssert.AreEqual(new[] { "SHOW_LOGS" }, BuildKinds.Defines(BuildKind.Development));
        }

        [Test]
        public void Release_HasNoDevelopmentPlayerAndNoDefines()
        {
            Assert.IsTrue((BuildKinds.Options(BuildKind.Release) & BuildOptions.Development) == 0);
            CollectionAssert.IsEmpty(BuildKinds.Defines(BuildKind.Release));
        }

        [TestCase(BuildKind.Development)]
        [TestCase(BuildKind.Release)]
        public void NoKind_DefinesThePipelineToggles(BuildKind kind) =>
            Assert.IsFalse(BuildKinds.Defines(kind).Any(symbol => symbol.StartsWith("ENABLE_RUNTIME_PIPELINE")));
    }
}

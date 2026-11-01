using System.Text;
using AF.Build.Editor;
using AF.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace AF.Build.Tests
{
    public class BuildGuardTests
    {
        [Test]
        public void BuildOutsideTheBuilder_IsRefused()
        {
            Assert.IsFalse(BuildGuard.IsAllowed(false, BuildOptions.None));
            Assert.IsFalse(BuildGuard.IsAllowed(false, BuildOptions.Development));
            var exception = Assert.Throws<BuildFailedException>(() => BuildGuard.Enforce(false, BuildOptions.None));
            StringAssert.Contains(FrameworkConstants.MenuRoot + "Build", exception.Message);
        }

        [Test]
        public void BuildFromTheBuilder_Passes()
        {
            Assert.IsTrue(BuildGuard.IsAllowed(true, BuildOptions.None));
            Assert.DoesNotThrow(() => BuildGuard.Enforce(true, BuildOptions.Development));
        }

        [Test]
        public void TestRunnerPlayerBuild_Passes() =>
            Assert.IsTrue(BuildGuard.IsAllowed(false, BuildOptions.IncludeTestAssemblies | BuildOptions.Development));

        [Test]
        public void BuildSession_IsOnlyActiveWhileMarked()
        {
            Assert.IsFalse(BuildSession.IsActive);
            BuildSession.Begin();
            try
            {
                Assert.IsTrue(BuildSession.IsActive);
            }
            finally
            {
                BuildSession.End();
            }
            Assert.IsFalse(BuildSession.IsActive);
        }

        [Test]
        public void PipelineMarker_IsFoundInBinaryData()
        {
            var data = Encoding.ASCII.GetBytes("\0\0Unity.Pipeline.Attributes\0RuntimePipelineServer\0");

            Assert.IsTrue(Builder.Contains(data, Encoding.ASCII.GetBytes("RuntimePipelineServer")));
            Assert.IsFalse(Builder.Contains(Encoding.ASCII.GetBytes("\0Unity.Pipeline.Attributes\0"), Encoding.ASCII.GetBytes("RuntimePipelineServer")));
        }
    }
}

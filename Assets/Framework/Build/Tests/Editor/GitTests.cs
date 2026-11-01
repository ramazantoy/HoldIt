using AF.Build.Editor;
using NUnit.Framework;

namespace AF.Build.Tests
{
    public class GitTests
    {
        [TestCase(" M Assets/Settings/PC_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset")]
        [TestCase("?? Assets/Framework/Build/Editor/Git.cs", "Assets/Framework/Build/Editor/Git.cs")]
        [TestCase("R  Assets/Old.cs -> Assets/New.cs", "Assets/New.cs")]
        [TestCase("?? \"Assets/With Space.txt\"", "Assets/With Space.txt")]
        public void PathOf_ReadsThePorcelainPath(string line, string expected) =>
            Assert.AreEqual(expected, Git.PathOf(line));
    }
}

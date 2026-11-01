using System;
using System.IO;
using AF.Build.Editor;
using NUnit.Framework;

namespace AF.Build.Tests
{
    public class AddressablesContentTests
    {
        [Test]
        public void AddressablesContent_IsFoundUnderStreamingAssets()
        {
            var output = Path.Combine(Path.GetTempPath(), "af-build-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var data = Directory.CreateDirectory(Path.Combine(output, "Game_Data", "StreamingAssets", "aa"));
                Assert.IsFalse(AddressablesContent.IsIncluded(output));

                File.WriteAllText(Path.Combine(output, "Game_Data", "settings.json"), "{}");
                Assert.IsFalse(AddressablesContent.IsIncluded(output), "Only aa/settings.json counts.");

                File.WriteAllText(Path.Combine(data.FullName, "settings.json"), "{}");
                Assert.IsTrue(AddressablesContent.IsIncluded(output));
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }
    }
}

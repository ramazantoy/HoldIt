using AF.Save;
using HoldIt.Settings;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class GameSettingsTests
    {
        [Test]
        public void New_AllowsVibration()
        {
            Assert.IsTrue(new GameSettings().Haptics);
        }

        [Test]
        public void Savable_IsTheGlobalSettingsSection()
        {
            ISavable savable = new GameSettings();

            Assert.AreEqual("game.settings", savable.Key);
            Assert.AreEqual(SaveDocument.Global, savable.Document);
            Assert.IsTrue(((GameSettingsData)savable.CreateDefaultData()).haptics);
        }

        [Test]
        public void SetHaptics_IsSaved_AndRestored()
        {
            var settings = new GameSettings();
            settings.SetHaptics(false);
            var data = ((ISavable)settings).CaptureData();

            var loaded = new GameSettings();
            ((ISavable)loaded).RestoreData(data);

            Assert.IsFalse(loaded.Haptics);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AF.Save;
using MessagePipe;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace AF.Settings.Tests
{
    public class DisplaySettingsTests
    {
        private IObjectResolver _container;
        private FakeDisplay _display;
        private FakeInputModes _input;
        private SettingsOptions _options;
        private List<DisplaySettingsChanged> _changes;
        private IDisposable _subscription;

        [SetUp]
        public void SetUp()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<DisplaySettingsChanged>(messagePipe);
            _container = builder.Build();
            _display = new FakeDisplay();
            _input = new FakeInputModes();
            _options = new SettingsOptions();
            _changes = new List<DisplaySettingsChanged>();
            _subscription = _container.Resolve<ISubscriber<DisplaySettingsChanged>>().Subscribe(_changes.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _subscription.Dispose();
            _container.Dispose();
        }

        [Test]
        public void NothingSaved_AppliesTheDesktopDefaults_QualityBeforeFramePacing()
        {
            var settings = Restored(new DisplaySettingsData());

            Assert.AreEqual(new DisplayState(new Vector2Int(1920, 1080), WindowMode.Borderless, true, 0, "PC", false), settings.Current);
            CollectionAssert.AreEqual(new[] { "quality PC", "pacing 1 -1", "window 1920x1080 Borderless" }, _display.Calls,
                "Changing the quality level resets vsync, so frame pacing follows it.");
            Assert.IsTrue(settings.HasWindow);
        }

        [Test]
        public void Resolutions_AreEachSizeOnce_UpToTheDisplay_LargestFirst()
        {
            var settings = Restored(new DisplaySettingsData());

            CollectionAssert.AreEqual(new[] { new Vector2Int(1920, 1080), new Vector2Int(1600, 900), new Vector2Int(1280, 720), new Vector2Int(800, 600) },
                settings.Resolutions);

            settings.SetWindowMode(WindowMode.Windowed);
            CollectionAssert.AreEqual(new[] { new Vector2Int(1600, 900), new Vector2Int(1280, 720), new Vector2Int(800, 600) }, settings.Resolutions,
                "A window as large as the display is not offered.");
            Assert.AreEqual(new Vector2Int(1600, 900), settings.Current.Resolution, "Without a choice, the largest window.");
        }

        [Test]
        public void Windows_FitBesideTheTaskbar_WithATitleBar()
        {
            _display.SupportedSizes = new[] { new Vector2Int(1920, 1080), new Vector2Int(1680, 1050), new Vector2Int(1600, 900), new Vector2Int(1280, 720) };
            _display.WorkArea = new Vector2Int(1920, 1032);
            var settings = Restored(new DisplaySettingsData { windowMode = WindowMode.Windowed });

            CollectionAssert.AreEqual(new[] { new Vector2Int(1600, 900), new Vector2Int(1280, 720) }, settings.Resolutions,
                "1680x1050 is smaller than the display but taller than the area a window has.");
            Assert.AreEqual(new Vector2Int(1600, 900), settings.Current.Resolution);
        }

        [Test]
        public void Choices_EqualToTheDefault_AreSavedAsUnset()
        {
            var settings = Restored(new DisplaySettingsData());

            settings.SetResolution(new Vector2Int(1280, 720));
            settings.SetWindowMode(WindowMode.Exclusive);
            settings.SetVSync(false);
            settings.SetFrameRateLimit(120);
            settings.SetQuality("Low");
            settings.SetConfineCursor(true);
            var data = Capture(settings);
            Assert.AreEqual((1280, 720), (data.width.Value, data.height.Value));
            Assert.AreEqual(WindowMode.Exclusive, data.windowMode);
            Assert.AreEqual(false, data.vsync);
            Assert.AreEqual(120, data.frameRateLimit);
            Assert.AreEqual("Low", data.quality);
            Assert.AreEqual(true, data.confineCursor);

            settings.SetResolution(new Vector2Int(1920, 1080));
            settings.SetWindowMode(WindowMode.Borderless);
            settings.SetVSync(true);
            settings.SetFrameRateLimit(0);
            settings.SetQuality("PC");
            settings.SetConfineCursor(false);
            data = Capture(settings);
            Assert.IsNull(data.width);
            Assert.IsNull(data.height);
            Assert.IsNull(data.windowMode);
            Assert.IsNull(data.vsync);
            Assert.IsNull(data.frameRateLimit);
            Assert.IsNull(data.quality);
            Assert.IsNull(data.confineCursor);
        }

        [Test]
        public void Changed_IsPublishedAfterApplying_OnlyWhenTheStateChanges()
        {
            var settings = Restored(new DisplaySettingsData());
            _changes.Clear();
            _display.Calls.Clear();

            settings.SetVSync(false);
            settings.SetVSync(false);
            settings.SetQuality("PC");

            Assert.AreEqual(1, _changes.Count);
            Assert.IsTrue(_changes[0].Previous.VSync);
            Assert.IsFalse(_changes[0].Current.VSync);
            Assert.AreEqual(settings.Current, _changes[0].Current);
            CollectionAssert.Contains(_display.Calls, "pacing 0 -1");
        }

        [Test]
        public void FrameRateLimit_IsIgnoredWhileVSyncIsOn()
        {
            var settings = Restored(new DisplaySettingsData());

            settings.SetFrameRateLimit(60);
            Assert.AreEqual("pacing 1 -1", LastPacing());

            settings.SetVSync(false);
            Assert.AreEqual("pacing 0 60", LastPacing());

            settings.SetFrameRateLimit(0);
            Assert.AreEqual("pacing 0 -1", LastPacing());
        }

        [Test]
        public void InvalidSavedChoices_FallBackForTheSession_AndAreKept()
        {
            _display.SupportsExclusive = false;
            var saved = new DisplaySettingsData
            {
                width = 3840, height = 2160, windowMode = WindowMode.Exclusive, frameRateLimit = 5, quality = "Ultra",
            };
            LogAssert.Expect(LogType.Warning, new Regex("Exclusive is not offered"));
            LogAssert.Expect(LogType.Warning, new Regex("3840x2160 is not offered"));
            LogAssert.Expect(LogType.Warning, new Regex("frame rate limit 5"));
            LogAssert.Expect(LogType.Warning, new Regex("no quality level 'Ultra'"));

            var settings = Restored(saved);

            Assert.AreEqual(new DisplayState(new Vector2Int(1920, 1080), WindowMode.Borderless, true, 0, "PC", false), settings.Current);
            var data = Capture(settings);
            Assert.AreEqual((3840, 2160), (data.width.Value, data.height.Value), "Plugging the larger monitor back in brings the choice back.");
            Assert.AreEqual("Ultra", data.quality);
        }

        [Test]
        public void Setters_RejectWhatIsNotOffered()
        {
            _display.SupportsExclusive = false;
            var settings = Restored(new DisplaySettingsData());

            Assert.Throws<ArgumentException>(() => settings.SetResolution(new Vector2Int(2560, 1440)));
            Assert.Throws<ArgumentException>(() => settings.SetWindowMode(WindowMode.Exclusive));
            Assert.Throws<ArgumentException>(() => settings.SetQuality("Ultra"));
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.SetFrameRateLimit(10));
            CollectionAssert.AreEqual(new[] { WindowMode.Windowed, WindowMode.Borderless }, settings.WindowModes);
        }

        [Test]
        public void Mobile_HasNoWindow_AndNoLimitMeansTheRefreshRate()
        {
            _display.IsMobile = true;
            _display.QualityLevels = new[] { "Mobile" };
            _display.StartupQuality = "Mobile";
            var settings = Restored(new DisplaySettingsData { vsync = true, confineCursor = true });

            Assert.IsFalse(settings.HasWindow);
            CollectionAssert.IsEmpty(settings.WindowModes);
            CollectionAssert.IsEmpty(settings.Resolutions);
            Assert.AreEqual(60, settings.Current.FrameRateLimit, "Unity's own mobile default would be 30.");
            Assert.IsFalse(settings.Current.VSync);
            Assert.IsFalse(_input.ConfineCursor);
            CollectionAssert.AreEqual(new[] { "quality Mobile", "pacing 0 60" }, _display.Calls);

            settings.SetFrameRateLimit(0);
            Assert.AreEqual("pacing 0 144", LastPacing(), "-1 would be 30 frames per second on mobile.");
            Assert.Throws<InvalidOperationException>(() => settings.SetResolution(new Vector2Int(1280, 720)));
            Assert.Throws<InvalidOperationException>(() => settings.SetWindowMode(WindowMode.Windowed));
            Assert.Throws<InvalidOperationException>(() => settings.SetVSync(false));
            Assert.Throws<InvalidOperationException>(() => settings.SetConfineCursor(true));
        }

        [Test]
        public void ConfineCursor_ReachesTheInputModeService()
        {
            var settings = Restored(new DisplaySettingsData());

            settings.SetConfineCursor(true);
            Assert.IsTrue(_input.ConfineCursor);

            settings.ResetToDefaults();
            Assert.IsFalse(_input.ConfineCursor);
        }

        [Test]
        public void ResetToDefaults_ClearsEveryChoice()
        {
            var settings = Restored(new DisplaySettingsData { width = 1280, height = 720, vsync = false, quality = "Low" });

            settings.ResetToDefaults();

            Assert.AreEqual(new DisplayState(new Vector2Int(1920, 1080), WindowMode.Borderless, true, 0, "PC", false), settings.Current);
            var data = Capture(settings);
            Assert.IsNull(data.width);
            Assert.IsNull(data.vsync);
            Assert.IsNull(data.quality);
        }

        [Test]
        public void AnOlderSection_WithoutLaterFields_Loads()
        {
            // As a build that only knew the resolution and the window mode would have written it.
            var section = JObject.Parse("{ \"width\": 1280, \"height\": 720, \"windowMode\": \"Windowed\" }");
            var data = (DisplaySettingsData)SaveJson.FromToken(section, typeof(DisplaySettingsData));

            var settings = Restored(data);

            Assert.AreEqual(new DisplayState(new Vector2Int(1280, 720), WindowMode.Windowed, true, 0, "PC", false), settings.Current);
            Assert.AreEqual("{\"width\":1280,\"height\":720,\"windowMode\":\"Windowed\",\"vsync\":null,\"frameRateLimit\":null,\"quality\":null,\"confineCursor\":null}",
                SaveJson.ToToken(Capture(settings)).ToString(Newtonsoft.Json.Formatting.None));
        }

        [Test]
        public void Dispose_DisposesTheBackend()
        {
            var settings = Restored(new DisplaySettingsData());

            settings.Dispose();

            Assert.IsTrue(_display.Disposed);
        }

        private DisplaySettings Restored(DisplaySettingsData data)
        {
            var settings = new DisplaySettings(_options, _display, _input, _container.Resolve<IPublisher<DisplaySettingsChanged>>());
            ((ISavable)settings).RestoreData(data);
            return settings;
        }

        private static DisplaySettingsData Capture(DisplaySettings settings) => (DisplaySettingsData)((ISavable)settings).CaptureData();

        private string LastPacing() => _display.Calls.FindLast(call => call.StartsWith("pacing"));
    }
}

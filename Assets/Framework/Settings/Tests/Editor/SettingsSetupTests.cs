using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AF.Audio;
using AF.Core.Editor;
using AF.Input;
using AF.Save;
using AF.Settings.Editor;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace AF.Settings.Tests
{
    public class SettingsSetupTests
    {
        [Test]
        public void UseSettings_NeedsSaveAudioAndInputFirst()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();

            StringAssert.Contains("UseSave", Assert.Throws<InvalidOperationException>(() => builder.UseSettings(messagePipe)).Message);
            builder.Register<ISaveRegistry>(_ => throw new NotSupportedException(), Lifetime.Singleton);
            StringAssert.Contains("UseAudio", Assert.Throws<InvalidOperationException>(() => builder.UseSettings(messagePipe)).Message);
            builder.RegisterInstance<IAudioVolumes>(new FakeVolumes(new AudioOptions()));
            StringAssert.Contains("UseInput", Assert.Throws<InvalidOperationException>(() => builder.UseSettings(messagePipe)).Message);
        }

        [Test]
        public void UseSettings_RegistersBothSavables_AndTheDisplaySettings()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            var options = new AudioOptions();
            builder.Register<ISaveRegistry>(_ => throw new NotSupportedException(), Lifetime.Singleton);
            builder.RegisterInstance(options);
            builder.RegisterInstance<IAudioVolumes>(new FakeVolumes(options));
            builder.RegisterInstance<IInputModeService>(new FakeInputModes());

            builder.UseSettings(messagePipe);

            using var container = builder.Build();
            var savables = container.Resolve<IReadOnlyList<ISavable>>();
            CollectionAssert.AreEquivalent(new[] { "settings.display", "settings.audio" }, savables.Select(savable => savable.Key));
            Assert.IsTrue(savables.All(savable => savable.Document == SaveDocument.Device));
            Assert.AreSame(container.Resolve<IDisplaySettings>(), savables.Single(savable => savable.Key == "settings.display"));
        }

        [Test]
        public void UseSettings_RejectsDefaultsTheSettersWouldRefuse()
        {
            foreach (var options in new[] { new SettingsOptions { FrameRateLimit = 10 }, new SettingsOptions { MobileFrameRateLimit = -1 } })
            {
                var builder = new ContainerBuilder();
                var messagePipe = builder.RegisterMessagePipe();
                builder.Register<ISaveRegistry>(_ => throw new NotSupportedException(), Lifetime.Singleton);
                builder.RegisterInstance<IAudioVolumes>(new FakeVolumes(new AudioOptions()));
                builder.RegisterInstance<IInputModeService>(new FakeInputModes());

                var exception = Assert.Throws<ArgumentException>(() => builder.UseSettings(messagePipe, options));
                StringAssert.Contains("FrameRateLimit", exception.Message);
            }
        }

        [Test]
        public void DirectDisplayWrites_AreFoundInCompiledCode()
        {
            var report = new ValidationReport();
            DirectDisplayWriteValidator.Check(WithNestedTypes(typeof(DirectWrites)), report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(10, report.ErrorCount, messages);
            StringAssert.Contains("DirectWrites.Resize calls Screen.SetResolution()", messages);
            StringAssert.Contains("DirectWrites.Fullscreen calls Screen.fullScreenMode", messages);
            StringAssert.Contains("DirectWrites.Quality calls QualitySettings.SetQualityLevel()", messages);
            StringAssert.Contains("DirectWrites.VSyncLater calls QualitySettings.vSyncCount", messages);
            StringAssert.Contains("DirectWrites.FrameRate calls Application.targetFrameRate", messages);
            StringAssert.Contains("DirectWrites.Confine calls IInputModeService.ConfineCursor", messages);
            StringAssert.Contains("DirectWrites.FullscreenFlag calls Screen.fullScreen", messages);
            StringAssert.Contains("DirectWrites.Increase calls QualitySettings.IncreaseLevel()", messages);
            StringAssert.Contains("DirectWrites.Decrease calls QualitySettings.DecreaseLevel()", messages);
            StringAssert.Contains("DirectWrites.CurrentLevel calls QualitySettings.currentLevel", messages);
        }

        [Test]
        public void ExemptAndReadingCode_IsNotReported()
        {
            var report = new ValidationReport();
            DirectDisplayWriteValidator.Check(WithNestedTypes(typeof(ExemptWrites), typeof(Reads)), report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        private static IEnumerable<Type> WithNestedTypes(params Type[] types) =>
            types.SelectMany(type => new[] { type }.Concat(WithNestedTypes(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))));

        private static class DirectWrites
        {
            public static void Resize() => Screen.SetResolution(1280, 720, FullScreenMode.Windowed);

            public static void Fullscreen() => Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

            public static void Quality() => QualitySettings.SetQualityLevel(0);

            public static Action VSyncLater() => () => QualitySettings.vSyncCount = 0;

            public static void FrameRate() => Application.targetFrameRate = 30;

            public static void Confine(IInputModeService modes) => modes.ConfineCursor = true;

            public static void FullscreenFlag() => Screen.fullScreen = true;

            public static void Increase() => QualitySettings.IncreaseLevel();

            public static void Decrease() => QualitySettings.DecreaseLevel(true);

#pragma warning disable CS0618 // obsolete, still a write the validator must catch
            public static void CurrentLevel() => QualitySettings.currentLevel = QualityLevel.Fastest;
#pragma warning restore CS0618
        }

        [DisplaySettingsExempt("test: a benchmark scene")]
        private static class ExemptWrites
        {
            public static void Resize() => Screen.SetResolution(1280, 720, FullScreenMode.Windowed);

            public static Action FrameRateLater() => () => Application.targetFrameRate = 30;
        }

        private static class Reads
        {
            public static int Width() => Screen.width;

            public static int Level() => QualitySettings.GetQualityLevel() + QualitySettings.vSyncCount + Application.targetFrameRate;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using AF.Audio.Editor;
using AF.Core.Editor;
using NUnit.Framework;
using UnityEngine;

namespace AF.Audio.Tests
{
    public class AudioValidatorTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
                Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void CueWithoutClipsOrOutput_IsAnError()
        {
            var cue = Track(SoundCue.Create(null));
            var report = new ValidationReport();

            AudioValidator.CheckCue(cue, "Cue", report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(2, report.ErrorCount, messages);
            StringAssert.Contains("has no clips", messages);
            StringAssert.Contains("no output mixer group", messages);
        }

        [Test]
        public void CueWithAnEmptyClipSlot_IsAnError()
        {
            var clip = Track(AudioClip.Create("Clip", 1, 1, 44100, false));
            var cue = Track(SoundCue.Create(null, false, false, clip, null));
            var report = new ValidationReport();

            AudioValidator.CheckCue(cue, "Cue", report);

            StringAssert.Contains("has an empty clip slot", report.ToString());
        }

        [Test]
        public void ListenerAndUnroutedSource_AreErrors()
        {
            var camera = Track(new GameObject("Camera"));
            camera.AddComponent<AudioListener>();
            var speaker = new GameObject("Speaker");
            speaker.transform.SetParent(camera.transform);
            speaker.AddComponent<AudioSource>();
            var report = new ValidationReport();

            AudioValidator.CheckObjects(new[] { camera }, "Scene", report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(2, report.ErrorCount, messages);
            StringAssert.Contains("an AudioListener", messages);
            StringAssert.Contains("an AudioSource without an output mixer group", messages);
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}

using System.Linq;
using NUnit.Framework;

namespace AF.Audio.Tests
{
    public class AudioSetupTests
    {
        private static readonly AudioChannel _master = new AudioChannel("Master", "MasterVolume");
        private static readonly AudioChannel _music = new AudioChannel("Music", "MusicVolume");
        private static readonly string[] _groups = { "Master", "Music", "SFX" };
        private static readonly string[] _exposed = { "MasterVolume", "MusicVolume" };

        [Test]
        public void MatchingMixer_HasNoProblems()
        {
            var options = new AudioOptions { Music = _music }.Add(_master).Add(_music);

            CollectionAssert.IsEmpty(AudioSetup.FindProblems(options, _groups, _exposed.Contains));
        }

        [Test]
        public void MissingGroupAndParameter_AreReported()
        {
            var options = new AudioOptions { Music = _music }.Add(_music).Add(new AudioChannel("Voice", "VoiceVolume"));

            var problems = AudioSetup.FindProblems(options, _groups, _exposed.Contains);

            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
            StringAssert.Contains("No mixer group is named 'Voice'. Groups: Master, Music, SFX.", problems[0]);
            StringAssert.Contains("'VoiceVolume'", problems[1]);
        }

        [Test]
        public void DuplicateGroupNames_AreReported()
        {
            var options = new AudioOptions { Music = _music }.Add(_music);

            var problems = AudioSetup.FindProblems(options, new[] { "Master", "Music", "Music" }, _exposed.Contains);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("2 mixer groups are named 'Music'", problems[0]);
        }

        [Test]
        public void MusicChannel_MustBeSetAndAdded()
        {
            StringAssert.Contains("No music channel", AudioSetup.FindProblems(new AudioOptions().Add(_master), _groups, _exposed.Contains).Single());
            StringAssert.Contains("not among the channels",
                AudioSetup.FindProblems(new AudioOptions { Music = _music }.Add(_master), _groups, _exposed.Contains).Single());
        }
    }
}
